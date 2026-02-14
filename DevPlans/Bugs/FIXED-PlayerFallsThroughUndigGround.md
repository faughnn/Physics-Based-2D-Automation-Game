# Bug: Player Falls Through Undig Ground While Digging

**Status:** OPEN
**Reported:** 2026-01-28

## Description

Sometimes when digging, the player falls through the ground of areas that haven't been dug out yet. The player clips through solid terrain that should still be intact.

## Steps to Reproduce

1. Start digging into terrain
2. Continue digging downward or into dense areas
3. Player occasionally falls through solid, unexcavated ground

## Expected Behavior

The player should only be able to pass through areas where cells have actually been removed by digging. Solid terrain should block the player at all times.

## Actual Behavior

The player clips through intact terrain and falls into areas that have not been dug out.

## Possible Causes

- Terrain collider not updating fast enough to account for player position during digging
- Digging operation temporarily removes collider data before re-generating it
- Player physics step occurring between collider teardown and rebuild
- Race condition between cell removal and terrain collider regeneration

## Severity

High - breaks core gameplay loop; player loses position and control unexpectedly.
