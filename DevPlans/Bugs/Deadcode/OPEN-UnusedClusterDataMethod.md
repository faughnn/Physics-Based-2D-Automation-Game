# Dead Code: Unused ClusterData.LocalToWorld Method

## Summary
A legacy coordinate conversion method exists but is never called.

## Location
- **File:** `Assets/Scripts/Simulation/Clusters/ClusterData.cs`
- **Line:** 86-101

## Unused Method
```csharp
/// <summary>
/// Legacy method - converts cluster pixel to world position
/// </summary>
public Vector2Int LocalToWorld(ClusterPixel pixel)
```

## Reason
- The code uses `LocalToWorldCell()` and `LocalToWorldFloat()` instead
- Comment explicitly marks it as "Legacy method"
- Performs same function as newer methods but with different signature

## Recommended Action
Remove the legacy method since newer alternatives are used everywhere.
