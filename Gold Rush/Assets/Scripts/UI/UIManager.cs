using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using GoldRush.Core;

namespace GoldRush.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        public Canvas MainCanvas { get; private set; }
        public BuildMenuUI BuildMenu { get; private set; }
        public GoldCounterUI GoldCounter { get; private set; }
        public DebugGridOverlay GridOverlay { get; private set; }

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
            CreateEventSystem();
            CreateCanvas();
            CreateBuildMenu();
            CreateGoldCounter();
            CreateInstructions();
            CreateGridOverlay();
        }

        private void CreateEventSystem()
        {
            // Check if EventSystem already exists
            if (FindAnyObjectByType<EventSystem>() != null)
                return;

            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.transform.SetParent(transform);

            eventSystemGO.AddComponent<EventSystem>();
            eventSystemGO.AddComponent<StandaloneInputModule>();

        }

        private void CreateCanvas()
        {
            GameObject canvasGO = new GameObject("UICanvas");
            canvasGO.transform.SetParent(transform);

            MainCanvas = canvasGO.AddComponent<Canvas>();
            MainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            MainCanvas.sortingOrder = 100;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();
        }

        private void CreateBuildMenu()
        {
            GameObject menuGO = new GameObject("BuildMenu");
            menuGO.transform.SetParent(MainCanvas.transform);

            BuildMenu = menuGO.AddComponent<BuildMenuUI>();
            BuildMenu.Initialize();
        }

        private void CreateGoldCounter()
        {
            GameObject counterGO = new GameObject("GoldCounter");
            counterGO.transform.SetParent(MainCanvas.transform);

            GoldCounter = counterGO.AddComponent<GoldCounterUI>();
            GoldCounter.Initialize();
        }

        private void CreateInstructions()
        {
            GameObject instructionsGO = new GameObject("Instructions");
            instructionsGO.transform.SetParent(MainCanvas.transform);

            RectTransform rect = instructionsGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(0, 0);
            rect.pivot = new Vector2(0, 0);
            rect.anchoredPosition = new Vector2(10, 10);
            rect.sizeDelta = new Vector2(400, 150);

            Text text = instructionsGO.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14;
            text.color = Color.white;
            text.text = "Controls:\n" +
                        "A/D - Move  |  Space/W - Jump\n" +
                        "Left Click - Dig / Place\n" +
                        "Right Click - Delete\n" +
                        "Tab - Build Menu\n" +
                        "Q/E - Change Direction\n" +
                        "Esc - Cancel Build";

            // Add shadow for readability
            Shadow shadow = instructionsGO.AddComponent<Shadow>();
            shadow.effectColor = Color.black;
            shadow.effectDistance = new Vector2(1, -1);
        }

        private void CreateGridOverlay()
        {
            GameObject overlayGO = new GameObject("DebugGridOverlay");
            // Don't parent to canvas - this is a world-space overlay
            GridOverlay = overlayGO.AddComponent<DebugGridOverlay>();
            GridOverlay.Initialize();
        }
    }
}
