# Bug: Procedural Icon/Sprite Generation Embedded in UI and Controller Code

## Status: OPEN

## Category: Architecture / Separation of Concerns

## Summary

~160 lines of pixel-by-pixel icon drawing are inlined in `Hotbar.cs`, and sprite generation methods (`CreateRectSprite()`, `CreateShovelSprite()`) are inlined in `GameController.cs`. These are rendering/art concerns that clutter UI and controller classes.

## Locations

### Hotbar.cs (lines 329-486)
`GetIconSprite()` contains procedural texture generation for every tool and structure type. Each icon is drawn pixel-by-pixel with hardcoded coordinates and colors. Adding a new item type requires adding another block of pixel-drawing code inside the Hotbar class.

### GameController.cs (lines 271-351)
`CreateRectSprite()` and `CreateShovelSprite()` generate item textures procedurally. These are rendering utilities that have nothing to do with game orchestration.

## Additional Issues

### Hotbar.RefreshSlotVisuals() called every frame
At line 105 in `Update()`, all slot visuals are rebuilt every frame even if nothing changed. Should be event-driven (refresh only when equipment or progression changes).

### Mixed UI approaches
`StructurePlacementController` uses legacy `OnGUI()` (lines 522-574) for mode indicators while the rest of the game uses Canvas-based UI. This inconsistency makes the UI harder to maintain and style.

## Refactor Direction

- Extract an `IconFactory` or `SpriteFactory` utility class for all procedural sprite/icon generation
- Or better: load icons from sprite assets instead of generating them in code
- Make `RefreshSlotVisuals()` event-driven (subscribe to equipment/progression change events)
- Replace `OnGUI` usage in `StructurePlacementController` with Canvas UI elements

## Affected Files

- `Assets/Scripts/Game/UI/Hotbar.cs`
- `Assets/Scripts/Game/GameController.cs`
- `Assets/Scripts/Game/Structures/StructurePlacementController.cs`

## Priority

Low — cosmetic/maintainability issue, no runtime bugs.
