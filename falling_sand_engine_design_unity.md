# Falling Sand Engine Design Document (Unity)

## Overview

A 2D pixel physics engine inspired by Noita's "Falling Everything" architecture, built for Unity using the Job System and Burst compiler. Designed for an automation game where structures (belts, lifts, furnaces) physically interact with simulated materials.

### Design Goals

- Simulate a world spanning multiple screens (~4,000 × 2,000 cells, rendered as 8,000 × 4,000 pixels)
- **Entire world always active** - no streaming, no freezing distant areas
- Maintain 60 FPS through aggressive dirty rectangle optimisation
- **Cell-based world** - cells are the atomic unit, rendered as 2×2 pixel blocks
- **Unity Job System + Burst** for parallel CPU simulation
- **GPU rendering** via Texture2D and custom shaders
- Support complex material interactions (heat transfer, phase changes)
- Handle overflow and spillage as real physics events

### Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│                     Main Thread                         │
├─────────────────────────────────────────────────────────┤
│  1. Input & Game Logic                                  │
│  2. Structure Update (mark dirty regions)               │
│  3. Schedule Physics Jobs (4-pass, returns JobHandle)   │
│  4. ... do other work while jobs run ...                │
│  5. Complete Jobs                                       │
│  6. Upload dirty regions to GPU texture                 │
│  7. Render (GPU shader does colouring/effects)          │
└─────────────────────────────────────────────────────────┘
```

---

## Data Structures

All simulation data lives in **NativeArrays** to avoid garbage collection and enable Burst compilation.

### Cell Structure

Cells are the atomic unit of simulation. Each cell is rendered as a 2×2 pixel block.

```csharp
[StructLayout(LayoutKind.Sequential)]
public struct Cell
{
    public byte materialId;      // Index into material definitions
    public byte flags;           // State flags (see below)
    public ushort frameUpdated;  // Prevents double-processing per frame
    public sbyte velocityX;      // Horizontal velocity (-16 to +16 cells)
    public sbyte velocityY;      // Vertical velocity (-16 to +16 cells)
    public byte temperature;     // 0-255 for heat simulation
    public byte structureId;     // If attached to a structure, which one (0 = none)
}
// Size: 8 bytes per cell
```

### Cell Flags

```csharp
public static class CellFlags
{
    public const byte None        = 0;
    public const byte OnBelt      = 1 << 0;  // Being moved by a belt
    public const byte OnLift      = 1 << 1;  // Being moved by a lift
    public const byte Burning     = 1 << 2;  // Currently on fire
    public const byte Wet         = 1 << 3;  // In contact with liquid
}
```

### World Storage

```csharp
public class CellWorld : IDisposable
{
    // Dimensions (in cells)
    public readonly int width;
    public readonly int height;
    
    // Primary data (persistent allocation)
    public NativeArray<Cell> cells;
    
    // Material definitions (read-only during simulation)
    [ReadOnly] public NativeArray<MaterialDef> materials;
    
    // Chunk tracking
    public NativeArray<ChunkState> chunks;
    public readonly int chunksX;
    public readonly int chunksY;
    
    // Frame counter (wraps at 65535)
    public ushort currentFrame;
    
    public CellWorld(int width, int height)
    {
        this.width = width;
        this.height = height;
        
        cells = new NativeArray<Cell>(width * height, Allocator.Persistent);
        
        chunksX = (width + 31) / 32;  // Round up
        chunksY = (height + 31) / 32;
        chunks = new NativeArray<ChunkState>(chunksX * chunksY, Allocator.Persistent);
    }
    
    public void Dispose()
    {
        if (cells.IsCreated) cells.Dispose();
        if (materials.IsCreated) materials.Dispose();
        if (chunks.IsCreated) chunks.Dispose();
    }
}
```

### Coordinate Helpers

```csharp
public static class WorldUtils
{
    // Cell index to flat array index
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CellIndex(int x, int y, int width) => y * width + x;
    
    // Cell to chunk
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CellToChunkX(int cx) => cx >> 5;  // cx / 32
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CellToChunkY(int cy) => cy >> 5;  // cy / 32
    
    // Chunk to cell (top-left corner)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ChunkToCellX(int chunkX) => chunkX << 5;  // chunkX * 32
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ChunkToCellY(int chunkY) => chunkY << 5;  // chunkY * 32
}
```

---

## Material System

Materials are data-driven definitions stored in a NativeArray for Burst access.

### Material Definition

```csharp
[StructLayout(LayoutKind.Sequential)]
public struct MaterialDef
{
    public byte density;            // For displacement (0-255, higher sinks)
    public byte friction;           // Affects horizontal spread (0-255)
    public BehaviourType behaviour; // Powder, Liquid, Gas, Static
    public byte flags;              // MaterialFlags below
    
    public byte ignitionTemp;       // Temperature to catch fire (0 = won't burn)
    public byte meltTemp;           // Temperature to melt (0 = won't melt)
    public byte freezeTemp;         // Temperature to solidify (255 = won't freeze)
    public byte boilTemp;           // Temperature to evaporate (0 = won't boil)
    
