namespace FallingSand
{
    /// <summary>
    /// Data for a single lift tile, stored in LiftManager's tile array.
    /// The position is the array index (y * width + x).
    /// </summary>
    public struct LiftTile
    {
        /// <summary>Which LiftStructure this tile belongs to (0 = not a lift)</summary>
        public ushort liftId;

        /// <summary>Original lift material for restoration after a cell passes through</summary>
        public byte materialId;

        /// <summary>True if this tile is underground (terrain not yet cleared)</summary>
        public bool isGhost;
    }
}
