# Bug: Cluster Cracking Overwrites Nearby Walls

## Summary
When a cluster cracks apart (e.g., from piston compression), the resulting sub-clusters overwrite wall cells below/adjacent to them. Wall cells are displaced as if they were loose material, then the cluster pixels are written unconditionally over the wall positions.

## Symptoms
- Walls below a cluster get partially destroyed when the cluster fractures
- Wall cells are "displaced" to nearby empty spots via BFS, corrupting the wall structure
- Happens specifically when a piston squeezes a cluster against or near a wall

## Reproduction
1. Place a wall structure
2. Place a cluster on top of or adjacent to the wall
3. Use a piston to squeeze the cluster from the side until it fractures
4. Observe wall cells being eaten/overwritten by the fractured cluster pieces

## Root Cause
`SyncClusterToWorld()` in `ClusterManager.cs:622-641` has no protection against overwriting static structure cells. The logic flow:

1. `ClearAllPixelsFromWorld()` removes cluster cells from grid
2. `Physics2D.Simulate()` moves clusters (piston pushes cluster against wall collider)
3. `CheckCompressionAndFracture()` fractures the cluster into sub-clusters at overlapping positions
4. `SyncAllToWorld()` writes all cluster pixels back to the grid

During step 4, `SyncClusterToWorld()` checks for non-Air unowned cells and "displaces" them, but this displacement treats wall cells as loose material:

```csharp
if (existing.materialId != Materials.Air && existing.ownerId == 0)
{
    DisplaceCell(new Vector2Int(cx, cy), existing, cluster.Velocity);
}
// Unconditionally writes cluster pixel over the position
world.cells[index] = newCell;
```

Wall cells match the condition (`materialId != Air`, `ownerId == 0`), so they get moved to a random nearby empty cell via BFS, then the cluster pixel overwrites the wall position.

**Contrast with correct behavior elsewhere:**
- `SimulateChunksJob.cs:771` correctly blocks cell movement into Static non-Passable cells
- `PistonManager.cs:518` correctly stops pushing when hitting Static materials
- `ClusterManager.SyncClusterToWorld()` has **no equivalent check**

## Affected Code
- `Assets/Scripts/Simulation/Clusters/ClusterManager.cs:622-641` - `SyncClusterToWorld()` unconditionally overwrites
- `Assets/Scripts/Simulation/Clusters/ClusterManager.cs:652-684` - `DisplaceCell()` displaces wall cells as loose material
- `Assets/Scripts/Simulation/Clusters/ClusterManager.cs:693-750` - `FindNearestEmptyCell()` moves wall cells to random spots
- `Assets/Scripts/Simulation/Clusters/ClusterManager.cs:331-478` - `FractureCluster()` creates sub-clusters at overlapping positions near walls

## Potential Solutions

### 1. Skip Static Non-Passable Cells During Sync
In `SyncClusterToWorld()`, before writing a cluster pixel, check if the existing cell is a static structure that shouldn't be overwritten:

```csharp
if (existing.materialId != Materials.Air && existing.ownerId == 0)
{
    MaterialDef existingMat = world.materials[existing.materialId];
    if (existingMat.behaviour == BehaviourType.Static &&
        (existingMat.flags & MaterialFlags.Passable) == 0)
    {
        continue; // Don't overwrite walls/structures
    }
    DisplaceCell(new Vector2Int(cx, cy), existing, cluster.Velocity);
}
```

The cluster pixel is simply not written at that position. The pixel data is retained in the cluster's pixel list, so it will attempt to write again next frame (and succeed if the cluster moves away from the wall). This is the simplest fix and aligns with how every other system handles walls.

**Material conservation note:** The cluster pixel isn't destroyed, it just can't be written to the grid while a wall occupies that cell. On subsequent frames, as the cluster moves, the pixel will find a valid cell to occupy.

### 2. Additionally Guard DisplaceCell
As extra safety, `DisplaceCell()` should also skip static structure cells so they are never displaced even if future code paths call it:

```csharp
private void DisplaceCell(Vector2Int pos, Cell cell, Vector2 velocity)
{
    MaterialDef mat = world.materials[cell.materialId];
    if (mat.behaviour == BehaviourType.Static && (mat.flags & MaterialFlags.Passable) == 0)
        return; // Never displace static structures
    // ... existing BFS displacement logic
}
```

## Priority
High

## Related Files
- `Assets/Scripts/Simulation/Clusters/ClusterManager.cs`
- `Assets/Scripts/Simulation/Clusters/ClusterData.cs`
- `Assets/Scripts/Simulation/Machines/PistonManager.cs`
- `Assets/Scripts/Simulation/Jobs/SimulateChunksJob.cs`
- `Assets/Scripts/Simulation/Structures/WallManager.cs`
