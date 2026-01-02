using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GoldRush.Core;
using GoldRush.World;
using GoldRush.Player;
using GoldRush.Particles;
using GoldRush.Building;
using GoldRush.Infrastructure;

namespace GoldRush.Tests.PlayMode
{
    [TestFixture]
    public class WorldGeneratorIntegrationTests
    {
        private GameObject gameManagerGO;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Create GameManager to bootstrap everything
            gameManagerGO = new GameObject("GameManager");
            gameManagerGO.AddComponent<GameManager>();
            yield return null; // Wait a frame for Awake to complete
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (gameManagerGO != null)
                Object.Destroy(gameManagerGO);

            // Clean up all GameObjects
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go != null && go.scene.IsValid())
                    Object.Destroy(go);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator WorldGenerator_CreatesTerrainBlocks()
        {
            yield return null;

            var terrainBlocks = Object.FindObjectsByType<TerrainBlock>(FindObjectsSortMode.None);
            Assert.Greater(terrainBlocks.Length, 0, "Should have created terrain blocks");
        }

        [UnityTest]
        public IEnumerator WorldGenerator_TerrainStartsAtCorrectRow()
        {
            yield return null;

            int terrainStartRow = GameSettings.WaterReservoirHeight + GameSettings.AirHeight;
            var terrainBlocks = Object.FindObjectsByType<TerrainBlock>(FindObjectsSortMode.None);

            foreach (var block in terrainBlocks)
            {
                Assert.GreaterOrEqual(block.GridY, terrainStartRow,
                    $"Terrain at ({block.GridX}, {block.GridY}) is above terrain start row {terrainStartRow}");
            }
        }

        [UnityTest]
        public IEnumerator Player_IsSpawned()
        {
            yield return null;

            var player = Object.FindFirstObjectByType<PlayerController>();
            Assert.IsNotNull(player, "Player should be spawned");
        }

        [UnityTest]
        public IEnumerator Player_HasRequiredComponents()
        {
            yield return null;

            var player = Object.FindFirstObjectByType<PlayerController>();
            Assert.IsNotNull(player.GetComponent<Rigidbody2D>(), "Player should have Rigidbody2D");
            Assert.IsNotNull(player.GetComponent<BoxCollider2D>(), "Player should have BoxCollider2D");
            Assert.IsNotNull(player.GetComponent<SpriteRenderer>(), "Player should have SpriteRenderer");
            Assert.IsNotNull(player.GetComponent<PlayerInput>(), "Player should have PlayerInput");
        }

        [UnityTest]
        public IEnumerator ParticlePool_IsInitialized()
        {
            yield return null;

            Assert.IsNotNull(ParticlePool.Instance, "ParticlePool singleton should exist");
        }

