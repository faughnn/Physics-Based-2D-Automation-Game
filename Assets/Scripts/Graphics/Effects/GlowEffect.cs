using UnityEngine;

namespace FallingSand.Graphics.Effects
{
    /// <summary>
    /// Enables glow for emissive materials and controls bloom post-processing.
    /// Materials with emission > 0 will output HDR values that trigger bloom.
    /// </summary>
    public class GlowEffect : GraphicsEffectBase
    {
        public override string EffectName => "Glow";
        public override string Description => "Emissive materials with bloom post-processing";
        public override bool DefaultEnabled => true;

        private const string ShaderProperty = "_GlowIntensity";
        private const string IntensityPrefsKey = "Graphics_GlowIntensity";

        private GraphicsManager manager;
        private float intensity = 0.5f;

        public float Intensity
        {
            get => intensity;
            set
            {
                intensity = Mathf.Clamp01(value);
                UpdateShaderProperties();
                SaveSettings();
            }
        }

        public GlowEffect(GraphicsManager manager)
        {
            this.manager = manager;
        }

        public override void OnEnable()
        {
            base.OnEnable();
            UpdateBloom();
        }

        public override void OnDisable()
        {
            base.OnDisable();
            UpdateBloom();
        }

        public override void UpdateShaderProperties()
        {
            SetGlobalFloat(ShaderProperty, isEnabled ? intensity : 0f);
        }

        private void UpdateBloom()
        {
            if (manager != null)
            {
                manager.SetBloomIntensity(isEnabled ? intensity : 0f);
                manager.SetBloomThreshold(0.8f);
            }
        }

        public override void LoadSettings()
        {
            base.LoadSettings();
            intensity = PlayerPrefs.GetFloat(IntensityPrefsKey, 0.5f);
            UpdateBloom();
        }

        public override void SaveSettings()
        {
            base.SaveSettings();
            PlayerPrefs.SetFloat(IntensityPrefsKey, intensity);
        }
    }
}
