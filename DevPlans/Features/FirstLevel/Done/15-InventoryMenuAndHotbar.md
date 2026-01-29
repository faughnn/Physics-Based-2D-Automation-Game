# Inventory Menu and Hotbar

## Overview
Add a UI system for selecting tools and placeable structures. Includes:
- **Hotbar** - Always visible, quick access via number keys (1-5) or scroll wheel
- **Inventory Menu** - Full menu opened with Tab/I, shows all unlocked items with drag-to-hotbar assignment

## Existing Systems (DO NOT duplicate)

The codebase already has tool inventory and equip logic in `PlayerController`:
- `HashSet<ToolType> inventory` — tracks collected tools
- `EquipTool(ToolType)` — equips a tool, fires `OnToolEquipped`
- `HasTool(ToolType)` — checks if player has collected a tool
- `CollectTool(ToolType)` — called when player walks over a `WorldItem`
- `OnToolEquipped` / `OnToolCollected` events

**`ToolType` enum** exists at `Assets/Scripts/Game/Items/ToolType.cs`:
```csharp
public enum ToolType
{
    None = 0,
    Shovel = 1,
}
```

**`StructureType` enum** exists at `Assets/Scripts/Structures/StructureType.cs`:
```csharp
public enum StructureType : byte
{
    None = 0, Belt = 1, Lift = 2, Furnace = 3, Press = 4, Wall = 5,
}
```

**`WorldItem`** exists at `Assets/Scripts/Game/Items/WorldItem.cs` — pickup trigger in the world.

**Tool gating** — `CellGrabSystem` (line 71) and `DiggingController` (line 61) both poll `player.EquippedTool` and early-return if not the expected tool.

**Structure placement** — `StructurePlacementController` uses its own B/L/W keybinds and `PlacementMode` enum, with `ProgressionManager.IsUnlocked()` gating. Currently independent of the tool system.

## Requirements

### Hotbar
- Fixed bar at bottom of screen (5 slots)
- Shows currently assigned tools/structures
- Number keys 1-5 to select slot
- Scroll wheel to cycle through slots
- Visual highlight on selected slot
- Greyed out slots for locked/uncollected items
- Items unlock as abilities are earned (belts, lifts) or items are collected (shovel)
- **Default selection**: Grabber tool (slot 1) on game start

### Inventory Menu
- Opens with Tab or I key
- **Does NOT pause the game** — world continues running while menu is open
- Grid of all items (tools + structures), categorized
- Drag items onto hotbar slots to assign them
- Shows item name and description on hover
- Close with Tab/I/Escape
- Locked items shown but not selectable/draggable
- Closing menu preserves the previously selected hotbar item

### Input Conflict Notes
- Keys 1-5 are used by `SandboxController` for material selection — **no conflict** in Game scene
- Keys 1-2 are mapped to "Previous"/"Next" in InputSystem_Actions — **dead bindings**, not referenced by any code. Safe to ignore.
- Tab and I are **free**, no existing bindings
- Escape is used by `SettingsMenu` (toggle) and `StructurePlacementController` (cancel placement). **Priority**: if inventory is open, Escape closes inventory; otherwise falls through to existing handlers.
- B/L/W keys currently used for structure placement — **replaced by hotbar selection**. Remove these keybinds from `StructurePlacementController`.

## Slot Layout

### Default Hotbar Slots (1-5)
1. **Grabber** - Default tool, grab and carry materials (always available)
2. **Shovel** - Dig terrain (available after pickup — player walks over shovel in world)
3. **Belt** - Place conveyor belts (unlocked after Level 1)
4. **Lift** - Place vertical lifts (unlocked after Level 2)
5. **Empty** - Future expansion

Players can reassign slots by dragging items from the inventory menu onto hotbar slots.

## Data Model

### ItemDefinition
```csharp
public enum ItemCategory
{
    Tool,       // Grabber, Shovel, etc.
    Structure   // Belt, Lift, etc.
}

public class ItemDefinition
{
    public string Id;
    public string DisplayName;
    public string Description;
    public ItemCategory Category;
    public Sprite Icon;
    public ToolType ToolType;              // For tools — uses existing enum
    public StructureType StructureType;    // For structures — uses existing enum
}
```

