# Bug: Game Input Fires Through Inventory Menu UI

## Summary
When a placeable item (belt, lift, wall) is equipped and the inventory menu is open, clicking and dragging within the inventory UI also triggers tile placement in the game world behind it. All game input handlers read raw mouse state and never check whether the pointer is over UI.

## Symptoms
- Clicking/dragging items in the inventory menu places structures in the game world
- Scroll wheel changes hotbar slot even while scrolling within the inventory menu
- Digging and cell grabbing also fire through the UI
- Pressing Escape closes the inventory AND exits placement mode simultaneously

## Root Cause
Four independent input-consuming scripts read directly from `Mouse.current` without checking `EventSystem.current.IsPointerOverGameObject()` or `InventoryMenu.IsOpen`:

1. **StructurePlacementController** — reads `mouse.leftButton` / `mouse.rightButton` for placement
2. **CellGrabSystem** — reads `mouse.leftButton` for grab/release
3. **DiggingController** — reads `mouse.leftButton.isPressed` for digging
4. **Hotbar** — reads `Mouse.current.scroll` for slot cycling

`InventoryMenu.IsOpen` exists as a public property but nothing queries it. `EventSystem.current.IsPointerOverGameObject()` is never called anywhere in the codebase. There is no centralized input gate.

## Affected Code
- `Assets/Scripts/Game/Structures/StructurePlacementController.cs:169` — `HandlePlacementInput()` called unconditionally
- `Assets/Scripts/Game/CellGrabSystem.cs:78,84` — raw mouse reads with no UI guard
- `Assets/Scripts/Game/Digging/DiggingController.cs:47` — raw mouse read with no UI guard
- `Assets/Scripts/Game/UI/Hotbar.cs:76-78` — scroll input with no UI guard
- `Assets/Scripts/Game/UI/InventoryMenu.cs:25` — `IsOpen` property exists but is unused by input handlers

## Potential Solutions
### 1. Centralized UI input guard
Add a static helper (e.g. `GameInput.IsPointerOverUI()`) that wraps `EventSystem.current.IsPointerOverGameObject()` and/or checks `InventoryMenu.IsOpen`. All four input handlers early-return when it returns true. This is the standard Unity pattern and keeps the check in one place.

### 2. Per-handler guards
Each of the four scripts independently checks `InventoryMenu.IsOpen` or `EventSystem.current.IsPointerOverGameObject()` at the top of their input-reading methods. Simpler to implement but duplicates the check in four places.

## Priority
High

## Related Files
- `Assets/Scripts/Game/Structures/StructurePlacementController.cs`
- `Assets/Scripts/Game/CellGrabSystem.cs`
- `Assets/Scripts/Game/Digging/DiggingController.cs`
- `Assets/Scripts/Game/UI/Hotbar.cs`
- `Assets/Scripts/Game/UI/InventoryMenu.cs`
- `Assets/Scripts/Game/UI/GameUIBuilder.cs`
