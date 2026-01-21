# Bucket & Progression System Implementation Plan

## Summary

Implement a bucket world object that collects falling cells (primarily dirt) and a progression system that tracks collected materials and unlocks player abilities. This is the core mechanic for the first level: fill the bucket with 20 dirt to unlock the belt placement ability.

---

## Goals

1. **Bucket World Object**: Static structure with a collection zone that detects and consumes falling cells
2. **Material Tracking**: Track collected cell counts by material type
3. **Progress UI**: Display current collection progress (e.g., "12/20 Dirt")
4. **Ability Unlock System**: Unlock abilities (belts) when thresholds are met
5. **Unlock Feedback**: Visual notification when abilities unlock

---

## Design

### Architecture Overview

Following the project's "Systems, Not Patches" philosophy:
- **ProgressionManager**: Single source of truth for all progression state (unlocks, tracked objectives)
- **Bucket**: World object that delegates collection events to ProgressionManager
- **CollectionZone**: Reusable component for detecting cells in an area (could be used by other structures)
- **ProgressionUI**: Observes ProgressionManager state and renders feedback

```
Assets/Scripts/Game/
├── Progression/
│   ├── ProgressionManager.cs      # Tracks unlocks, objectives, fires events
│   ├── Ability.cs                 # Enum of unlockable abilities
│   └── ObjectiveData.cs           # Data structure for objectives
├── WorldObjects/
│   ├── Bucket.cs                  # Bucket MonoBehaviour
│   └── CollectionZone.cs          # Detects and removes cells in area
└── UI/
    └── ProgressionUI.cs           # Renders progress and unlock notifications
```

---

## Cell Detection Strategy

### Approach: Direct Cell Scanning

