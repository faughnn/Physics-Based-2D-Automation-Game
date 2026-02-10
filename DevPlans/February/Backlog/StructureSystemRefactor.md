# Structure System Refactor

## Goal

Extract shared patterns from the three existing structure managers into a common foundation so that adding new structure types (Furnace, Press, etc.) doesn't require 400-500 lines of boilerplate and touching 7 files.

## Current State

BeltManager, LiftManager, and WallManager are standalone classes with no shared base, interface, or utilities. The following code is duplicated nearly identically across all three:

- `SnapToGrid()` — same 5-line method in all three
- `MarkChunksHasStructure()` — identical 15-line method in all three
- `UpdateChunksStructureFlag()` / `UpdateSingleChunkStructureFlag()` — same outer loop, minor inner variation
- Ghost mode: `ghostBlockOrigins` field, `UpdateGhostStates()`, `GetGhostBlockPositions()` — same pattern in all three
- Placement validation: check 8x8 area for Air/soft terrain, reject on hard material
- Removal: clear 8x8 area, handle ghost vs non-ghost, update chunk flags
- Disposal pattern

Beyond the managers themselves, adding a new structure type requires modifying:

1. `StructureType.cs` — enum value
2. `MaterialDef.cs` — new material IDs
3. `SimulationManager.cs` — field, construction, ghost update call, simulate parameter, dispose, public accessor
4. `CellSimulatorJobbed.cs` — field, parameter, pass to job
5. `SimulateChunksJob.cs` — ReadOnly field, interaction logic
6. `StructurePlacementController.cs` — switch cases in 4+ places
7. `GhostStructureRenderer.cs` — manager reference, color, position list, render loop

## Work Required

### Shared Utilities / Base
- Extract `SnapToGrid`, `MarkChunksHasStructure`, `UpdateChunksStructureFlag` into a shared `StructureUtils` static class or a base class
- Extract the ghost mode pattern (tracking, update, position query) into a reusable component or base implementation
- Extract the common placement validation (8x8 Air/soft terrain check) into a shared method
- Extract the common removal pattern

### Registration / Wiring
- Consider a registration pattern in `SimulationManager` so new structures don't require adding fields, constructor params, update calls, and dispose calls manually
- Reduce the switch-case sprawl in `StructurePlacementController` — possibly by defining placement behaviour (drag axis, snap rules) as data on the structure type rather than per-type handler methods

### Ghost Rendering
- `GhostStructureRenderer` should iterate registered structures rather than referencing each manager by name

### Simulation Job Coupling
- This is the hardest part. Each structure type has its own tile data layout and its own interaction logic inside the Burst-compiled job. A fully generic approach may not be practical here due to Burst constraints (no interfaces, no virtual dispatch). The pragmatic approach may be to keep per-structure fields in the job but minimize what needs to be added for each new type.

## Open Questions

- Should there be a `IStructureManager` interface, a `StructureManagerBase` abstract class, or just a static utility class? The right answer depends on how much behaviour varies between structure types.
- How much can Burst job coupling be reduced? Dense `NativeArray<T>` tile storage works well for lifts and walls but belts use a sparse `NativeHashMap`. Can these be unified or should they remain type-specific?
- Should placement behaviour (drag direction, grid snap rules, preview color) be defined as data rather than code?
