# Soft Edges Effect

## What It Does

Blends colors at material boundaries, creating smoother transitions instead of hard pixelated edges.

**Without soft edges**: Sharp, jagged pixel boundaries between materials.
**With soft edges**: Smooth, anti-aliased transitions between materials.

---

## Visual Example

Picture where water meets sand:
- Without soft edges: Hard stair-step line of blue pixels next to yellow pixels
- With soft edges: Gradual blend from blue to yellow at the boundary, smoother appearance

This is essentially anti-aliasing for material boundaries.

---

## How It Works

### The Concept: Boundary Blending

At the edge between two materials, instead of showing a hard cutoff, we blend the colors of neighboring pixels together.

### Implementation Approach

1. **Sample center pixel** - the current material color
2. **Sample 4 neighbors** - left, right, up, down colors
3. **Detect boundary** - do any neighbors have a different material?
4. **If boundary**: Blend center with average of neighbors
5. **If interior**: Keep original color (no blending needed)

---

## Shader Logic (Conceptual)

```
For each pixel:
1. Get center color
2. Get 4 neighbor colors
3. Calculate average of neighbors
4. Check if any neighbor has different material ID
5. If boundary:
   - Blend: 60% center + 40% average
6. If not boundary:
   - Keep 100% center color
```

The blend ratio (60/40) can be tuned for stronger or subtler effect.

---

## Why Check for Boundaries?

We only blend at boundaries because:
- Interior pixels don't need blending (all neighbors are same material)
- Blending everywhere would blur the entire image unnecessarily
- This check saves work and preserves interior detail

---

## Files to Modify

| File | Change |
|------|--------|
| `WorldRender.shader` | Add neighbor sampling and blending logic |
| `SoftEdgesEffect.cs` | New file - effect controller |

---

## Default State

**OFF** - This is the most expensive effect (4 extra texture samples + blending math), and some players prefer the crisp pixel-art look.

---

## Performance Impact

**Moderate**
- 4 additional texture samples per pixel
- Boundary detection logic
- Blending math when at boundary
- Most expensive of the 4 effects

This is why it defaults to OFF - players on lower-end hardware can keep it disabled.

---

## Dependencies

- Requires the graphics effects architecture (see 00-GraphicsEffectsArchitecture.md)
- Shader-only implementation (no CPU work)

---

## Verification

1. Create a diagonal line of sand against air
2. Toggle soft edges OFF - line has stair-step jagged edges
3. Toggle soft edges ON - line appears smoother
4. Check that interior areas remain sharp (not blurry)
5. Performance: Monitor FPS with effect on vs off

---

## Trade-offs

**Pros:**
- Smoother, more polished visual appearance
- Reduces "chunky" look of pixel-based simulation

**Cons:**
- Performance cost (extra sampling)
- Some players prefer pixel-art aesthetic
- Can make material boundaries less distinct

---

## Future Enhancements

- Configurable blend strength (slider instead of toggle)
- Per-material blend settings (some materials blend more than others)
- Smart sampling (only sample when near known boundaries)
