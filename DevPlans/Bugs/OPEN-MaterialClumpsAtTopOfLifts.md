# Bug: Material Clumps at the Top of Lifts

**Status:** OPEN
**Reported:** 2026-01-28

## Description

Material being transported upward by lifts tends to clump together at the top of the lift rather than dispersing smoothly. Cells pile up and bunch at the exit point.

## Steps to Reproduce

1. Set up a lift transporting powder or liquid upward
2. Feed material into the bottom of the lift
3. Observe material clumping/bunching at the top exit

## Expected Behavior

Material should exit the top of the lift and disperse or flow away relatively smoothly.

## Actual Behavior

Material accumulates and clumps at the top of the lift, creating a bottleneck.

## Notes

This may be an inherent limitation of how cell-based lifts work — cells arrive at the top faster than they can spread out, and there's limited space for them to go. May not be fully solvable without a dedicated dispersion mechanism at lift exits.

## Severity

Low — cosmetic/behavioral issue. Lifts still function, but the clumping looks unpolished.
