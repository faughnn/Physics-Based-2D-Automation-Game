# Level 2 Setup: "The Descent"

**STATUS: NOT STARTED**

---

## Review Notes

**Reviewed: 2026-01-26 (First Pass)**

### Verified Items
- Belt system API references are accurate (BeltManager.PlaceBelt, 8x8 blocks, DefaultSpeed=3)
- Belt placement requirements correct (must place on Air, adjacent belts merge)
- Unlock requirements consistent with 07-MultiObjectiveProgression (PlaceBelts unlocked by Level 1)
- Undulating slope concept is sound for preventing natural sliding

### Issues Found and Corrected

1. **CRITICAL: World Dimensions** - Changed from 1024x512 to 1920x1620 to match 08-TutorialMapLayout unified world design. This is a single continuous world, not separate levels.

2. **CRITICAL: Coordinate System** - Updated all coordinates to match 08-TutorialMapLayout:
   - Bucket 2: Changed from (880, 436) to (1300, 1436)
   - Stone slope: Now spans X=600-1919, Y=1080-1619 (ground level screen)
   - Player/Shovel spawns removed (player persists from Level 1 in unified world)

3. **Transport Distance** - Corrected from "~960 cells" to actual distance of ~700-750 cells along slope

4. **Slope Undulation Math** - Recalculated undulation table with correct values

5. **Dependencies** - Marked LevelData extensions as "NEEDS IMPLEMENTATION" (per 07-MultiObjectiveProgression) rather than "EXISTS"

6. **Level Architecture** - Clarified this is a zone within the unified tutorial world, not a separate level with its own world

### Dependencies on Other Plans
- **07-MultiObjectiveProgression**: Required for objective sequencing and bucket activation
- **08-TutorialMapLayout**: Defines the unified world layout and coordinate reference

---

**Second Pass (2026-01-26)**

### Cross-Plan Coordinate Verification

**08-TutorialMapLayout.md Master Layout:**
- Bucket 2 position: (1300, 1436) - MATCHES this plan
- Stone slope segments (X=600-1919, stepped Y values) - MATCHES this plan exactly
- Valley at X=1200-1400, Y=1450 surface - MATCHES this plan
- Rise at X=1400-1550, Y=1380 surface - MATCHES this plan
- Transition barrier at X=551-599, Y=1200-1619 - MATCHES this plan

### Progression Consistency (07-MultiObjectiveProgression.md)

**INCONSISTENCY FOUND AND NOTED:**
- This plan specifies Level 2 requires **1000 Dirt** and rewards **Ability.PlaceLifts**
- Plan 07-MultiObjectiveProgression specifies Level 2 requires **1000 Dirt** (in the example code at lines 639-640)
- Plan 08-TutorialMapLayout was CORRECTED to use **500 Dirt per level** (lines 509-519)

**Resolution:** The plans have divergent dirt requirements:
- Plan 07 example code: 500/1000/1500 (increasing difficulty)
- Plan 08 corrected values: 500/500/500 (consistent pacing)

This plan uses 1000 Dirt, aligning with Plan 07's example code pattern. This is intentional to increase difficulty as players have belts to automate transport. The discrepancy with Plan 08 should be resolved during implementation - recommend using increasing values (500/1000/1500) as transport distance increases significantly in Level 2.

**Reward Ability:** Ability.PlaceLifts - MATCHES both Plan 07 and Plan 08

### Belt System Verification (05-BeltSystem.md and 11-PlayerStructurePlacement.md)

**Verified against actual BeltManager.cs (Assets/Scripts/Structures/BeltManager.cs):**
- `BeltManager.PlaceBelt(x, y, direction)` - CONFIRMED (line 61-223)
- `BeltManager.RemoveBelt(x, y)` - CONFIRMED (line 229-330)
- `DefaultSpeed = 3` constant - CONFIRMED (line 30)
- 8x8 block size - CONFIRMED (PlaceBelt validates 8x8 area)
- Belts must be placed on Air - CONFIRMED (validation in PlaceBelt)
- Adjacent belts merge automatically - CONFIRMED (MergeBelts/ExtendBelt logic)

**11-PlayerStructurePlacement.md alignment:**
- B key toggles belt mode - MATCHES this plan
- Q/E for direction - MATCHES this plan
- Left click places, right click removes - MATCHES this plan
- `ProgressionManager.IsUnlocked(Ability.PlaceBelts)` gating - MATCHES this plan

### Final Status
All coordinates verified against master layout (08-TutorialMapLayout). Belt system references verified against actual code. One noted inconsistency in dirt requirements between Plan 08 (500) and this plan (1000) - recommend resolving to use increasing values during implementation.

