# Item and Hotbar Refactor

## Goal

Make the item registry and hotbar data-driven so that adding a new item doesn't require per-item methods and identity checks in Hotbar.

## Current State

`ItemRegistry` is a static class with handwritten `public static readonly` fields. This works but means every new item is a code change in a specific file rather than data registration.

`Hotbar` is the bigger problem:
- `GetIconSprite()` uses identity checks (`if (item == ItemRegistry.Grabber)`) to map items to sprites
- `CreateIconSprites()` calls individual `DrawGrabberIcon()`, `DrawShovelIcon()`, etc. — one method per item
- `GetRequiredAbility()` has a manual switch mapping StructureType to Ability
- `Initialize()` hardcodes 5 slots with specific item assignments
- Adding a new item means adding a sprite field, a draw method, a case in GetIconSprite, and a case in GetRequiredAbility

`InventoryMenu` and `InventoryItemSlot` are already generic — they iterate `ItemRegistry.All` and handle drag-and-drop without item-specific code.

## Work Required

### Icons on ItemDefinition
- Add an icon sprite (or icon generator callback) to `ItemDefinition` itself so each item carries its own visual
- Hotbar reads the icon from the item definition instead of maintaining a parallel mapping
- Eliminates GetIconSprite switch and per-item Draw methods from Hotbar

### Ability Mapping on ItemDefinition
- Add an optional `requiredAbility` field to `ItemDefinition`
- Hotbar checks `item.RequiredAbility` instead of switching on StructureType
- Eliminates GetRequiredAbility switch from Hotbar

### Default Slot Configuration
- Move default slot assignments to data (e.g., `ItemDefinition` has a `defaultSlot` field, or level data specifies starting hotbar layout)
- Hotbar initializes from this data instead of hardcoding `slots[0] = ItemRegistry.Grabber`

## Scope

This is a relatively small refactor — the core Hotbar rendering and input logic is fine. The changes are about moving per-item knowledge out of Hotbar and into ItemDefinition where it belongs.
