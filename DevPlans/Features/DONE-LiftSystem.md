# Feature: Lift System

## Summary

Vertical lifts that move cells upward using physics-based fractional velocity. Lifts are hollow force zones - material passes through them and experiences upward force that fights gravity.

---

## Design Decisions

### Hollow Force Zones
- Lifts are NOT solid structures - material can pass through them
- They define a region where upward force is applied
- Material enters from any side (typically pushed by a belt from below/side)
- Material exits naturally at the top with upward momentum
- Like a column of air being pushed from below

### Vertical Merging
- Adjacent lift tiles merge vertically (like belts merge horizontally)
- A tall lift is multiple 8x8 tiles stacked
- LiftStructure tracks minY/maxY for the merged extent

### Placement
- Same controls as belt placement
- 8x8 tiles on 8-aligned grid
- Placed via sandbox brush or game mechanics

---

## Physics: Loose Cells

### Gravity + Lift Force Fight
Gravity and lift force compete in the same fractional accumulator:

```csharp
int netForce = fractionalGravity;  // +17 (down)
if (inLift)
    netForce -= liftForce;  // 17 - 20 = -3 (net upward)

int newFracY = cell.velocityFracY + netForce;

if (newFracY >= 256)  // overflow -> accelerate down
{
    cell.velocityFracY = (byte)(newFracY - 256);
    cell.velocityY = (sbyte)math.min(cell.velocityY + gravity, maxVelocity);
}
else if (newFracY < 0)  // underflow -> accelerate up
{
    cell.velocityFracY = (byte)(newFracY + 256);
    cell.velocityY = (sbyte)math.max(cell.velocityY - gravity, -maxVelocity);
}
else
{
    cell.velocityFracY = (byte)newFracY;
}
```

**How it works:**
- Gravity adds +17 per frame (toward overflow/down)
- Lift subtracts 20 per frame (toward underflow/up)
- Net: -3 per frame in lift zone
- Underflows every ~85 frames, triggering velocityY-- (rise)
- Cells slow their fall, reverse direction, gradually accelerate upward

**Tuning:**
- `liftForce = 20`: slow rise (net -3)
- `liftForce = 25`: faster rise (net -8)
- `liftForce = 17`: hover (net 0)
- `liftForce < 17`: slows fall but doesn't reverse

### Lift Zone Detection (Cells)
Cell must be INSIDE the lift structure bounds:

```csharp
private bool IsInLiftZone(int x, int y)
{
    // Check if position is within any lift structure
    ushort liftId = liftTiles[y * width + x].liftId;
    return liftId != 0;
}
```

Uses LiftTile array (parallel to cells) for O(1) lookup.

### Natural Gaps (Emergent Behavior)
Bottom-to-top processing creates natural spacing:

**Frame 1:**
```
[C]  <- top cell moves up (nothing above)
[B]  <- can't move (C was there when processed)
[A]  <- can't move (B was there when processed)
```

**Frame 2:**
```
[C]  <- moves up again
[ ]  <- gap
[B]  <- now moves up (space above)
[A]  <- can't move yet
```

Material spreads out as it rises. This is intentional - gives lifts visual character.

**Requires:** Processed-this-frame flag to prevent double-movement.

---

## Physics: Clusters (Rigid Bodies)

### Force-Based Lift
Clusters in lift zones receive upward force that fights gravity:

```csharp
public void ApplyForcesToClusters(ClusterManager clusterManager, int worldWidth, int worldHeight)
{
    if (clusterManager == null || lifts.Length == 0) return;

    foreach (var cluster in clusterManager.AllClusters)
    {
        if (cluster.rb == null) continue;

        bool foundLift = false;

        for (int i = 0; i < lifts.Length; i++)
        {
            LiftStructure lift = lifts[i];
            if (ClusterInLiftZone(cluster, lift, worldWidth, worldHeight))
            {
                foundLift = true;

                // Apply upward force that slightly exceeds gravity
                // This mirrors how loose cells work - forces fight
                float liftForce = -Physics2D.gravity.y * LiftForceMultiplier * cluster.rb.mass;
                cluster.rb.AddForce(new Vector2(0, liftForce));
                break;
            }
        }

        cluster.isOnLift = foundLift;
    }
}
```