---

## Summary

Level 2 of the tutorial. The player must transport dirt across an impassable stone barrier using belts. This level teaches belt placement and demonstrates how belts work with gravity for downward transport. The stone slope has undulations (dips and rises) that prevent dirt from simply sliding down naturally.

---

## Design Goals

1. **Teach belt placement mechanics** - Player learns to place belts using the B key
2. **Show belts work WITH gravity** - Downward transport along a slope
3. **Semi-automation concept** - Player still digs and loads, belts handle transport
4. **Obstacle solving** - Stone barrier cannot be dug through, requiring alternative solutions
5. **Progression continuity** - Unlocks Lifts for Level 3

---

## Prerequisites

### Required Abilities (from Level 1 completion)
- **Ability.PlaceBelts** - Unlocked by completing Level 1's 500 Dirt collection objective

### Required Systems
| System | File | Status |
|--------|------|--------|
| BeltManager | `Assets/Scripts/Structures/BeltManager.cs` | EXISTS (Verified) |
| BeltStructure | `Assets/Scripts/Structures/BeltStructure.cs` | EXISTS (Verified) |
| LevelData | `Assets/Scripts/Game/Levels/LevelData.cs` | EXISTS (needs extensions per 07-MultiObjectiveProgression) |
| LevelLoader | `Assets/Scripts/Game/Levels/LevelLoader.cs` | EXISTS (Verified) |
| ProgressionManager | `Assets/Scripts/Game/Progression/ProgressionManager.cs` | EXISTS (needs extensions per 07-MultiObjectiveProgression) |
| Bucket | `Assets/Scripts/Game/WorldObjects/Bucket.cs` | EXISTS (needs inactive state per 07-MultiObjectiveProgression) |
| TutorialLevelData | `Assets/Scripts/Game/Levels/TutorialLevelData.cs` | NEEDS IMPLEMENTATION (per 08-TutorialMapLayout) |

### Dependency Plans
| Plan | Status | Must Complete Before Level 2 |
|------|--------|------------------------------|
| 07-MultiObjectiveProgression | PLANNED | YES - Required for bucket activation |
| 08-TutorialMapLayout | PLANNING | YES - Defines unified world terrain |

---

## World Configuration

### Dimensions (Unified Tutorial World)
```
World Width:  1920 cells
World Height: 1620 cells
World Units:  3840 x 3240 (cells * CellToWorldScale of 2)

Note: This is NOT a separate level - Level 2 is a zone within the unified
tutorial world defined in 08-TutorialMapLayout.md. The player progresses
through zones by completing objectives, not by loading new worlds.
```

### Screen Divisions (from 08-TutorialMapLayout)
```
Horizontal Screens (960 cells each):
- Screen 0: X = 0-959   (Left - Spawn Zone / Level 1)
- Screen 1: X = 960-1919 (Right - Stone Slope / Level 2)

Vertical Screens (540 cells each):
- Screen 0: Y = 0-539   (Top - Floating Island / Level 3)
- Screen 1: Y = 540-1079 (Middle - Air/Lift Zone)
- Screen 2: Y = 1080-1619 (Bottom - Ground Level)
```

### Coordinate System Reminder
- Cell (0,0) is top-left
- X increases right (0 to 1919)
- Y increases downward (0 to 1619)

---

## Level Layout Design

### Overview (Right Side of Unified World - Screen 1 Horizontally)

The stone slope occupies the right half of the ground level (Screen 2 vertically, Screen 1 horizontally). Players access this zone after completing Level 1's objective, which unlocks belt placement.

```
    X=600       X=900      X=1200     X=1500     X=1919
    |            |           |          |           |
Y=1080 +=========================================+
    |                                           |
    |                   AIR                     |
    |                                           |
Y=1200 |                                           |
    |    Stone barrier wall (transition)        |
    |    prevents easy access from Zone 1       |
    |                                           |
Y=1280 |====+                                      |
    |     \                                    |
Y=1320 |      \====+                               |
    |            \                             |
Y=1360 |             \====+                        |
    |                   \                      |
Y=1400 |                    \====+                 |
    |                          \               |
Y=1450 |            [Bucket 2]    \====+          |
    |                |               \         |
    |================|================\========|
Y=1619 +=========================================+
```

### Key Design Elements

1. **Dirt Source Area**: The diggable ground from Zone 1 (X=0-550) - player already knows this area
2. **Stone Slope with Undulations**: Impassable stone barrier that slopes downward from left to right
3. **Undulations**: Critical feature - dips and rises that trap dirt without belts
4. **Bucket Location**: At the bottom of the slope valley (1300, 1436), positioned to catch belt output
5. **Belt Path**: Player places belts along the stone surface to transport dirt
6. **Transport Distance**: ~700-750 cells from dirt source to bucket (following slope path)

