using UnityEngine;
using FallingSand.Debugging;

namespace FallingSand
{
    /// <summary>
    /// Main controller for the Game scene. Manages the simulation,
    /// player spawning, and initial terrain creation.
    /// </summary>
    public class GameController : MonoBehaviour
    {
        [Header("World Settings")]
        [SerializeField] private int worldWidth = 1920;
        [SerializeField] private int worldHeight = 1620;

        [Header("Viewport Settings")]
        [SerializeField] private int viewportHeight = 540;   // Cells visible vertically (width derived from aspect ratio)

        [Header("Player Settings")]
        [SerializeField] private Color playerColor = Color.cyan;

        [Header("Item Settings")]
        [SerializeField] private Color shovelColor = new Color(0.6f, 0.4f, 0.2f); // Brown

        private SimulationManager simulation;
        private GameObject player;
        private Camera mainCamera;
        private LevelLoader levelLoader;
        private Hotbar hotbar;
        private InventoryMenu inventoryMenu;

        private void Start()
        {
            // 1. Use level-defined world dimensions (overrides serialized fields)
            worldWidth = TutorialLevelData.WorldWidth;
            worldHeight = TutorialLevelData.WorldHeight;

            // 2. Find or create SimulationManager with correct dimensions
            simulation = SimulationManager.Instance;
            if (simulation == null)
            {
                simulation = SimulationManager.Create(worldWidth, worldHeight);
                simulation.Initialize();
            }

            // 3. Setup camera
            SetupCamera();

            // 4. Create ProgressionManager (must exist before Bucket)
            CreateProgressionManager();

            // 5. Load level terrain
            levelLoader = new LevelLoader(simulation);
            var levelData = TutorialLevelData.Create();
            levelLoader.LoadLevel(levelData);

            // 5b. Generate terrain colliders immediately (before player spawns)
            simulation.TerrainColliders.ProcessDirtyChunksWithLog();

            // 6. Register all level objectives
            foreach (var objective in levelData.Objectives)
            {
                ProgressionManager.Instance.AddObjective(objective);
            }

            // 7. Create the player at level-defined spawn
            CreatePlayer(levelData.PlayerSpawn);

            // 8. Setup camera follow (after player exists)
            SetupCameraFollow();

            // 9. Force camera to player position (CameraFollow.Initialize may not work correctly on first frame)
            Vector2 playerWorldPos = CoordinateUtils.CellToWorld(
                levelData.PlayerSpawn.x, levelData.PlayerSpawn.y, worldWidth, worldHeight);
            mainCamera.transform.position = new Vector3(playerWorldPos.x, playerWorldPos.y, -10);
            Debug.Log($"Camera positioned at player: {playerWorldPos}, cell spawn: {levelData.PlayerSpawn}");

            // 10. Spawn shovel item
            CreateShovelItem(levelData.ShovelSpawn);

            // 11. Create all buckets
            CreateBuckets(levelData);

            // 12. Add structure placement controller to player
            player.AddComponent<StructurePlacementController>();

            // 13. Add tool range indicator to player
            var rangeIndicator = player.AddComponent<ToolRangeIndicator>();
            rangeIndicator.Initialize(
                player.GetComponent<PlayerController>(),
                player.GetComponent<DiggingController>(),
                player.GetComponent<CellGrabSystem>()
            );

            // 14. Create game UI (Canvas, Hotbar, Inventory Menu)
            CreateGameUI();

            // 15. Create progression UI (if not already created)
            if (FindFirstObjectByType<ProgressionUI>() == null)
            {
                GameObject uiObj = new GameObject("ProgressionUI");
                uiObj.AddComponent<ProgressionUI>();
            }

            // 16. Create debug overlay
            CreateDebugOverlay();
        }

