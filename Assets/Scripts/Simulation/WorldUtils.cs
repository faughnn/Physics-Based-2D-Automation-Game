using System.Runtime.CompilerServices;

namespace FallingSand
{
    public static class WorldUtils
    {
        // Cell index to flat array index
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CellIndex(int x, int y, int width) => y * width + x;

        // Cell to chunk (using bit shift for speed, assumes ChunkSize = 32)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CellToChunkX(int cx) => cx >> 5;  // cx / 32

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CellToChunkY(int cy) => cy >> 5;  // cy / 32

        // Chunk to cell (top-left corner)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ChunkToCellX(int chunkX) => chunkX << 5;  // chunkX * 32

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ChunkToCellY(int chunkY) => chunkY << 5;  // chunkY * 32

        // Local position within chunk
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CellToLocalX(int cx) => cx & 31;  // cx % 32

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CellToLocalY(int cy) => cy & 31;  // cy % 32

        // Chunk index in flat array
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ChunkIndex(int chunkX, int chunkY, int chunksX) => chunkY * chunksX + chunkX;

        // Bounds checking
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInBounds(int x, int y, int width, int height)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }
    }
}
