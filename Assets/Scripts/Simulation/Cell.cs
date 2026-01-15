using System.Runtime.InteropServices;

namespace FallingSand
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Cell
    {
        public byte materialId;      // Index into material definitions
        public byte flags;           // State flags (see CellFlags)
        public ushort frameUpdated;  // Prevents double-processing per frame
        public sbyte velocityX;      // Horizontal velocity (-16 to +16 cells)
        public sbyte velocityY;      // Vertical velocity (-16 to +16 cells)
        public byte temperature;     // 0-255 for heat simulation
        public byte structureId;     // If attached to a structure, which one (0 = none)
    }
    // Size: 8 bytes per cell

    public static class CellFlags
    {
        public const byte None    = 0;
        public const byte OnBelt  = 1 << 0;  // Being moved by a belt
        public const byte OnLift  = 1 << 1;  // Being moved by a lift
        public const byte Burning = 1 << 2;  // Currently on fire
        public const byte Wet     = 1 << 3;  // In contact with liquid
    }
}
