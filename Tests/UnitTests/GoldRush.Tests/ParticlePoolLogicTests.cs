using Xunit;
using System.Collections.Generic;

namespace GoldRush.Tests
{
    // Test the pool logic without Unity dependencies
    public enum ParticleType
    {
        Sand,
        WetSand,
        Water,
        Gold,
        Slag
    }

    // Simplified pool for testing the logic
    public class MockParticlePool
    {
        private Dictionary<ParticleType, Queue<int>> pools = new();
        private Dictionary<ParticleType, List<int>> active = new();
        private Dictionary<ParticleType, int> maxCounts = new();
        private int nextId = 0;

        public MockParticlePool()
        {
            foreach (ParticleType type in Enum.GetValues(typeof(ParticleType)))
            {
                pools[type] = new Queue<int>();
                active[type] = new List<int>();
                maxCounts[type] = type switch
                {
                    ParticleType.Sand => GameSettings.MaxSandParticles,
                    ParticleType.WetSand => GameSettings.MaxWetSandParticles,
                    ParticleType.Water => GameSettings.MaxWaterParticles,
                    ParticleType.Gold => GameSettings.MaxGoldParticles,
                    ParticleType.Slag => GameSettings.MaxSlagParticles,
                    _ => 100
                };
            }
        }

        public int Get(ParticleType type)
        {
            int id;
            if (pools[type].Count > 0)
            {
                id = pools[type].Dequeue();
            }
            else if (active[type].Count < maxCounts[type])
            {
                id = nextId++;
            }
            else
            {
                // Recycle oldest
                id = active[type][0];
                active[type].RemoveAt(0);
            }
            active[type].Add(id);
            return id;
        }

        public void Return(int id, ParticleType type)
        {
            active[type].Remove(id);
            pools[type].Enqueue(id);
        }

        public int GetActiveCount(ParticleType type) => active[type].Count;
        public int GetPoolCount(ParticleType type) => pools[type].Count;
        public int GetMaxCount(ParticleType type) => maxCounts[type];
    }

    public class ParticlePoolLogicTests
    {
        [Fact]
        public void Get_FromEmptyPool_CreatesNewParticle()
        {
            var pool = new MockParticlePool();

            int id = pool.Get(ParticleType.Sand);

            Assert.Equal(1, pool.GetActiveCount(ParticleType.Sand));
        }

        [Fact]
        public void Return_AddsToPool()
        {
            var pool = new MockParticlePool();
            int id = pool.Get(ParticleType.Sand);

            pool.Return(id, ParticleType.Sand);

            Assert.Equal(0, pool.GetActiveCount(ParticleType.Sand));
            Assert.Equal(1, pool.GetPoolCount(ParticleType.Sand));
        }

        [Fact]
        public void Get_AfterReturn_ReusesParticle()
        {
            var pool = new MockParticlePool();
            int id1 = pool.Get(ParticleType.Sand);
            pool.Return(id1, ParticleType.Sand);

            int id2 = pool.Get(ParticleType.Sand);

            Assert.Equal(id1, id2); // Should reuse the same ID
        }

        [Fact]
        public void Get_AtMaxCapacity_RecyclesOldest()
        {
            var pool = new MockParticlePool();
            int max = pool.GetMaxCount(ParticleType.Sand);

            // Fill to capacity
            var ids = new List<int>();
            for (int i = 0; i < max; i++)
            {
                ids.Add(pool.Get(ParticleType.Sand));
            }

            Assert.Equal(max, pool.GetActiveCount(ParticleType.Sand));

            // Get one more - should recycle
            int newId = pool.Get(ParticleType.Sand);

            Assert.Equal(max, pool.GetActiveCount(ParticleType.Sand)); // Still at max
            Assert.Equal(ids[0], newId); // Should have recycled the oldest
        }

        [Fact]
        public void DifferentTypes_HaveSeparatePools()
        {
            var pool = new MockParticlePool();

            pool.Get(ParticleType.Sand);
            pool.Get(ParticleType.Sand);
            pool.Get(ParticleType.Water);

            Assert.Equal(2, pool.GetActiveCount(ParticleType.Sand));
            Assert.Equal(1, pool.GetActiveCount(ParticleType.Water));
            Assert.Equal(0, pool.GetActiveCount(ParticleType.Gold));
        }

        [Fact]
        public void MaxCounts_MatchGameSettings()
        {
            var pool = new MockParticlePool();

            Assert.Equal(GameSettings.MaxSandParticles, pool.GetMaxCount(ParticleType.Sand));
            Assert.Equal(GameSettings.MaxWetSandParticles, pool.GetMaxCount(ParticleType.WetSand));
            Assert.Equal(GameSettings.MaxWaterParticles, pool.GetMaxCount(ParticleType.Water));
            Assert.Equal(GameSettings.MaxGoldParticles, pool.GetMaxCount(ParticleType.Gold));
            Assert.Equal(GameSettings.MaxSlagParticles, pool.GetMaxCount(ParticleType.Slag));
        }

        [Fact]
        public void AllParticleTypes_AreSupported()
        {
            var pool = new MockParticlePool();

            foreach (ParticleType type in Enum.GetValues(typeof(ParticleType)))
            {
                int id = pool.Get(type);
                Assert.Equal(1, pool.GetActiveCount(type));
                pool.Return(id, type);
                Assert.Equal(0, pool.GetActiveCount(type));
            }
        }
    }
}
