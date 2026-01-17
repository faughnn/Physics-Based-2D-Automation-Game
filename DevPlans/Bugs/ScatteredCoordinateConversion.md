# Scattered Coordinate Conversion Logic Analysis

## Executive Summary

The codebase has **coordinate conversion logic scattered across 8+ files** with multiple duplicate implementations of the same formulas. While the formulas are mostly consistent, the scattered nature creates:

1. **Maintenance burden** - Changes must be made in multiple places
2. **Risk of bugs** - Future modifications could introduce inconsistencies
3. **Violation of DRY** - Same formulas appear 6+ times
4. **No single source of truth** - Developers must hunt to find the correct formula

---

## Coordinate System Overview

The project uses two coordinate systems:

### Cell Grid Coordinates
- Origin: Top-left corner
- X: Increases left to right (0 to `world.width - 1`)
- Y: Increases top to bottom (0 to `world.height - 1`)
- Units: Integer cell indices

### Unity World Coordinates
- Origin: Center of screen
- X: Increases left to right (`-world.width` to `+world.width`)
- Y: Increases bottom to top (`-world.height` to `+world.height`)
- Units: World units (1 cell = 2 world units)
- Scale: `CellToWorldScale = 2f`

---

## Conversion Formulas

### Cell to World (the correct formulas)

```csharp
// Cell -> World
worldX = cellX * 2f - world.width
worldY = world.height - cellY * 2f
```

### World to Cell (the correct formulas)

```csharp
// World -> Cell
cellX = (worldX + world.width) / 2f
cellY = (world.height - worldY) / 2f
```

---

## All Locations Where Conversions Occur

### 1. SandboxController.cs (Lines 214-216)
**File:** `G:\Sandy\Assets\Scripts\SandboxController.cs`

**Purpose:** Mouse input to cell position for painting

**Code:**
```csharp
// Convert world position to cell coordinates
// Quad spans from -worldWidth to +worldWidth (2x scale)
// So world X range is -worldWidth to +worldWidth
// Cell X = (worldX + worldWidth) / 2
int cellX = Mathf.FloorToInt((mouseWorldPos.x + worldWidth) / 2f);
int cellY = Mathf.FloorToInt((worldHeight - mouseWorldPos.y) / 2f); // Flip Y for Y=0 at top
```

**Notes:**
- Uses `Mathf.FloorToInt` for rounding
- Uses member variables `worldWidth`/`worldHeight` instead of `world.width`/`world.height`

---

### 2. ClusterData.cs - LocalToWorldCell (Lines 44-70)
**File:** `G:\Sandy\Assets\Scripts\Simulation\Clusters\ClusterData.cs`

**Purpose:** Convert cluster pixel to cell grid position

**Code:**
```csharp
public Vector2Int LocalToWorldCell(ClusterPixel pixel, int worldWidth, int worldHeight)
{
    float cos = Mathf.Cos(RotationRad);
    float sin = Mathf.Sin(RotationRad);

    // Local pixel coords are in Unity convention (Y+ = up)
    // Use standard rotation formula (same as Unity's transform rotation)
    float rotatedX = pixel.localX * cos - pixel.localY * sin;
    float rotatedY = pixel.localX * sin + pixel.localY * cos;

    // Position is in Unity world coords, convert to cell coords
    // World X: ranges from -worldWidth to +worldWidth
    // World Y: ranges from +worldHeight to -worldHeight (Y flipped)
    Vector2 pos = Position;

    // Convert world position to cell position
    // cellX = (worldX + worldWidth) / 2
    // cellY = (worldHeight - worldY) / 2
    float cellCenterX = (pos.x + worldWidth) / 2f;
    float cellCenterY = (worldHeight - pos.y) / 2f;

    // Add rotated X offset directly, but SUBTRACT rotated Y offset
    // because cell grid Y+ = down, while Unity Y+ = up
    return new Vector2Int(
        Mathf.RoundToInt(cellCenterX + rotatedX),
        Mathf.RoundToInt(cellCenterY - rotatedY)
    );
}
```

