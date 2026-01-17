using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using FallingSand.Graphics;
using FallingSand.Graphics.Effects;

namespace FallingSand.UI
{
    /// <summary>
    /// Canvas-based settings menu for graphics effects.
    /// Automatically generates toggles for all registered effects.
    /// Toggle with Escape key.
    /// </summary>
    public class SettingsMenu : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform panelTransform;
        [SerializeField] private RectTransform toggleContainer;

        private GraphicsManager graphicsManager;
        private Keyboard keyboard;
        private bool isOpen;

        // UI layout constants
        private const float PanelWidth = 300f;
        private const float PanelPadding = 20f;
        private const float ToggleHeight = 35f;
        private const float SliderHeight = 25f;
        private const float Spacing = 10f;

        private void Start()
        {
            keyboard = Keyboard.current;
            graphicsManager = GraphicsManager.Instance;

            if (graphicsManager == null)
            {
                Debug.LogWarning("[SettingsMenu] GraphicsManager not found");
                return;
            }

            CreateUI();
            SetMenuOpen(false);
        }

        private void Update()
        {
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                SetMenuOpen(!isOpen);
            }
        }

        private void SetMenuOpen(bool open)
        {
            isOpen = open;
            if (canvas != null)
                canvas.enabled = open;
        }

        private void CreateUI()
        {
            // Create Canvas
            GameObject canvasObj = new GameObject("SettingsCanvas");
            canvasObj.transform.SetParent(transform);
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();

            // Create Panel
            GameObject panelObj = new GameObject("SettingsPanel");
            panelObj.transform.SetParent(canvasObj.transform, false);
            panelTransform = panelObj.AddComponent<RectTransform>();

            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

            // Center the panel
            panelTransform.anchorMin = new Vector2(0.5f, 0.5f);
            panelTransform.anchorMax = new Vector2(0.5f, 0.5f);
            panelTransform.pivot = new Vector2(0.5f, 0.5f);
            panelTransform.sizeDelta = new Vector2(PanelWidth, 400f); // Will be adjusted

            // Add vertical layout
            VerticalLayoutGroup layout = panelObj.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset((int)PanelPadding, (int)PanelPadding, (int)PanelPadding, (int)PanelPadding);
            layout.spacing = Spacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = panelObj.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Create Header
            CreateLabel(panelObj.transform, "Graphics Settings", 24, FontStyle.Bold, Color.white);
            CreateSpacer(panelObj.transform, 10f);

            // Create toggles for each effect
            foreach (var effect in graphicsManager.Effects)
            {
                CreateEffectToggle(panelObj.transform, effect);

                // Add slider for glow intensity
                if (effect is GlowEffect glowEffect)
                {
                    CreateGlowSlider(panelObj.transform, glowEffect);
                }
            }

            CreateSpacer(panelObj.transform, 10f);

            // Create Close button
            CreateButton(panelObj.transform, "Close", () => SetMenuOpen(false));

            // Hint text
            CreateSpacer(panelObj.transform, 5f);
            CreateLabel(panelObj.transform, "Press ESC to toggle menu", 12, FontStyle.Italic, new Color(0.6f, 0.6f, 0.6f));

            Debug.Log("[SettingsMenu] UI created");
        }

        private void CreateLabel(Transform parent, string text, int fontSize, FontStyle style, Color color)
        {
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(parent, false);

            RectTransform rect = labelObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(PanelWidth - PanelPadding * 2, fontSize + 10);

            Text label = labelObj.AddComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = color;
            label.alignment = TextAnchor.MiddleCenter;
        }

        private void CreateSpacer(Transform parent, float height)
        {
            GameObject spacerObj = new GameObject("Spacer");
            spacerObj.transform.SetParent(parent, false);

            RectTransform rect = spacerObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, height);

            LayoutElement le = spacerObj.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
        }

        private void CreateEffectToggle(Transform parent, IGraphicsEffect effect)
        {
            GameObject toggleObj = new GameObject($"Toggle_{effect.EffectName}");
            toggleObj.transform.SetParent(parent, false);

            RectTransform rect = toggleObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(PanelWidth - PanelPadding * 2, ToggleHeight);

            HorizontalLayoutGroup hLayout = toggleObj.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 10f;
            hLayout.childAlignment = TextAnchor.MiddleLeft;
            hLayout.childControlWidth = false;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = false;

            // Toggle background/checkbox
            GameObject checkboxObj = new GameObject("Checkbox");
            checkboxObj.transform.SetParent(toggleObj.transform, false);

            RectTransform checkRect = checkboxObj.AddComponent<RectTransform>();
            checkRect.sizeDelta = new Vector2(ToggleHeight, ToggleHeight);

            Image checkBg = checkboxObj.AddComponent<Image>();
            checkBg.color = new Color(0.2f, 0.2f, 0.25f);

            Toggle toggle = checkboxObj.AddComponent<Toggle>();

            // Checkmark
            GameObject checkmarkObj = new GameObject("Checkmark");
            checkmarkObj.transform.SetParent(checkboxObj.transform, false);

            RectTransform checkmarkRect = checkmarkObj.AddComponent<RectTransform>();
            checkmarkRect.anchorMin = Vector2.zero;
            checkmarkRect.anchorMax = Vector2.one;
            checkmarkRect.offsetMin = new Vector2(5, 5);
            checkmarkRect.offsetMax = new Vector2(-5, -5);

            Image checkmark = checkmarkObj.AddComponent<Image>();
            checkmark.color = new Color(0.4f, 0.8f, 0.4f);

            toggle.graphic = checkmark;
            toggle.isOn = effect.IsEnabled;
            toggle.onValueChanged.AddListener(value => effect.IsEnabled = value);

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(toggleObj.transform, false);

            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(200, ToggleHeight);

            LayoutElement le = labelObj.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;

            Text label = labelObj.AddComponent<Text>();
            label.text = effect.EffectName;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 16;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleLeft;
        }

        private void CreateGlowSlider(Transform parent, GlowEffect glowEffect)
        {
            GameObject sliderObj = new GameObject("GlowIntensitySlider");
            sliderObj.transform.SetParent(parent, false);

            RectTransform rect = sliderObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(PanelWidth - PanelPadding * 2, SliderHeight);

            HorizontalLayoutGroup hLayout = sliderObj.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 10f;
            hLayout.childAlignment = TextAnchor.MiddleLeft;
            hLayout.childControlWidth = false;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = false;

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(sliderObj.transform, false);

            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(100, SliderHeight);

            Text label = labelObj.AddComponent<Text>();
            label.text = "  Intensity";
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 14;
            label.color = new Color(0.8f, 0.8f, 0.8f);
            label.alignment = TextAnchor.MiddleLeft;

            // Slider container
            GameObject sliderContainer = new GameObject("SliderContainer");
            sliderContainer.transform.SetParent(sliderObj.transform, false);

            RectTransform containerRect = sliderContainer.AddComponent<RectTransform>();
            containerRect.sizeDelta = new Vector2(130, SliderHeight);

            // Background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderContainer.transform, false);

            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.25f);
            bgRect.anchorMax = new Vector2(1, 0.75f);
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.25f);

            // Fill area
            GameObject fillAreaObj = new GameObject("FillArea");
            fillAreaObj.transform.SetParent(sliderContainer.transform, false);

            RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1, 0.75f);
            fillAreaRect.offsetMin = new Vector2(5, 0);
            fillAreaRect.offsetMax = new Vector2(-5, 0);

            // Fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillAreaObj.transform, false);

            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            Image fillImage = fillObj.AddComponent<Image>();
            fillImage.color = new Color(0.4f, 0.6f, 0.8f);

            // Handle slide area
            GameObject handleAreaObj = new GameObject("HandleSlideArea");
            handleAreaObj.transform.SetParent(sliderContainer.transform, false);

            RectTransform handleAreaRect = handleAreaObj.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(5, 0);
            handleAreaRect.offsetMax = new Vector2(-5, 0);

            // Handle
            GameObject handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(handleAreaObj.transform, false);

            RectTransform handleRect = handleObj.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(15, 0);

            Image handleImage = handleObj.AddComponent<Image>();
            handleImage.color = Color.white;

            // Slider component
            Slider slider = sliderContainer.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = glowEffect.Intensity;
            slider.onValueChanged.AddListener(value => glowEffect.Intensity = value);
        }

        private void CreateButton(Transform parent, string text, System.Action onClick)
        {
            GameObject buttonObj = new GameObject($"Button_{text}");
            buttonObj.transform.SetParent(parent, false);

            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(PanelWidth - PanelPadding * 2, 40);

            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.25f, 0.25f, 0.3f);

            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            button.onClick.AddListener(() => onClick?.Invoke());

            // Set up color transitions
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.25f, 0.25f, 0.3f);
            colors.highlightedColor = new Color(0.35f, 0.35f, 0.4f);
            colors.pressedColor = new Color(0.2f, 0.2f, 0.25f);
            button.colors = colors;

            // Button text
            GameObject labelObj = new GameObject("Text");
            labelObj.transform.SetParent(buttonObj.transform, false);

            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            Text label = labelObj.AddComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 18;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleCenter;
        }
    }
}
