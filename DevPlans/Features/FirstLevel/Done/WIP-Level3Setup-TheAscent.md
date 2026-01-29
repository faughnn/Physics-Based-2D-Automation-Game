# Level 3 Setup - "The Ascent"

**STATUS: PLANNED**

---

## Review Notes

**Reviewed:** 2026-01-26

### Verification Summary

1. **World Dimensions** - CORRECTED
   - Original plan used 1024x512 (inconsistent with master layout)
   - Updated to 1920x1620 per 08-TutorialMapLayout

2. **Floating Island Position** - CORRECTED
   - Original: X=700-900, Y=30-80
   - Corrected: X=100-700, Y=320-450 per 08-TutorialMapLayout

3. **Bucket 3 Position** - CORRECTED
   - Original: (780, 35)
   - Corrected: (200, 266) per 08-TutorialMapLayout

4. **Lift Zone Position** - CORRECTED
   - Original plan didn't mention pre-built lift zone walls
   - Added: Lift zone at X=440-520, Y=350-1200 per 08-TutorialMapLayout
   - Lift zone has stone walls and floor already built into terrain

5. **Vertical Transport Distance** - CORRECTED
   - Original claim: "~1080 cells" was incorrect
   - Actual lift zone height: ~850 cells (Y=350 to Y=1200)
   - Ground surface to island: ~1030 cells (Y=1350 to Y=320)

6. **Reward Ability** - CORRECTED
   - Original: Ability.PlaceHoppers (Level 3 reward)
   - Corrected: Ability.None (Victory - tutorial complete) per 07-MultiObjectiveProgression

7. **Lift System Reference** - CORRECTED
   - Original: "PLANNED-LiftSystem.md (needs creation)"
   - Corrected: File exists at `DevPlans/Features/PLANNED-LiftSystem.md`

8. **Objective Count** - VERIFIED
   - 1500 Dirt matches 08-TutorialMapLayout specification

9. **Level Design Logic** - VERIFIED
   - Lift + belt combination genuinely required
   - Lift zone connects ground level (Y~1200) to island level (Y~350)
   - Horizontal belt needed to bridge ~280 cell gap from lift exit (X~480) to bucket (X=200)
   - Player cannot reach island by jumping (~1030 cell vertical gap)

10. **Dependencies** - VERIFIED
    - All referenced systems exist or have dev plans
    - Lift system is documented but NOT IMPLEMENTED

### Changes Made
- Updated all coordinates to match 08-TutorialMapLayout
- Updated world dimensions from 1024x512 to 1920x1620
- Corrected vertical distance calculations
- Updated reward ability from PlaceHoppers to None (Victory)
- Fixed lift system documentation reference
- Updated terrain region specifications
- Updated visual diagrams

**Second Pass (2026-01-26)**

### Cross-Plan Verification

1. **Floating Island Coordinates** - VERIFIED EXACT MATCH
   - 08-TutorialMapLayout specifies:
     - Main platform: X=100-700, Y=350-450 (Materials.Stone)
     - Surface layer: X=120-680, Y=320-349 (Materials.Stone)
     - Bucket platform: X=150-250, Y=280-319 (Materials.Stone)
   - This plan matches exactly

2. **Lift Zone Coordinates** - VERIFIED EXACT MATCH
   - 08-TutorialMapLayout specifies:
     - Left wall: X=440-450, Y=350-1200
     - Right wall: X=510-520, Y=350-1200
     - Floor: X=440-520, Y=1200-1250
   - This plan matches exactly

3. **Bucket 3 Position** - VERIFIED EXACT MATCH
   - 08-TutorialMapLayout: (200, 266)
   - This plan: (200, 266)

4. **Objective Count Discrepancy** - NOTED
   - 07-MultiObjectiveProgression (after first pass corrections): Uses 500/1000/1500 progression
   - 08-TutorialMapLayout (after first pass corrections): Uses 500/500/500 (uniform)
   - This plan uses: 1500 Dirt
   - **Resolution**: The 500/1000/1500 pattern from 07-MultiObjectiveProgression is the intended design (increasing difficulty with automation). Plan 08's uniform 500 was a correction that may need review. This plan's 1500 is correct for Level 3.

