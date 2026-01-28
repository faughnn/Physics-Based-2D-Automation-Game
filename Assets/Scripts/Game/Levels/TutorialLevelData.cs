using System.Collections.Generic;
using UnityEngine;

namespace FallingSand
{
    /// <summary>
    /// Static data for the Tutorial Level - full 1920x1620 world.
    ///
    /// Three-zone layout with progressive difficulty:
    /// - Zone 1: Spawn/Dig Area (manual dirt collection)
    /// - Zone 2: Stone Slope (requires belts)
    /// - Zone 3: Floating Island (requires lifts)
    ///
    /// Progression:
    /// - Stage 1: Fill bucket 1 with 100 dirt (manual) -> Unlock Belts
    /// - Stage 2: Fill bucket 2 with 500 dirt (use belts) -> Unlock Lifts
    /// - Stage 3: Fill bucket 3 with 2000 dirt (use belts + lifts) -> Victory
    /// </summary>
    public static class TutorialLevelData
    {
        public const int WorldWidth = 1920;
        public const int WorldHeight = 1620;

        /// <summary>
        /// Creates the level data for the Tutorial level.
        /// </summary>
        public static LevelData Create()
        {
            return new LevelData
            {
                TerrainRegions = CreateTerrainRegions(),

                // Player spawns left side, above ground (will fall onto surface)
                PlayerSpawn = new Vector2Int(200, 1320),

                // Shovel spawns near player on ground level
                ShovelSpawn = new Vector2Int(350, 1336),

                // Three buckets for three-stage progression
                BucketSpawns = new List<Vector2Int>
                {
                    new Vector2Int(300, 1336),   // Bucket 1: Zone 1, near spawn
                    new Vector2Int(1300, 1436),  // Bucket 2: Zone 2, in slope valley
                    new Vector2Int(200, 831),    // Bucket 3: Zone 3, on floating island
                },

                // Three sequential objectives with increasing difficulty
                Objectives = new List<ObjectiveData>
                {
                    // Level 1: Manual transport -> Unlock Belts
                    new ObjectiveData(
                        targetMaterial: Materials.Dirt,
                        requiredCount: 100,
                        rewardAbility: Ability.PlaceBelts,
                        displayName: "Fill the bucket with dirt",
                        objectiveId: "level1",
                        prerequisiteId: ""  // No prerequisite, starts active
                    ),

                    // Level 2: Belt transport -> Unlock Lifts
                    new ObjectiveData(
                        targetMaterial: Materials.Dirt,
                        requiredCount: 500,
                        rewardAbility: Ability.PlaceLifts,
                        displayName: "Use belts to fill the bucket",
                        objectiveId: "level2",
                        prerequisiteId: "level1"
                    ),

                    // Level 3: Lift + Belt transport -> Victory
                    new ObjectiveData(
                        targetMaterial: Materials.Dirt,
                        requiredCount: 2000,
                        rewardAbility: Ability.None,
                        displayName: "Use lifts and belts to fill the bucket",
                        objectiveId: "level3",
                        prerequisiteId: "level2"
                    )
                }
            };
        }

        private static List<TerrainRegion> CreateTerrainRegions()
        {
            var regions = new List<TerrainRegion>();

            // === ZONE 1: Diggable Ground (Left Side, Ground Level) ===
            // Player mines dirt here for manual transport to Bucket 1
            regions.Add(new TerrainRegion(
                minX: 0,
                maxX: 550,
                minY: 1350,
                maxY: 1619,
                materialId: Materials.Ground
            ));

            // === ZONE 2: Stone Slope (Right Side, Ground Level) ===
            // Undulating stone surface - requires belts to transport dirt to Bucket 2

            // Stone barrier between diggable ground and slope
            regions.Add(new TerrainRegion(551, 599, 1310, 1619, Materials.Stone));

            // Slope segments creating an undulating surface
            regions.Add(new TerrainRegion(600, 750, 1330, 1619, Materials.Stone));   // Segment 1 (highest)
            regions.Add(new TerrainRegion(750, 900, 1350, 1619, Materials.Stone));   // Segment 2
            regions.Add(new TerrainRegion(900, 1050, 1370, 1619, Materials.Stone));  // Segment 3
            regions.Add(new TerrainRegion(1050, 1200, 1400, 1619, Materials.Stone)); // Segment 4
            regions.Add(new TerrainRegion(1200, 1400, 1450, 1619, Materials.Stone)); // Segment 5 (valley - Bucket 2)
            regions.Add(new TerrainRegion(1400, 1550, 1380, 1619, Materials.Stone)); // Segment 6 (rise)
            regions.Add(new TerrainRegion(1550, 1700, 1340, 1619, Materials.Stone)); // Segment 7
            regions.Add(new TerrainRegion(1700, 1919, 1300, 1619, Materials.Stone)); // Segment 8 (rightmost)

            // === ZONE 3: Floating Island (Left-Center, Above Viewport) ===
            // Player must build lifts to reach this island and transport dirt to Bucket 3
            // Positioned ~80 cells above viewport at peak jump

            // Main platform base (thick stone)
            regions.Add(new TerrainRegion(100, 700, 915, 1015, Materials.Stone));

            // Surface layer (thin stone)
            regions.Add(new TerrainRegion(120, 680, 885, 914, Materials.Stone));

            // Bucket platform (raised edges for Bucket 3)
            regions.Add(new TerrainRegion(150, 250, 845, 884, Materials.Stone));

            return regions;
        }
    }
}
