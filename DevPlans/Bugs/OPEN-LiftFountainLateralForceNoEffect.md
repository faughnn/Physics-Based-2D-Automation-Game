# Bug: Lift Fountain Lateral Force Has No Effect

## Summary
The lift exit lateral force (fountain effect) correctly accumulates `velocityX` via `velocityFracX` on the exit row, but `velocityX` is never consumed during free-fall. Material exits the lift moving purely vertically, and `velocityX` is either ignored or zeroed before it ever produces horizontal displacement.

## Symptoms
- Material exits the top of a lift in a straight vertical column — no fountain spray
- Increasing `LiftExitLateralForce` (even to extreme values) has no visible effect
- The lateral velocity is set correctly but discarded before use

## Root Cause
**There is no mechanism for horizontal displacement during free-fall.** The powder simulation is structured as sequential phases:

1. **Phase 1 (vertical):** Computes `targetY = y + velocityY`, traces a vertical path, moves to `(x, targetY)`, and **returns immediately**. `velocityX` is carried in cell data but `x` never changes.
2. **Phase 2 (diagonal):** Uses `velocityX` for diagonal traces, but **only runs when Phase 1 fails** (cell can't move vertically at all). During free-fall, Phase 1 always succeeds.
3. **Phase 3 (stuck):** Hard-zeros `velocityX` at two points — slide resistance check and final fallback.

For liquid, the same issue applies: `TryFall`/`TryRise` move purely vertically and return on success. The horizontal spread code only runs when vertical movement fails entirely.

Compare with `velocityY`: it directly determines the target position in Phase 1 (`targetY = y + velocityY`). There is no equivalent for `velocityX` — no code computes a combined `(targetX, targetY)` during movement.

## Affected Code
- `Assets/Scripts/Simulation/Jobs/SimulateChunksJob.cs` — `SimulatePowder()`
  - Line ~242: Phase 1 falling `MoveCell(x, y, x, targetY, cell)` — constant `x`, ignores `velocityX`
  - Line ~259: Phase 1 rising — same issue
  - Line ~345: Phase 3 slide resistance — `velocityX = 0`
  - Line ~373: Phase 3 stuck — `velocityX = 0`
- `Assets/Scripts/Simulation/Jobs/SimulateChunksJob.cs` — `SimulateLiquid()`
  - `TryFall()` / `TryRise()` — purely vertical movement, ignores `velocityX`

## Potential Solutions

### 1. Horizontal pass after vertical movement
After Phase 1 moves the cell vertically, apply `velocityX` as a separate horizontal displacement step in the same frame. This would be a new "Phase 1.5" that traces a horizontal path from the cell's new position. Simple and localized change — doesn't alter existing diagonal logic.

### 2. Combined trajectory in Phase 1
Compute target as `(x + velocityX, y + velocityY)` and trace a Bresenham-style line to it. More physically accurate (true projectile arc) but more invasive to the existing vertical trace logic.

### 3. Allow Phase 2 diagonal to run after successful Phase 1
Remove the early `return` from Phase 1 and let Phase 2 run as a follow-up. Would need Phase 2 to support upward/neutral diagonal traces (currently only traces downward). Riskier — changes fundamental simulation flow for all powder, not just lift exits.

## Priority
Medium

## Related Files
- `Assets/Scripts/Simulation/PhysicsSettings.cs` — `LiftExitLateralForce` constant
- `Assets/Scripts/Simulation/CellSimulatorJobbed.cs` — passes `liftExitLateralForce` to job
