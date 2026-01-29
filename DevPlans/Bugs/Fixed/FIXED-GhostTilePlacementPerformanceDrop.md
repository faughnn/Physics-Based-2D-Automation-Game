# Bug: Severe Performance Drop When Placing Ghost Tiles

**Status:** OPEN
**Reported:** 2026-01-28

## Description

Placing ghost tiles (structure placement preview) causes major frame rate drops. Performance tanks significantly while ghost tiles are being displayed or moved around.

## Steps to Reproduce

1. Enter structure placement mode
2. Move the ghost tile preview around the world
3. Observe severe frame rate degradation

## Expected Behavior

Ghost tile preview should be lightweight and not noticeably impact frame rate.

## Actual Behavior

Frame rate drops significantly while ghost tiles are active, making placement feel laggy and unresponsive.

## Possible Causes

- Ghost tiles may be triggering full chunk dirty marking every frame as they move
- Unnecessary cell simulation work caused by ghost tile updates
- Redundant terrain collider rebuilds triggered by ghost tile placement/removal
- Ghost tiles being written to and cleared from the cell world each frame rather than rendered as a separate overlay

## Severity

High — structure placement is a core mechanic and the performance hit makes it frustrating to use.
