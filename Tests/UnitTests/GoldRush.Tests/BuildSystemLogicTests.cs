using Xunit;
using System.Collections.Generic;
using UnityEngine;

namespace GoldRush.Tests
{
    public enum BuildType
    {
        None,
        Wall,
        Belt,
        Lift,
        Shaker,
        GoldStore
    }

    // Mock build system for testing placement logic
    public class MockBuildSystem
    {
        private Dictionary<Vector2Int, BuildType> placedInfrastructure = new();
        private HashSet<Vector2Int> terrainCells = new();

        public BuildType CurrentBuildType { get; set; } = BuildType.None;
        public bool DirectionPositive { get; set; } = true;

        public void SetTerrainAt(int x, int y)
        {
            terrainCells.Add(new Vector2Int(x, y));
        }

        public bool CanPlaceAt(Vector2Int gridPos)
        {
            // Check bounds
            if (gridPos.x < 0 || gridPos.x >= GameSettings.WorldWidthCells ||
                gridPos.y < 0 || gridPos.y >= GameSettings.WorldHeightCells)
            {
                return false;
            }

            // Check if already occupied by infrastructure
            if (placedInfrastructure.ContainsKey(gridPos))
            {
                return false;
            }

            // Check if occupied by terrain
            if (terrainCells.Contains(gridPos))
            {
                return false;
            }

            // Gold store needs 2 cells
            if (CurrentBuildType == BuildType.GoldStore)
            {
                Vector2Int secondPos = new Vector2Int(gridPos.x + 1, gridPos.y);
                if (placedInfrastructure.ContainsKey(secondPos) || terrainCells.Contains(secondPos))
                {
                    return false;
                }
                if (secondPos.x >= GameSettings.WorldWidthCells)
                {
                    return false;
                }
            }

            return true;
        }

        public bool TryPlaceAt(Vector2Int gridPos)
        {
            if (!CanPlaceAt(gridPos)) return false;

            placedInfrastructure[gridPos] = CurrentBuildType;

            // Gold store takes 2 cells
            if (CurrentBuildType == BuildType.GoldStore)
            {
                placedInfrastructure[new Vector2Int(gridPos.x + 1, gridPos.y)] = CurrentBuildType;
            }

            return true;
        }

        public bool TryDeleteAt(Vector2Int gridPos)
        {
            if (!placedInfrastructure.ContainsKey(gridPos)) return false;

            var type = placedInfrastructure[gridPos];
            placedInfrastructure.Remove(gridPos);

            // If gold store, also remove second cell
            if (type == BuildType.GoldStore)
            {
                placedInfrastructure.Remove(new Vector2Int(gridPos.x + 1, gridPos.y));
                placedInfrastructure.Remove(new Vector2Int(gridPos.x - 1, gridPos.y));
            }

            return true;
        }

        public int PlacedCount => placedInfrastructure.Count;
    }

    public class BuildSystemLogicTests
    {
        [Fact]
        public void CanPlaceAt_EmptyCell_ReturnsTrue()
        {
            var system = new MockBuildSystem();
            system.CurrentBuildType = BuildType.Wall;

            bool canPlace = system.CanPlaceAt(new Vector2Int(10, 10));

            Assert.True(canPlace);
        }

        [Fact]
        public void CanPlaceAt_OutOfBounds_ReturnsFalse()
        {
            var system = new MockBuildSystem();
            system.CurrentBuildType = BuildType.Wall;

            Assert.False(system.CanPlaceAt(new Vector2Int(-1, 10)));
            Assert.False(system.CanPlaceAt(new Vector2Int(10, -1)));
            Assert.False(system.CanPlaceAt(new Vector2Int(GameSettings.WorldWidthCells, 10)));
            Assert.False(system.CanPlaceAt(new Vector2Int(10, GameSettings.WorldHeightCells)));
        }

        [Fact]
        public void CanPlaceAt_OccupiedByInfrastructure_ReturnsFalse()
        {
            var system = new MockBuildSystem();
            system.CurrentBuildType = BuildType.Wall;
            system.TryPlaceAt(new Vector2Int(10, 10));

            bool canPlace = system.CanPlaceAt(new Vector2Int(10, 10));

            Assert.False(canPlace);
        }

        [Fact]
        public void CanPlaceAt_OccupiedByTerrain_ReturnsFalse()
        {
            var system = new MockBuildSystem();
            system.CurrentBuildType = BuildType.Wall;
            system.SetTerrainAt(10, 10);

            bool canPlace = system.CanPlaceAt(new Vector2Int(10, 10));

            Assert.False(canPlace);
        }

        [Fact]
        public void TryPlaceAt_ValidPosition_ReturnsTrue()
        {
            var system = new MockBuildSystem();
            system.CurrentBuildType = BuildType.Belt;

            bool placed = system.TryPlaceAt(new Vector2Int(10, 10));

            Assert.True(placed);
            Assert.Equal(1, system.PlacedCount);
        }

        [Fact]
        public void TryPlaceAt_InvalidPosition_ReturnsFalse()
        {
            var system = new MockBuildSystem();
            system.CurrentBuildType = BuildType.Wall;
            system.SetTerrainAt(10, 10);

            bool placed = system.TryPlaceAt(new Vector2Int(10, 10));

            Assert.False(placed);
            Assert.Equal(0, system.PlacedCount);
        }

        [Fact]
        public void GoldStore_OccupiesTwoCells()
        {
            var system = new MockBuildSystem();
            system.CurrentBuildType = BuildType.GoldStore;

            system.TryPlaceAt(new Vector2Int(10, 10));

            Assert.Equal(2, system.PlacedCount);
            Assert.False(system.CanPlaceAt(new Vector2Int(10, 10)));
            Assert.False(system.CanPlaceAt(new Vector2Int(11, 10)));
        }

        [Fact]
        public void GoldStore_AtRightEdge_CannotPlace()
        {
            var system = new MockBuildSystem();
            system.CurrentBuildType = BuildType.GoldStore;

            // Try to place at right edge - needs 2 cells but would go out of bounds
            bool canPlace = system.CanPlaceAt(new Vector2Int(GameSettings.WorldWidthCells - 1, 10));

            Assert.False(canPlace);
        }

        [Fact]
        public void TryDeleteAt_ExistingInfrastructure_ReturnsTrue()
        {
            var system = new MockBuildSystem();
            system.CurrentBuildType = BuildType.Wall;
            system.TryPlaceAt(new Vector2Int(10, 10));

            bool deleted = system.TryDeleteAt(new Vector2Int(10, 10));

            Assert.True(deleted);
            Assert.Equal(0, system.PlacedCount);
        }

        [Fact]
        public void TryDeleteAt_EmptyCell_ReturnsFalse()
        {
            var system = new MockBuildSystem();

            bool deleted = system.TryDeleteAt(new Vector2Int(10, 10));

            Assert.False(deleted);
        }

        [Fact]
        public void DirectionPositive_DefaultsToTrue()
        {
            var system = new MockBuildSystem();

            Assert.True(system.DirectionPositive);
        }

        [Fact]
        public void AllBuildTypes_CanBePlaced()
        {
            var system = new MockBuildSystem();
            int y = 10;

            foreach (BuildType type in Enum.GetValues(typeof(BuildType)))
            {
                if (type == BuildType.None) continue;

                system.CurrentBuildType = type;
                bool placed = system.TryPlaceAt(new Vector2Int(y, 5)); // Different x for each
                y += 3; // Space out for gold store

                Assert.True(placed, $"Failed to place {type}");
            }
        }
    }
}
