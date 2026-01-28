using UnityEngine;

namespace FallingSand.Debugging
{
    /// <summary>
    /// Debug section for visualizing lift zones.
    /// </summary>
    public class LiftDebugSection : DebugSectionBase
    {
        private readonly LiftManager liftManager;
        private readonly int worldWidth;
        private readonly int worldHeight;

        public override string SectionName => "Lifts";
        public override int Priority => 60;

        public LiftDebugSection(LiftManager liftManager, int worldWidth, int worldHeight)
        {
            this.liftManager = liftManager;
            this.worldWidth = worldWidth;
            this.worldHeight = worldHeight;
        }

        public override int DrawGUI(GUIStyle labelStyle, float x, float y, float lineHeight)
        {
            if (labelStyle == null) return 1;

            int liftCount = liftManager?.LiftCount ?? 0;
            GUI.Label(new Rect(x, y, 300, lineHeight), $"Lifts: {liftCount}", labelStyle);
            return 1;
        }

        public override void DrawGizmos()
        {
            if (liftManager == null) return;

            var lifts = liftManager.GetLifts();
            if (!lifts.IsCreated) return;

            Gizmos.color = new Color(0.4f, 0.8f, 0.4f, 0.3f);  // Semi-transparent green

            for (int i = 0; i < lifts.Length; i++)
            {
                var lift = lifts[i];

                // Convert lift bounds to world coordinates
                float minX = lift.tileX;
                float maxX = lift.tileX + LiftStructure.Width;
                float minY = lift.minY;
                float maxY = lift.maxY + LiftStructure.Height;

                // Convert cell coords to world coords
                Vector2 worldMin = CoordinateUtils.CellToWorld((int)minX, (int)maxY, worldWidth, worldHeight);
                Vector2 worldMax = CoordinateUtils.CellToWorld((int)maxX, (int)minY, worldWidth, worldHeight);

                Vector3 center = new Vector3(
                    (worldMin.x + worldMax.x) / 2f,
                    (worldMin.y + worldMax.y) / 2f,
                    0
                );
                Vector3 size = new Vector3(
                    worldMax.x - worldMin.x,
                    worldMax.y - worldMin.y,
                    0.1f
                );

                Gizmos.DrawCube(center, size);

                // Draw outline
                Gizmos.color = new Color(0.4f, 0.8f, 0.4f, 0.8f);
                Gizmos.DrawWireCube(center, size);
                Gizmos.color = new Color(0.4f, 0.8f, 0.4f, 0.3f);
            }
        }
    }
}
