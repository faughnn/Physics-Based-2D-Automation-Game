using System;
using Unity.Collections;
using UnityEngine;

namespace FallingSand
{
    /// <summary>
    /// Manages all lifts in the world.
    /// Handles 8x8 block placement/removal, lift merging, and force application.
    /// Lifts are hollow force zones - material passes through and experiences upward force.
    /// </summary>
    public class LiftManager : IDisposable
    {
        private readonly CellWorld world;
        private readonly int width;
        private readonly int height;

        // Lift tile storage: parallel array to cells for O(1) lookup in simulation job
        private NativeArray<LiftTile> liftTiles;

        // Lift structure storage: id → structure data
        private NativeHashMap<ushort, LiftStructure> liftLookup;

        // List of all lifts for iteration
        private NativeList<LiftStructure> lifts;

        // Next available lift ID
        private ushort nextLiftId = 1;

        // Default lift force (gravity is 17, so 20 gives net -3 upward)
        public const byte DefaultLiftForce = 20;

        // Force multiplier for clusters (1.2 = 120% of gravity)
        public const float LiftForceMultiplier = 1.2f;

        public NativeArray<LiftTile> LiftTiles => liftTiles;

        public LiftManager(CellWorld world, int initialCapacity = 64)
        {
            this.world = world;
            this.width = world.width;
            this.height = world.height;

            liftTiles = new NativeArray<LiftTile>(width * height, Allocator.Persistent);
            liftLookup = new NativeHashMap<ushort, LiftStructure>(initialCapacity, Allocator.Persistent);
            lifts = new NativeList<LiftStructure>(initialCapacity, Allocator.Persistent);
        }

        /// <summary>
        /// Snaps a coordinate to the 8x8 grid.
        /// </summary>
        public static int SnapToGrid(int coord)
        {
            if (coord < 0)
                return ((coord - LiftStructure.Width + 1) / LiftStructure.Width) * LiftStructure.Width;
            return (coord / LiftStructure.Width) * LiftStructure.Width;
        }

