using UnityEngine;
using UnityEngine.UI;
using GoldRush.Core;
using GoldRush.Particles;

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
            sr.sortingOrder = 1;

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

            return storeGO;
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

        private void OnTriggerEnter2D(Collider2D other)
        {
            GoldParticle gold = other.GetComponent<GoldParticle>();
            if (gold != null)
            {
                CollectGold(gold);
            }
        }

        private void CollectGold(GoldParticle gold)
        {
            // Return gold to pool
            gold.ReturnToPool();

            // Increment counts
            LocalGoldCount++;
            TotalGoldCollected++;

            // Update display
            if (counterText != null)
            {
                counterText.text = LocalGoldCount.ToString();
            }

            // Notify listeners
            OnGoldCollected?.Invoke(TotalGoldCollected);
        }

        public static void ResetTotalGold()
        {
            TotalGoldCollected = 0;
        }
    }
}
