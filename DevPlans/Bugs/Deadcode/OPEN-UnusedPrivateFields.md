# Dead Code: Unused Private Fields

## Summary
Two private fields are declared but never used.

## Locations

### SettingsMenu.cs - toggleContainer (Line 21)
```csharp
[SerializeField] private RectTransform toggleContainer;
```
- Declared with `[SerializeField]` but never assigned or read
- UI is built programmatically in `CreateUI()` without using this field
- Leftover from an earlier design

### SandboxController.cs - worldShader (Line 19)
```csharp
[SerializeField] private Shader worldShader;
```
- Declared but never read
- `CellRenderer` handles its own shader loading via `Shader.Find()`
- Was likely intended to pass shader to renderer but approach changed

## Recommended Action
Remove both fields as they serve no purpose.
