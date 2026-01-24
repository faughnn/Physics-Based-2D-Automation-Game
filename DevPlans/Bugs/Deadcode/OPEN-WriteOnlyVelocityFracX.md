# Dead Code: Write-Only Cell.velocityFracX Field

## Summary
The `velocityFracX` field in the Cell struct is defined but never read anywhere in the codebase.

## Location
- **File:** `Assets/Scripts/Simulation/Cell.cs`
- **Line:** 15

## Field
```csharp
public byte velocityFracX;    // Fractional X velocity accumulator
```

## Context
- `velocityFracY` IS used for fractional gravity accumulation (smooth falling)
- `velocityFracX` was presumably intended for horizontal forces (wind, explosions, etc.)
- The horizontal force system was never implemented

## Memory Impact
1 byte per cell - could save ~4MB on large worlds if removed.

## Recommended Action
Remove if horizontal force accumulation is not planned, or keep as placeholder with TODO comment.
