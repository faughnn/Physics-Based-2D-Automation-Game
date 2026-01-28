# Camera Follow System

**STATUS: IMPLEMENTED**

---

## Review Notes

**Reviewed: 2026-01-26 (First Pass)**

### Verified as Correct:
1. **CoordinateUtils** - Exists at `Assets/Scripts/Simulation/CoordinateUtils.cs` with `CellToWorldScale = 2`, `PixelsPerCell = 2`
2. **SimulationManager** - Exists with `GetRecommendedCameraSettings()`, singleton pattern, `WorldWidth`/`WorldHeight` accessors
3. **GameController** - Exists with `SetupCamera()` that calls `simulation.GetRecommendedCameraSettings()`
4. **PlayerController** - Exists with `Transform` and `Rigidbody2D` for camera to follow
5. **Coordinate system math** - Verified correct: world spans `(-worldWidth, -worldHeight)` to `(+worldWidth, +worldHeight)` in world units
6. **Ortho size calculation** - Verified: `viewportHeightCells * PixelsPerCell / 2 = 540 * 2 / 2 = 540` world units

### Issues Fixed:
1. **Level1Data.cs** - The file exists at `Assets/Scripts/Game/Levels/Level1Data.cs` and takes `worldWidth`/`worldHeight` as parameters (generic, not hardcoded). The plan's "Step 5" example was misleading - Level1Data.Create() already works with any dimensions. Clarified this in the plan.

2. **Existing GameController.SetupCamera()** - The current implementation calls `simulation.GetRecommendedCameraSettings()` which returns settings for viewing the FULL world. The plan correctly identifies this needs to change to use viewport dimensions instead. Verified the existing method signature.

3. **Camera directory** - Confirmed no `Assets/Scripts/Game/Camera/` directory exists yet - will need to be created.

### Important Architectural Notes:
- The plan introduces a SIGNIFICANT CHANGE: world dimensions from 1024x512 to 1920x1620 (4x larger area)
- This affects ALL FirstLevel plans (02, 03, 04, 05) which currently assume 1024x512
- **Recommendation**: Implement camera follow system first with current 1024x512 dimensions, THEN increase world size as a separate step. This is documented below.

### Dimension Verification (as specified in user request):
- Viewport: 960x540 cells (renders at 1920x1080 pixels at 2x scale) - **CORRECT**
- World: 1920x1620 cells (renders at 3840x3240 pixels) - **CORRECT for plan**
- Cell scale: 2x (1 cell = 2 world units) - **CORRECT**, matches `CoordinateUtils.CellToWorldScale`

### Dependencies Status:
| Dependency | Status | File |
|------------|--------|------|
| PlayerController | EXISTS | `Assets/Scripts/Game/PlayerController.cs` |
| GameController | EXISTS | `Assets/Scripts/Game/GameController.cs` |
| CoordinateUtils | EXISTS | `Assets/Scripts/Simulation/CoordinateUtils.cs` |
| SimulationManager | EXISTS | `Assets/Scripts/Simulation/SimulationManager.cs` |
| Level1Data | EXISTS | `Assets/Scripts/Game/Levels/Level1Data.cs` |
| LevelData | EXISTS | `Assets/Scripts/Game/Levels/LevelData.cs` |
| CameraFollow | NEEDS CREATION | `Assets/Scripts/Game/Camera/CameraFollow.cs` |

---

**Second Pass (2026-01-26)**

### Cross-Plan Consistency Verification

Verified this plan against the other verified FirstLevel plans:

| Plan | Dimensions | Status |
|------|-----------|--------|
| 08-TutorialMapLayout | World: 1920x1620, Viewport: 960x540 | **MATCHES** |
| 07-MultiObjectiveProgression | Uses dimensions from Level1Data (parameterized) | **COMPATIBLE** |
| 05-Level1Setup | Uses 1024x512 (legacy, will be updated) | **SUPERSEDED by 08** |
| 02-DiggingSystem | Dimension-agnostic (uses SimulationManager) | **COMPATIBLE** |
| 03-CellGrabDropSystem | Dimension-agnostic (uses SimulationManager) | **COMPATIBLE** |
| 04-BucketProgressionSystem | Dimension-agnostic (position from LevelData) | **COMPATIBLE** |

