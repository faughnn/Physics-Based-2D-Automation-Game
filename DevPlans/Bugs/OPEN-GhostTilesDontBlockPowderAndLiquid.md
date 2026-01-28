# Bug: Ghost Tiles Do Not Block Powder and Liquid From Flowing In

**Status:** OPEN
**Reported:** 2026-01-28

## Description

When ghost tiles (structure placement preview) are placed, powder and liquid cells can still flow into the spaces occupied by the ghost tiles. Ghost tiles should act as solid barriers that prevent external material from entering their occupied cells.

## Steps to Reproduce

1. Enter structure placement mode and place ghost tiles
2. Have powder, sand, or liquid flowing nearby
3. Observe material flowing through/into the ghost tile spaces

## Expected Behavior

Ghost tiles should block powder and liquid from flowing into their occupied cells. They should behave as solid obstacles for incoming material, even though the structure hasn't been fully placed yet.

## Actual Behavior

Powder and liquid freely flow into cells occupied by ghost tiles as if nothing is there.

## Severity

Medium — ghost tiles need to reserve their space to give accurate placement feedback and prevent material from filling in areas that are about to become structure tiles.
