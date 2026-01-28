# Multi-Objective Progression System

**STATUS: IMPLEMENTED**

---

## Review Notes

**Reviewed:** 2026-01-26
**Reviewer:** Claude Code Agent (First Pass)

### Files Verified to Exist
- `ProgressionManager.cs` - EXISTS at `G:\Sandy\Assets\Scripts\Game\Progression\ProgressionManager.cs`
- `ObjectiveData.cs` - EXISTS at `G:\Sandy\Assets\Scripts\Game\Progression\ObjectiveData.cs`
- `Ability.cs` - EXISTS at `G:\Sandy\Assets\Scripts\Game\Progression\Ability.cs`
- `Bucket.cs` - EXISTS at `G:\Sandy\Assets\Scripts\Game\WorldObjects\Bucket.cs`
- `LevelData.cs` - EXISTS at `G:\Sandy\Assets\Scripts\Game\Levels\LevelData.cs`
- `Level1Data.cs` - EXISTS at `G:\Sandy\Assets\Scripts\Game\Levels\Level1Data.cs`
- `GameController.cs` - EXISTS at `G:\Sandy\Assets\Scripts\Game\GameController.cs`

### API Verification

**ProgressionManager (Current State):**
- Has events: `OnMaterialCollected`, `OnAbilityUnlocked`, `OnObjectiveCompleted` - CONFIRMED
- Has `IsUnlocked(Ability)` method - CONFIRMED
- Has `AddObjective(ObjectiveData)` method - CONFIRMED
- Has `RecordCollection(Dictionary<byte, int>)` method - CONFIRMED
- Has `activeObjectives` list (private) - CONFIRMED
- MISSING: `OnObjectiveActivated` event (to be added per plan)
- MISSING: `allObjectives` dictionary, `completedObjectiveIds` set (to be added)
- MISSING: `objectiveId`/`prerequisiteId` support in ObjectiveData (to be added)

**ObjectiveData (Current State):**
- Has fields: `targetMaterial`, `requiredCount`, `rewardAbility`, `displayName` - CONFIRMED
- Constructor takes 4 parameters - CONFIRMED
- MISSING: `objectiveId` and `prerequisiteId` fields (to be added per plan)

**Ability enum (Current State):**
- Has: `None=0`, `PlaceBelts=1`, `PlaceLifts=2`, `PlaceFurnace=3` - CONFIRMED
- Sufficient for multi-objective design - OK

**Bucket (Current State):**
- Has `CollectionEnabled` property - CONFIRMED
- Has `Initialize(CellWorld, Vector2Int)` method - CONFIRMED (plan proposes extended signature)
- Has `SetObjective(ObjectiveData)` method - CONFIRMED
- Has `glowRenderer` and `remainingText` fields - CONFIRMED
- Subscribes to `OnObjectiveCompleted` and `OnMaterialCollected` - CONFIRMED
- MISSING: `objectiveId` field, inactive state support (to be added)

**LevelData (Current State):**
- Has: `TerrainRegions`, `PlayerSpawn`, `ShovelSpawn`, `BucketSpawn` (single), `Objective` (single) - CONFIRMED
- MISSING: `BucketSpawns` list, `Objectives` list (to be added per plan)

**Level1Data (Current State):**
- Uses single `BucketSpawn` and `Objective` - CONFIRMED
- Currently returns single-objective LevelData - will need update

### Progression Flow Verification

The plan specifies:
1. Level 1: Dirt -> Unlock Belts
2. Level 2: Dirt (with belts) -> Unlock Lifts
3. Level 3: Dirt (with belts+lifts) -> Tutorial complete

This matches:
- `Ability.PlaceBelts = 1` - exists
- `Ability.PlaceLifts = 2` - exists
- `Ability.None = 0` for final stage - correct pattern

### Consistency with Other Plans

**06-CameraFollowSystem.md:**
- References world dimensions 1920x1620 (vs current 1024x512) - OK, camera plan updates this
- No conflicts with multi-objective progression

**08-TutorialMapLayout.md:**
- References 3 buckets with different objectives - CONSISTENT with this plan
- Uses `ObjectiveData` without `objectiveId`/`prerequisiteId` fields - inconsistency found
- Uses `List<BucketSpawn>` structure instead of parallel `BucketSpawns`/`Objectives` lists - design difference

### Inconsistencies Found and Corrections Made

1. **CORRECTED:** Plan 08 (TutorialMapLayout) uses a `BucketSpawn` struct with embedded `ObjectiveData`, but this plan uses separate `BucketSpawns` and `Objectives` lists. Both designs are valid but should be consistent. The `BucketSpawn` struct approach (Plan 08) is cleaner. Updated this plan to note both are valid approaches.

