# Bug: Structure Manager Code Duplication

## Status: OPEN

## Category: Architecture / Refactor

## Summary

BeltManager (877 lines), LiftManager (709 lines), WallManager (354 lines), and PistonManager (905 lines) contain ~400-500 lines of near-identical code duplicated across all four files. Adding a new structure type requires modifying ~25-30 code locations across 10 files.

## Duplicated Patterns

| Pattern | Lines per copy | Copies | Total waste |
|---------|---------------|--------|-------------|
| `SnapToGrid()` | 5 | 4 | ~15 |
| `MarkChunksHasStructure()` | 15 | 4 | ~45 |
| `UpdateChunksStructureFlag()` | 35 | 4 | ~105 |
| `GetGhostBlockPositions()` | 12 | 3 | ~24 |
| `UpdateGhostStates()` scaffolding | 30 | 3 | ~60 |
| Placement validation (Air/soft terrain) | 20 | 3 | ~40 |
| Removal clear loop | 15 | 3 | ~30 |
| Dispose pattern | 5 | 4 | ~15 |

## Files That Must Be Touched to Add a New Structure Type

1. `StructureType.cs` — enum value
2. `MaterialDef.cs` — material IDs
3. New `*Manager.cs` — 350-700 lines (mostly boilerplate)
4. `SimulationManager.cs` — field, property, construction, ghost update, dispose, accessor (6 touch points)
5. `CellSimulatorJobbed.cs` — field, store reference, pass to job (3 touch points)
6. `SimulateChunksJob.cs` — ReadOnly field, CanMoveTo/MoveCell logic (2-3 touch points)
7. `StructurePlacementController.cs` — switch cases (4+ touch points)
8. `GhostStructureRenderer.cs` — manager reference, color, positions list, render loop (5 touch points)
9. `ItemRegistry.cs` — item definition
10. `Hotbar.cs` — slot, icon, ability mapping

## Affected Files

- `Assets/Scripts/Structures/BeltManager.cs`
- `Assets/Scripts/Structures/LiftManager.cs`
- `Assets/Scripts/Structures/WallManager.cs`
- `Assets/Scripts/Simulation/Machines/PistonManager.cs`
- `Assets/Scripts/Simulation/SimulationManager.cs`
- `Assets/Scripts/Simulation/CellSimulatorJobbed.cs`
- `Assets/Scripts/Simulation/SimulateChunksJob.cs`
- `Assets/Scripts/Game/Structures/StructurePlacementController.cs`
- `Assets/Scripts/Rendering/GhostStructureRenderer.cs`
- `Assets/Scripts/Game/UI/Hotbar.cs`
- `Assets/Scripts/Game/ItemRegistry.cs`

## Refactor Direction

- Extract `StructureUtils` static class for `SnapToGrid`, `MarkChunksHasStructure`, `UpdateChunksStructureFlag`
- Create `IStructureManager` interface and/or `StructureManagerBase` with ghost mode, placement validation, removal, disposal
- Registration-based approach in `SimulationManager` (iterate `List<IStructureManager>` instead of individual fields)
- `GhostStructureRenderer` iterates `IGhostProvider` list instead of referencing each manager by name
- Burst job constraint means per-structure fields in `SimulateChunksJob` must remain concrete — but everything outside the job is extractable

## Priority

High — this is the largest source of duplication and the primary barrier to extensibility.

## See Also

- `DevPlans/February/Backlog/StructureSystemRefactor.md` — existing refactor plan with more detail
