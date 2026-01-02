using UnityEngine;
using System.Collections.Generic;
using GoldRush.Core;
using GoldRush.Simulation;

namespace GoldRush.Infrastructure
{
    public class Shaker : MonoBehaviour
    {
        public int GridX { get; private set; }
        public int GridY { get; private set; }
        public bool PushesRight { get; private set; }

        private float shakeAmount = 0.02f;
        private float shakeSpeed = 30f;
        private Vector2 originalPosition;

        private float surfaceTimer;
        private const float SurfaceProcessInterval = 0.1f;  // How often to process surface (push slag/sand)

        private float fallTimer;  // Timer for wet sand falling through shaker

        // Grid coordinates for this shaker's area
        private int simGridMinX, simGridMaxX, simGridY;
        private const int ShakerBodyDepth = 8;  // 8 cells deep (16 pixels)

        // Track fall time for each wet sand cell inside the shaker body
        private Dictionary<int, float> fallingCells = new Dictionary<int, float>();

        public static GameObject Create(int gridX, int gridY, bool pushesRight, Transform parent = null)
        {
            GameObject shakerGO = new GameObject($"Shaker_{gridX}_{gridY}");
            if (parent != null) shakerGO.transform.SetParent(parent);

            // Position using shaker grid (32x16 half-cells)
            // gridY: even = top half, odd = bottom half of main cell
            Vector2 worldPos = GameSettings.ShakerGridToWorld(gridX, gridY);
            shakerGO.transform.position = worldPos;

            // Sprite
            SpriteRenderer sr = shakerGO.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteGenerator.CreateArrowSprite(GameSettings.GridSize, 16, GameSettings.ShakerColor, true, pushesRight);
            sr.sortingOrder = 8;  // Above terrain (simulation=5) but below player (10)

            // Trigger collider for detecting particles
            BoxCollider2D triggerCol = shakerGO.AddComponent<BoxCollider2D>();
            triggerCol.size = new Vector2(GameSettings.GridSize / GameSettings.PixelsPerUnit, 16f / GameSettings.PixelsPerUnit);
            triggerCol.isTrigger = true;

            // Solid top surface for particles to rest on
            GameObject solidTop = new GameObject("SolidTop");
            solidTop.transform.SetParent(shakerGO.transform);
            solidTop.transform.localPosition = new Vector3(0, 9f / GameSettings.PixelsPerUnit, 0);
            BoxCollider2D solidCol = solidTop.AddComponent<BoxCollider2D>();
            solidCol.size = new Vector2(GameSettings.GridSize / GameSettings.PixelsPerUnit, 2f / GameSettings.PixelsPerUnit);
            solidTop.layer = LayerSetup.InfrastructureLayer;

            // Layer
            shakerGO.layer = LayerSetup.InfrastructureLayer;

            // Component
            Shaker shaker = shakerGO.AddComponent<Shaker>();
            shaker.GridX = gridX;
            shaker.GridY = gridY;
            shaker.PushesRight = pushesRight;
            shaker.originalPosition = worldPos;
            shaker.InitializeGridCoords();

            return shakerGO;
        }

        private void InitializeGridCoords()
        {
            if (SimulationWorld.Instance == null) return;

            // Get the surface position ABOVE the shaker in world coords
            // Shaker is 32x16 (top at +8 pixels), particles rest on top (+9 to clear visual overlap)
            Vector2 surfacePos = (Vector2)transform.position + new Vector2(0, 9f / GameSettings.PixelsPerUnit);
            Vector2Int gridPos = SimulationWorld.Instance.WorldToGrid(surfacePos);

            // Shaker is 32 pixels wide = 16 simulation cells
            int halfWidth = 8;
            simGridMinX = gridPos.x - halfWidth;
            simGridMaxX = gridPos.x + halfWidth;
            simGridY = gridPos.y;  // Surface level in grid coords

            // Register shaker mesh blocking for shaker body - blocks everything EXCEPT gold
            // Gold falls straight through, wet sand is manually moved through
            var grid = SimulationWorld.Instance.Grid;
            for (int x = simGridMinX; x <= simGridMaxX; x++)
            {
                for (int dy = 1; dy <= ShakerBodyDepth; dy++)
                {
                    grid.SetShakerMeshBlocking(x, simGridY + dy, true);
                }
            }
        }

        private void OnDestroy()
        {
            // Unregister blocking when shaker is destroyed
            if (SimulationWorld.Instance == null) return;
            var grid = SimulationWorld.Instance.Grid;

            for (int x = simGridMinX; x <= simGridMaxX; x++)
            {
                for (int dy = 1; dy <= ShakerBodyDepth; dy++)
                {
                    grid.SetShakerMeshBlocking(x, simGridY + dy, false);
                }
            }
        }

        private const int WakeZoneBuffer = 8;  // Wake zone extends 8 cells beyond infrastructure

