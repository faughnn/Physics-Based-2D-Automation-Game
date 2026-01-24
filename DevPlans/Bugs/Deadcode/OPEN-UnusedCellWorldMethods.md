# Dead Code: Unused CellWorld Methods

## Summary
Two public methods in CellWorld are defined but never called.

## Location
- **File:** `Assets/Scripts/Simulation/CellWorld.cs`

## Unused Methods

### GetActiveChunks(NativeArray<int> activeChunkIndices) - Line 213
```csharp
public int GetActiveChunks(NativeArray<int> activeChunkIndices)
```
- Collects indices of chunks that need simulation
- Superseded by `CollectChunkGroups()` which handles the 4-pass checkerboard pattern
- Legacy method from before parallel processing was implemented

### MarkDirtyWithNeighbors(int cellX, int cellY) - Line 144
```csharp
public void MarkDirtyWithNeighbors(int cellX, int cellY)
```
- Marks a cell's chunk and all adjacent chunks as dirty
- Never called; `MarkDirty()` is used instead
- May have been for an earlier dirty propagation approach

## Recommended Action
Remove both methods as they are superseded by current implementations.
