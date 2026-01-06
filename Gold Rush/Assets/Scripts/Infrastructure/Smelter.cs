using UnityEngine;
using GoldRush.Core;
using GoldRush.Building;
using GoldRush.Simulation;

namespace GoldRush.Infrastructure
{
    public class Smelter : MonoBehaviour
    {
        public int GridX { get; private set; }
        public int GridY { get; private set; }

        private float smeltTimer;
        private const float SmeltInterval = 0.5f;  // How often to attempt smelting

        private float fireAnimTimer;
        private SpriteRenderer fireRenderer;

        // Grid coordinates for processing area
        private int simGridMinX, simGridMaxX, simGridMinY, simGridMaxY;

        // Smelting ratio: 2 Concentrate + 1 Coal → 1 Gold + 2 Slag
        private const int ConcentrateRequired = 2;
        private const int CoalRequired = 1;

        public static GameObject Create(int gridX, int gridY, Transform parent = null)
        {
            GameObject smelterGO = new GameObject($"Smelter_{gridX}_{gridY}");
            if (parent != null) smelterGO.transform.SetParent(parent);

            // Position using metadata grid (handles multi-cell offset)
            var info = BuildTypeData.Get(BuildType.Smelter);
            Vector2 worldPos = info.Grid.ToWorld(gridX, gridY);
            // Offset for 2-cell width
            worldPos.x += (info.CellSpanX - 1) * info.Grid.CellWidth / 2f / GameSettings.PixelsPerUnit;
            smelterGO.transform.position = worldPos;

            // Create visual components
            CreateVisuals(smelterGO);

            // Layer
            smelterGO.layer = LayerSetup.InfrastructureLayer;

            // Component
            Smelter smelter = smelterGO.AddComponent<Smelter>();
            smelter.GridX = gridX;
            smelter.GridY = gridY;
            smelter.fireRenderer = smelterGO.transform.Find("Fire")?.GetComponent<SpriteRenderer>();
            smelter.InitializeGridCoords();

            return smelterGO;
        }

        private static void CreateVisuals(GameObject parent)
        {
            float cellSize = GameSettings.GridSize / GameSettings.PixelsPerUnit;

            // Furnace body (64x32 pixels)
            SpriteRenderer bodySR = parent.AddComponent<SpriteRenderer>();
            bodySR.sprite = CreateFurnaceSprite();
            bodySR.sortingOrder = 8;  // Above terrain

            // Fire effect (animated)
            GameObject fireGO = new GameObject("Fire");
            fireGO.transform.SetParent(parent.transform);
            fireGO.transform.localPosition = new Vector3(0, -cellSize * 0.1f, 0);
            SpriteRenderer fireSR = fireGO.AddComponent<SpriteRenderer>();
            fireSR.sprite = CreateFireSprite(0);
            fireSR.sortingOrder = 9;  // Above furnace

            // Add colliders - open top and bottom
            float wallThickness = 2f / GameSettings.PixelsPerUnit;
            float height = cellSize;
            float width = cellSize * 2;

            // Left wall
            GameObject leftWall = new GameObject("LeftWall");
            leftWall.transform.SetParent(parent.transform);
            leftWall.transform.localPosition = new Vector3(-width / 2 + wallThickness / 2, 0, 0);
            BoxCollider2D leftCol = leftWall.AddComponent<BoxCollider2D>();
            leftCol.size = new Vector2(wallThickness, height);
            leftWall.layer = LayerSetup.InfrastructureLayer;

            // Right wall
            GameObject rightWall = new GameObject("RightWall");
            rightWall.transform.SetParent(parent.transform);
            rightWall.transform.localPosition = new Vector3(width / 2 - wallThickness / 2, 0, 0);
            BoxCollider2D rightCol = rightWall.AddComponent<BoxCollider2D>();
            rightCol.size = new Vector2(wallThickness, height);
            rightWall.layer = LayerSetup.InfrastructureLayer;

            // Bottom platform (partial - allows some material through)
            GameObject bottom = new GameObject("Bottom");
            bottom.transform.SetParent(parent.transform);
            bottom.transform.localPosition = new Vector3(0, -height / 2 + wallThickness / 2, 0);
            BoxCollider2D bottomCol = bottom.AddComponent<BoxCollider2D>();
            bottomCol.size = new Vector2(width * 0.7f, wallThickness);
            bottom.layer = LayerSetup.InfrastructureLayer;
        }

        private static Sprite CreateFurnaceSprite()
        {
            int width = GameSettings.GridSize * 2;
            int height = GameSettings.GridSize;
            Texture2D texture = new Texture2D(width, height);
            texture.filterMode = FilterMode.Point;

            Color brickColor = new Color(0.5f, 0.25f, 0.15f);  // Dark brick red
            Color darkBrick = new Color(0.35f, 0.15f, 0.1f);   // Darker for pattern
            Color metalColor = new Color(0.4f, 0.4f, 0.45f);   // Metal trim
            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Outer border
                    if (x <= 1 || x >= width - 2 || y <= 1 || y >= height - 2)
                    {
                        pixels[y * width + x] = metalColor;
                    }
                    // Top opening
                    else if (y >= height - 4 && x > 4 && x < width - 5)
                    {
                        pixels[y * width + x] = Color.clear;
                    }
                    // Bottom opening (smaller, centered)
                    else if (y < 5 && x > width / 3 && x < width * 2 / 3)
                    {
                        pixels[y * width + x] = Color.clear;
                    }
                    // Brick pattern
                    else
                    {
                        int brickRow = y / 4;
                        int offset = (brickRow % 2) * 4;
                        int brickCol = (x + offset) / 8;

                        // Mortar lines
                        if (y % 4 == 0 || (x + offset) % 8 == 0)
                        {
                            pixels[y * width + x] = darkBrick;
                        }
                        else
                        {
                            pixels[y * width + x] = brickColor;
                        }
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, width, height),
                                new Vector2(0.5f, 0.5f), GameSettings.PixelsPerUnit);
        }

