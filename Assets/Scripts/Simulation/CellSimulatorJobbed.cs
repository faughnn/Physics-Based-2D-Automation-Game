using System;
using System.Diagnostics;
using Unity.Collections;
using Unity.Jobs;

namespace FallingSand
{
    /// <summary>
    /// Multithreaded cell physics simulator using Unity Job System.
    /// Processes chunks in 4 passes (checkerboard pattern) to ensure thread safety.
    /// </summary>
    public class CellSimulatorJobbed : IDisposable
    {
        // Persistent allocations for chunk group lists
        private NativeList<int> groupA;
        private NativeList<int> groupB;
        private NativeList<int> groupC;
        private NativeList<int> groupD;

        // Performance tracking
        private Stopwatch stopwatch;
        public float LastSimulationTimeMs { get; private set; }
        public int LastActiveChunkCount { get; private set; }

        // Current managers (stored temporarily during Simulate for ScheduleGroup access)
        private LiftManager currentLiftManager;
        private BeltManager currentBeltManager;
        private WallManager currentWallManager;

        // Physics time accumulator for frame-rate-independent cluster/player physics.
        // Cell simulation is intentionally frame-count-based (Noita-style), but Unity
        // Physics2D (player, clusters) must step at a fixed rate so movement speed
        // doesn't scale with display FPS.
        private float physicsAccumulator;
        private const float MaxAccumulatedTime = 0.1f; // Cap to prevent spiral-of-death

        public CellSimulatorJobbed()
        {
            // Pre-allocate with reasonable capacity
            groupA = new NativeList<int>(64, Allocator.Persistent);
            groupB = new NativeList<int>(64, Allocator.Persistent);
            groupC = new NativeList<int>(64, Allocator.Persistent);
            groupD = new NativeList<int>(64, Allocator.Persistent);

            stopwatch = new Stopwatch();
        }

