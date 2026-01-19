# Duplicated CellToWorldScale Constant Analysis

## Executive Summary

The cell-to-world scale factor (2.0) is a fundamental constant that defines the relationship between cell grid coordinates and Unity world coordinates. Currently, this constant is **duplicated in 3 locations** as an explicit `const float CellToWorldScale = 2f` and appears as **magic number `2f`** in at least **8 additional locations** throughout the codebase. Additionally, the related `PixelsPerCell` constant (also 2) is duplicated in 2 locations.

This violates the project's architecture philosophy of "one source of truth" as documented in CLAUDE.md.

---

## Coordinate System Overview

The project uses two coordinate systems:

1. **Cell Grid Coordinates**: Origin at top-left, Y+ is down
   - X range: 0 to `world.width - 1`
   - Y range: 0 to `world.height - 1`

2. **Unity World Coordinates**: Origin at center, Y+ is up
   - X range: `-world.width` to `+world.width`
   - Y range: `-world.height` to `+world.height`

**Key transformations**:
- Cell to World: `worldX = cellX * 2 - worldWidth`, `worldY = worldHeight - cellY * 2`
- World to Cell: `cellX = (worldX + worldWidth) / 2`, `cellY = (worldHeight - worldY) / 2`

The `2` in these formulas is the cell-to-world scale factor (each cell occupies 2x2 world units).

---

## Explicit CellToWorldScale Constants

### Location 1: ClusterFactory.cs (line 42)
**File**: `G:\Sandy\Assets\Scripts\Simulation\Clusters\ClusterFactory.cs`

```csharp
// Line 42 - In CreateCluster()
const float CellToWorldScale = 2f;
for (int i = 0; i < outline.Length; i++)
{
    outline[i] *= CellToWorldScale;
}
```

**Usage**: Scaling marching squares polygon outline from cell units to world units.

### Location 2: ClusterFactory.cs (line 152)
**File**: `G:\Sandy\Assets\Scripts\Simulation\Clusters\ClusterFactory.cs`

```csharp
// Line 152 - In CalculatePhysicsProperties()
const float CellToWorldScale = 2f;

// Line 171
Vector2 centerOfMass = centerOfMassCell * CellToWorldScale;

// Lines 177-178
float dx = (pixel.localX - centerOfMassCell.x) * CellToWorldScale;
float dy = (pixel.localY - centerOfMassCell.y) * CellToWorldScale;
```

**Usage**: Converting center of mass and pixel distances for physics calculations.

### Location 3: TerrainColliderManager.cs (line 32)
**File**: `G:\Sandy\Assets\Scripts\Simulation\Clusters\TerrainColliderManager.cs`

```csharp
// Line 31-32
// Scale: 1 cell = 2 world units
private const float CellToWorldScale = 2f;

// Lines 200-201 (in UpdateChunkCollider)
float worldX = cellX * CellToWorldScale - world.width;
float worldY = world.height - cellY * CellToWorldScale;
```

**Usage**: Converting terrain collider outline vertices from cell to world coordinates.

### Location 4: PhysicsSettings.cs (line 42) - Default Parameter
**File**: `G:\Sandy\Assets\Scripts\Simulation\PhysicsSettings.cs`

```csharp
// Line 42
public static float GetUnityGravity(float cellGravity, float cellToWorldScale = 2f, float targetFps = 60f)
```

**Usage**: Default parameter value for gravity conversion. The value is embedded as a default rather than referencing a shared constant.

---

## Magic Number Usages (Inline `2f` or `* 2`)

### Location 5: ClusterFactory.cs (lines 104-105)
**File**: `G:\Sandy\Assets\Scripts\Simulation\Clusters\ClusterFactory.cs`

```csharp
// Lines 102-105 - In CreateClusterFromRegion()
// worldX = cellX * 2 - worldWidth
// worldY = worldHeight - cellY * 2
float worldCenterX = cellCenterX * 2f - world.width;
float worldCenterY = world.height - cellCenterY * 2f;
```

**Usage**: Converting cell center to world coordinates when creating cluster from region.

### Location 6: SandboxController.cs (lines 125-126)
**File**: `G:\Sandy\Assets\Scripts\SandboxController.cs`

```csharp
// Lines 124-129 - In SetupCamera()
// Each cell = 2x2 pixels
const int PixelsPerCell = 2;
int pixelHeight = worldHeight * PixelsPerCell; // 512 * 2 = 1024 pixels

// Ortho size = half the height in world units
mainCamera.orthographicSize = pixelHeight / 2f; // 512
```

