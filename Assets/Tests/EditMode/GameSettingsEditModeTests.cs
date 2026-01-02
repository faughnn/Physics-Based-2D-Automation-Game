using NUnit.Framework;
using UnityEngine;
using GoldRush.Core;

namespace GoldRush.Tests.EditMode
{
    [TestFixture]
    public class GameSettingsEditModeTests
    {
        [Test]
        public void GridToWorld_And_WorldToGrid_AreInverses()
        {
            // Test round-trip conversion for various grid positions
            int[] testX = { 0, 10, 20, 39 };
            int[] testY = { 0, 5, 12, 24 };

            foreach (int gx in testX)
            {
                foreach (int gy in testY)
                {
                    Vector2 worldPos = GameSettings.GridToWorld(gx, gy);
                    Vector2Int gridPos = GameSettings.WorldToGrid(worldPos);

                    Assert.AreEqual(gx, gridPos.x, $"X mismatch for grid ({gx}, {gy})");
                    Assert.AreEqual(gy, gridPos.y, $"Y mismatch for grid ({gx}, {gy})");
                }
            }
        }

        [Test]
        public void GridToWorld_AdjacentCells_HaveCorrectSpacing()
        {
            Vector2 pos1 = GameSettings.GridToWorld(10, 10);
            Vector2 pos2 = GameSettings.GridToWorld(11, 10);

            float expectedSpacing = GameSettings.GridSize / GameSettings.PixelsPerUnit;
            float actualSpacing = pos2.x - pos1.x;

            Assert.AreEqual(expectedSpacing, actualSpacing, 0.0001f);
        }

        [Test]
        public void WorldToGrid_Origin_ReturnsCenterCell()
        {
            Vector2Int gridPos = GameSettings.WorldToGrid(Vector2.zero);

            Assert.AreEqual(GameSettings.WorldWidthCells / 2, gridPos.x);
            Assert.AreEqual(GameSettings.WorldHeightCells / 2, gridPos.y);
        }

        [Test]
        public void ParticleSettings_AreReasonable()
        {
            Assert.Greater(GameSettings.ParticleMass, 0f);
            Assert.LessOrEqual(GameSettings.ParticleBounciness, 1f);
            Assert.GreaterOrEqual(GameSettings.ParticleFriction, 0f);
            Assert.Greater(GameSettings.ParticleRadius, 0f);
        }

        [Test]
        public void PoolSizes_ArePositive()
        {
            Assert.Greater(GameSettings.MaxSandParticles, 0);
            Assert.Greater(GameSettings.MaxWetSandParticles, 0);
            Assert.Greater(GameSettings.MaxWaterParticles, 0);
            Assert.Greater(GameSettings.MaxGoldParticles, 0);
            Assert.Greater(GameSettings.MaxSlagParticles, 0);
        }

        [Test]
        public void TerrainStartsAfterWaterAndAir()
        {
            int terrainStartRow = GameSettings.WaterReservoirHeight + GameSettings.AirHeight;
            Assert.Less(terrainStartRow, GameSettings.WorldHeightCells);
        }

        [Test]
        public void Gravity_IsDownward()
        {
            Assert.Less(GameSettings.Gravity.y, 0f, "Gravity should be negative (downward)");
        }
    }

    [TestFixture]
    public class LayerSetupEditModeTests
    {
        [Test]
        public void LayerIndices_AreUnique()
        {
            var layers = new[]
            {
                LayerSetup.PlayerLayer,
                LayerSetup.TerrainLayer,
                LayerSetup.SandLayer,
                LayerSetup.WetSandLayer,
                LayerSetup.GoldLayer,
                LayerSetup.SlagLayer,
                LayerSetup.WaterLayer,
                LayerSetup.InfrastructureLayer
            };

            var uniqueLayers = new System.Collections.Generic.HashSet<int>(layers);
            Assert.AreEqual(layers.Length, uniqueLayers.Count, "All layer indices should be unique");
        }

        [Test]
        public void Layers_AreWithinUnityRange()
        {
            var layers = new[]
            {
                LayerSetup.PlayerLayer,
                LayerSetup.TerrainLayer,
                LayerSetup.SandLayer,
                LayerSetup.WetSandLayer,
                LayerSetup.GoldLayer,
                LayerSetup.SlagLayer,
                LayerSetup.WaterLayer,
                LayerSetup.InfrastructureLayer
            };

            foreach (var layer in layers)
            {
                Assert.GreaterOrEqual(layer, 0);
                Assert.LessOrEqual(layer, 31);
            }
        }

        [Test]
        public void GetGroundMask_IncludesBothLayers()
        {
            int mask = LayerSetup.GetGroundMask();

            Assert.IsTrue((mask & (1 << LayerSetup.TerrainLayer)) != 0, "Ground mask should include terrain");
            Assert.IsTrue((mask & (1 << LayerSetup.InfrastructureLayer)) != 0, "Ground mask should include infrastructure");
        }
    }

    [TestFixture]
    public class SpriteGeneratorEditModeTests
    {
        [SetUp]
        public void SetUp()
        {
            SpriteGenerator.Initialize();
        }

        [Test]
        public void GetSprite_Player_ReturnsNonNull()
        {
            Sprite sprite = SpriteGenerator.GetSprite("Player");
            Assert.IsNotNull(sprite, "Player sprite should exist");
        }

        [Test]
        public void GetSprite_Terrain_ReturnsNonNull()
        {
            Sprite sprite = SpriteGenerator.GetSprite("Terrain");
            Assert.IsNotNull(sprite, "Terrain sprite should exist");
        }

        [Test]
        public void GetSprite_AllParticleTypes_ReturnNonNull()
        {
            string[] particleTypes = { "Sand", "WetSand", "Water", "Gold", "Slag" };

            foreach (string type in particleTypes)
            {
                Sprite sprite = SpriteGenerator.GetSprite(type);
                Assert.IsNotNull(sprite, $"{type} sprite should exist");
            }
        }

        [Test]
        public void GetSprite_AllInfrastructure_ReturnNonNull()
        {
            string[] infraTypes = { "Wall", "Belt", "Lift", "Shaker", "GoldStore" };

            foreach (string type in infraTypes)
            {
                Sprite sprite = SpriteGenerator.GetSprite(type);
                Assert.IsNotNull(sprite, $"{type} sprite should exist");
            }
        }

        [Test]
        public void GetParticleMaterial_ReturnsNonNull()
        {
            PhysicsMaterial2D material = SpriteGenerator.GetParticleMaterial();
            Assert.IsNotNull(material);
        }

        [Test]
        public void GetParticleMaterial_HasCorrectProperties()
        {
            PhysicsMaterial2D material = SpriteGenerator.GetParticleMaterial();
            Assert.AreEqual(GameSettings.ParticleBounciness, material.bounciness, 0.001f);
            Assert.AreEqual(GameSettings.ParticleFriction, material.friction, 0.001f);
        }
    }
}