5. **Reward Ability** - VERIFIED
   - 07-MultiObjectiveProgression specifies: Level 3 reward = Ability.None (Victory)
   - This plan correctly uses Ability.None

6. **Lift System Reference** - VERIFIED
   - PLANNED-LiftSystem.md exists at `DevPlans/Features/PLANNED-LiftSystem.md`
   - Describes physics-based lift force (gravity +17, lift -20, net -3 upward)
   - Open-on-all-sides design matches this plan's description
   - 8x8 tile size matches belt system

7. **Lift Placement (11-PlayerStructurePlacement.md)** - VERIFIED
   - Plan 11 handles lift placement in game mode (L key)
   - Correctly gates on Ability.PlaceLifts (unlocked from Level 2)
   - Lift placement implementation deferred until lift system implemented
   - No conflicts with this plan

8. **Terrain Region Consistency** - VERIFIED
   - All TerrainRegion specifications in this plan match 08-TutorialMapLayout exactly
   - Lift zone interior (X=451-509) correctly identified as Air for player lift placement

9. **Horizontal Distance Calculation** - VERIFIED
   - Lift zone center: X~480 (between walls at 440-450 and 510-520)
   - Bucket X: 200
   - Distance: 480 - 200 = 280 cells - CORRECT

### No Changes Required
All coordinates verified against master layout. The objective count (1500) aligns with the intended progression pattern from 07-MultiObjectiveProgression.

---

## Summary

Level 3 of the tutorial introduces vertical transport challenges. A floating island hovers ~1030 cells above ground level (from Y=1350 ground surface to Y=320 island surface) with a bucket waiting at the top. The player must combine belts (unlocked in Level 1) and lifts (unlocked in Level 2) to build a transport system that moves dirt UPWARD against gravity.

The world includes a pre-built lift zone (stone walls at X=440-520, Y=350-1200) that provides the vertical conduit. The player places lift structures inside this zone to enable upward transport, then uses belts to route material from the lift exit to the bucket on the floating island.

This is the final tutorial level - completing it marks victory.

---

## Prerequisites

This level requires the following systems to be fully implemented:

| Dependency | Status | Notes |
|------------|--------|-------|
| **02-DiggingSystem** | IMPLEMENTED | Player can dig Ground to produce Dirt |
| **04-BucketProgressionSystem** | IMPLEMENTED | Bucket + ProgressionManager |
| **05-Level1Setup** | IMPLEMENTED | LevelData, LevelLoader, Level1Data patterns |
| **05-BeltSystem** | IMPLEMENTED | BeltManager, 8x8 belt blocks |
| **08-TutorialMapLayout** | PLANNING | Master layout for full 1920x1620 world |
| **09-Level2Setup-TheDescent** | NOT STARTED | Must implement first (unlocks lifts) |
| **Lift System** | NOT IMPLEMENTED | See `DevPlans/Features/PLANNED-LiftSystem.md` |

**Critical Dependency:** The Lift System must be implemented before this level can function. The lift system design document exists at `DevPlans/Features/PLANNED-LiftSystem.md`.

---

## Design Goals

### Player Learning Progression

1. **Level 1**: Manual transport - dig, grab, carry, drop into bucket -> Unlocks Belts
2. **Level 2**: Horizontal automation - use belts to move dirt down slope to bucket -> Unlocks Lifts
3. **Level 3** (this level): Vertical automation - combine belts + lifts for upward transport -> Victory!

### Design Intent

- **Combines previous learnings**: Belts from L1, lifts from L2
- **First real machine**: Player chains multiple structure types together
- **Teaches vertical transport**: Gravity working AGAINST the player now
- **Pre-built infrastructure**: Lift zone walls provide the vertical conduit
- **Planning required**: Player must think about the full path before building
- **Maximum visual satisfaction**: Watching dirt travel a complex automated path
- **Tutorial Conclusion**: Successfully completing this level finishes the tutorial

