using UnityEngine;
using System.Collections.Generic;
using GoldRush.Core;

namespace GoldRush.Simulation
{
    public struct ClusterData
    {
        public uint ID;
        public int OriginX;        // Top-left X of cluster
        public int OriginY;        // Top-left Y of cluster
        public byte Size;          // 2, 4, or 8 (Gravel, Rock, Boulder)
        public MaterialType Type;  // Boulder, Rock, or Gravel
        public Vector2 Velocity;
        public float SubPositionX; // Fractional position accumulator X
        public float SubPositionY; // Fractional position accumulator Y
        public int SettleCounter;
        public bool IsActive;
    }

    public class ClusterManager
    {
        private SimulationGrid grid;
        private Dictionary<uint, ClusterData> clusters;
        private HashSet<uint> activeClusters;
        private uint nextClusterID = 1;

        private const int FramesToSettle = 3;
        private System.Random random = new System.Random();

        public ClusterManager(SimulationGrid simulationGrid)
        {
            grid = simulationGrid;
            clusters = new Dictionary<uint, ClusterData>();
            activeClusters = new HashSet<uint>();
        }

        public uint CreateCluster(int x, int y, int size, MaterialType type)
        {
            // Validate size (2, 4, 8 for Gravel, Rock, Boulder)
            if (size < 2 || size > 8) return 0;

            // Check if footprint is available
            if (!IsFootprintClear(x, y, size, 0))
                return 0;

            uint id = nextClusterID++;

            ClusterData cluster = new ClusterData
            {
                ID = id,
                OriginX = x,
                OriginY = y,
                Size = (byte)size,
                Type = type,
                Velocity = Vector2.zero,
                SubPositionX = 0,
                SubPositionY = 0,
                SettleCounter = 0,
                IsActive = true
            };

            clusters[id] = cluster;
            activeClusters.Add(id);

            // Fill grid cells
            for (int dy = 0; dy < size; dy++)
            {
                for (int dx = 0; dx < size; dx++)
                {
                    grid.Set(x + dx, y + dy, type);
                    grid.SetClusterID(x + dx, y + dy, id);
                }
            }

            return id;
        }

        public void RemoveCluster(uint id)
        {
            if (!clusters.TryGetValue(id, out ClusterData cluster))
                return;

            // Clear grid cells
            for (int dy = 0; dy < cluster.Size; dy++)
            {
                for (int dx = 0; dx < cluster.Size; dx++)
                {
                    int cx = cluster.OriginX + dx;
                    int cy = cluster.OriginY + dy;
                    grid.Set(cx, cy, MaterialType.Air);
                    grid.SetClusterID(cx, cy, 0);
                }
            }

            clusters.Remove(id);
            activeClusters.Remove(id);
        }

        public void BreakCluster(uint id, int newSize, MaterialType newType)
        {
            if (!clusters.TryGetValue(id, out ClusterData oldCluster))
                return;

            int oldSize = oldCluster.Size;
            int originX = oldCluster.OriginX;
            int originY = oldCluster.OriginY;

            // Remove old cluster (clears all cells to Air)
            RemoveCluster(id);

            if (newSize <= 1)
            {
                // Convert all cells to single-cell material (e.g., Sand)
                for (int dy = 0; dy < oldSize; dy++)
                {
                    for (int dx = 0; dx < oldSize; dx++)
                    {
                        grid.Set(originX + dx, originY + dy, newType);
                        grid.SetClusterID(originX + dx, originY + dy, 0);
                        grid.WakeCellAndNeighbors(originX + dx, originY + dy);
                    }
                }
            }
            else
            {
                // Create smaller clusters in a grid pattern
                List<Vector2Int> clusterPositions = new List<Vector2Int>();
                int gridCount = oldSize / newSize;

                for (int gy = 0; gy < gridCount; gy++)
                {
                    for (int gx = 0; gx < gridCount; gx++)
                    {
                        clusterPositions.Add(new Vector2Int(
                            originX + gx * newSize,
                            originY + gy * newSize
                        ));
                    }
                }

                HashSet<int> usedCells = new HashSet<int>();

                foreach (var pos in clusterPositions)
                {
                    uint newID = CreateCluster(pos.x, pos.y, newSize, newType);
                    if (newID != 0)
                    {
                        for (int dy = 0; dy < newSize; dy++)
                        {
                            for (int dx = 0; dx < newSize; dx++)
                            {
                                usedCells.Add((pos.y + dy) * grid.Width + (pos.x + dx));
                            }
                        }
                    }
                }

                for (int dy = 0; dy < oldSize; dy++)
                {
                    for (int dx = 0; dx < oldSize; dx++)
                    {
                        int cellIndex = (originY + dy) * grid.Width + (originX + dx);
                        if (!usedCells.Contains(cellIndex))
                        {
                            grid.Set(originX + dx, originY + dy, MaterialType.Sand);
                            grid.SetClusterID(originX + dx, originY + dy, 0);
                            grid.WakeCellAndNeighbors(originX + dx, originY + dy);
                        }
                    }
                }
            }
        }