**Notes:**
- Uses `Mathf.RoundToInt` for rounding (different from SandboxController)
- Also handles rotation transformation
- Passes world dimensions as parameters (good practice)

---

### 3. ClusterDebugSection.cs - CellToWorld (Lines 186-191)
**File:** `G:\Sandy\Assets\Scripts\Debug\Sections\ClusterDebugSection.cs`

**Purpose:** Convert cell position to world for debug gizmos

**Code:**
```csharp
private Vector2 CellToWorld(Vector2 cellPos)
{
    if (world == null) return Vector2.zero;
    float worldX = cellPos.x * 2f - world.width;
    float worldY = world.height - cellPos.y * 2f;
    return new Vector2(worldX, worldY);
}
```

**Notes:**
- Private helper method (not reusable)
- Inline magic number `2f` for scale

---

### 4. WorldDebugSection.cs (Lines 91-94)
**File:** `G:\Sandy\Assets\Scripts\Debug\Sections\WorldDebugSection.cs`

**Purpose:** Convert chunk cell bounds to world for gizmos

**Code:**
```csharp
// Convert cell coords to world coords
// Cells are displayed as 2x2 pixels, quad is centered at origin
float x1 = minCellX * 2f - world.width;
float x2 = maxCellX * 2f - world.width;
float y1 = world.height - maxCellY * 2f;  // Flip Y (cell Y=0 is top)
float y2 = world.height - minCellY * 2f;
```

**Notes:**
- Inline calculation, not extracted to method
- Inline magic number `2f` for scale

---

### 5. ClusterFactory.cs - CreateClusterFromRegion (Lines 101-105)
**File:** `G:\Sandy\Assets\Scripts\Simulation\Clusters\ClusterFactory.cs`

**Purpose:** Convert region center to world position for cluster creation

**Code:**
```csharp
// Convert cell center to world coordinates
// worldX = cellX * 2 - worldWidth
// worldY = worldHeight - cellY * 2
float worldCenterX = cellCenterX * 2f - world.width;
float worldCenterY = world.height - cellCenterY * 2f;
```

**Notes:**
- Inline calculation
- Local constant `CellToWorldScale = 2f` exists in same file but not used here

---

### 6. ClusterFactory.cs - Scale Polygon (Lines 42-45)
**File:** `G:\Sandy\Assets\Scripts\Simulation\Clusters\ClusterFactory.cs`

**Purpose:** Scale marching squares outline from cells to world

**Code:**
```csharp
// Scale polygon from cell units to world units
const float CellToWorldScale = 2f;
for (int i = 0; i < outline.Length; i++)
{
    outline[i] *= CellToWorldScale;
}
```

**Notes:**
- Uses local constant `CellToWorldScale`
- Only scaling, no offset (correct for local coordinates)

---

### 7. ClusterFactory.cs - CalculatePhysicsProperties (Lines 152, 171, 177-178)
**File:** `G:\Sandy\Assets\Scripts\Simulation\Clusters\ClusterFactory.cs`

**Purpose:** Scale pixel positions for physics calculations

**Code:**
```csharp
const float CellToWorldScale = 2f;
// ...
Vector2 centerOfMass = centerOfMassCell * CellToWorldScale;
// ...
float dx = (pixel.localX - centerOfMassCell.x) * CellToWorldScale;
float dy = (pixel.localY - centerOfMassCell.y) * CellToWorldScale;
```

**Notes:**
- Redeclares `CellToWorldScale` constant (3rd time in same file)
- Only scaling, no offset

---

### 8. TerrainColliderManager.cs (Lines 32, 62-63, 200-201)
**File:** `G:\Sandy\Assets\Scripts\Simulation\Clusters\TerrainColliderManager.cs`

**Purpose:** Generate terrain colliders at correct world positions

**Code:**
```csharp
// Scale: 1 cell = 2 world units
private const float CellToWorldScale = 2f;
// ...
// World spans from -width to +width in X, -height to +height in Y (due to 2x scale)
float halfWidth = world.width;   // width * CellToWorldScale / 2 = width
float halfHeight = world.height; // height * CellToWorldScale / 2 = height
// ...
// Convert cell coords to world coords
// worldX = cellX * 2 - worldWidth
// worldY = worldHeight - cellY * 2
float worldX = cellX * CellToWorldScale - world.width;
float worldY = world.height - cellY * CellToWorldScale;
```

