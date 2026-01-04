using UnityEngine;
using System.Collections.Generic;
using GoldRush.Core;

namespace GoldRush.Simulation
{
    public class SimulationGrid
    {
        public readonly int Width;
        public readonly int Height;

        private readonly MaterialType[] cells;
        private readonly bool[] updatedThisFrame;
        private readonly Vector2[] velocities;  // Per-cell velocity
        private readonly float[] subPositionX;  // Fractional position accumulator X
        private readonly float[] subPositionY;  // Fractional position accumulator Y
        private readonly bool[] infrastructureBlocking;  // Cells blocked by infrastructure (solid)
        private readonly bool[] shakerMeshBlocking;  // Cells blocked by shaker mesh (gold passes through)
        private readonly MaterialType[] filterBlocking;  // Cells that only block a specific material type
        private readonly uint[] clusterIDs;  // 0 = not part of a cluster, >0 = cluster ID
        private readonly System.Random random;

        // Cluster management
        private ClusterManager clusterManager;
        public ClusterManager ClusterManager => clusterManager;

        // Profiling
        public int ActiveCellCount => activeSet.Count;

        // ActiveSet system - only process active particles
        private HashSet<int> activeSet;
        private HashSet<int> nextActiveSet;  // Double-buffer to avoid modification during iteration
        private readonly int[] settleCounters;  // Frames since last movement per cell
        private const int FramesToSettle = 3;  // Frames without movement before sleeping

        public SimulationGrid(int width, int height)
        {
            Width = width;
            Height = height;
            cells = new MaterialType[width * height];
            updatedThisFrame = new bool[width * height];
            velocities = new Vector2[width * height];
            subPositionX = new float[width * height];
            subPositionY = new float[width * height];
            infrastructureBlocking = new bool[width * height];
            shakerMeshBlocking = new bool[width * height];
            filterBlocking = new MaterialType[width * height];
            clusterIDs = new uint[width * height];
            settleCounters = new int[width * height];
            random = new System.Random();

            // Initialize ActiveSet system
            activeSet = new HashSet<int>();
            nextActiveSet = new HashSet<int>();

            // Initialize ClusterManager
            clusterManager = new ClusterManager(this);

            // Initialize all cells to air
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = MaterialType.Air;
                velocities[i] = Vector2.zero;
                subPositionX[i] = 0f;
                subPositionY[i] = 0f;
                infrastructureBlocking[i] = false;
                shakerMeshBlocking[i] = false;
                filterBlocking[i] = MaterialType.Air;  // Air means no filter
                clusterIDs[i] = 0;  // Not part of any cluster
                settleCounters[i] = 0;
            }
        }

        // Get cell at position (returns Air if out of bounds)
        public MaterialType Get(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return MaterialType.Terrain; // Treat out of bounds as solid
            return cells[y * Width + x];
        }

        // Set cell at position
        public void Set(int x, int y, MaterialType type)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return;

            int index = y * Width + x;
            MaterialType oldType = cells[index];
            cells[index] = type;

