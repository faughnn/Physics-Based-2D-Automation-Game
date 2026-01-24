# Dead Code: Unused GraphicsManager Members

## Summary
Several public methods and properties in GraphicsManager are never accessed.

## Location
- **File:** `Assets/Scripts/Graphics/GraphicsManager.cs`

## Unused Methods
| Method | Line | Purpose |
|--------|------|---------|
| `SaveAllSettings()` | 106 | Save all effect settings at once |
| `GetEffect<T>()` | 123 | Get a specific effect by type |

## Unused Properties
| Property | Line | Purpose |
|----------|------|---------|
| `PostProcessVolume` | 23 | Access to post-process volume |
| `BloomComponent` | 24 | Access to bloom component |

## Notes
- Individual effects save their own settings when toggled via `GraphicsEffectBase.SaveSettings()`
- Effects are accessed via the settings menu toggle system, not via `GetEffect<T>()`
- The properties expose internals that nothing external needs

## Also in GraphicsEffectBase.cs
| Method | Line | Purpose |
|--------|------|---------|
| `SetGlobalInt(string propertyName, int value)` | 71 | Set shader int property |

- All derived effects use `SetGlobalFloat()` instead

## Recommended Action
Remove unused members or make them private/internal if only needed internally.
