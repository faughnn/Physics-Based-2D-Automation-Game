# Feature: Dirt Material

## Status: IN PROGRESS

## Summary
Add a new "Dirt" material that behaves as a powder but flows significantly less than sand. Dirt should pile up steeper and spread slower, simulating compacted earth behavior.

## Goals
- Provide a heavier, less mobile powder material for terrain building
- Differentiate material behaviors beyond just color and density
- Rename the unused `friction` field to `slideResistance` and activate it for powders
- Expand the palette of natural materials available

## What's Done

### MaterialDef.cs
- [x] Renamed `friction` field to `slideResistance`
- [x] Added `Materials.Dirt = 17` constant
- [x] Added Dirt material definition (density=140, slideResistance=200, brown color)
- [x] Set Sand's slideResistance to 0 (preserves original behavior)

### SimulateChunksJob.cs
- [x] Added slideResistance check in `SimulatePowder()` before diagonal movement
- [x] Uses position-based hash `HashPosition(x, y, 0)` (no currentFrame) for consistent per-position behavior

### SandboxController.cs
- [x] Added D key shortcut for Dirt selection
- [x] Added materials help log at startup

### Cell.cs
- [x] Added `CellFlags.Settled` constant (currently unused, for future optimization)

## What Still Needs Work

### Behavior Issues
1. **Position-based hash feels wrong** - Currently ~78% of positions never try to slide, ~22% always try. This creates "sticky spots" and "slidey spots" rather than dirt that naturally resists sliding. Need better approach.

2. **Settled flag not implemented** - We added the flag but reverted the implementation due to complexity. A proper settled system would:
   - Mark cells as settled when they can't move
   - Wake cells when neighbors move away
   - Significantly improve performance for stable piles

### Related Bug
- `OPEN-ChunkBufferZoneStall.md` - Horizontal banding when sand/dirt falls. Pre-existing issue with chunk buffer zones, affects all falling materials.

## Design Notes

### slideResistance Approaches Tried

**Approach 1: Per-frame random (reverted)**
```csharp
uint hash = HashPosition(x, y, currentFrame);
if ((hash & 255) < mat.slideResistance) // Don't slide
```
- Problem: Cell "jumps on the spot" - different decision each frame
- Problem: Cells never truly settle, chunks stay active

**Approach 2: Position-based (current)**
```csharp
uint hash = HashPosition(x, y, 0);  // No currentFrame
if ((hash & 255) < mat.slideResistance) // Don't slide
```
- Problem: Creates deterministic "sticky" and "slidey" positions
- Not natural dirt behavior

**Approach 3: Settled flag (attempted, reverted)**
- Set Settled flag when cell can't move
- Clear flag when neighbor moves away
- Problem: Complexity with determining when to settle (velocity=0 but could fall, etc.)

### Potential Better Approaches
1. **Check diagonals first** - Only do slideResistance check if diagonal IS available. If blocked, just stay put (no randomness needed).

2. **Velocity-based** - Dirt loses velocity faster, effectively making it "stickier" without special slide logic.

3. **Threshold-based settling** - After N frames of not moving, mark as settled. Simpler than per-move checks.

## Current Behavior
| Material | Density | slideResistance | Behavior |
|----------|---------|-----------------|----------|
| Sand     | 128     | 0               | Always tries to slide |
| Dirt     | 140     | 200             | ~78% of positions never slide, ~22% always slide |

## Related Files
- `Assets/Scripts/Simulation/MaterialDef.cs` - Material definitions
- `Assets/Scripts/Simulation/Jobs/SimulateChunksJob.cs` - Powder simulation logic
- `Assets/Scripts/Simulation/Cell.cs` - CellFlags.Settled (unused)
- `Assets/Scripts/SandboxController.cs` - D key shortcut

## Priority
Low - Quality of life addition, not blocking other features.
