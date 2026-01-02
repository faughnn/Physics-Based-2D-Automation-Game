using UnityEngine;
using GoldRush.Core;

namespace GoldRush.Particles
{
    public class WetSandParticle : MonoBehaviour
    {
        private Rigidbody2D rb;
        private bool isBeingProcessed;
        private float processTimer;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            isBeingProcessed = false;
            processTimer = 0f;
        }

        public void StartProcessing()
        {
            isBeingProcessed = true;
            processTimer = 0f;
        }

        public void StopProcessing()
        {
            isBeingProcessed = false;
            processTimer = 0f;
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
