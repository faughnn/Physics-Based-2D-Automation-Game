# Sandy - Roadmap

## Vision

A falling sand puzzle game where players build machines from simple structures to transport and process materials. Physics does the work - players just arrange the pieces.

**Simple structures + physics = emergent machines**

---

## Core Loop

1. Bucket appears somewhere on the map requiring a specific material
2. Player must figure out how to get that material to the bucket
3. May require: transporting, processing, combining structures creatively
4. Filling the bucket unlocks new tools/structures/areas
5. Repeat with increasingly complex requirements

---

## Major Systems

### Player
- Character that moves through the world
- Can pick up and use tools
- Can carry materials (somehow)
- Interacts with the physics

### Primitives vs Machines

**Key philosophy**: The game provides simple primitives. Players combine them into machines. Complex machines (crushers, sorters) are *invented* by players, not given.

#### Primitives (given to player)

| Primitive | What it does |
|-----------|--------------|
| Piston | Linear push/pull |
| Plate | Solid surface, can be moved |
| Motor/Axle | Provides rotation |
| Weight | Has mass, falls |
| Hinge | Allows rotation around a point |
| Spring | Stores and releases force |
| Grate | Surface with holes |
| Belt | Moves things horizontally |
| Trigger/Release | Timing mechanism |

#### Machines (player-built from primitives)

Crushing machines especially should be emergent:
- Jaw Crusher = Piston + Plate
- Stamp Mill = Weight + Release + Guides
- Roll Crusher = Two Motors + Two Drums

### Transport Structures

| Structure | Effect |
|-----------|--------|
| Belt | Moves stuff horizontally |
| Bucket Elevator | Chain of buckets lifts vertically |
| Catapult/Launcher | Flings clusters or material globs |
| Chute/Slide | Angled surface, gravity does the work |
| Pusher/Piston | Shoves things sideways |
| Trapdoor | Drops collected material when triggered |

### Heat/Processing

| Structure | Effect |
|-----------|--------|
| Furnace/Kiln | Heats contents, melts or burns |
| Cooler | Removes heat, solidifies molten stuff |
| Boiler | Heats water → steam (pressure?) |
| Quench Tank | Drops hot stuff into water |

### Flow Control

| Structure | Effect |
|-----------|--------|
| Hopper | Funnels material into narrow stream |
| Gate/Valve | Blocks or allows flow |
| Airlock | Two doors, never both open |
| Buffer Tank | Stores material, releases steadily |
| Splitter | Divides one stream into two |

### Power/Motion

| Structure | Effect |
|-----------|--------|
| Waterwheel | Falling water/material spins it, powers things |
| Windmill | Moving air spins it |
| Weight/Counterweight | Falling weight pulls something up |
| Pendulum | Swinging weight, periodic motion |
| Spring | Stores energy, releases with force |

### Buckets (Goals)
- Placed around the map in specific locations
- Detect material type and fill amount
- Trigger progression when filled

**Bucket Types** (escalating consequences for wrong material):

| Bucket | Behavior | Teaches |
|--------|----------|---------|
| Bin | Accepts anything, just counts target material | Basic transport |
| Filter Bucket | Wrong material passes through | That sorting exists |
| Purity Bucket | Needs 90%+ correct to complete | Build actual sorters |
| Fragile Bucket | Empties itself if contaminated | Precision matters |
| Volatile Bucket | Explodes on wrong material | High stakes, don't mess up |

### Sorting/Separating

| Structure | Sorts by |
|-----------|----------|
| Vibrating Screen | Shakes - small stuff falls through |
| Cyclone | Spins - heavy stuff flung outward |
| Settling Tank | Density - dense sinks, light floats |
| Magnetic Drum | Material - pulls iron off a belt |
| Air Classifier | Weight - fan blows light particles away |
| Trommel | Size - rotating drum with holes |

### Progression
- Start simple: shovel + dirt + nearby bucket
- Gradually introduce: distance, processing, automation
- Later puzzles need multi-stage solutions

---

## Rough Phases

### Phase 1: Foundation
- Player character & movement
- Basic tools (shovel)
- Carrying/dropping materials
- Bucket goal system

### Phase 2: Primitives & Building
- Core primitives (piston, plate, hinge, motor, etc.)
- Players can place/connect primitives
- Physics interactions between primitives and materials
- First emergent machines possible (simple crushers)

### Phase 3: Progression
- Unlock chain
- Map with bucket locations
- Increasing complexity
- Maybe: levels or open world with zones

### Phase 4: Polish & Emergence
- Tune physics for satisfying machines
- Edge cases and weird combos
- Let players break things in fun ways

---

## Inspiration
- Besiege (build machines from parts)
- Powder Toy / Noita (falling sand physics)
- Poly Bridge (physics puzzle solving)
- Factorio (automation satisfaction)

---

## Open Questions
- How does carrying work? Inventory? Physical holding?
- Pre-built maps or procedural?
- How do players place structures? Build mode? Physical placement?
