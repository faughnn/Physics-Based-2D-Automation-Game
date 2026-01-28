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
