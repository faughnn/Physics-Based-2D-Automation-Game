# Duplicated Physics Simulation Logic Analysis

## Overview

This document analyzes the duplication between `CellSimulator.cs` and `SimulateChunksJob.cs`, two implementations of the falling sand physics simulation. The job-based version has evolved beyond the original implementation with more sophisticated behavior, creating a maintenance risk.

---

## File Locations

| File | Path | Lines |
|------|------|-------|
| CellSimulator.cs | `G:\Sandy\Assets\Scripts\Simulation\CellSimulator.cs` | 383 lines |
| SimulateChunksJob.cs | `G:\Sandy\Assets\Scripts\Simulation\Jobs\SimulateChunksJob.cs` | 470 lines |

---

## Usage Analysis

### Is CellSimulator.cs Actually Used?

**NO.** `CellSimulator.cs` is **dead code** that is never called anywhere in the codebase.

**Evidence:**
- Search for `CellSimulator.Simulate` returns **zero results**
- `SandboxController.cs` (line 26) only creates `CellSimulatorJobbed`
- `SandboxController.Update()` (line 146) only calls `simulator.Simulate()` via `CellSimulatorJobbed`
- The class is `public static` with a `Simulate(CellWorld world)` method but nothing invokes it

### What IS Used?

The active simulation pipeline is:
1. `SandboxController.Update()` calls `simulator.Simulate(world, gravityDivisor, clusterManager)`
2. `CellSimulatorJobbed.Simulate()` schedules `SimulateChunksJob` instances
3. `SimulateChunksJob.Execute()` runs the actual physics

---

## Detailed Comparison of Duplicated Methods

### 1. SimulateCell() Method

| Aspect | CellSimulator.cs (Lines 85-122) | SimulateChunksJob.cs (Lines 88-128) |
|--------|--------------------------------|-------------------------------------|
| Frame check | `cell.frameUpdated == world.currentFrame` | `cell.frameUpdated == currentFrame` |
| Air check | Identical | Identical |
| **Cluster check** | **MISSING** | `if (cell.ownerId != 0) return;` (Line 103-104) |
| Material lookup | `world.materials[cell.materialId]` | `materials[cell.materialId]` |
| Static check | Identical | Identical |
| Behavior switch | Identical structure | Identical structure |

**Critical Difference:** The jobbed version skips cells owned by clusters (`ownerId != 0`). This is essential for rigid body physics integration.

---

### 2. SimulatePowder() Method

| Aspect | CellSimulator.cs (Lines 124-171) | SimulateChunksJob.cs (Lines 130-180) |
|--------|----------------------------------|--------------------------------------|
| **Gravity application** | `cell.velocityY = (sbyte)math.min(cell.velocityY + (int)PhysicsSettings.Gravity, PhysicsSettings.MaxVelocity)` (always applied) | Only applied when `currentFrame % gravityDivisor == 0` (Lines 133-136) |
| Falling logic | Identical | Identical |
| Diagonal logic | Identical | Identical |
| Stuck handling | Identical | Identical |

**Critical Difference:** The jobbed version supports variable gravity rate via `gravityDivisor` for slow-motion simulation.

---

### 3. SimulateLiquid() Method - MAJOR DIVERGENCE

This is where the implementations differ **significantly**.

#### CellSimulator.cs (Lines 173-230) - Simple Implementation
```csharp
private static void SimulateLiquid(CellWorld world, int x, int y, Cell cell, MaterialDef mat)
{
    // Apply gravity
    cell.velocityY = (sbyte)math.min(cell.velocityY + (int)PhysicsSettings.Gravity, PhysicsSettings.MaxVelocity);

    // Try falling first
    if (TryFall(world, x, y, cell, mat.density))
        return;

    if (TryDiagonalFall(world, x, y, cell, mat.density))
        return;

    // Spread horizontally
    int spread = math.max(1, (PhysicsSettings.MaxVelocity - math.abs(cell.velocityY)) / (mat.friction + 1));

    // Simple linear search in one direction, then the other
    for (int dist = 1; dist <= spread; dist++) { ... }
    for (int dist = 1; dist <= spread; dist++) { ... }
}
```

