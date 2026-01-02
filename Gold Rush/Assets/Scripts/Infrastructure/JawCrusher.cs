using UnityEngine;
using GoldRush.Core;
using GoldRush.Simulation;

namespace GoldRush.Infrastructure
{
    public class JawCrusher : MonoBehaviour
    {
        public int GridX { get; private set; }
        public int GridY { get; private set; }

        private float crushTimer;
        private const float CrushInterval = 0.3f;
        private bool jawClosed;

        private Transform movingJaw;
        private float jawOffset;
        private const float JawTravel = 2f / GameSettings.PixelsPerUnit;  // 2 pixels travel

        // Grid coordinates for processing area
        private int simGridMinX, simGridMaxX, simGridMinY, simGridMaxY;

        public static GameObject Create(int gridX, int gridY, Transform parent = null)
        {
            GameObject crusherGO = new GameObject($"JawCrusher_{gridX}_{gridY}");
            if (parent != null) crusherGO.transform.SetParent(parent);

            // Position using infra grid (8x8 pixel cells)
            Vector2 worldPos = GameSettings.InfraGridToWorld(gridX, gridY);
            // Offset for 8x16 size (center between two cells)
            worldPos.y -= GameSettings.InfraGridSize / (2f * GameSettings.PixelsPerUnit);
            crusherGO.transform.position = worldPos;

            // Create visual components
            CreateVisuals(crusherGO);

            // Layer
            crusherGO.layer = LayerSetup.InfrastructureLayer;

            // Component
            JawCrusher crusher = crusherGO.AddComponent<JawCrusher>();
            crusher.GridX = gridX;
            crusher.GridY = gridY;
            crusher.movingJaw = crusherGO.transform.Find("MovingJaw");
            crusher.InitializeGridCoords();

            return crusherGO;
        }

        private static void CreateVisuals(GameObject parent)
        {
            float cellSize = GameSettings.InfraGridSize / GameSettings.PixelsPerUnit;

            // Fixed jaw (right side - angled)
            GameObject fixedJawGO = new GameObject("FixedJaw");
            fixedJawGO.transform.SetParent(parent.transform);
            fixedJawGO.transform.localPosition = Vector3.zero;
            SpriteRenderer fixedSR = fixedJawGO.AddComponent<SpriteRenderer>();
            fixedSR.sprite = CreateFixedJawSprite();
            fixedSR.sortingOrder = 8;  // Above terrain

            // Moving jaw (left side - oscillates)
            GameObject movingJawGO = new GameObject("MovingJaw");
            movingJawGO.transform.SetParent(parent.transform);
            movingJawGO.transform.localPosition = Vector3.zero;
            SpriteRenderer movingSR = movingJawGO.AddComponent<SpriteRenderer>();
            movingSR.sprite = CreateMovingJawSprite();
            movingSR.sortingOrder = 9;  // Above fixed jaw

            // Add colliders
            float wallThickness = 1f / GameSettings.PixelsPerUnit;
            float height = cellSize * 2;

            // Right wall (fixed, angled)
            GameObject rightWall = new GameObject("RightWall");
            rightWall.transform.SetParent(parent.transform);
            rightWall.transform.localPosition = new Vector3(cellSize / 2 - wallThickness, 0, 0);
            rightWall.transform.localRotation = Quaternion.Euler(0, 0, 10);
            BoxCollider2D rightCol = rightWall.AddComponent<BoxCollider2D>();
            rightCol.size = new Vector2(wallThickness, height);
            rightWall.layer = LayerSetup.InfrastructureLayer;

            // Left wall (moving)
            GameObject leftWall = new GameObject("LeftWallCollider");
            leftWall.transform.SetParent(movingJawGO.transform);
            leftWall.transform.localPosition = new Vector3(-cellSize / 2 + wallThickness * 2, 0, 0);
            leftWall.transform.localRotation = Quaternion.Euler(0, 0, -10);
            BoxCollider2D leftCol = leftWall.AddComponent<BoxCollider2D>();
            leftCol.size = new Vector2(wallThickness * 2, height);
            leftWall.layer = LayerSetup.InfrastructureLayer;
        }

        private static Sprite CreateFixedJawSprite()
        {
            int width = GameSettings.InfraGridSize;
            int height = GameSettings.InfraGridSize * 2;
            Texture2D texture = new Texture2D(width, height);
            texture.filterMode = FilterMode.Point;

            Color jawColor = new Color(0.45f, 0.45f, 0.5f);  // Grey metal
            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Draw angled right jaw (V shape right side)
                    float progress = (float)y / height;  // 0 at bottom, 1 at top
                    int minX = (int)(width * 0.5f + progress * (width * 0.3f));  // More to the right at top

                    if (x >= minX)
                    {
                        pixels[y * width + x] = jawColor;
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

        private static Sprite CreateMovingJawSprite()
        {
            int width = GameSettings.InfraGridSize;
            int height = GameSettings.InfraGridSize * 2;
            Texture2D texture = new Texture2D(width, height);
            texture.filterMode = FilterMode.Point;

            Color jawColor = new Color(0.5f, 0.4f, 0.35f);  // Slightly different color
            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Draw angled left jaw (V shape left side)
                    float progress = (float)y / height;  // 0 at bottom, 1 at top
                    int maxX = (int)(width * 0.5f - progress * (width * 0.3f));  // More to the left at top

                    if (x <= maxX && x >= 0)
                    {
                        pixels[y * width + x] = jawColor;
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

            // Jaw crusher processes area roughly 4x8 simulation cells
            simGridMinX = gridPos.x - 2;
            simGridMaxX = gridPos.x + 2;
            simGridMinY = gridPos.y - 4;
            simGridMaxY = gridPos.y + 4;
        }

        private void Update()
        {
            if (SimulationWorld.Instance == null) return;

            crushTimer += Time.deltaTime;
            if (crushTimer >= CrushInterval)
            {
                crushTimer = 0f;
                jawClosed = !jawClosed;

                if (jawClosed)
                {
                    // Crush rock when jaw closes
                    CrushRock();
                }
            }

            // Animate moving jaw
            if (movingJaw != null)
            {
                float targetOffset = jawClosed ? JawTravel : 0f;
                jawOffset = Mathf.Lerp(jawOffset, targetOffset, Time.deltaTime * 30f);
                movingJaw.localPosition = new Vector3(jawOffset, 0, 0);
            }
        }

        private void CrushRock()
        {
            var grid = SimulationWorld.Instance.Grid;

            // Check inside the V-shape for rock and convert to gravel
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
