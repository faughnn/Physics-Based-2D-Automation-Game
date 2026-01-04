using UnityEngine;
using GoldRush.Core;
using GoldRush.Simulation;
using System.Collections.Generic;

namespace GoldRush.Infrastructure
{
    public class BigCrusher : MonoBehaviour
    {
        public int GridX { get; private set; }
        public int GridY { get; private set; }

        // Plate movement
        private float platePosition;  // 0 = fully open, 1 = fully closed (meeting in middle)
        private bool platesClosing = true;
        private const float PlateSpeed = 0.3f;  // How fast plates move (0-1 range per second)
        private const float PauseAtEnds = 0.3f;  // Pause time when fully open/closed
        private float pauseTimer;

        // Crusher dimensions in simulation grid cells
        private int crusherCenterX;
        private int crusherCenterY;
        private int crusherHalfWidth;  // Half width in sim cells (from center to wall)
        private int crusherTop;
        private int crusherBottom;

        // Plate thickness in sim cells
        private const int PlateThickness = 3;

        private Transform leftPlate;
        private Transform rightPlate;
        private SpriteRenderer leftPlateSR;
        private SpriteRenderer rightPlateSR;

        // Visual dimensions
        private float totalWidthWorld;
        private float plateStartOffset;

        public static GameObject Create(int gridX, int gridY, Transform parent = null)
        {
            GameObject crusherGO = new GameObject($"BigCrusher_{gridX}_{gridY}");
            if (parent != null) crusherGO.transform.SetParent(parent);

            // Position using infra grid (32x32 pixel cells)
            // Big crusher is 2x2 cells (64x64 pixels)
            Vector2 worldPos = GameSettings.InfraGridToWorld(gridX, gridY);
            // Offset to center of 2x2 cell area
            worldPos.x += GameSettings.InfraGridSize / (2f * GameSettings.PixelsPerUnit);
            worldPos.y -= GameSettings.InfraGridSize / (2f * GameSettings.PixelsPerUnit);
            crusherGO.transform.position = worldPos;

            // Create visual components
            CreateVisuals(crusherGO);

            // Layer
            crusherGO.layer = LayerSetup.InfrastructureLayer;

            // Component
            BigCrusher crusher = crusherGO.AddComponent<BigCrusher>();
            crusher.GridX = gridX;
            crusher.GridY = gridY;
            crusher.leftPlate = crusherGO.transform.Find("LeftPlate");
            crusher.rightPlate = crusherGO.transform.Find("RightPlate");
            crusher.leftPlateSR = crusher.leftPlate?.GetComponent<SpriteRenderer>();
            crusher.rightPlateSR = crusher.rightPlate?.GetComponent<SpriteRenderer>();
            crusher.InitializeGridCoords();

            return crusherGO;
        }

