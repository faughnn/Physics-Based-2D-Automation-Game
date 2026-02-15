# Bug: ClusterManager Accumulating God-Class Responsibilities

## Status: OPEN

## Category: Architecture / Refactor

## Summary

`ClusterManager.cs` (769 lines) has 5-6 distinct responsibilities that are growing with each new feature. While not yet a classic god class, the trend is toward unsustainable accumulation. The compression/fracture system (added recently) is the clearest extraction candidate.

## Current Responsibilities

1. **ID Allocation** (lines 117-138) — `AllocateId()`, `ReleaseId()` via Queue pool
2. **Registry/Lifecycle** (lines 143-188) — `Register()`, `Unregister()`, `RemoveCluster()`, dictionary bookkeeping
3. **Physics Orchestration** (lines 193-269) — `StepAndSync()`, manual sleep forcing workaround (30 lines), Physics2D.Simulate coordination
4. **Grid Sync** (lines 495-761) — `ClearAllPixelsFromWorld()`, `SyncAllToWorld()`, inverse-mapping rasterization, `DisplaceCell()`, `FindNearestEmptyCell()` (~266 lines, the largest responsibility)
5. **Compression Detection** (lines 279-335) — `CheckCompressionAndFracture()`, contact analysis, crush frame tracking
6. **Fracture Logic** (lines 342-489) — `FractureCluster()`, crack-line partitioning, pixel group assignment, sub-cluster creation (~147 lines of algorithm)

## Specific Issues

### Duplicated bounding box computation (5+ copies)
The min/max pixel bounds calculation appears in:
- `ClusterData.BuildPixelLookup()` (lines 85-93)
- `ClusterManager.FractureCluster()` (lines 351-359)
- `ClusterFactory.CalculateBounds()` (lines 198-214)
- `ClusterDebugSection.CalculateBoundingRadius()` (lines 198-207)
- `MarchingSquares.GenerateOutline()` (lines 127-136) and `GenerateOutlines()` (lines 27-33)

### Duplicated inverse transform code
The inverse mapping from cell-space to local pixel-space appears identically in:
- `ClearClusterPixels()` (lines 517-547)
- `SyncClusterToWorld()` (lines 597-631)

Both compute cos, sin, cellCenter, bounding box extents, then iterate cells doing the same transform. ~50 lines of duplicated setup; only the inner operation differs (clear vs. write).

### Manual sleep forcing is a growing special-case list
The anti-sleep guard (line 240) has 4 conditions that grow with each new feature:
```csharp
if (data.isOnBelt || data.isOnLift || data.isMachinePart || data.crushPressureFrames > 0)
```

## Extraction Candidates

| Responsibility | Target | Effort |
|---------------|--------|--------|
| Fracture logic | `ClusterFracturer` static class | Low — self-contained algorithm, only needs pixel list and crack parameters |
| Grid sync (clear + write) | `ClusterGridSync` class | Medium — tightly coupled to cluster dictionary and world reference |
| Compression detection | Merge into `ClusterFracturer` or separate `ClusterDamageSystem` | Low |
| Inverse transform utility | Shared method with action delegate for the inner operation | Low |
| Bounding box computation | Single utility method in `ClusterData` or `ClusterUtils` | Low |

## Affected Files

- `Assets/Scripts/Simulation/Clusters/ClusterManager.cs`
- `Assets/Scripts/Simulation/Clusters/ClusterData.cs`
- `Assets/Scripts/Simulation/Clusters/ClusterFactory.cs`
- `Assets/Scripts/Simulation/Clusters/MarchingSquares.cs`
- `Assets/Scripts/Debug/Sections/ClusterDebugSection.cs`

## Priority

Medium — currently manageable at 769 lines, but each new cluster feature accelerates the problem.
