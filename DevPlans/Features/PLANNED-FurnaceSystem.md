# Feature: Furnace System

## Summary

Furnaces are structures that apply heat to their interior, enabling smelting, melting, and other thermal processing of materials. They form the core of material transformation in the automation chain.

---

## Goals

- Apply heat to cells within furnace interior
- Trigger phase changes (melting, boiling, burning)
- Support configurable heat output levels
- Enable fuel-based or powered operation (future)
- Burst-compiled parallel simulation

---

## Design

### Overview

Furnaces are rectangular structures with solid walls and a hollow interior. Each frame, the furnace increases the temperature of all cells inside. When cell temperature exceeds material thresholds, phase changes occur.

### Key Components

**New Files:**
```
Assets/Scripts/Structures/
├── FurnaceStructure.cs      # Furnace data and bounds
├── FurnaceManager.cs        # Manages all furnaces
└── FurnaceSimulationJob.cs  # Burst-compiled heat application
```

**Modified Files:**
- `Assets/Scripts/Simulation/CellSimulatorJobbed.cs` - Add furnace simulation
- `Assets/Scripts/Simulation/MaterialDef.cs` - Ensure phase change fields are used

### Data Structures

```csharp
public struct FurnaceStructure
{
    public ushort id;
    public int x, y;           // Bottom-left corner
    public int width, height;  // Total size including walls
    public byte heatOutput;    // Temperature increase per frame (0-15)
    public byte maxTemp;       // Maximum interior temperature (caps heating)
    public FurnaceState state; // Off, Heating, Cooling
}

public enum FurnaceState : byte
{
    Off = 0,
    Heating = 1,
    Cooling = 2
}

public class FurnaceManager : IDisposable
{
    private NativeList<FurnaceStructure> furnaces;
    private Dictionary<ushort, FurnaceStructure> furnaceLookup;
}
```

### Behavior

**Heat Application:**
```csharp
[BurstCompile]
public struct FurnaceSimulationJob : IJobParallelFor
{
    [NativeDisableParallelForRestriction]
    public NativeArray<Cell> cells;

    [NativeDisableParallelForRestriction]
    public NativeArray<ChunkState> chunks;

    [ReadOnly]
    public NativeArray<MaterialDef> materials;

    [ReadOnly]
    public NativeArray<FurnaceStructure> furnaces;

    public int width;
    public int chunksX;

    public void Execute(int furnaceIndex)
    {
        FurnaceStructure furnace = furnaces[furnaceIndex];

        if (furnace.state != FurnaceState.Heating)
            return;

        // Interior bounds (exclude walls)
        int interiorMinX = furnace.x + 1;
        int interiorMaxX = furnace.x + furnace.width - 2;
        int interiorMinY = furnace.y + 1;
        int interiorMaxY = furnace.y + furnace.height - 2;

        for (int y = interiorMinY; y <= interiorMaxY; y++)
        {
            for (int x = interiorMinX; x <= interiorMaxX; x++)
            {
                int index = y * width + x;
                Cell cell = cells[index];

                if (cell.materialId == Materials.Air)
                    continue;

                // Apply heat (capped at maxTemp)
                int newTemp = math.min(
                    cell.temperature + furnace.heatOutput,
                    furnace.maxTemp
                );
                cell.temperature = (byte)newTemp;

                // Check phase changes
                MaterialDef mat = materials[cell.materialId];

                if (cell.temperature >= mat.meltTemp && mat.materialOnMelt != 0)
                {
                    cell.materialId = mat.materialOnMelt;
                    cell.velocityX = 0;
                    cell.velocityY = 0;
                    MarkDirty(x, y);
                }
                else if (cell.temperature >= mat.boilTemp && mat.materialOnBoil != 0)
                {
                    cell.materialId = mat.materialOnBoil;
                    cell.velocityY = -1;  // Start rising (gas)
                    MarkDirty(x, y);
                }
                else if (cell.temperature >= mat.ignitionTemp && mat.materialOnBurn != 0)
                {
                    cell.flags |= CellFlags.Burning;
                    // Burning handled by material reactions system
                    MarkDirty(x, y);
                }

                cells[index] = cell;
            }
        }
    }
}
```

---

## Integration Points

### Pipeline Order

```
1. Structure Forces → Clusters
2. Cluster Physics
3. Belt/Lift Cell Movement
4. Furnace Heat Application  ← NEW
5. Heat Transfer (ambient)   ← Related feature
6. Cell Simulation (4-pass)
7. Render
```

### Material Definitions Required

Furnaces depend on properly configured MaterialDef values:

```csharp
// Example: Iron Ore → Molten Iron
materials[Materials.IronOre] = new MaterialDef
{
    behaviour = BehaviourType.Powder,
    meltTemp = 200,
    materialOnMelt = Materials.MoltenIron,
    // ...
};

materials[Materials.MoltenIron] = new MaterialDef
{
    behaviour = BehaviourType.Liquid,
    density = 200,  // Heavy liquid
    freezeTemp = 150,
    materialOnFreeze = Materials.Iron,
    // ...
};
```

### Dependencies

- **Heat Transfer System**: For cooling when furnace turns off
- **Material Reactions System**: For burning behavior
- **CellWorld**: Cell array access
- **MaterialDef**: Phase change temperatures

---

## Visual Representation

- **Furnace walls**: Static solid material (stone/brick)
- **Interior glow**: Shader effect based on average interior temperature
- **Emission map**: Hot cells emit light

```csharp
// In renderer, sample furnace interiors for glow
float avgTemp = CalculateAverageInteriorTemp(furnace);
float glowIntensity = avgTemp / 255f;
```

---

## Open Questions

1. **Fuel system**: Do furnaces consume fuel or run indefinitely?
2. **Heat capacity**: Should larger furnaces heat slower?
3. **Exhaust**: Should burning produce smoke that needs venting?
4. **Furnace sizes**: Fixed sizes or player-configurable?
5. **Wall material**: Can furnace walls be any material or specific types?

---

## Priority

High - Core to material processing gameplay

---

## Related Files

- `G:\Sandy\falling_sand_engine_design_unity.md:1019-1082` - Original furnace design
- `G:\Sandy\Assets\Scripts\Simulation\MaterialDef.cs` - Phase change fields
- `G:\Sandy\DevPlans\Features\PLANNED-HeatTransfer.md` - Ambient heat system
- `G:\Sandy\DevPlans\Features\PLANNED-MaterialReactions.md` - Burning behavior
