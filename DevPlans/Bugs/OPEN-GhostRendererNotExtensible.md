# Bug: GhostStructureRenderer Not Extensible

## Status: OPEN

## Category: Architecture / Refactor

## Summary

`GhostStructureRenderer` takes three managers individually and has three identical positioning loops differing only by color. Adding a new ghostable structure type requires modifying the `Initialize()` signature and adding another copy-pasted loop.

## Duplicated Code

In `GhostStructureRenderer.cs` lines 91-121, three loops are structurally identical:

```csharp
// Belt ghosts
for (int i = 0; i < beltPositions.Count; i++) { /* position sprite, set color */ }
// Lift ghosts
for (int i = 0; i < liftPositions.Count; i++) { /* position sprite, set color */ }
// Wall ghosts
for (int i = 0; i < wallPositions.Count; i++) { /* position sprite, set color */ }
```

Only the positions list and color constant differ.

## Current Wiring

```csharp
public void Initialize(BeltManager beltMgr, LiftManager liftMgr, WallManager wallMgr)
```

Each new structure type requires:
1. Adding a parameter to `Initialize()`
2. Adding a field to store the manager
3. Adding a positions list
4. Adding a color constant
5. Adding another positioning loop
6. Modifying the call site in `SimulationManager`

## Refactor Direction

- Define an `IGhostProvider` interface:
  ```csharp
  interface IGhostProvider
  {
      void GetGhostBlockPositions(List<Vector2Int> positions);
      Color GhostColor { get; }
  }
  ```
- `GhostStructureRenderer.Initialize(List<IGhostProvider> providers)`
- Single loop iterates all providers

## Affected Files

- `Assets/Scripts/Rendering/GhostStructureRenderer.cs`
- `Assets/Scripts/Simulation/SimulationManager.cs` — call site

## Priority

Low-Medium — simple refactor, enables extensibility for future structure types.
