# Bug: Chunk Buffer Zone Stall

## Summary
Sand (and other falling materials) briefly pauses at specific Y coordinates that appear to correspond to chunk buffer zone boundaries (every ~16 cells from chunk edges).

## Symptoms
- Horizontal banding visible when large amounts of sand fall
- Materials briefly "stall" at certain Y positions before continuing to fall
- Stall positions appear to align with buffer zone edges (16 cells from chunk boundaries)

## Likely Cause
Related to the 4-pass chunk processing system and how `frameUpdated` interacts with buffer zones.

**Chunk system overview:**
- Chunks are 32x32 cells
- Each chunk processes an extended region with 16-cell buffer on each side (64x64 total)
- Groups A, B, C, D process sequentially within a frame
- `frameUpdated` prevents double-processing

**Suspected issue:**
When a cell moves from one chunk's buffer zone into another chunk's core, something in the frameUpdated logic or group ordering causes a 1-frame delay.

Possible scenarios:
1. Cell processed by Group A chunk's buffer, moves to position in Group C chunk's core
2. Group C processes later in same frame, but cell is skipped (frameUpdated) or something else causes delay
3. Cell doesn't move until next frame

## Reproduction
1. Open Sandbox scene
2. Select Sand material (key 3)
3. Paint a large amount of sand in the air
4. Observe horizontal banding as sand falls

## Files to Investigate
- `Assets/Scripts/Simulation/Jobs/SimulateChunksJob.cs` - SimulateCell, MoveCell, frameUpdated logic
- `Assets/Scripts/Simulation/CellSimulatorJobbed.cs` - Group scheduling
- `Assets/Scripts/Simulation/CellWorld.cs` - CollectChunkGroups

## Priority
Medium - Visual artifact, doesn't break functionality but affects polish.

## Notes
- This issue existed before dirt material changes
- Buffer size is 16 cells, max velocity is 16 cells/frame - these are related by design
- The 4-group checkerboard pattern is: A/B on even Y rows, C/D on odd Y rows

---

## Fix Applied (Pending Verification)

**Root cause:** Group A chunks (0, 2, 4...) run in parallel. Their extended regions were exactly adjacent (chunk 0: Y=0-47, chunk 2: Y=48-111). When chunk 0 reads Y=48 to check blocking, chunk 2 may be writing it simultaneously.

**Solution:** Asymmetric buffer sizes create a 2-cell gap between same-group chunks:
- Changed `BufferSize` from 16 to 15 in `SimulateChunksJob.cs`
- Changed `MaxVelocity` from 16 to 15 in `PhysicsSettings.cs`

This creates positions Y=47-48 (and similar at other chunk boundaries) that:
- Same-group chunks can READ but not WRITE
- Different-group chunks handle exclusively

**Files modified:**
- `Assets/Scripts/Simulation/Jobs/SimulateChunksJob.cs`
- `Assets/Scripts/Simulation/PhysicsSettings.cs`
- `CLAUDE.md` (documentation update)