2. **VERIFIED:** The existing `Bucket.Initialize()` signature takes `(CellWorld, Vector2Int)` but plan proposes extending to include `objectiveId` and `startsInactive`. This is an extension, not breaking change - OK.

3. **VERIFIED:** `Materials.Dirt` constant exists (byte value 17) - OK

### Changes Made
1. Updated to use per-bucket counting (each bucket has independent counter)
2. Set objective counts to 500/2000/5000 to encourage automation progression
3. Added design note about alternative `BucketSpawn` struct approach from Plan 08
4. Fixed Bucket event handlers to check objectiveId before responding

### Design Considerations to Address

**Material Count Tracking:** ~~The current design uses a global `collectedCounts` dictionary per material type.~~

**UPDATED:** Each bucket/objective has its own independent counter. Counts are tracked per-objective, not per-material:
- Bucket 1: Collect 500 Dirt (independent counter, starts at 0)
- Bucket 2: Collect 500 Dirt (independent counter, starts at 0 when activated)
- Bucket 3: Collect 500 Dirt (independent counter, starts at 0 when activated)

This means:
1. Each bucket maintains its own collection count
2. `RecordCollection` is called with an `objectiveId` to credit the correct bucket
3. `collectedCounts` dictionary is keyed by `objectiveId` instead of `materialId`
4. Each bucket needs to collect its full `requiredCount` independently

### Overall Assessment
The plan is well-designed and follows the "Systems, Not Patches" philosophy. The implementation order is logical.

---

**Second Pass (2026-01-26)** - Updated 2026-01-27

### Cross-Plan Consistency Verification

**UPDATED:** Changed to per-bucket counting (each bucket has independent counter).
Bucket counts increase to encourage automation: 500 → 2000 → 5000

| Plan | Level 1 | Level 2 | Level 3 | Verified |
|------|---------|---------|---------|----------|
| **07-MultiObjectiveProgression** | 500 Dirt → Belts | 2000 Dirt → Lifts | 5000 Dirt → Victory | YES |
| **08-TutorialMapLayout** | 500 Dirt → Belts | 2000 Dirt → Lifts | 5000 Dirt → Victory | **NEEDS UPDATE** |
| **09-Level2Setup-TheDescent** | (n/a) | 2000 Dirt → Lifts | (n/a) | **NEEDS UPDATE** |
| **10-Level3Setup-TheAscent** | (n/a) | (n/a) | 5000 Dirt → Victory | **NEEDS UPDATE** |

### Verified Consistent Items

1. **Bucket positions match across plans:**
   - Bucket 1: (300, 1336) - Plan 08
   - Bucket 2: (1300, 1436) - Plans 08, 09
   - Bucket 3: (200, 266) - Plans 08, 10

2. **Ability progression is consistent:**
   - Level 1 reward: `Ability.PlaceBelts` - All plans agree
   - Level 2 reward: `Ability.PlaceLifts` - All plans agree
   - Level 3 reward: `Ability.None` (Victory) - All plans agree

3. **Objective IDs and prerequisites are consistent:**
   - level1 → prerequisiteId: "" (starts active)
   - level2 → prerequisiteId: "level1"
   - level3 → prerequisiteId: "level2"

4. **Per-bucket counting design:**
   - Each bucket maintains its own collection counter
   - Counts are keyed by objectiveId, not materialId
   - 500/2000/5000 progression encourages automation:
     - 500: Doable by hand, teaches basic mechanics
     - 2000: Tedious by hand, encourages belt usage
     - 5000: Requires full belt+lift automation

### Recommendation

**ACTION NEEDED:** Update Plans 08, 09, and 10 to use 500/2000/5000 dirt (per-bucket counting).

---

## Summary

Extends the existing ProgressionManager to support a multi-stage tutorial with sequential objectives across 3 levels. Each level has a bucket that, when filled, unlocks a new structure placement ability AND reveals the next bucket. This creates a guided progression: fill bucket with dirt to unlock belts, fill bucket using belts to unlock lifts, fill bucket using belts+lifts to complete the tutorial.

---

## Goals

1. **Sequential Objective System**: Support chained objectives where completing one reveals/activates the next
2. **Bucket State Management**: Buckets can be inactive/hidden until their prerequisite is met
3. **Structure Gating**: Prevent placement of structures until their ability is unlocked
4. **Multi-Level Data**: Extend LevelData to support multiple buckets and objectives
5. **Event-Driven UI**: Fire events for objective completion, bucket reveal, and ability unlock
6. **Clear Visual Feedback**: Inactive buckets are visually distinct; active buckets glow

