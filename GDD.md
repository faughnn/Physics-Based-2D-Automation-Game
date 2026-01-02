# Gold Rush — Game Design Document

## Overview

A 2D side-scrolling sandbox game where the player digs terrain to produce sand, then builds an automated processing system using belts, lifts, water, and shakers to extract gold.

**Tech Stack:** Unity (2D), C#, Unity Physics 2D (Box2D)

**Target:** MVP prototype using placeholder shapes (sprites generated from code or Unity primitives). Fully code-driven — no manual scene wiring required beyond a bootstrap GameObject.

---

## Core Mechanics

### 1. Player Character

- Side-on view, controlled with keyboard
- Movement: left, right, jump
- Action: dig any block from anywhere (not just adjacent)
- Represented as a simple rectangle

### 2. Terrain and Digging

- World is a grid of solid terrain blocks
- When the player digs a block, it is destroyed and spawns **sand particles**
- Sand particles are small physics-enabled specks that fall with gravity
- Terrain is destructible; empty space allows movement and particle flow

### 3. Sand Particles

- Tiny circles, fully affected by Unity gravity
- Spawn at the location of a dug block
- Pile up naturally, flow through gaps
- Must be transported via infrastructure to be processed
- Particles should not leak between adjacent blocks (tight collision boundaries)

### 4. Infrastructure

All infrastructure is placed on a grid by the player. Placement is freeform (not restricted to solid ground).

#### Horizontal Belts
- Move sand particles horizontally (left or right, player chooses direction)
- Represented as a flat rectangle with directional indicator
- Sand landing on a belt is pushed in that direction

#### Vertical Lifts
- Move sand particles vertically (up or down, player chooses direction)
- Hollow design, full grid cell size (particles pass through the middle)
- Gradual acceleration — particles speed up smoothly rather than instant velocity
- Lifts can bridge gaps between vertical levels

#### Walls
- Solid placeable blocks
- Used to contain particles, direct flow, build structures
- Can be placed anywhere on the grid

#### Placement Rules
- Hold mouse to draw lines of belts, lifts, or walls (drag placement)
- No duplicate placement — cannot place infrastructure where one already exists
- Infrastructure can be deleted by the player

### 5. Water System

#### Water Reservoir
- Fixed reservoir at the **top** of the map (predefined area, not player-placed)
- Water flows as particles, affected by gravity
- Represented as blue particles

#### Water-Sand Interaction
- When water particles contact sand, the water is **absorbed** (water particle consumed)
- Sand becomes **wet sand** upon absorbing water
- Wet sand is a distinct particle type (visually different, e.g. darker colour)

#### Collision Notes
- No invisible collision under water — water zone should not block particles unexpectedly

### 6. Shaker Squares

- Placeable infrastructure block
- Wet sand placed on a shaker is processed over time
- Processing destroys the wet sand and produces:
  - **Gold particle** — falls through the shaker (small, yellow)
  - **Slag particle** — stays on top of the shaker (grey, discarded)
- Shakers slowly push wet sand and slag in one direction (player chooses direction on placement)
- This allows chaining shakers or automatically clearing slag off the edge
- Shakers vibrate visually to indicate operation

### 7. Gold Store

- Placeable container block
- Positioned below shakers to catch falling gold
- Collects and counts gold particles
- Represented as a rectangle with a counter display

### 8. Slag

- Waste product from shakers
- Sits on top of shaker, must be cleared or pushed off
- For MVP: slag can simply pile up and fall off edges, no special handling required

---

## Controls

| Input | Action |
|-------|--------|
| A / Left Arrow | Move left |
| D / Right Arrow | Move right |
| W / Up Arrow / Space | Jump |
| Left Click | Dig block (from anywhere) |
| Left Click (build mode) | Place selected infrastructure |
| Left Click + Drag | Draw line of selected infrastructure (belts/lifts/walls) |
| Right Click | Delete infrastructure or wall |
| Tab | Open/close build menu |
| Q / E | Rotate or toggle direction of selected infrastructure |
| Escape | Cancel placement / Open menu |

### Build Menu
- Opened with Tab
- Click on items to select infrastructure type
- Click in world to place (or drag to draw lines)

---

## Game World

- 2D side-on view
- World size: fixed for MVP (e.g. 800x600 or 1200x800 pixels)
- Coordinate system: origin top-left, Y increases downward
- Grid size: 32x32 pixels per cell (tuneable)

### Initial World Layout

- Upper portion: water reservoir (fixed, releases water particles)
- Middle portion: empty air
- Lower portion: solid diggable terrain
- Terrain extends to world boundaries (cannot dig outside)

---

## Physics (Unity Physics 2D)

### Gravity
- Set in Edit > Project Settings > Physics 2D, or via code: `Physics2D.gravity = new Vector2(0, -20f)`
- Suggested value: (0, -20) — tuneable

