# Two-Dimensional Material Movement

## Goal

Fix material movement so that horizontal velocity is properly consumed during free-fall and rising, enabling true 2D trajectories. Materials should arc, spread, and flow in both dimensions simultaneously rather than moving in purely vertical columns.

## Current State

- Phase 1 (vertical movement) moves cells straight up or down, ignoring `velocityX` entirely
- Phase 2 (diagonal) only runs when Phase 1 fails completely — during free-fall it never gets a chance
- Lift exit lateral force accumulates `velocityX` via `velocityFracX` but this is never consumed during flight
- The result: material launched from lifts goes straight up, material dropped from height falls straight down, no arcing or lateral spread during movement
- This also makes lift-top clogging worse since material has no way to disperse horizontally while airborne

## Work Required

- Redesign the movement phases so horizontal velocity is consumed alongside vertical movement
- Enable combined trajectories (diagonal movement during free-fall and rising)
- Handle collision response symmetrically — upward collisions should produce horizontal dispersal just like downward ones
- Ensure the changes work within the chunk threading model (cells can only write within their chunk's core region)
- Tune movement to feel physically plausible across different material types

## Dependencies

- Closely tied to Per-Material Attributes — once 2D movement works, per-material friction/viscosity/spread will control how each type moves through space
- Fixes the root cause of several open bugs: lift fountain lateral force, material clumping at lift tops, asymmetric collision response
