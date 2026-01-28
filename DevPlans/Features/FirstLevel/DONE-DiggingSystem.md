**STATUS: COMPLETED** - Implemented on 2026-01-25

# Digging System

## Summary

A player-driven digging mechanic that converts Ground material cells into Air and spawns loose Dirt particles with upward velocity. The player must have a shovel equipped and click within proximity range to dig. This is Game layer code that uses Simulation APIs - no modifications to the Simulation layer.

---

## Goals

1. Allow player to dig Ground material using mouse clicks
2. Require shovel to be equipped (via Item Pickup System)
3. Enforce proximity check - player must be near the dig location
4. Create satisfying visual feedback with dirt particles flying upward
5. Maintain clean Game/Simulation separation - Game layer orchestrates, Simulation layer executes

---

## Design

### Architecture Overview

```
Assets/Scripts/Game/
├── Digging/
│   └── DiggingController.cs    # Handles dig input, proximity, and effect
├── PlayerController.cs         # Already has EquippedTool property
└── GameController.cs           # Registers DiggingController
```

### Component: DiggingController

A MonoBehaviour attached to the Player that:
- Listens for mouse clicks
- Checks if shovel is equipped
- Validates proximity to dig location
- Converts Ground cells to Air
- Spawns Dirt particles with velocity

```csharp
namespace FallingSand
{
    public class DiggingController : MonoBehaviour
    {
        [Header("Dig Settings")]
        [SerializeField] private float digRadius = 8f;           // Cells affected
        [SerializeField] private float maxDigDistance = 100f;    // World units from player

        [Header("Particle Settings")]
        [SerializeField] private float minUpwardVelocity = 3f;   // Cells per frame
        [SerializeField] private float maxUpwardVelocity = 8f;   // Cells per frame
        [SerializeField] private float horizontalSpread = 3f;    // Max horizontal velocity

        private PlayerController player;
        private Camera mainCamera;

        // Access simulation via singleton
        private SimulationManager Simulation => SimulationManager.Instance;
        private CellWorld World => Simulation?.World;
    }
}
```

### Configuration Values

| Parameter | Value | Rationale |
|-----------|-------|-----------|
| `digRadius` | 8 cells | Satisfying chunk removal, not too surgical |
| `maxDigDistance` | 100 world units | ~50 cells, generous reach |
| `minUpwardVelocity` | 3 cells/frame | Minimum satisfying pop |
| `maxUpwardVelocity` | 8 cells/frame | Max upward fling |
| `horizontalSpread` | 3 cells/frame | Creates spread pattern |

### Velocity Storage

Cell velocity uses `sbyte` (-128 to 127), but velocities are typically -16 to +16 cells/frame:
- `Cell.velocityX`: sbyte, horizontal velocity
- `Cell.velocityY`: sbyte, vertical velocity (positive = downward in cell grid)

For upward motion, we need **negative velocityY** since cell Y=0 is at top.

---

## Implementation

### Step 1: Create DiggingController.cs

