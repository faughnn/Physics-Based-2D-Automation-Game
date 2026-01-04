using UnityEngine;

namespace GoldRush.Core
{
    public static class GameSettings
    {
        // Grid settings
        public const int GridSize = 32; // pixels per cell
        public const int WorldWidthCells = 40; // 40 * 32 = 1280 pixels
        public const int WorldHeightCells = 25; // 25 * 32 = 800 pixels

        // World layout (in cells from top)
        public const int WaterReservoirHeight = 3; // top 3 rows for water
        public const int AirHeight = 8; // next 8 rows are air
        // Remaining rows are terrain

        // Physics
        public static readonly Vector2 Gravity = new Vector2(0f, -20f);

        // Particle physics
        public const float ParticleMass = 0.1f;
        public const float ParticleLinearDrag = 0.5f;
        public const float ParticleGravityScale = 1f;
        public const float ParticleBounciness = 0.1f;
        public const float ParticleFriction = 0.5f;
        public const float ParticleRadius = 2f; // pixels

        // Particle pool
        public const int MaxSandParticles = 2000;
        public const int MaxWetSandParticles = 1000;
        public const int MaxWaterParticles = 500;
        public const int MaxGoldParticles = 500;
        public const int MaxSlagParticles = 500;
        public const int SandPerBlock = 8; // particles spawned when digging

        // Player
        public const float PlayerMoveSpeed = 5f;
        public const float PlayerJumpForce = 10f;
        public const int PlayerWidth = 24; // pixels
        public const int PlayerHeight = 48; // pixels

        // Unified Physics Constants (force zone system)
        public const float SimGravity = 0.5f;           // Downward force per frame
        public const float SimTerminalVelocity = 8f;    // Max speed in any direction
        public const float SimLiftForce = 1.5f;         // Upward force per frame (no collision penalty to overcome)
        public const float SimLiftCenteringForce = 0.3f; // Horizontal force pulling materials toward lift center
        public const float SimBlowerForce = 1.0f;       // Horizontal force per frame
        public const float SimBeltForce = 0.5f;         // Belt surface force per frame

        // Infrastructure
        public const float BeltSpeed = 3f;              // Legacy - kept for reference
        public const float ShakerFallInterval = 1.5f; // seconds per cell - wet sand falls slowly through shaker
        public const float ShakerPushSpeed = 0.5f;
        public const int BeltSize = 16; // 16x16 pixels for small belts (quarter of grid cell)
        public const int WallSize = 16; // 16x16 pixels for walls (same as belt)

        // Infrastructure grid (same size as main grid for belts/lifts)
        public const int InfraGridSize = 32;      // 32 pixels per infra cell (same as main grid)
        public const int InfraGridScale = 1;      // 1 infra cell per main cell
        public const int InfraWorldWidthCells = 40;    // Same as WorldWidthCells
        public const int InfraWorldHeightCells = 25;   // Same as WorldHeightCells

        // Water reservoir (bucket-style, right of player at ground level)
        public const int ReservoirLeftX = 27;   // Left wall X position
        public const int ReservoirRightX = 33;  // Right wall X position
        public const int ReservoirTopY = 5;     // Top of walls (in air zone)
        public const int ReservoirBottomY = 11; // Bottom (at terrain level)
        public const int ReservoirInteriorMinX = 28;
        public const int ReservoirInteriorMaxX = 32;
        public const int ReservoirInteriorMinY = 6;
        public const int ReservoirInteriorMaxY = 10;
        public const int ReservoirPreFillCount = 120; // Initial water particles
        public const float WaterSpawnRate = 0f; // No continuous spawning (finite water)

        // Colors (for placeholder sprites)
        public static readonly Color PlayerColor = new Color(0.2f, 0.8f, 0.2f); // Green
        public static readonly Color TerrainColor = new Color(0.55f, 0.35f, 0.2f); // Brown
        public static readonly Color SandColor = new Color(0.76f, 0.7f, 0.5f); // Tan/Beige
        public static readonly Color WetSandColor = new Color(0.4f, 0.3f, 0.2f); // Dark brown
        public static readonly Color GoldColor = new Color(1f, 0.84f, 0f); // Gold yellow
        public static readonly Color SlagColor = new Color(0.5f, 0.5f, 0.5f); // Grey
        public static readonly Color WaterColor = new Color(0.2f, 0.5f, 0.9f, 0.7f); // Blue semi-transparent
        public static readonly Color WallColor = new Color(0.3f, 0.3f, 0.3f); // Dark grey
        public static readonly Color BeltColor = new Color(0.25f, 0.25f, 0.25f); // Dark grey
        public static readonly Color LiftColor = new Color(0.25f, 0.25f, 0.25f); // Dark grey
        public static readonly Color ShakerColor = new Color(1f, 0.5f, 0f, 0.5f); // Orange, semi-transparent to see material falling through
        public static readonly Color GoldStoreColor = new Color(1f, 0.84f, 0f); // Yellow
        public static readonly Color BlowerColor = new Color(0.4f, 0.7f, 1f); // Light blue

