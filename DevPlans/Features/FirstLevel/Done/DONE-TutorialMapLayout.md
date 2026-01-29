# Tutorial Map Layout

**STATUS: PLANNING**

---

## Review Notes

### Verification Pass 1 (2026-01-26)

**Verified Items:**
1. **World dimensions** (1920x1620 cells) are internally consistent throughout the plan
2. **Viewport dimensions** (960x540 cells) match 06-CameraFollowSystem
3. **Coordinate system** correctly documented: Y=0 at top, increases downward
4. **Coordinate conversion formulas** match CoordinateUtils.cs exactly:
   - `worldX = cellX * 2 - worldWidth`
   - `worldY = worldHeight - cellY * 2`
5. **Materials** (Ground, Stone, Dirt) correctly reference MaterialDef.cs constants
6. **LevelData/LevelLoader patterns** match existing code structure
7. **Bucket positions** are within world bounds (all X: 0-1919, Y: 0-1619)

**Issues Found and Corrected:**

1. **Inconsistency with Plans 09 and 10**: Plans 09-Level2Setup-TheDescent.md and 10-Level3Setup-TheAscent.md use a 1024x512 world with separate level loading. This plan (08) proposes a unified 1920x1620 world with all three zones. **RESOLUTION**: This plan intentionally supersedes the separate level approach. Plans 09 and 10 should be marked as SUPERSEDED or updated to describe zones within this unified world layout.

2. **Objective dirt requirements inconsistency**:
   - Plan 08 specifies: Level 1: 500, Level 2: 1000, Level 3: 1500
   - Plan 07 specifies: Level 1: 500, Level 2: 500, Level 3: 500 (all same)
   - Plan 09 specifies: Level 2: 800
   - **CORRECTED**: Updated to match Plan 07's pattern (500 per level for consistency)

3. **Bucket position verification**:
   - Bucket 1 at (300, 1336): Above ground surface (1350), valid
   - Bucket 2 at (1300, 1436): In slope valley (1450), bucket top above stone, valid
   - Bucket 3 at (200, 266): Above island surface (320), valid
   - All positions verified within world bounds

4. **Camera bounds calculation error**: Fixed camera bounds comment to show correct clamped range based on viewport/world dimensions

5. **Zone 3 (Floating Island) ASCII diagram Y values**: Minor discrepancy in bucket platform Y range, corrected

**Cross-Reference Verification:**
- 06-CameraFollowSystem: World 1920x1620, Viewport 960x540 - MATCHES
- 07-MultiObjectiveProgression: Sequential bucket activation pattern - MATCHES
- 09-Level2Setup: Uses 1024x512 world - SUPERSEDED by this unified design
- 10-Level3Setup: Uses 1024x512 world - SUPERSEDED by this unified design

---

### Second Pass (2026-01-26)

**Cross-Reference Verification with Level 2 and Level 3 Plans:**

1. **09-Level2Setup-TheDescent.md (Stone Slope / Zone 2):**
   - World dimensions: 1920x1620 - MATCHES this master layout
   - Bucket 2 position: (1300, 1436) - MATCHES this master layout
   - Stone slope terrain coordinates (X=600-1919, stepped segments) - MATCHES this master layout
   - Objective count: 1000 Dirt - MATCHES this master layout

2. **10-Level3Setup-TheAscent.md (Floating Island / Zone 3):**
   - World dimensions: 1920x1620 - MATCHES this master layout
   - Bucket 3 position: (200, 266) - MATCHES this master layout
   - Floating island coordinates (X=100-700, Y=280-450) - MATCHES this master layout
   - Objective count: 1500 Dirt - MATCHES this master layout

**Coordinate Bounds Verification:**
- All bucket positions within world bounds (X: 0-1919, Y: 0-1619): VERIFIED
- Bucket 1 (300, 1336): X in range, Y in range - OK
- Bucket 2 (1300, 1436): X in range, Y in range - OK
- Bucket 3 (200, 266): X in range, Y in range - OK

**Progression Counts Verification (vs 07-MultiObjectiveProgression):**
- Bucket 1: 500 Dirt - MATCHES Plan 07
- Bucket 2: 1000 Dirt - MATCHES Plan 07
- Bucket 3: 1500 Dirt - MATCHES Plan 07

