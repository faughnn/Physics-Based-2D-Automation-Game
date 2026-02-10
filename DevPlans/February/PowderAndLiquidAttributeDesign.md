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

## Resolved: Low-Velocity Horizontal Movement

**No gap exists.** Gravity is always active. A powder cell moving horizontally will always arc downward and collide with the surface below it. That collision triggers restitution, which bleeds velocity. There is no scenario where a powder cell moves horizontally forever without hitting something. Restitution *is* the damping — it just fires via collision rather than per-frame decay.

## Considered and Rejected for Powder

| Attribute | Why considered | Why rejected |
|-----------|---------------|-------------|
| **Per-material gravity** | Research suggests it; heavier things "should" fall faster? | Physically incorrect — all objects accelerate equally under gravity. We don't simulate air resistance. A heavy boulder and a grain of sand fall at the same rate. Density already differentiates materials via displacement and lift resistance. Adding per-material gravity would make light materials float unrealistically. |
| **Damping / friction** | Research lists it as essential; bleeds velocity per frame | Redundant with restitution. Gravity guarantees powder cells collide with surfaces every frame of horizontal movement, so restitution fires continuously. No scenario exists where a powder cell travels horizontally without eventually hitting something. Two knobs (restitution + damping) would be confusing to tune with overlapping effects. |
| **Lifetime / decay** | Research uses a per-cell timer for embers, sparks dissipating | Valid feature but not a powder movement attribute. This is a cross-cutting concern (applies to all material types) and belongs in a separate decay/reaction system. Would add per-cell storage cost for something most powder materials never use. |
| **Reactions / temperature** | Research stores per-cell state byte for temperature, chemical reactions | Same as lifetime — cross-cutting systems, not movement attributes. Water + lava = steam is a reaction system. Heat propagation is a temperature system. Neither belongs in the per-material movement attribute set. These are future systems that layer on top of movement. |
| **Direction hint** | Research uses 2-bit flag for momentum memory in simulations without velocity | We already store full velocity per cell. Direction hints are for simulations where cells move 0-1 tiles per step and need to fake momentum. Our cells move up to 16 tiles per frame with real velocity vectors. Completely redundant. |

## Expected Powder Behavior by Material

### Sand (density: ~85, restitution: low ~30%, stability: low ~20%)
- **Dropped from height**: Falls, hits ground, barely bounces (low restitution), settles quickly
- **Poured onto pile**: Flows down sides freely (low stability), forms ~45° angle from grid geometry
- **Hit by explosion**: Scatters, arcs back down, each bounce kills most velocity, comes to rest fast
- **On a belt**: Pushed sideways at belt speed, falls off end normally
- **In a lift**: Pushed up with moderate resistance (medium-high density)
- **Meets water**: Sinks through (higher density than water)

### Gravel (density: ~95, restitution: medium ~50%, stability: high ~80%)
- **Dropped from height**: Falls, hits ground, bounces a bit (medium restitution), scatters slightly
- **Poured onto pile**: Stacks steeply (high stability), rarely topples, forms near-vertical faces
- **Hit by explosion**: Scatters, bounces several times before stopping, travels further than sand
- **On a belt**: Same as sand — belts push all materials equally
- **In a lift**: Hard to push (high density)
- **Meets water**: Sinks fast (much higher density)

### Ash (density: ~15, restitution: very low ~10%, stability: very low ~5%)
- **Dropped from height**: Falls, hits ground, almost no bounce, dead stop
- **Poured onto pile**: Spreads flat (very low stability), almost liquid-like spreading
- **Hit by explosion**: Scatters wide but each impact kills almost all velocity, settles quickly into flat layer
- **On a belt**: Pushed sideways, falls off end
- **In a lift**: Pushed up easily (very low density)
- **Meets water**: Sinks (still denser than water) but only barely

## Liquid Attributes (Locked)

| Attribute | What it does | When it applies |
|-----------|-------------|-----------------|
| `density` | Displacement ordering + lift resistance (shared with powder) | Oil floats on water; heavier liquids resist lifts more |
| `restitution` | Percentage of energy retained on collision (shared with powder) | Water splashes on impact; honey doesn't |
| `spread` | Probability (0-255) of horizontal movement per frame | Water spreads every frame; honey rarely spreads |

### How they interact — liquid lifecycle:

