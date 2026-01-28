using System.Collections.Generic;
using UnityEngine;

namespace FallingSand
{
    /// <summary>
    /// Configuration data for a game level.
    /// Defines terrain regions to fill and spawn positions.
    /// Supports multiple buckets and objectives for multi-stage progression.
    /// </summary>
    public class LevelData
    {
        /// <summary>
        /// Terrain regions to fill with materials.
        /// </summary>
        public List<TerrainRegion> TerrainRegions { get; set; } = new List<TerrainRegion>();

        /// <summary>
        /// Player spawn position in cell coordinates.
        /// </summary>
        public Vector2Int PlayerSpawn { get; set; }

        /// <summary>
        /// Shovel spawn position in cell coordinates.
        /// </summary>
        public Vector2Int ShovelSpawn { get; set; }

        /// <summary>
        /// Multiple bucket spawn positions for multi-stage progression.
        /// Each position is the top-left corner of the bucket structure.
        /// </summary>
        public List<Vector2Int> BucketSpawns { get; set; } = new List<Vector2Int>();

        /// <summary>
        /// Multiple objectives for multi-stage progression.
        /// Each objective corresponds to a bucket at the same index in BucketSpawns.
        /// </summary>
        public List<ObjectiveData> Objectives { get; set; } = new List<ObjectiveData>();
    }

    /// <summary>
    /// Defines a rectangular region to fill with a material.
    /// </summary>
    public struct TerrainRegion
    {
        /// <summary>
        /// Minimum X coordinate (inclusive).
        /// </summary>
        public int MinX;

        /// <summary>
        /// Maximum X coordinate (inclusive).
        /// </summary>
        public int MaxX;

        /// <summary>
        /// Minimum Y coordinate (inclusive).
        /// </summary>
        public int MinY;

        /// <summary>
        /// Maximum Y coordinate (inclusive).
        /// </summary>
        public int MaxY;

        /// <summary>
        /// Material ID to fill the region with.
        /// </summary>
        public byte MaterialId;

        public TerrainRegion(int minX, int maxX, int minY, int maxY, byte materialId)
        {
            MinX = minX;
            MaxX = maxX;
            MinY = minY;
            MaxY = maxY;
            MaterialId = materialId;
        }
    }
}
