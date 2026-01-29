using UnityEngine;
using UnityEngine.InputSystem;

namespace FallingSand
{
    public enum PlacementMode
    {
        None,       // Normal gameplay
        Belt,       // Placing belts
        Lift,       // Placing lifts
        Wall,       // Placing walls
    }

    /// <summary>
    /// Result of placement validation: fully valid, valid but ghosted, or invalid.
    /// </summary>
    public enum PlacementResult
    {
        Valid,       // All cells are Air — place normally
        ValidGhost,  // Some cells are soft terrain — place as ghost
        Invalid,     // Hard material, out of bounds, or overlap
    }

    /// <summary>
    /// Handles structure placement in the Game scene.
    /// Checks progression unlocks before allowing placement.
    /// Attached to the Player GameObject.
    /// </summary>
    public class StructurePlacementController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera mainCamera;

        [Header("Visual Feedback")]
        [SerializeField] private Color validPreviewColor = new Color(0f, 1f, 0f, 0.5f);
        [SerializeField] private Color ghostPreviewColor = new Color(0.3f, 0.7f, 1f, 0.5f);
        [SerializeField] private Color invalidPreviewColor = new Color(1f, 0f, 0f, 0.5f);

        private SimulationManager simulation;
        private PlacementMode currentMode = PlacementMode.None;
        private sbyte beltDirection = 1;  // +1 = right, -1 = left

        // Drag state for structure placement
        private bool isDragging = false;
        private int dragLockedY = -1;  // For belt horizontal drag
        private int dragLockedX = -1;  // For lift vertical drag

        // Preview visualization
        private GameObject previewObject;
        private SpriteRenderer previewRenderer;
        private Texture2D previewTexture;

        // Input
        private Keyboard keyboard;
        private Mouse mouse;

        // Locked structure feedback
        private string lockedMessage = "";
        private float lockedMessageTimer = 0f;
        private const float LockedMessageDuration = 2f;

        public PlacementMode CurrentMode => currentMode;
        public sbyte BeltDirection => beltDirection;

        private void Start()
        {
            simulation = SimulationManager.Instance;
            keyboard = Keyboard.current;
            mouse = Mouse.current;

            if (mainCamera == null)
                mainCamera = Camera.main;

            CreatePreviewObject();
        }

        private void Update()
        {
            HandleModeSelection();

            if (currentMode != PlacementMode.None)
            {
                HandlePlacementInput();
                UpdatePreview();
            }

            // Update locked message timer
            if (lockedMessageTimer > 0)
                lockedMessageTimer -= Time.deltaTime;
        }

        private void HandleModeSelection()
        {
            if (keyboard == null) return;

            // B key - Belt placement
            if (keyboard.bKey.wasPressedThisFrame)
            {
                if (ProgressionManager.Instance != null && ProgressionManager.Instance.IsUnlocked(Ability.PlaceBelts))
                {
                    ToggleMode(PlacementMode.Belt);
                }
                else
                {
                    ShowLockedMessage("Belts not yet unlocked!");
                }
            }

            // L key - Lift placement
            if (keyboard.lKey.wasPressedThisFrame)
            {
                if (ProgressionManager.Instance != null && ProgressionManager.Instance.IsUnlocked(Ability.PlaceLifts))
                {
                    ToggleMode(PlacementMode.Lift);
                }
                else
                {
                    ShowLockedMessage("Lifts not yet unlocked!");
                }
            }

            // W key - Wall placement (uses same unlock as Lifts)
            if (keyboard.wKey.wasPressedThisFrame)
            {
                if (ProgressionManager.Instance != null && ProgressionManager.Instance.IsUnlocked(Ability.PlaceLifts))
                {
                    ToggleMode(PlacementMode.Wall);
                }
                else
                {
                    ShowLockedMessage("Walls not yet unlocked!");
                }
            }

            // F8 - Debug: unlock all structures immediately
            if (keyboard.f8Key.wasPressedThisFrame)
            {
                if (ProgressionManager.Instance != null)
                {
                    ProgressionManager.Instance.ForceUnlock(Ability.PlaceBelts);
                    ProgressionManager.Instance.ForceUnlock(Ability.PlaceLifts);
                    Debug.Log("[Debug] All structures unlocked");
                }
            }

            // Escape cancels placement mode
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                ExitPlacementMode();
            }

