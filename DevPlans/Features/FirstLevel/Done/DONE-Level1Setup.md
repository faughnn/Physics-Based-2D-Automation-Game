# Level 1 Setup/Layout

**STATUS: IMPLEMENTED**

---

## Review Notes (2026-01-25) - Second Review

### All Dependencies IMPLEMENTED:
1. **02-DiggingSystem** - `Assets/Scripts/Game/Digging/DiggingController.cs` ✓
2. **03-CellGrabDropSystem** - `Assets/Scripts/Game/CellGrabSystem.cs` ✓
3. **04-BucketProgressionSystem** - All components exist: ✓
   - `Assets/Scripts/Game/WorldObjects/Bucket.cs`
   - `Assets/Scripts/Game/WorldObjects/CollectionZone.cs`
   - `Assets/Scripts/Game/Progression/ProgressionManager.cs`
   - `Assets/Scripts/Game/Progression/ObjectiveData.cs`
   - `Assets/Scripts/Game/Progression/Ability.cs`

### Verified as Correct:
- SimulationManager exists with singleton pattern (`SimulationManager.Instance`)
- SimulationManager provides `World`, `TerrainColliders`, `WorldWidth`, `WorldHeight` accessors
- `CoordinateUtils.cs` exists with `CellToWorld()`, `WorldToCell()`, `CellToWorldScale` constant
- World dimensions default to 1024x512 cells
- Cell coordinate system: Y=0 at top, Y increases downward
- Materials: `Sand`, `Stone`, `Dirt`, `Ground` all exist (IDs 2, 1, 17, 18)
- `Ground` material has `Diggable` flag set
- `ToolType` enum exists with `None = 0`, `Shovel = 1`
- `WorldItem.cs` exists in `Assets/Scripts/Game/Items/`
- `Bucket.Initialize(CellWorld world, Vector2Int cellPosition)` signature confirmed
- DiggingController converts Ground→Air and spawns Dirt particles with upward velocity
- CellGrabSystem uses right-click grab, release to drop
- ProgressionManager is singleton with `RecordCollection()`, `AddObjective()` methods

### Current GameController State (verified):
- `CreatePlayer()` - Uses serialized field `playerSpawnCell`, does NOT accept parameter
- `CreateShovelItem(Vector2 cellPosition)` - Takes Vector2 (not Vector2Int)
- `CreateInitialTerrain()` - Creates stone floor using `Materials.Stone` (will be replaced)
- `CreateBucket()` - Does NOT exist yet (needs to be added)
- ProgressionManager - NOT instantiated in Start() (needs to be added)

### Changes Required for Implementation:
1. Modify `CreatePlayer()` to accept `Vector2Int spawnCell` parameter
2. Add `CreateBucket(Vector2Int cellPosition)` method
3. Add ProgressionManager instantiation and objective registration
4. Replace `CreateInitialTerrain()` with LevelLoader approach using `Materials.Ground`

### Key Architecture Note:
The Bucket is a **static world structure** that collects falling cells, NOT a pickup item like the Shovel:
- Shovel: Spawned as `WorldItem`, picked up by walking over, triggers `OnTriggerEnter2D`
- Bucket: Created as a `Bucket` MonoBehaviour with Stone walls, has internal `CollectionZone`

### Second Review (2026-01-25) - VERIFIED:
All dependencies thoroughly checked against actual code:

| Component | File | Signature Verified |
|-----------|------|-------------------|
| DiggingController | `Assets/Scripts/Game/Digging/DiggingController.cs` | Uses `CoordinateUtils.WorldToCell()`, Ground→Air + spawns Dirt with velocity ✓ |
| CellGrabSystem | `Assets/Scripts/Game/CellGrabSystem.cs` | `GrabCellsAtPosition()`, `DropCellsAtPosition()`, `IsHolding` property ✓ |
| Bucket | `Assets/Scripts/Game/WorldObjects/Bucket.cs` | `Initialize(CellWorld world, Vector2Int cellPosition)` ✓ |
| CollectionZone | `Assets/Scripts/Game/WorldObjects/CollectionZone.cs` | `CollectCells()` returns `Dictionary<byte, int>` ✓ |
| ProgressionManager | `Assets/Scripts/Game/Progression/ProgressionManager.cs` | Singleton, `AddObjective()`, `RecordCollection()` ✓ |
| ObjectiveData | `Assets/Scripts/Game/Progression/ObjectiveData.cs` | Struct with constructor ✓ |
| CoordinateUtils | `Assets/Scripts/Simulation/CoordinateUtils.cs` | `CellToWorld()`, `WorldToCell()`, `CellToWorldScale` ✓ |

