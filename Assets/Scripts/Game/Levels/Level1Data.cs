using System.Collections.Generic;
using UnityEngine;

namespace FallingSand
{
    /// <summary>
    /// Static data for Level 1 - the tutorial level.
    /// Defines terrain layout, spawn positions, and multi-stage objectives.
    ///
    /// Three-stage progression:
    /// - Stage 1: Fill bucket 1 with 500 dirt (manual) -> Unlock Belts
    /// - Stage 2: Fill bucket 2 with 2000 dirt (use belts) -> Unlock Lifts
    /// - Stage 3: Fill bucket 3 with 5000 dirt (use belts + lifts) -> Tutorial Complete
    /// </summary>
    public static class Level1Data
    {
        /// <summary>
        /// Creates the level data for Level 1.
        /// </summary>
        /// <param name="worldWidth">World width in cells</param>
        /// <param name="worldHeight">World height in cells</param>
        public static LevelData Create(int worldWidth, int worldHeight)
        {
            // Ground surface: bottom 1/3 of screen
            // Cell Y=0 is TOP, Y increases downward
            int groundSurfaceY = worldHeight - (worldHeight / 3);

            return new LevelData
            {
                TerrainRegions = new List<TerrainRegion>
                {
                    // Main ground layer (diggable static terrain)
                    new TerrainRegion(
                        minX: 0,
                        maxX: worldWidth - 1,
                        minY: groundSurfaceY,
                        maxY: worldHeight - 1,
                        materialId: Materials.Ground
                    )
                },

                // Player spawns centered, above ground (will fall onto surface)
                PlayerSpawn = new Vector2Int(worldWidth / 2, groundSurfaceY - 20),

                // Shovel spawns to the right of player, just above ground
                ShovelSpawn = new Vector2Int(worldWidth / 2 + 100, groundSurfaceY - 8),

                // Three buckets for three-stage progression
                BucketSpawns = new List<Vector2Int>
                {
                    new Vector2Int(150, groundSurfaceY - 14),              // Bucket 1: Near player, ground level
                    new Vector2Int(300, groundSurfaceY - 60),              // Bucket 2: Higher, needs belt to reach
                    new Vector2Int(500, groundSurfaceY - 100),             // Bucket 3: Even higher, needs lift
                },

                // Three sequential objectives with INDEPENDENT counters
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
}
