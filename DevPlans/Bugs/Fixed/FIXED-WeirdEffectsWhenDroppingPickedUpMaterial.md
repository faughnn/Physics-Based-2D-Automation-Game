# Bug: Dropping Picked-Up Material Causes Weird Effects

**Status:** OPEN
**Reported:** 2026-01-28

## Description

After picking up material using the cell grab system and then dropping it, the dropped material behaves unexpectedly. The exact visual or physical effects are unclear but something is clearly wrong with how the material re-enters the simulation after being held.

## Steps to Reproduce

1. Pick up material using the grab system
2. Drop the material
3. Observe unexpected behavior from the dropped cells

## Expected Behavior

Dropped material should re-enter the simulation cleanly and behave as normal cells (fall with gravity, interact with terrain, etc.).

## Actual Behavior

Dropped material exhibits weird/unexpected effects upon release.

## Possible Causes

- Cells may be re-inserted with incorrect velocity, position, or material state
- Grabbed cells may retain stale simulation state (e.g., old velocity, wrong flags) when re-placed
- Cells may be placed at incorrect coordinates or overlap with existing terrain
- Chunk dirty flags may not be properly set for the drop location

## Severity

Medium — affects a core interaction mechanic. Picking up and dropping material should feel predictable.