**Plan is READY FOR IMPLEMENTATION.**

---

## Summary

Level initialization system for the first tutorial level. Sets up the cell world terrain, spawns the player, shovel, and bucket at their designated positions. This is Game layer code that uses Simulation APIs to configure the world.

---

## Goals

1. Create a reusable level initialization system
2. Fill bottom third of screen with diggable Ground material
3. Spawn player, shovel, and bucket at correct positions
4. Proper coordinate conversion between cell and world space
5. Clear separation: Game layer orchestrates, Simulation layer executes

---

## World Configuration

### Dimensions (from existing GameController)

```
World Width:  1024 cells
World Height: 512 cells

Pixel Size: 2048 x 1024 (2x scale, 1 cell = 2 world units)
```

### Coordinate Systems

**Cell Coordinates:**
- Origin (0,0) is top-left
- X increases right (0 to 1023)
- Y increases downward (0 to 511)

**World Coordinates (Unity):**
- Origin (0,0) is center of screen
- X range: -1024 to +1024 (worldWidth units)
- Y range: -512 to +512 (worldHeight units)

**Conversion Formulas:**
```csharp
// USE EXISTING CoordinateUtils CLASS (Assets/Scripts/Simulation/CoordinateUtils.cs)
// DO NOT duplicate these formulas inline

// Cell to World
Vector2 worldPos = CoordinateUtils.CellToWorld(cellX, cellY, worldWidth, worldHeight);

// World to Cell
Vector2Int cellPos = CoordinateUtils.WorldToCell(worldPos, worldWidth, worldHeight);

// The formulas used internally:
// worldX = cellX * 2 - worldWidth;     // worldX = cellX * 2 - 1024
// worldY = worldHeight - cellY * 2;    // worldY = 512 - cellY * 2
```

---

## Level Layout Design

### Terrain Layout

```
Y=0   +----------------------------------+ Top of world
      |                                  |
      |              AIR                 |
      |          (top 2/3)              |
      |                                  |
Y=341 +----------------------------------+ Ground surface (1/3 from bottom)
      |                                  |
      |             GROUND               |
      |          (bottom 1/3)            |
      |           (diggable)             |
Y=511 +----------------------------------+ Bottom of world
      X=0                           X=1023
```

### Key Positions

| Element | Cell Position | World Position | Notes |
|---------|--------------|----------------|-------|
| Ground Surface | Y = 341 | worldY = -170 | 512 * (2/3) = 341 |
| Ground Depth | Y = 341 to 511 | -- | 170 cells deep |
| Player Spawn | (512, 330) | (0, -148) | Centered, 11 cells above ground |
| Shovel Spawn | (600, 330) | (176, -148) | Right of player, on ground surface |
| Bucket Spawn | (400, 200) | (-224, 112) | Left side, elevated platform |

### Calculated Positions

```csharp
// World dimensions
const int WorldWidth = 1024;
const int WorldHeight = 512;

// Terrain
int groundSurfaceY = WorldHeight / 3;      // 170 (cell Y where ground starts)
int groundTopCellY = WorldHeight - groundSurfaceY; // 341 (inverted for cell coords)

// Actually, let's recalculate:
// Cell Y=0 is TOP, Cell Y=511 is BOTTOM
// Bottom 1/3 means cells from Y=341 to Y=511
// Ground surface is at cell Y = 341

// Player: centered horizontally, standing on ground
int playerCellX = WorldWidth / 2;          // 512
int playerCellY = groundTopCellY - 11;     // 330 (11 cells above ground for player height)

// Shovel: right of player spawn, resting on ground
int shovelCellX = playerCellX + 88;        // 600 (offset right)
int shovelCellY = groundTopCellY - 6;      // 335 (just above ground surface)

// Bucket: left side, on a small platform (or suspended)
// For now, place it on a raised area the player must reach
int bucketCellX = 400;                     // Left of center
int bucketCellY = 200;                     // Upper portion of screen
```

---

## Design

### Architecture

```
Assets/Scripts/Game/
├── Levels/
│   ├── LevelData.cs           # Data structure for level configuration
│   ├── LevelLoader.cs         # Loads level data and initializes world
│   └── Level1Data.cs          # Static data for Level 1 layout
├── GameController.cs          # Calls LevelLoader to set up level
└── (existing files)
```