    public byte materialOnMelt;     // Material ID when melted
    public byte materialOnFreeze;   // Material ID when frozen
    public byte materialOnBurn;     // Material ID when burned (ash, smoke)
    public byte materialOnBoil;     // Material ID when boiled (steam)
    
    public Color32 baseColour;      // RGBA for rendering
    public byte colourVariation;    // Random variation amount
    public byte padding1;
    public byte padding2;
    public byte padding3;
}

public enum BehaviourType : byte
{
    Static = 0,    // Never moves (stone, structure)
    Powder = 1,    // Falls, piles (sand, ore)
    Liquid = 2,    // Falls, spreads horizontally (water, oil)
    Gas = 3,       // Rises, disperses (steam, smoke)
}

public static class MaterialFlags
{
    public const byte None          = 0;
    public const byte ConductsHeat  = 1 << 0;
    public const byte Flammable     = 1 << 1;
    public const byte Conductive    = 1 << 2;  // Electricity
    public const byte Corrodes      = 1 << 3;  // Acid-like
}
```

### Example Materials

```csharp
public static class Materials
{
    public const byte Air = 0;
    public const byte Stone = 1;
    public const byte Sand = 2;
    public const byte Water = 3;
    public const byte Oil = 4;
    public const byte Steam = 5;
    public const byte IronOre = 6;
    public const byte MoltenIron = 7;
    public const byte Iron = 8;
    public const byte Coal = 9;
    public const byte Ash = 10;
    public const byte Smoke = 11;
    // ... etc
}

// Setup example:
materials[Materials.Sand] = new MaterialDef
{
    density = 128,
    friction = 20,
    behaviour = BehaviourType.Powder,
    flags = MaterialFlags.None,
    baseColour = new Color32(194, 178, 128, 255),
    colourVariation = 15,
};

materials[Materials.Water] = new MaterialDef
{
    density = 64,
    friction = 5,
    behaviour = BehaviourType.Liquid,
    flags = MaterialFlags.ConductsHeat,
    boilTemp = 100,
    materialOnBoil = Materials.Steam,
    baseColour = new Color32(32, 64, 192, 255),
    colourVariation = 10,
};
```

---

## Chunk System

### Chunk State

Each chunk (32×32 cells) tracks its dirty region and activity:

```csharp
[StructLayout(LayoutKind.Sequential)]
public struct ChunkState
{
    public ushort minX, minY;      // Dirty region bounds (local to chunk, 0-31)
    public ushort maxX, maxY;
    public byte flags;             // ChunkFlags below
    public byte activeLastFrame;   // Was dirty last frame? (for neighbour waking)
    public ushort structureMask;   // Bitmask of structures in this chunk
}

public static class ChunkFlags
{
    public const byte None         = 0;
    public const byte IsDirty      = 1 << 0;
    public const byte HasStructure = 1 << 1;  // Always simulates
}
```

### Chunk Grid Layout

For a 4,000 × 2,000 cell world:
- 125 × 63 chunks (32×32 cells each)
- 7,875 total chunks
- Most will be inactive (static terrain)
- Memory: 4,000 × 2,000 × 8 bytes = **64 MB** for cells

### Marking Dirty

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static void MarkDirty(
    ref NativeArray<ChunkState> chunks,
    int cellX, int cellY,
    int chunksX)
{
    int chunkX = cellX >> 5;  // / 32
    int chunkY = cellY >> 5;
    int chunkIndex = chunkY * chunksX + chunkX;
    
    int localX = cellX & 31;  // % 32
    int localY = cellY & 31;
    
    ChunkState chunk = chunks[chunkIndex];
    chunk.flags |= ChunkFlags.IsDirty;
    
    // Expand dirty bounds
    chunk.minX = (ushort)math.min(chunk.minX, localX);
    chunk.maxX = (ushort)math.max(chunk.maxX, localX);
    chunk.minY = (ushort)math.min(chunk.minY, localY);
    chunk.maxY = (ushort)math.max(chunk.maxY, localY);
    
    chunks[chunkIndex] = chunk;
}
```

---

## Multithreaded Simulation (Job System + Burst)

### The 4-Pass Checkerboard Pattern

Chunks are assigned to groups based on position:

```
A B A B A B A B
C D C D C D C D
A B A B A B A B
C D C D C D C D
```

Groups are processed sequentially (A → B → C → D), but chunks *within* each group run in parallel.

### Why 4 Groups?

Each chunk needs a 16-cell buffer in each direction for thread safety. With 4 groups, no simultaneously-processing chunks have overlapping buffers:

```
Pass 1 (A chunks only):
A . A .     A chunks are 2 apart in each direction
. . . .     64 cells between them = safe
A . A .
```

**Critical constraint**: No cell may move more than 16 cells per frame.

