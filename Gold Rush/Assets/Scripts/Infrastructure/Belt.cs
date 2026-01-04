using UnityEngine;
using GoldRush.Core;
using GoldRush.Simulation;

namespace GoldRush.Infrastructure
{
    public class Belt : MonoBehaviour
    {
        public int GridX { get; private set; }
        public int GridY { get; private set; }
        public bool MovesRight { get; private set; }

        // Grid coordinates for this belt's area
        private int simGridMinX, simGridMaxX, simGridY;

        public static GameObject Create(int gridX, int gridY, bool movesRight, Transform parent = null)
        {
            GameObject beltGO = new GameObject($"Belt_{gridX}_{gridY}");
            if (parent != null) beltGO.transform.SetParent(parent);

            // Position using sub-grid (16x16 pixel cells)
            Vector2 worldPos = GameSettings.SubGridToWorld(gridX, gridY);
            beltGO.transform.position = worldPos;

            // Sprite with arrow (16x16 pixels)
            SpriteRenderer sr = beltGO.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteGenerator.CreateArrowSprite(GameSettings.BeltSize, GameSettings.BeltSize, GameSettings.BeltColor, true, movesRight);
            sr.sortingOrder = 8;  // Above terrain (simulation=5) but below player (10)

            // Trigger collider (16x16 pixels)
            float size = GameSettings.BeltSize / GameSettings.PixelsPerUnit;
            BoxCollider2D col = beltGO.AddComponent<BoxCollider2D>();
            col.size = new Vector2(size, size);
            col.isTrigger = true;

            // Solid top surface (16x2 pixels, positioned at top of belt)
            GameObject solidPart = new GameObject("SolidTop");
            solidPart.transform.SetParent(beltGO.transform);
            solidPart.transform.localPosition = new Vector3(0, 8f / GameSettings.PixelsPerUnit, 0);
            BoxCollider2D solidCol = solidPart.AddComponent<BoxCollider2D>();
            solidCol.size = new Vector2(size, 2f / GameSettings.PixelsPerUnit);
            solidPart.layer = LayerSetup.InfrastructureLayer;

            // Layer
            beltGO.layer = LayerSetup.InfrastructureLayer;

            // Component
            Belt belt = beltGO.AddComponent<Belt>();
            belt.GridX = gridX;
            belt.GridY = gridY;
            belt.MovesRight = movesRight;
            belt.InitializeGridCoords();

            return beltGO;
        }

        private void InitializeGridCoords()
        {
            if (SimulationWorld.Instance == null) return;

            // Get the surface position ABOVE the belt in world coords
            // Belt is 16x16 pixels (top at +8), particles rest on top (+9 to clear visual overlap)
            Vector2 surfacePos = (Vector2)transform.position + new Vector2(0, 9f / GameSettings.PixelsPerUnit);
            Vector2Int gridPos = SimulationWorld.Instance.WorldToGrid(surfacePos);

            // Belt covers about 8 simulation cells wide (16 pixels / 2)
            int halfWidth = 4;
            simGridMinX = gridPos.x - halfWidth;
            simGridMaxX = gridPos.x + halfWidth;
            simGridY = gridPos.y;  // Surface level in grid coords

            // Register blocking cells - block the interior of the belt (below surface)
            var grid = SimulationWorld.Instance.Grid;
            for (int x = simGridMinX; x <= simGridMaxX; x++)
            {
                // Block 8 rows inside the belt (16 pixels / 2)
                for (int dy = 1; dy <= 8; dy++)
                {
                    grid.SetInfrastructureBlocking(x, simGridY + dy, true);
                }
            }

            // Register force zone for the belt surface (particles resting on top get pushed)
            // Surface zone: covers cells at surface level and a few cells above for stacking
            float beltDir = MovesRight ? 1f : -1f;
            ForceZone zone = new ForceZone
            {
                MinX = simGridMinX,
                MaxX = simGridMaxX,
                MinY = simGridY - 8,  // Cover stacked particles (up to 16 pixels above)
                MaxY = simGridY,       // Surface level
                Force = new Vector2(beltDir * GameSettings.SimBeltForce, 0),
                Owner = this
            };
            ForceZoneManager.Instance.RegisterZone(zone);
        }

        private void OnDestroy()
        {
            // Unregister force zone
            ForceZoneManager.Instance.UnregisterZone(this);

            // Unregister blocking cells when belt is destroyed
            if (SimulationWorld.Instance == null) return;
            var grid = SimulationWorld.Instance.Grid;

            for (int x = simGridMinX; x <= simGridMaxX; x++)
            {
                // Unblock 8 rows (16 pixels / 2)
                for (int dy = 1; dy <= 8; dy++)
                {
                    grid.SetInfrastructureBlocking(x, simGridY + dy, false);
                }
            }
        }
    }
}