### Key Consistency Points Verified:

1. **Dimension Consistency with Plan 08**: This plan specifies 1920x1620 world and 960x540 viewport, which **exactly matches** Plan 08-TutorialMapLayout. Both plans are consistent.

2. **Camera Bounds Match Plan 08**: Plan 08 specifies camera center bounds as X: -960 to +960, Y: -1080 to +1080 world units. This plan calculates the same values:
   - X: (-1920 + 960) to (+1920 - 960) = -960 to +960 **CORRECT**
   - Y: (-1620 + 540) to (+1620 - 540) = -1080 to +1080 **CORRECT**

3. **Coordinate Conversion Verified**: Cross-checked against `CoordinateUtils.cs`:
   - `worldX = cellX * 2 - worldWidth` matches plan formulas **CORRECT**
   - `worldY = worldHeight - cellY * 2` matches plan formulas **CORRECT**
   - `CellToWorldScale = 2` and `PixelsPerCell = 2` confirmed **CORRECT**

4. **Ortho Size Calculation**: Verified against Plan 08 which states `orthographicSize = 540` (half of viewport height in world units). This plan's formula: `viewportHeight * PixelsPerCell / 2 = 540 * 2 / 2 = 540` **CORRECT**

5. **Plan 05-Level1Setup Supersession**: Plan 05 uses 1024x512 dimensions. Once Plan 08-TutorialMapLayout is implemented, the unified world design (1920x1620) will supersede the smaller world. Plans 02, 03, 04 are dimension-agnostic and will work with either size.

### First Pass Corrections Verified as Accurate:

1. **Level1Data parameterization**: Confirmed Level1Data.Create() takes worldWidth/worldHeight parameters - works with any dimensions.

2. **Camera setup change needed**: Confirmed GameController.SetupCamera() currently calls `simulation.GetRecommendedCameraSettings()` which fits full world. Plan correctly identifies this needs viewport-based settings.

3. **CameraFollow directory**: Confirmed `Assets/Scripts/Game/Camera/` does not exist.

### Minor Correction Made:

The first pass Review Notes stated Plans 02, 03, 04, 05 "assume 1024x512". Upon second pass review:
- Plans 02, 03, 04 are **dimension-agnostic** (they use SimulationManager APIs, not hardcoded dimensions)
- Only Plan 05 explicitly uses 1024x512, and it is marked as superseded by Plan 08

### No Issues Found in Second Pass

All technical details, coordinate calculations, and cross-plan references are consistent. The first pass corrections were accurate. This plan is ready for implementation once Plan 08-TutorialMapLayout dimensions are adopted.

---

## Summary

A camera system that smoothly follows the player character within a world larger than the viewport. The viewport displays 960x540 cells (rendered at 1920x1080 pixels with 2x scale), while the world is 1920x1620 cells. The camera clamps to world bounds to prevent showing areas outside the world.

---

## Goals

1. **Viewport Size Change**: Update world/viewport dimensions from 1024x512 to 960x540 (viewport) and 1920x1620 (world)
2. **Smooth Follow**: Camera smoothly follows the player with configurable smoothing
3. **Bound Clamping**: Camera position clamps to world edges (never shows outside the world)
4. **Clean Separation**: Game layer component attached to camera, uses existing coordinate system
5. **Configurable**: Dead zone, smoothing speed, and look-ahead as tunable parameters

---

## Technical Context

### Current Setup

**SimulationManager** currently:
- Creates world with `worldWidth` and `worldHeight` (defaults 1024x512)
- `GetRecommendedCameraSettings()` returns ortho size to fit entire world in view
- Camera positioned at (0, 0, -10) showing the full world

**GameController** currently:
- Calls `SimulationManager.Create(worldWidth, worldHeight)` with serialized fields
- Calls `simulation.GetRecommendedCameraSettings()` to configure camera
- Camera is static, centered on world origin

