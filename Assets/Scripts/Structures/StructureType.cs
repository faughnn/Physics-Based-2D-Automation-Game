namespace FallingSand
{
    /// <summary>
    /// Type identifier for structures, stored in Cell.structureId.
    /// Each structure type has its own manager that owns detailed data.
    /// </summary>
    public enum StructureType : byte
    {
        None = 0,
        Belt = 1,
        Lift = 2,
        Furnace = 3, // Future
        Press = 4,   // Future
        Wall = 5,
        Piston = 6,
    }
}
