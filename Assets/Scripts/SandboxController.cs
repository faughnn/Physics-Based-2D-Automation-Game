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

        // Simulation speed is now in PhysicsSettings (shared with cluster physics)

        [Header("References")]
        [SerializeField] private Shader worldShader;

        private CellWorld world;
        private CellRenderer cellRenderer;
        private CellSimulatorJobbed simulator;
        private ClusterManager clusterManager;
        private ClusterTestSpawner clusterTestSpawner;
        private TerrainColliderManager terrainColliders;
        private DebugOverlay debugOverlay;
        private Camera mainCamera;

        // Material names for display
        private readonly string[] materialNames = { "Air", "Stone", "Sand", "Water", "Oil", "Steam" };

        // Input references
        private Mouse mouse;
        private Keyboard keyboard;

        public CellWorld World => world;
        public CellSimulatorJobbed Simulator => simulator;
        public byte CurrentMaterial => currentMaterial;
        public string CurrentMaterialName => currentMaterial < materialNames.Length ? materialNames[currentMaterial] : $"Material {currentMaterial}";
        public int SimulationSpeed => PhysicsSettings.SimulationSpeed;

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

            Debug.Log($"[SandboxController] === READY === World: {worldWidth}x{worldHeight} cells ({world.chunksX}x{world.chunksY} chunks)");
            Debug.Log("[SandboxController] Cluster controls: 7=Circle, 8=Square, 9=L-Shape, [/]=Size");
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
            // SimulationSpeed controls gravity rate (1=full speed, higher=slower acceleration)
            // clusterManager handles rigid body physics before cell simulation
            simulator.Simulate(world, PhysicsSettings.SimulationSpeed, clusterManager);

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

            // Paint with left mouse button
            if (mouse.leftButton.isPressed)
            {
                PaintAtMouse(currentMaterial);
            }
            // Erase with right mouse button
            else if (mouse.rightButton.isPressed)
            {
                PaintAtMouse(Materials.Air);
            }

            // Adjust brush size with scroll wheel
            float scroll = mouse.scroll.ReadValue().y;
            if (scroll != 0)
            {
                brushSize = Mathf.Clamp(brushSize + (scroll > 0 ? 1 : -1), 1, 50);
            }
        }

        private void HandleMaterialSelection()
        {
            if (keyboard == null) return;

            // Number keys to select materials
            if (keyboard.digit1Key.wasPressedThisFrame) currentMaterial = Materials.Air;
            if (keyboard.digit2Key.wasPressedThisFrame) currentMaterial = Materials.Stone;
            if (keyboard.digit3Key.wasPressedThisFrame) currentMaterial = Materials.Sand;
            if (keyboard.digit4Key.wasPressedThisFrame) currentMaterial = Materials.Water;
            if (keyboard.digit5Key.wasPressedThisFrame) currentMaterial = Materials.Oil;
            if (keyboard.digit6Key.wasPressedThisFrame) currentMaterial = Materials.Steam;

            // Numpad +/- to adjust simulation speed (shared with cluster physics)
            if (keyboard.numpadPlusKey.wasPressedThisFrame)
                PhysicsSettings.SimulationSpeed = Mathf.Clamp(PhysicsSettings.SimulationSpeed - 1,
                    PhysicsSettings.MinSimulationSpeed, PhysicsSettings.MaxSimulationSpeed);  // Faster
            if (keyboard.numpadMinusKey.wasPressedThisFrame)
                PhysicsSettings.SimulationSpeed = Mathf.Clamp(PhysicsSettings.SimulationSpeed + 1,
                    PhysicsSettings.MinSimulationSpeed, PhysicsSettings.MaxSimulationSpeed);  // Slower
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

        private void OnDestroy()
        {
            simulator?.Dispose();
            world?.Dispose();
        }
    }
}
