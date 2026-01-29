# Bug: Unlock Notification Text Shows Stale Hotkeys

## Summary
When the player unlocks belts or lifts, the on-screen notification tells them to press B or L to use the item. These hotkeys are no longer used — items are now equipped via the inventory menu (Tab/I) and hotbar (keys 1-5).

## Symptoms
- Unlocking belts shows: **"BELTS UNLOCKED! Press B to place belts."**
- Unlocking lifts shows: **"LIFTS UNLOCKED! Press L to place lifts."**
- Pressing B or L does nothing; the player must open the inventory (Tab/I) or select the hotbar slot (1-5) to equip the item

## Root Cause
`ProgressionUI.GetUnlockMessage()` was written before the inventory/hotbar system replaced direct hotkey placement. The messages were never updated to reflect the new input scheme.

## Affected Code
- `Assets/Scripts/Game/UI/ProgressionUI.cs:121-130` — `GetUnlockMessage()` method

```csharp
private string GetUnlockMessage(Ability ability)
{
    return ability switch
    {
        Ability.PlaceBelts => "BELTS UNLOCKED! Press B to place belts.",
        Ability.PlaceLifts => "LIFTS UNLOCKED! Press L to place lifts.",
        Ability.PlaceFurnace => "FURNACE UNLOCKED!",
        _ => $"{ability} UNLOCKED!"
    };
}
```

## Potential Solutions

### 1. Update Messages to Reference Inventory/Hotbar
Change the messages to point players to the correct input:
- `"BELTS UNLOCKED! Equip from inventory (Tab) or hotbar."`
- `"LIFTS UNLOCKED! Equip from inventory (Tab) or hotbar."`

### 2. Generic Unlock Message
Use a single format that won't go stale if inputs change again:
- `"BELTS UNLOCKED! Added to your inventory."`
- `"LIFTS UNLOCKED! Added to your inventory."`

## Priority
Low

## Related Files
- `Assets/Scripts/Game/UI/ProgressionUI.cs`
- `Assets/Scripts/Game/UI/InventoryMenu.cs`
- `Assets/Scripts/Game/UI/Hotbar.cs`
