# Structure Placement Through Terrain

## Problem
Currently, structures (belts, lifts) can only be placed in empty air. This creates a frustrating workflow:

1. Player digs out ground
2. Dirt falls and piles up
3. Player must manually move dirt out of the way
4. Player places automation structures
5. Player then needs to dig more and hope dirt reaches the structures

**Desired workflow:**
1. Player places lift/belt through solid ground
2. Player digs - dirt immediately falls onto the lift
3. Automation handles transport from the start

## Solution: Ghost Structures

Allow structures to be placed through certain materials. Structures placed in occupied cells enter a "ghost" state: rendered as a faded semi-transparent version, inactive (don't transport materials), and automatically activate when the blocking terrain is cleared. Ghost state is tracked **per 8x8 block**, not per entire merged structure.

### Material Categories

**Can Place Through:**
- **Ground** - Main diggable terrain (Static, Diggable)
- **Dirt** - Loose powder
- **Sand** - Loose powder
- **Water** - Liquid

**Cannot Place Through:**
- **Stone** - Permanent terrain
- **Other structures** - No overlapping belts/lifts
- **Wall** - Player-placed solid blocks

### Activation Rules (Using Existing `MaterialFlags.Passable`)

The existing `MaterialFlags.Passable` flag (`MaterialDef.cs:22`) already distinguishes structure types:
- Lift materials have `Passable` flag set (density 0, cells pass through)
- Belt materials do NOT have `Passable` (density 255, solid)

**Non-Passable structures (Belts):** Ghost block activates when ALL 64 cells in the 8x8 block are `Materials.Air`. Belts are solid -- they need empty space to exist.

**Passable structures (Lifts):** Ghost block activates when ALL 64 cells in the 8x8 block contain NO `Materials.Ground`. Loose powder (Dirt, Sand) and liquid (Water) are fine -- once the lift activates, those materials immediately receive lift force and begin rising.

## Data Model Changes

### BeltTile (modify existing)
File: `Assets/Scripts/Structures/BeltTile.cs`

```csharp
public struct BeltTile
{
    public sbyte direction;
    public ushort beltId;
    public bool isGhost;    // NEW: true if blocked by terrain
}
```

### LiftTile (modify existing)
File: `Assets/Scripts/Structures/LiftTile.cs`

```csharp
public struct LiftTile
{
    public ushort liftId;
    public byte materialId;  // EXISTING: lift material for MoveCell() restore
    public bool isGhost;     // NEW: true if blocked by terrain
}
```

The `materialId` field already exists -- it's used by `SimulateChunksJob.MoveCell()` to restore lift material when cells move out of lift zones. Ghost lift activation sets this field so the restore mechanism works immediately after activation.

Note: There is no unified `StructureTile` -- belts and lifts use separate tile types with different storage (BeltTile in `NativeHashMap<int, BeltTile>`, LiftTile in `NativeArray<LiftTile>`).

## Placement Changes

### CanPlaceStructureAt() - Relax Validation
File: `Assets/Scripts/Game/Structures/StructurePlacementController.cs`

Currently (`StructurePlacementController.cs:298-330`) requires all cells to be `Materials.Air`. Change to:

```csharp
private enum PlacementResult { Valid, ValidGhost, Invalid }

private PlacementResult CanPlaceStructureAt(int gridX, int gridY)
{
    var world = simulation.World;

    if (!world.IsInBounds(gridX, gridY) ||
        !world.IsInBounds(gridX + 7, gridY + 7))
        return PlacementResult.Invalid;

    bool anyOccupied = false;

    for (int dy = 0; dy < 8; dy++)
    {
        for (int dx = 0; dx < 8; dx++)
        {
            int cx = gridX + dx;
            int cy = gridY + dy;

            // Existing structure = always invalid
            if (simulation.BeltManager.HasBeltAt(cx, cy))
                return PlacementResult.Invalid;
            if (simulation.LiftManager.HasLiftAt(cx, cy))
                return PlacementResult.Invalid;

            byte mat = world.GetCell(cx, cy);
            if (mat == Materials.Air)
                continue;

            // Cannot place through Stone, Wall, or structure materials
            if (mat == Materials.Stone || mat == Materials.Wall ||
                Materials.IsBelt(mat) || Materials.IsLift(mat))
                return PlacementResult.Invalid;

            // Ground, Dirt, Sand, Water = allowed but ghost
            anyOccupied = true;
        }
    }

    return anyOccupied ? PlacementResult.ValidGhost : PlacementResult.Valid;
}
```

### BeltManager.PlaceBelt() - Ghost Path
File: `Assets/Scripts/Structures/BeltManager.cs`

Currently (`BeltManager.cs:76-92`) rejects non-Air cells. Change the validation loop:

```csharp
// Check if entire 8x8 area allows placement
bool anyGhost = false;
for (int dy = 0; dy < BeltStructure.Height; dy++)
{
    for (int dx = 0; dx < BeltStructure.Width; dx++)
    {
        int cx = gridX + dx;
        int cy = gridY + dy;
        int posKey = cy * width + cx;

        if (beltTiles.ContainsKey(posKey))
            return false;

        byte existingMaterial = world.GetCell(cx, cy);
        if (existingMaterial == Materials.Air)
            continue;

        // Allow placement through soft materials
        if (existingMaterial == Materials.Ground ||
            existingMaterial == Materials.Dirt ||
            existingMaterial == Materials.Sand ||
            existingMaterial == Materials.Water)
        {
            anyGhost = true;
            continue;
        }

        return false;  // Stone, Wall, other structures = blocked
    }
}
```

Then in the cell-filling loop (`BeltManager.cs:198-217`), **skip writing cell data for ghost tiles**:

```csharp
BeltTile tile = new BeltTile
{
    direction = direction,
    beltId = beltId,
    isGhost = anyGhost,  // Entire 8x8 block shares ghost state
};

for (int dy = 0; dy < BeltStructure.Height; dy++)
{
    for (int dx = 0; dx < BeltStructure.Width; dx++)
    {
        int cx = gridX + dx;
        int cy = gridY + dy;
        int posKey = cy * width + cx;

        beltTiles.Add(posKey, tile);

        if (!anyGhost)
        {
            // Normal path: write belt material to cell
            int cellIndex = cy * width + cx;
            Cell cell = world.cells[cellIndex];
            cell.materialId = GetBeltMaterialForChevron(cx, cy, direction);
            cell.structureId = (byte)StructureType.Belt;
            world.cells[cellIndex] = cell;
            world.MarkDirty(cx, cy);
        }
        // Ghost path: cell data untouched, terrain material remains
    }
}
```

### LiftManager.PlaceLift() - Ghost Path
File: `Assets/Scripts/Structures/LiftManager.cs`

Same pattern as belts. Currently (`LiftManager.cs:75-92`) rejects non-Air/non-Lift cells. Relax validation to allow soft materials, set `isGhost = true` on the `LiftTile`, and **skip writing lift material to cells** when ghost.

```csharp
LiftTile tile = new LiftTile
{
    liftId = liftId,
    isGhost = anyGhost,
};

for (int dy = 0; dy < LiftStructure.Height; dy++)
{
    for (int dx = 0; dx < LiftStructure.Width; dx++)
    {
        int cx = gridX + dx;
        int cy = gridY + dy;
        int posKey = cy * width + cx;

        liftTiles[posKey] = tile;

        if (!anyGhost)
        {
            // Normal path: write lift material
            byte liftMaterial = GetLiftMaterialForPattern(cx, cy);
            world.SetCell(cx, cy, liftMaterial);
            world.MarkDirty(cx, cy);
        }
        // Ghost path: cell data untouched
    }
}
```

### Ghost Structure Removal

Ghost structures can be removed with right-click, same as active structures. Since ghost structures don't modify cell data, removal only needs to clear the tile data structures (`beltTiles` HashMap or `liftTiles` array). The terrain underneath is unaffected.

The existing `RemoveBelt()` and `RemoveLift()` methods need a ghost check: if the tile being removed is ghost, skip the cell-clearing step (there's nothing to clear -- the cells still have terrain material).

## Simulation Changes

### SimulateBeltsJob - Skip Ghost Tiles
File: `Assets/Scripts/Simulation/Jobs/SimulateBeltsJob.cs`

The belt simulation job reads `beltTiles` to move cells on belt surfaces. Ghost belt tiles must be skipped:

```csharp
// When iterating belt tiles in the job:
if (tile.isGhost)
    continue;
```

### SimulateChunksJob - Skip Ghost Lift Tiles
File: `Assets/Scripts/Simulation/Jobs/SimulateChunksJob.cs`

Lift force is applied at `SimulateChunksJob.cs:153-160` by checking `liftTiles[index].liftId != 0`. Ghost lift tiles must not apply force:

```csharp
// Change from:
bool inLift = liftTiles.IsCreated && liftTiles[y * width + x].liftId != 0;

// To:
bool inLift = false;
if (liftTiles.IsCreated)
{
    var lt = liftTiles[y * width + x];
    inLift = lt.liftId != 0 && !lt.isGhost;
}
```

Same change needed for `SimulateLiquid()` if it has equivalent lift-zone checks.

### BeltManager.ApplyForcesToClusters() - Skip Ghost
File: `Assets/Scripts/Structures/BeltManager.cs`

Belt cluster force logic (`BeltManager.cs:668-737`) should skip ghost belt tiles when checking for belt-cluster contact.

### LiftManager.ApplyForcesToClusters() - Skip Ghost
File: `Assets/Scripts/Structures/LiftManager.cs`

Lift cluster force logic (`LiftManager.cs:356-406`) should skip if any involved tiles are ghost.

## Ghost Activation System

### Where It Runs

Ghost activation checks run on the **main thread**, once per frame, **before** the simulation step. This ensures that when a ghost activates, the cell data is written before the simulation processes it.

Add an `UpdateGhostStates()` method to both `BeltManager` and `LiftManager`, called from `SimulationManager.Update()` before `simulator.Simulate()`.

### BeltManager.UpdateGhostStates()

Iterate all ghost belt tiles. For each ghost 8x8 block, check if ALL 64 cells are `Materials.Air`. If so, activate: write belt material + structureId to cells, set `isGhost = false` on all tiles in the block.

```csharp
public void UpdateGhostStates()
{
    // Track which blocks we've already checked (by grid-snapped position)
    // to avoid checking the same 8x8 block 64 times
    var checkedBlocks = new NativeHashSet<int>(16, Allocator.Temp);

    var keys = beltTiles.GetKeyArray(Allocator.Temp);
    for (int i = 0; i < keys.Length; i++)
    {
        int posKey = keys[i];
        BeltTile tile = beltTiles[posKey];
        if (!tile.isGhost) continue;

        // Snap to block origin
        int cellY = posKey / width;
        int cellX = posKey - cellY * width;
        int blockX = SnapToGrid(cellX);
        int blockY = SnapToGrid(cellY);
        int blockKey = blockY * width + blockX;

        if (checkedBlocks.Contains(blockKey)) continue;
        checkedBlocks.Add(blockKey);

        // Check all 64 cells in block
        bool allAir = true;
        for (int dy = 0; dy < BeltStructure.Height && allAir; dy++)
            for (int dx = 0; dx < BeltStructure.Width && allAir; dx++)
                if (world.GetCell(blockX + dx, blockY + dy) != Materials.Air)
                    allAir = false;

        if (allAir)
            ActivateGhostBlock(blockX, blockY, tile.direction, tile.beltId);
    }

    keys.Dispose();
    checkedBlocks.Dispose();
}

private void ActivateGhostBlock(int blockX, int blockY, sbyte direction, ushort beltId)
{
    for (int dy = 0; dy < BeltStructure.Height; dy++)
    {
        for (int dx = 0; dx < BeltStructure.Width; dx++)
        {
            int cx = blockX + dx;
            int cy = blockY + dy;
            int posKey = cy * width + cx;

            // Update tile
            var tile = beltTiles[posKey];
            tile.isGhost = false;
            beltTiles.Remove(posKey);
            beltTiles.Add(posKey, tile);

            // Write belt material to cell (same as normal placement)
            int cellIndex = cy * width + cx;
            Cell cell = world.cells[cellIndex];
            cell.materialId = GetBeltMaterialForChevron(cx, cy, direction);
            cell.structureId = (byte)StructureType.Belt;
            world.cells[cellIndex] = cell;
            world.MarkDirty(cx, cy);
        }
    }
}
```

### LiftManager.UpdateGhostStates()

Same pattern, but checks for NO `Materials.Ground` cells instead of ALL Air:

```csharp
// Check all 64 cells in block
bool noGround = true;
for (int dy = 0; dy < LiftStructure.Height && noGround; dy++)
    for (int dx = 0; dx < LiftStructure.Width && noGround; dx++)
        if (world.GetCell(blockX + dx, blockY + dy) == Materials.Ground)
            noGround = false;

if (noGround)
    ActivateGhostBlock(blockX, blockY, liftId);
```

On lift activation, only write lift materials to cells that are currently Air. Leave Dirt/Sand/Water cells untouched. The existing `MoveCell()` mechanism in `SimulateChunksJob.cs:687-691` handles the rest: when a cell moves out of a lift tile position, it checks `liftTiles[fromIndex].liftId != 0` and restores the lift material from `liftTiles[fromIndex].materialId`. This is the same mechanism that already handles dirt passing through active lifts during normal gameplay -- no material is destroyed.

```csharp
private void ActivateGhostBlock(int blockX, int blockY, ushort liftId)
{
    for (int dy = 0; dy < LiftStructure.Height; dy++)
    {
        for (int dx = 0; dx < LiftStructure.Width; dx++)
        {
            int cx = blockX + dx;
            int cy = blockY + dy;
            int posKey = cy * width + cx;

            // Update tile: clear ghost, store the lift material for MoveCell restore
            byte liftMaterial = GetLiftMaterialForPattern(cx, cy);
            var tile = liftTiles[posKey];
            tile.isGhost = false;
            tile.materialId = liftMaterial;
            liftTiles[posKey] = tile;

            // Only write lift material to empty cells
            // Occupied cells (Dirt, Sand, Water) keep their material —
            // when they move out, MoveCell() auto-restores lift material from liftTiles.materialId
            if (world.GetCell(cx, cy) == Materials.Air)
            {
                world.SetCell(cx, cy, liftMaterial);
            }

            world.MarkDirty(cx, cy);
        }
    }
}

## Rendering Ghost Structures

### The Problem

The `CellRenderer` draws based on `cell.materialId`. Ghost structures do NOT write their materials to cells (the terrain material stays). So ghost structures are invisible to the normal renderer.

### Solution: Ghost Overlay Sprites

Create semi-transparent sprites for each ghost 8x8 block, similar to how the placement preview already works (`StructurePlacementController.CreatePreviewObject()` at line 332).

Add a `GhostOverlayRenderer` component (or integrate into existing structure managers) that:

1. Maintains a pool of `SpriteRenderer` GameObjects
2. Each frame, positions sprites at ghost block locations
3. Uses a faded, semi-transparent version of the structure color
4. Belt ghosts: faded gray with directional arrow/chevron pattern
5. Lift ghosts: faded green with upward arrow pattern

```csharp
// Ghost block visual
Color ghostBeltColor = new Color(0.3f, 0.3f, 0.4f, 0.35f);   // Faded gray
Color ghostLiftColor = new Color(0.3f, 0.5f, 0.3f, 0.35f);   // Faded green
```

This approach is cheap -- typically only a handful of ghost blocks exist at once. A few extra sprites is trivial for Unity.

### Placement Preview Update

Update `StructurePlacementController.UpdatePreview()` to show three states:
- **Green** - Empty area, structure will be immediately active
- **Blue/Cyan** - Occupied by soft terrain, structure will be ghost
- **Red** - Stone, Wall, or existing structure, cannot place

## Structure Merging with Ghost Blocks

When placing a ghost block adjacent to an existing belt/lift, the merging logic still runs. A merged structure can have a mix of active and ghost blocks. This is fine because ghost state is per-block, not per-structure.

Example: Active belt at X=0, ghost belt placed at X=8 in ground. They merge into one `BeltStructure` spanning X=0..15. The X=0 block has `isGhost=false`, the X=8 block has `isGhost=true`. Belt simulation processes only the active tiles.

## Files to Modify

### Tile Data
- `Assets/Scripts/Structures/BeltTile.cs` - Add `isGhost` field
- `Assets/Scripts/Structures/LiftTile.cs` - Add `isGhost` field

### Placement Validation
- `Assets/Scripts/Structures/BeltManager.cs` - Relax Air-only check, ghost placement path, `UpdateGhostStates()`, skip ghost in `RemoveBelt()` cell-clearing
- `Assets/Scripts/Structures/LiftManager.cs` - Same changes for lifts
- `Assets/Scripts/Game/Structures/StructurePlacementController.cs` - Three-state validation (Valid/ValidGhost/Invalid), preview colors

### Simulation Jobs (Burst)
- `Assets/Scripts/Simulation/Jobs/SimulateChunksJob.cs` - Check `!isGhost` for lift force application
- `Assets/Scripts/Simulation/Jobs/SimulateBeltsJob.cs` - Skip ghost belt tiles

### Simulation Pipeline
- `Assets/Scripts/Simulation/SimulationManager.cs` - Call `UpdateGhostStates()` on both managers before `simulator.Simulate()`
- `Assets/Scripts/Simulation/CellSimulatorJobbed.cs` - If cluster force methods need ghost checks

### Rendering
- New `GhostOverlayRenderer` or integrated into managers - Semi-transparent sprites at ghost block positions

## Edge Cases

### Dirt Falls Into Ghost Belt Area
Belt stays ghost (non-passable, needs Air). Player must dig/grab the loose dirt for the belt to activate. This is intentional -- belts are solid structures that need clear space.

### Dirt Present When Ghost Lift Activates
Not a problem. On activation, lift material is only written to Air cells. Dirt cells keep their material, but `liftTiles` is set to active, so the simulation applies lift force. When dirt moves out, `MoveCell()` auto-restores lift material from `liftTiles.materialId`. Same mechanism as normal lift operation -- no material lost.

### Player Digs Under Ghost Belt
Ghost belt in ground. Player digs below it. Ground under the belt is gone, but ground IN the belt remains. Player then digs the belt area itself. Belt activates (all cells now Air), floating in space. Normal behavior -- structures don't require support.

### Partially Ghost Merged Structure
A merged 3-block lift with blocks at Y=0 (active), Y=8 (ghost), Y=16 (active). The ghost block in the middle doesn't apply lift force. Cells reaching Y=8 from below lose lift force and may fall back. Once the ghost at Y=8 activates, the full lift works. This is correct and intuitive behavior.

### Ghost Block at World Edge
Standard bounds checking applies. If any part of the 8x8 block is out of bounds, placement is rejected (same as current behavior).

## Testing Checklist
- [ ] Can place belt through Ground material (ghost state)
- [ ] Can place belt through Dirt material (ghost state)
- [ ] Can place lift through Ground material (ghost state)
- [ ] Cannot place through Stone
- [ ] Cannot place through Wall
- [ ] Cannot overlap existing structures
- [ ] Ghost belt renders as faded semi-transparent overlay
- [ ] Ghost lift renders as faded semi-transparent overlay
- [ ] Ghost belt activates when all cells become Air
- [ ] Ghost lift activates when all Ground cells are removed (powder OK)
- [ ] Dirt present during lift activation is not destroyed (lift force applies, MoveCell restores lift material)
- [ ] On activation, structure materials are written to cells correctly
- [ ] Belt structureId is set on activation
- [ ] Ghost structures can be removed with right-click
- [ ] Removing ghost structure doesn't affect terrain
- [ ] Preview shows green (active), blue (ghost), red (invalid)
- [ ] Mixed active/ghost blocks in merged structure work correctly
- [ ] Ghost belt tiles skipped by SimulateBeltsJob
- [ ] Ghost lift tiles skipped by SimulateChunksJob lift force
- [ ] Ghost structures don't affect terrain colliders
- [ ] Ghost structures don't apply forces to clusters
- [ ] Performance OK with ghost checks each frame

## Future Considerations
- Ghost structures could show a "dig here" indicator
- Sound/particle effect when ghost activates
- Tutorial hint: "Place structures through terrain, then dig!"