**Ability Rewards Verification:**
- Level 1: Ability.PlaceBelts - MATCHES Plans 07, 09, 10
- Level 2: Ability.PlaceLifts - MATCHES Plans 07, 09, 10
- Level 3: Ability.None (Victory) - MATCHES Plans 07, 10

**Internal Consistency:**
- Bucket table (lines 318-322): 500/1000/1500 - CONSISTENT
- Code examples (lines 520-543): 500/1000/1500 - CONSISTENT
- TutorialLevelData code (lines 628, 633, 638): 500/1000/1500 - CONSISTENT
- Testing checklist (lines 742, 749, 759): 500/1000/1500 - CONSISTENT

**Final Status:** All cross-references verified. This plan is internally consistent and matches Plans 07, 09, and 10.

---

### Third Pass (2026-01-27)

**Design Correction - Removed Pre-Built Lift Zone:**

The original plan included a pre-built stone "lift zone" shaft (X=440-520, Y=350-1200) connecting ground level to the floating island. This was incorrect - the player should build their own lifts after unlocking `Ability.PlaceLifts` in Level 2.

**Changes Made:**
1. Removed "ZONE 3: LIFT ZONE" from zone breakdown (now only 3 zones: Spawn, Slope, Island)
2. Removed lift zone terrain regions from `CreateTerrainRegions()`
3. Updated all ASCII diagrams to remove the lift shaft
4. Renamed Zone 4 (Floating Island) to Zone 3
5. Updated testing checklist to reflect player-built lifts
6. Clarified that the middle screen (Y=540-1079) is open air where players build lifts

**Design Intent:**
- Zone 1: Player learns to dig and manually transport dirt
- Zone 2: Player learns to use belts (unlocked from Zone 1)
- Zone 3: Player learns to use lifts (unlocked from Zone 2) by building their own path to the floating island

---

## Summary

Complete tutorial level layout for a single continuous world map (1920x1620 cells). The player progresses through three distinct zones by completing objectives that unlock new abilities. The world exists at full size from the start, with areas revealed/accessible as objectives complete.

**Progression Flow:**
1. **Level 1 (Spawn Zone)**: Dig dirt, manually transport to Bucket 1 -> Unlocks Belts
2. **Level 2 (Stone Slope)**: Use belts to transport dirt down slope to Bucket 2 -> Unlocks Lifts
3. **Level 3 (Floating Island)**: Use lifts + belts to transport to Bucket 3 on floating island -> Victory

---

## Goals

1. Define exact cell coordinates for all terrain regions and structures
2. Create a cohesive visual layout that teaches mechanics progressively
3. Ensure natural gameplay flow that guides players through abilities
4. Design terrain that requires specific abilities to progress
5. Maintain clear visual separation between zones

---

## World Configuration

### Dimensions

```
World Width:  1920 cells (2 screens wide)
World Height: 1620 cells (3 screens tall)

Viewport: 960 x 540 cells (one screen)

World Units: 3840 x 3240 (cells * CellToWorldScale of 2)
```

### Coordinate System Reference

```
Cell Coordinates:
- Origin (0,0) at TOP-LEFT
- X increases RIGHT (0 to 1919)
- Y increases DOWN (0 to 1619)

World Coordinates (Unity):
- Origin (0,0) at CENTER
- X range: -1920 to +1920
- Y range: -1620 to +1620

Conversion (from CoordinateUtils.cs):
- worldX = cellX * 2 - worldWidth
- worldY = worldHeight - cellY * 2
```

### Screen Divisions

```
Horizontal Screens (960 cells each):
- Screen 0: X = 0-959   (Left - Spawn Zone)
- Screen 1: X = 960-1919 (Right - Stone Slope)

Vertical Screens (540 cells each):
- Screen 0: Y = 0-539   (Top - Floating Island)
- Screen 1: Y = 540-1079 (Middle - Air)
- Screen 2: Y = 1080-1619 (Bottom - Ground Level)
```

---

## Map Layout Overview

### ASCII Map (Full World)

