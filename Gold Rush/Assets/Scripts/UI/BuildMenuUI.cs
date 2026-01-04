using UnityEngine;
using UnityEngine.UI;
using GoldRush.Building;

namespace GoldRush.UI
{
    public class BuildMenuUI : MonoBehaviour
    {
        private RectTransform rectTransform;
        private bool isOpen;
        private Button[] buildButtons;

        public void Initialize()
        {
            rectTransform = gameObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = new Vector2(0, -10);
            rectTransform.sizeDelta = new Vector2(1120, 80);

            // Background
            Image bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

            // Create buttons
            CreateButtons();

            // Start closed
            Close();
        }

        private void CreateButtons()
        {
            string[] names = { "Wall", "Belt", "Filter", "Lift", "Blower", "Big Crush", "Sm Crush", "Grinder", "Shaker", "Smelter", "Gold Store" };
            BuildType[] types = {
                BuildType.Wall, BuildType.Belt, BuildType.FilterBelt, BuildType.Lift, BuildType.Blower,
                BuildType.BigCrusher, BuildType.SmallCrusher, BuildType.Grinder, BuildType.Shaker, BuildType.Smelter, BuildType.GoldStore
            };
            Color[] colors = {
                new Color(0.3f, 0.3f, 0.3f),      // Wall
                new Color(0.25f, 0.25f, 0.25f),   // Belt
                new Color(0.3f, 0.3f, 0.4f),      // Filter (darker blue-grey)
                new Color(0.25f, 0.25f, 0.25f),   // Lift
                new Color(0.4f, 0.7f, 1f),        // Blower (light blue)
                new Color(0.35f, 0.35f, 0.4f),    // Big Crusher (dark grey-blue)
                new Color(0.4f, 0.35f, 0.35f),    // Small Crusher (reddish grey)
                new Color(0.6f, 0.55f, 0.5f),     // Grinder (light brown metal)
                new Color(1f, 0.5f, 0f),          // Shaker
                new Color(0.5f, 0.25f, 0.15f),    // Smelter (brick red)
                new Color(1f, 0.84f, 0f)          // Gold Store
            };

            buildButtons = new Button[names.Length];
            float buttonWidth = 90f;
            float spacing = 10f;
            float startX = -(buttonWidth * names.Length + spacing * (names.Length - 1)) / 2f + buttonWidth / 2f;

            for (int i = 0; i < names.Length; i++)
            {
                GameObject buttonGO = new GameObject(names[i] + "Button");
                buttonGO.transform.SetParent(transform);

                RectTransform buttonRect = buttonGO.AddComponent<RectTransform>();
                buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
                buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
                buttonRect.pivot = new Vector2(0.5f, 0.5f);
                buttonRect.anchoredPosition = new Vector2(startX + i * (buttonWidth + spacing), 0);
                buttonRect.sizeDelta = new Vector2(buttonWidth, 60);

                Image buttonImage = buttonGO.AddComponent<Image>();
                buttonImage.color = colors[i];

                Button button = buttonGO.AddComponent<Button>();
                buildButtons[i] = button;

                // Capture index for closure
                int index = i;
                BuildType type = types[i];
                button.onClick.AddListener(() => OnButtonClicked(type));

                // Button text
                GameObject textGO = new GameObject("Text");
                textGO.transform.SetParent(buttonGO.transform);

                RectTransform textRect = textGO.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;
                textRect.anchoredPosition = Vector2.zero;

                Text text = textGO.AddComponent<Text>();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.text = names[i];
                text.fontSize = 14;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = Color.white;
            }
        }

        private void OnButtonClicked(BuildType type)
        {
            if (BuildSystem.Instance != null)
            {
                BuildSystem.Instance.SetBuildType(type);
            }
            Close();
        }

        private void Update()
        {
            // Toggle with Tab
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (isOpen)
                {
                    Close();
                }
                else
                {
                    Open();
                }
            }
        }

        public void Open()
        {
            isOpen = true;
            gameObject.SetActive(true);

            // Cancel any current build mode when opening menu
            if (BuildSystem.Instance != null)
            {
                BuildSystem.Instance.SetBuildType(BuildType.None);
            }
        }

        public void Close()
        {
            isOpen = false;
            gameObject.SetActive(false);
        }

        public bool IsOpen => isOpen;
    }
}
