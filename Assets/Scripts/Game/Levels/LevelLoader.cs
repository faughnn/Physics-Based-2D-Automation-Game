using UnityEngine;

namespace FallingSand
{
    /// <summary>
    /// Initializes the world based on level data.
    /// Uses SimulationManager APIs to set up terrain.
    /// </summary>
    public class LevelLoader
    {
        private readonly SimulationManager simulation;

        public LevelLoader(SimulationManager simulation)
        {
            this.simulation = simulation;
        }

        /// <summary>
        /// Loads level data into the world.
        /// Fills terrain regions with specified materials.
        /// </summary>
        public void LoadLevel(LevelData level)
        {
            foreach (var region in level.TerrainRegions)
            {
                FillRegion(region);
            }

            // Create clusters from spawn regions (terrain must be filled first)
            if (level.ClusterSpawns.Count > 0)
            {
                var clusterManager = simulation.ClusterManager;
                var world = simulation.World;
                var terrainColliders = simulation.TerrainColliders;

                foreach (var spawn in level.ClusterSpawns)
                {
                    var cluster = ClusterFactory.CreateClusterFromRegion(
                        world, spawn.X, spawn.Y, spawn.Width, spawn.Height, clusterManager);

                    // Force-sleep clusters created during level load so they don't jiggle
                    // against terrain colliders. They'll wake naturally when terrain is dug out.
                    if (cluster != null && cluster.rb != null)
                    {
                        cluster.rb.linearVelocity = Vector2.zero;
                        cluster.rb.angularVelocity = 0f;
                        cluster.rb.Sleep();
                    }

                    // Mark affected chunks dirty for terrain collider rebuild
                    for (int y = spawn.Y; y < spawn.Y + spawn.Height; y++)
                    {
                        for (int x = spawn.X; x < spawn.X + spawn.Width; x++)
                        {
                            terrainColliders.MarkChunkDirtyAt(x, y);
                        }
                    }
                }
            }
        }

        private void FillRegion(TerrainRegion region)
        {
            var world = simulation.World;
            var terrainColliders = simulation.TerrainColliders;

            for (int y = region.MinY; y <= region.MaxY; y++)
            {
                for (int x = region.MinX; x <= region.MaxX; x++)
                {
                    world.SetCell(x, y, region.MaterialId);

                    // Mark static terrain for collider generation
                    if (region.MaterialId == Materials.Stone ||
                        region.MaterialId == Materials.Ground)
                    {
                        terrainColliders.MarkChunkDirtyAt(x, y);
                    }

                }
            }
        }

        /// <summary>
        /// Converts cell position to world position.
        /// </summary>
        public Vector3 CellToWorldPosition(Vector2Int cellPos)
        {
            Vector2 worldPos = CoordinateUtils.CellToWorld(
                cellPos.x, cellPos.y,
                simulation.WorldWidth, simulation.WorldHeight);
            return new Vector3(worldPos.x, worldPos.y, 0);
        }
    }
}
