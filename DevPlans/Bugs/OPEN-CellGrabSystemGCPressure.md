# Bug: CellGrabSystem Creates Excessive GC Allocations During Drop

## Status: OPEN

## Category: Performance

## Summary

`CellGrabSystem` allocates new objects on every iteration during material drops, creating significant GC pressure for large grabs.

## Locations

### `GetNextMaterialToPlace()` (line 357-370)
Calls `grabbedCells.Keys.ToList()` every single invocation, creating a new `List<byte>` each time. During a drop of 500 cells, this creates 500 list allocations.

**Fix:** Iterate once and build a reusable queue, or use a pre-allocated list that is cleared and refilled.

### `GetRingPositions()` (lines 320-341)
Uses `IEnumerable<Vector2Int>` with `yield return`, creating an iterator object per ring per drop. For large drops with many rings, this is many allocations.

**Fix:** Accept a pre-allocated `List<Vector2Int>` parameter and fill it, or cache the ring positions.

### `Bucket.cs` cell collection (line 345)
Creates `new Dictionary<byte, int>` every frame that a cell is collected.

**Fix:** Pre-allocate the dictionary and clear it each frame.

## Impact

GC spikes during grab/drop operations, potentially causing frame hitches. More noticeable with large material quantities.

## Affected Files

- `Assets/Scripts/Game/CellGrabSystem.cs`
- `Assets/Scripts/Game/WorldObjects/Bucket.cs`

## Priority

Low-Medium — only noticeable with large material quantities, but easy to fix.
