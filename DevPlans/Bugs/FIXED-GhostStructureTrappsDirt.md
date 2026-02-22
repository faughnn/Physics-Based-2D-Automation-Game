# Bug: Ghost Belt/Wall Structures Trap Dirt Inside, Preventing Activation

## Summary
When a ghost belt or wall is placed through dirt terrain, the dirt occupying those cells becomes immobilized. Even after digging out the space below, the dirt cannot fall through or out of the ghost area. The ghost structure never activates because it requires all 64 cells to be Air. Only manually grabbing the dirt allows the ghost to activate. Lifts are NOT affected.

## Symptoms
- Place a belt through dirt terrain — it becomes a ghost belt
- Dig out the space below the ghost belt
- Dirt inside the ghost belt area does not fall into the empty space below
- Ghost belt never activates until the player manually grabs the trapped dirt
- Same behavior expected for ghost walls (same code path)
- Ghost lifts work correctly (different code path)

## Root Cause
`SimulateChunksJob.CanMoveTo()` blocks ALL cell movement into Air cells that overlap with ghost belt/wall tiles. This was intended to prevent external materials from flooding into a ghost area, but it also prevents dirt already inside the ghost area from moving within or falling out of it.

The check at `SimulateChunksJob.cs` lines ~759-764:
```csharp
// Inside the Air branch of CanMoveTo():
if (beltTiles.IsCreated && beltTiles.TryGetValue(idx, out BeltTile bt) && bt.isGhost)
    return false;  // Blocks ALL movement into ghost belt cells
if (wallTiles.IsCreated && wallTiles[idx].exists && wallTiles[idx].isGhost)
    return false;  // Blocks ALL movement into ghost wall cells
```

The check only looks at the **destination** cell. A dirt cell at row 6 of a ghost belt trying to fall to row 7 (both inside the ghost) is blocked because row 7 is an Air cell inside the ghost belt area. The dirt is completely immobilized.

**Why lifts are not affected:**
- `CanMoveTo()` does NOT check ghost lift tiles (lifts are passable by design)
- `LiftManager.UpdateGhostStates()` uses a relaxed check — only requires no Ground cells (Dirt/Sand/Water are OK)

## Affected Code
- `Assets/Scripts/Simulation/Jobs/SimulateChunksJob.cs` — `CanMoveTo()` ghost belt/wall blocking
- `Assets/Scripts/Structures/BeltManager.cs` — `UpdateGhostStates()` requires all 64 = Air
- `Assets/Scripts/Structures/WallManager.cs` — `UpdateGhostStates()` requires all 64 = Air

## Potential Solutions
### 1. Only block external entry, allow internal movement (Recommended)
Modify `CanMoveTo()` to also check whether the **source** cell is inside the same ghost structure. If both source and destination are ghost tiles of the same type, allow movement. This preserves the original intent (prevent new dirt from entering the ghost area) while letting trapped dirt drain out.

### 2. Remove ghost blocking from CanMoveTo entirely
Delete the ghost belt/wall checks from `CanMoveTo()`. Materials would freely fall through ghost areas. Simplest fix, but allows new dirt to pile into ghost areas from above, potentially preventing activation indefinitely.

### 3. Relax activation criteria for belts/walls
Change `UpdateGhostStates()` to activate when all cells are Air OR contain only falling materials (Powder/Liquid), similar to how lifts already work. The structure would activate and the falling materials would immediately drop out. Risk: brief visual glitch where structure material appears with dirt overlapping for 1 frame.

## Priority
Medium — ghost structures are a core feature, but the workaround (manually grabbing dirt) is available.

## Related Files
- `Assets/Scripts/Simulation/Jobs/SimulateChunksJob.cs`
- `Assets/Scripts/Structures/BeltManager.cs`
- `Assets/Scripts/Structures/WallManager.cs`
- `Assets/Scripts/Structures/LiftManager.cs` (not affected — reference for correct behavior)