        private void CreateGameUI()
        {
            var canvas = GameUIBuilder.CreateCanvas();
            var playerCtrl = player.GetComponent<PlayerController>();
            var progression = ProgressionManager.Instance;

            // Hotbar
            GameObject hotbarObj = new GameObject("Hotbar");
            hotbarObj.transform.SetParent(canvas.transform, false);
            hotbar = hotbarObj.AddComponent<Hotbar>();
            hotbar.Initialize(playerCtrl, progression);

            // Inventory Menu
            GameObject menuObj = new GameObject("InventoryMenu");
            menuObj.transform.SetParent(canvas.transform, false);
            inventoryMenu = menuObj.AddComponent<InventoryMenu>();
            inventoryMenu.Initialize(hotbar, playerCtrl, progression, canvas);
        }

        private void CreateDebugOverlay()
        {
            if (DebugOverlay.Instance != null) return;

            GameObject debugObj = new GameObject("DebugOverlay");
            var overlay = debugObj.AddComponent<DebugOverlay>();

            overlay.RegisterSection(new SimulationDebugSection(simulation));
            overlay.RegisterSection(new GameDebugSection(
                player.GetComponent<PlayerController>(), worldWidth, worldHeight));
        }

        private void SetupCamera()
        {
            // Destroy any existing main camera to ensure clean state
            mainCamera = Camera.main;
            if (mainCamera != null)
            {
                // Remove any existing CameraFollow that might interfere
                var existingFollow = mainCamera.GetComponent<CameraFollow>();
                if (existingFollow != null)
                {
                    Destroy(existingFollow);
                }
            }
            else
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                mainCamera = camObj.AddComponent<Camera>();
            }

            // Setup orthographic camera for viewport (not full world)
            mainCamera.orthographic = true;

            // Viewport dimensions determine ortho size
            // Formula: orthoSize = viewportHeightCells * PixelsPerCell / 2
            // For 540 cells: orthoSize = 540 * 2 / 2 = 540 world units
            mainCamera.orthographicSize = viewportHeight * CoordinateUtils.PixelsPerCell / 2f;

            // Temporary position - will be set to player spawn in step 8
            mainCamera.transform.position = new Vector3(0, 0, -10);
            mainCamera.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
        }

        private void SetupCameraFollow()
        {
            CameraFollow follow = mainCamera.gameObject.AddComponent<CameraFollow>();

            // World bounds in world units
            // World spans from (-worldWidth, -worldHeight) to (+worldWidth, +worldHeight)
            float worldHalfWidth = worldWidth;   // Cell width = world half-width in units
            float worldHalfHeight = worldHeight; // Cell height = world half-height in units

            follow.Initialize(
                player.transform,
                -worldHalfWidth, worldHalfWidth,   // World X bounds
                -worldHalfHeight, worldHalfHeight  // World Y bounds
            );

            // Force snap to player position after initialization
            follow.SnapToTarget();
        }

        private void CreateProgressionManager()
        {
            if (ProgressionManager.Instance == null)
            {
                GameObject pmObj = new GameObject("ProgressionManager");
                pmObj.AddComponent<ProgressionManager>();
            }
        }

        private void CreatePlayer(Vector2Int spawnCell)
        {
            player = new GameObject("Player");

            // Rigidbody2D - dynamic body affected by physics
            var rb = player.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 1f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;  // Don't rotate
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // Set gravity to match cell simulation
            float unityGravity = PhysicsSettings.GetUnityGravity();
            Physics2D.gravity = new Vector2(0, unityGravity);

            // BoxCollider2D - 8x16 cells = 16x32 world units
            var collider = player.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(16, 32);

            // Visual - SpriteRenderer with a white square sprite, tinted
            var sr = player.AddComponent<SpriteRenderer>();
            sr.sprite = CreateRectSprite(16, 32);
            sr.color = playerColor;
            sr.sortingOrder = 10;  // Render above terrain

            // PlayerController - handles movement and jumping
            var controller = player.AddComponent<PlayerController>();

            // CellGrabSystem - grab and drop loose cells (requires Shovel equipped)
            player.AddComponent<CellGrabSystem>();

            // DiggingController - dig Ground material with shovel
            player.AddComponent<DiggingController>();

            // Position player in world coordinates using CoordinateUtils
            Vector2 worldPos = CoordinateUtils.CellToWorld(spawnCell.x, spawnCell.y, worldWidth, worldHeight);
            player.transform.position = new Vector3(worldPos.x, worldPos.y, 0);
            Debug.Log($"Player created at cell {spawnCell} -> world {worldPos}");
        }