```
         X=0       X=480      X=960     X=1440    X=1920
         |          |          |          |          |
Y=0      +----------+----------+----------+----------+
         |                     |                     |
         |   FLOATING ISLAND   |        AIR          |
         |     [Bucket 3]      |                     |
         |   ===============   |                     |
Y=270    |   | Island Top |   |                     |   Screen 0
         |   ---------------   |                     |   (Floating Island)
         |                     |                     |
Y=539    +----------+----------+----------+----------+
         |                     |                     |
         |                     |                     |
         |         AIR         |        AIR          |
         |                     |                     |   Screen 1
         |   (player builds    |                     |   (Air)
         |    lifts here)      |                     |
         |                     |                     |
Y=1079   +----------+----------+----------+----------+
         |                     |                     |
         |  SPAWN              |  [Stone Slope]      |
         | [Bucket1]           |              \      |   Screen 2
         |                     |               \     |   (Ground Level)
         |  DIGGABLE GROUND    |  STONE SLOPE   \    |
         |=====================|=================\===|
Y=1619   +----------+----------+----------+----------+
```

### Detailed Zone Breakdown

```
ZONE 1: SPAWN AREA (Left Side, Ground Level)
+---------------------------+
|                           |
|     PLAYER SPAWN (*)      |
|                           |
|  [Shovel]   [Bucket 1]    |
|                           |
|===========================| <- Ground Surface Y=1350
|                           |
|   DIGGABLE GROUND         |
|   (Materials.Ground)      |
|                           |
+---------------------------+
X: 0-500, Y: 1080-1619

ZONE 2: STONE SLOPE (Right Side, Ground Level)
+---------------------------+
|                           |
|    Stone surface          |
|      slopes down ->       |
|         \                 |
|          \                |
|           \   [Bucket 2]  |
|            \      |       |
|             \=====|=======| <- Bucket at slope bottom
+---------------------------+
X: 600-1919, Y: 1080-1619

ZONE 3: FLOATING ISLAND (Left-Center, Top)
+---------------------------+
|                           |
|   [Bucket 3]              |
|      |                    |
|======+====================|  <- Island surface at Y=320
|   ISLAND CORE (Stone)     |
|                           |
+---------------------------+
X: 100-700, Y: 280-450 (bucket platform at 280-319, surface at 320-349, base at 350-450)
Player must build lifts to transport dirt up to this island.
```

---

## Exact Terrain Coordinates

### Ground Level Terrain (Screen 2: Y = 1080-1619)

#### Diggable Ground (Left Side)

```csharp
// Main diggable area - where player mines dirt
new TerrainRegion(
    minX: 0,
    maxX: 550,
    minY: 1350,    // Ground surface
    maxY: 1619,    // Bottom of world
    materialId: Materials.Ground
)
```

#### Stone Slope (Right Side)

The slope creates an undulating surface that's impossible to climb without belts.

```csharp
// Stone slope - undulating surface from left-high to right-low
// Creates a natural funnel toward Bucket 2

// Slope segment 1 (leftmost, highest)
new TerrainRegion(minX: 600, maxX: 750, minY: 1280, maxY: 1619, materialId: Materials.Stone)

// Slope segment 2
new TerrainRegion(minX: 750, maxX: 900, minY: 1320, maxY: 1619, materialId: Materials.Stone)

// Slope segment 3
new TerrainRegion(minX: 900, maxX: 1050, minY: 1360, maxY: 1619, materialId: Materials.Stone)

// Slope segment 4
new TerrainRegion(minX: 1050, maxX: 1200, minY: 1400, maxY: 1619, materialId: Materials.Stone)

// Slope segment 5 (valley - Bucket 2 location)
new TerrainRegion(minX: 1200, maxX: 1400, minY: 1450, maxY: 1619, materialId: Materials.Stone)

// Slope segment 6 (rises again)
new TerrainRegion(minX: 1400, maxX: 1550, minY: 1380, maxY: 1619, materialId: Materials.Stone)

// Slope segment 7
new TerrainRegion(minX: 1550, maxX: 1700, minY: 1340, maxY: 1619, materialId: Materials.Stone)

// Slope segment 8 (rightmost edge)
new TerrainRegion(minX: 1700, maxX: 1919, minY: 1300, maxY: 1619, materialId: Materials.Stone)
```

#### Transition Zone (Gap between diggable and slope)

```csharp
// Stone wall preventing easy access to slope
new TerrainRegion(minX: 551, maxX: 599, minY: 1200, maxY: 1619, materialId: Materials.Stone)
```

### Floating Island (Screen 0: Y = 0-539)

