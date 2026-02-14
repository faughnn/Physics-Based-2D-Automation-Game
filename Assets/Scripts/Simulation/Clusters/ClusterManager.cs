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

        // Compression detection settings
        private const float MinCrushImpulse = 5f;       // Minimum impulse to count as significant contact
        private const float OpposingDotThreshold = -0.5f; // Dot product threshold for opposing normals
        private const int CrushFrameThreshold = 30;       // Frames of sustained compression before fracture (~0.5s)
        private const int MinPixelsToFracture = 3;         // Clusters smaller than this can't fracture

        // Reusable buffers for compression detection
        private readonly ContactPoint2D[] contactBuffer = new ContactPoint2D[16];
        private readonly List<ClusterData> clustersToFracture = new List<ClusterData>();

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

            // Machine parts (piston plates) are kinematic — always sync their pixels
            if (cluster.isMachinePart) return false;

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
            PerformanceProfiler.StartTiming(TimingSlot.ClusterPhysics);
            var physicsWatch = System.Diagnostics.Stopwatch.StartNew();
            Physics2D.Simulate(deltaTime);
            physicsWatch.Stop();
            PhysicsTimeMs = (float)physicsWatch.Elapsed.TotalMilliseconds;
            PerformanceProfiler.StopTiming(TimingSlot.ClusterPhysics);

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
                            // Don't sleep if cluster is on a belt/lift or is a machine part (e.g. piston arm)
                            // Use cached flags (set by BeltManager/LiftManager BEFORE physics step)
                            if (cluster.isOnBelt || cluster.isOnLift || cluster.isMachinePart || cluster.crushPressureFrames > 0)
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

            // STEP 3: Check for compression and fracture clusters
            CheckCompressionAndFracture();

            // STEP 4: Sync cluster pixels to grid at new positions
            PerformanceProfiler.StartTiming(TimingSlot.ClusterSync);
            var syncWatch = System.Diagnostics.Stopwatch.StartNew();
            SyncAllToWorld();
            syncWatch.Stop();
            SyncTimeMs = (float)syncWatch.Elapsed.TotalMilliseconds + (float)clearWatch.Elapsed.TotalMilliseconds;
            PerformanceProfiler.StopTiming(TimingSlot.ClusterSync);
        }

        // =====================================================================
        // Compression Detection & Fracture
        // =====================================================================

        /// <summary>
        /// Check all dynamic clusters for opposing compression contacts.
        /// Clusters under sustained compression are fractured into smaller pieces.
        /// </summary>
        private void CheckCompressionAndFracture()
        {
            clustersToFracture.Clear();

            foreach (var cluster in clusters.Values)
            {
                if (cluster.rb == null) continue;
                if (cluster.rb.IsSleeping()) continue;
                if (cluster.isMachinePart) continue;
                if (cluster.pixels.Count < MinPixelsToFracture * 2) continue;

                int contactCount = cluster.rb.GetContacts(contactBuffer);
                if (contactCount < 2)
                {
                    cluster.crushPressureFrames = 0;
                    continue;
                }

                // Check for opposing high-impulse contacts
                bool hasOpposingPressure = false;
                for (int i = 0; i < contactCount && !hasOpposingPressure; i++)
                {
                    if (contactBuffer[i].normalImpulse < MinCrushImpulse) continue;

                    for (int j = i + 1; j < contactCount; j++)
                    {
                        if (contactBuffer[j].normalImpulse < MinCrushImpulse) continue;

                        float dot = Vector2.Dot(contactBuffer[i].normal, contactBuffer[j].normal);
                        if (dot < OpposingDotThreshold)
                        {
                            hasOpposingPressure = true;
                            break;
                        }
                    }
                }

                if (hasOpposingPressure)
                {
                    cluster.crushPressureFrames++;
                    if (cluster.crushPressureFrames > CrushFrameThreshold)
                    {
                        clustersToFracture.Add(cluster);
                    }
                }
                else
                {
                    cluster.crushPressureFrames = 0;
                }
            }

            // Fracture collected clusters (can't modify dictionary during iteration)
            for (int i = 0; i < clustersToFracture.Count; i++)
            {
                FractureCluster(clustersToFracture[i]);
            }
        }

        /// <summary>
        /// Fracture a cluster into smaller pieces using crack-line partitioning.
        /// Each pixel is assigned to a group based on which side of 1-2 random crack lines it falls on.
        /// No pixels are removed — small groups merge into the largest group to preserve all material.
        /// </summary>
        public void FractureCluster(ClusterData cluster)
        {
            var pixels = cluster.pixels;
            int pixelCount = pixels.Count;

            // Too small to split into two viable pieces
            if (pixelCount < MinPixelsToFracture * 2) return;

            // --- Compute bounding box of pixels in local space ---
            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;
            foreach (var p in pixels)
            {
                if (p.localX < minX) minX = p.localX;
                if (p.localX > maxX) maxX = p.localX;
                if (p.localY < minY) minY = p.localY;
                if (p.localY > maxY) maxY = p.localY;
            }

            // --- Generate crack lines ---
            int numCracks = (pixelCount < 20) ? 1 : Random.Range(1, 3);
            float centerX = (minX + maxX) * 0.5f;
            float centerY = (minY + maxY) * 0.5f;
            float extentX = (maxX - minX + 1) * 0.3f;
            float extentY = (maxY - minY + 1) * 0.3f;

            Vector2[] crackPoints = new Vector2[numCracks];
            Vector2[] crackNormals = new Vector2[numCracks];
            for (int i = 0; i < numCracks; i++)
            {
                crackPoints[i] = new Vector2(
                    centerX + Random.Range(-extentX, extentX),
                    centerY + Random.Range(-extentY, extentY));
                float angle = Random.Range(0f, Mathf.PI);
                // Normal perpendicular to crack direction — used for signed distance
                crackNormals[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }

            // --- Partition pixels by signed distance to each crack line ---
            // 1 line → 2 groups (bit 0), 2 lines → up to 4 groups (bit 0 + bit 1)
            int maxGroups = 1 << numCracks; // 2 or 4
            int[] groupCounts = new int[maxGroups];
            int[] pixelGroups = new int[pixelCount];

            for (int pi = 0; pi < pixelCount; pi++)
            {
                var p = pixels[pi];
                int group = 0;
                for (int ci = 0; ci < numCracks; ci++)
                {
                    float dx = p.localX - crackPoints[ci].x;
                    float dy = p.localY - crackPoints[ci].y;
                    float signedDist = dx * crackNormals[ci].x + dy * crackNormals[ci].y;
                    if (signedDist >= 0f)
                        group |= (1 << ci);
                }
                pixelGroups[pi] = group;
                groupCounts[group]++;
            }

            // --- Merge small groups into the largest group ---
            int largestGroup = 0;
            for (int g = 1; g < maxGroups; g++)
            {
                if (groupCounts[g] > groupCounts[largestGroup])
                    largestGroup = g;
            }

            for (int g = 0; g < maxGroups; g++)
            {
                if (g == largestGroup) continue;
                if (groupCounts[g] > 0 && groupCounts[g] < MinPixelsToFracture)
                {
                    // Merge into largest
                    groupCounts[largestGroup] += groupCounts[g];
                    groupCounts[g] = 0;
                    for (int pi = 0; pi < pixelCount; pi++)
                    {
                        if (pixelGroups[pi] == g)
                            pixelGroups[pi] = largestGroup;
                    }
                }
            }

            // --- Viability check: need at least 2 non-empty groups ---
            int viableGroups = 0;
            for (int g = 0; g < maxGroups; g++)
            {
                if (groupCounts[g] >= MinPixelsToFracture)
                    viableGroups++;
            }
            if (viableGroups < 2) return;

            // --- Create sub-clusters for each non-empty group ---
            Vector2 origVelocity = cluster.Velocity;
            float origAngularVel = cluster.rb != null ? cluster.rb.angularVelocity : 0f;
            float origRotation = cluster.rb != null ? cluster.rb.rotation : 0f;

            for (int g = 0; g < maxGroups; g++)
            {
                if (groupCounts[g] == 0) continue;

                // Collect pixels for this group and compute centroid
                var groupPixels = new List<ClusterPixel>(groupCounts[g]);
                float sumLX = 0, sumLY = 0;
                for (int pi = 0; pi < pixelCount; pi++)
                {
                    if (pixelGroups[pi] != g) continue;
                    var p = pixels[pi];
                    groupPixels.Add(p);
                    sumLX += p.localX;
                    sumLY += p.localY;
                }

                float centroidLX = sumLX / groupPixels.Count;
                float centroidLY = sumLY / groupPixels.Count;

                // Convert centroid from local to world space using cluster's transform
                float cos = Mathf.Cos(cluster.RotationRad);
                float sin = Mathf.Sin(cluster.RotationRad);
                float rotCX = centroidLX * cos - centroidLY * sin;
                float rotCY = centroidLX * sin + centroidLY * cos;
                Vector2 worldCentroid = cluster.Position + CoordinateUtils.ScaleCellToWorld(new Vector2(rotCX, rotCY));

                // Re-offset pixels relative to new centroid
                var subPixels = new List<ClusterPixel>(groupPixels.Count);
                for (int pi = 0; pi < groupPixels.Count; pi++)
                {
                    var p = groupPixels[pi];
                    short newLX = (short)Mathf.RoundToInt(p.localX - centroidLX);
                    short newLY = (short)Mathf.RoundToInt(p.localY - centroidLY);
                    subPixels.Add(new ClusterPixel(newLX, newLY, p.materialId));
                }

                // Create sub-cluster
                ClusterData subCluster = ClusterFactory.CreateCluster(subPixels, worldCentroid, this);
                if (subCluster != null && subCluster.rb != null)
                {
                    subCluster.rb.linearVelocity = origVelocity;
                    subCluster.rb.angularVelocity = origAngularVel;
                    subCluster.rb.rotation = origRotation;
                }
            }

            // --- Cleanup original cluster ---
            Unregister(cluster);
            Object.Destroy(cluster.gameObject);
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
        /// Clear a single cluster's pixels from the world grid using inverse mapping.
        /// Matches the same cell coverage as SyncClusterToWorld to avoid stale cells.
        /// </summary>
        private void ClearClusterPixels(ClusterData cluster)
        {
            cluster.BuildPixelLookup();
            if (cluster.pixels.Count == 0) return;

            float cos = Mathf.Cos(cluster.RotationRad);
            float sin = Mathf.Sin(cluster.RotationRad);
            Vector2 cellCenter = CoordinateUtils.WorldToCellFloat(cluster.Position, world.width, world.height);

            // Compute cell-space bounding box from local bounds + rotation
            float hx = Mathf.Max(Mathf.Abs(cluster.LocalMinX), Mathf.Abs(cluster.LocalMaxX)) + 1f;
            float hy = Mathf.Max(Mathf.Abs(cluster.LocalMinY), Mathf.Abs(cluster.LocalMaxY)) + 1f;
            float absCos = Mathf.Abs(cos);
            float absSin = Mathf.Abs(sin);
            float extentX = hx * absCos + hy * absSin;
            float extentY = hx * absSin + hy * absCos;

            int cellMinX = Mathf.Max(0, Mathf.FloorToInt(cellCenter.x - extentX));
            int cellMaxX = Mathf.Min(world.width - 1, Mathf.CeilToInt(cellCenter.x + extentX));
            int cellMinY = Mathf.Max(0, Mathf.FloorToInt(cellCenter.y - extentY));
            int cellMaxY = Mathf.Min(world.height - 1, Mathf.CeilToInt(cellCenter.y + extentY));

            for (int cy = cellMinY; cy <= cellMaxY; cy++)
            {
                for (int cx = cellMinX; cx <= cellMaxX; cx++)
                {
                    // Inverse transform: cell coords -> local pixel coords
                    float dx = cx - cellCenter.x;
                    float dy = cellCenter.y - cy;
                    float localXf = dx * cos + dy * sin;
                    float localYf = -dx * sin + dy * cos;

                    int localX = Mathf.RoundToInt(localXf);
                    int localY = Mathf.RoundToInt(localYf);

                    byte materialId = cluster.GetPixelMaterialAt(localX, localY);
                    if (materialId == Materials.Air) continue;

                    int index = cy * world.width + cx;
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
        /// Sync a single cluster's pixels to the world grid using inverse mapping.
        /// Iterates over the cell-space bounding box and maps each cell back to local
        /// pixel space, eliminating gaps caused by forward-mapping rounding.
        /// </summary>
        private void SyncClusterToWorld(ClusterData cluster)
        {
            cluster.BuildPixelLookup();
            if (cluster.pixels.Count == 0) return;

            float cos = Mathf.Cos(cluster.RotationRad);
            float sin = Mathf.Sin(cluster.RotationRad);
            Vector2 cellCenter = CoordinateUtils.WorldToCellFloat(cluster.Position, world.width, world.height);

            // Compute cell-space bounding box from local bounds + rotation
            float hx = Mathf.Max(Mathf.Abs(cluster.LocalMinX), Mathf.Abs(cluster.LocalMaxX)) + 1f;
            float hy = Mathf.Max(Mathf.Abs(cluster.LocalMinY), Mathf.Abs(cluster.LocalMaxY)) + 1f;
            float absCos = Mathf.Abs(cos);
            float absSin = Mathf.Abs(sin);
            float extentX = hx * absCos + hy * absSin;
            float extentY = hx * absSin + hy * absCos;

            int cellMinX = Mathf.Max(0, Mathf.FloorToInt(cellCenter.x - extentX));
            int cellMaxX = Mathf.Min(world.width - 1, Mathf.CeilToInt(cellCenter.x + extentX));
            int cellMinY = Mathf.Max(0, Mathf.FloorToInt(cellCenter.y - extentY));
            int cellMaxY = Mathf.Min(world.height - 1, Mathf.CeilToInt(cellCenter.y + extentY));

            for (int cy = cellMinY; cy <= cellMaxY; cy++)
            {
                for (int cx = cellMinX; cx <= cellMaxX; cx++)
                {
                    // Inverse transform: cell coords -> local pixel coords
                    float dx = cx - cellCenter.x;
                    float dy = cellCenter.y - cy; // cell Y+ down -> Unity Y+ up
                    float localXf = dx * cos + dy * sin;
                    float localYf = -dx * sin + dy * cos;

                    int localX = Mathf.RoundToInt(localXf);
                    int localY = Mathf.RoundToInt(localYf);

                    byte materialId = cluster.GetPixelMaterialAt(localX, localY);
                    if (materialId == Materials.Air) continue;

                    int index = cy * world.width + cx;
                    Cell existing = world.cells[index];

                    // If there's loose material here, push it aside
                    if (existing.materialId != Materials.Air && existing.ownerId == 0)
                    {
                        DisplaceCell(new Vector2Int(cx, cy), existing, cluster.Velocity);
                    }

                    // Write cluster pixel to grid
                    Cell newCell = new Cell
                    {
                        materialId = materialId,
                        ownerId = cluster.clusterId,
                        velocityX = 0,
                        velocityY = 0,
                        temperature = 20,
                        structureId = 0
                    };
                    world.cells[index] = newCell;
                    world.MarkDirty(cx, cy);
                }
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

                world.cells[newIndex] = cell;
                world.MarkDirty(newPos.x, newPos.y);

                DisplacementsThisFrame++;
            }
            else
            {
                // No space found within search radius - cell is lost
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
