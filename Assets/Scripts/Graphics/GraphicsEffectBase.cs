using UnityEngine;

namespace FallingSand.Graphics
{
    /// <summary>
    /// Base class for graphics effects with common functionality.
    /// </summary>
    public abstract class GraphicsEffectBase : IGraphicsEffect
    {
        public abstract string EffectName { get; }
        public abstract string Description { get; }
        public abstract bool DefaultEnabled { get; }

        protected bool isEnabled;
        public bool IsEnabled
        {
            get => isEnabled;
            set
            {
                if (isEnabled != value)
                {
                    isEnabled = value;
                    if (isEnabled)
                        OnEnable();
                    else
                        OnDisable();
                    SaveSettings();
                }
            }
        }

        protected string PrefsKey => $"Graphics_{EffectName.Replace(" ", "")}";

        public virtual void OnEnable()
        {
            UpdateShaderProperties();
        }

        public virtual void OnDisable()
        {
            UpdateShaderProperties();
        }

        public abstract void UpdateShaderProperties();

        public virtual void LoadSettings()
        {
            isEnabled = PlayerPrefs.GetInt(PrefsKey, DefaultEnabled ? 1 : 0) == 1;
            if (isEnabled)
                OnEnable();
            else
                OnDisable();
        }

        public virtual void SaveSettings()
        {
            PlayerPrefs.SetInt(PrefsKey, isEnabled ? 1 : 0);
        }

        /// <summary>
        /// Helper to set a global shader float property.
        /// </summary>
        protected void SetGlobalFloat(string propertyName, float value)
        {
            Shader.SetGlobalFloat(propertyName, value);
        }

        /// <summary>
        /// Helper to set a global shader int property.
        /// </summary>
        protected void SetGlobalInt(string propertyName, int value)
        {
            Shader.SetGlobalInt(propertyName, value);
        }
    }
}
