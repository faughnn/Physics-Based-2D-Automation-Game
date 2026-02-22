# Bug: ClusterManager Accumulating God-Class Responsibilities

## Status: MOSTLY FIXED

## Category: Architecture / Refactor

## Summary

`ClusterManager.cs` had 5-6 distinct responsibilities growing with each feature. Most have been addressed.

## Completed Fixes

- **Fracture logic** — extracted to `ClusterFracturer` static class (includes compression detection)
- **Inverse transform deduplication** — `ForEachClusterCell()` shared method with action delegate
- **Anti-sleep boolean accumulation** — replaced `isOnBelt`/`isOnLift` with `activeForceCount` via `IClusterForceProvider`
- **DisplaceCell material loss** — added `FindEmptyCellAbove()` fallback

## Remaining Items

### Duplicated bounding box computation (5+ copies)
The min/max pixel bounds calculation appears in:
- `ClusterData.BuildPixelLookup()`
- `ClusterFracturer.FractureCluster()`
- `ClusterFactory.CalculateBounds()`
- `ClusterDebugSection.CalculateBoundingRadius()`
- `MarchingSquares.GenerateOutline()` and `GenerateOutlines()`

Each computes bounds in a slightly different context (some need local coords, some need world coords, some compute radius). A shared utility could help but may be over-engineering for the current state.

## Priority

Low — the major extractions are done. ClusterManager is now ~530 lines with clear responsibilities.
