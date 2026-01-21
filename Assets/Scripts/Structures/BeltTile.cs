namespace FallingSand
{
    /// <summary>
    /// Data for a single belt tile, stored in BeltManager's HashMap.
    /// The position is the HashMap key (y * width + x).
    /// </summary>
    public struct BeltTile
    {
        /// <summary>+1 for right, -1 for left</summary>
        public sbyte direction;

        /// <summary>Which BeltStructure this tile belongs to</summary>
        public ushort beltId;
    }
}
