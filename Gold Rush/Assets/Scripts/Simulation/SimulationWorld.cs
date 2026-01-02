using UnityEngine;
using GoldRush.Core;

namespace GoldRush.Simulation
{
    public class SimulationWorld : MonoBehaviour
    {
        public static SimulationWorld Instance { get; private set; }

        public SimulationGrid Grid { get; private set; }
        public SimulationRenderer Renderer { get; private set; }

        // Simulation settings
        public const int CellPixelSize = 2;  // Each cell is 2x2 pixels
        public const int GridWidth = 640;     // 1280 / 2
        public const int GridHeight = 400;    // 800 / 2

        // Simulation speed
        private float simulationTimer;
        private const float SimulationInterval = 1f / 60f;  // 60 updates per second

        // Interaction processing
        private float interactionTimer;
        private const float InteractionInterval = 0.1f;  // Check interactions 10 times per second

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void Initialize()
        {
            // Create the simulation grid
            Grid = new SimulationGrid(GridWidth, GridHeight);

            // Create the renderer
            GameObject rendererGO = new GameObject("SimulationRenderer");
            rendererGO.transform.SetParent(transform);
            Renderer = rendererGO.AddComponent<SimulationRenderer>();
            Renderer.Initialize(Grid, CellPixelSize);

            // Initialize terrain in grid
            InitializeTerrain();

            // Initialize water reservoir
            InitializeWaterReservoir();

        }

        private void InitializeTerrain()
        {
            // Calculate terrain start row
            // In the old system: WaterReservoirHeight = 3, AirHeight = 8
            // Terrain starts at row 11 in a 25-row world
            // Scale to our 400-row grid: 11/25 * 400 = 176

            int terrainStartY = (int)((float)(GameSettings.WaterReservoirHeight + GameSettings.AirHeight) /
                                       GameSettings.WorldHeightCells * GridHeight);

            // Fill terrain from terrainStartY to bottom
            for (int y = terrainStartY; y < GridHeight; y++)
            {
                for (int x = 0; x < GridWidth; x++)
                {
                    Grid.Set(x, y, MaterialType.Terrain);
                }
            }

            // Create water reservoir structure
            // Old reservoir: LeftX=27, RightX=33, TopY=5, BottomY=11
            // Scale to simulation grid
            float scaleX = (float)GridWidth / GameSettings.WorldWidthCells;
            float scaleY = (float)GridHeight / GameSettings.WorldHeightCells;

            int resLeftX = (int)(GameSettings.ReservoirLeftX * scaleX);
            int resRightX = (int)(GameSettings.ReservoirRightX * scaleX);
            int resTopY = (int)(GameSettings.ReservoirTopY * scaleY);
            int resBottomY = (int)(GameSettings.ReservoirBottomY * scaleY);

            // Clear the reservoir interior (it's above terrain anyway)
            // But make sure the walls are terrain
            int wallThickness = (int)(scaleX);  // Scale wall thickness

            // Left wall
            for (int y = resTopY; y <= resBottomY; y++)
            {
                for (int wx = 0; wx < wallThickness; wx++)
                {
                    Grid.Set(resLeftX + wx, y, MaterialType.Terrain);
                }
            }

            // Right wall
            for (int y = resTopY; y <= resBottomY; y++)
            {
                for (int wx = 0; wx < wallThickness; wx++)
                {
                    Grid.Set(resRightX + wx, y, MaterialType.Terrain);
                }
            }

            // Bottom (already covered by terrain fill, but ensure it)
            for (int x = resLeftX; x <= resRightX + wallThickness; x++)
            {
                for (int wy = 0; wy < wallThickness; wy++)
                {
                    Grid.Set(x, resBottomY + wy, MaterialType.Terrain);
                }
            }

        }

        private void InitializeWaterReservoir()
        {
            // Fill reservoir interior with water
            float scaleX = (float)GridWidth / GameSettings.WorldWidthCells;
            float scaleY = (float)GridHeight / GameSettings.WorldHeightCells;

            int interiorMinX = (int)(GameSettings.ReservoirInteriorMinX * scaleX);
            int interiorMaxX = (int)(GameSettings.ReservoirInteriorMaxX * scaleX);
            int interiorMinY = (int)(GameSettings.ReservoirInteriorMinY * scaleY);
            int interiorMaxY = (int)(GameSettings.ReservoirInteriorMaxY * scaleY);

            for (int y = interiorMinY; y <= interiorMaxY; y++)
            {
                for (int x = interiorMinX; x <= interiorMaxX; x++)
                {
                    if (Grid.Get(x, y) == MaterialType.Air)
                    {
                        Grid.Set(x, y, MaterialType.Water);
                    }
                }
            }

        }