        /// <summary>
        /// Places an 8x8 lift block at the specified position.
        /// Position is snapped to 8x8 grid. Adjacent lifts merge vertically.
        /// </summary>
        public bool PlaceLift(int x, int y)
        {
            // Snap to 8x8 grid
            int gridX = SnapToGrid(x);
            int gridY = SnapToGrid(y);

            // Check bounds for entire 8x8 block
            if (!world.IsInBounds(gridX, gridY) ||
                !world.IsInBounds(gridX + LiftStructure.Width - 1, gridY + LiftStructure.Height - 1))
                return false;

            // Check if entire 8x8 area is clear (no existing lifts or solid materials)
            for (int dy = 0; dy < LiftStructure.Height; dy++)
            {
                for (int dx = 0; dx < LiftStructure.Width; dx++)
                {
                    int cx = gridX + dx;
                    int cy = gridY + dy;
                    int posKey = cy * width + cx;

                    if (liftTiles[posKey].liftId != 0)
                        return false;

                    byte existingMaterial = world.GetCell(cx, cy);
                    // Allow Air or existing lift materials (for state recovery)
                    if (existingMaterial != Materials.Air &&
                        !Materials.IsLift(existingMaterial))
                        return false;
                }
            }

            // Check for adjacent lifts to merge (same X, vertically adjacent)
            ushort topLiftId = 0;
            ushort bottomLiftId = 0;
            LiftStructure topLift = default;
            LiftStructure bottomLift = default;

            // Check top neighbor (at gridX, gridY - Height)
            int topY = gridY - LiftStructure.Height;
            if (topY >= 0)
            {
                int topPosKey = topY * width + gridX;
                LiftTile topTile = liftTiles[topPosKey];
                if (topTile.liftId != 0 && liftLookup.TryGetValue(topTile.liftId, out topLift))
                {
                    topLiftId = topTile.liftId;
                }
            }

            // Check bottom neighbor (at gridX, gridY + Height)
            int bottomY = gridY + LiftStructure.Height;
            if (bottomY < height)
            {
                int bottomPosKey = bottomY * width + gridX;
                LiftTile bottomTile = liftTiles[bottomPosKey];
                if (bottomTile.liftId != 0 && liftLookup.TryGetValue(bottomTile.liftId, out bottomLift))
                {
                    bottomLiftId = bottomTile.liftId;
                }
            }

            ushort liftId;
            LiftStructure lift;

            if (topLiftId != 0 && bottomLiftId != 0 && topLiftId != bottomLiftId)
            {
                // Merging three lifts: top + new + bottom
                // Extend top lift to include new block and bottom lift
                lift = topLift;
                lift.maxY = bottomLift.maxY;
                liftId = topLiftId;

                // Update all tiles from bottom lift to point to top lift
                UpdateLiftTileIds(gridX, bottomLift.minY, bottomLift.maxY + LiftStructure.Height - 1, topLiftId);

                // Remove bottom lift structure
                RemoveLiftStructure(bottomLiftId);

                // Update top lift in lookup
                liftLookup.Remove(topLiftId);
                liftLookup.Add(topLiftId, lift);
                UpdateLiftInList(topLiftId, lift);
            }
            else if (topLiftId != 0)
            {
                // Extend top lift downward to include new block
                lift = topLift;
                lift.maxY = gridY;
                liftId = topLiftId;

                liftLookup.Remove(topLiftId);
                liftLookup.Add(topLiftId, lift);
                UpdateLiftInList(topLiftId, lift);
            }
            else if (bottomLiftId != 0)
            {
                // Extend bottom lift upward to include new block
                lift = bottomLift;
                lift.minY = gridY;
                liftId = bottomLiftId;

                liftLookup.Remove(bottomLiftId);
                liftLookup.Add(bottomLiftId, lift);
                UpdateLiftInList(bottomLiftId, lift);
            }
            else
            {
                // Create new lift structure
                liftId = nextLiftId++;
                lift = new LiftStructure
                {
                    id = liftId,
                    tileX = gridX,
                    minY = gridY,
                    maxY = gridY,
                    liftForce = DefaultLiftForce,
                };

                liftLookup.Add(liftId, lift);
                lifts.Add(lift);
            }

            // Fill the 8x8 area with lift tiles and place lift materials for rendering
            for (int dy = 0; dy < LiftStructure.Height; dy++)
            {
                for (int dx = 0; dx < LiftStructure.Width; dx++)
                {
                    int cx = gridX + dx;
                    int cy = gridY + dy;
                    int posKey = cy * width + cx;

                    // Place lift material with arrow pattern for visualization
                    byte liftMaterial = GetLiftMaterialForPattern(cx, cy);

                    liftTiles[posKey] = new LiftTile
                    {
                        liftId = liftId,
                        materialId = liftMaterial,
                    };

                    world.SetCell(cx, cy, liftMaterial);
                    world.MarkDirty(cx, cy);
                }
            }

            // Mark chunks as having structure so they stay active
            MarkChunksHasStructure(gridX, gridY, LiftStructure.Width, LiftStructure.Height);

            return true;
        }

