using Unity.Jobs;
using UnityEngine;
using UnityEngine.InputSystem;
using FallingSand.Debugging;
using FallingSand.Graphics;
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

        private CellWorld world;
        private CellRenderer cellRenderer;
        private CellSimulatorJobbed simulator;
        private ClusterManager clusterManager;
        private ClusterTestSpawner clusterTestSpawner;
        private TerrainColliderManager terrainColliders;
        private BeltManager beltManager;
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

        public CellWorld World => world;
        public CellSimulatorJobbed Simulator => simulator;
        public byte CurrentMaterial => currentMaterial;
        public string CurrentMaterialName => currentMaterial < materialNames.Length ? materialNames[currentMaterial] : $"Material {currentMaterial}";
        public bool BeltMode => beltMode;
        public sbyte BeltDirection => beltDirection;
        public BeltManager BeltManager => beltManager;

        private void Start()
        {
            Debug.Log("[SandboxController] Start() called");

            // Get input devices
            mouse = Mouse.current;
            keyboard = Keyboard.current;
            Debug.Log($"[SandboxController] Mouse: {(mouse != null ? "found" : "NULL")}, Keyboard: {(keyboard != null ? "found" : "NULL")}");

            // Create the world
            Debug.Log($"[SandboxController] Creating world {worldWidth}x{worldHeight}...");
            world = new CellWorld(worldWidth, worldHeight);
            Debug.Log($"[SandboxController] World created. Cells: {world.cells.Length}, Chunks: {world.chunks.Length}");

            // Create the multithreaded simulator
            simulator = new CellSimulatorJobbed();
            Debug.Log("[SandboxController] CellSimulatorJobbed created");

            // Create cluster manager (handles rigid body physics)
            Debug.Log("[SandboxController] Creating ClusterManager...");
            GameObject clusterManagerObj = new GameObject("ClusterManager");
            clusterManager = clusterManagerObj.AddComponent<ClusterManager>();
            clusterManager.Initialize(world);
            Debug.Log("[SandboxController] ClusterManager created (Physics2D.autoSimulation disabled)");

            // Create cluster test spawner (handles 7/8/9 key spawning)
            clusterTestSpawner = clusterManagerObj.AddComponent<ClusterTestSpawner>();
            clusterTestSpawner.clusterManager = clusterManager;
            clusterTestSpawner.world = world;

            // Create terrain collider manager (for cluster-terrain collisions)
            terrainColliders = clusterManagerObj.AddComponent<TerrainColliderManager>();
            terrainColliders.Initialize(world);
            Debug.Log("[SandboxController] TerrainColliderManager created");

            // Create belt manager
            beltManager = new BeltManager(world);
            Debug.Log("[SandboxController] BeltManager created");

            // Create renderer
            Debug.Log("[SandboxController] Creating CellRenderer...");
            GameObject rendererObj = new GameObject("CellRenderer");
            cellRenderer = rendererObj.AddComponent<CellRenderer>();
            cellRenderer.Initialize(world);
            Debug.Log("[SandboxController] CellRenderer initialized");

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
            debugOverlay.RegisterSection(new SimulationDebugSection(this));
            debugOverlay.RegisterSection(new WorldDebugSection(world));
            debugOverlay.RegisterSection(new ClusterDebugSection(clusterManager, world));
            debugOverlay.RegisterSection(new InputDebugSection(this));

            // Create graphics manager (handles visual effects)
            GameObject graphicsObj = new GameObject("GraphicsManager");
            graphicsObj.AddComponent<GraphicsManager>();
            Debug.Log("[SandboxController] GraphicsManager created");

            // Create settings menu (ESC to toggle)
            GameObject settingsObj = new GameObject("SettingsMenu");
            settingsObj.AddComponent<SettingsMenu>();
            Debug.Log("[SandboxController] SettingsMenu created");

            Debug.Log($"[SandboxController] === READY === World: {worldWidth}x{worldHeight} cells ({world.chunksX}x{world.chunksY} chunks)");
            Debug.Log("[SandboxController] Cluster controls: 7=Circle, 8=Square, 9=L-Shape, [/]=Size");
            Debug.Log("[SandboxController] Belt controls: B=Toggle belt mode, Q/E=Rotate direction");
            Debug.Log("[SandboxController] Debug overlay: F3=Toggle, F4=Gizmos");
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

            // Each cell = 2×2 pixels
            const int PixelsPerCell = 2;
            int pixelHeight = worldHeight * PixelsPerCell; // 512 * 2 = 1024 pixels

            // Ortho size = half the height in world units
            mainCamera.orthographicSize = pixelHeight / 2f; // 512
            mainCamera.transform.position = new Vector3(0, 0, -10);
            mainCamera.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
        }

        private int frameCount = 0;

        private void Update()
        {
            frameCount++;

            HandleInput();
            HandleMaterialSelection();

            // Simulate physics (multithreaded) every frame
            // Gravity is applied at fixed interval (PhysicsSettings.GravityInterval)
            // clusterManager handles rigid body physics before cell simulation
            // beltManager applies horizontal force to clusters resting on belts
            simulator.Simulate(world, clusterManager, beltManager);

            // Simulate belt movement (Burst-compiled parallel job)
            JobHandle beltHandle = beltManager.ScheduleSimulateBelts(
                world.cells, world.chunks, world.materials,
                world.width, world.height,
                world.chunksX, world.chunksY,
                world.currentFrame);
            beltHandle.Complete();

            // Upload texture changes
            cellRenderer.UploadFullTexture();

            // Log active chunks periodically
            if (frameCount % 60 == 0)
            {
                int activeChunks = world.CountActiveChunks();
                int totalChunks = world.chunksX * world.chunksY;
                Debug.Log($"[Chunks] Active: {activeChunks}/{totalChunks} ({(activeChunks * 100f / totalChunks):F1}%)");
            }
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
            int cellX = Mathf.FloorToInt((mouseWorldPos.x + worldWidth) / 2f);
            int cellY = Mathf.FloorToInt((worldHeight - mouseWorldPos.y) / 2f); // Flip Y for Y=0 at top

            paintLogCount++;
            if (paintLogCount <= 5 || paintLogCount % 30 == 0)
            {
                Debug.Log($"[SandboxController] Paint #{paintLogCount}: screenPos={mousePos}, worldPos={mouseWorldPos}, cellPos=({cellX},{cellY}), material={materialId}, brush={brushSize}");
            }

            // Paint a circular brush
            int cellsPainted = 0;

            // Check if we're painting/erasing static materials (need to update terrain colliders)
            bool affectsColliders = materialId == Materials.Stone || materialId == Materials.Air;

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

            int cellX = Mathf.FloorToInt((mouseWorldPos.x + worldWidth) / 2f);
            int cellY = Mathf.FloorToInt((worldHeight - mouseWorldPos.y) / 2f);

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
            if (beltManager.PlaceBelt(cell.x, y, beltDirection))
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
            if (beltManager.RemoveBelt(cell.x, cell.y))
            {
                // Mark chunks dirty for terrain collider regeneration
                MarkBeltChunksDirty(gridX, gridY);
            }
        }

        private void MarkBeltChunksDirty(int gridX, int gridY)
        {
            // Mark all chunks covered by the 8x8 belt block
            for (int dy = 0; dy < 8; dy++)
            {
                for (int dx = 0; dx < 8; dx++)
                {
                    terrainColliders.MarkChunkDirtyAt(gridX + dx, gridY + dy);
                }
            }
        }

        private void OnDestroy()
        {
            beltManager?.Dispose();
            simulator?.Dispose();
            world?.Dispose();
        }
    }
}