---

## Current State Analysis

### Existing ProgressionManager (`Assets/Scripts/Game/Progression/ProgressionManager.cs`)

Currently supports:
- Single-objective tracking (`activeObjectives` list)
- Material collection via `RecordCollection()`
- Ability unlock via `OnAbilityUnlocked` event
- Objective completion via `OnObjectiveCompleted` event
- `IsUnlocked(Ability)` for gating checks

**Missing for multi-objective:**
- No concept of objective ordering/prerequisites
- No bucket activation/deactivation events
- No tracking of which bucket corresponds to which objective
- All objectives are active immediately when added

### Existing Bucket (`Assets/Scripts/Game/WorldObjects/Bucket.cs`)

Currently supports:
- `CollectionEnabled` property to enable/disable collection
- Visual glow for active collection zone
- Text display showing remaining count
- Subscribes to `OnObjectiveCompleted` to disable after completion

**Missing for multi-objective:**
- No initial "inactive" state (starts enabled)
- No visual for inactive/hidden state
- No activation trigger from external events

### Existing Ability Enum (`Assets/Scripts/Game/Progression/Ability.cs`)

```csharp
public enum Ability
{
    None = 0,
    PlaceBelts = 1,
    PlaceLifts = 2,
    PlaceFurnace = 3,
}
```

**Sufficient for multi-objective** - PlaceBelts and PlaceLifts already defined.

### Existing LevelData (`Assets/Scripts/Game/Levels/LevelData.cs`)

Currently supports:
- Single `BucketSpawn` position
- Single `Objective` property

**Missing for multi-objective:**
- Multiple bucket spawn positions
- Multiple objectives with prerequisites

---

## Design

### Architecture Overview

Following the "Systems, Not Patches" philosophy, the progression system should handle:
1. Objective sequencing (prerequisite tracking)
2. Bucket state coordination (which bucket is active)
3. Structure placement gating (via existing `IsUnlocked()`)

The ProgressionManager becomes the **single source of truth** for:
- Which objectives are available (prerequisites met)
- Which objectives are active (bucket is collecting)
- Which abilities are unlocked

### New Data Structures

#### Extended ObjectiveData

```csharp
namespace FallingSand
{
    /// <summary>
    /// Extended objective data with sequencing support.
    /// </summary>
    [System.Serializable]
    public struct ObjectiveData
    {
        public byte targetMaterial;
        public int requiredCount;
        public Ability rewardAbility;
        public string displayName;

        /// <summary>
        /// Unique identifier for this objective (e.g., "level1", "level2", "level3").
        /// Used for prerequisite tracking.
        /// </summary>
        public string objectiveId;

        /// <summary>
        /// ID of the objective that must be completed before this one activates.
        /// Empty string or null means no prerequisite (starts active).
        /// </summary>
        public string prerequisiteId;

        public ObjectiveData(byte targetMaterial, int requiredCount, Ability rewardAbility,
                            string displayName, string objectiveId = "", string prerequisiteId = "")
        {
            this.targetMaterial = targetMaterial;
            this.requiredCount = requiredCount;
            this.rewardAbility = rewardAbility;
            this.displayName = displayName;
            this.objectiveId = objectiveId;
            this.prerequisiteId = prerequisiteId;
        }
    }
}
```

#### Extended LevelData

> **Design Note:** This plan uses separate `BucketSpawns` and `Objectives` lists with matching indices.
> Plan 08-TutorialMapLayout proposes a `BucketSpawn` struct that bundles position + objective together.
> Both approaches work; the bundled struct is cleaner. Choose one approach during implementation.

```csharp
namespace FallingSand
{
    public class LevelData
    {
        public List<TerrainRegion> TerrainRegions { get; set; }
        public Vector2Int PlayerSpawn { get; set; }
        public Vector2Int ShovelSpawn { get; set; }

        /// <summary>
        /// Multiple bucket spawn positions, one per objective.
        /// Index corresponds to Objectives list.
        /// </summary>
        public List<Vector2Int> BucketSpawns { get; set; } = new List<Vector2Int>();

        /// <summary>
        /// Multiple objectives for multi-stage progression.
        /// </summary>
        public List<ObjectiveData> Objectives { get; set; } = new List<ObjectiveData>();
    }
}
```

### Extended ProgressionManager

