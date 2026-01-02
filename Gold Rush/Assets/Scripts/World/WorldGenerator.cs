using UnityEngine;
using GoldRush.Core;

namespace GoldRush.World
{
    public class WorldGenerator : MonoBehaviour
    {
        private TerrainBlock[,] terrainGrid;
        private GameObject terrainParent;

        public void Generate()
        {

            // Create parent for terrain blocks
            terrainParent = new GameObject("Terrain");
            terrainParent.transform.SetParent(transform);

            // Initialize terrain grid
            terrainGrid = new TerrainBlock[GameSettings.WorldWidthCells, GameSettings.WorldHeightCells];

            // Calculate terrain start row (after water reservoir and air)
            int terrainStartRow = GameSettings.WaterReservoirHeight + GameSettings.AirHeight;

            // Generate terrain blocks (skip reservoir area)
            for (int y = terrainStartRow; y < GameSettings.WorldHeightCells; y++)
            {
                for (int x = 0; x < GameSettings.WorldWidthCells; x++)
                {
                    // Skip the reservoir bottom row (it will be created separately)
                    if (y == GameSettings.ReservoirBottomY &&
                        x >= GameSettings.ReservoirLeftX && x <= GameSettings.ReservoirRightX)
                    {
                        continue;
                    }
                    CreateTerrainBlock(x, y);
                }
            }

            // Create water reservoir structure (bucket with diggable walls)
            CreateWaterReservoir();

            // Create world boundaries (invisible walls)
            CreateWorldBoundaries();

        }

        private void CreateWaterReservoir()
        {
            // Create left wall (extends from air zone down to terrain level)
            for (int y = GameSettings.ReservoirTopY; y <= GameSettings.ReservoirBottomY; y++)
            {
                CreateTerrainBlock(GameSettings.ReservoirLeftX, y);
            }

            // Create right wall
            for (int y = GameSettings.ReservoirTopY; y <= GameSettings.ReservoirBottomY; y++)
            {
                CreateTerrainBlock(GameSettings.ReservoirRightX, y);
            }

            // Create bottom (from left wall to right wall, at terrain level)
            for (int x = GameSettings.ReservoirLeftX; x <= GameSettings.ReservoirRightX; x++)
            {
                // Skip corners (already created with walls)
                if (x == GameSettings.ReservoirLeftX || x == GameSettings.ReservoirRightX)
                    continue;
                CreateTerrainBlock(x, GameSettings.ReservoirBottomY);
            }

        }

        private void CreateTerrainBlock(int gridX, int gridY)
        {
            GameObject blockGO = new GameObject($"Terrain_{gridX}_{gridY}");
            blockGO.transform.SetParent(terrainParent.transform);

            // Position
            Vector2 worldPos = GameSettings.GridToWorld(gridX, gridY);
            blockGO.transform.position = worldPos;

            // No sprite renderer - visuals are handled by SimulationWorld
            // These blocks are invisible collision objects for player physics

            // Add collider
            BoxCollider2D col = blockGO.AddComponent<BoxCollider2D>();
            col.size = new Vector2(GameSettings.GridSize / GameSettings.PixelsPerUnit,
                                   GameSettings.GridSize / GameSettings.PixelsPerUnit);

            // Set layer
            blockGO.layer = LayerSetup.TerrainLayer;

            // Add terrain block component
            TerrainBlock block = blockGO.AddComponent<TerrainBlock>();
            block.Initialize(gridX, gridY);

            // Store in grid
            terrainGrid[gridX, gridY] = block;
        }

        private void CreateWorldBoundaries()
        {
            float worldWidth = (GameSettings.WorldWidthCells * GameSettings.GridSize) / GameSettings.PixelsPerUnit;
            float worldHeight = (GameSettings.WorldHeightCells * GameSettings.GridSize) / GameSettings.PixelsPerUnit;
            float thickness = 1f;

            // Left wall
            CreateBoundary("LeftWall",
                new Vector2(-worldWidth / 2 - thickness / 2, 0),
                new Vector2(thickness, worldHeight + thickness * 2));

            // Right wall
            CreateBoundary("RightWall",
                new Vector2(worldWidth / 2 + thickness / 2, 0),
                new Vector2(thickness, worldHeight + thickness * 2));

            // Bottom
            CreateBoundary("BottomWall",
                new Vector2(0, -worldHeight / 2 - thickness / 2),
                new Vector2(worldWidth + thickness * 2, thickness));

            // Top (optional - can leave open for water reservoir)
            CreateBoundary("TopWall",
                new Vector2(0, worldHeight / 2 + thickness / 2),
                new Vector2(worldWidth + thickness * 2, thickness));
        }

        private void CreateBoundary(string name, Vector2 position, Vector2 size)
        {
            GameObject boundaryGO = new GameObject(name);
            boundaryGO.transform.SetParent(transform);
            boundaryGO.transform.position = position;

            BoxCollider2D col = boundaryGO.AddComponent<BoxCollider2D>();
            col.size = size;

            boundaryGO.layer = LayerSetup.TerrainLayer;
        }

        public TerrainBlock GetTerrainAt(int gridX, int gridY)
        {
            if (gridX < 0 || gridX >= GameSettings.WorldWidthCells ||
                gridY < 0 || gridY >= GameSettings.WorldHeightCells)
            {
                return null;
            }
            return terrainGrid[gridX, gridY];
        }

        public bool DigAt(int gridX, int gridY)
        {
            TerrainBlock block = GetTerrainAt(gridX, gridY);
            if (block != null && !block.IsDug)
            {
                block.Dig();
                terrainGrid[gridX, gridY] = null;
                return true;
            }
            return false;
        }

        public bool IsValidGridPosition(int gridX, int gridY)
        {
            return gridX >= 0 && gridX < GameSettings.WorldWidthCells &&
                   gridY >= 0 && gridY < GameSettings.WorldHeightCells;
        }

        public bool IsCellEmpty(int gridX, int gridY)
        {
            if (!IsValidGridPosition(gridX, gridY))
            {
                return false;
            }
            return terrainGrid[gridX, gridY] == null;
        }
    }
}
