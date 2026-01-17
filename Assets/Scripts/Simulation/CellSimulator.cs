using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace FallingSand
{
    /// <summary>
    /// Job-compatible cell physics simulation with dirty chunk tracking.
    /// Only simulates cells in active chunks' dirty regions.
    /// </summary>
    public static class CellSimulator
    {
        /// <summary>
        /// Simulate one frame of physics. Call from main thread.
        /// Only simulates cells in active chunks (dirty, recently active, or has structures).
        /// </summary>
        public static void Simulate(CellWorld world)
        {
            world.currentFrame++;

            // Get active chunks (allocated temporarily)
            using var activeChunks = new NativeArray<int>(world.chunksX * world.chunksY, Allocator.Temp);
            int activeCount = world.GetActiveChunks(activeChunks);

            // Process each active chunk
            for (int i = 0; i < activeCount; i++)
            {
                int chunkIndex = activeChunks[i];
                SimulateChunk(world, chunkIndex);
            }

            // Reset dirty state for next frame
            world.ResetDirtyState();
        }

        /// <summary>
        /// Simulates all cells within a single chunk's dirty bounds.
        /// </summary>
        private static void SimulateChunk(CellWorld world, int chunkIndex)
        {
            int chunkX = chunkIndex % world.chunksX;
            int chunkY = chunkIndex / world.chunksX;
            ChunkState chunk = world.chunks[chunkIndex];

            // Convert local dirty bounds to world coordinates
            int baseX = chunkX * CellWorld.ChunkSize;
            int baseY = chunkY * CellWorld.ChunkSize;

            // If bounds are inverted (minX > maxX), this chunk was marked dirty without specific bounds
            // (e.g., neighbor waking). In that case, simulate the entire chunk.
            int minX, maxX, minY, maxY;
            if (chunk.minX > chunk.maxX)
            {
                // No specific dirty bounds - simulate entire chunk
                minX = baseX;
                maxX = math.min(baseX + CellWorld.ChunkSize, world.width);
                minY = baseY;
                maxY = math.min(baseY + CellWorld.ChunkSize, world.height);
            }
            else
            {
                // Use dirty bounds
                minX = baseX + chunk.minX;
                maxX = math.min(baseX + chunk.maxX + 1, world.width);
                minY = baseY + chunk.minY;
                maxY = math.min(baseY + chunk.maxY + 1, world.height);
            }

            // Bottom-to-top, alternating X direction
            for (int y = maxY - 1; y >= minY; y--)
            {
                bool leftToRight = (y & 1) == 0;

                int startX = leftToRight ? minX : maxX - 1;
                int endX = leftToRight ? maxX : minX - 1;
                int stepX = leftToRight ? 1 : -1;

                for (int x = startX; x != endX; x += stepX)
                {
                    SimulateCell(world, x, y);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SimulateCell(CellWorld world, int x, int y)
        {
            int index = y * world.width + x;
            Cell cell = world.cells[index];

            // Skip if already processed this frame
            if (cell.frameUpdated == world.currentFrame)
                return;

            // Skip air
            if (cell.materialId == Materials.Air)
                return;

            MaterialDef mat = world.materials[cell.materialId];

            // Skip static materials
            if (mat.behaviour == BehaviourType.Static)
                return;

            // Mark as processed
            cell.frameUpdated = world.currentFrame;

            // Simulate based on behaviour type
            // Each function handles writing the cell (either via MoveCell or directly when stuck)
            switch (mat.behaviour)
            {
                case BehaviourType.Powder:
                    SimulatePowder(world, x, y, cell, mat);
                    break;
                case BehaviourType.Liquid:
                    SimulateLiquid(world, x, y, cell, mat);
                    break;
                case BehaviourType.Gas:
                    SimulateGas(world, x, y, cell, mat);
                    break;
            }
        }

        private static void SimulatePowder(CellWorld world, int x, int y, Cell cell, MaterialDef mat)
        {
            // Apply gravity
            cell.velocityY = (sbyte)math.min(cell.velocityY + (int)PhysicsSettings.Gravity, PhysicsSettings.MaxVelocity);

            // Try to move down by velocity
            int targetY = y + cell.velocityY;

            // Trace path for collision
            for (int checkY = y + 1; checkY <= targetY; checkY++)
            {
                if (!CanMoveTo(world, x, checkY, mat.density))
                {
                    targetY = checkY - 1;
                    cell.velocityY = 0;
                    break;
                }
            }

            if (targetY > y)
            {
                MoveCell(world, x, y, x, targetY, cell);
                return;
            }

            // Can't fall straight - try diagonals
            // Randomize direction to avoid bias
            bool tryLeftFirst = ((x + y + world.currentFrame) & 1) == 0;
            int dx1 = tryLeftFirst ? -1 : 1;
            int dx2 = tryLeftFirst ? 1 : -1;

            if (CanMoveTo(world, x + dx1, y + 1, mat.density))
            {
                MoveCell(world, x, y, x + dx1, y + 1, cell);
                return;
            }

            if (CanMoveTo(world, x + dx2, y + 1, mat.density))
            {
                MoveCell(world, x, y, x + dx2, y + 1, cell);
                return;
            }

            // Stuck - write back with zeroed velocity
            cell.velocityX = 0;
            cell.velocityY = 0;
            world.cells[y * world.width + x] = cell;
        }

        private static void SimulateLiquid(CellWorld world, int x, int y, Cell cell, MaterialDef mat)
        {
            // Apply gravity
            cell.velocityY = (sbyte)math.min(cell.velocityY + (int)PhysicsSettings.Gravity, PhysicsSettings.MaxVelocity);

            // Try falling first
            if (TryFall(world, x, y, cell, mat.density))
                return;

            if (TryDiagonalFall(world, x, y, cell, mat.density))
                return;

            // Spread horizontally
            int spread = math.max(1, (PhysicsSettings.MaxVelocity - math.abs(cell.velocityY)) / (mat.friction + 1));

            bool tryLeftFirst = ((x + y + world.currentFrame) & 1) == 0;
            int dx1 = tryLeftFirst ? -1 : 1;
            int dx2 = tryLeftFirst ? 1 : -1;

            // Try first direction
            for (int dist = 1; dist <= spread; dist++)
            {
                int targetX = x + dx1 * dist;
                if (!IsInBounds(targetX, y, world.width, world.height))
                    break;

                if (CanMoveTo(world, targetX, y, mat.density))
                {
                    MoveCell(world, x, y, targetX, y, cell);
                    return;
                }

                // Hit something solid, stop searching this direction
                if (!IsEmpty(world, targetX, y))
                    break;
            }

            // Try second direction
            for (int dist = 1; dist <= spread; dist++)
            {
                int targetX = x + dx2 * dist;
                if (!IsInBounds(targetX, y, world.width, world.height))
                    break;

                if (CanMoveTo(world, targetX, y, mat.density))
                {
                    MoveCell(world, x, y, targetX, y, cell);
                    return;
                }

                if (!IsEmpty(world, targetX, y))
                    break;
            }

            // Stuck - write back with zeroed velocity
            cell.velocityY = 0;
            world.cells[y * world.width + x] = cell;
        }

        private static void SimulateGas(CellWorld world, int x, int y, Cell cell, MaterialDef mat)
        {
            // Gases rise - negative gravity
            cell.velocityY = (sbyte)math.max(cell.velocityY - (int)PhysicsSettings.Gravity, -PhysicsSettings.MaxVelocity);

            int targetY = y + cell.velocityY; // velocityY is negative, so this goes up

            // Trace path upward
            for (int checkY = y - 1; checkY >= targetY; checkY--)
            {
                if (!CanMoveTo(world, x, checkY, mat.density))
                {
                    targetY = checkY + 1;
                    break;
                }
            }

            if (targetY < y)
            {
                MoveCell(world, x, y, x, targetY, cell);
                return;
            }

            // Try diagonal upward
            bool tryLeftFirst = ((x + y + world.currentFrame) & 1) == 0;
            int dx1 = tryLeftFirst ? -1 : 1;
            int dx2 = tryLeftFirst ? 1 : -1;

            if (CanMoveTo(world, x + dx1, y - 1, mat.density))
            {
                MoveCell(world, x, y, x + dx1, y - 1, cell);
                return;
            }

            if (CanMoveTo(world, x + dx2, y - 1, mat.density))
            {
                MoveCell(world, x, y, x + dx2, y - 1, cell);
                return;
            }

            // Spread horizontally (gases disperse)
            int spread = 4;
            for (int dist = 1; dist <= spread; dist++)
            {
                int targetX = x + dx1 * dist;
                if (CanMoveTo(world, targetX, y, mat.density))
                {
                    MoveCell(world, x, y, targetX, y, cell);
                    return;
                }
            }

            // Stuck - write back with zeroed velocity
            cell.velocityY = 0;
            world.cells[y * world.width + x] = cell;
        }

        private static bool TryFall(CellWorld world, int x, int y, Cell cell, byte density)
        {
            int targetY = y + cell.velocityY;

            for (int checkY = y + 1; checkY <= targetY; checkY++)
            {
                if (!CanMoveTo(world, x, checkY, density))
                {
                    targetY = checkY - 1;
                    break;
                }
            }

            if (targetY > y)
            {
                MoveCell(world, x, y, x, targetY, cell);
                return true;
            }
            return false;
        }

        private static bool TryDiagonalFall(CellWorld world, int x, int y, Cell cell, byte density)
        {
            bool tryLeftFirst = ((x + y + world.currentFrame) & 1) == 0;
            int dx1 = tryLeftFirst ? -1 : 1;
            int dx2 = tryLeftFirst ? 1 : -1;

            if (CanMoveTo(world, x + dx1, y + 1, density))
            {
                MoveCell(world, x, y, x + dx1, y + 1, cell);
                return true;
            }

            if (CanMoveTo(world, x + dx2, y + 1, density))
            {
                MoveCell(world, x, y, x + dx2, y + 1, cell);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsInBounds(int x, int y, int width, int height)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsEmpty(CellWorld world, int x, int y)
        {
            return world.cells[y * world.width + x].materialId == Materials.Air;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool CanMoveTo(CellWorld world, int x, int y, byte myDensity)
        {
            if (!IsInBounds(x, y, world.width, world.height))
                return false;

            Cell target = world.cells[y * world.width + x];

            // Can move into air
            if (target.materialId == Materials.Air)
                return true;

            // Can displace lighter materials (not static)
            MaterialDef targetMat = world.materials[target.materialId];
            if (targetMat.behaviour == BehaviourType.Static)
                return false;

            return myDensity > targetMat.density;
        }

        private static void MoveCell(CellWorld world, int fromX, int fromY, int toX, int toY, Cell cell)
        {
            int fromIndex = fromY * world.width + fromX;
            int toIndex = toY * world.width + toX;

            // Get target cell (usually air, but could be lighter material for displacement)
            Cell targetCell = world.cells[toIndex];

            // Mark target as processed so it doesn't get simulated again this frame
            targetCell.frameUpdated = world.currentFrame;

            // Swap - target goes to source, source goes to target
            world.cells[fromIndex] = targetCell;
            world.cells[toIndex] = cell;

            // Mark both positions dirty (with neighbor propagation)
            world.MarkDirtyWithNeighbors(fromX, fromY);
            world.MarkDirtyWithNeighbors(toX, toY);
        }
    }
}
