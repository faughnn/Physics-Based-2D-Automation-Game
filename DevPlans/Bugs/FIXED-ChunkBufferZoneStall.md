# Bug: Chunk Buffer Zone Stall (FIXED)

## Summary
Sand (and other falling materials) briefly paused at specific Y coordinates corresponding to chunk boundaries, causing visible horizontal banding.

## Root Cause
The issue had two parts:

1. **Chunks were simulating cells in their buffer zone** - A chunk would simulate cells outside its 32x32 core, causing ownership conflicts with the actual owning chunk.

2. **Handoff delay at boundaries** - When a cell moved from one chunk's core into another chunk's core, the `frameUpdated` flag prevented the receiving chunk from continuing the movement in the same frame, causing a 1-frame stall.

## Solution

### Fix 1: Core-Only Simulation
Chunks now only simulate cells within their 32x32 core region. The buffer zone is only for writing destinations - cells that land there are owned by a different chunk.

```csharp
// SimulateChunk now iterates only over core, not extended region
for (int y = coreMaxY - 1; y >= coreMinY; y--)
```

### Fix 2: Cross-Chunk Handoff
When a cell moves outside its chunk's core (into the buffer), we clear `frameUpdated` so the receiving chunk can continue its movement in the same frame.

```csharp
// In MoveCell:
bool crossingChunkBoundary = toX < coreMinX || toX >= coreMaxX || toY < coreMinY || toY >= coreMaxY;
if (crossingChunkBoundary)
{
    cell.frameUpdated = 0;
}
```

### Settings Restored
- `BufferSize = 16` (was temporarily changed to 15)
- `MaxVelocity = 16` (was temporarily changed to 15)

## Files Modified
- `Assets/Scripts/Simulation/Jobs/SimulateChunksJob.cs`
- `Assets/Scripts/Simulation/PhysicsSettings.cs`
- `CLAUDE.md`

## Trade-off
Cells crossing chunk boundaries may receive double gravity application (once per chunk). This is acceptable because:
- Gravity is only applied every 15 frames, so it rarely happens
- Velocity is capped at 16, limiting the effect
- The visual improvement far outweighs the minor physics inconsistency
