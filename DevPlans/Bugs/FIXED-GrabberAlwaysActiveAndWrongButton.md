# Bug: Grabber Activates at All Times and Uses Wrong Mouse Button

## Summary
CellGrabSystem activates on right-click regardless of hotbar selection. It should only be active when Grabber is equipped in the hotbar, and should use left click to pick up / release left click to drop.

## Symptoms
- Grab system responds to right-click at all times, even when a structure (Belt/Lift) is selected
- Uses right mouse button instead of left mouse button for grab/drop

## Root Cause
Two issues in `CellGrabSystem.cs`:

1. **Wrong mouse button**: Lines 80 and 86 use `mouse.rightButton` for grab and drop. Should use `mouse.leftButton`.

2. **Tool check too broad**: The guard at lines 71-74 allows activation for both Grabber and Shovel. If the intent is grab-only for Grabber, the Shovel check should be removed. Additionally, the guard fails open when `player` is null due to `&&` short-circuiting — should return early when player is null.

Current guard:
```csharp
if (player != null &&
    player.EquippedTool != ToolType.Grabber &&
    player.EquippedTool != ToolType.Shovel)
    return;
```

## Affected Code
- `Assets/Scripts/Game/CellGrabSystem.cs:71-74` — tool check guard
- `Assets/Scripts/Game/CellGrabSystem.cs:80` — grab input (rightButton → leftButton)
- `Assets/Scripts/Game/CellGrabSystem.cs:86` — drop input (rightButton → leftButton)

## Potential Solutions
### 1. Fix mouse button and tighten guard
Change `mouse.rightButton` to `mouse.leftButton` on both press and release checks. Change the guard to:
```csharp
if (player == null || player.EquippedTool != ToolType.Grabber)
    return;
```
This ensures grab only works with Grabber equipped and fixes the null-player fail-open.

Note: Shovel + grab may be desired as a separate feature, but the Shovel already has DiggingController for its primary action. Grab should be Grabber-only.

## Priority
High

## Related Files
- `Assets/Scripts/Game/PlayerController.cs` — EquippedTool property
- `Assets/Scripts/Game/UI/Hotbar.cs` — equip flow
- `Assets/Scripts/Game/Digging/DiggingController.cs` — Shovel's own tool check
