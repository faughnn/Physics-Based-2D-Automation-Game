using UnityEngine;
using UnityEngine.InputSystem;
using FallingSand.Debugging;
using FallingSand.UI;

namespace FallingSand
{
    public enum SandboxMode
    {
        Paint,
        Belt,
        Lift,
        Wall,
        Piston
    }

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

        // Placement mode
        private SandboxMode currentMode = SandboxMode.Paint;
        private sbyte beltDirection = 1;  // +1 = right, -1 = left
        private bool beltDragging = false;
        private int beltDragY = -1;  // Snapped Y position locked during drag
        private bool liftDragging = false;
        private int liftDragX = -1;  // Snapped X position locked during drag (vertical line)
        private PistonDirection pistonDirection = PistonDirection.Right;

        // Input references
        private Mouse mouse;
        private Keyboard keyboard;

        public CellWorld World => simulation?.World;
        public CellSimulatorJobbed Simulator => simulation?.Simulator;
        public byte CurrentMaterial => currentMaterial;
        public string CurrentMaterialName => currentMaterial < materialNames.Length ? materialNames[currentMaterial] : $"Material {currentMaterial}";
        public SandboxMode CurrentMode => currentMode;
        public sbyte BeltDirection => beltDirection;
        public BeltManager BeltManager => simulation?.BeltManager;
        public LiftManager LiftManager => simulation?.LiftManager;
        public WallManager WallManager => simulation?.WallManager;
        public PistonDirection PistonDirection => pistonDirection;
        public PistonManager PistonManager => simulation?.MachineManager?.Pistons;