**Notes:**
- Has class-level constant `CellToWorldScale`
- Uses constant in formula (good practice)
- Contains comment explaining boundary math

---

### 9. PhysicsSettings.cs - GetUnityGravity (Lines 42-49)
**File:** `G:\Sandy\Assets\Scripts\Simulation\PhysicsSettings.cs`

**Purpose:** Convert cell gravity to Unity physics gravity

**Code:**
```csharp
public static float GetUnityGravity(float cellGravity, float cellToWorldScale = 2f, float targetFps = 60f)
{
    // cellGravity is in cells/frame^2
    // Convert to world units/sec^2:
    // - Multiply by cellToWorldScale to get world units
    // - Multiply by fps^2 to convert from per-frame to per-second
    // - Negate because Unity Y+ is up, cell Y+ is down
    return -cellGravity * cellToWorldScale * targetFps * targetFps;
}
```

**Notes:**
- Uses default parameter `cellToWorldScale = 2f`
- This is the only place with the scale as a method parameter

---

### 10. CellRenderer.cs - CreateQuad (Lines 111-115)
**File:** `G:\Sandy\Assets\Scripts\Rendering\CellRenderer.cs`

**Purpose:** Create world-space quad for rendering

**Code:**
```csharp
// Each cell renders as 2x2 pixels
const int PixelsPerCell = 2;
int pixelWidth = world.width * PixelsPerCell;   // 1024 * 2 = 2048 pixels
int pixelHeight = world.height * PixelsPerCell; // 512 * 2 = 1024 pixels
float halfWidth = pixelWidth / 2f;   // 1024
float halfHeight = pixelHeight / 2f; // 512
```

**Notes:**
- Uses `PixelsPerCell` constant (same value as `CellToWorldScale`)
- Different naming convention suggests unclear relationship

---

## Inventory of Scale Constants

| Location | Name | Value | Scope |
|----------|------|-------|-------|
| ClusterFactory.cs:42 | `CellToWorldScale` | 2f | Local (method) |
| ClusterFactory.cs:152 | `CellToWorldScale` | 2f | Local (method) |
| TerrainColliderManager.cs:32 | `CellToWorldScale` | 2f | Private const |
| CellRenderer.cs:111 | `PixelsPerCell` | 2 | Local const |
| PhysicsSettings.cs:42 | `cellToWorldScale` | 2f (default) | Parameter |
| SandboxController.cs | (hardcoded `2f`) | 2f | Inline |
| ClusterDebugSection.cs | (hardcoded `2f`) | 2f | Inline |
| WorldDebugSection.cs | (hardcoded `2f`) | 2f | Inline |
| ClusterData.cs | (hardcoded factor) | 2f | Inline |

**Total:** 9 different places define or use the scale factor, with 4 different naming approaches.

---

## Identified Issues

### Issue 1: No Centralized Conversion Methods
There is no utility class or extension methods for coordinate conversion. Each file implements its own version.

### Issue 2: Inconsistent Rounding
- `SandboxController.cs` uses `Mathf.FloorToInt`
- `ClusterData.cs` uses `Mathf.RoundToInt`

This could cause off-by-one errors when a position is exactly on a cell boundary.

### Issue 3: Magic Numbers
The value `2f` appears inline in multiple places instead of referencing a central constant.

### Issue 4: Duplicate Constants
`CellToWorldScale` is declared as a local constant in multiple methods of `ClusterFactory.cs` instead of being a class or project-level constant.

### Issue 5: Inconsistent Naming
- `CellToWorldScale` (most cluster code)
- `PixelsPerCell` (renderer)
- `cellToWorldScale` (physics parameter)
- No name (inline `2f`)

### Issue 6: Missing Helper for Common Patterns
The pattern `world.height - cellY * 2f` (Y flip) appears in every Cell-to-World conversion but is never extracted.

---

## Potential Bugs

