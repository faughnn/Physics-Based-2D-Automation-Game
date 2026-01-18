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
        public void Simulate(CellWorld world, ClusterManager clusterManager = null)
        {
            stopwatch.Restart();

            world.currentFrame++;

            // ========== CLUSTER PHYSICS (runs before cell simulation) ==========
            // Step 1: Clear old cluster pixels from grid
            // Step 2: Step Unity physics (Physics2D.Simulate)
            // Step 3: Sync cluster pixels to grid at new positions
            if (clusterManager != null)
            {
                clusterManager.StepAndSync(UnityEngine.Time.fixedDeltaTime);
            }

            // ========== CELL SIMULATION ==========
            // Collect active chunks into groups
            world.CollectChunkGroups(groupA, groupB, groupC, groupD);

            LastActiveChunkCount = groupA.Length + groupB.Length + groupC.Length + groupD.Length;

            // Schedule 4 passes with dependencies
            // Each pass must complete before the next starts
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
                chunkIndices = chunkIndices.AsArray(),
                width = world.width,
                height = world.height,
                chunksX = world.chunksX,
                chunksY = world.chunksY,
                currentFrame = world.currentFrame,
                gravityInterval = PhysicsSettings.GravityInterval,
                gravity = PhysicsSettings.CellGravityAccel,
                maxVelocity = PhysicsSettings.MaxVelocity,
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