**Availability is determined by existing systems, not by ItemDefinition fields:**
- **Tools**: `PlayerController.HasTool(item.ToolType)` — true if collected via WorldItem pickup
- **Structures**: `ProgressionManager.IsUnlocked(item.RequiredAbility)` — true if ability unlocked
- **Grabber**: Always available (special case — `ToolType.Grabber` with no pickup required)

> **Note:** `RequiresPickup` and `RequiredAbility` fields removed from `ItemDefinition`. Availability logic lives in the existing systems (`PlayerController` for tools, `ProgressionManager` for structures). The Hotbar queries those systems rather than duplicating the logic in item data.

### Availability Check (in Hotbar)
```csharp
private bool IsItemAvailable(ItemDefinition item)
{
    if (item.Category == ItemCategory.Tool)
    {
        if (item.ToolType == ToolType.Grabber) return true; // Always available
        return playerController.HasTool(item.ToolType);
    }
    else // Structure
    {
        return progressionManager.IsUnlocked(GetRequiredAbility(item.StructureType));
    }
}
```

## Equip Flow — Unified System

Currently tools and structures have separate equip paths. This plan unifies them through `PlayerController`.

### Changes to PlayerController

Extend `PlayerController` to handle both tools and structures:

```csharp
// Existing fields (keep):
private HashSet<ToolType> inventory;
private ToolType equippedTool;
public event Action<ToolType> OnToolEquipped;

// New fields:
private StructureType equippedStructure = StructureType.None;
public event Action<StructureType> OnStructureEquipped;

// New property:
public StructureType EquippedStructure => equippedStructure;

// New method:
public void EquipStructure(StructureType structure)
{
    equippedTool = ToolType.None;          // Unequip tool
    equippedStructure = structure;
    OnToolEquipped?.Invoke(ToolType.None); // Notify tool listeners
    OnStructureEquipped?.Invoke(structure);
}

// Modified EquipTool:
public bool EquipTool(ToolType tool)
{
    if (tool != ToolType.None && !inventory.Contains(tool) && tool != ToolType.Grabber)
        return false;

    equippedTool = tool;
    equippedStructure = StructureType.None; // Unequip structure
    OnToolEquipped?.Invoke(tool);
    OnStructureEquipped?.Invoke(StructureType.None); // Notify structure listeners
    return true;
}
```

**Key rule:** Equipping a tool unequips any structure, and vice versa. Only one thing active at a time.

### Changes to StructurePlacementController

- **Remove** B/L/W keybinds from `HandleModeSelection()`
- **Listen** to `PlayerController.OnStructureEquipped` to enter/exit placement mode:
```csharp
private void OnStructureEquipped(StructureType type)
{
    switch (type)
    {
        case StructureType.Belt: SetMode(PlacementMode.Belt); break;
        case StructureType.Lift: SetMode(PlacementMode.Lift); break;
        case StructureType.Wall: SetMode(PlacementMode.Wall); break;
        default: SetMode(PlacementMode.None); break;
    }
}
```
- Keep Q/E for rotation, Escape for cancel (Escape also tells Hotbar to reselect previous tool)
- Keep F8 debug unlock

### Tool Activation — No Changes Needed

`CellGrabSystem` and `DiggingController` already poll `player.EquippedTool` each frame:
- `CellGrabSystem` checks `EquippedTool != ToolType.Shovel` → early return (line 71)
- `DiggingController` checks `EquippedTool != ToolType.Shovel` → return false (line 61)

These will need updating to also check for Grabber:
- `CellGrabSystem`: active when `EquippedTool == ToolType.Grabber` (grab only, no dig)
- `DiggingController`: active when `EquippedTool == ToolType.Shovel` (dig + grab)

Both systems naturally deactivate when a structure is equipped (since `EquipStructure` sets `equippedTool = None`).

## Changes to ToolType Enum

Add `Grabber` to `Assets/Scripts/Game/Items/ToolType.cs`:
```csharp
public enum ToolType
{
    None = 0,
    Shovel = 1,
    Grabber = 2,
}
```

## Implementation

### Files to Create

#### 1. `Assets/Scripts/Game/UI/ItemDefinition.cs`
Data class for item definitions. Contains `ItemCategory` enum and `ItemDefinition` class.

