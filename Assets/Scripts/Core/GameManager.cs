using UnityEngine;
using GoldRush.World;
using GoldRush.Player;
using GoldRush.Particles;
using GoldRush.Building;
using GoldRush.UI;

namespace GoldRush.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // References to major systems
        public WorldGenerator WorldGenerator { get; private set; }
        public PlayerController Player { get; private set; }
        public ParticlePool ParticlePool { get; private set; }
        public WaterReservoir WaterReservoir { get; private set; }
        public BuildSystem BuildSystem { get; private set; }
        public UIManager UIManager { get; private set; }

        private Camera mainCamera;

        private void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            Debug.Log("GameManager: Initializing Gold Rush...");

            // Initialize core systems
            InitializeSystems();

            // Generate world
            GenerateWorld();

            // Initialize particle pool
            InitializeParticlePool();

            // Initialize water reservoir
            InitializeWaterReservoir();

            // Spawn player
            SpawnPlayer();

            // Initialize build system
            InitializeBuildSystem();

            // Initialize UI
            InitializeUI();

            // Setup camera
            SetupCamera();

            Debug.Log("GameManager: Initialization complete!");
        }

        private void InitializeSystems()
        {
            // Initialize sprites first (needed by everything else)
            SpriteGenerator.Initialize();

            // Initialize physics layers and collision matrix
            LayerSetup.Initialize();
        }

        private void InitializeParticlePool()
        {
            GameObject poolGO = new GameObject("ParticlePool");
            ParticlePool = poolGO.AddComponent<ParticlePool>();
        }

        private void InitializeWaterReservoir()
        {
            GameObject reservoirGO = new GameObject("WaterReservoir");
            WaterReservoir = reservoirGO.AddComponent<WaterReservoir>();
            WaterReservoir.Initialize();
        }

        private void InitializeBuildSystem()
        {
            GameObject buildGO = new GameObject("BuildSystem");
            BuildSystem = buildGO.AddComponent<BuildSystem>();
            BuildSystem.Initialize();
        }

        private void InitializeUI()
        {
            GameObject uiGO = new GameObject("UIManager");
            UIManager = uiGO.AddComponent<UIManager>();
            UIManager.Initialize();
        }

        private void GenerateWorld()
        {
            // Create world generator
            GameObject worldGO = new GameObject("World");
            WorldGenerator = worldGO.AddComponent<WorldGenerator>();
            WorldGenerator.Generate();
        }

        private void SpawnPlayer()
        {
            // Create player GameObject
            GameObject playerGO = new GameObject("Player");

            // Add sprite renderer
            SpriteRenderer sr = playerGO.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteGenerator.GetSprite("Player");
            sr.sortingOrder = 10; // Player renders above terrain

            // Add rigidbody
            Rigidbody2D rb = playerGO.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            // Add collider
            BoxCollider2D col = playerGO.AddComponent<BoxCollider2D>();
            col.size = new Vector2(GameSettings.PlayerWidth / GameSettings.PixelsPerUnit,
                                   GameSettings.PlayerHeight / GameSettings.PixelsPerUnit);

            // Set layer
            playerGO.layer = LayerSetup.PlayerLayer;

            // Add controller scripts
            playerGO.AddComponent<PlayerInput>();
            Player = playerGO.AddComponent<PlayerController>();

            // Position player in the air above terrain
            int spawnGridX = GameSettings.WorldWidthCells / 2;
            int spawnGridY = GameSettings.WaterReservoirHeight + GameSettings.AirHeight - 2;
            Vector2 spawnPos = GameSettings.GridToWorld(spawnGridX, spawnGridY);
            playerGO.transform.position = spawnPos;

            Debug.Log($"GameManager: Player spawned at grid ({spawnGridX}, {spawnGridY})");
        }

        private void SetupCamera()
        {
            // Find or create main camera
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                GameObject camGO = new GameObject("Main Camera");
                camGO.tag = "MainCamera";
                mainCamera = camGO.AddComponent<Camera>();
                camGO.AddComponent<AudioListener>();
            }

            // Configure camera for 2D
            mainCamera.orthographic = true;
            mainCamera.backgroundColor = new Color(0.5f, 0.7f, 1f); // Sky blue background

            // Calculate orthographic size to fit world
            float worldHeightUnits = (GameSettings.WorldHeightCells * GameSettings.GridSize) / GameSettings.PixelsPerUnit;
            mainCamera.orthographicSize = worldHeightUnits / 2f;

            // Center camera on world
            mainCamera.transform.position = new Vector3(0, 0, -10);
        }

        // Public method to get terrain block at grid position
        public TerrainBlock GetTerrainAt(int gridX, int gridY)
        {
            if (WorldGenerator != null)
            {
                return WorldGenerator.GetTerrainAt(gridX, gridY);
            }
            return null;
        }

        // Public method to dig terrain at grid position
        public bool DigTerrainAt(int gridX, int gridY)
        {
            if (WorldGenerator != null)
            {
                return WorldGenerator.DigAt(gridX, gridY);
            }
            return false;
        }
    }
}
