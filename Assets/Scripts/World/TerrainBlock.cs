using UnityEngine;
using GoldRush.Core;
using GoldRush.Particles;

namespace GoldRush.World
{
    public class TerrainBlock : MonoBehaviour
    {
        public int GridX { get; private set; }
        public int GridY { get; private set; }
        public bool IsDug { get; private set; }

        public void Initialize(int gridX, int gridY)
        {
            GridX = gridX;
            GridY = gridY;
            IsDug = false;
        }

        public void Dig()
        {
            if (IsDug) return;

            IsDug = true;

            // Spawn sand particles
            SpawnSandParticles();

            // Destroy this block
            Destroy(gameObject);
        }

        private void SpawnSandParticles()
        {
            if (ParticlePool.Instance == null) return;

            Vector2 spawnPos = transform.position;
            float spreadRadius = (GameSettings.GridSize / GameSettings.PixelsPerUnit) * 0.4f;

            for (int i = 0; i < GameSettings.SandPerBlock; i++)
            {
                GameObject sand = ParticlePool.Instance.Get(ParticleType.Sand);

                // Randomize position within the block area
                Vector2 offset = Random.insideUnitCircle * spreadRadius;
                sand.transform.position = spawnPos + offset;

                // Add slight random velocity for natural spreading
                Rigidbody2D rb = sand.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = new Vector2(Random.Range(-1f, 1f), Random.Range(-0.5f, 0.5f));
                }
            }
        }
    }
}
