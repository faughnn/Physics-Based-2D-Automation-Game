# Bug: Holding Mouse Button Does Not Repeat Digging

**Status:** OPEN
**Reported:** 2026-01-28

## Description

Digging only triggers on initial mouse click. Holding the mouse button down does nothing — the player must repeatedly click to keep digging. Digging should auto-repeat every 100ms while the mouse button is held.

## Expected Behavior

Holding the mouse button should automatically repeat the dig action every 100ms (~10 times per second).

## Actual Behavior

Only a single dig occurs on mouse down. Holding the button produces no further digs. The player must release and click again for each dig.

## Severity

Medium — core mechanic requires excessive clicking. Hold-to-dig is standard for this type of game.
