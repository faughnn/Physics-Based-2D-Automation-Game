# Powder and Liquid Attribute Design

## Approach

Design attributes by going through each behavior type (powder, liquid, gas) scenario by scenario, identifying what should differ between materials, and extracting the minimal set of attributes.

## Global Decisions

- **Gravity is a global constant**, not per-material. Everything accelerates the same in free fall.
- **No per-material drag/air resistance** for now. Terminal velocity is handled by the global max velocity cap (16).
- **Density is not just displacement** — it also drives lift resistance. Denser materials are harder for lifts to push. This is physically correct: gravity pulls everything equally, but applied forces (lifts) affect lighter materials more.
- **All attributes are bytes (0-255)**, integer math only, Burst-compatible.
- **One flat struct** (`MaterialDef`) — all attributes live on every material. Simulation code for each behavior type reads whichever fields it cares about.

## Powder Attributes (Locked)

| Attribute | What it does | When it applies |
|-----------|-------------|-----------------|
| `density` | Displacement ordering + lift resistance | Heavier sinks below lighter; heavier resists lift more |
| `restitution` | Percentage of energy retained on collision, any direction | Moving cell hits something — floor, wall, ceiling, other cell |
| `stability` | Probability of refusing to topple at rest (rename from `slideResistance`) | Cell is at rest on a pile, diagonals are open |

### How they interact — powder lifecycle:

1. **Free fall** — global gravity, no per-material difference
2. **Impact** — `restitution` determines how much velocity survives, cell bounces/scatters
3. **Subsequent bounces** — `restitution` applied each collision, energy decays naturally (percentage-based)
4. **At rest** — `stability` decides whether cell topples off pile or stays put
5. **Displacement** — `density` determines who sinks below who
6. **In a lift** — `density` determines how much the cell resists upward force
7. **Belt push** — belts give a fixed push to all materials equally, no per-material difference

### Restitution is percentage-based, not fixed amount

Each bounce costs a percentage of current velocity. Fast cells lose more absolute energy per bounce, slow cells die quickly. Gives natural decaying bounce pattern: big splash → smaller bounce → smaller bounce → stop.

### Why no friction/damping for powder

Every slowdown scenario is covered:
- **Between bounces**: gravity pulls cell down into next collision, restitution bleeds energy
- **At rest**: stability handles whether cell slides or stays
- **Belt/lift**: fixed forces, no per-material response needed beyond density

## Open Question: Low-Velocity Horizontal Movement

There's a gap: a powder cell moving slowly sideways across a flat surface. It has velocity (so stability doesn't apply) but nothing to collide with (so restitution doesn't apply). What slows it down?

Options discussed:
1. **Add friction** — velocity decays each step (reintroduces the attribute we removed)
2. **Surface contact = collision** — moving along a surface applies restitution each step
3. **Velocity threshold** — below some speed, snap to zero and let stability take over

**Not yet decided.** This needs more thought before moving on.

## Liquid Attributes

**Not yet designed.** Next step after resolving the powder open question.

## Gas Attributes

**Not yet designed.** After liquid.

## Renames

| Old Name | New Name | Reason |
|----------|----------|--------|
| `slideResistance` | `stability` | Describes what it actually controls — stability at rest on a pile |
| `dispersionRate` | `spread` | Simpler, clearer — horizontal spread distance |
