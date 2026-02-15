# Bug: DisplaceCell Silently Loses Material

## Status: OPEN

## Category: Bug / Material Conservation Violation

## Summary

`ClusterManager.DisplaceCell()` can silently destroy material when the BFS displacement search fails to find an empty cell. This directly violates the Material Conservation principle stated in CLAUDE.md.

## Evidence

In `ClusterManager.cs` around line 692-694, when `FindNearestEmptyCell()` returns no result, the displaced cell's material is simply discarded with a comment acknowledging the loss:

```csharp
// cell is lost
```

## Reproduction

1. Create a tightly packed area with clusters and loose material
2. Move or rotate a cluster so it overlaps loose cells
3. If the BFS search radius is exhausted without finding an empty cell, the overlapped material vanishes

## Impact

- Violates the stated principle: "Materials must NEVER silently vanish"
- Players lose material with no feedback or explanation
- More likely in congested areas near structures and terrain

## Expected Behavior

Per CLAUDE.md: "If an operation can't place all materials (congested area, out of bounds), retain the unplaced materials and give the player a way to retry — never discard them."

**Chosen approach:** Spawn the material above the congested area. If the BFS fails to find an empty cell within its search radius, place the displaced cell above the cluster/congestion zone where there is likely to be open air.

## Affected Files

- `Assets/Scripts/Simulation/Clusters/ClusterManager.cs` — `DisplaceCell()`, `FindNearestEmptyCell()`

## Priority

Medium — direct principle violation, but only triggers in tightly packed scenarios.

## See Also

- `OPEN-GrabbedMaterialQuantityLostOnDrop.md` — similar material loss issue in the grab system
