using UnityEngine;
using GoldRush.Core;
using GoldRush.Simulation;
using System.Collections.Generic;

namespace GoldRush.Infrastructure
{
    public class SmallCrusher : MonoBehaviour
    {
        public int GridX { get; private set; }
        public int GridY { get; private set; }

        // Plate movement
        private float platePosition;  // 0 = fully open, 1 = fully closed
        private bool platesClosing = true;
        private const float PlateSpeed = 0.4f;  // Slightly faster than big crusher
        private const float PauseAtEnds = 0.25f;
        private float pauseTimer;

        // Crusher dimensions in simulation grid cells
        private int crusherCenterX;
        private int crusherCenterY;
        private int crusherHalfWidth;
        private int crusherTop;
        private int crusherBottom;

        private const int PlateThickness = 2;

        private Transform leftPlate;
        private Transform rightPlate;

        // Visual dimensions
        private float totalWidthWorld;
        private float plateStartOffset;

        public static GameObject Create(int gridX, int gridY, Transform parent = null)
        {
            GameObject crusherGO = new GameObject($"SmallCrusher_{gridX}_{gridY}");
            if (parent != null) crusherGO.transform.SetParent(parent);

            // Position using infra grid (32x32 pixel cells)
            Vector2 worldPos = GameSettings.InfraGridToWorld(gridX, gridY);
            crusherGO.transform.position = worldPos;

            // Create visual components
            CreateVisuals(crusherGO);

            // Layer
            crusherGO.layer = LayerSetup.InfrastructureLayer;

            // Component
            SmallCrusher crusher = crusherGO.AddComponent<SmallCrusher>();
            crusher.GridX = gridX;
            crusher.GridY = gridY;
            crusher.leftPlate = crusherGO.transform.Find("LeftPlate");
            crusher.rightPlate = crusherGO.transform.Find("RightPlate");
            crusher.InitializeGridCoords();

            return crusherGO;
        }

        private static void CreateVisuals(GameObject parent)
        {
            float cellSize = GameSettings.InfraGridSize / GameSettings.PixelsPerUnit;

            // Frame (32x32 pixels)
            SpriteRenderer frameSR = parent.AddComponent<SpriteRenderer>();
            frameSR.sprite = CreateFrameSprite();
            frameSR.sortingOrder = 8;

            // Left plate (movable)
            GameObject leftGO = new GameObject("LeftPlate");
            leftGO.transform.SetParent(parent.transform);
            leftGO.transform.localPosition = new Vector3(-cellSize / 2 + 3f / GameSettings.PixelsPerUnit, 0, 0);
            SpriteRenderer leftSR = leftGO.AddComponent<SpriteRenderer>();
            leftSR.sprite = CreatePlateSprite();
            leftSR.sortingOrder = 9;

            // Right plate (movable)
            GameObject rightGO = new GameObject("RightPlate");
            rightGO.transform.SetParent(parent.transform);
            rightGO.transform.localPosition = new Vector3(cellSize / 2 - 3f / GameSettings.PixelsPerUnit, 0, 0);
            SpriteRenderer rightSR = rightGO.AddComponent<SpriteRenderer>();
            rightSR.sprite = CreatePlateSprite();
            rightSR.flipX = true;
            rightSR.sortingOrder = 9;

            // Side wall colliders
            float wallThickness = 3f / GameSettings.PixelsPerUnit;

            // Left wall
            GameObject leftWall = new GameObject("LeftWall");
            leftWall.transform.SetParent(parent.transform);
            leftWall.transform.localPosition = new Vector3(-cellSize / 2, 0, 0);
            BoxCollider2D leftCol = leftWall.AddComponent<BoxCollider2D>();
            leftCol.size = new Vector2(wallThickness, cellSize);
            leftWall.layer = LayerSetup.InfrastructureLayer;

            // Right wall
            GameObject rightWall = new GameObject("RightWall");
            rightWall.transform.SetParent(parent.transform);
            rightWall.transform.localPosition = new Vector3(cellSize / 2, 0, 0);
            BoxCollider2D rightCol = rightWall.AddComponent<BoxCollider2D>();
            rightCol.size = new Vector2(wallThickness, cellSize);
            rightWall.layer = LayerSetup.InfrastructureLayer;

            // Bottom grate
            GameObject grate = new GameObject("Grate");
            grate.transform.SetParent(parent.transform);
            grate.transform.localPosition = new Vector3(0, -cellSize / 2 + 2f / GameSettings.PixelsPerUnit, 0);
            SpriteRenderer grateSR = grate.AddComponent<SpriteRenderer>();
            grateSR.sprite = CreateGrateSprite();
            grateSR.sortingOrder = 10;
        }

        private static Sprite CreateFrameSprite()
        {
            int size = GameSettings.InfraGridSize;  // 32 pixels
            Texture2D texture = new Texture2D(size, size);
            texture.filterMode = FilterMode.Point;

            Color frameColor = new Color(0.4f, 0.35f, 0.35f);
            Color darkColor = new Color(0.25f, 0.2f, 0.2f);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (x < 2 || x >= size - 2)
                    {
                        pixels[y * size + x] = frameColor;
                    }
                    else if (y >= size - 2)
                    {
                        pixels[y * size + x] = darkColor;
                    }
                    else if (y < 3)
                    {
                        pixels[y * size + x] = darkColor;
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, size, size),
                                new Vector2(0.5f, 0.5f), GameSettings.PixelsPerUnit);
        }

