# Bug: SandboxController Uses Boolean Flags Instead of Enum

## Status: OPEN

## Category: Architecture / Code Smell

## Summary

`SandboxController` uses mutually exclusive boolean fields for placement modes instead of a single enum. The manual mutual-exclusion toggle logic is fragile and error-prone.

## Current State

In `SandboxController.cs` lines 30-45:
```csharp
private bool beltMode;
private bool liftMode;
private bool wallMode;
private bool pistonMode;
```

The toggle logic at lines 247-272 sets one boolean true and all others false:
```csharp
if (Input.GetKeyDown(KeyCode.B))
{
    beltMode = !beltMode;
    liftMode = false;
    wallMode = false;
    pistonMode = false;
}
```

This pattern is repeated for each mode key. Adding a new mode means:
1. Adding a new boolean field
2. Adding a new toggle block
3. Adding `newMode = false;` to every OTHER toggle block

This is O(n^2) code growth per new mode.

## Expected Pattern

A `PlacementMode` enum already exists in the Game layer (`StructurePlacementController`). The sandbox should use the same approach:

```csharp
enum SandboxMode { None, Paint, Belt, Lift, Wall, Piston }
private SandboxMode currentMode;
```

Toggle becomes:
```csharp
currentMode = (currentMode == SandboxMode.Belt) ? SandboxMode.None : SandboxMode.Belt;
```

## Affected Files

- `Assets/Scripts/SandboxController.cs`

## Priority

Low — functional but fragile; will become worse as new structure/tool types are added.