**Coordinate System** (from CoordinateUtils):
- Cell grid: (0,0) at top-left, Y+ = down
- Unity world: (0,0) at center, Y+ = up
- Scale: 1 cell = 2 world units (`CellToWorldScale = 2`)
- World spans from `(-worldWidth, -worldHeight)` to `(+worldWidth, +worldHeight)`

### New Requirements

| Property | Current Value | New Value |
|----------|---------------|-----------|
| Viewport Width | 1024 cells | 960 cells |
| Viewport Height | 512 cells | 540 cells |
| World Width | 1024 cells | 1920 cells |
| World Height | 512 cells | 1620 cells |
| Render Resolution | 2048x1024 px | 1920x1080 px |
| World Pixel Size | (same as render) | 3840x3240 px |

**Note:** The viewport is the visible area (what the camera shows). The world is the total simulation area.

---

## Design

### Architecture Overview

```
Assets/Scripts/Game/
├── Camera/
│   └── CameraFollow.cs    # MonoBehaviour attached to Main Camera
├── GameController.cs      # Modified to use new dimensions and setup camera follow
```

The camera follow system is purely a Game layer concern - it doesn't modify simulation behavior.

### CameraFollow Component

```csharp
namespace FallingSand
{
    /// <summary>
    /// Smoothly follows a target (player) while clamping to world bounds.
    /// Attach to the Main Camera.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Follow Settings")]
        [SerializeField] private float smoothSpeed = 5f;        // Lerp speed
        [SerializeField] private Vector2 deadZone = Vector2.zero;  // Size of dead zone (world units)
        [SerializeField] private float lookAheadDistance = 0f;  // Look ahead in movement direction

        [Header("Bounds")]
        [SerializeField] private bool clampToBounds = true;

        // World bounds (set by Initialize)
        private float worldMinX, worldMaxX;
        private float worldMinY, worldMaxY;
        private float viewportHalfWidth, viewportHalfHeight;

        private Camera cam;
    }
}
```

### World and Viewport Bounds

The camera's viewable area is determined by its orthographic size:
- `orthoSize` = half the viewport height in world units
- Viewport half-height = `orthoSize`
- Viewport half-width = `orthoSize * aspectRatio`

For our 960x540 viewport (1920x1080 at 2x):
- Pixel dimensions: 1920x1080
- Ortho size = 1080 / 2 = 540 world units (half height)
- Aspect ratio = 1920 / 1080 = 16/9
- Half-width = 540 * (16/9) = 960 world units

World bounds in world coordinates:
- Cell world: 1920x1620 cells
- World units: each cell is 2 world units
- World X range: -1920 to +1920 (total 3840 units)
- World Y range: -1620 to +1620 (total 3240 units)

### Camera Clamping Logic

```csharp
private Vector3 ClampCameraPosition(Vector3 desiredPos)
{
    // Calculate the bounds for camera center position
    // Camera can move from (minX + halfWidth) to (maxX - halfWidth)

    float clampedX = Mathf.Clamp(desiredPos.x,
        worldMinX + viewportHalfWidth,
        worldMaxX - viewportHalfWidth);

    float clampedY = Mathf.Clamp(desiredPos.y,
        worldMinY + viewportHalfHeight,
        worldMaxY - viewportHalfHeight);

    return new Vector3(clampedX, clampedY, desiredPos.z);
}
```

With viewport 960x540 world units and world 3840x3240 world units:
- Camera X can range from: -1920 + 960 = -960 to +1920 - 960 = +960
- Camera Y can range from: -1620 + 540 = -1080 to +1620 - 540 = +1080

---

## Implementation

### Step 1: Update World Dimensions in GameController

Modify `GameController.cs` serialized fields:

```csharp
[Header("World Settings")]
[SerializeField] private int worldWidth = 1920;   // Was 1024
[SerializeField] private int worldHeight = 1620;  // Was 512

[Header("Viewport Settings")]
[SerializeField] private int viewportWidth = 960;   // NEW - cells visible horizontally
[SerializeField] private int viewportHeight = 540;  // NEW - cells visible vertically
```