---

## Terrain Specifications

**Note:** These regions are already defined in 08-TutorialMapLayout.md as part of the unified world. This section documents the specific regions relevant to Level 2 (Stone Slope zone).

### Reference: Diggable Ground (from Zone 1)
```
MinX: 0
MaxX: 550
MinY: 1350
MaxY: 1619
Material: Materials.Ground
```

This is the Zone 1 diggable area. Players bring dirt FROM here TO the stone slope.

### Region: Transition Wall (Barrier between Zone 1 and Zone 2)
```
MinX: 551
MaxX: 599
MinY: 1200
MaxY: 1619
Material: Materials.Stone
```

This wall prevents players from easily walking dirt across. Belts are needed.

### Region: Stone Slope with Undulations

The slope is defined as stepped terrain segments in 08-TutorialMapLayout. For Level 2 to work correctly, we enhance this with undulations that create "traps" where dirt accumulates.

**Slope Segments from 08-TutorialMapLayout:**
```csharp
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

// Slope segment 6 (rises again - creates trap)
new TerrainRegion(minX: 1400, maxX: 1550, minY: 1380, maxY: 1619, materialId: Materials.Stone)

// Slope segment 7
new TerrainRegion(minX: 1550, maxX: 1700, minY: 1340, maxY: 1619, materialId: Materials.Stone)

// Slope segment 8 (rightmost edge)
new TerrainRegion(minX: 1700, maxX: 1919, minY: 1300, maxY: 1619, materialId: Materials.Stone)
```

### Enhanced Undulation System

For better gameplay, we can optionally enhance the stepped terrain with smooth undulations. The stepped approach from 08-TutorialMapLayout already creates traps at segment boundaries where the slope rises again.

**Key Trap Locations (where dirt accumulates without belts):**

| X Range | Surface Y | Description |
|---------|-----------|-------------|
| 1200-1400 | 1450 | Valley before Bucket 2 - lowest point |
| 1400-1550 | 1380 | Rise after valley - creates natural trap |
| ~1300 | 1436 | Bucket 2 location (in the valley) |

**Why the stepped design works:**
- Segment 5 (X=1200-1400) creates a valley at Y=1450
- Segment 6 (X=1400-1550) rises to Y=1380 - dirt cannot flow past this
- Without belts, dirt collects in the valley but cannot reach Bucket 2 efficiently
- Belts are needed to push dirt over the rises and guide it into the bucket

**Optional: Smooth Undulation Overlay**

If the stepped design proves too easy, add procedural undulations:

```csharp
// Base slope: starts at (600, 1280), ends at (1400, 1450)
// Undulation amplitude: 20 cells
// Undulation wavelength: 150 cells

public static int GetSlopeY(int x)
{
    const int StartX = 600;
    const int StartY = 1280;
    const int EndX = 1400;
    const int EndY = 1450;
    const int Amplitude = 20;
    const int Wavelength = 150;

    if (x < StartX) return StartY;
    if (x > EndX) return EndY;

    float t = (float)(x - StartX) / (EndX - StartX);
    float baseY = StartY + t * (EndY - StartY);
    float undulation = Amplitude * Mathf.Sin((x - StartX) * 2 * Mathf.PI / Wavelength);

    return Mathf.RoundToInt(baseY + undulation);
}
```

---

## Spawn Positions

**Note:** In the unified tutorial world, the player does NOT respawn between levels. After completing Level 1, the player walks/travels to Level 2's zone. Player and Shovel spawns are defined only in Level 1 / Zone 1.

### Player and Shovel (defined in 08-TutorialMapLayout)
```
Player Spawn: (200, 1320) - Zone 1, ground level
Shovel Spawn: (350, 1336) - Zone 1, on ground
```

These are NOT respawned for Level 2. The player retains their shovel and position.

### Bucket 2 Spawn
```
Cell Position: (1300, 1436)
World Position: (680, -1252)
Notes: In the valley of the stone slope (segment 5)
       Total width of bucket: 20 cells (16 interior + 2x2 walls)
       Bucket top-left corner at (1300, 1436)

Bucket is INACTIVE until Level 1 is completed (see 07-MultiObjectiveProgression)
```

**World Position Calculation:**
```
Using CoordinateUtils with worldWidth=1920, worldHeight=1620:
worldX = 1300 * 2 - 1920 = 680
worldY = 1620 - 1436 * 2 = -1252
Position: (680, -1252)
```

