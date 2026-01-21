# Item Pickup & Tool System

## Summary

A simple system for world items that the player can collect by walking over them. Collected tools are automatically equipped and tracked on the player. This is the foundation for the shovel and future tools.

---

## Goals

1. Items exist as physical objects in the game world
2. Player collects items by walking over them (trigger-based pickup)
3. Equipped tool is tracked on the player
4. Visual representation of items in the world
5. Clean separation: Game layer only (no Simulation layer changes)

---

## Design

### Architecture Overview

```
Assets/Scripts/Game/
├── Items/
│   ├── ToolType.cs           # Enum for tool types
│   ├── WorldItem.cs          # Component for items in the world
│   └── ItemSpawner.cs        # Helper to spawn items (optional, for GameController)
├── PlayerController.cs       # Extended with equipped tool tracking
└── GameController.cs         # Spawns initial items
```

### ToolType Enum

```csharp
namespace FallingSand
{
    public enum ToolType
    {
        None = 0,
        Shovel = 1,
        // Future: Pickaxe, Bucket, etc.
    }
}
```

### WorldItem Component

Items in the world are GameObjects with:
- `SpriteRenderer` for visual
- `Collider2D` (trigger) for pickup detection
- `WorldItem` component for item data

```csharp
namespace FallingSand
{
    public class WorldItem : MonoBehaviour
    {
        [SerializeField] private ToolType toolType = ToolType.Shovel;

        public ToolType ToolType => toolType;

        private void Awake()
        {
            // Ensure collider is a trigger
            var collider = GetComponent<Collider2D>();
            if (collider != null)
                collider.isTrigger = true;
        }

        /// <summary>
        /// Called when the item is collected. Destroys the world object.
        /// </summary>
        public void Collect()
        {
            Debug.Log($"[WorldItem] {toolType} collected!");
            Destroy(gameObject);
        }
    }
}
```

### PlayerController Extensions

Add equipped tool tracking and trigger-based pickup:

```csharp
// Add to PlayerController.cs

// New field
private ToolType equippedTool = ToolType.None;

// New public property
public ToolType EquippedTool => equippedTool;

// New event for when tool changes (optional, for UI/feedback)
public event System.Action<ToolType> OnToolEquipped;

// Add trigger detection
private void OnTriggerEnter2D(Collider2D other)
{
    var item = other.GetComponent<WorldItem>();
    if (item != null)
    {
        EquipTool(item.ToolType);
        item.Collect();
    }
}

private void EquipTool(ToolType tool)
{
    equippedTool = tool;
    Debug.Log($"[PlayerController] Equipped: {tool}");
    OnToolEquipped?.Invoke(tool);
}
```

---

## Item Spawning

### In GameController

Items are spawned as part of level setup:

```csharp
// Add to GameController.cs

[Header("Item Settings")]
[SerializeField] private Color shovelColor = new Color(0.6f, 0.4f, 0.2f); // Brown

private void CreateShovelItem(Vector2 cellPosition)
{
    GameObject item = new GameObject("Shovel");

    // Visual
    var sr = item.AddComponent<SpriteRenderer>();
    sr.sprite = CreateShovelSprite();
    sr.color = shovelColor;
    sr.sortingOrder = 5; // Below player (10), above terrain

    // Trigger collider for pickup
    var collider = item.AddComponent<BoxCollider2D>();
    collider.size = new Vector2(12, 12); // Slightly larger than visual for easy pickup
    collider.isTrigger = true;

    // Item component
    var worldItem = item.AddComponent<WorldItem>();
    // toolType defaults to Shovel

    // Position in world coordinates
    float worldX = cellPosition.x * 2 - worldWidth;
    float worldY = worldHeight - cellPosition.y * 2;
    item.transform.position = new Vector3(worldX, worldY, 0);

    Debug.Log($"[GameController] Shovel spawned at cell ({cellPosition.x}, {cellPosition.y})");
}

private Sprite CreateShovelSprite()
{
    // Simple 8x8 shovel shape (can be replaced with actual sprite later)
    int width = 8, height = 12;
    Texture2D tex = new Texture2D(width, height);
    tex.filterMode = FilterMode.Point;

    Color[] pixels = new Color[width * height];
    // Fill with transparent
    for (int i = 0; i < pixels.Length; i++)
        pixels[i] = Color.clear;

    // Draw shovel shape (simple rectangle for now)
    for (int y = 0; y < height; y++)
    {
        for (int x = 0; x < width; x++)
        {
            // Handle (thin, top)
            if (y >= 6 && x >= 3 && x <= 4)
                pixels[y * width + x] = Color.white;
            // Blade (wider, bottom)
            else if (y < 6 && x >= 1 && x <= 6)
                pixels[y * width + x] = Color.white;
        }
    }

    tex.SetPixels(pixels);
    tex.Apply();

    return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 1f);
}
```

