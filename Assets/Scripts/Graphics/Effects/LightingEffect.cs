namespace FallingSand.Graphics.Effects
{
    /// <summary>
    /// Applies pseudo-lighting based on material density gradients.
    /// Computes normals from neighboring cell densities and applies directional lighting.
    /// </summary>
    public class LightingEffect : GraphicsEffectBase
    {
        public override string EffectName => "Lighting";
        public override string Description => "Pseudo-lighting from density gradients";
        public override bool DefaultEnabled => true;

        private const string ShaderProperty = "_LightingEnabled";

        public override void UpdateShaderProperties()
        {
            SetGlobalFloat(ShaderProperty, isEnabled ? 1f : 0f);
        }
    }
}