        /// <summary>
        /// Simulate one frame of physics using parallel jobs.
        /// </summary>
        /// <param name="world">The cell world to simulate</param>
        /// <param name="clusterManager">Optional cluster manager for rigid body physics</param>
        /// <param name="beltManager">Optional belt manager for belt-cluster interaction and ghost tile blocking</param>
        /// <param name="liftManager">Optional lift manager for lift-cluster interaction and lift zones</param>
        /// <param name="wallManager">Optional wall manager for ghost tile blocking</param>
        /// <param name="machineManager">Optional machine manager for piston motor updates</param>
        public void Simulate(CellWorld world, ClusterManager clusterManager = null, BeltManager beltManager = null, LiftManager liftManager = null, WallManager wallManager = null, MachineManager machineManager = null)
        {
            stopwatch.Restart();

            world.currentFrame++;

            // ========== CLUSTER PHYSICS (fixed-rate, decoupled from display frame rate) ==========
            // Cell simulation is frame-count-based (runs once per display frame), but
            // Unity Physics2D must step at a fixed rate so player/cluster movement
            // speed doesn't scale with FPS.
            if (clusterManager != null)
            {
                physicsAccumulator += UnityEngine.Time.deltaTime;
                if (physicsAccumulator > MaxAccumulatedTime)
                    physicsAccumulator = MaxAccumulatedTime;

                float fixedStep = UnityEngine.Time.fixedDeltaTime;
                while (physicsAccumulator >= fixedStep)
                {
                    // Apply belt/lift forces each physics step
                    if (beltManager != null)
                        beltManager.ApplyForcesToClusters(clusterManager, world.width, world.height);
                    if (liftManager != null)
                        liftManager.ApplyForcesToClusters(clusterManager, world.width, world.height);

                    // Update piston motors before physics step
                    if (machineManager != null)
                        machineManager.UpdateMotors();

                    clusterManager.StepAndSync(fixedStep);
                    physicsAccumulator -= fixedStep;
                }
            }

            // Store manager references for ScheduleGroup
            currentLiftManager = liftManager;
            currentBeltManager = beltManager;
            currentWallManager = wallManager;

            // ========== CELL SIMULATION ==========
            // Collect active chunks into groups
            world.CollectChunkGroups(groupA, groupB, groupC, groupD);

            LastActiveChunkCount = groupA.Length + groupB.Length + groupC.Length + groupD.Length;

            // Schedule 4 passes with dependencies
            // Each pass must complete before the next starts
            if (PerformanceProfiler.Enabled)
            {
                // When profiling: complete each group separately to measure timing
                if (groupA.Length > 0)
                {
                    PerformanceProfiler.StartTiming(TimingSlot.CellSimGroupA);
                    ScheduleGroup(world, groupA, default).Complete();
                    PerformanceProfiler.StopTiming(TimingSlot.CellSimGroupA);
                }
                if (groupB.Length > 0)
                {
                    PerformanceProfiler.StartTiming(TimingSlot.CellSimGroupB);
                    ScheduleGroup(world, groupB, default).Complete();
                    PerformanceProfiler.StopTiming(TimingSlot.CellSimGroupB);
                }
                if (groupC.Length > 0)
                {
                    PerformanceProfiler.StartTiming(TimingSlot.CellSimGroupC);
                    ScheduleGroup(world, groupC, default).Complete();
                    PerformanceProfiler.StopTiming(TimingSlot.CellSimGroupC);
                }
                if (groupD.Length > 0)
                {
                    PerformanceProfiler.StartTiming(TimingSlot.CellSimGroupD);
                    ScheduleGroup(world, groupD, default).Complete();
                    PerformanceProfiler.StopTiming(TimingSlot.CellSimGroupD);
                }
            }
            else
            {
                // Normal mode: chain all groups for maximum parallelism
                JobHandle handle = default;

                if (groupA.Length > 0)
                    handle = ScheduleGroup(world, groupA, handle);
                if (groupB.Length > 0)
                    handle = ScheduleGroup(world, groupB, handle);
                if (groupC.Length > 0)
                    handle = ScheduleGroup(world, groupC, handle);
                if (groupD.Length > 0)
                    handle = ScheduleGroup(world, groupD, handle);

                // Complete all jobs
                handle.Complete();
            }

            // Reset dirty state for next frame
            world.ResetDirtyState();

            stopwatch.Stop();
            LastSimulationTimeMs = (float)stopwatch.Elapsed.TotalMilliseconds;
        }

        private JobHandle ScheduleGroup(CellWorld world, NativeList<int> chunkIndices, JobHandle dependency)
        {
            var job = new SimulateChunksJob
            {
                cells = world.cells,
                chunks = world.chunks,
                materials = world.materials,
                liftTiles = currentLiftManager?.LiftTiles ?? default,
                beltTiles = currentBeltManager?.GetBeltTiles() ?? default,
                wallTiles = currentWallManager?.WallTiles ?? default,
                chunkIndices = chunkIndices.AsArray(),
                width = world.width,
                height = world.height,
                chunksX = world.chunksX,
                chunksY = world.chunksY,
                currentFrame = world.currentFrame,
                fractionalGravity = PhysicsSettings.FractionalGravity,
                gravity = PhysicsSettings.CellGravityAccel,
                maxVelocity = PhysicsSettings.MaxVelocity,
                liftForce = PhysicsSettings.LiftForce,
                liftExitLateralForce = PhysicsSettings.LiftExitLateralForce,
            };

            // innerLoopBatchCount = 1 means each chunk is processed by one thread
            // This is appropriate since each chunk has significant work (up to 48x48 cells)
            return job.Schedule(chunkIndices.Length, 1, dependency);
        }

        public void Dispose()
        {
            if (groupA.IsCreated) groupA.Dispose();
            if (groupB.IsCreated) groupB.Dispose();
            if (groupC.IsCreated) groupC.Dispose();
            if (groupD.IsCreated) groupD.Dispose();
        }
    }
}
