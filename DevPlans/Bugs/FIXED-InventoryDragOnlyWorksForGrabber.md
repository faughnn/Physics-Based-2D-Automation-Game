# Bug: Only Grabber Can Be Dragged From Inventory Menu

## Summary
In the inventory menu, only the Grabber item can be dragged onto the hotbar. All other items (Shovel, Belt, Lift) cannot be dragged.

## Symptoms
- Grabber can be dragged from inventory to hotbar slots
- Shovel, Belt, Lift cannot be dragged at all
- No drag visual appears for non-Grabber items

## Root Cause
Two issues:

### 1. Missing raycast target on InventoryItemSlot GameObject
The `InventoryItemSlot` MonoBehaviour (which implements `IBeginDragHandler`, `IDragHandler`, `IEndDragHandler`) is attached to `slotObj`, but `slotObj` has no `Image` or `Graphic` component. Unity's EventSystem only delivers pointer/drag events to GameObjects that have a `Graphic` with `raycastTarget = true`. The background `Image` is on a child `bgObj`, so drag events go to `bgObj` (which has no drag handlers) instead of `slotObj`.

Grabber may appear to work because it's always available and the first item — its position may overlap with the overlay panel `Image` in a way that accidentally routes events.

### 2. `isAvailable` baked at creation time
`InventoryItemSlot.Setup()` stores `isAvailable` as a field set once at build time. Since Shovel starts uncollected and Belt/Lift start locked, `isAvailable` is `false` at creation and never updated — even after the player collects the shovel or unlocks structures, `OnBeginDrag` still returns early due to `!isAvailable`.

## Affected Code
- `Assets/Scripts/Game/UI/InventoryMenu.cs:231-277` — `CreateItemSlot()` does not add a raycast-target `Image` to the slot root GameObject
- `Assets/Scripts/Game/UI/InventoryItemSlot.cs:21-27` — `Setup()` bakes `isAvailable` once, never refreshed
- `Assets/Scripts/Game/UI/InventoryItemSlot.cs:35` — `OnBeginDrag` checks stale `isAvailable`

## Potential Solutions
### 1. Add transparent Image to slotObj + refresh availability
Add a transparent `Image` component to `slotObj` (the GameObject with `InventoryItemSlot`) so the EventSystem can deliver drag/pointer events to it. Also add a public method to `InventoryItemSlot` to update `isAvailable`, and call it from `InventoryMenu.RefreshItemAvailability()`.

### 2. Move drag handlers to bgObj
Alternatively, add the `InventoryItemSlot` component to `bgObj` instead of `slotObj`, since `bgObj` already has an `Image`. Less clean but fewer changes.

## Priority
High

## Related Files
- `Assets/Scripts/Game/UI/InventoryMenu.cs`
- `Assets/Scripts/Game/UI/InventoryItemSlot.cs`
- `Assets/Scripts/Game/UI/Hotbar.cs`
