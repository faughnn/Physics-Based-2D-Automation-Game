# Feature: Material Reactions System

## Summary

Temperature-triggered material transformations: burning, melting, freezing, and boiling. When cell temperature crosses material thresholds, the cell transforms into a different material with appropriate behavior changes.

---

## Goals

- Transform materials when temperature thresholds are crossed
- Support burning with fire spread and fuel consumption
- Handle solid↔liquid↔gas phase transitions
- Integrate with heat transfer system
- Burst-compiled within cell simulation

---

## Design

### Overview

Material reactions are checked during cell simulation. When a cell's temperature exceeds (or falls below) material-defined thresholds, the cell transforms. Burning is special: it spreads to neighbors and consumes fuel over time.

### Key Components

**Modified Files:**
```
Assets/Scripts/Simulation/
├── Jobs/SimulateChunksJob.cs  # Add phase change checks
├── MaterialDef.cs             # Verify reaction fields
└── Cell.cs                    # Burning flag usage
```

**No new files needed** - reactions integrate into existing simulation.

### Data Structures

MaterialDef already has the required fields:

```csharp
public struct MaterialDef
{
    // Phase change temperatures
    public byte ignitionTemp;    // Temperature to catch fire (0 = won't burn)
    public byte meltTemp;        // Temperature to melt (0 = won't melt)
    public byte freezeTemp;      // Temperature to solidify (255 = won't freeze)
    public byte boilTemp;        // Temperature to evaporate (0 = won't boil)

    // Resulting materials
    public byte materialOnMelt;   // Material ID when melted
    public byte materialOnFreeze; // Material ID when frozen
    public byte materialOnBurn;   // Material ID when burned (ash, smoke)
    public byte materialOnBoil;   // Material ID when boiled (steam)

    // Flags
    public byte flags;            // MaterialFlags.Flammable, etc.
}

public static class CellFlags
{
    public const byte Burning = 1 << 2;  // Currently on fire
}
```

### Behavior

**Phase Change Check (in SimulateChunksJob):**
```csharp
private void CheckPhaseChanges(int x, int y, ref Cell cell, MaterialDef mat)
{
    // Melting: solid → liquid
    if (mat.meltTemp > 0 && cell.temperature >= mat.meltTemp && mat.materialOnMelt != 0)
    {
        cell.materialId = mat.materialOnMelt;
        cell.velocityX = 0;
        cell.velocityY = 0;
        cell.flags &= unchecked((byte)~CellFlags.Burning);  // Stop burning
        MarkDirty(x, y);
        return;
    }

    // Freezing: liquid → solid
    if (mat.freezeTemp < 255 && cell.temperature <= mat.freezeTemp && mat.materialOnFreeze != 0)
    {
        cell.materialId = mat.materialOnFreeze;
        cell.velocityX = 0;
        cell.velocityY = 0;
        MarkDirty(x, y);
        return;
    }

    // Boiling: liquid → gas
    if (mat.boilTemp > 0 && cell.temperature >= mat.boilTemp && mat.materialOnBoil != 0)
    {
        cell.materialId = mat.materialOnBoil;
        cell.velocityY = -1;  // Start rising
        MarkDirty(x, y);
        return;
    }

    // Ignition: start burning
    if (mat.ignitionTemp > 0 && cell.temperature >= mat.ignitionTemp)
    {
        if ((mat.flags & MaterialFlags.Flammable) != 0)
        {
            cell.flags |= CellFlags.Burning;
        }
    }
}
```

**Burning Simulation:**
```csharp
private void SimulateBurning(int x, int y, ref Cell cell, MaterialDef mat)
{
    if ((cell.flags & CellFlags.Burning) == 0)
        return;

    // Burning cells emit heat
    cell.temperature = (byte)math.min(255, cell.temperature + 5);

    // Spread fire to flammable neighbors (random chance)
    if (ShouldSpreadFire())
    {
        TryIgniteNeighbor(x - 1, y);
        TryIgniteNeighbor(x + 1, y);
        TryIgniteNeighbor(x, y - 1);
        TryIgniteNeighbor(x, y + 1);
    }

    // Consume fuel (random chance to turn to ash/smoke)
    if (ShouldConsumeFuel())
    {
        // Chance to become ash (falls) or smoke (rises)
        if (RandomBool())
        {
            cell.materialId = mat.materialOnBurn;  // Usually ash
            cell.flags &= unchecked((byte)~CellFlags.Burning);
        }
        else
        {
            cell.materialId = Materials.Smoke;
            cell.velocityY = -1;
            cell.flags &= unchecked((byte)~CellFlags.Burning);
        }
        MarkDirty(x, y);
    }
}

private void TryIgniteNeighbor(int x, int y)
{
    if (!IsInBounds(x, y))
        return;

    int index = y * width + x;
    Cell neighbor = cells[index];
    MaterialDef neighborMat = materials[neighbor.materialId];

    // Check if flammable and not already burning
    if ((neighborMat.flags & MaterialFlags.Flammable) != 0 &&
        (neighbor.flags & CellFlags.Burning) == 0)
    {
        // Heat up neighbor (may ignite on next frame)
        neighbor.temperature = (byte)math.min(255, neighbor.temperature + 10);
        cells[index] = neighbor;
        MarkDirty(x, y);
    }
}

private bool ShouldSpreadFire()
{
    // ~10% chance per frame
    return RandomValue() < 26;  // 26/256 ≈ 10%
}

private bool ShouldConsumeFuel()
{
    // ~2% chance per frame (fire lasts ~50 frames average)
    return RandomValue() < 5;  // 5/256 ≈ 2%
}
```

