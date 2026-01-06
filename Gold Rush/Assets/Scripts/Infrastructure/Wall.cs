using UnityEngine;
using GoldRush.Core;
using GoldRush.Building;
using GoldRush.Simulation;

namespace GoldRush.Infrastructure
{
    public class Wall : MonoBehaviour
    {
        public int GridX { get; private set; }
        public int GridY { get; private set; }

        // Grid coordinates for blocking
        private int simGridMinX, simGridMaxX, simGridMinY, simGridMaxY;

        public static GameObject Create(int gridX, int gridY, Transform parent = null)
        {
            GameObject wallGO = new GameObject($"Wall_{gridX}_{gridY}");
            if (parent != null) wallGO.transform.SetParent(parent);

            // Position using metadata grid
            var info = BuildTypeData.Get(BuildType.Wall);
            Vector2 worldPos = info.Grid.ToWorld(gridX, gridY);
            wallGO.transform.position = worldPos;

            // Sprite (16x16 solid block)
            SpriteRenderer sr = wallGO.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteGenerator.CreateSolidWallSprite(GameSettings.WallSize, GameSettings.WallColor);
            sr.sortingOrder = 8;  // Above terrain (simulation=5) but below player (10)

            // Collider (solid, not trigger) - 16x16 pixels
            float size = GameSettings.WallSize / GameSettings.PixelsPerUnit;
            BoxCollider2D col = wallGO.AddComponent<BoxCollider2D>();
            col.size = new Vector2(size, size);

            // Layer
            wallGO.layer = LayerSetup.InfrastructureLayer;

            // Component
            Wall wall = wallGO.AddComponent<Wall>();
            wall.GridX = gridX;
            wall.GridY = gridY;
            wall.InitializeGridCoords();

            return wallGO;
        }

        private void InitializeGridCoords()
        {
            if (SimulationWorld.Instance == null) return;

            Vector2 worldPos = transform.position;
            Vector2Int gridPos = SimulationWorld.Instance.WorldToGrid(worldPos);

            // Wall dimensions from metadata
            var info = BuildTypeData.Get(BuildType.Wall);
            simGridMinX = gridPos.x - info.SimHalfWidth;
            simGridMaxX = gridPos.x + info.SimHalfWidth;
            simGridMinY = gridPos.y - info.SimHalfHeight;
            simGridMaxY = gridPos.y + info.SimHalfHeight;

            // Register blocking for all cells in the wall
            var grid = SimulationWorld.Instance.Grid;
            for (int y = simGridMinY; y <= simGridMaxY; y++)
            {
                for (int x = simGridMinX; x <= simGridMaxX; x++)
                {
                    grid.SetInfrastructureBlocking(x, y, true);
                }
            }
        }

        private void OnDestroy()
        {
            // Unregister blocking when wall is destroyed
            if (SimulationWorld.Instance == null) return;
            var grid = SimulationWorld.Instance.Grid;

            for (int y = simGridMinY; y <= simGridMaxY; y++)
            {
                for (int x = simGridMinX; x <= simGridMaxX; x++)
                {
                    grid.SetInfrastructureBlocking(x, y, false);
                }
            }
        }
    }
}