#### SimulateChunksJob.cs (Lines 182-257) - Sophisticated Implementation
```csharp
private void SimulateLiquid(int x, int y, Cell cell, MaterialDef mat)
{
    // Track if we were free-falling before this frame
    bool wasFreeFalling = cell.velocityY > 2;

    // Conditional gravity application
    if (currentFrame % gravityDivisor == 0)
    {
        cell.velocityY = (sbyte)math.min(cell.velocityY + gravity, maxVelocity);
    }

    // Try falling first
    if (TryFall(x, y, cell, mat.density))
        return;

    if (TryDiagonalFall(x, y, cell, mat.density))
        return;

    // Can't fall - convert vertical momentum to horizontal spread (Java-style)
    int velocityBoost = wasFreeFalling ? math.abs(cell.velocityY) / 3 : 0;
    int spread = mat.dispersionRate + velocityBoost;

    // Add randomization for natural look (Burst-compatible hash)
    uint hash = HashPosition(x, y, currentFrame);
    int randomOffset = (int)(hash % 3) - 1;  // -1, 0, or +1
    spread = math.max(1, spread + randomOffset);

    // Convert falling velocity to horizontal velocity when landing
    if (wasFreeFalling && cell.velocityX == 0)
    {
        bool goLeft = (hash & 4) != 0;
        cell.velocityX = (sbyte)(goLeft ? -4 : 4);
    }

    // Determine primary direction: follow existing horizontal velocity
    bool tryLeftFirst;
    if (cell.velocityX < 0)
        tryLeftFirst = true;
    else if (cell.velocityX > 0)
        tryLeftFirst = false;
    else
        tryLeftFirst = ((x + y + currentFrame) & 1) == 0;

    // Find FURTHEST reachable position (not first)
    int bestDist1 = FindSpreadDistance(x, y, dx1, spread, mat.density);
    int bestDist2 = FindSpreadDistance(x, y, dx2, spread, mat.density);

    // Move to furthest valid position
    // Dampen horizontal velocity over time
    cell.velocityX = (sbyte)(cell.velocityX * 7 / 8);
}
```

#### Key Behavioral Differences in Liquid Simulation

| Feature | CellSimulator.cs | SimulateChunksJob.cs |
|---------|------------------|----------------------|
| **Momentum tracking** | None | Tracks `wasFreeFalling` state |
| **Velocity-to-spread conversion** | No | `velocityBoost = wasFreeFalling ? abs(velocityY) / 3 : 0` |
| **Spread calculation** | `(MaxVelocity - abs(velocityY)) / (friction + 1)` | `mat.dispersionRate + velocityBoost` |
| **Randomization** | None | `HashPosition()` adds -1/0/+1 variation |
| **Horizontal velocity** | Not used | Landing water gains `velocityX = +/-4` |
| **Direction preference** | Position-based only | Follows existing `velocityX` first |
| **Search strategy** | First valid position | Furthest valid position via `FindSpreadDistance()` |
| **Velocity dampening** | None | `velocityX *= 7/8` each frame |
| **Gravity divisor** | Not supported | Supported |

**Unique methods in SimulateChunksJob.cs:**
- `FindSpreadDistance()` (Lines 260-280) - Finds maximum reachable distance
- `HashPosition()` (Lines 283-289) - Burst-compatible position hash for randomization

---

### 4. SimulateGas() Method

| Aspect | CellSimulator.cs (Lines 232-287) | SimulateChunksJob.cs (Lines 291-349) |
|--------|----------------------------------|--------------------------------------|
| **Gravity application** | Always applied | Only when `currentFrame % gravityDivisor == 0` |
| Rising logic | Identical | Identical |
| Diagonal logic | Identical | Identical |
| Horizontal spread | Identical | Identical |
| Stuck handling | Identical | Identical |

**Only difference:** Gravity divisor support.

---

### 5. Helper Methods Comparison

| Method | CellSimulator.cs | SimulateChunksJob.cs | Differences |
|--------|------------------|----------------------|-------------|
| `TryFall()` | Lines 289-308 | Lines 351-370 | Field access only |
| `TryDiagonalFall()` | Lines 310-329 | Lines 372-391 | Field access only |
| `IsInBounds()` | Lines 331-335 | Lines 393-397 | Signature (world params vs fields) |
| `IsEmpty()` | Lines 337-341 | Lines 399-403 | Field access |
| `CanMoveTo()` | Lines 343-361 | Lines 405-423 | Field access |
| `MoveCell()` | Lines 363-381 | Lines 425-443 | Dirty marking method |
| `FindSpreadDistance()` | **N/A** | Lines 260-280 | **Only in jobbed version** |
| `HashPosition()` | **N/A** | Lines 283-289 | **Only in jobbed version** |
| `MarkDirtyInternal()` | **N/A** | Lines 445-468 | Job-local dirty tracking |

---

### 6. MoveCell() Method - Dirty Tracking Difference

**CellSimulator.cs (Line 379-380):**
```csharp
world.MarkDirtyWithNeighbors(fromX, fromY);
world.MarkDirtyWithNeighbors(toX, toY);
```

**SimulateChunksJob.cs (Line 441-442):**
```csharp
MarkDirtyInternal(fromX, fromY);
MarkDirtyInternal(toX, toY);
```

The job version has its own `MarkDirtyInternal()` that writes directly to the `chunks` NativeArray to avoid thread contention.

---

## Risk Assessment

### Divergence Risk: HIGH

The implementations have already significantly diverged, particularly in liquid simulation. Future changes to `SimulateChunksJob.cs` will not be reflected in `CellSimulator.cs`, and vice versa (if anyone mistakenly updates it).

### Dead Code Risk: HIGH

