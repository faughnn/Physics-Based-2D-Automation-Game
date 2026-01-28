using UnityEngine;

namespace FallingSand.Debugging
{
    /// <summary>
    /// Debug section for game-specific state: player position, objectives, equipped tool, abilities.
    /// </summary>
    public class GameDebugSection : DebugSectionBase
    {
        public override string SectionName => "Game";
        public override int Priority => 20; // After Simulation (10)

        private readonly PlayerController playerController;
        private readonly int worldWidth;
        private readonly int worldHeight;

        // Cached values
        private Vector2Int cachedCellPos;
        private string cachedObjectiveText;
        private string cachedEquippedTool;
        private string cachedAbilities;

        public GameDebugSection(PlayerController playerController, int worldWidth, int worldHeight)
        {
            this.playerController = playerController;
            this.worldWidth = worldWidth;
            this.worldHeight = worldHeight;
        }

        protected override void UpdateCachedValues()
        {
            // Player position in cell coords
            if (playerController != null)
            {
                Vector2 worldPos = playerController.transform.position;
                cachedCellPos = CoordinateUtils.WorldToCell(worldPos, worldWidth, worldHeight);
                cachedEquippedTool = playerController.EquippedTool.ToString();
            }

            // Current objective
            var pm = ProgressionManager.Instance;
            if (pm != null)
            {
                var objective = pm.GetCurrentObjective();
                if (objective.HasValue)
                {
                    var obj = objective.Value;
                    int collected = pm.GetCollectedCount(obj.objectiveId);
                    cachedObjectiveText = $"{obj.displayName}: {collected}/{obj.requiredCount}";
                }
                else
                {
                    cachedObjectiveText = "None";
                }

                // Unlocked abilities
                var abilities = new System.Text.StringBuilder();
                if (pm.IsUnlocked(Ability.PlaceBelts)) abilities.Append("Belts ");
                if (pm.IsUnlocked(Ability.PlaceLifts)) abilities.Append("Lifts ");
                if (pm.IsUnlocked(Ability.PlaceFurnace)) abilities.Append("Furnace ");
                cachedAbilities = abilities.Length > 0 ? abilities.ToString().TrimEnd() : "None";
            }
        }

        public override int DrawGUI(GUIStyle labelStyle, float x, float y, float lineHeight)
        {
            if (labelStyle == null) return 4;

            float width = 260f;
            int lines = 0;

            // Player cell position
            DrawLabel($"Player: ({cachedCellPos.x}, {cachedCellPos.y})",
                x, y + lines * lineHeight, width, lineHeight, labelStyle, Color.white);
            lines++;

            // Equipped tool
            DrawLabel($"Equipped: {cachedEquippedTool}",
                x, y + lines * lineHeight, width, lineHeight, labelStyle, Color.white);
            lines++;

            // Current objective
            DrawLabel($"Objective: {cachedObjectiveText}",
                x, y + lines * lineHeight, width, lineHeight, labelStyle, Color.white);
            lines++;

            // Unlocked abilities
            DrawLabel($"Abilities: {cachedAbilities}",
                x, y + lines * lineHeight, width, lineHeight, labelStyle, Color.cyan);
            lines++;

            return lines;
        }
    }
}
