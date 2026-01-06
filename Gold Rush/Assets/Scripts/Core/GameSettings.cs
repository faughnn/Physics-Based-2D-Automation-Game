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
        public const float SimBeltForce = 0.5f;         // Belt surface force per frame (legacy - kept for reference)
        public const int BeltShiftInterval = 10;          // Frames between belt shifts (~6 shifts/sec at 60fps)

        // Infrastructure
        public const float BeltSpeed = 3f;              // Legacy - kept for reference
        public const float ShakerFallInterval = 1.5f; // seconds per cell - wet sand falls slowly through shaker
        public const float ShakerPushSpeed = 0.5f;
        public const int BeltSize = 16; // 16x16 pixels for small belts (quarter of grid cell)
        public const int WallSize = 16; // 16x16 pixels for walls (same as belt)
        public const int InfraGridSize = 32; // 32x32 pixels for main infrastructure grid

        // Pusher timing (frames at 60fps simulation rate)
        public const int PusherExtendFrames = 30;    // 0.5s to extend
        public const int PusherRetractFrames = 20;   // 0.33s to retract (faster)
        public const int PusherPauseFrames = 15;     // 0.25s pause at each end

        // Pusher dimensions
        public const int PusherSize = 32;            // pixels (1 infra grid cell)
        public const int PusherPlateThickness = 4;   // pixels (2 sim cells)
        public const int PusherGapSize = 2;          // pixels (1 sim cell) - sand escape gap

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
        public static readonly Color PusherFrameColor = new Color(0.4f, 0.4f, 0.45f); // Dark grey-blue
        public static readonly Color PusherPlateColor = new Color(0.6f, 0.6f, 0.65f); // Light grey

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
    }
}
