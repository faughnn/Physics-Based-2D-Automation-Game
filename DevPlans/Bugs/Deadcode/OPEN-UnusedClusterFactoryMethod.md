# Dead Code: Unused ClusterFactory.CreateClusterFromRegion

## Summary
A public static method for creating clusters from world regions is never called.

## Location
- **File:** `Assets/Scripts/Simulation/Clusters/ClusterFactory.cs`
- **Line:** 98-151

## Unused Method
```csharp
public static ClusterData CreateClusterFromRegion(
    CellWorld world,
    int startX, int startY,
    int regionWidth, int regionHeight,
    ClusterManager manager)
```

## Purpose
- Creates a cluster by extracting cells from a rectangular world region
- Alternative to `CreateCluster()` which takes a Texture2D

## Reason Unused
- All cluster creation in the codebase uses `CreateCluster(Texture2D, ...)` instead
- No gameplay system extracts regions from the world to create clusters

## Recommended Action
Remove if not needed, or keep if planned for a future "selection to cluster" feature.
