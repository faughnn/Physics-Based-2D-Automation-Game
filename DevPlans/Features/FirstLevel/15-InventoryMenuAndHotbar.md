# Inventory Menu and Hotbar

## Overview
Add a UI system for selecting tools and placeable structures. Includes:
- **Hotbar** - Always visible, quick access via number keys (1-5)
- **Inventory Menu** - Full menu opened with Tab/I, shows all unlocked items

## Requirements

### Hotbar
- Fixed bar at bottom of screen (5 slots)
- Shows currently available tools/structures
- Number keys 1-5 to select slot
- Visual highlight on selected slot
- Greyed out slots for locked items
- Items unlock as abilities are earned (belts, lifts)

### Inventory Menu
- Opens with Tab or I key
- Pauses game while open (Time.timeScale = 0)
- Grid of all items (tools + structures)
- Click to select and equip
- Shows item name and description on hover
- Close with Tab/I/Escape
- Locked items shown but not selectable

## Slot Layout

### Hotbar Slots (1-5)
1. **Shovel** - Dig and carry dirt (always available once collected)
2. **Belt** - Place conveyor belts (unlocked after Level 1)
3. **Lift** - Place vertical lifts (unlocked after Level 2)
4. **Empty** - Future expansion
5. **Empty** - Future expansion

## Data Model

### ItemSlot
```csharp
public enum ItemCategory
{
    Tool,       // Shovel, etc.
    Structure   // Belt, Lift, etc.
}

public class ItemDefinition
{
    public string Id;
    public string DisplayName;
    public string Description;
    public ItemCategory Category;
    public Sprite Icon;
    public Ability RequiredAbility;  // Ability.None = always available (if collected)
    public ToolType ToolType;        // For tools
    public StructureType StructureType;  // For structures
}
```

### StructureType Enum
```csharp
public enum StructureType
{
    None,
    Belt,
    Lift
}
```

## Implementation

### Files to Create

#### 1. `Assets/Scripts/Game/UI/ItemDefinition.cs`
Data class for item definitions.

#### 2. `Assets/Scripts/Game/UI/ItemRegistry.cs`
Static registry of all items:
```csharp
public static class ItemRegistry
{
    public static readonly ItemDefinition Shovel = new ItemDefinition
    {
        Id = "shovel",
        DisplayName = "Shovel",
        Description = "Dig terrain and carry loose dirt",
        Category = ItemCategory.Tool,
        RequiredAbility = Ability.None,
        ToolType = ToolType.Shovel
    };

    public static readonly ItemDefinition Belt = new ItemDefinition
    {
        Id = "belt",
        DisplayName = "Conveyor Belt",
        Description = "Automatically moves materials horizontally",
        Category = ItemCategory.Structure,
        RequiredAbility = Ability.PlaceBelts,
        StructureType = StructureType.Belt
    };

    public static readonly ItemDefinition Lift = new ItemDefinition
    {
        Id = "lift",
        DisplayName = "Lift",
        Description = "Moves materials vertically",
        Category = ItemCategory.Structure,
        RequiredAbility = Ability.PlaceLifts,
        StructureType = StructureType.Lift
    };

    public static IEnumerable<ItemDefinition> All => ...;
}
```

#### 3. `Assets/Scripts/Game/UI/Hotbar.cs`
MonoBehaviour for hotbar display and input:
```csharp
public class Hotbar : MonoBehaviour
{
    private const int SlotCount = 5;
    private ItemDefinition[] slots = new ItemDefinition[SlotCount];
    private int selectedIndex = 0;

    private void Update()
    {
        // Number key input (1-5)
        for (int i = 0; i < SlotCount; i++)
        {
            if (Keyboard.current[(Key)(Key.Digit1 + i)].wasPressedThisFrame)
            {
                SelectSlot(i);
            }
        }
    }

    private void SelectSlot(int index)
    {
        if (slots[index] == null) return;
        if (!IsItemUnlocked(slots[index])) return;

        selectedIndex = index;
        EquipItem(slots[index]);
    }

    private void OnGUI()
    {
        // Draw hotbar at bottom center
        // Each slot: icon, number, highlight if selected, grey if locked
    }
}
```

