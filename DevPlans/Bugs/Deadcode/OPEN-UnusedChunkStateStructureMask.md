# Dead Code: Unused ChunkState.structureMask Field

## Summary
The `structureMask` field in ChunkState is initialized to 0 but never read or meaningfully modified.

## Location
- **File:** `Assets/Scripts/Simulation/ChunkState.cs`
- **Line:** 12

## Field
```csharp
public ulong structureMask;   // Bitmask of which structures are in this chunk
```

## Usage
- Set to 0 in CellWorld constructor during chunk initialization
- Never modified after initialization
- Never read for any logic

## Intended Purpose
Likely meant to track which structure types exist in each chunk for optimization (skip structure simulation for chunks without structures).

## Recommended Action
Remove if the structure mask optimization is not planned, or implement the optimization.