---

## Collision Layers (Recommended)

For proper trigger detection without physics interference:

| Layer | Purpose |
|-------|---------|
| Default | Terrain colliders |
| Player | Player collider |
| Items | World items (triggers) |

Configure in Unity:
- Player layer collides with Default (terrain)
- Player layer triggers with Items
- Items don't collide with anything (triggers only)

**Note:** Can work without custom layers initially by using trigger colliders, but layers prevent edge cases.

---

## Event Flow

```
1. Player walks over item
   └─> OnTriggerEnter2D on PlayerController
       └─> Check for WorldItem component
           └─> EquipTool(item.ToolType)
               ├─> Update equippedTool field
               ├─> Fire OnToolEquipped event
               └─> Log to console
           └─> item.Collect()
               └─> Destroy item GameObject
```

---

## Integration Points

### With PlayerController

- Add `EquippedTool` property for other systems to query
- Add `OnToolEquipped` event for UI/audio feedback
- Pickup happens automatically via OnTriggerEnter2D

### With GameController

- Spawn items during level setup (in Start or separate method)
- Can be extended to read item positions from level data

### With Future Systems

- **Tool Usage System**: Will check `player.EquippedTool` to determine dig behavior
- **UI System**: Can subscribe to `OnToolEquipped` to show equipped item
- **Audio System**: Can subscribe to `OnToolEquipped` for pickup sound

---

## Testing Checklist

- [ ] Shovel spawns at specified position
- [ ] Shovel is visible in the world
- [ ] Walking over shovel triggers pickup
- [ ] Shovel disappears after pickup
- [ ] PlayerController.EquippedTool returns Shovel after pickup
- [ ] Console logs confirm pickup
- [ ] Multiple items can be placed (only one equipped at a time)

---

## Future Extensions

1. **Drop Tool**: Player can drop currently equipped tool back into the world
2. **Tool Switching**: If multiple tool types, switch between them
3. **Visual Indicator**: Show equipped tool on player sprite
4. **Pickup Animation**: Brief scale/fade animation on collect
5. **Pickup Sound**: Audio feedback via event subscription

---

## Design Principles

Following the project's architecture philosophy:

- **Game layer only**: No changes to Simulation layer required
- **Simple and focused**: No inventory system, just "what tool am I holding"
- **Event-driven**: OnToolEquipped allows loose coupling to UI/audio
- **Unity-native**: Uses standard trigger collision, no custom physics

---

## Implementation Order

### Phase 1: Core Types
1. Create `ToolType.cs` enum
2. Create `WorldItem.cs` component

### Phase 2: Player Integration
3. Add equippedTool field and property to PlayerController
4. Add OnTriggerEnter2D pickup logic to PlayerController
5. Add OnToolEquipped event (optional but recommended)

### Phase 3: Spawning
6. Add CreateShovelItem method to GameController
7. Call from Start() to spawn test shovel

### Phase 4: Testing
8. Play test: walk over shovel, verify pickup
9. Verify EquippedTool property works
10. Test edge cases (multiple items, boundaries)
