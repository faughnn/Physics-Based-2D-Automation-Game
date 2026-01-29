using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace FallingSand
{
    /// <summary>
    /// Manages all walls in the world.
    /// Handles 8x8 block placement/removal with ghost support for placing through terrain.
    /// Walls are purely static - no simulation behavior, no per-frame updates.
    /// </summary>
    public class WallManager : IDisposable
    {
        private readonly CellWorld world;
        private readonly int width;
        private readonly int height;

        // Wall tile storage: parallel array to cells for O(1) lookup
        private NativeArray<WallTile> wallTiles;

        // Tracked ghost block origins (gridY * width + gridX) for iteration
        private NativeHashSet<int> ghostBlockOrigins;

        // Block dimensions (same as lifts/belts)
        public const int BlockSize = 8;

        public NativeArray<WallTile> WallTiles => wallTiles;

        public WallManager(CellWorld world, int initialCapacity = 64)
        {
            this.world = world;
            this.width = world.width;
            this.height = world.height;

            wallTiles = new NativeArray<WallTile>(width * height, Allocator.Persistent);
            ghostBlockOrigins = new NativeHashSet<int>(initialCapacity, Allocator.Persistent);
        }

        /// <summary>
        /// Snaps a coordinate to the 8x8 grid.
        /// </summary>
        public static int SnapToGrid(int coord)
        {
            if (coord < 0)
                return ((coord - BlockSize + 1) / BlockSize) * BlockSize;
            return (coord / BlockSize) * BlockSize;
        }

        /// <summary>
        /// Places an 8x8 wall block at the specified position.
        /// Position is snapped to 8x8 grid. Returns true if placed successfully.
        /// </summary>
        public bool PlaceWall(int x, int y)
        {
            // Snap to 8x8 grid
            int gridX = SnapToGrid(x);
            int gridY = SnapToGrid(y);

            // Check bounds for entire 8x8 block
            if (!world.IsInBounds(gridX, gridY) ||
                !world.IsInBounds(gridX + BlockSize - 1, gridY + BlockSize - 1))
                return false;

            // Check if entire 8x8 area is placeable (Air, or soft terrain for ghost)
            bool anyGhost = false;
            for (int dy = 0; dy < BlockSize; dy++)
            {
                for (int dx = 0; dx < BlockSize; dx++)
                {
                    int cx = gridX + dx;
                    int cy = gridY + dy;
                    int posKey = cy * width + cx;

                    // Already has a wall here — reject
                    if (wallTiles[posKey].exists)
                        return false;

                    byte existingMaterial = world.GetCell(cx, cy);

                    // Air is always placeable
                    if (existingMaterial == Materials.Air)
                        continue;

                    // Soft terrain — will be ghost
                    if (Materials.IsSoftTerrain(existingMaterial))
                    {
                        anyGhost = true;
                        continue;
                    }

                    // Hard material (Stone, other structures, etc.) — reject
                    return false;
                }
            }

            // Place wall tiles and materials
            for (int dy = 0; dy < BlockSize; dy++)
            {
                for (int dx = 0; dx < BlockSize; dx++)
                {
                    int cx = gridX + dx;
                    int cy = gridY + dy;
                    int posKey = cy * width + cx;

                    wallTiles[posKey] = new WallTile
                    {
                        exists = true,
                        isGhost = anyGhost,
                    };

                    // Only write wall material if not ghost
                    if (!anyGhost)
                    {
                        world.SetCell(cx, cy, Materials.Wall);
                        world.MarkDirty(cx, cy);
                    }
                }
            }

            if (anyGhost)
                ghostBlockOrigins.Add(gridY * width + gridX);

            // Mark chunks as having structure so they stay active
            MarkChunksHasStructure(gridX, gridY, BlockSize, BlockSize);

            return true;
        }

        /// <summary>
        /// Removes the 8x8 wall block at the specified position.
        /// Position is snapped to 8x8 grid. Returns true if removed successfully.
        /// </summary>
        public bool RemoveWall(int x, int y)
        {
            // Snap to 8x8 grid
            int gridX = SnapToGrid(x);
            int gridY = SnapToGrid(y);

            int posKey = gridY * width + gridX;

            // Check if there's a wall at this grid position
            if (!wallTiles[posKey].exists)
                return false;

            bool tileIsGhost = wallTiles[posKey].isGhost;

            // Clear the 8x8 area
            WallTile emptyTile = default;
            for (int dy = 0; dy < BlockSize; dy++)
            {
                for (int dx = 0; dx < BlockSize; dx++)
                {
                    int cx = gridX + dx;
                    int cy = gridY + dy;
                    int cellPosKey = cy * width + cx;

                    wallTiles[cellPosKey] = emptyTile;

                    // Only clear cell material for non-ghost tiles
                    if (!tileIsGhost)
                    {
                        world.SetCell(cx, cy, Materials.Air);
                        world.MarkDirty(cx, cy);
                    }
                }
            }

            if (tileIsGhost)
                ghostBlockOrigins.Remove(gridY * width + gridX);

            // Update chunk structure flags
            UpdateChunksStructureFlag(gridX, gridY, BlockSize, BlockSize);

            return true;
        }

        /// <summary>
        /// Checks if there's a wall tile at the specified position.
        /// </summary>
        public bool HasWallAt(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
                return false;
            return wallTiles[y * width + x].exists;
        }

        /// <summary>
        /// Gets the wall tile at the specified position.
        /// </summary>
        public WallTile GetWallTile(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
                return default;
            return wallTiles[y * width + x];
        }

        /// <summary>
        /// Checks all ghost wall tiles and activates blocks where terrain has been cleared.
        /// A ghost wall block activates when all 64 cells are Air.
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

                // Check that all cells are Air
                bool allClear = true;
                for (int dy = 0; dy < BlockSize && allClear; dy++)
                {
                    for (int dx = 0; dx < BlockSize && allClear; dx++)
                    {
                        byte mat = world.GetCell(gridX + dx, gridY + dy);
                        if (mat != Materials.Air)
                            allClear = false;
                    }
                }

                if (!allClear) continue;

                // Activate: clear ghost, write wall material
                for (int dy = 0; dy < BlockSize; dy++)
                {
                    for (int dx = 0; dx < BlockSize; dx++)
                    {
                        int cx = gridX + dx;
                        int cy = gridY + dy;
                        int posKey = cy * width + cx;

                        WallTile updated = wallTiles[posKey];
                        updated.isGhost = false;
                        wallTiles[posKey] = updated;

                        world.SetCell(cx, cy, Materials.Wall);
                        world.MarkDirty(cx, cy);
                    }
                }

                // Remove from tracked ghost set
                ghostBlockOrigins.Remove(blockKey);
            }

            blockKeys.Dispose();
        }

        /// <summary>
        /// Populates a list with grid-snapped positions of all ghost wall blocks.
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
                    if (wallTiles[y * width + x].exists)
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
            if (wallTiles.IsCreated) wallTiles.Dispose();
            if (ghostBlockOrigins.IsCreated) ghostBlockOrigins.Dispose();
        }
    }
}
