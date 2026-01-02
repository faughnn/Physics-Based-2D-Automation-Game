using UnityEngine;
using UnityEngine.UI;
using GoldRush.Core;
using GoldRush.Simulation;

namespace GoldRush.Infrastructure
{
    public class GoldStore : MonoBehaviour
    {
        public static int TotalGoldCollected { get; private set; }
        public static event System.Action<int> OnGoldCollected;

        public int GridX { get; private set; }
        public int GridY { get; private set; }
        public int LocalGoldCount { get; private set; }

        private TextMesh counterText;
        private float collectTimer;
        private const float CollectInterval = 0.1f;

        // Grid coordinates for collection area
        private int simGridMinX, simGridMaxX, simGridMinY, simGridMaxY;

        public static GameObject Create(int gridX, int gridY, Transform parent = null)
        {
            GameObject storeGO = new GameObject($"GoldStore_{gridX}_{gridY}");
            if (parent != null) storeGO.transform.SetParent(parent);

            // Position (gold store is 64x32, spans 2 cells horizontally)
            Vector2 worldPos = GameSettings.GridToWorld(gridX, gridY);
            worldPos.x += GameSettings.GridSize / 2f / GameSettings.PixelsPerUnit; // Offset to center on 2 cells
            storeGO.transform.position = worldPos;

            // Sprite
            SpriteRenderer sr = storeGO.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteGenerator.GetSprite("GoldStore");
            sr.sortingOrder = 8;  // Above terrain (simulation=5) but below player (10)

            // Trigger collider to catch gold
            BoxCollider2D triggerCol = storeGO.AddComponent<BoxCollider2D>();
            triggerCol.size = new Vector2(64f / GameSettings.PixelsPerUnit, 32f / GameSettings.PixelsPerUnit);
            triggerCol.isTrigger = true;

            // Solid bottom so gold can pile if not collected
            GameObject solidBottom = new GameObject("SolidBottom");
            solidBottom.transform.SetParent(storeGO.transform);
            solidBottom.transform.localPosition = new Vector3(0, -14f / GameSettings.PixelsPerUnit, 0);
            BoxCollider2D solidCol = solidBottom.AddComponent<BoxCollider2D>();
            solidCol.size = new Vector2(60f / GameSettings.PixelsPerUnit, 4f / GameSettings.PixelsPerUnit);
            solidBottom.layer = LayerSetup.InfrastructureLayer;

            // Layer
            storeGO.layer = LayerSetup.InfrastructureLayer;

            // Component
            GoldStore store = storeGO.AddComponent<GoldStore>();
            store.GridX = gridX;
            store.GridY = gridY;
            store.LocalGoldCount = 0;

            // Counter text
            store.CreateCounterText();

            // Initialize grid coords
            store.InitializeGridCoords();

            return storeGO;
        }

        private void InitializeGridCoords()
        {
            if (SimulationWorld.Instance == null) return;

            Vector2 worldPos = transform.position;
            Vector2Int gridPos = SimulationWorld.Instance.WorldToGrid(worldPos);

            // Store covers about 32x16 simulation cells (64x32 pixels / 2)
            int halfWidth = 16;
            int halfHeight = 8;
            simGridMinX = gridPos.x - halfWidth;
            simGridMaxX = gridPos.x + halfWidth;
            simGridMinY = gridPos.y - halfHeight;
            simGridMaxY = gridPos.y + halfHeight;
        }

        private void CreateCounterText()
        {
            GameObject textGO = new GameObject("Counter");
            textGO.transform.SetParent(transform);
            textGO.transform.localPosition = Vector3.zero;

            counterText = textGO.AddComponent<TextMesh>();
            counterText.text = "0";
            counterText.fontSize = 32;
            counterText.characterSize = 0.1f;
            counterText.anchor = TextAnchor.MiddleCenter;
            counterText.alignment = TextAlignment.Center;
            counterText.color = Color.black;

            // Add mesh renderer for sorting
            MeshRenderer mr = textGO.GetComponent<MeshRenderer>();
            mr.sortingOrder = 10;
        }

        private void Update()
        {
            if (SimulationWorld.Instance == null) return;

            collectTimer += Time.deltaTime;
            if (collectTimer < CollectInterval) return;
            collectTimer = 0f;

            CollectGoldFromGrid();
        }

        private void CollectGoldFromGrid()
        {
            var grid = SimulationWorld.Instance.Grid;
            int collected = 0;

            for (int x = simGridMinX; x <= simGridMaxX; x++)
            {
                for (int y = simGridMinY; y <= simGridMaxY; y++)
                {
                    if (grid.Get(x, y) == MaterialType.Gold)
                    {
                        grid.Set(x, y, MaterialType.Air);
                        collected++;
                    }
                }
            }

            if (collected > 0)
            {
                LocalGoldCount += collected;
                TotalGoldCollected += collected;

                if (counterText != null)
                {
                    counterText.text = LocalGoldCount.ToString();
                }

                OnGoldCollected?.Invoke(TotalGoldCollected);
            }
        }

        public static void ResetTotalGold()
        {
            TotalGoldCollected = 0;
        }
    }
}
