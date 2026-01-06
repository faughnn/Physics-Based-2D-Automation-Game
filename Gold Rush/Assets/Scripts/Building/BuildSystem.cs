using UnityEngine;
using System.Collections.Generic;
using GoldRush.Core;
using GoldRush.Infrastructure;
using GoldRush.Simulation;
using GoldRush.UI;

namespace GoldRush.Building
{
    public enum BuildType
    {
        None,
        Wall,
        Belt,
        FilterBelt,
        Lift,
        Shaker,
        GoldStore,
        BigCrusher,
        SmallCrusher,
        Blower,
        Grinder,
        Smelter,
        Pusher
    }

    public class BuildSystem : MonoBehaviour
    {
        public static BuildSystem Instance { get; private set; }

        public BuildType CurrentBuildType { get; private set; }
        public bool DirectionPositive { get; private set; } = true; // Right/Up

        private Dictionary<Vector2Int, GameObject> placedInfrastructure = new Dictionary<Vector2Int, GameObject>();
        private Transform infrastructureParent;
        private BuildPreview preview;
        private Camera mainCamera;

        private bool isDragging;
        private Vector2Int dragStartPos;
        private List<Vector2Int> dragPositions = new List<Vector2Int>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            CurrentBuildType = BuildType.None;
        }

        public void Initialize()
        {
            infrastructureParent = new GameObject("Infrastructure").transform;
            mainCamera = Camera.main;

            // Create preview
            GameObject previewGO = new GameObject("BuildPreview");
            preview = previewGO.AddComponent<BuildPreview>();
            preview.Initialize(this);

            Debug.Log("BuildSystem: Initialized");
        }

        private void Update()
        {
            // Debug: Press F1 to dump all placed infrastructure
            if (Input.GetKeyDown(KeyCode.F1))
            {
                DebugDumpInfrastructure();
            }

            // Debug: Press F2 to clear ghost entries (null GameObjects)
            if (Input.GetKeyDown(KeyCode.F2))
            {
                CleanupGhostEntries();
            }

            if (CurrentBuildType == BuildType.None) return;

            HandleInput();
            UpdatePreview();
        }

        private void DebugDumpInfrastructure()
        {
            Debug.Log($"=== Placed Infrastructure ({placedInfrastructure.Count} entries) ===");
            foreach (var kvp in placedInfrastructure)
            {
                string objName = kvp.Value != null ? kvp.Value.name : "NULL (ghost)";
                Debug.Log($"  Position {kvp.Key}: {objName}");
            }
            Debug.Log("=== End ===");
        }

        private void CleanupGhostEntries()
        {
            List<Vector2Int> toRemove = new List<Vector2Int>();
            foreach (var kvp in placedInfrastructure)
            {
                if (kvp.Value == null)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var pos in toRemove)
            {
                placedInfrastructure.Remove(pos);
            }

            Debug.Log($"Cleaned up {toRemove.Count} ghost entries");
        }