        public bool TryMoveCluster(uint id, int deltaX, int deltaY)
        {
            if (!clusters.TryGetValue(id, out ClusterData cluster))
                return false;

            int newX = cluster.OriginX + deltaX;
            int newY = cluster.OriginY + deltaY;

            // Check if new footprint is clear
            if (!IsFootprintClear(newX, newY, cluster.Size, id))
                return false;

            // Clear old positions
            for (int dy = 0; dy < cluster.Size; dy++)
            {
                for (int dx = 0; dx < cluster.Size; dx++)
                {
                    int oldX = cluster.OriginX + dx;
                    int oldY = cluster.OriginY + dy;
                    grid.Set(oldX, oldY, MaterialType.Air);
                    grid.SetClusterID(oldX, oldY, 0);
                }
            }

            // Wake any clusters or cells that were resting on top of this one
            int aboveY = cluster.OriginY - 1;
            if (aboveY >= 0)
            {
                for (int dx = 0; dx < cluster.Size; dx++)
                {
                    int checkX = cluster.OriginX + dx;
                    if (grid.InBounds(checkX, aboveY))
                    {
                        uint aboveClusterId = grid.GetClusterID(checkX, aboveY);
                        if (aboveClusterId != 0 && aboveClusterId != id)
                        {
                            WakeCluster(aboveClusterId);
                        }
                        grid.WakeCellAndNeighbors(checkX, aboveY);
                    }
                }
            }

            // Update cluster position
            cluster.OriginX = newX;
            cluster.OriginY = newY;

            // Set new positions
            for (int dy = 0; dy < cluster.Size; dy++)
            {
                for (int dx = 0; dx < cluster.Size; dx++)
                {
                    grid.Set(newX + dx, newY + dy, cluster.Type);
                    grid.SetClusterID(newX + dx, newY + dy, id);
                }
            }

            clusters[id] = cluster;
            return true;
        }

        private bool IsFootprintClear(int x, int y, int size, uint ignoreClusterID)
        {
            for (int dy = 0; dy < size; dy++)
            {
                for (int dx = 0; dx < size; dx++)
                {
                    int checkX = x + dx;
                    int checkY = y + dy;

                    if (!grid.InBounds(checkX, checkY))
                        return false;

                    MaterialType cell = grid.Get(checkX, checkY);
                    uint cellCluster = grid.GetClusterID(checkX, checkY);

                    if (cellCluster == ignoreClusterID && ignoreClusterID != 0)
                        continue;

                    if (cell == MaterialType.Terrain)
                        return false;

                    if (grid.IsBlockedByInfrastructure(checkX, checkY, cell))
                        return false;

                    if (cellCluster != 0)
                        return false;

                    if (cell != MaterialType.Air)
                        return false;
                }
            }
            return true;
        }

        public void Update()
        {
            uint[] toProcess = new uint[activeClusters.Count];
            activeClusters.CopyTo(toProcess);
            activeClusters.Clear();

            foreach (uint id in toProcess)
            {
                UpdateCluster(id);
            }
        }

