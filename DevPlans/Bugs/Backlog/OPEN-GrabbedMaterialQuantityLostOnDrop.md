# Bug: Grabbed Material Quantity Lost on Drop

## Summary
When picking up dirt (or other materials) with the grab system and dropping them, the amount dropped doesn't match the amount picked up. Material is silently destroyed when the drop area doesn't have enough free space.

## Symptoms
- Pick up a clump of dirt, drop it nearby — visibly less material comes out
- Worse near terrain, walls, or other obstacles where fewer empty cells are available
- No warning or feedback that material was lost

## Root Cause
The drop spiral in `CellGrabSystem.DropCellsAtPosition()` iterates outward from the cursor through a limited number of rings. If it can't find enough empty (air) cells to place all grabbed material — because the area is obstructed by terrain, structures, or other materials — it calls `ClearGrabbedCells()` unconditionally, silently destroying any unplaced cells.

The `maxRing` formula `ceil(sqrt(totalGrabbedCount)) + 10` provides enough candidate positions in open space, but in congested areas many positions are occupied, so the spiral exhausts its range before placing everything.

A secondary factor: the simulation continues running between grab (mouse press) and drop (mouse release), so material can flow into the drop zone and further reduce available placement positions.

## Affected Code
- `Assets/Scripts/Game/CellGrabSystem.cs:236` — `ClearGrabbedCells()` called unconditionally after drop loop, discarding unplaced material
- `Assets/Scripts/Game/CellGrabSystem.cs:214` — `maxRing` calculation may be insufficient in obstructed areas
- `Assets/Scripts/Game/CellGrabSystem.cs:208-237` — entire `DropCellsAtPosition()` method

## Potential Solutions
### 1. Retain unplaced material
Don't call `ClearGrabbedCells()` when cells remain unplaced. Keep `isHolding = true` so the player can attempt to drop again in a more open area. Only clear when all cells have been successfully placed.

### 2. Adaptive ring expansion
Instead of a fixed `maxRing`, keep expanding outward until all cells are placed or the entire reachable world area is exhausted. This ensures material is placed if any free space exists nearby, at the cost of potentially scattering material over a wide area.

### 3. Hybrid approach
Expand the ring further (e.g. double the current radius), and if cells still remain, keep them held so the player can retry. This avoids extreme scatter while preventing silent loss.

## Priority
Medium

## Related Files
- `Assets/Scripts/Game/CellGrabSystem.cs`
- `Assets/Scripts/Simulation/CellWorld.cs`
