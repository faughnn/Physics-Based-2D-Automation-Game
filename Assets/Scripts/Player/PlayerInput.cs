using UnityEngine;
using GoldRush.Core;
using GoldRush.World;
using GoldRush.Building;

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

        private Camera mainCamera;

        private void Start()
        {
            mainCamera = Camera.main;
        }

        private void Update()
        {
            // Read movement input
            HorizontalInput = Input.GetAxisRaw("Horizontal"); // A/D or Left/Right arrows

            // Read jump input
            JumpPressed = Input.GetButtonDown("Jump"); // W, Up, or Space

            // Handle digging (left click when not in build mode)
            if (Input.GetMouseButtonDown(0) && !IsInBuildMode)
            {
                TryDig();
            }

            // Handle infrastructure deletion (right click)
            if (Input.GetMouseButtonDown(1))
            {
                TryDelete();
            }
        }

        private void TryDig()
        {
            if (mainCamera == null) return;

            // Get mouse position in world coordinates
            Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);

            // Convert to grid position
            Vector2Int gridPos = GameSettings.WorldToGrid(mouseWorldPos);

            // Try to dig at that position
            GameManager.Instance.DigTerrainAt(gridPos.x, gridPos.y);
        }

        private void TryDelete()
        {
            if (mainCamera == null) return;

            Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int gridPos = GameSettings.WorldToGrid(mouseWorldPos);

            // Try to delete infrastructure at position
            if (BuildSystem.Instance != null)
            {
                BuildSystem.Instance.TryDeleteAt(gridPos);
            }
        }
    }
}