### Step 2: Update Camera Setup

Modify `GameController.SetupCamera()` to use viewport dimensions instead of world dimensions.

**Note:** The current implementation calls `simulation.GetRecommendedCameraSettings()` which returns settings to view the FULL world. This will be replaced with viewport-based settings.

```csharp
private void SetupCamera()
{
    mainCamera = Camera.main;
    if (mainCamera == null)
    {
        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        mainCamera = camObj.AddComponent<Camera>();
    }

    // Setup orthographic camera for viewport (not full world)
    mainCamera.orthographic = true;

    // Viewport dimensions determine ortho size
    // Formula: orthoSize = viewportHeightCells * PixelsPerCell / 2
    // For 540 cells: orthoSize = 540 * 2 / 2 = 540 world units
    mainCamera.orthographicSize = viewportHeight * CoordinateUtils.PixelsPerCell / 2f;

    // Initial position at world center (will be updated by CameraFollow)
    mainCamera.transform.position = new Vector3(0, 0, -10);
    mainCamera.backgroundColor = new Color(0.1f, 0.1f, 0.15f);

    // Add camera follow component
    SetupCameraFollow();
}

private void SetupCameraFollow()
{
    CameraFollow follow = mainCamera.gameObject.AddComponent<CameraFollow>();

    // Initialize with world bounds
    float worldHalfWidth = worldWidth;   // Cell width * scale (1920 * 2 / 2 = 1920)
    float worldHalfHeight = worldHeight; // Cell height * scale (1620 * 2 / 2 = 1620)

    follow.Initialize(
        player.transform,
        -worldHalfWidth, worldHalfWidth,   // World X bounds
        -worldHalfHeight, worldHalfHeight  // World Y bounds
    );
}
```

### Step 3: Create CameraFollow.cs

