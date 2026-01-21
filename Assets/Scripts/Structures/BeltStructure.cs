namespace FallingSand
{
    /// <summary>
    /// A contiguous horizontal run of belt blocks moving in the same direction.
    /// Each belt block is 8x8 cells. The surface where cells sit is at tileY + Height.
    /// </summary>
    public struct BeltStructure
    {
        /// <summary>Width of a single belt block in cells</summary>
        public const int Width = 8;

        /// <summary>Height of a single belt block in cells</summary>
        public const int Height = 8;

        /// <summary>Unique identifier for this belt</summary>
        public ushort id;

        /// <summary>Y coordinate of the bottom of the 8x8 belt block</summary>
        public int tileY;

        /// <summary>Leftmost X coordinate of the belt</summary>
        public int minX;

        /// <summary>Rightmost X coordinate of the belt</summary>
        public int maxX;

        /// <summary>+1 for right, -1 for left</summary>
        public sbyte direction;

        /// <summary>Frames per move (1 = every frame, 3 = every 3 frames)</summary>
        public byte speed;

        /// <summary>Frame offset for staggered movement timing</summary>
        public byte frameOffset;

        /// <summary>Y coordinate where cells sit (one row above the belt, at smaller Y value)</summary>
        /// <remarks>In this coordinate system, Y=0 is at top and Y increases downward.
        /// Cells fall by increasing Y, so they pile up at tileY - 1 (above the belt).</remarks>
        public int SurfaceY => tileY - 1;

        /// <summary>Total span of the belt in cells (may include multiple merged blocks)</summary>
        public int Span => maxX - minX + Width;
    }
}
