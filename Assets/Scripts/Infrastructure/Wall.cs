using UnityEngine;
using GoldRush.Core;

namespace GoldRush.Infrastructure
{
    public class Wall : MonoBehaviour
    {
        public int GridX { get; private set; }
        public int GridY { get; private set; }

        public static GameObject Create(int gridX, int gridY, Transform parent = null)
        {
            GameObject wallGO = new GameObject($"Wall_{gridX}_{gridY}");
            if (parent != null) wallGO.transform.SetParent(parent);

            // Position
            Vector2 worldPos = GameSettings.GridToWorld(gridX, gridY);
            wallGO.transform.position = worldPos;

            // Sprite
            SpriteRenderer sr = wallGO.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteGenerator.GetSprite("Wall");
            sr.sortingOrder = 1;

            // Collider (solid, not trigger)
            BoxCollider2D col = wallGO.AddComponent<BoxCollider2D>();
            col.size = new Vector2(GameSettings.GridSize / GameSettings.PixelsPerUnit,
                                   GameSettings.GridSize / GameSettings.PixelsPerUnit);

            // Layer
            wallGO.layer = LayerSetup.InfrastructureLayer;

            // Component
            Wall wall = wallGO.AddComponent<Wall>();
            wall.GridX = gridX;
            wall.GridY = gridY;

            return wallGO;
        }
    }
}
