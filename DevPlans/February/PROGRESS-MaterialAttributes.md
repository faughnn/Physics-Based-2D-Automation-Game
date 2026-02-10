# Material Attributes — Current Progress

## Status: In Progress — Powder and Liquid attributes LOCKED. Ready to design gas.

## What's Done

### Powder (Locked)
- Three attributes: `density`, `restitution`, `stability`
- Resolved horizontal movement gap (gravity guarantees collisions, restitution is the damping)
- Rejected: per-material gravity, damping, lifetime, reactions, temperature, direction hints
- Documented expected behavior for sand, gravel, ash

### Liquid (Locked)
- Three attributes: `density`, `restitution`, `spread`
- Two shared with powder (density, restitution), one new (spread)
- Spread = probability of horizontal movement per frame (0-255). Stops naturally when surface is level.
- Viscosity emerges from spread (low spread = viscous). No separate attribute needed.
- Anti-oscillation is implementation detail, not a per-material attribute
- Stability and spread are related but kept separate (opposite impulses for powder vs liquid)
- Rejected: flow_speed (= spread), damping, direction_memory, viscosity, surface_tension
- Documented expected behavior for water, honey, oil

### Global Decisions
- Gravity is global, not per-material
- No per-material drag/air resistance
- Density drives displacement + lift resistance
- All attributes are bytes (0-255), integer math, Burst-compatible
- One flat struct, each behavior type reads what it cares about

## What's Open

- Nothing for powder or liquid. All questions resolved.

## What's Next

1. Design gas attributes (same approach — list scenarios, identify what differs between smoke/steam/etc.)
2. Reconcile shared vs unique attributes across all three types
3. Update `OPEN-MaterialMovementLacksPerMaterialAttributes.md` with final attribute set
4. Implementation depends on `TwoDimensionalMaterialMovement.md` being done first

## Key Files

- `PowderAndLiquidAttributeDesign.md` — full design decisions, rationale, rejected alternatives, expected behaviors
- `OPEN-MaterialMovementLacksPerMaterialAttributes.md` — bug report / feature spec (needs updating when design is final)
- `TwoDimensionalMaterialMovement.md` — prerequisite refactor
