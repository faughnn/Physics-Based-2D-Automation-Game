using UnityEngine;

namespace FallingSand
{
    public class MetricsOverlay : MonoBehaviour
    {
        [SerializeField] private SandboxController sandbox;

        private float deltaTime;
        private float updateInterval = 0.5f;
        private float lastUpdateTime;

        private int cachedFps;
        private int cachedActiveChunks;
        private int cachedActiveCells;

        private GUIStyle boxStyle;
        private GUIStyle labelStyle;

        private void Start()
        {
            if (sandbox == null)
            {
                sandbox = FindFirstObjectByType<SandboxController>();
            }
        }

        private void Update()
        {
            // Smooth delta time for FPS calculation
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

            // Update cached values periodically to avoid per-frame counting
            if (Time.time - lastUpdateTime > updateInterval)
            {
                lastUpdateTime = Time.time;
                cachedFps = Mathf.RoundToInt(1f / deltaTime);

                if (sandbox != null && sandbox.World != null)
                {
                    cachedActiveChunks = sandbox.World.CountActiveChunks();
                    cachedActiveCells = sandbox.World.CountActiveCells();
                }
            }
        }

        private void OnGUI()
        {
            // Initialize styles once
            if (boxStyle == null)
            {
                boxStyle = new GUIStyle(GUI.skin.box);
                boxStyle.normal.background = MakeTexture(2, 2, new Color(0, 0, 0, 0.7f));

                labelStyle = new GUIStyle(GUI.skin.label);
                labelStyle.fontSize = 14;
                labelStyle.normal.textColor = Color.white;
            }

            // Draw metrics box in top-left corner
            float padding = 10;
            float width = 200;
            float lineHeight = 20;
            float height = lineHeight * 5 + padding * 2;

            Rect boxRect = new Rect(padding, padding, width, height);
            GUI.Box(boxRect, GUIContent.none, boxStyle);

            float y = padding + 5;
            float x = padding + 10;

            // FPS
            Color fpsColor = cachedFps >= 55 ? Color.green : (cachedFps >= 30 ? Color.yellow : Color.red);
            GUI.color = fpsColor;
            GUI.Label(new Rect(x, y, width, lineHeight), $"FPS: {cachedFps}", labelStyle);
            y += lineHeight;

            // Reset color for other labels
            GUI.color = Color.white;

            // Active chunks
            GUI.Label(new Rect(x, y, width, lineHeight), $"Active Chunks: {cachedActiveChunks}", labelStyle);
            y += lineHeight;

            // Active cells
            GUI.Label(new Rect(x, y, width, lineHeight), $"Active Cells: {cachedActiveCells:N0}", labelStyle);
            y += lineHeight;

            // Current material
            if (sandbox != null)
            {
                GUI.Label(new Rect(x, y, width, lineHeight), $"Material: {sandbox.CurrentMaterialName}", labelStyle);
                y += lineHeight;

                GUI.Label(new Rect(x, y, width, lineHeight), "Keys: 1-5 | LMB/RMB", labelStyle);
            }
        }

        private Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            Texture2D texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