        /// <summary>
        /// Removes the 8x8 lift block at the specified position.
        /// Position is snapped to 8x8 grid. May split a merged lift into two.
        /// </summary>
        public bool RemoveLift(int x, int y)
        {
            // Snap to 8x8 grid
            int gridX = SnapToGrid(x);
            int gridY = SnapToGrid(y);

            int posKey = gridY * width + gridX;

            // Check if there's a lift at this grid position
            LiftTile tile = liftTiles[posKey];
            if (tile.liftId == 0)
                return false;

            ushort liftId = tile.liftId;
            if (!liftLookup.TryGetValue(liftId, out LiftStructure lift))
                return false;

            // Clear the 8x8 area of lift tiles and reset materials to Air
            LiftTile emptyTile = default;
            for (int dy = 0; dy < LiftStructure.Height; dy++)
            {
                for (int dx = 0; dx < LiftStructure.Width; dx++)
                {
                    int cx = gridX + dx;
                    int cy = gridY + dy;
                    int cellPosKey = cy * width + cx;

                    liftTiles[cellPosKey] = emptyTile;
                    world.SetCell(cx, cy, Materials.Air);
                    world.MarkDirty(cx, cy);
                }
            }

            // Handle lift splitting
            bool hasTopPart = lift.minY < gridY;
            bool hasBottomPart = lift.maxY > gridY;

            if (hasTopPart && hasBottomPart)
            {
                // Split into two lifts: keep top part in existing lift, create new for bottom
                LiftStructure topLift = lift;
                topLift.maxY = gridY - LiftStructure.Height;

                liftLookup.Remove(liftId);
                liftLookup.Add(liftId, topLift);
                UpdateLiftInList(liftId, topLift);

                // Create new lift for bottom part
                ushort newLiftId = nextLiftId++;
                LiftStructure bottomLift = new LiftStructure
                {
                    id = newLiftId,
                    tileX = lift.tileX,
                    minY = gridY + LiftStructure.Height,
                    maxY = lift.maxY,
                    liftForce = lift.liftForce,
                };

                liftLookup.Add(newLiftId, bottomLift);
                lifts.Add(bottomLift);

                // Update tiles in bottom part to point to new lift
                UpdateLiftTileIds(lift.tileX, bottomLift.minY, bottomLift.maxY + LiftStructure.Height - 1, newLiftId);
            }
            else if (hasTopPart)
            {
                // Only top part remains
                LiftStructure topLift = lift;
                topLift.maxY = gridY - LiftStructure.Height;

                liftLookup.Remove(liftId);
                liftLookup.Add(liftId, topLift);
                UpdateLiftInList(liftId, topLift);
            }
            else if (hasBottomPart)
            {
                // Only bottom part remains
                LiftStructure bottomLift = lift;
                bottomLift.minY = gridY + LiftStructure.Height;

                liftLookup.Remove(liftId);
                liftLookup.Add(liftId, bottomLift);
                UpdateLiftInList(liftId, bottomLift);
            }
            else
            {
                // This was the only block in the lift, remove entirely
                RemoveLiftStructure(liftId);
            }

            // Update chunk structure flags (may clear flag if no lifts remain)
            UpdateChunksStructureFlag(gridX, gridY, LiftStructure.Width, LiftStructure.Height);

            return true;
        }

        /// <summary>
        /// Checks if there's a lift tile at the specified position.
        /// </summary>
        public bool HasLiftAt(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
                return false;
            return liftTiles[y * width + x].liftId != 0;
        }

        /// <summary>
        /// Gets the lift tile at the specified position.
        /// </summary>
        public LiftTile GetLiftTile(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
                return default;
            return liftTiles[y * width + x];
        }

        /// <summary>
        /// Gets the lift structure by ID, if it exists.
        /// </summary>
        public bool TryGetLift(ushort liftId, out LiftStructure lift)
        {
            return liftLookup.TryGetValue(liftId, out lift);
        }

        /// <summary>
        /// Gets all lift structures for iteration.
        /// </summary>
        public NativeList<LiftStructure> GetLifts() => lifts;

        /// <summary>
        /// Gets the number of lift structures.
        /// </summary>
        public int LiftCount => lifts.Length;

