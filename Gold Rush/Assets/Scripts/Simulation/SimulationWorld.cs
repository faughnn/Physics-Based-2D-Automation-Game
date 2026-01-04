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

        // Vein types - stores what material each terrain cell should produce when dug
        private MaterialType[] veinTypes;

        // Cluster spawn points - stores cluster size at each position (0 = no cluster)
        private byte[] clusterSpawnSizes;  // 0 = no cluster, 2/3/4 = cluster size
        private MaterialType[] clusterSpawnTypes;  // Material type for cluster

        // Cluster generation settings
        private const float ClusterChance = 0.40f;  // 40% of terrain can have clusters
        private const float ClusterNoiseScale = 0.05f;  // Noise scale for cluster distribution

        // Vein generation settings
        private const float RockVeinChance = 0.15f;    // 15% of terrain
        private const float OreVeinChance = 0.08f;     // 8% of terrain (deeper = more)
        private const float CoalVeinChance = 0.10f;    // 10% of terrain
        private const float VeinNoiseScale = 0.05f;    // Perlin noise scale for natural patterns

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

            // Initialize vein types array (same size as grid)
            veinTypes = new MaterialType[GridWidth * GridHeight];
            clusterSpawnSizes = new byte[GridWidth * GridHeight];
            clusterSpawnTypes = new MaterialType[GridWidth * GridHeight];
            for (int i = 0; i < veinTypes.Length; i++)
            {
                veinTypes[i] = MaterialType.Sand;  // Default to sand
                clusterSpawnSizes[i] = 0;  // No cluster
                clusterSpawnTypes[i] = MaterialType.Sand;
            }

            // Create the renderer
            GameObject rendererGO = new GameObject("SimulationRenderer");
            rendererGO.transform.SetParent(transform);
            Renderer = rendererGO.AddComponent<SimulationRenderer>();
            Renderer.Initialize(Grid, CellPixelSize);

            // Initialize terrain in grid
            InitializeTerrain();

            // Generate ore veins in terrain
            GenerateVeins();

            // Initialize water reservoir
            InitializeWaterReservoir();

            // Spawn test rocks/boulders on surface for testing crushers
            SpawnSurfaceTestRocks();

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

        private void SpawnSurfaceTestRocks()
        {
            // Calculate terrain surface Y
            int terrainStartY = (int)((float)(GameSettings.WaterReservoirHeight + GameSettings.AirHeight) /
                                       GameSettings.WorldHeightCells * GridHeight);

            var clusterMgr = Grid.ClusterManager;

            // Spawn a variety of rocks and boulders along the surface
            // Start from left side, skip reservoir area (around x=350-450)
            int spacing = 20;  // Space between clusters

            // Spawn Boulders (8x8) on the left side
            for (int i = 0; i < 5; i++)
            {
                int x = 50 + i * spacing;
                int y = terrainStartY - 8;  // 8 cells above terrain (boulder is 8x8)
                clusterMgr.CreateCluster(x, y, 8, MaterialType.Boulder);
            }

            // Spawn Rocks (4x4) in the middle-left
            for (int i = 0; i < 8; i++)
            {
                int x = 180 + i * (spacing / 2);
                int y = terrainStartY - 4;  // 4 cells above terrain
                clusterMgr.CreateCluster(x, y, 4, MaterialType.Rock);
            }

            // Spawn Gravel (2x2) near middle
            for (int i = 0; i < 10; i++)
            {
                int x = 280 + i * 8;
                int y = terrainStartY - 2;  // 2 cells above terrain
                clusterMgr.CreateCluster(x, y, 2, MaterialType.Gravel);
            }

            // Spawn more on the right side (after reservoir ~450)
            // Boulders
            for (int i = 0; i < 5; i++)
            {
                int x = 470 + i * spacing;
                int y = terrainStartY - 8;
                clusterMgr.CreateCluster(x, y, 8, MaterialType.Boulder);
            }

            // Rocks
            for (int i = 0; i < 8; i++)
            {
                int x = 520 + i * (spacing / 2);
                int y = terrainStartY - 4;
                clusterMgr.CreateCluster(x, y, 4, MaterialType.Rock);
            }

            // Gravel
            for (int i = 0; i < 10; i++)
            {
                int x = 580 + i * 8;
                int y = terrainStartY - 2;
                clusterMgr.CreateCluster(x, y, 2, MaterialType.Gravel);
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

        private void GenerateVeins()
        {
            // Calculate terrain start row
            int terrainStartY = (int)((float)(GameSettings.WaterReservoirHeight + GameSettings.AirHeight) /
                                       GameSettings.WorldHeightCells * GridHeight);

            // Random offsets for Perlin noise to create unique world each time
            float rockOffsetX = Random.Range(0f, 10000f);
            float rockOffsetY = Random.Range(0f, 10000f);
            float oreOffsetX = Random.Range(0f, 10000f);
            float oreOffsetY = Random.Range(0f, 10000f);
            float coalOffsetX = Random.Range(0f, 10000f);
            float coalOffsetY = Random.Range(0f, 10000f);
            float clusterOffsetX = Random.Range(0f, 10000f);
            float clusterOffsetY = Random.Range(0f, 10000f);

            int terrainDepth = GridHeight - terrainStartY;

            for (int y = terrainStartY; y < GridHeight; y++)
            {
                for (int x = 0; x < GridWidth; x++)
                {
                    // Only generate veins in terrain cells
                    if (Grid.Get(x, y) != MaterialType.Terrain) continue;

                    int index = y * GridWidth + x;

                    // Calculate depth factor (0 at surface, 1 at bottom)
                    float depthFactor = (float)(y - terrainStartY) / terrainDepth;

                    // Determine vein type first
                    MaterialType veinType = MaterialType.Sand;

                    // Rock veins - use Perlin noise for natural clusters
                    float rockNoise = Mathf.PerlinNoise(
                        (x + rockOffsetX) * VeinNoiseScale,
                        (y + rockOffsetY) * VeinNoiseScale
                    );
                    if (rockNoise > (1f - RockVeinChance))
                    {
                        veinType = MaterialType.Rock;
                    }
                    else
                    {
                        // Ore veins - more common at depth
                        float oreNoise = Mathf.PerlinNoise(
                            (x + oreOffsetX) * VeinNoiseScale * 1.5f,
                            (y + oreOffsetY) * VeinNoiseScale * 1.5f
                        );
                        float depthModifiedOreChance = OreVeinChance * (0.5f + depthFactor);
                        if (oreNoise > (1f - depthModifiedOreChance))
                        {
                            veinType = MaterialType.Ore;
                        }
                        else
                        {
                            // Coal veins - scattered throughout
                            float coalNoise = Mathf.PerlinNoise(
                                (x + coalOffsetX) * VeinNoiseScale * 0.8f,
                                (y + coalOffsetY) * VeinNoiseScale * 0.8f
                            );
                            if (coalNoise > (1f - CoalVeinChance))
                            {
                                veinType = MaterialType.Coal;
                            }
                        }
                    }

                    veinTypes[index] = veinType;

                    // Assign cluster spawn for ALL terrain (not just rock veins)
                    // Clusters are based on depth, independent of vein type
                    AssignClusterSpawn(x, y, index, depthFactor, clusterOffsetX, clusterOffsetY);
                }
            }
        }

        // Assign cluster spawn point based on depth
        private void AssignClusterSpawn(int x, int y, int index, float depthFactor, float offsetX, float offsetY)
        {
            // Use Perlin noise to determine if this is a cluster origin point
            float clusterNoise = Mathf.PerlinNoise(
                (x + offsetX) * ClusterNoiseScale,
                (y + offsetY) * ClusterNoiseScale
            );

            if (clusterNoise < (1f - ClusterChance))
                return;  // Not a cluster spawn point

            // Determine cluster size based on depth:
            // Surface (0-33% depth): 2x2 Gravel (4x4 pixels)
            // Mid (33-66% depth): 4x4 Rock (8x8 pixels)
            // Deep (66-100% depth): 8x8 Boulder (16x16 pixels)
            byte clusterSize;
            MaterialType clusterType;

            if (depthFactor > 0.66f)
            {
                clusterSize = 8;  // Boulder
                clusterType = MaterialType.Boulder;
            }
            else if (depthFactor > 0.33f)
            {
                clusterSize = 4;  // Rock
                clusterType = MaterialType.Rock;
            }
            else
            {
                clusterSize = 2;  // Gravel
                clusterType = MaterialType.Gravel;
            }

            clusterSpawnSizes[index] = clusterSize;
            clusterSpawnTypes[index] = clusterType;
        }

        // Get the vein type at a grid position (what material should spawn when dug)
        public MaterialType GetVeinType(int x, int y)
        {
            if (x < 0 || x >= GridWidth || y < 0 || y >= GridHeight)
                return MaterialType.Sand;
            return veinTypes[y * GridWidth + x];
        }

        // Profiling
        private float profileTimer;
        private float gridTimeAccum;
        private float clusterTimeAccum;
        private float renderTimeAccum;
        private int profileFrames;
        private System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

        private void Update()
        {
            if (Grid == null) return;

            // Update simulation at fixed rate
            simulationTimer += Time.deltaTime;
            while (simulationTimer >= SimulationInterval)
            {
                simulationTimer -= SimulationInterval;

                sw.Restart();
                Grid.Update();
                sw.Stop();
                gridTimeAccum += sw.ElapsedTicks / (float)System.Diagnostics.Stopwatch.Frequency * 1000f;
            }

            // Process material interactions less frequently
            interactionTimer += Time.deltaTime;
            if (interactionTimer >= InteractionInterval)
            {
                interactionTimer = 0f;
                Grid.ProcessInteractions();
            }

            // Render
            sw.Restart();
            Renderer.Render();
            sw.Stop();
            renderTimeAccum += sw.ElapsedTicks / (float)System.Diagnostics.Stopwatch.Frequency * 1000f;

            // Log profiling every second
            profileFrames++;
            profileTimer += Time.deltaTime;
            if (profileTimer >= 1f)
            {
                int activeCells = Grid.ActiveCellCount;
                int clusterCount = Grid.ClusterManager.ClusterCount;
                int activeClusterCount = Grid.ClusterManager.ActiveClusterCount;
                var breakdown = Grid.GetActiveBreakdown();
                Debug.Log($"[PERF] Grid: {gridTimeAccum:F1}ms | Render: {renderTimeAccum:F1}ms | Active: {activeCells} | Moved: {breakdown.moved} | CanFall: {breakdown.canFall} | HasVel: {breakdown.hasVel} | Settling: {breakdown.settling}");
                profileTimer = 0f;
                gridTimeAccum = 0f;
                renderTimeAccum = 0f;
                profileFrames = 0;
            }
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

            // Track positions that should spawn clusters (process after clearing terrain)
            System.Collections.Generic.List<(int x, int y, byte size, MaterialType type)> clusterSpawns =
                new System.Collections.Generic.List<(int, int, byte, MaterialType)>();

            // Track cells to fill with single-cell materials (not part of clusters)
            System.Collections.Generic.List<(int x, int y, MaterialType mat)> singleCells =
                new System.Collections.Generic.List<(int, int, MaterialType)>();

            // First pass: collect cluster spawns and single cells, clear terrain to Air
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
                                int index = y * GridWidth + x;
                                byte clusterSize = clusterSpawnSizes[index];

                                if (clusterSize > 0)
                                {
                                    // Record cluster spawn for later
                                    clusterSpawns.Add((x, y, clusterSize, clusterSpawnTypes[index]));
                                    // Clear the spawn point so we don't spawn again
                                    clusterSpawnSizes[index] = 0;
                                }
                                else
                                {
                                    // Record as single-cell material
                                    MaterialType veinMaterial = GetVeinType(x, y);
                                    singleCells.Add((x, y, veinMaterial));
                                }

                                // Clear terrain to Air first (so clusters can check footprint)
                                Grid.Set(x, y, MaterialType.Air);
                            }
                        }
                    }
                }
            }

            // Second pass: spawn clusters (they need Air cells to check footprint)
            foreach (var spawn in clusterSpawns)
            {
                TrySpawnCluster(spawn.x, spawn.y, spawn.size, spawn.type, kickDirection, kickStrength);
            }

            // Third pass: fill remaining cells with single-cell materials
            foreach (var cell in singleCells)
            {
                // Only set if still Air (not claimed by a cluster)
                if (Grid.Get(cell.x, cell.y) == MaterialType.Air)
                {
                    Grid.Set(cell.x, cell.y, cell.mat);

                    // Apply kickback velocity
                    float randomFactor = 0.7f + Random.value * 0.6f;
                    Vector2 kickVel = kickDirection * kickStrength * randomFactor;
                    kickVel.y = -kickVel.y;
                    Grid.SetVelocity(cell.x, cell.y, kickVel);
                }
            }
        }

        // Try to spawn a cluster at the given position
        private void TrySpawnCluster(int x, int y, byte size, MaterialType type, Vector2 kickDirection, float kickStrength)
        {
            var clusterMgr = Grid.ClusterManager;

            // Try to create the cluster (ClusterManager checks if footprint is clear)
            uint clusterId = clusterMgr.CreateCluster(x, y, size, type);

            if (clusterId != 0)
            {
                // Successfully created cluster - give it velocity
                float randomFactor = 0.7f + Random.value * 0.6f;
                Vector2 kickVel = kickDirection * kickStrength * randomFactor;
                kickVel.y = -kickVel.y;  // Invert Y for grid coordinates
                clusterMgr.SetClusterVelocity(clusterId, kickVel);
            }
            else
            {
                // Couldn't create cluster (not enough space), fall back to single-cell materials
                for (int dy = 0; dy < size; dy++)
                {
                    for (int dx = 0; dx < size; dx++)
                    {
                        int cx = x + dx;
                        int cy = y + dy;
                        if (Grid.InBounds(cx, cy) && Grid.Get(cx, cy) == MaterialType.Air)
                        {
                            // Spawn single-cell version based on type
                            MaterialType singleType = type switch
                            {
                                MaterialType.Boulder => MaterialType.Rock,
                                MaterialType.Rock => MaterialType.Gravel,
                                MaterialType.Gravel => MaterialType.Sand,
                                _ => MaterialType.Sand
                            };
                            Grid.Set(cx, cy, singleType);

                            // Apply kickback
                            float randomFactor = 0.7f + Random.value * 0.6f;
                            Vector2 kickVel = kickDirection * kickStrength * randomFactor;
                            kickVel.y = -kickVel.y;
                            Grid.SetVelocity(cx, cy, kickVel);
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