### Group Assignment

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static int GetChunkGroup(int chunkX, int chunkY)
{
    // A=0, B=1, C=2, D=3
    return (chunkX & 1) + ((chunkY & 1) << 1);
}
```

### Simulation Job

```csharp
[BurstCompile]
public struct SimulateChunksJob : IJobParallelFor
{
    // World data (read/write)
    [NativeDisableParallelForRestriction]
    public NativeArray<Cell> cells;
    
    [NativeDisableParallelForRestriction]
    public NativeArray<ChunkState> chunks;
    
    // Material definitions (read-only)
    [ReadOnly]
    public NativeArray<MaterialDef> materials;
    
    // Which chunks to process this pass
    [ReadOnly]
    public NativeArray<int> chunkIndices;
    
    // World dimensions
    public int width;
    public int height;
    public int chunksX;
    
    // Frame counter
    public ushort currentFrame;
    
    public void Execute(int jobIndex)
    {
        int chunkIndex = chunkIndices[jobIndex];
        SimulateChunk(chunkIndex);
    }
    
    private void SimulateChunk(int chunkIndex)
    {
        int chunkX = chunkIndex % chunksX;
        int chunkY = chunkIndex / chunksX;
        
        // Extended bounds (core + 16-cell buffer)
        int minX = math.max(0, chunkX * 32 - 16);
        int maxX = math.min(width, chunkX * 32 + 32 + 16);
        int minY = math.max(0, chunkY * 32 - 16);
        int maxY = math.min(height, chunkY * 32 + 32 + 16);
        
        // Process bottom-to-top (critical for falling)
        for (int y = maxY - 1; y >= minY; y--)
        {
            // Alternate left-right each row to reduce directional bias
            bool leftToRight = (y & 1) == 0;
            
            int startX = leftToRight ? minX : maxX - 1;
            int endX = leftToRight ? maxX : minX - 1;
            int stepX = leftToRight ? 1 : -1;
            
            for (int x = startX; x != endX; x += stepX)
            {
                SimulateCell(x, y);
            }
        }
    }
    
    private void SimulateCell(int x, int y)
    {
        int index = y * width + x;
        Cell cell = cells[index];
        
        // Skip if already processed
        if (cell.frameUpdated == currentFrame)
            return;
        
        // Skip static materials
        MaterialDef mat = materials[cell.materialId];
        if (mat.behaviour == BehaviourType.Static)
            return;
        
        // Mark as processed
        cell.frameUpdated = currentFrame;
        
        // Simulate based on behaviour type
        switch (mat.behaviour)
        {
            case BehaviourType.Powder:
                SimulatePowder(x, y, ref cell, mat);
                break;
            case BehaviourType.Liquid:
                SimulateLiquid(x, y, ref cell, mat);
                break;
            case BehaviourType.Gas:
                SimulateGas(x, y, ref cell, mat);
                break;
        }
        
        // Write back
        cells[index] = cell;
    }
}
```

### Powder Simulation (Sand, Ore)

```csharp
private void SimulatePowder(int x, int y, ref Cell cell, MaterialDef mat)
{
    // Apply gravity
    cell.velocityY = (sbyte)math.min(cell.velocityY + 1, 16);
    
    // Try to move down by velocity
    int targetY = y + cell.velocityY;
    
    // Trace path for collision
    for (int checkY = y + 1; checkY <= targetY; checkY++)
    {
        if (!CanMoveTo(x, checkY, mat.density))
        {
            targetY = checkY - 1;
            cell.velocityY = 0;
            break;
        }
    }
    
    if (targetY != y)
    {
        MoveCell(x, y, x, targetY, ref cell);
        return;
    }
    
    // Can't fall straight - try diagonals
    // Randomise direction to avoid bias
    bool tryLeftFirst = ((x + y + currentFrame) & 1) == 0;
    
    int dx1 = tryLeftFirst ? -1 : 1;
    int dx2 = tryLeftFirst ? 1 : -1;
    
    if (CanMoveTo(x + dx1, y + 1, mat.density))
    {
        MoveCell(x, y, x + dx1, y + 1, ref cell);
        return;
    }
    
    if (CanMoveTo(x + dx2, y + 1, mat.density))
    {
        MoveCell(x, y, x + dx2, y + 1, ref cell);
        return;
    }
    
    // Stuck
    cell.velocityX = 0;
    cell.velocityY = 0;
}
```

### Liquid Simulation (Water, Oil)

```csharp
private void SimulateLiquid(int x, int y, ref Cell cell, MaterialDef mat)
{
    // Try falling first (same as powder)
    cell.velocityY = (sbyte)math.min(cell.velocityY + 1, 16);
    
    if (TryFall(x, y, ref cell, mat.density))
        return;
    
    if (TryDiagonalFall(x, y, ref cell, mat.density))
        return;
    
    // Spread horizontally
    int spread = math.max(1, (16 - math.abs(cell.velocityY)) / (mat.friction + 1));
    
    bool tryLeftFirst = ((x + y + currentFrame) & 1) == 0;
    int dx1 = tryLeftFirst ? -1 : 1;
    int dx2 = tryLeftFirst ? 1 : -1;
    
    // Try first direction
    for (int dist = 1; dist <= spread; dist++)
    {
        int targetX = x + dx1 * dist;
        if (CanMoveTo(targetX, y, mat.density))
        {
            MoveCell(x, y, targetX, y, ref cell);
            return;
        }
        if (!IsEmpty(targetX, y))
            break;  // Hit wall
    }
    
    // Try second direction
    for (int dist = 1; dist <= spread; dist++)
    {
        int targetX = x + dx2 * dist;
        if (CanMoveTo(targetX, y, mat.density))
        {
            MoveCell(x, y, targetX, y, ref cell);
            return;
        }
        if (!IsEmpty(targetX, y))
            break;
    }
    
    // Stuck
    cell.velocityY = 0;
}