---

## World Layout

### Dimensions (per 08-TutorialMapLayout)

```
World Width:  1920 cells (2 screens wide)
World Height: 1620 cells (3 screens tall)
Viewport:     960 x 540 cells (one screen)
```

### Terrain Design

```
Y=0      +------------------------------------------+ Top of world
         |                                          |
         |                                          |
Y=200    |                                          |
         |   [Bucket 3]                             |
Y=266    |   +=======+                              |
Y=280    |   | Raised|                              |
Y=320    |   +================================+     |  <- Island surface
         |===+================================+===  |  <- Main platform (Stone)
Y=350    |   |                                |     |
Y=450    |==========================================|  <- Island bottom
         |                                          |
         |           |     |                        |
         |           | LIFT|   <- Pre-built lift    |
         |           | ZONE|      zone walls        |
         |           |     |      X=440-520         |
         |           |     |                        |
Y=1200   |           +=====+   <- Lift zone floor   |
         |                                          |
Y=1350   |==========================================|  <- Ground surface
         |                                          |
         |          DIGGABLE GROUND                 |
         |          (Materials.Ground)              |
Y=1619   +------------------------------------------+ Bottom of world
         X=0        X=440 X=520              X=1920
```

### Key Measurements (per 08-TutorialMapLayout)

| Metric | Value | Calculation |
|--------|-------|-------------|
| Ground surface Y | 1350 | Diggable area minY |
| Floating island top Y | 280 | Bucket platform level |
| Floating island surface Y | 320-350 | Main walking surface |
| Floating island bottom Y | 450 | Solid platform bottom |
| Lift zone top Y | 350 | Connects to island level |
| Lift zone bottom Y | 1200 | Connects to ground level |
| Lift zone height | 850 cells | 1200 - 350 = 850 cells |
| Ground to island gap | ~1030 cells | 1350 - 320 = 1030 cells |
| Lift exit to Bucket gap | ~280 cells | X=480 (lift center) to X=200 (bucket) |

**Note:** The ~1080 cells originally mentioned aligns with the actual ground-to-island vertical distance (~1030 cells).

---

## Position Specifications (per 08-TutorialMapLayout)

### Floating Island

```csharp
// Island main platform (Stone)
new TerrainRegion(minX: 100, maxX: 700, minY: 350, maxY: 450, materialId: Materials.Stone)

// Island top layer (Stone)
new TerrainRegion(minX: 120, maxX: 680, minY: 320, maxY: 349, materialId: Materials.Stone)

// Island raised edges - Bucket platform area (Stone)
new TerrainRegion(minX: 150, maxX: 250, minY: 280, maxY: 319, materialId: Materials.Stone)

// Island dimensions:
// - Width: 600 cells (100 to 700)
// - Height: ~130 cells (Y=320 to Y=450 main body)
// - Position: Left-center of world, high in sky
// - Bucket sits on raised platform at X=150-250, Y=280-319
```

### Lift Zone Structure (Pre-Built)

The lift zone is a pre-built vertical conduit with stone walls:

```csharp
// Left wall of lift column
new TerrainRegion(minX: 440, maxX: 450, minY: 350, maxY: 1200, materialId: Materials.Stone)

// Right wall of lift column
new TerrainRegion(minX: 510, maxX: 520, minY: 350, maxY: 1200, materialId: Materials.Stone)

// Floor connection at ground level
new TerrainRegion(minX: 440, maxX: 520, minY: 1200, maxY: 1250, materialId: Materials.Stone)

// Lift zone interior: X=451-509, Y=350-1199 (Air - player places lifts here)
```

### Ground Terrain

```csharp
// Main diggable ground (left side of world - shared with Level 1)
new TerrainRegion(
    minX: 0,
    maxX: 550,
    minY: 1350,
    maxY: 1619,
    materialId: Materials.Ground
)
```

### Bucket 3 Position