```csharp
namespace FallingSand
{
    public class ProgressionManager : MonoBehaviour
    {
        // UPDATED: Added objectiveId parameter so buckets can filter events
        // Signature: materialId, currentCount, requiredCount, objectiveId
        public event Action<byte, int, int, string> OnMaterialCollected;
        public event Action<Ability> OnAbilityUnlocked;
        public event Action<ObjectiveData> OnObjectiveCompleted;  // ObjectiveData contains objectiveId

        // NEW: Event fired when a bucket should be revealed/activated
        public event Action<string> OnObjectiveActivated;  // objectiveId

        // Existing state
        private HashSet<Ability> unlockedAbilities = new HashSet<Ability>();

        // UPDATED: Counts are now per-objective (keyed by objectiveId), not per-material
        // Each bucket has its own independent counter
        private Dictionary<string, int> collectedCounts = new Dictionary<string, int>();

        // NEW: All registered objectives (both pending and active)
        private Dictionary<string, ObjectiveData> allObjectives = new Dictionary<string, ObjectiveData>();

        // NEW: Completed objective IDs (for prerequisite checking)
        private HashSet<string> completedObjectiveIds = new HashSet<string>();

        // Active objectives (only objectives with met prerequisites)
        private List<ObjectiveData> activeObjectives = new List<ObjectiveData>();

        /// <summary>
        /// Register an objective. If prerequisites are met, activates immediately.
        /// Otherwise, waits until prerequisite completes.
        /// </summary>
        public void AddObjective(ObjectiveData objective)
        {
            // Store in all objectives
            if (!string.IsNullOrEmpty(objective.objectiveId))
            {
                allObjectives[objective.objectiveId] = objective;
            }

            // Check if we can activate now
            if (CanActivateObjective(objective))
            {
                ActivateObjective(objective);
            }
            // Otherwise it stays pending until prerequisite completes
        }

        private bool CanActivateObjective(ObjectiveData objective)
        {
            // No prerequisite = always activatable
            if (string.IsNullOrEmpty(objective.prerequisiteId))
                return true;

            // Prerequisite must be completed
            return completedObjectiveIds.Contains(objective.prerequisiteId);
        }

        private void ActivateObjective(ObjectiveData objective)
        {
            activeObjectives.Add(objective);

            // Fire activation event for bucket to respond to
            if (!string.IsNullOrEmpty(objective.objectiveId))
            {
                OnObjectiveActivated?.Invoke(objective.objectiveId);
            }
        }

        private void CompleteObjective(int index)
        {
            var objective = activeObjectives[index];

            // Mark as completed
            if (!string.IsNullOrEmpty(objective.objectiveId))
            {
                completedObjectiveIds.Add(objective.objectiveId);
            }

            // Unlock ability
            if (objective.rewardAbility != Ability.None)
            {
                UnlockAbility(objective.rewardAbility);
            }

            // Fire completion event
            OnObjectiveCompleted?.Invoke(objective);

            // Remove from active
            activeObjectives.RemoveAt(index);

            // Check for newly activatable objectives
            ActivatePendingObjectives();
        }

        private void ActivatePendingObjectives()
        {
            foreach (var kvp in allObjectives)
            {
                var objective = kvp.Value;

                // Skip if already active or completed
                if (activeObjectives.Exists(o => o.objectiveId == objective.objectiveId))
                    continue;
                if (completedObjectiveIds.Contains(objective.objectiveId))
                    continue;

                // Check if we can now activate
                if (CanActivateObjective(objective))
                {
                    ActivateObjective(objective);
                }
            }
        }

        // UPDATED: RecordCollection now uses per-objective counting
        // Each bucket/objective has its own independent counter
        public void RecordCollection(Dictionary<byte, int> collected, string objectiveId)
        {
            // Find the objective for this bucket
            int objectiveIndex = activeObjectives.FindIndex(o => o.objectiveId == objectiveId);
            if (objectiveIndex < 0) return;  // Objective not active

            var objective = activeObjectives[objectiveIndex];

            foreach (var kvp in collected)
            {
                byte materialId = kvp.Key;
                int count = kvp.Value;

                // Only count materials that match the objective's target
                if (materialId != objective.targetMaterial)
                    continue;

                // Initialize counter for this objective if needed
                if (!collectedCounts.ContainsKey(objectiveId))
                    collectedCounts[objectiveId] = 0;

                collectedCounts[objectiveId] += count;
                int current = collectedCounts[objectiveId];

                // Notify with objectiveId so only the relevant bucket updates
                OnMaterialCollected?.Invoke(materialId, current, objective.requiredCount, objectiveId);

                if (current >= objective.requiredCount)
                {
                    CompleteObjective(objectiveIndex);
                    return;  // Objective completed, stop processing
                }
            }
        }

        /// <summary>
        /// Check if an objective is currently active (collecting).
        /// </summary>
        public bool IsObjectiveActive(string objectiveId)
        {
            return activeObjectives.Exists(o => o.objectiveId == objectiveId);
        }

        /// <summary>
        /// Check if an objective has been completed.
        /// </summary>
        public bool IsObjectiveCompleted(string objectiveId)
        {
            return completedObjectiveIds.Contains(objectiveId);
        }

        /// <summary>
        /// Get the currently active objective (first in list).
        /// Returns null if no active objectives.
        /// </summary>
        public ObjectiveData? GetCurrentObjective()
        {
            return activeObjectives.Count > 0 ? activeObjectives[0] : (ObjectiveData?)null;
        }
    }
}
```