        private static Sprite CreatePlateSprite()
        {
            int width = 6;
            int height = 20;
            Texture2D texture = new Texture2D(width, height);
            texture.filterMode = FilterMode.Point;

            Color plateColor = new Color(0.55f, 0.5f, 0.5f);
            Color highlightColor = new Color(0.65f, 0.6f, 0.6f);
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
            int width = 26;
            int height = 3;
            Texture2D texture = new Texture2D(width, height);
            texture.filterMode = FilterMode.Point;

            Color grateColor = new Color(0.45f, 0.4f, 0.4f);
            Color holeColor = new Color(0.15f, 0.12f, 0.12f);
            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if ((x % 3) < 1 && y == 1)
                    {
                        pixels[y * width + x] = holeColor;
                    }
                    else
                    {
                        pixels[y * width + x] = grateColor;
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

            // Small crusher is 32 pixels wide = 16 sim cells wide
            // Interior is about 28 pixels = 14 sim cells
            crusherHalfWidth = 7;
            crusherTop = gridPos.y - 7;
            crusherBottom = gridPos.y + 7;

            // Store visual dimensions
            float cellSize = GameSettings.InfraGridSize / GameSettings.PixelsPerUnit;
            totalWidthWorld = cellSize;
            plateStartOffset = 3f / GameSettings.PixelsPerUnit;

            // Set up grate blocking at the bottom (blocks clusters from falling through)
            // Grate is 2 cells thick at the bottom
            SetGrateBlocking(true);
        }

        private void SetGrateBlocking(bool blocking)
        {
            if (SimulationWorld.Instance == null) return;
            var grid = SimulationWorld.Instance.Grid;

            // Block the bottom 2 rows of cells (the grate area)
            for (int y = crusherBottom - 1; y <= crusherBottom; y++)
            {
                for (int x = crusherCenterX - crusherHalfWidth + 1; x <= crusherCenterX + crusherHalfWidth - 1; x++)
                {
                    grid.SetInfrastructureBlocking(x, y, blocking);
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
                int pushAmount = leftPlateEdge - clusterLeft + 1;
                for (int i = 0; i < pushAmount; i++)
                {
                    if (!clusterMgr.TryMoveCluster(clusterId, 1, 0))
                    {
                        var updatedCluster = clusterMgr.GetCluster(clusterId);
                        if (updatedCluster.HasValue)
                        {
                            int newRight = updatedCluster.Value.OriginX + updatedCluster.Value.Size - 1;
                            if (newRight >= rightPlateEdge - 1)
                            {
                                CrushCluster(clusterId, updatedCluster.Value, clusterMgr);
                            }
                        }
                        break;
                    }
                }
            }
            else if (touchedByRight && !touchedByLeft)
            {
                int pushAmount = clusterRight - rightPlateEdge + 1;
                for (int i = 0; i < pushAmount; i++)
                {
                    if (!clusterMgr.TryMoveCluster(clusterId, -1, 0))
                    {
                        var updatedCluster = clusterMgr.GetCluster(clusterId);
                        if (updatedCluster.HasValue)
                        {
                            int newLeft = updatedCluster.Value.OriginX;
                            if (newLeft <= leftPlateEdge + 1)
                            {
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
            // Rock (4x4) -> Gravel (2x2)
            // Gravel (2x2) -> Sand (1x1)
            // Boulder shouldn't fit in small crusher, but handle anyway
            if (cluster.Type == MaterialType.Boulder && cluster.Size == 8)
            {
                // Boulder too big for small crusher - break to rocks
                clusterMgr.BreakCluster(clusterId, 4, MaterialType.Rock);
            }
            else if (cluster.Type == MaterialType.Rock && cluster.Size == 4)
            {
                clusterMgr.BreakCluster(clusterId, 2, MaterialType.Gravel);
            }
            else if (cluster.Type == MaterialType.Gravel && cluster.Size == 2)
            {
                clusterMgr.BreakCluster(clusterId, 1, MaterialType.Sand);
            }
            else
            {
                clusterMgr.BreakCluster(clusterId, 1, MaterialType.Sand);
            }
        }

        private void UpdatePlateVisuals()
        {
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

            // Let single-cell materials (sand, gold, etc.) fall through the grate
            ProcessMaterialsThroughGrate();
        }

        private void ProcessMaterialsThroughGrate()
        {
            var grid = SimulationWorld.Instance.Grid;

            // Check the row just above the grate for single-cell materials
            int grateTopY = crusherBottom - 2;  // Just above the grate blocking

            for (int x = crusherCenterX - crusherHalfWidth + 1; x <= crusherCenterX + crusherHalfWidth - 1; x++)
            {
                MaterialType type = grid.Get(x, grateTopY);

                // Only process single-cell materials (not clusters, not air, not terrain)
                if (type != MaterialType.Air && type != MaterialType.Terrain &&
                    !MaterialProperties.IsClusterMaterial(type) &&
                    grid.GetClusterID(x, grateTopY) == 0)
                {
                    // Find a spot below the grate to place this material
                    int belowY = crusherBottom + 1;
                    if (grid.InBounds(x, belowY) && grid.Get(x, belowY) == MaterialType.Air)
                    {
                        // Move the material through the grate
                        grid.Set(x, grateTopY, MaterialType.Air);
                        grid.Set(x, belowY, type);
                        grid.WakeCell(x, belowY);
                    }
                }
            }
        }
    }
}
