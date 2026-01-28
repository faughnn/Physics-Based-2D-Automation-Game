namespace FallingSand
{
    /// <summary>
    /// A contiguous vertical column of lift blocks.
    /// Each lift block is 8x8 cells. Lifts are hollow force zones - material passes through
    /// and experiences upward force that fights gravity.
    /// </summary>
    public struct LiftStructure
    {
        /// <summary>Width of a single lift block in cells</summary>
        public const int Width = 8;

        /// <summary>Height of a single lift block in cells</summary>
        public const int Height = 8;

        /// <summary>Unique identifier for this lift</summary>
        public ushort id;

        /// <summary>X coordinate of the left edge of the 8x8 lift block</summary>
        public int tileX;

        /// <summary>Topmost Y coordinate of the lift (smallest Y value)</summary>
        public int minY;

        /// <summary>Bottommost Y coordinate of the lift's top-left corner (largest Y value)</summary>
        public int maxY;

        /// <summary>Fractional lift force (default 20, gravity is 17)</summary>
        public byte liftForce;

        /// <summary>Total vertical span of the lift in cells (may include multiple merged blocks)</summary>
        public int Span => maxY - minY + Height;
    }
}