---

## Material Configuration Examples

```csharp
// Wood: burns to ash
materials[Materials.Wood] = new MaterialDef
{
    behaviour = BehaviourType.Static,
    flags = MaterialFlags.Flammable,
    ignitionTemp = 150,
    materialOnBurn = Materials.Ash,
    baseColour = new Color32(139, 90, 43, 255),
};

// Coal: burns hot, to ash
materials[Materials.Coal] = new MaterialDef
{
    behaviour = BehaviourType.Powder,
    flags = MaterialFlags.Flammable | MaterialFlags.ConductsHeat,
    ignitionTemp = 180,
    materialOnBurn = Materials.Ash,
    baseColour = new Color32(30, 30, 30, 255),
};

// Water: freezes and boils
materials[Materials.Water] = new MaterialDef
{
    behaviour = BehaviourType.Liquid,
    flags = MaterialFlags.ConductsHeat,
    freezeTemp = 10,
    materialOnFreeze = Materials.Ice,
    boilTemp = 100,
    materialOnBoil = Materials.Steam,
    baseColour = new Color32(32, 64, 192, 255),
};

// Ice: melts back to water
materials[Materials.Ice] = new MaterialDef
{
    behaviour = BehaviourType.Static,
    flags = MaterialFlags.ConductsHeat,
    meltTemp = 15,
    materialOnMelt = Materials.Water,
    baseColour = new Color32(200, 220, 255, 255),
};

// Iron Ore: melts to molten iron
materials[Materials.IronOre] = new MaterialDef
{
    behaviour = BehaviourType.Powder,
    flags = MaterialFlags.ConductsHeat,
    meltTemp = 200,
    materialOnMelt = Materials.MoltenIron,
    baseColour = new Color32(120, 80, 60, 255),
};

// Molten Iron: freezes to solid iron
materials[Materials.MoltenIron] = new MaterialDef
{
    behaviour = BehaviourType.Liquid,
    density = 200,
    flags = MaterialFlags.ConductsHeat,
    freezeTemp = 150,
    materialOnFreeze = Materials.Iron,
    baseColour = new Color32(255, 120, 50, 255),
};
```

---

## Integration Points

### Pipeline Order

```
1. Structure Forces → Clusters
2. Cluster Physics
3. Belt/Lift Cell Movement
4. Furnace Heat Application
5. Heat Transfer (diffusion)
6. Cell Simulation (4-pass)  ← Phase changes here
   └─ Per cell: Move → CheckPhaseChanges → SimulateBurning
7. Render
```

### Dependencies

- **Heat Transfer System**: Provides temperature changes
- **Furnace System**: Primary heat source for melting
- **MaterialDef**: Phase change configuration

---

## Visual Effects

**Burning Cells:**
- Shader adds flickering orange/yellow overlay
- Emission map shows bright spots
- Optional: particle effects for sparks

**Molten Materials:**
- High emission (glow)
- Temperature-based color shift

```hlsl
// In shader
if (cell.flags & BURNING_FLAG)
{
    float flicker = frac(sin(_Time.y * 20 + cellPos.x * 7) * 43758.5453);
    color.rgb = lerp(color.rgb, float3(1, 0.5, 0), 0.5 + flicker * 0.5);
}
```

---

## Open Questions

1. **Fire spread rate**: How fast should fire spread? Configurable?
2. **Extinguishing**: Water contact stops burning? Temperature threshold?
3. **Smoke behavior**: Should smoke eventually dissipate (become air)?
4. **Chain reactions**: Explosions? Acid dissolving materials?
5. **Reaction products**: Multiple outputs (e.g., burning produces ash AND smoke)?

---

## Priority

High - Required for furnace-based gameplay

---

## Related Files

- `G:\Sandy\falling_sand_engine_design_unity.md:143-235` - Material definitions
- `G:\Sandy\Assets\Scripts\Simulation\MaterialDef.cs` - Phase change fields
- `G:\Sandy\Assets\Scripts\Simulation\Jobs/SimulateChunksJob.cs` - Integration point
- `G:\Sandy\DevPlans\Features\PLANNED-HeatTransfer.md` - Temperature system
- `G:\Sandy\DevPlans\Features\PLANNED-FurnaceSystem.md` - Heat source
