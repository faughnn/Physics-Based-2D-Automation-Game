using Xunit;
using UnityEngine;

namespace GoldRush.Tests
{
    // Copy of GameSettings for testing (extracted pure logic)
    public static class GameSettings
    {
        public const int GridSize = 32;
        public const int WorldWidthCells = 40;
        public const int WorldHeightCells = 25;
        public const int WaterReservoirHeight = 3;
        public const int AirHeight = 8;
        public const float PixelsPerUnit = 32f;

        public const float ParticleMass = 0.1f;
        public const float ParticleLinearDrag = 0.5f;
        public const float ParticleBounciness = 0.1f;
        public const float ParticleFriction = 0.5f;
        public const float ParticleRadius = 2f;

        public const int MaxSandParticles = 2000;
        public const int MaxWetSandParticles = 1000;
        public const int MaxWaterParticles = 500;
        public const int MaxGoldParticles = 500;
        public const int MaxSlagParticles = 500;
        public const int SandPerBlock = 8;

        public const float PlayerMoveSpeed = 5f;
        public const float PlayerJumpForce = 10f;
        public const float BeltSpeed = 3f;
        public const float LiftSpeed = 4f;
        public const float ShakerProcessTime = 2f;

        public static Vector2 GridToWorld(int gridX, int gridY)
        {
            float worldX = (gridX - WorldWidthCells / 2f + 0.5f) * GridSize / PixelsPerUnit;
            float worldY = (WorldHeightCells / 2f - gridY - 0.5f) * GridSize / PixelsPerUnit;
            return new Vector2(worldX, worldY);
        }

        public static Vector2Int WorldToGrid(Vector2 worldPos)
        {
            int gridX = Mathf.FloorToInt(worldPos.x * PixelsPerUnit / GridSize + WorldWidthCells / 2f);
            int gridY = Mathf.FloorToInt(WorldHeightCells / 2f - worldPos.y * PixelsPerUnit / GridSize);
            return new Vector2Int(gridX, gridY);
        }
    }

    public class GameSettingsTests
    {
        [Fact]
        public void GridSize_Is32Pixels()
        {
            Assert.Equal(32, GameSettings.GridSize);
        }

        [Fact]
        public void WorldDimensions_AreCorrect()
        {
            Assert.Equal(40, GameSettings.WorldWidthCells);
            Assert.Equal(25, GameSettings.WorldHeightCells);
        }

        [Fact]
        public void GridToWorld_CenterOfWorld_ReturnsOrigin()
        {
            // Center cell should be near origin
            int centerX = GameSettings.WorldWidthCells / 2;
            int centerY = GameSettings.WorldHeightCells / 2;

            Vector2 worldPos = GameSettings.GridToWorld(centerX, centerY);

            // Should be close to (0, 0) with small offset for cell center
            Assert.True(Mathf.Abs(worldPos.x) < 1f, $"Expected x near 0, got {worldPos.x}");
            Assert.True(Mathf.Abs(worldPos.y) < 1f, $"Expected y near 0, got {worldPos.y}");
        }

        [Fact]
        public void GridToWorld_TopLeftCorner_ReturnsNegativeXPositiveY()
        {
            Vector2 worldPos = GameSettings.GridToWorld(0, 0);

            Assert.True(worldPos.x < 0, $"Top-left x should be negative, got {worldPos.x}");
            Assert.True(worldPos.y > 0, $"Top-left y should be positive, got {worldPos.y}");
        }

        [Fact]
        public void GridToWorld_BottomRightCorner_ReturnsPositiveXNegativeY()
        {
            Vector2 worldPos = GameSettings.GridToWorld(GameSettings.WorldWidthCells - 1, GameSettings.WorldHeightCells - 1);

            Assert.True(worldPos.x > 0, $"Bottom-right x should be positive, got {worldPos.x}");
            Assert.True(worldPos.y < 0, $"Bottom-right y should be negative, got {worldPos.y}");
        }

        [Fact]
        public void WorldToGrid_RoundTrip_ReturnsOriginalPosition()
        {
            // Test multiple grid positions
            int[] testX = { 0, 10, 20, 39 };
            int[] testY = { 0, 5, 12, 24 };

            foreach (int gx in testX)
            {
                foreach (int gy in testY)
                {
                    Vector2 worldPos = GameSettings.GridToWorld(gx, gy);
                    Vector2Int gridPos = GameSettings.WorldToGrid(worldPos);

                    Assert.Equal(gx, gridPos.x);
                    Assert.Equal(gy, gridPos.y);
                }
            }
        }

        [Fact]
        public void WorldToGrid_Origin_ReturnsCenterCell()
        {
            Vector2Int gridPos = GameSettings.WorldToGrid(new Vector2(0, 0));

            // Origin should map to center of grid
            Assert.Equal(GameSettings.WorldWidthCells / 2, gridPos.x);
            Assert.Equal(GameSettings.WorldHeightCells / 2, gridPos.y);
        }

        [Fact]
        public void AdjacentCells_HaveCorrectSpacing()
        {
            Vector2 pos1 = GameSettings.GridToWorld(10, 10);
            Vector2 pos2 = GameSettings.GridToWorld(11, 10);

            float expectedSpacing = GameSettings.GridSize / GameSettings.PixelsPerUnit;
            float actualSpacing = pos2.x - pos1.x;

            Assert.Equal(expectedSpacing, actualSpacing, 4);
        }

        [Fact]
        public void ParticleSettings_AreWithinReasonableRanges()
        {
            Assert.True(GameSettings.ParticleMass > 0 && GameSettings.ParticleMass < 1f);
            Assert.True(GameSettings.ParticleBounciness >= 0 && GameSettings.ParticleBounciness <= 1f);
            Assert.True(GameSettings.ParticleFriction >= 0 && GameSettings.ParticleFriction <= 1f);
            Assert.True(GameSettings.ParticleRadius > 0);
        }

        [Fact]
        public void PoolSizes_ArePositive()
        {
            Assert.True(GameSettings.MaxSandParticles > 0);
            Assert.True(GameSettings.MaxWetSandParticles > 0);
            Assert.True(GameSettings.MaxWaterParticles > 0);
            Assert.True(GameSettings.MaxGoldParticles > 0);
            Assert.True(GameSettings.MaxSlagParticles > 0);
        }

        [Fact]
        public void TerrainStartsAfterWaterAndAir()
        {
            int terrainStartRow = GameSettings.WaterReservoirHeight + GameSettings.AirHeight;

            Assert.Equal(11, terrainStartRow); // 3 water + 8 air = 11
            Assert.True(terrainStartRow < GameSettings.WorldHeightCells);
        }

        [Fact]
        public void SandPerBlock_IsReasonable()
        {
            Assert.True(GameSettings.SandPerBlock >= 1);
            Assert.True(GameSettings.SandPerBlock <= 20);
        }
    }
}
