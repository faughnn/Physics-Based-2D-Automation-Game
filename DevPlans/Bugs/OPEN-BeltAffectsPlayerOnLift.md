# Belt Affects Player Movement While On Lift

**Status:** OPEN

## Description

When the player is riding a lift upward and passes near a belt structure, the belt inappropriately affects the player's movement. The player should not be influenced by belts while on a lift.

## Steps to Reproduce

1. Place a belt near the path of a lift
2. Ride the lift upward
3. As the lift passes the belt, observe the player's movement being affected

## Expected Behavior

The player should move smoothly upward on the lift without being affected by nearby belts.

## Actual Behavior

The belt influences the player's movement as the lift carries them past it, causing unintended lateral movement or disruption.

## Likely Cause

The player's physics or movement system is picking up belt force zones regardless of whether the player is currently on a lift. The belt force application likely needs to be suppressed or ignored when the player is in a "riding lift" state.