**LiftForceMultiplier:** ~1.2 means lift force is 120% of gravity, so clusters slowly accelerate upward.

### Cluster Detection
Check if cluster's bounds overlap lift zone:

```csharp
private bool ClusterInLiftZone(ClusterData cluster, LiftStructure lift, int worldWidth, int worldHeight)
{
    Vector2 cellPos = CoordinateUtils.WorldToCellFloat(cluster.Position, worldWidth, worldHeight);

    // Check if cluster center is within lift bounds
    int liftMinX = lift.tileX;
    int liftMaxX = lift.tileX + LiftStructure.TileSize;
    int liftMinY = lift.minY;
    int liftMaxY = lift.maxY + LiftStructure.TileSize;

    return cellPos.x >= liftMinX && cellPos.x < liftMaxX &&
           cellPos.y >= liftMinY && cellPos.y < liftMaxY;
}
```

### Sleep Prevention
Add `isOnLift` flag to ClusterData. ClusterManager checks both:

```csharp
if (cluster.isOnBelt || cluster.isOnLift)
{
    cluster.lowVelocityFrames = 0;
    continue;  // Don't sleep
}
```

---

## Technical Details

### Material IDs
Starting at 19 (17-18 are Dirt/Ground):
```csharp
public const byte LiftUp = 19;
public const byte LiftUpLight = 20;
```

Note: These are for RENDERING only. Lift tiles don't block movement - they're force zones.

### Data Structures
```csharp
// LiftTile.cs - parallel array to cells
public struct LiftTile
{
    public ushort liftId;  // 0 = not in lift, >0 = which lift structure
}

// LiftStructure.cs
public struct LiftStructure
{
    public const int TileSize = 8;

    public ushort id;
    public int tileX;           // X position (8-aligned)
    public int minY, maxY;      // Vertical extent (merged lifts)
    public byte liftForce;      // Fractional force (default 20)
}
```

### LiftManager
Mirrors BeltManager:
- Tracks all LiftStructure instances
- Maintains LiftTile array
- Provides `ApplyForcesToClusters()` method
- Handles placement/removal/merging

### StructureType
`StructureType.Lift = 2` is already reserved.

---

## Pipeline Integration

```
SimulationManager.Update():
1. simulator.Simulate()
   - Belt forces -> clusters (beltManager.ApplyForcesToClusters)
   - Lift forces -> clusters (liftManager.ApplyForcesToClusters)  [NEW]
   - Cluster physics (clusterManager.StepAndSync)
   - Cell simulation (4-pass)
     - SimulatePowder/Liquid checks liftTiles, applies net force
   - ResetDirtyState
2. beltManager.ScheduleSimulateBelts()
3. cellRenderer.UploadFullTexture()
```

---

## Visual Representation

- 8x8 tiles with animated upward arrows
- Semi-transparent or outline style (shows it's hollow)
- Light/dark stripe pattern animates for movement feel
- Cells visible passing through

---

## Dependencies

- **Processed flag** - Must be implemented first to prevent double-movement of rising cells

---

## Files to Create/Modify

**New Files:**
- `Assets/Scripts/Structures/LiftTile.cs`
- `Assets/Scripts/Structures/LiftStructure.cs`
- `Assets/Scripts/Structures/LiftManager.cs`

**Modify:**
- `Assets/Scripts/Simulation/Jobs/SimulateChunksJob.cs` - Add lift zone check, net force calc
- `Assets/Scripts/Simulation/MaterialDef.cs` - Add LiftUp/LiftUpLight IDs
- `Assets/Scripts/Simulation/PhysicsSettings.cs` - Add LiftForce constant
- `Assets/Scripts/Simulation/Clusters/ClusterData.cs` - Add isOnLift flag
- `Assets/Scripts/Simulation/Clusters/ClusterManager.cs` - Check isOnLift for sleep
- `Assets/Scripts/Simulation/CellSimulatorJobbed.cs` - Call liftManager.ApplyForcesToClusters
- `Assets/Scripts/SandboxController.cs` - Add lift placement mode

---

## Priority

Medium - Enables vertical automation and interesting factory layouts
