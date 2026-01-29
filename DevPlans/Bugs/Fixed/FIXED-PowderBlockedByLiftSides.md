# Powder Blocked By Lift Sides

## Status: OPEN

## Description
Powder (sand, dirt, etc.) cannot fall into a lift from the side. The side walls of the lift structure block particles from entering laterally, even though the lift should be open for material to flow in from adjacent cells.

## Expected Behavior
Powder should be able to fall or slide into a lift from the side, entering the lift column when adjacent to it.

## Actual Behavior
Powder stacks against the side of the lift as if it were a solid wall, unable to enter the lift area.

## Root Cause

Lift cells use materials `LiftUp` (19) and `LiftUpLight` (20), defined in `MaterialDef.cs:255-275` as `BehaviourType.Static` with `density = 0`. The comment even states: *"Lifts are hollow force zones - material passes through them. These materials are for rendering only."*

However, `SimulateChunksJob.CanMoveTo()` (`SimulateChunksJob.cs:~665`) unconditionally blocks movement into any `Static` material:

```csharp
if (targetMat.behaviour == BehaviourType.Static)
    return false;
```

There is no exception for lift materials. So despite the intent that lifts be passable, `CanMoveTo()` treats them as solid walls. This blocks entry from **all directions** (sides, top, bottom), not just sides.

The lift force system itself works correctly — it uses the separate `LiftTile` parallel array to apply upward force to cells already in the zone. The problem is that no cell can ever enter the zone through normal simulation movement.

## Fix Approach
Make `CanMoveTo()` treat lift materials as passable. Options:
1. Add an explicit check for `Materials.IsLift(target.materialId)` before the Static check
2. Add a `MaterialFlags.Passable` flag and check it in `CanMoveTo()`
3. Change lift material behaviour from `Static` to something passable

Option 2 is the most extensible (other future structures could reuse the flag). When a cell moves into a lift position, the lift material should remain (it's a structure) — so this also needs a swap mechanic where the moving cell displaces the lift material, or the lift material coexists.

## Files
- `Assets/Scripts/Simulation/Jobs/SimulateChunksJob.cs` — `CanMoveTo()` method
- `Assets/Scripts/Simulation/MaterialDef.cs` — lift material definitions
