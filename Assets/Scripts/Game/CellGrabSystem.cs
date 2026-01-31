using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FallingSand
{
    /// <summary>
    /// Allows grabbing loose cells (powders, liquids, gases) from the world
    /// and dropping them elsewhere. Tied to the Shovel tool.
    /// </summary>
    public class CellGrabSystem : MonoBehaviour
    {
        [Header("Grab Settings")]
        [SerializeField] private float grabRadius = 8f;      // In cells
        [SerializeField] private int maxGrabCount = 500;     // Prevent grabbing too many cells
        [SerializeField] private float maxGrabDistance = 600f; // World units (3x shovel range)

        // Storage: materialId -> count of grabbed cells
        private Dictionary<byte, int> grabbedCells = new Dictionary<byte, int>();

        // Offset storage for preview: each grabbed cell's offset from grab center + material
        private List<(int dx, int dy, byte materialId)> grabbedCellOffsets = new List<(int, int, byte)>();

        // State
        private bool isHolding = false;
        private int totalGrabbedCount = 0;

        // References
        private SimulationManager simulation;
        private CellWorld world;
        private Camera mainCamera;
        private Mouse mouse;
        private PlayerController player;

        // Preview rendering
        private GameObject previewObject;
        private SpriteRenderer previewRenderer;
        private Texture2D previewTexture;

        private void Start()
        {
            // Get references
            simulation = SimulationManager.Instance;
            if (simulation == null)
            {
                Debug.LogError("[CellGrabSystem] No SimulationManager found!");
                enabled = false;
                return;
            }
            world = simulation.World;

            mainCamera = Camera.main;
            mouse = Mouse.current;

            // Find player controller (we're likely attached to player or nearby)
            player = GetComponent<PlayerController>();
            if (player == null)
            {
                player = FindFirstObjectByType<PlayerController>();
            }
        }

        private void Update()
        {
            if (mouse == null || world == null) return;
            if (GameInput.IsPointerOverUI()) return;

            // Only active when Grabber is equipped
            if (player == null || player.EquippedTool != ToolType.Grabber)
                return;

            // Range check - block grab/drop if cursor is too far from player
            Vector2 mouseScreen = mouse.position.ReadValue();
            Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, 0));
            float distToMouse = Vector2.Distance(transform.position, (Vector2)mouseWorld);
            if (distToMouse > maxGrabDistance)
                return;

            // Get current cell position under mouse
            Vector2Int cellPos = GetCellAtMouse();

            // Grab on left mouse button press
            if (mouse.leftButton.wasPressedThisFrame)
            {
                GrabCellsAtPosition(cellPos.x, cellPos.y);
                isHolding = totalGrabbedCount > 0;
                if (isHolding)
                    CreateGrabPreview();
            }
            // Drop on release
            else if (mouse.leftButton.wasReleasedThisFrame && isHolding)
            {
                DropCellsAtPosition(cellPos.x, cellPos.y);
                isHolding = false;
                DestroyGrabPreview();
            }
        }

        private void LateUpdate()
        {
            if (previewObject == null) return;

            // Hide preview if tool switched away from Grabber
            if (player == null || player.EquippedTool != ToolType.Grabber)
            {
                previewObject.SetActive(false);
                return;
            }

            previewObject.SetActive(true);

            if (mouse == null || mainCamera == null) return;
            Vector2 mouseScreen = mouse.position.ReadValue();
            Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, 0));
            previewObject.transform.position = new Vector3(mouseWorld.x, mouseWorld.y, 0f);
        }

        private void CreateGrabPreview()
        {
            if (grabbedCellOffsets.Count == 0) return;

            // Compute bounding box
            int minDx = int.MaxValue, maxDx = int.MinValue;
            int minDy = int.MaxValue, maxDy = int.MinValue;
            foreach (var (dx, dy, _) in grabbedCellOffsets)
            {
                if (dx < minDx) minDx = dx;
                if (dx > maxDx) maxDx = dx;
                if (dy < minDy) minDy = dy;
                if (dy > maxDy) maxDy = dy;
            }

            int texWidth = maxDx - minDx + 1;
            int texHeight = maxDy - minDy + 1;

            // Create texture
            previewTexture = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false);
            previewTexture.filterMode = FilterMode.Point;

            // Fill with transparent
            var clearPixels = new Color[texWidth * texHeight];
            previewTexture.SetPixels(clearPixels);

            // Set each grabbed cell pixel
            foreach (var (dx, dy, matId) in grabbedCellOffsets)
            {
                int px = dx - minDx;
                // Flip Y: cell Y+ is down, texture Y+ is up
                int py = (maxDy - minDy) - (dy - minDy);
                Color c = world.materials[matId].baseColour;
                c.a = 0.5f;
                previewTexture.SetPixel(px, py, c);
            }
            previewTexture.Apply();

            // Create sprite: pixelsPerUnit=0.5 means each pixel = 2 world units = 1 cell
            Rect rect = new Rect(0, 0, texWidth, texHeight);
            Vector2 pivot = new Vector2(
                (float)(-minDx) / texWidth,
                (float)(maxDy) / texHeight  // pivot at grab center in flipped coords
            );
            Sprite sprite = Sprite.Create(previewTexture, rect, pivot, 0.5f);

            // Create game object
            previewObject = new GameObject("GrabPreview");
            previewRenderer = previewObject.AddComponent<SpriteRenderer>();
            previewRenderer.sprite = sprite;
            previewRenderer.sortingOrder = 100;
        }

        private void DestroyGrabPreview()
        {
            if (previewObject != null)
            {
                Destroy(previewObject);
                previewObject = null;
                previewRenderer = null;
            }
            if (previewTexture != null)
            {
                Destroy(previewTexture);
                previewTexture = null;
            }
        }

        /// <summary>
        /// Converts mouse screen position to cell coordinates.
        /// </summary>
        private Vector2Int GetCellAtMouse()
        {
            Vector2 mousePos = mouse.position.ReadValue();
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0));

            return CoordinateUtils.WorldToCell(mouseWorldPos, simulation.WorldWidth, simulation.WorldHeight);
        }

        /// <summary>
        /// Checks if a cell at (x, y) can be grabbed.
        /// Grabbable: non-air, non-static behaviour, not owned by cluster.
        /// </summary>
        private bool IsCellGrabbable(int x, int y)
        {
            if (!world.IsInBounds(x, y))
                return false;

            int index = y * world.width + x;
            Cell cell = world.cells[index];

            // Skip air
            if (cell.materialId == Materials.Air)
                return false;

            // Skip cluster-owned cells
            if (cell.ownerId != 0)
                return false;

            // Check behaviour type - only grab moveable materials
            MaterialDef mat = world.materials[cell.materialId];
            if (mat.behaviour == BehaviourType.Static)
                return false;

            return true;
        }

        /// <summary>
        /// Grabs all grabbable cells within grabRadius of the center position.
        /// </summary>
        private void GrabCellsAtPosition(int centerX, int centerY)
        {
            int radiusInt = Mathf.CeilToInt(grabRadius);
            float radiusSq = grabRadius * grabRadius;

            for (int dy = -radiusInt; dy <= radiusInt; dy++)
            {
                for (int dx = -radiusInt; dx <= radiusInt; dx++)
                {
                    // Check circular distance
                    if (dx * dx + dy * dy > radiusSq)
                        continue;

                    // Enforce max grab count
                    if (totalGrabbedCount >= maxGrabCount)
                        return;

                    int x = centerX + dx;
                    int y = centerY + dy;

                    if (IsCellGrabbable(x, y))
                    {
                        grabbedCellOffsets.Add((dx, dy, world.cells[y * world.width + x].materialId));
                        GrabCell(x, y);
                    }
                }
            }
        }

        /// <summary>
        /// Grabs a single cell at (x, y), removing it from the world.
        /// </summary>
        private void GrabCell(int x, int y)
        {
            int index = y * world.width + x;
            byte materialId = world.cells[index].materialId;

            // Track grabbed material
            if (!grabbedCells.ContainsKey(materialId))
                grabbedCells[materialId] = 0;
            grabbedCells[materialId]++;
            totalGrabbedCount++;

            // Remove from world
            world.SetCell(x, y, Materials.Air);
        }

        /// <summary>
        /// Drops all grabbed cells at the center position, spiraling outward.
        /// </summary>
        private void DropCellsAtPosition(int centerX, int centerY)
        {
            if (totalGrabbedCount == 0)
                return;

            int spawned = 0;
            int maxRing = Mathf.CeilToInt(Mathf.Sqrt(totalGrabbedCount)) + 10; // Enough rings to place all cells

            for (int ring = 0; ring <= maxRing && spawned < totalGrabbedCount; ring++)
            {
                foreach (var pos in GetRingPositions(centerX, centerY, ring))
                {
                    if (spawned >= totalGrabbedCount)
                        break;

                    if (CanPlaceCell(pos.x, pos.y))
                    {
                        byte materialToPlace = GetNextMaterialToPlace();
                        if (materialToPlace != Materials.Air)
                        {
                            world.SetCell(pos.x, pos.y, materialToPlace);
                            spawned++;
                        }
                    }
                }
            }

            // Clear any remaining (couldn't place all)
            ClearGrabbedCells();
        }

        /// <summary>
        /// Returns positions forming a ring at the given distance from center.
        /// Ring 0 = just the center. Ring 1 = 8 positions around center, etc.
        /// </summary>
        private IEnumerable<Vector2Int> GetRingPositions(int centerX, int centerY, int ring)
        {
            if (ring == 0)
            {
                yield return new Vector2Int(centerX, centerY);
                yield break;
            }

            // Top and bottom edges
            for (int dx = -ring; dx <= ring; dx++)
            {
                yield return new Vector2Int(centerX + dx, centerY - ring); // Top
                yield return new Vector2Int(centerX + dx, centerY + ring); // Bottom
            }

            // Left and right edges (excluding corners already covered)
            for (int dy = -ring + 1; dy < ring; dy++)
            {
                yield return new Vector2Int(centerX - ring, centerY + dy); // Left
                yield return new Vector2Int(centerX + ring, centerY + dy); // Right
            }
        }

        /// <summary>
        /// Checks if a cell can be placed at (x, y).
        /// </summary>
        private bool CanPlaceCell(int x, int y)
        {
            if (!world.IsInBounds(x, y))
                return false;

            return world.GetCell(x, y) == Materials.Air;
        }

        /// <summary>
        /// Returns and removes the next material from grabbed cells.
        /// </summary>
        private byte GetNextMaterialToPlace()
        {
            // Iterate over keys to avoid modifying collection during iteration
            foreach (var materialId in grabbedCells.Keys.ToList())
            {
                if (grabbedCells[materialId] > 0)
                {
                    grabbedCells[materialId]--;
                    totalGrabbedCount--;
                    return materialId;
                }
            }
            return Materials.Air;
        }

        /// <summary>
        /// Clears all grabbed cells (discard/cancel).
        /// </summary>
        private void ClearGrabbedCells()
        {
            grabbedCells.Clear();
            grabbedCellOffsets.Clear();
            totalGrabbedCount = 0;
        }

        /// <summary>
        /// Returns true if currently holding grabbed cells.
        /// </summary>
        public bool IsHolding => isHolding;

        /// <summary>
        /// Returns the total count of grabbed cells.
        /// </summary>
        public int TotalGrabbedCount => totalGrabbedCount;

        public float MaxGrabDistance => maxGrabDistance;
        public float GrabRadius => grabRadius;
    }
}
