# Per-Material Attributes for Powder and Liquid

## Goal

Define distinct physical attributes for each powder and liquid material type, and make the simulation consume them. Each material should feel and behave differently rather than sharing identical movement logic differentiated only by density.

## Current State

- All powders share the same hard-coded physics: gravity, friction (`* 7 / 8`), momentum transfer (`70%`), spread boost (`velocityY / 3`), landing velocity (`+/-4`)
- All liquids share the same spread distance, rise behavior, and flow characteristics
- The only per-material differentiators are `density`, `slideResistance`, and `dispersionRate`
- All other movement characteristics are identical for every powder and every liquid
- `MaterialDef` has flags (Flammable, ConductsHeat, etc.) but no continuous physical attributes beyond those three

## Symptoms

- All powders fall at exactly the same rate regardless of weight
- All powders and liquids have identical friction/damping (87.5% retention)
- All powders transfer momentum on collision at the same 70% ratio
- All liquids get the same velocity-to-spread boost formula (`velocityY / 3`)
- All liquids get the same landing horizontal velocity (+/-4)
- All gases spread at the same fixed distance (4)
- No way to create a "heavy sand" that falls faster, a "sticky mud" with high friction, or a "viscous oil" that spreads slowly

## Hard-Coded Constants That Should Be Per-Material

| Constant | Location | Value | Should Be |
|----------|----------|-------|-----------|
| `fractionalGravity` | `SimulatePowder` / `SimulateLiquid` | `17` | Per-material gravity |
| Friction retention | `SimulatePowder` Phase 2 | `* 7 / 8` (87.5%) | Per-material friction |
| Momentum transfer | `SimulatePowder` Phase 1 collision | `70%` | Per-material restitution |
| Spread boost | `SimulateLiquid` | `velocityY / 3` | Per-material restitution |
| Landing velocity | `SimulateLiquid` | `+/-4` | Per-material restitution |
| Gas spread distance | `SimulateGas` | `4` | Should read existing `dispersionRate` (currently ignored for gas) |

## Proposed New Attributes

Three new fields on `MaterialDef`, all bytes, all integer math (Burst-compatible):

| Attribute | Range | 0 means | 255 means | Replaces |
|-----------|-------|---------|-----------|----------|
| `gravity` | 0-255 | No gravity | Max gravity | Hard-coded `fractionalGravity = 17` |
| `friction` | 0-255 | Frictionless | Instant stop | Hard-coded `* 7 / 8` retention |
| `restitution` | 0-255 | Absorbs all energy | Perfectly elastic | Hard-coded 70% momentum transfer, liquid splash velocity |

Existing attributes remain:

| Attribute | What it does |
|-----------|-------------|
| `density` | Displacement ordering in `CanMoveTo()` |
| `slideResistance` | Powder piling angle (probability of refusing diagonal slide) |
| `spread` | Horizontal spread distance (rename from `dispersionRate`; wire up for gas too) |

### Rename: `dispersionRate` → `spread`

Simpler name, says exactly what it is — how far the material spreads horizontally. Also fix `SimulateGas` to actually read this field instead of using hardcoded `4`.

## Suggested Values

| Material | gravity | friction | restitution | density | slideResistance | spread |
|----------|---------|----------|-------------|---------|-----------------|--------|
| Sand | 17 | 32 | 179 | 128 | 0 | 0 |
| Dirt | 22 | 96 | 80 | 140 | 50 | 0 |
| Water | 17 | 16 | 200 | 64 | 5 | 5 |
| Oil | 13 | 56 | 140 | 48 | 15 | 4 |
| Steam | 10 | 56 | 100 | 4 | 2 | 4 |

*Values are starting points — will need playtesting and tuning.*

## Dependencies

- **Two-Dimensional Material Movement** should be implemented first — it fixes the movement architecture (horizontal velocity during free-fall, symmetric collision response). Per-material attributes feed into that architecture. See `TwoDimensionalMaterialMovement.md`.
- Design the 2D movement code with parameterization points for these attributes, even if initially hardcoded.

## Work Required

1. Add `gravity`, `friction`, `restitution` fields to `MaterialDef`
2. Rename `dispersionRate` → `spread`
3. Define values for each existing material
4. Update `SimulateChunksJob` to read per-material values instead of hard-coded constants
5. Fix `SimulateGas` to read `spread` instead of hardcoded `4`
6. Playtest and tune until each material feels distinct

## Affected Code

- `Assets/Scripts/Simulation/MaterialDef.cs` — struct definition, new fields + rename
- `Assets/Scripts/Simulation/Jobs/SimulateChunksJob.cs` — replace hard-coded constants with material reads
- `Assets/Scripts/Simulation/Materials` static class — set values for all material definitions

## Priority

Medium