        private static Sprite CreateFireSprite(int frame)
        {
            int width = 20;
            int height = 16;
            Texture2D texture = new Texture2D(width, height);
            texture.filterMode = FilterMode.Point;

            Color fireOrange = new Color(1f, 0.5f, 0f);
            Color fireYellow = new Color(1f, 0.8f, 0.2f);
            Color fireRed = new Color(0.9f, 0.2f, 0f);
            Color[] pixels = new Color[width * height];

            // Simple animated fire
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    pixels[y * width + x] = Color.clear;

                    // Fire shape - wider at bottom, narrower at top with flickering
                    float progress = (float)y / height;
                    float maxWidth = width * 0.8f * (1f - progress * 0.5f);
                    float centerX = width / 2f;

                    // Add some wave to the flames based on frame
                    float wave = Mathf.Sin((y * 0.5f + frame * 0.3f)) * 2f;
                    float adjustedX = x - wave;

                    if (Mathf.Abs(adjustedX - centerX) < maxWidth / 2f)
                    {
                        // Color based on height
                        if (progress < 0.3f)
                            pixels[y * width + x] = fireYellow;
                        else if (progress < 0.6f)
                            pixels[y * width + x] = fireOrange;
                        else if (progress < 0.9f)
                            pixels[y * width + x] = fireRed;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, width, height),
                                new Vector2(0.5f, 0.5f), GameSettings.PixelsPerUnit);
        }

        private void InitializeGridCoords()
        {
            if (SimulationWorld.Instance == null) return;

            Vector2 worldPos = transform.position;
            Vector2Int gridPos = SimulationWorld.Instance.WorldToGrid(worldPos);

            // Smelter dimensions from metadata
            var info = BuildTypeData.Get(BuildType.Smelter);
            simGridMinX = gridPos.x - info.SimHalfWidth;
            simGridMaxX = gridPos.x + info.SimHalfWidth;
            simGridMinY = gridPos.y - info.SimHalfHeight;
            simGridMaxY = gridPos.y + info.SimHalfHeight;
        }

        private void Update()
        {
            if (SimulationWorld.Instance == null) return;

            // Animate fire
            fireAnimTimer += Time.deltaTime;
            if (fireRenderer != null && fireAnimTimer >= 0.1f)
            {
                fireAnimTimer = 0f;
                int frame = (int)(Time.time * 10f) % 10;
                fireRenderer.sprite = CreateFireSprite(frame);
            }

            // Process smelting
            smeltTimer += Time.deltaTime;
            if (smeltTimer >= SmeltInterval)
            {
                smeltTimer = 0f;
                TrySmelt();
            }
        }

        private void TrySmelt()
        {
            var grid = SimulationWorld.Instance.Grid;

            // Count concentrate and coal in the smelter area
            int concentrateCount = 0;
            int coalCount = 0;

            // Store positions of materials for removal
            System.Collections.Generic.List<Vector2Int> concentratePositions = new System.Collections.Generic.List<Vector2Int>();
            System.Collections.Generic.List<Vector2Int> coalPositions = new System.Collections.Generic.List<Vector2Int>();

            for (int y = simGridMinY; y <= simGridMaxY; y++)
            {
                for (int x = simGridMinX; x <= simGridMaxX; x++)
                {
                    MaterialType type = grid.Get(x, y);

                    if (type == MaterialType.Concentrate)
                    {
                        concentrateCount++;
                        concentratePositions.Add(new Vector2Int(x, y));
                    }
                    else if (type == MaterialType.Coal)
                    {
                        coalCount++;
                        coalPositions.Add(new Vector2Int(x, y));
                    }
                }
            }

            // Check if we have enough materials
            if (concentrateCount >= ConcentrateRequired && coalCount >= CoalRequired)
            {
                // Remove consumed materials (2 concentrate, 1 coal)
                for (int i = 0; i < ConcentrateRequired && i < concentratePositions.Count; i++)
                {
                    var pos = concentratePositions[i];
                    grid.Set(pos.x, pos.y, MaterialType.Air);
                }

                for (int i = 0; i < CoalRequired && i < coalPositions.Count; i++)
                {
                    var pos = coalPositions[i];
                    grid.Set(pos.x, pos.y, MaterialType.Air);
                }

                // Spawn outputs (1 gold, 2 slag)
                // Try to spawn at the bottom of the smelter
                int spawnY = simGridMaxY + 1;  // Below smelter
                int spawnX = (simGridMinX + simGridMaxX) / 2;

                // Spawn gold
                if (grid.Get(spawnX, spawnY) == MaterialType.Air)
                {
                    grid.Set(spawnX, spawnY, MaterialType.Gold);
                    grid.WakeCell(spawnX, spawnY);
                }

                // Spawn slag on either side
                if (grid.Get(spawnX - 2, spawnY) == MaterialType.Air)
                {
                    grid.Set(spawnX - 2, spawnY, MaterialType.Slag);
                    grid.WakeCell(spawnX - 2, spawnY);
                }
                if (grid.Get(spawnX + 2, spawnY) == MaterialType.Air)
                {
                    grid.Set(spawnX + 2, spawnY, MaterialType.Slag);
                    grid.WakeCell(spawnX + 2, spawnY);
                }
            }
        }
    }
}
