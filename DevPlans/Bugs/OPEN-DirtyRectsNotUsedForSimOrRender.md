# Bug: Dirty Rectangles Tracked But Never Used by Simulation or Renderer

**Status:** OPEN
**Reported:** 2026-01-28

## Description

The dirty rectangle system (`minX/maxX/minY/maxY` per chunk) is correctly tracked by all code paths — belts, powder, water, clusters, digging — but **no system actually reads the bounds** to narrow its work. Both the simulation job and the cell renderer always process the full 64x64 chunk region regardless of how small the dirty area is.

The dirty bounds are effectively write-only data, read only by the debug gizmo overlay in `WorldDebugSection`.

## Evidence

### Dirty bounds are tracked correctly everywhere

All three `MarkDirty` variants properly expand per-cell bounds:

- **`CellWorld.MarkDirty()`** (`CellWorld.cs:115-138`) — CPU-side, used by belts, lifts, clusters, digging, SetCell
- **`SimulateChunksJob.MarkDirtyInternal()`** (`SimulateChunksJob.cs:718-741`) — Burst job, used during cell movement
- **`SimulateBeltsJob.MarkDirty()`** — Belt simulation job, same pattern

All correctly set `IsDirty` flag and expand `minX/maxX/minY/maxY`.

### Simulation ignores dirty bounds

`SimulateChunksJob.SimulateChunk()` (`SimulateChunksJob.cs:70-103`) always iterates the full core region:

```csharp
int coreMinX = chunkX * ChunkSize;
int coreMinY = chunkY * ChunkSize;
int coreMaxX = math.min(width, coreMinX + ChunkSize);
int coreMaxY = math.min(height, coreMinY + ChunkSize);

for (int y = coreMaxY - 1; y >= coreMinY; y--)
{
    for (int x = startX; x != endX; x += stepX)
    {
        SimulateCell(x, y);
    }
}
```

It never reads `chunk.minX`, `chunk.maxX`, `chunk.minY`, or `chunk.maxY`. Every active chunk processes all 4,096 cells.

### Renderer ignores dirty bounds

`CellRenderer.UploadChunk()` (`CellRenderer.cs:249-278`) always uploads the full 64x64 region:

```csharp
int startX = chunkX * CellWorld.ChunkSize;
int startY = chunkY * CellWorld.ChunkSize;
int endX = Mathf.Min(startX + CellWorld.ChunkSize, world.width);
int endY = Mathf.Min(startY + CellWorld.ChunkSize, world.height);
```

No dirty bounds consulted.

### Only consumer is debug gizmos

`WorldDebugSection.DrawGizmos()` (`WorldDebugSection.cs:51-108`) reads the dirty bounds to draw colored rectangles. This is the only code that reads the bounds.

### Additional issue: MarkChunkDirtyOnly skips bounds

`CellWorld.MarkChunkDirtyOnly()` (`CellWorld.cs:177-183`) sets `IsDirty` but does **not** set dirty bounds (they remain inverted: min > max). Used for neighbor chunk waking. This is harmless today since nothing reads bounds, but would be a bug if dirty-rect-scoped processing were implemented.

## Impact

- **Performance:** Every active chunk simulates all 4,096 cells even if only a small region changed. For chunks where a single cell moved near an edge, 99%+ of `SimulateCell` calls are wasted.
- **Rendering:** Every active chunk uploads all 4,096 pixels to the texture even if only a small sub-region changed.
- **Misleading infrastructure:** The dirty rect tracking code gives the impression that sub-chunk optimization is in place, but it isn't.

## Severity

Medium — performance optimization. The dirty/active chunk system still prevents fully inactive chunks from being processed, but within active chunks, no sub-chunk optimization occurs despite the infrastructure being in place.
