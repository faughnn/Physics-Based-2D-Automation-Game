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
        private readonly Vector2[] velocities;  // Per-cell velocity for kickback/lift effects
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

        private const float VelocityFriction = 0.85f;  // Velocity decay per frame
        private const float VelocityThreshold = 0.1f;  // Minimum velocity before zeroing

        public SimulationGrid(int width, int height)
        {
            Width = width;
            Height = height;
            cells = new MaterialType[width * height];
            updatedThisFrame = new bool[width * height];
            velocities = new Vector2[width * height];
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
                bool hasVelocity = velocities[index].sqrMagnitude > VelocityThreshold * VelocityThreshold;
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
            // They should only fall straight down, controlled by the shaker's ProcessFallingWetSand
            if (shakerMeshBlocking[index] && type != MaterialType.Gold)
            {
                // Keep active but don't process - shaker will move it
                nextActiveSet.Add(index);
                return;
            }

            bool moved = false;

            // Apply gravity to velocity (unified physics system)
            Vector2 vel = velocities[index];

            // Snappy start: if particle is at rest and can fall, give it initial push
            // Only apply when truly stationary (not when lift is pushing upward)
            if (Mathf.Abs(vel.y) < 0.1f)
            {
                MaterialType below = Get(x, y + 1);
                bool canFall = (below == MaterialType.Air || MaterialProperties.CanDisplace(type, below));
                if (canFall)
                {
                    vel.y = 1f;  // Initial downward velocity for immediate movement
                }
            }

            // Apply gravity (always pulls down)
            vel.y += GameSettings.SimGravity;

            // Cap at terminal velocity
            if (vel.y > GameSettings.SimTerminalVelocity)
                vel.y = GameSettings.SimTerminalVelocity;

            velocities[index] = vel;

            // First, try velocity-based movement (use ray casting for high speeds)
            if (vel.sqrMagnitude > VelocityThreshold * VelocityThreshold)
            {
                float speed = vel.magnitude;

                if (speed > 1.5f)
                {
                    // High velocity - use ray casting to prevent tunneling
                    if (TryMoveWithRayCast(x, y, vel, type))
                    {
                        return;  // Ray casting handles ActiveSet and velocity
                    }
                    else
                    {
                        // Hit something - reduce velocity significantly
                        velocities[index] = vel * 0.3f;
                    }
                }
                else
                {
                    // Low velocity - use simple single-step movement
                    int moveX = Mathf.RoundToInt(vel.x);
                    int moveY = Mathf.RoundToInt(vel.y);

                    if (moveX != 0 || moveY != 0)
                    {
                        int targetX = x + Mathf.Clamp(moveX, -1, 1);
                        int targetY = y + Mathf.Clamp(moveY, -1, 1);

                        if (TryMoveWithVelocity(x, y, targetX, targetY, type))
                        {
                            // Apply friction to velocity at new position
                            Vector2 newVel = vel * VelocityFriction;
                            if (newVel.sqrMagnitude < VelocityThreshold * VelocityThreshold)
                                newVel = Vector2.zero;
                            velocities[targetY * Width + targetX] = newVel;
                            return;  // TryMoveWithVelocity already handles ActiveSet
                        }
                        else
                        {
                            // Hit something - reduce velocity significantly
                            velocities[index] = vel * 0.3f;
                        }
                    }
                }
            }

            // Skip normal falling if particle has ANY upward velocity (being lifted)
            Vector2 currentVel = velocities[index];
            if (currentVel.y < 0f)
            {
                // Particle is being pushed upward - keep active, don't try to fall
                nextActiveSet.Add(index);
                return;
            }

            // Then apply normal falling behavior
            if (type == MaterialType.Water)
            {
                moved = UpdateWater(x, y);
            }
            else
            {
                moved = UpdateFalling(x, y, type);
            }

            // Handle settling - if didn't move, manage sleep state
            if (!moved)
            {
                // Decay velocity when blocked (particle couldn't move)
                Vector2 remainingVel = velocities[index];
                if (remainingVel.sqrMagnitude > 0)
                {
                    remainingVel *= VelocityFriction;
                    if (remainingVel.sqrMagnitude < VelocityThreshold * VelocityThreshold)
                        remainingVel = Vector2.zero;
                    velocities[index] = remainingVel;
                }

                // Check if particle could potentially fall (don't sleep if space below)
                MaterialType below = Get(x, y + 1);
                bool canPotentiallyFall = (below == MaterialType.Air || MaterialProperties.CanDisplace(type, below));

                // Check if particle still has velocity or hasn't settled yet
                if (canPotentiallyFall ||
                    remainingVel.sqrMagnitude > VelocityThreshold * VelocityThreshold ||
                    settleCounters[index] < FramesToSettle)
                {
                    // Keep active - can fall, has velocity, or still settling
                    nextActiveSet.Add(index);
                    settleCounters[index]++;
                }
                // else: particle sleeps (not added to nextActiveSet)
            }
            // If moved, TryMove already added to nextActiveSet
        }

        // Update falling materials (sand, wetsand, gold, slag)
        // Returns true if particle moved
        private bool UpdateFalling(int x, int y, MaterialType type)
        {
            // Try to fall straight down
            if (TryMove(x, y, x, y + 1, type))
                return true;

            // Try to fall diagonally (randomize direction to prevent bias)
            bool tryLeftFirst = random.Next(2) == 0;

            if (tryLeftFirst)
            {
                if (TryMove(x, y, x - 1, y + 1, type)) return true;
                if (TryMove(x, y, x + 1, y + 1, type)) return true;
            }
            else
            {
                if (TryMove(x, y, x + 1, y + 1, type)) return true;
                if (TryMove(x, y, x - 1, y + 1, type)) return true;
            }

            // Can't move - stay in place
            return false;
        }

        // Update water - falls and spreads horizontally
        // Returns true if water moved
        private bool UpdateWater(int x, int y)
        {
            // Try to fall straight down
            if (TryMove(x, y, x, y + 1, MaterialType.Water))
                return true;

            // Try to fall diagonally
            bool tryLeftFirst = random.Next(2) == 0;

            if (tryLeftFirst)
            {
                if (TryMove(x, y, x - 1, y + 1, MaterialType.Water)) return true;
                if (TryMove(x, y, x + 1, y + 1, MaterialType.Water)) return true;
            }
            else
            {
                if (TryMove(x, y, x + 1, y + 1, MaterialType.Water)) return true;
                if (TryMove(x, y, x - 1, y + 1, MaterialType.Water)) return true;
            }

            // Can't fall - try to spread horizontally
            int spreadDist = MaterialProperties.GetSpreadDistance(MaterialType.Water);
            bool spreadLeft = random.Next(2) == 0;

            for (int i = 1; i <= spreadDist; i++)
            {
                int targetX = spreadLeft ? x - i : x + i;

                // Check if we can move there
                MaterialType target = Get(targetX, y);
                if (target == MaterialType.Air)
                {
                    // Check if there's space below (prefer falling)
                    MaterialType below = Get(targetX, y + 1);
                    if (below == MaterialType.Air || MaterialProperties.CanDisplace(MaterialType.Water, below))
                    {
                        // Move there - it will fall next frame
                        TryMove(x, y, targetX, y, MaterialType.Water);
                        return true;
                    }
                    else
                    {
                        // Just spread horizontally
                        TryMove(x, y, targetX, y, MaterialType.Water);
                        return true;
                    }
                }
                else if (target != MaterialType.Air)
                {
                    // Hit something solid, stop spreading this direction
                    break;
                }
            }

            // Try other direction
            spreadLeft = !spreadLeft;
            for (int i = 1; i <= spreadDist; i++)
            {
                int targetX = spreadLeft ? x - i : x + i;
                MaterialType target = Get(targetX, y);
                if (target == MaterialType.Air)
                {
                    TryMove(x, y, targetX, y, MaterialType.Water);
                    return true;
                }
                else if (target != MaterialType.Air)
                {
                    break;
                }
            }

            return false;
        }

        // Try to move a cell from one position to another
        private bool TryMove(int fromX, int fromY, int toX, int toY, MaterialType type)
        {
            if (!InBounds(toX, toY))
                return false;

            // Check if blocked by infrastructure (gold can pass through shaker mesh)
            if (IsBlockedByInfrastructure(toX, toY, type))
                return false;

            MaterialType target = Get(toX, toY);

            // Can we move into this space?
            if (target == MaterialType.Air)
            {
                // Simple move into empty space
                cells[fromY * Width + fromX] = MaterialType.Air;
                cells[toY * Width + toX] = type;
                MarkUpdated(toX, toY);

                // ActiveSet management: add new position
                int toIndex = toY * Width + toX;
                nextActiveSet.Add(toIndex);
                settleCounters[toIndex] = 0;

                // Only wake cells that could actually fall into the vacated space
                // (above and diagonal-above, not sideways or below)
                WakeCell(fromX, fromY - 1);      // Directly above
                WakeCell(fromX - 1, fromY - 1);  // Diagonal above-left
                WakeCell(fromX + 1, fromY - 1);  // Diagonal above-right

                return true;
            }
            else if (MaterialProperties.CanDisplace(type, target))
            {
                // Swap positions (density displacement)
                cells[fromY * Width + fromX] = target;
                cells[toY * Width + toX] = type;
                MarkUpdated(toX, toY);

                // ActiveSet management: both positions need processing
                int toIndex = toY * Width + toX;
                int fromIndex = fromY * Width + fromX;
                nextActiveSet.Add(toIndex);
                nextActiveSet.Add(fromIndex);  // Displaced material needs to move
                settleCounters[toIndex] = 0;
                settleCounters[fromIndex] = 0;

                return true;
            }

            return false;
        }

        // Try to move a cell with velocity transfer
        private bool TryMoveWithVelocity(int fromX, int fromY, int toX, int toY, MaterialType type)
        {
            if (!InBounds(toX, toY))
                return false;

            // Check if blocked by infrastructure (gold can pass through shaker mesh)
            if (IsBlockedByInfrastructure(toX, toY, type))
                return false;

            MaterialType target = Get(toX, toY);

            if (target == MaterialType.Air)
            {
                // Move cell and transfer velocity
                int fromIndex = fromY * Width + fromX;
                int toIndex = toY * Width + toX;

                cells[fromIndex] = MaterialType.Air;
                cells[toIndex] = type;
                velocities[fromIndex] = Vector2.zero;  // Clear old position velocity
                MarkUpdated(toX, toY);

                // ActiveSet management
                nextActiveSet.Add(toIndex);
                settleCounters[toIndex] = 0;

                // Only wake cells above that could fall into vacated space
                WakeCell(fromX, fromY - 1);
                WakeCell(fromX - 1, fromY - 1);
                WakeCell(fromX + 1, fromY - 1);

                return true;
            }
            else if (MaterialProperties.CanDisplace(type, target))
            {
                // Swap with velocity transfer
                int fromIndex = fromY * Width + fromX;
                int toIndex = toY * Width + toX;

                Vector2 fromVel = velocities[fromIndex];
                Vector2 toVel = velocities[toIndex];

                cells[fromIndex] = target;
                cells[toIndex] = type;
                velocities[fromIndex] = toVel * 0.5f;  // Displaced gets partial velocity
                MarkUpdated(toX, toY);

                // ActiveSet management: both positions need processing
                nextActiveSet.Add(toIndex);
                nextActiveSet.Add(fromIndex);  // Displaced material needs to move
                settleCounters[toIndex] = 0;
                settleCounters[fromIndex] = 0;

                return true;
            }

            return false;
        }

        // Ray casting movement for high-velocity particles (prevents tunneling)
        private const int MaxRayCastSteps = 10;

        private bool TryMoveWithRayCast(int x, int y, Vector2 velocity, MaterialType type)
        {
            float speed = velocity.magnitude;
            if (speed < 1f)
                return false;

            int steps = Mathf.CeilToInt(speed);
            steps = Mathf.Min(steps, MaxRayCastSteps);

            Vector2 direction = velocity.normalized;
            int currentX = x;
            int currentY = y;

            // Accumulate fractional movement for diagonal paths
            float accumX = 0f;
            float accumY = 0f;

            for (int i = 0; i < steps; i++)
            {
                accumX += direction.x;
                accumY += direction.y;

                int stepX = Mathf.RoundToInt(accumX);
                int stepY = Mathf.RoundToInt(accumY);

                if (stepX == 0 && stepY == 0)
                    continue;

                int nextX = currentX + stepX;
                int nextY = currentY + stepY;

                // Reset accumulators after taking a step
                accumX -= stepX;
                accumY -= stepY;

                // Try to move one step
                if (!TryMoveOneStep(currentX, currentY, nextX, nextY, type))
                {
                    // Hit obstacle - stop here
                    break;
                }

                currentX = nextX;
                currentY = nextY;
            }

            bool moved = (currentX != x || currentY != y);
            if (moved)
            {
                // Apply reduced velocity at final position
                int finalIndex = currentY * Width + currentX;
                Vector2 newVel = velocity * VelocityFriction;
                if (newVel.sqrMagnitude < VelocityThreshold * VelocityThreshold)
                    newVel = Vector2.zero;
                velocities[finalIndex] = newVel;
            }

            return moved;
        }

        // Single step movement for ray casting (no velocity transfer)
        private bool TryMoveOneStep(int fromX, int fromY, int toX, int toY, MaterialType type)
        {
            if (!InBounds(toX, toY))
                return false;

            // Check if blocked by infrastructure (gold can pass through shaker mesh)
            if (IsBlockedByInfrastructure(toX, toY, type))
                return false;

            MaterialType target = Get(toX, toY);

            if (target == MaterialType.Air)
            {
                int fromIndex = fromY * Width + fromX;
                int toIndex = toY * Width + toX;

                cells[fromIndex] = MaterialType.Air;
                cells[toIndex] = type;
                velocities[fromIndex] = Vector2.zero;
                MarkUpdated(toX, toY);

                // ActiveSet management
                nextActiveSet.Add(toIndex);
                settleCounters[toIndex] = 0;

                // Only wake cells above that could fall into vacated space
                WakeCell(fromX, fromY - 1);
                WakeCell(fromX - 1, fromY - 1);
                WakeCell(fromX + 1, fromY - 1);

                return true;
            }
            else if (MaterialProperties.CanDisplace(type, target))
            {
                int fromIndex = fromY * Width + fromX;
                int toIndex = toY * Width + toX;

                cells[fromIndex] = target;
                cells[toIndex] = type;
                velocities[fromIndex] = Vector2.zero;
                MarkUpdated(toX, toY);

                // ActiveSet management
                nextActiveSet.Add(toIndex);
                nextActiveSet.Add(fromIndex);
                settleCounters[toIndex] = 0;
                settleCounters[fromIndex] = 0;

                return true;
            }

            return false;
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
