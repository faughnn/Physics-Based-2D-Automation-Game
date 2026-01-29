namespace FallingSand
{
    /// <summary>
    /// Data for a single wall tile, stored in WallManager's tile array.
    /// The position is the array index (y * width + x).
    /// </summary>
    public struct WallTile
    {
        /// <summary>True if this cell is part of a wall block</summary>
        public bool exists;

        /// <summary>True if this tile is underground (terrain not yet cleared)</summary>
        public bool isGhost;
    }
}