#### 2. `Assets/Scripts/Game/UI/ItemRegistry.cs`
Static registry of all items:
```csharp
public static class ItemRegistry
{
    public static readonly ItemDefinition Grabber = new ItemDefinition
    {
        Id = "grabber",
        DisplayName = "Grabber",
        Description = "Grab and carry loose materials",
        Category = ItemCategory.Tool,
        ToolType = ToolType.Grabber
    };

    public static readonly ItemDefinition Shovel = new ItemDefinition
    {
        Id = "shovel",
        DisplayName = "Shovel",
        Description = "Dig terrain and carry loose dirt",
        Category = ItemCategory.Tool,
        ToolType = ToolType.Shovel
    };

    public static readonly ItemDefinition Belt = new ItemDefinition
    {
        Id = "belt",
        DisplayName = "Conveyor Belt",
        Description = "Automatically moves materials horizontally",
        Category = ItemCategory.Structure,
        StructureType = StructureType.Belt
    };

    public static readonly ItemDefinition Lift = new ItemDefinition
    {
        Id = "lift",
        DisplayName = "Lift",
        Description = "Moves materials vertically",
        Category = ItemCategory.Structure,
        StructureType = StructureType.Lift
    };

    public static readonly List<ItemDefinition> All = new List<ItemDefinition>
        { Grabber, Shovel, Belt, Lift };

    public static IEnumerable<ItemDefinition> Tools =>
        All.Where(i => i.Category == ItemCategory.Tool);

    public static IEnumerable<ItemDefinition> Structures =>
        All.Where(i => i.Category == ItemCategory.Structure);
}
```

#### 3. `Assets/Scripts/Game/UI/Hotbar.cs`
MonoBehaviour for hotbar display and input:
```csharp
public class Hotbar : MonoBehaviour
{
    private const int SlotCount = 5;
    private const int SlotSize = 60;
    private const int SlotPadding = 8;

    private ItemDefinition[] slots = new ItemDefinition[SlotCount];
    private Rect[] slotRects = new Rect[SlotCount]; // Cached for drag-drop hit testing
    private int selectedIndex = 0;
    private PlayerController playerController;
    private ProgressionManager progressionManager;

    private void Start()
    {
        // Assign default layout
        slots[0] = ItemRegistry.Grabber;
        slots[1] = ItemRegistry.Shovel;
        slots[2] = ItemRegistry.Belt;
        slots[3] = ItemRegistry.Lift;
        // slots[4] = empty
        selectedIndex = 0;
        EquipSlot(0); // Grabber equipped by default
    }

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

        // Scroll wheel cycling
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (scroll > 0) CycleSlot(-1);
        else if (scroll < 0) CycleSlot(1);
    }

    private void CycleSlot(int direction)
    {
        // Skip empty/unavailable slots when cycling
        int next = selectedIndex;
        for (int i = 0; i < SlotCount; i++)
        {
            next = (next + direction + SlotCount) % SlotCount;
            if (slots[next] != null && IsItemAvailable(slots[next]))
            {
                SelectSlot(next);
                return;
            }
        }
    }

    private void SelectSlot(int index)
    {
        if (slots[index] == null) return;
        if (!IsItemAvailable(slots[index])) return;
        selectedIndex = index;
        EquipSlot(index);
    }

    private void EquipSlot(int index)
    {
        var item = slots[index];
        if (item.Category == ItemCategory.Tool)
            playerController.EquipTool(item.ToolType);
        else
            playerController.EquipStructure(item.StructureType);
    }

    private bool IsItemAvailable(ItemDefinition item)
    {
        if (item.Category == ItemCategory.Tool)
        {
            if (item.ToolType == ToolType.Grabber) return true;
            return playerController.HasTool(item.ToolType);
        }
        else
        {
            return progressionManager.IsUnlocked(GetRequiredAbility(item.StructureType));
        }
    }

    // Expose slot rects for InventoryMenu drag-drop hit testing
    public Rect GetSlotRect(int index) => slotRects[index];
    public int SlotCountPublic => SlotCount;

    public void AssignSlot(int index, ItemDefinition item)
    {
        slots[index] = item;
    }

    private void OnGUI()
    {
        // Calculate slot rects (bottom center of screen)
        float totalWidth = SlotCount * SlotSize + (SlotCount - 1) * SlotPadding;
        float startX = (Screen.width - totalWidth) / 2;
        float startY = Screen.height - SlotSize - 20;

        for (int i = 0; i < SlotCount; i++)
        {
            slotRects[i] = new Rect(
                startX + i * (SlotSize + SlotPadding),
                startY,
                SlotSize, SlotSize);

            // Draw slot background (highlighted if selected, greyed if locked)
            // Draw icon
            // Draw number label (1-5)
            // Draw name below
        }
    }
}
```

