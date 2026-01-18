# Glow Effect

## What It Does

Makes certain materials emit light that bleeds into surrounding areas, creating a glowing aura effect.

**Without glow**: Lava/molten metal is just a bright orange color.
**With glow**: Lava emits a warm orange aura that lights up the area around it.

---

## Visual Example

Picture molten iron in a dark cave:
- Without glow: Bright orange pixels surrounded by dark pixels - abrupt transition
- With glow: Bright orange core with a soft orange halo bleeding outward, illuminating nearby stone

This effect consists of two parts working together.

---

## Part 1: Material Emission

### What It Does
Some materials are "emissive" - they output more light than standard rendering. The shader outputs HDR (High Dynamic Range) colors for these materials, meaning values brighter than normal white.

### Which Materials Glow?
- **Lava** (future material) - bright orange/red emission
- **Molten Iron** (future material) - bright yellow/white emission
- **Fire** (future material) - orange/yellow emission
- Any hot/energy materials you add later

### Implementation
1. Add `emission` field to `MaterialDef` (0-255 value)
2. Build emission texture (256x1) from these values
3. Shader reads emission and boosts output color accordingly

---

## Part 2: Bloom Post-Processing

### What It Does
Bloom is a camera effect that takes bright pixels and "bleeds" them outward, creating the halo/aura effect. It runs after the main rendering.

### How It Works
1. Camera renders scene to texture
2. Bloom pass extracts bright pixels (above threshold)
3. Bright pixels are blurred and spread outward
4. Blurred bright areas are added back to original image
5. Result: bright things have soft glowing halos

### Unity Implementation
Unity's URP has built-in bloom. The project already has:
- `DefaultVolumeProfile.asset` with Bloom effect
- Bloom is currently set to intensity 0 (disabled)

The `GlowEffect` simply adjusts this bloom's intensity at runtime.

---

## How the Two Parts Work Together

1. Emissive materials output HDR colors (brighter than 1.0)
2. Bloom extracts these HDR pixels (above threshold ~0.8)
3. Bloom blurs and spreads the bright areas
4. Result: Emissive materials create visible glowing auras

Without emission, bloom has nothing to work with.
Without bloom, emission just makes things brighter (no spread).

---

## Files to Modify

| File | Change |
|------|--------|
| `MaterialDef.cs` | Add `emission` byte field (0-255) |
| `CellRenderer.cs` | Build emission texture from MaterialDef |
| `WorldRender.shader` | Sample emission, output HDR values |
| `GlowEffect.cs` | New file - controls bloom intensity |

---

## Default State

**ON** with medium intensity - provides atmospheric lighting without being overwhelming.

---

## Performance Impact

**Low-Moderate**
- Emission: One texture sample + multiply (cheap)
- Bloom: Unity's optimized bloom is reasonably fast
- Overall: Noticeable but acceptable on most hardware

---

## Dependencies

- Requires the graphics effects architecture (see 00-GraphicsEffectsArchitecture.md)
- Requires URP post-processing volume (already exists)
- Needs emissive materials to have any visible effect

---

## Verification

1. Add an emissive material (set emission > 0 in MaterialDef)
2. Place some of that material in the world
3. Toggle glow OFF - material is just a bright color
4. Toggle glow ON - material has a visible aura/halo
5. Adjust intensity slider - aura should grow/shrink
6. Verify bloom threshold works (only bright things glow)

---

## Configuration Options

### Intensity Slider
- Range: 0.0 to 1.0
- Controls bloom strength
- Higher = larger, more visible glow halos
- Lower = subtle, understated glow

### Threshold (advanced, could be exposed later)
- How bright a pixel must be to trigger bloom
- Lower threshold = more things glow
- Higher threshold = only very bright things glow

---

## Future Enhancements

- Per-material emission colors (not just intensity)
- Animated emission (pulsing glow for magical items)
- Temperature-based emission (hot things glow proportionally)
- Light propagation (glow actually illuminates nearby cells)
