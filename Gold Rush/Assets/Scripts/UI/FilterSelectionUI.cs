using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using GoldRush.Simulation;

namespace GoldRush.UI
{
    public class FilterSelectionUI : MonoBehaviour
    {
        public static FilterSelectionUI Instance { get; private set; }

        private GameObject panel;
        private RectTransform panelRect;
        private bool isOpen;

        // Currently selected materials to block
        public HashSet<MaterialType> SelectedMaterials { get; private set; } = new HashSet<MaterialType>();

        // Materials that can be filtered
        private static readonly (MaterialType type, string name, Color color)[] FilterableMaterials = {
            (MaterialType.Gold, "Gold", new Color(1f, 0.84f, 0f)),
            (MaterialType.Concentrate, "Concentrate", new Color(0.6f, 0.5f, 0.3f)),
            (MaterialType.Coal, "Coal", new Color(0.15f, 0.15f, 0.15f)),
            (MaterialType.Ore, "Ore", new Color(0.72f, 0.53f, 0.04f)),
            (MaterialType.Boulder, "Boulder", new Color(0.38f, 0.38f, 0.38f)),
            (MaterialType.Rock, "Rock", new Color(0.5f, 0.5f, 0.5f)),
            (MaterialType.Gravel, "Gravel", new Color(0.63f, 0.63f, 0.63f)),
            (MaterialType.Sand, "Sand", new Color(0.76f, 0.7f, 0.5f)),
            (MaterialType.WetSand, "Wet Sand", new Color(0.6f, 0.55f, 0.4f)),
            (MaterialType.Slag, "Slag", new Color(0.3f, 0.25f, 0.2f))
        };

        private Dictionary<MaterialType, Toggle> toggles = new Dictionary<MaterialType, Toggle>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void Initialize()
        {
            CreatePanel();
            panel.SetActive(false);
        }

        private void CreatePanel()
        {
            // Create panel
            panel = new GameObject("FilterSelectionPanel");
            panel.transform.SetParent(transform);

            panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(250, 340);

            // Background
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

            // Title
            GameObject titleGO = new GameObject("Title");
            titleGO.transform.SetParent(panel.transform);
            RectTransform titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -5);
            titleRect.sizeDelta = new Vector2(-20, 30);

            Text titleText = titleGO.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.text = "Select Materials to BLOCK";
            titleText.fontSize = 16;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;

            // Create checkboxes
            float startY = -40;
            float itemHeight = 28;

            for (int i = 0; i < FilterableMaterials.Length; i++)
            {
                var mat = FilterableMaterials[i];
                CreateCheckbox(mat.type, mat.name, mat.color, startY - i * itemHeight);
            }

            // Close button / instructions
            GameObject instructGO = new GameObject("Instructions");
            instructGO.transform.SetParent(panel.transform);
            RectTransform instructRect = instructGO.AddComponent<RectTransform>();
            instructRect.anchorMin = new Vector2(0, 0);
            instructRect.anchorMax = new Vector2(1, 0);
            instructRect.pivot = new Vector2(0.5f, 0);
            instructRect.anchoredPosition = new Vector2(0, 5);
            instructRect.sizeDelta = new Vector2(-20, 25);

            Text instructText = instructGO.AddComponent<Text>();
            instructText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            instructText.text = "[F to close] [Click to place]";
            instructText.fontSize = 12;
            instructText.fontStyle = FontStyle.Italic;
            instructText.alignment = TextAnchor.MiddleCenter;
            instructText.color = new Color(0.7f, 0.7f, 0.7f);
        }

        private void CreateCheckbox(MaterialType type, string name, Color color, float yPos)
        {
            GameObject checkboxGO = new GameObject($"Checkbox_{name}");
            checkboxGO.transform.SetParent(panel.transform);

            RectTransform checkRect = checkboxGO.AddComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0, 1);
            checkRect.anchorMax = new Vector2(1, 1);
            checkRect.pivot = new Vector2(0, 1);
            checkRect.anchoredPosition = new Vector2(15, yPos);
            checkRect.sizeDelta = new Vector2(-30, 25);

            // Toggle component
            Toggle toggle = checkboxGO.AddComponent<Toggle>();

            // Checkbox background
            GameObject bgGO = new GameObject("Background");
            bgGO.transform.SetParent(checkboxGO.transform);
            RectTransform bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.5f);
            bgRect.anchorMax = new Vector2(0, 0.5f);
            bgRect.pivot = new Vector2(0, 0.5f);
            bgRect.anchoredPosition = new Vector2(0, 0);
            bgRect.sizeDelta = new Vector2(20, 20);
            Image bgImage = bgGO.AddComponent<Image>();
            bgImage.color = new Color(0.3f, 0.3f, 0.3f);

            // Checkmark
            GameObject checkmarkGO = new GameObject("Checkmark");
            checkmarkGO.transform.SetParent(bgGO.transform);
            RectTransform checkmarkRect = checkmarkGO.AddComponent<RectTransform>();
            checkmarkRect.anchorMin = Vector2.zero;
            checkmarkRect.anchorMax = Vector2.one;
            checkmarkRect.sizeDelta = new Vector2(-4, -4);
            checkmarkRect.anchoredPosition = Vector2.zero;
            Image checkmarkImage = checkmarkGO.AddComponent<Image>();
            checkmarkImage.color = new Color(0.2f, 0.8f, 0.2f);

            toggle.targetGraphic = bgImage;
            toggle.graphic = checkmarkImage;
            toggle.isOn = false;

            // Color swatch
            GameObject swatchGO = new GameObject("Swatch");
            swatchGO.transform.SetParent(checkboxGO.transform);
            RectTransform swatchRect = swatchGO.AddComponent<RectTransform>();
            swatchRect.anchorMin = new Vector2(0, 0.5f);
            swatchRect.anchorMax = new Vector2(0, 0.5f);
            swatchRect.pivot = new Vector2(0, 0.5f);
            swatchRect.anchoredPosition = new Vector2(28, 0);
            swatchRect.sizeDelta = new Vector2(16, 16);
            Image swatchImage = swatchGO.AddComponent<Image>();
            swatchImage.color = color;

            // Label
            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(checkboxGO.transform);
            RectTransform labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.pivot = new Vector2(0, 0.5f);
            labelRect.anchoredPosition = new Vector2(50, 0);
            labelRect.sizeDelta = new Vector2(-50, 0);

            Text labelText = labelGO.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.text = name;
            labelText.fontSize = 14;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.color = Color.white;

            // Store reference and add listener
            toggles[type] = toggle;
            MaterialType capturedType = type;
            toggle.onValueChanged.AddListener((isOn) => OnToggleChanged(capturedType, isOn));
        }

        private void OnToggleChanged(MaterialType type, bool isOn)
        {
            if (isOn)
                SelectedMaterials.Add(type);
            else
                SelectedMaterials.Remove(type);
        }

        public void Open()
        {
            isOpen = true;
            panel.SetActive(true);
        }

        public void Close()
        {
            isOpen = false;
            panel.SetActive(false);
        }

        public void Toggle()
        {
            if (isOpen)
                Close();
            else
                Open();
        }

        public bool IsOpen => isOpen;

        public void ClearSelection()
        {
            SelectedMaterials.Clear();
            foreach (var toggle in toggles.Values)
            {
                toggle.isOn = false;
            }
        }
    }
}