#### 4. `Assets/Scripts/Game/UI/InventoryMenu.cs`
Full inventory screen with drag-to-hotbar:
```csharp
public class InventoryMenu : MonoBehaviour
{
    private bool isOpen = false;
    private ItemDefinition draggingItem = null;
    private Hotbar hotbar;

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
        // No time scale change — game keeps running
    }

    private void CloseMenu()
    {
        isOpen = false;
        draggingItem = null;
    }

    private void OnGUI()
    {
        if (!isOpen) return;

        // Draw semi-transparent background overlay
        // Draw categorized grid: TOOLS section, STRUCTURES section
        // Each item: icon + name, greyed if locked/uncollected
        // Hover: show tooltip (name + description) at bottom of menu

        // Drag handling:
        // - Mouse down on available item → set draggingItem
        // - While dragging → draw item icon at cursor position
        // - Mouse up → check hotbar.GetSlotRect(i) for hit
        //   If hit: hotbar.AssignSlot(i, draggingItem)
        //   Always: clear draggingItem
    }
}
```

### Drag-to-Hotbar Interaction

The `InventoryMenu` needs to know where hotbar slots are on screen for drop detection. This is handled by `Hotbar` exposing slot screen rects:

1. **Hotbar** caches `Rect[] slotRects` during `OnGUI()` (computed from layout)
2. **Hotbar** exposes `GetSlotRect(int index)` — returns the screen-space Rect for a slot
3. **InventoryMenu** holds a reference to `Hotbar` (injected by GameController)
4. On mouse-up while dragging, `InventoryMenu` iterates `hotbar.GetSlotRect(i)` and checks `rect.Contains(mousePos)`
5. If a slot is hit, calls `hotbar.AssignSlot(i, draggingItem)`

This keeps layout ownership in `Hotbar` — `InventoryMenu` only reads rects, never computes them.

### Files to Modify

#### `Assets/Scripts/Game/Items/ToolType.cs`
Add `Grabber`:
```csharp
public enum ToolType
{
    None = 0,
    Shovel = 1,
    Grabber = 2,
}
```

#### `Assets/Scripts/Game/PlayerController.cs`
- Add `equippedStructure` field, `EquippedStructure` property, `OnStructureEquipped` event
- Add `EquipStructure(StructureType)` method (unequips tool)
- Modify `EquipTool()` to also clear `equippedStructure`
- Allow `Grabber` to be equipped without being in `inventory` (always available)

#### `Assets/Scripts/Game/CellGrabSystem.cs`
- Change tool check (line 71) from `!= ToolType.Shovel` to: active when `EquippedTool == ToolType.Grabber || EquippedTool == ToolType.Shovel`

#### `Assets/Scripts/Game/Digging/DiggingController.cs`
- Keep existing check (line 61): only active when `EquippedTool == ToolType.Shovel`

#### `Assets/Scripts/Game/Structures/StructurePlacementController.cs`
- **Remove** B/L/W keybind handling from `HandleModeSelection()`
- **Add** listener for `PlayerController.OnStructureEquipped` to enter/exit placement mode
- Keep Q/E rotation, Escape cancel, F8 debug unlock

#### `Assets/Scripts/Game/GameController.cs`
- Create Hotbar and InventoryMenu GameObjects
- Wire references: `Hotbar` gets `PlayerController` + `ProgressionManager`, `InventoryMenu` gets `Hotbar`
- **Remove** `PlayerEquipment` creation (not needed — `PlayerController` handles this)

### Files NOT Created (removed from plan)

- ~~`PlayerEquipment.cs`~~ — `PlayerController` already has this functionality. Extended in place rather than duplicated.

## Visual Design

