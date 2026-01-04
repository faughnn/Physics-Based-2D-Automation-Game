using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using GoldRush.Core;
using GoldRush.Simulation;
using GoldRush.Building;

namespace GoldRush.Player
{
    public class PickupTool : MonoBehaviour
    {
        public static PickupTool Instance { get; private set; }

        private const int PickupRadiusCells = 6;  // Radius in simulation grid cells

        private bool isEnabled;
        private bool isHolding;
        private Camera mainCamera;

        // Visual indicator
        private GameObject indicatorGO;
        private SpriteRenderer indicatorSR;

        // Held materials data
        private struct HeldMaterial
        {
            public MaterialType Type;
            public Vector2Int RelativePos;  // Relative to pickup center
        }

        private struct HeldCluster
        {
            public MaterialType Type;
            public int Size;
            public Vector2Int RelativeOrigin;  // Relative to pickup center
        }

        private List<HeldMaterial> heldMaterials = new List<HeldMaterial>();
        private List<HeldCluster> heldClusters = new List<HeldCluster>();

        // Current center position for drop calculations
        private Vector2Int currentGridCenter;

        // Visual preview of held materials
        private GameObject previewGO;
        private SpriteRenderer previewSR;
        private Texture2D previewTexture;
        private Vector2Int previewOffset;  // Offset from center to preview anchor

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void Initialize()
        {
            mainCamera = Camera.main;
            CreateIndicator();
        }

        private void CreateIndicator()
        {
            indicatorGO = new GameObject("PickupIndicator");
            indicatorGO.transform.SetParent(transform);

            indicatorSR = indicatorGO.AddComponent<SpriteRenderer>();
            indicatorSR.sprite = CreateCircleSprite();
            indicatorSR.sortingOrder = 100;
            indicatorSR.color = new Color(0.3f, 0.8f, 0.3f, 0.5f);  // Green, semi-transparent

            indicatorGO.SetActive(false);
        }

