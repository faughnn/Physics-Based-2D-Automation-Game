# Feature: Wedge Tile Placement

**Status:** PLANNED

## Summary

Add a new structure tile type: the wedge. A wedge is like a wall tile but only half-filled diagonally, creating a sloped surface. When material hits a wedge, it bounces/redirects in the direction of the slope. Primary use case is placing wedges at the top of lifts to redirect material that would otherwise clump.

## Design

### Wedge Tile

- A triangular solid tile occupying one half of a cell diagonally
- Four possible orientations: slope facing top-left, top-right, bottom-left, bottom-right
- Solid on one side, open on the other
- Material colliding with the slope surface is redirected along the slope direction

### Placement

- Placeable via the structure placement system like walls, belts, and lifts
- Player selects orientation before or during placement (e.g., rotate with R key)
- Ghost tile preview shows the wedge orientation

### Physics Interaction

- Powder/liquid hitting the solid half is deflected along the slope
- Acts as a wall on the solid side — blocks material from passing through
- Open side allows material to flow freely
- Gravity still applies — material slides along the slope rather than sticking

### Primary Use Case

Place wedges at the top of lifts to redirect material sideways instead of letting it clump at the exit point. For example, a top-right wedge at a lift exit would push material to the right as it comes up.

## Technical Feasibility Analysis

### Current Physics Limitations

The current simulation **cannot support true half-cell diagonal tiles** without new code. Key constraints:

1. **No partial cells.** Every cell position is fully solid or fully open. `CanMoveTo()` (`SimulateChunksJob.cs:666`) has no concept of direction-dependent blocking — it checks bounds, air, passable flag, and density. That's it.

2. **Collision response is downward-biased.** When a falling cell hits a solid, vertical momentum converts to diagonal sliding by probing `(x-1, y+1)` and `(x+1, y+1)` — always downward diagonals (`SimulateChunksJob.cs:240-241`). This works for spreading material on flat surfaces, but material coming **up** out of a lift that hits something above converts momentum into downward-diagonal movement — sending it back down, not sideways.

3. **No bounce or elastic reflection.** The system converts vertical velocity into diagonal sliding momentum. There is no angle-of-incidence reflection.

### No-Code Workaround: Staircase Pattern

A diagonal staircase of wall tiles placed next to a lift exit could approximate wedge behavior using existing physics:
- Material exits the lift, decelerates, starts falling
- The existing diagonal slide logic (`SimulatePowder` Phase 2-3) naturally slides falling material along a staircase slope
- No simulation changes needed — just a build pattern

**Limitation:** This requires multiple tiles to form a slope, not a single compact wedge.

### Implementation Approaches (Requires New Code)

**Approach A: Parallel data array (like lifts)**
- Add a `NativeArray<WedgeTile>` parallel to the cell grid, storing wedge orientation per cell
- Add a check in the movement/collision code: when a cell is blocked by a wedge, apply lateral velocity based on wedge orientation instead of the default downward-diagonal probe
- Wedge cells would be `BehaviourType.Static` with a new flag or material type
- Follows the lift precedent (`liftTiles` array, `LiftTile` struct)

**Approach B: Force-redirect on contact**
- Similar to how lifts modify `fractionalGravity` for cells inside them, wedges could apply a lateral force/velocity to cells that collide with them
- Would need new logic in `SimulatePowder` to detect wedge contact and set `velocityX` accordingly
- Simpler than full direction-dependent passability but less physically accurate

**Approach C: Multi-cell staircase as a single placeable structure**
- Define a wedge as a 2x2 or 3x3 arrangement of solid and air cells placed as one unit
- The existing diagonal slide physics handles the rest naturally
- No simulation code changes — just a structure placement template
- Trade-off: wedges are bigger than one tile and less flexible

## Open Questions

- Should wedges be craftable or available from the start?
- Can wedges be placed adjacent to each other to form longer slopes?
- Which implementation approach best balances effort vs quality? (Approach C is cheapest; Approach A is most robust)
- For the lift-redirect use case specifically, is a staircase build pattern sufficient or does it need to be a single tile?
