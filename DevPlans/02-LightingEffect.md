# Lighting Effect

## What It Does

Adds a sense of depth and dimension by simulating light hitting surfaces. Makes materials look 3D instead of flat.

**Without lighting**: Everything is uniformly lit - looks like a 2D sprite.
**With lighting**: Surfaces facing the "light" are brighter, surfaces facing away are darker - looks like a 3D scene.

---

## Visual Example

Picture a pile of sand:
- Without lighting: Uniform color, hard to see the shape
- With lighting: Left sides of bumps are bright, right sides are shadowed - you can "see" the 3D form

The light comes from the upper-left (like most games/UI conventions), creating:
- Bright highlights on top-left facing surfaces
- Subtle shadows on bottom-right facing surfaces

---

## How It Works

### The Concept: Normals from Neighbors

In 3D graphics, surfaces are lit based on their "normal" (the direction they face). We can fake this in 2D by looking at density differences between neighboring pixels.

**Key insight**: Where materials meet air, there's an "edge" that implies a surface direction.

### Implementation Approach

1. **Sample 4 neighbors** (left, right, up, down)
2. **Compute gradient** (how much density changes in X and Y)
3. **Derive normal** from gradient (points away from dense areas)
4. **Calculate lighting** by comparing normal to light direction
5. **Apply to color** - brighter where facing light, darker where facing away

---

## Shader Logic (Conceptual)

```
For each pixel:
1. Get density of 4 neighbors (dense material = 1, air = 0)
2. Horizontal gradient = left_density - right_density
3. Vertical gradient = down_density - up_density
4. Normal = normalize(horizontal, vertical, 0.5)
5. Light direction = (0.3, -0.5, 1.0) - upper left
6. Brightness = dot(normal, light_direction)
7. Final color = base_color * brightness_factor
```

---

## What is "Density"?

For this effect, density means "is there solid material here?"
- Air = 0 (empty space)
- Any other material = 1 (solid)

This creates lighting at material boundaries, which is where we perceive depth.

---

## Files to Modify

| File | Change |
|------|--------|
| `WorldRender.shader` | Add neighbor sampling and lighting math |
| `LightingEffect.cs` | New file - effect controller |

---

## Default State

**ON** - This effect adds significant visual quality for minimal cost.

---

## Performance Impact

**Low**
- 4 additional texture samples per pixel (for neighbors)
- Vector math for normal calculation
- No additional passes or geometry

---

## Dependencies

- Requires the graphics effects architecture (see 00-GraphicsEffectsArchitecture.md)
- Shader-only implementation (no CPU work)

---

## Verification

1. Create a pile of sand or stone
2. Toggle lighting OFF - pile looks flat
3. Toggle lighting ON - pile looks 3D with depth
4. Check that light appears to come from upper-left
5. Edges facing light should be brighter
6. Edges facing away should be darker

---

## Tuning Options

The light direction is hardcoded but could be made configurable:
- `_LightDirection` uniform for dynamic light angle
- Could even animate for day/night cycles (future feature)