        private Sprite CreateCircleSprite()
        {
            int size = PickupRadiusCells * 4;  // Visual size in pixels
            Texture2D texture = new Texture2D(size, size);
            texture.filterMode = FilterMode.Point;

            Color32 clear = new Color32(0, 0, 0, 0);
            Color32 outline = new Color32(255, 255, 255, 200);
            Color32[] pixels = new Color32[size * size];

            // Fill with transparent
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = clear;

            // Draw circle outline
            float center = size / 2f;
            float radius = size / 2f - 1;
            float thickness = 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                    if (dist >= radius - thickness && dist <= radius)
                    {
                        pixels[y * size + x] = outline;
                    }
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), GameSettings.PixelsPerUnit);
        }

        private void Update()
        {
            // Don't process if in build mode
            if (BuildSystem.Instance != null && BuildSystem.Instance.CurrentBuildType != BuildType.None)
            {
                if (isEnabled)
                {
                    isEnabled = false;
                    indicatorGO.SetActive(false);
                    DropAll();
                }
                return;
            }

            // Toggle with G key
            if (Input.GetKeyDown(KeyCode.G))
            {
                isEnabled = !isEnabled;
                indicatorGO.SetActive(isEnabled);

                if (!isEnabled && isHolding)
                {
                    DropAll();
                }
            }

            if (!isEnabled) return;

            // Update indicator position
            Vector2 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            indicatorGO.transform.position = mouseWorld;

            // Handle mouse input
            if (Input.GetMouseButtonDown(0))
            {
                TryPickup(mouseWorld);
            }
            else if (Input.GetMouseButton(0) && isHolding)
            {
                MoveHeldItems(mouseWorld);
            }
            else if (Input.GetMouseButtonUp(0) && isHolding)
            {
                DropAll();
            }

            // Update indicator color based on state
            if (isHolding)
            {
                indicatorSR.color = new Color(0.8f, 0.6f, 0.2f, 0.6f);  // Orange when holding
            }
            else
            {
                indicatorSR.color = new Color(0.3f, 0.8f, 0.3f, 0.5f);  // Green when ready
            }
        }

        private void TryPickup(Vector2 worldPos)
        {
            if (SimulationWorld.Instance == null) return;

            var grid = SimulationWorld.Instance.Grid;
            var clusterMgr = grid.ClusterManager;

            Vector2Int centerGrid = SimulationWorld.Instance.WorldToGrid(worldPos);
            currentGridCenter = centerGrid;

            HashSet<uint> foundClusterIDs = new HashSet<uint>();
            heldMaterials.Clear();
            heldClusters.Clear();

            // Scan circular area
            for (int dy = -PickupRadiusCells; dy <= PickupRadiusCells; dy++)
            {
                for (int dx = -PickupRadiusCells; dx <= PickupRadiusCells; dx++)
                {
                    // Check if within circle
                    if (dx * dx + dy * dy > PickupRadiusCells * PickupRadiusCells)
                        continue;

                    int x = centerGrid.x + dx;
                    int y = centerGrid.y + dy;

                    if (!grid.InBounds(x, y)) continue;

                    // Check for cluster
                    uint clusterId = grid.GetClusterID(x, y);
                    if (clusterId != 0)
                    {
                        if (!foundClusterIDs.Contains(clusterId))
                        {
                            foundClusterIDs.Add(clusterId);

                            var clusterData = clusterMgr.GetCluster(clusterId);
                            if (clusterData.HasValue)
                            {
                                // Store cluster info with relative position
                                heldClusters.Add(new HeldCluster
                                {
                                    Type = clusterData.Value.Type,
                                    Size = clusterData.Value.Size,
                                    RelativeOrigin = new Vector2Int(
                                        clusterData.Value.OriginX - centerGrid.x,
                                        clusterData.Value.OriginY - centerGrid.y
                                    )
                                });

                                // IMMEDIATELY remove cluster from grid - it's now "held"
                                clusterMgr.RemoveCluster(clusterId);
                            }
                        }
                        continue;
                    }

                    // Check for single-cell material
                    MaterialType type = grid.Get(x, y);
                    if (type != MaterialType.Air && type != MaterialType.Terrain)
                    {
                        heldMaterials.Add(new HeldMaterial
                        {
                            Type = type,
                            RelativePos = new Vector2Int(dx, dy)
                        });

                        // Clear from grid immediately
                        grid.Set(x, y, MaterialType.Air);
                    }
                }
            }

            if (heldClusters.Count > 0 || heldMaterials.Count > 0)
            {
                isHolding = true;
                CreatePreviewTexture();

                // Position preview at initial pickup location
                if (previewGO != null)
                {
                    previewGO.transform.position = worldPos;
                }
            }
        }

        private void CreatePreviewTexture()
        {
            // Calculate bounds of held items
            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;

            foreach (var mat in heldMaterials)
            {
                minX = Mathf.Min(minX, mat.RelativePos.x);
                maxX = Mathf.Max(maxX, mat.RelativePos.x);
                minY = Mathf.Min(minY, mat.RelativePos.y);
                maxY = Mathf.Max(maxY, mat.RelativePos.y);
            }
            foreach (var cluster in heldClusters)
            {
                minX = Mathf.Min(minX, cluster.RelativeOrigin.x);
                maxX = Mathf.Max(maxX, cluster.RelativeOrigin.x + cluster.Size - 1);
                minY = Mathf.Min(minY, cluster.RelativeOrigin.y);
                maxY = Mathf.Max(maxY, cluster.RelativeOrigin.y + cluster.Size - 1);
            }

            if (minX > maxX) return; // Nothing to preview

            // Store offset for positioning (center of bounds relative to pickup center)
            previewOffset = new Vector2Int((minX + maxX) / 2, (minY + maxY) / 2);

            int width = (maxX - minX + 1) * 2;  // 2 pixels per cell
            int height = (maxY - minY + 1) * 2;

            // Create or resize texture
            if (previewTexture == null || previewTexture.width != width || previewTexture.height != height)
            {
                if (previewTexture != null) Destroy(previewTexture);
                previewTexture = new Texture2D(width, height);
                previewTexture.filterMode = FilterMode.Point;
            }

            // Clear to transparent
            Color32[] pixels = new Color32[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(0, 0, 0, 0);

            // Draw held materials with 50% alpha
            foreach (var mat in heldMaterials)
            {
                // Convert cluster material types to Sand for preview (they'll become Sand on drop)
                MaterialType displayType = mat.Type;
                if (displayType == MaterialType.Boulder ||
                    displayType == MaterialType.Rock ||
                    displayType == MaterialType.Gravel)
                {
                    displayType = MaterialType.Sand;
                }

                Color32 color = MaterialProperties.GetColor(displayType);
                color.a = 128;  // 50% opacity
                int px = (mat.RelativePos.x - minX) * 2;
                int py = (maxY - mat.RelativePos.y) * 2;  // Flip Y for texture coords
                for (int dy = 0; dy < 2; dy++)
                    for (int dx = 0; dx < 2; dx++)
                        if (py + dy >= 0 && py + dy < height && px + dx >= 0 && px + dx < width)
                            pixels[(py + dy) * width + (px + dx)] = color;
            }

            // Draw held clusters
            foreach (var cluster in heldClusters)
            {
                Color32 color = MaterialProperties.GetColor(cluster.Type);
                color.a = 128;
                for (int cy = 0; cy < cluster.Size; cy++)
                {
                    for (int cx = 0; cx < cluster.Size; cx++)
                    {
                        int px = (cluster.RelativeOrigin.x + cx - minX) * 2;
                        int py = (maxY - (cluster.RelativeOrigin.y + cy)) * 2;
                        for (int dy = 0; dy < 2; dy++)
                            for (int dx = 0; dx < 2; dx++)
                                if (py + dy >= 0 && py + dy < height && px + dx >= 0 && px + dx < width)
                                    pixels[(py + dy) * width + (px + dx)] = color;
                    }
                }
            }

            previewTexture.SetPixels32(pixels);
            previewTexture.Apply();

            // Create preview game object if needed
            if (previewGO == null)
            {
                previewGO = new GameObject("PickupPreview");
                previewSR = previewGO.AddComponent<SpriteRenderer>();
                previewSR.sortingOrder = 99;
            }

            previewSR.sprite = Sprite.Create(previewTexture,
                new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f),
                GameSettings.PixelsPerUnit);
            previewGO.SetActive(true);
        }

        private void MoveHeldItems(Vector2 worldPos)
        {
            if (SimulationWorld.Instance == null) return;

            // Simply track the new center position
            // Materials are NOT in the grid while held - they're "virtual"
            // This prevents the simulation from moving them and causing duplication
            currentGridCenter = SimulationWorld.Instance.WorldToGrid(worldPos);

            // Update preview position
            if (previewGO != null && previewGO.activeSelf)
            {
                previewGO.transform.position = worldPos;
            }
        }

        private void DropAll()
        {
            if (SimulationWorld.Instance == null) return;

            var grid = SimulationWorld.Instance.Grid;
            var clusterMgr = grid.ClusterManager;

            // Sort clusters by Y position (lowest Y first = highest in world)
            // This ensures we place top clusters first, avoiding conflicts
            var sortedClusters = heldClusters.OrderBy(c =>
                currentGridCenter.y + c.RelativeOrigin.y).ToList();

            // Create clusters at final positions
            foreach (var cluster in sortedClusters)
            {
                int x = currentGridCenter.x + cluster.RelativeOrigin.x;
                int y = currentGridCenter.y + cluster.RelativeOrigin.y;

                // Try to create at exact position first
                uint newId = clusterMgr.CreateCluster(x, y, cluster.Size, cluster.Type);

                // If blocked, search UPWARD (decreasing Y in grid coords)
                if (newId == 0)
                {
                    int searchY = y - 1;  // Start one cell up
                    int maxSearchUp = 50;

                    while (newId == 0 && searchY >= y - maxSearchUp && searchY >= 0)
                    {
                        newId = clusterMgr.CreateCluster(x, searchY, cluster.Size, cluster.Type);
                        searchY--;  // Move up (decreasing Y)
                    }
                }

                if (newId != 0)
                {
                    clusterMgr.WakeCluster(newId);
                }
            }

            // Sort materials by Y position too
            var sortedMaterials = heldMaterials.OrderBy(m =>
                currentGridCenter.y + m.RelativePos.y).ToList();

            // Place single-cell materials
            foreach (var mat in sortedMaterials)
            {
                int x = currentGridCenter.x + mat.RelativePos.x;
                int y = currentGridCenter.y + mat.RelativePos.y;

                if (!grid.InBounds(x, y)) continue;

                // Convert cluster material types to Sand - they shouldn't exist as single cells
                // (they might have been picked up from orphan cells created by dig fallback)
                MaterialType typeToPlace = mat.Type;
                if (typeToPlace == MaterialType.Boulder ||
                    typeToPlace == MaterialType.Rock ||
                    typeToPlace == MaterialType.Gravel)
                {
                    typeToPlace = MaterialType.Sand;
                }

                // Try exact position first
                if (grid.Get(x, y) == MaterialType.Air)
                {
                    grid.Set(x, y, typeToPlace);
                    grid.WakeCell(x, y);
                }
                else
                {
                    // Search upward for free space
                    int searchY = y - 1;
                    while (searchY >= 0 && grid.InBounds(x, searchY) && grid.Get(x, searchY) != MaterialType.Air)
                    {
                        searchY--;
                    }

                    if (searchY >= 0 && grid.InBounds(x, searchY) && grid.Get(x, searchY) == MaterialType.Air)
                    {
                        grid.Set(x, searchY, typeToPlace);
                        grid.WakeCell(x, searchY);
                    }
                }
            }

            // Clear state
            heldClusters.Clear();
            heldMaterials.Clear();
            isHolding = false;

            // Hide preview
            if (previewGO != null)
            {
                previewGO.SetActive(false);
            }
        }

        public bool IsEnabled => isEnabled;
        public bool IsHolding => isHolding;
    }
}
