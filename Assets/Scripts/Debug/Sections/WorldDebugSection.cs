using UnityEngine;

namespace FallingSand.Debugging
{
    /// <summary>
    /// Debug section for world statistics.
    /// Shows Active Chunks, Active Cells, and dirty rect gizmos.
    /// </summary>
    public class WorldDebugSection : DebugSectionBase
    {
        public override string SectionName => "World";
        public override int Priority => 20;

        private readonly CellWorld world;

        private int cachedActiveChunks;
        private int cachedActiveCells;

        public Color DirtyRectColor { get; set; } = Color.red;

        public WorldDebugSection(CellWorld world)
        {
            this.world = world;
        }

        protected override void UpdateCachedValues()
        {
            if (world != null)
            {
                cachedActiveChunks = world.CountActiveChunks();
                cachedActiveCells = world.CountActiveCells();
            }
        }

        public override int DrawGUI(GUIStyle labelStyle, float x, float y, float lineHeight)
        {
            if (labelStyle == null) return 2;

            float width = 260f;
            int lines = 0;

            DrawLabel($"Active Chunks: {cachedActiveChunks}", x, y + lines * lineHeight, width, lineHeight, labelStyle, Color.white);
            lines++;

            DrawLabel($"Active Cells: {cachedActiveCells:N0}", x, y + lines * lineHeight, width, lineHeight, labelStyle, Color.white);
            lines++;

            return lines;
        }

        public override void DrawGizmos()
        {
            if (world == null) return;

            // Draw dirty bounds for each active chunk
            for (int i = 0; i < world.chunks.Length; i++)
            {
                ChunkState chunk = world.chunks[i];

                // Only draw for dirty or recently active chunks
                if ((chunk.flags & ChunkFlags.IsDirty) == 0 && chunk.activeLastFrame == 0)
                    continue;

                int chunkX = i % world.chunksX;
                int chunkY = i / world.chunksX;

                // Calculate cell coordinates
                int baseCellX = chunkX * CellWorld.ChunkSize;
                int baseCellY = chunkY * CellWorld.ChunkSize;

                int minCellX, maxCellX, minCellY, maxCellY;

                // If bounds are inverted, draw entire chunk border
                if (chunk.minX > chunk.maxX)
                {
                    minCellX = baseCellX;
                    maxCellX = baseCellX + CellWorld.ChunkSize;
                    minCellY = baseCellY;
                    maxCellY = baseCellY + CellWorld.ChunkSize;
                }
                else
                {
                    minCellX = baseCellX + chunk.minX;
                    maxCellX = baseCellX + chunk.maxX + 1;
                    minCellY = baseCellY + chunk.minY;
                    maxCellY = baseCellY + chunk.maxY + 1;
                }

                // Convert cell coords to world coords
                Vector2 bottomLeft = CoordinateUtils.CellToWorld(minCellX, maxCellY, world.width, world.height);
                Vector2 topRight = CoordinateUtils.CellToWorld(maxCellX, minCellY, world.width, world.height);
                float x1 = bottomLeft.x;
                float x2 = topRight.x;
                float y1 = bottomLeft.y;
                float y2 = topRight.y;

                // Draw rectangle using Gizmos
                Gizmos.color = DirtyRectColor;
                Vector3 bl = new Vector3(x1, y1, 0);
                Vector3 br = new Vector3(x2, y1, 0);
                Vector3 tr = new Vector3(x2, y2, 0);
                Vector3 tl = new Vector3(x1, y2, 0);

                Gizmos.DrawLine(bl, br);
                Gizmos.DrawLine(br, tr);
                Gizmos.DrawLine(tr, tl);
                Gizmos.DrawLine(tl, bl);
            }
        }
    }
}
