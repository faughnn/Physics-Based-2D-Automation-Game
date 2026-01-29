using UnityEngine;
using Unity.Jobs.LowLevel.Unsafe;

namespace FallingSand.Debugging
{
    /// <summary>
    /// Debug section for simulation performance metrics.
    /// Shows FPS, Sim Time, Worker Threads.
    /// When profiling enabled (F5), shows detailed per-system timing breakdown.
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
            bool profilingEnabled = FallingSand.PerformanceProfiler.Enabled;

            // If style is null, just return line count for layout calculation
            if (labelStyle == null)
            {
                // Base lines: FPS, Sim Time, Worker Threads = 3
                // Profiling adds: blank, Frame Budget, blank, "Cell Sim Groups" header, 4 groups,
                // blank, "Other Systems" header, 6 systems = 15 more
                return profilingEnabled ? 18 : 3;
            }

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

            // Profiling detail section
            if (profilingEnabled)
            {
                var timings = FallingSand.PerformanceProfiler.CurrentTimings;

                // Blank line
                lines++;

                // Frame budget - green < 60%, yellow < 90%, red >= 90%
                float budgetPercent = timings.FrameBudgetPercent;
                Color budgetColor = GetPerformanceColor(budgetPercent, 60f, 90f);
                DrawLabel($"Frame Budget: {budgetPercent:F1}% of 16.6ms", x, y + lines * lineHeight, width, lineHeight, labelStyle, budgetColor);
                lines++;

                // Blank line
                lines++;

                // Cell Sim Groups header
                DrawLabel("--- Cell Sim Groups ---", x, y + lines * lineHeight, width, lineHeight, labelStyle, Color.yellow);
                lines++;

                // Group timings - green < 2ms, yellow < 4ms, red >= 4ms
                DrawTimingLine("Group A", timings.CellSimGroupAMs, 2f, 4f, x, y, lines, width, lineHeight, labelStyle);
                lines++;
                DrawTimingLine("Group B", timings.CellSimGroupBMs, 2f, 4f, x, y, lines, width, lineHeight, labelStyle);
                lines++;
                DrawTimingLine("Group C", timings.CellSimGroupCMs, 2f, 4f, x, y, lines, width, lineHeight, labelStyle);
                lines++;
                DrawTimingLine("Group D", timings.CellSimGroupDMs, 2f, 4f, x, y, lines, width, lineHeight, labelStyle);
                lines++;

                // Blank line
                lines++;

                // Other Systems header
                DrawLabel("--- Other Systems ---", x, y + lines * lineHeight, width, lineHeight, labelStyle, Color.yellow);
                lines++;

                // Cluster Physics - green < 2ms, yellow < 5ms, red >= 5ms
                DrawTimingLine("Cluster Physics", timings.ClusterPhysicsMs, 2f, 5f, x, y, lines, width, lineHeight, labelStyle);
                lines++;

                // Cluster Sync - green < 1ms, yellow < 3ms, red >= 3ms
                DrawTimingLine("Cluster Sync", timings.ClusterSyncMs, 1f, 3f, x, y, lines, width, lineHeight, labelStyle);
                lines++;

                // Belt Simulation - green < 1ms, yellow < 3ms, red >= 3ms
                DrawTimingLine("Belt Sim", timings.BeltSimulationMs, 1f, 3f, x, y, lines, width, lineHeight, labelStyle);
                lines++;

                // Render Upload - green < 1ms, yellow < 3ms, red >= 3ms
                DrawTimingLine("Render Upload", timings.RenderUploadMs, 1f, 3f, x, y, lines, width, lineHeight, labelStyle);
                lines++;

                // Terrain Colliders - green < 1ms, yellow < 3ms, red >= 3ms
                DrawTimingLine("Terrain Colliders", timings.TerrainCollidersMs, 1f, 3f, x, y, lines, width, lineHeight, labelStyle);
                lines++;

                // Ghost States - green < 1ms, yellow < 3ms, red >= 3ms
                DrawTimingLine("Ghost States", timings.GhostStateUpdateMs, 1f, 3f, x, y, lines, width, lineHeight, labelStyle);
                lines++;
            }

            return lines;
        }

        private void DrawTimingLine(string label, float ms, float greenThreshold, float yellowThreshold,
            float x, float y, int lineIndex, float width, float lineHeight, GUIStyle labelStyle)
        {
            Color color = GetPerformanceColor(ms, greenThreshold, yellowThreshold);
            DrawLabel($"{label}: {ms:F2}ms", x, y + lineIndex * lineHeight, width, lineHeight, labelStyle, color);
        }
    }
}