**Note**: `PixelsPerCell` is conceptually the same as `CellToWorldScale`.

### Location 7: SandboxController.cs (lines 215-216)
**File**: `G:\Sandy\Assets\Scripts\SandboxController.cs`

```csharp
// Lines 214-216 - In PaintAtMouse()
// Cell X = (worldX + worldWidth) / 2
int cellX = Mathf.FloorToInt((mouseWorldPos.x + worldWidth) / 2f);
int cellY = Mathf.FloorToInt((worldHeight - mouseWorldPos.y) / 2f); // Flip Y for Y=0 at top
```

**Usage**: Converting mouse world position to cell coordinates for painting.

### Location 8: CellRenderer.cs (lines 111-115)
**File**: `G:\Sandy\Assets\Scripts\Rendering\CellRenderer.cs`

```csharp
// Lines 110-115 - In CreateQuad()
// Each cell renders as 2x2 pixels
const int PixelsPerCell = 2;
int pixelWidth = world.width * PixelsPerCell;   // 1024 * 2 = 2048 pixels
int pixelHeight = world.height * PixelsPerCell; // 512 * 2 = 1024 pixels
float halfWidth = pixelWidth / 2f;   // 1024
float halfHeight = pixelHeight / 2f; // 512
```

**Note**: Second occurrence of `PixelsPerCell` constant.

### Location 9: ClusterData.cs (lines 62-63)
**File**: `G:\Sandy\Assets\Scripts\Simulation\Clusters\ClusterData.cs`

```csharp
// Lines 60-63 - In LocalToWorldCell()
// cellX = (worldX + worldWidth) / 2
// cellY = (worldHeight - worldY) / 2
float cellCenterX = (pos.x + worldWidth) / 2f;
float cellCenterY = (worldHeight - pos.y) / 2f;
```

**Usage**: Converting Unity world position to cell coordinates.

### Location 10: ClusterDebugSection.cs (lines 189-190)
**File**: `G:\Sandy\Assets\Scripts\Debug\Sections\ClusterDebugSection.cs`

```csharp
// Lines 186-192 - In CellToWorld()
private Vector2 CellToWorld(Vector2 cellPos)
{
    if (world == null) return Vector2.zero;
    float worldX = cellPos.x * 2f - world.width;
    float worldY = world.height - cellPos.y * 2f;
    return new Vector2(worldX, worldY);
}
```

**Usage**: Converting cell position to world position for gizmo drawing.

### Location 11: WorldDebugSection.cs (lines 91-94)
**File**: `G:\Sandy\Assets\Scripts\Debug\Sections\WorldDebugSection.cs`

```csharp
// Lines 89-94 - In DrawGizmos()
// Convert cell coords to world coords
// Cells are displayed as 2x2 pixels, quad is centered at origin
float x1 = minCellX * 2f - world.width;
float x2 = maxCellX * 2f - world.width;
float y1 = world.height - maxCellY * 2f;  // Flip Y (cell Y=0 is top)
float y2 = world.height - minCellY * 2f;
```

**Usage**: Converting chunk dirty bounds to world coordinates for gizmo drawing.

### Location 12: TerrainColliderManager.cs (lines 62-63)
**File**: `G:\Sandy\Assets\Scripts\Simulation\Clusters\TerrainColliderManager.cs`

```csharp
// Lines 61-63 - In CreateWorldBoundaries()
// World spans from -width to +width in X, -height to +height in Y (due to 2x scale)
float halfWidth = world.width;   // width * CellToWorldScale / 2 = width
float halfHeight = world.height; // height * CellToWorldScale / 2 = height
```

**Note**: The comment explains the calculation but the simplification hides the scale factor.

---

## Related Constants Analysis

### PixelsPerCell (Duplicated in 2 Locations)

This constant represents the same concept as `CellToWorldScale` but with a different name:

1. **SandboxController.cs:125** - `const int PixelsPerCell = 2;`
2. **CellRenderer.cs:111** - `const int PixelsPerCell = 2;`

Both represent "each cell occupies 2 world units" or "each cell renders as 2x2 pixels".

---

## PhysicsSettings.cs Analysis

**File**: `G:\Sandy\Assets\Scripts\Simulation\PhysicsSettings.cs`

The file already exists and contains physics-related constants:

