namespace FallingSand
{
    /// <summary>
    /// Shared physics constants used by both cell simulation and cluster physics.
    /// Single source of truth for physics values.
    /// </summary>
    public static class PhysicsSettings
    {
        /// <summary>
        /// Gravity acceleration in cells per frame².
        /// Positive value = downward (increasing cell Y).
        /// </summary>
        public const float Gravity = 1f;

        /// <summary>
        /// Maximum fall velocity in cells per frame.
        /// </summary>
        public const int MaxVelocity = 16;

        /// <summary>
        /// Simulation speed divisor (1 = full speed, higher = slower).
        /// Controls how often gravity is applied for cells, and scales physics timestep for clusters.
        /// Runtime adjustable via numpad +/-.
        /// </summary>
        public static int SimulationSpeed { get; set; } = 15;

        /// <summary>
        /// Minimum simulation speed (fastest).
        /// </summary>
        public const int MinSimulationSpeed = 1;

        /// <summary>
        /// Maximum simulation speed divisor (slowest).
        /// </summary>
        public const int MaxSimulationSpeed = 20;

        /// <summary>
        /// Convert cell gravity to Unity Physics2D gravity.
        /// Unity uses world units per second², Y+ = up.
        /// Cell system uses cells per frame², Y+ = down.
        /// </summary>
        public static float GetUnityGravity(float cellGravity, float cellToWorldScale = 2f, float targetFps = 60f)
        {
            // cellGravity is in cells/frame²
            // Convert to world units/sec²:
            // - Multiply by cellToWorldScale to get world units
            // - Multiply by fps² to convert from per-frame to per-second
            // - Negate because Unity Y+ is up, cell Y+ is down
            return -cellGravity * cellToWorldScale * targetFps * targetFps;
        }
    }
}