### Extended Bucket

```csharp
namespace FallingSand
{
    public class Bucket : MonoBehaviour
    {
        // Existing fields...

        /// <summary>
        /// The objective ID this bucket tracks. Used for activation events.
        /// </summary>
        private string objectiveId;

        /// <summary>
        /// Whether this bucket starts inactive (waiting for prerequisite).
        /// </summary>
        private bool startsInactive = false;

        // Inactive visual state
        private Color inactiveGlowColor = new Color(0.3f, 0.3f, 0.3f, 0.15f);  // Dim gray
        private Color inactiveTextColor = new Color(0.5f, 0.5f, 0.5f);

        /// <summary>
        /// Initialize the bucket. If startsInactive is true, bucket won't
        /// collect until its objective is activated.
        /// </summary>
        public void Initialize(CellWorld world, Vector2Int cellPosition,
                              string objectiveId = "", bool startsInactive = false)
        {
            this.objectiveId = objectiveId;
            this.startsInactive = startsInactive;

            // ... existing initialization ...

            // Start inactive if specified
            if (startsInactive)
            {
                SetInactiveVisuals();
                collectionEnabled = false;
            }

            // Subscribe to activation events
            if (ProgressionManager.Instance != null && !string.IsNullOrEmpty(objectiveId))
            {
                ProgressionManager.Instance.OnObjectiveActivated += HandleObjectiveActivated;
            }
        }

        private void HandleObjectiveActivated(string activatedId)
        {
            if (activatedId == objectiveId)
            {
                ActivateBucket();
            }
        }

        // UPDATED: Now checks objectiveId to only respond to THIS bucket's objective
        private void HandleMaterialCollected(byte materialId, int currentCount, int requiredCount, string completedObjectiveId)
        {
            // Only update display if this is OUR objective
            if (completedObjectiveId != objectiveId)
                return;

            if (materialId == targetMaterial)
            {
                targetRequired = requiredCount;
                UpdateRemainingText(currentCount);
            }
        }

        // UPDATED: Now checks objectiveId to only disable THIS bucket when ITS objective completes
        private void HandleObjectiveCompleted(ObjectiveData objective)
        {
            // Only respond if this is OUR objective
            if (objective.objectiveId != objectiveId)
                return;

            // Disable collection once objective is done
            collectionEnabled = false;

            // Fade out the glow effect
            if (glowRenderer != null)
            {
                glowRenderer.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0f);
            }

            // Play completion sound (same as shovel pickup)
            AudioManager.Instance?.PlayPickupSound();
        }

        private void ActivateBucket()
        {
            collectionEnabled = true;
            SetActiveVisuals();

            // Optional: Play activation sound/particle effect
        }

        // UPDATED: Now passes objectiveId to RecordCollection for per-bucket counting
        // Only collects cells matching targetMaterial - other materials accumulate
        private void Update()
        {
            if (!collectionEnabled || collectionZone == null)
                return;

            // Only collect cells matching our target material
            // Other materials (e.g., Sand falling into a Dirt bucket) will accumulate
            // and the player must manually remove them
            var collected = collectionZone.CollectCellsOfType(targetMaterial);

            if (collected.Count > 0)
            {
                // Pass our objectiveId so the count is credited to THIS bucket
                ProgressionManager.Instance?.RecordCollection(collected, objectiveId);
            }
        }

        private void SetInactiveVisuals()
        {
            if (glowRenderer != null)
            {
                glowRenderer.color = inactiveGlowColor;
            }
            if (remainingText != null)
            {
                remainingText.color = inactiveTextColor;
                remainingText.text = "---";  // Or hidden entirely
            }
        }

        private void SetActiveVisuals()
        {
            if (glowRenderer != null)
            {
                glowRenderer.color = glowColor;
            }
            if (remainingText != null)
            {
                remainingText.color = Color.white;
                UpdateRemainingText();
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.OnObjectiveActivated -= HandleObjectiveActivated;
                // ... existing unsubscriptions ...
            }
        }
    }
}
```