        private void HandleInput()
        {
            // Toggle direction with Q/E
            if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.E))
            {
                DirectionPositive = !DirectionPositive;
                Debug.Log($"Direction: {(DirectionPositive ? "Right/Up" : "Left/Down")}");
            }

            // Open filter selection with F (for FilterBelt)
            if (Input.GetKeyDown(KeyCode.F) && CurrentBuildType == BuildType.FilterBelt)
            {
                if (FilterSelectionUI.Instance != null)
                {
                    FilterSelectionUI.Instance.Toggle();
                }
                return;
            }

            // Don't process placement if filter selection is open
            if (FilterSelectionUI.Instance != null && FilterSelectionUI.Instance.IsOpen)
            {
                return;
            }

            // Cancel with Escape
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SetBuildType(BuildType.None);
                return;
            }

            Vector2Int gridPos = GetMouseGridPosition();

            // Start drag
            if (Input.GetMouseButtonDown(0))
            {
                isDragging = true;
                dragStartPos = gridPos;
                dragPositions.Clear();
            }

            // Update drag positions
            if (isDragging)
            {
                UpdateDragPositions(gridPos);
            }

            // End drag and place
            if (Input.GetMouseButtonUp(0) && isDragging)
            {
                isDragging = false;
                PlaceAtPositions(dragPositions);
                dragPositions.Clear();
            }
        }

        private void UpdateDragPositions(Vector2Int currentPos)
        {
            dragPositions.Clear();

            // Calculate line from start to current
            int dx = currentPos.x - dragStartPos.x;
            int dy = currentPos.y - dragStartPos.y;

            // Determine primary direction
            bool horizontal = Mathf.Abs(dx) >= Mathf.Abs(dy);

            if (horizontal)
            {
                int dir = dx >= 0 ? 1 : -1;
                for (int x = dragStartPos.x; x != currentPos.x + dir; x += dir)
                {
                    dragPositions.Add(new Vector2Int(x, dragStartPos.y));
                }
            }
            else
            {
                int dir = dy >= 0 ? 1 : -1;
                for (int y = dragStartPos.y; y != currentPos.y + dir; y += dir)
                {
                    dragPositions.Add(new Vector2Int(dragStartPos.x, y));
                }
            }

            // Ensure at least the start position
            if (dragPositions.Count == 0)
            {
                dragPositions.Add(dragStartPos);
            }
        }

        private void UpdatePreview()
        {
            if (preview == null) return;

            if (isDragging && dragPositions.Count > 0)
            {
                preview.ShowMultiple(dragPositions, CurrentBuildType, DirectionPositive);
            }
            else
            {
                Vector2Int gridPos = GetMouseGridPosition();
                bool canPlace = CanPlaceAt(gridPos);
                preview.ShowSingle(gridPos, CurrentBuildType, DirectionPositive, canPlace);
            }
        }

        private void PlaceAtPositions(List<Vector2Int> positions)
        {
            foreach (Vector2Int pos in positions)
            {
                TryPlaceAt(pos);
            }
        }

        public bool TryPlaceAt(Vector2Int gridPos)
        {
            if (!CanPlaceAt(gridPos)) return false;

            GameObject placed = null;

            switch (CurrentBuildType)
            {
                case BuildType.Wall:
                    placed = Wall.Create(gridPos.x, gridPos.y, infrastructureParent);
                    break;
                case BuildType.Belt:
                    placed = Belt.Create(gridPos.x, gridPos.y, DirectionPositive, infrastructureParent);
                    break;
                case BuildType.FilterBelt:
                    var blockedMats = FilterSelectionUI.Instance?.SelectedMaterials;
                    placed = FilterBelt.Create(gridPos.x, gridPos.y, DirectionPositive, infrastructureParent, blockedMats);
                    break;
                case BuildType.Lift:
                    placed = Lift.Create(gridPos.x, gridPos.y, DirectionPositive, infrastructureParent);
                    break;
                case BuildType.Shaker:
                    placed = Shaker.Create(gridPos.x, gridPos.y, DirectionPositive, infrastructureParent);
                    break;
                case BuildType.GoldStore:
                    placed = GoldStore.Create(gridPos.x, gridPos.y, infrastructureParent);
                    break;
                case BuildType.BigCrusher:
                    placed = BigCrusher.Create(gridPos.x, gridPos.y, infrastructureParent);
                    break;
                case BuildType.SmallCrusher:
                    placed = SmallCrusher.Create(gridPos.x, gridPos.y, infrastructureParent);
                    break;
                case BuildType.Blower:
                    placed = Blower.Create(gridPos.x, gridPos.y, DirectionPositive, infrastructureParent);
                    break;
                case BuildType.Grinder:
                    placed = Grinder.Create(gridPos.x, gridPos.y, infrastructureParent);
                    break;
                case BuildType.Smelter:
                    placed = Smelter.Create(gridPos.x, gridPos.y, infrastructureParent);
                    break;
                case BuildType.Pusher:
                    var pushDir = DirectionPositive ? PushDirection.Right : PushDirection.Left;
                    placed = Pusher.Create(gridPos.x, gridPos.y, pushDir, infrastructureParent);
                    break;
            }

            if (placed != null)
            {
                placedInfrastructure[gridPos] = placed;

                // Register all cells for multi-cell buildings using metadata
                var info = BuildTypeData.Get(CurrentBuildType);
                for (int dx = 0; dx < info.CellSpanX; dx++)
                {
                    for (int dy = 0; dy < info.CellSpanY; dy++)
                    {
                        if (dx == 0 && dy == 0) continue; // Already registered above
                        placedInfrastructure[new Vector2Int(gridPos.x + dx, gridPos.y + dy)] = placed;
                    }
                }

                // Explode any particles trapped in the build area upward
                ExplodeParticlesInArea(gridPos, CurrentBuildType);

                return true;
            }

            return false;
        }

        // Explode particles in the infrastructure area upward (like digging effect)
        private void ExplodeParticlesInArea(Vector2Int gridPos, BuildType type)
        {
            if (SimulationWorld.Instance == null) return;

            var grid = SimulationWorld.Instance.Grid;
            Rect bounds = GetWorldBoundsForPlacement(gridPos, type);

            // Convert world bounds to simulation grid coordinates
            Vector2Int minGrid = SimulationWorld.Instance.WorldToGrid(new Vector2(bounds.xMin, bounds.yMax));
            Vector2Int maxGrid = SimulationWorld.Instance.WorldToGrid(new Vector2(bounds.xMax, bounds.yMin));

            float kickStrength = 20f;

            for (int y = minGrid.y; y <= maxGrid.y; y++)
            {
                for (int x = minGrid.x; x <= maxGrid.x; x++)
                {
                    MaterialType material = grid.Get(x, y);
                    if (MaterialProperties.IsSimulated(material))
                    {
                        // Give upward velocity with some randomness
                        float randomX = (UnityEngine.Random.value - 0.5f) * 10f;
                        float kickY = -kickStrength * (0.7f + UnityEngine.Random.value * 0.6f);  // Negative Y = up in grid coords
                        grid.SetVelocity(x, y, new Vector2(randomX, kickY));
                        grid.WakeCell(x, y);
                    }
                }
            }
        }

        public bool CanPlaceAt(Vector2Int gridPos)
        {
            // Get grid bounds from metadata
            if (!BuildTypeData.TryGet(CurrentBuildType, out var info))
            {
                return false;
            }

            int maxX = info.Grid.WorldWidthCells;
            int maxY = info.Grid.WorldHeightCells;

            if (gridPos.x < 0 || gridPos.x >= maxX ||
                gridPos.y < 0 || gridPos.y >= maxY)
            {
                return false;
            }

            // Get world bounds of proposed building
            Rect proposedBounds = GetWorldBoundsForPlacement(gridPos, CurrentBuildType);

            // Check for world-space overlap with all existing infrastructure
            foreach (var kvp in placedInfrastructure)
            {
                if (kvp.Value == null) continue;

                // Get the collider bounds of existing infrastructure
                Collider2D col = kvp.Value.GetComponent<Collider2D>();
                if (col != null)
                {
                    Rect existingBounds = new Rect(
                        col.bounds.min.x,
                        col.bounds.min.y,
                        col.bounds.size.x,
                        col.bounds.size.y
                    );

                    // Check for overlap (with small tolerance)
                    if (proposedBounds.Overlaps(existingBounds))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private Rect GetWorldBoundsForPlacement(Vector2Int gridPos, BuildType type)
        {
            var info = BuildTypeData.Get(type);
            Vector2 worldPos = info.Grid.ToWorld(gridPos.x, gridPos.y);
            float width = info.VisualWidthUnits;
            float height = info.VisualHeightUnits;

            // Return bounds centered on worldPos
            return new Rect(
                worldPos.x - width / 2f,
                worldPos.y - height / 2f,
                width,
                height
            );
        }

        public bool TryDeleteAt(Vector2Int gridPos)
        {
            if (placedInfrastructure.TryGetValue(gridPos, out GameObject infrastructure))
            {
                // Find all cells this infrastructure occupies
                List<Vector2Int> toRemove = new List<Vector2Int>();
                foreach (var kvp in placedInfrastructure)
                {
                    if (kvp.Value == infrastructure)
                    {
                        toRemove.Add(kvp.Key);
                    }
                }

                foreach (var pos in toRemove)
                {
                    placedInfrastructure.Remove(pos);
                }

                Destroy(infrastructure);
                return true;
            }
            return false;
        }

        public void SetBuildType(BuildType type)
        {
            CurrentBuildType = type;

            if (preview != null)
            {
                preview.SetVisible(type != BuildType.None);
            }

            Debug.Log($"Build type: {type}");
        }

        public Vector2Int GetMouseGridPosition()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            Vector2 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);

            // Use appropriate grid based on build type
            if (BuildTypeData.TryGet(CurrentBuildType, out var info))
            {
                return info.Grid.FromWorld(mouseWorld);
            }
            return GameSettings.WorldToGrid(mouseWorld);
        }
    }
}
