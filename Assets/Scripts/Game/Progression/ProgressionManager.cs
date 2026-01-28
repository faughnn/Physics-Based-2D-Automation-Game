using System;
using System.Collections.Generic;
using UnityEngine;

namespace FallingSand
{
    /// <summary>
    /// Central manager for all progression state: unlocked abilities, tracked objectives,
    /// and collected material counts. Single source of truth for progression.
    ///
    /// Supports multi-objective progression with sequential activation via prerequisites.
    /// Each objective has its own independent collection counter (per-bucket counting).
    /// </summary>
    public class ProgressionManager : MonoBehaviour
    {
        // Singleton access
        public static ProgressionManager Instance { get; private set; }

        /// <summary>
        /// Fired when materials are collected for an objective.
        /// Args: materialId, currentCount, requiredCount, objectiveId
        /// </summary>
        public event Action<byte, int, int, string> OnMaterialCollected;

        /// <summary>
        /// Fired when an ability is unlocked. Arg: the unlocked ability
        /// </summary>
        public event Action<Ability> OnAbilityUnlocked;

        /// <summary>
        /// Fired when an objective is completed. Arg: the completed objective
        /// </summary>
        public event Action<ObjectiveData> OnObjectiveCompleted;

        /// <summary>
        /// Fired when an objective becomes active (bucket should activate).
        /// Arg: objectiveId
        /// </summary>
        public event Action<string> OnObjectiveActivated;

        // Current state
        private HashSet<Ability> unlockedAbilities = new HashSet<Ability>();

        // Per-objective collection counts (keyed by objectiveId, not materialId)
        private Dictionary<string, int> collectedCounts = new Dictionary<string, int>();

        // All registered objectives (both pending and active)
        private Dictionary<string, ObjectiveData> allObjectives = new Dictionary<string, ObjectiveData>();

        // Completed objective IDs (for prerequisite checking)
        private HashSet<string> completedObjectiveIds = new HashSet<string>();

        // Active objectives (only objectives with met prerequisites)
        private List<ObjectiveData> activeObjectives = new List<ObjectiveData>();

        // Read-only access to active objectives
        public IReadOnlyList<ObjectiveData> ActiveObjectives => activeObjectives;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[ProgressionManager] Duplicate instance detected, destroying this one");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Check if an ability is unlocked.
        /// </summary>
        public bool IsUnlocked(Ability ability)
        {
            return unlockedAbilities.Contains(ability);
        }

        /// <summary>
        /// Directly unlock an ability (for testing or scripted events).
        /// </summary>
        public void ForceUnlock(Ability ability)
        {
            UnlockAbility(ability);
        }

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

            // Initialize counter for this objective
            if (!string.IsNullOrEmpty(objective.objectiveId))
            {
                collectedCounts[objective.objectiveId] = 0;
            }

            // Fire activation event for bucket to respond to
            if (!string.IsNullOrEmpty(objective.objectiveId))
            {
                OnObjectiveActivated?.Invoke(objective.objectiveId);
            }
        }

        /// <summary>
        /// Record collected materials for a specific objective.
        /// Each bucket/objective has its own independent counter.
        /// </summary>
        /// <param name="collected">Materials collected (keyed by materialId)</param>
        /// <param name="objectiveId">The objective to credit</param>
        public void RecordCollection(Dictionary<byte, int> collected, string objectiveId)
        {
            // Find the objective for this bucket
            int objectiveIndex = activeObjectives.FindIndex(o => o.objectiveId == objectiveId);
            if (objectiveIndex < 0) return; // Objective not active

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
                    return; // Objective completed, stop processing
                }
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

                // Skip if already active
                bool isActive = false;
                for (int i = 0; i < activeObjectives.Count; i++)
                {
                    if (activeObjectives[i].objectiveId == objective.objectiveId)
                    {
                        isActive = true;
                        break;
                    }
                }
                if (isActive) continue;

                // Skip if already completed
                if (completedObjectiveIds.Contains(objective.objectiveId))
                    continue;

                // Check if we can now activate
                if (CanActivateObjective(objective))
                {
                    ActivateObjective(objective);
                }
            }
        }

        private void UnlockAbility(Ability ability)
        {
            if (unlockedAbilities.Add(ability))
            {
                OnAbilityUnlocked?.Invoke(ability);
            }
        }

        /// <summary>
        /// Check if an objective is currently active (collecting).
        /// </summary>
        public bool IsObjectiveActive(string objectiveId)
        {
            for (int i = 0; i < activeObjectives.Count; i++)
            {
                if (activeObjectives[i].objectiveId == objectiveId)
                    return true;
            }
            return false;
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

        /// <summary>
        /// Get current collected count for an objective.
        /// </summary>
        public int GetCollectedCount(string objectiveId)
        {
            return collectedCounts.TryGetValue(objectiveId, out int count) ? count : 0;
        }

        /// <summary>
        /// Clear all objectives and collected counts (for level reset).
        /// </summary>
        public void Reset()
        {
            activeObjectives.Clear();
            allObjectives.Clear();
            completedObjectiveIds.Clear();
            collectedCounts.Clear();
            // Note: unlocked abilities are NOT cleared - they persist across levels
        }

        /// <summary>
        /// Clear all state including unlocked abilities (for full game reset).
        /// </summary>
        public void ResetAll()
        {
            Reset();
            unlockedAbilities.Clear();
        }
    }
}