```csharp
// Bucket on floating island raised platform
Vector2Int Bucket3Spawn = new Vector2Int(200, 266);

// Cell position (200, 266) places bucket on the raised edge section
// Bucket needs material routed from lift exit via horizontal belt
```

**Bucket Placement Logic:**
- Bucket structure is 20 cells wide (16 interior + 2 wall thickness on each side)
- Bucket structure is 14 cells tall (12 depth + 2 wall thickness at bottom)
- Bucket spawn position is TOP-LEFT corner
- Bucket at (200, 266) sits on the raised platform section (Y=280-319)
- Horizontal distance from lift zone (X~480) to bucket (X=200): ~280 cells
- Requires belt(s) to bridge the gap from lift exit to bucket

### Player Spawn

```csharp
// Player spawns same as Level 1 - in spawn zone area
Vector2Int PlayerSpawn = new Vector2Int(200, 1320);  // Falls onto ground
```

### Shovel Spawn (if needed)

If player doesn't carry shovel between levels:
```csharp
Vector2Int ShovelSpawn = new Vector2Int(350, 1336);
```

---

## Lift System Requirements

Level 3 requires a working lift system. The design is documented in `DevPlans/Features/PLANNED-LiftSystem.md`.

### Key Lift System Concepts (from PLANNED-LiftSystem.md)

**Open on All Sides:**
- Lifts don't contain material - they apply upward force to material passing through
- A belt must push material into the lift from below
- Material exits naturally at the top and continues with its velocity
- Like air being pushed from below

**Physics-Based Movement:**
- Gravity and lift force compete in the fractional velocity accumulator
- Gravity: +17/frame (down), Lift: -20/frame (up)
- Net effect in lift zone: -3/frame (slow rise)
- Material gradually accelerates upward inside lift zone

### LiftStructure (from PLANNED-LiftSystem.md)

```csharp
public struct LiftStructure
{
    public const int TileSize = 8;

    public ushort id;
    public int tileX;           // X position (8-aligned)
    public int minY, maxY;      // Vertical extent (merged lifts)
    public byte liftForce;      // Fractional force (default 20)
}
```

### Visual Concept

```
        EXIT (cells fall off here with upward momentum)
         v
    [====]  <- Receiving belt or platform
        |L|  <- Lift zone (cells move UP through zone)
        |L|
        |L|
        |L|
    =====>   <- Feeding belt brings cells to lift base
```

### Lift-Belt Connection Points for Level 3

The magic of Level 3 is connecting structures within the pre-built lift zone:

1. **Belt feeds into lift base**: Belt moves dirt to lift zone entry at Y~1200
2. **Lift raises dirt**: Cells travel upward through the 850-cell lift zone
3. **Lift exits at island level**: Cells exit at Y~350 with upward momentum
4. **Belt routes to bucket**: Horizontal belt(s) guide dirt across the ~280 cells from lift exit (X~480) to bucket (X=200)

---

## Multi-Stage Transport Design

### Intended Solution Path

```
Player's dig site (ground level, X=0-550, Y=1350)
       |
       v
   [DIG AREA] - Player digs Ground, produces Dirt
       |
       | (Dirt falls/belts to lift zone entry)
       v
   [BELT 1] - Horizontal belt moves dirt to lift base (Y~1200)
       |
       v
   [LIFT ZONE] - Pre-built walls at X=440-520, Y=350-1200
       |         Player places lift structures inside
       | (Cells rise ~850 cells)
       v
   [BELT 2] - Belt at island level (Y~350) routes left to bucket
       |       Distance: ~280 cells from X=480 to X=200
       v
   [BUCKET 3] - Collection point on floating island at (200, 266)
```

### Minimum Required Structures

| Structure | Position | Direction | Purpose |
|-----------|----------|-----------|---------|
| Belt 1 | Ground level, routing to X~440-520, Y~1200 | Right (+1) | Feed dirt to lift zone base |
| Lift(s) | Inside lift zone X=451-509, Y=350-1199 | Up (-1) | Raise dirt vertically |
| Belt 2 | Island level, X~480 to X~200, Y~350 | Left (-1) | Route dirt from lift exit to bucket |

