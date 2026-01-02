using UnityEngine;
using GoldRush.Core;

namespace GoldRush.Particles
{
    public class WaterParticle : MonoBehaviour
    {
        private Rigidbody2D rb;
        private float lifetime;
        private const float MaxLifetime = 30f; // Auto-recycle after 30 seconds

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            lifetime = 0f;
        }

        private void Update()
        {
            // Track lifetime to prevent water from accumulating forever
            lifetime += Time.deltaTime;
            if (lifetime > MaxLifetime)
            {
                ReturnToPool();
            }
        }

        public void ReturnToPool()
        {
            ParticlePool.Instance.Return(gameObject, ParticleType.Water);
        }
    }
}