`CellSimulator.cs` provides no value but:
1. Increases codebase size
2. Could confuse developers
3. Might be mistakenly used in the future
4. Tests written against it would pass but not reflect actual game behavior

### Maintenance Cost: MEDIUM

Currently `CellSimulator.cs` is not maintained (dead code), but its existence:
1. Makes the codebase harder to understand
2. Could lead to bugs if someone tries to use it expecting it to match jobbed behavior
3. Violates the "single source of truth" principle from CLAUDE.md

---

## Proposed Solutions

### Option 1: Delete CellSimulator.cs (Recommended)

**Pros:**
- Eliminates duplication entirely
- Single source of truth
- Reduces codebase size
- Follows project guidelines ("Systems, Not Patches")

**Cons:**
- Loses a simple reference implementation for debugging
- Cannot fallback to single-threaded if job system fails

**Implementation:**
1. Delete `G:\Sandy\Assets\Scripts\Simulation\CellSimulator.cs`
2. Delete `G:\Sandy\Assets\Scripts\Simulation\CellSimulator.cs.meta`
3. Update any documentation referencing it

---

### Option 2: Extract Shared Logic to Static Methods

Create a shared physics helper class that both implementations call.

**Pros:**
- DRY principle
- Easier to maintain consistent behavior
- Can still have job and non-job versions

**Cons:**
- Burst compilation requires special handling for static methods
- More complex architecture
- May not be worth it if non-jobbed version isn't used

**Implementation:**
```csharp
// New file: CellPhysicsLogic.cs
[BurstCompile]
public static class CellPhysicsLogic
{
    public static bool TryFall(/* params */) { ... }
    public static bool TryDiagonalFall(/* params */) { ... }
    public static int FindSpreadDistance(/* params */) { ... }
    // etc.
}
```

---

### Option 3: Keep as Debug/Test Fallback (Not Recommended)

Mark `CellSimulator.cs` as obsolete and keep for debugging.

**Pros:**
- Provides simple single-threaded fallback
- Useful for debugging job-related issues

**Cons:**
- Must maintain two implementations
- Will continue to diverge
- Violates project guidelines

---

## Recommendation

**Delete `CellSimulator.cs` entirely (Option 1).**

Rationale:
1. It is not used anywhere in the codebase
2. The jobbed version is strictly superior (better liquid physics, cluster support, simulation speed control)
3. The project guidelines explicitly state "One source of truth - if logic exists, it lives in ONE place"
4. If single-threaded debugging is needed in the future, it can be added as a debug flag in `CellSimulatorJobbed` that runs jobs synchronously

---

## Files to Modify

| Action | File | Notes |
|--------|------|-------|
| DELETE | `G:\Sandy\Assets\Scripts\Simulation\CellSimulator.cs` | Dead code |
| DELETE | `G:\Sandy\Assets\Scripts\Simulation\CellSimulator.cs.meta` | Unity meta file |
| VERIFY | Git status already shows both files as deleted | Confirm changes are staged |

---

## Summary Table

| Method | Lines in CellSimulator | Lines in SimulateChunksJob | Identical? |
|--------|----------------------|---------------------------|------------|
| SimulateCell | 85-122 (37 lines) | 88-128 (40 lines) | NO - cluster check missing |
| SimulatePowder | 124-171 (47 lines) | 130-180 (50 lines) | NO - gravity divisor |
| SimulateLiquid | 173-230 (57 lines) | 182-257 (75 lines) | **MAJOR DIFFERENCES** |
| SimulateGas | 232-287 (55 lines) | 291-349 (58 lines) | NO - gravity divisor |
| TryFall | 289-308 (19 lines) | 351-370 (19 lines) | Structurally identical |
| TryDiagonalFall | 310-329 (19 lines) | 372-391 (19 lines) | Structurally identical |
| IsInBounds | 331-335 (4 lines) | 393-397 (4 lines) | Signature differs |
| IsEmpty | 337-341 (4 lines) | 399-403 (4 lines) | Field access differs |
| CanMoveTo | 343-361 (18 lines) | 405-423 (18 lines) | Structurally identical |
| MoveCell | 363-381 (18 lines) | 425-443 (18 lines) | Dirty marking differs |
| FindSpreadDistance | N/A | 260-280 (20 lines) | **Job only** |
| HashPosition | N/A | 283-289 (6 lines) | **Job only** |
| MarkDirtyInternal | N/A | 445-468 (23 lines) | **Job only** |

---

## Conclusion

The `CellSimulator.cs` file is dead code that duplicates (and is inferior to) the logic in `SimulateChunksJob.cs`. The jobbed version has evolved with:
- Sophisticated momentum-based liquid spreading
- Velocity dampening and horizontal momentum transfer
- Cluster ownership checks for rigid body integration
- Simulation speed control via gravity divisor
- Randomized spread for natural-looking fluid behavior

The recommended action is to **delete `CellSimulator.cs`** entirely, as it provides no value and violates the project's architectural principles.
