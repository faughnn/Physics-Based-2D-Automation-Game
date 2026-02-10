# Two-Dimensional Material Movement

## Goal

Fix material movement so that horizontal velocity is properly consumed during free-fall and rising, enabling true 2D trajectories. Materials should arc, spread, and flow in both dimensions simultaneously rather than moving in purely vertical columns.

## Current State

- Phase 1 (vertical movement) moves cells straight up or down, ignoring `velocityX` entirely
- Phase 2 (diagonal) only runs when Phase 1 fails completely — during free-fall it never gets a chance
- Lift exit lateral force accumulates `velocityX` via `velocityFracX` but this is never consumed during flight
- The result: material launched from lifts goes straight up, material dropped from height falls straight down, no arcing or lateral spread during movement
- This also makes lift-top clogging worse since material has no way to disperse horizontally while airborne

## Current Code Structure (SimulateChunksJob.cs)

The existing movement is a 3-phase fallback chain built for a simple falling-sand sim with no upward or horizontal forces:

1. **Phase 1 — Vertical**: Ray-march from current position by `velocityY` cells. If any vertical movement succeeds, **return immediately** (line 243). `velocityX` is never touched.
2. **Phase 2 — Diagonal**: Only runs when Phase 1 is fully blocked. Collision response converts ~50% of vertical velocity into diagonal. Traces a 45-degree path using `velocityX`.
3. **Phase 3 — Simple slide**: Single-step down-left/down-right with `slideResistance` check. Last resort.

The early return in Phase 1 is the root cause. Any time a cell can fall even 1 cell, it does so and skips all horizontal processing.

## New Design: Unified Movement

Replace the 3-phase fallback with a single unified step:

1. **Apply forces** — gravity, lift, belt push → update velocity
2. **Trace movement** — single ray-march along the full velocity vector `(velocityX, velocityY)`, consuming both axes together. Like a Bresenham line from current position to `(x + velocityX, y + velocityY)`. Handle collisions with restitution along the way.
3. **At-rest behavior** — if velocity is zero: powder checks stability/topple, liquid checks spread

This means a cell launched from a lift with lateral force will actually arc — moving up and sideways simultaneously, tracing a real trajectory.

## Hard Prerequisite: Fix frameUpdated at Chunk Edges

### Why frameUpdated is needed

Cells can move in any direction. The scan order is bottom-to-top with alternating left/right per row. A cell that moves **into a not-yet-scanned position** will be encountered again by the scan and simulated twice in one frame. `frameUpdated` prevents this — cells marked as already-moved this frame get skipped.

### Which directions need frameUpdated

- **Moving down**: Safe. Scan moves upward, away from the destination. Cell won't be reached again.
- **Moving up**: Unsafe. Scan is heading toward the destination. Needs `frameUpdated`.
- **Moving in scan direction horizontally**: Unsafe. Scan will reach the destination in the same row. Needs `frameUpdated`.
- **Moving against scan direction horizontally**: Safe. Scan already passed that column.

### Why alternating left/right is needed

Without alternation, there's a directional bias. Not in double-processing (frameUpdated handles that), but in **environment response**. Cells around a destination that haven't been scanned yet will react to the arrival this frame. Cells that were already scanned won't react until next frame. With a fixed left-to-right scan, the environment always responds faster to rightward movement — visible as asymmetric spreading, lopsided explosions, etc.

Alternating left/right per row averages out the bias. The equivalent vertical bias (environment responds faster to downward movement) exists but is acceptable because gravity makes downward movement dominant anyway.

### The chunk-edge bug

`frameUpdated` currently has a known bug at chunk boundaries. Cells falling downward across chunk edges stutter — they pause for a frame at every chunk boundary. The exact cause was never determined. Currently `frameUpdated` is only used for upward movement to avoid triggering this bug.

**This bug must be investigated and fixed before unified 2D movement can be implemented.** Without reliable `frameUpdated`, we can't have alternating scan direction, which means horizontal bias, which means visibly broken simulation.

### Investigation approach

- Look at how `frameUpdated` is set in `MoveCell()` and checked in `SimulateCell()`
- Check if the issue is related to cells crossing from one chunk group to another within the same frame
- Check if the frame counter comparison has edge cases at chunk boundaries
- Test with downward-moving cells and `frameUpdated` enabled to reproduce the stutter
- The 4-pass group system means adjacent chunks are processed in different passes — a cell moving from group A's chunk into group C's chunk might have timing issues with the frame counter

## Work Order

1. **Investigate and fix frameUpdated chunk-edge stutter** — prerequisite for everything else
2. **Enable frameUpdated for all movement directions** — once the bug is fixed
3. **Replace 3-phase movement with unified trace** — single Bresenham-style ray-march consuming both velocityX and velocityY
4. **Implement collision response with restitution** — percentage-based velocity retention on impact, any direction
5. **Implement at-rest behaviors** — powder stability/topple, liquid spread (separate from movement trace)
6. **Integrate per-material attributes** — density, restitution, stability, spread control the unified movement

## Dependencies

- **Per-Material Attributes** (`PowderAndLiquidAttributeDesign.md`) — attribute design is mostly complete (powder locked, liquid locked, gas not yet designed). Implementation happens after 2D movement works.
- Fixes the root cause of several open bugs: lift fountain lateral force, material clumping at lift tops, asymmetric collision response
