namespace FallingSand.Graphics
{
    /// <summary>
    /// Interface for graphics effects that can be toggled and configured.
    /// Follow this pattern to add new visual effects to the game.
    /// </summary>
    public interface IGraphicsEffect
    {
        /// <summary>
        /// Display name for this effect in the settings menu.
        /// </summary>
        string EffectName { get; }

        /// <summary>
        /// Short description of what this effect does.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Whether this effect is currently enabled.
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Whether this effect should be enabled by default.
        /// </summary>
        bool DefaultEnabled { get; }

        /// <summary>
        /// Called when the effect is enabled.
        /// </summary>
        void OnEnable();

        /// <summary>
        /// Called when the effect is disabled.
        /// </summary>
        void OnDisable();

        /// <summary>
        /// Called each frame to update shader properties.
        /// </summary>
        void UpdateShaderProperties();

        /// <summary>
        /// Load saved settings from PlayerPrefs.
        /// </summary>
        void LoadSettings();

        /// <summary>
        /// Save current settings to PlayerPrefs.
        /// </summary>
        void SaveSettings();
    }
}