private bool TryFall(int x, int y, ref Cell cell, byte density)
{
    int targetY = y + cell.velocityY;
    
    for (int checkY = y + 1; checkY <= targetY; checkY++)
    {
        if (!CanMoveTo(x, checkY, density))
        {
            targetY = checkY - 1;
            cell.velocityY = 0;
            break;
        }
    }
    
    if (targetY != y)
    {
        MoveCell(x, y, x, targetY, ref cell);
        return true;
    }
    return false;
}

private bool TryDiagonalFall(int x, int y, ref Cell cell, byte density)
{
    bool tryLeftFirst = ((x + y + currentFrame) & 1) == 0;
    int dx1 = tryLeftFirst ? -1 : 1;
    int dx2 = tryLeftFirst ? 1 : -1;
    
    if (CanMoveTo(x + dx1, y + 1, density))
    {
        MoveCell(x, y, x + dx1, y + 1, ref cell);
        return true;
    }
    
    if (CanMoveTo(x + dx2, y + 1, density))
    {
        MoveCell(x, y, x + dx2, y + 1, ref cell);
        return true;
    }
    
    return false;
}
```

### Gas Simulation (Steam, Smoke)

```csharp
private void SimulateGas(int x, int y, ref Cell cell, MaterialDef mat)
{
    // Gases rise - invert gravity
    cell.velocityY = (sbyte)math.max(cell.velocityY - 1, -16);
    
    int targetY = y + cell.velocityY;  // velocityY is negative, so this goes up
    
    // Trace path upward
    for (int checkY = y - 1; checkY >= targetY; checkY--)
    {
        if (!CanMoveTo(x, checkY, mat.density))
        {
            targetY = checkY + 1;
            break;
        }
    }
    
    if (targetY != py)
    {
        MoveCell(x, y, x, targetY, ref cell);
        return;
    }
    
    // Try diagonal upward
    bool tryLeftFirst = ((x + y + currentFrame) & 1) == 0;
    int dx1 = tryLeftFirst ? -1 : 1;
    int dx2 = tryLeftFirst ? 1 : -1;
    
    if (CanMoveTo(x + dx1, y - 1, mat.density))
    {
        MoveCell(x, y, x + dx1, y - 1, ref cell);
        return;
    }
    
    if (CanMoveTo(x + dx2, y - 1, mat.density))
    {
        MoveCell(x, y, x + dx2, y - 1, ref cell);
        return;
    }
    
    // Spread horizontally (gases disperse)
    int spread = 4;
    for (int dist = 1; dist <= spread; dist++)
    {
        int targetX = x + dx1 * dist;
        if (CanMoveTo(targetX, y, mat.density))
        {
            MoveCell(x, y, targetX, y, ref cell);
            return;
        }
    }
    
    cell.velocityY = 0;
}
```

### Movement Helpers

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private bool IsInBounds(int x, int y)
{
    return x >= 0 && x < width && y >= 0 && y < height;
}

[MethodImpl(MethodImplOptions.AggressiveInlining)]
private bool IsEmpty(int x, int y)
{
    if (!IsInBounds(x, y)) return false;
    return cells[y * width + x].materialId == Materials.Air;
}

[MethodImpl(MethodImplOptions.AggressiveInlining)]
private bool CanMoveTo(int x, int y, byte myDensity)
{
    if (!IsInBounds(x, y)) return false;
    
    Cell target = cells[y * width + x];
    
    // Can move into air
    if (target.materialId == Materials.Air)
        return true;
    
    // Can displace lighter materials (not solids)
    MaterialDef targetMat = materials[target.materialId];
    if (targetMat.behaviour == BehaviourType.Static)
        return false;
    
    return myDensity > targetMat.density;
}

private void MoveCell(int fromX, int fromY, int toX, int toY, ref Cell cell)
{
    int fromIndex = fromY * width + fromX;
    int toIndex = toY * width + toX;
    
    // Get target cell
    Cell targetCell = cells[toIndex];
    
    // Swap
    cells[fromIndex] = targetCell;
    cells[toIndex] = cell;
    
    // Mark chunks dirty
    MarkDirtyInternal(fromX, fromY);
    MarkDirtyInternal(toX, toY);
}

private void MarkDirtyInternal(int x, int y)
{
    int chunkX = x >> 5;
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

### Scheduling the 4-Pass Simulation

```csharp
public class CellSimulator : MonoBehaviour
{
    private CellWorld world;
    
