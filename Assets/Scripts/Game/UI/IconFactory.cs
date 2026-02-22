using UnityEngine;

namespace FallingSand
{
    /// <summary>
    /// Centralized procedural sprite/icon generation.
    /// Keeps rendering concerns out of UI and controller classes.
    /// </summary>
    public static class IconFactory
    {
        private const int IconSize = 32;

        public static Sprite CreateGrabberIcon() => CreateProceduralIcon(DrawGrabberIcon);
        public static Sprite CreateShovelIcon() => CreateProceduralIcon(DrawShovelIcon);
        public static Sprite CreateBeltIcon() => CreateProceduralIcon(DrawBeltIcon);
        public static Sprite CreateLiftIcon() => CreateProceduralIcon(DrawLiftIcon);
        public static Sprite CreateWallIcon() => CreateProceduralIcon(DrawWallIcon);
        public static Sprite CreatePistonIcon() => CreateProceduralIcon(DrawPistonIcon);

        /// <summary>
        /// Creates a simple rectangular sprite filled with white.
        /// </summary>
        public static Sprite CreateRectSprite(int width, int height, float pixelsPerUnit = 1f)
        {
            Texture2D tex = new Texture2D(width, height);
            tex.filterMode = FilterMode.Point;

            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        }

        /// <summary>
        /// Creates a shovel-shaped sprite (16x64 pixels at PPU=2 = 8x32 world units).
        /// </summary>
        public static Sprite CreateShovelSprite()
        {
            int width = 16, height = 64;
            Texture2D tex = new Texture2D(width, height);
            tex.filterMode = FilterMode.Point;

            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            int bladeHeight = 20;
            int handleWidth = 4;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (y >= bladeHeight && x >= (width - handleWidth) / 2 && x < (width + handleWidth) / 2)
                        pixels[y * width + x] = Color.white;
                    else if (y < bladeHeight && x >= 2 && x < width - 2)
                        pixels[y * width + x] = Color.white;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 2f);
        }

        private static Sprite CreateProceduralIcon(System.Action<Color[]> drawFunc)
        {
            var tex = new Texture2D(IconSize, IconSize);
            tex.filterMode = FilterMode.Point;
            Color[] pixels = new Color[IconSize * IconSize];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;
            drawFunc(pixels);
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, IconSize, IconSize), new Vector2(0.5f, 0.5f), IconSize);
        }

        private static void DrawGrabberIcon(Color[] pixels)
        {
            const int s = IconSize;
            for (int y = 4; y < 16; y++)
                for (int x = 10; x < 22; x++)
                    pixels[y * s + x] = Color.white;
            for (int y = 16; y < 28; y++)
            {
                if (y < 26) pixels[y * s + 8] = pixels[y * s + 9] = Color.white;
                pixels[y * s + 12] = pixels[y * s + 13] = Color.white;
                pixels[y * s + 17] = pixels[y * s + 18] = Color.white;
                if (y < 26) pixels[y * s + 22] = pixels[y * s + 23] = Color.white;
            }
        }

        private static void DrawShovelIcon(Color[] pixels)
        {
            const int s = IconSize;
            for (int y = 14; y < 30; y++)
                for (int x = 14; x < 18; x++)
                    pixels[y * s + x] = Color.white;
            for (int y = 2; y < 14; y++)
                for (int x = 10; x < 22; x++)
                    pixels[y * s + x] = Color.white;
        }

        private static void DrawBeltIcon(Color[] pixels)
        {
            const int s = IconSize;
            for (int y = 13; y < 19; y++)
                for (int x = 4; x < 28; x++)
                    pixels[y * s + x] = Color.white;
            for (int i = 0; i < 5; i++)
            {
                int x = 22 + i;
                if (x >= s) break;
                pixels[(16 + i) * s + x] = Color.white;
                pixels[(16 - i) * s + x] = Color.white;
            }
        }

        private static void DrawLiftIcon(Color[] pixels)
        {
            const int s = IconSize;
            for (int y = 4; y < 28; y++)
                for (int x = 13; x < 19; x++)
                    pixels[y * s + x] = Color.white;
            for (int i = 0; i < 5; i++)
            {
                int y = 22 + i;
                if (y >= s) break;
                pixels[y * s + (16 + i)] = Color.white;
                pixels[y * s + (16 - i)] = Color.white;
            }
        }

        private static void DrawWallIcon(Color[] pixels)
        {
            const int s = IconSize;
            Color brick = Color.white;
            Color mortar = new Color(0.5f, 0.5f, 0.5f, 1f);

            for (int y = 4; y < 28; y++)
                for (int x = 4; x < 28; x++)
                    pixels[y * s + x] = mortar;

            int brickH = 5;
            int mortarH = 1;
            int rowStart = 4;
            int row = 0;
            while (rowStart + brickH <= 28)
            {
                bool offset = (row % 2) == 1;
                int brickW = 10;
                int mortarW = 2;
                int xStart = 4 + (offset ? brickW / 2 : 0);
                for (int x = xStart; x < 28; x++)
                {
                    int localX = x - xStart;
                    if (localX % (brickW + mortarW) < brickW)
                    {
                        for (int y = rowStart; y < rowStart + brickH; y++)
                            pixels[y * s + x] = brick;
                    }
                }
                if (offset)
                {
                    for (int x = 4; x < 4 + brickW / 2; x++)
                        for (int y = rowStart; y < rowStart + brickH; y++)
                            pixels[y * s + x] = brick;
                }
                rowStart += brickH + mortarH;
                row++;
            }
        }

        private static void DrawPistonIcon(Color[] pixels)
        {
            const int s = IconSize;
            Color baseColor = new Color(0.6f, 0.6f, 0.7f, 1f);
            Color armColor = Color.white;

            for (int y = 6; y < 26; y++)
                for (int x = 4; x < 14; x++)
                    pixels[y * s + x] = baseColor;

            for (int y = 12; y < 20; y++)
                for (int x = 14; x < 28; x++)
                    pixels[y * s + x] = armColor;

            for (int i = 0; i < 4; i++)
            {
                int ax = 24 + i;
                if (ax < s)
                {
                    if (16 + i < s) pixels[(16 + i) * s + ax] = baseColor;
                    if (16 - i >= 0) pixels[(16 - i) * s + ax] = baseColor;
                }
            }
        }
    }
}
