# Bug: No Unified Force Application System

## Status: OPEN

## Category: Architecture / Refactor

## Summary

Forces on clusters are applied through three separate, ad-hoc mechanisms with inconsistent APIs. There is no `ForceZoneManager` or unified interface. Adding a new force provider requires changes to 4 files and a new boolean flag on `ClusterData`.

## Current State

### Inconsistent force semantics

| Source | Method | Mechanism | Problem |
|--------|--------|-----------|---------|
| BeltManager | `ApplyForcesToClusters()` line 697 | **Velocity injection** (`vel.x = direction * speed`) | Overrides existing velocity — conflicts with other forces |
| LiftManager | `ApplyForcesToClusters()` line 387 | **AddForce()** | Proper physics interaction |
| PistonManager | `UpdateMotors()` line 270 | **AddForce()** | Proper physics interaction |

If a cluster sits on a belt AND is pushed by a piston, the belt's velocity override fights the piston's force unpredictably.

### O(n*m) scaling
Both `BeltManager.ApplyForcesToClusters` and `LiftManager.ApplyForcesToClusters` iterate ALL clusters against ALL structures. No spatial partitioning or broad-phase rejection at the structure level.

### Special-case booleans accumulating
`ClusterData` has `isOnBelt`, `isOnLift`, `isMachinePart` — each a separate boolean checked in the anti-sleep guard at `ClusterManager.StepAndSync()` line 240. Each new force type would add another flag:
```csharp
if (data.isOnBelt || data.isOnLift || data.isMachinePart || data.crushPressureFrames > 0)
    // don't force sleep
```

### Force application wired manually
In `CellSimulatorJobbed.Simulate()` (lines 77-80), each force provider is called individually:
```csharp
if (beltManager != null) beltManager.ApplyForcesToClusters(...);
if (liftManager != null) liftManager.ApplyForcesToClusters(...);
```
Adding a new force type means adding another `if` block here.

## Refactor Direction

- Define an `IClusterForceProvider` interface with `ApplyForcesToClusters(ClusterManager, ...)`
- Register providers in a list, iterate in a single loop
- **Each provider keeps its own application semantics** — belts use velocity injection (intentional: moves whole powder column, not just bottom row), lifts and pistons use `AddForce()`
- Replace individual boolean flags (`isOnBelt`, `isOnLift`, `isMachinePart`) on `ClusterData` with a `ClusterFlags` bitfield or a count of active force sources, so the sleep guard doesn't need a growing list of special cases
- Consider spatial queries (force zones with bounds) instead of O(n*m) iteration

## Affected Files

- `Assets/Scripts/Structures/BeltManager.cs` — `ApplyForcesToClusters()`
- `Assets/Scripts/Structures/LiftManager.cs` — `ApplyForcesToClusters()`
- `Assets/Scripts/Simulation/Machines/PistonManager.cs` — `UpdateMotors()`
- `Assets/Scripts/Simulation/CellSimulatorJobbed.cs` — manual wiring
- `Assets/Scripts/Simulation/Clusters/ClusterData.cs` — boolean flags
- `Assets/Scripts/Simulation/Clusters/ClusterManager.cs` — anti-sleep guard

## Priority

Medium-High — inconsistent force semantics cause subtle physics bugs, and the pattern doesn't scale.
