using UnityEngine;
using GoldRush.Core;

namespace GoldRush.UI
{
    public class DebugGridOverlay : MonoBehaviour
    {
        private Texture2D gridTexture;
        private SpriteRenderer spriteRenderer;
        private bool showGrid = true;

        public void Initialize()
        {
            // Create grid texture covering the world
            int worldPixelWidth = GameSettings.WorldWidthCells * GameSettings.GridSize;
            int worldPixelHeight = GameSettings.WorldHeightCells * GameSettings.GridSize;

            gridTexture = new Texture2D(worldPixelWidth, worldPixelHeight);
            gridTexture.filterMode = FilterMode.Point;

            Color transparent = new Color(0, 0, 0, 0);
            Color gridLine = new Color(1f, 1f, 1f, 0.15f);  // Light white grid
            Color infraLine = new Color(0f, 1f, 1f, 0.25f);  // Cyan for infra grid

            Color[] pixels = new Color[worldPixelWidth * worldPixelHeight];

            // Fill transparent
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = transparent;

            // Draw main grid lines (32x32 pixel cells)
            for (int y = 0; y < worldPixelHeight; y++)
            {
                for (int x = 0; x < worldPixelWidth; x++)
                {
                    bool isMainGridLine = (x % GameSettings.GridSize == 0) || (y % GameSettings.GridSize == 0);
                    bool isInfraGridLine = (x % GameSettings.InfraGridSize == 0) || (y % GameSettings.InfraGridSize == 0);

                    if (isMainGridLine)
                    {
                        pixels[y * worldPixelWidth + x] = gridLine;
                    }
                    else if (isInfraGridLine)
                    {
                        pixels[y * worldPixelWidth + x] = infraLine;
                    }
                }
            }

            gridTexture.SetPixels(pixels);
            gridTexture.Apply();

            // Create sprite - centered pivot like simulation renderer
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = Sprite.Create(
                gridTexture,
                new Rect(0, 0, worldPixelWidth, worldPixelHeight),
                new Vector2(0.5f, 0.5f),  // Centered pivot
                GameSettings.PixelsPerUnit
            );
            spriteRenderer.sortingOrder = 100;  // On top of everything

            // Position at world origin (centered like simulation)
            transform.position = new Vector3(0, 0, 0);

            Debug.Log("DebugGridOverlay: Initialized");
        }

        private void Update()
        {
            // Toggle with G key
            if (Input.GetKeyDown(KeyCode.G))
            {
                showGrid = !showGrid;
                spriteRenderer.enabled = showGrid;
                Debug.Log($"Grid overlay: {(showGrid ? "ON" : "OFF")}");
            }
        }
    }
}
