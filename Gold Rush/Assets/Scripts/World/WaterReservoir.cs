using UnityEngine;
using GoldRush.Core;
using GoldRush.Particles;

namespace GoldRush.World
{
    public class WaterReservoir : MonoBehaviour
    {
        private Vector2 spawnAreaMin;
        private Vector2 spawnAreaMax;

        public void Initialize()
        {
            // Calculate spawn area based on reservoir interior bounds
            // Convert grid positions to world positions
            Vector2 minGridWorld = GameSettings.GridToWorld(GameSettings.ReservoirInteriorMinX, GameSettings.ReservoirInteriorMaxY);
            Vector2 maxGridWorld = GameSettings.GridToWorld(GameSettings.ReservoirInteriorMaxX, GameSettings.ReservoirInteriorMinY);

            // Add small padding to keep water away from edges
            float padding = 0.1f;
            spawnAreaMin = new Vector2(minGridWorld.x + padding, minGridWorld.y + padding);
            spawnAreaMax = new Vector2(maxGridWorld.x - padding, maxGridWorld.y - padding);

            // Pre-fill the reservoir with water particles
            PreFillReservoir();

            Debug.Log($"WaterReservoir: Initialized with {GameSettings.ReservoirPreFillCount} water particles");
        }

        private void PreFillReservoir()
        {
            if (ParticlePool.Instance == null)
            {
                Debug.LogWarning("WaterReservoir: ParticlePool not available for pre-fill");
                return;
            }

            for (int i = 0; i < GameSettings.ReservoirPreFillCount; i++)
            {
                SpawnWaterParticle();
            }
        }

        private void SpawnWaterParticle()
        {
            GameObject water = ParticlePool.Instance.Get(ParticleType.Water);
            if (water == null) return;

            // Random position within reservoir interior
            float x = Random.Range(spawnAreaMin.x, spawnAreaMax.x);
            float y = Random.Range(spawnAreaMin.y, spawnAreaMax.y);
            water.transform.position = new Vector2(x, y);

            // Start with zero velocity (water is contained)
            Rigidbody2D rb = water.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
    }
}