        private void Start()
        {
            // Get input devices
            mouse = Mouse.current;
            keyboard = Keyboard.current;

            // Find or create SimulationManager
            simulation = SimulationManager.Instance;
            if (simulation == null)
            {
                simulation = SimulationManager.Create(worldWidth, worldHeight);
                simulation.Initialize();
            }

            // Create cluster test spawner (handles 7/8/9 key spawning)
            // Attach to ClusterManager's GameObject
            GameObject clusterManagerObj = simulation.ClusterManager.gameObject;
            clusterTestSpawner = clusterManagerObj.AddComponent<ClusterTestSpawner>();
            clusterTestSpawner.clusterManager = simulation.ClusterManager;
            clusterTestSpawner.world = simulation.World;

            // Setup camera
            SetupCamera();

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
            debugOverlay.RegisterSection(new LiftDebugSection(simulation.LiftManager, simulation.WorldWidth, simulation.WorldHeight));

            // Create settings menu (ESC to toggle)
            GameObject settingsObj = new GameObject("SettingsMenu");
            settingsObj.AddComponent<SettingsMenu>();
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

            switch (currentMode)
            {
                case SandboxMode.Belt:
                    if (mouse.leftButton.wasPressedThisFrame)
                    {
                        Vector2Int cell = GetCellAtMouse();
                        beltDragY = BeltManager.SnapToGrid(cell.y);
                        beltDragging = true;
                        PlaceBeltAtMouse();
                    }
                    else if (mouse.leftButton.isPressed && beltDragging)
                        PlaceBeltAtMouse();
                    else if (mouse.leftButton.wasReleasedThisFrame)
                    {
                        beltDragging = false;
                        beltDragY = -1;
                    }
                    else if (mouse.rightButton.isPressed)
                        RemoveBeltAtMouse();
                    break;

                case SandboxMode.Lift:
                    if (mouse.leftButton.wasPressedThisFrame)
                    {
                        Vector2Int cell = GetCellAtMouse();
                        liftDragX = LiftManager.SnapToGrid(cell.x);
                        liftDragging = true;
                        PlaceLiftAtMouse();
                    }
                    else if (mouse.leftButton.isPressed && liftDragging)
                        PlaceLiftAtMouse();
                    else if (mouse.leftButton.wasReleasedThisFrame)
                    {
                        liftDragging = false;
                        liftDragX = -1;
                    }
                    else if (mouse.rightButton.isPressed)
                        RemoveLiftAtMouse();
                    break;

                case SandboxMode.Wall:
                    if (mouse.leftButton.isPressed)
                        PlaceWallAtMouse();
                    else if (mouse.rightButton.isPressed)
                        RemoveWallAtMouse();
                    break;

                case SandboxMode.Piston:
                    if (mouse.leftButton.isPressed)
                        PlacePistonAtMouse();
                    else if (mouse.rightButton.isPressed)
                        RemovePistonAtMouse();
                    break;

                default: // Paint
                    if (mouse.leftButton.isPressed)
                        PaintAtMouse(currentMaterial);
                    else if (mouse.rightButton.isPressed)
                        PaintAtMouse(Materials.Air);
                    break;
            }

            // Adjust brush size with scroll wheel (only in paint mode)
            if (currentMode == SandboxMode.Paint)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (scroll != 0)
                {
                    brushSize = Mathf.Clamp(brushSize + (scroll > 0 ? 1 : -1), 1, 50);
                }
            }
        }

        private void ToggleMode(SandboxMode mode)
        {
            currentMode = (currentMode == mode) ? SandboxMode.Paint : mode;
        }

        private void HandleMaterialSelection()
        {
            if (keyboard == null) return;

            if (keyboard.bKey.wasPressedThisFrame) ToggleMode(SandboxMode.Belt);
            if (keyboard.lKey.wasPressedThisFrame) ToggleMode(SandboxMode.Lift);
            if (keyboard.wKey.wasPressedThisFrame) ToggleMode(SandboxMode.Wall);
            if (keyboard.pKey.wasPressedThisFrame) ToggleMode(SandboxMode.Piston);

            // Q/E to rotate belt direction or piston direction
            if (currentMode == SandboxMode.Belt)
            {
                if (keyboard.qKey.wasPressedThisFrame) beltDirection = -1;
                if (keyboard.eKey.wasPressedThisFrame) beltDirection = 1;
            }
            else if (currentMode == SandboxMode.Piston)
            {
                if (keyboard.qKey.wasPressedThisFrame)
                    pistonDirection = (PistonDirection)(((int)pistonDirection + 3) % 4);
                if (keyboard.eKey.wasPressedThisFrame)
                    pistonDirection = (PistonDirection)(((int)pistonDirection + 1) % 4);
            }

            // Number keys to select materials (only in paint mode)
            if (currentMode == SandboxMode.Paint)
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

        private void PaintAtMouse(byte materialId)
        {
            Vector2 mousePos = mouse.position.ReadValue();
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0));

            // Convert world position to cell coordinates
            Vector2Int cell = CoordinateUtils.WorldToCell(mouseWorldPos, simulation.WorldWidth, simulation.WorldHeight);
            int cellX = cell.x;
            int cellY = cell.y;

            // Paint a circular brush
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
                    }
                }
            }
        }

        private Vector2Int GetCellAtMouse()
        {
            return CoordinateUtils.ScreenToCell(mainCamera, mouse.position.ReadValue(), simulation.WorldWidth, simulation.WorldHeight);
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
                MarkStructureChunksDirty(gridX, gridY);
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
                MarkStructureChunksDirty(gridX, gridY);
            }
        }

        private void MarkStructureChunksDirty(int gridX, int gridY, int size = 8)
        {
            StructureUtils.MarkChunksDirtyForBlock(simulation.TerrainColliders, gridX, gridY, size);
        }

        private void PlaceLiftAtMouse()
        {
            Vector2Int cell = GetCellAtMouse();
            // Use locked X during drag for vertical line placement
            int x = liftDragging && liftDragX >= 0 ? liftDragX : cell.x;

            simulation.LiftManager.PlaceLift(x, cell.y);
        }

        private void RemoveLiftAtMouse()
        {
            Vector2Int cell = GetCellAtMouse();
            simulation.LiftManager.RemoveLift(cell.x, cell.y);
        }

        private void PlaceWallAtMouse()
        {
            Vector2Int cell = GetCellAtMouse();
            int gridX = WallManager.SnapToGrid(cell.x);
            int gridY = WallManager.SnapToGrid(cell.y);

            if (simulation.WallManager.PlaceWall(gridX, gridY))
            {
                // Mark chunks dirty for terrain collider regeneration
                MarkStructureChunksDirty(gridX, gridY);
            }
        }

        private void RemoveWallAtMouse()
        {
            Vector2Int cell = GetCellAtMouse();
            int gridX = WallManager.SnapToGrid(cell.x);
            int gridY = WallManager.SnapToGrid(cell.y);

            if (simulation.WallManager.RemoveWall(gridX, gridY))
            {
                // Mark chunks dirty for terrain collider regeneration
                MarkStructureChunksDirty(gridX, gridY);
            }
        }


        private void PlacePistonAtMouse()
        {
            Vector2Int cell = GetCellAtMouse();
            simulation.MachineManager.Pistons.PlacePiston(cell.x, cell.y, pistonDirection);
        }

        private void RemovePistonAtMouse()
        {
            Vector2Int cell = GetCellAtMouse();
            simulation.MachineManager.Pistons.RemovePiston(cell.x, cell.y);
        }

        // Note: OnDestroy() removed - SimulationManager handles disposal of simulation resources
    }
}
