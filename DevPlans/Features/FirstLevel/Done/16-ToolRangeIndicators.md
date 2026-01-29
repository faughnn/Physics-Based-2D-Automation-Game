# Tool Range Indicators

## Overview
Visual feedback showing the effective range of player tools. Helps players understand where they can dig and grab cells.

## Current Tool Ranges
From existing code:
- **DiggingController**: `maxDigDistance = 100` world units from player, `digRadius = 8` cells around cursor
- **CellGrabSystem**: `grabRadius = 8` cells around cursor (no max distance currently)

## Requirements

### 1. Player Reach Circle
- Circle around the **player** showing max interaction distance
- Only visible when shovel is equipped
- Faded/dashed line style (not intrusive)
- Color: Light blue or white with low opacity

### 2. Cursor Effect Circle
- Circle around the **mouse cursor** showing dig/grab radius
- Shows the actual area that will be affected
- Different colors for dig vs grab mode:
  - **Left click ready (dig)**: Orange/yellow circle
  - **Right click holding (grab)**: Green circle while grabbing
  - **Right click holding (drop preview)**: Blue circle for drop zone
- Only visible when shovel equipped and cursor within range

### 3. Out-of-Range Feedback
- When cursor is beyond max dig distance:
  - Reach circle turns red briefly
  - Cursor circle disappears or turns grey
  - Optional: subtle "X" indicator

## Visual Design

```
                    ┌─ Cursor circle (8 cell radius)
                    │  Shows affected area
                    ▼
                  ╭───╮
     ╭────────────┤ ○ ├────────────╮
     │            ╰───╯            │
     │                             │
     │         ┌───────┐           │
     │         │ PLAYER│           │
     │         └───────┘           │
     │              │              │
     ╰──────────────┼──────────────╯
                    │
           Player reach circle
         (100 world unit radius)
```

## Implementation

### File to Create
`Assets/Scripts/Game/UI/ToolRangeIndicator.cs`

```csharp
public class ToolRangeIndicator : MonoBehaviour
{
    [Header("Reach Circle (around player)")]
    [SerializeField] private float reachRadius = 100f;  // Match maxDigDistance
    [SerializeField] private Color reachColor = new Color(1f, 1f, 1f, 0.3f);
    [SerializeField] private Color reachOutOfRangeColor = new Color(1f, 0.3f, 0.3f, 0.5f);
    [SerializeField] private int reachSegments = 64;

    [Header("Cursor Circle (around mouse)")]
    [SerializeField] private float cursorRadius = 8f;   // Match digRadius/grabRadius
    [SerializeField] private Color digColor = new Color(1f, 0.8f, 0.2f, 0.5f);    // Orange/yellow
    [SerializeField] private Color grabColor = new Color(0.2f, 1f, 0.4f, 0.5f);   // Green
    [SerializeField] private Color dropColor = new Color(0.4f, 0.6f, 1f, 0.5f);   // Blue
    [SerializeField] private int cursorSegments = 32;

    private LineRenderer reachCircle;
    private LineRenderer cursorCircle;

    private PlayerController player;
    private CellGrabSystem grabSystem;
    private Camera mainCamera;

    private void Start()
    {
        player = GetComponent<PlayerController>();
        grabSystem = GetComponent<CellGrabSystem>();
        mainCamera = Camera.main;

        CreateReachCircle();
        CreateCursorCircle();
    }

    private void Update()
    {
        bool showIndicators = player.EquippedTool == ToolType.Shovel;

        reachCircle.enabled = showIndicators;
        cursorCircle.enabled = showIndicators;

        if (!showIndicators) return;

        UpdateReachCircle();
        UpdateCursorCircle();
    }

    private void CreateReachCircle()
    {
        GameObject obj = new GameObject("ReachCircle");
        obj.transform.SetParent(transform);
        reachCircle = obj.AddComponent<LineRenderer>();
        reachCircle.loop = true;
        reachCircle.useWorldSpace = true;
        reachCircle.startWidth = 2f;
        reachCircle.endWidth = 2f;
        reachCircle.positionCount = reachSegments;
        reachCircle.material = new Material(Shader.Find("Sprites/Default"));
        reachCircle.sortingOrder = 5;
    }

    private void CreateCursorCircle()
    {
        GameObject obj = new GameObject("CursorCircle");
        cursorCircle = obj.AddComponent<LineRenderer>();
        cursorCircle.loop = true;
        cursorCircle.useWorldSpace = true;
        cursorCircle.startWidth = 1.5f;
        cursorCircle.endWidth = 1.5f;
        cursorCircle.positionCount = cursorSegments;
        cursorCircle.material = new Material(Shader.Find("Sprites/Default"));
        cursorCircle.sortingOrder = 6;
    }

    private void UpdateReachCircle()
    {
        Vector3 playerPos = transform.position;
        Vector2 mouseWorld = GetMouseWorldPosition();
        float distToMouse = Vector2.Distance(playerPos, mouseWorld);

        // Change color if out of range
        Color color = distToMouse <= reachRadius ? reachColor : reachOutOfRangeColor;
        reachCircle.startColor = color;
        reachCircle.endColor = color;

        // Draw circle around player
        DrawCircle(reachCircle, playerPos, reachRadius, reachSegments);
    }

    private void UpdateCursorCircle()
    {
        Vector2 mouseWorld = GetMouseWorldPosition();
        float distToMouse = Vector2.Distance(transform.position, mouseWorld);

        // Hide if out of range
        if (distToMouse > reachRadius)
        {
            cursorCircle.enabled = false;
            return;
        }
        cursorCircle.enabled = true;

        // Determine color based on state
        Color color = digColor;
        if (grabSystem != null && grabSystem.IsHolding)
        {
            color = dropColor;  // Holding cells, showing drop preview
        }

        cursorCircle.startColor = color;
        cursorCircle.endColor = color;

        // Convert cursor radius from cells to world units (2 units per cell)
        float worldRadius = cursorRadius * 2f;
        DrawCircle(cursorCircle, mouseWorld, worldRadius, cursorSegments);
    }

    private void DrawCircle(LineRenderer lr, Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float x = center.x + Mathf.Cos(angle) * radius;
            float y = center.y + Mathf.Sin(angle) * radius;
            lr.SetPosition(i, new Vector3(x, y, 0));
        }
    }

    private Vector2 GetMouseWorldPosition()
    {
        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(
            new Vector3(mouseScreen.x, mouseScreen.y, 0));
        return mouseWorld;
    }
}
```

### File to Modify
`Assets/Scripts/Game/GameController.cs`

Add `ToolRangeIndicator` component to player:
```csharp
// In CreatePlayer(), after adding other components:
player.AddComponent<ToolRangeIndicator>();
```

## Configuration
Expose these in inspector for tuning:
- Reach radius (should match `DiggingController.maxDigDistance`)
- Cursor radius (should match `DiggingController.digRadius` / `CellGrabSystem.grabRadius`)
- Colors and opacity for each state
- Line widths

## Alternative: Shader-Based Approach
For better visuals, could use a custom shader with:
- Dashed lines for reach circle
- Soft glow on cursor circle
- Animated pulse when grabbing

## Testing Checklist
- [ ] Circles only visible when shovel equipped
- [ ] Reach circle follows player
- [ ] Cursor circle follows mouse
- [ ] Reach circle turns red when cursor out of range
- [ ] Cursor circle hidden when out of range
- [ ] Colors change based on dig/grab/drop state
- [ ] Circles render above terrain but below UI
