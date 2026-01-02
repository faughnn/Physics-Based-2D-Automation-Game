using UnityEngine;
using System.Collections.Generic;

namespace GoldRush.Core
{
    public static class SpriteGenerator
    {
        private static Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
        private static PhysicsMaterial2D particleMaterial;

        public static void Initialize()
        {
            // Create physics material for particles
            particleMaterial = new PhysicsMaterial2D("ParticleMaterial");
            particleMaterial.bounciness = GameSettings.ParticleBounciness;
            particleMaterial.friction = GameSettings.ParticleFriction;

            // Pre-generate all sprites
            GenerateAllSprites();

            Debug.Log("SpriteGenerator: All sprites generated");
        }

        private static void GenerateAllSprites()
        {
            // Generate rectangle sprites
            CreateRectSprite("Player", GameSettings.PlayerWidth, GameSettings.PlayerHeight, GameSettings.PlayerColor);
            CreateRectSprite("Terrain", GameSettings.GridSize, GameSettings.GridSize, GameSettings.TerrainColor);
            CreateRectSprite("Wall", GameSettings.GridSize, GameSettings.GridSize, GameSettings.WallColor);
            CreateRectSprite("Belt", GameSettings.GridSize, 8, GameSettings.BeltColor);
            CreateRectSprite("Lift", GameSettings.GridSize, GameSettings.GridSize, GameSettings.LiftColor);
            CreateRectSprite("Shaker", GameSettings.GridSize, 16, GameSettings.ShakerColor);
            CreateRectSprite("GoldStore", 64, 32, GameSettings.GoldStoreColor);

            // Generate circle sprites for particles
            CreateCircleSprite("Sand", (int)GameSettings.ParticleRadius * 2, GameSettings.SandColor);
            CreateCircleSprite("WetSand", (int)GameSettings.ParticleRadius * 2, GameSettings.WetSandColor);
            CreateCircleSprite("Water", (int)GameSettings.ParticleRadius * 2, GameSettings.WaterColor);
            CreateCircleSprite("Gold", (int)GameSettings.ParticleRadius * 2, GameSettings.GoldColor);
            CreateCircleSprite("Slag", (int)GameSettings.ParticleRadius * 2, GameSettings.SlagColor);

            // Generate water reservoir visual
            CreateRectSprite("WaterReservoir", GameSettings.GridSize * GameSettings.WorldWidthCells,
                            GameSettings.GridSize * GameSettings.WaterReservoirHeight,
                            new Color(GameSettings.WaterColor.r, GameSettings.WaterColor.g, GameSettings.WaterColor.b, 0.3f));
        }