            // Wake cell and neighbors when simulated material is added/changed
            if (type != oldType)
            {
                if (MaterialProperties.IsSimulated(type))
                {
                    // New simulated material - wake it
                    nextActiveSet.Add(index);  // Use nextActiveSet to avoid collection modification during iteration
                    settleCounters[index] = 0;
                    // Wake cells above that might fall onto this new material
                    WakeCell(x, y - 1);
                    WakeCell(x - 1, y - 1);
                    WakeCell(x + 1, y - 1);
                }
                else if (MaterialProperties.IsSimulated(oldType))
                {
                    // Cell was cleared - wake cells above so they can fall into vacated space
                    WakeCell(x, y - 1);
                    WakeCell(x - 1, y - 1);
                    WakeCell(x + 1, y - 1);
                }
            }
        }

        // Get velocity at position
        public Vector2 GetVelocity(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return Vector2.zero;
            return velocities[y * Width + x];
        }

        // Set velocity at position
        public void SetVelocity(int x, int y, Vector2 vel)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return;
            velocities[y * Width + x] = vel;
        }

        // Add velocity to existing velocity at position
        public void AddVelocity(int x, int y, Vector2 vel)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return;
            velocities[y * Width + x] += vel;
        }

        // Set infrastructure blocking at position (solid - blocks everything)
        public void SetInfrastructureBlocking(int x, int y, bool blocking)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return;
            infrastructureBlocking[y * Width + x] = blocking;
        }

        // Set shaker mesh blocking at position (blocks everything EXCEPT gold)
        public void SetShakerMeshBlocking(int x, int y, bool blocking)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return;
            shakerMeshBlocking[y * Width + x] = blocking;
        }

        // Set filter blocking at position (blocks ONLY the specified material, Air = no filter)
        public void SetFilterBlocking(int x, int y, MaterialType filterType)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return;
            filterBlocking[y * Width + x] = filterType;
        }

        // Get the filter type at a position
        public MaterialType GetFilterBlocking(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return MaterialType.Air;
            return filterBlocking[y * Width + x];
        }

        // Get cluster ID at position (0 = not part of a cluster)
        public uint GetClusterID(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return 0;
            return clusterIDs[y * Width + x];
        }

        // Set cluster ID at position
        public void SetClusterID(int x, int y, uint id)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return;
            clusterIDs[y * Width + x] = id;
        }

        // Check if position is inside a shaker mesh (for special movement rules)
        public bool IsInShakerMesh(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return false;
            return shakerMeshBlocking[y * Width + x];
        }

        // Check if position is blocked by infrastructure for a specific material
        public bool IsBlockedByInfrastructure(int x, int y, MaterialType type = MaterialType.Sand)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return false;
            int index = y * Width + x;

            // Solid infrastructure blocks everything
            if (infrastructureBlocking[index])
                return true;

            // Shaker mesh blocks everything EXCEPT gold
            if (shakerMeshBlocking[index] && type != MaterialType.Gold)
                return true;

            // Filter blocking - blocks ONLY the specified material type
            MaterialType filter = filterBlocking[index];
            if (filter != MaterialType.Air && filter == type)
                return true;

            return false;
        }

        // Wake a cell (add to nextActiveSet for processing next frame)
        public void WakeCell(int x, int y)
        {
            if (!InBounds(x, y))
                return;
            int index = y * Width + x;
            // Only wake cells with simulated materials
            if (MaterialProperties.IsSimulated(cells[index]))
            {
                nextActiveSet.Add(index);  // Add to next frame's set to avoid collection modification during iteration
                settleCounters[index] = 0;
            }
        }

        // Wake all 8 neighbors of a cell
        public void WakeNeighborsAt(int x, int y)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx != 0 || dy != 0)
                    {
                        WakeCell(x + dx, y + dy);
                    }
                }
            }
        }

        // Wake a cell and all its neighbors
        public void WakeCellAndNeighbors(int x, int y)
        {
            WakeCell(x, y);
            WakeNeighborsAt(x, y);
        }

        // Check if a cell is currently active
        public bool IsActive(int x, int y)
        {
            if (!InBounds(x, y))
                return false;
            return activeSet.Contains(y * Width + x);
        }

        // Get count of active cells (for debugging/stats)
        public int GetActiveCount()
        {
            return activeSet.Count;
        }

        // Debug: Get breakdown of why cells are staying active
        public struct ActiveBreakdown
        {
            public int moved;
            public int canFall;
            public int hasVel;
            public int settling;
        }

        public ActiveBreakdown GetActiveBreakdown()
        {
            ActiveBreakdown b = new ActiveBreakdown();
            foreach (int index in activeSet)
            {
                int x = index % Width;
                int y = index / Width;
                MaterialType type = cells[index];

                if (!MaterialProperties.IsSimulated(type)) continue;

                MaterialType below = Get(x, y + 1);
                bool canPotentiallyFall = (below == MaterialType.Air || MaterialProperties.CanDisplace(type, below));
                bool hasVelocity = velocities[index].sqrMagnitude > 0.01f;
                bool isSettling = settleCounters[index] < FramesToSettle;

                if (canPotentiallyFall) b.canFall++;
                if (hasVelocity) b.hasVel++;
                if (isSettling) b.settling++;
            }
            return b;
        }

        // Check if position is within bounds
        public bool InBounds(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        // Main simulation update - only process active cells
        public void Update()
        {
            // Clear update flags
            System.Array.Clear(updatedThisFrame, 0, updatedThisFrame.Length);

            // Process clusters first (they move as units)
            clusterManager.Update();

            // Process only active single cells (skip cells that are part of clusters)
            foreach (int index in activeSet)
            {
                // Skip cells that are part of clusters - ClusterManager handles them
                if (clusterIDs[index] != 0)
                    continue;

                int x = index % Width;
                int y = index / Width;
                UpdateCell(x, y);
            }

            // Swap buffers for next frame
            var temp = activeSet;
            activeSet = nextActiveSet;
            nextActiveSet = temp;
            nextActiveSet.Clear();
        }

        private void UpdateCell(int x, int y)
        {
            int index = y * Width + x;

            // Skip if already updated this frame
            if (updatedThisFrame[index])
                return;

            MaterialType type = cells[index];

            // Skip non-simulated materials
            if (!MaterialProperties.IsSimulated(type))
                return;

            // Skip non-gold materials inside shaker mesh - shaker handles their movement
            if (shakerMeshBlocking[index] && type != MaterialType.Gold)
            {
                nextActiveSet.Add(index);
                return;
            }

            // === UNIFIED PHYSICS STEP ===

            // 1. Collect forces: gravity + force zones
            Vector2 force = new Vector2(0, GameSettings.SimGravity);
            force += ForceZoneManager.Instance.GetNetForce(x, y);

            // 2. Update velocity
            Vector2 vel = velocities[index];
            vel += force;
            vel.x = Mathf.Clamp(vel.x, -GameSettings.SimTerminalVelocity, GameSettings.SimTerminalVelocity);
            vel.y = Mathf.Clamp(vel.y, -GameSettings.SimTerminalVelocity, GameSettings.SimTerminalVelocity);
            velocities[index] = vel;

            // 3. Accumulate sub-position
            subPositionX[index] += vel.x;
            subPositionY[index] += vel.y;

            // 4. Calculate cells to move
            int moveX = (int)subPositionX[index];
            int moveY = (int)subPositionY[index];
            subPositionX[index] -= moveX;
            subPositionY[index] -= moveY;

            bool moved = false;
            int currentX = x;
            int currentY = y;

            // 5. Execute movement one cell at a time
            while (moveX != 0 || moveY != 0)
            {
                int stepX = Mathf.Clamp(moveX, -1, 1);
                int stepY = Mathf.Clamp(moveY, -1, 1);

                int targetX = currentX + stepX;
                int targetY = currentY + stepY;

                if (TryMoveUnified(currentX, currentY, targetX, targetY, type))
                {
                    currentX = targetX;
                    currentY = targetY;
                    moveX -= stepX;
                    moveY -= stepY;
                    moved = true;
                }
                else
                {
                    // Blocked - try to resolve
                    bool resolvedX = (stepX == 0);
                    bool resolvedY = (stepY == 0);

                    // If moving diagonally and blocked, try axis-aligned moves
                    if (stepX != 0 && stepY != 0)
                    {
                        // Try vertical only
                        if (TryMoveUnified(currentX, currentY, currentX, currentY + stepY, type))
                        {
                            currentY += stepY;
                            moveY -= stepY;
                            moved = true;
                            resolvedY = true;
                        }
                        // Try horizontal only
                        else if (TryMoveUnified(currentX, currentY, currentX + stepX, currentY, type))
                        {
                            currentX += stepX;
                            moveX -= stepX;
                            moved = true;
                            resolvedX = true;
                        }
                    }

                    // Zero velocity in blocked directions
                    int newIndex = currentY * Width + currentX;
                    if (!resolvedX && stepX != 0)
                    {
                        velocities[newIndex] = new Vector2(0, velocities[newIndex].y);
                        subPositionX[newIndex] = 0;
                        moveX = 0;
                    }
                    if (!resolvedY && stepY != 0)
                    {
                        // Try diagonal spread when falling and blocked below
                        if (stepY > 0 && TryDiagonalSpread(currentX, currentY, type))
                        {
                            moved = true;
                            // Position updated by TryDiagonalSpread, exit loop
                            break;
                        }

                        velocities[newIndex] = new Vector2(velocities[newIndex].x, 0);
                        subPositionY[newIndex] = 0;
                        moveY = 0;
                    }

                    if (!resolvedX && !resolvedY)
                        break;
                }
            }

            // 6. Handle water horizontal spread (special case)
            if (!moved && type == MaterialType.Water)
            {
                moved = TryWaterSpread(currentX, currentY);
            }

            // 7. Sleep check
            UpdateSleepState(currentX, currentY, moved);
        }

        // Try diagonal spread when falling is blocked (forms pyramids)
        private bool TryDiagonalSpread(int x, int y, MaterialType type)
        {
            bool tryLeftFirst = random.Next(2) == 0;

            if (tryLeftFirst)
            {
                if (TryMoveUnified(x, y, x - 1, y + 1, type)) return true;
                if (TryMoveUnified(x, y, x + 1, y + 1, type)) return true;
            }
            else
            {
                if (TryMoveUnified(x, y, x + 1, y + 1, type)) return true;
                if (TryMoveUnified(x, y, x - 1, y + 1, type)) return true;
            }
            return false;
        }

        // Water horizontal spreading
        private bool TryWaterSpread(int x, int y)
        {
            int spreadDist = MaterialProperties.GetSpreadDistance(MaterialType.Water);
            bool spreadLeft = random.Next(2) == 0;

            for (int i = 1; i <= spreadDist; i++)
            {
                int checkX = spreadLeft ? x - i : x + i;
                if (TryMoveUnified(x, y, checkX, y, MaterialType.Water))
                    return true;
            }

            // Try other direction
            for (int i = 1; i <= spreadDist; i++)
            {
                int checkX = spreadLeft ? x + i : x - i;
                if (TryMoveUnified(x, y, checkX, y, MaterialType.Water))
                    return true;
            }

            return false;
        }

        // Unified movement with velocity/subposition transfer
        private bool TryMoveUnified(int fromX, int fromY, int toX, int toY, MaterialType type)
        {
            if (!InBounds(toX, toY))
                return false;

            if (IsBlockedByInfrastructure(toX, toY, type))
                return false;

            MaterialType target = Get(toX, toY);

            if (target == MaterialType.Air)
            {
                int fromIndex = fromY * Width + fromX;
                int toIndex = toY * Width + toX;

                // Move cell
                cells[fromIndex] = MaterialType.Air;
                cells[toIndex] = type;

                // Transfer velocity and sub-position
                velocities[toIndex] = velocities[fromIndex];
                subPositionX[toIndex] = subPositionX[fromIndex];
                subPositionY[toIndex] = subPositionY[fromIndex];
                velocities[fromIndex] = Vector2.zero;
                subPositionX[fromIndex] = 0;
                subPositionY[fromIndex] = 0;

                MarkUpdated(toX, toY);
                nextActiveSet.Add(toIndex);
                settleCounters[toIndex] = 0;

                // Wake cells above that could fall
                WakeCell(fromX, fromY - 1);
                WakeCell(fromX - 1, fromY - 1);
                WakeCell(fromX + 1, fromY - 1);

                return true;
            }
            else if (MaterialProperties.CanDisplace(type, target))
            {
                int fromIndex = fromY * Width + fromX;
                int toIndex = toY * Width + toX;

                // Swap cells
                cells[fromIndex] = target;
                cells[toIndex] = type;

                // Transfer velocity to new position, displaced gets some momentum
                Vector2 movingVel = velocities[fromIndex];
                velocities[toIndex] = movingVel;
                velocities[fromIndex] = movingVel * 0.3f;  // Displaced gets partial momentum

                // Transfer sub-position
                float subX = subPositionX[fromIndex];
                float subY = subPositionY[fromIndex];
                subPositionX[toIndex] = subX;
                subPositionY[toIndex] = subY;
                subPositionX[fromIndex] = 0;
                subPositionY[fromIndex] = 0;

                MarkUpdated(toX, toY);
                nextActiveSet.Add(toIndex);
                nextActiveSet.Add(fromIndex);  // Displaced needs processing
                settleCounters[toIndex] = 0;
                settleCounters[fromIndex] = 0;

                return true;
            }

            return false;
        }

        // Update sleep state for a particle
        private void UpdateSleepState(int x, int y, bool moved)
        {
            int index = y * Width + x;
            Vector2 vel = velocities[index];

            if (moved)
            {
                nextActiveSet.Add(index);
                settleCounters[index] = 0;
                return;
            }

            // Check if there are forces at this position
            bool hasForces = ForceZoneManager.Instance.HasForceAt(x, y);

            // Check if can potentially fall
            MaterialType type = cells[index];
            MaterialType below = Get(x, y + 1);
            bool canFall = (below == MaterialType.Air || MaterialProperties.CanDisplace(type, below));

            // Check if has velocity
            bool hasVelocity = vel.sqrMagnitude > 0.01f;

            if (hasForces || canFall || hasVelocity || settleCounters[index] < FramesToSettle)
            {
                nextActiveSet.Add(index);
                settleCounters[index]++;
            }
            // else: particle sleeps
        }

        private void MarkUpdated(int x, int y)
        {
            if (InBounds(x, y))
                updatedThisFrame[y * Width + x] = true;
        }

        // Add material at position (for spawning sand when digging, etc.)
        public void AddMaterial(int x, int y, MaterialType type)
        {
            if (!InBounds(x, y))
                return;

            MaterialType current = Get(x, y);
            if (current == MaterialType.Air)
            {
                Set(x, y, type);
            }
        }

        // Fill a rectangular area with material
        public void FillRect(int x, int y, int width, int height, MaterialType type)
        {
            for (int dy = 0; dy < height; dy++)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    Set(x + dx, y + dy, type);
                }
            }
        }

        // Check for material interactions (sand + water = wet sand)
        public void ProcessInteractions()
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    MaterialType type = Get(x, y);

                    if (type == MaterialType.Sand)
                    {
                        // Check neighbors for water
                        if (HasNeighbor(x, y, MaterialType.Water))
                        {
                            // Convert sand to wet sand and remove one water
                            Set(x, y, MaterialType.WetSand);
                            RemoveFirstNeighbor(x, y, MaterialType.Water);
                        }
                    }
                }
            }
        }

        private bool HasNeighbor(int x, int y, MaterialType type)
        {
            return Get(x - 1, y) == type || Get(x + 1, y) == type ||
                   Get(x, y - 1) == type || Get(x, y + 1) == type;
        }

        private void RemoveFirstNeighbor(int x, int y, MaterialType type)
        {
            if (Get(x - 1, y) == type) { Set(x - 1, y, MaterialType.Air); return; }
            if (Get(x + 1, y) == type) { Set(x + 1, y, MaterialType.Air); return; }
            if (Get(x, y - 1) == type) { Set(x, y - 1, MaterialType.Air); return; }
            if (Get(x, y + 1) == type) { Set(x, y + 1, MaterialType.Air); return; }
        }
    }
}
