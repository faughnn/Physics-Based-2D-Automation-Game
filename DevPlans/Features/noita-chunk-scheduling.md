# Noita-Style Chunk Scheduling for Unity Jobs

## Overview

Single buffer, 4-phase checkerboard scheduling. All four phases execute within one simulation frame. Parallelism is within each phase, not across phases.

## Chunk Layout

World is divided into 64×64 pixel chunks. Each chunk is assigned to one of four phases (A, B, C, D) in a checkerboard pattern:

```
+---+---+---+---+
| A | B | A | B |
+---+---+---+---+
| C | D | C | D |
+---+---+---+---+
| A | B | A | B |
+---+---+---+---+
| C | D | C | D |
+---+---+---+---+
```

Same-phase chunks are always 128px apart in both axes (one full chunk gap).

## Processing vs Read/Write Regions

Two distinct regions per chunk:

**Processing region (64×64):** Only pixels within the home chunk get their movement rules evaluated. You iterate through these pixels.

**Read/write region (128×128):** The home chunk plus 32px buffer on all sides. When a home pixel moves, it can land anywhere in this region - including into neighbouring chunks' home areas.

```
+---------------------------+
|                           |
|   buffer: read/write OK   |
|   but do NOT process      |
|       +-----------+       |
|       |           |       |
|       |  64×64    |       |
|       |  home:    |       |
|       |  PROCESS  |       |
|       |  these    |       |
|       +-----------+       |
|                           |
|                           |
+---------------------------+

Total read/write region: 128×128 pixels (32 + 64 + 32)
```

**Example:** A pixel at (63, 32) in chunk A falls diagonally right to (64, 33). Position (64, 33) is inside chunk B's home region, but chunk A can write there because it's within A's buffer. Chunk B will process that pixel in a later phase.

This is safe because same-phase chunks are 128px apart. Their read/write regions touch at the edges but never overlap.

**Velocity cap:** Pixels cannot move more than 16 pixels per phase. This ensures they stay within the read/write region.

## Frame Execution Order

One simulation frame:

```
1. Schedule all A chunks (parallel)
2. Wait for all A chunks to complete
3. Schedule all B chunks (parallel)
4. Wait for all B chunks to complete
5. Schedule all C chunks (parallel)
6. Wait for all C chunks to complete
7. Schedule all D chunks (parallel)
8. Wait for all D chunks to complete
9. Frame complete
```

Every pixel in the world is evaluated exactly once per frame.

## Unity Jobs Implementation

**Critical: JobHandle chaining**

Each phase must take the previous phase's handle as its dependency:

```csharp
JobHandle RunSimulationFrame()
{
    JobHandle phaseA = SchedulePhase(PhaseType.A, default);
    JobHandle phaseB = SchedulePhase(PhaseType.B, phaseA);
    JobHandle phaseC = SchedulePhase(PhaseType.C, phaseB);
    JobHandle phaseD = SchedulePhase(PhaseType.D, phaseC);
    
    return phaseD;
}

JobHandle SchedulePhase(PhaseType phase, JobHandle dependency)
{
    var job = new ChunkUpdateJob 
    { 
        pixels = pixelBuffer,
        chunkIndices = GetChunkIndicesForPhase(phase),
        // other data...
    };
    
    return job.Schedule(chunkCountForPhase, 1, dependency);
}
```

**Common mistake - missing dependencies:**

```csharp
// WRONG - all phases run simultaneously, causes race conditions
jobA.Schedule(chunksA, 1, default);
jobB.Schedule(chunksB, 1, default);
jobC.Schedule(chunksC, 1, default);
jobD.Schedule(chunksD, 1, default);
```

**Required attribute for single-buffer writes:**

```csharp
[NativeDisableContainerSafetyRestriction]
public NativeArray<PixelData> pixels;
```

This tells Unity you've manually ensured thread safety (via the checkerboard pattern).

## Pixel Iteration Within a Chunk

**Vertical: bottom-to-top** (highest Y to lowest Y). This prevents a pixel being processed multiple times in one pass - when it falls down, the destination row has already been processed.

