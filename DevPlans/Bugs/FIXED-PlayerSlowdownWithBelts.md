# Player Slowdown With Belts

## Summary
Player movement speed decreases significantly when conveyor belts are placed in the world.

## Steps to Reproduce
1. Start game, observe normal player movement speed
2. Place one or more conveyor belts
3. Observe player movement is now much slower

## Expected Behavior
Player movement speed should remain constant regardless of belts in the world.

## Actual Behavior
Player moves noticeably slower after belts are placed.

## Possible Causes
- Belt simulation may be affecting player physics/velocity
- Belt colliders may be creating drag on player
- Belt update loop may be impacting frame rate (causing deltaTime-based movement to slow)
- Force zones from belts may be applying to the player unintentionally

## Files to Investigate
- `Assets/Scripts/Game/PlayerController.cs` - Movement code
- `Assets/Scripts/Simulation/Belts/` - Belt implementation
- `Assets/Scripts/Simulation/ForceZones/` - Force zone system (if belts use this)

## Severity
Medium - Affects core gameplay feel

## Status
OPEN
