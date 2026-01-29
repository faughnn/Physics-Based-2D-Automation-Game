using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace FallingSand
{
    /// <summary>
    /// Manages all conveyor belts in the world.
    /// Handles 8x8 block placement/removal, belt merging, and simulation.
    /// </summary>
    public class BeltManager : IDisposable
    {
        private readonly CellWorld world;
        private readonly TerrainColliderManager terrainColliders;
        private readonly int width;

        // Belt tile storage: position (y * width + x) → tile data
        private NativeHashMap<int, BeltTile> beltTiles;

        // Belt structure storage: id → structure data
        private NativeHashMap<ushort, BeltStructure> beltLookup;

        // List of all belts for iteration during simulation
        private NativeList<BeltStructure> belts;

        // Next available belt ID
        private ushort nextBeltId = 1;

        // Tracked ghost block origins (gridY * width + gridX) for O(1) iteration
        private NativeHashSet<int> ghostBlockOrigins;

        // Default belt speed (frames per move)
        public const byte DefaultSpeed = 3;

        // Velocity imparted to clusters resting on belts (world units per second)
        // This directly sets velocity rather than applying force (mimics belt carrying objects)
        public const float BeltCarrySpeed = 30f;

        public BeltManager(CellWorld world, TerrainColliderManager terrainColliders, int initialCapacity = 64)
        {
            this.world = world;
            this.terrainColliders = terrainColliders;
            this.width = world.width;

            beltTiles = new NativeHashMap<int, BeltTile>(initialCapacity * 64, Allocator.Persistent);
            beltLookup = new NativeHashMap<ushort, BeltStructure>(initialCapacity, Allocator.Persistent);
            belts = new NativeList<BeltStructure>(initialCapacity, Allocator.Persistent);
            ghostBlockOrigins = new NativeHashSet<int>(initialCapacity, Allocator.Persistent);
        }

        /// <summary>
        /// Snaps a coordinate to the 8x8 grid.
        /// </summary>
        public static int SnapToGrid(int coord)
        {
            // Handle negative coordinates correctly
            if (coord < 0)
                return ((coord - BeltStructure.Width + 1) / BeltStructure.Width) * BeltStructure.Width;
            return (coord / BeltStructure.Width) * BeltStructure.Width;
        }

        /// <summary>
        /// Places an 8x8 belt block at the specified position.
        /// Position is snapped to 8x8 grid. Adjacent belts with same direction merge.
        /// </summary>
        public bool PlaceBelt(int x, int y, sbyte direction)
        {
            // Validate direction
            if (direction != 1 && direction != -1)
                return false;

            // Snap to 8x8 grid
            int gridX = SnapToGrid(x);
            int gridY = SnapToGrid(y);

            // Check bounds for entire 8x8 block
            if (!world.IsInBounds(gridX, gridY) ||
                !world.IsInBounds(gridX + BeltStructure.Width - 1, gridY + BeltStructure.Height - 1))
                return false;

            // Check if entire 8x8 area is placeable (Air or soft terrain)
            bool anyGhost = false;
            for (int dy = 0; dy < BeltStructure.Height; dy++)
            {
                for (int dx = 0; dx < BeltStructure.Width; dx++)
                {
                    int cx = gridX + dx;
                    int cy = gridY + dy;
                    int posKey = cy * width + cx;

                    if (beltTiles.ContainsKey(posKey))
                        return false;

                    byte existingMaterial = world.GetCell(cx, cy);
                    if (existingMaterial == Materials.Air)
                        continue;

                    if (Materials.IsSoftTerrain(existingMaterial))
                    {
                        anyGhost = true;
                        continue;
                    }

                    // Hard material (Stone, Wall, belt, lift, etc.) — reject
                    return false;
                }
            }

            // Check for adjacent belts to merge (same Y, same direction)
            ushort leftBeltId = 0;
            ushort rightBeltId = 0;
            BeltStructure leftBelt = default;
            BeltStructure rightBelt = default;

            // Check left neighbor (at gridX - Width, gridY)
            int leftX = gridX - BeltStructure.Width;
            if (leftX >= 0)
            {
                int leftPosKey = gridY * width + leftX;
                if (beltTiles.TryGetValue(leftPosKey, out BeltTile leftTile))
                {
                    if (leftTile.direction == direction && beltLookup.TryGetValue(leftTile.beltId, out leftBelt))
                    {
                        leftBeltId = leftTile.beltId;
                    }
                }
            }

            // Check right neighbor (at gridX + Width, gridY)
            int rightX = gridX + BeltStructure.Width;
            if (rightX < world.width)
            {
                int rightPosKey = gridY * width + rightX;
                if (beltTiles.TryGetValue(rightPosKey, out BeltTile rightTile))
                {
                    if (rightTile.direction == direction && beltLookup.TryGetValue(rightTile.beltId, out rightBelt))
                    {
                        rightBeltId = rightTile.beltId;
                    }
                }
            }

            ushort beltId;
            BeltStructure belt;

            if (leftBeltId != 0 && rightBeltId != 0 && leftBeltId != rightBeltId)
            {
                // Merging three belts: left + new + right
                // Extend left belt to include new block and right belt
                belt = leftBelt;
                belt.maxX = rightBelt.maxX;
                beltId = leftBeltId;

                // Update all tiles from right belt to point to left belt
                UpdateBeltTileIds(rightBelt.minX, rightBelt.maxX + BeltStructure.Width - 1, gridY, leftBeltId, direction);

                // Remove right belt structure
                RemoveBeltStructure(rightBeltId);

                // Update left belt in lookup
                beltLookup.Remove(leftBeltId);
                beltLookup.Add(leftBeltId, belt);
                UpdateBeltInList(leftBeltId, belt);
            }
            else if (leftBeltId != 0)
            {
                // Extend left belt to include new block
                belt = leftBelt;
                belt.maxX = gridX;
                beltId = leftBeltId;

                beltLookup.Remove(leftBeltId);
                beltLookup.Add(leftBeltId, belt);
                UpdateBeltInList(leftBeltId, belt);
            }
            else if (rightBeltId != 0)
            {
                // Extend right belt to include new block
                belt = rightBelt;
                belt.minX = gridX;
                beltId = rightBeltId;

                beltLookup.Remove(rightBeltId);
                beltLookup.Add(rightBeltId, belt);
                UpdateBeltInList(rightBeltId, belt);
            }
            else
            {
                // Create new belt structure
                beltId = nextBeltId++;
                belt = new BeltStructure
                {
                    id = beltId,
                    tileY = gridY,
                    minX = gridX,
                    maxX = gridX,
                    direction = direction,
                    speed = DefaultSpeed,
                    frameOffset = (byte)(beltId % DefaultSpeed),
                };

                beltLookup.Add(beltId, belt);
                belts.Add(belt);
            }

            // Fill the 8x8 area with belt tiles
            BeltTile tile = new BeltTile
            {
                direction = direction,
                beltId = beltId,
                isGhost = anyGhost,
            };

            for (int dy = 0; dy < BeltStructure.Height; dy++)
            {
                for (int dx = 0; dx < BeltStructure.Width; dx++)
                {
                    int cx = gridX + dx;
                    int cy = gridY + dy;
                    int posKey = cy * width + cx;

                    beltTiles.Add(posKey, tile);

                    if (!anyGhost)
                    {
                        // Update the cell grid with chevron pattern
                        int cellIndex = cy * width + cx;
                        Cell cell = world.cells[cellIndex];
                        cell.materialId = GetBeltMaterialForChevron(cx, cy, direction);
                        cell.structureId = (byte)StructureType.Belt;
                        world.cells[cellIndex] = cell;

                        world.MarkDirty(cx, cy);
                    }
                }
            }

            if (anyGhost)
                ghostBlockOrigins.Add(gridY * width + gridX);

            // Mark all affected chunks as having a structure
            MarkChunksHasStructure(gridX, gridY, BeltStructure.Width, BeltStructure.Height);

            return true;
        }

        /// <summary>
        /// Removes the 8x8 belt block at the specified position.
        /// Position is snapped to 8x8 grid. May split a merged belt into two.
        /// </summary>
        public bool RemoveBelt(int x, int y)
        {
            // Snap to 8x8 grid
            int gridX = SnapToGrid(x);
            int gridY = SnapToGrid(y);

            int posKey = gridY * width + gridX;

            // Check if there's a belt at this grid position
            if (!beltTiles.TryGetValue(posKey, out BeltTile tile))
                return false;

            ushort beltId = tile.beltId;
            if (!beltLookup.TryGetValue(beltId, out BeltStructure belt))
                return false;

            // Clear the 8x8 area
            bool tileIsGhost = tile.isGhost;
            for (int dy = 0; dy < BeltStructure.Height; dy++)
            {
                for (int dx = 0; dx < BeltStructure.Width; dx++)
                {
                    int cx = gridX + dx;
                    int cy = gridY + dy;
                    int cellPosKey = cy * width + cx;

                    beltTiles.Remove(cellPosKey);

                    if (!tileIsGhost)
                    {
                        // Clear the cell (ghost tiles have no cell data to clear)
                        int cellIndex = cy * width + cx;
                        Cell cell = world.cells[cellIndex];
                        cell.materialId = Materials.Air;
                        cell.structureId = (byte)StructureType.None;
                        world.cells[cellIndex] = cell;

                        world.MarkDirty(cx, cy);
                    }
                }
            }

            if (tileIsGhost)
                ghostBlockOrigins.Remove(gridY * width + gridX);

            // Handle belt splitting
            bool hasLeftPart = belt.minX < gridX;
            bool hasRightPart = belt.maxX > gridX;

            if (hasLeftPart && hasRightPart)
            {
                // Split into two belts: keep left part in existing belt, create new for right
                BeltStructure leftBelt = belt;
                leftBelt.maxX = gridX - BeltStructure.Width;

                beltLookup.Remove(beltId);
                beltLookup.Add(beltId, leftBelt);
                UpdateBeltInList(beltId, leftBelt);

                // Create new belt for right part
                ushort newBeltId = nextBeltId++;
                BeltStructure rightBelt = new BeltStructure
                {
                    id = newBeltId,
                    tileY = belt.tileY,
                    minX = gridX + BeltStructure.Width,
                    maxX = belt.maxX,
                    direction = belt.direction,
                    speed = belt.speed,
                    frameOffset = (byte)(newBeltId % DefaultSpeed),
                };

                beltLookup.Add(newBeltId, rightBelt);
                belts.Add(rightBelt);

                // Update tiles in right part to point to new belt
                UpdateBeltTileIds(rightBelt.minX, rightBelt.maxX + BeltStructure.Width - 1, belt.tileY, newBeltId, belt.direction);
            }
            else if (hasLeftPart)
            {
                // Only left part remains
                BeltStructure leftBelt = belt;
                leftBelt.maxX = gridX - BeltStructure.Width;

                beltLookup.Remove(beltId);
                beltLookup.Add(beltId, leftBelt);
                UpdateBeltInList(beltId, leftBelt);
            }
            else if (hasRightPart)
            {
                // Only right part remains
                BeltStructure rightBelt = belt;
                rightBelt.minX = gridX + BeltStructure.Width;

                beltLookup.Remove(beltId);
                beltLookup.Add(beltId, rightBelt);
                UpdateBeltInList(beltId, rightBelt);
            }
            else
            {
                // This was the only block in the belt, remove entirely
                RemoveBeltStructure(beltId);
            }

            // Update chunk structure flags
            UpdateChunksStructureFlag(gridX, gridY, BeltStructure.Width, BeltStructure.Height);

            return true;
        }

        /// <summary>
        /// Checks if there's a belt tile at the specified position.
        /// </summary>
        public bool HasBeltAt(int x, int y)
        {
            return beltTiles.ContainsKey(y * width + x);
        }

        /// <summary>
        /// Gets the belt tile at the specified position, if any.
        /// </summary>
        public bool TryGetBeltTile(int x, int y, out BeltTile tile)
        {
            return beltTiles.TryGetValue(y * width + x, out tile);
        }

        /// <summary>
        /// Gets the belt structure by ID, if it exists.
        /// </summary>
        public bool TryGetBelt(ushort beltId, out BeltStructure belt)
        {
            return beltLookup.TryGetValue(beltId, out belt);
        }

        /// <summary>
        /// Gets all belt structures for iteration.
        /// </summary>
        public NativeList<BeltStructure> GetBelts() => belts;

        /// <summary>
        /// Gets the number of belt tiles placed (individual cells, not blocks).
        /// </summary>
        public int TileCount => beltTiles.Count;

        /// <summary>
        /// Gets the number of belt structures.
        /// </summary>
        public int BeltCount => belts.Length;

        /// <summary>
        /// Simulates belt movement - moves ALL cells sitting on belt surfaces horizontally.
        /// This includes the entire pile of sand, not just the bottom row.
        /// Call this after the main cell simulation each frame.
        /// </summary>
        public void SimulateBelts(ushort currentFrame)
        {
            for (int i = 0; i < belts.Length; i++)
            {
                BeltStructure belt = belts[i];

                // Check if this belt should move this frame (based on speed and frame offset)
                if ((currentFrame - belt.frameOffset) % belt.speed != 0)
                    continue;

                // The surface row where cells sit is just above the belt (tileY - 1)
                // In this coordinate system, Y=0 is at top, Y increases downward
                // So "above" means smaller Y value
                int surfaceY = belt.tileY - 1;

                // Skip if surface is out of bounds
                if (surfaceY < 0 || surfaceY >= world.height)
                    continue;

                // Scan from minX to maxX + Width - 1 (the full span of the belt)
                int scanMinX = belt.minX;
                int scanMaxX = belt.maxX + BeltStructure.Width - 1;

                // Move cells in belt direction
                // Process columns in opposite direction of movement to avoid double-moving
                if (belt.direction > 0)
                {
                    // Moving right: scan columns from right to left
                    for (int x = scanMaxX; x >= scanMinX; x--)
                    {
                        MoveColumnOnBelt(x, surfaceY, belt.direction);
                    }
                }
                else
                {
                    // Moving left: scan columns from left to right
                    for (int x = scanMinX; x <= scanMaxX; x++)
                    {
                        MoveColumnOnBelt(x, surfaceY, belt.direction);
                    }
                }
            }
        }

        /// <summary>
        /// Schedules a Burst-compiled parallel job to simulate belt movement.
        /// Returns a JobHandle that must be completed before accessing cells/chunks.
        /// </summary>
        public JobHandle ScheduleSimulateBelts(
            NativeArray<Cell> cells,
            NativeArray<ChunkState> chunks,
            NativeArray<MaterialDef> materials,
            int width, int height, int chunksX, int chunksY,
            ushort currentFrame,
            JobHandle dependency = default)
        {
            if (belts.Length == 0)
                return dependency;

            var job = new SimulateBeltsJob
            {
                cells = cells,
                chunks = chunks,
                materials = materials,
                belts = belts.AsArray(),
                beltTiles = beltTiles,
                width = width,
                height = height,
                chunksX = chunksX,
                chunksY = chunksY,
                currentFrame = currentFrame
            };

            // innerloopBatchCount=1 since each belt is independent work
            return job.Schedule(belts.Length, 1, dependency);
        }

        /// <summary>
        /// Moves an entire column of cells above the belt surface.
        /// Scans from surface upward and moves all movable cells.
        /// </summary>
        private void MoveColumnOnBelt(int x, int surfaceY, sbyte direction)
        {
            if (x < 0 || x >= world.width)
                return;

            int targetX = x + direction;
            if (targetX < 0 || targetX >= world.width)
                return;

            // Scan upward from surface (decreasing Y) and move all cells in this column
            for (int y = surfaceY; y >= 0; y--)
            {
                int index = y * width + x;
                Cell cell = world.cells[index];

                // Stop scanning if we hit air (top of pile)
                if (cell.materialId == Materials.Air)
                    break;

                // Skip belt tiles (stop scanning at belt surface)
                if (Materials.IsBelt(cell.materialId))
                    break;

                // Skip cells owned by clusters (rigid bodies)
                if (cell.ownerId != 0)
                    continue;

                MaterialDef mat = world.materials[cell.materialId];

                // Only move powder and liquid
                if (mat.behaviour != BehaviourType.Powder && mat.behaviour != BehaviourType.Liquid)
                    continue;

                int targetIndex = y * width + targetX;
                Cell targetCell = world.cells[targetIndex];

                // Can only move into air
                if (targetCell.materialId != Materials.Air)
                    continue;

                // Swap cells
                world.cells[index] = targetCell;
                world.cells[targetIndex] = cell;

                // Mark dirty for rendering
                world.MarkDirty(x, y);
                world.MarkDirty(targetX, y);
            }
        }

        // 8x8 chevron pattern (1 = light, 0 = dark) - points RIGHT
        private static readonly byte[] ChevronPattern =
        {
            0, 0, 0, 1, 1, 0, 0, 0,
            0, 0, 1, 1, 0, 0, 0, 0,
            0, 1, 1, 0, 0, 0, 0, 1,
            1, 1, 0, 0, 0, 0, 1, 1,
            1, 1, 0, 0, 0, 0, 1, 1,
            0, 1, 1, 0, 0, 0, 0, 1,
            0, 0, 1, 1, 0, 0, 0, 0,
            0, 0, 0, 1, 1, 0, 0, 0,
        };

        /// <summary>
        /// Gets the material ID for a belt cell based on position and direction.
        /// </summary>
        private static byte GetBeltMaterialForChevron(int x, int y, sbyte direction)
        {
            int localX = ((x % 8) + 8) % 8;
            int localY = ((y % 8) + 8) % 8;

            // Mirror X for right-pointing belts
            if (direction > 0)
                localX = 7 - localX;

            bool isLight = ChevronPattern[localY * 8 + localX] == 1;

            return direction > 0
                ? (isLight ? Materials.BeltRightLight : Materials.BeltRight)
                : (isLight ? Materials.BeltLeftLight : Materials.BeltLeft);
        }

        private void UpdateBeltTileIds(int minX, int maxX, int y, ushort newBeltId, sbyte direction)
        {
            for (int dy = 0; dy < BeltStructure.Height; dy++)
            {
                for (int cx = minX; cx <= maxX; cx++)
                {
                    int cy = y + dy;
                    int posKey = cy * width + cx;

                    if (beltTiles.TryGetValue(posKey, out BeltTile existing))
                    {
                        existing.direction = direction;
                        existing.beltId = newBeltId;
                        // Preserve isGhost state
                        beltTiles.Remove(posKey);
                        beltTiles.Add(posKey, existing);
                    }
                }
            }
        }

        private void UpdateBeltInList(ushort beltId, BeltStructure newBelt)
        {
            for (int i = 0; i < belts.Length; i++)
            {
                if (belts[i].id == beltId)
                {
                    belts[i] = newBelt;
                    return;
                }
            }
        }

        private void RemoveBeltStructure(ushort beltId)
        {
            if (!beltLookup.ContainsKey(beltId))
                return;

            beltLookup.Remove(beltId);

            for (int i = 0; i < belts.Length; i++)
            {
                if (belts[i].id == beltId)
                {
                    belts.RemoveAtSwapBack(i);
                    break;
                }
            }
        }

        private void MarkChunksHasStructure(int cellX, int cellY, int width, int height)
        {
            // Find all chunks that overlap with this area
            int startChunkX = cellX / CellWorld.ChunkSize;
            int startChunkY = cellY / CellWorld.ChunkSize;
            int endChunkX = (cellX + width - 1) / CellWorld.ChunkSize;
            int endChunkY = (cellY + height - 1) / CellWorld.ChunkSize;

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
            int chunkEndX = Math.Min(chunkStartX + CellWorld.ChunkSize, world.width);
            int chunkEndY = Math.Min(chunkStartY + CellWorld.ChunkSize, world.height);

            bool hasStructure = false;
            for (int y = chunkStartY; y < chunkEndY && !hasStructure; y++)
            {
                for (int x = chunkStartX; x < chunkEndX && !hasStructure; x++)
                {
                    if (beltTiles.ContainsKey(y * width + x))
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

        /// <summary>
        /// Set velocity on clusters resting on belts to carry them along.
        /// Also marks clusters as "on belt" so ClusterManager won't sleep them.
        /// </summary>
        public void ApplyForcesToClusters(ClusterManager clusterManager, int worldWidth, int worldHeight)
        {
            if (clusterManager == null || belts.Length == 0) return;

            foreach (var cluster in clusterManager.AllClusters)
            {
                if (cluster.rb == null) continue;

                bool foundBelt = false;

                // Check each belt for intersection
                for (int i = 0; i < belts.Length; i++)
                {
                    BeltStructure belt = belts[i];
                    if (ClusterRestingOnBelt(cluster, belt, worldWidth, worldHeight))
                    {
                        foundBelt = true;

                        // Directly set X velocity to carry the cluster (like a real conveyor)
                        // Setting velocity auto-wakes sleeping rigidbodies
                        Vector2 vel = cluster.rb.linearVelocity;
                        vel.x = belt.direction * BeltCarrySpeed;
                        cluster.rb.linearVelocity = vel;
                        break;
                    }
                }

                // Mark cluster as on belt - used by ClusterManager to prevent sleeping
                cluster.isOnBelt = foundBelt;
            }
        }

        /// <summary>
        /// Check if any pixel of a cluster is resting on a belt's surface.
        /// </summary>
        private bool ClusterRestingOnBelt(ClusterData cluster, BeltStructure belt, int worldWidth, int worldHeight)
        {
            int surfaceY = belt.SurfaceY;
            int beltMinX = belt.minX;
            int beltMaxX = belt.minX + belt.Span;

            // Convert cluster position to cell coordinates
            Vector2 cellCenter = CoordinateUtils.WorldToCellFloat(cluster.Position, worldWidth, worldHeight);
            float cellCenterX = cellCenter.x;
            float cellCenterY = cellCenter.y;

            // Quick rejection: cluster too far from belt surface Y
            if (Mathf.Abs(cellCenterY - surfaceY) > cluster.localRadius)
                return false;

            // Quick rejection: cluster too far from belt X range
            float beltCenterX = (beltMinX + beltMaxX) / 2f;
            float beltHalfSpan = (beltMaxX - beltMinX) / 2f;
            if (Mathf.Abs(cellCenterX - beltCenterX) > cluster.localRadius + beltHalfSpan)
                return false;

            // Detailed pixel check (only runs if bounds overlap)
            foreach (var pixel in cluster.pixels)
            {
                Vector2Int cellPos = cluster.LocalToWorldCell(pixel, worldWidth, worldHeight);

                if (cellPos.y == surfaceY &&
                    cellPos.x >= beltMinX && cellPos.x < beltMaxX)
                {
                    // Check that the belt tile directly below (on the belt surface row) is not ghost
                    int belTileY = surfaceY + 1; // First row of the belt block
                    int tileKey = belTileY * width + cellPos.x;
                    if (beltTiles.TryGetValue(tileKey, out BeltTile bt) && bt.isGhost)
                        continue; // Ghost tile — skip this pixel

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks all ghost belt tiles and activates blocks where terrain has been fully cleared.
        /// A ghost block activates when all 64 cells in its 8x8 area are Air.
        /// </summary>
        public void UpdateGhostStates()
        {
            if (ghostBlockOrigins.Count == 0) return;

            // Copy to temp array so we can modify the set while iterating
            var blockKeys = ghostBlockOrigins.ToNativeArray(Allocator.Temp);

            for (int b = 0; b < blockKeys.Length; b++)
            {
                int blockKey = blockKeys[b];
                int gridY = blockKey / width;
                int gridX = blockKey % width;

                // Check if ALL 64 cells are Air
                bool allAir = true;
                for (int dy = 0; dy < BeltStructure.Height && allAir; dy++)
                {
                    for (int dx = 0; dx < BeltStructure.Width && allAir; dx++)
                    {
                        byte mat = world.GetCell(gridX + dx, gridY + dy);
                        if (mat != Materials.Air)
                            allAir = false;
                    }
                }

                if (!allAir) continue;

                // Activate: get tile info from first cell in block
                int firstKey = gridY * width + gridX;
                if (!beltTiles.TryGetValue(firstKey, out BeltTile firstTile))
                    continue;

                sbyte direction = firstTile.direction;

                // Write belt material to cells and clear ghost flag
                for (int dy = 0; dy < BeltStructure.Height; dy++)
                {
                    for (int dx = 0; dx < BeltStructure.Width; dx++)
                    {
                        int cx = gridX + dx;
                        int cy = gridY + dy;
                        int posKey = cy * width + cx;

                        // Update tile: clear ghost
                        BeltTile updated = beltTiles[posKey];
                        updated.isGhost = false;
                        beltTiles.Remove(posKey);
                        beltTiles.Add(posKey, updated);

                        // Write belt material to cell
                        int cellIndex = cy * width + cx;
                        Cell cell = world.cells[cellIndex];
                        cell.materialId = GetBeltMaterialForChevron(cx, cy, direction);
                        cell.structureId = (byte)StructureType.Belt;
                        world.cells[cellIndex] = cell;

                        world.MarkDirty(cx, cy);
                        terrainColliders.MarkChunkDirtyAt(cx, cy);
                    }
                }

                // Remove from tracked ghost set
                ghostBlockOrigins.Remove(blockKey);
            }

            blockKeys.Dispose();
        }

        /// <summary>
        /// Populates a list with grid-snapped positions of all ghost belt blocks.
        /// </summary>
        public void GetGhostBlockPositions(List<Vector2Int> positions)
        {
            if (ghostBlockOrigins.Count == 0) return;

            var keys = ghostBlockOrigins.ToNativeArray(Allocator.Temp);
            for (int i = 0; i < keys.Length; i++)
            {
                int blockKey = keys[i];
                int gridY = blockKey / width;
                int gridX = blockKey % width;
                positions.Add(new Vector2Int(gridX, gridY));
            }
            keys.Dispose();
        }

        /// <summary>
        /// Gets the native belt tiles map for use in Burst jobs.
        /// </summary>
        public NativeHashMap<int, BeltTile> GetBeltTiles() => beltTiles;

        public void Dispose()
        {
            if (beltTiles.IsCreated) beltTiles.Dispose();
            if (beltLookup.IsCreated) beltLookup.Dispose();
            if (belts.IsCreated) belts.Dispose();
            if (ghostBlockOrigins.IsCreated) ghostBlockOrigins.Dispose();
        }
    }
}
