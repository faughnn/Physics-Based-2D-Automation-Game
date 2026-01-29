# Stone Island Too High

## Summary
The floating stone island (Zone 3) is positioned too high in the world. It should be just out of view from ground level so players discover it naturally when building their first lift.

## Current Behavior
The floating island is at Y 280-450, which is far above the visible area when standing at ground level (Y ~1350).

## Expected Behavior
Island should be positioned just above the top of the viewport when standing at ground level:
- Player at ground level sees sky above
- Building a lift 50-100 cells reveals the island
- Creates a "discovery" moment

## Current Island Position
From `TutorialLevelData.cs`:
```csharp
// Main platform base (thick stone)
regions.Add(new TerrainRegion(100, 700, 350, 450, Materials.Stone));

// Surface layer (thin stone)
regions.Add(new TerrainRegion(120, 680, 320, 349, Materials.Stone));

// Bucket platform
regions.Add(new TerrainRegion(150, 250, 280, 319, Materials.Stone));
```

## Suggested Fix
Calculate based on:
- Ground level: Y ~1350
- Viewport height: 540 cells
- Top of viewport from ground: Y ~1350 - 270 = ~1080
- Island should be around Y 900-1050 (just above viewport)

New approximate values:
```csharp
// Main platform base
regions.Add(new TerrainRegion(100, 700, 950, 1050, Materials.Stone));

// Surface layer
regions.Add(new TerrainRegion(120, 680, 920, 949, Materials.Stone));

// Bucket platform
regions.Add(new TerrainRegion(150, 250, 880, 919, Materials.Stone));
```

Also update Bucket 3 spawn position to match new island height.

## Files to Modify
- `Assets/Scripts/Game/Levels/TutorialLevelData.cs`

## Severity
Medium - Affects game feel and discovery moment

## Status
OPEN
