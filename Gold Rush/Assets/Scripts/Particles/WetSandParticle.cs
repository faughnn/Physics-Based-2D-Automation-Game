using UnityEngine;
using GoldRush.Core;

namespace GoldRush.Particles
{
    public class WetSandParticle : MonoBehaviour
    {
        private Rigidbody2D rb;
        private bool isBeingProcessed;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            isBeingProcessed = false;
        }

        public void StartProcessing()
        {
            isBeingProcessed = true;
        }

        public void StopProcessing()
        {
            isBeingProcessed = false;
        }

        public bool IsBeingProcessed => isBeingProcessed;

        public void ApplyHorizontalPush(float speed)
        {
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(speed, rb.linearVelocity.y);
            }
        }

        public void ReturnToPool()
        {
            isBeingProcessed = false;
            ParticlePool.Instance.Return(gameObject, ParticleType.WetSand);
        }
    }
}
