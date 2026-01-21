using System.Collections.Generic;
using UnityEngine;

namespace FallingSand
{
    /// <summary>
    /// Manages all clusters in the world. Handles ID allocation, registration,
    /// and orchestrates the sync between Unity physics and the cell grid.
    /// </summary>
    public class ClusterManager : MonoBehaviour
    {
        [Header("Limits")]
        public int maxClusters = 65535;

        [Header("Physics Settings")]
        public float defaultDensity = 1f;
        public float defaultFriction = 0.3f;
        public float defaultBounciness = 0.2f;

        [Header("Momentum Transfer")]
        [Tooltip("How much of cluster velocity is transferred to displaced cells (0-1)")]
        public float displacementMomentumFactor = 0.5f;

        [Header("Debug")]
        public bool logClusterCreation = true;
        public bool logDisplacements = false;

        // All active clusters
        private Dictionary<ushort, ClusterData> clusters = new Dictionary<ushort, ClusterData>();

        // ID allocation
        private Queue<ushort> freeIds = new Queue<ushort>();
        private ushort nextNewId = 1;  // 0 is reserved for "no owner"

        // Stats
        public int ActiveCount => clusters.Count;
        public int TotalPixelCount { get; private set; }
        public int DisplacementsThisFrame { get; private set; }
        public float PhysicsTimeMs { get; private set; }
        public float SyncTimeMs { get; private set; }
        public int SleepingCount { get; private set; }
        public int SkippedSyncCount { get; private set; }

        // Position tolerance for considering a sleeping cluster as "same position"
        private const float PositionTolerance = 0.01f;
        private const float RotationTolerance = 0.1f;  // degrees

        // Reference to world (set by SandboxController)
        private CellWorld world;

        public IEnumerable<ClusterData> AllClusters => clusters.Values;

        private void Awake()
        {
            // Disable automatic physics - we'll step manually
            Physics2D.simulationMode = SimulationMode2D.Script;

            // Set Physics2D gravity to match cell simulation gravity
            // Uses shared PhysicsSettings as single source of truth
            float unityGravity = PhysicsSettings.GetUnityGravity();
            Physics2D.gravity = new Vector2(0, unityGravity);

            // Configure sleep thresholds to reduce jitter when clusters come to rest
            Physics2D.linearSleepTolerance = PhysicsSettings.LinearSleepTolerance;
            Physics2D.angularSleepTolerance = PhysicsSettings.AngularSleepTolerance;
            Physics2D.timeToSleep = PhysicsSettings.TimeToSleep;
        }

        /// <summary>
        /// Initialize with reference to the cell world.
        /// </summary>
        public void Initialize(CellWorld cellWorld)
        {
            world = cellWorld;
        }

        /// <summary>
        /// Check if a cluster can skip sync because it's sleeping at the same position.
        /// </summary>
        private bool ShouldSkipSync(ClusterData cluster)
        {
            if (cluster.rb == null) return false;

            // Must be sleeping
            if (!cluster.rb.IsSleeping()) return false;

            // Must have already synced pixels at this position
            if (!cluster.isPixelsSynced) return false;

            // Check position hasn't changed significantly
            Vector2 posDelta = cluster.Position - cluster.lastSyncedPosition;
            if (posDelta.sqrMagnitude > PositionTolerance * PositionTolerance) return false;

            // Check rotation hasn't changed significantly
            float rotDelta = Mathf.Abs(cluster.rb.rotation - cluster.lastSyncedRotation);
            if (rotDelta > RotationTolerance) return false;

            return true;
        }

        /// <summary>
        /// Allocate a unique cluster ID.
        /// </summary>
        public ushort AllocateId()
        {
            if (freeIds.Count > 0)
                return freeIds.Dequeue();

            if (nextNewId >= maxClusters)
            {
                Debug.LogError($"[ClusterManager] Max clusters ({maxClusters}) reached!");
                return 0;
            }

            return nextNewId++;
        }

