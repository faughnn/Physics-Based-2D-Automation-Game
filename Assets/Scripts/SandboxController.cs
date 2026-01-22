using UnityEngine;
using UnityEngine.InputSystem;
using FallingSand.Debugging;
using FallingSand.UI;

namespace FallingSand
{
    public class SandboxController : MonoBehaviour
    {
        [Header("World Settings")]
        [SerializeField] private int worldWidth = 1024;
        [SerializeField] private int worldHeight = 512;

        [Header("Brush Settings")]
        [SerializeField] private int brushSize = 5;
        [SerializeField] private byte currentMaterial = Materials.Sand;

        [Header("References")]
        [SerializeField] private Shader worldShader;

        private SimulationManager simulation;
        private ClusterTestSpawner clusterTestSpawner;
        private DebugOverlay debugOverlay;
        private Camera mainCamera;

        // Material names for display
        private readonly string[] materialNames = { "Air", "Stone", "Sand", "Water", "Oil", "Steam" };

        // Belt placement mode
        private bool beltMode = false;
        private sbyte beltDirection = 1;  // +1 = right, -1 = left
        private bool beltDragging = false;
        private int beltDragY = -1;  // Snapped Y position locked during drag

        // Input references
        private Mouse mouse;
        private Keyboard keyboard;

        public CellWorld World => simulation?.World;
        public CellSimulatorJobbed Simulator => simulation?.Simulator;
        public byte CurrentMaterial => currentMaterial;
        public string CurrentMaterialName => currentMaterial < materialNames.Length ? materialNames[currentMaterial] : $"Material {currentMaterial}";
        public bool BeltMode => beltMode;
        public sbyte BeltDirection => beltDirection;
        public BeltManager BeltManager => simulation?.BeltManager;

        private void Start()
        {
            Debug.Log("[SandboxController] Start() called");

            // Get input devices
            mouse = Mouse.current;
            keyboard = Keyboard.current;
            Debug.Log($"[SandboxController] Mouse: {(mouse != null ? "found" : "NULL")}, Keyboard: {(keyboard != null ? "found" : "NULL")}");

            // Find or create SimulationManager
            simulation = SimulationManager.Instance;
            if (simulation == null)
            {
                simulation = SimulationManager.Create(worldWidth, worldHeight);
                simulation.Initialize();
            }
            Debug.Log("[SandboxController] SimulationManager ready");

            // Create cluster test spawner (handles 7/8/9 key spawning)
            // Attach to ClusterManager's GameObject
            GameObject clusterManagerObj = simulation.ClusterManager.gameObject;
            clusterTestSpawner = clusterManagerObj.AddComponent<ClusterTestSpawner>();
            clusterTestSpawner.clusterManager = simulation.ClusterManager;
            clusterTestSpawner.world = simulation.World;

            // Setup camera
            Debug.Log("[SandboxController] Setting up camera...");
            SetupCamera();
            Debug.Log($"[SandboxController] Camera setup complete. Ortho size: {mainCamera.orthographicSize}, Pos: {mainCamera.transform.position}");

            // Give test spawner camera reference
            clusterTestSpawner.mainCamera = mainCamera;

            // Create unified debug overlay
            GameObject debugObj = new GameObject("DebugOverlay");
            debugOverlay = debugObj.AddComponent<DebugOverlay>();

            // Register debug sections
            debugOverlay.RegisterSection(new SimulationDebugSection(simulation));
            debugOverlay.RegisterSection(new WorldDebugSection(simulation.World));
            debugOverlay.RegisterSection(new ClusterDebugSection(simulation.ClusterManager, simulation.World));
            debugOverlay.RegisterSection(new InputDebugSection(this));

            // Create settings menu (ESC to toggle)
            GameObject settingsObj = new GameObject("SettingsMenu");
            settingsObj.AddComponent<SettingsMenu>();
            Debug.Log("[SandboxController] SettingsMenu created");

            Debug.Log($"[SandboxController] === READY === World: {simulation.WorldWidth}x{simulation.WorldHeight} cells");
            Debug.Log("[SandboxController] Cluster controls: 7=Circle, 8=Square, 9=L-Shape, [/]=Size");
            Debug.Log("[SandboxController] Belt controls: B=Toggle belt mode, Q/E=Rotate direction");
            Debug.Log("[SandboxController] Debug overlay: F3=Toggle, F4=Gizmos");
            Debug.Log("[SandboxController] Materials: 1=Air, 2=Stone, 3=Sand, 4=Water, 5=Oil, 6=Steam, D=Dirt");
        }