```csharp
// G:\Sandy\Assets\Scripts\Game\Camera\CameraFollow.cs

using UnityEngine;

namespace FallingSand
{
    /// <summary>
    /// Smoothly follows a target transform while clamping to world bounds.
    /// Prevents the camera from showing areas outside the cell world.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Follow Settings")]
        [SerializeField] private float smoothSpeed = 8f;
        [SerializeField] private Vector2 deadZone = new Vector2(50f, 30f);  // World units
        [SerializeField] private float lookAheadFactor = 0f;  // 0 = no look-ahead

        [Header("Bounds")]
        [SerializeField] private bool clampToBounds = true;

        // World bounds
        private float worldMinX, worldMaxX;
        private float worldMinY, worldMaxY;

        // Cached camera properties
        private Camera cam;
        private float viewportHalfWidth;
        private float viewportHalfHeight;

        // Tracking for smooth follow
        private Vector3 currentVelocity;
        private Vector3 desiredPosition;

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        /// <summary>
        /// Initialize camera bounds. Call after camera ortho size is set.
        /// </summary>
        /// <param name="target">Transform to follow</param>
        /// <param name="minX">World minimum X</param>
        /// <param name="maxX">World maximum X</param>
        /// <param name="minY">World minimum Y</param>
        /// <param name="maxY">World maximum Y</param>
        public void Initialize(Transform target, float minX, float maxX, float minY, float maxY)
        {
            this.target = target;
            worldMinX = minX;
            worldMaxX = maxX;
            worldMinY = minY;
            worldMaxY = maxY;

            // Calculate viewport dimensions from camera
            viewportHalfHeight = cam.orthographicSize;
            viewportHalfWidth = viewportHalfHeight * cam.aspect;

            // Snap to target immediately on start
            if (target != null)
            {
                Vector3 targetPos = GetTargetPosition();
                targetPos = ClampPosition(targetPos);
                transform.position = new Vector3(targetPos.x, targetPos.y, transform.position.z);
            }
        }

        /// <summary>
        /// Set the target transform to follow.
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        private void LateUpdate()
        {
            if (target == null || cam == null) return;

            // Get desired camera position based on target
            Vector3 targetPos = GetTargetPosition();

            // Apply dead zone - only move if target exceeds dead zone
            Vector3 currentPos = transform.position;
            Vector3 delta = targetPos - currentPos;

            // Dead zone check (horizontal)
            if (Mathf.Abs(delta.x) < deadZone.x)
                targetPos.x = currentPos.x;
            else
                targetPos.x = targetPos.x - Mathf.Sign(delta.x) * deadZone.x;

            // Dead zone check (vertical)
            if (Mathf.Abs(delta.y) < deadZone.y)
                targetPos.y = currentPos.y;
            else
                targetPos.y = targetPos.y - Mathf.Sign(delta.y) * deadZone.y;

            // Clamp to world bounds
            if (clampToBounds)
            {
                targetPos = ClampPosition(targetPos);
            }

            // Smooth follow
            Vector3 smoothed = Vector3.Lerp(currentPos, targetPos, smoothSpeed * Time.deltaTime);

            // Preserve Z position
            smoothed.z = transform.position.z;

            transform.position = smoothed;
        }

        private Vector3 GetTargetPosition()
        {
            Vector3 pos = target.position;

            // Optional: Look-ahead based on target velocity (if it has a Rigidbody2D)
            if (lookAheadFactor > 0)
            {
                Rigidbody2D rb = target.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    pos.x += rb.linearVelocity.x * lookAheadFactor;
                    pos.y += rb.linearVelocity.y * lookAheadFactor;
                }
            }

            return pos;
        }

        private Vector3 ClampPosition(Vector3 pos)
        {
            // Camera center must stay within bounds such that edges don't exceed world bounds
            float clampedX = Mathf.Clamp(pos.x,
                worldMinX + viewportHalfWidth,
                worldMaxX - viewportHalfWidth);

            float clampedY = Mathf.Clamp(pos.y,
                worldMinY + viewportHalfHeight,
                worldMaxY - viewportHalfHeight);

            return new Vector3(clampedX, clampedY, pos.z);
        }

        /// <summary>
        /// Immediately snap camera to target position (no smoothing).
        /// </summary>
        public void SnapToTarget()
        {
            if (target == null) return;

            Vector3 targetPos = GetTargetPosition();
            if (clampToBounds)
            {
                targetPos = ClampPosition(targetPos);
            }
            transform.position = new Vector3(targetPos.x, targetPos.y, transform.position.z);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (cam == null) cam = GetComponent<Camera>();
            if (cam == null) return;

            // Draw viewport bounds
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;

            Gizmos.color = Color.green;
            Vector3 pos = transform.position;
            Gizmos.DrawWireCube(pos, new Vector3(halfW * 2, halfH * 2, 0));

            // Draw dead zone
            if (deadZone.magnitude > 0)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(pos, new Vector3(deadZone.x * 2, deadZone.y * 2, 0));
            }
        }
#endif
    }
}
```

### Step 4: Update SimulationManager (Optional Enhancement)

Add a method to support custom viewport sizes:

```csharp
/// <summary>
/// Returns camera settings for a specific viewport size (smaller than world).
/// </summary>
public (float orthoSize, Vector3 position) GetCameraSettingsForViewport(int viewportWidthCells, int viewportHeightCells)
{
    // Viewport in world units (each cell = 2 world units)
    int pixelHeight = viewportHeightCells * CoordinateUtils.PixelsPerCell;

    // Ortho size = half the viewport height in world units
    float orthoSize = pixelHeight / 2f;
    Vector3 position = new Vector3(0, 0, -10);

    return (orthoSize, position);
}
```

### Step 5: Level1Data Already Handles Variable Dimensions

**No changes needed to Level1Data.cs** - it already uses parameterized `worldWidth` and `worldHeight`:

```csharp
// Level1Data.Create() already works generically:
public static LevelData Create(int worldWidth, int worldHeight)
{
    int groundSurfaceY = worldHeight - (worldHeight / 3);

    return new LevelData
    {
        // Uses worldWidth/worldHeight parameters throughout
        PlayerSpawn = new Vector2Int(worldWidth / 2, groundSurfaceY - 20),
        // etc.
    };
}
```

