# Reduce Dirt Requirements

## Problem
The current dirt requirements for the first two levels are tedious:
- Level 1: 500 dirt (manual transport) - too much clicking/carrying
- Level 2: 2000 dirt (belt transport) - takes too long even with belts

## Current Values
```csharp
// In TutorialLevelData.cs
Level 1: 500 dirt   -> Unlock Belts
Level 2: 2000 dirt  -> Unlock Lifts
Level 3: 5000 dirt  -> Victory
```

## Proposed Values
```csharp
Level 1: 100 dirt   -> Unlock Belts  (was 500, reduced 80%)
Level 2: 500 dirt   -> Unlock Lifts  (was 2000, reduced 75%)
Level 3: 2000 dirt  -> Victory       (was 5000, reduced 60%)
```

## Rationale
- Level 1 should be a quick tutorial - just enough to understand the grab/drop mechanic
- Level 2 introduces belts - should feel faster than manual, not more tedious
- Level 3 can be longer since player has full toolset (belts + lifts)

## Implementation

### File to Modify
`Assets/Scripts/Game/Levels/TutorialLevelData.cs`

### Changes
Update the `Objectives` list in the `Create()` method:

```csharp
Objectives = new List<ObjectiveData>
{
    new ObjectiveData(
        targetMaterial: Materials.Dirt,
        requiredCount: 100,  // Changed from 500
        rewardAbility: Ability.PlaceBelts,
        displayName: "Fill the bucket with dirt",
        objectiveId: "level1",
        prerequisiteId: ""
    ),
    new ObjectiveData(
        targetMaterial: Materials.Dirt,
        requiredCount: 500,  // Changed from 2000
        rewardAbility: Ability.PlaceLifts,
        displayName: "Use belts to fill the bucket",
        objectiveId: "level2",
        prerequisiteId: "level1"
    ),
    new ObjectiveData(
        targetMaterial: Materials.Dirt,
        requiredCount: 2000,  // Changed from 5000
        rewardAbility: Ability.None,
        displayName: "Use lifts and belts to fill the bucket",
        objectiveId: "level3",
        prerequisiteId: "level2"
    )
}
```

## Testing
- Level 1 should complete in under 1 minute of manual carrying
- Level 2 should complete in 1-2 minutes with a simple belt setup
- Level 3 should be the main challenge, 3-5 minutes with belt+lift system
