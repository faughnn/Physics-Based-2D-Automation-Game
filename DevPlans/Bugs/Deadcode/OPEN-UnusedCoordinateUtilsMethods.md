# Dead Code: Unused CoordinateUtils Methods

## Summary
Several methods in the CoordinateUtils class are defined but never called.

## Location
- **File:** `Assets/Scripts/Simulation/CoordinateUtils.cs`

## Unused Methods
| Method | Line | Purpose |
|--------|------|---------|
| `WorldToCellRounded(Vector2 worldPos, int worldWidth, int worldHeight)` | 81 | Convert world position to cell with rounding |
| `ScaleWorldToCell(float worldDistance)` | 109 | Scale a world distance to cell units |
| `ScaleWorldToCell(Vector2 worldVector)` | 125 | Scale a world vector to cell units |

## Used Methods
- `CellToWorld` - Converting cell coordinates to world position
- `WorldToCell` - Converting world position to cell coordinates
- `CellScale` constant - Used throughout

## Recommended Action
Remove the unused methods or evaluate if they should be used somewhere for consistency.
