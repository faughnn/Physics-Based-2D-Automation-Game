# Bug: Material Falls Back Into Lift

## Summary

Material exiting the top of a lift has no horizontal dispersal mechanism. Rising cells that hit a solid above the lift get their velocity zeroed and fall back down, creating a tight oscillation loop where material endlessly bounces between the lift top and the blockage above. This causes material to clump at lift exits instead of spreading sideways.

## Symptoms

- Powder/liquid piles up at the top of lifts instead of dispersing
- Material visibly oscillates — rising then falling back into the lift
- Lift exits become clogged, reducing throughput
- Building walls or structures above a lift makes the problem worse

## Root Cause

The collision response in `SimulatePowder` is **downward-biased**. When a falling cell hits a solid, its vertical momentum is converted into diagonal movement (lines 235-268). But this code only activates for `velocityY > 1` (downward). Rising material (`velocityY < 0`) that hits a ceiling gets **no horizontal momentum injection** at all.

The cascade:

1. **No upward collision dispersal** — `if (collided && cell.velocityY > 1)` at line 236 excludes rising cells entirely. There is no equivalent `velocityY < -1` block.
2. **Phase 3 slide only checks downward diagonals** — `CanMoveTo(x ± 1, y + 1)` at lines 329-337. No lateral (`y + 0`) or upward-diagonal (`y - 1`) slide exists.
3. **Velocity fully zeroed when stuck** — lines 341-344 zero both `velocityX` and `velocityY`, erasing all momentum.
4. **Cell re-enters lift zone** — with zero velocity, the cell is back in the lift, lift force re-applies, it rises again, hits the same blockage, repeats.

## Affected Code

- `Assets/Scripts/Simulation/Jobs/SimulateChunksJob.cs:236` — Diagonal momentum transfer condition (`velocityY > 1`, excludes rising cells)
- `Assets/Scripts/Simulation/Jobs/SimulateChunksJob.cs:215-233` — Rising path trace (sets `collided` but response is missing)
- `Assets/Scripts/Simulation/Jobs/SimulateChunksJob.cs:329-337` — Phase 3 slide (only checks downward diagonals)
- `Assets/Scripts/Simulation/Jobs/SimulateChunksJob.cs:341-344` — Velocity zeroing when stuck
- `Assets/Scripts/Simulation/Jobs/SimulateChunksJob.cs:353` — Liquid `wasFreeFalling` only checks positive velocity (`velocityY > 2`)

## Potential Solutions

### 1. Add upward collision dispersal (symmetric collision response)

Add a mirror of the downward collision response for `velocityY < -1`. When rising material hits a ceiling, convert upward momentum into horizontal dispersal — set `velocityX` to a lateral value and `velocityY` near zero. This follows the existing pattern and makes collision response symmetric rather than downward-only.

Also add lateral (`y + 0`) and upward-diagonal (`y - 1`) probes to the Phase 3 slide for cells with negative velocity.

### 2. Lift exit force

Make lifts impart lateral velocity at their top cell. The lift system already tracks tile positions — the top row could apply a configurable horizontal force (left, right, or split both ways) to exiting material. This is a targeted solution for the lift use case specifically but doesn't fix the general problem of rising material having no dispersal.

### 3. Both

Solution 1 fixes the systemic gap in the physics. Solution 2 gives players control over where lift output goes. They complement each other.

## Priority

Medium

## Related Files

- `Assets/Scripts/Simulation/Jobs/SimulateChunksJob.cs`
- `Assets/Scripts/Simulation/PhysicsSettings.cs`
- `Assets/Scripts/Simulation/Structures/LiftManager.cs`
- `DevPlans/Bugs/Backlog/OPEN-MaterialClumpsAtTopOfLifts.md` (related backlog bug)
- `DevPlans/Features/FirstLevel/18-WedgeTilePlacement.md` (originally proposed wedge tiles to solve this)