### Bug Risk 1: Rounding Inconsistency
If an object is at world position (0, 0), the cell position calculation differs:

```csharp
// SandboxController (FloorToInt)
cellX = Floor((0 + 1024) / 2) = Floor(512) = 512

// ClusterData (RoundToInt)
cellX = Round((0 + 1024) / 2) = Round(512) = 512
```

For position (1.0, 0):
```csharp
// SandboxController (FloorToInt)
cellX = Floor((1 + 1024) / 2) = Floor(512.5) = 512

// ClusterData (RoundToInt)
cellX = Round((1 + 1024) / 2) = Round(512.5) = 513  // DIFFERENT!
```

**Impact:** Mouse painting could be slightly misaligned with cluster pixel positions at cell boundaries.

### Bug Risk 2: Debug Visualization Mismatch
`ClusterDebugSection.CellToWorld` and `WorldDebugSection` use slightly different code paths. If one is updated and the other isn't, debug gizmos could become misaligned with actual rendering.

---

## Proposed Unified Solution

### Create: `CoordinateUtils.cs`

```csharp
// G:\Sandy\Assets\Scripts\Simulation\CoordinateUtils.cs

namespace FallingSand
{
    /// <summary>
    /// Single source of truth for cell-to-world coordinate conversions.
    /// All coordinate conversion should go through these methods.
    /// </summary>
    public static class CoordinateUtils
    {
        /// <summary>
        /// Scale factor: 1 cell = 2 world units.
        /// Used for both position conversion and physics calculations.
        /// </summary>
        public const float CellToWorldScale = 2f;

        /// <summary>
        /// Inverse scale for world-to-cell conversion.
        /// </summary>
        public const float WorldToCellScale = 0.5f;

        /// <summary>
        /// Convert cell coordinates to Unity world coordinates.
        /// </summary>
        /// <param name="cellX">Cell X position (0 = left edge)</param>
        /// <param name="cellY">Cell Y position (0 = top edge)</param>
        /// <param name="worldWidth">Width of cell grid</param>
        /// <param name="worldHeight">Height of cell grid</param>
        /// <returns>Unity world position (centered origin, Y+ = up)</returns>
        public static Vector2 CellToWorld(float cellX, float cellY, int worldWidth, int worldHeight)
        {
            float worldX = cellX * CellToWorldScale - worldWidth;
            float worldY = worldHeight - cellY * CellToWorldScale;
            return new Vector2(worldX, worldY);
        }

        /// <summary>
        /// Convert cell coordinates to Unity world coordinates.
        /// </summary>
        public static Vector2 CellToWorld(Vector2Int cellPos, int worldWidth, int worldHeight)
        {
            return CellToWorld(cellPos.x, cellPos.y, worldWidth, worldHeight);
        }

        /// <summary>
        /// Convert cell coordinates to Unity world coordinates.
        /// </summary>
        public static Vector2 CellToWorld(Vector2Int cellPos, CellWorld world)
        {
            return CellToWorld(cellPos.x, cellPos.y, world.width, world.height);
        }

        /// <summary>
        /// Convert Unity world coordinates to cell coordinates.
        /// Uses Floor rounding (position is within the returned cell).
        /// </summary>
        /// <param name="worldX">Unity world X position</param>
        /// <param name="worldY">Unity world Y position</param>
        /// <param name="worldWidth">Width of cell grid</param>
        /// <param name="worldHeight">Height of cell grid</param>
        /// <returns>Cell coordinates (Y=0 at top)</returns>
        public static Vector2Int WorldToCell(float worldX, float worldY, int worldWidth, int worldHeight)
        {
            int cellX = Mathf.FloorToInt((worldX + worldWidth) * WorldToCellScale);
            int cellY = Mathf.FloorToInt((worldHeight - worldY) * WorldToCellScale);
            return new Vector2Int(cellX, cellY);
        }

        /// <summary>
        /// Convert Unity world coordinates to cell coordinates.
        /// </summary>
        public static Vector2Int WorldToCell(Vector2 worldPos, int worldWidth, int worldHeight)
        {
            return WorldToCell(worldPos.x, worldPos.y, worldWidth, worldHeight);
        }

        /// <summary>
        /// Convert Unity world coordinates to cell coordinates.
        /// </summary>
        public static Vector2Int WorldToCell(Vector2 worldPos, CellWorld world)
        {
            return WorldToCell(worldPos.x, worldPos.y, world.width, world.height);
        }

        /// <summary>
        /// Convert Unity world coordinates to cell coordinates with Round rounding.
        /// Use this when you need the nearest cell center (e.g., for cluster pixel placement).
        /// </summary>
        public static Vector2Int WorldToCellRounded(float worldX, float worldY, int worldWidth, int worldHeight)
        {
            int cellX = Mathf.RoundToInt((worldX + worldWidth) * WorldToCellScale);
            int cellY = Mathf.RoundToInt((worldHeight - worldY) * WorldToCellScale);
            return new Vector2Int(cellX, cellY);
        }

        /// <summary>
        /// Convert Unity world coordinates to floating-point cell coordinates.
        /// Useful for sub-cell precision calculations.
        /// </summary>
        public static Vector2 WorldToCellFloat(float worldX, float worldY, int worldWidth, int worldHeight)
        {
            float cellX = (worldX + worldWidth) * WorldToCellScale;
            float cellY = (worldHeight - worldY) * WorldToCellScale;
            return new Vector2(cellX, cellY);
        }

        /// <summary>
        /// Scale a distance/offset from cell units to world units.
        /// Does NOT apply any coordinate flip - use for magnitudes only.
        /// </summary>
        public static float ScaleCellToWorld(float cellDistance)
        {
            return cellDistance * CellToWorldScale;
        }

        /// <summary>
        /// Scale a distance/offset from world units to cell units.
        /// Does NOT apply any coordinate flip - use for magnitudes only.
        /// </summary>
        public static float ScaleWorldToCell(float worldDistance)
        {
            return worldDistance * WorldToCellScale;
        }

        /// <summary>
        /// Get the world-space bounding box for the entire cell grid.
        /// </summary>
        public static Rect GetWorldBounds(int worldWidth, int worldHeight)
        {
            // World spans from -width to +width in X, -height to +height in Y
            return new Rect(-worldWidth, -worldHeight, worldWidth * 2, worldHeight * 2);
        }
    }
}
```