        /// <summary>
        /// Release a cluster ID for reuse.
        /// </summary>
        public void ReleaseId(ushort id)
        {
            if (id != 0)
                freeIds.Enqueue(id);
        }

        /// <summary>
        /// Register a cluster with the manager.
        /// </summary>
        public void Register(ClusterData cluster)
        {
            if (cluster.clusterId == 0)
            {
                Debug.LogError("[ClusterManager] Cannot register cluster with ID 0");
                return;
            }

            clusters[cluster.clusterId] = cluster;
            TotalPixelCount += cluster.pixels.Count;

            if (logClusterCreation)
            {
                Debug.Log($"[Cluster] Created #{cluster.clusterId} with {cluster.pixels.Count} pixels at {cluster.Position}");
            }
        }

        /// <summary>
        /// Unregister a cluster from the manager.
        /// </summary>
        public void Unregister(ClusterData cluster)
        {
            if (clusters.Remove(cluster.clusterId))
            {
                TotalPixelCount -= cluster.pixels.Count;
                ReleaseId(cluster.clusterId);

                if (logClusterCreation)
                {
                    Debug.Log($"[Cluster] Destroyed #{cluster.clusterId}");
                }
            }
        }

        /// <summary>
        /// Get a cluster by ID.
        /// </summary>
        public ClusterData GetCluster(ushort id)
        {
            clusters.TryGetValue(id, out ClusterData cluster);
            return cluster;
        }

        /// <summary>
        /// Step cluster physics and sync to world.
        /// Called from CellSimulatorJobbed before cell simulation.
        /// </summary>
        public void StepAndSync(float deltaTime)
        {
            if (world == null) return;

            DisplacementsThisFrame = 0;
            SkippedSyncCount = 0;
            SleepingCount = 0;

            // Count sleeping clusters
            foreach (var cluster in clusters.Values)
            {
                if (cluster.rb != null && cluster.rb.IsSleeping())
                    SleepingCount++;
            }

            // STEP 1: Clear old cluster pixels from grid
            var clearWatch = System.Diagnostics.Stopwatch.StartNew();
            ClearAllPixelsFromWorld();
            clearWatch.Stop();

            // STEP 2: Step Unity physics
            // Gravity is now baked with 1/15 factor, so use deltaTime directly
            var physicsWatch = System.Diagnostics.Stopwatch.StartNew();
            Physics2D.Simulate(deltaTime);
            physicsWatch.Stop();
            PhysicsTimeMs = (float)physicsWatch.Elapsed.TotalMilliseconds;

            // Manual sleep forcing - physics solver maintains equilibrium velocity (~1.6) due to
            // constant penetration resolution, so Unity's sleep system never triggers.
            // We track consecutive low-velocity frames and force sleep after threshold.
            foreach (var cluster in clusters.Values)
            {
                if (cluster.rb != null && !cluster.rb.IsSleeping())
                {
                    float linVel = cluster.rb.linearVelocity.magnitude;
                    int contactCount = cluster.rb.GetContacts(new ContactPoint2D[4]);

                    if (linVel < 3f && contactCount > 0)
                    {
                        cluster.lowVelocityFrames++;

                        if (cluster.lowVelocityFrames > 30)  // ~0.5 seconds at 60fps
                        {
                            // Don't sleep if cluster is on a belt - belts need to keep moving it
                            // Use cached isOnBelt flag (set by BeltManager BEFORE physics step)
                            if (cluster.isOnBelt)
                            {
                                cluster.lowVelocityFrames = 0;
                                continue;
                            }

                            cluster.rb.linearVelocity = Vector2.zero;
                            cluster.rb.angularVelocity = 0f;
                            cluster.rb.Sleep();
                            cluster.lowVelocityFrames = 0;
                        }
                    }
                    else
                    {
                        cluster.lowVelocityFrames = 0;
                    }
                }
            }

            // STEP 3: Sync cluster pixels to grid at new positions
            var syncWatch = System.Diagnostics.Stopwatch.StartNew();
            SyncAllToWorld();
            syncWatch.Stop();
            SyncTimeMs = (float)syncWatch.Elapsed.TotalMilliseconds + (float)clearWatch.Elapsed.TotalMilliseconds;
        }

