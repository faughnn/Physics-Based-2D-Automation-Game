# Bug: Small Materials Can Get Inside Clusters

## Summary
Small materials (sand, powder, etc.) can penetrate into cluster rigid bodies, getting inside their boundaries instead of colliding with their surfaces.

## Symptoms
- Sand or other loose materials pass through cluster boundaries
- Small particles end up embedded inside rigid body clusters
- More noticeable with fast-moving or small materials
- Clusters may appear to "absorb" nearby loose cells

## Root Cause
The `CanMoveTo` function in `SimulateChunksJob.cs` is **missing a check for cluster ownership** (`ownerId`).

While the simulation correctly skips simulating cluster-owned cells (line 104):
```csharp
if (cell.ownerId != 0)
    return;
```

The `CanMoveTo` function only checks material type and density, not ownership:
```csharp
private bool CanMoveTo(int x, int y, byte myDensity)
{
    // ... bounds check ...
    Cell target = cells[y * width + x];
    if (target.materialId == Materials.Air)
        return true;
    // MISSING: if (target.ownerId != 0) return false;
    MaterialDef targetMat = materials[target.materialId];
    if (targetMat.behaviour == BehaviourType.Static)
        return false;
    return myDensity > targetMat.density;
}
```

This allows cells to move into positions occupied by cluster pixels if the material density check passes.

### Why Small/Fast Materials Are More Affected
1. Higher velocities mean more cells traversed per frame (up to 16 via `MaxVelocity`)
2. Discrete collision checks in `TryFall` trace paths using the flawed `CanMoveTo`
3. Edge timing allows particles to slip through between cluster sync phases

## Affected Code
- `Assets/Scripts/Simulation/Jobs/SimulateChunksJob.cs:406-424` - `CanMoveTo()` function

## Potential Solutions

### 1. Add ownerId Check to CanMoveTo (Recommended)
Add a single line to `CanMoveTo`:
```csharp
if (target.ownerId != 0)
    return false;
```

This prevents cells from ever moving into cluster-occupied positions. Simple, direct fix addressing the root cause.

### 2. Check ownerId in All Movement Functions
Rather than just `CanMoveTo`, audit all movement functions (`TryFall`, `TrySlide`, etc.) to ensure they respect cluster ownership. More thorough but `CanMoveTo` is the central gatekeeper so Solution 1 should suffice.

## Priority
Medium - Does not cause crashes but breaks expected physics behavior and visual coherence.

## Related Files
- `Assets/Scripts/Simulation/Jobs/SimulateChunksJob.cs` - Cell simulation and movement
- `Assets/Scripts/Simulation/Clusters/ClusterManager.cs` - Cluster sync and displacement
- `Assets/Scripts/Simulation/Clusters/ClusterData.cs` - Cluster pixel ownership
- `Assets/Scripts/Simulation/Cell.cs` - Cell struct with ownerId field