### LevelData Structure

```csharp
namespace FallingSand
{
    /// <summary>
    /// Configuration data for a game level.
    /// </summary>
    public class LevelData
    {
        // Terrain regions (list of rectangular areas to fill)
        public List<TerrainRegion> TerrainRegions;

        // Spawn positions (in cell coordinates)
        public Vector2Int PlayerSpawn;
        public Vector2Int ShovelSpawn;
        public Vector2Int BucketSpawn;

        // Optional: additional spawn points for future items
        public List<ItemSpawn> ItemSpawns;
    }

    public struct TerrainRegion
    {
        public int MinX, MaxX;  // Cell X range (inclusive)
        public int MinY, MaxY;  // Cell Y range (inclusive)
        public byte MaterialId; // Material to fill with
    }

    public struct ItemSpawn
    {
        public Vector2Int CellPosition;
        public ToolType ToolType;
    }
}
```

### Level1Data

```csharp
namespace FallingSand
{
    public static class Level1Data
    {
        public static LevelData Create(int worldWidth, int worldHeight)
        {
            // Ground surface: bottom 1/3 of screen
            int groundSurfaceY = worldHeight - (worldHeight / 3); // Cell Y = 341

            return new LevelData
            {
                TerrainRegions = new List<TerrainRegion>
                {
                    // Main ground layer (diggable static terrain)
                    // NOTE: Use Materials.Ground (static, diggable) NOT Sand
                    // When dug, Ground converts to Dirt particles
                    new TerrainRegion
                    {
                        MinX = 0,
                        MaxX = worldWidth - 1,
                        MinY = groundSurfaceY,
                        MaxY = worldHeight - 1,
                        MaterialId = Materials.Ground  // Static diggable terrain
                    }
                },

                // Player spawns centered, above ground
                PlayerSpawn = new Vector2Int(worldWidth / 2, groundSurfaceY - 20),

                // Shovel spawns to the right of player, on ground
                ShovelSpawn = new Vector2Int(worldWidth / 2 + 100, groundSurfaceY - 8),

                // Bucket spawns accessible but requires walking
                // NOTE: Bucket is a STRUCTURE, not a pickup - position is top-left corner
                BucketSpawn = new Vector2Int(150, groundSurfaceY - 50)
            };
        }
    }
}
```

### LevelLoader

```csharp
namespace FallingSand
{
    /// <summary>
    /// Initializes the world based on level data.
    /// Uses SimulationManager APIs to set up terrain and spawn points.
    /// </summary>
    public class LevelLoader
    {
        private readonly SimulationManager simulation;

        public LevelLoader(SimulationManager simulation)
        {
            this.simulation = simulation;
        }

        /// <summary>
        /// Loads level data into the world.
        /// </summary>
        public void LoadLevel(LevelData level)
        {
            // 1. Fill terrain regions
            foreach (var region in level.TerrainRegions)
            {
                FillRegion(region);
            }

            // 2. Return spawn positions for GameController to use
            // (spawning is handled by GameController, not here)
        }

        private void FillRegion(TerrainRegion region)
        {
            var world = simulation.World;
            var terrainColliders = simulation.TerrainColliders;

            for (int y = region.MinY; y <= region.MaxY; y++)
            {
                for (int x = region.MinX; x <= region.MaxX; x++)
                {
                    world.SetCell(x, y, region.MaterialId);

                    // Mark static terrain for collider generation
                    // NOTE: Ground is static and diggable, also needs colliders
                    if (region.MaterialId == Materials.Stone ||
                        region.MaterialId == Materials.Ground)
                    {
                        terrainColliders.MarkChunkDirtyAt(x, y);
                    }
                }
            }
        }

        /// <summary>
        /// Converts cell position to world position.
        /// USE CoordinateUtils.CellToWorld() instead of duplicating this logic.
        /// </summary>
        public Vector3 CellToWorld(Vector2Int cellPos)
        {
            // Prefer using CoordinateUtils.CellToWorld() directly
            Vector2 worldPos = CoordinateUtils.CellToWorld(cellPos.x, cellPos.y,
                simulation.WorldWidth, simulation.WorldHeight);
            return new Vector3(worldPos.x, worldPos.y, 0);
        }
    }
}
```

---

## Integration with GameController

### Current GameController State (as of review)

