# Dead Code: Unused WorldUtils Class

## Summary
The entire `WorldUtils` static class is defined but none of its 9 methods are ever called anywhere in the codebase.

## Location
- **File:** `Assets/Scripts/Simulation/WorldUtils.cs`
- **Lines:** 5-42

## Unused Methods
| Method | Line | Purpose |
|--------|------|---------|
| `CellIndex(x, y, width)` | 9 | Calculate cell array index |
| `CellToChunkX(cellX)` | 13 | Convert cell X to chunk X |
| `CellToChunkY(cellY)` | 16 | Convert cell Y to chunk Y |
| `ChunkToCellX(chunkX)` | 20 | Convert chunk X to cell X origin |
| `ChunkToCellY(chunkY)` | 23 | Convert chunk Y to cell Y origin |
| `CellToLocalX(cellX)` | 27 | Get local X within chunk |
| `CellToLocalY(cellY)` | 30 | Get local Y within chunk |
| `ChunkIndex(chunkX, chunkY, chunksX)` | 34 | Calculate chunk array index |
| `IsInBounds(x, y, width, height)` | 38 | Check if coordinates are valid |

## Why It's Dead Code
The codebase performs these operations inline using bit operations:
- `>> 6` for division by 64 (cell to chunk)
- `& 63` for modulo 64 (cell to local)
- `y * width + x` for index calculation

## Recommended Action
Delete the entire `WorldUtils.cs` file as it provides no value.