        private static void CreateRectSprite(string name, int width, int height, Color color)
        {
            Texture2D texture = new Texture2D(width, height);
            texture.filterMode = FilterMode.Point;

            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply();

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, width, height),
                                         new Vector2(0.5f, 0.5f), GameSettings.PixelsPerUnit);
            spriteCache[name] = sprite;
        }

        private static void CreateCircleSprite(string name, int diameter, Color color)
        {
            // Ensure minimum size
            diameter = Mathf.Max(diameter, 4);

            Texture2D texture = new Texture2D(diameter, diameter);
            texture.filterMode = FilterMode.Point;

            Color[] pixels = new Color[diameter * diameter];
            float radius = diameter / 2f;
            Vector2 center = new Vector2(radius, radius);

            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    Vector2 pos = new Vector2(x + 0.5f, y + 0.5f);
                    float dist = Vector2.Distance(pos, center);
                    if (dist <= radius)
                    {
                        pixels[y * diameter + x] = color;
                    }
                    else
                    {
                        pixels[y * diameter + x] = Color.clear;
                    }
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, diameter, diameter),
                                         new Vector2(0.5f, 0.5f), GameSettings.PixelsPerUnit);
            spriteCache[name] = sprite;
        }

        public static Sprite GetSprite(string name)
        {
            if (spriteCache.TryGetValue(name, out Sprite sprite))
            {
                return sprite;
            }
            Debug.LogWarning($"SpriteGenerator: Sprite '{name}' not found");
            return null;
        }

        public static PhysicsMaterial2D GetParticleMaterial()
        {
            return particleMaterial;
        }

        // Create a semi-circle sprite for dig preview
        public static Sprite CreateSemiCircleSprite(int radius, Color color)
        {
            int diameter = radius * 2;
            Texture2D texture = new Texture2D(diameter, diameter);
            texture.filterMode = FilterMode.Point;

            Color[] pixels = new Color[diameter * diameter];
            Vector2 center = new Vector2(radius, radius);

            // Semi-circle: only fill pixels in the "forward" half (right side)
            // The flat edge is on the left, curved edge on the right
            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    Vector2 pos = new Vector2(x + 0.5f, y + 0.5f);
                    Vector2 offset = pos - center;
                    float dist = offset.magnitude;

                    // Check if in circle and in the right half (x >= center means forward half)
                    if (dist <= radius && offset.x >= 0)
                    {
                        pixels[y * diameter + x] = color;
                    }
                    else
                    {
                        pixels[y * diameter + x] = Color.clear;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            // Pivot at center so rotation works correctly
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, diameter, diameter),
                                         new Vector2(0.5f, 0.5f), GameSettings.PixelsPerUnit);
            return sprite;
        }

        // Create a directional arrow sprite for belts/lifts
        public static Sprite CreateArrowSprite(int width, int height, Color color, bool horizontal, bool positive)
        {
            string key = $"Arrow_{width}x{height}_{(horizontal ? "H" : "V")}_{(positive ? "P" : "N")}";

            if (spriteCache.TryGetValue(key, out Sprite cached))
            {
                return cached;
            }

            Texture2D texture = new Texture2D(width, height);
            texture.filterMode = FilterMode.Point;

            Color[] pixels = new Color[width * height];

            // Fill with base color
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            // Draw arrow
            Color arrowColor = Color.white;
            int arrowSize = Mathf.Min(width, height) / 3;

            if (horizontal)
            {
                // Horizontal arrow
                int centerY = height / 2;
                int startX = positive ? width / 4 : width * 3 / 4;
                int endX = positive ? width * 3 / 4 : width / 4;
                int dir = positive ? 1 : -1;

                // Arrow line
                for (int x = startX; x != endX; x += dir)
                {
                    if (x >= 0 && x < width)
                    {
                        pixels[centerY * width + x] = arrowColor;
                    }
                }

                // Arrow head
                int tipX = endX - dir;
                for (int i = 1; i <= arrowSize; i++)
                {
                    int headX = tipX - dir * i;
                    if (headX >= 0 && headX < width)
                    {
                        if (centerY - i >= 0) pixels[(centerY - i) * width + headX] = arrowColor;
                        if (centerY + i < height) pixels[(centerY + i) * width + headX] = arrowColor;
                    }
                }
            }
            else
            {
                // Vertical arrow
                int centerX = width / 2;
                int startY = positive ? height / 4 : height * 3 / 4;
                int endY = positive ? height * 3 / 4 : height / 4;
                int dir = positive ? 1 : -1;

                // Arrow line
                for (int y = startY; y != endY; y += dir)
                {
                    if (y >= 0 && y < height)
                    {
                        pixels[y * width + centerX] = arrowColor;
                    }
                }

                // Arrow head
                int tipY = endY - dir;
                for (int i = 1; i <= arrowSize; i++)
                {
                    int headY = tipY - dir * i;
                    if (headY >= 0 && headY < height)
                    {
                        if (centerX - i >= 0) pixels[headY * width + centerX - i] = arrowColor;
                        if (centerX + i < width) pixels[headY * width + centerX + i] = arrowColor;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, width, height),
                                         new Vector2(0.5f, 0.5f), GameSettings.PixelsPerUnit);
            spriteCache[key] = sprite;
            return sprite;
        }

        // Create a hollow lift sprite with thin walls and arrow indicator
        public static Sprite CreateHollowLiftSprite(int size, Color color, bool movesUp)
        {
            string key = $"HollowLift_{size}_{(movesUp ? "U" : "D")}";

            if (spriteCache.TryGetValue(key, out Sprite cached))
            {
                return cached;
            }

            Texture2D texture = new Texture2D(size, size);
            texture.filterMode = FilterMode.Point;

            Color[] pixels = new Color[size * size];

            // Start with transparent
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }

            // Draw left and right walls (1 pixel thick)
            for (int y = 0; y < size; y++)
            {
                pixels[y * size + 0] = color;           // Left wall
                pixels[y * size + (size - 1)] = color;  // Right wall
            }

            // Draw larger arrow in center (about 1/3 of size)
            Color arrowColor = Color.white;
            int centerX = size / 2;
            int arrowHeight = size / 3;
            int tipY = movesUp ? size - 4 : 3;
            int arrowDir = movesUp ? -1 : 1;

            // Arrow shaft (vertical line)
            for (int i = 0; i < arrowHeight; i++)
            {
                int y = tipY + arrowDir * i;
                if (y >= 0 && y < size)
                {
                    pixels[y * size + centerX] = arrowColor;
                }
            }

            // Arrow head (triangle)
            for (int i = 1; i <= 4; i++)
            {
                int headY = tipY + arrowDir * i;
                if (headY >= 0 && headY < size)
                {
                    if (centerX - i >= 1) pixels[headY * size + centerX - i] = arrowColor;
                    if (centerX + i < size - 1) pixels[headY * size + centerX + i] = arrowColor;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size),
                                         new Vector2(0.5f, 0.5f), GameSettings.PixelsPerUnit);
            spriteCache[key] = sprite;
            return sprite;
        }

        // Create a hollow blower sprite (horizontal version of lift) with thin walls and arrow indicator
        public static Sprite CreateHollowBlowerSprite(int size, Color color, bool blowsRight)
        {
            string key = $"HollowBlower_{size}_{(blowsRight ? "R" : "L")}";

            if (spriteCache.TryGetValue(key, out Sprite cached))
            {
                return cached;
            }

            Texture2D texture = new Texture2D(size, size);
            texture.filterMode = FilterMode.Point;

            Color[] pixels = new Color[size * size];

            // Start with transparent
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }

            // Draw top and bottom walls (1 pixel thick)
            for (int x = 0; x < size; x++)
            {
                pixels[0 * size + x] = color;              // Bottom wall
                pixels[(size - 1) * size + x] = color;     // Top wall
            }

            // Draw larger horizontal arrow in center (about 1/3 of size)
            Color arrowColor = Color.white;
            int centerY = size / 2;
            int arrowWidth = size / 3;
            int tipX = blowsRight ? size - 4 : 3;
            int arrowDir = blowsRight ? -1 : 1;

            // Arrow shaft (horizontal line)
            for (int i = 0; i < arrowWidth; i++)
            {
                int x = tipX + arrowDir * i;
                if (x >= 0 && x < size)
                {
                    pixels[centerY * size + x] = arrowColor;
                }
            }

            // Arrow head (triangle)
            for (int i = 1; i <= 4; i++)
            {
                int headX = tipX + arrowDir * i;
                if (headX >= 0 && headX < size)
                {
                    if (centerY - i >= 1) pixels[(centerY - i) * size + headX] = arrowColor;
                    if (centerY + i < size - 1) pixels[(centerY + i) * size + headX] = arrowColor;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size),
                                         new Vector2(0.5f, 0.5f), GameSettings.PixelsPerUnit);
            spriteCache[key] = sprite;
            return sprite;
        }

        // Create a solid wall sprite (no arrow, just a solid block)
        public static Sprite CreateSolidWallSprite(int size, Color color)
        {
            string key = $"SolidWall_{size}";

            if (spriteCache.TryGetValue(key, out Sprite cached))
            {
                return cached;
            }

            Texture2D texture = new Texture2D(size, size);
            texture.filterMode = FilterMode.Point;

            Color[] pixels = new Color[size * size];

            // Fill with solid color
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            // Add a subtle border (slightly darker)
            Color borderColor = color * 0.7f;
            borderColor.a = 1f;
            for (int x = 0; x < size; x++)
            {
                pixels[0 * size + x] = borderColor;              // Bottom
                pixels[(size - 1) * size + x] = borderColor;     // Top
            }
            for (int y = 0; y < size; y++)
            {
                pixels[y * size + 0] = borderColor;              // Left
                pixels[y * size + (size - 1)] = borderColor;     // Right
            }

            texture.SetPixels(pixels);
            texture.Apply();

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size),
                                         new Vector2(0.5f, 0.5f), GameSettings.PixelsPerUnit);
            spriteCache[key] = sprite;
            return sprite;
        }
    }
}
