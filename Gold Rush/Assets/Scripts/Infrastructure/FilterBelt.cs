using UnityEngine;
using GoldRush.Core;
using GoldRush.Building;
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

        private int simGridMinX, simGridMaxX, simGridY;
        private SpriteRenderer spriteRenderer;
        private GameObject filterIndicator;
        private SpriteRenderer indicatorRenderer;

        public static GameObject Create(int gridX, int gridY, bool movesRight, Transform parent = null, HashSet<MaterialType> blockedMaterials = null)
        {
            GameObject beltGO = new GameObject($"FilterBelt_{gridX}_{gridY}");
            if (parent != null) beltGO.transform.SetParent(parent);

            var info = BuildTypeData.Get(BuildType.FilterBelt);
            Vector2 worldPos = info.Grid.ToWorld(gridX, gridY);
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

            var info = BuildTypeData.Get(BuildType.FilterBelt);
            simGridMinX = gridPos.x - info.SimHalfWidth;
            simGridMaxX = gridPos.x + info.SimHalfWidth;
            simGridY = gridPos.y;

            // Block interior cells (same as Belt.cs)
            var grid = SimulationWorld.Instance.Grid;
            for (int x = simGridMinX; x <= simGridMaxX; x++)
            {
                for (int dy = 1; dy <= 8; dy++)
                {
                    grid.SetInfrastructureBlocking(x, simGridY + dy, true);
                }
            }

            // Register belt surface with filter
            BeltSurface surface = new BeltSurface
            {
                MinX = simGridMinX,
                MaxX = simGridMaxX,
                SurfaceY = simGridY,
                MovesRight = MovesRight,
                Owner = this,
                BlockedMaterials = BlockedMaterials
            };
            BeltVelocityManager.Instance.RegisterBeltSurface(surface);
        }

        private void OnDestroy()
        {
            BeltVelocityManager.Instance?.UnregisterBeltSurface(this);

            if (SimulationWorld.Instance != null)
            {
                var grid = SimulationWorld.Instance.Grid;
                for (int x = simGridMinX; x <= simGridMaxX; x++)
                {
                    for (int dy = 1; dy <= 8; dy++)
                    {
                        grid.SetInfrastructureBlocking(x, simGridY + dy, false);
                    }
                }
            }
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
    }
}