The current `GameController.cs` already has:
- `CreatePlayer()` - but does NOT accept spawn position (uses serialized field)
- `CreateShovelItem(Vector2 cellPosition)` - already exists, takes Vector2
- `CreateInitialTerrain()` - creates a simple stone floor (will be replaced)

### Required Changes to GameController

```csharp
// CURRENT CreatePlayer() signature (NO position parameter):
private void CreatePlayer()
{
    // Uses serialized field: playerSpawnCell
}

// NEEDS TO BE CHANGED TO:
private void CreatePlayer(Vector2Int spawnCell)
{
    // Accept spawn position from level data
}

// CURRENT CreateShovelItem() signature (Vector2, not Vector2Int):
private void CreateShovelItem(Vector2 cellPosition)

// CreateBucketItem does NOT EXIST - but note that the Bucket from
// plan 04 is a WORLD STRUCTURE, not a pickup item. See note below.
```

### Important: Bucket is NOT a Pickup Item

The Bucket system from 04-BucketProgressionSystem.md creates a **static world structure** with Stone walls that collects falling cells. It is NOT a `WorldItem` like the Shovel.

The bucket should be created via:
```csharp
private void CreateBucket(Vector2Int cellPosition)
{
    GameObject bucketObj = new GameObject("Bucket");
    Bucket bucket = bucketObj.AddComponent<Bucket>();
    bucket.Initialize(simulation.World, cellPosition);
}
```

### Modified Start Flow

```csharp
private void Start()
{
    // 1. Initialize simulation (existing)
    simulation = SimulationManager.Instance;
    if (simulation == null)
    {
        simulation = SimulationManager.Create(worldWidth, worldHeight);
        simulation.Initialize();
    }

    // 2. Setup camera (existing)
    SetupCamera();

    // 3. Create ProgressionManager (NEW - must exist before Bucket)
    CreateProgressionManager();

    // 4. Load level (NEW - replaces CreateInitialTerrain())
    var levelLoader = new LevelLoader(simulation);
    var levelData = Level1Data.Create(worldWidth, worldHeight);
    levelLoader.LoadLevel(levelData);

    // 5. Register Level 1 objective (NEW)
    ProgressionManager.Instance.AddObjective(new ObjectiveData(
        targetMaterial: Materials.Dirt,
        requiredCount: 100,  // Adjust as needed for gameplay
        rewardAbility: Ability.PlaceBelts,
        displayName: "Collect Dirt"
    ));

    // 6. Spawn player at level-defined position
    // NOTE: CreatePlayer() must be modified to accept spawn position
    CreatePlayer(levelData.PlayerSpawn);

    // 7. Spawn shovel item (already exists but uses Vector2)
    CreateShovelItem(levelData.ShovelSpawn);

    // 8. Create bucket STRUCTURE (not a pickup item!)
    CreateBucket(levelData.BucketSpawn);
}

private void CreateProgressionManager()
{
    // ProgressionManager is a singleton - create if it doesn't exist
    if (ProgressionManager.Instance == null)
    {
        GameObject pmObj = new GameObject("ProgressionManager");
        pmObj.AddComponent<ProgressionManager>();
    }
}
```

---

## Spawn Position Details

### Player Spawn

```
Cell Position: (512, 321)
- Centered horizontally (worldWidth / 2)
- 20 cells above ground surface
- Player is 16 cells tall, so feet will be ~4 cells above ground (will fall onto it)

World Position: (0, -130)
```

### Shovel Spawn

```
Cell Position: (612, 333)
- 100 cells right of center
- 8 cells above ground surface (resting on top)
- Player walks right to collect

World Position: (200, -154)
```

### Bucket Spawn

```
Cell Position: (150, 291)
- Far left side of screen
- 50 cells above ground surface
- Requires building a platform OR digging a path
- Could be placed on a raised stone platform

World Position: (-724, -70)
```

### Alternative: Bucket on Ground

For simpler initial implementation, place bucket on ground:

```
Cell Position: (150, 333)
- Same Y as shovel (on ground surface)
- Far left, requires walking

World Position: (-724, -154)
```

---

## Material Considerations

### Ground Material - ALREADY EXISTS

The `Materials.Ground` (ID 18) already exists in `MaterialDef.cs`:
```csharp
// Ground - static diggable terrain
defs[Ground] = new MaterialDef
{
    density = 255,
    behaviour = BehaviourType.Static,
    flags = MaterialFlags.ConductsHeat | MaterialFlags.Diggable,
    baseColour = new Color32(92, 64, 51, 255),  // Dark brown
    colourVariation = 8,
};
```