        /// <summary>
        /// Apply upward force to clusters within lift zones.
        /// Force slightly exceeds gravity so clusters gradually accelerate upward.
        /// </summary>
        public void ApplyForcesToClusters(ClusterManager clusterManager, int worldWidth, int worldHeight)
        {
            if (clusterManager == null || lifts.Length == 0) return;

            foreach (var cluster in clusterManager.AllClusters)
            {
                if (cluster.rb == null) continue;

                bool foundLift = false;

                for (int i = 0; i < lifts.Length; i++)
                {
                    LiftStructure lift = lifts[i];
                    if (ClusterInLiftZone(cluster, lift, worldWidth, worldHeight))
                    {
                        foundLift = true;

                        // Apply upward force that slightly exceeds gravity
                        // This mirrors how loose cells work - forces fight
                        float liftForce = -Physics2D.gravity.y * LiftForceMultiplier * cluster.rb.mass;
                        cluster.rb.AddForce(new Vector2(0, liftForce));
                        break;
                    }
                }

                cluster.isOnLift = foundLift;
            }
        }

        /// <summary>
        /// Applies lift force to a Rigidbody2D if it's within a lift zone.
        /// Returns true if the body is in a lift zone, false otherwise.
        /// </summary>
        public bool ApplyLiftForce(Rigidbody2D rb, int worldWidth, int worldHeight)
        {
            if (rb == null || lifts.Length == 0) return false;

            Vector2 cellPos = CoordinateUtils.WorldToCellFloat(rb.position, worldWidth, worldHeight);

            for (int i = 0; i < lifts.Length; i++)
            {
                LiftStructure lift = lifts[i];
                if (PositionInLiftZone(cellPos, lift))
                {
                    float liftForce = -Physics2D.gravity.y * LiftForceMultiplier * rb.mass;
                    rb.AddForce(new Vector2(0, liftForce));
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if a cell position is within the given lift's zone.
        /// </summary>
        private bool PositionInLiftZone(Vector2 cellPos, LiftStructure lift)
        {
            int liftMinX = lift.tileX;
            int liftMaxX = lift.tileX + LiftStructure.Width;
            int liftMinY = lift.minY;
            int liftMaxY = lift.maxY + LiftStructure.Height;

            return cellPos.x >= liftMinX && cellPos.x < liftMaxX &&
                   cellPos.y >= liftMinY && cellPos.y < liftMaxY;
        }

        /// <summary>
        /// Check if cluster's center is within lift zone bounds.
        /// </summary>
        private bool ClusterInLiftZone(ClusterData cluster, LiftStructure lift, int worldWidth, int worldHeight)
        {
            Vector2 cellPos = CoordinateUtils.WorldToCellFloat(cluster.Position, worldWidth, worldHeight);
            return PositionInLiftZone(cellPos, lift);
        }

        // 8x8 upward arrow pattern (1 = light, 0 = dark)
        private static readonly byte[] ArrowPattern =
        {
            0, 0, 0, 1, 1, 0, 0, 0,
            0, 0, 1, 1, 1, 1, 0, 0,
            0, 1, 1, 1, 1, 1, 1, 0,
            1, 1, 0, 1, 1, 0, 1, 1,
            0, 0, 0, 1, 1, 0, 0, 0,
            0, 0, 0, 1, 1, 0, 0, 0,
            0, 0, 0, 1, 1, 0, 0, 0,
            0, 0, 0, 1, 1, 0, 0, 0,
        };

        /// <summary>
        /// Gets the material ID for a lift cell based on position.
        /// </summary>
        private static byte GetLiftMaterialForPattern(int x, int y)
        {
            int localX = ((x % 8) + 8) % 8;
            int localY = ((y % 8) + 8) % 8;

            bool isLight = ArrowPattern[localY * 8 + localX] == 1;

            return isLight ? Materials.LiftUpLight : Materials.LiftUp;
        }

        private void UpdateLiftTileIds(int tileX, int minY, int maxY, ushort newLiftId)
        {
            for (int cy = minY; cy <= maxY; cy++)
            {
                for (int dx = 0; dx < LiftStructure.Width; dx++)
                {
                    int cx = tileX + dx;
                    int posKey = cy * width + cx;
                    LiftTile existing = liftTiles[posKey];
                    existing.liftId = newLiftId;
                    liftTiles[posKey] = existing;
                }
            }
        }

        private void UpdateLiftInList(ushort liftId, LiftStructure newLift)
        {
            for (int i = 0; i < lifts.Length; i++)
            {
                if (lifts[i].id == liftId)
                {
                    lifts[i] = newLift;
                    return;
                }
            }
        }

        private void RemoveLiftStructure(ushort liftId)
        {
            if (!liftLookup.ContainsKey(liftId))
                return;

            liftLookup.Remove(liftId);

            for (int i = 0; i < lifts.Length; i++)
            {
                if (lifts[i].id == liftId)
                {
                    lifts.RemoveAtSwapBack(i);
                    break;
                }
            }
        }

        private void MarkChunksHasStructure(int cellX, int cellY, int areaWidth, int areaHeight)
        {
            // Find all chunks that overlap with this area
            int startChunkX = cellX / CellWorld.ChunkSize;
            int startChunkY = cellY / CellWorld.ChunkSize;
            int endChunkX = (cellX + areaWidth - 1) / CellWorld.ChunkSize;
            int endChunkY = (cellY + areaHeight - 1) / CellWorld.ChunkSize;

            for (int cy = startChunkY; cy <= endChunkY; cy++)
            {
                for (int cx = startChunkX; cx <= endChunkX; cx++)
                {
                    if (cx >= 0 && cx < world.chunksX && cy >= 0 && cy < world.chunksY)
                    {
                        int chunkIndex = cy * world.chunksX + cx;
                        ChunkState chunk = world.chunks[chunkIndex];
                        chunk.flags |= ChunkFlags.HasStructure;
                        world.chunks[chunkIndex] = chunk;
                    }
                }
            }
        }

        private void UpdateChunksStructureFlag(int cellX, int cellY, int areaWidth, int areaHeight)
        {
            // Find all chunks that overlap with this area
            int startChunkX = cellX / CellWorld.ChunkSize;
            int startChunkY = cellY / CellWorld.ChunkSize;
            int endChunkX = (cellX + areaWidth - 1) / CellWorld.ChunkSize;
            int endChunkY = (cellY + areaHeight - 1) / CellWorld.ChunkSize;

            for (int chunkY = startChunkY; chunkY <= endChunkY; chunkY++)
            {
                for (int chunkX = startChunkX; chunkX <= endChunkX; chunkX++)
                {
                    if (chunkX >= 0 && chunkX < world.chunksX && chunkY >= 0 && chunkY < world.chunksY)
                    {
                        UpdateSingleChunkStructureFlag(chunkX, chunkY);
                    }
                }
            }
        }

        private void UpdateSingleChunkStructureFlag(int chunkX, int chunkY)
        {
            int chunkStartX = chunkX * CellWorld.ChunkSize;
            int chunkStartY = chunkY * CellWorld.ChunkSize;
            int chunkEndX = Math.Min(chunkStartX + CellWorld.ChunkSize, width);
            int chunkEndY = Math.Min(chunkStartY + CellWorld.ChunkSize, height);

            bool hasStructure = false;
            for (int y = chunkStartY; y < chunkEndY && !hasStructure; y++)
            {
                for (int x = chunkStartX; x < chunkEndX && !hasStructure; x++)
                {
                    if (liftTiles[y * width + x].liftId != 0)
                    {
                        hasStructure = true;
                    }
                }
            }

            int chunkIndex = chunkY * world.chunksX + chunkX;
            ChunkState chunk = world.chunks[chunkIndex];

            if (hasStructure)
                chunk.flags |= ChunkFlags.HasStructure;
            else
                chunk.flags &= unchecked((byte)~ChunkFlags.HasStructure);

            world.chunks[chunkIndex] = chunk;
        }

        public void Dispose()
        {
            if (liftTiles.IsCreated) liftTiles.Dispose();
            if (liftLookup.IsCreated) liftLookup.Dispose();
            if (lifts.IsCreated) lifts.Dispose();
        }
    }
}
