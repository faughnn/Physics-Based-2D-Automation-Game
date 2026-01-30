# Bug: Grabbed Material Not Visibly Shown at Cursor

## Summary
When cells are grabbed with the CellGrabSystem, there is no transparent visual preview of the held material at the mouse cursor. The only feedback is a small "Holding: N" text label. The grabbed material should appear as a transparent blob at the cursor until dropped.

## Symptoms
- Grab material with the grabber tool — material disappears from the world
- Only a text label ("Holding: 42") appears near the cursor
- No visual representation of what you're holding or where it will land
- The AoE circle is also hidden during hold, leaving no spatial feedback at all
- Dropping feels blind — you can't see the material until it's placed

## Root Cause
Three gaps in the current implementation:

1. **No rendering code for held material** — `CellGrabSystem.OnGUI()` (lines 100-116) only draws a text count label via `GUI.Label`. No texture, sprite, or shape is rendered at the cursor.

2. **AoE circle hidden during hold** — `ToolRangeIndicator.cs:176-177` explicitly sets `showAoe = false` when `grabSystem.IsHolding`, removing the only spatial indicator at the cursor.

3. **No spatial data preserved during grab** — `grabbedCells` is a `Dictionary<byte, int>` mapping materialId to count (line 20). The original cell positions relative to the grab center are discarded, so there's no shape data to render even if a renderer existed.

## Affected Code
- `Assets/Scripts/Game/CellGrabSystem.cs:20` — `grabbedCells` stores only material counts, no positions
- `Assets/Scripts/Game/CellGrabSystem.cs:100-116` — `OnGUI()` only renders text label
- `Assets/Scripts/Game/CellGrabSystem.cs:160-186` — `GrabCellsAtPosition()` discards cell positions after clearing them
- `Assets/Scripts/Game/UI/ToolRangeIndicator.cs:176-177` — AoE circle hidden during hold

## Potential Solutions
### 1. Texture-based cursor preview
During grab, record each cell's offset relative to the grab center along with its materialId. Generate a small `Texture2D` from these offsets using `MaterialDef.baseColour` for each cell, apply ~50% alpha for transparency, and render it at the cursor position via a `SpriteRenderer` or `GUI.DrawTexture` in `OnGUI()`. Update the texture position each frame to track the mouse. On drop, destroy the texture.

### 2. Simple circle preview with material color
Skip recording individual cell positions. Instead, draw a filled circle at the cursor (similar to the AoE circle but filled/semi-transparent) colored by the dominant grabbed material's `baseColour`. Simpler to implement but less visually informative than showing the actual cell shape.

## Priority
Medium

## Related Files
- `Assets/Scripts/Game/CellGrabSystem.cs`
- `Assets/Scripts/Game/UI/ToolRangeIndicator.cs`
- `Assets/Scripts/Simulation/MaterialDef.cs` — `baseColour` field available for rendering
- `Assets/Scripts/Game/GameController.cs` — wires up CellGrabSystem