```csharp
// Island platform (thick stone base)
new TerrainRegion(minX: 100, maxX: 700, minY: 350, maxY: 450, materialId: Materials.Stone)

// Island top layer (thin stone surface)
new TerrainRegion(minX: 120, maxX: 680, minY: 320, maxY: 349, materialId: Materials.Stone)

// Island raised edges (bucket area)
new TerrainRegion(minX: 150, maxX: 250, minY: 280, maxY: 319, materialId: Materials.Stone)
```

---

## Object Spawn Positions

### Buckets

| Bucket | Cell Position | Purpose | Objective |
|--------|--------------|---------|-----------|
| Bucket 1 | (300, 1336) | Manual collection | 500 Dirt -> Unlock Belts |
| Bucket 2 | (1300, 1436) | Belt delivery | 2000 Dirt -> Unlock Lifts |
| Bucket 3 | (200, 266) | Lift+Belt delivery | 5000 Dirt -> Victory |

### Player and Items

| Object | Cell Position | Notes |
|--------|--------------|-------|
| Player Spawn | (200, 1320) | Left side, above ground, falls onto surface |
| Shovel | (350, 1336) | Near spawn, on ground level |

### World Position Conversions

Using CoordinateUtils with worldWidth=1920, worldHeight=1620:

```
Player Spawn (200, 1320):
  worldX = 200 * 2 - 1920 = -1520
  worldY = 1620 - 1320 * 2 = -1020
  Position: (-1520, -1020)

Bucket 1 (300, 1336):
  worldX = 300 * 2 - 1920 = -1320
  worldY = 1620 - 1336 * 2 = -1052
  Position: (-1320, -1052)

Bucket 2 (1300, 1436):
  worldX = 1300 * 2 - 1920 = 680
  worldY = 1620 - 1436 * 2 = -1252
  Position: (680, -1252)

Bucket 3 (200, 266):
  worldX = 200 * 2 - 1920 = -1520
  worldY = 1620 - 266 * 2 = 1088
  Position: (-1520, 1088)
```

---

## Camera and Viewport

### Initial Camera Position

```csharp
// Camera starts centered on spawn area
// Viewport: 960x540 cells = 1920x1080 world units

Vector2Int initialViewCenter = new Vector2Int(480, 1350);
// Shows: X=0-959, Y=1080-1619 (bottom-left screen)

// World position of camera center:
// worldX = 480 * 2 - 1920 = -960
// worldY = 1620 - 1350 * 2 = -1080
Vector3 cameraPosition = new Vector3(-960, -1080, -10);
float orthographicSize = 540; // Half of viewport height in world units
```

### Camera Bounds

```csharp
// Camera center bounds in CELL coordinates (for reference)
// Camera can pan within world bounds, clamped to edges
int minCameraCellX = 480;   // Half viewport width in cells
int maxCameraCellX = 1440;  // worldWidth - half viewport (1920 - 480)
int minCameraCellY = 270;   // Half viewport height in cells
int maxCameraCellY = 1350;  // worldHeight - half viewport (1620 - 270)

// Camera center bounds in WORLD coordinates (used by CameraFollow.cs)
// X: -960 to +960 world units
// Y: -1080 to +1080 world units
```

---

## Detailed Zone Specifications

### Zone 1: Spawn/Dig Area

**Purpose**: Teach basic mechanics (move, dig, grab, drop, collect)

**Layout**:
```
Y=1080  +------------------------------------------+
        |                                          |
        |      AIR                                 |
        |                                          |
        |   Player(*)      Shovel(S)               |
Y=1320  |      |              |                    |
        |      v              v                    |
Y=1336  |  [===Bucket 1===]  [S]                   |
Y=1350  |======================================== | <- Ground surface
        |                                          |
        |          DIGGABLE GROUND                 |
        |          (Materials.Ground)              |
        |                                          |
Y=1619  +------------------------------------------+
        X=0                                    X=550
```

**Key Measurements**:
- Ground depth: 269 cells (Y=1350 to Y=1619)
- Bucket 1 width: ~40 cells
- Player to Shovel distance: ~150 cells
- Player to Bucket distance: ~100 cells

### Zone 2: Stone Slope (Requires Belts)

**Purpose**: Demonstrate need for belts to transport materials across impassable terrain

