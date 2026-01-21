# Feature: Dirt Material

## Summary
Add a new "Dirt" material that behaves as a powder but flows significantly less than sand and water. Dirt should pile up steeper and spread slower, simulating compacted earth behavior.

## Goals
- Provide a heavier, less mobile powder material for terrain building
- Differentiate material behaviors beyond just color and density
- Make use of the existing but unused `friction` field in MaterialDef
- Expand the palette of natural materials available

## Design

### Overview
Dirt will be a `BehaviourType.Powder` material with high friction. The simulation will be modified to use the `friction` field when deciding whether a powder particle slides diagonally, causing high-friction materials like dirt to pile steeper.

### Key Components

**New constant in `Materials` class:**
```csharp
public const byte Dirt = 21;  // Note: 17-20 reserved for Lift system
```

**New material definition:**
```csharp
defs[Dirt] = new MaterialDef  // ID 21
{
    density = 140,                              // Heavier than sand (128)
    friction = 200,                             // High friction (sand = 20)
    behaviour = BehaviourType.Powder,
    flags = MaterialFlags.None,
    baseColour = new Color32(139, 90, 43, 255), // Brown
    colourVariation = 12,
};
```

**Simulation change in `SimulatePowder()`:**
Add friction check before diagonal sliding:
```csharp
// Higher friction = less likely to slide
if (random.NextUInt(256) < mat.friction)
    return; // Don't slide, just stay put
```

### Data Structures
No new data structures needed. Uses existing `MaterialDef` struct which already has a `friction` field (currently unused for powders).

### Behavior
1. Dirt falls straight down like sand
2. When blocked from falling, dirt attempts diagonal slide
3. **New:** Random check against friction - dirt (friction=200) only slides ~22% of the time vs sand (friction=20) sliding ~92% of the time
4. Result: Dirt piles up steeper and spreads much slower than sand

**Comparison:**
| Material | Density | Friction | Slide Chance |
|----------|---------|----------|--------------|
| Sand     | 128     | 20       | ~92%         |
| Dirt     | 140     | 200      | ~22%         |

## Integration Points
- `MaterialDef.cs` - Add ID constant and definition
- `SimulateChunksJob.cs` - Modify `SimulatePowder()` to use friction
- `SandboxController.cs` - Optionally add to material selection UI
- `CellRenderer.cs` - Automatic (reads colors from MaterialDef)

## Open Questions
- Should dirt have any temperature-related behaviors (e.g., turn to mud when wet)?
- Should friction affect falling speed or only diagonal sliding?
- What keyboard shortcut for dirt selection (if any)?

## Priority
Low - Quality of life addition, not blocking other features.

## Related Files
- `Assets/Scripts/Simulation/MaterialDef.cs` - Material definitions
- `Assets/Scripts/Simulation/Jobs/SimulateChunksJob.cs` - Powder simulation logic
- `Assets/Scripts/SandboxController.cs` - Material selection UI
- `Assets/Scripts/Rendering/CellRenderer.cs` - Color palette generation
