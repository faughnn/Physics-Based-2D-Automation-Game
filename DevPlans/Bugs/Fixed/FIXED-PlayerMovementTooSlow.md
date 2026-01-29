# Bug: Player Movement Speed Too Slow

**Status:** FIXED
**Reported:** 2026-01-28
**Fixed:** 2026-01-29

## Description

The player character moves too slowly. General movement feels sluggish and doesn't match the pace expected for the game.

## Expected Behavior

Player movement should feel snappy and responsive, with enough speed to traverse the world comfortably.

## Actual Behavior

Player movement speed is noticeably too slow.

## Severity

Medium — affects overall game feel and player experience.

## Resolution

Increased `moveSpeed` in `PlayerController.cs` from `80f` to `250f` world units/sec. The original value was too low for responsive platformer movement.
