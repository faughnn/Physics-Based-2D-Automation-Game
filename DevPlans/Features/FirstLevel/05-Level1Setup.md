# Level 1 Setup/Layout

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
// Cell to World
worldX = cellX * 2 - worldWidth;     // worldX = cellX * 2 - 1024
worldY = worldHeight - cellY * 2;    // worldY = 512 - cellY * 2

// World to Cell
cellX = (worldX + worldWidth) / 2;   // cellX = (worldX + 1024) / 2
cellY = (worldHeight - worldY) / 2;  // cellY = (512 - worldY) / 2
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
                    // Main ground layer (diggable)
                    new TerrainRegion
                    {
                        MinX = 0,
                        MaxX = worldWidth - 1,
                        MinY = groundSurfaceY,
                        MaxY = worldHeight - 1,
                        MaterialId = Materials.Sand  // Using Sand as diggable "ground"
                    }
                },

                // Player spawns centered, above ground
                PlayerSpawn = new Vector2Int(worldWidth / 2, groundSurfaceY - 20),

                // Shovel spawns to the right of player, on ground
                ShovelSpawn = new Vector2Int(worldWidth / 2 + 100, groundSurfaceY - 8),

                // Bucket spawns accessible but requires walking
                // On ground surface, far left
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
                    if (region.MaterialId == Materials.Stone ||
                        region.MaterialId == Materials.Sand)
                    {
                        terrainColliders.MarkChunkDirtyAt(x, y);
                    }
                }
            }
        }

        /// <summary>
        /// Converts cell position to world position.
        /// </summary>
        public Vector3 CellToWorld(Vector2Int cellPos)
        {
            float worldX = cellPos.x * 2 - simulation.WorldWidth;
            float worldY = simulation.WorldHeight - cellPos.y * 2;
            return new Vector3(worldX, worldY, 0);
        }
    }
}
```

---

## Integration with GameController

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

    // 3. Load level (NEW)
    var levelLoader = new LevelLoader(simulation);
    var levelData = Level1Data.Create(worldWidth, worldHeight);
    levelLoader.LoadLevel(levelData);

    // 4. Spawn entities at level-defined positions (NEW)
    CreatePlayer(levelData.PlayerSpawn);
    CreateShovelItem(levelData.ShovelSpawn);
    CreateBucketItem(levelData.BucketSpawn);
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

### Ground Material

Currently no "Ground" material exists. Options:

1. **Use Sand** - Already exists, falls/moves, creates interesting digging
2. **Use Stone** - Static, would need pickaxe to dig
3. **Add new "Dirt" material** - Static but diggable with shovel

**Recommendation for Level 1:** Use Sand initially. It's already diggable and creates satisfying pile behavior. Add Dirt material later if needed.

### Bucket Platform (if elevated)

If bucket is placed above ground level:
- Create a small Stone platform for it to rest on
- Or suspend it in air (for simplicity in tutorial)

---

## Order of Initialization

```
1. SimulationManager.Create(width, height)
   └── Creates CellWorld (all Air)
   └── Creates ClusterManager
   └── Creates TerrainColliderManager
   └── Creates BeltManager
   └── Creates CellRenderer

2. LevelLoader.LoadLevel(levelData)
   └── Fills terrain regions with materials
   └── Marks chunks dirty for collider generation

3. TerrainColliderManager (auto-updates)
   └── Generates PolygonCollider2D for static terrain

4. GameController.CreatePlayer(spawnPos)
   └── Creates player GameObject with Rigidbody2D
   └── Positions at world coordinates

5. GameController.CreateShovelItem(spawnPos)
   └── Creates shovel WorldItem
   └── Positions at world coordinates

6. GameController.CreateBucketItem(spawnPos)
   └── Creates bucket WorldItem (or BucketTrigger zone)
   └── Positions at world coordinates
```

---

## Dependencies

### Required Systems (must exist before Level 1)

1. **SimulationManager** - World initialization
2. **CellWorld** - Cell storage and manipulation
3. **TerrainColliderManager** - Physics colliders for terrain
4. **Materials** - Sand material for ground

### Required First Level Systems

1. **01-ItemPickupSystem** - WorldItem component for shovel/bucket
2. **02-DiggingMechanic** - Shovel removes Sand cells
3. **03-BucketSystem** - Bucket accepts dropped dirt
4. **04-WinCondition** - Checks bucket fill state

### Created by This System

1. **LevelData** - Level configuration structure
2. **LevelLoader** - Level initialization logic
3. **Level1Data** - Specific Level 1 configuration

---

## Testing Checklist

- [ ] World initializes with correct dimensions (1024x512)
- [ ] Ground fills bottom third (Y=341 to Y=511)
- [ ] Player spawns above ground, falls onto surface
- [ ] Shovel spawns on ground surface, visible
- [ ] Bucket spawns at designated location
- [ ] Terrain colliders generate correctly for ground
- [ ] Player can walk on ground surface
- [ ] Player can reach shovel location
- [ ] Player can reach bucket location
- [ ] No gaps or visual artifacts in terrain

---

## Implementation Order

### Phase 1: Data Structures
1. Create `Assets/Scripts/Game/Levels/` directory
2. Create `LevelData.cs` with terrain and spawn structures
3. Create `Level1Data.cs` with static level configuration

### Phase 2: Level Loader
4. Create `LevelLoader.cs` with terrain filling logic
5. Add `CellToWorld()` helper method
6. Add terrain collider marking

### Phase 3: GameController Integration
7. Modify `GameController.Start()` to use LevelLoader
8. Update `CreatePlayer()` to accept spawn position
9. Add `CreateShovelItem()` method (from ItemPickupSystem plan)
10. Add `CreateBucketItem()` method (from BucketSystem plan)

### Phase 4: Testing
11. Play test level layout
12. Verify spawn positions
13. Test terrain collisions
14. Adjust positions as needed

---

## Visual Reference

```
Screen Layout (1024x512 cells):

    0                 512               1024
    |                  |                  |
0   +------------------+------------------+  <- Top (Air)
    |                  |                  |
    |                  |                  |
    |        [Bucket]  |                  |  <- Y~291 (if elevated)
    |                  |                  |
    |                  |                  |
    |              [Player]               |  <- Y~321 (spawn)
341 +=========[Shovel]=+==================+  <- Ground Surface
    |        \\\\\\\\\\|//////////////////|
    |       SAND/GROUND (diggable)        |
    |///////////////////////////////////// |
511 +-------------------------------------+  <- Bottom
```

---

## Notes

- The bucket position may need adjustment based on gameplay testing
- Consider adding a small ramp or stairway for player access to elevated areas
- Future levels can reuse LevelLoader with different LevelData configurations
- Consider serializing LevelData to JSON/ScriptableObjects for editor support
