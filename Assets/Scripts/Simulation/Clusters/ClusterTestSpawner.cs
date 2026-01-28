using UnityEngine;
using UnityEngine.InputSystem;

namespace FallingSand
{
    /// <summary>
    /// Handles test cluster spawning with keyboard shortcuts.
    /// Keys: 7=Circle, 8=Square, 9=L-Shape, [/]=Size adjustment
    /// </summary>
    public class ClusterTestSpawner : MonoBehaviour
    {
        [Header("References")]
        public ClusterManager clusterManager;
        public CellWorld world;
        public Camera mainCamera;

        [Header("Test Spawning")]
        public byte testMaterial = Materials.Stone;
        public int testSize = 15;

        private Mouse mouse;
        private Keyboard keyboard;

        private void Start()
        {
            mouse = Mouse.current;
            keyboard = Keyboard.current;
        }

        private void Update()
        {
            HandleTestInput();
        }

        private void HandleTestInput()
        {
            if (clusterManager == null || mainCamera == null || world == null) return;
            if (mouse == null || keyboard == null) return;

            Vector2 mouseScreen = mouse.position.ReadValue();
            Vector2 mouseWorld = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, 0));

            // Number keys spawn different cluster shapes
            if (keyboard.digit7Key.wasPressedThisFrame)
            {
                ClusterFactory.CreateCircleCluster(mouseWorld, testSize, testMaterial, clusterManager);
            }
            if (keyboard.digit8Key.wasPressedThisFrame)
            {
                ClusterFactory.CreateSquareCluster(mouseWorld, testSize, testMaterial, clusterManager);
            }
            if (keyboard.digit9Key.wasPressedThisFrame)
            {
                ClusterFactory.CreateLShapeCluster(mouseWorld, testSize * 2, testMaterial, clusterManager);
            }

            // Adjust test size with [ and ]
            if (keyboard.leftBracketKey.wasPressedThisFrame)
            {
                testSize = Mathf.Max(2, testSize - 1);
            }
            if (keyboard.rightBracketKey.wasPressedThisFrame)
            {
                testSize = Mathf.Min(20, testSize + 1);
            }
        }
    }
}
