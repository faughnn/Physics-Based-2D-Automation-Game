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

            // Create a global volume if none exists
            if (postProcessVolume == null)
            {
                GameObject volumeObj = new GameObject("PostProcessVolume");
                postProcessVolume = volumeObj.AddComponent<Volume>();
                postProcessVolume.isGlobal = true;

                // Create a new profile with bloom
                var profile = ScriptableObject.CreateInstance<VolumeProfile>();
                bloomComponent = profile.Add<Bloom>(overrides: true);
                bloomComponent.threshold.value = 0.8f;
                bloomComponent.intensity.value = 0f;  // Start disabled, GlowEffect controls this
                bloomComponent.scatter.value = 0.7f;

                postProcessVolume.profile = profile;
                return;
            }

            if (postProcessVolume.profile != null)
            {
                postProcessVolume.profile.TryGet(out bloomComponent);
            }
            else
            {
                Debug.LogWarning("[GraphicsManager] Post-processing volume has no profile");
            }
        }

        private void RegisterDefaultEffects()
        {
            RegisterEffect(new Effects.NoiseVariationEffect());
            RegisterEffect(new Effects.LightingEffect());
            RegisterEffect(new Effects.SoftEdgesEffect());
            RegisterEffect(new Effects.GlowEffect(this));
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
