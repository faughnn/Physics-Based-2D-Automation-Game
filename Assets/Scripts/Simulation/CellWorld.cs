using System;
using Unity.Collections;
using Unity.Jobs;

namespace FallingSand
{
    public class CellWorld : IDisposable
    {
        // Dimensions (in cells)
        public readonly int width;
        public readonly int height;

        // Primary data (persistent allocation)
        public NativeArray<Cell> cells;

        // Material definitions (read-only during simulation)
        public NativeArray<MaterialDef> materials;

        // Chunk tracking
        public NativeArray<ChunkState> chunks;
        public readonly int chunksX;
        public readonly int chunksY;

        // Chunk size constant
        public const int ChunkSize = 64;

        // Edge threshold for neighbor marking - mark neighbor chunks dirty when cells are this close to edge
        public const int EdgeThreshold = 2;

        // Frame counter (wraps at 65535)
        public ushort currentFrame;

        public CellWorld(int width, int height)
        {
            this.width = width;
            this.height = height;

            // Allocate cell array
            cells = new NativeArray<Cell>(width * height, Allocator.Persistent);

            // Calculate chunk grid dimensions (round up)
            chunksX = (width + ChunkSize - 1) / ChunkSize;
            chunksY = (height + ChunkSize - 1) / ChunkSize;
            chunks = new NativeArray<ChunkState>(chunksX * chunksY, Allocator.Persistent);

            // Initialize chunks with reset dirty bounds
            for (int i = 0; i < chunks.Length; i++)
            {
                chunks[i] = new ChunkState
                {
                    minX = ChunkSize,  // Invalid bounds = not dirty
                    maxX = 0,
                    minY = ChunkSize,
                    maxY = 0,
                    flags = ChunkFlags.None,
                    activeLastFrame = 0,
                    structureMask = 0,
                };
            }

            // Initialize material definitions
            var defaultMaterials = Materials.CreateDefaults();
            materials = new NativeArray<MaterialDef>(Materials.Count, Allocator.Persistent);
            for (int i = 0; i < defaultMaterials.Length; i++)
            {
                materials[i] = defaultMaterials[i];
            }

            // Initialize all cells to air
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = new Cell
                {
                    materialId = Materials.Air,
                    flags = CellFlags.None,
                    velocityX = 0,
                    velocityY = 0,
                    temperature = 20,  // Room temperature
                    structureId = 0,
                };
            }

            currentFrame = 0;
        }

        public void SetCell(int x, int y, byte materialId)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
                return;

            int index = y * width + x;
            var cell = cells[index];
            cell.materialId = materialId;
            cell.velocityX = 0;
            cell.velocityY = 0;
            cells[index] = cell;

