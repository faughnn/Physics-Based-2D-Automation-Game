using UnityEngine;

namespace FallingSand
{
    /// <summary>
    /// Centralized coordinate conversion utilities.
    /// Single source of truth for cell-to-world scaling and conversions.
    ///
    /// Coordinate systems:
    /// - Cell grid: (0,0) at top-left, Y+ = down, integer coordinates
    /// - Unity world: (0,0) at center, Y+ = up, float coordinates
    ///
    /// World spans from (-worldWidth, -worldHeight) to (+worldWidth, +worldHeight)
    /// because each cell is 2x2 world units (CellToWorldScale = 2).
    /// </summary>
    public static class CoordinateUtils
    {
        /// <summary>
        /// World units per cell. Each cell renders as 2x2 pixels.
        /// </summary>
        public const float CellToWorldScale = 2f;

        /// <summary>
        /// Cells per world unit (inverse of CellToWorldScale).
        /// </summary>
        public const float WorldToCellScale = 0.5f;

        /// <summary>
        /// Pixels per cell (same as CellToWorldScale, but as int for rendering).
        /// </summary>
        public const int PixelsPerCell = 2;

        /// <summary>
        /// Convert cell coordinates to world coordinates.
        /// </summary>
        /// <param name="cellX">Cell X coordinate</param>
        /// <param name="cellY">Cell Y coordinate</param>
        /// <param name="worldWidth">Width of the cell world in cells</param>
        /// <param name="worldHeight">Height of the cell world in cells</param>
        /// <returns>World position</returns>
        public static Vector2 CellToWorld(int cellX, int cellY, int worldWidth, int worldHeight)
        {
            // worldX = cellX * 2 - worldWidth
            // worldY = worldHeight - cellY * 2  (Y flipped: cell Y=0 is top)
            float worldX = cellX * CellToWorldScale - worldWidth;
            float worldY = worldHeight - cellY * CellToWorldScale;
            return new Vector2(worldX, worldY);
        }

        /// <summary>
        /// Convert cell coordinates (float) to world coordinates.
        /// </summary>
        public static Vector2 CellToWorld(float cellX, float cellY, int worldWidth, int worldHeight)
        {
            float worldX = cellX * CellToWorldScale - worldWidth;
            float worldY = worldHeight - cellY * CellToWorldScale;
            return new Vector2(worldX, worldY);
        }

        /// <summary>
        /// Convert world coordinates to cell coordinates using FloorToInt.
        /// Use for mouse/painting input where we want the cell containing the point.
        /// </summary>
        /// <param name="worldPos">World position</param>
        /// <param name="worldWidth">Width of the cell world in cells</param>
        /// <param name="worldHeight">Height of the cell world in cells</param>
        /// <returns>Cell coordinates</returns>
        public static Vector2Int WorldToCell(Vector2 worldPos, int worldWidth, int worldHeight)
        {
            // cellX = (worldX + worldWidth) / 2
            // cellY = (worldHeight - worldY) / 2  (Y flipped: cell Y=0 is top)
            int cellX = Mathf.FloorToInt((worldPos.x + worldWidth) * WorldToCellScale);
            int cellY = Mathf.FloorToInt((worldHeight - worldPos.y) * WorldToCellScale);
            return new Vector2Int(cellX, cellY);
        }

        /// <summary>
        /// Convert world coordinates to cell coordinates using RoundToInt.
        /// Use for cluster pixel placement where we want the nearest cell center.
        /// </summary>
        public static Vector2Int WorldToCellRounded(Vector2 worldPos, int worldWidth, int worldHeight)
        {
            float cellX = (worldPos.x + worldWidth) * WorldToCellScale;
            float cellY = (worldHeight - worldPos.y) * WorldToCellScale;
            return new Vector2Int(Mathf.RoundToInt(cellX), Mathf.RoundToInt(cellY));
        }

        /// <summary>
        /// Convert world coordinates to cell coordinates as floats (sub-cell precision).
        /// </summary>
        public static Vector2 WorldToCellFloat(Vector2 worldPos, int worldWidth, int worldHeight)
        {
            float cellX = (worldPos.x + worldWidth) * WorldToCellScale;
            float cellY = (worldHeight - worldPos.y) * WorldToCellScale;
            return new Vector2(cellX, cellY);
        }

        /// <summary>
        /// Scale a distance from cell units to world units.
        /// </summary>
        public static float ScaleCellToWorld(float cellDistance)
        {
            return cellDistance * CellToWorldScale;
        }

        /// <summary>
        /// Scale a distance from world units to cell units.
        /// </summary>
        public static float ScaleWorldToCell(float worldDistance)
        {
            return worldDistance * WorldToCellScale;
        }

        /// <summary>
        /// Scale a Vector2 from cell units to world units.
        /// </summary>
        public static Vector2 ScaleCellToWorld(Vector2 cellVector)
        {
            return cellVector * CellToWorldScale;
        }

        /// <summary>
        /// Scale a Vector2 from world units to cell units.
        /// </summary>
        public static Vector2 ScaleWorldToCell(Vector2 worldVector)
        {
            return worldVector * WorldToCellScale;
        }
    }
}
