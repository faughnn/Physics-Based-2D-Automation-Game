# Bug: No Tool Area-of-Effect Preview at Cursor

## Summary
When the shovel or grabber is equipped, there is no visual indicator at the cursor showing how large an area will be dug or grabbed. Players have no way to predict the affected region before clicking.

## Symptoms
- Equipping the shovel and hovering over terrain shows no preview of the dig area
- Equipping the grabber shows no preview of the grab area
- Players must click and observe the result to understand the tool's area of effect
- This makes precise digging and grabbing difficult, especially near edges or structures

## Root Cause
Neither `DiggingController` nor `CellGrabSystem` render any cursor-following area indicator. Both tools use an 8-cell circular radius (`digRadius = 8f`, `grabRadius = 8f`) with a `dx^2 + dy^2 <= r^2` iteration pattern, but this area is never visualized.

The existing `ToolRangeIndicator` only shows the max reach arc from the player — it does not display the area of effect at the cursor position.

## Affected Code
- `Assets/Scripts/Game/Digging/DiggingController.cs:13` — `digRadius = 8f` (not visualized)
- `Assets/Scripts/Game/CellGrabSystem.cs:15` — `grabRadius = 8f` (not visualized)
- `Assets/Scripts/Game/UI/ToolRangeIndicator.cs` — only shows max reach arc, not AoE circle

## Potential Solutions
### 1. Add AoE Circle LineRenderer to ToolRangeIndicator
Extend the existing `ToolRangeIndicator` with a second `LineRenderer` that draws a full circle at the mouse cursor position. The radius would be `CoordinateUtils.ScaleCellToWorld(toolRadius)` (8 cells × 2 = 16 world units). Uses the same `Sprites/Default` material and rendering pattern already established.

- Pros: Reuses existing component which already has references to both tools and runs in LateUpdate
- Cons: Adds a second responsibility to ToolRangeIndicator

### 2. Create Separate ToolAoEIndicator Component
New MonoBehaviour following the same LineRenderer pattern, attached to the player alongside ToolRangeIndicator. Draws a semi-transparent circle at the cursor position matching the active tool's radius.

- Pros: Clean separation of concerns (range indicator vs AoE indicator)
- Cons: Another component to wire up in GameController

### Notes
- Circle should be semi-transparent (e.g., alpha 0.4) and match the tool's color (orange for shovel, green for grabber)
- Circle should only appear when cursor is within the tool's max reach distance
- Convert cell radius to world radius via `CoordinateUtils.ScaleCellToWorld()` (multiply by 2)

## Priority
Medium

## Related Files
- `Assets/Scripts/Game/UI/ToolRangeIndicator.cs`
- `Assets/Scripts/Game/Digging/DiggingController.cs`
- `Assets/Scripts/Game/CellGrabSystem.cs`
- `Assets/Scripts/Simulation/CoordinateUtils.cs`
