# Bug: Hotbar Not Updated When Shovel Is Picked Up

## Summary
When the player picks up the shovel, it is automatically equipped internally (`PlayerController.equippedTool` becomes `Shovel`), but the hotbar UI still highlights the Grabber slot. The hotbar is never notified of the pickup or equip.

## Symptoms
- Pick up the shovel from the world
- The shovel is equipped and functional (digging works)
- The hotbar still visually highlights the Grabber (slot 0)
- Manually pressing the Shovel hotbar key correctly syncs everything

## Root Cause
`PlayerController.CollectTool()` sets `equippedTool = tool` and fires `OnToolEquipped`, but **nobody subscribes to that event** for the purpose of updating the hotbar UI.

The `Hotbar` class manages its own `selectedIndex`, which only changes via keyboard/scroll input in `Hotbar.Update()`. It has no connection to `PlayerController.OnToolCollected` or `OnToolEquipped`.

The wiring in `GameController.CreateGameUI()` passes the `PlayerController` reference to `Hotbar.Initialize()`, but no event subscription is set up there.

## Affected Code
- `Assets/Scripts/Game/PlayerController.cs:237-246` — `CollectTool()` equips tool internally, fires events
- `Assets/Scripts/Game/PlayerController.cs:40-41` — `OnToolCollected` / `OnToolEquipped` events (no subscribers for hotbar)
- `Assets/Scripts/Game/UI/Hotbar.cs:42-60` — `Initialize()` receives PlayerController but doesn't subscribe to events
- `Assets/Scripts/Game/UI/Hotbar.cs:98` — `SelectSlot()` is private, only called from keyboard/scroll input

## Potential Solutions
### 1. Subscribe Hotbar to OnToolEquipped
In `Hotbar.Initialize()`, subscribe to `playerController.OnToolEquipped`. When fired, find the slot index matching the equipped tool type and call `SelectSlot()` to update the visual highlight.

### 2. Expose a public SelectByToolType method
Add a `SelectByToolType(ToolType)` public method on Hotbar. Have `GameController` wire up the event: `playerController.OnToolEquipped += (tool) => hotbar.SelectByToolType(tool)`.

## Priority
Medium

## Related Files
- `Assets/Scripts/Game/PlayerController.cs`
- `Assets/Scripts/Game/UI/Hotbar.cs`
- `Assets/Scripts/Game/GameController.cs`
- `Assets/Scripts/Game/Items/WorldItem.cs`
