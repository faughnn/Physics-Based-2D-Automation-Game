using System.Collections.Generic;
using UnityEngine;

namespace FallingSand
{
    /// <summary>
    /// A reusable cell collection zone that scans a rectangular area and removes
    /// movable cells. Used by buckets and other collection structures.
    /// </summary>
    public class CollectionZone
    {
        private readonly CellWorld world;
        private readonly RectInt cellBounds;

        /// <summary>
        /// The cell bounds of this collection zone.
        /// </summary>
        public RectInt Bounds => cellBounds;

        /// <summary>
        /// Creates a collection zone for the given world and bounds.
        /// </summary>
        /// <param name="world">The cell world to scan</param>
        /// <param name="cellBounds">The rectangular area to scan (in cell coordinates)</param>
        public CollectionZone(CellWorld world, RectInt cellBounds)
        {
            this.world = world;
            this.cellBounds = cellBounds;
        }

        /// <summary>
        /// Scans the zone for movable cells and removes them.
        /// Returns count of cells removed per material type.
        /// Skips air, static materials, and cluster-owned cells.
        /// </summary>
        public Dictionary<byte, int> CollectCells()
        {
            var collected = new Dictionary<byte, int>();

            for (int y = cellBounds.yMin; y < cellBounds.yMax; y++)
            {
                for (int x = cellBounds.xMin; x < cellBounds.xMax; x++)
                {
                    // Bounds check
                    if (!world.IsInBounds(x, y))
                        continue;

                    byte materialId = world.GetCell(x, y);

                    // Skip air
                    if (materialId == Materials.Air)
                        continue;

                    // Skip static materials (stone, belt tiles, etc.)
                    MaterialDef mat = world.materials[materialId];
                    if (mat.behaviour == BehaviourType.Static)
                        continue;

                    // Skip cluster-owned cells (rigid body owned)
                    int index = y * world.width + x;
                    if (world.cells[index].ownerId != 0)
                        continue;

                    // Collect and remove
                    if (!collected.ContainsKey(materialId))
                        collected[materialId] = 0;
                    collected[materialId]++;

                    world.SetCell(x, y, Materials.Air);
                }
            }

            return collected;
        }

        /// <summary>
        /// Scans the zone for cells of a specific material type and removes only those.
        /// Other materials are left in place (they accumulate and player must remove).
        /// Returns count of cells removed.
        /// </summary>
        /// <param name="targetMaterial">The material ID to collect</param>
        public int CollectCellsOfType(byte targetMaterial)
        {
            int collected = 0;

            for (int y = cellBounds.yMin; y < cellBounds.yMax; y++)
            {
                for (int x = cellBounds.xMin; x < cellBounds.xMax; x++)
                {
                    // Bounds check
                    if (!world.IsInBounds(x, y))
                        continue;

                    byte materialId = world.GetCell(x, y);

                    // Only collect the target material
                    if (materialId != targetMaterial)
                        continue;

                    // Skip static materials (shouldn't happen for Dirt, but safety check)
                    MaterialDef mat = world.materials[materialId];
                    if (mat.behaviour == BehaviourType.Static)
                        continue;

                    // Skip cluster-owned cells (rigid body owned)
                    int index = y * world.width + x;
                    if (world.cells[index].ownerId != 0)
                        continue;

                    // Collect and remove
                    collected++;
                    world.SetCell(x, y, Materials.Air);
                }
            }

            return collected;
        }

        /// <summary>
        /// Counts cells in the zone without removing them.
        /// Useful for preview or debugging.
        /// </summary>
        public Dictionary<byte, int> CountCells()
        {
            var counts = new Dictionary<byte, int>();

            for (int y = cellBounds.yMin; y < cellBounds.yMax; y++)
            {
                for (int x = cellBounds.xMin; x < cellBounds.xMax; x++)
                {
                    if (!world.IsInBounds(x, y))
                        continue;

                    byte materialId = world.GetCell(x, y);

                    if (materialId == Materials.Air)
                        continue;

                    MaterialDef mat = world.materials[materialId];
                    if (mat.behaviour == BehaviourType.Static)
                        continue;

                    int index = y * world.width + x;
                    if (world.cells[index].ownerId != 0)
                        continue;

                    if (!counts.ContainsKey(materialId))
                        counts[materialId] = 0;
                    counts[materialId]++;
                }
            }

            return counts;
        }
    }
}