Rather than using Unity triggers (which don't work with cells), scan the cell grid directly each frame:

```csharp
public class CollectionZone
{
    private RectInt cellBounds;  // Zone bounds in cell coordinates
    private CellWorld world;

    /// <summary>
    /// Scans the zone for movable cells and removes them.
    /// Returns count of cells removed per material type.
    /// </summary>
    public Dictionary<byte, int> CollectCells()
    {
        var collected = new Dictionary<byte, int>();

        for (int y = cellBounds.yMin; y < cellBounds.yMax; y++)
        {
            for (int x = cellBounds.xMin; x < cellBounds.xMax; x++)
            {
                byte materialId = world.GetCell(x, y);

                // Skip air and static materials
                if (materialId == Materials.Air) continue;
                MaterialDef mat = world.materials[materialId];
                if (mat.behaviour == BehaviourType.Static) continue;

                // Skip cluster-owned cells
                int index = y * world.width + x;
                if (world.cells[index].ownerId != 0) continue;

                // Collect and remove
                if (!collected.ContainsKey(materialId))
                    collected[materialId] = 0;
                collected[materialId]++;

                world.SetCell(x, y, Materials.Air);
            }
        }

        return collected;
    }
}
```

### World-to-Cell Coordinate Conversion

The simulation uses a coordinate system where:
- Cell (0, 0) is top-left
- Y increases downward
- Each cell = 2x2 world units

```csharp
public static class CoordinateHelper
{
    public static Vector2Int WorldToCell(Vector2 worldPos, int worldWidth, int worldHeight)
    {
        // World coords: X from -worldWidth to +worldWidth, Y from -worldHeight to +worldHeight
        // Cell coords: X from 0 to worldWidth-1, Y from 0 to worldHeight-1 (Y=0 at top)
        int cellX = Mathf.FloorToInt((worldPos.x + worldWidth) / 2f);
        int cellY = Mathf.FloorToInt((worldHeight - worldPos.y) / 2f);
        return new Vector2Int(cellX, cellY);
    }

    public static Vector2 CellToWorld(int cellX, int cellY, int worldWidth, int worldHeight)
    {
        float worldX = cellX * 2f - worldWidth;
        float worldY = worldHeight - cellY * 2f;
        return new Vector2(worldX, worldY);
    }
}
```

---

## Data Structures

### Ability Enum

```csharp
namespace FallingSand
{
    /// <summary>
    /// Unlockable player abilities. Can be used as flags if needed.
    /// </summary>
    public enum Ability
    {
        None = 0,
        PlaceBelts = 1,
        PlaceLifts = 2,
        PlaceFurnace = 3,
        // Future abilities...
    }
}
```

### ObjectiveData

```csharp
namespace FallingSand
{
    /// <summary>
    /// Defines a collection objective with target material and count.
    /// </summary>
    [System.Serializable]
    public struct ObjectiveData
    {
        public byte targetMaterial;      // Material ID to collect (e.g., Dirt)
        public int requiredCount;        // How many to collect
        public Ability rewardAbility;    // Ability unlocked on completion
        public string displayName;       // For UI (e.g., "Collect Dirt")
    }
}
```

### ProgressionManager

```csharp
namespace FallingSand
{
    public class ProgressionManager : MonoBehaviour
    {
        // Singleton access
        public static ProgressionManager Instance { get; private set; }

        // Events for UI and game logic
        public event System.Action<byte, int, int> OnMaterialCollected;  // materialId, newCount, required
        public event System.Action<Ability> OnAbilityUnlocked;
        public event System.Action<ObjectiveData> OnObjectiveCompleted;

        // Current state
        private HashSet<Ability> unlockedAbilities = new HashSet<Ability>();
        private Dictionary<byte, int> collectedCounts = new Dictionary<byte, int>();

        // Active objectives
        private List<ObjectiveData> activeObjectives = new List<ObjectiveData>();

        /// <summary>
        /// Check if an ability is unlocked.
        /// </summary>
        public bool IsUnlocked(Ability ability)
        {
            return unlockedAbilities.Contains(ability);
        }

        /// <summary>
        /// Record collected materials and check for objective completion.
        /// </summary>
        public void RecordCollection(Dictionary<byte, int> collected)
        {
            foreach (var kvp in collected)
            {
                byte materialId = kvp.Key;
                int count = kvp.Value;

                if (!collectedCounts.ContainsKey(materialId))
                    collectedCounts[materialId] = 0;

                collectedCounts[materialId] += count;

                // Find matching objectives
                foreach (var objective in activeObjectives)
                {
                    if (objective.targetMaterial == materialId)
                    {
                        int current = collectedCounts[materialId];
                        OnMaterialCollected?.Invoke(materialId, current, objective.requiredCount);

                        if (current >= objective.requiredCount)
                        {
                            CompleteObjective(objective);
                        }
                    }
                }
            }
        }

        private void CompleteObjective(ObjectiveData objective)
        {
            if (objective.rewardAbility != Ability.None)
            {
                UnlockAbility(objective.rewardAbility);
            }
            OnObjectiveCompleted?.Invoke(objective);
            activeObjectives.Remove(objective);
        }

        private void UnlockAbility(Ability ability)
        {
            if (unlockedAbilities.Add(ability))
            {
                OnAbilityUnlocked?.Invoke(ability);
                Debug.Log($"[Progression] Unlocked ability: {ability}");
            }
        }

        /// <summary>
        /// Get current collected count for a material.
        /// </summary>
        public int GetCollectedCount(byte materialId)
        {
            return collectedCounts.TryGetValue(materialId, out int count) ? count : 0;
        }

        /// <summary>
        /// Register an objective to track.
        /// </summary>
        public void AddObjective(ObjectiveData objective)
        {
            activeObjectives.Add(objective);
        }
    }
}
```

---

## Bucket Implementation

### Bucket Component

```csharp
namespace FallingSand
{
    /// <summary>
    /// A static world object that collects falling cells.
    /// Renders as a visual bucket shape and has an internal collection zone.
    /// </summary>
    public class Bucket : MonoBehaviour
    {
        [Header("Bucket Settings")]
        [SerializeField] private int widthInCells = 16;    // Interior width
        [SerializeField] private int depthInCells = 12;    // Interior depth
        [SerializeField] private int wallThickness = 2;    // Wall thickness in cells

        [Header("Visual")]
        [SerializeField] private Color bucketColor = new Color(0.4f, 0.35f, 0.3f);

        private CollectionZone collectionZone;
        private CellWorld world;

        public void Initialize(CellWorld world, Vector2Int cellPosition)
        {
            this.world = world;

            // Calculate interior bounds (collection zone)
            int interiorX = cellPosition.x + wallThickness;
            int interiorY = cellPosition.y + wallThickness;

            RectInt interiorBounds = new RectInt(
                interiorX,
                interiorY,
                widthInCells,
                depthInCells
            );

            collectionZone = new CollectionZone(world, interiorBounds);

            // Create bucket walls using Stone material
            CreateBucketWalls(cellPosition);

            // Position the Unity transform for rendering
            PositionTransform(cellPosition);
        }

        private void CreateBucketWalls(Vector2Int pos)
        {
            int totalWidth = widthInCells + wallThickness * 2;
            int totalHeight = depthInCells + wallThickness;  // No top wall

            // Left wall
            for (int y = 0; y < totalHeight; y++)
            {
                for (int x = 0; x < wallThickness; x++)
                {
                    world.SetCell(pos.x + x, pos.y + y, Materials.Stone);
                }
            }

            // Right wall
            for (int y = 0; y < totalHeight; y++)
            {
                for (int x = 0; x < wallThickness; x++)
                {
                    world.SetCell(pos.x + wallThickness + widthInCells + x, pos.y + y, Materials.Stone);
                }
            }

            // Bottom wall
            for (int x = 0; x < totalWidth; x++)
            {
                for (int y = 0; y < wallThickness; y++)
                {
                    world.SetCell(pos.x + x, pos.y + depthInCells + wallThickness + y, Materials.Stone);
                }
            }
        }

        private void Update()
        {
            // Collect cells each frame
            var collected = collectionZone.CollectCells();

            if (collected.Count > 0)
            {
                ProgressionManager.Instance?.RecordCollection(collected);
            }
        }
    }
}
```

### Collection Zone Position

The collection zone covers the interior of the bucket, typically a few cells above the bottom. This creates the effect of cells falling in and being collected:

```
     ___________      <- Opening (no wall)
    |           |
    | ZONE HERE | <- CollectionZone scans this area
    |           |
    |___________|    <- Bottom wall
```

When cells enter the zone, they're removed and counted. The zone should be positioned slightly above the bottom so cells have time to "fall in" before being collected.

---

## UI System

### Simple OnGUI Approach

For rapid prototyping, use Unity's immediate-mode GUI:

```csharp
namespace FallingSand
{
    public class ProgressionUI : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float notificationDuration = 3f;

        private string currentNotification = "";
        private float notificationTimer = 0f;

        // Current display state
        private byte displayMaterial = 0;
        private int displayCurrent = 0;
        private int displayRequired = 0;
        private bool hasActiveObjective = false;

        private void Start()
        {
            var progression = ProgressionManager.Instance;
            if (progression != null)
            {
                progression.OnMaterialCollected += HandleMaterialCollected;
                progression.OnAbilityUnlocked += HandleAbilityUnlocked;
            }
        }

        private void HandleMaterialCollected(byte materialId, int current, int required)
        {
            displayMaterial = materialId;
            displayCurrent = current;
            displayRequired = required;
            hasActiveObjective = true;
        }

        private void HandleAbilityUnlocked(Ability ability)
        {
            currentNotification = GetUnlockMessage(ability);
            notificationTimer = notificationDuration;
        }

        private string GetUnlockMessage(Ability ability)
        {
            return ability switch
            {
                Ability.PlaceBelts => "BELTS UNLOCKED! Press B to place belts.",
                Ability.PlaceLifts => "LIFTS UNLOCKED! Press L to place lifts.",
                _ => $"{ability} UNLOCKED!"
            };
        }

        private void Update()
        {
            if (notificationTimer > 0)
                notificationTimer -= Time.deltaTime;
        }

        private void OnGUI()
        {
            // Progress display (top-right corner)
            if (hasActiveObjective)
            {
                string materialName = GetMaterialName(displayMaterial);
                string progressText = $"{materialName}: {displayCurrent}/{displayRequired}";

                GUIStyle progressStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 24,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperRight
                };
                progressStyle.normal.textColor = Color.white;

                Rect progressRect = new Rect(Screen.width - 220, 20, 200, 40);
                GUI.Label(progressRect, progressText, progressStyle);

                // Progress bar
                Rect barBg = new Rect(Screen.width - 220, 60, 200, 20);
                GUI.Box(barBg, "");

                float progress = Mathf.Clamp01((float)displayCurrent / displayRequired);
                Rect barFill = new Rect(barBg.x + 2, barBg.y + 2, (barBg.width - 4) * progress, barBg.height - 4);
                GUI.DrawTexture(barFill, Texture2D.whiteTexture);
            }

            // Unlock notification (center screen)
            if (notificationTimer > 0 && !string.IsNullOrEmpty(currentNotification))
            {
                GUIStyle notifyStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 36,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };

                // Fade out effect
                float alpha = Mathf.Min(1f, notificationTimer);
                notifyStyle.normal.textColor = new Color(1f, 1f, 0f, alpha);  // Yellow

                Rect notifyRect = new Rect(0, Screen.height / 3, Screen.width, 60);
                GUI.Label(notifyRect, currentNotification, notifyStyle);
            }
        }

        private string GetMaterialName(byte materialId)
        {
            // TODO: Add Dirt material
            return materialId switch
            {
                Materials.Sand => "Sand",
                Materials.Stone => "Stone",
                Materials.Water => "Water",
                Materials.IronOre => "Iron Ore",
                Materials.Coal => "Coal",
                _ => "Material"
            };
        }
    }
}
```

---

## Gating Belt Placement

The belt placement system (likely in PlayerController or a separate PlacementController) should check `ProgressionManager.IsUnlocked(Ability.PlaceBelts)` before allowing belt placement:

```csharp
// In placement input handling:
if (Input.GetKeyDown(KeyCode.B))
{
    if (ProgressionManager.Instance.IsUnlocked(Ability.PlaceBelts))
    {
        // Enter belt placement mode
        currentPlacementMode = PlacementMode.Belt;
    }
    else
    {
        // Optional: Show "Belts not yet unlocked" feedback
        Debug.Log("Belts not unlocked yet!");
    }
}
```

---

## Integration with GameController

```csharp
// In GameController.Start():

private void Start()
{
    // ... existing initialization ...

    // Create ProgressionManager
    GameObject progressionObj = new GameObject("ProgressionManager");
    var progressionManager = progressionObj.AddComponent<ProgressionManager>();

    // Add the first level objective
    progressionManager.AddObjective(new ObjectiveData
    {
        targetMaterial = Materials.Dirt,  // Assuming Dirt is added
        requiredCount = 20,
        rewardAbility = Ability.PlaceBelts,
        displayName = "Collect Dirt"
    });

    // Create the bucket
    CreateBucket();

    // Create progression UI
    GameObject uiObj = new GameObject("ProgressionUI");
    uiObj.AddComponent<ProgressionUI>();
}

private void CreateBucket()
{
    GameObject bucketObj = new GameObject("Bucket");
    Bucket bucket = bucketObj.AddComponent<Bucket>();

    // Position bucket in cell coordinates (e.g., bottom-center of world)
    Vector2Int bucketCellPos = new Vector2Int(worldWidth / 2 - 10, worldHeight - 80);
    bucket.Initialize(simulation.World, bucketCellPos);
}
```

---

## Adding Dirt Material

A new Dirt material needs to be added to Materials.cs:

```csharp
// In Materials.cs:
public const byte Dirt = 17;  // Next available ID after BeltRightLight

// In CreateDefaults():
defs[Dirt] = new MaterialDef
{
    density = 140,              // Slightly heavier than sand
    friction = 30,              // More friction than sand
    behaviour = BehaviourType.Powder,
    flags = MaterialFlags.None,
    baseColour = new Color32(101, 67, 33, 255),  // Brown
    colourVariation = 20,
};
```

---

## Implementation Order

### Phase 1: Core Infrastructure
1. Add `Ability` enum
2. Add `ObjectiveData` struct
3. Create `ProgressionManager` with events and state tracking
4. Add Dirt material to Materials.cs

### Phase 2: Collection System
5. Create `CollectionZone` helper class
6. Create `Bucket` MonoBehaviour
7. Integrate bucket with world (creates walls)
8. Test: Bucket appears in world, walls are solid

### Phase 3: Collection Logic
9. Implement cell scanning in CollectionZone
10. Hook bucket to ProgressionManager
11. Test: Dropping sand/dirt into bucket increments count

### Phase 4: UI
12. Create `ProgressionUI` with OnGUI
13. Display progress counter
14. Display unlock notification
15. Test: UI updates as materials collected

### Phase 5: Ability Gating
16. Add ability check to belt placement
17. Test: Can't place belts until objective complete
18. Test: Full flow - fill bucket, unlock belts, place belts

---

## Edge Cases

1. **Cluster cells in bucket**: Skip cells with `ownerId != 0` (rigid body owned)
2. **Static materials**: Skip static materials (Stone, belt tiles)
3. **Empty bucket**: No collection events fire, UI shows 0/N
4. **Overflow**: Once objective complete, continue collecting (future objectives may use same material)
5. **Multiple objectives**: Support multiple active objectives for same material
6. **Save/Load**: ProgressionManager state should be serializable (future consideration)

---

## Future Considerations

- **Multiple buckets**: Each bucket could have its own objective or feed into shared progression
- **Different collection modes**: Some buckets might only accept specific materials
- **Bucket capacity**: Visual fill level, overflow behavior
- **Sound effects**: Collection sounds, unlock fanfares
- **Particle effects**: Sparkles when cells are collected, celebration on unlock

---

## Design Principles Applied

- **Systems not patches**: ProgressionManager is the single system for all progression logic
- **Single source of truth**: Unlock state lives only in ProgressionManager
- **No special cases**: Any world object can use CollectionZone to detect cells
- **Events for decoupling**: UI observes ProgressionManager via events, no direct coupling
- **Game layer separation**: All code in Assets/Scripts/Game/, simulation layer unchanged
