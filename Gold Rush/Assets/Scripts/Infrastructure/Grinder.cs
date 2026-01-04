using UnityEngine;
using GoldRush.Core;
using GoldRush.Simulation;

namespace GoldRush.Infrastructure
{
    public class Grinder : MonoBehaviour
    {
        public int GridX { get; private set; }
        public int GridY { get; private set; }

        private float rotationAngle;
        private const float RotationSpeed = 240f;  // Degrees per second (faster than roller crusher)

        private Transform leftDisc;
        private Transform rightDisc;

        // Grid coordinates for processing area
        private int simGridMinX, simGridMaxX, simGridY;

        public static GameObject Create(int gridX, int gridY, Transform parent = null)
        {
            GameObject grinderGO = new GameObject($"Grinder_{gridX}_{gridY}");
            if (parent != null) grinderGO.transform.SetParent(parent);

            // Position using infra grid (32x32 pixel cells)
            Vector2 worldPos = GameSettings.InfraGridToWorld(gridX, gridY);
            grinderGO.transform.position = worldPos;

            // Create visual components
            CreateVisuals(grinderGO);

            // Layer
            grinderGO.layer = LayerSetup.InfrastructureLayer;

            // Component
            Grinder grinder = grinderGO.AddComponent<Grinder>();
            grinder.GridX = gridX;
            grinder.GridY = gridY;
            grinder.leftDisc = grinderGO.transform.Find("LeftDisc");
            grinder.rightDisc = grinderGO.transform.Find("RightDisc");
            grinder.InitializeGridCoords();

            return grinderGO;
        }

        private static void CreateVisuals(GameObject parent)
        {
            float cellSize = GameSettings.InfraGridSize / GameSettings.PixelsPerUnit;

            // Frame (32x32 pixels)
            SpriteRenderer frameSR = parent.AddComponent<SpriteRenderer>();
            frameSR.sprite = CreateFrameSprite();
            frameSR.sortingOrder = 8;  // Above terrain

            // Left grinding disc (10x10 pixels, rotates clockwise)
            GameObject leftGO = new GameObject("LeftDisc");
            leftGO.transform.SetParent(parent.transform);
            leftGO.transform.localPosition = new Vector3(-cellSize * 0.25f, 0, 0);
            SpriteRenderer leftSR = leftGO.AddComponent<SpriteRenderer>();
            leftSR.sprite = CreateDiscSprite();
            leftSR.sortingOrder = 9;  // Above frame

            // Right grinding disc (10x10 pixels, rotates counter-clockwise)
            GameObject rightGO = new GameObject("RightDisc");
            rightGO.transform.SetParent(parent.transform);
            rightGO.transform.localPosition = new Vector3(cellSize * 0.25f, 0, 0);
            SpriteRenderer rightSR = rightGO.AddComponent<SpriteRenderer>();
            rightSR.sprite = CreateDiscSprite();
            rightSR.sortingOrder = 9;  // Above frame

            // Add colliders - funnel shape to guide material into grinder
            float wallThickness = 2f / GameSettings.PixelsPerUnit;

            // Left angled wall (guides material into discs)
            GameObject leftWall = new GameObject("LeftWall");
            leftWall.transform.SetParent(parent.transform);
            leftWall.transform.localPosition = new Vector3(-cellSize / 2 + wallThickness, cellSize * 0.25f, 0);
            leftWall.transform.localRotation = Quaternion.Euler(0, 0, 25);
            BoxCollider2D leftCol = leftWall.AddComponent<BoxCollider2D>();
            leftCol.size = new Vector2(wallThickness, cellSize * 0.5f);
            leftWall.layer = LayerSetup.InfrastructureLayer;

            // Right angled wall
            GameObject rightWall = new GameObject("RightWall");
            rightWall.transform.SetParent(parent.transform);
            rightWall.transform.localPosition = new Vector3(cellSize / 2 - wallThickness, cellSize * 0.25f, 0);
            rightWall.transform.localRotation = Quaternion.Euler(0, 0, -25);
            BoxCollider2D rightCol = rightWall.AddComponent<BoxCollider2D>();
            rightCol.size = new Vector2(wallThickness, cellSize * 0.5f);
            rightWall.layer = LayerSetup.InfrastructureLayer;
        }

