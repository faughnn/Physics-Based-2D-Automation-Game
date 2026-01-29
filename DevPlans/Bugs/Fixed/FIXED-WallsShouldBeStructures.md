# Walls Should Be Structures

## Status: OPEN

## Summary

Walls are currently implemented as raw material stamps (`Materials.Wall`) rather than managed structures. They should be proper structures with a manager, tile tracking, and ghost support — consistent with how belts and lifts work.

## Current Behavior

- Walls exist only as a sandbox feature (`SandboxController`, W key)
- Placement writes `Materials.Wall` bytes directly into the cell grid (8x8 fill)
- No `WallManager`, no tile tracking, no ghost state
- Walls are not available in the Game scene at all
- `StructureType` enum has no `Wall` entry

## Expected Behavior

- Walls should be a proper structure type with a `WallManager` (like `BeltManager` / `LiftManager`)
- Walls should support ghost placement through soft terrain
- Walls should be available in the Game scene via `StructurePlacementController`
- Walls should unlock alongside lifts (same progression tier: `Ability.PlaceLifts`)
- `StructureType` enum should include a `Wall` entry

---

## Design Decisions

### Walls Are Purely Static
Unlike belts (which move cells) and lifts (which apply forces), walls have no simulation behavior. They are simply solid material blocks that exist in the cell world.

**Implications:**
- No per-frame simulation updates needed
- No `HasStructure` chunk flag required (walls don't need active chunk simulation)
- No `cell.structureId` assignment needed (no structure lookup during simulation)
- No merging of adjacent wall blocks (merging serves no purpose for static blockers)

### Simplified Manager
`WallManager` will be simpler than `BeltManager`/`LiftManager`:
- Tile tracking for placement/removal queries
- Ghost state tracking and activation
- No structure grouping, no simulation methods

---

## Implementation Plan

### 1. Add Wall to StructureType enum
**File:** `Assets/Scripts/Structures/StructureType.cs`
- Add `Wall = 5` to the enum (after Press = 4)

### 2. Create WallTile struct
**File:** `Assets/Scripts/Structures/Wall/WallTile.cs` (new)
```csharp
public struct WallTile
{
    public bool exists;
    public bool isGhost;
    public int blockOriginX;  // Origin of the 8x8 block this tile belongs to
    public int blockOriginY;
}
```

### 3. Create WallManager
**File:** `Assets/Scripts/Structures/Wall/WallManager.cs` (new)

**Data structures:**
- `NativeArray<WallTile> tiles` — parallel to cell array, tracks wall tiles
- `NativeHashSet<int> ghostBlockOrigins` — tracks ghost block positions for rendering

**Methods:**
| Method | Purpose |
|--------|---------|
| `Initialize(width, height)` | Allocate native collections |
| `PlaceWall(x, y, CellWorld)` | Place 8x8 block, snap to grid, handle ghost state |
| `RemoveWall(x, y, CellWorld)` | Remove 8x8 block containing position |
| `HasWallAt(x, y)` | Check if wall tile exists at position |
| `UpdateGhostStates(CellWorld)` | Check ghost blocks, activate when terrain clears |
| `GetGhostBlockPositions(List<Vector2Int>)` | For ghost renderer |
| `Dispose()` | Clean up native collections |

**PlaceWall logic:**
1. Snap (x, y) to 8x8 grid: `originX = (x / 8) * 8`, `originY = (y / 8) * 8`
2. Check all 64 cells for soft terrain → if any, mark as ghost
3. If ghost: only write tile data, don't write materials
4. If not ghost: write `Materials.Wall` to all 64 cells, mark dirty

**UpdateGhostStates logic:**
1. Iterate `ghostBlockOrigins`
2. For each origin, check if all 64 cells are Air
3. If clear: write materials, clear `isGhost` flag, remove from ghost set

### 4. Integrate with SimulationManager
**File:** `Assets/Scripts/Simulation/SimulationManager.cs`

- Add `public WallManager WallManager { get; private set; }`
- In `Initialize()`: create WallManager, call `wallManager.Initialize(width, height)`
- In `Update()`: call `wallManager.UpdateGhostStates(cellWorld)` (before ghost renderer)
- In `OnDestroy()`: call `wallManager.Dispose()`

### 5. Update GhostStructureRenderer
**File:** `Assets/Scripts/Rendering/GhostStructureRenderer.cs`

- Add `WallManager wallManager` field
- Update `Initialize()` to accept WallManager
- In `Update()`: call `wallManager.GetGhostBlockPositions()`, render with wall ghost color

### 6. Update StructurePlacementController
**File:** `Assets/Scripts/Game/Structures/StructurePlacementController.cs`

- Add `Wall` to `PlacementMode` enum
- Add W key binding to toggle wall mode (check `Ability.PlaceLifts` for unlock)
- Add `HandleWallPlacement()` method (similar to belt/lift handlers)
- Update `CanPlaceStructureAt()` to check `wallManager.HasWallAt()`
- Update placement validation: walls follow same rules as belts (solid, can ghost through soft terrain)

### 7. Update SandboxController (optional cleanup)
**File:** `Assets/Scripts/SandboxController.cs`

- Replace direct material stamping with `SimulationManager.WallManager.PlaceWall()`
- This gives sandbox walls the same behavior as game walls (ghost support, proper tracking)

---

## Files to Create
- `Assets/Scripts/Structures/Wall/WallTile.cs`
- `Assets/Scripts/Structures/Wall/WallManager.cs`

## Files to Modify
- `Assets/Scripts/Structures/StructureType.cs` — add Wall entry
- `Assets/Scripts/Simulation/SimulationManager.cs` — create and manage WallManager
- `Assets/Scripts/Rendering/GhostStructureRenderer.cs` — render wall ghosts
- `Assets/Scripts/Game/Structures/StructurePlacementController.cs` — add wall placement mode
- `Assets/Scripts/SandboxController.cs` — use WallManager instead of direct stamping

---

## Testing Checklist
- [ ] Wall placement works in Game scene (W key when lifts unlocked)
- [ ] Wall placement works in Sandbox scene
- [ ] Walls block cell movement (sand, water, etc.)
- [ ] Ghost walls appear when placed through soft terrain
- [ ] Ghost walls activate when terrain is cleared
- [ ] Wall removal works correctly
- [ ] Cannot place walls over existing belts/lifts/walls
- [ ] Cannot place walls in hard terrain
