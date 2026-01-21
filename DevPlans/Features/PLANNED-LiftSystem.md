# Feature: Lift System

## Summary

Vertical conveyor lifts that move cells and clusters up or down. Lifts are the vertical counterpart to belts, enabling vertical material transport. Uses 8x8 tile blocks that can be merged vertically (like belts merge horizontally).

---

## Goals

- Move loose cells (powder, liquid) vertically at configurable speeds
- Support both upward and downward movement
- Carry clusters by setting velocity directly (like belts)
- Integrate with existing belt and cell simulation pipeline
- Burst-compiled parallel simulation
- Follow belt system architecture patterns for consistency

---

## Design

### Overview

Lifts are 8x8 cell blocks placed on an 8-aligned grid. Adjacent lift blocks with the same direction merge vertically (like belts merge horizontally). Cells on the lift surface are moved in the lift's direction. Clusters touching the lift have their Y velocity set directly.

### Key Components

**New Files:**
```
Assets/Scripts/Structures/
├── LiftTile.cs              # Per-cell tile data
├── LiftStructure.cs         # Merged lift structure data
├── LiftManager.cs           # Manager class (mirrors BeltManager)

Assets/Scripts/Simulation/Jobs/
└── SimulateLiftJob.cs       # Burst job for cell movement
```

**Modified Files:**
- `Assets/Scripts/Simulation/MaterialDef.cs` - Add lift material IDs and colors
- `Assets/Scripts/Simulation/CellSimulatorJobbed.cs` - Call lift simulation
- `Assets/Scripts/SandboxController.cs` - Add lift placement mode
- `Assets/Scripts/Simulation/Clusters/ClusterData.cs` - Add `isOnLift` flag
- `Assets/Scripts/Simulation/Clusters/ClusterManager.cs` - Prevent sleep when `isOnLift`

### Data Structures

```csharp
// LiftTile.cs - Per-cell data (mirrors BeltTile)
public struct LiftTile
{
    public sbyte direction;    // +1 = down, -1 = up
    public ushort liftId;      // Which LiftStructure this tile belongs to
}

// LiftStructure.cs - Merged lift data (mirrors BeltStructure)
public struct LiftStructure
{
    public const int Width = 8;
    public const int Height = 8;

    public ushort id;
    public int tileX;           // X coordinate of the lift block
    public int minY, maxY;      // Vertical extent (merged lifts)
    public sbyte direction;     // +1 = down, -1 = up
    public byte speed;          // Frames per move (default 3)
    public byte frameOffset;    // Staggered timing

    // Surface where cells enter (left edge of lift)
    public int SurfaceX => tileX - 1;
    public int Span => maxY - minY + Height;
}
```

**Material IDs to add (MaterialDef.cs, after BeltRightLight = 16):**
```csharp
public const byte LiftUp = 17;        // Dark stripe, moving up
public const byte LiftDown = 18;      // Dark stripe, moving down
public const byte LiftUpLight = 19;   // Light stripe, moving up
public const byte LiftDownLight = 20; // Light stripe, moving down
```

**ClusterData addition:**
```csharp
[HideInInspector] public bool isOnLift;  // Set by LiftManager, prevents sleep
```

### Behavior

**Loose Cell Movement (SimulateLiftJob):**
1. Each frame (based on speed), scan the surface column (X = tileX - 1)
2. Process in opposite direction of movement to avoid double-moving:
   - Up lift: scan bottom-to-top
   - Down lift: scan top-to-bottom
3. For each cell on surface within lift's Y range:
   - Skip air, structure tiles, cluster-owned cells
   - Only move Powder and Liquid behavior types
   - Swap cell with target position if target is air
4. Mark affected chunks dirty

```csharp
[BurstCompile]
public struct SimulateLiftJob : IJobParallelFor
{
    public void Execute(int liftIndex)
    {
        LiftStructure lift = lifts[liftIndex];

        if ((currentFrame - lift.frameOffset) % lift.speed != 0)
            return;

        int surfaceX = lift.SurfaceX;
        if (surfaceX < 0 || surfaceX >= width)
            return;

        // Process rows in opposite direction of movement
        if (lift.direction < 0)  // Moving up: scan bottom to top
        {
            for (int y = lift.minY + lift.Span - 1; y >= lift.minY; y--)
                MoveRowOnLift(surfaceX, y, lift.direction);
        }
        else  // Moving down: scan top to bottom
        {
            for (int y = lift.minY; y < lift.minY + lift.Span; y++)
                MoveRowOnLift(surfaceX, y, lift.direction);
        }
    }
}
```

