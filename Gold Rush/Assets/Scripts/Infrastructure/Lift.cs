using UnityEngine;
using System.Collections.Generic;
using GoldRush.Core;
using GoldRush.Simulation;

namespace GoldRush.Infrastructure
{
    public class Lift : MonoBehaviour
    {
        public int GridX { get; private set; }
        public int GridY { get; private set; }
        public bool MovesUp { get; private set; }

        private float accelerateTimer;
        private const float AccelerateInterval = 0.016f;  // Apply acceleration every frame (~60fps)

        // Grid coordinates for this lift's area in simulation grid
        private int simGridMinX, simGridMaxX, simGridMinY, simGridMaxY;

        public static GameObject Create(int gridX, int gridY, bool movesUp, Transform parent = null)
        {
            GameObject liftGO = new GameObject($"Lift_{gridX}_{gridY}");
            if (parent != null) liftGO.transform.SetParent(parent);

            // Position using infra grid (32x32 pixel cells)
            Vector2 worldPos = GameSettings.InfraGridToWorld(gridX, gridY);
            liftGO.transform.position = worldPos;

            // Create hollow visual (32x32 with thin walls)
            CreateHollowVisual(liftGO, movesUp);

            // Create trigger zone for the full area (no walls)
            float size = GameSettings.InfraGridSize / GameSettings.PixelsPerUnit;
            BoxCollider2D triggerCol = liftGO.AddComponent<BoxCollider2D>();
            triggerCol.size = new Vector2(size, size);  // Full size, no walls
            triggerCol.isTrigger = true;

            // Layer
            liftGO.layer = LayerSetup.InfrastructureLayer;

            // Component
            Lift lift = liftGO.AddComponent<Lift>();
            lift.GridX = gridX;
            lift.GridY = gridY;
            lift.MovesUp = movesUp;
            lift.InitializeGridCoords();

            return liftGO;
        }

        private void InitializeGridCoords()
        {
            if (SimulationWorld.Instance == null) return;

            Vector2 worldPos = transform.position;
            Vector2Int gridPos = SimulationWorld.Instance.WorldToGrid(worldPos);

            // Lift is 32x32 pixels = 16x16 simulation cells
            int halfSize = 8;
            simGridMinX = gridPos.x - halfSize;
            simGridMaxX = gridPos.x + halfSize;
            simGridMinY = gridPos.y - halfSize;
            simGridMaxY = gridPos.y + halfSize;

            // No wall blocking - lift is open for particles to flow through
        }

        private void OnDestroy()
        {
            // No blocking to unregister - lift is open
        }

        private HashSet<uint> processedClusters = new HashSet<uint>();

        private void Update()
        {
            if (SimulationWorld.Instance == null) return;

            accelerateTimer += Time.deltaTime;
            if (accelerateTimer < AccelerateInterval) return;
            accelerateTimer = 0f;

            var grid = SimulationWorld.Instance.Grid;
            var clusterMgr = grid.ClusterManager;

            // Track which clusters we've already processed this frame
            processedClusters.Clear();

            for (int y = simGridMinY; y <= simGridMaxY; y++)
            {
                for (int x = simGridMinX; x <= simGridMaxX; x++)
                {
                    // Check for cluster first
                    uint clusterId = grid.GetClusterID(x, y);
                    if (clusterId != 0)
                    {
                        // Only process each cluster once
                        if (!processedClusters.Contains(clusterId))
                        {
                            processedClusters.Add(clusterId);

                            // Get cluster data and apply velocity
                            var clusterData = clusterMgr.GetCluster(clusterId);
                            if (clusterData.HasValue)
                            {
                                Vector2 vel = clusterData.Value.Velocity;

                                // Direct addition: push in lift direction
                                float liftDir = MovesUp ? -1f : 1f;  // Negative Y = up in grid coords
                                vel.y += liftDir * GameSettings.SimLiftForce;

                                // Cap at terminal velocity
                                vel.y = Mathf.Clamp(vel.y, -GameSettings.SimTerminalVelocity, GameSettings.SimTerminalVelocity);

                                clusterMgr.SetClusterVelocity(clusterId, vel);
                            }
                        }
                        continue;  // Skip single-cell processing for clustered cells
                    }

                    MaterialType type = grid.Get(x, y);

                    if (MaterialProperties.IsSimulated(type))
                    {
                        // Get current velocity
                        Vector2 vel = grid.GetVelocity(x, y);

                        // Direct addition: push in lift direction
                        float liftDir = MovesUp ? -1f : 1f;  // Negative Y = up in grid coords
                        vel.y += liftDir * GameSettings.SimLiftForce;

                        // Cap at terminal velocity
                        vel.y = Mathf.Clamp(vel.y, -GameSettings.SimTerminalVelocity, GameSettings.SimTerminalVelocity);

                        grid.SetVelocity(x, y, vel);
                    }
                }
            }
        }

        private static void CreateHollowVisual(GameObject parent, bool movesUp)
        {
            // Create a sprite for the hollow frame (32x32 pixels)
            SpriteRenderer sr = parent.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteGenerator.CreateHollowLiftSprite(GameSettings.InfraGridSize, GameSettings.LiftColor, movesUp);
            sr.sortingOrder = 8;  // Above terrain (simulation=5) but below player (10)
        }


        private void OnTriggerStay2D(Collider2D other)
        {
            // Legacy physics-based acceleration for any remaining rigidbody particles
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // Direct addition: push in lift direction (Unity Y is up = positive)
                float liftDir = MovesUp ? 1f : -1f;
                float newY = rb.linearVelocity.y + liftDir * GameSettings.SimLiftForce * 60f; // Scale for Unity physics
                newY = Mathf.Clamp(newY, -GameSettings.SimTerminalVelocity * 60f, GameSettings.SimTerminalVelocity * 60f);
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, newY);
            }
        }
    }
}