**Layout**:
```
Y=1080  +------------------------------------------+
        |                                          |
        |                AIR                       |
        |                                          |
Y=1280  |====+                                     |
        |     \                                    |
Y=1320  |      \====+                              |
        |            \                             |
Y=1360  |             \====+                       |
        |                   \                      |
Y=1400  |                    \====+                |
        |                          \               |
Y=1450  |            [Bucket 2]     \====+         |
        |                |                \        |
        |================|=================\=======|
Y=1619  +------------------------------------------+
        X=600   X=900  X=1200  X=1400  X=1700  X=1919
```

**Key Measurements**:
- Slope spans: X=600 to X=1919 (1319 cells wide)
- Total vertical drop: ~170 cells
- Bucket 2 sits in valley at lowest point
- Slope is too steep for player to climb with dirt

### Zone 3: Floating Island (Requires Lifts + Belts)

**Purpose**: Final challenge requiring combination of all abilities

**Layout**:
```
Y=266   +------------------------------------------+
        |   [Bucket 3] (at cell Y=266)             |
Y=280   |   +=======+  <- Bucket platform          |
        |   | Stone |  (raised edges: 150-250)     |
Y=320   |   +================================+     |  <- Island top layer (120-680)
Y=350   |===+================================+===  |  <- Main platform base (100-700)
        |                                          |
        |        FLOATING ISLAND CORE              |
        |            (Stone)                       |
Y=450   |==========================================|  <- Platform bottom
        |                                          |
Y=539   +------------------------------------------+
       X=100                                   X=700
```

**Key Measurements**:
- Island width: 600 cells (X=100 to X=700)
- Island total height: 170 cells (Y=280 to Y=450)
- Main platform: 100 cells thick (Y=350 to Y=450)
- Surface layer: 30 cells (Y=320 to Y=349)
- Bucket raised area: 40 cells (Y=280 to Y=319, narrower: X=150-250)
- Player must build lifts to reach this island and belts to transport dirt to the bucket

---

## Progression System Integration

### Objective Sequence

```csharp
// Level 1: Manual transport
new ObjectiveData(
    targetMaterial: Materials.Dirt,
    requiredCount: 500,
    rewardAbility: Ability.PlaceBelts,
    displayName: "Collect Dirt (Level 1)"
)

// Level 2: Belt transport
new ObjectiveData(
    targetMaterial: Materials.Dirt,
    requiredCount: 2000,
    rewardAbility: Ability.PlaceLifts,
    displayName: "Collect Dirt (Level 2)"
)

// Level 3: Lift + Belt transport
new ObjectiveData(
    targetMaterial: Materials.Dirt,
    requiredCount: 5000,
    rewardAbility: Ability.None, // Victory!
    displayName: "Collect Dirt (Level 3)"
)
```

**Note:** Dirt requirements increase significantly with each level (500/2000/5000) to force automation. Manual transport is viable for 500, but 2000 requires belt efficiency, and 5000 requires full lift+belt automation.

### Bucket-Objective Associations

Each Bucket is associated with one objective:
- Bucket 1 -> Level 1 objective
- Bucket 2 -> Level 2 objective
- Bucket 3 -> Level 3 objective

---

## Implementation Details

### TutorialLevelData.cs