        private static Sprite CreateFrameSprite()
        {
            int size = GameSettings.InfraGridSize;
            Texture2D texture = new Texture2D(size, size);
            texture.filterMode = FilterMode.Point;

            Color frameColor = new Color(0.35f, 0.35f, 0.4f);  // Dark grey-blue
            Color holeColor = Color.clear;
            Color[] pixels = new Color[size * size];

            float centerX = size / 2f;
            float centerY = size / 2f;
            float outerRadius = size / 2f;
            float innerRadius = size / 4f;  // Hole in center for material to fall through

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - centerX + 0.5f;
                    float dy = y - centerY + 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    // Draw frame with central opening
                    if (x == 0 || x == size - 1 || y == 0 || y == size - 1)
                    {
                        pixels[y * size + x] = frameColor;  // Outer border
                    }
                    else if (dist < innerRadius)
                    {
                        pixels[y * size + x] = holeColor;  // Central hole
                    }
                    else if (y < size / 3)
                    {
                        pixels[y * size + x] = holeColor;  // Bottom opening for output
                    }
                    else
                    {
                        pixels[y * size + x] = holeColor;  // Interior mostly open
                    }
                }
            }

            // Draw frame edges
            for (int y = 0; y < size; y++)
            {
                pixels[y * size + 0] = frameColor;
                pixels[y * size + size - 1] = frameColor;
            }
            for (int x = 0; x < size; x++)
            {
                pixels[0 * size + x] = frameColor;
                pixels[(size - 1) * size + x] = frameColor;
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, size, size),
                                new Vector2(0.5f, 0.5f), GameSettings.PixelsPerUnit);
        }

        private static Sprite CreateDiscSprite()
        {
            int diameter = 10;
            Texture2D texture = new Texture2D(diameter, diameter);
            texture.filterMode = FilterMode.Point;

            Color discColor = new Color(0.6f, 0.55f, 0.5f);  // Light brown metal
            Color grooveColor = new Color(0.4f, 0.35f, 0.3f);  // Darker grooves
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
                        // Add radial grooves for grinding texture
                        float angle = Mathf.Atan2(y - radius, x - radius);
                        int groove = Mathf.FloorToInt((angle + Mathf.PI) / (Mathf.PI / 4));

                        if (groove % 2 == 0)
                        {
                            pixels[y * diameter + x] = discColor;
                        }
                        else
                        {
                            pixels[y * diameter + x] = grooveColor;
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

            // Grinder processes area roughly 8x8 simulation cells
            simGridMinX = gridPos.x - 4;
            simGridMaxX = gridPos.x + 4;
            simGridY = gridPos.y;
        }

        private void Update()
        {
            if (SimulationWorld.Instance == null) return;

            // Rotate discs visually (opposite directions)
            rotationAngle += RotationSpeed * Time.deltaTime;
            if (leftDisc != null)
            {
                leftDisc.localRotation = Quaternion.Euler(0, 0, rotationAngle);
            }
            if (rightDisc != null)
            {
                rightDisc.localRotation = Quaternion.Euler(0, 0, -rotationAngle);
            }

            // Continuously grind materials
            GrindMaterial();
        }

        private void GrindMaterial()
        {
            var grid = SimulationWorld.Instance.Grid;
            var clusterMgr = grid.ClusterManager;

            // First, handle 2x2 Gravel clusters
            int simGridMinY = simGridY - 4;
            int simGridMaxY = simGridY + 4;

            foreach (var cluster in clusterMgr.GetClustersInArea(simGridMinX, simGridMinY, simGridMaxX, simGridMaxY))
            {
                if (cluster.Type == MaterialType.Gravel && cluster.Size == 2)
                {
                    // Break 2x2 Gravel into 1x1 Sand
                    clusterMgr.BreakCluster(cluster.ID, 1, MaterialType.Sand);
                }
            }

            // Then handle legacy single-cell gravel and concentrate
            for (int dy = -4; dy <= 4; dy++)
            {
                int y = simGridY + dy;
                for (int x = simGridMinX; x <= simGridMaxX; x++)
                {
                    // Skip cells that are part of clusters
                    if (grid.GetClusterID(x, y) != 0) continue;

                    MaterialType type = grid.Get(x, y);

                    // Grind gravel and concentrate into sand
                    if (type == MaterialType.Gravel || type == MaterialType.Concentrate)
                    {
                        grid.Set(x, y, MaterialType.Sand);
                    }
                }
            }
        }
    }
}
