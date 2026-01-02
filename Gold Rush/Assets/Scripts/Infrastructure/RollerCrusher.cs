using UnityEngine;
using GoldRush.Core;
using GoldRush.Simulation;

namespace GoldRush.Infrastructure
{
    public class RollerCrusher : MonoBehaviour
    {
        public int GridX { get; private set; }
        public int GridY { get; private set; }

        private float rotationAngle;
        private const float RotationSpeed = 180f;  // Degrees per second

        private Transform leftRoller;
        private Transform rightRoller;

        // Grid coordinates for processing area
        private int simGridMinX, simGridMaxX, simGridY;

        public static GameObject Create(int gridX, int gridY, Transform parent = null)
        {
            GameObject crusherGO = new GameObject($"RollerCrusher_{gridX}_{gridY}");
            if (parent != null) crusherGO.transform.SetParent(parent);

            // Position using infra grid (8x8 pixel cells)
            Vector2 worldPos = GameSettings.InfraGridToWorld(gridX, gridY);
            // Offset for 16x8 size (center between two cells horizontally)
            worldPos.x += GameSettings.InfraGridSize / (2f * GameSettings.PixelsPerUnit);
            crusherGO.transform.position = worldPos;

            // Create visual components
            CreateVisuals(crusherGO);

            // Layer
            crusherGO.layer = LayerSetup.InfrastructureLayer;

            // Component
            RollerCrusher crusher = crusherGO.AddComponent<RollerCrusher>();
            crusher.GridX = gridX;
            crusher.GridY = gridY;
            crusher.leftRoller = crusherGO.transform.Find("LeftRoller");
            crusher.rightRoller = crusherGO.transform.Find("RightRoller");
            crusher.InitializeGridCoords();

            return crusherGO;
        }

        private static void CreateVisuals(GameObject parent)
        {
            float cellSize = GameSettings.InfraGridSize / GameSettings.PixelsPerUnit;

            // Frame (16x8 pixels)
            SpriteRenderer frameSR = parent.AddComponent<SpriteRenderer>();
            frameSR.sprite = CreateFrameSprite();
            frameSR.sortingOrder = 8;  // Above terrain

            // Left roller (6x6 pixels, rotates clockwise)
            GameObject leftGO = new GameObject("LeftRoller");
            leftGO.transform.SetParent(parent.transform);
            leftGO.transform.localPosition = new Vector3(-cellSize * 0.35f, 0, 0);
            SpriteRenderer leftSR = leftGO.AddComponent<SpriteRenderer>();
            leftSR.sprite = CreateRollerSprite();
            leftSR.sortingOrder = 9;  // Above frame

            // Right roller (6x6 pixels, rotates counter-clockwise)
            GameObject rightGO = new GameObject("RightRoller");
            rightGO.transform.SetParent(parent.transform);
            rightGO.transform.localPosition = new Vector3(cellSize * 0.35f, 0, 0);
            SpriteRenderer rightSR = rightGO.AddComponent<SpriteRenderer>();
            rightSR.sprite = CreateRollerSprite();
            rightSR.sortingOrder = 9;  // Above frame

            // Add colliders - funnel shape
            float wallThickness = 1f / GameSettings.PixelsPerUnit;
            float totalWidth = cellSize * 2;

            // Left angled wall (guides material into rollers)
            GameObject leftWall = new GameObject("LeftWall");
            leftWall.transform.SetParent(parent.transform);
            leftWall.transform.localPosition = new Vector3(-totalWidth / 2 + wallThickness, cellSize * 0.3f, 0);
            leftWall.transform.localRotation = Quaternion.Euler(0, 0, 30);
            BoxCollider2D leftCol = leftWall.AddComponent<BoxCollider2D>();
            leftCol.size = new Vector2(wallThickness, cellSize * 0.6f);
            leftWall.layer = LayerSetup.InfrastructureLayer;

            // Right angled wall
            GameObject rightWall = new GameObject("RightWall");
            rightWall.transform.SetParent(parent.transform);
            rightWall.transform.localPosition = new Vector3(totalWidth / 2 - wallThickness, cellSize * 0.3f, 0);
            rightWall.transform.localRotation = Quaternion.Euler(0, 0, -30);
            BoxCollider2D rightCol = rightWall.AddComponent<BoxCollider2D>();
            rightCol.size = new Vector2(wallThickness, cellSize * 0.6f);
            rightWall.layer = LayerSetup.InfrastructureLayer;
        }

        private static Sprite CreateFrameSprite()
        {
            int width = GameSettings.InfraGridSize * 2;
            int height = GameSettings.InfraGridSize;
            Texture2D texture = new Texture2D(width, height);
            texture.filterMode = FilterMode.Point;

            Color frameColor = new Color(0.3f, 0.3f, 0.35f);  // Dark grey-blue
            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Draw frame outline
                    if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                    {
                        pixels[y * width + x] = frameColor;
                    }
                    else
                    {
                        pixels[y * width + x] = Color.clear;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, width, height),
                                new Vector2(0.5f, 0.5f), GameSettings.PixelsPerUnit);
        }

        private static Sprite CreateRollerSprite()
        {
            int diameter = 6;
            Texture2D texture = new Texture2D(diameter, diameter);
            texture.filterMode = FilterMode.Point;

            Color rollerColor = new Color(0.5f, 0.4f, 0.3f);  // Brown metal
            Color centerColor = new Color(0.6f, 0.5f, 0.4f);  // Lighter center for rotation visibility
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
                        // Add a notch for rotation visibility
                        if (x == diameter / 2 && y >= diameter / 2)
                        {
                            pixels[y * diameter + x] = centerColor;
                        }
                        else
                        {
                            pixels[y * diameter + x] = rollerColor;
                        }
                    }
                    else
                    {
                        pixels[y * diameter + x] = Color.clear;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, diameter, diameter),
                                new Vector2(0.5f, 0.5f), GameSettings.PixelsPerUnit);
        }

        private void InitializeGridCoords()
        {
            if (SimulationWorld.Instance == null) return;

            Vector2 worldPos = transform.position;
            Vector2Int gridPos = SimulationWorld.Instance.WorldToGrid(worldPos);

            // Roller crusher processes area roughly 8x4 simulation cells
            simGridMinX = gridPos.x - 4;
            simGridMaxX = gridPos.x + 4;
            simGridY = gridPos.y;
        }

        private void Update()
        {
            if (SimulationWorld.Instance == null) return;

            // Rotate rollers visually
            rotationAngle += RotationSpeed * Time.deltaTime;
            if (leftRoller != null)
            {
                leftRoller.localRotation = Quaternion.Euler(0, 0, rotationAngle);
            }
            if (rightRoller != null)
            {
                rightRoller.localRotation = Quaternion.Euler(0, 0, -rotationAngle);
            }

            // Continuously process rock
            CrushRock();
        }

        private void CrushRock()
        {
            var grid = SimulationWorld.Instance.Grid;

            // Check area around the rollers for rock
            for (int dy = -2; dy <= 2; dy++)
            {
                int y = simGridY + dy;
                for (int x = simGridMinX; x <= simGridMaxX; x++)
                {
                    MaterialType type = grid.Get(x, y);
                    if (type == MaterialType.Rock)
                    {
                        grid.Set(x, y, MaterialType.Gravel);
                    }
                }
            }
        }
    }
}
