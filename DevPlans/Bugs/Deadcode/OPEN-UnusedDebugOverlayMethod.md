# Dead Code: Unused DebugOverlay.UnregisterSection

## Summary
The method to unregister debug sections is never called.

## Location
- **File:** `Assets/Scripts/Debug/DebugOverlay.cs`
- **Line:** 65-68

## Unused Method
```csharp
public void UnregisterSection(IDebugSection section)
{
    sections.Remove(section);
}
```

## Reason
- Debug sections are registered at startup
- No code ever removes a debug section
- Sections persist for the entire application lifetime

## Recommended Action
Remove the method since debug sections are never dynamically unregistered.