        private static void CreateVisuals(GameObject parent)
        {
            float cellSize = GameSettings.InfraGridSize / GameSettings.PixelsPerUnit;
            float totalWidth = cellSize * 2;   // 2 cells wide (64 pixels)
            float totalHeight = cellSize * 2;  // 2 cells tall (64 pixels)

            // Frame (64x64 pixels) - outer shell
            SpriteRenderer frameSR = parent.AddComponent<SpriteRenderer>();
            frameSR.sprite = CreateFrameSprite();
            frameSR.sortingOrder = 8;

            // Left plate (movable) - starts at left edge
            GameObject leftGO = new GameObject("LeftPlate");
            leftGO.transform.SetParent(parent.transform);
            leftGO.transform.localPosition = new Vector3(-totalWidth / 2 + 6f / GameSettings.PixelsPerUnit, 0, 0);
            SpriteRenderer leftSR = leftGO.AddComponent<SpriteRenderer>();
            leftSR.sprite = CreatePlateSprite();
            leftSR.sortingOrder = 9;

            // Right plate (movable) - starts at right edge
            GameObject rightGO = new GameObject("RightPlate");
            rightGO.transform.SetParent(parent.transform);
            rightGO.transform.localPosition = new Vector3(totalWidth / 2 - 6f / GameSettings.PixelsPerUnit, 0, 0);
            SpriteRenderer rightSR = rightGO.AddComponent<SpriteRenderer>();
            rightSR.sprite = CreatePlateSprite();
            rightSR.flipX = true;
            rightSR.sortingOrder = 9;

            // Side wall colliders
            float wallThickness = 4f / GameSettings.PixelsPerUnit;

            // Left wall
            GameObject leftWall = new GameObject("LeftWall");
            leftWall.transform.SetParent(parent.transform);
            leftWall.transform.localPosition = new Vector3(-totalWidth / 2, 0, 0);
            BoxCollider2D leftCol = leftWall.AddComponent<BoxCollider2D>();
            leftCol.size = new Vector2(wallThickness, totalHeight);
            leftWall.layer = LayerSetup.InfrastructureLayer;

            // Right wall
            GameObject rightWall = new GameObject("RightWall");
            rightWall.transform.SetParent(parent.transform);
            rightWall.transform.localPosition = new Vector3(totalWidth / 2, 0, 0);
            BoxCollider2D rightCol = rightWall.AddComponent<BoxCollider2D>();
            rightCol.size = new Vector2(wallThickness, totalHeight);
            rightWall.layer = LayerSetup.InfrastructureLayer;

            // Bottom grate (visual only)
            GameObject grate = new GameObject("Grate");
            grate.transform.SetParent(parent.transform);
            grate.transform.localPosition = new Vector3(0, -totalHeight / 2 + 3f / GameSettings.PixelsPerUnit, 0);
            SpriteRenderer grateSR = grate.AddComponent<SpriteRenderer>();
            grateSR.sprite = CreateGrateSprite();
            grateSR.sortingOrder = 10;
        }

