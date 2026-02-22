using System.Collections.Generic;
using UnityEngine;

namespace FallingSand
{
    /// <summary>
    /// A static world object that collects falling cells.
    /// Renders as a bucket shape with walls and has an internal collection zone.
    /// The collection zone is a thin strip near the bottom, allowing cells
    /// to visually accumulate before being collected.
    ///
    /// Supports multi-objective progression: buckets can start inactive and
    /// activate when their prerequisite objective completes.
    /// Only collects cells matching the target material - other materials accumulate.
    /// </summary>
    public class Bucket : MonoBehaviour
    {
        [Header("Bucket Settings")]
        [SerializeField] private int widthInCells = 16;    // Interior width
        [SerializeField] private int depthInCells = 12;    // Interior depth
        [SerializeField] private int wallThickness = 2;    // Wall thickness in cells

        [Header("Visual")]
        [SerializeField] private Color glowColor = new Color(0.3f, 0.8f, 0.4f, 0.3f);  // Subtle green glow
        [SerializeField] private Color inactiveGlowColor = new Color(0.3f, 0.3f, 0.3f, 0.15f);  // Dim gray
        [SerializeField] private Color inactiveTextColor = new Color(0.5f, 0.5f, 0.5f);
        private SpriteRenderer glowRenderer;

        private CollectionZone collectionZone;
        private CellWorld world;
        private bool collectionEnabled = true;

        // Cluster collection
        private RectInt interiorBounds;
        private float clusterScanTimer = 0f;
        private const float ClusterScanInterval = 0.5f;

        // Reusable dictionary to avoid GC allocation per frame
        private readonly Dictionary<byte, int> collectedDict = new Dictionary<byte, int>();

        // Objective tracking
        private string objectiveId;
        private bool startsInactive = false;

        // UI display on bucket
        private TextMesh remainingText;
        private int targetRequired = 0;
        private byte targetMaterial = 0;

        /// <summary>
        /// Whether collection is currently enabled.
        /// </summary>
        public bool CollectionEnabled
        {
            get => collectionEnabled;
            set => collectionEnabled = value;
        }

        /// <summary>
        /// Initialize the bucket at the given cell position.
        /// </summary>
        /// <param name="world">The cell world</param>
        /// <param name="cellPosition">Top-left corner of the bucket in cell coordinates</param>
        /// <param name="objectiveId">The objective ID this bucket tracks</param>
        /// <param name="startsInactive">Whether bucket starts inactive (waiting for prerequisite)</param>
        public void Initialize(CellWorld world, Vector2Int cellPosition, string objectiveId = "", bool startsInactive = false)
        {
            this.world = world;
            this.objectiveId = objectiveId;
            this.startsInactive = startsInactive;

            // Calculate interior bounds
            int interiorX = cellPosition.x + wallThickness;

            // Collection zone is a thin strip near the bottom (2 cells high)
            // This allows cells to accumulate visually before being collected
            int collectionZoneHeight = 2;
            int collectionZoneY = cellPosition.y + depthInCells - collectionZoneHeight;

            RectInt collectionBounds = new RectInt(
                interiorX,
                collectionZoneY,
                widthInCells,
                collectionZoneHeight
            );

            collectionZone = new CollectionZone(world, collectionBounds);

            // Store full interior bounds for cluster detection
            interiorBounds = new RectInt(
                cellPosition.x + wallThickness,
                cellPosition.y,
                widthInCells,
                depthInCells
            );

            // Create bucket walls using Stone material
            CreateBucketWalls(cellPosition);

            // Create visual glow for collection zone
            CreateCollectionZoneGlow(collectionBounds);

            // Subscribe to progression events
            if (ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.OnObjectiveCompleted += HandleObjectiveCompleted;
                ProgressionManager.Instance.OnMaterialCollected += HandleMaterialCollected;

                if (!string.IsNullOrEmpty(objectiveId))
                {
                    ProgressionManager.Instance.OnObjectiveActivated += HandleObjectiveActivated;
                }
            }

            // Create text display above bucket
            CreateRemainingText(cellPosition);

            // Start inactive if specified
            if (startsInactive)
            {
                SetInactiveVisuals();
                collectionEnabled = false;
            }
        }