        private void SetupCamera()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                mainCamera = camObj.AddComponent<Camera>();
            }

            // Setup orthographic camera to view the world
            mainCamera.orthographic = true;

            // Get recommended settings from SimulationManager
            var (orthoSize, position) = simulation.GetRecommendedCameraSettings();
            mainCamera.orthographicSize = orthoSize;
            mainCamera.transform.position = position;
            mainCamera.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
        }

        private void Update()
        {
            HandleInput();
            HandleMaterialSelection();
            // Simulation loop is now handled by SimulationManager
        }

        private void HandleInput()
        {
            if (mouse == null) return;

            if (beltMode)
            {
                // Belt placement mode with horizontal line snapping
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    // Start of drag - lock the Y position
                    Vector2Int cell = GetCellAtMouse();
                    beltDragY = BeltManager.SnapToGrid(cell.y);
                    beltDragging = true;
                    PlaceBeltAtMouse();
                }
                else if (mouse.leftButton.isPressed && beltDragging)
                {
                    // Continue drag - use locked Y
                    PlaceBeltAtMouse();
                }
                else if (mouse.leftButton.wasReleasedThisFrame)
                {
                    // End of drag
                    beltDragging = false;
                    beltDragY = -1;
                }
                else if (mouse.rightButton.isPressed)
                {
                    RemoveBeltAtMouse();
                }
            }
            else
            {
                // Normal paint mode
                if (mouse.leftButton.isPressed)
                {
                    PaintAtMouse(currentMaterial);
                }
                else if (mouse.rightButton.isPressed)
                {
                    PaintAtMouse(Materials.Air);
                }
            }

            // Adjust brush size with scroll wheel (only in paint mode)
            if (!beltMode)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (scroll != 0)
                {
                    brushSize = Mathf.Clamp(brushSize + (scroll > 0 ? 1 : -1), 1, 50);
                }
            }
        }

        private void HandleMaterialSelection()
        {
            if (keyboard == null) return;

            // B key toggles belt mode
            if (keyboard.bKey.wasPressedThisFrame)
            {
                beltMode = !beltMode;
                Debug.Log($"[SandboxController] Belt mode: {(beltMode ? "ON" : "OFF")} (direction: {(beltDirection > 0 ? "RIGHT" : "LEFT")})");
            }

            // Q/E to rotate belt direction
            if (keyboard.qKey.wasPressedThisFrame)
            {
                beltDirection = -1;
                Debug.Log("[SandboxController] Belt direction: LEFT");
            }
            if (keyboard.eKey.wasPressedThisFrame)
            {
                beltDirection = 1;
                Debug.Log("[SandboxController] Belt direction: RIGHT");
            }

            // Number keys to select materials (only when not in belt mode)
            if (!beltMode)
            {
                if (keyboard.digit1Key.wasPressedThisFrame) currentMaterial = Materials.Air;
                if (keyboard.digit2Key.wasPressedThisFrame) currentMaterial = Materials.Stone;
                if (keyboard.digit3Key.wasPressedThisFrame) currentMaterial = Materials.Sand;
                if (keyboard.digit4Key.wasPressedThisFrame) currentMaterial = Materials.Water;
                if (keyboard.digit5Key.wasPressedThisFrame) currentMaterial = Materials.Oil;
                if (keyboard.digit6Key.wasPressedThisFrame) currentMaterial = Materials.Steam;
                if (keyboard.dKey.wasPressedThisFrame) currentMaterial = Materials.Dirt;
            }
        }

        private int paintLogCount = 0;

        private void PaintAtMouse(byte materialId)
        {
            Vector2 mousePos = mouse.position.ReadValue();
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0));

            // Convert world position to cell coordinates
            // Quad spans from -worldWidth to +worldWidth (2x scale)
            // So world X range is -worldWidth to +worldWidth
            // Cell X = (worldX + worldWidth) / 2
            int cellX = Mathf.FloorToInt((mouseWorldPos.x + simulation.WorldWidth) / 2f);
            int cellY = Mathf.FloorToInt((simulation.WorldHeight - mouseWorldPos.y) / 2f); // Flip Y for Y=0 at top

            paintLogCount++;
            if (paintLogCount <= 5 || paintLogCount % 30 == 0)
            {
                Debug.Log($"[SandboxController] Paint #{paintLogCount}: screenPos={mousePos}, worldPos={mouseWorldPos}, cellPos=({cellX},{cellY}), material={materialId}, brush={brushSize}");
            }

            // Paint a circular brush
            int cellsPainted = 0;

            // Check if we're painting/erasing static materials (need to update terrain colliders)
            bool affectsColliders = materialId == Materials.Stone || materialId == Materials.Air;
            var world = simulation.World;
            var terrainColliders = simulation.TerrainColliders;

            for (int dy = -brushSize; dy <= brushSize; dy++)
            {
                for (int dx = -brushSize; dx <= brushSize; dx++)
                {
                    if (dx * dx + dy * dy <= brushSize * brushSize)
                    {
                        int px = cellX + dx;
                        int py = cellY + dy;

                        // Check if we're modifying static material
                        if (affectsColliders && world.IsInBounds(px, py))
                        {
                            byte existingMat = world.GetCell(px, py);
                            if (existingMat == Materials.Stone || materialId == Materials.Stone)
                            {
                                terrainColliders.MarkChunkDirtyAt(px, py);
                            }
                        }

                        world.SetCell(px, py, materialId);
                        cellsPainted++;
                    }
                }
            }

            if (paintLogCount <= 5)
            {
                Debug.Log($"[SandboxController] Painted {cellsPainted} cells");
            }
        }

        private Vector2Int GetCellAtMouse()
        {
            Vector2 mousePos = mouse.position.ReadValue();
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0));

            int cellX = Mathf.FloorToInt((mouseWorldPos.x + simulation.WorldWidth) / 2f);
            int cellY = Mathf.FloorToInt((simulation.WorldHeight - mouseWorldPos.y) / 2f);

            return new Vector2Int(cellX, cellY);
        }

        private void PlaceBeltAtMouse()
        {
            Vector2Int cell = GetCellAtMouse();
            // Use locked Y during drag for horizontal line placement
            int y = beltDragging && beltDragY >= 0 ? beltDragY : cell.y;

            // Get snapped position for marking chunks dirty
            int gridX = BeltManager.SnapToGrid(cell.x);
            int gridY = BeltManager.SnapToGrid(y);

            // BeltManager handles grid snapping internally
            if (simulation.BeltManager.PlaceBelt(cell.x, y, beltDirection))
            {
                // Mark chunks dirty for terrain collider regeneration (belts are static)
                MarkBeltChunksDirty(gridX, gridY);
            }
        }

        private void RemoveBeltAtMouse()
        {
            Vector2Int cell = GetCellAtMouse();

            // Get snapped position for marking chunks dirty
            int gridX = BeltManager.SnapToGrid(cell.x);
            int gridY = BeltManager.SnapToGrid(cell.y);

            // BeltManager handles grid snapping internally
            if (simulation.BeltManager.RemoveBelt(cell.x, cell.y))
            {
                // Mark chunks dirty for terrain collider regeneration
                MarkBeltChunksDirty(gridX, gridY);
            }
        }

        private void MarkBeltChunksDirty(int gridX, int gridY)
        {
            // Mark all chunks covered by the 8x8 belt block
            var terrainColliders = simulation.TerrainColliders;
            for (int dy = 0; dy < 8; dy++)
            {
                for (int dx = 0; dx < 8; dx++)
                {
                    terrainColliders.MarkChunkDirtyAt(gridX + dx, gridY + dy);
                }
            }
        }
        // Note: OnDestroy() removed - SimulationManager handles disposal of simulation resources
    }
}