---

## Refactoring Checklist

After creating `CoordinateUtils.cs`, update these files:

1. **SandboxController.cs:214-216** - Use `CoordinateUtils.WorldToCell()`
2. **ClusterData.cs:62-63** - Use `CoordinateUtils.WorldToCellFloat()`
3. **ClusterDebugSection.cs:186-191** - Replace private method with `CoordinateUtils.CellToWorld()`
4. **WorldDebugSection.cs:91-94** - Use `CoordinateUtils.CellToWorld()`
5. **ClusterFactory.cs:42,104-105,152** - Use `CoordinateUtils.CellToWorldScale` and conversion methods
6. **TerrainColliderManager.cs:32,200-201** - Use `CoordinateUtils.CellToWorldScale` and conversion methods
7. **CellRenderer.cs:111** - Rename `PixelsPerCell` or reference `CoordinateUtils.CellToWorldScale`
8. **PhysicsSettings.cs:42** - Reference `CoordinateUtils.CellToWorldScale` instead of default parameter

---

## Priority

**Medium-High** - While no critical bugs have been identified yet, the scattered logic creates significant technical debt and increases the risk of introducing bugs during future changes. The unified solution is straightforward to implement and would improve code quality significantly.

---

## Implementation Notes

1. The rounding difference (Floor vs Round) should be standardized. Recommend:
   - `WorldToCell()` with Floor for mouse/painting (you want to paint the cell you're "in")
   - `WorldToCellRounded()` with Round for physics/cluster sync (you want the nearest cell center)

2. Consider adding extension methods for CellWorld:
   ```csharp
   public static Vector2 ToWorldPos(this CellWorld world, int cellX, int cellY)
   ```

3. The constant `CellToWorldScale` should be the single source of truth - if this ever needs to change (e.g., higher resolution rendering), there would be only one place to update.
