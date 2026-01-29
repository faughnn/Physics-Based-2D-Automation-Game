# Bug: Player Controller Overhaul Needed

## Status: OPEN

## Summary

The player controller has accumulated multiple interaction bugs with the simulation's structure systems (belts, lifts, terrain). Rather than patching each individually, the player controller needs a systematic overhaul to properly integrate with these systems.

## Consolidated Issues

### 1. Player Falls Through Belts in Dug Out Ground (NEW)

**Symptoms:** When belts are placed in terrain and the terrain is dug out, the player falls through the belt as if it doesn't exist.

**Root Cause:** Ghost belts do not trigger terrain collider regeneration when activated. In `BeltManager.UpdateGhostStates()` (lines 825-832), when a ghost belt activates:
- Belt material is written to cells
- `world.MarkDirty()` is called (rendering only)
- **Missing:** `terrainColliders.MarkChunkDirtyAt()` is never called

**Evidence:** Compare to `DiggingController.cs:118` which correctly calls both `world.SetCell()` AND `terrainColliders.MarkChunkDirtyAt()`.

**Affected Files:**
- `Assets/Scripts/Structures/BeltManager.cs` - `UpdateGhostStates()` missing collider update
- `Assets/Scripts/Structures/WallManager.cs` - Same issue in `UpdateGhostStates()`

### 2. Player Falls Through Undig Ground While Digging

**Symptoms:** Player clips through solid, unexcavated terrain during digging.

**Root Cause:** Race condition between cell removal and terrain collider regeneration. Physics step may occur between collider teardown and rebuild.

**Affected Files:**
- `Assets/Scripts/Game/Digging/DiggingController.cs`
- `Assets/Scripts/Simulation/Clusters/TerrainColliderManager.cs`

### 3. Player Lift Force Not Working Correctly

**Symptoms:**
- Lift boosts jump height but doesn't provide continuous upward force
- Player doesn't rise when standing still in a lift

**Root Cause:** Ground collision prevents upward movement. Unlike cells/clusters that can pass through lift material, the player is a solid Rigidbody2D that collides with terrain beneath them.

**Affected Files:**
- `Assets/Scripts/Game/PlayerController.cs`
- `Assets/Scripts/Structures/LiftManager.cs`

### 4. Belt Affects Player While On Lift

**Symptoms:** Player movement is disrupted by nearby belts while riding a lift.

**Root Cause:** No state machine to suppress belt forces when player is in "lift riding" state.

**Affected Files:**
- `Assets/Scripts/Game/PlayerController.cs`

## Proposed Overhaul

### Core Problems

1. **No Player-Structure Interaction System** - Player relies entirely on Unity physics rather than having explicit knowledge of simulation structures.

2. **No State Machine** - Player doesn't track states like "on belt", "in lift", "digging" that would allow proper force prioritization.

3. **Collider Updates Scattered** - Each system (digging, belts, walls, lifts) independently manages collider updates with no unified approach.

### Recommended Architecture

```
PlayerController
├── PlayerStateManager (on belt, in lift, grounded, airborne, digging)
├── PlayerStructureInteraction (queries BeltManager, LiftManager directly)
│   ├── GetBeltForceAtPosition()
│   ├── GetLiftForceAtPosition()
│   └── ShouldSuppressBeltForce() // true if in lift, etc.
└── PlayerGroundDetection (unified ground check)
    ├── TerrainCollider check
    ├── Structure check (belts, walls)
    └── Cluster check
```

### Immediate Fixes (Before Overhaul)

1. **Ghost belt collider fix:** Add `terrainColliders.MarkChunkDirtyAt()` call in `BeltManager.UpdateGhostStates()` and `WallManager.UpdateGhostStates()`

2. **Belt-on-lift suppression:** Add `isInLift` check before applying belt forces in PlayerController

## Priority

High - Multiple core gameplay interactions are broken.

## Related Files

- `Assets/Scripts/Game/PlayerController.cs`
- `Assets/Scripts/Game/Digging/DiggingController.cs`
- `Assets/Scripts/Structures/BeltManager.cs`
- `Assets/Scripts/Structures/LiftManager.cs`
- `Assets/Scripts/Structures/WallManager.cs`
- `Assets/Scripts/Simulation/Clusters/TerrainColliderManager.cs`

## See Also

These individual bug files are now consolidated here:
- `OPEN-PlayerFallsThroughUndigGround.md`
- `OPEN-PlayerLiftForceNotWorking.md`
- `OPEN-BeltAffectsPlayerOnLift.md`