**For Level 1:** Use `Materials.Ground` for the main terrain. When dug, it converts to `Materials.Dirt` particles (as specified in 02-DiggingSystem).

### Dirt Material - ALREADY EXISTS

The `Materials.Dirt` (ID 17) already exists:
```csharp
// Dirt - heavy powder that piles steeply
defs[Dirt] = new MaterialDef
{
    density = 140,
    slideResistance = 50,
    behaviour = BehaviourType.Powder,
    flags = MaterialFlags.None,
    baseColour = new Color32(139, 90, 43, 255), // Brown
    colourVariation = 12,
};
```

### Bucket Platform (if elevated)

If bucket is placed above ground level:
- Create a small Stone platform for it to rest on
- Or place it at ground level for simplicity in tutorial

---

## Order of Initialization

```
1. SimulationManager.Create(width, height)
   └── Creates CellWorld (all Air)
   └── Creates ClusterManager
   └── Creates TerrainColliderManager
   └── Creates BeltManager
   └── Creates CellRenderer
   └── Creates GraphicsManager

2. GameController.CreateProgressionManager() [NEW]
   └── Creates ProgressionManager singleton (must exist before Bucket)

3. LevelLoader.LoadLevel(levelData)
   └── Fills terrain regions with Ground material
   └── Marks chunks dirty for collider generation

4. ProgressionManager.AddObjective() [NEW]
   └── Registers Level 1 objective (collect Dirt → unlock PlaceBelts)

5. TerrainColliderManager (auto-updates)
   └── Generates PolygonCollider2D for static terrain (Ground, Stone)

6. GameController.CreatePlayer(spawnPos) [NEEDS MODIFICATION]
   └── Creates player GameObject with Rigidbody2D
   └── Positions at world coordinates using CoordinateUtils.CellToWorld()
   └── Attaches PlayerController, DiggingController, CellGrabSystem

7. GameController.CreateShovelItem(spawnPos) [EXISTS]
   └── Creates shovel WorldItem with BoxCollider2D (trigger)
   └── Positions at world coordinates

8. GameController.CreateBucket(spawnPos) [NEEDS CREATION]
   └── Creates Bucket MonoBehaviour (NOT a WorldItem!)
   └── Bucket.Initialize() creates Stone walls in cell world
   └── Sets up CollectionZone for detecting falling cells
   └── Subscribes to ProgressionManager.OnObjectiveCompleted
```

---

## Dependencies

### Required Systems (must exist before Level 1) - ALL VERIFIED TO EXIST

1. **SimulationManager** - World initialization (EXISTS at `Assets/Scripts/Simulation/SimulationManager.cs`)
2. **CellWorld** - Cell storage and manipulation (EXISTS)
3. **TerrainColliderManager** - Physics colliders for terrain (EXISTS)
4. **Materials** - Sand, Stone, Dirt, Ground all exist (EXISTS at `Assets/Scripts/Simulation/MaterialDef.cs`)
5. **CoordinateUtils** - Coordinate conversion (EXISTS at `Assets/Scripts/Simulation/CoordinateUtils.cs`)

### Required First Level Systems - STATUS

| Plan | System | Status | Notes |
|------|--------|--------|-------|
| 01 | ItemPickupSystem | **COMPLETED** | WorldItem, ToolType exist; CreateShovelItem exists |
| 02 | DiggingSystem | **COMPLETED** | `DiggingController.cs` at `Assets/Scripts/Game/Digging/` |
| 03 | CellGrabDropSystem | **COMPLETED** | `CellGrabSystem.cs` at `Assets/Scripts/Game/` |
| 04 | BucketProgressionSystem | **COMPLETED** | `Bucket.cs`, `CollectionZone.cs`, `ProgressionManager.cs` all exist |

### Implementation Order

All dependencies are now complete. This plan (05-Level1Setup) is ready for implementation.

### Created by This System

1. **LevelData** - Level configuration structure (NEW)
2. **LevelLoader** - Level initialization logic (NEW)
3. **Level1Data** - Specific Level 1 configuration (NEW)
4. **Modifications to GameController.CreatePlayer()** - Accept spawn position

---

## Testing Checklist