    // Chunk lists per group (reused each frame)
    private NativeList<int> groupA, groupB, groupC, groupD;
    
    void Update()
    {
        // Increment frame counter
        world.currentFrame++;
        
        // Collect active chunks into groups
        CollectActiveChunks();
        
        // Schedule 4 passes with dependencies
        JobHandle handle = default;
        
        if (groupA.Length > 0)
            handle = ScheduleGroup(groupA, handle);
        if (groupB.Length > 0)
            handle = ScheduleGroup(groupB, handle);
        if (groupC.Length > 0)
            handle = ScheduleGroup(groupC, handle);
        if (groupD.Length > 0)
            handle = ScheduleGroup(groupD, handle);
        
        // Complete all jobs
        handle.Complete();
        
        // Record dirty state for next frame
        UpdateDirtyState();
        
        // Upload to GPU and render
        UploadDirtyRegions();
    }
    
    private void CollectActiveChunks()
    {
        groupA.Clear();
        groupB.Clear();
        groupC.Clear();
        groupD.Clear();
        
        for (int i = 0; i < world.chunks.Length; i++)
        {
            ChunkState chunk = world.chunks[i];
            
            // Process if dirty, has structure, or was dirty last frame
            bool shouldProcess = 
                (chunk.flags & ChunkFlags.IsDirty) != 0 ||
                (chunk.flags & ChunkFlags.HasStructure) != 0 ||
                chunk.activeLastFrame != 0;
            
            if (!shouldProcess)
                continue;
            
            int chunkX = i % world.chunksX;
            int chunkY = i / world.chunksX;
            int group = (chunkX & 1) + ((chunkY & 1) << 1);
            
            switch (group)
            {
                case 0: groupA.Add(i); break;
                case 1: groupB.Add(i); break;
                case 2: groupC.Add(i); break;
                case 3: groupD.Add(i); break;
            }
        }
    }
    
    private JobHandle ScheduleGroup(NativeList<int> chunkIndices, JobHandle dependency)
    {
        var job = new SimulateChunksJob
        {
            cells = world.cells,
            chunks = world.chunks,
            materials = world.materials,
            chunkIndices = chunkIndices.AsArray(),
            width = world.width,
            height = world.height,
            chunksX = world.chunksX,
            currentFrame = world.currentFrame,
        };
        
        return job.Schedule(chunkIndices.Length, 1, dependency);
    }
    
    private void UpdateDirtyState()
    {
        for (int i = 0; i < world.chunks.Length; i++)
        {
            ChunkState chunk = world.chunks[i];
            
            // Record if was dirty (for neighbour waking next frame)
            chunk.activeLastFrame = (chunk.flags & ChunkFlags.IsDirty) != 0 ? (byte)1 : (byte)0;
            
            // Reset dirty bounds (unless has structure)
            if ((chunk.flags & ChunkFlags.HasStructure) == 0)
            {
                chunk.flags &= unchecked((byte)~ChunkFlags.IsDirty);
                chunk.minX = 32;
                chunk.maxX = 0;
                chunk.minY = 32;
                chunk.maxY = 0;
            }
            
            world.chunks[i] = chunk;
        }
    }
}
```

---

## Structure System

Structures are entities that physically interact with the cell world.

### Structure Base

```csharp
public struct Structure
{
    public int id;
    public StructureType type;
    public int x, y;              // Position in cells
    public int width, height;     // Size in cells
    public byte rotation;         // 0, 1, 2, 3 (× 90°)
    public byte flags;
    public ushort typeData;       // Type-specific packed data
}

public enum StructureType : byte
{
    Belt = 0,
    Lift = 1,
    Furnace = 2,
    Press = 3,
    Grinder = 4,
}
```

### Structure Registration

When placed, structures mark their chunks as always-dirty:

```csharp
public void PlaceStructure(Structure structure)
{
    structures.Add(structure);
    
    // Mark overlapping chunks
    int minChunkX = structure.x >> 5;
    int maxChunkX = (structure.x + structure.width - 1) >> 5;
    int minChunkY = structure.y >> 5;
    int maxChunkY = (structure.y + structure.height - 1) >> 5;
    
    for (int cy = minChunkY; cy <= maxChunkY; cy++)
    {
        for (int cx = minChunkX; cx <= maxChunkX; cx++)
        {
            int chunkIndex = cy * world.chunksX + cx;
            ChunkState chunk = world.chunks[chunkIndex];
            chunk.flags |= ChunkFlags.HasStructure;
            world.chunks[chunkIndex] = chunk;
        }
    }
}
```

### Belt Behaviour

Belts move cells horizontally at a fixed rate:

```csharp
[BurstCompile]
public struct SimulateBeltsJob : IJobParallelFor
{
    [NativeDisableParallelForRestriction]
    public NativeArray<Cell> cells;
    