When GameController passes the new dimensions (1920x1620), Level1Data will automatically calculate correct positions:
- Ground surface: `1620 - (1620 / 3) = 1620 - 540 = 1080` (cell Y)
- Player spawn: `(960, 1060)` (centered, above ground)
- Shovel spawn: `(1060, 1072)` (right of player)
- Bucket spawn: `(150, 1066)` (left side, above ground)

**Note:** The bucket position may need manual adjustment since it uses a hardcoded X offset (150). Consider making this relative to `worldWidth` for better scaling.

---

## Coordinate Reference

### World Coordinate Bounds

With a 1920x1620 cell world where each cell is 2 world units:

| Property | Cell Value | World Value |
|----------|------------|-------------|
| World Width | 1920 cells | 3840 units |
| World Height | 1620 cells | 3240 units |
| World Min X | Cell 0 | -1920 units |
| World Max X | Cell 1919 | +1920 units |
| World Min Y | Cell 1619 (bottom) | -1620 units |
| World Max Y | Cell 0 (top) | +1620 units |

### Viewport Coordinate Bounds

With a 960x540 cell viewport:

| Property | Cell Value | World Value |
|----------|------------|-------------|
| Viewport Width | 960 cells | 1920 units |
| Viewport Height | 540 cells | 1080 units |
| Viewport Half-Width | 480 cells | 960 units |
| Viewport Half-Height | 270 cells | 540 units |

### Camera Position Range (Clamped)

Camera center can move within:
- X: (-1920 + 960) to (+1920 - 960) = -960 to +960
- Y: (-1620 + 540) to (+1620 - 540) = -1080 to +1080

---

## Dependencies

| Dependency | Status | File Location | Notes |
|------------|--------|---------------|-------|
| PlayerController | **EXISTS** | `Assets/Scripts/Game/PlayerController.cs` | Has Transform and Rigidbody2D for camera to follow |
| GameController | **EXISTS** | `Assets/Scripts/Game/GameController.cs` | Has `SetupCamera()` that needs modification |
| CoordinateUtils | **EXISTS** | `Assets/Scripts/Simulation/CoordinateUtils.cs` | `CellToWorldScale = 2`, `PixelsPerCell = 2` |
| SimulationManager | **EXISTS** | `Assets/Scripts/Simulation/SimulationManager.cs` | `GetRecommendedCameraSettings()`, `WorldWidth`, `WorldHeight` |
| Level1Data | **EXISTS** | `Assets/Scripts/Game/Levels/Level1Data.cs` | Already parameterized, works with any dimensions |
| LevelData | **EXISTS** | `Assets/Scripts/Game/Levels/LevelData.cs` | Data structures for level configuration |

### Related FirstLevel Plans
| Plan | Status | Impact |
|------|--------|--------|
| 02-DiggingSystem | COMPLETED | Uses world dimensions - will work with any size |
| 03-CellGrabDropSystem | COMPLETED | Uses world dimensions via SimulationManager |
| 04-BucketProgressionSystem | COMPLETED | Bucket position from Level1Data |
| 05-Level1Setup | IMPLEMENTED | Uses parameterized dimensions |

---

## Testing Checklist

### Viewport and Rendering
- [ ] World renders at 1920x1620 cells (verify in debug overlay)
- [ ] Viewport shows 960x540 cells
- [ ] Screen resolution is 1920x1080 (or scales appropriately)
- [ ] No black borders or stretched rendering

### Camera Following
- [ ] Camera smoothly follows player movement
- [ ] Camera responds to player jumping
- [ ] Smooth speed feels natural (adjust if too sluggish or too snappy)
- [ ] Dead zone prevents camera jitter during small movements