> **Notes:**
> - `HandleObjectiveCompleted` already receives `ObjectiveData` which contains `objectiveId`, so no event signature change is needed.
> - `OnMaterialCollected` needs its signature updated to include `objectiveId` - see Extended ProgressionManager section.
> - `CollectionZone` needs a new method `CollectCellsOfType(byte materialId)` that only removes and counts cells of the specified material type. Other materials remain in place and accumulate.
> - `AudioManager.Instance.PlayPickupSound()` assumes an AudioManager exists with this method (same sound used for shovel pickup).

### Structure Placement Gating

Structure placement systems should check `ProgressionManager.IsUnlocked()` before allowing placement:

```csharp
// Example: Belt placement handler (future implementation)
public class StructurePlacementController : MonoBehaviour
{
    private void Update()
    {
        // Belt placement (B key)
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (ProgressionManager.Instance.IsUnlocked(Ability.PlaceBelts))
            {
                EnterBeltPlacementMode();
            }
            else
            {
                ShowLockedFeedback("Belts not yet unlocked!");
            }
        }

        // Lift placement (L key)
        if (Input.GetKeyDown(KeyCode.L))
        {
            if (ProgressionManager.Instance.IsUnlocked(Ability.PlaceLifts))
            {
                EnterLiftPlacementMode();
            }
            else
            {
                ShowLockedFeedback("Lifts not yet unlocked!");
            }
        }
    }
}
```

---

## Multi-Level Configuration

### Level 1 Data (Updated)

```csharp
public static class Level1Data
{
    public static LevelData Create(int worldWidth, int worldHeight)
    {
        int groundSurfaceY = worldHeight - (worldHeight / 3);

        return new LevelData
        {
            TerrainRegions = new List<TerrainRegion>
            {
                new TerrainRegion(0, worldWidth - 1, groundSurfaceY, worldHeight - 1, Materials.Ground)
            },

            PlayerSpawn = new Vector2Int(worldWidth / 2, groundSurfaceY - 20),
            ShovelSpawn = new Vector2Int(worldWidth / 2 + 100, groundSurfaceY - 8),

            // Three buckets for three-stage progression
            BucketSpawns = new List<Vector2Int>
            {
                new Vector2Int(150, groundSurfaceY - 14),              // Bucket 1: Near player, ground level
                new Vector2Int(300, groundSurfaceY - 60),              // Bucket 2: Higher, needs belt to reach
                new Vector2Int(500, groundSurfaceY - 100),             // Bucket 3: Even higher, needs lift
            },

            // Three sequential objectives with INDEPENDENT counters
            // Each bucket tracks its own collection count (not cumulative)
            // Increasing counts encourage automation: manual -> belts -> belts+lifts
            Objectives = new List<ObjectiveData>
            {
                // Level 1: Fill bucket with dirt (no tools needed) -> Unlock Belts
                new ObjectiveData(
                    targetMaterial: Materials.Dirt,
                    requiredCount: 500,   // Manageable by hand
                    rewardAbility: Ability.PlaceBelts,
                    displayName: "Fill the bucket with dirt",
                    objectiveId: "level1",
                    prerequisiteId: ""  // No prerequisite, starts active
                ),

                // Level 2: Fill bucket using belts -> Unlock Lifts
                new ObjectiveData(
                    targetMaterial: Materials.Dirt,
                    requiredCount: 2000,  // Encourages using belts for efficiency
                    rewardAbility: Ability.PlaceLifts,
                    displayName: "Use belts to fill the bucket",
                    objectiveId: "level2",
                    prerequisiteId: "level1"  // Requires level1 completion
                ),

                // Level 3: Fill bucket using belts + lifts -> Tutorial Complete
                new ObjectiveData(
                    targetMaterial: Materials.Dirt,
                    requiredCount: 5000,  // Requires full automation with belts + lifts
                    rewardAbility: Ability.None,  // No new ability, just completion
                    displayName: "Use lifts and belts to fill the bucket",
                    objectiveId: "level3",
                    prerequisiteId: "level2"  // Requires level2 completion
                )
            }
        };
    }
}
```

### GameController Integration (Updated)

