using UnityEngine;
using GoldRush.Core;
using GoldRush.Building;
using GoldRush.Simulation;
using System.Collections.Generic;

namespace GoldRush.Infrastructure
{
    public class Pusher : MonoBehaviour
    {
        public int GridX { get; private set; }
        public int GridY { get; private set; }
        public PushDirection Direction { get; private set; }

        // Simulation grid coordinates
        private int centerX, centerY;
        private int halfSize;  // Half size in sim cells (8 for 32px pusher)

        // Plate state
        private int currentPlateCell;  // Current plate position in sim cells from back wall
        private int maxPlateTravel;    // Maximum cells the plate can travel
        private int plateThicknessCells;  // Plate thickness in sim cells (2 for 4px)
        private int gapCells;          // Gap at full extension in sim cells (1 for 2px)
        private int wallThicknessCells; // Side wall thickness in sim cells

        // Visuals
        private Transform plateTransform;
        private Transform topBellowsTransform;
        private Transform bottomBellowsTransform;
        private SpriteRenderer topBellowsSR;
        private SpriteRenderer bottomBellowsSR;
        private float plateVisualMaxTravel;

        // Direction vectors
        private int pushDirX, pushDirY;  // Direction plate pushes (+1 or -1)

        public static GameObject Create(int gridX, int gridY, PushDirection direction, Transform parent = null)
        {
            GameObject pusherGO = new GameObject($"Pusher_{gridX}_{gridY}_{direction}");
            if (parent != null) pusherGO.transform.SetParent(parent);

            // Position using metadata grid
            var info = BuildTypeData.Get(BuildType.Pusher);
            Vector2 worldPos = info.Grid.ToWorld(gridX, gridY);
            pusherGO.transform.position = worldPos;

            // Layer
            pusherGO.layer = LayerSetup.InfrastructureLayer;

            // Component
            Pusher pusher = pusherGO.AddComponent<Pusher>();
            pusher.GridX = gridX;
            pusher.GridY = gridY;
            pusher.Direction = direction;
            pusher.SetupDirectionVectors();
            pusher.CreateVisuals();
            pusher.InitializeGridCoords();

            // Register with manager
            PusherManager.Instance.RegisterPusher(pusher);

            return pusherGO;
        }

        private void SetupDirectionVectors()
        {
            switch (Direction)
            {
                case PushDirection.Right:
                    pushDirX = 1; pushDirY = 0;
                    break;
                case PushDirection.Left:
                    pushDirX = -1; pushDirY = 0;
                    break;
                case PushDirection.Up:
                    pushDirX = 0; pushDirY = -1;  // Up is negative Y in grid coords
                    break;
                case PushDirection.Down:
                    pushDirX = 0; pushDirY = 1;
                    break;
            }
        }

        private void CreateVisuals()
        {
            // Housing frame (back wall + side edges only)
            SpriteRenderer frameSR = gameObject.AddComponent<SpriteRenderer>();
            frameSR.sprite = CreateHousingSprite();
            frameSR.sortingOrder = 8;

            // Plate (child object for movement)
            GameObject plateGO = new GameObject("Plate");
            plateGO.transform.SetParent(transform);
            plateGO.layer = LayerSetup.InfrastructureLayer;
            SpriteRenderer plateSR = plateGO.AddComponent<SpriteRenderer>();
            plateSR.sprite = CreatePlateSprite();
            plateSR.sortingOrder = 10;
            plateTransform = plateGO.transform;

            // Top bellows (stretchy accordion)
            GameObject topGO = new GameObject("TopBellows");
            topGO.transform.SetParent(transform);
            topGO.layer = LayerSetup.InfrastructureLayer;
            topBellowsSR = topGO.AddComponent<SpriteRenderer>();
            topBellowsSR.sortingOrder = 9;
            topBellowsTransform = topGO.transform;

            // Bottom bellows
            GameObject bottomGO = new GameObject("BottomBellows");
            bottomGO.transform.SetParent(transform);
            bottomGO.layer = LayerSetup.InfrastructureLayer;
            bottomBellowsSR = bottomGO.AddComponent<SpriteRenderer>();
            bottomBellowsSR.sortingOrder = 9;
            bottomBellowsTransform = bottomGO.transform;

            // Calculate visual travel distance
            float plateThicknessPx = GameSettings.PusherPlateThickness;
            float gapPx = GameSettings.PusherGapSize;
            float interiorPx = GameSettings.PusherSize - 4;  // Minus frame walls
            plateVisualMaxTravel = (interiorPx - plateThicknessPx - gapPx) / GameSettings.PixelsPerUnit;

            // Initial position
            UpdateVisuals(0f);
        }

