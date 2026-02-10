# Material System Extensions

## Goal

Address the small extensibility gaps in the material system so that adding new materials is a clean, single-file operation with no gotchas.

## Current State

The material system is already well-structured. Adding a new powder or liquid requires only adding an ID and a `MaterialDef` entry in `MaterialDef.cs`. The simulation dispatches on `BehaviourType`, the renderer builds palettes from the def array, and gameplay systems (collection zones, grab, belts) operate on behaviour type.

However, a few spots require manual updates that could be forgotten:

- `IsSoftTerrain()` is a hard-coded whitelist of 4 material IDs rather than a `MaterialFlags` flag
- `ProgressionUI.GetMaterialName()` is a switch statement mapping material IDs to display strings
- `IsBelt()` and `IsLift()` are hard-coded ID range checks rather than flag-driven
- `LevelLoader.cs` has hard-coded checks for Stone and Ground when marking terrain for collider generation
- No per-material display name exists in `MaterialDef` itself — UI has to maintain its own mapping

## Work Required

- Add a `SoftTerrain` flag to `MaterialFlags` and replace `IsSoftTerrain()` with a flag check
- Add a `displayName` field to `MaterialDef` so UI can read it directly instead of maintaining a separate switch
- Consider replacing `IsBelt()` / `IsLift()` with a `structureType` field on `MaterialDef` or a flag, so structure material identification is data-driven
- Make collider generation check a flag (e.g., `GeneratesCollider`) rather than checking specific material IDs
- Audit for any other hard-coded material ID references that should be flag-driven

## Scope

This is a small, focused cleanup — not a rewrite. The core architecture is sound. The goal is to eliminate the handful of places where adding a material requires remembering to update a whitelist somewhere.
