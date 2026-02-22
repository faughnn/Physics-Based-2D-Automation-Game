using System.Collections.Generic;
using UnityEngine;

namespace FallingSand
{
    /// <summary>
    /// Static data for the Crushing Test Level - 960x810 world.
    ///
    /// Stone clusters are embedded in diggable ground. The player digs them out,
    /// builds crushers (pistons + walls), and deposits crushed pieces into a bucket.
    /// All structure abilities are unlocked from the start.
    /// </summary>
    public static class CrushingLevelData
    {
        public const int WorldWidth = 960;
        public const int WorldHeight = 810;

        public static LevelData Create()
        {
            return new LevelData
            {
                TerrainRegions = CreateTerrainRegions(),

                // Player spawns above ground surface
                PlayerSpawn = new Vector2Int(100, 340),

                // Shovel spawns nearby on same surface
                ShovelSpawn = new Vector2Int(180, 356),

                // Single bucket on stone bedrock near bottom
                BucketSpawns = new List<Vector2Int>
                {
                    new Vector2Int(420, 706),
                },

                Objectives = new List<ObjectiveData>
                {
                    new ObjectiveData(
                        targetMaterial: Materials.Stone,
                        requiredCount: 1500,
                        rewardAbility: Ability.None,
                        displayName: "Crush and collect stone clusters",
                        objectiveId: "crush1",
                        prerequisiteId: ""
                    )
                },

                ClusterSpawns = CreateClusterSpawns(),
                UnlockAllAbilities = true,
            };
        }

        private static List<TerrainRegion> CreateTerrainRegions()
        {
            var regions = new List<TerrainRegion>();

            // === Diggable Ground band (Y=360 to Y=719) ===
            regions.Add(new TerrainRegion(
                minX: 0,
                maxX: 959,
                minY: 360,
                maxY: 719,
                materialId: Materials.Ground
            ));

            // === Stone bedrock (Y=720 to Y=809) ===
            regions.Add(new TerrainRegion(
                minX: 0,
                maxX: 959,
                minY: 720,
                maxY: 809,
                materialId: Materials.Stone
            ));

            // === Stone sub-regions for clusters (overwrite Ground where clusters will be) ===
            // These get extracted into physics clusters by LevelLoader

            // Small cluster 1 - shallow
            regions.Add(new TerrainRegion(150, 159, 430, 439, Materials.Stone));
            // Small cluster 2 - medium depth
            regions.Add(new TerrainRegion(300, 309, 480, 489, Materials.Stone));
            // Small cluster 3 - shallow
            regions.Add(new TerrainRegion(700, 709, 450, 459, Materials.Stone));
            // Medium cluster 1 - shallow
            regions.Add(new TerrainRegion(450, 469, 400, 414, Materials.Stone));
            // Medium cluster 2 - medium depth
            regions.Add(new TerrainRegion(600, 619, 500, 514, Materials.Stone));
            // Large cluster - deep
            regions.Add(new TerrainRegion(200, 229, 550, 574, Materials.Stone));

            return regions;
        }

        private static List<ClusterSpawnRegion> CreateClusterSpawns()
        {
            return new List<ClusterSpawnRegion>
            {
                // Small clusters (10x10)
                new ClusterSpawnRegion(150, 430, 10, 10),
                new ClusterSpawnRegion(300, 480, 10, 10),
                new ClusterSpawnRegion(700, 450, 10, 10),
                // Medium clusters (20x15)
                new ClusterSpawnRegion(450, 400, 20, 15),
                new ClusterSpawnRegion(600, 500, 20, 15),
                // Large cluster (30x25)
                new ClusterSpawnRegion(200, 550, 30, 25),
            };
        }
    }
}