```csharp
// G:\Sandy\Assets\Scripts\Game\Digging\DiggingController.cs

using UnityEngine;
using UnityEngine.InputSystem;

namespace FallingSand
{
    /// <summary>
    /// Handles player digging interaction. Requires shovel to be equipped.
    /// Converts Ground cells to Air and spawns Dirt particles with upward velocity.
    /// </summary>
    public class DiggingController : MonoBehaviour
    {
        [Header("Dig Settings")]
        [SerializeField] private float digRadius = 8f;
        [SerializeField] private float maxDigDistance = 100f;

        [Header("Particle Settings")]
        [SerializeField] private float minUpwardVelocity = 3f;
        [SerializeField] private float maxUpwardVelocity = 8f;
        [SerializeField] private float horizontalSpread = 3f;

        private PlayerController player;
        private Camera mainCamera;
        private Mouse mouse;

        private SimulationManager Simulation => SimulationManager.Instance;
        private CellWorld World => Simulation?.World;

        private void Start()
        {
            player = GetComponent<PlayerController>();
            mainCamera = Camera.main;
            mouse = Mouse.current;

            if (player == null)
                Debug.LogError("[DiggingController] No PlayerController found on this GameObject!");
        }

        private void Update()
        {
            if (mouse == null || player == null || World == null) return;

            // Left click to dig
            if (mouse.leftButton.wasPressedThisFrame)
            {
                TryDig();
            }
        }

        private void TryDig()
        {
            // Check shovel equipped
            if (player.EquippedTool != ToolType.Shovel)
            {
                Debug.Log("[DiggingController] Cannot dig - no shovel equipped");
                return;
            }

            // Get click position in world coordinates
            Vector2 mouseScreen = mouse.position.ReadValue();
            Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(
                new Vector3(mouseScreen.x, mouseScreen.y, 0));

            // Proximity check
            Vector2 playerPos = transform.position;
            Vector2 clickPos = new Vector2(mouseWorld.x, mouseWorld.y);
            float distance = Vector2.Distance(playerPos, clickPos);

            if (distance > maxDigDistance)
            {
                Debug.Log($"[DiggingController] Too far to dig: {distance:F1} > {maxDigDistance}");
                return;
            }

            // Convert click to cell coordinates
            Vector2Int centerCell = WorldToCell(clickPos);

            // Perform the dig
            DigAt(centerCell);
        }

        private void DigAt(Vector2Int center)
        {
            int radius = Mathf.RoundToInt(digRadius);
            int cellsDug = 0;
            int dirtSpawned = 0;

            var world = World;
            var terrainColliders = Simulation.TerrainColliders;

            // Iterate over circular area
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    // Circular check
                    if (dx * dx + dy * dy > radius * radius)
                        continue;

                    int x = center.x + dx;
                    int y = center.y + dy;

                    if (!world.IsInBounds(x, y))
                        continue;

                    byte material = world.GetCell(x, y);

                    // Check if this is Ground material (diggable)
                    if (material != Materials.Ground)
                        continue;

                    // Convert Ground to Air
                    world.SetCell(x, y, Materials.Air);
                    terrainColliders.MarkChunkDirtyAt(x, y);
                    cellsDug++;

                    // Spawn Dirt particle above with upward velocity
                    // Find spawn position (1-2 cells above the dug cell)
                    int spawnY = y - 1;  // One cell up (lower Y = higher position)
                    if (spawnY >= 0 && world.GetCell(x, spawnY) == Materials.Air)
                    {
                        SpawnDirtWithVelocity(x, spawnY);
                        dirtSpawned++;
                    }
                }
            }

            Debug.Log($"[DiggingController] Dug {cellsDug} Ground cells, spawned {dirtSpawned} Dirt particles");
        }

        private void SpawnDirtWithVelocity(int x, int y)
        {
            var world = World;
            int index = y * world.width + x;

            // Create Dirt cell with upward velocity
            Cell cell = world.cells[index];
            cell.materialId = Materials.Dirt;

            // Random upward velocity (negative Y because cell Y increases downward)
            float upwardSpeed = Random.Range(minUpwardVelocity, maxUpwardVelocity);
            float horizontalSpeed = Random.Range(-horizontalSpread, horizontalSpread);

            // Clamp to sbyte range (-128 to 127)
            cell.velocityY = (sbyte)Mathf.Clamp(-upwardSpeed, -16, 16);
            cell.velocityX = (sbyte)Mathf.Clamp(horizontalSpeed, -16, 16);

            world.cells[index] = cell;
            world.MarkDirty(x, y);
        }
    }
}
```

### Step 2: Register in GameController

Add to `GameController.CreatePlayer()`:

```csharp
// Add DiggingController to player
player.AddComponent<DiggingController>();
Debug.Log("[GameController] DiggingController added to player");
```

### Step 3: Ensure Dependencies Exist

The system requires:
1. **Item Pickup System** (01-ItemPickupSystem.md) - For `player.EquippedTool` and `ToolType.Shovel`
2. **Ground Material** (PLANNED-GroundMaterial.md) - `Materials.Ground = 18`
3. **Dirt Material** (PLANNED-DirtMaterial.md) - `Materials.Dirt = 17`

---

## Accessing Simulation from Game Layer

The Game layer accesses Simulation through the **SimulationManager singleton**:

```csharp
// Get the simulation manager
SimulationManager simulation = SimulationManager.Instance;

// Access the cell world
CellWorld world = simulation.World;

// Read cells
byte material = world.GetCell(x, y);

// Write cells (simple)
world.SetCell(x, y, Materials.Air);

// Write cells with velocity (direct array access)
int index = y * world.width + x;
Cell cell = world.cells[index];
cell.materialId = Materials.Dirt;
cell.velocityX = 2;
cell.velocityY = -5;  // Negative = upward
world.cells[index] = cell;
world.MarkDirty(x, y);

// Update terrain colliders when modifying static materials
simulation.TerrainColliders.MarkChunkDirtyAt(x, y);
```

### Key Points

1. **Use the singleton** - `SimulationManager.Instance` provides access to all simulation systems
2. **SetCell for simple changes** - Resets velocity, marks dirty automatically
3. **Direct array access for velocity** - When spawning particles with velocity, modify `world.cells[index]` directly
4. **Always MarkDirty** - After modifying cells directly, call `world.MarkDirty(x, y)`
5. **Update terrain colliders** - When modifying static materials (Ground, Stone), call `TerrainColliders.MarkChunkDirtyAt()`

---

## Spawning Cells with Velocity

### Cell Velocity System

Cells have `velocityX` and `velocityY` as `sbyte` values:
- Range: -128 to +127 (typically use -16 to +16)
- Units: cells per frame
- **Important**: Cell grid Y=0 is TOP, so negative `velocityY` = moving upward

