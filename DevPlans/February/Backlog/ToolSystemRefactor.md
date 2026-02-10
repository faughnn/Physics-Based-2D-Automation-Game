# Tool System Refactor

## Goal

Introduce a common tool abstraction so that adding a new tool doesn't require touching 8-10 files and duplicating boilerplate.

## Current State

Each tool is a standalone MonoBehaviour (DiggingController, CellGrabSystem) with no shared interface. Adding a new tool requires:

1. Add to `ToolType` enum
2. Add an `ItemDefinition` in `ItemRegistry` (static field + add to `All` list)
3. Create a new MonoBehaviour for the tool behavior
4. Add `player.AddComponent<NewTool>()` in `GameController.CreatePlayer()`
5. Add switch case in `ToolRangeIndicator.LateUpdate()` for range
6. Add switch case in `ToolRangeIndicator` for AoE radius
7. Create a procedural icon method in `Hotbar` (e.g., `DrawPickaxeIcon`)
8. Wire the sprite field in `Hotbar.CreateIconSprites()` and `Hotbar.GetIconSprite()`
9. Update `Hotbar.Initialize()` default slot assignments
10. Possibly create a `WorldItem` spawn in `GameController`

`ToolRangeIndicator` is the most fragile piece — it holds direct references to DiggingController and CellGrabSystem and switches on ToolType for max range and AoE radius values. Every new tool adds another field and another branch.

## Work Required

### Common Tool Interface
- Define an `ITool` interface or `ToolBase` base class with shared properties: `MaxRange`, `AoERadius`, `IsActive`, `ToolType`
- Tools register themselves or are discoverable on the player GameObject
- ToolRangeIndicator queries the active tool's properties instead of switching on type and holding references to each concrete tool

### Icon System
- Move icon generation out of Hotbar — either tools provide their own icon sprite, or an icon registry maps item IDs to sprites
- Hotbar should not need a new method and a new field for each tool

### Registration
- Tools could self-register with the player controller on Awake, or the player could find all ITool components on itself
- GameController still adds the components, but the rest of the system discovers them generically

## Open Questions

- Should tools define their own input handling, or should a central tool manager handle input and delegate to the active tool?
- Structure placement is also a "tool" in a sense — should it share this abstraction or remain separate?
