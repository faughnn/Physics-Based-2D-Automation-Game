using Unity.Jobs;
using UnityEngine;

namespace FallingSand
{
    /// <summary>
    /// Manages the core simulation systems. Shared by SandboxController (sandbox scene)
    /// and GameController (game scene) to eliminate code duplication.
    /// </summary>
    public class SimulationManager : MonoBehaviour
    {
        [Header("World Settings")]
        [SerializeField] private int worldWidth = 1024;
        [SerializeField] private int worldHeight = 512;

        // Core simulation systems
        private CellWorld world;
        private CellSimulatorJobbed simulator;
        private ClusterManager clusterManager;
        private TerrainColliderManager terrainColliders;
        private BeltManager beltManager;
        private CellRenderer cellRenderer;

        // Singleton instance
        private static SimulationManager instance;
        public static SimulationManager Instance => instance;

        // Public accessors
        public CellWorld World => world;
        public CellSimulatorJobbed Simulator => simulator;
        public ClusterManager ClusterManager => clusterManager;
        public TerrainColliderManager TerrainColliders => terrainColliders;
        public BeltManager BeltManager => beltManager;
        public CellRenderer CellRenderer => cellRenderer;
        public int WorldWidth => worldWidth;
        public int WorldHeight => worldHeight;

        private int frameCount = 0;

        private void Awake()
        {
            // Set up singleton
            if (instance != null && instance != this)
            {
                Debug.LogWarning("[SimulationManager] Duplicate instance detected, destroying this one");
                Destroy(gameObject);
                return;
            }
            instance = this;
        }

        /// <summary>
        /// Factory method to create a SimulationManager with specified dimensions.
        /// </summary>
        public static SimulationManager Create(int width, int height)
        {
            GameObject simObj = new GameObject("SimulationManager");
            SimulationManager manager = simObj.AddComponent<SimulationManager>();
            manager.worldWidth = width;
            manager.worldHeight = height;
            return manager;
        }

        /// <summary>
        /// Initialize all simulation systems. Called automatically in Start(),
        /// or can be called manually if created via Create().
        /// </summary>
        public void Initialize()
        {
            if (world != null)
            {
                Debug.LogWarning("[SimulationManager] Already initialized");
                return;
            }

            Debug.Log($"[SimulationManager] Initializing {worldWidth}x{worldHeight} world...");

            // Create the world
            world = new CellWorld(worldWidth, worldHeight);
            Debug.Log($"[SimulationManager] World created. Cells: {world.cells.Length}, Chunks: {world.chunks.Length}");

            // Create the multithreaded simulator
            simulator = new CellSimulatorJobbed();
            Debug.Log("[SimulationManager] CellSimulatorJobbed created");

            // Create cluster manager (handles rigid body physics)
            GameObject clusterManagerObj = new GameObject("ClusterManager");
            clusterManager = clusterManagerObj.AddComponent<ClusterManager>();
            clusterManager.Initialize(world);
            Debug.Log("[SimulationManager] ClusterManager created");

            // Create terrain collider manager (for cluster-terrain collisions)
            terrainColliders = clusterManagerObj.AddComponent<TerrainColliderManager>();
            terrainColliders.Initialize(world);
            Debug.Log("[SimulationManager] TerrainColliderManager created");

            // Create belt manager
            beltManager = new BeltManager(world);
            Debug.Log("[SimulationManager] BeltManager created");

            // Create renderer
            GameObject rendererObj = new GameObject("CellRenderer");
            cellRenderer = rendererObj.AddComponent<CellRenderer>();
            cellRenderer.Initialize(world);
            Debug.Log("[SimulationManager] CellRenderer initialized");

            // Create graphics manager (handles visual effects)
            GameObject graphicsObj = new GameObject("GraphicsManager");
            graphicsObj.AddComponent<FallingSand.Graphics.GraphicsManager>();
            Debug.Log("[SimulationManager] GraphicsManager created");

            Debug.Log($"[SimulationManager] === READY === World: {worldWidth}x{worldHeight} cells ({world.chunksX}x{world.chunksY} chunks)");
        }

        private void Start()
        {
            // Auto-initialize if not already done (for scene-placed instances)
            if (world == null)
            {
                Initialize();
            }
        }

        private void Update()
        {
            if (world == null) return;

            frameCount++;

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

        /// <summary>
        /// Returns recommended camera settings for viewing this simulation.
        /// </summary>
        public (float orthoSize, Vector3 position) GetRecommendedCameraSettings()
        {
            // Each cell = PixelsPerCell×PixelsPerCell pixels
            int pixelHeight = worldHeight * CoordinateUtils.PixelsPerCell;

            // Ortho size = half the height in world units
            float orthoSize = pixelHeight / 2f;
            Vector3 position = new Vector3(0, 0, -10);

            return (orthoSize, position);
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }

            beltManager?.Dispose();
            simulator?.Dispose();
            world?.Dispose();
        }
    }
}