### Spawn Pattern

```csharp
private void SpawnDirtWithVelocity(int x, int y)
{
    var world = World;
    int index = y * world.width + x;

    Cell cell = world.cells[index];
    cell.materialId = Materials.Dirt;

    // Upward velocity (negative Y)
    float upSpeed = Random.Range(3f, 8f);
    float hSpeed = Random.Range(-3f, 3f);

    cell.velocityY = (sbyte)Mathf.Clamp(-upSpeed, -16, 16);
    cell.velocityX = (sbyte)Mathf.Clamp(hSpeed, -16, 16);

    world.cells[index] = cell;
    world.MarkDirty(x, y);
}
```

---

## Coordinate Conversion

The project has two coordinate systems:

### Cell Grid
- Origin: Top-left
- X: 0 to width-1 (left to right)
- Y: 0 to height-1 (top to bottom)

### Unity World
- Origin: Center
- X: -width to +width (left to right)
- Y: +height to -height (top to bottom)
- Scale: 1 cell = 2 world units

### Conversion Formulas

```csharp
// World to Cell
int cellX = Mathf.FloorToInt((worldX + worldWidth) / 2f);
int cellY = Mathf.FloorToInt((worldHeight - worldY) / 2f);

// Cell to World
float worldX = cellX * 2f - worldWidth;
float worldY = worldHeight - cellY * 2f;
```

**Use `CoordinateUtils`** - The `CoordinateUtils` class in `Assets/Scripts/Simulation/CoordinateUtils.cs` provides these conversions. Use `CoordinateUtils.WorldToCell()` for consistency across the codebase.

---

## Dependencies

| Dependency | Status | Notes |
|------------|--------|-------|
| Item Pickup System | **Exists** | 01-ItemPickupSystem.md - Provides `ToolType.Shovel` and `EquippedTool` |
| Ground Material | **Exists** | `Materials.Ground = 18` in MaterialDef.cs |
| Dirt Material | **Exists** | `Materials.Dirt = 17` in MaterialDef.cs |
| SimulationManager | **Exists** | Singleton access to simulation |
| PlayerController | **Exists** | Has `EquippedTool` property |
| CoordinateUtils | **Exists** | `CoordinateUtils.WorldToCell()` for coordinate conversion |

All dependencies are now implemented.

---

## Event Flow

```
1. Player clicks left mouse button
   |
   v
2. DiggingController.TryDig()
   |
   +-- Check EquippedTool == Shovel --> NO --> Log "no shovel", return
   |
   +-- Get click world position
   |
   +-- Calculate distance to player
   |
   +-- Check distance <= maxDigDistance --> NO --> Log "too far", return
   |
   +-- Convert world pos to cell coords
   |
   v
3. DiggingController.DigAt(centerCell)
   |
   +-- For each cell in radius:
   |     |
   |     +-- Check IsInBounds
   |     +-- Check material == Ground
   |     +-- SetCell(x, y, Air)
   |     +-- MarkChunkDirtyAt for terrain collider
   |     +-- SpawnDirtWithVelocity above
   |
   v
4. SpawnDirtWithVelocity(x, y)
   |
   +-- Set materialId = Dirt
   +-- Set velocityY = random negative (upward)
   +-- Set velocityX = random spread
   +-- MarkDirty
   |
   v
5. Next simulation frame
   |
   +-- Dirt particles move according to velocity
   +-- Gravity pulls them back down
   +-- They pile up as loose material
```

---

## Testing Checklist

- [ ] Cannot dig without shovel equipped
- [ ] Cannot dig when click is too far from player
- [ ] Ground cells convert to Air when dug
- [ ] Dirt particles spawn above dug area
- [ ] Dirt particles have visible upward motion
- [ ] Dirt particles spread horizontally
- [ ] Dirt particles fall back down and pile up
- [ ] Terrain collider updates after digging (player can walk through dug area)
- [ ] Multiple digs work correctly
- [ ] Digging at world boundaries doesn't crash

---

## Future Extensions

1. **Dig Sound Effect** - Play audio on successful dig
2. **Dig Animation** - Visual effect at dig point
3. **Dig Cooldown** - Prevent spam clicking
4. **Variable Dig Radius** - Different shovels have different radii
5. **Material Drops** - Some Ground cells drop special items
6. **Dig Particles** - Unity particle system for dust/debris
7. **Hardness System** - Some ground takes multiple hits

---

## Design Principles

Following the project's architecture philosophy:

- **Game layer orchestrates** - DiggingController is game logic
- **Simulation layer executes** - CellWorld handles cell data
- **Single source of truth** - SimulationManager singleton
- **No special cases in Simulation** - Ground is just a static material with a flag
- **Clean separation** - Game knows about Tools, Simulation knows about Materials
