# Bug: Player Is Same Width as Lift (Should Be 2/3)

## Summary
The player's width is 16 world units (8 cells), which is exactly the same width as a lift (also 8 cells / 16 world units). The player should be 2/3 the width of a lift (~10.67 world units / ~5.33 cells) so it fits comfortably inside lift shafts.

## Symptoms
- Player is exactly as wide as a lift, leaving no clearance
- Player likely clips or catches on the edges of lift shafts
- Feels oversized relative to structures

## Root Cause
The player's collider and sprite are created inline in `GameController.CreatePlayer()` with a hardcoded width of 16 world units — the same as an 8-cell structure block. No scaling factor accounts for the intended player-to-structure size ratio.

```csharp
// GameController.cs:218
collider.size = new Vector2(16, 32);

// GameController.cs:222
sr.sprite = CreateRectSprite(16, 32);
```

Lift/structure block width is `8 cells * CellToWorldScale(2) = 16` world units, so the ratio is 1:1 instead of the intended 2:3.

## Affected Code
- `Assets/Scripts/Game/GameController.cs:218` — `collider.size = new Vector2(16, 32)` — width should be ~10.67
- `Assets/Scripts/Game/GameController.cs:222` — `CreateRectSprite(16, 32)` — width should match collider

## Potential Solutions
### 1. Change player width to 2/3 of structure block width
Set both the collider and sprite width to `16 * (2f/3f)` ≈ 10.67 world units. Height can stay at 32 or be adjusted proportionally. This is a two-line change in `CreatePlayer()`.

Consider extracting a named constant (e.g., `PlayerWidthCells = 5.33f`) or computing it from the structure block size to make the relationship explicit:
```csharp
float playerWidth = 8 * CoordinateUtils.CellToWorldScale * (2f / 3f);
```

## Priority
Medium

## Related Files
- `Assets/Scripts/Game/GameController.cs` — `CreatePlayer()` method
- `Assets/Scripts/Simulation/CoordinateUtils.cs` — `CellToWorldScale = 2`
- `Assets/Scripts/Structures/LiftManager.cs` — lift tile is 8x8 cells
