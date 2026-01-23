# Chunk Boundary Race Conditions - Execution Analysis

**Date:** 2026-01-23
**Status:** Critical Issues Identified
**Context:** Comparing implementation against Noita's 4-phase checkerboard approach

---

## Executive Summary

The implementation correctly follows Noita's architectural pattern (4-phase checkerboard, overlap zones, single-buffering), but has **critical execution flaws** that cause data races and chunk boundary problems. The issues stem from:

1. **Insufficient buffer for multi-cell movements** - Liquid/gas spreading exceeds the safety gap
2. **Non-atomic cell operations** - 10-byte Cell struct creates torn reads/writes
3. **Inadequate synchronization** - frameUpdated check is not atomic
4. **Race conditions in chunk state updates** - MarkDirtyInternal has read-modify-write races

---

## Architecture Comparison

### What's Correct ✓

- **4-phase checkerboard pattern** - Groups A, B, C, D processed sequentially
- **Group assignment** - `(chunkX & 1) + ((chunkY & 1) << 1)` matches Noita
- **Overlap zones** - 15-cell buffer creating 62-cell extended regions
- **Velocity constraint** - MaxVelocity = 15 matches BufferSize = 15
- **Bottom-up processing** - Y iteration from max to min with alternating X direction
- **Single-buffered** - Writes directly to cells array
- **Dirty chunk optimization** - Tracks IsDirty, activeLastFrame, HasStructure
- **Chunk wake-up system** - MarkDirtyWithNeighbors() with EdgeThreshold

### Scale Comparison

| Parameter | Your Implementation | Noita | Notes |
|-----------|---------------------|-------|-------|
| Chunk size | 32×32 cells | 64×64 pixels | Proportionally similar |
| Buffer size | 15 cells | 32 pixels | Both ~47-50% of chunk size |
| Extended region | 62 cells (32+15+15) | 128 pixels (64+32+32) | Same concept |
| Same-group spacing | 64 cells | 128 pixels | 2× chunk size |
| Safety gap | 2 cells | 0 cells (regions touch) | Critical difference |
| Max velocity | 15 cells/frame | 32 pixels/frame | Enforced |

---

## Issue #1: Insufficient Buffer for Multi-Cell Movements ⚠️ CRITICAL

### The Problem

Buffer size (15 cells) is designed for **velocity-based movement** (MaxVelocity=15).
However, **liquids and gases use pathfinding-based movement** that can exceed this.

### The Math

**Chunk Layout (Group A):**
```
Chunk 0: core cells [0, 31], extended region [0, 46]
Chunk 2: core cells [64, 95], extended region [49, 110]
Gap between extended regions: cells [47, 48] = 2-cell gap
```

