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
    }
}