        private void UpdateCluster(uint id)
        {
            if (!clusters.TryGetValue(id, out ClusterData cluster))
                return;

            if (!cluster.IsActive)
                return;

            // === UNIFIED PHYSICS STEP (same as SimulationGrid) ===

            // 1. Collect forces: gravity + force zones
            // Use center of cluster for force zone query
            int centerX = cluster.OriginX + cluster.Size / 2;
            int centerY = cluster.OriginY + cluster.Size / 2;

            Vector2 force = new Vector2(0, GameSettings.SimGravity);
            force += ForceZoneManager.Instance.GetNetForce(centerX, centerY);

            // 2. Update velocity
            cluster.Velocity += force;
            cluster.Velocity.x = Mathf.Clamp(cluster.Velocity.x, -GameSettings.SimTerminalVelocity, GameSettings.SimTerminalVelocity);
            cluster.Velocity.y = Mathf.Clamp(cluster.Velocity.y, -GameSettings.SimTerminalVelocity, GameSettings.SimTerminalVelocity);

            // 3. Accumulate sub-position
            cluster.SubPositionX += cluster.Velocity.x;
            cluster.SubPositionY += cluster.Velocity.y;

            // 4. Calculate cells to move
            int moveX = (int)cluster.SubPositionX;
            int moveY = (int)cluster.SubPositionY;
            cluster.SubPositionX -= moveX;
            cluster.SubPositionY -= moveY;

            bool moved = false;

            // 5. Execute movement one cell at a time
            while (moveX != 0 || moveY != 0)
            {
                int stepX = Mathf.Clamp(moveX, -1, 1);
                int stepY = Mathf.Clamp(moveY, -1, 1);

                // Try diagonal first if both X and Y need to move
                if (stepX != 0 && stepY != 0)
                {
                    if (TryMoveCluster(id, stepX, stepY))
                    {
                        moveX -= stepX;
                        moveY -= stepY;
                        moved = true;
                        // Re-read cluster after move
                        if (!clusters.TryGetValue(id, out cluster)) return;
                        continue;
                    }
                }

                // Try vertical movement
                if (stepY != 0)
                {
                    if (TryMoveCluster(id, 0, stepY))
                    {
                        moveY -= stepY;
                        moved = true;
                        // Re-read cluster after move
                        if (!clusters.TryGetValue(id, out cluster)) return;
                    }
                    else
                    {
                        // Blocked vertically - try diagonal spread when falling
                        if (stepY > 0)
                        {
                            bool tryLeftFirst = random.Next(2) == 0;
                            if (tryLeftFirst)
                            {
                                if (TryMoveCluster(id, -1, 1)) moved = true;
                                else if (TryMoveCluster(id, 1, 1)) moved = true;
                            }
                            else
                            {
                                if (TryMoveCluster(id, 1, 1)) moved = true;
                                else if (TryMoveCluster(id, -1, 1)) moved = true;
                            }
                        }

                        // Stop vertical velocity and sub-position
                        cluster.Velocity.y = 0;
                        cluster.SubPositionY = 0;
                        moveY = 0;

                        // Re-read cluster after potential diagonal move
                        if (!clusters.TryGetValue(id, out cluster)) return;
                    }
                }

                // Try horizontal movement
                if (stepX != 0)
                {
                    if (TryMoveCluster(id, stepX, 0))
                    {
                        moveX -= stepX;
                        moved = true;
                        // Re-read cluster after move
                        if (!clusters.TryGetValue(id, out cluster)) return;
                    }
                    else
                    {
                        // Stop horizontal velocity and sub-position
                        cluster.Velocity.x = 0;
                        cluster.SubPositionX = 0;
                        moveX = 0;
                    }
                }

                // If we couldn't move at all this iteration, break
                if (stepX == 0 && stepY == 0)
                    break;
            }

            // 6. Update settle state
            bool hasForces = ForceZoneManager.Instance.HasForceAt(centerX, centerY);
            bool hasVelocity = cluster.Velocity.sqrMagnitude > 0.01f;

            // Check if can potentially fall
            bool canFall = IsFootprintClear(cluster.OriginX, cluster.OriginY + 1, cluster.Size, id);

            if (moved)
            {
                cluster.SettleCounter = 0;
                cluster.IsActive = true;
                activeClusters.Add(id);
            }
            else if (hasForces || canFall || hasVelocity || cluster.SettleCounter < FramesToSettle)
            {
                cluster.SettleCounter++;
                cluster.IsActive = true;
                activeClusters.Add(id);
            }
            else
            {
                cluster.IsActive = false;
                cluster.Velocity = Vector2.zero;
                cluster.SubPositionX = 0;
                cluster.SubPositionY = 0;
            }

            clusters[id] = cluster;
        }

        public void WakeCluster(uint id)
        {
            if (clusters.TryGetValue(id, out ClusterData cluster))
            {
                cluster.IsActive = true;
                cluster.SettleCounter = 0;
                clusters[id] = cluster;
                activeClusters.Add(id);
            }
        }

        public void SetClusterVelocity(uint id, Vector2 velocity)
        {
            if (clusters.TryGetValue(id, out ClusterData cluster))
            {
                cluster.Velocity = velocity;
                cluster.IsActive = true;
                cluster.SettleCounter = 0;
                clusters[id] = cluster;
                activeClusters.Add(id);
            }
        }

        public void WakeClusterAt(int x, int y)
        {
            uint id = grid.GetClusterID(x, y);
            if (id != 0)
            {
                WakeCluster(id);
            }
        }

        public ClusterData? GetCluster(uint id)
        {
            if (clusters.TryGetValue(id, out ClusterData cluster))
                return cluster;
            return null;
        }

        public IEnumerable<ClusterData> GetClustersInArea(int minX, int minY, int maxX, int maxY)
        {
            HashSet<uint> foundIDs = new HashSet<uint>();

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (!grid.InBounds(x, y)) continue;

                    uint id = grid.GetClusterID(x, y);
                    if (id != 0 && !foundIDs.Contains(id))
                    {
                        foundIDs.Add(id);
                        if (clusters.TryGetValue(id, out ClusterData cluster))
                        {
                            yield return cluster;
                        }
                    }
                }
            }
        }

        public int ClusterCount => clusters.Count;
        public int ActiveClusterCount => activeClusters.Count;
    }
}
