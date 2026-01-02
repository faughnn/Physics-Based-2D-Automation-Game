using UnityEngine;
using GoldRush.Core;

namespace GoldRush.Particles
{
    public class GoldParticle : MonoBehaviour
    {
        private Rigidbody2D rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        public void ReturnToPool()
        {
            ParticlePool.Instance.Return(gameObject, ParticleType.Gold);
        }
    }
}
