using UnityEngine;
using System.Collections.Generic;
using GoldRush.Core;
using GoldRush.Infrastructure;

namespace GoldRush.Building
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
            if (CurrentBuildType == BuildType.None) return;

            HandleInput();
            UpdatePreview();
        }

        private void HandleInput()
        {
            // Toggle direction with Q/E
            if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.E))
            {
                DirectionPositive = !DirectionPositive;
                Debug.Log($"Direction: {(DirectionPositive ? "Right/Up" : "Left/Down")}");
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
                case BuildType.Lift:
                    placed = Lift.Create(gridPos.x, gridPos.y, DirectionPositive, infrastructureParent);
                    break;
                case BuildType.Shaker:
                    placed = Shaker.Create(gridPos.x, gridPos.y, DirectionPositive, infrastructureParent);
                    break;
                case BuildType.GoldStore:
                    placed = GoldStore.Create(gridPos.x, gridPos.y, infrastructureParent);
                    // Gold store takes 2 cells
                    placedInfrastructure[new Vector2Int(gridPos.x + 1, gridPos.y)] = placed;
                    break;
            }

            if (placed != null)
            {
                placedInfrastructure[gridPos] = placed;
                return true;
            }

            return false;
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
            if (GameManager.Instance != null)
            {
                var terrain = GameManager.Instance.GetTerrainAt(gridPos.x, gridPos.y);
                if (terrain != null)
                {
                    return false;
                }
            }

            // Gold store needs 2 cells
            if (CurrentBuildType == BuildType.GoldStore)
            {
                Vector2Int secondPos = new Vector2Int(gridPos.x + 1, gridPos.y);
                if (placedInfrastructure.ContainsKey(secondPos))
                {
                    return false;
                }
            }

            return true;
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
            return GameSettings.WorldToGrid(mouseWorld);
        }
    }
}
