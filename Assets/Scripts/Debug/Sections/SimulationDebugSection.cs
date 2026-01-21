using UnityEngine;
using Unity.Jobs.LowLevel.Unsafe;

namespace FallingSand.Debugging
{
    /// <summary>
    /// Debug section for simulation performance metrics.
    /// Shows FPS, Sim Time, Worker Threads.
    /// </summary>
    public class SimulationDebugSection : DebugSectionBase
    {
        public override string SectionName => "Simulation";
        public override int Priority => 10;

        private readonly SimulationManager simulation;

        private float deltaTime;
        private int cachedFps;
        private float cachedSimTimeMs;
        private int workerThreadCount;

        public SimulationDebugSection(SimulationManager simulation)
        {
            this.simulation = simulation;
            workerThreadCount = JobsUtility.JobWorkerCount;
        }

        protected override void UpdateCachedValues()
        {
            cachedFps = Mathf.RoundToInt(1f / deltaTime);

            if (simulation != null && simulation.Simulator != null)
            {
                cachedSimTimeMs = simulation.Simulator.LastSimulationTimeMs;
            }
        }

        public override int DrawGUI(GUIStyle labelStyle, float x, float y, float lineHeight)
        {
            // If style is null, just return line count for layout calculation
            if (labelStyle == null) return 3;

            // Smooth delta time each frame for accurate FPS
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

            float width = 260f;
            int lines = 0;

            // FPS - green >= 55, yellow >= 30, red < 30
            Color fpsColor = GetPerformanceColorInverse(cachedFps, 55, 30);
            DrawLabel($"FPS: {cachedFps}", x, y + lines * lineHeight, width, lineHeight, labelStyle, fpsColor);
            lines++;

            // Sim Time - green < 8ms, yellow < 16ms, red >= 16ms
            Color simColor = GetPerformanceColor(cachedSimTimeMs, 8f, 16f);
            DrawLabel($"Sim Time: {cachedSimTimeMs:F2}ms", x, y + lines * lineHeight, width, lineHeight, labelStyle, simColor);
            lines++;

            // Worker Threads
            DrawLabel($"Worker Threads: {workerThreadCount}", x, y + lines * lineHeight, width, lineHeight, labelStyle, Color.cyan);
            lines++;

            return lines;
        }
    }
}
