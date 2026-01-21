# Belt System Implementation Plan

## Goal

Implement conveyor belts that move cells and clusters horizontally. Belts are structures made of individual tiles placed adjacently, which move materials in a specified direction.

---

## Design Summary

From our discussion:
- Belts move **entire columns** of cells, not just the surface layer
- Columns stop at obstructions (solid tiles, other belt structures)
- Cell simulation handles redistribution (falling, diagonals) after belt movement
- Clusters on belts receive force applied to their Rigidbody2D
- Multiple belts run in parallel via Unity Job System

---

## File Structure

```
Assets/Scripts/Structures/
├── StructureType.cs         # Enum for structure types (Belt=1, Lift=2, etc.)
├── BeltTile.cs              # Individual belt tile data
├── BeltStructure.cs         # Grouped belt (contiguous tiles)
├── BeltManager.cs           # Manages all belts, runs simulation
└── BeltSimulationJob.cs     # Burst-compiled parallel job

Assets/Scripts/Simulation/
└── CellSimulatorJobbed.cs   # Modified to call BeltManager
```

### StructureType Enum

```csharp
public enum StructureType : byte
{
    None = 0,
    Belt = 1,
    Lift = 2,    // Future
    Furnace = 3, // Future
    Press = 4,   // Future
}
```

This value is stored in `Cell.structureId` to mark cells as part of a structure.

---

## Data Structures

### Storage Approach

**Hybrid approach using `Cell.structureId` as type flag:**
- `Cell.structureId = StructureType.Belt` (constant, e.g., 1) marks belt tiles
- Enables quick "is this a structure?" checks and `HasStructure` chunk flag
- BeltManager owns all belt-specific data via HashMap
- Other structure types (lifts, furnaces) use different `structureId` values

### BeltTile

Stored in BeltManager's HashMap (keyed by position):

```csharp
public struct BeltTile
{
    public sbyte direction;    // +1 (right) or -1 (left)
    public ushort beltId;      // Which BeltStructure this belongs to
}
```

### BeltStructure

A contiguous run of belt tiles (grouped on placement):

```csharp
public struct BeltStructure
{
    public ushort id;          // Unique identifier
    public int tileY;          // Y coordinate of belt tiles
    public int minX, maxX;     // X range of belt
    public sbyte direction;    // +1 (right) or -1 (left)
    public byte speed;         // Frames per move (1 = every frame, 3 = every 3 frames, etc.)
    public byte frameOffset;   // For staggered movement timing
}
// Note: surfaceY (where cells sit) = tileY + 1
```

### BeltManager

Manages all belts and orchestrates simulation:

```csharp
public class BeltManager : IDisposable
{
    private NativeList<BeltStructure> belts;
    private NativeHashMap<ushort, BeltStructure> beltLookup;  // ID → structure
    private NativeHashMap<int, BeltTile> beltTiles;           // position → tile (key = y * width + x)
    private ushort nextBeltId = 1;
}
```

---

## Pipeline Integration

Current pipeline:
```
1. Cluster Physics (StepAndSync)
2. Cell Simulation (4-pass parallel)
3. Render
```

New pipeline:
```
1. Belt Forces → Clusters (apply force to Rigidbody2D)
2. Cluster Physics (StepAndSync)
3. Belt Cell Movement (parallel per-belt)
4. Cell Simulation (4-pass parallel)
5. Render
```

### In CellSimulatorJobbed.Simulate():

```csharp
public void Simulate(CellWorld world, ClusterManager clusterManager, BeltManager beltManager)
{
    world.currentFrame++;

    // 1. Apply belt forces to clusters (before physics step)
    if (beltManager != null && clusterManager != null)
    {
        beltManager.ApplyForcesToClusters(clusterManager);
    }

    // 2. Cluster physics
    if (clusterManager != null)
    {
        clusterManager.StepAndSync(Time.fixedDeltaTime);
    }

    // 3. Belt cell movement
    if (beltManager != null)
    {
        beltManager.SimulateBelts(world);
    }

    // 4. Cell simulation (existing 4-pass)
    // ... existing code ...
}
```

---

## Belt Cell Movement

### BeltSimulationJob

