using System.Runtime.InteropServices;

namespace FallingSand
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ChunkState
    {
        public ushort minX, minY;      // Dirty region bounds (local to chunk, 0-31)
        public ushort maxX, maxY;
        public byte flags;             // ChunkFlags below
        public byte activeLastFrame;   // Was dirty last frame? (for neighbour waking)
        public ushort structureMask;   // Bitmask of structures in this chunk
    }

    public static class ChunkFlags
    {
        public const byte None         = 0;
        public const byte IsDirty      = 1 << 0;
        public const byte HasStructure = 1 << 1;  // Always simulates
    }
}
