# Walls Should Be Structures

## Status: OPEN

## Summary

Walls are currently implemented as raw material stamps (`Materials.Wall`) rather than managed structures. They should be proper structures with a manager, tile tracking, and ghost support — consistent with how belts and lifts work.

## Current Behavior

- Walls exist only as a sandbox feature (`SandboxController`, W key)
- Placement writes `Materials.Wall` bytes directly into the cell grid (8x8 fill)
- No `WallManager`, no `WallStructure`, no tile tracking, no ghost state
- Walls are not available in the Game scene at all
- `StructureType` enum has no `Wall` entry

## Expected Behavior

- Walls should be a proper structure type with a `WallManager` (like `BeltManager` / `LiftManager`)
- Walls should support ghost placement through soft terrain
- Walls should be available in the Game scene via `StructurePlacementController`
- Walls should unlock alongside lifts (same progression tier: `Ability.PlaceLifts`)
- `StructureType` enum should include a `Wall` entry

## Affected Files

- `Assets/Scripts/Structures/StructureType.cs` — needs `Wall` entry
- `Assets/Scripts/Game/Structures/StructurePlacementController.cs` — needs `PlacementMode.Wall` and W key binding
- `Assets/Scripts/Game/Progression/Ability.cs` — may need a `PlaceWalls` ability, or walls can piggyback on `PlaceLifts`
- New: `WallManager` and `WallStructure` in `Assets/Scripts/Structures/`