        // Pixels per unit (for converting between pixels and Unity units)
        public const float PixelsPerUnit = 32f;

        // Helper to convert grid position to world position
        public static Vector2 GridToWorld(int gridX, int gridY)
        {
            float worldX = (gridX - WorldWidthCells / 2f + 0.5f) * GridSize / PixelsPerUnit;
            float worldY = (WorldHeightCells / 2f - gridY - 0.5f) * GridSize / PixelsPerUnit;
            return new Vector2(worldX, worldY);
        }

        // Helper to convert world position to grid position
        public static Vector2Int WorldToGrid(Vector2 worldPos)
        {
            int gridX = Mathf.FloorToInt(worldPos.x * PixelsPerUnit / GridSize + WorldWidthCells / 2f);
            int gridY = Mathf.FloorToInt(WorldHeightCells / 2f - worldPos.y * PixelsPerUnit / GridSize);
            return new Vector2Int(gridX, gridY);
        }

        // Helper to convert infra grid position to world position
        public static Vector2 InfraGridToWorld(int gridX, int gridY)
        {
            float worldX = (gridX - InfraWorldWidthCells / 2f + 0.5f) * InfraGridSize / PixelsPerUnit;
            float worldY = (InfraWorldHeightCells / 2f - gridY - 0.5f) * InfraGridSize / PixelsPerUnit;
            return new Vector2(worldX, worldY);
        }

        // Helper to convert world position to infra grid position
        public static Vector2Int WorldToInfraGrid(Vector2 worldPos)
        {
            int gridX = Mathf.FloorToInt(worldPos.x * PixelsPerUnit / InfraGridSize + InfraWorldWidthCells / 2f);
            int gridY = Mathf.FloorToInt(InfraWorldHeightCells / 2f - worldPos.y * PixelsPerUnit / InfraGridSize);
            return new Vector2Int(gridX, gridY);
        }

        // Sub-grid for belts/walls (16x16, 4 positions per main grid cell)
        public const int SubGridSize = 16; // 16 pixels per sub-grid cell
        public const int SubGridWorldWidthCells = 80;  // 40 * 2 = 80 sub-cells
        public const int SubGridWorldHeightCells = 50; // 25 * 2 = 50 sub-cells

        // Helper to convert sub-grid position to world position (for belts/walls)
        public static Vector2 SubGridToWorld(int gridX, int gridY)
        {
            float worldX = (gridX - SubGridWorldWidthCells / 2f + 0.5f) * SubGridSize / PixelsPerUnit;
            float worldY = (SubGridWorldHeightCells / 2f - gridY - 0.5f) * SubGridSize / PixelsPerUnit;
            return new Vector2(worldX, worldY);
        }

        // Helper to convert world position to sub-grid position
        public static Vector2Int WorldToSubGrid(Vector2 worldPos)
        {
            int gridX = Mathf.FloorToInt(worldPos.x * PixelsPerUnit / SubGridSize + SubGridWorldWidthCells / 2f);
            int gridY = Mathf.FloorToInt(SubGridWorldHeightCells / 2f - worldPos.y * PixelsPerUnit / SubGridSize);
            return new Vector2Int(gridX, gridY);
        }

        // Shaker sub-grid (32x16, 2 vertical positions per main grid cell)
        public const int ShakerSubGridHeight = 16; // Half of main grid height

        // Helper to convert shaker grid position to world position
        public static Vector2 ShakerGridToWorld(int gridX, int gridY)
        {
            // gridX uses main grid (32px), gridY uses half-grid (16px)
            float worldX = (gridX - WorldWidthCells / 2f + 0.5f) * GridSize / PixelsPerUnit;
            float worldY = (WorldHeightCells * 2 / 2f - gridY - 0.5f) * ShakerSubGridHeight / PixelsPerUnit;
            return new Vector2(worldX, worldY);
        }

        // Helper to convert world position to shaker grid position
        public static Vector2Int WorldToShakerGrid(Vector2 worldPos)
        {
            int gridX = Mathf.FloorToInt(worldPos.x * PixelsPerUnit / GridSize + WorldWidthCells / 2f);
            int gridY = Mathf.FloorToInt(WorldHeightCells * 2 / 2f - worldPos.y * PixelsPerUnit / ShakerSubGridHeight);
            return new Vector2Int(gridX, gridY);
        }
    }
}
