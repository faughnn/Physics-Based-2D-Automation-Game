# Bug: Unlock Requirements Buried in Hotbar Switch Statement

## Status: OPEN

## Category: Architecture / Data-Driven Design

## Summary

Structure unlock requirements are defined in a switch statement inside `Hotbar.GetRequiredAbility()` rather than on the structure/item definitions themselves. This makes it non-obvious which structures share unlock tiers (e.g., Wall intentionally unlocks with Lifts via `Ability.PlaceLifts`).

## Current State

```csharp
// Hotbar.cs - unlock mapping buried in UI code
case StructureType.Wall:
    return Ability.PlaceLifts; // Intentional: walls and lifts unlock together
```

The mapping is correct but not self-documenting. Adding new structures means adding to this switch, and the grouping logic isn't visible from the structure definitions.

## Refactor Direction

Make unlock requirements part of each structure's data definition (in `ItemRegistry` or `ItemDefinition`) rather than a switch in Hotbar. This way:
- Looking at a structure's definition shows its unlock requirement
- No switch statement to maintain
- Unlock tier groupings are obvious from the data

This ties into the broader structure system data-driven refactor (`OPEN-StructureManagerDuplication.md`).

## Affected Files

- `Assets/Scripts/Game/UI/Hotbar.cs`
- `Assets/Scripts/Game/ItemRegistry.cs`

## Priority

Low — fold into structure system refactor.