        [UnityTest]
        public IEnumerator BuildSystem_IsInitialized()
        {
            yield return null;

            Assert.IsNotNull(BuildSystem.Instance, "BuildSystem singleton should exist");
        }
    }

    [TestFixture]
    public class ParticleSystemIntegrationTests
    {
        private GameObject gameManagerGO;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            gameManagerGO = new GameObject("GameManager");
            gameManagerGO.AddComponent<GameManager>();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (gameManagerGO != null)
                Object.Destroy(gameManagerGO);

            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go != null && go.scene.IsValid())
                    Object.Destroy(go);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator ParticlePool_Get_ReturnsActiveParticle()
        {
            yield return null;

            GameObject sand = ParticlePool.Instance.Get(ParticleType.Sand);

            Assert.IsNotNull(sand);
            Assert.IsTrue(sand.activeInHierarchy);
        }

        [UnityTest]
        public IEnumerator ParticlePool_Return_DeactivatesParticle()
        {
            yield return null;

            GameObject sand = ParticlePool.Instance.Get(ParticleType.Sand);
            ParticlePool.Instance.Return(sand, ParticleType.Sand);

            Assert.IsFalse(sand.activeInHierarchy);
        }

        [UnityTest]
        public IEnumerator SandParticle_HasPhysicsComponents()
        {
            yield return null;

            GameObject sand = ParticlePool.Instance.Get(ParticleType.Sand);

            Assert.IsNotNull(sand.GetComponent<Rigidbody2D>(), "Sand should have Rigidbody2D");
            Assert.IsNotNull(sand.GetComponent<CircleCollider2D>(), "Sand should have CircleCollider2D");
            Assert.IsNotNull(sand.GetComponent<SandParticle>(), "Sand should have SandParticle component");
        }

        [UnityTest]
        public IEnumerator SandParticle_FallsWithGravity()
        {
            yield return null;

            GameObject sand = ParticlePool.Instance.Get(ParticleType.Sand);
            sand.transform.position = new Vector3(0, 5, 0);
            float initialY = sand.transform.position.y;

            // Wait for physics
            yield return new WaitForSeconds(0.5f);

            Assert.Less(sand.transform.position.y, initialY, "Sand should fall due to gravity");
        }

        [UnityTest]
        public IEnumerator WaterParticle_IsCreatedCorrectly()
        {
            yield return null;

            GameObject water = ParticlePool.Instance.Get(ParticleType.Water);

            Assert.IsNotNull(water);
            Assert.IsNotNull(water.GetComponent<WaterParticle>());
            Assert.AreEqual(LayerSetup.WaterLayer, water.layer);
        }

        [UnityTest]
        public IEnumerator AllParticleTypes_CanBeCreated()
        {
            yield return null;

            foreach (ParticleType type in System.Enum.GetValues(typeof(ParticleType)))
            {
                GameObject particle = ParticlePool.Instance.Get(type);
                Assert.IsNotNull(particle, $"Should be able to create {type} particle");
                Assert.IsTrue(particle.activeInHierarchy, $"{type} particle should be active");
            }
        }
    }

    [TestFixture]
    public class DiggingIntegrationTests
    {
        private GameObject gameManagerGO;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            gameManagerGO = new GameObject("GameManager");
            gameManagerGO.AddComponent<GameManager>();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (gameManagerGO != null)
                Object.Destroy(gameManagerGO);

            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go != null && go.scene.IsValid())
                    Object.Destroy(go);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator DigTerrain_DestroysBlock()
        {
            yield return null;

            // Find a terrain block
            var terrainBlocks = Object.FindObjectsByType<TerrainBlock>(FindObjectsSortMode.None);
            Assert.Greater(terrainBlocks.Length, 0);

            TerrainBlock block = terrainBlocks[0];
            int gridX = block.GridX;
            int gridY = block.GridY;
            int initialCount = terrainBlocks.Length;

            // Dig it
            GameManager.Instance.DigTerrainAt(gridX, gridY);
            yield return null;

            // Verify block is gone
            var remainingBlocks = Object.FindObjectsByType<TerrainBlock>(FindObjectsSortMode.None);
            Assert.AreEqual(initialCount - 1, remainingBlocks.Length, "One block should be destroyed");
        }

        [UnityTest]
        public IEnumerator DigTerrain_SpawnsSandParticles()
        {
            yield return null;

            int initialSandCount = ParticlePool.Instance.GetActiveCount(ParticleType.Sand);

            // Find and dig a terrain block
            var terrainBlocks = Object.FindObjectsByType<TerrainBlock>(FindObjectsSortMode.None);
            TerrainBlock block = terrainBlocks[0];
            GameManager.Instance.DigTerrainAt(block.GridX, block.GridY);

            yield return null;

            int newSandCount = ParticlePool.Instance.GetActiveCount(ParticleType.Sand);
            Assert.Greater(newSandCount, initialSandCount, "Digging should spawn sand particles");
        }
    }

    [TestFixture]
    public class InfrastructureIntegrationTests
    {
        private GameObject gameManagerGO;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            gameManagerGO = new GameObject("GameManager");
            gameManagerGO.AddComponent<GameManager>();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (gameManagerGO != null)
                Object.Destroy(gameManagerGO);

            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go != null && go.scene.IsValid())
                    Object.Destroy(go);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator Wall_CanBePlaced()
        {
            yield return null;

            GameObject wall = Wall.Create(15, 10);

            Assert.IsNotNull(wall);
            Assert.IsNotNull(wall.GetComponent<Wall>());
            Assert.IsNotNull(wall.GetComponent<BoxCollider2D>());
        }

        [UnityTest]
        public IEnumerator Belt_CanBePlaced()
        {
            yield return null;

            GameObject belt = Belt.Create(15, 10, true);

            Assert.IsNotNull(belt);
            Assert.IsNotNull(belt.GetComponent<Belt>());
        }

        [UnityTest]
        public IEnumerator Belt_MovesParticlesHorizontally()
        {
            yield return null;

            // Create belt
            GameObject beltGO = Belt.Create(15, 8, true); // Moving right
            Vector2 beltPos = beltGO.transform.position;

            // Create sand particle above belt
            GameObject sand = ParticlePool.Instance.Get(ParticleType.Sand);
            sand.transform.position = beltPos + Vector2.up * 0.5f;

            float initialX = sand.transform.position.x;

            // Wait for physics
            yield return new WaitForSeconds(1f);

            // Particle should have moved right
            Assert.Greater(sand.transform.position.x, initialX, "Belt should move sand to the right");
        }

        [UnityTest]
        public IEnumerator Lift_CanBePlaced()
        {
            yield return null;

            GameObject lift = Lift.Create(15, 10, true);

            Assert.IsNotNull(lift);
            Assert.IsNotNull(lift.GetComponent<Lift>());
        }

        [UnityTest]
        public IEnumerator GoldStore_CanBePlaced()
        {
            yield return null;

            GameObject store = GoldStore.Create(15, 10);

            Assert.IsNotNull(store);
            Assert.IsNotNull(store.GetComponent<GoldStore>());
        }

        [UnityTest]
        public IEnumerator Shaker_CanBePlaced()
        {
            yield return null;

            GameObject shaker = Shaker.Create(15, 10, true);

            Assert.IsNotNull(shaker);
            Assert.IsNotNull(shaker.GetComponent<Shaker>());
        }
    }
}
