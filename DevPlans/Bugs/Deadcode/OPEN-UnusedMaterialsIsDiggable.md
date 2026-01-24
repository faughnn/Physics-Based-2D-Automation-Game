# Dead Code: Unused Materials.IsDiggable Method

## Summary
A helper method to check if materials are diggable is never called.

## Location
- **File:** `Assets/Scripts/Simulation/MaterialDef.cs`
- **Line:** 85-88

## Unused Method
```csharp
public static bool IsDiggable(MaterialDef mat)
{
    return (mat.materialFlags & MaterialFlags.Diggable) != 0;
}
```

## Notes
- The `Diggable` flag IS assigned to materials (Ground, Dirt, Stone, etc.)
- But this helper method to check the flag is never called
- The digging gameplay feature hasn't been implemented yet

## Recommended Action
Keep if digging system is planned, remove otherwise.