        private static Sprite CreateFrameSprite()
        {
            int width = GameSettings.InfraGridSize * 2;   // 64 pixels
            int height = GameSettings.InfraGridSize * 2;  // 64 pixels
            Texture2D texture = new Texture2D(width, height);
            texture.filterMode = FilterMode.Point;

            Color frameColor = new Color(0.35f, 0.35f, 0.4f);
            Color darkColor = new Color(0.25f, 0.25f, 0.3f);
            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (x < 4 || x >= width - 4)
                    {
                        pixels[y * width + x] = frameColor;
                    }
                    else if (y >= height - 3)
                    {
                        pixels[y * width + x] = darkColor;
                    }
                    else if (y < 6)
                    {
                        pixels[y * width + x] = darkColor;
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

        private static Sprite CreatePlateSprite()
        {
            int width = 10;
            int height = 52;
            Texture2D texture = new Texture2D(width, height);
            texture.filterMode = FilterMode.Point;

            Color plateColor = new Color(0.5f, 0.5f, 0.55f);
            Color highlightColor = new Color(0.6f, 0.6f, 0.65f);
            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (x >= width - 2)
                    {
                        pixels[y * width + x] = highlightColor;
                    }
                    else
                    {
                        pixels[y * width + x] = plateColor;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, width, height),
                                new Vector2(0.5f, 0.5f), GameSettings.PixelsPerUnit);
        }

        private static Sprite CreateGrateSprite()
        {
            int width = 52;
            int height = 6;
            Texture2D texture = new Texture2D(width, height);
            texture.filterMode = FilterMode.Point;

            Color grateColor = new Color(0.4f, 0.4f, 0.45f);
            Color[] pixels = new Color[width * height];

            // Pattern matches simulation blocking: 3 cells (6 pixels) holes, 3 cells (6 pixels) bars
            // Offset by 6 pixels so holes are at 0-5, 12-17, etc. (matches blocking offset)
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // 6 pixels hole, 6 pixels bar, repeating (offset to match blocking)
                    bool isBar = ((x + 6) % 12) < 6;

                    if (isBar)
                    {
                        pixels[y * width + x] = grateColor;
                    }
                    else
                    {
                        // Transparent holes - you can see through them
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

            crusherCenterX = gridPos.x;
            crusherCenterY = gridPos.y;

            // Big crusher is 64 pixels wide = 32 sim cells wide
            // Interior is about 56 pixels = 28 sim cells (walls are 4 pixels each)
            crusherHalfWidth = 14;  // From center to inner edge of wall
            crusherTop = gridPos.y - 14;
            crusherBottom = gridPos.y + 14;

            // Store visual dimensions
            float cellSize = GameSettings.InfraGridSize / GameSettings.PixelsPerUnit;
            totalWidthWorld = cellSize * 2;
            plateStartOffset = 6f / GameSettings.PixelsPerUnit;

            // Set up grate blocking at the bottom (blocks clusters from falling through)
            // Grate is 3 cells thick at the bottom
            SetGrateBlocking(true);
        }

        private void SetGrateBlocking(bool blocking)
        {
            if (SimulationWorld.Instance == null) return;
            var grid = SimulationWorld.Instance.Grid;

            // Create grate with holes - alternating pattern
            // Holes are 3 cells wide (enough for gravel 2x2)
            // Bars are 3 cells wide (blocks rocks 4x4)
            int grateStartX = crusherCenterX - crusherHalfWidth + 2;
            int grateEndX = crusherCenterX + crusherHalfWidth - 2;

            for (int y = crusherBottom - 2; y <= crusherBottom; y++)
            {
                for (int x = grateStartX; x <= grateEndX; x++)
                {
                    // Create pattern: 3 open, 3 blocked, repeat (offset so center has a hole)
                    int relX = x - grateStartX;
                    bool isBar = ((relX + 3) % 6) < 3;  // Offset by 3 so holes are at 0-2, 6-8, 12-14...

                    if (isBar)
                    {
                        grid.SetInfrastructureBlocking(x, y, blocking);
                    }
                    else if (!blocking)
                    {
                        // When removing, clear any blocking that might exist
                        grid.SetInfrastructureBlocking(x, y, false);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            SetGrateBlocking(false);
        }

        private void Update()
        {
            if (SimulationWorld.Instance == null) return;

            // Handle pause at ends
            if (pauseTimer > 0)
            {
                pauseTimer -= Time.deltaTime;
                UpdatePlateVisuals();
                WakeParticlesInArea();
                return;
            }

            // Move plates
            if (platesClosing)
            {
                platePosition += PlateSpeed * Time.deltaTime;
                if (platePosition >= 1f)
                {
                    platePosition = 1f;
                    platesClosing = false;
                    pauseTimer = PauseAtEnds;
                }
            }
            else
            {
                platePosition -= PlateSpeed * Time.deltaTime;
                if (platePosition <= 0f)
                {
                    platePosition = 0f;
                    platesClosing = true;
                    pauseTimer = PauseAtEnds;
                }
            }

            // Process physical interactions
            if (platesClosing)
            {
                ProcessPlatePhysics();
            }

            // Update visuals
            UpdatePlateVisuals();

            // Keep particles awake
            WakeParticlesInArea();
        }

        private void ProcessPlatePhysics()
        {
            var grid = SimulationWorld.Instance.Grid;
            var clusterMgr = grid.ClusterManager;

            // Calculate current plate positions in grid coordinates
            // At platePosition=0: plates at edges (crusherHalfWidth from center)
            // At platePosition=1: plates meet in middle (0 from center)
            int maxTravel = crusherHalfWidth - PlateThickness;
            int currentTravel = Mathf.RoundToInt(platePosition * maxTravel);

            int leftPlateInnerEdge = crusherCenterX - crusherHalfWidth + currentTravel + PlateThickness;
            int rightPlateInnerEdge = crusherCenterX + crusherHalfWidth - currentTravel - PlateThickness;

            // Find all clusters in the crusher
            HashSet<uint> processedClusters = new HashSet<uint>();

            for (int y = crusherTop; y <= crusherBottom; y++)
            {
                for (int x = crusherCenterX - crusherHalfWidth; x <= crusherCenterX + crusherHalfWidth; x++)
                {
                    uint clusterId = grid.GetClusterID(x, y);
                    if (clusterId != 0 && !processedClusters.Contains(clusterId))
                    {
                        processedClusters.Add(clusterId);
                        ProcessClusterWithPlates(clusterId, leftPlateInnerEdge, rightPlateInnerEdge, clusterMgr);
                    }
                }
            }
        }

        private void ProcessClusterWithPlates(uint clusterId, int leftPlateEdge, int rightPlateEdge, ClusterManager clusterMgr)
        {
            var clusterData = clusterMgr.GetCluster(clusterId);
            if (!clusterData.HasValue) return;

            var cluster = clusterData.Value;
            int clusterLeft = cluster.OriginX;
            int clusterRight = cluster.OriginX + cluster.Size - 1;

            // Check if cluster is being touched by plates
            bool touchedByLeft = clusterLeft <= leftPlateEdge;
            bool touchedByRight = clusterRight >= rightPlateEdge;

            if (touchedByLeft && touchedByRight)
            {
                // Squeezed by both plates - CRUSH IT!
                CrushCluster(clusterId, cluster, clusterMgr);
                return;
            }

            // Push cluster if touched by one plate
            if (touchedByLeft && !touchedByRight)
            {
                // Left plate is pushing - try to move cluster right
                int pushAmount = leftPlateEdge - clusterLeft + 1;
                for (int i = 0; i < pushAmount; i++)
                {
                    if (!clusterMgr.TryMoveCluster(clusterId, 1, 0))
                    {
                        // Can't move - check if blocked by right plate
                        var updatedCluster = clusterMgr.GetCluster(clusterId);
                        if (updatedCluster.HasValue)
                        {
                            int newRight = updatedCluster.Value.OriginX + updatedCluster.Value.Size - 1;
                            if (newRight >= rightPlateEdge - 1)
                            {
                                // Blocked by right plate - crush!
                                CrushCluster(clusterId, updatedCluster.Value, clusterMgr);
                            }
                        }
                        break;
                    }
                }
            }
            else if (touchedByRight && !touchedByLeft)
            {
                // Right plate is pushing - try to move cluster left
                int pushAmount = clusterRight - rightPlateEdge + 1;
                for (int i = 0; i < pushAmount; i++)
                {
                    if (!clusterMgr.TryMoveCluster(clusterId, -1, 0))
                    {
                        // Can't move - check if blocked by left plate
                        var updatedCluster = clusterMgr.GetCluster(clusterId);
                        if (updatedCluster.HasValue)
                        {
                            int newLeft = updatedCluster.Value.OriginX;
                            if (newLeft <= leftPlateEdge + 1)
                            {
                                // Blocked by left plate - crush!
                                CrushCluster(clusterId, updatedCluster.Value, clusterMgr);
                            }
                        }
                        break;
                    }
                }
            }
        }

        private void CrushCluster(uint clusterId, ClusterData cluster, ClusterManager clusterMgr)
        {
            // Boulder (8x8) -> Rock (4x4)
            // Rock (4x4) -> Gravel (2x2)
            // Gravel falls through holes - NOT crushed by big crusher
            if (cluster.Type == MaterialType.Boulder && cluster.Size == 8)
            {
                clusterMgr.BreakCluster(clusterId, 4, MaterialType.Rock);
            }
            else if (cluster.Type == MaterialType.Rock && cluster.Size == 4)
            {
                clusterMgr.BreakCluster(clusterId, 2, MaterialType.Gravel);
            }
            // Gravel (2x2) - do nothing, let it fall through holes
        }

        private void UpdatePlateVisuals()
        {
            // Calculate visual plate travel
            // At platePosition=0: plates at starting position
            // At platePosition=1: plates meet in middle
            float maxTravelPixels = (crusherHalfWidth - PlateThickness) * SimulationWorld.CellPixelSize;
            float currentTravelWorld = (platePosition * maxTravelPixels) / GameSettings.PixelsPerUnit;

            if (leftPlate != null)
            {
                leftPlate.localPosition = new Vector3(
                    -totalWidthWorld / 2 + plateStartOffset + currentTravelWorld,
                    0, 0);
            }
            if (rightPlate != null)
            {
                rightPlate.localPosition = new Vector3(
                    totalWidthWorld / 2 - plateStartOffset - currentTravelWorld,
                    0, 0);
            }
        }

        private void WakeParticlesInArea()
        {
            var grid = SimulationWorld.Instance.Grid;

            for (int y = crusherTop; y <= crusherBottom; y++)
            {
                for (int x = crusherCenterX - crusherHalfWidth; x <= crusherCenterX + crusherHalfWidth; x++)
                {
                    if (MaterialProperties.IsSimulated(grid.Get(x, y)))
                    {
                        grid.WakeCell(x, y);
                    }

                    uint clusterId = grid.GetClusterID(x, y);
                    if (clusterId != 0)
                    {
                        grid.ClusterManager.WakeCluster(clusterId);
                    }
                }
            }
        }
    }
}