### Bound Clamping
- [ ] Camera stops at left world edge (doesn't show past X=0)
- [ ] Camera stops at right world edge (doesn't show past X=1919)
- [ ] Camera stops at top world edge (doesn't show past Y=0)
- [ ] Camera stops at bottom world edge (doesn't show past Y=1619)
- [ ] Player can move to corners and camera clamps correctly

### Edge Cases
- [ ] Player spawns with camera correctly positioned
- [ ] SnapToTarget() immediately positions camera without smoothing
- [ ] Camera works correctly if player is destroyed and re-created
- [ ] Camera doesn't break when player is at exact world corners

---

## Configuration Options

| Parameter | Default | Description |
|-----------|---------|-------------|
| `smoothSpeed` | 8.0 | Higher = faster follow, lower = more lag |
| `deadZone` | (50, 30) | Area around screen center where camera doesn't move |
| `lookAheadFactor` | 0.0 | Look ahead based on velocity (0 = disabled) |
| `clampToBounds` | true | Whether to enforce world edge clamping |

### Tuning Recommendations

- **Platformer feel**: smoothSpeed 8-10, deadZone (50, 30), lookAhead 0
- **Cinematic feel**: smoothSpeed 3-5, deadZone (100, 60), lookAhead 0.1
- **Tight control**: smoothSpeed 15+, deadZone (0, 0), lookAhead 0

---

## Implementation Order

**IMPORTANT**: Implement camera follow FIRST with existing 1024x512 dimensions, THEN scale up world size. This allows testing the camera system without breaking existing functionality.

### Phase 1: CameraFollow Component (with current 1024x512)
1. Create `Assets/Scripts/Game/Camera/` directory
2. Create `CameraFollow.cs` with basic follow logic
3. Add viewport dimension fields to `GameController` (initially same as world: 1024x512)
4. Modify `SetupCamera()` to use viewport dimensions for ortho size
5. Wire up `SetupCameraFollow()` in GameController
6. Test: Camera follows player (no visual change since viewport = world)

### Phase 2: Bound Clamping (still 1024x512)
7. Implement `ClampPosition()` in CameraFollow
8. Pass world bounds from GameController to CameraFollow.Initialize()
9. Test: Clamping works at world edges

### Phase 3: Dead Zone and Polish
10. Add dead zone support
11. Add `SnapToTarget()` for instant repositioning
12. Add editor gizmos for debugging
13. Add look-ahead option (optional)
14. Tune smoothSpeed and deadZone values
15. Test: Follow feels smooth and responsive

### Phase 4: Scale Up Dimensions
16. Update `GameController` world dimensions to 1920x1620
17. Update viewport dimensions to 960x540
18. Test: World is larger, camera follows correctly
19. Verify Level1Data generates correct positions for new dimensions

### Phase 5: Final Testing
20. Test full gameplay loop with new dimensions
21. Verify camera clamps correctly at all world edges
22. Adjust terrain and spawn positions if needed

---

## Files to Create/Modify

### New Files
| File | Purpose |
|------|---------|
| `Assets/Scripts/Game/Camera/CameraFollow.cs` | Camera follow component |
| `Assets/Scripts/Game/Camera/CameraFollow.cs.meta` | Unity meta file (auto-generated) |

### Modified Files
| File | Changes |
|------|---------|
| `Assets/Scripts/Game/GameController.cs` | Add viewport dimension fields, modify `SetupCamera()`, add `SetupCameraFollow()` |

### Files NOT Requiring Changes
| File | Reason |
|------|--------|
| `Assets/Scripts/Game/Levels/Level1Data.cs` | Already parameterized - works with any world dimensions |
| `Assets/Scripts/Game/Levels/LevelData.cs` | No changes needed |
| `Assets/Scripts/Simulation/SimulationManager.cs` | Optional enhancement only; not required for basic implementation |

---

## Design Principles Applied

- **Game layer only**: CameraFollow is purely game code, doesn't touch simulation
- **Single responsibility**: CameraFollow only handles camera movement
- **Configurable**: All behavior tunable via serialized fields
- **Uses existing systems**: Works with existing coordinate system and player
- **Clean integration**: Camera setup flows through GameController