        private void Update()
        {
            // Vibrate effect
            float xOffset = Mathf.Sin(Time.time * shakeSpeed) * shakeAmount;
            transform.position = originalPosition + new Vector2(xOffset, 0);

            if (SimulationWorld.Instance == null) return;
            var grid = SimulationWorld.Instance.Grid;

            // Wake all particles in and around the shaker (ActiveSet optimization)
            // BUT skip particles inside the shaker body - we move those manually straight down
            for (int y = simGridY - 16 - WakeZoneBuffer; y <= simGridY + ShakerBodyDepth + WakeZoneBuffer; y++)
            {
                for (int x = simGridMinX - WakeZoneBuffer; x <= simGridMaxX + WakeZoneBuffer; x++)
                {
                    // Don't wake particles inside the shaker body - ProcessFallingWetSand handles them
                    bool insideShakerBody = (y > simGridY && y <= simGridY + ShakerBodyDepth &&
                                             x >= simGridMinX && x <= simGridMaxX);

                    if (!insideShakerBody && MaterialProperties.IsSimulated(grid.Get(x, y)))
                    {
                        grid.WakeCell(x, y);
                    }
                }
            }

            // Process surface (push slag/sand, intake wet sand)
            surfaceTimer += Time.deltaTime;
            if (surfaceTimer >= SurfaceProcessInterval)
            {
                surfaceTimer = 0f;
                ProcessSurface();
            }

            // Process wet sand falling through shaker body
            fallTimer += Time.deltaTime;
            if (fallTimer >= GameSettings.ShakerFallInterval)
            {
                fallTimer = 0f;
                ProcessFallingWetSand();
            }
        }

        // Process material on the shaker surface
        private void ProcessSurface()
        {
            var grid = SimulationWorld.Instance.Grid;
            int direction = PushesRight ? 1 : -1;

            // Process in direction of movement to avoid double-moving
            int startX = PushesRight ? simGridMaxX : simGridMinX;
            int endX = PushesRight ? simGridMinX : simGridMaxX;
            int step = PushesRight ? -1 : 1;

            // Check cells at and above the surface
            for (int x = startX; PushesRight ? x >= endX : x <= endX; x += step)
            {
                for (int dy = -16; dy <= 0; dy++)
                {
                    int y = simGridY + dy;
                    MaterialType type = grid.Get(x, y);

                    if (type == MaterialType.WetSand && dy == 0)
                    {
                        // Wet sand at surface: try to pull into shaker body
                        int bodyTopY = simGridY + 1;
                        if (grid.Get(x, bodyTopY) == MaterialType.Air)
                        {
                            // Space available - spawn slag to side and pull wet sand in
                            int slagX = x + direction;
                            if (grid.Get(slagX, y) == MaterialType.Air)
                            {
                                grid.Set(slagX, y, MaterialType.Slag);
                            }

                            grid.Set(x, y, MaterialType.Air);
                            grid.Set(x, bodyTopY, MaterialType.WetSand);
                        }
                        else
                        {
                            // Body is full - push wet sand sideways like other materials
                            int newX = x + direction;
                            if (grid.Get(newX, y) == MaterialType.Air)
                            {
                                grid.Set(x, y, MaterialType.Air);
                                grid.Set(newX, y, MaterialType.WetSand);
                            }
                        }
                    }
                    else if (type == MaterialType.Slag || type == MaterialType.Sand || type == MaterialType.WetSand)
                    {
                        // Push slag, dry sand, and wet sand (above surface) along the surface
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

        // Process wet sand falling through the shaker body (sieving)
        private void ProcessFallingWetSand()
        {
            var grid = SimulationWorld.Instance.Grid;

            // Process from bottom to top so falling doesn't cascade in one frame
            for (int dy = ShakerBodyDepth; dy >= 1; dy--)
            {
                int y = simGridY + dy;

                for (int x = simGridMinX; x <= simGridMaxX; x++)
                {
                    MaterialType type = grid.Get(x, y);

                    if (type == MaterialType.WetSand)
                    {
                        if (dy == ShakerBodyDepth)
                        {
                            // At bottom of shaker: wet sand becomes gold and exits
                            grid.Set(x, y, MaterialType.Air);

                            // Spawn gold below shaker
                            int goldY = simGridY + ShakerBodyDepth + 1;
                            if (grid.Get(x, goldY) == MaterialType.Air)
                            {
                                grid.Set(x, goldY, MaterialType.Gold);
                            }
                        }
                        else
                        {
                            // Inside shaker body: fall down one cell if space available
                            int belowY = y + 1;
                            if (grid.Get(x, belowY) == MaterialType.Air)
                            {
                                grid.Set(x, y, MaterialType.Air);
                                grid.Set(x, belowY, MaterialType.WetSand);
                            }
                        }
                    }
                }
            }
        }
    }
}