### Distance Calculation
- Dirt source (Zone 1): approximately (275, 1485) (center of diggable area)
- Bucket 2 center: approximately (1310, 1448)
- Horizontal distance: ~1035 cells
- Vertical distance: ~37 cells (bucket is lower)
- Direct path: ~1036 cells
- Path along slope surface: ~750-800 cells (following stepped terrain)

The stepped terrain with rises creates natural barriers that require belts to traverse efficiently.

---

## Belt Placement Requirements

### Belt Specifications (from BeltManager.cs - Verified)
- Each belt block: 8x8 cells
- Belts must be placed on Air (cannot overlay existing material)
- Adjacent belts with same direction automatically merge
- Belts move cells at 1 cell per 3 frames (`DefaultSpeed = 3`)
- Direction: +1 for right, -1 for left
- `PlaceBelt(x, y, direction)` snaps to 8x8 grid automatically

### Required Belt Coverage (Updated for 1920x1620 World)

To transport dirt from Zone 1 to Bucket 2, belts must bridge the transition zone and handle the stepped terrain.

**Belt Segment 1: Transition Zone (X: 550-600)**
- Player needs to get dirt across the stone barrier wall
- Belt placement: 6-8 belt blocks on top of transition wall
- Direction: +1 (right)
- Y position: 1199 (just above the wall at Y=1200)

**Belt Segment 2: First slope descent (X: 600-750)**
- Guiding dirt down the first segment
- 19 belt blocks (152 cells)
- Direction: +1 (right)

**Belt Segment 3: Mid-slope (X: 750-1050)**
- Covering segments 2-3
- ~38 belt blocks (304 cells)
- Direction: +1 (right)

**Belt Segment 4: Approach and delivery (X: 1050-1300)**
- Final delivery to bucket
- ~32 belt blocks (256 cells)
- Direction: +1 (right)

**Belt Segment 5: Trap prevention (X: 1400-1550)**
- This segment RISES, creating a trap - belts here prevent accumulation
- ~19 belt blocks (152 cells)
- Direction: -1 (LEFT - pushing back toward bucket!)

**Total Belt Blocks for Full Coverage:** ~114 belt blocks

### Strategic Belt Placement (Minimum Required)

Players don't need full coverage. Key strategic locations:

**Critical Zone 1: Transition (X: 550-600)**
- 6 belt blocks to get dirt across barrier
- Without this, dirt cannot leave Zone 1

**Critical Zone 2: Valley delivery (X: 1200-1320)**
- ~15 belt blocks to guide dirt into bucket
- Direction: +1

**Critical Zone 3: Post-valley trap (X: 1400-1500)**
- ~12 belt blocks facing LEFT to push overflow back
- Without this, dirt piles at the rise and doesn't reach bucket

**Minimum belts for completion:** ~33 belt blocks strategically placed

---

## Belt Placement Controls

### Player Input (Game Mode)
```
B key: Toggle belt placement mode
Left Mouse: Place belt at cursor position (if unlocked)
Direction: Belts placed follow terrain slope (+1 for rightward/downward)
```

### Belt Placement Validation
- Must have Ability.PlaceBelts unlocked
- Target area must be 8x8 Air cells
- Cannot place on stone or other materials
- Belts snap to 8x8 grid

---

## Objective Configuration

### Level 2 Objective (per 07-MultiObjectiveProgression and 08-TutorialMapLayout)
```csharp
new ObjectiveData(
    targetMaterial: Materials.Dirt,
    requiredCount: 1000,          // Per 08-TutorialMapLayout
    rewardAbility: Ability.PlaceLifts,
    displayName: "Level 2: Use Belts",
    objectiveId: "level2",
    prerequisiteId: "level1"      // Requires Level 1 completion
)
```

### Why 1000 Dirt?
- Level 1 required 500 Dirt with short carry distance (~100 cells)
- Level 2 has ~750-800 cell transport distance along stepped terrain
- Manual carry would be extremely tedious (impossible without belts practically)
- Encourages player to use newly unlocked belts for efficiency
- Belt transport makes 1000 achievable in reasonable time
- Consistent with 08-TutorialMapLayout specification

### Progression Flow
1. Player completes Level 1 (500 Dirt to Bucket 1)
2. `Ability.PlaceBelts` unlocks
3. `OnObjectiveActivated("level2")` fires
4. Bucket 2 activates (was inactive/dim before)
5. Player places belts and transports 1000 Dirt
6. Level 2 completes, `Ability.PlaceLifts` unlocks
7. Bucket 3 activates for Level 3

---

## Level2 Zone Configuration