    [ReadOnly]
    public NativeArray<Structure> belts;
    
    public int width;
    public ushort currentFrame;
    
    public void Execute(int index)
    {
        Structure belt = belts[index];
        
        // Belt surface is top row of structure
        int surfaceY = belt.y;
        
        // Direction from typeData (1 = right, -1 = left)
        int direction = (belt.typeData & 1) == 0 ? 1 : -1;
        int speed = (belt.typeData >> 1) & 7;  // 1-7 cells per frame
        
        // Process in direction of movement to avoid gaps
        int startX = direction > 0 ? belt.x : belt.x + belt.width - 1;
        int endX = direction > 0 ? belt.x + belt.width : belt.x - 1;
        
        for (int x = startX; x != endX; x += direction)
        {
            int cellIndex = surfaceY * width + x;
            Cell cell = cells[cellIndex];
            
            // Skip air and static
            if (cell.materialId == Materials.Air)
                continue;
            
            int targetX = x + direction * speed;
            int targetIndex = surfaceY * width + targetX;
            
            // Check destination is empty
            if (cells[targetIndex].materialId != Materials.Air)
                continue;  // Blocked - belt backs up naturally
            
            // Swap (move cell, leave air behind)
            cells[cellIndex] = cells[targetIndex];
            cells[targetIndex] = cell;
        }
    }
}
```

### Lift Behaviour

Lifts move cells vertically:

```csharp
[BurstCompile]
public struct SimulateLiftsJob : IJobParallelFor
{
    [NativeDisableParallelForRestriction]
    public NativeArray<Cell> cells;
    
    [ReadOnly]
    public NativeArray<Structure> lifts;
    
    public int width;
    public int height;
    
    public void Execute(int index)
    {
        Structure lift = lifts[index];
        
        // Lift column
        int shaftX = lift.x;
        
        // Direction from typeData (1 = up, -1 = down)
        int direction = (lift.typeData & 1) == 0 ? -1 : 1;  // -1 = up (decreasing Y)
        int speed = (lift.typeData >> 1) & 7;
        
        // Process in direction of movement
        int startY, endY, stepY;
        if (direction < 0)  // Moving up
        {
            startY = lift.y;
            endY = lift.y + lift.height;
            stepY = 1;
        }
        else  // Moving down
        {
            startY = lift.y + lift.height - 1;
            endY = lift.y - 1;
            stepY = -1;
        }
        
        for (int y = startY; y != endY; y += stepY)
        {
            int cellIndex = y * width + shaftX;
            Cell cell = cells[cellIndex];
            
            if (cell.materialId == Materials.Air)
                continue;
            
            int targetY = y + direction * speed;
            if (targetY < 0 || targetY >= height)
                continue;
            
            int targetIndex = targetY * width + shaftX;
            
            if (cells[targetIndex].materialId != Materials.Air)
                continue;
            
            cells[cellIndex] = cells[targetIndex];
            cells[targetIndex] = cell;
        }
    }
}
```

### Furnace Behaviour

Furnaces apply heat to their interior:

```csharp
[BurstCompile]
public struct SimulateFurnacesJob : IJobParallelFor
{
    [NativeDisableParallelForRestriction]
    public NativeArray<Cell> cells;
    
    [ReadOnly]
    public NativeArray<Structure> furnaces;
    
    [ReadOnly]
    public NativeArray<MaterialDef> materials;
    
    public int width;
    
    public void Execute(int index)
    {
        Structure furnace = furnaces[index];
        
        int heatOutput = (furnace.typeData >> 4) & 15;  // 0-15 heat per frame
        
        // Heat interior (exclude walls)
        for (int y = furnace.y + 1; y < furnace.y + furnace.height - 1; y++)
        {
            for (int x = furnace.x + 1; x < furnace.x + furnace.width - 1; x++)
            {
                int cellIndex = y * width + x;
                Cell cell = cells[cellIndex];
                
                if (cell.materialId == Materials.Air)
                    continue;
                
                // Increase temperature
                cell.temperature = (byte)math.min(255, cell.temperature + heatOutput);
                
                // Check for phase changes
                MaterialDef mat = materials[cell.materialId];
                
                if (cell.temperature >= mat.meltTemp && mat.materialOnMelt != 0)
                {
                    cell.materialId = mat.materialOnMelt;
                    cell.velocityX = 0;
                    cell.velocityY = 0;
                }
                else if (cell.temperature >= mat.boilTemp && mat.materialOnBoil != 0)
                {
                    cell.materialId = mat.materialOnBoil;
                    cell.velocityY = -1;  // Start rising
                }
                else if (cell.temperature >= mat.ignitionTemp && mat.materialOnBurn != 0)
                {
                    cell.materialId = mat.materialOnBurn;
                }
                
                cells[cellIndex] = cell;
            }
        }
    }
}
```

### Structure Update Order

Structures update *before* physics simulation:

```csharp
void Update()
{
    // 1. Update structures (apply forces, move cells)
    var beltJob = new SimulateBeltsJob { ... };
    var liftJob = new SimulateLiftsJob { ... };
    var furnaceJob = new SimulateFurnacesJob { ... };
    
    JobHandle structureHandle = beltJob.Schedule(belts.Length, 4);
    structureHandle = liftJob.Schedule(lifts.Length, 4, structureHandle);
    structureHandle = furnaceJob.Schedule(furnaces.Length, 4, structureHandle);
    structureHandle.Complete();
    
    // 2. Physics simulation (4-pass)
    SimulatePhysics();
    
    // 3. Render
    UploadAndRender();
}
```

---

## GPU Rendering

Cells are rendered as 2×2 pixel blocks. The GPU scales the cell texture up.

### Setup

```csharp
public class CellRenderer : MonoBehaviour
{
    private Texture2D cellTexture;       // Cell material IDs
    private Texture2D paletteTexture;    // Material ID → colour
    private Material renderMaterial;
    