        /// <summary>
        /// Sets the objective this bucket is tracking for display purposes.
        /// </summary>
        public void SetObjective(ObjectiveData objective)
        {
            targetRequired = objective.requiredCount;
            targetMaterial = objective.targetMaterial;
            objectiveId = objective.objectiveId;
            UpdateRemainingText();
        }

        private void HandleObjectiveActivated(string activatedId)
        {
            if (activatedId == objectiveId)
            {
                ActivateBucket();
            }
        }

        private void ActivateBucket()
        {
            collectionEnabled = true;
            SetActiveVisuals();
        }

        private void SetInactiveVisuals()
        {
            if (glowRenderer != null)
            {
                glowRenderer.color = inactiveGlowColor;
            }
            if (remainingText != null)
            {
                remainingText.color = inactiveTextColor;
                remainingText.text = "---";
            }
        }

        private void SetActiveVisuals()
        {
            if (glowRenderer != null)
            {
                glowRenderer.color = glowColor;
            }
            if (remainingText != null)
            {
                remainingText.color = Color.white;
                UpdateRemainingText();
            }
        }

        private void CreateRemainingText(Vector2Int cellPosition)
        {
            // Create text object above the bucket
            GameObject textObj = new GameObject("RemainingText");
            textObj.transform.SetParent(transform);

            remainingText = textObj.AddComponent<TextMesh>();
            remainingText.fontSize = 72;
            remainingText.characterSize = 2f;
            remainingText.anchor = TextAnchor.MiddleCenter;
            remainingText.alignment = TextAlignment.Center;
            remainingText.color = Color.white;

            // Position above the bucket (convert cell to world)
            int totalWidth = widthInCells + wallThickness * 2;
            Vector2 textWorldPos = CoordinateUtils.CellToWorld(
                cellPosition.x + totalWidth / 2f,
                cellPosition.y - 3,  // 3 cells above bucket top
                world.width,
                world.height
            );
            textObj.transform.position = new Vector3(textWorldPos.x, textWorldPos.y, 0);

            UpdateRemainingText();
        }

        private void HandleMaterialCollected(byte materialId, int currentCount, int requiredCount, string completedObjectiveId)
        {
            // Only update display if this is OUR objective
            if (completedObjectiveId != objectiveId)
                return;

            if (materialId == targetMaterial)
            {
                targetRequired = requiredCount;
                UpdateRemainingText(currentCount);
            }
        }

        private void UpdateRemainingText(int currentCount = 0)
        {
            if (remainingText == null) return;

            int remaining = Mathf.Max(0, targetRequired - currentCount);
            if (remaining > 0)
            {
                remainingText.text = $"{remaining}";
            }
            else
            {
                remainingText.text = "DONE!";
                remainingText.color = Color.green;
            }
        }

        private void HandleObjectiveCompleted(ObjectiveData objective)
        {
            // Only respond if this is OUR objective
            if (objective.objectiveId != objectiveId)
                return;

            // Disable collection once objective is done
            collectionEnabled = false;

            // Fade out the glow effect
            if (glowRenderer != null)
            {
                glowRenderer.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0f);
            }

            // Play completion sound (same as shovel pickup)
            // AudioManager.Instance?.PlayPickupSound();
            // TODO: Add AudioManager when audio system is implemented
        }

        private void CreateCollectionZoneGlow(RectInt zoneBounds)
        {
            // Create child object for glow effect
            GameObject glowObj = new GameObject("CollectionZoneGlow");
            glowObj.transform.SetParent(transform);

            glowRenderer = glowObj.AddComponent<SpriteRenderer>();
            glowRenderer.sprite = CreateGlowSprite();
            glowRenderer.color = glowColor;
            glowRenderer.sortingOrder = -1;  // Behind cells

            // Position and scale to match zone bounds (convert cell coords to world)
            float worldWidth = zoneBounds.width * CoordinateUtils.CellToWorldScale;
            float worldHeight = zoneBounds.height * CoordinateUtils.CellToWorldScale;

            // Cell (0,0) is top-left, Y increases downward
            // World coords: center of zone
            Vector2 zoneCenter = CoordinateUtils.CellToWorld(
                zoneBounds.x + zoneBounds.width / 2f,
                zoneBounds.y + zoneBounds.height / 2f,
                world.width,
                world.height
            );

            glowObj.transform.position = new Vector3(zoneCenter.x, zoneCenter.y, 0);
            glowObj.transform.localScale = new Vector3(worldWidth, worldHeight, 1);
        }