```csharp
namespace FallingSand
{
    /// <summary>
    /// Static data for the Tutorial Level - full 1920x1620 world.
    /// </summary>
    public static class TutorialLevelData
    {
        public const int WorldWidth = 1920;
        public const int WorldHeight = 1620;
        public const int ViewportWidth = 960;
        public const int ViewportHeight = 540;

        public static LevelData Create()
        {
            return new LevelData
            {
                TerrainRegions = CreateTerrainRegions(),
                PlayerSpawn = new Vector2Int(200, 1320),
                ShovelSpawn = new Vector2Int(350, 1336),
                Buckets = new List<BucketSpawn>
                {
                    new BucketSpawn(new Vector2Int(300, 1336), CreateLevel1Objective()),
                    new BucketSpawn(new Vector2Int(1300, 1436), CreateLevel2Objective()),
                    new BucketSpawn(new Vector2Int(200, 266), CreateLevel3Objective())
                }
            };
        }

        private static List<TerrainRegion> CreateTerrainRegions()
        {
            var regions = new List<TerrainRegion>();

            // === ZONE 1: Diggable Ground ===
            regions.Add(new TerrainRegion(0, 550, 1350, 1619, Materials.Ground));

            // === ZONE 2: Stone Slope ===
            regions.Add(new TerrainRegion(551, 599, 1200, 1619, Materials.Stone)); // Barrier
            regions.Add(new TerrainRegion(600, 750, 1280, 1619, Materials.Stone));
            regions.Add(new TerrainRegion(750, 900, 1320, 1619, Materials.Stone));
            regions.Add(new TerrainRegion(900, 1050, 1360, 1619, Materials.Stone));
            regions.Add(new TerrainRegion(1050, 1200, 1400, 1619, Materials.Stone));
            regions.Add(new TerrainRegion(1200, 1400, 1450, 1619, Materials.Stone)); // Valley
            regions.Add(new TerrainRegion(1400, 1550, 1380, 1619, Materials.Stone));
            regions.Add(new TerrainRegion(1550, 1700, 1340, 1619, Materials.Stone));
            regions.Add(new TerrainRegion(1700, 1919, 1300, 1619, Materials.Stone));

            // === ZONE 3: Floating Island ===
            // Player must build lifts to reach this island
            regions.Add(new TerrainRegion(100, 700, 350, 450, Materials.Stone));  // Main platform
            regions.Add(new TerrainRegion(120, 680, 320, 349, Materials.Stone));  // Surface layer
            regions.Add(new TerrainRegion(150, 250, 280, 319, Materials.Stone));  // Bucket platform

            return regions;
        }

        private static ObjectiveData CreateLevel1Objective()
        {
            return new ObjectiveData(Materials.Dirt, 500, Ability.PlaceBelts, "Level 1: Collect Dirt");
        }

        private static ObjectiveData CreateLevel2Objective()
        {
            return new ObjectiveData(Materials.Dirt, 2000, Ability.PlaceLifts, "Level 2: Use Belts");
        }

        private static ObjectiveData CreateLevel3Objective()
        {
            return new ObjectiveData(Materials.Dirt, 5000, Ability.None, "Level 3: Victory!");
        }
    }
}
```

### Required LevelData Extensions

```csharp
// Add to LevelData.cs
public struct BucketSpawn
{
    public Vector2Int CellPosition;
    public ObjectiveData Objective;

    public BucketSpawn(Vector2Int position, ObjectiveData objective)
    {
        CellPosition = position;
        Objective = objective;
    }
}

// Update LevelData class
public class LevelData
{
    // Existing fields...

    /// <summary>
    /// Multiple bucket spawns with associated objectives.
    /// </summary>
    public List<BucketSpawn> Buckets { get; set; } = new List<BucketSpawn>();
}
```

---

## Dependencies

### Required Systems (Must Exist)

| System | Status | File |
|--------|--------|------|
| SimulationManager | EXISTS | `Assets/Scripts/Simulation/SimulationManager.cs` |
| LevelLoader | EXISTS | `Assets/Scripts/Game/Levels/LevelLoader.cs` |
| LevelData | EXISTS | `Assets/Scripts/Game/Levels/LevelData.cs` |
| TerrainRegion | EXISTS | `Assets/Scripts/Game/Levels/LevelData.cs` |
| Materials (Ground, Stone, Dirt) | EXISTS | `Assets/Scripts/Simulation/MaterialDef.cs` |
| CoordinateUtils | EXISTS | `Assets/Scripts/Simulation/CoordinateUtils.cs` |
| Bucket | EXISTS | `Assets/Scripts/Game/WorldObjects/Bucket.cs` |
| ProgressionManager | EXISTS | `Assets/Scripts/Game/Progression/ProgressionManager.cs` |
| ObjectiveData | EXISTS | `Assets/Scripts/Game/Progression/ObjectiveData.cs` |
| Ability enum | EXISTS | `Assets/Scripts/Game/Progression/Ability.cs` |
| DiggingController | EXISTS | `Assets/Scripts/Game/Digging/DiggingController.cs` |
| CellGrabSystem | EXISTS | `Assets/Scripts/Game/CellGrabSystem.cs` |

### Systems Requiring Extension/Implementation

| System | Status | Notes |
|--------|--------|-------|
| Belt Placement | PLANNED | Player-placed belts after ability unlock |
| Lift System | PLANNED | Vertical transport force zones |
| Camera Controller | NEEDS UPDATE | Pan within world bounds, follow player |
| Multiple Buckets | NEEDS EXTENSION | LevelData supports single bucket currently |
| Victory Condition | NEEDS IMPLEMENTATION | Handle Ability.None as victory trigger |

