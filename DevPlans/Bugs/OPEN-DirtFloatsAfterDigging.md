# Bug: Dug Dirt Gets Stuck Floating in the Air

**Status:** OPEN
**Reported:** 2026-01-28

## Description

When digging dirt, cells are ejected upward as expected, but some of the ejected dirt particles get stuck mid-air and never fall back down. They remain floating indefinitely.

## Steps to Reproduce

1. Dig into dirt terrain
2. Observe dirt cells flying upward from the dig site
3. Some cells stop moving and remain suspended in the air

## Expected Behavior

All ejected dirt cells should eventually fall back down due to gravity and settle on the ground or other terrain.

## Actual Behavior

Some dirt cells freeze in place mid-air after being ejected, floating permanently.

## Possible Causes

- Ejected cells land in a chunk that goes inactive (dirty flag clears) before they finish settling, so they never get simulated again
- Related to the dirty rectangle tracking bug (OPEN-DirtyRectsNotUsedForSimOrRender) — a cell may land in a region of a chunk that isn't being re-evaluated
- Cells ejected across a chunk boundary may not properly mark the destination chunk as dirty
- Velocity may be zeroed out prematurely while the cell is still airborne

## Severity

Medium — visual artifact that breaks immersion. Core digging mechanic works but leaves floating debris.
