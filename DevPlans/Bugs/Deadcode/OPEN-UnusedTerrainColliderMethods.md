# Dead Code: Unused TerrainColliderManager Methods

## Summary
Two public methods in TerrainColliderManager are never called externally.

## Location
- **File:** `Assets/Scripts/Simulation/Clusters/TerrainColliderManager.cs`

## Unused Methods

### MarkChunkDirty(int chunkIndex) - Line 95
```csharp
public void MarkChunkDirty(int chunkIndex)
```
- Only called internally by `MarkChunkDirtyAt()`
- No external code calls this with a direct chunk index
- Should be made `private`

### RegenerateAllColliders() - Line 281
```csharp
public void RegenerateAllColliders()
```
- Forces regeneration of all chunk colliders
- Never called anywhere in the codebase
- May have been for debugging or level transitions

## Recommended Action
- Make `MarkChunkDirty` private
- Remove `RegenerateAllColliders` or keep if useful for debugging/level loading
