# Dead Code: Unused ClusterManager Methods

## Summary
Two public methods in ClusterManager are defined but never called.

## Location
- **File:** `Assets/Scripts/Simulation/Clusters/ClusterManager.cs`

## Unused Methods

### GetCluster(ushort id) - Line 167
```csharp
public ClusterData GetCluster(ushort id)
```
- Retrieves a cluster by its ID
- Never called anywhere in codebase
- Direct dictionary access or iteration is used instead

### Unregister(ClusterData cluster) - Line 150
```csharp
public void Unregister(ClusterData cluster)
```
- Removes a cluster from the manager
- Never called - clusters are registered but never explicitly unregistered
- May indicate a memory leak if clusters should be cleaned up

## Recommended Action
- Remove `GetCluster` if not needed
- Investigate whether `Unregister` should be called somewhere (potential memory leak) or remove if clusters are never destroyed