```csharp
private void Start()
{
    // ... existing setup ...

    // Load level data
    var levelData = Level1Data.Create(worldWidth, worldHeight);
    levelLoader.LoadLevel(levelData);

    // Register ALL objectives (ProgressionManager handles activation order)
    foreach (var objective in levelData.Objectives)
    {
        ProgressionManager.Instance.AddObjective(objective);
    }

    // Create player and shovel
    CreatePlayer(levelData.PlayerSpawn);
    CreateShovelItem(levelData.ShovelSpawn);

    // Create ALL buckets (with appropriate activation state)
    for (int i = 0; i < levelData.BucketSpawns.Count; i++)
    {
        var objective = levelData.Objectives[i];
        bool startsInactive = !string.IsNullOrEmpty(objective.prerequisiteId);

        CreateBucket(
            levelData.BucketSpawns[i],
            objective,
            startsInactive
        );
    }
}

private void CreateBucket(Vector2Int cellPosition, ObjectiveData objective, bool startsInactive)
{
    GameObject bucketObj = new GameObject($"Bucket_{objective.objectiveId}");
    Bucket bucket = bucketObj.AddComponent<Bucket>();
    bucket.Initialize(
        simulation.World,
        cellPosition,
        objective.objectiveId,
        startsInactive
    );
    bucket.SetObjective(objective);
}
```

---

## Event Flow

### Progression Flow Diagram

```
Player fills Bucket 1 (dirt by hand)
        |
        v
OnMaterialCollected (dirt, 500, 500)
        |
        v
CompleteObjective("level1")
        |
        +---> OnAbilityUnlocked(PlaceBelts)
        |         |
        |         v
        |     [Player can now place belts]
        |
        +---> OnObjectiveCompleted(level1)
        |         |
        |         v
        |     [Bucket 1 disables collection]
        |
        +---> ActivatePendingObjectives()
                  |
                  v
              OnObjectiveActivated("level2")
                  |
                  v
              [Bucket 2 activates, shows target count]
                  |
                  v
              Player uses belts to fill Bucket 2
                  |
                  v
              (repeat for level 3...)
```

### UI Updates

The existing `ProgressionUI` can be extended to show:
- Current active objective display name
- Progress bar for current objective
- Unlock notification when ability is granted
- "Tutorial Complete!" message when all objectives done

---

## Implementation Order

### Phase 1: Extend ObjectiveData

1. Add `objectiveId` field to ObjectiveData struct
2. Add `prerequisiteId` field to ObjectiveData struct
3. Update constructor to accept new fields

### Phase 2: Extend ProgressionManager

5. Add `allObjectives` dictionary for all registered objectives
6. Add `completedObjectiveIds` set for tracking completion
7. Add `OnObjectiveActivated` event
8. Implement `CanActivateObjective()` method
9. Implement `ActivateObjective()` method
10. Update `CompleteObjective()` to call `ActivatePendingObjectives()`
11. Implement `ActivatePendingObjectives()` method
12. Add `IsObjectiveActive()` query method
13. Add `IsObjectiveCompleted()` query method
14. Add `GetCurrentObjective()` query method

### Phase 3: Extend Bucket and CollectionZone

15. Add `objectiveId` field to Bucket
16. Add `startsInactive` field to Bucket
17. Add inactive visual state (dim glow, gray text)
18. Update `Initialize()` to accept new parameters
19. Subscribe to `OnObjectiveActivated` event
20. Implement `HandleObjectiveActivated()` method
21. Implement `ActivateBucket()` method
22. Implement `SetInactiveVisuals()` / `SetActiveVisuals()` methods
23. Update `OnDestroy()` to unsubscribe from new event
24. Fix `HandleMaterialCollected` to check objectiveId
25. Fix `HandleObjectiveCompleted` to check objectiveId and play completion sound
26. Add `CollectCellsOfType(byte materialId)` method to CollectionZone
27. Update `Update()` to use `CollectCellsOfType` and pass objectiveId to RecordCollection

### Phase 4: Extend LevelData

28. Change `BucketSpawn` to `BucketSpawns` list
29. Change `Objective` to `Objectives` list
30. Update `Level1Data.Create()` with multi-objective configuration (500/2000/5000 counts)

### Phase 5: Update GameController

31. Update bucket creation loop to iterate over `BucketSpawns`
32. Update objective registration to iterate over `Objectives`
33. Pass `startsInactive` flag based on prerequisite presence

### Phase 6: Structure Gating (Optional - depends on belt placement system)

34. Create `StructurePlacementController` or update existing placement code
35. Gate belt placement on `IsUnlocked(Ability.PlaceBelts)`
36. Gate lift placement on `IsUnlocked(Ability.PlaceLifts)`
37. Add visual/audio feedback for locked structures

---

## Dependencies

### Required (Already Exist)

