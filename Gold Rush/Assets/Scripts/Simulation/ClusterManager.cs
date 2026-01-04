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
        private const float VelocityThreshold = 0.1f;
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
                // Boulder 8x8 -> 4 Rock 4x4 (2x2 grid of clusters)
                // Rock 4x4 -> 4 Gravel 2x2 (2x2 grid of clusters)

                List<Vector2Int> clusterPositions = new List<Vector2Int>();
                int gridCount = oldSize / newSize;  // How many clusters fit per dimension

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

                // Track which cells are used by new clusters
                HashSet<int> usedCells = new HashSet<int>();

                // Create new clusters
                foreach (var pos in clusterPositions)
                {
                    uint newID = CreateCluster(pos.x, pos.y, newSize, newType);
                    if (newID != 0)
                    {
                        // Mark cells as used
                        for (int dy = 0; dy < newSize; dy++)
                        {
                            for (int dx = 0; dx < newSize; dx++)
                            {
                                usedCells.Add((pos.y + dy) * grid.Width + (pos.x + dx));
                            }
                        }
                    }
                }

                // Fill remaining cells with sand (remainder from breaking)
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
            // Check the row just above the old position
            int aboveY = cluster.OriginY - 1;
            if (aboveY >= 0)
            {
                for (int dx = 0; dx < cluster.Size; dx++)
                {
                    int checkX = cluster.OriginX + dx;
                    if (grid.InBounds(checkX, aboveY))
                    {
                        // Wake clusters above
                        uint aboveClusterId = grid.GetClusterID(checkX, aboveY);
                        if (aboveClusterId != 0 && aboveClusterId != id)
                        {
                            WakeCluster(aboveClusterId);
                        }

                        // Wake single cells above (sand, etc.)
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

                    // Check bounds
                    if (!grid.InBounds(checkX, checkY))
                        return false;

                    MaterialType cell = grid.Get(checkX, checkY);
                    uint cellCluster = grid.GetClusterID(checkX, checkY);

                    // Allow if it's our own cluster (moving into own space)
                    if (cellCluster == ignoreClusterID && ignoreClusterID != 0)
                        continue;

                    // Block if terrain
                    if (cell == MaterialType.Terrain)
                        return false;

                    // Block if infrastructure
                    if (grid.IsBlockedByInfrastructure(checkX, checkY, cell))
                        return false;

                    // Block if occupied by another cluster
                    if (cellCluster != 0)
                        return false;

                    // Block if occupied by non-air material (single cells)
                    if (cell != MaterialType.Air)
                        return false;
                }
            }
            return true;
        }

        public void Update()
        {
            // Copy active clusters to process (so we can modify activeClusters during iteration)
            uint[] toProcess = new uint[activeClusters.Count];
            activeClusters.CopyTo(toProcess);

            // Clear for this frame - UpdateCluster will re-add if still active
            activeClusters.Clear();

            // Process each cluster
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

            // Apply gravity (unified physics system)
            cluster.Velocity.y += GameSettings.SimGravity;

            // Clamp velocity at terminal velocity
            cluster.Velocity.x = Mathf.Clamp(cluster.Velocity.x, -GameSettings.SimTerminalVelocity, GameSettings.SimTerminalVelocity);
            cluster.Velocity.y = Mathf.Clamp(cluster.Velocity.y, -GameSettings.SimTerminalVelocity, GameSettings.SimTerminalVelocity);

            bool moved = false;

            // Handle upward movement (from lifts - negative Y in grid coords)
            if (cluster.Velocity.y <= -1f)
            {
                if (TryMoveCluster(id, 0, -1))
                {
                    moved = true;
                    cluster.Velocity.y += 1f;
                }
                else
                {
                    // Hit something above, stop upward velocity
                    cluster.Velocity.y = 0;
                }
            }
            // Handle downward movement (falling)
            else if (cluster.Velocity.y >= 1f)
            {
                if (TryMoveCluster(id, 0, 1))
                {
                    moved = true;
                    cluster.Velocity.y -= 1f;
                }
                else
                {
                    // Hit something below - try diagonal moves like sand
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

                    // Stop vertical velocity
                    cluster.Velocity.y = 0;
                }
            }

            // Handle horizontal movement (from belts)
            if (Mathf.Abs(cluster.Velocity.x) >= 1f)
            {
                int moveX = cluster.Velocity.x >= 1f ? 1 : -1;
                if (TryMoveCluster(id, moveX, 0))
                {
                    moved = true;
                    cluster.Velocity.x -= moveX;
                }
                else
                {
                    // Hit something, stop horizontal velocity
                    cluster.Velocity.x = 0;
                }
            }

            // Clusters fall straight down only - no diagonal movement
            // (Unlike sand, rocks and boulders don't spread sideways)

            // Re-read cluster from dictionary to get updated position from TryMoveCluster
            // (TryMoveCluster updates the dictionary, but we have a stale local copy)
            // Preserve our velocity modifications since TryMoveCluster overwrites with old velocity
            Vector2 currentVelocity = cluster.Velocity;
            if (!clusters.TryGetValue(id, out cluster))
                return;
            cluster.Velocity = currentVelocity;

            // Update settle state
            if (moved)
            {
                cluster.SettleCounter = 0;
                cluster.IsActive = true;
                activeClusters.Add(id);
            }
            else
            {
                cluster.SettleCounter++;
                if (cluster.SettleCounter >= FramesToSettle)
                {
                    cluster.IsActive = false;
                    cluster.Velocity = Vector2.zero;
                }
                else
                {
                    activeClusters.Add(id);
                }
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
