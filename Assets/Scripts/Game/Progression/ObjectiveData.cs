namespace FallingSand
{
    /// <summary>
    /// Defines a collection objective with target material and count.
    /// Objectives track material collection and unlock abilities when completed.
    /// Supports sequential objectives via prerequisiteId.
    /// </summary>
    [System.Serializable]
    public struct ObjectiveData
    {
        /// <summary>
        /// Material ID to collect (e.g., Materials.Dirt)
        /// </summary>
        public byte targetMaterial;

        /// <summary>
        /// How many cells of the target material to collect
        /// </summary>
        public int requiredCount;

        /// <summary>
        /// Ability unlocked on completion (Ability.None if no unlock)
        /// </summary>
        public Ability rewardAbility;

        /// <summary>
        /// Display name for UI (e.g., "Collect Dirt")
        /// </summary>
        public string displayName;

        /// <summary>
        /// Unique identifier for this objective (e.g., "level1", "level2").
        /// Used for prerequisite tracking and bucket association.
        /// </summary>
        public string objectiveId;

        /// <summary>
        /// ID of the objective that must be completed before this one activates.
        /// Empty string means no prerequisite (starts active immediately).
        /// </summary>
        public string prerequisiteId;

        /// <summary>
        /// Creates a new objective with sequencing support.
        /// </summary>
        public ObjectiveData(byte targetMaterial, int requiredCount, Ability rewardAbility,
                            string displayName, string objectiveId, string prerequisiteId)
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
