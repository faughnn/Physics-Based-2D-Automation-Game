namespace FallingSand.Graphics.Effects
{
    /// <summary>
    /// Applies per-material color noise variation based on MaterialDef.colourVariation.
    /// </summary>
    public class NoiseVariationEffect : GraphicsEffectBase
    {
        public override string EffectName => "Noise Variation";
        public override string Description => "Per-material color variation for visual interest";
        public override bool DefaultEnabled => true;

        private const string ShaderProperty = "_NoiseEnabled";

        public override void UpdateShaderProperties()
        {
            SetGlobalFloat(ShaderProperty, isEnabled ? 1f : 0f);
        }
    }
}
