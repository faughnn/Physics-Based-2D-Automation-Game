# Feature: Ground Material

## Summary
Add a new "Ground" material that represents diggable terrain. Ground is a static material (like stone) that provides collision, but when the player digs it, those cells convert to Air and spawn loose "Dirt" particles. Ground is the source terrain; Dirt is what you get when you excavate it.

## Goals
- Provide a static, collidable terrain material that players can dig through
- Visually distinct from Stone (darker brown color) to signal "this can be dug"
- Integrate with existing terrain collision system (TerrainColliderManager)
- Establish a "diggable" flag pattern for future expandability

## Design

### Overview
Ground will be a `BehaviourType.Static` material with a new `Diggable` flag. The simulation layer only defines the material and its properties - the actual digging mechanics (detecting player input, converting Ground to Air, spawning Dirt) will be handled by the Game layer.

### Key Components

**New constant in `Materials` class:**
```csharp
public const byte Ground = 18;  // After Dirt (17)
```

**New flag in `MaterialFlags`:**
```csharp
public const byte Diggable = 1 << 4;  // Can be excavated by player
```

**New material definition:**
```csharp
defs[Ground] = new MaterialDef
{
    density = 255,                              // Maximum density (static)
    behaviour = BehaviourType.Static,
    flags = MaterialFlags.ConductsHeat | MaterialFlags.Diggable,
    baseColour = new Color32(92, 64, 51, 255),  // Dark brown (darker than Dirt's 139, 90, 43)
    colourVariation = 8,
};
```

### Color Comparison
| Material | RGB | Description |
|----------|-----|-------------|
| Dirt (powder) | (139, 90, 43) | Medium brown, loose earth |
| Ground (static) | (92, 64, 51) | Dark brown, packed earth |
| Stone (static) | (100, 100, 105) | Gray, cannot dig |

### Diggable Flag
The `Diggable` flag is a property marker that the Game layer can query to determine which materials can be excavated. This follows the existing flags pattern (ConductsHeat, Flammable, etc.) and allows for future diggable materials without special-case code.

**Helper method in `Materials` class (alongside `IsBelt()`):**
```csharp
public static bool IsDiggable(MaterialDef mat)
{
    return (mat.flags & MaterialFlags.Diggable) != 0;
}
```

### Behavior
1. Ground is static - it never moves in simulation
2. Ground provides collision via TerrainColliderManager (same as Stone)
3. Ground has the Diggable flag set
4. When Game layer digs Ground:
   - Ground cell converts to Air
   - Dirt particle spawns (handled by Game layer, not Simulation)
   - TerrainColliderManager regenerates chunk collider

## Integration Points
- `MaterialDef.cs` - Add `Diggable` flag, ID constant, and definition
- `TerrainColliderManager.cs` - No changes needed (already handles all static materials)
- `SimulateChunksJob.cs` - No changes needed (skips static materials)
- `CellRenderer.cs` - Automatic (reads colors from MaterialDef)

## Implementation Steps
1. Add `Diggable` flag to `MaterialFlags` class
2. Add `Ground = 18` constant to `Materials` class
3. Add Ground definition in `Materials.CreateDefaults()`
4. (Optional) Add `IsDiggable()` helper method

## Open Questions
- Should there be visual variation in Ground (e.g., occasional pebbles)?
- Should Ground have different hardness levels in the future?
- Should digging Ground sometimes yield other materials (rocks, minerals)?

## Notes
- The digging mechanic itself is **not** part of this plan - that belongs in Game layer
- This material ID (18) follows Dirt (17) which is already implemented (see DONE-DirtMaterial.md)
- Ground does NOT need special handling in SimulateChunksJob - it's just a static material

## Priority
Medium - Required for terrain gameplay. Dirt dependency is already satisfied.

## Related Files
- `Assets/Scripts/Simulation/MaterialDef.cs` - Material definitions
- `Assets/Scripts/Simulation/Clusters/TerrainColliderManager.cs` - Static collision
- `DevPlans/Features/DONE-DirtMaterial.md` - Dirt material (already implemented)
