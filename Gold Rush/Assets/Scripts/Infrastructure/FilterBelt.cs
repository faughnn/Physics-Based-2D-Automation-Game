using UnityEngine;
using UnityEngine.UI;
using GoldRush.Core;
using GoldRush.Simulation;
using System.Collections.Generic;

namespace GoldRush.Infrastructure
{
    public class FilterBelt : MonoBehaviour
    {
        public int GridX { get; private set; }
        public int GridY { get; private set; }
        public bool MovesRight { get; private set; }

        // Set of materials that are BLOCKED (cannot pass through)
        public HashSet<MaterialType> BlockedMaterials { get; private set; } = new HashSet<MaterialType>();

        private float moveTimer;
        private const float MoveInterval = 0.05f;

        private int simGridMinX, simGridMaxX, simGridY;
        private SpriteRenderer spriteRenderer;
        private GameObject filterIndicator;
        private SpriteRenderer indicatorRenderer;

        public static GameObject Create(int gridX, int gridY, bool movesRight, Transform parent = null, HashSet<MaterialType> blockedMaterials = null)
        {
            GameObject beltGO = new GameObject($"FilterBelt_{gridX}_{gridY}");
            if (parent != null) beltGO.transform.SetParent(parent);

            Vector2 worldPos = GameSettings.SubGridToWorld(gridX, gridY);
            beltGO.transform.position = worldPos;

            // Main sprite - darker color to distinguish from regular belt
            SpriteRenderer sr = beltGO.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteGenerator.CreateArrowSprite(GameSettings.BeltSize, GameSettings.BeltSize,
                new Color(0.3f, 0.3f, 0.4f), true, movesRight);
            sr.sortingOrder = 8;

            // Filter indicator (small squares showing blocked material colors)
            GameObject indicator = new GameObject("FilterIndicator");
            indicator.transform.SetParent(beltGO.transform);
            indicator.transform.localPosition = Vector3.zero;
            SpriteRenderer indicatorSR = indicator.AddComponent<SpriteRenderer>();
            indicatorSR.sortingOrder = 9;

            // Trigger collider
            float size = GameSettings.BeltSize / GameSettings.PixelsPerUnit;
            BoxCollider2D col = beltGO.AddComponent<BoxCollider2D>();
            col.size = new Vector2(size, size);
            col.isTrigger = true;

            // Layer
            beltGO.layer = LayerSetup.InfrastructureLayer;

            // Component
            FilterBelt belt = beltGO.AddComponent<FilterBelt>();
            belt.GridX = gridX;
            belt.GridY = gridY;
            belt.MovesRight = movesRight;
            belt.spriteRenderer = sr;
            belt.filterIndicator = indicator;
            belt.indicatorRenderer = indicatorSR;

            // Set blocked materials (default to Gold if none specified)
            if (blockedMaterials != null && blockedMaterials.Count > 0)
            {
                belt.BlockedMaterials = new HashSet<MaterialType>(blockedMaterials);
            }
            else
            {
                belt.BlockedMaterials.Add(MaterialType.Gold);
            }

            belt.UpdateIndicator();
            belt.InitializeGridCoords();

            return beltGO;
        }

        private void InitializeGridCoords()
        {
            if (SimulationWorld.Instance == null) return;

            Vector2 surfacePos = (Vector2)transform.position + new Vector2(0, 9f / GameSettings.PixelsPerUnit);
            Vector2Int gridPos = SimulationWorld.Instance.WorldToGrid(surfacePos);

            int halfWidth = 4;
            simGridMinX = gridPos.x - halfWidth;
            simGridMaxX = gridPos.x + halfWidth;
            simGridY = gridPos.y;

            UpdateFilterBlocking();
        }

        private void UpdateFilterBlocking()
        {
            // Filter belt doesn't use grid blocking - it handles materials directly in Update
        }

