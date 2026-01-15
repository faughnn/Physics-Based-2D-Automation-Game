using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace FallingSand
{
    /// <summary>
    /// Burst-compiled job for simulating cell physics in parallel.
    /// Each job instance processes one chunk's extended region (core 32x32 + 16-cell buffer).
    /// </summary>
    [BurstCompile]
    public struct SimulateChunksJob : IJobParallelFor
    {
        // Read/Write access to world data
        [NativeDisableParallelForRestriction]
        public NativeArray<Cell> cells;

        [NativeDisableParallelForRestriction]
        public NativeArray<ChunkState> chunks;

        // Read-only material definitions
        [ReadOnly]
        public NativeArray<MaterialDef> materials;

        // Chunk indices to process this pass
        [ReadOnly]
        public NativeArray<int> chunkIndices;

        // World dimensions
        public int width;
        public int height;
        public int chunksX;
        public int chunksY;

        // Frame counter for double-processing prevention
        public ushort currentFrame;

        // Buffer size around each chunk
        private const int BufferSize = 16;
        private const int ChunkSize = 32;

        public void Execute(int jobIndex)
        {
            int chunkIndex = chunkIndices[jobIndex];
            SimulateChunk(chunkIndex);
        }

        private void SimulateChunk(int chunkIndex)
        {
            int chunkX = chunkIndex % chunksX;
            int chunkY = chunkIndex / chunksX;

            // Core chunk bounds
            int coreMinX = chunkX * ChunkSize;
            int coreMinY = chunkY * ChunkSize;

            // Extended bounds with buffer (clamped to world bounds)
            int extMinX = math.max(0, coreMinX - BufferSize);
            int extMaxX = math.min(width, coreMinX + ChunkSize + BufferSize);
            int extMinY = math.max(0, coreMinY - BufferSize);
            int extMaxY = math.min(height, coreMinY + ChunkSize + BufferSize);

            // Process bottom-to-top (critical for falling), alternating X direction
            for (int y = extMaxY - 1; y >= extMinY; y--)
            {
                bool leftToRight = (y & 1) == 0;

                int startX = leftToRight ? extMinX : extMaxX - 1;
                int endX = leftToRight ? extMaxX : extMinX - 1;
                int stepX = leftToRight ? 1 : -1;

                for (int x = startX; x != endX; x += stepX)
                {
                    SimulateCell(x, y);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SimulateCell(int x, int y)
        {
            int index = y * width + x;
            Cell cell = cells[index];

            // Skip if already processed this frame
            if (cell.frameUpdated == currentFrame)
                return;

            // Skip air
            if (cell.materialId == Materials.Air)
                return;

            MaterialDef mat = materials[cell.materialId];

            // Skip static materials
            if (mat.behaviour == BehaviourType.Static)
                return;

            // Mark as processed
            cell.frameUpdated = currentFrame;

            // Simulate based on behaviour type
            switch (mat.behaviour)
            {
                case BehaviourType.Powder:
                    SimulatePowder(x, y, cell, mat);
                    break;
                case BehaviourType.Liquid:
                    SimulateLiquid(x, y, cell, mat);
                    break;
                case BehaviourType.Gas:
                    SimulateGas(x, y, cell, mat);
                    break;
            }
        }

        private void SimulatePowder(int x, int y, Cell cell, MaterialDef mat)
        {
            // Apply gravity
            cell.velocityY = (sbyte)math.min(cell.velocityY + 1, 16);

            // Try to move down by velocity
            int targetY = y + cell.velocityY;

            // Trace path for collision
            for (int checkY = y + 1; checkY <= targetY; checkY++)
            {
                if (!CanMoveTo(x, checkY, mat.density))
                {
                    targetY = checkY - 1;
                    cell.velocityY = 0;
                    break;
                }
            }

            if (targetY > y)
            {
                MoveCell(x, y, x, targetY, cell);
                return;
            }

            // Can't fall straight - try diagonals
            // Randomize direction to avoid bias
            bool tryLeftFirst = ((x + y + currentFrame) & 1) == 0;
            int dx1 = tryLeftFirst ? -1 : 1;
            int dx2 = tryLeftFirst ? 1 : -1;

            if (CanMoveTo(x + dx1, y + 1, mat.density))
            {
                MoveCell(x, y, x + dx1, y + 1, cell);
                return;
            }

            if (CanMoveTo(x + dx2, y + 1, mat.density))
            {
                MoveCell(x, y, x + dx2, y + 1, cell);
                return;
            }

            // Stuck - write back with zeroed velocity
            cell.velocityX = 0;
            cell.velocityY = 0;
            cells[y * width + x] = cell;
        }

        private void SimulateLiquid(int x, int y, Cell cell, MaterialDef mat)
        {
            // Apply gravity
            cell.velocityY = (sbyte)math.min(cell.velocityY + 1, 16);

            // Try falling first
            if (TryFall(x, y, cell, mat.density))
                return;

            if (TryDiagonalFall(x, y, cell, mat.density))
                return;

            // Spread horizontally
            int spread = math.max(1, (16 - math.abs(cell.velocityY)) / (mat.friction + 1));

            bool tryLeftFirst = ((x + y + currentFrame) & 1) == 0;
            int dx1 = tryLeftFirst ? -1 : 1;
            int dx2 = tryLeftFirst ? 1 : -1;

            // Try first direction
            for (int dist = 1; dist <= spread; dist++)
            {
                int targetX = x + dx1 * dist;
                if (!IsInBounds(targetX, y))
                    break;

                if (CanMoveTo(targetX, y, mat.density))
                {
                    MoveCell(x, y, targetX, y, cell);
                    return;
                }

                // Hit something solid, stop searching this direction
                if (!IsEmpty(targetX, y))
                    break;
            }

            // Try second direction
            for (int dist = 1; dist <= spread; dist++)
            {
                int targetX = x + dx2 * dist;
                if (!IsInBounds(targetX, y))
                    break;

                if (CanMoveTo(targetX, y, mat.density))
                {
                    MoveCell(x, y, targetX, y, cell);
                    return;
                }

                if (!IsEmpty(targetX, y))
                    break;
            }

            // Stuck - write back with zeroed velocity
            cell.velocityY = 0;
            cells[y * width + x] = cell;
        }

        private void SimulateGas(int x, int y, Cell cell, MaterialDef mat)
        {
            // Gases rise - negative gravity
            cell.velocityY = (sbyte)math.max(cell.velocityY - 1, -16);

            int targetY = y + cell.velocityY; // velocityY is negative, so this goes up

            // Trace path upward
            for (int checkY = y - 1; checkY >= targetY; checkY--)
            {
                if (!CanMoveTo(x, checkY, mat.density))
                {
                    targetY = checkY + 1;
                    break;
                }
            }

            if (targetY < y)
            {
                MoveCell(x, y, x, targetY, cell);
                return;
            }

            // Try diagonal upward
            bool tryLeftFirst = ((x + y + currentFrame) & 1) == 0;
            int dx1 = tryLeftFirst ? -1 : 1;
            int dx2 = tryLeftFirst ? 1 : -1;

            if (CanMoveTo(x + dx1, y - 1, mat.density))
            {
                MoveCell(x, y, x + dx1, y - 1, cell);
                return;
            }

            if (CanMoveTo(x + dx2, y - 1, mat.density))
            {
                MoveCell(x, y, x + dx2, y - 1, cell);
                return;
            }

            // Spread horizontally (gases disperse)
            int spread = 4;
            for (int dist = 1; dist <= spread; dist++)
            {
                int targetX = x + dx1 * dist;
                if (CanMoveTo(targetX, y, mat.density))
                {
                    MoveCell(x, y, targetX, y, cell);
                    return;
                }
            }

            // Stuck - write back with zeroed velocity
            cell.velocityY = 0;
            cells[y * width + x] = cell;
        }

        private bool TryFall(int x, int y, Cell cell, byte density)
        {
            int targetY = y + cell.velocityY;

            for (int checkY = y + 1; checkY <= targetY; checkY++)
            {
                if (!CanMoveTo(x, checkY, density))
                {
                    targetY = checkY - 1;
                    break;
                }
            }

            if (targetY > y)
            {
                MoveCell(x, y, x, targetY, cell);
                return true;
            }
            return false;
        }

        private bool TryDiagonalFall(int x, int y, Cell cell, byte density)
        {
            bool tryLeftFirst = ((x + y + currentFrame) & 1) == 0;
            int dx1 = tryLeftFirst ? -1 : 1;
            int dx2 = tryLeftFirst ? 1 : -1;

            if (CanMoveTo(x + dx1, y + 1, density))
            {
                MoveCell(x, y, x + dx1, y + 1, cell);
                return true;
            }

            if (CanMoveTo(x + dx2, y + 1, density))
            {
                MoveCell(x, y, x + dx2, y + 1, cell);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsEmpty(int x, int y)
        {
            return cells[y * width + x].materialId == Materials.Air;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanMoveTo(int x, int y, byte myDensity)
        {
            if (!IsInBounds(x, y))
                return false;

            Cell target = cells[y * width + x];

            // Can move into air
            if (target.materialId == Materials.Air)
                return true;

            // Can displace lighter materials (not static)
            MaterialDef targetMat = materials[target.materialId];
            if (targetMat.behaviour == BehaviourType.Static)
                return false;

            return myDensity > targetMat.density;
        }

        private void MoveCell(int fromX, int fromY, int toX, int toY, Cell cell)
        {
            int fromIndex = fromY * width + fromX;
            int toIndex = toY * width + toX;

            // Get target cell (usually air, but could be lighter material for displacement)
            Cell targetCell = cells[toIndex];

            // Mark target as processed so it doesn't get simulated again this frame
            targetCell.frameUpdated = currentFrame;

            // Swap - target goes to source, source goes to target
            cells[fromIndex] = targetCell;
            cells[toIndex] = cell;

            // Mark both positions dirty
            MarkDirtyInternal(fromX, fromY);
            MarkDirtyInternal(toX, toY);
        }

        private void MarkDirtyInternal(int x, int y)
        {
            int chunkX = x >> 5; // x / 32
            int chunkY = y >> 5; // y / 32

            if (chunkX < 0 || chunkX >= chunksX || chunkY < 0 || chunkY >= chunksY)
                return;

            int chunkIndex = chunkY * chunksX + chunkX;

            ChunkState chunk = chunks[chunkIndex];
            chunk.flags |= ChunkFlags.IsDirty;

            int localX = x & 31; // x % 32
            int localY = y & 31; // y % 32

            // Update dirty bounds (race conditions on min/max are acceptable - worst case is extra work)
            if (localX < chunk.minX) chunk.minX = (ushort)localX;
            if (localX > chunk.maxX) chunk.maxX = (ushort)localX;
            if (localY < chunk.minY) chunk.minY = (ushort)localY;
            if (localY > chunk.maxY) chunk.maxY = (ushort)localY;

            chunks[chunkIndex] = chunk;
        }
    }
}