### Alternative Player Solutions

The beauty of sandbox puzzles is multiple valid solutions:

1. **Single tall lift**: Fill entire lift zone with one lift structure
2. **Multi-stage lifts**: Shorter lifts with gaps (more building but more reliable)
3. **Multiple parallel lifts**: Use full width of lift zone for higher throughput
4. **Creative belt routing**: Any path that gets dirt to the bucket works!

---

## Level Data Structure

**Note:** Level 3 is part of the unified TutorialLevelData. This section shows Level 3-specific configuration.

### Level 3 Objective (from TutorialLevelData.cs)

```csharp
// Level 3: Lift + Belt transport - Victory!
new ObjectiveData(
    targetMaterial: Materials.Dirt,
    requiredCount: 1500,
    rewardAbility: Ability.None,  // Victory - tutorial complete!
    displayName: "Level 3: Victory!"
)
```

### Level 3 Bucket Position

```csharp
new BucketSpawn(new Vector2Int(200, 266), CreateLevel3Objective())
```

### Terrain Regions Relevant to Level 3

Level 3 uses terrain defined in TutorialLevelData. Key regions:

```csharp
// === ZONE 4: Floating Island ===
regions.Add(new TerrainRegion(100, 700, 350, 450, Materials.Stone));  // Main platform
regions.Add(new TerrainRegion(120, 680, 320, 349, Materials.Stone));  // Surface layer
regions.Add(new TerrainRegion(150, 250, 280, 319, Materials.Stone));  // Bucket platform

// === ZONE 3: Lift Zone ===
regions.Add(new TerrainRegion(440, 450, 350, 1200, Materials.Stone)); // Left wall
regions.Add(new TerrainRegion(510, 520, 350, 1200, Materials.Stone)); // Right wall
regions.Add(new TerrainRegion(440, 520, 1200, 1250, Materials.Stone)); // Floor

// === ZONE 1: Diggable Ground (shared across levels) ===
regions.Add(new TerrainRegion(0, 550, 1350, 1619, Materials.Ground));
```

### Why 1500 Dirt?

- Level 1 required 500 Dirt (manual transport - easy)
- Level 2 required 1000 Dirt (belt transport - medium)
- Level 3 requires 1500 Dirt (lift + belt - hardest, but fully automated once built)
- The higher count rewards the effort of building a complete automated system
- Once the transport chain is working, 1500 is achievable through sustained operation

---

## Ability Progression Update

### Current Ability.cs (per 07-MultiObjectiveProgression)

```csharp
public enum Ability
{
    None = 0,
    PlaceBelts = 1,    // Level 1 reward
    PlaceLifts = 2,    // Level 2 reward
    PlaceFurnace = 3,  // Future levels
}
```

### Level 3 Reward: Victory (Ability.None)

Level 3 is the final tutorial level. Completing it does NOT unlock a new ability - instead:
- Display "TUTORIAL COMPLETE!" or "VICTORY!" message
- Mark tutorial as finished
- Potentially unlock free play mode or next game section

Future abilities (Hoppers, Furnaces, Gates) are reserved for post-tutorial content.

---

## Implementation Order

### Phase 1: Prerequisites (Must Complete First)

1. [ ] Implement 08-TutorialMapLayout (unified world with all zones)
2. [ ] Implement 09-Level2Setup-TheDescent (unlocks lifts)
3. [ ] Implement Lift System per `DevPlans/Features/PLANNED-LiftSystem.md`
4. [ ] Test lift system in sandbox mode
5. [ ] Implement lift placement UI in game mode (gated by Ability.PlaceLifts)

### Phase 2: Level 3 Specific

Level 3 uses terrain already defined in TutorialLevelData. Main tasks:

