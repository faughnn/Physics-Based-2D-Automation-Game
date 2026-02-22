# Bug: Piston Placement Destroys Dirt Instead of Using Ghost Mode

## Summary
Placing a piston over dirt (or any soft terrain) silently destroys the existing material. The entire 16x16 area is cleared to Air, violating material conservation. Other structures (belts, lifts, walls) correctly use ghost mode to leave terrain in place until it naturally clears.

## Symptoms
- Placing a piston over dirt causes the dirt to vanish instantly
- Up to 256 cells of material are permanently lost per placement
- Affects all soft terrain types: Dirt, Sand, Water, Ground

## Root Cause
`PistonManager.PlacePiston()` (lines 110-121) unconditionally clears the 16x16 area to Air after the validation check accepts soft terrain. Unlike Belt/Lift/Wall managers, the PistonManager has no ghost mode system — it immediately overwrites cells instead of deferring activation until terrain naturally clears.

```csharp
// Lines 110-121 — THE BUG
for (int dy = 0; dy < BlockSize; dy++)
{
    for (int dx = 0; dx < BlockSize; dx++)
    {
        int cx = gridX + dx;
        int cy = gridY + dy;
        world.SetCell(cx, cy, Materials.Air);   // destroys existing material
        world.MarkDirty(cx, cy);
        terrainColliders.MarkChunkDirtyAt(cx, cy);
    }
}
```

The PistonManager is missing all ghost infrastructure that the other structure managers have:
- No `isGhost` field on piston data
- No `ghostBlockOrigins` tracking set
- No `UpdateGhostStates()` implementation
- Does not implement `IStructureManager`

## Affected Code
- `Assets/Scripts/Simulation/Machines/PistonManager.cs:110-121` — cells cleared to Air unconditionally
- `Assets/Scripts/Simulation/Machines/PistonManager.cs:96-108` — validation accepts soft terrain but doesn't set ghost flag

## Potential Solutions
### 1. Add Ghost Mode to PistonManager (Recommended)
Follow the same pattern used by BeltManager, LiftManager, and WallManager:
1. Add an `isGhost` field to `PistonData`
2. Add a `ghostBlockOrigins` HashSet for tracking
3. When soft terrain is detected during validation, set `anyGhost = true` and do NOT clear cells
4. Skip writing PistonBase cells and creating the cluster/colliders until ghost clears
5. Implement `UpdateGhostStates()` to check when all terrain has naturally cleared from the 16x16 area
6. On ghost activation, proceed with current placement logic
7. Consider implementing `IStructureManager` so ghost updates are called automatically

### 2. Displace Material Instead of Destroying It
Instead of clearing to Air, push displaced material to nearby empty cells. Simpler but less consistent with the existing ghost pattern used by other structures.

## Priority
High — violates the core material conservation principle and affects normal gameplay.

## Related Files
- `Assets/Scripts/Simulation/Machines/PistonManager.cs`
- `Assets/Scripts/Structures/BeltManager.cs` — reference ghost mode implementation
- `Assets/Scripts/Structures/LiftManager.cs` — reference ghost mode implementation
- `Assets/Scripts/Structures/WallManager.cs` — reference ghost mode implementation
- `Assets/Scripts/Structures/IStructureManager.cs` — interface PistonManager should implement
- `Assets/Scripts/Game/Structures/StructurePlacementController.cs` — game-level placement code
