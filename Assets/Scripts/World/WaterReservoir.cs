using UnityEngine;
using GoldRush.Core;
using GoldRush.Particles;

namespace GoldRush.World
{
    public class WaterReservoir : MonoBehaviour
    {
        private float spawnTimer;
        private float spawnInterval;
        private Vector2 spawnAreaMin;
        private Vector2 spawnAreaMax;

        public void Initialize()
        {
            spawnInterval = 1f / GameSettings.WaterSpawnRate;
            spawnTimer = 0f;

            // Calculate spawn area (top of the map, spanning the width)
            float worldWidth = (GameSettings.WorldWidthCells * GameSettings.GridSize) / GameSettings.PixelsPerUnit;
            float worldHeight = (GameSettings.WorldHeightCells * GameSettings.GridSize) / GameSettings.PixelsPerUnit;
            float reservoirHeight = (GameSettings.WaterReservoirHeight * GameSettings.GridSize) / GameSettings.PixelsPerUnit;

            spawnAreaMin = new Vector2(-worldWidth / 2 + 1f, worldHeight / 2 - reservoirHeight);
            spawnAreaMax = new Vector2(worldWidth / 2 - 1f, worldHeight / 2 - 0.5f);

            // Create visual indicator for water reservoir
            CreateVisual();

            Debug.Log($"WaterReservoir: Initialized, spawning {GameSettings.WaterSpawnRate} particles/sec");
        }

        private void CreateVisual()
        {
            // Create a semi-transparent blue rectangle to show the reservoir area
            GameObject visual = new GameObject("ReservoirVisual");
            visual.transform.SetParent(transform);

            SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteGenerator.GetSprite("WaterReservoir");
            sr.sortingOrder = -1; // Behind everything

            // Position at top of world
            float worldHeight = (GameSettings.WorldHeightCells * GameSettings.GridSize) / GameSettings.PixelsPerUnit;
            float reservoirHeight = (GameSettings.WaterReservoirHeight * GameSettings.GridSize) / GameSettings.PixelsPerUnit;
            visual.transform.position = new Vector2(0, worldHeight / 2 - reservoirHeight / 2);
        }

        private void Update()
        {
            if (ParticlePool.Instance == null) return;

            spawnTimer += Time.deltaTime;

            while (spawnTimer >= spawnInterval)
            {
                spawnTimer -= spawnInterval;
                SpawnWaterParticle();
            }
        }

        private void SpawnWaterParticle()
        {
            GameObject water = ParticlePool.Instance.Get(ParticleType.Water);

            // Random position within spawn area
            float x = Random.Range(spawnAreaMin.x, spawnAreaMax.x);
            float y = Random.Range(spawnAreaMin.y, spawnAreaMax.y);
            water.transform.position = new Vector2(x, y);

            // Small random initial velocity
            Rigidbody2D rb = water.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(-1f, 0f));
            }
        }
    }
}