        /// <summary>
        /// Clear all cluster pixels from the world grid.
        /// Called before physics step so clusters can move freely.
        /// </summary>
        private void ClearAllPixelsFromWorld()
        {
            foreach (var cluster in clusters.Values)
            {
                // Skip clearing if cluster is sleeping at the same position
                if (ShouldSkipSync(cluster))
                    continue;

                // Mark as not synced since we're clearing
                cluster.isPixelsSynced = false;
                ClearClusterPixels(cluster);
            }
        }

        /// <summary>
        /// Clear a single cluster's pixels from the world grid.
        /// </summary>
        private void ClearClusterPixels(ClusterData cluster)
        {
            foreach (var pixel in cluster.pixels)
            {
                // Convert from Unity world coords to cell grid coords
                Vector2Int cellPos = cluster.LocalToWorldCell(pixel, world.width, world.height);

                if (!world.IsInBounds(cellPos.x, cellPos.y))
                    continue;

                int index = cellPos.y * world.width + cellPos.x;
                Cell cell = world.cells[index];

                // Only clear if this cluster owns the cell
                if (cell.ownerId == cluster.clusterId)
                {
                    cell.materialId = Materials.Air;
                    cell.ownerId = 0;
                    cell.velocityX = 0;
                    cell.velocityY = 0;
                    world.cells[index] = cell;
                }
            }
        }

        /// <summary>
        /// Sync all cluster pixels to the world grid at their new positions.
        /// </summary>
        private void SyncAllToWorld()
        {
            foreach (var cluster in clusters.Values)
            {
                // Skip syncing if cluster is sleeping at the same position
                if (ShouldSkipSync(cluster))
                {
                    SkippedSyncCount++;
                    continue;
                }

                SyncClusterToWorld(cluster);

                // Record sync state for sleep optimization
                cluster.isPixelsSynced = true;
                cluster.lastSyncedPosition = cluster.Position;
                cluster.lastSyncedRotation = cluster.rb != null ? cluster.rb.rotation : 0f;
            }
        }

        /// <summary>
        /// Sync a single cluster's pixels to the world grid.
        /// Displaces any loose cells that are in the way.
        /// </summary>
        private void SyncClusterToWorld(ClusterData cluster)
        {
            foreach (var pixel in cluster.pixels)
            {
                // Convert from Unity world coords to cell grid coords
                Vector2Int cellPos = cluster.LocalToWorldCell(pixel, world.width, world.height);

                if (!world.IsInBounds(cellPos.x, cellPos.y))
                    continue;

                int index = cellPos.y * world.width + cellPos.x;
                Cell existing = world.cells[index];

                // If there's loose material here, push it aside
                if (existing.materialId != Materials.Air && existing.ownerId == 0)
                {
                    DisplaceCell(cellPos, existing, cluster.Velocity);
                }

                // Write cluster pixel to grid
                Cell newCell = new Cell
                {
                    materialId = pixel.materialId,
                    ownerId = cluster.clusterId,
                    frameUpdated = world.currentFrame,
                    velocityX = 0,
                    velocityY = 0,
                    temperature = 20,
                    structureId = 0
                };
                world.cells[index] = newCell;

                // Mark chunk dirty
                world.MarkDirty(cellPos.x, cellPos.y);
            }
        }

