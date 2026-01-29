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
        private LiftManager liftManager;
        private WallManager wallManager;
        private CellRenderer cellRenderer;
        private GhostStructureRenderer ghostRenderer;

        // Singleton instance
        private static SimulationManager instance;
        public static SimulationManager Instance => instance;

        // Public accessors
        public CellWorld World => world;
        public CellSimulatorJobbed Simulator => simulator;
        public ClusterManager ClusterManager => clusterManager;
        public TerrainColliderManager TerrainColliders => terrainColliders;
        public BeltManager BeltManager => beltManager;
        public LiftManager LiftManager => liftManager;
        public WallManager WallManager => wallManager;
        public CellRenderer CellRenderer => cellRenderer;
        public int WorldWidth => worldWidth;
        public int WorldHeight => worldHeight;

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

            // Create the world
            world = new CellWorld(worldWidth, worldHeight);

            // Create the multithreaded simulator
            simulator = new CellSimulatorJobbed();

            // Create cluster manager (handles rigid body physics)
            GameObject clusterManagerObj = new GameObject("ClusterManager");
            clusterManager = clusterManagerObj.AddComponent<ClusterManager>();
            clusterManager.Initialize(world);

            // Create terrain collider manager (for cluster-terrain collisions)
            terrainColliders = clusterManagerObj.AddComponent<TerrainColliderManager>();
            terrainColliders.Initialize(world);

            // Create belt manager
            beltManager = new BeltManager(world);

            // Create lift manager
            liftManager = new LiftManager(world);

            // Create wall manager
            wallManager = new WallManager(world);

            // Create renderer
            GameObject rendererObj = new GameObject("CellRenderer");
            cellRenderer = rendererObj.AddComponent<CellRenderer>();
            cellRenderer.Initialize(world);

            // Create ghost structure renderer (overlays for underground structures)
            GameObject ghostObj = new GameObject("GhostStructureRenderer");
            ghostRenderer = ghostObj.AddComponent<GhostStructureRenderer>();
            ghostRenderer.Initialize(beltManager, liftManager, wallManager, worldWidth, worldHeight);

            // Create graphics manager (handles visual effects)
            GameObject graphicsObj = new GameObject("GraphicsManager");
            graphicsObj.AddComponent<FallingSand.Graphics.GraphicsManager>();
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

            // Activate ghost structures whose terrain has been cleared
            PerformanceProfiler.StartTiming(TimingSlot.GhostStateUpdate);
            beltManager.UpdateGhostStates();
            liftManager.UpdateGhostStates();
            wallManager.UpdateGhostStates();
            PerformanceProfiler.StopTiming(TimingSlot.GhostStateUpdate);

            // Simulate physics (multithreaded) every frame
            // Gravity is applied at fixed interval (PhysicsSettings.GravityInterval)
            // clusterManager handles rigid body physics before cell simulation
            // beltManager applies horizontal force to clusters resting on belts
            // liftManager applies upward force to cells/clusters in lift zones
            // Note: Cell sim group timings are handled internally in CellSimulatorJobbed
            simulator.Simulate(world, clusterManager, beltManager, liftManager, wallManager);

            // Simulate belt movement (Burst-compiled parallel job)
            PerformanceProfiler.StartTiming(TimingSlot.BeltSimulation);
            JobHandle beltHandle = beltManager.ScheduleSimulateBelts(
                world.cells, world.chunks, world.materials,
                world.width, world.height,
                world.chunksX, world.chunksY,
                world.currentFrame);
            beltHandle.Complete();
            PerformanceProfiler.StopTiming(TimingSlot.BeltSimulation);

            // Upload texture changes (only dirty chunks)
            PerformanceProfiler.StartTiming(TimingSlot.RenderUpload);
            cellRenderer.UploadDirtyChunks();
            PerformanceProfiler.StopTiming(TimingSlot.RenderUpload);

            // Finalize frame timings
            PerformanceProfiler.EndFrame();
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

            wallManager?.Dispose();
            liftManager?.Dispose();
            beltManager?.Dispose();
            simulator?.Dispose();
            world?.Dispose();
        }
    }
}
