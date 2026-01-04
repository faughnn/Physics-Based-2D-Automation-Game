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
            lift.InitializeGridCoords();

            return liftGO;
        }

        private void InitializeGridCoords()
        {
            if (SimulationWorld.Instance == null) return;

            Vector2 worldPos = transform.position;
            Vector2Int gridPos = SimulationWorld.Instance.WorldToGrid(worldPos);

            // Lift is 32x32 pixels = 16x16 simulation cells
            // Shrink horizontally by 1 cell on each side to avoid edge issues
            // Keep full vertical coverage so stacked lifts don't have gaps
            int halfSizeX = 7;
            int halfSizeY = 8;
            simGridMinX = gridPos.x - halfSizeX;
            simGridMaxX = gridPos.x + halfSizeX;
            simGridMinY = gridPos.y - halfSizeY;
            simGridMaxY = gridPos.y + halfSizeY;

            // Register TWO force zones to create a centering funnel effect
            // Left half: pushes right + up/down
            // Right half: pushes left + up/down
            float liftDir = MovesUp ? -1f : 1f;
            float liftForceY = liftDir * GameSettings.SimLiftForce;
            float centeringForce = GameSettings.SimLiftCenteringForce;

            // Left half zone - push right toward center
            ForceZone leftZone = new ForceZone
            {
                MinX = simGridMinX,
                MaxX = gridPos.x - 1,
                MinY = simGridMinY,
                MaxY = simGridMaxY,
                Force = new Vector2(centeringForce, liftForceY),
                Owner = this
            };
            ForceZoneManager.Instance.RegisterZone(leftZone);

            // Right half zone - push left toward center
            ForceZone rightZone = new ForceZone
            {
                MinX = gridPos.x,
                MaxX = simGridMaxX,
                MinY = simGridMinY,
                MaxY = simGridMaxY,
                Force = new Vector2(-centeringForce, liftForceY),
                Owner = this
            };
            ForceZoneManager.Instance.RegisterZone(rightZone);
        }

        private void OnDestroy()
        {
            ForceZoneManager.Instance.UnregisterZone(this);
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
                // Direct addition: push in lift direction (Unity Y is up = positive)
                float liftDir = MovesUp ? 1f : -1f;
                float newY = rb.linearVelocity.y + liftDir * GameSettings.SimLiftForce * 60f; // Scale for Unity physics
                newY = Mathf.Clamp(newY, -GameSettings.SimTerminalVelocity * 60f, GameSettings.SimTerminalVelocity * 60f);
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, newY);
            }
        }
    }
}