### Collision Layers
Define layers in code or Project Settings to control interactions:

| Layer | Collides With |
|-------|---------------|
| Player | Terrain, Infrastructure |
| Sand | Terrain, Infrastructure, Sand, Water |
| WetSand | Terrain, Infrastructure, WetSand |
| Gold | Terrain, Infrastructure, GoldStore |
| Slag | Terrain, Infrastructure |
| Water | Terrain, Water |
| Infrastructure | Player, Sand, WetSand, Gold, Slag |

Collision matrix configured programmatically via `Physics2D.IgnoreLayerCollision()`.

### Particle Behaviour
- Sand/wet sand/gold/slag use Rigidbody2D (Dynamic) + CircleCollider2D
- Rigidbody2D settings:
  - Mass: 0.1
  - Linear drag: 0.5
  - Gravity scale: 1
- Physics Material 2D:
  - Bounciness: 0.1
  - Friction: 0.5
- **Object pooling essential** — reuse particle GameObjects rather than Instantiate/Destroy

### Belt Physics
- Belts use a trigger collider (BoxCollider2D, isTrigger = true)
- OnTriggerStay2D applies velocity to particles: `rb.velocity = new Vector2(beltSpeed, rb.velocity.y)`

### Lift Physics
- Lifts are hollow (full grid cell size) — particles enter and travel through the middle
- Use trigger colliders on the lift boundaries
- Gradual acceleration: particles speed up over time rather than instant velocity change
- Implementation: apply force each frame rather than setting velocity directly, or lerp velocity towards target speed
- Alternatively, use AreaEffector2D for built-in force fields

---

## Prototype Visuals (Placeholder)

All graphics are simple shapes for MVP:

| Entity | Shape | Colour |
|--------|-------|--------|
| Player | Rectangle (24x48) | Green |
| Terrain block | Rectangle (32x32) | Brown |
| Sand particle | Circle (radius 2-3) | Tan/Beige |
| Wet sand particle | Circle (radius 2-3) | Dark brown |
| Gold particle | Circle (radius 2) | Yellow |
| Slag particle | Circle (radius 2-3) | Grey |
| Water particle | Circle (radius 2-3) | Blue (semi-transparent) |
| Wall | Rectangle (32x32) | Dark grey |
| Belt | Rectangle (32x8) | Dark grey with arrow |
| Lift | Hollow rectangle (32x32) | Dark grey with arrow |
| Shaker | Rectangle (32x16) | Orange, vibrates |
| Gold store | Rectangle (64x32) | Yellow outline with counter |

---

## MVP Scope

### Included
- Player movement and jumping
- Dig any block from anywhere
- Sand particle spawning and physics (proper piling, no leaks)
- Water reservoir at top, water flows as particles
- Water absorption into sand (water consumed, sand becomes wet)
- Belt placement and operation (horizontal transport)
- Lift placement and operation (vertical transport, hollow, gradual acceleration)
- Wall placement
- Shaker placement and processing (wet sand → gold + slag)
- Gold store placement and collection
- Build menu (Tab to open, click to select)
- Drag placement for lines of infrastructure
- Delete infrastructure and walls
- No duplicate placement in same grid cell
- Basic UI: gold counter, build menu

### Excluded (Future)
- Automated diggers
- Piped water / player-placed water
- Gold-rich vs barren terrain variation
- Saving/loading
- Sound effects and music
- Win/lose conditions
- Terrain generation beyond initial layout

---

## Project Structure (Unity)

```
GoldRush/
├── Assets/
│   ├── Scripts/
│   │   ├── Core/
│   │   │   ├── GameManager.cs        # Entry point, bootstraps entire game
│   │   │   ├── GameSettings.cs       # Constants (grid size, physics values)
│   │   │   └── LayerSetup.cs         # Programmatic layer/collision config
│   │   ├── World/
│   │   │   ├── WorldGenerator.cs     # Creates terrain grid at runtime
│   │   │   ├── TerrainBlock.cs       # Individual terrain block behaviour
│   │   │   └── WaterReservoir.cs     # Top water area, spawns water particles
│   │   ├── Player/
│   │   │   ├── PlayerController.cs   # Movement, jumping, digging
│   │   │   └── PlayerInput.cs        # Input handling
│   │   ├── Particles/
│   │   │   ├── ParticlePool.cs       # Object pool for all particle types
│   │   │   ├── SandParticle.cs       # Sand behaviour
│   │   │   ├── WetSandParticle.cs    # Wet sand behaviour
│   │   │   ├── WaterParticle.cs      # Water behaviour, absorbed by sand
│   │   │   ├── GoldParticle.cs       # Gold behaviour
│   │   │   └── SlagParticle.cs       # Slag behaviour
│   │   ├── Infrastructure/
│   │   │   ├── Belt.cs               # Horizontal transport
│   │   │   ├── Lift.cs               # Vertical transport (hollow, gradual accel)
│   │   │   ├── Wall.cs               # Solid placeable blocks
│   │   │   ├── Shaker.cs             # Processes wet sand into gold/slag
│   │   │   └── GoldStore.cs          # Collects and counts gold
│   │   ├── Building/
│   │   │   ├── BuildSystem.cs        # Placement logic, grid snapping
│   │   │   └── BuildPreview.cs       # Ghost preview when placing
│   │   └── UI/
│   │       ├── BuildMenuUI.cs        # Infrastructure selection
│   │       ├── GoldCounterUI.cs      # Displays gold collected
│   │       └── UIManager.cs          # UI bootstrapping
│   ├── Resources/                    # Empty for MVP (no art assets)
│   └── Scenes/
│       └── Bootstrap.unity           # Minimal scene: one GameObject with GameManager
├── ProjectSettings/                  # Unity project settings (auto-generated)
└── Packages/                         # Unity packages (auto-generated)
```

