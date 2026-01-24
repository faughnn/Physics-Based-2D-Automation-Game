namespace FallingSand
{
    /// <summary>
    /// Shared physics constants used by both cell simulation and cluster physics.
    /// Single source of truth for physics values.
    /// </summary>
    public static class PhysicsSettings
    {
        /// <summary>
        /// Fractional gravity increment per frame. When the accumulator overflows 255,
        /// velocity increases by 1. Value of 17 gives ~15 frames between increments
        /// (256/17 ≈ 15), matching the old GravityInterval behavior but with smooth
        /// per-cell distribution instead of synchronized jumps.
        /// </summary>
        public const byte FractionalGravity = 17;

        /// <summary>
        /// Integer gravity applied on gravity frames (for cell integer math).
        /// Actual effect: CellGravityAccel / GravityInterval = 1/15 cells/frame²
        /// </summary>
        public const int CellGravityAccel = 1;

        /// <summary>
        /// Maximum velocity in cells per frame (any direction).
        /// Must be less than half the gap between same-group chunk cores (64 cells)
        /// to prevent race conditions during parallel chunk processing.
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
        /// <param name="cellToWorldScale">World units per cell (default CoordinateUtils.CellToWorldScale)</param>
        /// <param name="targetFps">Target frame rate (default 60)</param>
        /// <returns>Gravity in world units/sec² (negative = down)</returns>
        public static float GetUnityGravity(float cellToWorldScale = CoordinateUtils.CellToWorldScale, float targetFps = 60f)
        {
            // Effective gravity = FractionalGravity / 256 cells/frame²
            // (each frame adds FractionalGravity to accumulator; overflow at 256 increments velocity)
            float effectiveCellGravity = FractionalGravity / 256f;
            // Convert to world units/sec²:
            // - Multiply by cellToWorldScale to get world units
            // - Multiply by fps² to convert from per-frame to per-second
            // - Negate because Unity Y+ is up, cell Y+ is down
            return -effectiveCellGravity * cellToWorldScale * targetFps * targetFps;
            // Result: -(17/256) * 2 * 60 * 60 ≈ -478 units/sec² (similar to old -480)
        }
    }
}
