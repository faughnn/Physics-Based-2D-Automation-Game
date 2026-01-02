using UnityEngine;
using System.Collections.Generic;
using GoldRush.Core;
using GoldRush.Particles;

namespace GoldRush.Infrastructure
{
    public class Shaker : MonoBehaviour
    {
        public int GridX { get; private set; }
        public int GridY { get; private set; }
        public bool PushesRight { get; private set; }

        private float pushSpeed;
        private float shakeAmount = 0.02f;
        private float shakeSpeed = 30f;
        private Vector2 originalPosition;

        private Dictionary<WetSandParticle, float> processingParticles = new Dictionary<WetSandParticle, float>();
        private List<WetSandParticle> toRemove = new List<WetSandParticle>();

        public static GameObject Create(int gridX, int gridY, bool pushesRight, Transform parent = null)
        {
            GameObject shakerGO = new GameObject($"Shaker_{gridX}_{gridY}");
            if (parent != null) shakerGO.transform.SetParent(parent);

            // Position (shaker is 32x16, positioned at bottom of cell)
            Vector2 worldPos = GameSettings.GridToWorld(gridX, gridY);
            worldPos.y -= (GameSettings.GridSize - 16) / 2f / GameSettings.PixelsPerUnit;
            shakerGO.transform.position = worldPos;

            // Sprite
            SpriteRenderer sr = shakerGO.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteGenerator.CreateArrowSprite(GameSettings.GridSize, 16, GameSettings.ShakerColor, true, pushesRight);
            sr.sortingOrder = 1;

            // Trigger collider for detecting particles
            BoxCollider2D triggerCol = shakerGO.AddComponent<BoxCollider2D>();
            triggerCol.size = new Vector2(GameSettings.GridSize / GameSettings.PixelsPerUnit, 16f / GameSettings.PixelsPerUnit);
            triggerCol.isTrigger = true;

            // Solid top surface for particles to rest on
            GameObject solidTop = new GameObject("SolidTop");
            solidTop.transform.SetParent(shakerGO.transform);
            solidTop.transform.localPosition = new Vector3(0, 8f / GameSettings.PixelsPerUnit, 0);
            BoxCollider2D solidCol = solidTop.AddComponent<BoxCollider2D>();
            solidCol.size = new Vector2(GameSettings.GridSize / GameSettings.PixelsPerUnit, 2f / GameSettings.PixelsPerUnit);
            solidTop.layer = LayerSetup.InfrastructureLayer;

            // Layer
            shakerGO.layer = LayerSetup.InfrastructureLayer;

            // Component
            Shaker shaker = shakerGO.AddComponent<Shaker>();
            shaker.GridX = gridX;
            shaker.GridY = gridY;
            shaker.PushesRight = pushesRight;
            shaker.pushSpeed = pushesRight ? GameSettings.ShakerPushSpeed : -GameSettings.ShakerPushSpeed;
            shaker.originalPosition = worldPos;

            return shakerGO;
        }

        private void Update()
        {
            // Vibrate effect
            float xOffset = Mathf.Sin(Time.time * shakeSpeed) * shakeAmount;
            transform.position = originalPosition + new Vector2(xOffset, 0);

            // Process wet sand particles
            ProcessParticles();
        }

        private void ProcessParticles()
        {
            toRemove.Clear();

            foreach (var kvp in processingParticles)
            {
                WetSandParticle wetSand = kvp.Key;
                float timer = kvp.Value + Time.deltaTime;

                if (wetSand == null || !wetSand.gameObject.activeInHierarchy)
                {
                    toRemove.Add(wetSand);
                    continue;
                }

                if (timer >= GameSettings.ShakerProcessTime)
                {
                    // Processing complete - spawn gold and slag
                    CompleteProcessing(wetSand);
                    toRemove.Add(wetSand);
                }
                else
                {
                    processingParticles[wetSand] = timer;
                }
            }

            foreach (var particle in toRemove)
            {
                processingParticles.Remove(particle);
            }
        }

        private void CompleteProcessing(WetSandParticle wetSand)
        {
            Vector2 pos = wetSand.transform.position;

            // Return wet sand to pool
            wetSand.ReturnToPool();

            // Spawn gold particle (below the shaker)
            if (ParticlePool.Instance != null)
            {
                GameObject gold = ParticlePool.Instance.Get(ParticleType.Gold);
                gold.transform.position = pos + Vector2.down * 0.3f;

                // Spawn slag particle (on top of shaker)
                GameObject slag = ParticlePool.Instance.Get(ParticleType.Slag);
                slag.transform.position = pos + Vector2.up * 0.1f;

                // Push slag in shaker direction
                SlagParticle slagComp = slag.GetComponent<SlagParticle>();
                if (slagComp != null)
                {
                    slagComp.ApplyHorizontalPush(pushSpeed * 2f);
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            WetSandParticle wetSand = other.GetComponent<WetSandParticle>();
            if (wetSand != null && !processingParticles.ContainsKey(wetSand))
            {
                processingParticles[wetSand] = 0f;
                wetSand.StartProcessing();
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            // Apply horizontal push to wet sand and slag
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                WetSandParticle wetSand = other.GetComponent<WetSandParticle>();
                SlagParticle slag = other.GetComponent<SlagParticle>();

                if (wetSand != null || slag != null)
                {
                    rb.linearVelocity = new Vector2(pushSpeed, rb.linearVelocity.y);
                }
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            WetSandParticle wetSand = other.GetComponent<WetSandParticle>();
            if (wetSand != null)
            {
                processingParticles.Remove(wetSand);
                wetSand.StopProcessing();
            }
        }
    }
}