    // Buffer for dirty region uploads
    private NativeArray<byte> uploadBuffer;
    
    void Start()
    {
        // Create cell texture (R8 format - just material IDs)
        // Size is world in CELLS, not pixels
        cellTexture = new Texture2D(
            world.width,
            world.height,
            TextureFormat.R8,
            mipChain: false,
            linear: true
        );
        cellTexture.filterMode = FilterMode.Point;  // Crisp cells
        cellTexture.wrapMode = TextureWrapMode.Clamp;
        
        // Create palette texture (256 colours)
        paletteTexture = new Texture2D(256, 1, TextureFormat.RGBA32, false);
        paletteTexture.filterMode = FilterMode.Point;
        BuildPalette();
        
        // Assign to material
        renderMaterial.SetTexture("_CellTex", cellTexture);
        renderMaterial.SetTexture("_PaletteTex", paletteTexture);
        
        // Upload buffer (size of one chunk)
        uploadBuffer = new NativeArray<byte>(32 * 32, Allocator.Persistent);
    }
    
    void BuildPalette()
    {
        Color32[] colours = new Color32[256];
        
        for (int i = 0; i < world.materials.Length; i++)
        {
            colours[i] = world.materials[i].baseColour;
        }
        
        paletteTexture.SetPixels32(colours);
        paletteTexture.Apply();
    }
}
```

### Dirty Region Upload

```csharp
void UploadDirtyRegions()
{
    for (int i = 0; i < world.chunks.Length; i++)
    {
        ChunkState chunk = world.chunks[i];
        
        if (chunk.activeLastFrame == 0)
            continue;
        
        int chunkX = i % world.chunksX;
        int chunkY = i / world.chunksX;
        
        // Chunk bounds in cells
        int cx = chunkX * 32;
        int cy = chunkY * 32;
        
        // Build material ID buffer for this chunk
        int bufferIndex = 0;
        for (int ly = 0; ly < 32; ly++)
        {
            for (int lx = 0; lx < 32; lx++)
            {
                int worldX = cx + lx;
                int worldY = cy + ly;
                
                if (worldX < world.width && worldY < world.height)
                {
                    int cellIndex = worldY * world.width + worldX;
                    uploadBuffer[bufferIndex] = world.cells[cellIndex].materialId;
                }
                else
                {
                    uploadBuffer[bufferIndex] = Materials.Air;
                }
                bufferIndex++;
            }
        }
        
        // Upload chunk region to texture
        var colors = new Color32[32 * 32];
        for (int j = 0; j < uploadBuffer.Length; j++)
        {
            byte val = uploadBuffer[j];
            colors[j] = new Color32(val, 0, 0, 255);
        }
        cellTexture.SetPixels32(cx, cy, 32, 32, colors);
    }
    
    cellTexture.Apply(updateMipmaps: false);
}
```

### Shader

The shader samples the cell texture. The quad mesh is sized to render cells as 2×2 pixel blocks:

```hlsl
Shader "CellWorld/WorldRender"
{
    Properties
    {
        _CellTex ("Cell Texture", 2D) = "white" {}
        _PaletteTex ("Palette Texture", 2D) = "white" {}
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            sampler2D _CellTex;
            sampler2D _PaletteTex;
            float4 _CellTex_TexelSize;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Sample material ID (stored in red channel, 0-1)
                float materialId = tex2D(_CellTex, i.uv).r;
                
                // Look up colour from palette
                fixed4 colour = tex2D(_PaletteTex, float2(materialId, 0.5));
                
                // Add subtle variation based on cell position
                float2 cellPos = floor(i.uv * _CellTex_TexelSize.zw);
                float noise = frac(sin(dot(cellPos, float2(12.9898, 78.233))) * 43758.5453);
                colour.rgb *= 0.95 + noise * 0.1;
                
                return colour;
            }
            ENDCG
        }
    }
}
```

To render cells as 2×2 pixels, scale your render quad to double the cell texture size, or adjust your camera orthographic size accordingly.

### Optional: Heat Glow

```hlsl
sampler2D _TemperatureTex;

