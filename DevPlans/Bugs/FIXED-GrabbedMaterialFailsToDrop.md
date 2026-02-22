# Bug: Grabbed Material Fails to Drop

## Summary
When the player picks up material with the grabber and tries to drop it, none of it drops. Introduced by the Wave 1 GC pressure refactoring of `GetNextMaterialToPlace()`.

## Symptoms
- Player grabs material normally
- On drop attempt, nothing is placed in the world
- Material remains "held" but cannot be released

## Root Cause
The GC pressure fix in Wave 1 changed `GetNextMaterialToPlace()` from using `grabbedCells.Keys.ToList()` (safe snapshot) to iterating `foreach (var kvp in grabbedCells)` and modifying the dictionary value in-place. This is undefined behavior in C# — modifying a `Dictionary` during `foreach` enumeration can throw `InvalidOperationException` on some runtimes (particularly IL2CPP). The method returns immediately after modification so it may work on Mono, but fails silently on IL2CPP builds, causing the entire `DropCellsAtPosition` call to abort.

## Affected Code
- `Assets/Scripts/Game/CellGrabSystem.cs:360-372` — `GetNextMaterialToPlace()` modifies dictionary during foreach

## Potential Solutions
### 1. Break before modifying (Recommended)
Find the first valid key with `foreach`/`break`, then modify the dictionary after the loop exits. No allocation, safe on all runtimes.

## Fix
Changed `GetNextMaterialToPlace()` to find the key with `foreach`/`break` first, then modify the dictionary after exiting the enumerator. Zero-allocation, safe on all runtimes.

## Priority
High — core gameplay feature (grab/drop) is broken.

## Related Files
- `Assets/Scripts/Game/CellGrabSystem.cs`
- `DevPlans/Bugs/FIXED-CellGrabSystemGCPressure.md`
