using UnityEngine;

namespace FallingSand
{
    /// <summary>
    /// Main controller for the Game scene. Manages the simulation,
    /// player spawning, and initial terrain creation.
    /// </summary>
    public class GameController : MonoBehaviour
    {
        [Header("World Settings")]
        [SerializeField] private int worldWidth = 1024;
        [SerializeField] private int worldHeight = 512;

        [Header("Player Settings")]
        [SerializeField] private Color playerColor = Color.cyan;
        [SerializeField] private Vector2 playerSpawnCell = new Vector2(512, 100);
        [SerializeField] private float moveSpeed = 200f;
        [SerializeField] private float jumpForce = 400f;

        private SimulationManager simulation;
        private GameObject player;
        private Camera mainCamera;

        private void Start()
        {
            Debug.Log("[GameController] Starting Game scene...");

            // Find or create SimulationManager
            simulation = SimulationManager.Instance;
            if (simulation == null)
            {
                simulation = SimulationManager.Create(worldWidth, worldHeight);
                simulation.Initialize();
            }
            Debug.Log("[GameController] SimulationManager ready");

            // Setup camera
            SetupCamera();

            // Create initial terrain (floor for player to stand on)
            CreateInitialTerrain();

            // Create the player
            CreatePlayer();

            Debug.Log($"[GameController] === READY === World: {simulation.WorldWidth}x{simulation.WorldHeight}");
            Debug.Log("[GameController] Controls: A/D or Arrows = Move, Space = Jump");
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

            Debug.Log($"[GameController] Camera setup. Ortho size: {orthoSize}, Pos: {position}");
        }

        private void CreateInitialTerrain()
        {
            var world = simulation.World;
            var terrainColliders = simulation.TerrainColliders;

            // Create a stone floor near the bottom
            // Y increases downward in cell space, so worldHeight - 50 is 50 cells from bottom
            int floorY = worldHeight - 50;
            int floorThickness = 10;

            Debug.Log($"[GameController] Creating stone floor at Y={floorY}, thickness={floorThickness}");

            for (int x = 0; x < worldWidth; x++)
            {
                for (int y = floorY; y < floorY + floorThickness; y++)
                {
                    world.SetCell(x, y, Materials.Stone);
                    terrainColliders.MarkChunkDirtyAt(x, y);
                }
            }

            Debug.Log($"[GameController] Floor created: {worldWidth}x{floorThickness} cells of stone");
        }

        private void CreatePlayer()
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
            Debug.Log($"[GameController] Physics2D.gravity set to {Physics2D.gravity}");

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

            // Position player in world coordinates
            // Cell to World: worldX = cellX * 2 - worldWidth, worldY = worldHeight - cellY * 2
            float worldX = playerSpawnCell.x * 2 - worldWidth;
            float worldY = worldHeight - playerSpawnCell.y * 2;
            player.transform.position = new Vector3(worldX, worldY, 0);

            Debug.Log($"[GameController] Player created at cell ({playerSpawnCell.x}, {playerSpawnCell.y}) -> world ({worldX}, {worldY})");
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
