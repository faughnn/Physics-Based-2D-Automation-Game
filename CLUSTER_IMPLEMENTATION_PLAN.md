# Cluster (Rigid Body) Physics Implementation Plan

## Overview

Add Noita-style cluster physics to the falling sand simulation. Clusters are groups of cells that move as rigid bodies with full physics (gravity, velocity, rotation, angular velocity).

**Key decisions:**
- **Unity Rigidbody2D with manual stepping** (like Noita's Box2D approach)
- "Two worlds" approach: cluster physics runs first, then syncs to grid, then cell simulation
- Max 65,535 clusters (ushort ownerId)
- Marching squares generates PolygonCollider2D for each cluster

---

## Frame Loop (Plain English)

```
Each Frame:
│
├── STEP 1: Cluster Physics (Unity Rigidbody2D)
│   └── Unity moves clusters based on velocity, gravity, collisions
│
├── STEP 2: Sync Clusters to Grid
│   ├── Calculate where each cluster's pixels are now (after physics)
│   ├── Write those pixels to the cell grid (set ownerId)
│   └── Push aside any loose sand/water (give them momentum)
│
└── STEP 3: Cell Simulation (existing system)
    ├── Skip cells where ownerId != 0 (they belong to clusters)
    └── Process loose cells normally (falling sand rules)
```

---

## What is a Cluster?

A cluster is a **Unity GameObject** with:

```
Cluster GameObject
├── Rigidbody2D          ← Unity handles physics (mass, velocity, rotation)
├── PolygonCollider2D    ← Collision shape from marching squares
└── ClusterData          ← Our data (pixels, materials, ownerId)
```

Unity's physics handles:
- Gravity
- Collision detection (cluster vs cluster, cluster vs world)
- Collision response (bouncing, friction)
- Rotation from off-center impacts

We handle:
- Generating the polygon from pixels (marching squares)
- Syncing cluster pixels to the cell grid
- Displacing loose cells with momentum transfer
- Telling cell simulation to skip cluster-owned cells

---

## Data Structures

### Modified Cell Struct (Cell.cs) ✅ DONE

```csharp
[StructLayout(LayoutKind.Sequential)]
public struct Cell
{
    public byte materialId;      // 1 byte
    public byte flags;           // 1 byte
    public ushort frameUpdated;  // 2 bytes
    public sbyte velocityX;      // 1 byte
    public sbyte velocityY;      // 1 byte
    public byte temperature;     // 1 byte
    public byte structureId;     // 1 byte - buildings/machines (unchanged)
    public ushort ownerId;       // 2 bytes NEW - cluster ownership (0 = free)
}
// Size: 10 bytes per cell (was 8)
```

### ClusterPixel Struct ✅ DONE

```csharp
[StructLayout(LayoutKind.Sequential)]
public struct ClusterPixel
{
    public short localX;         // offset from center of mass
    public short localY;
    public byte materialId;
}
```

### ClusterData Component ✅ DONE

```csharp
public class ClusterData : MonoBehaviour
{
    public ushort clusterId;                    // Unique ID (1-65535)
    public List<ClusterPixel> pixels;           // Pixel positions relative to center
    public Vector2 centerOfMass;                // Local center of mass offset

    // Cached references
    public Rigidbody2D rb;
    public PolygonCollider2D polyCollider;
}
```

---

## Files Created ✅

```
Assets/Scripts/Simulation/Clusters/
├── ClusterPixel.cs         ✅ DONE
├── ClusterData.cs          ✅ DONE
├── ClusterManager.cs       ✅ DONE
├── ClusterFactory.cs       ✅ DONE
├── MarchingSquares.cs      ✅ DONE
└── ClusterDebug.cs         ✅ DONE
```

---

## Files Modified ✅

| File | Status | Changes |
|------|--------|---------|
| `Cell.cs` | ✅ DONE | Added `ownerId` field (ushort) |
| `SimulateChunksJob.cs` | ✅ DONE | Skip cells where `ownerId != 0` |
| `CellSimulatorJobbed.cs` | ✅ DONE | Added ClusterManager parameter, calls StepAndSync |
| `CellWorld.cs` | ✅ DONE | Added `IsInBounds()` method |
| `SandboxController.cs` | ✅ DONE | Creates ClusterManager and ClusterDebug |

---

## Integration: Modified Simulation Loop

**File: CellSimulatorJobbed.cs**

```csharp
public void Simulate(CellWorld world, int gravityDivisor = 1, ClusterManager clusterManager = null)
{
    world.currentFrame++;

    // ========== STEP 1: CLUSTER PHYSICS ==========
    if (clusterManager != null)
    {
        clusterManager.StepAndSync(Time.fixedDeltaTime);
    }

    // ========== STEP 2: CELL SIMULATION (existing) ==========
    world.CollectChunkGroups(groupA, groupB, groupC, groupD);
    // ... existing job scheduling
}
```

**At startup (ClusterManager.Awake):**
```csharp
Physics2D.autoSimulation = false;  // We control when physics runs
```

---

## Key Algorithms

### Marching Squares ✅ DONE

Converts pixel grid → polygon outline for PolygonCollider2D.

### Local-to-World Transform ✅ DONE

```csharp
Vector2Int LocalToWorld(ClusterPixel pixel, ClusterData cluster)
{
    float cos = Mathf.Cos(cluster.RotationRad);
    float sin = Mathf.Sin(cluster.RotationRad);
    float rotatedX = pixel.localX * cos - pixel.localY * sin;
    float rotatedY = pixel.localX * sin + pixel.localY * cos;
    return new Vector2Int(
        Mathf.RoundToInt(cluster.Position.x + rotatedX),
        Mathf.RoundToInt(cluster.Position.y + rotatedY)
    );
}
```

### Sync to World ✅ DONE

- Clear old pixel positions from grid
- Step Unity physics
- Write new pixel positions to grid
- Displace loose cells with momentum transfer

---

## Test Controls (ClusterDebug.cs) ✅ DONE

- **7** = Spawn circle cluster at mouse
- **8** = Spawn square cluster at mouse
- **9** = Spawn L-shape cluster at mouse
- **[** / **]** = Decrease/increase test cluster size

---

## What Still Needs Testing

1. **Compile and run** - verify no errors
2. **Spawn a cluster** - press 7/8/9 with mouse in world
3. **Verify it falls** - should drop with gravity (Unity default gravity)
4. **Verify collision** - should stop on stone floor (requires Phase 5)
5. **Verify cell skip** - cluster pixels shouldn't simulate as individual cells
6. **Verify displacement** - cluster should push sand aside
7. **Verify rotation** - throw clusters at an angle, verify they rotate correctly

**Known coordinate system details:**
- Clusters use Unity world coordinates for physics
- Cell grid uses its own coordinate system (Y=0 at top, Y+ = down)
- LocalToWorldCell handles the conversion with correct rotation math

---

## Phased Implementation Status

### Phase 1: Foundation ✅ COMPLETE
- [x] Add `ownerId` to Cell struct
- [x] Create ClusterData MonoBehaviour
- [x] Create ClusterManager (tracks clusters, allocates IDs)
- [x] Set up `Physics2D.autoSimulation = false`
- [x] Modify SimulateChunksJob to skip `ownerId != 0`
- [ ] Test: Create a static cluster manually, verify cells are skipped

### Phase 2: Marching Squares + Polygon Collider ✅ COMPLETE
- [x] Implement MarchingSquares.cs
- [x] Create ClusterFactory (creates GameObject with Rigidbody2D + PolygonCollider2D)
- [ ] Test: Create cluster, verify polygon outline is correct

### Phase 3: Physics Integration ✅ COMPLETE
- [x] Integrate `Physics2D.Simulate()` into simulation loop
- [x] Implement ClusterWorldSync (clear old positions, write new positions)
- [ ] Test: Cluster falls and lands on static ground

### Phase 4: Cell Displacement ✅ COMPLETE
- [x] Implement displacement logic (push loose cells aside)
- [x] Add momentum transfer (cells get velocity from cluster)
- [ ] Test: Drop cluster into sand pile, verify sand is pushed aside

### Phase 5: World Collision - NOT STARTED
- [ ] Create static colliders for world boundaries
- [ ] Create colliders for static terrain (stone, etc.)
- [ ] Test: Clusters bounce off walls and static terrain

### Phase 6: Creation Tools - NOT STARTED
- [ ] ClusterFactory.CreateFromRegion() - convert world region to cluster
- [ ] UI for spawning test clusters
- [ ] Test: Draw shape, convert to falling cluster

---

## Debugging & Testing Tools ✅ DONE

### Visual Overlays (Gizmos)
- Polygon outlines (green)
- Center of mass (yellow)
- Velocity vectors (red arrows)
- Pixel positions (blue)
- Bounding circles (cyan)

### Stats Display (OnGUI)
- Active cluster count
- Total pixel count
- Displacements per frame
- Physics time (ms)
- Sync time (ms)

---

## Configuration (ClusterManager)

```csharp
public float defaultDensity = 1f;
public float defaultFriction = 0.3f;
public float defaultBounciness = 0.2f;
public float displacementMomentumFactor = 0.5f;
```

---

## Coordinate System Note

**Two coordinate systems are in use:**

### Unity World Coordinates (used by physics)
- Origin at center of screen
- X: -worldWidth to +worldWidth (each cell = 2 world units)
- Y: -worldHeight to +worldHeight (positive Y = up)
- Gravity pulls in -Y direction (normal Unity behavior)

### Cell Grid Coordinates (used by simulation)
- Origin at top-left
- X: 0 to width-1 (positive X = right, same as Unity)
- Y: 0 to height-1 (positive Y = DOWN, opposite to Unity)
- Cell Y=0 is at the TOP of the screen

### Conversion Formulas
```
World to Cell:
  cellX = (worldX + worldWidth) / 2
  cellY = (worldHeight - worldY) / 2

Cell to World:
  worldX = cellX * 2 - worldWidth
  worldY = worldHeight - cellY * 2
```

### How Clusters Handle This
- `ClusterData.Position` returns Unity world coordinates (from Rigidbody2D)
- `ClusterData.LocalToWorldCell()` converts to cell grid coordinates for sync
- `ClusterManager` uses cell coordinates for grid read/write operations
- `ClusterDebug` uses world coordinates for gizmo visualization

---

## Reference (Noita's Approach)

We're following Noita's architecture:
- **Two worlds**: Physics engine (they use Box2D, we use Unity Rigidbody2D) + pixel grid
- **Manual stepping**: We control when physics runs
- **Marching squares**: Convert pixels → polygon collision shape
- **Sync each frame**: Write cluster pixels to grid after physics
- **Displaced cells get velocity**: Momentum transfer from clusters to loose cells

Sources:
- [GDC Vault - Exploring the Tech and Design of 'Noita'](https://www.gdcvault.com/play/1025695/Exploring-the-Tech-and-Design/)
- [80.lv - Noita Falling Sand](https://80.lv/articles/noita-a-game-based-on-falling-sand-simulation)
- [Slow Rush Studios - Bridging Physics Worlds](https://www.slowrush.dev/news/bridging-physics-worlds/)
