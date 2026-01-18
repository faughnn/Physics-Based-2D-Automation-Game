# Bug: Displacement BFS Slowdown

## Summary
Cluster sync becomes slow when dropping clusters into loose materials (sand, water). The displacement system does expensive BFS searches for each overlapping pixel.

## Symptoms
- Noticeable frame rate drop when cluster enters sand/water
- Sync time increases significantly during displacement

## Root Cause
In `ClusterManager.SyncClusterToWorld()`, when a cluster pixel overlaps a loose material cell, `DisplaceCell()` is called. This triggers `FindNearestEmptyCell()` which does a BFS search with radius 16.

For a cluster with N pixels overlapping M loose cells:
- M separate BFS searches per frame
- Each BFS can visit hundreds of cells before finding empty space
- O(M × search_area) per frame while cluster is moving through material

Example: 225-pixel cluster with 100 overlapping sand cells = 100 BFS searches/frame

## Affected Code
- `ClusterManager.cs:DisplaceCell()` - line ~326
- `ClusterManager.cs:FindNearestEmptyCell()` - line ~378

## Potential Solutions

### 1. Batch displacement searches
Instead of searching for each pixel independently, collect all pixels needing displacement and find empty spots in one pass.

### 2. Reduce search radius
Current radius is 16. Could use adaptive radius - start small, expand only if needed.

### 3. Cache empty cell positions
Track known empty cells nearby and reuse between displacement calls within same frame.

### 4. Limit displacements per frame
Cap the number of displacement searches per frame, let remaining cells be handled next frame.

### 5. Skip displacement for fast-moving clusters
If cluster velocity is high, material will be displaced naturally by subsequent frames anyway.

## Priority
Medium - affects gameplay feel but doesn't break functionality

## Related Files
- `Assets/Scripts/Simulation/Clusters/ClusterManager.cs`
