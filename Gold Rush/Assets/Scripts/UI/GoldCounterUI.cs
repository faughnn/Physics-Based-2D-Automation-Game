using UnityEngine;
using UnityEngine.UI;
using GoldRush.Infrastructure;

namespace GoldRush.UI
{
    public class GoldCounterUI : MonoBehaviour
    {
        private Text counterText;

        public void Initialize()
        {
            RectTransform rectTransform = gameObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1, 1);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.pivot = new Vector2(1, 1);
            rectTransform.anchoredPosition = new Vector2(-20, -20);
            rectTransform.sizeDelta = new Vector2(200, 50);

            // Background
            Image bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            // Gold icon (simple colored square)
            GameObject iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(transform);

            RectTransform iconRect = iconGO.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0, 0.5f);
            iconRect.anchorMax = new Vector2(0, 0.5f);
            iconRect.pivot = new Vector2(0, 0.5f);
            iconRect.anchoredPosition = new Vector2(10, 0);
            iconRect.sizeDelta = new Vector2(30, 30);

            Image iconImage = iconGO.AddComponent<Image>();
            iconImage.color = new Color(1f, 0.84f, 0f); // Gold color

            // Counter text
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(transform);

            RectTransform textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.offsetMin = new Vector2(50, 0);
            textRect.offsetMax = new Vector2(-10, 0);

            counterText = textGO.AddComponent<Text>();
            counterText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            counterText.text = "Gold: 0";
            counterText.fontSize = 24;
            counterText.alignment = TextAnchor.MiddleLeft;
            counterText.color = Color.white;

            // Subscribe to gold collection events
            GoldStore.OnGoldCollected += OnGoldCollected;

            UpdateDisplay(0);
        }

        private void OnDestroy()
        {
            GoldStore.OnGoldCollected -= OnGoldCollected;
        }

        private void OnGoldCollected(int totalGold)
        {
            UpdateDisplay(totalGold);
        }

        private void UpdateDisplay(int amount)
        {
            if (counterText != null)
            {
                counterText.text = $"Gold: {amount}";
            }
        }
    }
}
