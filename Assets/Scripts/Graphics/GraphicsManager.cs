using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FallingSand.Graphics
{
    /// <summary>
    /// Singleton manager for all graphics effects.
    /// Handles effect registration, shader communication, and post-processing.
    /// </summary>
    public class GraphicsManager : MonoBehaviour
    {
        public static GraphicsManager Instance { get; private set; }

        [Header("Post-Processing")]
        [SerializeField] private Volume postProcessVolume;

        private List<IGraphicsEffect> effects = new List<IGraphicsEffect>();
        private Bloom bloomComponent;

        public IReadOnlyList<IGraphicsEffect> Effects => effects;
        public Volume PostProcessVolume => postProcessVolume;
        public Bloom BloomComponent => bloomComponent;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            FindPostProcessVolume();
            RegisterDefaultEffects();
            LoadAllSettings();
        }

        private void FindPostProcessVolume()
        {
            if (postProcessVolume == null)
            {
                postProcessVolume = FindFirstObjectByType<Volume>();
            }

            if (postProcessVolume != null && postProcessVolume.profile != null)
            {
                postProcessVolume.profile.TryGet(out bloomComponent);
                if (bloomComponent != null)
                {
                    Debug.Log("[GraphicsManager] Found bloom component in post-process volume");
                }
            }
            else
            {
                Debug.LogWarning("[GraphicsManager] No post-processing volume found");
            }
        }

        private void RegisterDefaultEffects()
        {
            RegisterEffect(new Effects.NoiseVariationEffect());
            RegisterEffect(new Effects.LightingEffect());
            RegisterEffect(new Effects.SoftEdgesEffect());
            RegisterEffect(new Effects.GlowEffect(this));

            Debug.Log($"[GraphicsManager] Registered {effects.Count} graphics effects");
        }

        public void RegisterEffect(IGraphicsEffect effect)
        {
            effects.Add(effect);
        }

        public void LoadAllSettings()
        {
            foreach (var effect in effects)
            {
                effect.LoadSettings();
            }
        }

        public void SaveAllSettings()
        {
            foreach (var effect in effects)
            {
                effect.SaveSettings();
            }
            PlayerPrefs.Save();
        }

        private void Update()
        {
            foreach (var effect in effects)
            {
                effect.UpdateShaderProperties();
            }
        }

        public T GetEffect<T>() where T : class, IGraphicsEffect
        {
            foreach (var effect in effects)
            {
                if (effect is T typed)
                    return typed;
            }
            return null;
        }

        public void SetBloomIntensity(float intensity)
        {
            if (bloomComponent != null)
            {
                bloomComponent.intensity.value = intensity;
            }
        }

        public void SetBloomThreshold(float threshold)
        {
            if (bloomComponent != null)
            {
                bloomComponent.threshold.value = threshold;
            }
        }
    }
}