### Level Setup
- [ ] World initializes with correct dimensions (1024x512)
- [ ] Ground fills bottom third (Y=341 to Y=511) using `Materials.Ground`
- [ ] Player spawns above ground, falls onto surface
- [ ] Shovel spawns on ground surface, visible
- [ ] Bucket spawns at designated location with Stone walls
- [ ] Terrain colliders generate correctly for Ground material
- [ ] Player can walk on ground surface
- [ ] Player can reach shovel location
- [ ] Player can reach bucket location
- [ ] No gaps or visual artifacts in terrain

### Gameplay Flow
- [ ] Player picks up shovel (walks through, triggers OnTriggerEnter2D)
- [ ] Left-click digs Ground → spawns Dirt particles with upward velocity
- [ ] Right-click grabs Dirt particles
- [ ] Right-release drops Dirt particles
- [ ] Dirt dropped in bucket triggers collection
- [ ] ProgressionManager tracks collected Dirt count
- [ ] Objective completes when threshold reached
- [ ] Bucket glow fades after objective completion

---

## Implementation Order

### Pre-requisites (from other plans) - ALL COMPLETE ✓
- [x] **02-DiggingSystem**: `DiggingController.cs` exists
- [x] **03-CellGrabDropSystem**: `CellGrabSystem.cs` exists
- [x] **04-BucketProgressionSystem**: `Bucket.cs`, `ProgressionManager.cs`, `CollectionZone.cs` all exist

### Phase 1: Data Structures - COMPLETE
1. [x] Create `Assets/Scripts/Game/Levels/` directory
2. [x] Create `LevelData.cs` with terrain and spawn structures
3. [x] Create `Level1Data.cs` with static level configuration

### Phase 2: Level Loader - COMPLETE
4. [x] Create `LevelLoader.cs` with terrain filling logic
5. [x] Use `CoordinateUtils.CellToWorld()` (already exists - do NOT duplicate)
6. [x] Add terrain collider marking for Ground material

### Phase 3: GameController Integration - COMPLETE
7. [x] Remove `CreateInitialTerrain()`, replaced with LevelLoader approach
8. [x] Modify `GameController.Start()` to use LevelLoader
9. [x] Update `CreatePlayer()` to accept spawn position parameter
10. [x] Update `CreateShovelItem()` to use Vector2Int and CoordinateUtils
11. [x] Add `CreateBucket()` method that creates Bucket MonoBehaviour
12. [x] Add `CreateProgressionManager()` method
13. [x] Register objective with ProgressionManager

### Phase 4: Testing
12. Play test level layout
13. Verify spawn positions using coordinate conversion
14. Test terrain collisions with Ground material
15. Test that player can dig Ground and it produces Dirt
16. Test that Bucket collects Dirt cells
17. Adjust positions as needed

---

## Visual Reference

```
Screen Layout (1024x512 cells):

    0                 512               1024
    |                  |                  |
0   +------------------+------------------+  <- Top (Air)
    |                  |                  |
    |                  |                  |
    |   [Bucket]       |                  |  <- Y~291 (bucket structure with walls)
    |   |_____|        |                  |
    |                  |                  |
    |              [Player]               |  <- Y~321 (spawn, falls to ground)
341 +=========[Shovel]=+==================+  <- Ground Surface (Materials.Ground)
    |##################################|
    |       GROUND (static, diggable)      |  <- Materials.Ground = 18
    |   (when dug, spawns Dirt particles)  |  <- Dirt = Materials.Dirt = 17
511 +-------------------------------------+  <- Bottom
```

Note: Bucket is a STRUCTURE with Stone walls, not a pickup item.

---

## Notes

- The bucket position may need adjustment based on gameplay testing
- Consider adding a small ramp or stairway for player access to elevated areas
- Future levels can reuse LevelLoader with different LevelData configurations
- Consider serializing LevelData to JSON/ScriptableObjects for editor support

---

## Files to Reference During Implementation

| File | Purpose |
|------|---------|
| `Assets/Scripts/Simulation/CoordinateUtils.cs` | Use for coordinate conversion - DO NOT duplicate |
| `Assets/Scripts/Simulation/MaterialDef.cs` | Materials.Ground (18), Materials.Dirt (17) exist |
| `Assets/Scripts/Game/GameController.cs` | Needs modification to accept spawn positions |
| `Assets/Scripts/Game/Items/WorldItem.cs` | Existing item pickup system |
| `DevPlans/Features/FirstLevel/04-BucketProgressionSystem.md` | Bucket is a structure, not pickup |