6. [ ] Verify Bucket 3 is spawned at (200, 266) with correct objective
7. [ ] Verify lift zone walls/floor are solid
8. [ ] Test lift placement inside lift zone (X=451-509)
9. [ ] Test belt placement on island surface
10. [ ] Ensure terrain colliders work for floating island

### Phase 3: Victory Condition

11. [ ] Implement victory state when Level 3 objective completes
12. [ ] Display "TUTORIAL COMPLETE!" message
13. [ ] Handle Ability.None as victory trigger (per 07-MultiObjectiveProgression)

### Phase 4: Full Integration Testing

14. [ ] Test complete progression: L1 -> L2 -> L3 -> Victory
15. [ ] Test player can't reach island without lift system
16. [ ] Test full transport chain: dig -> belt -> lift -> belt -> bucket
17. [ ] Verify 1500 dirt is achievable with working transport chain

---

## Testing Checklist

### Level Setup Tests

- [ ] Floating island renders at correct position (X=100-700, Y=320-450)
- [ ] Island bucket platform renders correctly (X=150-250, Y=280-319)
- [ ] Lift zone walls render correctly (X=440-450 and X=510-520, Y=350-1200)
- [ ] Lift zone floor renders correctly (X=440-520, Y=1200-1250)
- [ ] Ground terrain (diggable area) at X=0-550, Y=1350-1619
- [ ] Bucket 3 spawns at (200, 266) on island raised platform
- [ ] Bucket shows remaining count correctly (1500 target)

### Collision Tests

- [ ] Player cannot walk through air to reach island
- [ ] Player can walk on ground surface (Y=1350)
- [ ] Player can stand on floating island surface (Y=320)
- [ ] Terrain colliders generate for island Stone blocks
- [ ] Terrain colliders generate for lift zone walls
- [ ] Player cannot walk through lift zone walls

### Structure Placement Tests

- [ ] Belts can be placed at ground level (routing to lift zone)
- [ ] Belts can be placed on floating island surface
- [ ] Lifts can ONLY be placed in Air cells (inside lift zone interior)
- [ ] Lifts cannot be placed through lift zone walls
- [ ] Adjacent lifts merge correctly (vertical stacking)
- [ ] Lift placement requires Ability.PlaceLifts (unlocked from Level 2)

### Transport Chain Tests

- [ ] Dig Ground -> produces Dirt particles
- [ ] Dirt falls onto belt -> belt moves it horizontally toward lift zone
- [ ] Dirt enters lift zone at Y~1200
- [ ] Lift applies upward force, dirt rises through zone
- [ ] Dirt exits lift zone at Y~350 with upward momentum
- [ ] Belt on island routes dirt horizontally toward bucket (~280 cells)
- [ ] Dirt reaches bucket -> collected and counted
- [ ] Progress UI updates correctly
- [ ] Objective completes at 1500 dirt

### Victory Tests

- [ ] On objective completion (1500 dirt), victory state triggers
- [ ] "TUTORIAL COMPLETE!" or "VICTORY!" notification appears
- [ ] Bucket stops collecting after completion
- [ ] Bucket displays "DONE!" text
- [ ] No new ability unlocks (Ability.None is victory marker)

---

## Visual Reference

```
Full World Layout (1920x1620 cells):

         X=0       X=480      X=960     X=1440    X=1920
         |          |          |          |          |
Y=0      +----------+----------+----------+----------+
         |                     |                     |
         |   [Bucket 3]        |        AIR          |
         |   +=======+         |                     |
Y=280    |   | Raised|         |                     |
Y=320    |===+===============+=|                     |  <- Island surface
         |  FLOATING ISLAND   |                     |
Y=450    |====================|                     |
         |         |     |    |                     |
Y=539    +---------+-----+----+----------+----------+
         |         |     |    |                     |
         |         | LIFT|    |                     |
         |         | ZONE|    |        AIR          |
         |         |walls|    |                     |
         |         |X=440|    |                     |
         |         |-520 |    |                     |
         |         |     |    |                     |
Y=1079   +---------+-----+----+----------+----------+
         |         |     |    |                     |
         |  SPAWN  |=====+    |                     |
         |  [Bucket1]    ^    |                     |
Y=1200   |         LIFT FLOOR |                     |
         |                    |                     |
Y=1350   |====================|=====================|  <- Ground surface
         |  DIGGABLE GROUND   |                     |
Y=1619   +----------+----------+----------+----------+
        X=0       X=550       X=960              X=1920

Legend:
===  Stone (floating island, lift zone walls)
+++  Ground (diggable terrain)
LIFT ZONE  Pre-built walls with Air interior (player places lifts inside)
```