fixed4 frag (v2f i) : SV_Target
{
    float materialId = tex2D(_CellTex, i.uv).r;
    fixed4 colour = tex2D(_PaletteTex, float2(materialId, 0.5));
    
    float temp = tex2D(_TemperatureTex, i.uv).r;
    
    fixed3 hotColour = lerp(fixed3(1, 0.3, 0), fixed3(1, 1, 0.8), temp);
    colour.rgb = lerp(colour.rgb, hotColour, temp * temp);
    
    return colour;
}
```

---

## Heat Simulation

### Heat Transfer Job

Uses double-buffering to avoid order-dependent results:

```csharp
[BurstCompile]
public struct HeatTransferJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<Cell> cellsRead;
    
    public NativeArray<Cell> cellsWrite;
    
    [ReadOnly]
    public NativeArray<MaterialDef> materials;
    
    public int width;
    public int height;
    
    public void Execute(int index)
    {
        int x = index % width;
        int y = index / width;
        
        Cell cell = cellsRead[index];
        MaterialDef mat = materials[cell.materialId];
        
        if ((mat.flags & MaterialFlags.ConductsHeat) == 0)
        {
            cellsWrite[index] = cell;
            return;
        }
        
        // Average with neighbours
        int totalTemp = cell.temperature;
        int count = 1;
        
        if (x > 0) { totalTemp += GetNeighbourTemp(index - 1); count++; }
        if (x < width - 1) { totalTemp += GetNeighbourTemp(index + 1); count++; }
        if (y > 0) { totalTemp += GetNeighbourTemp(index - width); count++; }
        if (y < height - 1) { totalTemp += GetNeighbourTemp(index + width); count++; }
        
        int newTemp = (totalTemp / count) - 1;  // Slight cooling
        cell.temperature = (byte)math.max(0, newTemp);
        
        cellsWrite[index] = cell;
    }
    
    private int GetNeighbourTemp(int index)
    {
        Cell neighbour = cellsRead[index];
        MaterialDef mat = materials[neighbour.materialId];
        
        if ((mat.flags & MaterialFlags.ConductsHeat) == 0)
            return 0;
        
        return neighbour.temperature;
    }
}
```

Swap buffers after job completes:

```csharp
void UpdateHeat()
{
    var heatJob = new HeatTransferJob
    {
        cellsRead = world.cells,
        cellsWrite = world.cellsTemp,
        materials = world.materials,
        width = world.width,
        height = world.height,
    };
    
    heatJob.Schedule(world.cells.Length, 64).Complete();
    
    // Swap
    (world.cells, world.cellsTemp) = (world.cellsTemp, world.cells);
}
```

---

## Performance Summary

### Memory Usage

For a 4,000 × 2,000 cell world (8,000 × 4,000 rendered pixels):

| Data | Size |
|------|------|
| Cells (8 bytes each) | 64 MB |
| Chunks (~12 bytes each) | ~94 KB |
| Materials (24 bytes, 256 max) | 6 KB |
| GPU texture (1 byte per cell) | 8 MB |
| **Total** | **~72 MB** |

### Expected Performance

- Static world: near-zero CPU cost
- Active factory: scales with active chunks
- Target: 60 FPS with 100+ active chunks (~100,000 cells)

---

## Implementation Roadmap

### Phase 1: Data Structures (Week 1)
- Cell, ChunkState structs
- CellWorld with NativeArrays
- Material definitions

### Phase 2: Single-Threaded Sim (Week 2)
- Powder, liquid, gas behaviours
- Movement and swapping

### Phase 3: Dirty Tracking (Week 3)
- Chunk grid
- MarkDirty, skip clean chunks

### Phase 4: Multithreading (Week 4)
- IJobParallelFor with Burst
- 4-pass scheduling

### Phase 5: GPU Rendering (Week 5)
- Cell texture, palette
- Shader with 2× display

### Phase 6: Structures (Week 6-7)
- Belt, lift, furnace jobs

### Phase 7: Polish (Week 8+)
- Heat transfer
- More materials
- Save/load

---

## Appendix: Common Pitfalls

### Job System
- **"Native array disposed"** → Call Complete() before Dispose()
- **Job runs on main thread** → Check Burst Inspector for errors

### Burst
- **"Managed objects"** → Use structs and NativeArrays only
- **"Boxing"** → Don't cast value types to object

### Threading
- **Cells duplicating** → Check chunk groups, velocity ≤ 16

---

## Resources

- **Noita GDC 2019**: YouTube "Noita GDC"
- **Unity Job System**: docs.unity3d.com/Manual/JobSystem.html
- **Burst**: docs.unity3d.com/Packages/com.unity.burst@latest
