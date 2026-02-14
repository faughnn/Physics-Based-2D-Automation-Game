# Bug: Clusters Break Too Easily From Cluster-on-Cluster Collisions

## Summary
The compression/fracture system added for pistons to break clusters is triggering too liberally. Normal cluster-on-cluster collisions (dropping, stacking) cause clusters to fracture when they should only break under genuine crush pressure (e.g., pistons).

## Symptoms
- Dropping a cluster onto another cluster causes one or both to fracture
- Stacked clusters fracture after ~0.5 seconds of sitting on each other
- Clusters break apart from ordinary gravity-loaded contact, not just piston crushing

## Root Cause
The compression detection system in `ClusterManager.CheckCompressionAndFracture()` has thresholds that are too permissive for general gameplay:

1. **`MinCrushImpulse = 5f` is too low** - Normal gravity-loaded contacts between stacked clusters easily exceed this. Two clusters resting on each other generate impulses well above 5 from weight alone.

2. **No distinction between crush and normal contact** - The system treats a cluster sitting on terrain with another cluster on top the same as a cluster being crushed by a piston. Any two contacts with opposing normals and impulse > 5 start the fracture countdown.

3. **`CrushFrameThreshold = 30` (~0.5s) is too short** - Clusters that land on each other and settle routinely have 0.5s of sustained contact. This is normal physics settling, not crushing.

4. **Anti-sleep feedback loop** - `crushPressureFrames > 0` prevents clusters from sleeping (line 229), keeping them awake to accumulate more crush frames, creating a self-reinforcing cycle toward fracture.

5. **Piston force (800) amplifies the issue** - When a piston pushes a cluster against something, the 800-force push creates very strong opposing contacts, virtually guaranteeing fracture. This is the intended use case, but the same system over-triggers in non-piston scenarios.

## Affected Code
- `Assets/Scripts/Simulation/Clusters/ClusterManager.cs` - Threshold constants (lines 49-52), `CheckCompressionAndFracture()` (lines 268-324), anti-sleep guard (line 229)
- `Assets/Scripts/Simulation/Clusters/ClusterData.cs` - `crushPressureFrames` field (line 46)
- `Assets/Scripts/Simulation/Machines/PistonManager.cs` - `ClusterPushForce = 800f`, force application when stalled (lines 267-282)

## Potential Solutions

### 1. Material Strength / Pressure Threshold System (User's Preferred Direction)
Add a `strength` or `crushResistance` property to `MaterialDef`. Each material defines how much force is needed to break it apart. The compression detection compares total opposing impulse against the cluster's material strength before counting frames. This makes the system data-driven and extensible - stone clusters would be very hard to break, sand clusters easier, etc.

**Tradeoffs:** More design work needed to tune per-material values. Most architecturally sound long-term solution.

### 2. Raise Thresholds + Require Kinematic Source
Increase `MinCrushImpulse` significantly (50-100+), increase `CrushFrameThreshold` (120+ frames / 2+ seconds), and require at least one opposing contact to come from a kinematic or machine-part body. This would effectively limit fracture to piston scenarios only.

**Tradeoffs:** Quick fix but less flexible. Wouldn't support future mechanics where dynamic objects should also cause crushing.

### 3. Hybrid: Material Strength + Higher Base Thresholds
Combine both approaches: add material strength as the long-term system, but also raise the base thresholds so the system is less trigger-happy even before material tuning. Machine parts (pistons) could apply a force multiplier or bypass the strength check.

**Tradeoffs:** Best of both worlds but more implementation effort.

## Priority
High - Clusters breaking from normal interactions severely impacts gameplay.

## Related Files
- `Assets/Scripts/Simulation/Clusters/ClusterManager.cs`
- `Assets/Scripts/Simulation/Clusters/ClusterData.cs`
- `Assets/Scripts/Simulation/Clusters/ClusterFactory.cs`
- `Assets/Scripts/Simulation/Machines/PistonManager.cs`
- `Assets/Scripts/Simulation/MaterialDef.cs`