**Horizontal: alternate per row** to prevent directional bias. Even rows iterate left-to-right, odd rows iterate right-to-left.

```csharp
for (int y = maxY; y >= minY; y--)  // bottom to top
{
    if (y % 2 == 0)
    {
        for (int x = minX; x <= maxX; x++)  // left to right
            ProcessPixel(x, y);
    }
    else
    {
        for (int x = maxX; x >= minX; x--)  // right to left
            ProcessPixel(x, y);
    }
}
```

Without alternation, two pixels competing for the same diagonal cell will consistently favour one direction, causing sand piles to visibly lean.

## Dirty Rect Optimisation (Optional but Recommended)

Each chunk tracks a rectangle of "active" pixels. Only iterate within the dirty rect, not the full 64×64. When a pixel moves, expand the dirty rect to include its new position.

Static chunks (no movement for N frames) can skip processing entirely.

## Velocity and Gravity

**Apply gravity every frame, not every N frames.** Applying gravity every 15 frames causes visible stuttering where all pixels jump in unison.

**Use fractional accumulation** so pixels accelerate smoothly:

```
Frame 1:  vel=0.1,  move 0
Frame 10: vel=1.0,  move 1
Frame 11: vel=1.1,  move 1
Frame 20: vel=2.0,  move 2
Frame 30: vel=3.0,  move 3
```

After initial acceleration, pixels move every frame with increasing distance - natural falling motion.

**Implementation options (depends on current pixel struct):**

1. **Two bytes per axis:** One signed byte for integer velocity (-32 to +32), one byte for fractional accumulator (0-255). Overflow triggers velocity increment.

2. **Separate velocity array:** Keep pixel struct as bytes, store velocity in parallel `NativeArray<float2>` with same indices.

3. **Expand struct:** Use shorts or floats directly if memory isn't a constraint.

**TODO:** Review current pixel struct and bitwise operations before deciding approach.

**Stepping through path on movement:**

When a pixel has velocity > 1, don't teleport directly. Step through each cell checking for collisions:

```csharp
int targetDistance = Mathf.Min((int)velocity.y, 16);  // clamp to 16
int actualDistance = 0;

for (int i = 1; i <= targetDistance; i++)
{
    if (IsEmpty(x, y + i))
        actualDistance = i;
    else
        break;  // hit something
}

// Move pixel by actualDistance, update velocity on collision
```

This prevents fast pixels tunnelling through thin surfaces.

## Debugging with Unity Profiler

**Window → Analysis → Profiler**

Use the **Timeline view** (CPU → Timeline) to visualise job execution. You can see:

- Each job as a horizontal bar
- When it starts/ends
- Which thread it ran on
- Whether phases are overlapping (they shouldn't be)

If your A/B/C/D phases are running simultaneously instead of sequentially, you'll see it immediately - the job bars will overlap instead of running one after another.

Also useful for finding actual bottlenecks. Don't assume - measure. The problem might be:
- Your chunk jobs (simulation logic)
- `Texture2D.Apply()` (uploading to GPU)
- Memory allocations
- Something else entirely

Get it working correctly first, then profile and optimise what the data shows you.

## Summary Checklist

- [ ] 64×64 chunks in A/B/C/D checkerboard pattern
- [ ] 32px read/write buffer on all sides (128×128 total region per chunk)
- [ ] Only process pixels in home 64×64, buffer is for read/write only
- [ ] Max pixel velocity capped at 16px per phase
- [ ] 4 phases per frame, executed sequentially
- [ ] JobHandle from phase N passed as dependency to phase N+1
- [ ] Bottom-to-top pixel iteration within chunks
- [ ] Horizontal direction alternates per row (even=left-to-right, odd=right-to-left)
- [ ] `[NativeDisableContainerSafetyRestriction]` on pixel buffer
- [ ] Gravity applied every frame with fractional accumulation
- [ ] Step through path cell-by-cell when velocity > 1 (no teleporting)