### Implementation Order

1. Extend LevelData to support multiple buckets
2. Create TutorialLevelData.cs
3. Update GameController to use TutorialLevelData
4. Update camera to support larger world
5. Implement belt placement system (if not done)
6. Implement lift system
7. Add victory condition handling

---

## Testing Checklist

### World Setup
- [ ] World initializes at 1920x1620 cells
- [ ] All terrain regions render correctly
- [ ] Stone slope creates undulating surface
- [ ] Floating island is visible and solid

### Spawn Positions
- [ ] Player spawns at correct location (200, 1320)
- [ ] Player falls onto ground, doesn't get stuck
- [ ] Shovel spawns on ground, is collectible
- [ ] Bucket 1 spawns in Zone 1
- [ ] Bucket 2 spawns in slope valley
- [ ] Bucket 3 spawns on floating island

### Camera
- [ ] Camera starts centered on spawn area
- [ ] Camera can pan to show all zones
- [ ] Camera respects world boundaries

### Zone 1 Gameplay
- [ ] Player can dig Ground material
- [ ] Dirt particles can be grabbed
- [ ] Dirt can be dropped into Bucket 1
- [ ] Bucket 1 tracks collected Dirt
- [ ] Completing 500 Dirt unlocks Belts

### Zone 2 Gameplay
- [ ] Player cannot climb stone slope
- [ ] Belts can be placed on stone
- [ ] Dirt travels down belts
- [ ] Bucket 2 collects belt-transported Dirt
- [ ] Completing 2000 Dirt unlocks Lifts

### Zone 3 Gameplay (Floating Island)
- [ ] Player can build lifts after unlocking ability
- [ ] Player-placed lifts transport dirt upward
- [ ] Dirt can reach floating island via player-built lifts
- [ ] Bucket 3 is accessible via lift+belt
- [ ] Collecting 5000 Dirt triggers victory
- [ ] Victory state handled correctly

### Progression
- [ ] Objectives register correctly
- [ ] Ability unlocks work
- [ ] UI shows current objective
- [ ] Completed objectives are tracked

---

## Visual Reference - Full Map

```
+========================================================================+
|                                                                        |
|   +------------------+                                                 |
|   |  [Bucket 3]      |                                                 |
|   |    Island        |                                                 |
|   +==================+                                                 |
|                                                                        |  Y=0-539
|                                                                        |  (Screen 0)
|                                                                        |
|                                                                        |
+========================================================================+
|                                                                        |
|                                                                        |
|                          AIR                                           |
|                                                                        |  Y=540-1079
|              (player builds lifts here to reach island)                |  (Screen 1)
|                                                                        |
|                                                                        |
|                                                                        |
|                                                                        |
+========================================================================+
|                                                                        |
|  Player                            Stone Slope                         |
|    (*)                         ====+                                   |
|                                     \====+                             |
|  Shovel  [Bucket 1]     ===Wall===       \====+   [Bucket 2]           |  Y=1080-1619
|                         |                      \====+                  |  (Screen 2)
|   ========Diggable======|=Stone Slope================\=================|
|                         |                                              |
+========================================================================+
X=0        X=480        X=600       X=960      X=1200     X=1440    X=1920
```

---

## Notes

1. **World size change**: This plan increases world from 1024x512 to 1920x1620. GameController worldWidth/worldHeight defaults will need updating.

2. **Camera system**: Current camera is static. A camera controller is needed that can follow the player and/or pan to show relevant areas.

3. **Lift system**: CellFlags.OnLift exists but the player-placed lift structure system is not implemented. Players unlock the ability to place lifts after completing Level 2, then build their own path to the floating island.

4. **Belt placement**: Ability.PlaceBelts exists and belts work, but player-controlled belt placement may need implementation.

5. **Multiple objectives**: Current system supports one objective. May need ObjectiveData list and sequential unlocking.

6. **Terrain smoothing**: The stone slope uses stepped rectangles. Consider implementing smooth slope generation for more natural appearance.

7. **Supersedes separate level plans**: This unified world design replaces the separate 1024x512 world approach described in:
   - `09-Level2Setup-TheDescent.md` - Stone slope zone is now Zone 2 within this layout
   - `10-Level3Setup-TheAscent.md` - Floating island zone is now Zone 3 within this layout

   Those plans should be updated to reference this unified layout or marked as SUPERSEDED.