**Important:** Level 2 is NOT a separate LevelData. It is part of the unified TutorialLevelData defined in 08-TutorialMapLayout. The terrain regions for the stone slope are already included there.

### Zone 2 Configuration (Part of TutorialLevelData)

The stone slope terrain is defined in `TutorialLevelData.CreateTerrainRegions()`:

```csharp
// === ZONE 2: Stone Slope (from 08-TutorialMapLayout) ===
regions.Add(new TerrainRegion(551, 599, 1200, 1619, Materials.Stone)); // Barrier
regions.Add(new TerrainRegion(600, 750, 1280, 1619, Materials.Stone));
regions.Add(new TerrainRegion(750, 900, 1320, 1619, Materials.Stone));
regions.Add(new TerrainRegion(900, 1050, 1360, 1619, Materials.Stone));
regions.Add(new TerrainRegion(1050, 1200, 1400, 1619, Materials.Stone));
regions.Add(new TerrainRegion(1200, 1400, 1450, 1619, Materials.Stone)); // Valley
regions.Add(new TerrainRegion(1400, 1550, 1380, 1619, Materials.Stone));
regions.Add(new TerrainRegion(1550, 1700, 1340, 1619, Materials.Stone));
regions.Add(new TerrainRegion(1700, 1919, 1300, 1619, Materials.Stone));
```

### Bucket 2 Configuration (Part of TutorialLevelData)

```csharp
// Bucket spawns with associated objectives
Buckets = new List<BucketSpawn>
{
    new BucketSpawn(new Vector2Int(300, 1336), CreateLevel1Objective()),
    new BucketSpawn(new Vector2Int(1300, 1436), CreateLevel2Objective()),  // <-- Bucket 2
    new BucketSpawn(new Vector2Int(200, 266), CreateLevel3Objective())
}

private static ObjectiveData CreateLevel2Objective()
{
    return new ObjectiveData(
        targetMaterial: Materials.Dirt,
        requiredCount: 1000,
        rewardAbility: Ability.PlaceLifts,
        displayName: "Level 2: Use Belts",
        objectiveId: "level2",
        prerequisiteId: "level1"
    );
}
```

### Optional: Smooth Slope Enhancement

If testing reveals the stepped terrain is too easy, add smooth undulations:

```csharp
/// <summary>
/// Optional enhancement: Generate smooth undulations over the stepped terrain.
/// Call after LevelLoader.LoadLevel() if smoother slope is desired.
/// </summary>
public static class Level2SlopeEnhancement
{
    private const int SlopeStartX = 600;
    private const int SlopeStartY = 1280;
    private const int SlopeEndX = 1400;
    private const int SlopeEndY = 1450;
    private const int UndulationAmplitude = 15;
    private const int UndulationWavelength = 150;
    private const int SlopeThickness = 40;

    public static void EnhanceWithUndulations(CellWorld world)
    {
        for (int x = SlopeStartX; x <= SlopeEndX; x++)
        {
            int surfaceY = GetSlopeY(x);

            // Fill from surface down to surface + thickness
            for (int y = surfaceY; y < surfaceY + SlopeThickness && y < world.height; y++)
            {
                world.SetCell(x, y, Materials.Stone);
            }
        }

        // Mark terrain colliders dirty
        var terrainColliders = SimulationManager.Instance?.TerrainColliders;
        if (terrainColliders != null)
        {
            for (int x = SlopeStartX; x <= SlopeEndX; x++)
            {
                int surfaceY = GetSlopeY(x);
                for (int y = surfaceY; y < surfaceY + SlopeThickness && y < world.height; y++)
                {
                    terrainColliders.MarkChunkDirtyAt(x, y);
                }
            }
        }
    }

    public static int GetSlopeY(int x)
    {
        if (x < SlopeStartX) return SlopeStartY;
        if (x > SlopeEndX) return SlopeEndY;

        float t = (float)(x - SlopeStartX) / (SlopeEndX - SlopeStartX);
        float baseY = SlopeStartY + t * (SlopeEndY - SlopeStartY);
        float undulation = UndulationAmplitude * Mathf.Sin((x - SlopeStartX) * 2 * Mathf.PI / UndulationWavelength);

        return Mathf.RoundToInt(baseY + undulation);
    }

    public static int GetBeltPlacementY(int x)
    {
        return GetSlopeY(x) - 1;  // 1 cell above stone surface
    }
}
```

---

## LevelLoader Modifications

### Current LevelLoader (Verified)
The current `LevelLoader.cs` handles rectangular `TerrainRegion` filling and works correctly for the stepped terrain approach.

### Optional Enhancement for Smooth Undulations

If the smooth undulation enhancement is used, add post-load callback support:

