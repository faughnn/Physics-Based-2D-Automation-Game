# Cell Grab & Drop System

## Summary

A player mechanic that allows grabbing loose cells (powders, liquids, gases) from the world within a circular radius, holding them while the mouse is pressed, and dropping them back at the cursor position on release. This creates an intuitive way for players to move piles of material around the world.

---

## Goals

1. Click detection to grab loose cells near the cursor
2. Circular grab radius (configurable)
3. Remove grabbed cells from world and track them (count per material type)
4. Visual feedback showing held cells (count displayed near cursor)
5. Drop cells back at cursor position on mouse release
6. Only grab moveable materials (powders, liquids, gases - not static, not clusters)
7. Clean separation: Game layer only using Simulation APIs

---

## Design

### Architecture Overview

```
Assets/Scripts/Game/
├── CellGrabSystem.cs         # Main system handling grab/hold/drop logic
└── GrabVisualFeedback.cs     # Simple UI showing held cell count (optional separate file)
```

### Key Data Structures

#### GrabbedCells Storage

Track grabbed cells by material type using a dictionary:

```csharp
namespace FallingSand
{
    public class CellGrabSystem : MonoBehaviour
    {
        [Header("Grab Settings")]
        [SerializeField] private float grabRadius = 8f;  // Cells

        // Storage: materialId -> count
        private Dictionary<byte, int> grabbedCells = new Dictionary<byte, int>();

        // State
        private bool isHolding = false;
        private int totalGrabbedCount = 0;

        // References
        private CellWorld world;
        private Camera mainCamera;
    }
}
```

### Determining if a Cell is Grabbable

A cell is grabbable if:
1. It is NOT air (materialId != Materials.Air)
2. Its BehaviourType is NOT Static (excludes stone, belts, etc.)
3. It is NOT owned by a cluster (cell.ownerId == 0)

```csharp
private bool IsCellGrabbable(int x, int y)
{
    if (!world.IsInBounds(x, y))
        return false;

    int index = y * world.width + x;
    Cell cell = world.cells[index];

    // Skip air
    if (cell.materialId == Materials.Air)
        return false;

    // Skip cluster-owned cells
    if (cell.ownerId != 0)
        return false;

    // Check behaviour type - only grab moveable materials
    MaterialDef mat = world.materials[cell.materialId];
    if (mat.behaviour == BehaviourType.Static)
        return false;

    return true;
}
```

**BehaviourType Reference (from MaterialDef.cs):**
- `Static = 0` - Never moves (stone, belts) - NOT grabbable
- `Powder = 1` - Falls, piles (sand, ore) - Grabbable
- `Liquid = 2` - Falls, spreads (water, oil) - Grabbable
- `Gas = 3` - Rises, disperses (steam, smoke) - Grabbable

---

## Grab Mechanics

### Grab Radius

Circular area around click point. All grabbable cells within radius are collected:

```csharp
private void GrabCellsAtPosition(int centerX, int centerY)
{
    int radiusInt = Mathf.CeilToInt(grabRadius);
    float radiusSq = grabRadius * grabRadius;

    for (int dy = -radiusInt; dy <= radiusInt; dy++)
    {
        for (int dx = -radiusInt; dx <= radiusInt; dx++)
        {
            // Check circular distance
            if (dx * dx + dy * dy > radiusSq)
                continue;

            int x = centerX + dx;
            int y = centerY + dy;

            if (IsCellGrabbable(x, y))
            {
                GrabCell(x, y);
            }
        }
    }
}

private void GrabCell(int x, int y)
{
    int index = y * world.width + x;
    byte materialId = world.cells[index].materialId;

    // Track grabbed material
    if (!grabbedCells.ContainsKey(materialId))
        grabbedCells[materialId] = 0;
    grabbedCells[materialId]++;
    totalGrabbedCount++;

    // Remove from world
    world.SetCell(x, y, Materials.Air);
}
```

### Hold State

