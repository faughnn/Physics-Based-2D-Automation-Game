# Dead Code: Unused PlayerController Public API

## Summary
Several public properties, methods, and events in PlayerController are never accessed externally.

## Location
- **File:** `Assets/Scripts/Game/PlayerController.cs`

## Unused Members

### Events (Never Subscribed To)
| Event | Line | Purpose |
|-------|------|---------|
| `OnToolEquipped` | 36 | Fired when tool is equipped - no subscribers |
| `OnToolCollected` | 37 | Fired when tool is collected - no subscribers |

### Properties (Never Read Externally)
| Property | Line | Purpose |
|----------|------|---------|
| `IsGrounded` | 124 | Check if player is on ground |
| `EquippedTool` | 129 | Get currently equipped tool |
| `Inventory` | 134 | Get player's tool inventory |

### Methods (Never Called)
| Method | Line | Purpose |
|--------|------|---------|
| `HasTool(ToolType tool)` | 139 | Check if player has a tool |
| `EquipTool(ToolType tool)` | 147 | Equip a specific tool |

## Notes
- Events are invoked but have no subscribers (write-only)
- These appear to be API methods for future game systems
- Tool equipping happens internally via `CollectTool()`

## Recommended Action
- Remove the events if no system will subscribe to them
- Keep public API if game systems will use them, otherwise make private or remove
