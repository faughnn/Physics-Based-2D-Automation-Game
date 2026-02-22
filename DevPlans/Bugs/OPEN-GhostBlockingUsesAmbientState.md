# Bug: Ghost Blocking in CanMoveTo Uses Ambient State

## Summary
The source-aware ghost blocking in `SimulateChunksJob.CanMoveTo()` uses a `currentCellIdx` field (ambient state) instead of an explicit parameter. This works correctly but creates an implicit contract that is fragile and hard to reason about.

## Symptoms
- No runtime bugs — the fix is functionally correct
- `CanMoveTo(int x, int y, byte myDensity)` signature hides its dependency on `currentCellIdx`
- Adding new call sites to `CanMoveTo` could silently produce incorrect ghost blocking if `currentCellIdx` is stale

## Root Cause
The ghost structure system has two competing requirements:
1. Ghost structures must block **external** material from entering (prevents new sand filling ghost areas)
2. Material **already inside** ghost areas must drain out (so ghosts can eventually activate)

This was solved by checking the source cell's position in `CanMoveTo`. Rather than adding a `fromIdx` parameter to all 20 call sites, the source index was stored as a mutable field `currentCellIdx` on the job struct, set once in `SimulateCell()` and read implicitly in `CanMoveTo()`.

The approach works because:
- `IJobParallelFor` copies the struct per worker thread (thread-safe)
- `currentCellIdx` always represents the original cell being simulated, which is semantically correct even for trace functions (`TraceDiagonalPath`, `FindSpreadDistance`) that probe multiple destinations

## Affected Code
- `Assets/Scripts/Simulation/Jobs/SimulateChunksJob.cs:75` — `currentCellIdx` field
- `Assets/Scripts/Simulation/Jobs/SimulateChunksJob.cs:122` — set in `SimulateCell()`
- `Assets/Scripts/Simulation/Jobs/SimulateChunksJob.cs:765-774` — read in `CanMoveTo()` ghost checks

## Potential Solutions
### 1. Explicit `fromIdx` parameter (Recommended)
Change signature to `CanMoveTo(int fromIdx, int x, int y, byte myDensity)`. Makes the contract explicit at all 20 call sites. Zero runtime cost (Burst inlines identically). Purely mechanical diff.

### 2. Pre-computed ghost block map
Allocate a `NativeArray<byte> ghostBlockMap` parallel to cells. Pre-compute on main thread: `ghostBlockMap[idx] = 1` if inside any ghost structure. Unifies belt and wall ghost checks into a single O(1) lookup. Still needs source awareness via either approach.

### 3. Cell flag
Add an `InGhostZone` bit to `Cell.flags` (5 of 8 bits currently used). Set when ghost structures are placed, clear on activation. Source check becomes a bit test on a value already loaded. Maintenance cost: must keep flag in sync with ghost placement/removal/activation.

## Priority
Low — functionally correct, no runtime impact. Architectural cleanliness issue only.

## Related Files
- `Assets/Scripts/Structures/BeltManager.cs` — ghost belt activation
- `Assets/Scripts/Structures/WallManager.cs` — ghost wall activation
- `Assets/Scripts/Structures/LiftManager.cs` — lifts use passability instead of ghost blocking
- `DevPlans/Bugs/FIXED-GhostStructureTrappsDirt.md` — the original bug this fix addresses
- `DevPlans/Bugs/FIXED-GhostTilesDontBlockPowderAndLiquid.md` — the opposite problem (no blocking at all)
- `DevPlans/February/Backlog/StructureSystemRefactor.md` — broader structure system coupling issues
