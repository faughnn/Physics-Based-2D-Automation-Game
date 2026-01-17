namespace FallingSand.Graphics.Effects
{
    /// <summary>
    /// Blends colors at material boundaries for softer visual transitions.
    /// Most expensive effect due to extra texture samples.
    /// </summary>
    public class SoftEdgesEffect : GraphicsEffectBase
    {
        public override string EffectName => "Soft Edges";
        public override string Description => "Blend colors at material boundaries";
        public override bool DefaultEnabled => false; // Off by default due to performance cost

        private const string ShaderProperty = "_SoftEdgesEnabled";

        public override void UpdateShaderProperties()
        {
            SetGlobalFloat(ShaderProperty, isEnabled ? 1f : 0f);
        }
    }
}
