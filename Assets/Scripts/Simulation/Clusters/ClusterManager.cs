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

        // Reference to world (set by SandboxController)
        private CellWorld world;

        public IEnumerable<ClusterData> AllClusters => clusters.Values;

        private void Awake()
        {
            // Disable automatic physics - we'll step manually
            Physics2D.simulationMode = SimulationMode2D.Script;

            // Set Physics2D gravity to match cell simulation gravity
            // Uses shared PhysicsSettings as single source of truth
            float unityGravity = PhysicsSettings.GetUnityGravity(PhysicsSettings.Gravity);
            Physics2D.gravity = new Vector2(0, unityGravity);
        }

        /// <summary>
        /// Initialize with reference to the cell world.
        /// </summary>
        public void Initialize(CellWorld cellWorld)
        {
            world = cellWorld;
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

            // STEP 1: Clear old cluster pixels from grid
            var clearWatch = System.Diagnostics.Stopwatch.StartNew();
            ClearAllPixelsFromWorld();
            clearWatch.Stop();

            // STEP 2: Step Unity physics
            // Scale timestep by simulation speed to match cell gravity behavior
            // Higher SimulationSpeed = slower physics (smaller timestep)
            float scaledDeltaTime = deltaTime / PhysicsSettings.SimulationSpeed;

            var physicsWatch = System.Diagnostics.Stopwatch.StartNew();
            Physics2D.Simulate(scaledDeltaTime);
            physicsWatch.Stop();
            PhysicsTimeMs = (float)physicsWatch.Elapsed.TotalMilliseconds;

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
                SyncClusterToWorld(cluster);
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
        /// </summary>
        private void DisplaceCell(Vector2Int fromPos, Cell cell, Vector2 clusterVelocity)
        {
            // Check 8 neighbors for empty spot
            // In cell coords: positive Y is down, positive X is right
            // Priority: down (let it fall naturally), sides, then up
            Vector2Int[] offsets = new Vector2Int[]
            {
                new Vector2Int(0, 1),    // down (positive Y in cell coords)
                new Vector2Int(-1, 0),   // left
                new Vector2Int(1, 0),    // right
                new Vector2Int(-1, 1),   // down-left
                new Vector2Int(1, 1),    // down-right
                new Vector2Int(0, -1),   // up (negative Y in cell coords)
                new Vector2Int(-1, -1),  // up-left
                new Vector2Int(1, -1),   // up-right
            };

            foreach (var offset in offsets)
            {
                Vector2Int newPos = fromPos + offset;

                if (!world.IsInBounds(newPos.x, newPos.y))
                    continue;

                int newIndex = newPos.y * world.width + newPos.x;
                Cell target = world.cells[newIndex];

                if (target.materialId == Materials.Air)
                {
                    // Convert cluster velocity to cell velocity
                    // Unity velocity: X positive = right, Y positive = up (world)
                    // Cell velocity: X positive = right (same), Y positive = down (flipped)
                    // Scale factor: ~2 world units per cell, velocity is cells/frame not units/second
                    float scaleFactor = displacementMomentumFactor * 0.5f; // rough conversion
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
                    return;
                }
            }

            // No space found - cell is lost (could spawn particle effect here)
            DisplacementsThisFrame++;
        }

        private void OnDestroy()
        {
            // Re-enable auto simulation when destroyed
            Physics2D.simulationMode = SimulationMode2D.FixedUpdate;
        }
    }
}
