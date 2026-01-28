using UnityEngine;

namespace FallingSand
{
    /// <summary>
    /// Displays progression UI using OnGUI.
    /// Shows current objective progress (top-right) and unlock notifications (center).
    /// </summary>
    public class ProgressionUI : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float notificationDuration = 3f;

        private string currentNotification = "";
        private float notificationTimer = 0f;

        // Current display state
        private string displayObjectiveId = "";
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
                progression.OnObjectiveCompleted += HandleObjectiveCompleted;
                progression.OnObjectiveActivated += HandleObjectiveActivated;

                // Check if there's already an active objective
                if (progression.ActiveObjectives.Count > 0)
                {
                    var objective = progression.ActiveObjectives[0];
                    displayObjectiveId = objective.objectiveId;
                    displayMaterial = objective.targetMaterial;
                    displayCurrent = progression.GetCollectedCount(objective.objectiveId);
                    displayRequired = objective.requiredCount;
                    hasActiveObjective = true;
                }
            }
        }

        private void OnDestroy()
        {
            var progression = ProgressionManager.Instance;
            if (progression != null)
            {
                progression.OnMaterialCollected -= HandleMaterialCollected;
                progression.OnAbilityUnlocked -= HandleAbilityUnlocked;
                progression.OnObjectiveCompleted -= HandleObjectiveCompleted;
                progression.OnObjectiveActivated -= HandleObjectiveActivated;
            }
        }

        private void HandleObjectiveActivated(string objectiveId)
        {
            // When a new objective activates, update display to show it
            var progression = ProgressionManager.Instance;
            if (progression == null) return;

            // Find the newly activated objective
            foreach (var obj in progression.ActiveObjectives)
            {
                if (obj.objectiveId == objectiveId)
                {
                    displayObjectiveId = obj.objectiveId;
                    displayMaterial = obj.targetMaterial;
                    displayCurrent = 0;
                    displayRequired = obj.requiredCount;
                    hasActiveObjective = true;
                    break;
                }
            }
        }

        private void HandleObjectiveCompleted(ObjectiveData objective)
        {
            // If the completed objective was the one we're displaying, check for next
            if (objective.objectiveId == displayObjectiveId)
            {
                var progression = ProgressionManager.Instance;
                if (progression != null && progression.ActiveObjectives.Count > 0)
                {
                    // Show next active objective
                    var nextObjective = progression.ActiveObjectives[0];
                    displayObjectiveId = nextObjective.objectiveId;
                    displayMaterial = nextObjective.targetMaterial;
                    displayCurrent = progression.GetCollectedCount(nextObjective.objectiveId);
                    displayRequired = nextObjective.requiredCount;
                }
                else
                {
                    // No more objectives
                    hasActiveObjective = false;
                }
            }
        }

        private void HandleMaterialCollected(byte materialId, int current, int required, string objectiveId)
        {
            // Only update display if this is the objective we're tracking
            if (objectiveId == displayObjectiveId)
            {
                displayMaterial = materialId;
                displayCurrent = Mathf.Min(current, required);  // Cap display at required
                displayRequired = required;
                hasActiveObjective = true;
            }
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
                Ability.PlaceFurnace => "FURNACE UNLOCKED!",
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
            DrawProgressDisplay();
            DrawUnlockNotification();
        }

        private void DrawProgressDisplay()
        {
            if (!hasActiveObjective)
                return;

            string materialName = GetMaterialName(displayMaterial);
            string progressText = $"{materialName}: {displayCurrent}/{displayRequired}";

            // Style for progress text
            GUIStyle progressStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperRight
            };
            progressStyle.normal.textColor = Color.white;

            // Draw with shadow for better readability
            Rect progressRect = new Rect(Screen.width - 220, 20, 200, 40);
            DrawTextWithShadow(progressRect, progressText, progressStyle);

            // Progress bar background
            Rect barBg = new Rect(Screen.width - 220, 60, 200, 20);
            GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            GUI.DrawTexture(barBg, Texture2D.whiteTexture);

            // Progress bar fill
            float progress = Mathf.Clamp01((float)displayCurrent / displayRequired);
            Rect barFill = new Rect(barBg.x + 2, barBg.y + 2, (barBg.width - 4) * progress, barBg.height - 4);
            GUI.color = new Color(0.3f, 0.8f, 0.3f, 1f);  // Green fill
            GUI.DrawTexture(barFill, Texture2D.whiteTexture);

            GUI.color = Color.white;
        }

        private void DrawUnlockNotification()
        {
            if (notificationTimer <= 0 || string.IsNullOrEmpty(currentNotification))
                return;

            GUIStyle notifyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 36,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            // Fade out effect
            float alpha = Mathf.Min(1f, notificationTimer);
            notifyStyle.normal.textColor = new Color(1f, 1f, 0f, alpha);  // Yellow

            Rect notifyRect = new Rect(0, Screen.height / 3f, Screen.width, 60);

            // Draw with shadow
            GUIStyle shadowStyle = new GUIStyle(notifyStyle);
            shadowStyle.normal.textColor = new Color(0f, 0f, 0f, alpha * 0.5f);
            Rect shadowRect = new Rect(notifyRect.x + 2, notifyRect.y + 2, notifyRect.width, notifyRect.height);
            GUI.Label(shadowRect, currentNotification, shadowStyle);
            GUI.Label(notifyRect, currentNotification, notifyStyle);
        }

        private void DrawTextWithShadow(Rect rect, string text, GUIStyle style)
        {
            // Draw shadow
            GUIStyle shadowStyle = new GUIStyle(style);
            shadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.7f);
            Rect shadowRect = new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height);
            GUI.Label(shadowRect, text, shadowStyle);

            // Draw main text
            GUI.Label(rect, text, style);
        }

        private string GetMaterialName(byte materialId)
        {
            return materialId switch
            {
                Materials.Sand => "Sand",
                Materials.Stone => "Stone",
                Materials.Water => "Water",
                Materials.IronOre => "Iron Ore",
                Materials.Coal => "Coal",
                Materials.Dirt => "Dirt",
                Materials.Ground => "Ground",
                _ => "Material"
            };
        }
    }
}
