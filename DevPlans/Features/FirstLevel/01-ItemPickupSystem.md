# Item Pickup & Tool System

## Summary

A simple system for world items that the player can collect by walking over them. Collected tools are automatically equipped and tracked on the player. This is the foundation for the shovel and future tools.

---

## Goals

1. Items exist as physical objects in the game world
2. Player collects items by walking over them (trigger-based pickup)
3. Collected tools go into player inventory, with one "equipped" tool active
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

### Player Physics Requirements

**IMPORTANT**: For `OnTriggerEnter2D` to work, the player GameObject MUST have:

1. **Rigidbody2D** - Required for any 2D physics/trigger detection
   - `bodyType = Dynamic` (for gravity and movement)
   - `gravityScale = 1` (or as needed)
   - `freezeRotation = true` (prevent tumbling)

2. **Collider2D** - Required for trigger detection with items
   - Recommended: `CapsuleCollider2D` (pill shape for smooth movement over terrain)
   - Alternative: `BoxCollider2D` (simpler but may catch on edges)
   - `isTrigger = false` (solid collider for terrain collision)

The player's solid collider will detect overlaps with item trigger colliders, firing `OnTriggerEnter2D` on the PlayerController.

```csharp
// In GameController.CreatePlayer():
var rb = player.AddComponent<Rigidbody2D>();
rb.gravityScale = 1f;
rb.freezeRotation = true;

var collider = player.AddComponent<CapsuleCollider2D>();
collider.size = new Vector2(12, 24);  // ~6x12 cells (player is ~16 cells tall)
collider.direction = CapsuleDirection2D.Vertical;
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
            // Validate and configure collider
            var collider = GetComponent<Collider2D>();
            if (collider == null)
            {
                Debug.LogError($"[WorldItem] No Collider2D attached to {gameObject.name}! Item won't be collectible.");
                return;
            }
            collider.isTrigger = true;
        }

        /// <summary>
        /// Called when the item is collected. Plays feedback and destroys the world object.
        /// </summary>
        public void Collect()
        {
            Debug.Log($"[WorldItem] {toolType} collected!");

            // Play pickup sound (procedural beep)
            PlayPickupSound();

            Destroy(gameObject);
        }

        private void PlayPickupSound()
        {
            // Create temporary audio source for one-shot sound
            GameObject audioObj = new GameObject("PickupSound");
            AudioSource source = audioObj.AddComponent<AudioSource>();

            // Generate simple beep (A5 note, 0.1s duration)
            int sampleRate = 44100;
            float frequency = 880f;
            float duration = 0.1f;

            AudioClip clip = AudioClip.Create("Beep", (int)(sampleRate * duration), 1, sampleRate, false);
            float[] samples = new float[(int)(sampleRate * duration)];

            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = 1f - (t / duration);  // Fade out
                samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * envelope * 0.3f;
            }

            clip.SetData(samples, 0);
            source.clip = clip;
            source.Play();

            Destroy(audioObj, duration + 0.1f);
        }
    }
}
```

### PlayerController Extensions

Add inventory and equipped tool tracking with trigger-based pickup:

```csharp
// Add to PlayerController.cs

// Inventory - all tools the player has collected
private HashSet<ToolType> inventory = new HashSet<ToolType>();

// Currently equipped (active) tool
private ToolType equippedTool = ToolType.None;

// Public properties
public ToolType EquippedTool => equippedTool;
public IReadOnlyCollection<ToolType> Inventory => inventory;

// Events for UI/feedback
public event System.Action<ToolType> OnToolEquipped;
public event System.Action<ToolType> OnToolCollected;

// Add trigger detection (requires player to have Collider2D - see Player Physics Requirements)
private void OnTriggerEnter2D(Collider2D other)
{
    var item = other.GetComponent<WorldItem>();
    if (item != null)
    {
        CollectTool(item.ToolType);
        item.Collect();
    }
}

private void CollectTool(ToolType tool)
{
    // Add to inventory
    inventory.Add(tool);
    OnToolCollected?.Invoke(tool);

    // Silently equip the new tool (replaces current)
    equippedTool = tool;
    Debug.Log($"[PlayerController] Collected and equipped: {tool}");
    OnToolEquipped?.Invoke(tool);
}

/// <summary>
/// Switch to a different tool from inventory.
/// </summary>
public bool EquipTool(ToolType tool)
{
    if (tool == ToolType.None || inventory.Contains(tool))
    {
        equippedTool = tool;
        OnToolEquipped?.Invoke(tool);
        return true;
    }
    return false;  // Don't have this tool
}

/// <summary>
/// Check if player has a tool in inventory.
/// </summary>
public bool HasTool(ToolType tool)
{
    return inventory.Contains(tool);
}
```

---

## Item Spawning

### Coordinate Convention

Consistent with the rest of the codebase:
- Functions receive **cell coordinates** as parameters
- Conversion to world coordinates happens inside the function
- Scale: 1 cell = 2 world units (`CellToWorldScale = 2f`)
- Formula: `worldX = cellX * 2 - worldWidth`, `worldY = worldHeight - cellY * 2`

### In GameController

