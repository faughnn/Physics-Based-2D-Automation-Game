# Bug: NoiseVariation Toggle Appears Inverted

**Status:** OPEN (Verified 2026-01-23)
**Note:** The else branch on lines 115-119 of `WorldRender.shader` still applies noise (`colour.rgb *= 0.95 + noise * 0.1;`) when `_NoiseEnabled` is disabled.

---

## Summary
The NoiseVariation effect toggle doesn't disable noise - it just switches between two noise modes. Toggling "ON" can appear to reduce noise, making the toggle feel inverted.

## Observed Behavior
- Toggle OFF: Noise is still present (hardcoded default)
- Toggle ON: Per-material noise is applied (may be less visible than default)

## Expected Behavior
- Toggle OFF: No noise variation - flat, uniform material colors
- Toggle ON: Per-material noise variation based on `MaterialDef.colourVariation`

## Root Cause
The shader has an `else` branch that applies default noise even when the effect is disabled:

```hlsl
// WorldRender.shader lines 107-119
if (_NoiseEnabled > 0.5)
{
    // Per-material variation from _VariationTex
    float variation = SAMPLE_TEXTURE2D(_VariationTex, ...).r;
    float noiseAmount = variation * 0.25;
    colour.rgb *= 1.0 - noiseAmount + noise * noiseAmount * 2.0;
}
else
{
    // BUG: Still applies noise when "off"
    colour.rgb *= 0.95 + noise * 0.1;
}
```

## Fix
Remove the `else` branch so OFF truly disables noise:

```hlsl
if (_NoiseEnabled > 0.5)
{
    float variation = SAMPLE_TEXTURE2D(_VariationTex, ...).r;
    float noiseAmount = variation * 0.25;
    colour.rgb *= 1.0 - noiseAmount + noise * noiseAmount * 2.0;
}
// No else - when off, colors remain unchanged
```

## Files Affected
- `Assets/Shaders/WorldRender.shader`

## Notes
The noise was present in the shader before the effects system was added. The "effect" was layered on top without removing the original hardcoded noise.
