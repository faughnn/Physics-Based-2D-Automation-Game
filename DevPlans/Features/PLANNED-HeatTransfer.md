# Feature: Heat Transfer System

## Summary

Ambient heat conduction between cells, enabling temperature to spread through materials and dissipate over time. This provides realistic thermal behavior for furnaces, cooling, and environmental temperature effects.

---

## Goals

- Transfer heat between adjacent cells based on conductivity
- Gradual cooling toward ambient temperature
- Support heat-insulating and heat-conducting materials
- Burst-compiled parallel simulation
- Avoid order-dependent artifacts (double buffering)

---

## Design

### Overview

Each frame, cells exchange heat with their neighbors based on material conductivity. Heat flows from hot to cold cells. All cells gradually cool toward ambient temperature. Double-buffering prevents order-dependent results.

### Key Components

**New Files:**
```
Assets/Scripts/Simulation/
├── HeatTransferJob.cs       # Burst-compiled heat diffusion
└── HeatSettings.cs          # Ambient temp, cooling rate constants
```

**Modified Files:**
- `Assets/Scripts/Simulation/CellWorld.cs` - Add second cell buffer for double-buffering
- `Assets/Scripts/Simulation/CellSimulatorJobbed.cs` - Add heat transfer pass
- `Assets/Scripts/Simulation/MaterialDef.cs` - Verify ConductsHeat flag usage

### Data Structures

```csharp
// In CellWorld.cs - add second buffer
public NativeArray<Cell> cells;
public NativeArray<Cell> cellsTemp;  // For double-buffering

// Heat settings
public static class HeatSettings
{
    public const byte AmbientTemperature = 20;
    public const int CoolingRate = 1;  // Temp decrease per frame toward ambient
    public const float ConductionRate = 0.25f;  // Heat transfer coefficient
}
```

### Behavior

**Double-Buffered Heat Transfer:**
```csharp
[BurstCompile]
public struct HeatTransferJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<Cell> cellsRead;

    [WriteOnly]
    public NativeArray<Cell> cellsWrite;

    [ReadOnly]
    public NativeArray<MaterialDef> materials;

    public int width;
    public int height;
    public byte ambientTemp;

    public void Execute(int index)
    {
        int x = index % width;
        int y = index / width;

        Cell cell = cellsRead[index];
        MaterialDef mat = materials[cell.materialId];

        // Non-conducting materials don't transfer heat
        if ((mat.flags & MaterialFlags.ConductsHeat) == 0)
        {
            cellsWrite[index] = cell;
            return;
        }

        // Gather neighbor temperatures
        int totalTemp = cell.temperature;
        int conductingNeighbors = 1;

        // Check 4 cardinal neighbors
        if (x > 0)
            AddNeighborTemp(index - 1, ref totalTemp, ref conductingNeighbors);
        if (x < width - 1)
            AddNeighborTemp(index + 1, ref totalTemp, ref conductingNeighbors);
        if (y > 0)
            AddNeighborTemp(index - width, ref totalTemp, ref conductingNeighbors);
        if (y < height - 1)
            AddNeighborTemp(index + width, ref totalTemp, ref conductingNeighbors);

        // Average temperature with neighbors
        int avgTemp = totalTemp / conductingNeighbors;

        // Blend toward average (partial conduction)
        int newTemp = cell.temperature + (int)((avgTemp - cell.temperature) * 0.25f);

        // Cool toward ambient
        if (newTemp > ambientTemp)
            newTemp = math.max(ambientTemp, newTemp - 1);
        else if (newTemp < ambientTemp)
            newTemp = math.min(ambientTemp, newTemp + 1);

        cell.temperature = (byte)math.clamp(newTemp, 0, 255);
        cellsWrite[index] = cell;
    }

    private void AddNeighborTemp(int neighborIndex, ref int totalTemp, ref int count)
    {
        Cell neighbor = cellsRead[neighborIndex];
        MaterialDef neighborMat = materials[neighbor.materialId];

        if ((neighborMat.flags & MaterialFlags.ConductsHeat) != 0)
        {
            totalTemp += neighbor.temperature;
            count++;
        }
    }
}
```

**Buffer Swap After Job:**
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
        ambientTemp = HeatSettings.AmbientTemperature,
    };

    heatJob.Schedule(world.cells.Length, 256).Complete();

    // Swap buffers
    (world.cells, world.cellsTemp) = (world.cellsTemp, world.cells);
}
```

---

## Integration Points

### Pipeline Order

```
1. Structure Forces → Clusters
2. Cluster Physics
3. Belt/Lift Cell Movement
4. Furnace Heat Application
5. Heat Transfer (diffusion)  ← NEW
6. Phase Change Check         ← After heat settles
7. Cell Simulation (4-pass)
8. Render
```

### Material Conductivity

Materials need proper ConductsHeat flags:

| Material | ConductsHeat | Notes |
|----------|--------------|-------|
| Stone | Yes | Slow but conducts |
| Sand | Yes | Moderate |
| Water | Yes | Good conductor |
| Air | No | Insulator |
| Iron | Yes | Excellent conductor |
| Wood | No | Insulator (also flammable) |

### Dependencies

- **Furnace System**: Primary heat source
- **Material Reactions**: Phase changes triggered by temperature
- **CellWorld**: Double buffer support

---

## Performance Considerations

**Chunk-Based Optimization:**
Only process chunks with temperature variance:
```csharp
// In ChunkState
public byte maxTemperature;
public byte minTemperature;

// Skip chunk if uniform temperature and at ambient
if (chunk.maxTemperature == chunk.minTemperature &&
    chunk.maxTemperature == HeatSettings.AmbientTemperature)
    continue;
```

**Reduced Frequency:**
Heat transfer can run at lower frequency than cell simulation:
```csharp
if (currentFrame % 2 == 0)  // Every other frame
    UpdateHeat();
```

---

## Visual Effects

**Temperature-Based Rendering:**
```csharp
// In CellRenderer - upload temperature texture
temperatureTexture.SetPixels(...);

// In shader
float temp = tex2D(_TemperatureTex, i.uv).r;
float3 hotColor = lerp(float3(1, 0.3, 0), float3(1, 1, 0.8), temp);
color.rgb = lerp(color.rgb, hotColor, temp * temp);
```

---

## Open Questions

1. **Diagonal conduction**: Include 8 neighbors or just 4?
2. **Heat capacity**: Should dense materials heat/cool slower?
3. **Chunk optimization**: Worth tracking temperature ranges per chunk?
4. **Heat sources**: Environmental heat (lava, sun) beyond furnaces?
5. **Frequency**: Every frame or reduced rate for performance?

---

## Priority

Medium - Required for furnace cooling and thermal realism

---

## Related Files

- `G:\Sandy\falling_sand_engine_design_unity.md:1319-1398` - Original heat design
- `G:\Sandy\Assets\Scripts\Simulation\Cell.cs` - temperature field
- `G:\Sandy\Assets\Scripts\Simulation\MaterialDef.cs` - ConductsHeat flag
- `G:\Sandy\DevPlans\Features\PLANNED-FurnaceSystem.md` - Heat source
- `G:\Sandy\DevPlans\Features\PLANNED-MaterialReactions.md` - Phase changes
