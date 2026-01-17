using System.Runtime.InteropServices;

namespace FallingSand
{
    /// <summary>
    /// Represents a single pixel in a cluster, with position relative to center of mass.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ClusterPixel
    {
        public short localX;      // Offset from center of mass
        public short localY;      // Offset from center of mass
        public byte materialId;   // Material type of this pixel

        public ClusterPixel(short localX, short localY, byte materialId)
        {
            this.localX = localX;
            this.localY = localY;
            this.materialId = materialId;
        }
    }
}