        private void Update()
        {
            if (Grid == null) return;

            // Update simulation at fixed rate
            simulationTimer += Time.deltaTime;
            while (simulationTimer >= SimulationInterval)
            {
                simulationTimer -= SimulationInterval;
                Grid.Update();
            }

            // Process material interactions less frequently
            interactionTimer += Time.deltaTime;
            if (interactionTimer >= InteractionInterval)
            {
                interactionTimer = 0f;
                Grid.ProcessInteractions();
            }

            // Render
            Renderer.Render();
        }

        // Public methods for game integration

        // Dig terrain at a grid position, spawning sand
        public void DigAt(int gridX, int gridY, int radius = 3)
        {
            int sandToSpawn = 0;

            // First pass: clear terrain to air and count how much we dug
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (dx * dx + dy * dy <= radius * radius)
                    {
                        int x = gridX + dx;
                        int y = gridY + dy;

                        if (Grid.InBounds(x, y) && Grid.Get(x, y) == MaterialType.Terrain)
                        {
                            Grid.Set(x, y, MaterialType.Air);
                            sandToSpawn++;
                        }
                    }
                }
            }

            // Second pass: spawn sand at top of the hole so it falls
            int spawnY = gridY - radius - 1;  // Just above the hole
            int spawnX = gridX - radius / 2;
            int spawned = 0;

            for (int i = 0; i < sandToSpawn && spawned < sandToSpawn; i++)
            {
                int x = spawnX + (i % radius);
                int y = spawnY - (i / radius);

                if (Grid.InBounds(x, y) && Grid.Get(x, y) == MaterialType.Air)
                {
                    Grid.Set(x, y, MaterialType.Sand);
                    spawned++;
                }
            }
        }

        // Dig terrain at a world position
        public void DigAtWorld(Vector2 worldPos, int radius = 3)
        {
            Vector2Int gridPos = Renderer.WorldToGrid(worldPos);
            DigAt(gridPos.x, gridPos.y, radius);
        }

        // Dig a semi-circle of terrain at grid position, converting terrain to sand in-place
        // Direction points from flat edge toward curved edge (dig direction)
        public void DigSemiCircle(int gridX, int gridY, Vector2 direction, int radius = 12)
        {
            direction = direction.normalized;

            // Kickback direction is opposite to dig direction (toward player)
            Vector2 kickDirection = -direction;
            float kickStrength = 15f;  // Strong kickback toward player

            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    // Check if in circle
                    if (dx * dx + dy * dy <= radius * radius)
                    {
                        // Check if in forward half (dot product with direction >= 0)
                        Vector2 offset = new Vector2(dx, -dy); // Negate dy because grid Y is inverted
                        float dot = Vector2.Dot(offset, direction);

                        if (dot >= 0)
                        {
                            int x = gridX + dx;
                            int y = gridY + dy;

                            if (Grid.InBounds(x, y) && Grid.Get(x, y) == MaterialType.Terrain)
                            {
                                // Convert terrain to sand in-place
                                Grid.Set(x, y, MaterialType.Sand);

                                // Apply kickback velocity toward player
                                // Add some randomness for natural look
                                float randomFactor = 0.7f + Random.value * 0.6f;
                                Vector2 kickVel = kickDirection * kickStrength * randomFactor;
                                // Invert Y for grid coordinates (negative Y is up in grid)
                                kickVel.y = -kickVel.y;
                                Grid.SetVelocity(x, y, kickVel);
                            }
                        }
                    }
                }
            }
        }

        // Dig semi-circle at world position
        public void DigSemiCircleWorld(Vector2 worldPos, Vector2 direction, int radius = 12)
        {
            Vector2Int gridPos = Renderer.WorldToGrid(worldPos);
            DigSemiCircle(gridPos.x, gridPos.y, direction, radius);
        }

        // Spawn material at world position
        public void SpawnMaterial(Vector2 worldPos, MaterialType type, int radius = 1)
        {
            Vector2Int gridPos = Renderer.WorldToGrid(worldPos);

            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int x = gridPos.x + dx;
                    int y = gridPos.y + dy;

                    if (Grid.InBounds(x, y) && Grid.Get(x, y) == MaterialType.Air)
                    {
                        Grid.Set(x, y, type);
                    }
                }
            }
        }

        // Check if a grid cell is solid (for collision)
        public bool IsSolid(int gridX, int gridY)
        {
            MaterialType type = Grid.Get(gridX, gridY);
            return type == MaterialType.Terrain;
        }

        // Convert between coordinate systems
        public Vector2Int WorldToGrid(Vector2 worldPos)
        {
            return Renderer.WorldToGrid(worldPos);
        }

        public Vector2 GridToWorld(int gridX, int gridY)
        {
            return Renderer.GridToWorld(gridX, gridY);
        }
    }
}
