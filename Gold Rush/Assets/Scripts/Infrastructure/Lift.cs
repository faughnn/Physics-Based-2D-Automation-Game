using UnityEngine;
using GoldRush.Core;
using GoldRush.Simulation;

namespace GoldRush.Infrastructure
{
    public class Lift : MonoBehaviour
    {
        public int GridX { get; private set; }
        public int GridY { get; private set; }
        public bool MovesUp { get; private set; }

        private float targetSpeed;
        private float liftAcceleration;
        private float accelerateTimer;
        private const float AccelerateInterval = 0.016f;  // Apply acceleration every frame (~60fps)

        // Grid coordinates for this lift's area in simulation grid
        private int simGridMinX, simGridMaxX, simGridMinY, simGridMaxY;

        public static GameObject Create(int gridX, int gridY, bool movesUp, Transform parent = null)
        {
            GameObject liftGO = new GameObject($"Lift_{gridX}_{gridY}");
            if (parent != null) liftGO.transform.SetParent(parent);

            // Position using infra grid (32x32 pixel cells)
            Vector2 worldPos = GameSettings.InfraGridToWorld(gridX, gridY);
            liftGO.transform.position = worldPos;

            // Create hollow visual (32x32 with thin walls)
            CreateHollowVisual(liftGO, movesUp);

            // Create trigger zone for the full area (no walls)
            float size = GameSettings.InfraGridSize / GameSettings.PixelsPerUnit;
            BoxCollider2D triggerCol = liftGO.AddComponent<BoxCollider2D>();
            triggerCol.size = new Vector2(size, size);  // Full size, no walls
            triggerCol.isTrigger = true;

            // Layer
            liftGO.layer = LayerSetup.InfrastructureLayer;

            // Component
            Lift lift = liftGO.AddComponent<Lift>();
            lift.GridX = gridX;
            lift.GridY = gridY;
            lift.MovesUp = movesUp;
            lift.targetSpeed = movesUp ? GameSettings.LiftSpeed : -GameSettings.LiftSpeed;
            lift.liftAcceleration = GameSettings.LiftAcceleration;
            lift.InitializeGridCoords();

            return liftGO;
        }

        private void InitializeGridCoords()
        {
            if (SimulationWorld.Instance == null) return;

            Vector2 worldPos = transform.position;
            Vector2Int gridPos = SimulationWorld.Instance.WorldToGrid(worldPos);

            // Lift is 32x32 pixels = 16x16 simulation cells
            int halfSize = 8;
            simGridMinX = gridPos.x - halfSize;
            simGridMaxX = gridPos.x + halfSize;
            simGridMinY = gridPos.y - halfSize;
            simGridMaxY = gridPos.y + halfSize;

            // No wall blocking - lift is open for particles to flow through
        }

        private void OnDestroy()
        {
            // No blocking to unregister - lift is open
        }

        private const int WakeZoneBuffer = 8;  // Wake zone extends 8 cells beyond infrastructure

        private void Update()
        {
            if (SimulationWorld.Instance == null) return;

            var grid = SimulationWorld.Instance.Grid;

            // Wake all particles in and around the lift (ActiveSet optimization)
            for (int y = simGridMinY - WakeZoneBuffer; y <= simGridMaxY + WakeZoneBuffer; y++)
            {
                for (int x = simGridMinX - WakeZoneBuffer; x <= simGridMaxX + WakeZoneBuffer; x++)
                {
                    if (MaterialProperties.IsSimulated(grid.Get(x, y)))
                    {
                        grid.WakeCell(x, y);
                    }
                }
            }

            accelerateTimer += Time.deltaTime;
            if (accelerateTimer < AccelerateInterval) return;
            accelerateTimer = 0f;

            // Target velocity in grid coords (negative Y = up)
            float targetVelY = MovesUp ? -targetSpeed : targetSpeed;

            for (int y = simGridMinY; y <= simGridMaxY; y++)
            {
                for (int x = simGridMinX; x <= simGridMaxX; x++)
                {
                    MaterialType type = grid.Get(x, y);

                    if (MaterialProperties.IsSimulated(type))
                    {
                        // Get current velocity
                        Vector2 vel = grid.GetVelocity(x, y);

                        // Accelerate toward target - use strong acceleration
                        float accelStep = liftAcceleration * AccelerateInterval;
                        vel.y = Mathf.MoveTowards(vel.y, targetVelY, accelStep);

                        // Ensure minimum upward velocity while in lift
                        if (MovesUp && vel.y > -2f)
                        {
                            vel.y = -2f;  // Minimum upward velocity
                        }

                        grid.SetVelocity(x, y, vel);
                    }
                }
            }
        }

        private static void CreateHollowVisual(GameObject parent, bool movesUp)
        {
            // Create a sprite for the hollow frame (32x32 pixels)
            SpriteRenderer sr = parent.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteGenerator.CreateHollowLiftSprite(GameSettings.InfraGridSize, GameSettings.LiftColor, movesUp);
            sr.sortingOrder = 8;  // Above terrain (simulation=5) but below player (10)
        }


        private void OnTriggerStay2D(Collider2D other)
        {
            // Legacy physics-based acceleration for any remaining rigidbody particles
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                float currentY = rb.linearVelocity.y;
                float newY = Mathf.MoveTowards(currentY, targetSpeed, liftAcceleration * Time.fixedDeltaTime);
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, newY);
            }
        }
    }
}