        private void ClearFilterBlocking()
        {
            // Nothing to clear since we handle it in Update
        }

        private void OnDestroy()
        {
            ClearFilterBlocking();
        }

        private void UpdateIndicator()
        {
            if (indicatorRenderer == null) return;

            // Create indicator showing blocked material colors as vertical stripes
            int count = BlockedMaterials.Count;
            if (count == 0)
            {
                indicatorRenderer.sprite = null;
                return;
            }

            int size = 10;
            Texture2D tex = new Texture2D(size, size);
            tex.filterMode = FilterMode.Point;
            Color32[] pixels = new Color32[size * size];

            // Get colors for blocked materials
            List<Color32> colors = new List<Color32>();
            foreach (var mat in BlockedMaterials)
            {
                colors.Add(MaterialProperties.GetColor(mat));
            }

            // Draw vertical stripes for each blocked material
            int stripeWidth = Mathf.Max(1, size / count);
            for (int x = 0; x < size; x++)
            {
                int colorIndex = Mathf.Min(x / stripeWidth, colors.Count - 1);
                Color32 color = colors[colorIndex];

                for (int y = 0; y < size; y++)
                {
                    pixels[y * size + x] = color;
                }
            }

            // Add white border
            for (int i = 0; i < size; i++)
            {
                pixels[i] = new Color32(255, 255, 255, 255); // Bottom
                pixels[(size - 1) * size + i] = new Color32(255, 255, 255, 255); // Top
                pixels[i * size] = new Color32(255, 255, 255, 255); // Left
                pixels[i * size + size - 1] = new Color32(255, 255, 255, 255); // Right
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            indicatorRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), GameSettings.PixelsPerUnit);
        }

        private HashSet<uint> processedClusters = new HashSet<uint>();
        private const float BeltVelocity = 2f;

        private void Update()
        {
            if (SimulationWorld.Instance == null) return;

            // Move blocked materials horizontally on the belt
            moveTimer += Time.deltaTime;
            if (moveTimer < MoveInterval) return;
            moveTimer = 0f;

            var grid = SimulationWorld.Instance.Grid;
            var clusterMgr = grid.ClusterManager;

            int direction = MovesRight ? 1 : -1;
            float targetVelX = MovesRight ? BeltVelocity : -BeltVelocity;
            int startX = MovesRight ? simGridMaxX : simGridMinX;
            int endX = MovesRight ? simGridMinX : simGridMaxX;
            int step = MovesRight ? -1 : 1;

            // Track which clusters we've already processed this frame
            processedClusters.Clear();

            for (int x = startX; MovesRight ? x >= endX : x <= endX; x += step)
            {
                for (int dy = -8; dy <= 0; dy++)
                {
                    int y = simGridY + dy;

                    // Check for cluster first
                    uint clusterId = grid.GetClusterID(x, y);
                    if (clusterId != 0)
                    {
                        // Only process each cluster once
                        if (!processedClusters.Contains(clusterId))
                        {
                            processedClusters.Add(clusterId);

                            // Get cluster data - only move if cluster's material is blocked
                            var clusterData = clusterMgr.GetCluster(clusterId);
                            if (clusterData.HasValue && BlockedMaterials.Contains(clusterData.Value.Type))
                            {
                                Vector2 vel = clusterData.Value.Velocity;
                                vel.x = Mathf.MoveTowards(vel.x, targetVelX, BeltVelocity);
                                clusterMgr.SetClusterVelocity(clusterId, vel);
                            }
                        }
                        continue;
                    }

                    MaterialType type = grid.Get(x, y);

                    // Move materials that are in the blocked set
                    if (BlockedMaterials.Contains(type))
                    {
                        int newX = x + direction;
                        if (grid.Get(newX, y) == MaterialType.Air)
                        {
                            grid.Set(x, y, MaterialType.Air);
                            grid.Set(newX, y, type);
                        }
                    }
                }
            }
        }

    }
}
