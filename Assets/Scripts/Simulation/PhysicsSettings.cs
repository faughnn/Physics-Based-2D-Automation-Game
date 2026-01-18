namespace FallingSand
{
    /// <summary>
    /// Shared physics constants used by both cell simulation and cluster physics.
    /// Single source of truth for physics values.
    /// </summary>
    public static class PhysicsSettings
    {
        /// <summary>
        /// Gravity interval - how many frames between gravity applications for cells.
        /// This gives effective gravity of 1/15 cells/frame², matching tested behavior.
        /// </summary>
        public const int GravityInterval = 15;

        /// <summary>
        /// Integer gravity applied on gravity frames (for cell integer math).
        /// Actual effect: CellGravityAccel / GravityInterval = 1/15 cells/frame²
        /// </summary>
        public const int CellGravityAccel = 1;

        /// <summary>
        /// Maximum fall velocity in cells per frame.
        /// </summary>
        public const int MaxVelocity = 16;

        // Sleep thresholds for cluster physics
        /// <summary>
        /// Linear velocity below which bodies can start sleeping (world units/sec).
        /// </summary>
        public const float LinearSleepTolerance = 0.5f;

        /// <summary>
        /// Angular velocity below which bodies can start sleeping (degrees/sec).
        /// </summary>
        public const float AngularSleepTolerance = 2f;

        /// <summary>
        /// Time a body must be below thresholds before sleeping (seconds).
        /// </summary>
        public const float TimeToSleep = 0.5f;

        /// <summary>
        /// Get Unity Physics2D gravity that matches cell simulation gravity.
        /// Unity uses world units per second², Y+ = up.
        /// Cell system uses cells per frame², Y+ = down.
        /// </summary>
        /// <param name="cellToWorldScale">World units per cell (default 2)</param>
        /// <param name="targetFps">Target frame rate (default 60)</param>
        /// <returns>Gravity in world units/sec² (negative = down)</returns>
        public static float GetUnityGravity(float cellToWorldScale = 2f, float targetFps = 60f)
        {
            // Effective gravity = CellGravityAccel / GravityInterval = 1/15 cells/frame²
            float effectiveCellGravity = (float)CellGravityAccel / GravityInterval;
            // Convert to world units/sec²:
            // - Multiply by cellToWorldScale to get world units
            // - Multiply by fps² to convert from per-frame to per-second
            // - Negate because Unity Y+ is up, cell Y+ is down
            return -effectiveCellGravity * cellToWorldScale * targetFps * targetFps;
            // Result: -(1/15) * 2 * 60 * 60 = -480 units/sec²
        }
    }
}
