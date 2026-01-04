using UnityEngine;
using System.Collections.Generic;
using GoldRush.Core;
using GoldRush.Simulation;

namespace GoldRush.Infrastructure
{
    public class Belt : MonoBehaviour
    {
        public int GridX { get; private set; }
        public int GridY { get; private set; }
        public bool MovesRight { get; private set; }

        private float speed;
        private float moveTimer;
        private const float MoveInterval = 0.05f;  // Move cells every 50ms

        // Grid coordinates for this belt's area
        private int simGridMinX, simGridMaxX, simGridY;

        public static GameObject Create(int gridX, int gridY, bool movesRight, Transform parent = null)
        {
            GameObject beltGO = new GameObject($"Belt_{gridX}_{gridY}");
            if (parent != null) beltGO.transform.SetParent(parent);

            // Position using sub-grid (16x16 pixel cells)
            Vector2 worldPos = GameSettings.SubGridToWorld(gridX, gridY);
            beltGO.transform.position = worldPos;

            // Sprite with arrow (16x16 pixels)
            SpriteRenderer sr = beltGO.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteGenerator.CreateArrowSprite(GameSettings.BeltSize, GameSettings.BeltSize, GameSettings.BeltColor, true, movesRight);
            sr.sortingOrder = 8;  // Above terrain (simulation=5) but below player (10)

            // Trigger collider (16x16 pixels)
            float size = GameSettings.BeltSize / GameSettings.PixelsPerUnit;
            BoxCollider2D col = beltGO.AddComponent<BoxCollider2D>();
            col.size = new Vector2(size, size);
            col.isTrigger = true;

            // Solid top surface (16x2 pixels, positioned at top of belt)
            GameObject solidPart = new GameObject("SolidTop");
            solidPart.transform.SetParent(beltGO.transform);
            solidPart.transform.localPosition = new Vector3(0, 8f / GameSettings.PixelsPerUnit, 0);
            BoxCollider2D solidCol = solidPart.AddComponent<BoxCollider2D>();
            solidCol.size = new Vector2(size, 2f / GameSettings.PixelsPerUnit);
            solidPart.layer = LayerSetup.InfrastructureLayer;

            // Layer
            beltGO.layer = LayerSetup.InfrastructureLayer;

            // Component
            Belt belt = beltGO.AddComponent<Belt>();
            belt.GridX = gridX;
            belt.GridY = gridY;
            belt.MovesRight = movesRight;
            belt.speed = movesRight ? GameSettings.BeltSpeed : -GameSettings.BeltSpeed;
            belt.InitializeGridCoords();

            return beltGO;
        }

        private void InitializeGridCoords()
        {
            if (SimulationWorld.Instance == null) return;

            // Get the surface position ABOVE the belt in world coords
            // Belt is 16x16 pixels (top at +8), particles rest on top (+9 to clear visual overlap)
            Vector2 surfacePos = (Vector2)transform.position + new Vector2(0, 9f / GameSettings.PixelsPerUnit);
            Vector2Int gridPos = SimulationWorld.Instance.WorldToGrid(surfacePos);

            // Belt covers about 8 simulation cells wide (16 pixels / 2)
            int halfWidth = 4;
            simGridMinX = gridPos.x - halfWidth;
            simGridMaxX = gridPos.x + halfWidth;
            simGridY = gridPos.y;  // Surface level in grid coords

            // Register blocking cells - block the interior of the belt (below surface)
            var grid = SimulationWorld.Instance.Grid;
            for (int x = simGridMinX; x <= simGridMaxX; x++)
            {
                // Block 8 rows inside the belt (16 pixels / 2)
                for (int dy = 1; dy <= 8; dy++)
                {
                    grid.SetInfrastructureBlocking(x, simGridY + dy, true);
                }
            }
        }

        private void OnDestroy()
        {
            // Unregister blocking cells when belt is destroyed
            if (SimulationWorld.Instance == null) return;
            var grid = SimulationWorld.Instance.Grid;

            for (int x = simGridMinX; x <= simGridMaxX; x++)
            {
                // Unblock 8 rows (16 pixels / 2)
                for (int dy = 1; dy <= 8; dy++)
                {
                    grid.SetInfrastructureBlocking(x, simGridY + dy, false);
                }
            }
        }

        private HashSet<uint> processedClusters = new HashSet<uint>();
        private const float BeltVelocity = 2f;  // Horizontal velocity to apply to clusters

        private void Update()
        {
            if (SimulationWorld.Instance == null) return;

            // Move particles horizontally on timer
            moveTimer += Time.deltaTime;
            if (moveTimer < MoveInterval) return;
            moveTimer = 0f;

            var grid = SimulationWorld.Instance.Grid;
            var clusterMgr = grid.ClusterManager;

            int direction = MovesRight ? 1 : -1;
            float targetVelX = MovesRight ? BeltVelocity : -BeltVelocity;

            // Track which clusters we've already processed this frame
            processedClusters.Clear();

            // Process in the direction of movement to avoid double-moving
            int startX = MovesRight ? simGridMaxX : simGridMinX;
            int endX = MovesRight ? simGridMinX : simGridMaxX;
            int step = MovesRight ? -1 : 1;

            for (int x = startX; MovesRight ? x >= endX : x <= endX; x += step)
            {
                // Check cells at and above the surface - allow stacking up to 16 pixels (8 sim cells)
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

                            // Get cluster data and apply horizontal velocity
                            var clusterData = clusterMgr.GetCluster(clusterId);
                            if (clusterData.HasValue)
                            {
                                Vector2 vel = clusterData.Value.Velocity;
                                // Set horizontal velocity toward belt direction
                                vel.x = Mathf.MoveTowards(vel.x, targetVelX, BeltVelocity);
                                clusterMgr.SetClusterVelocity(clusterId, vel);
                            }
                        }
                        continue;  // Skip single-cell processing for clustered cells
                    }

                    MaterialType type = grid.Get(x, y);

                    if (MaterialProperties.IsSimulated(type))
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
