# Bug: Belts Not Moving Rigid Bodies

## Summary
Belts are supposed to apply force to move rigid body clusters along their surface, but clusters that land on belts remain stationary instead of being transported.

## Symptoms
- Clusters fall onto belts and stop moving
- Belts appear to have no effect on clusters
- Works briefly when cluster first contacts belt, then stops
- Debug logs show "Cluster is sleeping, skipping"

## Root Cause
**Sleeping clusters are entirely skipped in the belt force application code.**

When a cluster lands on a belt:
1. It briefly receives belt force while moving
2. Velocity drops below 3 units/sec threshold
3. After 30 frames (~0.5 sec), `ClusterManager.StepAndSync()` forces it to sleep
4. `BeltManager.ApplyForcesToClusters()` skips all sleeping clusters
5. Cluster never wakes up, never receives belt force again

### The Skip Logic (BeltManager.cs:682-688)
```csharp
if (cluster.rb.IsSleeping())
{
    if (beltForceLogCounter % 60 == 0)
        Debug.Log($"[BeltForce] Cluster #{cluster.clusterId} is sleeping, skipping");
    continue;  // <-- SLEEPING CLUSTERS NEVER RECEIVE FORCE
}
```

### The Sleep Logic (ClusterManager.cs:204-231)
```csharp
if (linVel < 3f && contactCount > 0)
{
    cluster.lowVelocityFrames++;
    if (cluster.lowVelocityFrames > 30)
    {
        cluster.rb.linearVelocity = Vector2.zero;
        cluster.rb.angularVelocity = 0f;
        cluster.rb.Sleep();  // <-- Forced to sleep
    }
}
```

### Secondary Issue: Surface Y Exact Match
The belt detection requires cluster pixels to be **exactly** at `surfaceY` (belt.tileY - 1). No tolerance means slight positioning differences can cause missed detection.

### Tertiary Issue: Weak Force
`BeltForceStrength = 5f` may be too weak for the cluster masses involved. A 15-radius cluster has mass ~707, giving acceleration of only 0.007 units/sec^2.

## Affected Code
- `Assets/Scripts/Structures/BeltManager.cs:682-688` - Sleeping cluster skip
- `Assets/Scripts/Structures/BeltManager.cs:738` - Exact surfaceY match
- `Assets/Scripts/Structures/BeltManager.cs:668` - BeltForceStrength constant
- `Assets/Scripts/Simulation/Clusters/ClusterManager.cs:204-231` - Force sleep logic

## Potential Solutions

### 1. Wake Clusters on Belts (Recommended)
Before skipping sleeping clusters, check if they're on a belt and wake them:
```csharp
if (cluster.rb.IsSleeping())
{
    // Check if on any belt first
    for (int i = 0; i < belts.Length; i++)
    {
        if (ClusterRestingOnBelt(cluster, belts[i], ...))
        {
            cluster.rb.WakeUp();
            break;
        }
    }
    if (cluster.rb.IsSleeping()) continue;  // Still sleeping = not on belt
}
```
- **Tradeoff**: Additional belt checks for sleeping clusters, but correct behavior

### 2. Remove Sleeping Check Entirely
Let Unity's physics handle sleep naturally instead of manual sleep logic:
```csharp
// Remove this block entirely:
// if (cluster.rb.IsSleeping()) continue;
```
- **Tradeoff**: May conflict with ClusterManager's sleep optimization

### 3. Add Surface Y Tolerance
Allow clusters slightly above or below the exact surface to receive force:
```csharp
if (cellPos.y >= surfaceY - 1 && cellPos.y <= surfaceY + 1 && ...)
```
- **Tradeoff**: May apply force to clusters not truly resting on belt

### 4. Increase Belt Force Strength
Increase `BeltForceStrength` from 5f to something proportional to expected mass:
```csharp
private const float BeltForceStrength = 500f;  // Or calculate based on mass
```
- **Tradeoff**: May cause other physics issues if too strong

### 5. Exempt Belt-Adjacent Clusters from Sleep
In ClusterManager, don't force sleep if cluster is on a belt (requires belt awareness):
- **Tradeoff**: Adds coupling between ClusterManager and BeltManager

## Priority
**High** - Core feature (belt transport) is completely non-functional

## Related Files
- `Assets/Scripts/Structures/BeltManager.cs`
- `Assets/Scripts/Structures/BeltStructure.cs`
- `Assets/Scripts/Simulation/Clusters/ClusterManager.cs`
- `Assets/Scripts/Simulation/Clusters/ClusterData.cs`
- `Assets/Scripts/Simulation/Clusters/ClusterFactory.cs`
