# Bug: PlayerController Mixes Movement and Inventory Responsibilities

## Status: OPEN

## Category: Architecture / Refactor

## Summary

`PlayerController` is both a physics controller (movement, jumping, ground check, coyote time) and an inventory/equipment manager (tool equipping, structure selection, events). These are two distinct concerns in one MonoBehaviour.

## Current Responsibilities

### Movement (should stay in PlayerController)
- Horizontal movement with acceleration
- Jump with coyote time
- Ground detection via raycasts
- Rigidbody2D force application

### Inventory/Equipment (should be extracted)
- `ToolType` tracking and `EquipTool()` / `EquipStructure()`
- `OnToolEquipped`, `OnToolCollected`, `OnStructureEquipped` events (lines 40-43)
- Tool/structure state management
- Hotbar integration

### Direct Singleton Access
- `FixedUpdate()` (line 85) reaches through `SimulationManager.Instance.LiftManager` to apply lift force to the player
- This tight coupling means PlayerController can't be tested without a full SimulationManager
- Lift force for the player should be handled by a game-layer physics adapter or passed via dependency injection

## Refactor Direction

Split into:
- `PlayerMovement` — physics, ground detection, jumping
- `PlayerInventory` — tool/structure management, equipment events
- Accept dependencies via `Initialize()` instead of accessing singletons

## Affected Files

- `Assets/Scripts/Game/PlayerController.cs`

## Priority

Low — functional but increasingly coupled. Related to `OPEN-PlayerControllerOverhaul.md` which covers interaction bugs.

## See Also

- `OPEN-PlayerControllerOverhaul.md` — covers player-structure interaction bugs and proposes a state machine