While mouse is held down, cells remain grabbed:
- Visual feedback shows count
- No continuous grabbing (only grabs on initial click)
- Player can move cursor to choose drop location

### Drop Mechanics

On release, spawn cells back at cursor position:

```csharp
private void DropCellsAtPosition(int centerX, int centerY)
{
    if (totalGrabbedCount == 0)
        return;

    // Spawn in expanding rings from center
    int spawned = 0;
    int maxAttempts = totalGrabbedCount * 4; // Allow for blocked positions
    int ring = 0;

    while (spawned < totalGrabbedCount && ring < maxAttempts)
    {
        // Spiral outward from center
        foreach (var pos in GetRingPositions(centerX, centerY, ring))
        {
            if (spawned >= totalGrabbedCount)
                break;

            if (CanPlaceCell(pos.x, pos.y))
            {
                byte materialToPlace = GetNextMaterialToPlace();
                if (materialToPlace != Materials.Air)
                {
                    world.SetCell(pos.x, pos.y, materialToPlace);
                    spawned++;
                }
            }
        }
        ring++;
    }

    ClearGrabbedCells();
}

private bool CanPlaceCell(int x, int y)
{
    if (!world.IsInBounds(x, y))
        return false;

    return world.GetCell(x, y) == Materials.Air;
}

private byte GetNextMaterialToPlace()
{
    // Return materials in order, decrementing counts
    foreach (var kvp in grabbedCells.ToList())
    {
        if (kvp.Value > 0)
        {
            grabbedCells[kvp.Key]--;
            totalGrabbedCount--;
            return kvp.Key;
        }
    }
    return Materials.Air;
}
```

---

## Visual Feedback

### Simple Approach: Count Near Cursor

Display grabbed cell count as text near the cursor using OnGUI:

```csharp
private void OnGUI()
{
    if (!isHolding || totalGrabbedCount == 0)
        return;

    Vector2 mousePos = Mouse.current.position.ReadValue();

    // Offset from cursor
    Vector2 labelPos = new Vector2(mousePos.x + 20, Screen.height - mousePos.y - 20);

    // Build display string
    string text = $"Holding: {totalGrabbedCount}";

    // Optional: show breakdown by material
    // foreach (var kvp in grabbedCells)
    // {
    //     if (kvp.Value > 0)
    //         text += $"\n  {GetMaterialName(kvp.Key)}: {kvp.Value}";
    // }

    GUI.Label(new Rect(labelPos.x, labelPos.y, 200, 100), text);
}
```

### Future Enhancement: Particle Preview

Later iterations could show a cluster of particles following the cursor to preview drop area. This is not required for initial implementation.

---

## Input Handling

### Mouse State Machine

```csharp
private void Update()
{
    if (mouse == null) return;

    // Get current cell position
    Vector2Int cellPos = GetCellAtMouse();

    // Grab on left mouse button press
    if (mouse.leftButton.wasPressedThisFrame)
    {
        GrabCellsAtPosition(cellPos.x, cellPos.y);
        isHolding = totalGrabbedCount > 0;
    }
    // Drop on release
    else if (mouse.leftButton.wasReleasedThisFrame && isHolding)
    {
        DropCellsAtPosition(cellPos.x, cellPos.y);
        isHolding = false;
    }

    // Cancel on right click (drop cells back at original position or discard)
    if (mouse.rightButton.wasPressedThisFrame && isHolding)
    {
        ClearGrabbedCells();  // Discard held cells
        isHolding = false;
    }
}
```

### Coordinate Conversion

Reuse pattern from SandboxController:

```csharp
private Vector2Int GetCellAtMouse()
{
    Vector2 mousePos = mouse.position.ReadValue();
    Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0));

    // Convert world position to cell coordinates
    // Based on SandboxController's coordinate system
    int cellX = Mathf.FloorToInt((mouseWorldPos.x + worldWidth) / 2f);
    int cellY = Mathf.FloorToInt((worldHeight - mouseWorldPos.y) / 2f);

    return new Vector2Int(cellX, cellY);
}
```