```csharp
/// <summary>
/// Loads level data into the world, then calls optional post-load callback
/// for procedural terrain generation.
/// </summary>
public void LoadLevel(LevelData level, System.Action<CellWorld> postLoadCallback = null)
{
    foreach (var region in level.TerrainRegions)
    {
        FillRegion(region);
    }

    // Call post-load callback for procedural generation (e.g., undulating slopes)
    postLoadCallback?.Invoke(simulation.World);
}
```

**Usage:**
```csharp
levelLoader.LoadLevel(tutorialData, Level2SlopeEnhancement.EnhanceWithUndulations);
```

### Note on Stepped vs Smooth Terrain

The stepped terrain defined in 08-TutorialMapLayout should work for the base game. The smooth undulation system is an optional enhancement if playtesting reveals issues.

---

## LevelData Extensions Required

**Note:** These extensions are defined in 07-MultiObjectiveProgression.md and must be implemented BEFORE Level 2 setup.

### Multiple Bucket Support (from 07-MultiObjectiveProgression)

```csharp
// Already designed in 07-MultiObjectiveProgression
public class LevelData
{
    // Existing fields...

    /// <summary>
    /// Multiple bucket spawn positions, one per objective.
    /// Index corresponds to Objectives list.
    /// </summary>
    public List<Vector2Int> BucketSpawns { get; set; } = new List<Vector2Int>();

    /// <summary>
    /// Multiple objectives for multi-stage progression.
    /// </summary>
    public List<ObjectiveData> Objectives { get; set; } = new List<ObjectiveData>();

    // Legacy single-bucket properties for backwards compatibility
    public Vector2Int BucketSpawn { get => ...; set => ...; }
    public ObjectiveData Objective { get => ...; set => ...; }
}
```

### Extended ObjectiveData (from 07-MultiObjectiveProgression)

```csharp
public struct ObjectiveData
{
    public byte targetMaterial;
    public int requiredCount;
    public Ability rewardAbility;
    public string displayName;
    public string objectiveId;       // NEW: Unique identifier
    public string prerequisiteId;    // NEW: Required objective to complete first
}
```

### Implementation Status
| Component | Status | Notes |
|-----------|--------|-------|
| LevelData.BucketSpawns | NEEDS IMPLEMENTATION | Per 07-MultiObjectiveProgression |
| LevelData.Objectives | NEEDS IMPLEMENTATION | Per 07-MultiObjectiveProgression |
| ObjectiveData.objectiveId | NEEDS IMPLEMENTATION | Per 07-MultiObjectiveProgression |
| ObjectiveData.prerequisiteId | NEEDS IMPLEMENTATION | Per 07-MultiObjectiveProgression |
| ProgressionManager.OnObjectiveActivated | NEEDS IMPLEMENTATION | Per 07-MultiObjectiveProgression |
| Bucket inactive state | NEEDS IMPLEMENTATION | Per 07-MultiObjectiveProgression |

---

## GameController Integration

### Unified Tutorial World Loading

**Important:** There is NO `LoadLevel2()` function. The unified tutorial world loads ONCE at game start, with ALL terrain and ALL buckets. Progression is handled by the ProgressionManager activating/deactivating buckets.

```csharp
// In GameController.cs - Tutorial world loading (single load for all levels)

private void LoadTutorialWorld()
{
    var levelLoader = new LevelLoader(simulation);
    var tutorialData = TutorialLevelData.Create();

    // Load ALL terrain at once (includes Zone 1, Zone 2, Zone 3, Zone 4)
    levelLoader.LoadLevel(tutorialData);

    // Optional: Add smooth undulations to Zone 2 slope
    // Level2SlopeEnhancement.EnhanceWithUndulations(simulation.World);

    // Spawn player (once, at Zone 1 spawn point)
    CreatePlayer(tutorialData.PlayerSpawn);

    // Spawn shovel (once, at Zone 1)
    CreateShovelItem(tutorialData.ShovelSpawn);

    // Create ALL buckets at once
    // Buckets 2 and 3 start INACTIVE per 07-MultiObjectiveProgression
    for (int i = 0; i < tutorialData.Buckets.Count; i++)
    {
        var bucket = tutorialData.Buckets[i];
        bool startsInactive = !string.IsNullOrEmpty(bucket.Objective.prerequisiteId);
        CreateBucket(bucket.CellPosition, bucket.Objective, startsInactive);
    }

    // Register ALL objectives (ProgressionManager handles activation order)
    foreach (var bucket in tutorialData.Buckets)
    {
        ProgressionManager.Instance.AddObjective(bucket.Objective);
    }
}
```