Items are spawned as part of level setup:

```csharp
// Add to GameController.cs

[Header("Item Settings")]
[SerializeField] private Color shovelColor = new Color(0.6f, 0.4f, 0.2f); // Brown

private void CreateShovelItem(Vector2 cellPosition)
{
    GameObject item = new GameObject("Shovel");

    // Visual - sprite is 8x12 pixels at PPU=2, resulting in 4x6 world units (2x3 cells)
    var sr = item.AddComponent<SpriteRenderer>();
    sr.sprite = CreateShovelSprite();
    sr.color = shovelColor;
    sr.sortingOrder = 5; // Below player (10), above terrain

    // Trigger collider for pickup - matches sprite size (4x6 world units)
    var collider = item.AddComponent<BoxCollider2D>();
    collider.size = new Vector2(4, 6);
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
    // 8x12 pixel texture at PPU=2 = 4x6 world units = 2x3 cells
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

    // PPU=2: 8x12 pixels becomes 4x6 world units (2x3 cells)
    return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 2f);
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
           └─> CollectTool(item.ToolType)
               ├─> Add to inventory HashSet
               ├─> Fire OnToolCollected event
               ├─> Update equippedTool field (silently replaces)
               ├─> Fire OnToolEquipped event
               └─> Log to console
           └─> item.Collect()
               ├─> Play pickup sound (procedural beep)
               └─> Destroy item GameObject
```

---

## Integration Points

### With PlayerController

- **Required components**: Rigidbody2D + CapsuleCollider2D (see Player Physics Requirements)
- Add `EquippedTool` property for other systems to query
- Add `Inventory` property to check collected tools
- Add `HasTool()` method for quick checks
- Add `OnToolEquipped` and `OnToolCollected` events for UI feedback
- Pickup happens automatically via OnTriggerEnter2D

### With GameController

- Spawn items during level setup (in Start or separate method)
- Can be extended to read item positions from level data
- Must ensure player has required physics components

### With Future Systems

- **Tool Usage System**: Will check `player.EquippedTool` to determine dig behavior
- **UI System**: Can subscribe to `OnToolCollected` to update inventory display
- **UI System**: Can subscribe to `OnToolEquipped` to show active tool
- **Audio System**: Pickup sound is now handled in WorldItem.Collect()

---

## Testing Checklist

### Player Setup
- [ ] Player has Rigidbody2D with correct settings (dynamic, gravity, freeze rotation)
- [ ] Player has CapsuleCollider2D (or BoxCollider2D) for trigger detection
- [ ] Player collider is NOT a trigger (solid for terrain collision)

### Item Spawning
- [ ] Shovel spawns at specified position
- [ ] Shovel is visible in the world (correct sprite size)
- [ ] Shovel has BoxCollider2D set as trigger

### Pickup Behavior
- [ ] Walking over shovel triggers OnTriggerEnter2D
- [ ] Shovel disappears after pickup
- [ ] Pickup sound plays (procedural beep)
- [ ] PlayerController.EquippedTool returns Shovel after pickup
- [ ] PlayerController.HasTool(ToolType.Shovel) returns true
- [ ] Console logs confirm pickup

### Inventory
- [ ] Collected tools are added to inventory
- [ ] Picking up new tool silently replaces equipped tool
- [ ] Previous tools remain in inventory
- [ ] EquipTool() can switch between inventory tools

---

## Future Extensions

1. **Drop Tool**: Player can drop currently equipped tool back into the world
2. **Tool Hotbar UI**: Visual display of inventory with keybinds to switch tools
3. **Visual Indicator**: Show equipped tool on player sprite
4. **Pickup Animation**: Brief scale/fade animation on collect (scale pop)
5. **Asset-based Audio**: Replace procedural beep with proper sound file

---

## Design Principles

Following the project's architecture philosophy:

- **Game layer only**: No changes to Simulation layer required
- **Simple inventory**: HashSet of collected tools, one equipped at a time
- **Silent replacement**: Picking up a new tool auto-equips it without prompt
- **Event-driven**: OnToolEquipped/OnToolCollected allow loose coupling to UI/audio
- **Unity-native**: Uses standard trigger collision, no custom physics

---

## Implementation Order

### Phase 1: Core Types
1. Create `ToolType.cs` enum
2. Create `WorldItem.cs` component (with null check and pickup sound)

### Phase 2: Player Setup
3. Ensure PlayerController has Rigidbody2D (dynamic, freeze rotation)
4. Ensure PlayerController has CapsuleCollider2D (NOT trigger)
5. Add inventory HashSet and equippedTool field to PlayerController
6. Add public properties: EquippedTool, Inventory, HasTool()
7. Add OnTriggerEnter2D pickup logic to PlayerController
8. Add OnToolEquipped and OnToolCollected events

### Phase 3: Spawning
9. Add CreateShovelItem method to GameController
10. Call from Start() to spawn test shovel

### Phase 4: Testing
11. Play test: walk over shovel, verify pickup
12. Verify pickup sound plays
13. Verify EquippedTool and HasTool() work
14. Test picking up second tool (should silently replace equipped)
15. Test EquipTool() to switch between inventory items
