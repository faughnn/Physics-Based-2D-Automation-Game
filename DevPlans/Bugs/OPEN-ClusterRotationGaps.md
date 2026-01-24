# Bug: Tiny Gaps Appear in Clusters During Rotation

**Status:** OPEN (Verified 2026-01-23)
**Note:** `LocalToWorldCell()` in `ClusterData.cs:56-82` still uses `Mathf.RoundToInt` independently for each pixel. No gap-filling or conservative rasterization has been implemented.

---

## Summary
When clusters rotate, tiny gaps appear in their interior due to discrete rounding during pixel coordinate transformation. Each pixel is independently transformed and rounded to integer grid coordinates, which can cause adjacent pixels to skip cells.

## Symptoms
- Small holes/gaps appear inside clusters during rotation
- Gaps are most visible at non-axis-aligned angles (30, 45, 60 degrees)
- Gaps flicker or move as the cluster continues rotating
- Interior of cluster appears to have missing pixels

## Root Cause
The `LocalToWorldCell` function in `ClusterData.cs:53-80` transforms each pixel independently using `Mathf.RoundToInt`:

```csharp
return new Vector2Int(
    Mathf.RoundToInt(cellCenterX + rotatedX),
    Mathf.RoundToInt(cellCenterY - rotatedY)
);
```

**The problem:** Rotation is a continuous transformation applied to discrete pixels that are then re-discretized. When two adjacent pixels are rotated:
- They may both round to the same cell (overlap - no visual issue)
- They may round to non-adjacent cells (gap - visible hole)

**Example at 45 degrees:**
```
Pixel A at (0,0) -> rotates to (0, 0)      -> rounds to cell (0, 0)
Pixel B at (1,0) -> rotates to (0.707, 0.707) -> rounds to cell (1, 1)

Gap appears at cell (1, 0) or (0, 1) - they're no longer adjacent!
```

The sync process in `ClusterManager.SyncClusterToWorld` writes each pixel without detecting or filling gaps.

## Affected Code
- `Assets/Scripts/Simulation/Clusters/ClusterData.cs:53-80` - `LocalToWorldCell()` transformation
- `Assets/Scripts/Simulation/Clusters/ClusterManager.cs:323-358` - `SyncClusterToWorld()` no gap detection

## Potential Solutions

### 1. Post-Transform Gap Filling (Recommended)
After transforming all pixels, detect interior gaps by checking 4-connectivity and fill them with the surrounding material.

**Pros:** Simple to implement, preserves existing pipeline
**Cons:** Slight performance cost, may occasionally fill unintended cells

### 2. Conservative Rasterization
For each pixel, draw not just the rounded cell but also any cells the pixel's area overlaps when transformed.

**Pros:** Mathematically correct coverage
**Cons:** Can cause clusters to "grow" slightly, more complex to implement

### 3. Scanline Fill Approach
Rasterize the rotated cluster polygon outline and flood-fill the interior, rather than transforming individual pixels.

**Pros:** Guaranteed no interior gaps
**Cons:** Requires significant refactor, loses per-pixel material info for multi-material clusters

### 4. Higher Precision Storage
Store local coordinates as floats instead of shorts, use sub-pixel precision during transformation.

**Pros:** More accurate transformation
**Cons:** Memory increase, still fundamentally a discretization problem

## Priority
Low - Visual artifact only, does not affect physics or gameplay logic.

## Related Files
- `Assets/Scripts/Simulation/Clusters/ClusterData.cs` - Pixel transformation
- `Assets/Scripts/Simulation/Clusters/ClusterManager.cs` - Sync pipeline
- `Assets/Scripts/Simulation/Clusters/ClusterPixel.cs` - Pixel data structure
- `DevPlans/Bugs/OPEN-ScatteredCoordinateConversion.md` - Related coordinate issues