### Belt Placement in Game Mode

The player needs a way to place belts during gameplay. This is gated by `Ability.PlaceBelts`:

```csharp
// BeltPlacementController.cs or integrated into PlayerController

private bool isBeltPlacementMode = false;

private void HandleBeltPlacement()
{
    // Check if belts are unlocked
    if (!ProgressionManager.Instance.IsUnlocked(Ability.PlaceBelts))
    {
        if (isBeltPlacementMode)
        {
            isBeltPlacementMode = false;
            ShowFeedback("Belts not unlocked yet!");
        }
        return;
    }

    // Toggle belt placement mode with B key
    if (Keyboard.current.bKey.wasPressedThisFrame)
    {
        isBeltPlacementMode = !isBeltPlacementMode;
        ShowFeedback(isBeltPlacementMode ? "Belt placement ON" : "Belt placement OFF");
    }

    if (!isBeltPlacementMode)
        return;

    // Show ghost preview at cursor position
    ShowBeltGhostPreview();

    // Place belt on left click
    if (Mouse.current.leftButton.wasPressedThisFrame)
    {
        Vector2Int cellPos = GetCellAtMouse();

        // Determine direction based on context or always right for now
        sbyte direction = 1;  // +1 = right (downslope direction for Zone 2)

        bool success = SimulationManager.Instance.BeltManager.PlaceBelt(cellPos.x, cellPos.y, direction);

        if (success)
            PlayPlaceSound();
        else
            ShowFeedback("Cannot place belt here");
    }

    // Remove belt on right click
    if (Mouse.current.rightButton.wasPressedThisFrame)
    {
        Vector2Int cellPos = GetCellAtMouse();
        SimulationManager.Instance.BeltManager.RemoveBelt(cellPos.x, cellPos.y);
    }
}
```

---

## Visual/Audio Polish (Future)

### Level Introduction
- Camera pan showing the obstacle and bucket location
- Text overlay: "The Descent - Transport dirt to the bucket"
- Highlight that belts are now available

### Belt Placement Feedback
- Ghost preview of belt before placing
- Sound effect on successful placement
- Error feedback if placement fails

### Transport Visualization
- Dirt particles visibly moving on belts
- Satisfying flow when system is working

---

## Testing Checklist

### World Setup Tests (Unified Tutorial World)
- [ ] World initializes with correct dimensions (1920x1620)
- [ ] Diggable Ground fills Zone 1 area (0-550, 1350-1619)
- [ ] Transition barrier wall exists (551-599, 1200-1619)
- [ ] Stone slope segments fill Zone 2 (600-1919, varying Y)
- [ ] Slope valley exists at X=1200-1400 (Y=1450 surface)
- [ ] Slope rise exists at X=1400-1550 (Y=1380 surface - creates trap)
- [ ] Player spawns correctly at (200, 1320)
- [ ] Bucket 2 spawns at (1300, 1436)
- [ ] Bucket 2 is positioned in the valley to catch belt output

### Belt System Tests
- [ ] Ability.PlaceBelts is required to place belts
- [ ] B key toggles belt placement mode
- [ ] Belts can be placed on Air cells
- [ ] Belts cannot be placed on Stone
- [ ] Belts snap to 8x8 grid
- [ ] Adjacent belts merge correctly
- [ ] Belts transport dirt in correct direction (+1 = rightward)
- [ ] Dirt accumulates at undulation peaks without belts
- [ ] Dirt flows over peaks when belts are placed

### Gameplay Flow Tests
- [ ] After Level 1 completion, Ability.PlaceBelts is unlocked
- [ ] After Level 1 completion, Bucket 2 activates (was dim/inactive)
- [ ] Player can dig Ground and produce Dirt (from Zone 1)
- [ ] Dirt cannot easily pass the transition barrier without belts
- [ ] Dirt accumulates at slope rises without belts (trap behavior)
- [ ] Without belts, dirt does NOT reach Bucket 2 efficiently
- [ ] With strategic belt placement, dirt reaches Bucket 2
- [ ] Bucket 2 collects dirt correctly
- [ ] Progress counter updates (1000 Dirt target)
- [ ] Objective completes at 1000 Dirt
- [ ] Ability.PlaceLifts unlocks on completion
- [ ] Bucket 3 activates after Level 2 completion

### Edge Cases
- [ ] Player cannot place belts without unlock
- [ ] Belts placed at world edge behave correctly
- [ ] Very large dirt piles don't break belt transport
- [ ] Multiple belt segments work together
- [ ] Removing a belt mid-chain stops transport

### Performance Tests
- [ ] Frame rate stable with active belt transport
- [ ] No memory leaks from belt placement/removal
- [ ] Large dirt quantities don't cause slowdown

