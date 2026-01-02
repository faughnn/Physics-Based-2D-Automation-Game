using UnityEngine;

namespace GoldRush.Simulation
{
    public class SimulationRenderer : MonoBehaviour
    {
        private SimulationGrid grid;
        private Texture2D texture;
        private SpriteRenderer spriteRenderer;
        private Color32[] pixels;

        // Scale factor: how many screen pixels per simulation cell
        private int cellPixelSize;

        public void Initialize(SimulationGrid simulationGrid, int pixelsPerCell)
        {
            grid = simulationGrid;
            cellPixelSize = pixelsPerCell;

            // Create texture matching grid size * cell pixel size
            int texWidth = grid.Width * cellPixelSize;
            int texHeight = grid.Height * cellPixelSize;

            texture = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point; // Pixel-perfect rendering
            texture.wrapMode = TextureWrapMode.Clamp;

            pixels = new Color32[texWidth * texHeight];

            // Create sprite renderer to display the texture
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            UpdateSprite();

            // Position the renderer to cover the world
            // World is centered at origin, so position at origin
            transform.position = Vector3.zero;

            // Set sorting order - simulation renders ABOVE infrastructure (belts=1, lifts=1)
            // so particles are visible on top of belts
            spriteRenderer.sortingOrder = 5;
        }

        // Call this each frame after grid.Update()
        public void Render()
        {
            // Update pixel buffer from grid
            for (int gy = 0; gy < grid.Height; gy++)
            {
                for (int gx = 0; gx < grid.Width; gx++)
                {
                    MaterialType type = grid.Get(gx, gy);
                    Color32 color = MaterialProperties.GetColor(type);

                    // Fill the cell pixels (cellPixelSize x cellPixelSize block)
                    // Note: texture Y is flipped (0 at bottom, Height at top)
                    // Grid Y: 0 at top, Height at bottom
                    int baseTexY = (grid.Height - 1 - gy) * cellPixelSize;
                    int baseTexX = gx * cellPixelSize;

                    for (int py = 0; py < cellPixelSize; py++)
                    {
                        for (int px = 0; px < cellPixelSize; px++)
                        {
                            int texX = baseTexX + px;
                            int texY = baseTexY + py;
                            int pixelIndex = texY * (grid.Width * cellPixelSize) + texX;
                            pixels[pixelIndex] = color;
                        }
                    }
                }
            }

            // Apply to texture
            texture.SetPixels32(pixels);
            texture.Apply();

            UpdateSprite();
        }

        private void UpdateSprite()
        {
            // Texture is 1280x800 pixels (grid.Width * cellPixelSize)
            // World is 40x25 units
            // PPU = textureWidth / worldWidth = 1280 / 40 = 32
            float pixelsPerUnit = 32f;

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit
            );

            spriteRenderer.sprite = sprite;
        }

        // Convert world position to grid position
        public Vector2Int WorldToGrid(Vector2 worldPos)
        {
            // World is centered at origin
            // Grid (0,0) is top-left
            float worldWidth = grid.Width * cellPixelSize / 32f;  // In world units
            float worldHeight = grid.Height * cellPixelSize / 32f;

            // Convert from world to normalized (0-1)
            float normX = (worldPos.x + worldWidth / 2f) / worldWidth;
            float normY = (worldHeight / 2f - worldPos.y) / worldHeight;

            int gridX = Mathf.FloorToInt(normX * grid.Width);
            int gridY = Mathf.FloorToInt(normY * grid.Height);

            return new Vector2Int(
                Mathf.Clamp(gridX, 0, grid.Width - 1),
                Mathf.Clamp(gridY, 0, grid.Height - 1)
            );
        }

        // Convert grid position to world position (center of cell)
        public Vector2 GridToWorld(int gridX, int gridY)
        {
            float worldWidth = grid.Width * cellPixelSize / 32f;
            float worldHeight = grid.Height * cellPixelSize / 32f;

            float normX = (gridX + 0.5f) / grid.Width;
            float normY = (gridY + 0.5f) / grid.Height;

            float worldX = normX * worldWidth - worldWidth / 2f;
            float worldY = worldHeight / 2f - normY * worldHeight;

            return new Vector2(worldX, worldY);
        }
    }
}