**Cluster Movement (LiftManager.ApplyForcesToClusters):**
```csharp
public void ApplyForcesToClusters(ClusterManager clusterManager, int worldWidth, int worldHeight)
{
    foreach (var cluster in clusterManager.AllClusters)
    {
        if (cluster.rb == null) continue;
        bool foundLift = false;

        for (int i = 0; i < lifts.Length; i++)
        {
            if (ClusterTouchingLift(cluster, lifts[i], worldWidth, worldHeight))
            {
                foundLift = true;
                // Set Y velocity directly (Unity Y+ = up, so negate direction)
                Vector2 vel = cluster.rb.linearVelocity;
                vel.y = -lifts[i].direction * LiftCarrySpeed;  // direction -1 (up) → positive vel
                cluster.rb.linearVelocity = vel;
                break;
            }
        }
        cluster.isOnLift = foundLift;
    }
}
```

**Input Handling (SandboxController):**
- `L` key: Toggle lift mode
- `Q` key: Set direction to UP (-1)
- `E` key: Set direction to DOWN (+1)
- Left click drag: Place lift blocks (X locked during drag for vertical lines)
- Right click: Remove lift blocks

### Visual Pattern

Arrow pattern pointing up or down (8x8, similar to belt chevrons):
```
Up Lift:      Down Lift:
0 0 0 1 1 0 0 0    0 0 0 1 1 0 0 0
0 0 1 1 1 1 0 0    0 0 0 1 1 0 0 0
0 1 1 1 1 1 1 0    0 0 0 1 1 0 0 0
0 0 0 1 1 0 0 0    0 1 1 1 1 1 1 0
0 0 0 1 1 0 0 0    0 0 1 1 1 1 0 0
0 0 0 1 1 0 0 0    0 0 0 1 1 0 0 0
0 0 0 1 1 0 0 0    0 0 0 1 1 0 0 0
0 0 0 1 1 0 0 0    0 0 0 1 1 0 0 0
```

---

## Integration Points

### Pipeline Order (CellSimulatorJobbed.Simulate)

```
1. Belt Forces → Clusters (beltManager.ApplyForcesToClusters)
2. Lift Forces → Clusters (liftManager.ApplyForcesToClusters)  [NEW]
3. Cluster Physics (clusterManager.StepAndSync)
4. Cell Simulation (4-pass chunks)
5. Belt Cell Movement (SimulateBeltsJob)
6. Lift Cell Movement (SimulateLiftJob)  [NEW]
```

### ClusterManager Sleep Prevention

```csharp
// In StepAndSync, extend existing belt check:
if (cluster.isOnBelt || cluster.isOnLift)
{
    cluster.lowVelocityFrames = 0;
    continue;  // Don't sleep
}
```

### StructureType Enum

`StructureType.Lift = 2` is already reserved in the enum.

---

## Open Questions

1. **Surface side**: Left edge only, or both left and right edges?
   - Recommendation: Left edge only (simpler, matches belt pattern)

2. **Lift + Belt intersection**: How do they interact at junctions?
   - Could defer to later; for now, they don't intersect

3. **Carry speed**: Same as belts (30 units/sec) or different?
   - Recommendation: Same speed for consistency

---

## Priority

Medium - Enables vertical automation and interesting factory layouts

---

## Related Files

- `Assets/Scripts/Structures/BeltManager.cs` - Pattern to follow
- `Assets/Scripts/Structures/BeltStructure.cs` - Pattern to follow
- `Assets/Scripts/Structures/BeltTile.cs` - Pattern to follow
- `Assets/Scripts/Simulation/Jobs/SimulateBeltsJob.cs` - Pattern to follow
- `Assets/Scripts/Simulation/MaterialDef.cs` - Add material IDs
- `Assets/Scripts/Simulation/CellSimulatorJobbed.cs` - Integration point
- `Assets/Scripts/SandboxController.cs` - Input handling
- `Assets/Scripts/Simulation/Clusters/ClusterData.cs` - Add isOnLift
- `Assets/Scripts/Simulation/Clusters/ClusterManager.cs` - Sleep prevention