1. **Free fall** — global gravity, no per-material difference (same as powder)
2. **Impact** — `restitution` determines splash vs dead stop. Water splashes (medium restitution), honey thuds (very low restitution)
3. **Horizontal spread** — blocked below, cell checks sideways. `spread` = probability it moves. High spread = fast flowing water. Low spread = slow oozing honey. This is the defining difference from powder.
4. **Leveling** — spread fires every frame as long as the surface is uneven. Once liquid is level (equal height on both sides), there's nowhere to spread to, so it stops naturally. No separate leveling system needed.
5. **Displacement** — `density` determines layering. Oil (low density) floats on water (medium density).
6. **In a lift** — `density` determines resistance. Water pushed up easily, mercury barely moves.
7. **Belt push** — belt gives fixed push. Liquid with high `spread` dilutes the effect (spreading in all directions). Low `spread` liquid (honey) moves as a blob with the belt.
8. **At rest** — liquid has `stability = 0`. It never refuses to move. Combined with `spread`, liquid always seeks to flatten.

### Spread stops naturally

Spread is not perpetual motion. A liquid cell only spreads when there's somewhere lower or emptier to go. Once a pool is level, no cell has anywhere to spread to, and movement stops. No damping or decay needed for horizontal spread.

### Anti-oscillation is an implementation detail, not an attribute

The research warns about liquid jittering between walls. The fix (prefer last movement direction) is needed for ALL liquids equally — it's simulation logic, not a per-material tuning value. Not an attribute.

### Stability and spread are related but separate

- `stability` (powder): probability of *refusing* to move at rest
- `spread` (liquid): probability of *actively moving* at rest

They're opposite impulses. Powder reads stability, liquid reads spread, each type ignores the other. Keeping them separate is cleaner than merging into one signed value.

### Viscosity emerges from spread

The research treats viscosity as a core attribute. In our system, viscosity is just low `spread`. Honey = low spread = cells rarely move apart = stays blobby. Water = high spread = cells rush to separate = thin and flat. No separate viscosity attribute needed.

### Surface tension skipped

Would require neighbor-checking system (cells prefer being adjacent to same-type cells). Subtle visual effect, high implementation cost. Not worth it for movement attributes.

## Considered and Rejected for Liquid

| Attribute | Why considered | Why rejected |
|-----------|---------------|-------------|
| **flow_speed** | Research lists it as core liquid attribute | This is our `spread` under a different name. Covered. |
| **damping** | Research lists it for liquids too | Spread stops naturally when surface is level. No perpetual motion to damp. Same reasoning as powder — gravity handles vertical slowdown via restitution, spread handles horizontal movement via leveling. |
| **direction_memory** | Research warns about oscillation without it | Implementation detail, not a per-material attribute. All liquids need anti-oscillation equally. Lives in simulation code, not material definition. |
| **viscosity** | Seems like a core liquid property | Emerges from `spread`. Low spread = viscous (honey). High spread = thin (water). Adding a separate viscosity would duplicate spread's effect. |
| **surface_tension** | Water beads up, forms droplets | Requires expensive neighbor-checking each frame for a subtle visual effect. Not a movement attribute. Could be a future system layered on top. |

## Expected Liquid Behavior by Material

### Water (density: ~30, restitution: medium ~40%, spread: very high ~240)
- **Dropped from height**: Falls, splashes on impact (medium restitution), droplets scatter
- **Poured into container**: Fills up, levels quickly (very high spread), flat surface within a few frames
- **On a belt**: Pushed sideways but also spreading everywhere, belt effect somewhat diluted
- **In a lift**: Pushed up easily (low density)
- **Meets oil**: Oil floats on top (oil has lower density)
- **Meets sand**: Sand sinks through (sand has higher density)

### Honey (density: ~45, restitution: very low ~15%, spread: low ~40)
- **Dropped from height**: Falls, thuds on impact, barely splashes, stays as a blob
- **Poured into container**: Oozes slowly, takes many frames to level, sits in thick lumps for a while
- **On a belt**: Moves as a cohesive blob, belt effect very visible
- **In a lift**: Moderate resistance (medium density)
- **Meets water**: Sinks below water (higher density)

### Oil (density: ~20, restitution: low ~25%, spread: high ~200)
- **Dropped from height**: Falls, mild splash, less energetic than water
- **Poured into container**: Spreads fast (nearly as fast as water), levels quickly
- **On a belt**: Similar to water, spreads while being pushed
- **In a lift**: Pushed up very easily (very low density)
- **Meets water**: Floats on top (lower density than water)

## Gas Attributes

**Not yet designed.** After liquid.

## Renames

| Old Name | New Name | Reason |
|----------|----------|--------|
| `slideResistance` | `stability` | Describes what it actually controls — stability at rest on a pile |
| `dispersionRate` | `spread` | Simpler, clearer — horizontal spread distance |
