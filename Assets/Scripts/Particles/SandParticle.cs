using UnityEngine;
using GoldRush.Core;

namespace GoldRush.Particles
{
    public class SandParticle : MonoBehaviour
    {
        private Rigidbody2D rb;
        private SpriteRenderer sr;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            sr = GetComponent<SpriteRenderer>();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            // Check if colliding with water
            WaterParticle water = collision.gameObject.GetComponent<WaterParticle>();
            if (water != null)
            {
                // Convert to wet sand
                ConvertToWetSand();

                // Return water to pool
                water.ReturnToPool();
            }
        }

        private void ConvertToWetSand()
        {
            // Get position and velocity before returning
            Vector2 pos = transform.position;
            Vector2 vel = rb.linearVelocity;

            // Return this sand particle to pool
            ParticlePool.Instance.Return(gameObject, ParticleType.Sand);

            // Get wet sand particle from pool
            GameObject wetSand = ParticlePool.Instance.Get(ParticleType.WetSand);
            wetSand.transform.position = pos;

            Rigidbody2D wetRb = wetSand.GetComponent<Rigidbody2D>();
            if (wetRb != null)
            {
                wetRb.linearVelocity = vel;
            }
        }

        public void ReturnToPool()
        {
            ParticlePool.Instance.Return(gameObject, ParticleType.Sand);
        }
    }
}
