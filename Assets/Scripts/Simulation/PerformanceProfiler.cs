using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace FallingSand
{
    /// <summary>
    /// Timing slots for profiling different simulation systems.
    /// </summary>
    public enum TimingSlot
    {
        ClusterPhysics,
        ClusterSync,
        CellSimGroupA,
        CellSimGroupB,
        CellSimGroupC,
        CellSimGroupD,
        BeltSimulation,
        RenderUpload,
        TerrainColliders,
        Digging,
        GhostStateUpdate,
        Count
    }

    /// <summary>
    /// Contains all frame timing data in milliseconds.
    /// </summary>
    public struct FrameTimings
    {
        public float ClusterPhysicsMs;
        public float ClusterSyncMs;
        public float CellSimGroupAMs;
        public float CellSimGroupBMs;
        public float CellSimGroupCMs;
        public float CellSimGroupDMs;
        public float BeltSimulationMs;
        public float RenderUploadMs;
        public float TerrainCollidersMs;
        public float DiggingMs;
        public float GhostStateUpdateMs;

        public float TotalCellSimMs => CellSimGroupAMs + CellSimGroupBMs + CellSimGroupCMs + CellSimGroupDMs;
        public float TotalClusterMs => ClusterPhysicsMs + ClusterSyncMs;

        public float TotalFrameMs =>
            ClusterPhysicsMs + ClusterSyncMs +
            CellSimGroupAMs + CellSimGroupBMs + CellSimGroupCMs + CellSimGroupDMs +
            BeltSimulationMs + RenderUploadMs + TerrainCollidersMs +
            DiggingMs + GhostStateUpdateMs;

        /// <summary>
        /// Frame budget percentage (based on 60 FPS = 16.667ms budget).
        /// </summary>
        public float FrameBudgetPercent => (TotalFrameMs / 16.667f) * 100f;
    }

    /// <summary>
    /// Centralized performance profiler for the simulation.
    /// Toggle with F5 in debug overlay. Zero overhead when disabled.
    /// </summary>
    public static class PerformanceProfiler
    {
        private static bool enabled = false;
        private static readonly Stopwatch[] stopwatches;
        private static readonly float[] timingsMs;
        private static FrameTimings currentTimings;

        /// <summary>
        /// Enable/disable profiling. When disabled, StartTiming/StopTiming have zero overhead.
        /// </summary>
        public static bool Enabled
        {
            get => enabled;
            set => enabled = value;
        }

        /// <summary>
        /// Current frame timings. Updated after EndFrame() is called.
        /// </summary>
        public static FrameTimings CurrentTimings => currentTimings;

        static PerformanceProfiler()
        {
            int count = (int)TimingSlot.Count;
            stopwatches = new Stopwatch[count];
            timingsMs = new float[count];

            for (int i = 0; i < count; i++)
            {
                stopwatches[i] = new Stopwatch();
            }
        }

        /// <summary>
        /// Start timing a specific slot. No-op when profiling is disabled.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StartTiming(TimingSlot slot)
        {
            if (!enabled) return;
            stopwatches[(int)slot].Restart();
        }

        /// <summary>
        /// Stop timing a specific slot. No-op when profiling is disabled.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StopTiming(TimingSlot slot)
        {
            if (!enabled) return;
            var sw = stopwatches[(int)slot];
            sw.Stop();
            timingsMs[(int)slot] = (float)sw.Elapsed.TotalMilliseconds;
        }

        /// <summary>
        /// Consolidate timings into the FrameTimings struct. Call at end of frame.
        /// </summary>
        public static void EndFrame()
        {
            if (!enabled) return;

            currentTimings = new FrameTimings
            {
                ClusterPhysicsMs = timingsMs[(int)TimingSlot.ClusterPhysics],
                ClusterSyncMs = timingsMs[(int)TimingSlot.ClusterSync],
                CellSimGroupAMs = timingsMs[(int)TimingSlot.CellSimGroupA],
                CellSimGroupBMs = timingsMs[(int)TimingSlot.CellSimGroupB],
                CellSimGroupCMs = timingsMs[(int)TimingSlot.CellSimGroupC],
                CellSimGroupDMs = timingsMs[(int)TimingSlot.CellSimGroupD],
                BeltSimulationMs = timingsMs[(int)TimingSlot.BeltSimulation],
                RenderUploadMs = timingsMs[(int)TimingSlot.RenderUpload],
                TerrainCollidersMs = timingsMs[(int)TimingSlot.TerrainColliders],
                DiggingMs = timingsMs[(int)TimingSlot.Digging],
                GhostStateUpdateMs = timingsMs[(int)TimingSlot.GhostStateUpdate]
            };

            // Reset timings for next frame
            for (int i = 0; i < timingsMs.Length; i++)
            {
                timingsMs[i] = 0f;
            }
        }
    }
}