        /// <summary>
        /// Creates a simple rectangular sprite with the given dimensions.
        /// </summary>
        private Sprite CreateRectSprite(int width, int height)
        {
            // Create a small white texture
            Texture2D tex = new Texture2D(width, height);
            tex.filterMode = FilterMode.Point;

            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }
            tex.SetPixels(pixels);
            tex.Apply();

            // Create sprite with pivot at center
            return Sprite.Create(
                tex,
                new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f),
                1f  // 1 pixel per unit
            );
        }

        private void CreateShovelItem(Vector2Int cellPosition)
        {
            GameObject item = new GameObject("Shovel");

            // Visual - sprite is 16x64 pixels at PPU=2, resulting in 8x32 world units (player height)
            var sr = item.AddComponent<SpriteRenderer>();
            sr.sprite = CreateShovelSprite();
            sr.color = shovelColor;
            sr.sortingOrder = 5; // Below player (10), above terrain

            // Trigger collider for pickup - matches sprite size (8x32 world units)
            var collider = item.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(8, 32);
            collider.isTrigger = true;

            // Item component
            item.AddComponent<WorldItem>();

            // Position in world coordinates using CoordinateUtils
            Vector2 worldPos = CoordinateUtils.CellToWorld(cellPosition.x, cellPosition.y, worldWidth, worldHeight);
            item.transform.position = new Vector3(worldPos.x, worldPos.y, 0);
        }

        private Sprite CreateShovelSprite()
        {
            // 16x64 pixel texture at PPU=2 = 8x32 world units (same height as player)
            int width = 16, height = 64;
            Texture2D tex = new Texture2D(width, height);
            tex.filterMode = FilterMode.Point;

            Color[] pixels = new Color[width * height];
            // Fill with transparent
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            // Draw shovel shape
            int bladeHeight = 20;  // Bottom portion - wider blade
            int handleWidth = 4;   // Thin handle

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Handle (thin, top portion)
                    if (y >= bladeHeight && x >= (width - handleWidth) / 2 && x < (width + handleWidth) / 2)
                        pixels[y * width + x] = Color.white;
                    // Blade (wider, bottom portion)
                    else if (y < bladeHeight && x >= 2 && x < width - 2)
                        pixels[y * width + x] = Color.white;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            // PPU=2: 16x64 pixels becomes 8x32 world units
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 2f);
        }

        private void CreateBuckets(LevelData levelData)
        {
            int count = Mathf.Min(levelData.BucketSpawns.Count, levelData.Objectives.Count);

            for (int i = 0; i < count; i++)
            {
                var position = levelData.BucketSpawns[i];
                var objective = levelData.Objectives[i];

                // Determine if this bucket starts inactive (has a prerequisite)
                bool startsInactive = !string.IsNullOrEmpty(objective.prerequisiteId);

                CreateBucket(position, objective, startsInactive);
            }
        }

        private void CreateBucket(Vector2Int cellPosition, ObjectiveData objective, bool startsInactive)
        {
            GameObject bucketObj = new GameObject($"Bucket_{objective.objectiveId}");
            Bucket bucket = bucketObj.AddComponent<Bucket>();
            bucket.Initialize(simulation.World, cellPosition, objective.objectiveId, startsInactive);
            bucket.SetObjective(objective);
        }

        /// <summary>
        /// Gets the player GameObject.
        /// </summary>
        public GameObject Player => player;

        /// <summary>
        /// Gets the SimulationManager instance.
        /// </summary>
        public SimulationManager Simulation => simulation;
    }
}