```csharp
[BurstCompile]
public struct BeltSimulationJob : IJobParallelFor
{
    [NativeDisableParallelForRestriction]
    public NativeArray<Cell> cells;

    [NativeDisableParallelForRestriction]
    public NativeArray<ChunkState> chunks;

    [ReadOnly]
    public NativeArray<MaterialDef> materials;

    [ReadOnly]
    public NativeArray<BeltStructure> belts;

    public int width;
    public int height;
    public int chunksX;
    public ushort currentFrame;

    public void Execute(int beltIndex)
    {
        BeltStructure belt = belts[beltIndex];

        // Speed control: only move every N frames
        if ((currentFrame + belt.frameOffset) % belt.speed != 0)
            return;

        // Surface is one cell above belt tiles
        int surfaceY = belt.tileY + 1;

        // Process columns in movement direction to avoid overwriting
        int startX = belt.direction > 0 ? belt.maxX : belt.minX;
        int endX = belt.direction > 0 ? belt.minX - 1 : belt.maxX + 1;
        int step = -belt.direction;

        for (int x = startX; x != endX; x += step)
        {
            MoveColumn(x, surfaceY, belt.direction);
        }
    }

    private void MoveColumn(int x, int surfaceY, int direction)
    {
        int destX = x + direction;

        // Check bounds
        if (destX < 0 || destX >= width)
            return;

        // Check if there's a cell on the belt surface
        int surfaceIndex = surfaceY * width + x;
        Cell surfaceCell = cells[surfaceIndex];

        if (IsAirOrStatic(surfaceCell))
            return;

        // Check if surface destination is clear
        int destSurfaceIndex = surfaceY * width + destX;
        if (!IsAir(cells[destSurfaceIndex]))
            return;  // Blocked, belt backs up

        // Move column: from surface upward until air or obstruction
        for (int y = surfaceY; y < height; y++)
        {
            int srcIndex = y * width + x;
            int dstIndex = y * width + destX;

            Cell srcCell = cells[srcIndex];

            // Stop at air (top of pile)
            if (IsAir(srcCell))
                break;

            // Stop at static/solid (obstruction)
            if (IsStatic(srcCell))
                break;

            // Stop if destination is blocked
            Cell dstCell = cells[dstIndex];
            if (!IsAir(dstCell))
                break;

            // Move the cell
            cells[dstIndex] = srcCell;
            cells[srcIndex] = CreateAirCell();

            // Mark chunks dirty
            MarkDirty(x, y);
            MarkDirty(destX, y);
        }
    }

    // Helper methods...
}
```

---

## Belt-Cluster Interaction

Clusters resting on belts receive horizontal force:

```csharp
public void ApplyForcesToClusters(ClusterManager clusterManager)
{
    foreach (var cluster in clusterManager.GetAllClusters())
    {
        // Check each belt
        foreach (var belt in belts)
        {
            if (ClusterRestingOnBelt(cluster, belt))
            {
                // Apply horizontal force
                float force = belt.direction * BeltForceStrength;
                cluster.rb.AddForce(new Vector2(force, 0));
            }
        }
    }
}

private bool ClusterRestingOnBelt(ClusterData cluster, BeltStructure belt)
{
    // Surface is one above belt tiles, cluster rests on surface
    int surfaceY = belt.tileY + 1;

    // Check if any cluster pixel is on the belt surface
    foreach (var pixel in cluster.GetWorldPixels())
    {
        if (pixel.y == surfaceY &&
            pixel.x >= belt.minX && pixel.x <= belt.maxX)
        {
            return true;
        }
    }
    return false;
}
```

---

## Belt Tile Placement

### Player Controls

- **Left-click drag**: Place belt tiles along drag path (horizontal only, same Y)
- **Left-click**: Place single belt tile
- **Q/E**: Rotate belt direction (left/right)
- **Right-click**: Remove belt tile

### Placing a Belt Tile

```csharp
public void PlaceBeltTile(int x, int y, sbyte direction)
{
    BeltTile tile = new BeltTile { x = x, y = y, direction = direction };

    // Check for adjacent belt tiles with same direction
    BeltStructure? leftBelt = GetBeltAt(x - 1, y);
    BeltStructure? rightBelt = GetBeltAt(x + 1, y);

    if (leftBelt.HasValue && leftBelt.Value.direction == direction)
    {
        // Extend left belt to include this tile
        ExtendBelt(leftBelt.Value.id, x);
        tile.beltId = leftBelt.Value.id;

        // Merge with right belt if it exists and matches
        if (rightBelt.HasValue && rightBelt.Value.direction == direction)
        {
            MergeBelts(leftBelt.Value.id, rightBelt.Value.id);
        }
    }
    else if (rightBelt.HasValue && rightBelt.Value.direction == direction)
    {
        // Extend right belt to include this tile
        ExtendBelt(rightBelt.Value.id, x);
        tile.beltId = rightBelt.Value.id;
    }
    else
    {
        // Create new belt structure
        tile.beltId = CreateNewBelt(x, y, direction);
    }

    beltTiles[y * width + x] = tile;

    // Mark the cell as belt (solid/static)
    MarkCellAsBeltTile(x, y);
}
```

### Removing a Belt Tile

