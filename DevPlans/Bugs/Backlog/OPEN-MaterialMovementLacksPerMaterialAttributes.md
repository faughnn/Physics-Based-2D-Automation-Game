# Bug: Material Movement Lacks Per-Material Attributes

## Summary
Powder and liquid simulation share hard-coded physics constants across all materials of the same `BehaviourType`. Only three attributes (`density`, `slideResistance`, `dispersionRate`) actually differentiate materials. All other movement characteristics — gravity rate, friction, momentum transfer, damping — are identical for every powder and every liquid.

## Symptoms
- All powders fall at exactly the same rate regardless of weight
- All powders and liquids have identical friction/damping (87.5% retention)
- All powders transfer momentum on collision at the same 70% ratio
- All liquids get the same velocity-to-spread boost formula (`velocityY / 3`)
- All liquids get the same landing horizontal velocity (+/-4)
- All gases spread at the same fixed distance (4)
- No way to create a "heavy sand" that falls faster, a "sticky mud" with high friction, or a "viscous oil" that spreads slowly

## Root Cause
The simulation jobs in `SimulateChunksJob.cs` use hard-coded constants where they should be reading per-material fields from `MaterialDef`. The `MaterialDef` struct has room for more fields but they were never added or wired in.

### Hard-coded constants that should be per-material:

| Constant | Location | Value | Should Be |
|----------|----------|-------|-----------|
| `fractionalGravity` | `SimulatePowder` / `SimulateLiquid` | `17` | Per-material weight/gravity multiplier |
| Friction retention | `SimulatePowder` Phase 2 | `* 7 / 8` (87.5%) | Per-material friction coefficient |
| Momentum transfer | `SimulatePowder` Phase 1 collision | `70%` | Per-material elasticity/inertia |
| Spread boost | `SimulateLiquid` | `velocityY / 3` | Per-material viscosity |
| Landing velocity | `SimulateLiquid` | `+/-4` | Per-material splash/energy |
| Gas spread distance | `SimulateGas` | `4` | Per-material dispersion |

### Currently used attributes (only 3):
- `density` — displacement ordering in `CanMoveTo()`
- `slideResistance` — powder piling angle in Phase 3
- `dispersionRate` — liquid base horizontal spread

## Affected Code
- `Assets/Scripts/Simulation/Jobs/SimulateChunksJob.cs:192` — powder gravity (fractionalGravity = 17)
- `Assets/Scripts/Simulation/Jobs/SimulateChunksJob.cs:282` — powder friction (* 7 / 8)
- `Assets/Scripts/Simulation/Jobs/SimulateChunksJob.cs:236` — powder momentum transfer (70%)
- `Assets/Scripts/Simulation/Jobs/SimulateChunksJob.cs:401` — liquid spread boost (velocityY / 3)
- `Assets/Scripts/Simulation/Jobs/SimulateChunksJob.cs:410` — liquid landing velocity (+/-4)
- `Assets/Scripts/Simulation/Jobs/SimulateChunksJob.cs:554` — gas spread distance (4)
- `Assets/Scripts/Simulation/MaterialDef.cs` — struct definition, needs new fields

## Potential Solutions

### 1. Add Per-Material Physics Fields to MaterialDef
Add new fields to `MaterialDef`:
- `gravityScale` (byte) — fractional gravity accumulation rate (current default: 17)
- `friction` (byte) — velocity retention per frame (0 = instant stop, 255 = no friction; current effective value: ~224)
- `inertia` (byte) — momentum transfer ratio on collision (current effective value: ~179 for 70%)
- `viscosity` (byte) — liquid only: velocity-to-spread damping (lower = more viscous)
- `gasDispersion` (byte) — gas only: spread distance per frame

Then replace hard-coded constants in `SimulateChunksJob.cs` with reads from the material definitions array.

### 2. Keep It Simple — Only Add What's Needed Now
Start with just the highest-impact attributes:
- `gravityScale` — lets heavy materials fall faster than light ones
- `friction` — lets sticky materials slow down faster

Add others later as specific materials need them. Avoids over-engineering unused fields.

## Priority
Medium

## Related Files
- `Assets/Scripts/Simulation/MaterialDef.cs`
- `Assets/Scripts/Simulation/Jobs/SimulateChunksJob.cs`
