using UnityEngine;
using GoldRush.Core;
using GoldRush.Building;
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

            // Position using metadata grid
            var info = BuildTypeData.Get(BuildType.Belt);
            Vector2 worldPos = info.Grid.ToWorld(gridX, gridY);
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

            // Belt coverage from metadata
            var info = BuildTypeData.Get(BuildType.Belt);
            simGridMinX = gridPos.x - info.SimHalfWidth;
            simGridMaxX = gridPos.x + info.SimHalfWidth;
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

            // Register belt surface for bulk transport
            // Every N frames, all materials on this surface shift 1 cell in belt direction
            BeltSurface surface = new BeltSurface
            {
                MinX = simGridMinX,
                MaxX = simGridMaxX,
                SurfaceY = simGridY,
                MovesRight = MovesRight,
                Owner = this
            };
            BeltVelocityManager.Instance.RegisterBeltSurface(surface);
        }

        private void OnDestroy()
        {
            // Unregister belt surface
            BeltVelocityManager.Instance.UnregisterBeltSurface(this);

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