```csharp
public void RemoveBeltTile(int x, int y)
{
    if (!beltTiles.TryGetValue(y * width + x, out BeltTile tile))
        return;

    BeltStructure belt = beltLookup[tile.beltId];

    if (x == belt.minX && x == belt.maxX)
    {
        // Only tile in belt, remove entire belt
        RemoveBelt(tile.beltId);
    }
    else if (x == belt.minX)
    {
        // Left edge, shrink belt
        belt.minX = x + 1;
        UpdateBelt(belt);
    }
    else if (x == belt.maxX)
    {
        // Right edge, shrink belt
        belt.maxX = x - 1;
        UpdateBelt(belt);
    }
    else
    {
        // Middle tile - split belt into two
        SplitBelt(tile.beltId, x);
    }

    beltTiles.Remove(y * width + x);
    ClearBeltTileCell(x, y);
}
```

---

## Belt Speed Options

Rather than moving 1 cell per frame (very fast), use frame skipping:

| Speed Setting | Frames per Move | Cells/Second @ 60fps |
|---------------|-----------------|----------------------|
| Fast          | 1               | 60                   |
| Normal        | 3               | 20                   |
| Slow          | 6               | 10                   |

```csharp
// In BeltSimulationJob.Execute():
if ((currentFrame + belt.frameOffset) % belt.speed != 0)
    return;  // Skip this frame
```

The `frameOffset` allows different belts to move on different frames, spreading the load.

---

## Visual Representation

Belt tiles render as simple boxes with arrows indicating direction:
- Solid colored box for the belt tile itself
- Arrow overlay showing flow direction (left or right)
- Animation can be added later if needed

---

## Chunk Dirty Marking

Belt movement must mark affected chunks dirty:

```csharp
private void MarkDirty(int x, int y)
{
    int chunkX = x >> 5;  // / 32
    int chunkY = y >> 5;
    int chunkIndex = chunkY * chunksX + chunkX;

    ChunkState chunk = chunks[chunkIndex];
    chunk.flags |= ChunkFlags.IsDirty;

    int localX = x & 31;
    int localY = y & 31;

    chunk.minX = (ushort)math.min(chunk.minX, localX);
    chunk.maxX = (ushort)math.max(chunk.maxX, localX);
    chunk.minY = (ushort)math.min(chunk.minY, localY);
    chunk.maxY = (ushort)math.max(chunk.maxY, localY);

    chunks[chunkIndex] = chunk;
}
```

---

## Implementation Order

### Phase 1: Core Belt Structure
1. Create `BeltTile`, `BeltStructure`, `BeltManager` classes
2. Implement belt tile placement (without grouping - single-tile belts)
3. Test: Place belt tiles, verify they're solid

### Phase 2: Belt Cell Movement
4. Implement `BeltSimulationJob` with column movement
5. Integrate into `CellSimulatorJobbed` pipeline
6. Test: Sand on single-tile belt moves

### Phase 3: Belt Grouping
7. Implement adjacent tile grouping on placement
8. Implement belt splitting on tile removal
9. Test: Multi-tile belts work correctly

### Phase 4: Cluster Integration
10. Implement `ApplyForcesToClusters`
11. Test: Clusters on belts move

### Phase 5: Polish
12. Add belt speed configuration
13. Add belt visuals/animation
14. Performance testing with many belts

---

## Edge Cases

1. **Belt at world edge**: Hard boundary - material stops, doesn't wrap
2. **Belt into wall**: Material backs up naturally (column movement checks destination)
3. **Opposing belts meeting**: Material piles up in the middle (both try to push to center, blocked)
4. **Overlapping belts**: Prevent placement on same tile
5. **Cluster partially on belt**: Apply force if any pixel touches belt surface
6. **Liquid on belt**: Same as powder (column movement)
7. **Gas on belt**: Moves with column (cell simulation handles rising afterward)

---

## Decisions

1. **Belt tile storage**: Hybrid approach - `Cell.structureId` as type flag (`StructureType.Belt`), BeltManager owns detailed data via `NativeHashMap<int, BeltTile>`
2. **Belt surface vs structure**: Surface is directly above the belt tile (`surfaceY = tileY + 1`)
3. **Vertical belts (lifts)**: Separate system - no vertical belts, lifts will work differently
4. **Belt corners/curves**: Not supported
5. **Placement controls**: Click-drag to place multiple tiles, single click for one tile, Q/E to rotate direction
6. **Rendering**: Simple boxes with direction arrows (no animation initially)
7. **Liquids**: Affected by belt movement (column movement works for all non-static materials)

---

## Design Principles

Following the project's architecture philosophy:

- **Systems not patches**: BeltManager is a unified system for all belt behavior
- **Single source of truth**: Belt structures stored in BeltManager
- **No special cases**: All belts follow the same simulation logic
- **Parallel-first**: Job System for belt simulation from the start
