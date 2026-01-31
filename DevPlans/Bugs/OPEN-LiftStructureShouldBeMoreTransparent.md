# Bug: Lift Structure Should Be More Transparent

## Summary
Active lift structures render as fully opaque blocks, making it impossible to see materials flowing through them. Since lifts have the `Passable` flag (materials pass through them), they should be visually transparent so the player can see what's happening inside.

## Symptoms
- Lifts appear as solid opaque green blocks, identical in opacity to belts and walls
- Materials flowing upward through a lift are completely hidden behind the lift's cells
- No visual distinction between lifts (passable) and walls/belts (solid) in terms of opacity
- Player cannot see if a lift is working or jammed without watching the output

## Root Cause
The cell rendering pipeline has no support for per-material transparency:

1. **Shader is fully opaque** — `WorldRender.shader` uses `"RenderType" = "Opaque"` and `"Queue" = "Geometry"` with no `Blend` state. Even if a material's `baseColour` alpha were lowered, it would have no visual effect.

2. **Palette alpha ignored** — `CellRenderer.BuildPalette()` copies `baseColour` (including alpha) into the palette texture, but the shader never uses the alpha channel for blending.

3. **Density override hides passable nature** — `CellRenderer.UploadChunk()` line 270 sets `density = 255` for all non-Air materials, so lifts get the same edge lighting as solid structures despite having `density = 0` in their `MaterialDef`.

## Affected Code
- `Assets/Resources/Shaders/WorldRender.shader:16-18` — Opaque render type, Geometry queue, no Blend state
- `Assets/Scripts/Rendering/CellRenderer.cs:170` — Palette copies alpha but shader ignores it
- `Assets/Scripts/Rendering/CellRenderer.cs:270` — Density override treats lifts as solid
- `Assets/Scripts/Simulation/MaterialDef.cs:271-288` — LiftUp/LiftUpLight have alpha=255

## Potential Solutions
### 1. Shader alpha blending from palette
Add alpha blending support to `WorldRender.shader` by reading the palette alpha channel and enabling `Blend SrcAlpha OneMinusSrcAlpha`. Then lower the lift material `baseColour` alpha (e.g., to 128 for 50% transparency). This is the minimal change — the palette texture already stores alpha, it's just not used. Risk: all transparent cells would need correct draw ordering, or artifacts may appear. Since lifts are static and behind flowing materials, this may be acceptable with a simple `"Queue" = "Transparent"` change, but it would affect ALL materials that have alpha < 255.

### 2. Two-pass rendering (opaque + transparent)
Render opaque materials in the current Geometry pass, then render transparent materials (lifts) in a second Transparent pass with blending enabled. This avoids sorting issues for opaque materials while correctly blending lifts. More complex but architecturally cleaner. Could use a material flag (e.g., `Passable`) or a shader keyword to distinguish passes.

### 3. Overlay approach (like ghost renderer)
Skip shader changes entirely. Render lift cells as Air in the main cell texture so they're invisible, then use a separate `SpriteRenderer` overlay (similar to `GhostStructureRenderer`) to draw lifts with transparency. This keeps the main shader simple but adds a parallel rendering system for lifts.

## Priority
Medium

## Related Files
- `Assets/Resources/Shaders/WorldRender.shader` — Cell rendering shader
- `Assets/Scripts/Rendering/CellRenderer.cs` — Palette and density texture upload
- `Assets/Scripts/Simulation/MaterialDef.cs` — Lift material definitions
- `Assets/Scripts/Structures/LiftManager.cs:475-499` — Lift arrow pattern
- `Assets/Scripts/Rendering/GhostStructureRenderer.cs` — Existing transparent overlay system (ghost structures only)
