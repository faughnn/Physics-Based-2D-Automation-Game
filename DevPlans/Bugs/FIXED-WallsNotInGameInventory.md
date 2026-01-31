# Bug: Walls Not In Game Inventory

## Summary
Walls cannot be placed in game mode because they are never registered in the item/inventory system. The Wall structure type is fully implemented in the simulation layer and placement controller, but it was never added to `ItemRegistry` or the `Hotbar`.

## Symptoms
- Walls do not appear in the hotbar or inventory
- No way to select or place walls in game mode
- F8 (debug unlock all structures) doesn't help because the item simply doesn't exist in the UI
- Walls are supposed to unlock alongside lifts (Stage 2 completion)

## Root Cause
Three things are missing from the UI layer:

1. **No Wall item definition in `ItemRegistry`** — `ItemRegistry.cs` defines Grabber, Shovel, Belt, and Lift but has no Wall entry. The `All` list only contains those four items.

2. **No Wall slot in `Hotbar`** — `Hotbar.Initialize()` hardcodes slots 0-3 (Grabber, Shovel, Belt, Lift). Slot 4 exists but is left empty. There is also no `wallSprite` or `DrawWallIcon` method.

3. **No Wall case in `GetIconSprite`** — The hotbar's icon sprite lookup doesn't handle a Wall item.

The placement controller (`StructurePlacementController`) and simulation layer (`WallManager`) both fully support walls already. The unlock gating (`Hotbar.GetRequiredAbility`) already maps `StructureType.Wall` to `Ability.PlaceLifts`. Only the inventory/UI registration is missing.

## Affected Code
- `Assets/Scripts/Game/UI/ItemRegistry.cs` — Missing Wall `ItemDefinition` and `All` list entry
- `Assets/Scripts/Game/UI/Hotbar.cs` — Missing slot assignment, icon sprite, and draw method

## Potential Solutions
### 1. Register Wall in ItemRegistry and Hotbar
- Add `Wall` static field to `ItemRegistry` (Id = "wall", Category = Structure, StructureType = Wall)
- Add it to `ItemRegistry.All`
- In `Hotbar`: add `wallSprite` field, `DrawWallIcon()` method, `GetIconSprite` case, and assign `slots[4] = ItemRegistry.Wall`
- No changes needed to placement controller, progression, or simulation — those already work

## Priority
Medium

## Related Files
- `Assets/Scripts/Game/UI/ItemRegistry.cs`
- `Assets/Scripts/Game/UI/Hotbar.cs`
- `Assets/Scripts/Game/Structures/StructurePlacementController.cs`
- `Assets/Scripts/Simulation/Structures/WallManager.cs`
