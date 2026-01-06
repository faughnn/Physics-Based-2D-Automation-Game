using UnityEngine;
using System.Collections.Generic;

namespace GoldRush.Building
{
    public readonly struct PlacementGrid
    {
        public readonly int CellWidth;   // pixels
        public readonly int CellHeight;  // pixels
        public readonly int WorldWidthCells;
        public readonly int WorldHeightCells;

        public PlacementGrid(int cellWidth, int cellHeight)
        {
            CellWidth = cellWidth;
            CellHeight = cellHeight;
            WorldWidthCells = 1280 / cellWidth;
            WorldHeightCells = 800 / cellHeight;
        }

        public Vector2 ToWorld(int x, int y)
        {
            float worldX = (x - WorldWidthCells / 2f + 0.5f) * CellWidth / 32f;
            float worldY = (WorldHeightCells / 2f - y - 0.5f) * CellHeight / 32f;
            return new Vector2(worldX, worldY);
        }

        public Vector2Int FromWorld(Vector2 pos)
        {
            int x = Mathf.FloorToInt(pos.x * 32f / CellWidth + WorldWidthCells / 2f);
            int y = Mathf.FloorToInt(WorldHeightCells / 2f - pos.y * 32f / CellHeight);
            return new Vector2Int(x, y);
        }

        // Predefined grids
        public static readonly PlacementGrid Main = new(32, 32);    // 40x25 cells
        public static readonly PlacementGrid Sub = new(16, 16);     // 80x50 cells
        public static readonly PlacementGrid Shaker = new(32, 16);  // 40x50 cells
    }

    public readonly struct BuildTypeInfo
    {
        public readonly PlacementGrid Grid;
        public readonly int VisualWidth;      // pixels
        public readonly int VisualHeight;     // pixels
        public readonly int CellSpanX;        // cells wide (1, 2, etc)
        public readonly int CellSpanY;        // cells tall
        public readonly int SimHalfWidth;     // sim cells from center
        public readonly int SimHalfHeight;    // sim cells from center

        public BuildTypeInfo(PlacementGrid grid, int visualWidth, int visualHeight,
                            int cellSpanX, int cellSpanY, int simHalfWidth, int simHalfHeight)
        {
            Grid = grid;
            VisualWidth = visualWidth;
            VisualHeight = visualHeight;
            CellSpanX = cellSpanX;
            CellSpanY = cellSpanY;
            SimHalfWidth = simHalfWidth;
            SimHalfHeight = simHalfHeight;
        }

        // Convenience properties
        public float VisualWidthUnits => VisualWidth / 32f;
        public float VisualHeightUnits => VisualHeight / 32f;
    }

    public static class BuildTypeData
    {
        private static readonly Dictionary<BuildType, BuildTypeInfo> _data = new()
        {
            // SubGrid (16x16) infrastructure
            { BuildType.Belt,         new(PlacementGrid.Sub, 16, 16, 1, 1, 4, 4) },
            { BuildType.Wall,         new(PlacementGrid.Sub, 16, 16, 1, 1, 4, 4) },
            { BuildType.FilterBelt,   new(PlacementGrid.Sub, 16, 16, 1, 1, 4, 4) },

            // Main grid (32x32) infrastructure
            { BuildType.Lift,         new(PlacementGrid.Main, 32, 32, 1, 1, 7, 8) },
            { BuildType.Blower,       new(PlacementGrid.Main, 32, 32, 1, 1, 8, 8) },
            { BuildType.SmallCrusher, new(PlacementGrid.Main, 32, 32, 1, 1, 7, 7) },
            { BuildType.Grinder,      new(PlacementGrid.Main, 32, 32, 1, 1, 4, 8) },
            { BuildType.Pusher,       new(PlacementGrid.Main, 32, 32, 1, 1, 8, 8) },

            // Multi-cell infrastructure
            { BuildType.BigCrusher,   new(PlacementGrid.Main, 64, 64, 2, 2, 14, 14) },
            { BuildType.GoldStore,    new(PlacementGrid.Main, 64, 32, 2, 1, 16, 8) },
            { BuildType.Smelter,      new(PlacementGrid.Main, 64, 32, 2, 1, 8, 4) },

            // Shaker grid (32x16) infrastructure
            { BuildType.Shaker,       new(PlacementGrid.Shaker, 32, 16, 1, 1, 8, 4) },
        };

        public static BuildTypeInfo Get(BuildType type) => _data[type];

        public static bool TryGet(BuildType type, out BuildTypeInfo info) => _data.TryGetValue(type, out info);
    }
}
