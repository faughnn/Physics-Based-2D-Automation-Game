using UnityEngine;
using GoldRush.Core;
using GoldRush.Simulation;

namespace GoldRush.Infrastructure
{
    public class StampMill : MonoBehaviour
    {
        public int GridX { get; private set; }
        public int GridY { get; private set; }

        private float stampTimer;
        private const float StampInterval = 0.5f;
        private bool stampDown;

        private Transform stampHead;
        private float stampOffset;
        private const float StampTravel = 4f / GameSettings.PixelsPerUnit;  // 4 pixels travel

        // Grid coordinates for processing area
        private int simGridMinX, simGridMaxX, simGridMinY, simGridMaxY;

        public static GameObject Create(int gridX, int gridY, Transform parent = null)
        {
            GameObject millGO = new GameObject($"StampMill_{gridX}_{gridY}");
            if (parent != null) millGO.transform.SetParent(parent);

            // Position using infra grid (8x8 pixel cells)
            Vector2 worldPos = GameSettings.InfraGridToWorld(gridX, gridY);
            // Offset for 8x16 size (center between two cells)
            worldPos.y -= GameSettings.InfraGridSize / (2f * GameSettings.PixelsPerUnit);
            millGO.transform.position = worldPos;

            // Create visual components
            CreateVisuals(millGO);

            // Layer
            millGO.layer = LayerSetup.InfrastructureLayer;

            // Component
            StampMill mill = millGO.AddComponent<StampMill>();
            mill.GridX = gridX;
            mill.GridY = gridY;
            mill.stampHead = millGO.transform.Find("StampHead");
            mill.InitializeGridCoords();

            return millGO;
        }

        private static void CreateVisuals(GameObject parent)
        {
            float cellSize = GameSettings.InfraGridSize / GameSettings.PixelsPerUnit;

            // Base/Anvil (bottom part - 8x6 pixels)
            GameObject baseGO = new GameObject("Base");
            baseGO.transform.SetParent(parent.transform);
            baseGO.transform.localPosition = new Vector3(0, -cellSize * 0.4f, 0);
            SpriteRenderer baseSR = baseGO.AddComponent<SpriteRenderer>();
            baseSR.sprite = CreateStampBaseSprite();
            baseSR.sortingOrder = 8;  // Above terrain

            // Stamp head (top part - 8x6 pixels, animates)
            GameObject headGO = new GameObject("StampHead");
            headGO.transform.SetParent(parent.transform);
            headGO.transform.localPosition = new Vector3(0, cellSize * 0.4f, 0);
            SpriteRenderer headSR = headGO.AddComponent<SpriteRenderer>();
            headSR.sprite = CreateStampHeadSprite();
            headSR.sortingOrder = 9;  // Above base

            // Add colliders - side walls only (open top and bottom)
            float wallThickness = 1f / GameSettings.PixelsPerUnit;
            float height = cellSize * 2;

            // Left wall
            GameObject leftWall = new GameObject("LeftWall");
            leftWall.transform.SetParent(parent.transform);
            leftWall.transform.localPosition = new Vector3(-cellSize / 2 + wallThickness / 2, 0, 0);
            BoxCollider2D leftCol = leftWall.AddComponent<BoxCollider2D>();
            leftCol.size = new Vector2(wallThickness, height);
            leftWall.layer = LayerSetup.InfrastructureLayer;

            // Right wall
            GameObject rightWall = new GameObject("RightWall");
            rightWall.transform.SetParent(parent.transform);
            rightWall.transform.localPosition = new Vector3(cellSize / 2 - wallThickness / 2, 0, 0);
            BoxCollider2D rightCol = rightWall.AddComponent<BoxCollider2D>();
            rightCol.size = new Vector2(wallThickness, height);
            rightWall.layer = LayerSetup.InfrastructureLayer;
        }

        private static Sprite CreateStampBaseSprite()
        {
            int width = GameSettings.InfraGridSize;
            int height = 6;
            Texture2D texture = new Texture2D(width, height);
            texture.filterMode = FilterMode.Point;

            Color baseColor = new Color(0.4f, 0.3f, 0.2f);  // Brown
            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Draw anvil shape - wider at bottom
                    if (x == 0 || x == width - 1)
                    {
                        pixels[y * width + x] = baseColor;
                    }
                    else if (y < 2)  // Bottom platform
                    {
                        pixels[y * width + x] = baseColor;
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

        private static Sprite CreateStampHeadSprite()
        {
            int width = GameSettings.InfraGridSize;
            int height = 6;
            Texture2D texture = new Texture2D(width, height);
            texture.filterMode = FilterMode.Point;

            Color headColor = new Color(0.5f, 0.5f, 0.5f);  // Grey metal
            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Draw stamp head - T shape
                    if (y >= height - 2)  // Top bar
                    {
                        pixels[y * width + x] = headColor;
                    }
                    else if (x >= 2 && x < width - 2)  // Stem
                    {
                        pixels[y * width + x] = headColor;
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

        private void InitializeGridCoords()
        {
            if (SimulationWorld.Instance == null) return;

            Vector2 worldPos = transform.position;
            Vector2Int gridPos = SimulationWorld.Instance.WorldToGrid(worldPos);

            // Stamp mill processes area roughly 4x8 simulation cells
            simGridMinX = gridPos.x - 2;
            simGridMaxX = gridPos.x + 2;
            simGridMinY = gridPos.y - 4;
            simGridMaxY = gridPos.y + 4;
        }

        private void Update()
        {
            if (SimulationWorld.Instance == null) return;

            stampTimer += Time.deltaTime;
            if (stampTimer >= StampInterval)
            {
                stampTimer = 0f;
                stampDown = !stampDown;

                if (stampDown)
                {
                    // Crush rock when stamp comes down
                    CrushRock();
                }
            }

            // Animate stamp head
            if (stampHead != null)
            {
                float targetOffset = stampDown ? -StampTravel : 0f;
                stampOffset = Mathf.Lerp(stampOffset, targetOffset, Time.deltaTime * 20f);
                float cellSize = GameSettings.InfraGridSize / GameSettings.PixelsPerUnit;
                stampHead.localPosition = new Vector3(0, cellSize * 0.4f + stampOffset, 0);
            }
        }

        private void CrushRock()
        {
            var grid = SimulationWorld.Instance.Grid;

            // Check middle area for rock and convert to gravel
            for (int y = simGridMinY; y <= simGridMaxY; y++)
            {
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
