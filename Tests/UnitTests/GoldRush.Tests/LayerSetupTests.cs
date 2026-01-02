using Xunit;

namespace GoldRush.Tests
{
    // Copy of LayerSetup constants for testing
    public static class LayerSetup
    {
        public const int DefaultLayer = 0;
        public const int PlayerLayer = 8;
        public const int TerrainLayer = 9;
        public const int SandLayer = 10;
        public const int WetSandLayer = 11;
        public const int GoldLayer = 12;
        public const int SlagLayer = 13;
        public const int WaterLayer = 14;
        public const int InfrastructureLayer = 15;

        public static int GetTerrainMask() => 1 << TerrainLayer;
        public static int GetInfrastructureMask() => 1 << InfrastructureLayer;
        public static int GetGroundMask() => (1 << TerrainLayer) | (1 << InfrastructureLayer);
    }

    public class LayerSetupTests
    {
        [Fact]
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

            var uniqueLayers = new HashSet<int>(layers);
            Assert.Equal(layers.Length, uniqueLayers.Count);
        }

        [Fact]
        public void CustomLayers_StartAtLayer8()
        {
            // Unity reserves layers 0-7, custom should start at 8
            Assert.True(LayerSetup.PlayerLayer >= 8);
            Assert.True(LayerSetup.TerrainLayer >= 8);
            Assert.True(LayerSetup.SandLayer >= 8);
        }

        [Fact]
        public void Layers_AreWithinUnityRange()
        {
            // Unity supports layers 0-31
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
                Assert.True(layer >= 0 && layer <= 31, $"Layer {layer} is out of Unity range (0-31)");
            }
        }

        [Fact]
        public void GetTerrainMask_ReturnsSingleBit()
        {
            int mask = LayerSetup.GetTerrainMask();
            int expected = 1 << LayerSetup.TerrainLayer;

            Assert.Equal(expected, mask);
        }

        [Fact]
        public void GetGroundMask_IncludesTerrainAndInfrastructure()
        {
            int mask = LayerSetup.GetGroundMask();

            // Check terrain bit is set
            Assert.True((mask & (1 << LayerSetup.TerrainLayer)) != 0);

            // Check infrastructure bit is set
            Assert.True((mask & (1 << LayerSetup.InfrastructureLayer)) != 0);
        }

        [Fact]
        public void ParticleLayers_AreContiguous()
        {
            // Sand, WetSand, Gold, Slag should be adjacent for easier collision matrix setup
            Assert.Equal(LayerSetup.SandLayer + 1, LayerSetup.WetSandLayer);
            Assert.Equal(LayerSetup.WetSandLayer + 1, LayerSetup.GoldLayer);
            Assert.Equal(LayerSetup.GoldLayer + 1, LayerSetup.SlagLayer);
        }
    }
}
