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

        // Infrastructure
        public const float BeltSpeed = 3f;
        public const float LiftSpeed = 4f;
        public const float LiftAcceleration = 2f; // gradual acceleration
        public const float ShakerProcessTime = 2f; // seconds
        public const float ShakerPushSpeed = 0.5f;

        // Water reservoir
        public const float WaterSpawnRate = 2f; // particles per second

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
        public static readonly Color ShakerColor = new Color(1f, 0.5f, 0f); // Orange
        public static readonly Color GoldStoreColor = new Color(1f, 0.84f, 0f); // Yellow

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
