using UnityEngine;
using System.Collections.Generic;
using GoldRush.Core;

namespace GoldRush.Particles
{
    public enum ParticleType
    {
        Sand,
        WetSand,
        Water,
        Gold,
        Slag
    }

    public class ParticlePool : MonoBehaviour
    {
        public static ParticlePool Instance { get; private set; }

        private Dictionary<ParticleType, Queue<GameObject>> pools;
        private Dictionary<ParticleType, GameObject> prefabs;
        private Dictionary<ParticleType, Transform> parents;
        private Dictionary<ParticleType, int> maxCounts;
        private Dictionary<ParticleType, List<GameObject>> activeParticles;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            Initialize();
        }

        private void Initialize()
        {
            pools = new Dictionary<ParticleType, Queue<GameObject>>();
            prefabs = new Dictionary<ParticleType, GameObject>();
            parents = new Dictionary<ParticleType, Transform>();
            activeParticles = new Dictionary<ParticleType, List<GameObject>>();

            maxCounts = new Dictionary<ParticleType, int>
            {
                { ParticleType.Sand, GameSettings.MaxSandParticles },
                { ParticleType.WetSand, GameSettings.MaxWetSandParticles },
                { ParticleType.Water, GameSettings.MaxWaterParticles },
                { ParticleType.Gold, GameSettings.MaxGoldParticles },
                { ParticleType.Slag, GameSettings.MaxSlagParticles }
            };

            // Create prefabs and pools for each type
            foreach (ParticleType type in System.Enum.GetValues(typeof(ParticleType)))
            {
                CreatePrefab(type);
                CreatePool(type);
            }

            Debug.Log("ParticlePool: Initialized all particle pools");
        }

        private void CreatePrefab(ParticleType type)
        {
            GameObject prefab = new GameObject($"{type}Prefab");
            prefab.SetActive(false);
            prefab.transform.SetParent(transform);

            // Add sprite renderer
            SpriteRenderer sr = prefab.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteGenerator.GetSprite(type.ToString());
            sr.sortingOrder = 5;

            // Add rigidbody
            Rigidbody2D rb = prefab.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.mass = GameSettings.ParticleMass;
            rb.linearDamping = GameSettings.ParticleLinearDrag;
            rb.gravityScale = GameSettings.ParticleGravityScale;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            // Add circle collider
            CircleCollider2D col = prefab.AddComponent<CircleCollider2D>();
            col.radius = GameSettings.ParticleRadius / GameSettings.PixelsPerUnit;
            col.sharedMaterial = SpriteGenerator.GetParticleMaterial();

            // Set layer based on type
            prefab.layer = GetLayerForType(type);

            // Add particle behavior script
            switch (type)
            {
                case ParticleType.Sand:
                    prefab.AddComponent<SandParticle>();
                    break;
                case ParticleType.WetSand:
                    prefab.AddComponent<WetSandParticle>();
                    break;
                case ParticleType.Water:
                    prefab.AddComponent<WaterParticle>();
                    break;
                case ParticleType.Gold:
                    prefab.AddComponent<GoldParticle>();
                    break;
                case ParticleType.Slag:
                    prefab.AddComponent<SlagParticle>();
                    break;
            }

            prefabs[type] = prefab;
        }

        private void CreatePool(ParticleType type)
        {
            // Create parent object for organization
            GameObject parent = new GameObject($"{type}Particles");
            parent.transform.SetParent(transform);
            parents[type] = parent.transform;

            // Create pool
            pools[type] = new Queue<GameObject>();
            activeParticles[type] = new List<GameObject>();

            // Pre-instantiate some particles
            int preloadCount = maxCounts[type] / 4; // Preload 25%
            for (int i = 0; i < preloadCount; i++)
            {
                GameObject particle = CreateParticle(type);
                particle.SetActive(false);
                pools[type].Enqueue(particle);
            }
        }

        private GameObject CreateParticle(ParticleType type)
        {
            GameObject particle = Instantiate(prefabs[type], parents[type]);
            particle.name = $"{type}_{activeParticles[type].Count + pools[type].Count}";
            return particle;
        }

        private int GetLayerForType(ParticleType type)
        {
            switch (type)
            {
                case ParticleType.Sand: return LayerSetup.SandLayer;
                case ParticleType.WetSand: return LayerSetup.WetSandLayer;
                case ParticleType.Water: return LayerSetup.WaterLayer;
                case ParticleType.Gold: return LayerSetup.GoldLayer;
                case ParticleType.Slag: return LayerSetup.SlagLayer;
                default: return 0;
            }
        }

        public GameObject Get(ParticleType type)
        {
            GameObject particle;

            if (pools[type].Count > 0)
            {
                particle = pools[type].Dequeue();
            }
            else if (activeParticles[type].Count < maxCounts[type])
            {
                particle = CreateParticle(type);
            }
            else
            {
                // Recycle oldest active particle
                particle = RecycleOldest(type);
            }

            particle.SetActive(true);
            activeParticles[type].Add(particle);

            // Reset rigidbody
            Rigidbody2D rb = particle.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            return particle;
        }

        public void Return(GameObject particle, ParticleType type)
        {
            if (particle == null) return;

            particle.SetActive(false);
            activeParticles[type].Remove(particle);
            pools[type].Enqueue(particle);
        }

        private GameObject RecycleOldest(ParticleType type)
        {
            if (activeParticles[type].Count == 0) return CreateParticle(type);

            GameObject oldest = activeParticles[type][0];
            activeParticles[type].RemoveAt(0);
            oldest.SetActive(false);
            return oldest;
        }

        public int GetActiveCount(ParticleType type)
        {
            return activeParticles[type].Count;
        }

        public int GetTotalActiveCount()
        {
            int total = 0;
            foreach (var list in activeParticles.Values)
            {
                total += list.Count;
            }
            return total;
        }
    }
}
