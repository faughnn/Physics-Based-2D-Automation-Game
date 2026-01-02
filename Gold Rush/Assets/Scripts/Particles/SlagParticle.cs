using UnityEngine;
using GoldRush.Core;

namespace GoldRush.Particles
{
    public class SlagParticle : MonoBehaviour
    {
        private Rigidbody2D rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        public void ApplyHorizontalPush(float speed)
        {
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(speed, rb.linearVelocity.y);
            }
        }

        public void ReturnToPool()
        {
            ParticlePool.Instance.Return(gameObject, ParticleType.Slag);
        }
    }
}
