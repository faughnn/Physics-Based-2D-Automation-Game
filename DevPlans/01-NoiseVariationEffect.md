# Noise Variation Effect

## What It Does

Adds subtle random color variation to each pixel of a material, making large areas of the same material look more natural and less flat.

**Without noise**: A wall of stone looks like a solid block of one color.
**With noise**: Each stone pixel has slightly different brightness, giving texture and depth.

---

## Visual Example

Imagine a sandy beach:
- Without variation: Perfectly uniform yellow - looks artificial
- With variation: Each grain slightly lighter or darker - looks like real sand

The variation amount is already defined per-material in `MaterialDef.colourVariation`:
- Stone: High variation (rough, varied texture)
- Water: Low variation (smooth, uniform liquid)
- Sand: Medium variation (granular but consistent)

---

## How It Works

### Current State
- `MaterialDef` already has `colourVariation` field (0-255)
- This value is currently ignored by the shader

### Implementation Approach

1. **Build a variation texture** (256x1 pixels)
   - Each pixel represents one material
   - Red channel = that material's variation amount

2. **Shader reads this texture**
   - Looks up variation amount for current material
   - Generates noise based on pixel position
   - Scales noise by variation amount
   - Applies to final color

---

## Shader Logic (Conceptual)

```
For each pixel:
1. Get the material ID (already doing this)
2. Look up variation amount from texture (0.0 to 1.0)
3. Generate noise value based on pixel position (-1 to +1)
4. Scale noise by variation amount and a global intensity
5. Multiply base color by (1 + scaled_noise)
```

The noise is deterministic based on position, so it doesn't flicker.

---

## Files to Modify

| File | Change |
|------|--------|
| `CellRenderer.cs` | Build and upload variation texture |
| `WorldRender.shader` | Add sampler, add noise logic |
| `NoiseVariationEffect.cs` | New file - effect controller |

---

## Default State

**ON** - This effect is cheap and makes everything look better.

---

## Performance Impact

**Very Low**
- One extra texture sample per pixel
- Simple math operations
- No additional geometry or passes

---

## Dependencies

- Requires the graphics effects architecture (see 00-GraphicsEffectsArchitecture.md)
- Uses existing `MaterialDef.colourVariation` values

---

## Verification

1. Toggle effect off - materials should look flat/uniform
2. Toggle effect on - materials should have subtle variation
3. Compare stone (high variation) vs water (low variation)
4. Check that the effect is consistent (no flickering)
