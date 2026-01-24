# Dead Code: Unused StructureType Enum Values

## Summary
Three enum values in StructureType are explicitly marked as "Future" and never used.

## Location
- **File:** `Assets/Scripts/Structures/StructureType.cs`
- **Lines:** 11-13

## Unused Values
```csharp
Lift = 2,      // Future
Furnace = 3,   // Future
Press = 4,     // Future
```

## Used Values
- `None = 0` - Default/no structure
- `Belt = 1` - Conveyor belts (fully implemented)

## Recommended Action
Remove the unused enum values or keep with clear documentation that they are placeholders for future features.