### Bootstrap Pattern

The only manual Unity Editor setup required:

1. Create new 2D Unity project
2. Create empty scene called "Bootstrap"
3. Create one empty GameObject named "GameManager"
4. Attach `GameManager.cs` to it
5. Hit Play — everything else is instantiated by code

---

## Implementation Notes for Claude Code

All game setup happens in code. The only manual step is creating an empty Unity 2D project with a bootstrap scene containing one GameObject with GameManager.cs attached.

### Build Order

1. **GameManager + Settings** — Entry point that instantiates everything. Define constants (grid size, world dimensions, physics values).

2. **Placeholder sprite generation** — Create simple coloured sprites in code using `Texture2D` and `Sprite.Create()`. No external assets needed.

3. **World generation** — Spawn terrain grid as GameObjects with BoxCollider2D. Store in 2D array for digging lookup. Ensure tight collision boundaries (no particle leaks).

4. **Player** — Spawn player GameObject with Rigidbody2D, BoxCollider2D, PlayerController script. Implement movement and jumping.

5. **Digging** — Allow digging any block from anywhere (click to dig). Spawn sand particles from ParticlePool.

6. **ParticlePool** — Pre-instantiate particle GameObjects for all types (sand, wet sand, water, gold, slag). Provide Get() and Return() methods. Essential for performance.

7. **Sand physics** — Particles with Rigidbody2D + CircleCollider2D. Should fall, pile, flow naturally without leaking between blocks.

8. **Water reservoir** — Spawn at top of map. Periodically releases water particles that flow downward.

9. **Water-sand interaction** — When water particle contacts sand particle, consume water and convert sand to wet sand.

10. **Walls** — Placeable solid blocks for containing/directing particles.

11. **Belts** — Trigger collider that modifies particle velocity in OnTriggerStay2D.

12. **Lifts** — Hollow design (full grid cell), gradual acceleration using force or velocity lerp.

13. **Build system** — Tab opens menu, click to select type, click to place, drag to draw lines. Track occupied cells to prevent duplicates. Right-click to delete.

14. **Shakers** — Trigger collider with timer. When wet sand enters, start processing. On complete: return wet sand to pool, spawn gold (below) and slag (above) from pool. Apply slow horizontal push via velocity.

15. **Gold store** — Trigger collider that catches gold, returns it to pool, increments counter.

16. **UI** — Use Unity's built-in UI (Canvas, Text) spawned from code. Build menu (Tab toggle, clickable items) and gold counter.

### Performance Considerations

- **Object pooling is mandatory** — Never use Instantiate/Destroy in gameplay loop
- **Consider particle limits** — Cap active particles (e.g. 5000) and recycle oldest if exceeded
- **Hybrid physics** — If still slow, consider disabling Rigidbody2D when particles are stationary (sleep) and only reactivating when disturbed
- **Particle simulation LOD** — Far-off particles could be simplified or merged (future optimisation)

---

## Success Criteria (MVP)

The prototype is complete when:

- [x] Player can move and jump
- [x] Player can dig any block from anywhere
- [x] Sand particles spawn and obey gravity
- [x] Sand particles pile properly without leaking between blocks
- [x] Water reservoir at top releases water particles
- [x] Water particles flow with gravity
- [x] Water absorbs into sand (water consumed, sand becomes wet)
- [x] Belts can be placed and transport particles horizontally
- [x] Lifts can be placed and transport particles vertically (hollow, gradual acceleration)
- [x] Walls can be placed to contain/direct particles
- [x] Tab opens build menu, click to select items
- [x] Hold mouse to draw lines of belts/lifts/walls
- [x] No duplicate placement in same grid cell
- [x] Infrastructure and walls can be deleted
- [x] Wet sand on shakers produces gold and slag
- [x] Gold store collects gold and displays count
- [x] All interactions use placeholder shapes (no art assets required)