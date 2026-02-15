# Bug: Structure Placement Logic Duplicated Between Scenes

## Status: OPEN

## Category: Architecture / Refactor

## Summary

`SandboxController` (lines 140-460) and `StructurePlacementController` (lines 181-380) contain near-identical code for placing and removing structures. This directly violates the "one source of truth" principle.

## Duplicated Logic

### Belt drag-locking by Y coordinate
Both controllers implement the same pattern: on mouse down, lock Y to the clicked cell's Y; on drag, constrain placement to that row.

### Lift drag-locking by X coordinate
Same pattern but locking X instead of Y.

### `GetCellAtMouse()`
Duplicated in **4 classes**:
- `SandboxController.cs:344`
- `StructurePlacementController.cs:509`
- `CellGrabSystem.cs:193`
- `DiggingController` (inline equivalent)

### `MarkBeltChunksDirty` / `MarkWallChunksDirty` / `MarkStructureChunksDirty`
The **same method** exists under three different names in three different files:
- `SandboxController.MarkBeltChunksDirty()` (lines 386-397)
- `SandboxController.MarkWallChunksDirty()` (lines 440-451) — identical to the above
- `StructurePlacementController.MarkStructureChunksDirty()` (line 370)

All three iterate an 8x8 region calling `MarkChunkDirtyAt`. Same logic, different names.

### Wall/Piston placement
Both controllers have nearly identical placement and removal code for walls and pistons.

## Refactor Direction

- Extract a `StructurePlacementHelper` utility class with:
  - `GetCellAtMouse(Camera, CellWorld)` — one implementation shared by all consumers
  - `MarkStructureChunksDirty(CellWorld, int x, int y, int size)` — one implementation
  - `TryPlaceStructure(StructureType, ...)` / `TryRemoveStructure(StructureType, ...)` — shared placement/removal logic
  - Drag-lock state management for belt/lift constrained placement
- Both `SandboxController` and `StructurePlacementController` delegate to the shared helper

## Affected Files

- `Assets/Scripts/SandboxController.cs`
- `Assets/Scripts/Game/Structures/StructurePlacementController.cs`
- `Assets/Scripts/Game/CellGrabSystem.cs`
- `Assets/Scripts/Game/Digging/DiggingController.cs`

## Priority

High — a bug fix in one placement path will be missed in the other, leading to divergent behavior.