#### 4. `Assets/Scripts/Game/UI/InventoryMenu.cs`
Full inventory screen:
```csharp
public class InventoryMenu : MonoBehaviour
{
    private bool isOpen = false;

    private void Update()
    {
        if (Keyboard.current.tabKey.wasPressedThisFrame ||
            Keyboard.current.iKey.wasPressedThisFrame)
        {
            ToggleMenu();
        }

        if (isOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseMenu();
        }
    }

    private void ToggleMenu()
    {
        isOpen = !isOpen;
        Time.timeScale = isOpen ? 0f : 1f;
    }

    private void OnGUI()
    {
        if (!isOpen) return;

        // Draw semi-transparent background
        // Draw grid of all items
        // Highlight hovered item, show tooltip
        // Click to select
    }
}
```

#### 5. `Assets/Scripts/Game/UI/PlayerEquipment.cs`
Manages what the player currently has equipped:
```csharp
public class PlayerEquipment : MonoBehaviour
{
    public static PlayerEquipment Instance { get; private set; }

    public ItemDefinition EquippedItem { get; private set; }

    public event Action<ItemDefinition> OnEquipmentChanged;

    public void Equip(ItemDefinition item)
    {
        EquippedItem = item;
        OnEquipmentChanged?.Invoke(item);
    }

    public bool IsInBuildMode => EquippedItem?.Category == ItemCategory.Structure;
}
```

### Files to Modify

#### `Assets/Scripts/Game/GameController.cs`
Add creation of Hotbar and InventoryMenu:
```csharp
// In Start():
CreateInventoryUI();

private void CreateInventoryUI()
{
    // PlayerEquipment singleton
    var equipObj = new GameObject("PlayerEquipment");
    equipObj.AddComponent<PlayerEquipment>();

    // Hotbar
    var hotbarObj = new GameObject("Hotbar");
    hotbarObj.AddComponent<Hotbar>();

    // Inventory Menu
    var menuObj = new GameObject("InventoryMenu");
    menuObj.AddComponent<InventoryMenu>();
}
```

#### `Assets/Scripts/Game/Structures/StructurePlacementController.cs`
Check `PlayerEquipment.Instance.EquippedItem` instead of ability directly.

## Visual Design

### Hotbar
```
┌─────────────────────────────────────────┐
│  [1]     [2]     [3]     [4]     [5]    │
│ ┌───┐   ┌───┐   ┌───┐   ┌───┐   ┌───┐  │
│ │ ⛏ │   │ ═ │   │ ↑ │   │   │   │   │  │
│ └───┘   └───┘   └───┘   └───┘   └───┘  │
│ Shovel   Belt    Lift                   │
│   ▲                                     │
│ selected                                │
└─────────────────────────────────────────┘
```

### Inventory Menu
```
┌────────────────────────────────────────────┐
│              INVENTORY                [X]  │
├────────────────────────────────────────────┤
│                                            │
│   TOOLS                                    │
│   ┌───┐                                    │
│   │ ⛏ │  Shovel                            │
│   └───┘                                    │
│                                            │
│   STRUCTURES                               │
│   ┌───┐   ┌───┐                            │
│   │ ═ │   │ ↑ │                            │
│   └───┘   └───┘                            │
│   Belt    Lift                             │
│           (locked)                         │
│                                            │
│   ─────────────────────────────────────    │
│   Shovel                                   │
│   Dig terrain and carry loose dirt         │
│                                            │
└────────────────────────────────────────────┘
```

## Input Summary
| Key | Action |
|-----|--------|
| 1-5 | Select hotbar slot |
| Tab / I | Toggle inventory menu |
| Escape | Close inventory menu |
| Left Click | Select item in menu |

## Integration with Existing Systems
- `ProgressionManager` - Check `HasAbility()` for unlock status
- `PlayerController` - Remove tool inventory (moved to PlayerEquipment)
- `StructurePlacementController` - Read from PlayerEquipment

## Testing Checklist
- [ ] Hotbar displays at bottom of screen
- [ ] Number keys select hotbar slots
- [ ] Locked items appear greyed out
- [ ] Tab opens inventory menu
- [ ] Game pauses while menu is open
- [ ] Clicking item equips it
- [ ] Escape closes menu
- [ ] Equipping belt enables belt placement mode
- [ ] Equipping lift enables lift placement mode
- [ ] Equipping shovel enables digging mode
