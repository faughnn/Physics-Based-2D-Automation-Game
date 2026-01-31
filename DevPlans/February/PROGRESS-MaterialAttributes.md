# Material Attributes — Current Progress

## Status: In Progress — Powder attributes mostly locked, one open question remaining

## What's Done

- Decided to design attributes per behavior type (powder, liquid, gas) by going through scenarios
- Established global decisions: gravity is global, no per-material drag, density drives lift resistance
- Went through all powder scenarios (free fall, impact, sliding, belt, lift, displacement, etc.)
- Locked three powder attributes: `density`, `restitution`, `stability`
- Decided restitution is percentage-based, not fixed amount
- Determined friction/damping is NOT needed for powder (restitution covers slowdown between bounces)
- Renamed `slideResistance` → `stability`, `dispersionRate` → `spread`

## What's Open

- **Low-velocity horizontal movement gap**: powder sliding across a flat surface with no collisions — what bleeds off the velocity? Three options on the table (friction, surface-as-collision, velocity threshold). Needs a decision.

## What's Next

1. Resolve the horizontal movement open question
2. Design liquid attributes (same approach — list scenarios, identify differences)
3. Design gas attributes
4. Reconcile shared vs unique attributes across all three types
5. Update `OPEN-MaterialMovementLacksPerMaterialAttributes.md` with final attribute set
6. Implementation depends on `TwoDimensionalMaterialMovement.md` being done first

## Key Files

- `PowderAndLiquidAttributeDesign.md` — full design decisions and rationale
- `OPEN-MaterialMovementLacksPerMaterialAttributes.md` — bug report / feature spec (needs updating when design is final)
- `TwoDimensionalMaterialMovement.md` — prerequisite refactor
