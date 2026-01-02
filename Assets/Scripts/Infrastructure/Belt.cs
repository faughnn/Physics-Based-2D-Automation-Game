using UnityEngine;
using GoldRush.Core;

namespace GoldRush.Infrastructure
{
    public class Belt : MonoBehaviour
    {
        public int GridX { get; private set; }
        public int GridY { get; private set; }
        public bool MovesRight { get; private set; }

        private float speed;

        public static GameObject Create(int gridX, int gridY, bool movesRight, Transform parent = null)
        {
            GameObject beltGO = new GameObject($"Belt_{gridX}_{gridY}");
            if (parent != null) beltGO.transform.SetParent(parent);

            // Position
            Vector2 worldPos = GameSettings.GridToWorld(gridX, gridY);
            // Offset down slightly since belt is shorter than full cell
            worldPos.y -= (GameSettings.GridSize - 8) / 2f / GameSettings.PixelsPerUnit;
            beltGO.transform.position = worldPos;

            // Sprite with arrow
            SpriteRenderer sr = beltGO.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteGenerator.CreateArrowSprite(GameSettings.GridSize, 8, GameSettings.BeltColor, true, movesRight);
            sr.sortingOrder = 1;

            // Trigger collider
            BoxCollider2D col = beltGO.AddComponent<BoxCollider2D>();
            col.size = new Vector2(GameSettings.GridSize / GameSettings.PixelsPerUnit, 8f / GameSettings.PixelsPerUnit);
            col.isTrigger = true;

            // Also add a small solid collider on top so particles rest on the belt
            GameObject solidPart = new GameObject("SolidTop");
            solidPart.transform.SetParent(beltGO.transform);
            solidPart.transform.localPosition = new Vector3(0, 4f / GameSettings.PixelsPerUnit, 0);
            BoxCollider2D solidCol = solidPart.AddComponent<BoxCollider2D>();
            solidCol.size = new Vector2(GameSettings.GridSize / GameSettings.PixelsPerUnit, 2f / GameSettings.PixelsPerUnit);
            solidPart.layer = LayerSetup.InfrastructureLayer;

            // Layer
            beltGO.layer = LayerSetup.InfrastructureLayer;

            // Component
            Belt belt = beltGO.AddComponent<Belt>();
            belt.GridX = gridX;
            belt.GridY = gridY;
            belt.MovesRight = movesRight;
            belt.speed = movesRight ? GameSettings.BeltSpeed : -GameSettings.BeltSpeed;

            return beltGO;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // Apply horizontal velocity while preserving vertical
                rb.linearVelocity = new Vector2(speed, rb.linearVelocity.y);
            }
        }
    }
}
