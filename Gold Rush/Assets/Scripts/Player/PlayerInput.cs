using UnityEngine;
using GoldRush.Core;
using GoldRush.World;
using GoldRush.Building;
using GoldRush.UI;

namespace GoldRush.Player
{
    public class PlayerInput : MonoBehaviour
    {
        // Movement input
        public float HorizontalInput { get; private set; }
        public bool JumpPressed { get; private set; }

        // Build mode
        public bool IsInBuildMode => BuildSystem.Instance != null &&
                                     BuildSystem.Instance.CurrentBuildType != BuildType.None;

        // Pickup tool active
        public bool IsPickupToolActive => PickupTool.Instance != null && PickupTool.Instance.IsEnabled;

        private Camera mainCamera;

        // Dig preview
        private GameObject digPreview;
        private SpriteRenderer digPreviewRenderer;
        private const int DigRadius = 12;  // Simulation cells
        private static readonly Color PreviewColor = new Color(1f, 1f, 1f, 0.5f);

        // Dig cooldown
        private float digCooldown;
        private const float DigCooldownTime = 0.05f;  // Fast digging (20 digs/second)

        // Delete drag
        private bool isDeletingDrag;
        private Vector2 deleteStartWorldPos;
        private Vector2 deleteCurrentWorldPos;

        private void Start()
        {
            mainCamera = Camera.main;
            CreateDigPreview();
        }

        private void CreateDigPreview()
        {
            digPreview = new GameObject("DigPreview");
            digPreviewRenderer = digPreview.AddComponent<SpriteRenderer>();
            // DigRadius is in simulation cells, each cell is 2 pixels
            digPreviewRenderer.sprite = SpriteGenerator.CreateSemiCircleSprite(DigRadius * 2, PreviewColor);
            digPreviewRenderer.sortingOrder = 50;
            digPreview.SetActive(false);
        }

        private void Update()
        {
            // Read movement input using direct key detection
            HorizontalInput = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                HorizontalInput = -1f;
            else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                HorizontalInput = 1f;

            // Read jump input
            JumpPressed = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);

            // Update cooldown
            if (digCooldown > 0)
                digCooldown -= Time.deltaTime;

            // Update dig preview
            UpdateDigPreview();

            // Handle digging (hold left click to continuously dig)
            // Disabled when in build mode or pickup tool is active
            if (Input.GetMouseButton(0) && !IsInBuildMode && !IsPickupToolActive && digCooldown <= 0)
            {
                TryDig();
                digCooldown = DigCooldownTime;
            }

            // Handle infrastructure deletion (drag to delete in a line)
            if (!IsInBuildMode)
            {
                Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);

                if (Input.GetMouseButtonDown(1))
                {
                    isDeletingDrag = true;
                    deleteStartWorldPos = mouseWorldPos;
                }

                if (isDeletingDrag)
                {
                    deleteCurrentWorldPos = mouseWorldPos;
                }

                if (Input.GetMouseButtonUp(1) && isDeletingDrag)
                {
                    isDeletingDrag = false;
                    DeleteInLine(deleteStartWorldPos, deleteCurrentWorldPos);
                }
            }

            // Toggle build menu with Tab
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                ToggleBuildMenu();
            }
        }

        private void UpdateDigPreview()
        {
            if (mainCamera == null || digPreview == null) return;

            // Hide preview if in build mode, pickup tool active, or on cooldown
            if (IsInBuildMode || IsPickupToolActive || digCooldown > 0)
            {
                digPreview.SetActive(false);
                return;
            }

            // Get mouse position in world
            Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector2 playerPos = transform.position;

            // Calculate direction from player to mouse
            Vector2 direction = (mouseWorldPos - playerPos).normalized;

            // Position preview at mouse
            digPreview.transform.position = mouseWorldPos;

            // Rotate preview so curved edge faces dig direction
            // The sprite is drawn with curved edge facing right (positive X)
            // So we rotate to align positive X with the direction vector
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            digPreview.transform.rotation = Quaternion.Euler(0, 0, angle);

            // Show preview
            digPreview.SetActive(true);
        }

        private void ToggleBuildMenu()
        {
            if (UIManager.Instance != null && UIManager.Instance.BuildMenu != null)
            {
                if (UIManager.Instance.BuildMenu.IsOpen)
                    UIManager.Instance.BuildMenu.Close();
                else
                    UIManager.Instance.BuildMenu.Open();
            }
        }

        private void TryDig()
        {
            if (mainCamera == null) return;
            if (GameManager.Instance == null) return;

            // Get mouse position in world coordinates
            Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector2 playerPos = transform.position;

            // Calculate direction from player to mouse
            Vector2 direction = (mouseWorldPos - playerPos).normalized;

            // Convert to grid position
            Vector2Int gridPos = GameSettings.WorldToGrid(mouseWorldPos);

            // Try to dig at that position with direction
            GameManager.Instance.DigTerrainAt(gridPos.x, gridPos.y, mouseWorldPos, direction);
        }

        private void TryDeleteAtWorldPos(Vector2 worldPos)
        {
            if (BuildSystem.Instance == null) return;

            // Try each unique grid type for deletion
            if (BuildSystem.Instance.TryDeleteAt(PlacementGrid.Sub.FromWorld(worldPos))) return;
            if (BuildSystem.Instance.TryDeleteAt(PlacementGrid.Main.FromWorld(worldPos))) return;
            BuildSystem.Instance.TryDeleteAt(PlacementGrid.Shaker.FromWorld(worldPos));
        }

        private void DeleteInLine(Vector2 startWorld, Vector2 endWorld)
        {
            if (BuildSystem.Instance == null) return;

            // Calculate distance and step
            float distance = Vector2.Distance(startWorld, endWorld);

            // Step size based on smallest grid (16 pixels = 0.5 world units)
            float stepSize = 0.25f;
            int steps = Mathf.Max(1, Mathf.CeilToInt(distance / stepSize));

            // Delete along the line
            for (int i = 0; i <= steps; i++)
            {
                float t = steps > 0 ? (float)i / steps : 0f;
                Vector2 pos = Vector2.Lerp(startWorld, endWorld, t);
                TryDeleteAtWorldPos(pos);
            }
        }
    }
}