            // Q/E to rotate belt direction (only in belt mode)
            if (currentMode == PlacementMode.Belt)
            {
                if (keyboard.qKey.wasPressedThisFrame)
                    beltDirection = -1;
                if (keyboard.eKey.wasPressedThisFrame)
                    beltDirection = 1;
            }
        }

        private void ToggleMode(PlacementMode mode)
        {
            if (currentMode == mode)
            {
                ExitPlacementMode();
            }
            else
            {
                currentMode = mode;
                previewObject.SetActive(true);
            }
        }

        private void ExitPlacementMode()
        {
            currentMode = PlacementMode.None;
            isDragging = false;
            dragLockedY = -1;
            dragLockedX = -1;
            previewObject.SetActive(false);
        }

        private void HandlePlacementInput()
        {
            if (mouse == null) return;

            switch (currentMode)
            {
                case PlacementMode.Belt:
                    HandleBeltPlacement();
                    break;
                case PlacementMode.Lift:
                    HandleLiftPlacement();
                    break;
                case PlacementMode.Wall:
                    HandleWallPlacement();
                    break;
            }
        }

        private void HandleBeltPlacement()
        {
            Vector2Int cellPos = GetCellAtMouse();

            // Start drag
            if (mouse.leftButton.wasPressedThisFrame)
            {
                isDragging = true;
                dragLockedY = BeltManager.SnapToGrid(cellPos.y);
                TryPlaceBelt(cellPos.x, dragLockedY);
            }
            // Continue drag
            else if (mouse.leftButton.isPressed && isDragging)
            {
                TryPlaceBelt(cellPos.x, dragLockedY);
            }
            // End drag
            else if (mouse.leftButton.wasReleasedThisFrame)
            {
                isDragging = false;
                dragLockedY = -1;
            }
            // Remove belt
            else if (mouse.rightButton.isPressed)
            {
                TryRemoveBelt(cellPos.x, cellPos.y);
            }
        }

        private void TryPlaceBelt(int x, int y)
        {
            int gridX = BeltManager.SnapToGrid(x);
            int gridY = BeltManager.SnapToGrid(y);

            if (simulation.BeltManager.PlaceBelt(gridX, gridY, beltDirection))
            {
                // Mark terrain collider chunks dirty
                MarkStructureChunksDirty(gridX, gridY);
            }
        }

        private void TryRemoveBelt(int x, int y)
        {
            int gridX = BeltManager.SnapToGrid(x);
            int gridY = BeltManager.SnapToGrid(y);

            if (simulation.BeltManager.RemoveBelt(gridX, gridY))
            {
                MarkStructureChunksDirty(gridX, gridY);
            }
        }

        private void HandleLiftPlacement()
        {
            Vector2Int cellPos = GetCellAtMouse();

            // Start drag - lock the X position for vertical line placement
            if (mouse.leftButton.wasPressedThisFrame)
            {
                isDragging = true;
                dragLockedX = LiftManager.SnapToGrid(cellPos.x);
                TryPlaceLift(dragLockedX, cellPos.y);
            }
            // Continue drag - use locked X
            else if (mouse.leftButton.isPressed && isDragging)
            {
                TryPlaceLift(dragLockedX, cellPos.y);
            }
            // End drag
            else if (mouse.leftButton.wasReleasedThisFrame)
            {
                isDragging = false;
                dragLockedX = -1;
            }
            // Remove lift
            else if (mouse.rightButton.isPressed)
            {
                TryRemoveLift(cellPos.x, cellPos.y);
            }
        }

        private void TryPlaceLift(int x, int y)
        {
            int gridX = LiftManager.SnapToGrid(x);
            int gridY = LiftManager.SnapToGrid(y);

            simulation.LiftManager.PlaceLift(gridX, gridY);
        }

        private void TryRemoveLift(int x, int y)
        {
            simulation.LiftManager.RemoveLift(x, y);
        }

        private void HandleWallPlacement()
        {
            Vector2Int cellPos = GetCellAtMouse();

            // Simple click-to-place (no drag locking needed for walls)
            if (mouse.leftButton.isPressed)
            {
                TryPlaceWall(cellPos.x, cellPos.y);
            }
            else if (mouse.rightButton.isPressed)
            {
                TryRemoveWall(cellPos.x, cellPos.y);
            }
        }

        private void TryPlaceWall(int x, int y)
        {
            int gridX = WallManager.SnapToGrid(x);
            int gridY = WallManager.SnapToGrid(y);

            if (simulation.WallManager.PlaceWall(gridX, gridY))
            {
                MarkStructureChunksDirty(gridX, gridY);
            }
        }

        private void TryRemoveWall(int x, int y)
        {
            int gridX = WallManager.SnapToGrid(x);
            int gridY = WallManager.SnapToGrid(y);

            if (simulation.WallManager.RemoveWall(gridX, gridY))
            {
                MarkStructureChunksDirty(gridX, gridY);
            }
        }

        private void MarkStructureChunksDirty(int gridX, int gridY)
        {
            var terrainColliders = simulation.TerrainColliders;
            for (int dy = 0; dy < 8; dy++)
            {
                for (int dx = 0; dx < 8; dx++)
                {
                    terrainColliders.MarkChunkDirtyAt(gridX + dx, gridY + dy);
                }
            }
        }

        private void UpdatePreview()
        {
            if (!previewObject.activeSelf) return;

            Vector2Int cellPos = GetCellAtMouse();
            int gridX, gridY;

            if (currentMode == PlacementMode.Belt)
            {
                gridX = BeltManager.SnapToGrid(cellPos.x);
                gridY = isDragging && dragLockedY >= 0 ? dragLockedY : BeltManager.SnapToGrid(cellPos.y);
            }
            else if (currentMode == PlacementMode.Lift)
            {
                gridX = isDragging && dragLockedX >= 0 ? dragLockedX : LiftManager.SnapToGrid(cellPos.x);
                gridY = LiftManager.SnapToGrid(cellPos.y);
            }
            else // Wall mode - simple grid snap
            {
                gridX = WallManager.SnapToGrid(cellPos.x);
                gridY = WallManager.SnapToGrid(cellPos.y);
            }

            // Position preview at center of 8x8 block
            Vector2 worldPos = CoordinateUtils.CellToWorld(gridX + 3.5f, gridY + 3.5f,
                simulation.WorldWidth, simulation.WorldHeight);

            previewObject.transform.position = new Vector3(worldPos.x, worldPos.y, 0);

            // Check if placement is valid
            PlacementResult result = CanPlaceStructureAt(gridX, gridY);
            previewRenderer.color = result switch
            {
                PlacementResult.Valid => validPreviewColor,
                PlacementResult.ValidGhost => ghostPreviewColor,
                _ => invalidPreviewColor,
            };
        }

        private PlacementResult CanPlaceStructureAt(int gridX, int gridY)
        {
            var world = simulation.World;

            // Check bounds
            if (!world.IsInBounds(gridX, gridY) ||
                !world.IsInBounds(gridX + 7, gridY + 7))
                return PlacementResult.Invalid;

            bool anyGhost = false;

            // Check if area is clear
            for (int dy = 0; dy < 8; dy++)
            {
                for (int dx = 0; dx < 8; dx++)
                {
                    int cx = gridX + dx;
                    int cy = gridY + dy;

                    // Check for existing belt
                    if (simulation.BeltManager.HasBeltAt(cx, cy))
                        return PlacementResult.Invalid;

                    // Check for existing lift
                    if (simulation.LiftManager.HasLiftAt(cx, cy))
                        return PlacementResult.Invalid;

                    // Check for existing wall
                    if (simulation.WallManager.HasWallAt(cx, cy))
                        return PlacementResult.Invalid;

                    byte mat = world.GetCell(cx, cy);
                    if (mat == Materials.Air)
                        continue;

                    // Lift materials are allowed (for lift re-placement)
                    if (Materials.IsLift(mat))
                        continue;

                    if (Materials.IsSoftTerrain(mat))
                    {
                        anyGhost = true;
                        continue;
                    }

                    // Hard material — invalid
                    return PlacementResult.Invalid;
                }
            }

            return anyGhost ? PlacementResult.ValidGhost : PlacementResult.Valid;
        }

        private void CreatePreviewObject()
        {
            previewObject = new GameObject("PlacementPreview");
            previewRenderer = previewObject.AddComponent<SpriteRenderer>();

            // Create an 8x8 block sprite (16x16 world units at CellToWorldScale=2)
            previewTexture = new Texture2D(8, 8);
            previewTexture.filterMode = FilterMode.Point;
            Color[] pixels = new Color[64];
            for (int i = 0; i < 64; i++)
                pixels[i] = Color.white;
            previewTexture.SetPixels(pixels);
            previewTexture.Apply();

            float blockWorldSize = 8 * CoordinateUtils.CellToWorldScale;
            previewRenderer.sprite = Sprite.Create(previewTexture, new Rect(0, 0, 8, 8),
                new Vector2(0.5f, 0.5f), 8f / blockWorldSize);
            previewRenderer.sortingOrder = 100;  // Above everything

            previewObject.SetActive(false);
        }

        private Vector2Int GetCellAtMouse()
        {
            Vector2 mousePos = mouse.position.ReadValue();
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0));
            return CoordinateUtils.WorldToCell(worldPos, simulation.WorldWidth, simulation.WorldHeight);
        }

        private void ShowLockedMessage(string message)
        {
            lockedMessage = message;
            lockedMessageTimer = LockedMessageDuration;
        }

        private void OnGUI()
        {
            // Show current mode indicator
            if (currentMode != PlacementMode.None)
            {
                DrawModeIndicator();
            }

            // Show locked message
            if (lockedMessageTimer > 0 && !string.IsNullOrEmpty(lockedMessage))
            {
                DrawLockedMessage();
            }
        }

        private void DrawModeIndicator()
        {
            string modeText = currentMode switch
            {
                PlacementMode.Belt => $"BELT MODE (Dir: {(beltDirection > 0 ? "Right" : "Left")}) - Q/E rotate, LMB place, RMB remove, ESC cancel",
                PlacementMode.Lift => "LIFT MODE - LMB place, RMB remove, ESC cancel",
                PlacementMode.Wall => "WALL MODE - LMB place, RMB remove, ESC cancel",
                _ => ""
            };

            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft
            };
            style.normal.textColor = Color.yellow;

            Rect rect = new Rect(20, Screen.height - 40, 600, 30);
            GUI.Label(rect, modeText, style);
        }

        private void DrawLockedMessage()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            float alpha = Mathf.Min(1f, lockedMessageTimer);
            style.normal.textColor = new Color(1f, 0.3f, 0.3f, alpha);  // Red

            Rect rect = new Rect(0, Screen.height / 2f, Screen.width, 40);
            GUI.Label(rect, lockedMessage, style);
        }

        private void OnDestroy()
        {
            if (previewObject != null)
                Destroy(previewObject);
            if (previewTexture != null)
                Destroy(previewTexture);
        }
    }
}