        /// <summary>
        /// Displace a cell that's in the way of a cluster.
        /// Finds a nearby empty spot and gives the cell momentum.
        /// Uses BFS to search further if immediate neighbors are occupied.
        /// </summary>
        private void DisplaceCell(Vector2Int fromPos, Cell cell, Vector2 clusterVelocity)
        {
            // Try to find an empty spot using BFS
            Vector2Int? emptySpot = FindNearestEmptyCell(fromPos, maxSearchRadius: 16);

            if (emptySpot.HasValue)
            {
                Vector2Int newPos = emptySpot.Value;
                int newIndex = newPos.y * world.width + newPos.x;

                // Convert cluster velocity to cell velocity
                // Unity velocity: X positive = right, Y positive = up (world)
                // Cell velocity: X positive = right (same), Y positive = down (flipped)
                // Scale factor: ~2 world units per cell, velocity is cells/frame not units/second
                float scaleFactor = displacementMomentumFactor * 0.5f;
                cell.velocityX = (sbyte)Mathf.Clamp(
                    clusterVelocity.x * scaleFactor,
                    -PhysicsSettings.MaxVelocity, PhysicsSettings.MaxVelocity);
                cell.velocityY = (sbyte)Mathf.Clamp(
                    -clusterVelocity.y * scaleFactor,  // Negate Y for coordinate flip
                    -PhysicsSettings.MaxVelocity, PhysicsSettings.MaxVelocity);
                cell.ownerId = 0;
                cell.frameUpdated = world.currentFrame;

                world.cells[newIndex] = cell;
                world.MarkDirty(newPos.x, newPos.y);

                DisplacementsThisFrame++;

                if (logDisplacements)
                {
                    Debug.Log($"[Cluster] Displaced cell {cell.materialId} from {fromPos} to {newPos}");
                }
            }
            else
            {
                // No space found within search radius - cell is lost
                // This should be rare now with larger search radius
                if (logDisplacements)
                {
                    Debug.LogWarning($"[Cluster] Cell {cell.materialId} at {fromPos} lost - no empty space found!");
                }
            }
        }

        // Reusable collections for BFS to avoid allocations
        private Queue<Vector2Int> bfsQueue = new Queue<Vector2Int>();
        private HashSet<long> bfsVisited = new HashSet<long>();

        /// <summary>
        /// Find the nearest empty cell using BFS, prioritizing downward movement.
        /// </summary>
        private Vector2Int? FindNearestEmptyCell(Vector2Int start, int maxSearchRadius)
        {
            bfsQueue.Clear();
            bfsVisited.Clear();

            bfsQueue.Enqueue(start);
            bfsVisited.Add(((long)start.x << 32) | (uint)start.y);

            // Direction offsets - prioritize down, then sides, then up
            Vector2Int[] offsets = new Vector2Int[]
            {
                new Vector2Int(0, 1),    // down (positive Y in cell coords)
                new Vector2Int(-1, 1),   // down-left
                new Vector2Int(1, 1),    // down-right
                new Vector2Int(-1, 0),   // left
                new Vector2Int(1, 0),    // right
                new Vector2Int(0, -1),   // up
                new Vector2Int(-1, -1),  // up-left
                new Vector2Int(1, -1),   // up-right
            };

            while (bfsQueue.Count > 0)
            {
                Vector2Int current = bfsQueue.Dequeue();

                // Check if too far from start
                int dist = Mathf.Abs(current.x - start.x) + Mathf.Abs(current.y - start.y);
                if (dist > maxSearchRadius)
                    continue;

                foreach (var offset in offsets)
                {
                    Vector2Int neighbor = current + offset;

                    // Bounds check
                    if (!world.IsInBounds(neighbor.x, neighbor.y))
                        continue;

                    // Visited check
                    long key = ((long)neighbor.x << 32) | (uint)neighbor.y;
                    if (bfsVisited.Contains(key))
                        continue;
                    bfsVisited.Add(key);

                    // Check if empty
                    int index = neighbor.y * world.width + neighbor.x;
                    if (world.cells[index].materialId == Materials.Air)
                    {
                        return neighbor;
                    }

                    // Not empty, but add to queue to explore further
                    bfsQueue.Enqueue(neighbor);
                }
            }

            return null; // No empty cell found within radius
        }

        private void OnDestroy()
        {
            // Re-enable auto simulation when destroyed
            Physics2D.simulationMode = SimulationMode2D.FixedUpdate;
        }
    }
}
