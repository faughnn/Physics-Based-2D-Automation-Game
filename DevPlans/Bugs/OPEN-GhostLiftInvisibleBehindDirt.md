# Bug: Ghost Lift Invisible Behind Dirt During Activation

## Summary
When a lift is placed as a ghost structure in soft terrain and the player digs away surrounding material, the lift becomes completely invisible once it activates while dirt cells still occupy its area. The lift is functionally working (applying force) but has zero visual representation — the player only sees dirt.

## Symptoms
- Place a lift in terrain containing dirt/ground
- Dig away the ground material so only dirt remains in the lift area
- The lift activates (ghost overlay disappears) but dirt renders in its place
- The lift is invisible — no ghost overlay, no lift material visible
- The lift still functions (applies upward force to materials passing through)
- Player has no way to see the lift is there until all dirt naturally clears out

## Root Cause
Three factors combine to make the lift completely invisible during the transition window:

1. **Single-layer cell grid** — Each cell holds only one material ID. When dirt occupies a lift cell, the grid stores dirt, not lift material. `LiftManager.UpdateGhostStates()` only writes lift material to Air cells (line ~604-607):
   ```csharp
   byte existingMat = world.GetCell(cx, cy);
   if (existingMat == Materials.Air)
       world.SetCell(cx, cy, updated.materialId);
   ```

2. **Ghost overlay removed on activation** — When `isGhost` is cleared, `GhostStructureRenderer` stops drawing the overlay for that block (it only renders blocks returned by `GetGhostBlockPositions()`). So the semi-transparent ghost sprite disappears.

3. **Lift material only restored when dirt moves** — `SimulateChunksJob.MoveCell()` (line ~777-780) restores lift material when a cell moves out of a lift tile. But until the dirt actually moves, the lift has no visual representation at all.

The design assumed dirt would quickly move out of passable lifts, but in practice the transition window can last many frames — especially if dirt is blocked or settling slowly.

## Affected Code
- `Assets/Scripts/Structures/LiftManager.cs:604-607` — Only writes lift material to Air cells on activation
- `Assets/Scripts/Structures/LiftManager.cs:561-618` — Ghost-to-active transition allows activation with dirt present
- `Assets/Scripts/Rendering/GhostStructureRenderer.cs` — Stops rendering overlay once `isGhost` is cleared
- `Assets/Scripts/Simulation/Jobs/SimulateChunksJob.cs:777-780` — Restores lift material only when dirt moves out

## Potential Solutions
### 1. Keep overlay visible during transition (Minimal fix)
Continue showing a structure overlay (similar to ghost overlay but with active-lift coloring) for activated lifts that still have non-lift material in their cells. Track "transitioning" blocks in `GhostStructureRenderer` alongside ghost blocks. Remove the overlay per-cell as lift material gets restored by the simulation.

### 2. Force-clear dirt on activation
When a lift activates, displace remaining dirt cells to nearby empty positions (respecting material conservation). This eliminates the invisible transition window entirely. Slightly more complex but provides immediate visual feedback.

### 3. Solve via lift transparency (addresses related bug too)
If `OPEN-LiftStructureShouldBeMoreTransparent` is fixed first (making lifts render semi-transparently), this bug could be partially addressed: write lift material over dirt on activation since the lift would be transparent enough to see materials through it. This is the most complete solution but depends on shader changes.

## Priority
Medium

## Related Files
- `Assets/Scripts/Structures/LiftManager.cs` — Ghost-to-active transition logic
- `Assets/Scripts/Rendering/GhostStructureRenderer.cs` — Ghost overlay rendering
- `Assets/Scripts/Simulation/Jobs/SimulateChunksJob.cs` — Lift material restoration on cell move
- `Assets/Scripts/Rendering/CellRenderer.cs` — Cell world rendering (opaque, single-layer)
- `DevPlans/Bugs/OPEN-LiftStructureShouldBeMoreTransparent.md` — Related: lifts are fully opaque even when active
