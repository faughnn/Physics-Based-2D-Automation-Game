# Bug: Massive FPS Drop When First Belt or Lift Is Placed

**Status:** OPEN
**Reported:** 2026-01-28

## Description

The moment the first belt or lift structure is placed, FPS drops dramatically — from ~400 FPS down to ~70 FPS. This is a one-time cliff, not a gradual degradation. Placing additional structures does not cause a proportional further drop.

## Steps to Reproduce

1. Start the game with no structures placed (observe ~400 FPS)
2. Place a single belt or lift
3. FPS immediately drops to ~70

## Expected Behavior

Placing a single small structure should have negligible performance impact.

## Actual Behavior

FPS drops from ~400 to ~70 the instant the first structure is placed — roughly an 80% reduction.

## Possible Causes

- Structure placement may activate an expensive system that was previously idle (belt simulation, lift simulation)
- HasStructure chunk flag may be waking up large numbers of chunks that were previously inactive
- Terrain collider rebuild may be triggering expensive recalculation across the entire world
- A per-frame system may start running unconditionally once any structure exists (e.g., iterating all chunks looking for structures)

## Severity

High — 80% FPS loss from a single structure placement is a critical performance regression. Structures are a core mechanic.
