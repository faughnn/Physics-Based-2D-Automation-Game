# Bug: Rigid Body + Belt Frame Stutter

## Summary
Spawning rigid bodies causes periodic frame stutters (every ~0.5 seconds), but ONLY when belts are also placed in the world. Simulation time spikes momentarily high during these stutters.

## Symptoms
- Frame stutter every few seconds when rigid bodies exist
- Only occurs when belts are placed (no belts = no stutter)
- Simulation time metric spikes during stutters
- Gets worse with more clusters and/or more belts

## Root Cause
**O(clusters * belts * pixels) complexity** in `BeltManager.ApplyForcesToClusters()` combined with **excessive unthrottled Debug.Log calls**.

### Primary Issue: Expensive Nested Loops
The `ApplyForcesToClusters` method runs every frame with triple-nested loops:

```csharp
foreach (var cluster in clusterManager.AllClusters)     // O(clusters)
    for (int i = 0; i < belts.Length; i++)              // O(belts)
        foreach (var pixel in cluster.pixels)           // O(pixels) in ClusterRestingOnBelt
```

Each pixel iteration calls `LocalToWorldCell()` which performs:
- `Mathf.Cos()` and `Mathf.Sin()` calls
- 4 floating-point multiplications
- Integer division and rounding

With a default 15-radius cluster (~707 pixels) and 10 belts: **7,070 iterations per cluster per frame**.

### Secondary Issue: Unthrottled Debug.Log Calls
7 different Debug.Log calls in `ApplyForcesToClusters`:
- **Line 699**: Logs every frame when force is applied (NO THROTTLE)
- **Lines 725, 734, 743**: Log per-pixel details every 30 frames

Every 30 frames (~0.5 seconds), `logDetails` becomes true, triggering per-pixel logging. With 707 pixels * 10 belts = **7,070 Debug.Log calls in a single frame**.

## Affected Code
- `Assets/Scripts/Structures/BeltManager.cs:670-749` - `ApplyForcesToClusters()` method
- `Assets/Scripts/Structures/BeltManager.cs:699` - Unthrottled Debug.Log
- `Assets/Scripts/Structures/BeltManager.cs:725,734,743` - Per-pixel Debug.Log every 30 frames
- `Assets/Scripts/Simulation/Clusters/ClusterData.cs:54-55` - `LocalToWorldCell()` trig calculations
- `Assets/Scripts/Simulation/CellSimulatorJobbed.cs:48-53` - Calls belt forces every frame

## Potential Solutions

### 1. Remove/Throttle Debug.Log Calls (Quick Fix)
Remove or heavily throttle all Debug.Log calls in `ApplyForcesToClusters`. Especially line 699 which has no throttle at all.
- **Tradeoff**: Loses debug visibility, but massive perf gain

### 2. Bounding Box Pre-Check
Before iterating pixels, check if cluster AABB overlaps belt bounds:
```csharp
if (!cluster.bounds.Overlaps(belt.bounds)) continue;
```
- **Tradeoff**: Small overhead per check, but skips most pixel iterations

### 3. Cache Trig Calculations
`LocalToWorldCell` recalculates cos/sin for every pixel. Cache these per-cluster per-frame:
```csharp
float cachedCos = Mathf.Cos(rotation);
float cachedSin = Mathf.Sin(rotation);
```
- **Tradeoff**: Minor memory, significant CPU savings

### 4. Skip Sleeping Clusters Entirely
Currently iterates all pixels of sleeping clusters. Skip them completely unless belt is directly underneath.
- **Tradeoff**: May miss edge cases where sleeping cluster should wake

### 5. Spatial Partitioning
Only check clusters that are spatially near belts using a grid or quadtree.
- **Tradeoff**: More complex, but scales properly with world size

## Priority
**High** - Causes noticeable gameplay stutters that scale poorly with content

## Related Files
- `Assets/Scripts/Structures/BeltManager.cs`
- `Assets/Scripts/Structures/BeltStructure.cs`
- `Assets/Scripts/Simulation/Clusters/ClusterData.cs`
- `Assets/Scripts/Simulation/Clusters/ClusterManager.cs`
- `Assets/Scripts/Simulation/CellSimulatorJobbed.cs`