        private Sprite CreateGlowSprite()
        {
            // Create a simple 1x1 white texture for the glow
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        private void CreateBucketWalls(Vector2Int pos)
        {
            int totalWidth = widthInCells + wallThickness * 2;
            int totalHeight = depthInCells + wallThickness;  // No top wall (open top)

            // Left wall
            for (int y = 0; y < totalHeight; y++)
            {
                for (int x = 0; x < wallThickness; x++)
                {
                    world.SetCell(pos.x + x, pos.y + y, Materials.Stone);
                }
            }

            // Right wall
            for (int y = 0; y < totalHeight; y++)
            {
                for (int x = 0; x < wallThickness; x++)
                {
                    world.SetCell(pos.x + wallThickness + widthInCells + x, pos.y + y, Materials.Stone);
                }
            }

            // Bottom wall (directly below the interior)
            for (int x = 0; x < totalWidth; x++)
            {
                for (int y = 0; y < wallThickness; y++)
                {
                    world.SetCell(pos.x + x, pos.y + depthInCells + y, Materials.Stone);
                }
            }

            // Mark terrain colliders dirty for the bucket area
            var terrainColliders = SimulationManager.Instance?.TerrainColliders;
            if (terrainColliders != null)
            {
                for (int x = 0; x < totalWidth; x++)
                {
                    for (int y = 0; y < totalHeight; y++)
                    {
                        terrainColliders.MarkChunkDirtyAt(pos.x + x, pos.y + y);
                    }
                }
            }
        }

        private void Update()
        {
            if (!collectionEnabled || collectionZone == null)
                return;

            // Only collect cells matching our target material
            // Other materials (e.g., Sand falling into a Dirt bucket) will accumulate
            // and the player must manually remove them
            int collected = collectionZone.CollectCellsOfType(targetMaterial);

            if (collected > 0)
            {
                collectedDict.Clear();
                collectedDict[targetMaterial] = collected;
                ProgressionManager.Instance?.RecordCollection(collectedDict, objectiveId);
            }

            // Periodically scan for clusters fully inside the bucket
            clusterScanTimer -= Time.deltaTime;
            if (clusterScanTimer <= 0f)
            {
                clusterScanTimer = ClusterScanInterval;
                TryCollectClusters();
            }
        }

        private void TryCollectClusters()
        {
            var clusterManager = SimulationManager.Instance?.ClusterManager;
            if (clusterManager == null) return;

            // Collect into a temp list to avoid modifying during iteration
            var toCollect = new List<ClusterData>();

            foreach (var cluster in clusterManager.AllClusters)
            {
                if (cluster == null) continue;
                if (cluster.isMachinePart) continue;
                if (cluster.Velocity.sqrMagnitude > 4f) continue; // velocity > 2

                // Check if ALL pixels fall within the interior bounds
                bool allInside = true;
                int matchingPixels = 0;

                foreach (var pixel in cluster.pixels)
                {
                    Vector2Int cellPos = cluster.LocalToWorldCell(pixel, world.width, world.height);
                    if (cellPos.x < interiorBounds.x || cellPos.x >= interiorBounds.x + interiorBounds.width ||
                        cellPos.y < interiorBounds.y || cellPos.y >= interiorBounds.y + interiorBounds.height)
                    {
                        allInside = false;
                        break;
                    }
                    if (pixel.materialId == targetMaterial)
                        matchingPixels++;
                }

                if (allInside && matchingPixels > 0)
                    toCollect.Add(cluster);
            }

            foreach (var cluster in toCollect)
            {
                // Count matching pixels before destroying
                int count = 0;
                foreach (var pixel in cluster.pixels)
                {
                    if (pixel.materialId == targetMaterial)
                        count++;
                }

                clusterManager.RemoveCluster(cluster);

                if (count > 0)
                {
                    collectedDict.Clear();
                    collectedDict[targetMaterial] = count;
                    ProgressionManager.Instance?.RecordCollection(collectedDict, objectiveId);
                }
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.OnObjectiveCompleted -= HandleObjectiveCompleted;
                ProgressionManager.Instance.OnMaterialCollected -= HandleMaterialCollected;
                ProgressionManager.Instance.OnObjectiveActivated -= HandleObjectiveActivated;
            }
        }
    }
}
