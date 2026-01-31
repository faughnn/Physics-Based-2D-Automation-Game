# Progression System Extensions

## Goal

Prepare the progression system to support objective types beyond "collect N of material X" when the need arises.

## Current State

The progression system is well-architected with clean event-driven design:
- `ProgressionManager` fires events: OnMaterialCollected, OnAbilityUnlocked, OnObjectiveCompleted, OnObjectiveActivated
- `ObjectiveData` defines target material, required count, reward ability, display name, prerequisite chaining
- Buckets subscribe to events and self-configure from ObjectiveData
- ProgressionUI subscribes to events and updates generically

This works well for the current game loop. The limitations are:

- `ObjectiveData` only models one objective type: material collection with a count threshold
- `ProgressionManager.RecordCollection()` is the only way to advance objectives
- `ProgressionUI.GetMaterialName()` is a hardcoded switch (should use MaterialDef display names once those exist)
- `ProgressionUI.GetUnlockMessage()` is a hardcoded switch on Ability
- `Ability` enum has no metadata — display names are maintained separately in UI code
- Buckets are the only physical objective mechanism

## Work Required (When Needed)

### Objective Type Abstraction
- Add an objective type discriminator to `ObjectiveData` (e.g., `CollectMaterial`, `BuildStructures`, `ReachLocation`)
- Generalize `ProgressionManager` to advance objectives through different trigger methods, not just `RecordCollection`
- Keep the event-driven architecture — it's the right pattern

### Ability Metadata
- Add a `displayName` field to the Ability system (either on a data struct or a static registry) so ProgressionUI doesn't need a switch statement for unlock messages

### UI Display Names
- Once MaterialDef has a `displayName` field (from MaterialSystemExtensions plan), remove `GetMaterialName()` switch from ProgressionUI

## Scope

This is low priority. The current system handles the tutorial level's needs. Extend it when you have a concrete second objective type to implement — not before. The event-driven architecture means the extension points are clean when you need them.
