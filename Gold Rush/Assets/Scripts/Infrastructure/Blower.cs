using UnityEngine;
using GoldRush.Core;
using GoldRush.Simulation;

namespace GoldRush.Infrastructure
{
    public class Blower : MonoBehaviour
    {
        public int GridX { get; private set; }
        public int GridY { get; private set; }
        public bool BlowsRight { get; private set; }

        // Grid coordinates for this blower's area in simulation grid
        private int simGridMinX, simGridMaxX, simGridMinY, simGridMaxY;

        public static GameObject Create(int gridX, int gridY, bool blowsRight, Transform parent = null)
        {
            GameObject blowerGO = new GameObject($"Blower_{gridX}_{gridY}");
            if (parent != null) blowerGO.transform.SetParent(parent);

            // Position using infra grid (32x32 pixel cells, same as Lift)
            Vector2 worldPos = GameSettings.InfraGridToWorld(gridX, gridY);
            blowerGO.transform.position = worldPos;

            // Create hollow visual (32x32 with thin walls, horizontal)
            CreateHollowVisual(blowerGO, blowsRight);

            // Trigger collider (32x32 pixels, same as Lift)
            float size = GameSettings.InfraGridSize / GameSettings.PixelsPerUnit;
            BoxCollider2D triggerCol = blowerGO.AddComponent<BoxCollider2D>();
            triggerCol.size = new Vector2(size, size);
            triggerCol.isTrigger = true;

            // Layer
            blowerGO.layer = LayerSetup.InfrastructureLayer;

            // Component
            Blower blower = blowerGO.AddComponent<Blower>();
            blower.GridX = gridX;
            blower.GridY = gridY;
            blower.BlowsRight = blowsRight;
            blower.InitializeGridCoords();

            return blowerGO;
        }

        private static void CreateHollowVisual(GameObject parent, bool blowsRight)
        {
            // Create a sprite for the hollow frame (32x32 pixels, horizontal)
            SpriteRenderer sr = parent.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteGenerator.CreateHollowBlowerSprite(GameSettings.InfraGridSize, GameSettings.BlowerColor, blowsRight);
            sr.sortingOrder = 8;  // Above terrain (simulation=5) but below player (10)
        }

        private void InitializeGridCoords()
        {
            if (SimulationWorld.Instance == null) return;

            Vector2 worldPos = transform.position;
            Vector2Int gridPos = SimulationWorld.Instance.WorldToGrid(worldPos);

            // Blower is 32x32 pixels = 16x16 simulation cells (same as Lift)
            int halfSize = 8;
            simGridMinX = gridPos.x - halfSize;
            simGridMaxX = gridPos.x + halfSize;
            simGridMinY = gridPos.y - halfSize;
            simGridMaxY = gridPos.y + halfSize;

            // Register force zone - horizontal force
            float blowDir = BlowsRight ? 1f : -1f;
            ForceZone zone = new ForceZone
            {
                MinX = simGridMinX,
                MaxX = simGridMaxX,
                MinY = simGridMinY,
                MaxY = simGridMaxY,
                Force = new Vector2(blowDir * GameSettings.SimBlowerForce, 0),
                Owner = this
            };
            ForceZoneManager.Instance.RegisterZone(zone);
        }

        private void OnDestroy()
        {
            ForceZoneManager.Instance.UnregisterZone(this);
        }
    }
}