---

## Integration Points

### With SimulationManager / CellWorld

- Access `CellWorld` for reading/writing cells
- Use `CellWorld.SetCell()` for removing and placing cells
- Use `CellWorld.GetCell()` for checking cell contents
- Access `CellWorld.materials[]` for BehaviourType checks
- Access `CellWorld.cells[]` for ownerId checks

### With PlayerController

CellGrabSystem should be a separate component that can be enabled/disabled. It may conflict with other click interactions:

- Only active when player has Shovel equipped (check `player.EquippedTool == ToolType.Shovel`)
- Or make it a distinct tool type (GrabTool)

### With SandboxController (Sandbox Mode)

In sandbox mode, this system may not be needed or should be disabled to avoid conflicting with paint/brush controls.

---

## Configuration

```csharp
[Header("Grab Settings")]
[SerializeField] private float grabRadius = 8f;      // Cells
[SerializeField] private int maxGrabCount = 500;     // Prevent grabbing too many cells

[Header("Drop Settings")]
[SerializeField] private bool dropWithVelocity = true;  // Give dropped cells slight downward velocity
[SerializeField] private float dropSpread = 1f;         // How spread out dropped cells are
```

---

## Edge Cases

1. **Clicking on empty space**: No cells grabbed, isHolding stays false
2. **Clicking on static material**: Not grabbed (stone, belts ignored)
3. **Clicking on cluster**: Not grabbed (ownerId check excludes cluster cells)
4. **Dropping on solid**: Cells spawn in nearest available air cells, spiral outward
5. **Dropping at world edge**: Only spawn cells within bounds
6. **Dropping with no room**: Remaining cells are lost (or could be returned to player)
7. **Mixing materials**: Different material types tracked separately, spawn in whatever order
8. **Very large grabs**: Cap at maxGrabCount to prevent performance issues

---

## Testing Checklist

- [ ] Can grab sand cells with left click
- [ ] Cells are removed from world on grab
- [ ] Count displays near cursor while holding
- [ ] Cells spawn at cursor position on release
- [ ] Cannot grab stone or belts (static materials)
- [ ] Cannot grab cluster-owned cells
- [ ] Can grab water (liquid)
- [ ] Can grab steam (gas)
- [ ] Dropping on solid spawns cells around the solid
- [ ] Right click cancels grab (clears held cells)
- [ ] Grab radius is respected (circular area)
- [ ] World bounds are respected on grab and drop

---

## Implementation Order

### Phase 1: Core System
1. Create `CellGrabSystem.cs` with basic structure
2. Implement `IsCellGrabbable()` check
3. Implement `GrabCellsAtPosition()` with circular radius
4. Test: Click grabs cells, they disappear from world

### Phase 2: Drop Mechanics
5. Implement `DropCellsAtPosition()` with spiral spawn
6. Implement state machine (wasPressedThisFrame, wasReleasedThisFrame)
7. Test: Click to grab, release to drop

### Phase 3: Visual Feedback
8. Add OnGUI display for held cell count
9. Test: Count updates correctly, disappears on drop

### Phase 4: Integration
10. Wire up to GameController or PlayerController
11. Add tool requirement check (optional)
12. Test with various materials and scenarios

### Phase 5: Polish
13. Add right-click cancel
14. Add maxGrabCount limit
15. Add configuration options
16. Performance testing with large grabs

---

## Design Principles

Following the project's architecture philosophy:

- **Game layer only**: Uses Simulation APIs (CellWorld), no changes to simulation code
- **Systems not patches**: Single unified system handles all grab/drop behavior
- **Single responsibility**: CellGrabSystem handles only grab/drop, not other interactions
- **Uses existing APIs**: Leverages CellWorld.SetCell(), GetCell(), materials[], cells[]
- **Coordinate conversion**: Follows SandboxController pattern for mouse-to-cell conversion
