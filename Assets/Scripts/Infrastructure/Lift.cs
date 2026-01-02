using UnityEngine;
using GoldRush.Core;

namespace GoldRush.Infrastructure
{
    public class Lift : MonoBehaviour
    {
        public int GridX { get; private set; }
        public int GridY { get; private set; }
        public bool MovesUp { get; private set; }

        private float targetSpeed;
        private float acceleration;

        public static GameObject Create(int gridX, int gridY, bool movesUp, Transform parent = null)
        {
            GameObject liftGO = new GameObject($"Lift_{gridX}_{gridY}");
            if (parent != null) liftGO.transform.SetParent(parent);

            // Position
            Vector2 worldPos = GameSettings.GridToWorld(gridX, gridY);
            liftGO.transform.position = worldPos;

            // Create hollow visual (just the borders)
            CreateHollowVisual(liftGO, movesUp);

            // Create trigger zone in the middle
            BoxCollider2D triggerCol = liftGO.AddComponent<BoxCollider2D>();
            float size = GameSettings.GridSize / GameSettings.PixelsPerUnit;
            triggerCol.size = new Vector2(size * 0.8f, size * 0.8f); // Slightly smaller than cell
            triggerCol.isTrigger = true;

            // Create solid side walls
            CreateSideWalls(liftGO);

            // Layer
            liftGO.layer = LayerSetup.InfrastructureLayer;

            // Component
            Lift lift = liftGO.AddComponent<Lift>();
            lift.GridX = gridX;
            lift.GridY = gridY;
            lift.MovesUp = movesUp;
            lift.targetSpeed = movesUp ? GameSettings.LiftSpeed : -GameSettings.LiftSpeed;
            lift.acceleration = GameSettings.LiftAcceleration;

            return liftGO;
        }

        private static void CreateHollowVisual(GameObject parent, bool movesUp)
        {
            float size = GameSettings.GridSize / GameSettings.PixelsPerUnit;
            float borderWidth = 4f / GameSettings.PixelsPerUnit;

            // Create a sprite for the frame
            SpriteRenderer sr = parent.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteGenerator.CreateArrowSprite(GameSettings.GridSize, GameSettings.GridSize,
                                                          GameSettings.LiftColor, false, movesUp);
            sr.sortingOrder = 1;
        }

        private static void CreateSideWalls(GameObject parent)
        {
            float size = GameSettings.GridSize / GameSettings.PixelsPerUnit;
            float wallThickness = 4f / GameSettings.PixelsPerUnit;

            // Left wall
            GameObject leftWall = new GameObject("LeftWall");
            leftWall.transform.SetParent(parent.transform);
            leftWall.transform.localPosition = new Vector3(-size / 2 + wallThickness / 2, 0, 0);
            BoxCollider2D leftCol = leftWall.AddComponent<BoxCollider2D>();
            leftCol.size = new Vector2(wallThickness, size);
            leftWall.layer = LayerSetup.InfrastructureLayer;

            // Right wall
            GameObject rightWall = new GameObject("RightWall");
            rightWall.transform.SetParent(parent.transform);
            rightWall.transform.localPosition = new Vector3(size / 2 - wallThickness / 2, 0, 0);
            BoxCollider2D rightCol = rightWall.AddComponent<BoxCollider2D>();
            rightCol.size = new Vector2(wallThickness, size);
            rightWall.layer = LayerSetup.InfrastructureLayer;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // Gradual acceleration towards target speed
                float currentY = rb.linearVelocity.y;
                float newY = Mathf.MoveTowards(currentY, targetSpeed, acceleration * Time.fixedDeltaTime);
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, newY);
            }
        }
    }
}