        private Sprite CreateHousingSprite()
        {
            int size = GameSettings.PusherSize;  // 32 pixels
            Texture2D texture = new Texture2D(size, size);
            texture.filterMode = FilterMode.Point;

            Color frameColor = GameSettings.PusherFrameColor;
            Color darkColor = new Color(frameColor.r * 0.7f, frameColor.g * 0.7f, frameColor.b * 0.7f);
            Color[] pixels = new Color[size * size];

            // Clear to transparent
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            int wallThickness = 2;
            int backWallThickness = 3;

            // Draw only back wall and thin edge strips (bellows will fill the rest)
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isFrame = false;

                    switch (Direction)
                    {
                        case PushDirection.Right:
                            if (x < backWallThickness) isFrame = true;  // Back wall
                            break;
                        case PushDirection.Left:
                            if (x >= size - backWallThickness) isFrame = true;
                            break;
                        case PushDirection.Up:
                            if (y < backWallThickness) isFrame = true;
                            break;
                        case PushDirection.Down:
                            if (y >= size - backWallThickness) isFrame = true;
                            break;
                    }

                    if (isFrame)
                        pixels[y * size + x] = frameColor;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, size, size),
                                new Vector2(0.5f, 0.5f), GameSettings.PixelsPerUnit);
        }

        private Sprite CreatePlateSprite()
        {
            int thickness = GameSettings.PusherPlateThickness;  // 4 pixels
            int length = GameSettings.PusherSize - 8;  // Interior minus bellows space (24 pixels)

            int width, height;
            if (Direction == PushDirection.Left || Direction == PushDirection.Right)
            {
                width = thickness;
                height = length;
            }
            else
            {
                width = length;
                height = thickness;
            }

            Texture2D texture = new Texture2D(width, height);
            texture.filterMode = FilterMode.Point;

            Color plateColor = GameSettings.PusherPlateColor;
            Color highlightColor = new Color(
                Mathf.Min(1f, plateColor.r + 0.15f),
                Mathf.Min(1f, plateColor.g + 0.15f),
                Mathf.Min(1f, plateColor.b + 0.15f)
            );
            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Highlight the pushing edge
                    bool isHighlight = false;
                    if (Direction == PushDirection.Right) isHighlight = x >= width - 1;
                    else if (Direction == PushDirection.Left) isHighlight = x == 0;
                    else if (Direction == PushDirection.Up) isHighlight = y >= height - 1;
                    else if (Direction == PushDirection.Down) isHighlight = y == 0;

                    pixels[y * width + x] = isHighlight ? highlightColor : plateColor;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, width, height),
                                new Vector2(0.5f, 0.5f), GameSettings.PixelsPerUnit);
        }

        private Sprite CreateBellowsSprite(int width, int height)
        {
            Texture2D texture = new Texture2D(width, height);
            texture.filterMode = FilterMode.Point;

            Color bellowsColor = GameSettings.PusherPlateColor;
            Color stripeColor = new Color(bellowsColor.r * 0.85f, bellowsColor.g * 0.85f, bellowsColor.b * 0.85f);
            Color[] pixels = new Color[width * height];

            // Accordion stripe pattern
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Stripe every 3 pixels for accordion look
                    bool isStripe;
                    if (Direction == PushDirection.Left || Direction == PushDirection.Right)
                        isStripe = (x % 3) == 0;
                    else
                        isStripe = (y % 3) == 0;

                    pixels[y * width + x] = isStripe ? stripeColor : bellowsColor;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, width, height),
                                new Vector2(0.5f, 0.5f), GameSettings.PixelsPerUnit);
        }

        private void InitializeGridCoords()
        {
            if (SimulationWorld.Instance == null) return;

            Vector2 worldPos = transform.position;
            Vector2Int gridPos = SimulationWorld.Instance.WorldToGrid(worldPos);

            centerX = gridPos.x;
            centerY = gridPos.y;

            // Pusher is 32 pixels = 16 sim cells
            halfSize = 8;
            wallThicknessCells = 2;  // 4 pixels = 2 sim cells for side walls/bellows

            // Plate dimensions in sim cells
            plateThicknessCells = GameSettings.PusherPlateThickness / 2;  // 4px / 2 = 2 cells
            gapCells = GameSettings.PusherGapSize / 2;  // 2px / 2 = 1 cell

            // Max travel: interior minus plate minus gap
            int interiorCells = (GameSettings.PusherSize - 4) / 2;  // 14 cells
            maxPlateTravel = interiorCells - plateThicknessCells - gapCells;

            // Start fully retracted
            currentPlateCell = 0;

            // Set up initial blocking (back wall + bellows at position 0)
            SetBackWallBlocking(true);
            UpdateAllBlocking(SimulationWorld.Instance.Grid, 0);
        }

        private void SetBackWallBlocking(bool blocking)
        {
            if (SimulationWorld.Instance == null) return;
            var grid = SimulationWorld.Instance.Grid;

            int backWallDepth = 2;

            for (int perpOffset = -halfSize + 1; perpOffset < halfSize - 1; perpOffset++)
            {
                for (int depthOffset = 0; depthOffset < backWallDepth; depthOffset++)
                {
                    int x, y;
                    if (pushDirX != 0)
                    {
                        x = centerX - pushDirX * (halfSize - depthOffset);
                        y = centerY + perpOffset;
                    }
                    else
                    {
                        x = centerX + perpOffset;
                        y = centerY - pushDirY * (halfSize - depthOffset);
                    }

                    if (grid.InBounds(x, y))
                        grid.SetInfrastructureBlocking(x, y, blocking);
                }
            }
        }

        private void UpdateAllBlocking(SimulationGrid grid, int plateCell)
        {
            // Clear all interior blocking first
            for (int perpOffset = -halfSize + 1; perpOffset < halfSize - 1; perpOffset++)
            {
                for (int depthOffset = 0; depthOffset <= maxPlateTravel + plateThicknessCells; depthOffset++)
                {
                    int x, y;
                    if (pushDirX != 0)
                    {
                        x = centerX - pushDirX * halfSize + pushDirX * (depthOffset + 2);
                        y = centerY + perpOffset;
                    }
                    else
                    {
                        x = centerX + perpOffset;
                        y = centerY - pushDirY * halfSize + pushDirY * (depthOffset + 2);
                    }

                    if (grid.InBounds(x, y))
                        grid.SetInfrastructureBlocking(x, y, false);
                }
            }

            // Block plate cells
            for (int perpOffset = -halfSize + wallThicknessCells; perpOffset < halfSize - wallThicknessCells; perpOffset++)
            {
                for (int thicknessOffset = 0; thicknessOffset < plateThicknessCells; thicknessOffset++)
                {
                    int x, y;
                    if (pushDirX != 0)
                    {
                        x = centerX - pushDirX * halfSize + pushDirX * (plateCell + thicknessOffset + 2);
                        y = centerY + perpOffset;
                    }
                    else
                    {
                        x = centerX + perpOffset;
                        y = centerY - pushDirY * halfSize + pushDirY * (plateCell + thicknessOffset + 2);
                    }

                    if (grid.InBounds(x, y))
                        grid.SetInfrastructureBlocking(x, y, true);
                }
            }

            // Block bellows (top and bottom strips from back wall to plate)
            // These are the sealed edges that prevent materials falling through
            for (int wallOffset = 0; wallOffset < wallThicknessCells; wallOffset++)
            {
                // Block from back wall to current plate position
                for (int depthOffset = 0; depthOffset < plateCell + plateThicknessCells; depthOffset++)
                {
                    // Top/first bellows
                    int x1, y1;
                    if (pushDirX != 0)
                    {
                        x1 = centerX - pushDirX * halfSize + pushDirX * (depthOffset + 2);
                        y1 = centerY - halfSize + 1 + wallOffset;  // Top edge
                    }
                    else
                    {
                        x1 = centerX - halfSize + 1 + wallOffset;  // Left edge
                        y1 = centerY - pushDirY * halfSize + pushDirY * (depthOffset + 2);
                    }

                    if (grid.InBounds(x1, y1))
                        grid.SetInfrastructureBlocking(x1, y1, true);

                    // Bottom/second bellows
                    int x2, y2;
                    if (pushDirX != 0)
                    {
                        x2 = centerX - pushDirX * halfSize + pushDirX * (depthOffset + 2);
                        y2 = centerY + halfSize - 2 - wallOffset;  // Bottom edge
                    }
                    else
                    {
                        x2 = centerX + halfSize - 2 - wallOffset;  // Right edge
                        y2 = centerY - pushDirY * halfSize + pushDirY * (depthOffset + 2);
                    }

                    if (grid.InBounds(x2, y2))
                        grid.SetInfrastructureBlocking(x2, y2, true);
                }
            }
        }

        public void ProcessPhysics(SimulationGrid grid, float targetExtension)
        {
            int targetCell = Mathf.RoundToInt(targetExtension * maxPlateTravel);

            bool moved = false;

            // Try to extend toward target
            while (currentPlateCell < targetCell)
            {
                if (!TryAdvancePlate(grid))
                    break;
                moved = true;
            }

            // Retraction is always allowed
            while (currentPlateCell > targetCell)
            {
                currentPlateCell--;
                moved = true;
            }

            if (moved)
            {
                UpdateAllBlocking(grid, currentPlateCell);
            }

            // Update visuals
            float visualExtension = (float)currentPlateCell / maxPlateTravel;
            UpdateVisuals(visualExtension);

            // Wake particles in the pusher area
            WakeParticlesInArea(grid);
        }

        private bool TryAdvancePlate(SimulationGrid grid)
        {
            int newPlateCell = currentPlateCell + 1;

            // Check cells in front of the plate (inside the bellows area)
            bool canAdvance = true;
            List<(int x, int y, MaterialType type, uint clusterId)> materialsInFront = new();

            for (int perpOffset = -halfSize + wallThicknessCells; perpOffset < halfSize - wallThicknessCells; perpOffset++)
            {
                int frontX, frontY;
                if (pushDirX != 0)
                {
                    frontX = centerX - pushDirX * halfSize + pushDirX * (newPlateCell + plateThicknessCells + 1);
                    frontY = centerY + perpOffset;
                }
                else
                {
                    frontX = centerX + perpOffset;
                    frontY = centerY - pushDirY * halfSize + pushDirY * (newPlateCell + plateThicknessCells + 1);
                }

                if (!grid.InBounds(frontX, frontY)) continue;

                MaterialType type = grid.Get(frontX, frontY);
                uint clusterId = grid.GetClusterID(frontX, frontY);

                if (type != MaterialType.Air || clusterId != 0)
                {
                    materialsInFront.Add((frontX, frontY, type, clusterId));
                }
            }

            HashSet<uint> processedClusters = new HashSet<uint>();

            foreach (var (x, y, type, clusterId) in materialsInFront)
            {
                if (clusterId != 0)
                {
                    if (processedClusters.Contains(clusterId)) continue;
                    processedClusters.Add(clusterId);

                    if (!grid.ClusterManager.TryMoveCluster(clusterId, pushDirX, pushDirY))
                    {
                        var clusterData = grid.ClusterManager.GetCluster(clusterId);
                        if (clusterData.HasValue)
                        {
                            if (!TryCrushCluster(clusterId, clusterData.Value, grid.ClusterManager))
                            {
                                canAdvance = false;
                            }
                        }
                    }
                }
                else if (MaterialProperties.IsSimulated(type))
                {
                    int destX = x + pushDirX;
                    int destY = y + pushDirY;

                    if (grid.InBounds(destX, destY) &&
                        grid.Get(destX, destY) == MaterialType.Air &&
                        !grid.IsBlockedByInfrastructure(destX, destY, type))
                    {
                        grid.Set(x, y, MaterialType.Air);
                        grid.Set(destX, destY, type);
                        grid.WakeCell(destX, destY);
                    }
                    else
                    {
                        canAdvance = false;
                    }
                }
            }

            if (canAdvance)
            {
                currentPlateCell = newPlateCell;
                return true;
            }

            return false;
        }

        private bool TryCrushCluster(uint clusterId, ClusterData cluster, ClusterManager clusterMgr)
        {
            if (cluster.Type == MaterialType.Boulder && cluster.Size == 8)
            {
                clusterMgr.BreakCluster(clusterId, 4, MaterialType.Rock);
                return true;
            }
            else if (cluster.Type == MaterialType.Rock && cluster.Size == 4)
            {
                clusterMgr.BreakCluster(clusterId, 2, MaterialType.Gravel);
                return true;
            }
            else if (cluster.Type == MaterialType.Gravel && cluster.Size == 2)
            {
                clusterMgr.BreakCluster(clusterId, 1, MaterialType.Sand);
                return true;
            }

            return false;
        }

        private void UpdateVisuals(float extension)
        {
            float ppu = GameSettings.PixelsPerUnit;
            float backWallOffset = 3f / ppu;
            float plateHalfThickness = (GameSettings.PusherPlateThickness / 2f) / ppu;
            float offset = extension * plateVisualMaxTravel;

            // Position plate
            if (plateTransform != null)
            {
                Vector3 platePos = Vector3.zero;
                switch (Direction)
                {
                    case PushDirection.Right:
                        platePos.x = -0.5f + backWallOffset + plateHalfThickness + offset;
                        break;
                    case PushDirection.Left:
                        platePos.x = 0.5f - backWallOffset - plateHalfThickness - offset;
                        break;
                    case PushDirection.Up:
                        platePos.y = -0.5f + backWallOffset + plateHalfThickness + offset;
                        break;
                    case PushDirection.Down:
                        platePos.y = 0.5f - backWallOffset - plateHalfThickness - offset;
                        break;
                }
                plateTransform.localPosition = platePos;
            }

            // Update bellows - they stretch from back wall to plate
            float bellowsThickness = 4f / ppu;  // 4 pixels thick
            float minBellowsLength = 4f / ppu;  // Minimum visual length
            float bellowsLength = Mathf.Max(minBellowsLength, backWallOffset + offset);

            // Create/update bellows sprites based on current length
            int bellowsWidthPx, bellowsHeightPx;
            if (Direction == PushDirection.Left || Direction == PushDirection.Right)
            {
                bellowsWidthPx = Mathf.Max(4, Mathf.RoundToInt(bellowsLength * ppu));
                bellowsHeightPx = 4;
            }
            else
            {
                bellowsWidthPx = 4;
                bellowsHeightPx = Mathf.Max(4, Mathf.RoundToInt(bellowsLength * ppu));
            }

            // Update bellows sprites
            if (topBellowsSR != null)
            {
                topBellowsSR.sprite = CreateBellowsSprite(bellowsWidthPx, bellowsHeightPx);
            }
            if (bottomBellowsSR != null)
            {
                bottomBellowsSR.sprite = CreateBellowsSprite(bellowsWidthPx, bellowsHeightPx);
            }

            // Position bellows
            float bellowsOffset = bellowsLength / 2f;
            float edgeOffset = (GameSettings.PusherSize / 2f - 2f) / ppu;  // 2px from edge

            if (topBellowsTransform != null && bottomBellowsTransform != null)
            {
                switch (Direction)
                {
                    case PushDirection.Right:
                        topBellowsTransform.localPosition = new Vector3(-0.5f + bellowsOffset, edgeOffset, 0);
                        bottomBellowsTransform.localPosition = new Vector3(-0.5f + bellowsOffset, -edgeOffset, 0);
                        break;
                    case PushDirection.Left:
                        topBellowsTransform.localPosition = new Vector3(0.5f - bellowsOffset, edgeOffset, 0);
                        bottomBellowsTransform.localPosition = new Vector3(0.5f - bellowsOffset, -edgeOffset, 0);
                        break;
                    case PushDirection.Up:
                        topBellowsTransform.localPosition = new Vector3(-edgeOffset, -0.5f + bellowsOffset, 0);
                        bottomBellowsTransform.localPosition = new Vector3(edgeOffset, -0.5f + bellowsOffset, 0);
                        break;
                    case PushDirection.Down:
                        topBellowsTransform.localPosition = new Vector3(-edgeOffset, 0.5f - bellowsOffset, 0);
                        bottomBellowsTransform.localPosition = new Vector3(edgeOffset, 0.5f - bellowsOffset, 0);
                        break;
                }
            }
        }

        private void WakeParticlesInArea(SimulationGrid grid)
        {
            for (int dy = -halfSize; dy <= halfSize; dy++)
            {
                for (int dx = -halfSize; dx <= halfSize; dx++)
                {
                    int x = centerX + dx;
                    int y = centerY + dy;

                    if (!grid.InBounds(x, y)) continue;

                    if (MaterialProperties.IsSimulated(grid.Get(x, y)))
                    {
                        grid.WakeCell(x, y);
                    }

                    uint clusterId = grid.GetClusterID(x, y);
                    if (clusterId != 0)
                    {
                        grid.ClusterManager.WakeCluster(clusterId);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            PusherManager.Instance.UnregisterPusher(this);

            if (SimulationWorld.Instance != null)
            {
                var grid = SimulationWorld.Instance.Grid;

                // Clear back wall
                SetBackWallBlocking(false);

                // Clear all interior blocking
                for (int perpOffset = -halfSize + 1; perpOffset < halfSize - 1; perpOffset++)
                {
                    for (int depthOffset = 0; depthOffset <= maxPlateTravel + plateThicknessCells + 2; depthOffset++)
                    {
                        int x, y;
                        if (pushDirX != 0)
                        {
                            x = centerX - pushDirX * halfSize + pushDirX * (depthOffset + 1);
                            y = centerY + perpOffset;
                        }
                        else
                        {
                            x = centerX + perpOffset;
                            y = centerY - pushDirY * halfSize + pushDirY * (depthOffset + 1);
                        }

                        if (grid.InBounds(x, y))
                            grid.SetInfrastructureBlocking(x, y, false);
                    }
                }
            }
        }
    }
}