            // Mark chunk dirty
            MarkDirty(x, y);
        }

        public byte GetCell(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
                return Materials.Air;

            return cells[y * width + x].materialId;
        }

        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        public void MarkDirty(int cellX, int cellY)
        {
            int chunkX = cellX / ChunkSize;
            int chunkY = cellY / ChunkSize;

            if (chunkX < 0 || chunkX >= chunksX || chunkY < 0 || chunkY >= chunksY)
                return;

            int chunkIndex = chunkY * chunksX + chunkX;

            int localX = cellX % ChunkSize;
            int localY = cellY % ChunkSize;

            ChunkState chunk = chunks[chunkIndex];
            chunk.flags |= ChunkFlags.IsDirty;

            // Expand dirty bounds
            if (localX < chunk.minX) chunk.minX = (ushort)localX;
            if (localX > chunk.maxX) chunk.maxX = (ushort)localX;
            if (localY < chunk.minY) chunk.minY = (ushort)localY;
            if (localY > chunk.maxY) chunk.maxY = (ushort)localY;

            chunks[chunkIndex] = chunk;
        }

        /// <summary>
        /// Marks the chunk containing (cellX, cellY) dirty, and also marks neighbor chunks
        /// if the cell is near a chunk boundary. Call this when a cell moves.
        /// </summary>
        public void MarkDirtyWithNeighbors(int cellX, int cellY)
        {
            // Mark the primary chunk
            MarkDirty(cellX, cellY);

            // Check proximity to chunk boundaries and mark neighbors
            int localX = cellX & 63;  // cellX % ChunkSize
            int localY = cellY & 63;  // cellY % ChunkSize

            int chunkX = cellX >> 6;  // cellX / ChunkSize
            int chunkY = cellY >> 6;  // cellY / ChunkSize

            // Left neighbor
            if (localX < EdgeThreshold && chunkX > 0)
                MarkChunkDirtyOnly(chunkX - 1, chunkY);

            // Right neighbor
            if (localX >= ChunkSize - EdgeThreshold && chunkX < chunksX - 1)
                MarkChunkDirtyOnly(chunkX + 1, chunkY);

            // Top neighbor (Y=0 is top, so lower chunkY)
            if (localY < EdgeThreshold && chunkY > 0)
                MarkChunkDirtyOnly(chunkX, chunkY - 1);

            // Bottom neighbor (higher chunkY)
            if (localY >= ChunkSize - EdgeThreshold && chunkY < chunksY - 1)
                MarkChunkDirtyOnly(chunkX, chunkY + 1);
        }

        /// <summary>
        /// Marks a chunk dirty by chunk coordinates without expanding bounds.
        /// Used for neighbor waking - the chunk will be simulated but we don't know which cell triggered it.
        /// </summary>
        private void MarkChunkDirtyOnly(int chunkX, int chunkY)
        {
            int chunkIndex = chunkY * chunksX + chunkX;
            ChunkState chunk = chunks[chunkIndex];
            chunk.flags |= ChunkFlags.IsDirty;
            chunks[chunkIndex] = chunk;
        }

        public int CountActiveCells()
        {
            int count = 0;
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i].materialId != Materials.Air)
                    count++;
            }
            return count;
        }

        public int CountActiveChunks()
        {
            int count = 0;
            for (int i = 0; i < chunks.Length; i++)
            {
                if ((chunks[i].flags & ChunkFlags.IsDirty) != 0 || chunks[i].activeLastFrame != 0)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Collects indices of all chunks that should be simulated this frame.
        /// Returns them in bottom-to-top order (matching cell iteration order).
        /// </summary>
        /// <param name="activeChunkIndices">Pre-allocated array to fill (size >= chunksX * chunksY)</param>
        /// <returns>Number of active chunks</returns>
        public int GetActiveChunks(NativeArray<int> activeChunkIndices)
        {
            int count = 0;

            // Iterate bottom-to-top (higher chunkY first, matching cell iteration order)
            for (int chunkY = chunksY - 1; chunkY >= 0; chunkY--)
            {
                for (int chunkX = 0; chunkX < chunksX; chunkX++)
                {
                    int chunkIndex = chunkY * chunksX + chunkX;
                    ChunkState chunk = chunks[chunkIndex];

                    // Active if: dirty OR was active last frame OR has structures
                    bool isActive = (chunk.flags & ChunkFlags.IsDirty) != 0
                                 || chunk.activeLastFrame != 0
                                 || (chunk.flags & ChunkFlags.HasStructure) != 0;

                    if (isActive)
                    {
                        activeChunkIndices[count++] = chunkIndex;
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Collects active chunks into 4 groups based on their position for parallel processing.
        /// Groups form a checkerboard pattern:
        /// A B A B
        /// C D C D
        /// A B A B
        /// </summary>
        public void CollectChunkGroups(
            NativeList<int> groupA,
            NativeList<int> groupB,
            NativeList<int> groupC,
            NativeList<int> groupD)
        {
            groupA.Clear();
            groupB.Clear();
            groupC.Clear();
            groupD.Clear();

            for (int i = 0; i < chunks.Length; i++)
            {
                ChunkState chunk = chunks[i];

                // Active if: dirty OR was active last frame OR has structures
                bool shouldProcess = (chunk.flags & ChunkFlags.IsDirty) != 0
                                  || chunk.activeLastFrame != 0
                                  || (chunk.flags & ChunkFlags.HasStructure) != 0;

                if (!shouldProcess)
                    continue;

                int chunkX = i % chunksX;
                int chunkY = i / chunksX;

                // Group assignment: A=0, B=1, C=2, D=3
                int group = (chunkX & 1) + ((chunkY & 1) << 1);

                switch (group)
                {
                    case 0: groupA.Add(i); break;
                    case 1: groupB.Add(i); break;
                    case 2: groupC.Add(i); break;
                    case 3: groupD.Add(i); break;
                }
            }
        }

        /// <summary>
        /// Resets dirty state at end of frame. Must be called after simulation completes.
        /// Sets activeLastFrame based on current dirty state, then clears IsDirty flag and resets bounds.
        /// </summary>
        public void ResetDirtyState()
        {
            for (int i = 0; i < chunks.Length; i++)
            {
                ChunkState chunk = chunks[i];

                // Remember if this chunk was dirty (for neighbor waking next frame)
                chunk.activeLastFrame = (byte)((chunk.flags & ChunkFlags.IsDirty) != 0 ? 1 : 0);

                // Clear dirty flag and reset bounds (unless has structure - those stay dirty)
                if ((chunk.flags & ChunkFlags.HasStructure) == 0)
                {
                    chunk.flags &= unchecked((byte)~ChunkFlags.IsDirty);
                    chunk.minX = ChunkSize;  // 64 - inverted bounds = empty
                    chunk.maxX = 0;
                    chunk.minY = ChunkSize;
                    chunk.maxY = 0;
                }

                chunks[i] = chunk;
            }
        }

        public void Dispose()
        {
            if (cells.IsCreated) cells.Dispose();
            if (materials.IsCreated) materials.Dispose();
            if (chunks.IsCreated) chunks.Dispose();
        }
    }
}