**Liquid spreading from X=46 (edge of chunk 0's extended region):**
```
dispersionRate:  5 (water) or 4 (oil)
velocityBoost:  +5 (when velocityY = 15, falling at max speed)
randomOffset:   +1 (random variation)
─────────────────────────────────────
MAXIMUM:        11 cells
Reaches:        X = 46 + 11 = 57 ← INSIDE CHUNK 2's REGION [49, 110]
```

**Gas spreading from X=46:**
```
hardcoded spread: 4 cells
Reaches:          X = 46 + 4 = 50 ← INSIDE CHUNK 2's REGION [49, 110]
```

### The Race Condition

When chunks 0 and 2 run **IN PARALLEL** (both are Group A):

```
Thread 1 (Chunk 0):                    Thread 2 (Chunk 2):
─────────────────────────────────────  ─────────────────────────────────────
Processes cell at X=46                 Processes cells X ∈ [49, 110]
Liquid spreads 11 cells right          Currently at X=57
Writes cells[57] via MoveCell()        Reads/writes cells[57] via SimulateCell()

                    ↓ CONCURRENT ACCESS TO cells[57] ↓
                           DATA RACE
```

### Noita's Solution

**"No pixel can move more than 32 pixels in a single frame"**

This is a **HARD CONSTRAINT on ALL movement types**, not just velocity:
- Velocity-based movement: capped at 32 pixels
- Liquid spreading: capped at 32 pixels
- Gas dispersion: capped at 32 pixels
- Pathfinding: capped at 32 pixels

Noita's extended regions (128 pixels) **exactly touch** with no gap, because no pixel can write beyond 32 pixels from the chunk edge (32 + 32 = 64, the distance to the next same-group chunk).

### Your Implementation

**Current:**
- ✓ Velocity-based vertical movement: capped at 15 cells
- ✗ Liquid horizontal spreading: **up to 11 cells** (VIOLATES 2-cell gap)
- ✗ Gas horizontal spreading: **up to 4 cells** (VIOLATES 2-cell gap)

**What's Needed:**
- **Global movement constraint:** No cell can move more than 2 cells horizontally per frame
- OR increase buffer to 16+ cells to ensure gap ≥ max_spread
- OR use Noita's approach: buffer = chunk_size/2, no gap, hard movement limit

### Where This Happens

`Assets/Scripts/Simulation/Jobs/SimulateChunksJob.cs`:
- Line 219: `int spread = mat.dispersionRate + velocityBoost;`
- Liquid spreading in `FindSpreadDistance()` can return up to 11 cells
- Gas spreading hardcoded to 4 cells

These movements are **not constrained by MaxVelocity** (which only applies to velocity-based falling).

---

## Issue #2: Non-Atomic Cell Operations ⚠️ CRITICAL

### The Cell Struct

```csharp
[StructLayout(LayoutKind.Sequential)]
public struct Cell  // Size: 10 bytes (confirmed in Cell.cs:17)
{
    public byte materialId;      // 1 byte
    public byte flags;           // 1 byte
    public ushort frameUpdated;  // 2 bytes
    public sbyte velocityX;      // 1 byte
    public sbyte velocityY;      // 1 byte
    public byte temperature;     // 1 byte
    public byte structureId;     // 1 byte
    public ushort ownerId;       // 2 bytes
}
```

**Total: 10 bytes**

### Why This is a Problem

CPU guarantees atomic operations only for:
- 1, 2, 4, or 8-byte values
- Must be naturally aligned
- No guarantees for 10-byte structs

**Cell reads/writes are NOT atomic!**

### Race Scenario

```
Thread 1 (Chunk 0):                    Thread 2 (Chunk 2):
─────────────────────────────────────  ─────────────────────────────────────
MoveCell(46, y, 57, y):                SimulateCell(57, y):
  cells[57] = cell                       cell = cells[57]
  ↓ 10-byte write                        ↓ 10-byte read
  writes byte 0                          reads byte 0 (old value)
  writes byte 1                          reads byte 1 (old value)
  writes byte 2                          reads byte 2 (old value)
  writes byte 3                          reads byte 3 (old value)
  writes byte 4                          reads byte 4 (old value)
  writes byte 5                          reads byte 5 (NEW value) ← TORN!
  writes byte 6                          reads byte 6 (NEW value)
  writes byte 7                          reads byte 7 (NEW value)
  writes byte 8                          reads byte 8 (NEW value)
  writes byte 9                          reads byte 9 (NEW value)
```

**Result: TORN READ**
Thread 2 has a corrupted cell with mixed old/new data!

### Possible Outcomes

A torn read can produce:
- **Wrong materialId**: Water becomes stone, or air (bytes 0-1 corrupted)
- **Wrong velocity**: Particles suddenly accelerate, stop, or reverse (bytes 4-5 corrupted)
- **Wrong frameUpdated**: Cell gets double-processed or skipped (bytes 2-3 corrupted)
- **Wrong ownerId**: Cluster cells become free, or free cells join clusters (bytes 8-9 corrupted)
- **Wrong structureId**: Building cells detach or attach incorrectly (byte 7 corrupted)

**This explains erratic chunk boundary behavior!**

Cells appear to:
- Freeze (velocityX/Y become 0)
- Disappear (materialId becomes Air)
- Teleport (ownerId changes, picked up by wrong cluster)
- Change material (materialId corrupted)
- Get stuck (frameUpdated corrupted, skips processing)

### Memory Ordering Issues

Even without torn reads, there are memory ordering problems:
- CPU caches can reorder reads/writes
- Thread 1's writes might not be visible to Thread 2 immediately
- No memory barriers or volatiles ensure synchronization

C# `NativeArray<Cell>` in Burst does not guarantee memory ordering without explicit fences.

---

## Issue #3: frameUpdated Check is Not Synchronization ⚠️ HIGH

### The Current Approach

```csharp
void SimulateCell(int x, int y)
{
    Cell cell = cells[index];
    if (cell.frameUpdated == currentFrame)
        return;  // Already processed

    cell.frameUpdated = currentFrame;
    // ... simulate cell
    cells[index] = cell;
}
```

This prevents double-processing in **serial execution**.
In **parallel execution**, it's a race condition.

### The Race: Check-Then-Act (TOCTOU)

**Time Of Check, Time Of Use vulnerability:**

```
Thread 1:                              Thread 2:
─────────────────────────────────────  ─────────────────────────────────────
cell = cells[57]                       cell = cells[57]
frameUpdated = 100 (last frame)        frameUpdated = 100 (last frame)
currentFrame = 101                     currentFrame = 101
if (100 == 101)? NO                    if (100 == 101)? NO
  continue to simulate                   continue to simulate
cell.frameUpdated = 101                cell.frameUpdated = 101
... simulate cell ...                  ... simulate cell ...
cells[57] = cell ← WRITE               cells[57] = cell ← WRITE
```

**Both threads pass the check because they read BEFORE either writes!**

Result:
- Both threads simulate the same cell
- Both write back their results
- One thread's simulation is lost (last write wins)
- Cell behavior becomes unpredictable (lost updates)

### Even With Atomic Operations

Even if Cell reads/writes were atomic, `frameUpdated` alone is insufficient:
1. **Check-then-act is not atomic** - The check and the write are separate operations
2. **Two threads can both see frameUpdated != currentFrame simultaneously**
3. **Both proceed to simulate, both write back results**
4. **Lost write** - One thread's simulation is discarded

### What's Needed

Real synchronization requires:
- Atomic compare-and-swap: `if frameUpdated != currentFrame, set it to currentFrame atomically`
- Memory barriers to ensure visibility
- Lock-free algorithms (complex)

OR the Noita approach: **Guarantee that no two threads ever access the same cell** through sufficient buffer zones.

---

## Issue #4: Race Conditions in MarkDirtyInternal ⚠️ MEDIUM

### The Current Implementation

```csharp
void MarkDirtyInternal(int x, int y)
{
    int chunkIndex = ...;
    ChunkState chunk = chunks[chunkIndex];  // ← READ
    chunk.flags |= ChunkFlags.IsDirty;

    if (localX < chunk.minX) chunk.minX = (ushort)localX;
    if (localX > chunk.maxX) chunk.maxX = (ushort)localX;
    if (localY < chunk.minY) chunk.minY = (ushort)localY;
    if (localY > chunk.maxY) chunk.maxY = (ushort)localY;

    chunks[chunkIndex] = chunk;  // ← WRITE
}
```

### The Race: Read-Modify-Write

**Scenario:**

```
Chunk 0 moves cell to X=57 (in Chunk 1's territory):
  → Calls MarkDirtyInternal(57, y)
  → Updates chunks[1]

Chunk 1 processes a cell at X=50 (in its own territory):
  → Calls MarkDirtyInternal(50, y)
  → Updates chunks[1]

If these happen in parallel:
```

```
Thread 1 (Chunk 0):                    Thread 2 (Chunk 1):
─────────────────────────────────────  ─────────────────────────────────────
chunk = chunks[1]                      chunk = chunks[1]
  minX=10, maxX=45                       minX=10, maxX=45
chunk.minX = min(57, 10) = 10          chunk.minX = min(50, 10) = 10
chunk.maxX = max(57, 45) = 57          chunk.maxX = max(50, 45) = 50
chunks[1] = chunk (maxX=57) ← WRITE    chunks[1] = chunk (maxX=50) ← WRITE
                                       (overwrites Thread 1's update!)
```

**Result: Lost Update**
Thread 1's update (maxX=57) is overwritten by Thread 2's stale value (maxX=50).

### Why This Matters

You noted in comments: *"race conditions on min/max are acceptable - worst case is extra work"*

However:
1. The **entire ChunkState struct** is being read-modified-written, not just individual fields
2. Lost updates to `minX/maxX/minY/maxY` cause **incorrect dirty bounds**
3. Cells outside dirty bounds are skipped, leading to **missed simulations**
4. The `flags` field is also part of this race, potentially losing the `IsDirty` flag itself

### ChunkState Size

```csharp
public struct ChunkState
{
    public ushort minX, minY;      // 2+2 = 4 bytes
    public ushort maxX, maxY;      // 2+2 = 4 bytes
    public byte flags;             // 1 byte
    public byte activeLastFrame;   // 1 byte
    public ushort structureMask;   // 2 bytes
}
// Total: 12 bytes (likely padded to 16 for alignment)
```

ChunkState is **12-16 bytes**, which is NOT atomic on most systems.

### Impact on Chunk Boundaries

When a cell crosses a chunk boundary:
- Source chunk marks its own state
- Source chunk marks target chunk's state ← **Race with target chunk**
- Target chunk marks its own state ← **Race with source chunk**

If updates are lost, chunks don't wake up properly, causing cells to "stall" at boundaries.

---

## Issue #5: Iteration Order Doesn't Prevent Conflicts (Minor)

### Current Approach

Each chunk processes bottom-to-top, alternating X direction per row:
```csharp
for (int y = extMaxY - 1; y >= extMinY; y--)
{
    bool leftToRight = (y & 1) == 0;
    // Process X in alternating direction
}
```

This helps **visual quality** (no directional bias), but doesn't prevent races.

### Example Timeline

```
Y=100, row is even → left-to-right

Chunk 0 processes: X=0, 1, 2, ... 46
Chunk 2 processes: X=49, 50, 51, ... 110

Both process the same Y at roughly the same time.
```

If Chunk 0 moves a cell to X=57:
```
T0: Chunk 0 at X=46, Chunk 2 at X=49
T1: Chunk 0 processes X=46, starts moving water to X=57
T2: Chunk 0 writes cells[57]
T3: Chunk 2 reaches X=57 in iteration
T4: Chunk 2 reads cells[57]
```

Even with "correct" timing (T4 after T2), problems occur:
1. **Non-atomic write/read** - T2 and T4 can overlap, causing torn reads
2. **Memory ordering** - Chunk 2's cache might not see Chunk 0's write
3. **Double-processing** - If T4 reads before T2 completes, frameUpdated check fails

The iteration order is good for visual quality, but provides **no thread safety guarantees**.

---

## Root Cause Summary

Your chunk boundary stalls are caused by:

### 1. Insufficient Buffer (CRITICAL)
- **Liquid spreading:** up to 11 cells from buffer edge → crosses into same-group chunk's region
- **Gas spreading:** up to 4 cells from buffer edge → crosses into same-group chunk's region
- **Result:** Same-group chunks write to overlapping cells → **data races on cells[]**

### 2. Non-Atomic Cell Operations (CRITICAL)
- **Cell struct:** 10 bytes, not atomically read/written
- **Result:** Torn reads create corrupted cells with wrong materials, velocities, flags
- **Observable:** Cells freeze, disappear, teleport, change material at boundaries

### 3. Inadequate Synchronization (HIGH)
- **frameUpdated check:** Not atomic, both threads can pass simultaneously
- **Result:** Double-processing and lost updates
- **Observable:** Unpredictable cell behavior, "glitchy" physics

### 4. Chunk State Races (MEDIUM)
- **MarkDirtyInternal:** Read-modify-write race on chunks[]
- **Result:** Lost dirty bounds, missed simulations
- **Observable:** Chunks don't wake up properly, cells stall at edges

The stalling behavior is primarily caused by **torn reads/writes** creating corrupted cells that appear to freeze, vanish, or malfunction.

---

## Comparison to Noita's Approach

### Noita's Guarantees

| Aspect | Implementation |
|--------|----------------|
| **Movement constraint** | Hard limit: 32 pixels for ALL movement types |
| **Buffer size** | 32 pixels (50% of chunk size) |
| **Same-group spacing** | 128 pixels (2× chunk size) |
| **Extended regions** | Exactly touch, no gap needed |
| **Thread safety** | Spatial isolation - no cell accessed by >1 thread |
| **Synchronization** | Not needed - guaranteed no overlap |

### Your Implementation

| Aspect | Implementation | Issue |
|--------|----------------|-------|
| **Movement constraint** | 15 cells for velocity only | ✗ Spreading exceeds limit |
| **Buffer size** | 15 cells (47% of chunk size) | ✗ Too small for spreading |
| **Same-group spacing** | 64 cells (2× chunk size) | ✓ Correct |
| **Extended regions** | 2-cell gap | ✗ Gap violated by spreading |
| **Thread safety** | Relies on frameUpdated check | ✗ Non-atomic, insufficient |
| **Synchronization** | None | ✗ Cell/ChunkState races |

---

## Recommendations

To fix the chunk boundary issues, you need to:

### Option 1: Enforce Global Movement Limit (Noita's Approach)
1. **Cap all movement** to ≤ 2 cells per frame (to respect the 2-cell gap)
2. Clamp `FindSpreadDistance()` results to max 2 cells
3. Reduce gas spread from 4 to 2 cells
4. This ensures no cell can write beyond the gap

**Pros:** Minimal code changes, keeps buffer size
**Cons:** Significantly slower liquid/gas spreading (gameplay impact)

### Option 2: Increase Buffer Size
1. Increase BufferSize to 16+ cells
2. Increase MaxVelocity to 16 to match
3. Ensures gap ≥ 11 cells (max liquid spread)
4. Extended regions: 64 cells (32 + 16 + 16), gap = 0 (regions touch)

**Pros:** Keeps existing movement speeds
**Cons:** More cells processed per chunk, higher overhead, still has race conditions

### Option 3: Adopt Noita's Exact Approach
1. Increase ChunkSize to 64 cells
2. Set BufferSize to 32 cells (50% of chunk)
3. Enforce hard limit: no movement > 32 cells per frame
4. Extended regions touch exactly (64 + 32 + 32 = 128, next chunk at 128)
5. Scale up MaxVelocity to 32

**Pros:** Proven approach, perfect spatial isolation
**Cons:** Larger chunks = fewer chunks, coarser parallelism, more code changes

### Option 4: Use Buffered Cross-Chunk Moves (Advanced)
1. Detect when MoveCell() crosses into another chunk's extended region
2. Buffer these moves instead of applying immediately
3. After all parallel processing, commit buffered moves in a sync step
4. Requires conflict resolution (multiple cells → same target)

**Pros:** Keeps current buffer/chunk sizes, allows fast spreading
**Cons:** Complex implementation, sync step overhead, conflicts need resolution

### All Options Still Need:
- **Atomic cell operations** - Use separate arrays or double-buffering
- **Atomic chunk state updates** - Use atomic operations or per-chunk locks
- **Memory barriers** - Ensure visibility across threads

---

## References

- `Assets/Scripts/Simulation/CellWorld.cs` - Chunk management, ChunkSize=32, EdgeThreshold=2
- `Assets/Scripts/Simulation/PhysicsSettings.cs` - MaxVelocity=15, BufferSize (implicitly 15)
- `Assets/Scripts/Simulation/Jobs/SimulateChunksJob.cs` - Cell simulation, movement logic
- `Assets/Scripts/Simulation/Cell.cs` - Cell struct, 10 bytes
- `Assets/Scripts/Simulation/ChunkState.cs` - ChunkState struct, 12 bytes

---

## Next Steps

1. **Decide on approach** - Which option above fits your gameplay/performance needs?
2. **Profile current issues** - Measure frequency of boundary stalls to validate fixes
3. **Implement fix** - Start with movement constraint (easiest) or buffer increase
4. **Add atomics** - Fix Cell/ChunkState race conditions
5. **Test thoroughly** - High particle density near chunk boundaries
6. **Document constraints** - Ensure future materials respect movement limits