```csharp
public static class PhysicsSettings
{
    public const float Gravity = 1f;
    public const int MaxVelocity = 16;
    public static int SimulationSpeed { get; set; } = 15;
    public const int MinSimulationSpeed = 1;
    public const int MaxSimulationSpeed = 20;

    public static float GetUnityGravity(float cellGravity, float cellToWorldScale = 2f, float targetFps = 60f)
    {
        return -cellGravity * cellToWorldScale * targetFps * targetFps;
    }
}
```

**Problem**: The `cellToWorldScale = 2f` default parameter embeds the magic number rather than referencing a centralized constant.

---

## Inconsistencies Found

### No Major Inconsistencies
All locations use the value `2f` (or `2` for integers) consistently. The relationship is:
- 1 cell = 2 world units (horizontal and vertical)
- 1 cell = 2x2 pixels

### Naming Inconsistency
- `CellToWorldScale` - Used in cluster/physics code
- `PixelsPerCell` - Used in rendering/camera code

These are conceptually identical but named differently.

---

## Proposed Solution

### Option A: Add Constants to PhysicsSettings.cs (Recommended)

Since `PhysicsSettings.cs` already exists as a "single source of truth" for physics constants, add the scale factor there:

```csharp
public static class PhysicsSettings
{
    // Existing constants...

    /// <summary>
    /// Scale factor from cell units to world units.
    /// Each cell occupies this many world units in each dimension.
    /// Also equals pixels per cell for rendering.
    /// </summary>
    public const float CellToWorldScale = 2f;

    /// <summary>
    /// Integer version for rendering calculations.
    /// </summary>
    public const int PixelsPerCell = 2;

    // Update GetUnityGravity to use the constant:
    public static float GetUnityGravity(float cellGravity, float targetFps = 60f)
    {
        return -cellGravity * CellToWorldScale * targetFps * targetFps;
    }
}
```

### Option B: Create New CoordinateSystem.cs

If the coordinate system logic is considered separate from physics, create a new file:

```csharp
// Assets/Scripts/Simulation/CoordinateSystem.cs
public static class CoordinateSystem
{
    /// <summary>
    /// Scale factor from cell units to world units.
    /// </summary>
    public const float CellToWorldScale = 2f;

    /// <summary>
    /// Integer version (pixels per cell).
    /// </summary>
    public const int PixelsPerCell = 2;

    /// <summary>
    /// Convert cell coordinates to world coordinates.
    /// </summary>
    public static Vector2 CellToWorld(float cellX, float cellY, int worldWidth, int worldHeight)
    {
        return new Vector2(
            cellX * CellToWorldScale - worldWidth,
            worldHeight - cellY * CellToWorldScale
        );
    }

    /// <summary>
    /// Convert world coordinates to cell coordinates.
    /// </summary>
    public static Vector2 WorldToCell(float worldX, float worldY, int worldWidth, int worldHeight)
    {
        return new Vector2(
            (worldX + worldWidth) / CellToWorldScale,
            (worldHeight - worldY) / CellToWorldScale
        );
    }
}
```

---

## Files Requiring Updates

Once a centralized constant is established, these files need updates:

| File | Lines | Change Required |
|------|-------|-----------------|
| `ClusterFactory.cs` | 42, 152, 104, 105 | Replace local const and magic numbers |
| `TerrainColliderManager.cs` | 32, 200, 201 | Replace const and magic numbers |
| `PhysicsSettings.cs` | 42 | Replace default parameter |
| `SandboxController.cs` | 125, 215, 216 | Replace const and magic numbers |
| `CellRenderer.cs` | 111 | Replace const |
| `ClusterData.cs` | 62, 63 | Replace magic numbers |
| `ClusterDebugSection.cs` | 189, 190 | Replace magic numbers |
| `WorldDebugSection.cs` | 91-94 | Replace magic numbers |

**Total: 8 files, ~20 locations**

---

## Impact Assessment

- **Risk Level**: Low - purely refactoring, no logic changes
- **Benefit**: Single source of truth for scale factor; easier to modify if game design changes
- **Complexity**: Simple find-and-replace after adding centralized constant

---

## Recommendation

**Use Option A (PhysicsSettings.cs)** because:
1. The file already exists with the right namespace and purpose
2. It already contains physics constants that depend on this scale factor
3. It follows the existing pattern in the codebase
4. Minimal new code required

Additionally, consider adding helper methods like `CellToWorld()` and `WorldToCell()` to reduce code duplication further, but this can be done in a follow-up refactor.