### Level 3 Transport Path

```
1. Player digs at ground level (X=0-550, Y=1350-1619)
2. Dirt belts/falls to lift zone entry (X~480, Y~1200)
3. Lift(s) raise dirt through lift zone (Y=1200 -> Y=350)
4. Belt(s) route dirt across island (X=480 -> X=200)
5. Dirt falls into Bucket 3 at (200, 266)
```

---

## Edge Cases

1. **Player tries to jump to island**: Height is ~1030 cells - impossible without structures
2. **Player tries to climb lift zone walls**: Walls are solid stone, cannot pass through
3. **Lift placed outside lift zone**: Should fail - lifts only work on Air cells
4. **Lift not reaching full height**: Partial lift may slow but not fully raise material
5. **Belt gap between lift exit and bucket**: Material may pile up without complete belt routing
6. **Dirt scattered on island**: Some dirt may miss bucket without proper routing; 1500 target accounts for some loss
7. **Player places belts/lifts wrong direction**: Material flows wrong way, player must fix placement

---

## Design Principles Applied

Following the project's architecture philosophy:

- **Systems not patches**: Uses existing Belt/Lift systems, no special-case code for Level 3
- **Single source of truth**: LevelData defines all level configuration
- **No special cases**: All terrain regions use same TerrainRegion system
- **Reusable patterns**: Level3Data follows same pattern as Level1Data

---

## Files to Create/Modify

### Files Required for Level 3 (most defined in earlier plans)

Level 3 primarily depends on:
- **TutorialLevelData.cs** - Unified level data with all terrain and buckets (per 08-TutorialMapLayout)
- **Lift System** - Must be implemented per `DevPlans/Features/PLANNED-LiftSystem.md`

### Prerequisite Files (must exist first)

| File | Notes | Status |
|------|-------|--------|
| `Assets/Scripts/Simulation/Jobs/SimulateChunksJob.cs` | Add lift zone check per PLANNED-LiftSystem | EXISTS |
| `Assets/Scripts/Simulation/MaterialDef.cs` | Add LiftUp/LiftUpLight materials | EXISTS |
| `Assets/Scripts/Game/Levels/TutorialLevelData.cs` | Unified tutorial level | NEEDS CREATION |
| `DevPlans/Features/PLANNED-LiftSystem.md` | Lift system design doc | EXISTS |

### Modified Files

| File | Changes |
|------|---------|
| `Assets/Scripts/Game/GameController.cs` | Victory condition handling |
| `Assets/Scripts/Game/UI/ProgressionUI.cs` | Victory message display |

### No New Level3Data.cs

Unlike the original plan, Level 3 does NOT need a separate `Level3Data.cs`. All terrain, spawns, and objectives are defined in the unified `TutorialLevelData.cs` per 08-TutorialMapLayout.

---

## Notes

- This plan now uses the full 1920x1620 world per 08-TutorialMapLayout.
- The ~1080 cell vertical distance aligns with actual ground-to-island gap (~1030 cells).
- Pre-built lift zone walls (X=440-520) provide the vertical conduit - player places lifts inside.
- Lift system design is documented in `DevPlans/Features/PLANNED-LiftSystem.md`.
- Tutorial hints/tooltips may be needed to guide players in building their first machine.
- Level 3 is the FINAL tutorial level - completion triggers victory, not another ability unlock.
- The horizontal distance from lift exit to bucket (~280 cells) requires intentional belt routing.