### Hotbar
```
┌─────────────────────────────────────────────────┐
│  [1]     [2]     [3]     [4]     [5]            │
│ ┌───┐   ┌───┐   ┌───┐   ┌───┐   ┌───┐          │
│ │ ✋ │   │ ⛏ │   │ ═ │   │ ↑ │   │   │          │
│ └───┘   └───┘   └───┘   └───┘   └───┘          │
│ Grab    Shovel   Belt    Lift                   │
│   ▲                                             │
│ selected                                        │
└─────────────────────────────────────────────────┘
```

### Inventory Menu
```
┌────────────────────────────────────────────┐
│              INVENTORY                [X]  │
├────────────────────────────────────────────┤
│                                            │
│   TOOLS                                   │
│   ┌───┐   ┌───┐                            │
│   │ ✋ │   │ ⛏ │                            │
│   └───┘   └───┘                            │
│   Grab    Shovel                           │
│           (not collected)                  │
│                                            │
│   STRUCTURES                               │
│   ┌───┐   ┌───┐                            │
│   │ ═ │   │ ↑ │                            │
│   └───┘   └───┘                            │
│   Belt    Lift                             │
│           (locked)                         │
│                                            │
│   ─────────────────────────────────────    │
│   Grabber                                  │
│   Grab and carry loose materials           │
│                                            │
│   Drag items onto hotbar to assign slots   │
│                                            │
└────────────────────────────────────────────┘
```

## Input Summary
| Key | Action |
|-----|--------|
| 1-5 | Select hotbar slot |
| Scroll Wheel | Cycle hotbar slots (skips empty/unavailable) |
| Tab / I | Toggle inventory menu |
| Escape | Close inventory menu (priority over settings/cancel) |
| Left Click + Drag | Drag item from inventory onto hotbar slot |
| Q / E | Rotate structure direction (unchanged, only in placement mode) |

## Integration with Existing Systems
- **`PlayerController`** — Extended with `EquipStructure()` and `OnStructureEquipped`. Remains the single source of truth for tool inventory and equipped state. No new singleton needed.
- **`ProgressionManager`** — Queried by Hotbar for structure unlock status via `IsUnlocked()`. No changes needed.
- **`StructurePlacementController`** — Removes B/L/W keybinds. Listens to `OnStructureEquipped` to enter/exit placement mode.
- **`CellGrabSystem`** — Updated tool check to include Grabber.
- **`DiggingController`** — No changes (already checks for Shovel only).
- **`WorldItem` / pickup system** — No changes. Shovel pickup already works via `OnTriggerEnter2D`.
- **`ToolType` enum** — Add `Grabber` value.
- **`StructureType` enum** — Reuse existing. No changes.

## Rendering Approach
Using **Unity UI (Canvas)** built entirely in code — no prefabs or scene objects. This gives resolution-independent scaling via `CanvasScaler`, native drag-and-drop support via `IDragHandler`/`IDropHandler`, and proper UI styling with `Image`/`Text` components. A `GameUIBuilder` helper creates the Canvas, CanvasScaler, GraphicRaycaster, and EventSystem once in `GameController`.

## Testing Checklist
- [ ] Hotbar displays at bottom of screen
- [ ] Number keys select hotbar slots
- [ ] Scroll wheel cycles hotbar slots (skips empty/unavailable)
- [ ] Grabber is selected by default on game start
- [ ] Locked items appear greyed out
- [ ] Uncollected items (shovel before pickup) appear greyed out
- [ ] Tab opens inventory menu
- [ ] Game does NOT pause while menu is open
- [ ] Dragging item onto hotbar assigns it to that slot
- [ ] Escape closes menu (takes priority over other Escape handlers)
- [ ] Closing menu preserves previous hotbar selection
- [ ] Equipping belt enters belt placement mode (no B key needed)
- [ ] Equipping lift enters lift placement mode (no L key needed)
- [ ] Equipping shovel enables digging mode
- [ ] Equipping grabber enables grab mode (no dig)
- [ ] Equipping a tool exits structure placement mode
- [ ] Equipping a structure exits tool mode
- [ ] Collecting shovel via WorldItem pickup makes it available in hotbar
- [ ] B/L/W keys no longer toggle structure placement directly