---

## Implementation Order

### Prerequisites (Must Complete First)
1. **Implement 07-MultiObjectiveProgression** - Required for bucket activation/deactivation
2. **Implement 08-TutorialMapLayout** - Defines the unified world and terrain

### Phase 1: Verify Terrain Layout
1. Load unified tutorial world and verify Zone 2 terrain renders correctly
2. Verify stepped terrain creates visible trap areas
3. Test that dirt accumulates at rises (X=1400-1550) without belts
4. If traps are insufficient, implement optional `Level2SlopeEnhancement`

### Phase 2: Belt Placement in Game Mode
5. Create `BeltPlacementController.cs` or integrate into PlayerController
6. Gate placement behind `Ability.PlaceBelts`
7. Add B key toggle
8. Add ghost preview at cursor position
9. Add right-click to remove belts

### Phase 3: Integration Testing
10. Complete Level 1 to unlock belts
11. Verify Bucket 2 activates after Level 1 completion
12. Test belt placement on slope
13. Verify dirt transport via belts reaches Bucket 2
14. Balance dirt requirement (1000) vs belt efficiency
15. Verify Ability.PlaceLifts unlocks on completion

### Phase 4: Polish
16. Add belt placement sound effects
17. Add visual feedback for locked/unlocked abilities
18. Add camera hints to show Bucket 2 location when Level 2 starts

---

## Files to Create/Modify

| File | Action | Purpose |
|------|--------|---------|
| `Assets/Scripts/Game/Levels/TutorialLevelData.cs` | CREATE | Unified tutorial world config (per 08-TutorialMapLayout) |
| `Assets/Scripts/Game/Levels/Level2SlopeEnhancement.cs` | CREATE (Optional) | Smooth undulations if stepped terrain insufficient |
| `Assets/Scripts/Game/Levels/LevelLoader.cs` | MODIFY (Optional) | Add post-load callback for slope enhancement |
| `Assets/Scripts/Game/GameController.cs` | MODIFY | Load unified tutorial world instead of Level1Data |
| `Assets/Scripts/Game/BeltPlacementController.cs` | CREATE | Belt placement in game mode |
| `Assets/Scripts/Game/PlayerController.cs` | MODIFY | Hook up belt placement controller |

### Prerequisite Files (from other plans)

These must be implemented BEFORE Level 2 can work:

| File | Plan | Purpose |
|------|------|---------|
| `Assets/Scripts/Game/Levels/LevelData.cs` | 07-MultiObjectiveProgression | Add BucketSpawns, Objectives lists |
| `Assets/Scripts/Game/Progression/ObjectiveData.cs` | 07-MultiObjectiveProgression | Add objectiveId, prerequisiteId |
| `Assets/Scripts/Game/Progression/ProgressionManager.cs` | 07-MultiObjectiveProgression | Add OnObjectiveActivated event |
| `Assets/Scripts/Game/WorldObjects/Bucket.cs` | 07-MultiObjectiveProgression | Add inactive state support |

---

## Notes

### Why Terrain Traps Are Critical
Without rises/traps in the slope, dirt would naturally slide down to the bottom due to powder physics. The stepped terrain with rises (particularly at X=1400-1550 where Y rises from 1450 to 1380) creates barriers where dirt accumulates. Belts are needed to push dirt "uphill" over these rises, then gravity helps it flow to the bucket.

### Belt Direction Strategy
- **Primary belts (+1 direction, rightward)**: Used on descending sections to guide dirt toward bucket
- **Counter-flow belts (-1 direction, leftward)**: Used at the X=1400-1550 rise to push overflow BACK toward Bucket 2
- The combination ensures dirt is funneled into the bucket valley rather than piling at the rise

### Balancing Considerations
- If 1000 Dirt is too tedious, reduce to 750 or 800
- If players don't need many belts, increase the rise height (make Y=1380 lower, like 1350)
- If transport is too slow, consider reducing belt speed (`DefaultSpeed` in BeltManager)
- Current BeltManager.DefaultSpeed = 3 means 1 cell movement every 3 frames

### Future Level 3 Teaser
Level 2 completion unlocks Lifts, hinting that Level 3 will require vertical transport. The floating island (Zone 4) and lift column (Zone 3) are already visible in the world. When Level 2 completes, Bucket 3 on the floating island activates.

### Architecture Consistency
This plan follows the unified tutorial world design from 08-TutorialMapLayout:
- Single world load at game start
- All terrain exists from the beginning
- Progression is controlled by bucket activation, not level loading
- Player moves through zones, not between levels