| Component | Location | Status |
|-----------|----------|--------|
| ProgressionManager | `Assets/Scripts/Game/Progression/ProgressionManager.cs` | EXISTS |
| ObjectiveData | `Assets/Scripts/Game/Progression/ObjectiveData.cs` | EXISTS |
| Ability enum | `Assets/Scripts/Game/Progression/Ability.cs` | EXISTS |
| Bucket | `Assets/Scripts/Game/WorldObjects/Bucket.cs` | EXISTS |
| LevelData | `Assets/Scripts/Game/Levels/LevelData.cs` | EXISTS |
| Level1Data | `Assets/Scripts/Game/Levels/Level1Data.cs` | EXISTS |
| GameController | `Assets/Scripts/Game/GameController.cs` | EXISTS |

### Future Dependencies (Not Yet Implemented)

| Component | Purpose | Notes |
|-----------|---------|-------|
| Belt Placement System | Place belt structures in game mode | Must check `IsUnlocked(PlaceBelts)` |
| Lift Placement System | Place lift structures in game mode | Must check `IsUnlocked(PlaceLifts)` |
| Lift Structure | Vertical cell transport | Needs implementation (similar to BeltStructure) |

---

## Testing Checklist

### Progression Logic
- [ ] First objective (level1) starts active with no prerequisite
- [ ] Second objective (level2) starts inactive
- [ ] Third objective (level3) starts inactive
- [ ] Completing level1 fires `OnObjectiveCompleted`
- [ ] Completing level1 fires `OnAbilityUnlocked(PlaceBelts)`
- [ ] Completing level1 fires `OnObjectiveActivated("level2")`
- [ ] Completing level2 fires `OnAbilityUnlocked(PlaceLifts)`
- [ ] Completing level2 fires `OnObjectiveActivated("level3")`
- [ ] Completing level3 does NOT fire ability unlock (None)
- [ ] `IsUnlocked(PlaceBelts)` returns false before level1, true after
- [ ] `IsUnlocked(PlaceLifts)` returns false before level2, true after
- [ ] `IsObjectiveActive("level1")` returns true at start
- [ ] `IsObjectiveActive("level2")` returns false at start, true after level1

### Bucket Behavior
- [ ] Bucket 1 starts with active visuals (glow, count display)
- [ ] Bucket 2 starts with inactive visuals (dim, "---" or hidden)
- [ ] Bucket 3 starts with inactive visuals
- [ ] Bucket 1 collects cells and reports to ProgressionManager
- [ ] Bucket 2 does NOT collect cells until activated
- [ ] Bucket 2 activates when level1 completes
- [ ] Bucket 2 shows active visuals after activation
- [ ] Bucket 2 displays correct target count after activation
- [ ] Bucket 2 starts collecting after activation

### Structure Gating
- [ ] Cannot place belts before level1 completion
- [ ] Can place belts after level1 completion
- [ ] Cannot place lifts before level2 completion
- [ ] Can place lifts after level2 completion
- [ ] Appropriate feedback shown when trying to place locked structures

### Visual/Audio Feedback
- [ ] Inactive buckets are visually distinct (dim/gray)
- [ ] Bucket activation has clear visual change
- [ ] Ability unlock notification appears on screen
- [ ] Sound plays on objective completion (same as shovel pickup)
- [ ] (Optional) Sound plays on bucket activation

### Material Filtering
- [ ] Bucket only collects cells matching targetMaterial (Dirt)
- [ ] Non-matching materials (e.g., Sand) accumulate in bucket
- [ ] Accumulated non-matching materials can be removed by player
- [ ] Collection count only increases for matching material

### Edge Cases
- [ ] Objective with empty prerequisiteId starts active
- [ ] Multiple objectives with same targetMaterial track separately (per-bucket counting)
- [ ] Reset() clears objectives but preserves ability tracking
- [ ] ResetAll() clears everything including abilities

---

## Notes

### Future Considerations

1. **Save/Load**: Need to persist `completedObjectiveIds` and `unlockedAbilities` for game saves
2. **Multiple Concurrent Objectives**: Current design supports this but UI only shows first active
3. **Branch Objectives**: Could extend prerequisite system to support "complete any of X, Y, Z"
4. **Objective Descriptions**: Could add hint text like "Use belts to move dirt upward"
5. **Visual Guides**: Arrow indicators pointing to active bucket

### Architecture Notes

This design follows the project's "Systems, Not Patches" philosophy:
- ProgressionManager is the single source of truth for ALL progression state
- Buckets observe progression events rather than managing their own state
- No special-case code for individual objectives - the system handles all cases
- Structure gating uses the same `IsUnlocked()` API regardless of structure type
