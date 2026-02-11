using System;
using System.Collections.Generic;
using UnityEngine;

namespace FallingSand
{
    /// <summary>
    /// Manages all piston instances.
    /// 16x16 block pistons with kinematic plate, global phase sync, fill area, and stalling.
    /// </summary>
    public class PistonManager : IDisposable
    {
        // Piston geometry
        public const int BlockSize = 16;
        public const int BaseThickness = 2;
        public const int PlateThickness = 2;
        public const int MaxTravel = BlockSize - BaseThickness - PlateThickness; // 12

        // Timing
        public const float CycleDuration = 3.0f;
        public const float DwellFraction = 0.15f;

        // Visuals
        public const float ShaftThicknessWorld = 4f;
        private static readonly Color ShaftColor = new Color(0.5f, 0.5f, 0.55f);

        private CellWorld world;
        private ClusterManager clusterManager;
        private TerrainColliderManager terrainColliders;
        private readonly List<PistonData> pistons = new List<PistonData>();
        private readonly int[] pushChainBuffer = new int[BlockSize];
        private Sprite shaftSprite;

        public int PistonCount => pistons.Count;

        public void Initialize(CellWorld world, ClusterManager clusterManager, TerrainColliderManager terrainColliders)
        {
            this.world = world;
            this.clusterManager = clusterManager;
            this.terrainColliders = terrainColliders;

            // Create cached 1x1 white pixel sprite for shaft rendering
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            shaftSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        /// <summary>
        /// Snaps a coordinate to the 16-cell grid.
        /// </summary>
        public static int SnapToGrid(int coord)
        {
            if (coord < 0)
                return ((coord - BlockSize + 1) / BlockSize) * BlockSize;
            return (coord / BlockSize) * BlockSize;
        }

        /// <summary>
        /// Global piston phase: 0 = retracted, 1 = extended.
        /// Includes dwell periods at both extremes.
        /// </summary>
        private float CalculateDesiredStrokeT()
        {
            float cycleT = (Time.time % CycleDuration) / CycleDuration;

            if (cycleT < DwellFraction)
                return 0f;
            else if (cycleT < 0.5f)
                return Mathf.Clamp01((cycleT - DwellFraction) / (0.5f - DwellFraction));
            else if (cycleT < 0.5f + DwellFraction)
                return 1f;
            else
                return Mathf.Clamp01(1f - (cycleT - 0.5f - DwellFraction) / (1f - 0.5f - DwellFraction));
        }

        // =====================================================================
        // Placement / Removal
        // =====================================================================

        public bool PlacePiston(int cellX, int cellY, PistonDirection direction)
        {
            int gridX = SnapToGrid(cellX);
            int gridY = SnapToGrid(cellY);

            // Bounds check for 16x16 block
            if (!world.IsInBounds(gridX, gridY) ||
                !world.IsInBounds(gridX + BlockSize - 1, gridY + BlockSize - 1))
                return false;

            // Validate area is clear (air or soft terrain, no overlapping structures)
            for (int dy = 0; dy < BlockSize; dy++)
            {
                for (int dx = 0; dx < BlockSize; dx++)
                {
                    int cx = gridX + dx;
                    int cy = gridY + dy;
                    if (HasPistonAt(cx, cy)) return false;

                    byte mat = world.GetCell(cx, cy);
                    if (mat != Materials.Air && !Materials.IsSoftTerrain(mat))
                        return false;
                }
            }

            // Clear entire 16x16 to Air
            for (int dy = 0; dy < BlockSize; dy++)
            {
                for (int dx = 0; dx < BlockSize; dx++)
                {
                    int cx = gridX + dx;
                    int cy = gridY + dy;
                    world.SetCell(cx, cy, Materials.Air);
                    world.MarkDirty(cx, cy);
                    terrainColliders.MarkChunkDirtyAt(cx, cy);
                }
            }

            // Write PistonBase cells for base bar only
            WriteBaseBar(gridX, gridY, direction);

            // Mark chunks as having structure
            MarkChunksHasStructure(gridX, gridY);

            // Create anchor GO at block center
            Vector2 blockCenter = CoordinateUtils.CellToWorld(
                gridX + BlockSize / 2f, gridY + BlockSize / 2f,
                world.width, world.height);

            GameObject anchorObj = new GameObject($"PistonAnchor_{gridX}_{gridY}");
            anchorObj.transform.position = new Vector3(blockCenter.x, blockCenter.y, 0);
            Rigidbody2D anchorRb = anchorObj.AddComponent<Rigidbody2D>();
            anchorRb.bodyType = RigidbodyType2D.Kinematic;

            // Base collider (permanent)
            BoxCollider2D baseCollider = anchorObj.AddComponent<BoxCollider2D>();
            ConfigureBaseCollider(baseCollider, direction);

            // Fill collider (dynamic, initially disabled)
            BoxCollider2D fillColl = anchorObj.AddComponent<BoxCollider2D>();
            fillColl.enabled = false;

            // Create plate cluster
            List<ClusterPixel> platePixels = CreatePlatePixels(direction);
            Vector2 retractedPos = CalculatePlateWorldPos(gridX, gridY, direction, retracted: true);
            Vector2 extendedPos = CalculatePlateWorldPos(gridX, gridY, direction, retracted: false);

            ClusterData armCluster = ClusterFactory.CreateCluster(platePixels, retractedPos, clusterManager);
            if (armCluster == null)
            {
                UnityEngine.Object.Destroy(anchorObj);
                ClearBaseBar(gridX, gridY, direction);
                return false;
            }

            // Make plate kinematic
            armCluster.rb.bodyType = RigidbodyType2D.Kinematic;
            armCluster.rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            armCluster.isMachinePart = true;

            // Create shaft visual
            GameObject rodObj = new GameObject($"PistonShaft_{gridX}_{gridY}");
            SpriteRenderer rodRenderer = rodObj.AddComponent<SpriteRenderer>();
            rodRenderer.sprite = shaftSprite;
            rodRenderer.color = ShaftColor;
            rodRenderer.sortingOrder = 5;
            rodRenderer.enabled = false;

            // Store piston data
            PistonData piston = new PistonData
            {
                baseCellX = gridX,
                baseCellY = gridY,
                direction = direction,
                anchorObject = anchorObj,
                armCluster = armCluster,
                retractedWorldPos = retractedPos,
                extendedWorldPos = extendedPos,
                currentStrokeT = 0f,
                lastFillExtent = 0,
                fillCollider = fillColl,
                rodObject = rodObj,
                rodRenderer = rodRenderer,
            };
            pistons.Add(piston);
            return true;
        }

        public bool RemovePiston(int cellX, int cellY)
        {
            int gridX = SnapToGrid(cellX);
            int gridY = SnapToGrid(cellY);

            for (int i = pistons.Count - 1; i >= 0; i--)
            {
                var piston = pistons[i];
                if (piston.baseCellX != gridX || piston.baseCellY != gridY)
                    continue;

                // Destroy cluster
                if (piston.armCluster != null)
                {
                    clusterManager.Unregister(piston.armCluster);
                    UnityEngine.Object.Destroy(piston.armCluster.gameObject);
                }

                // Destroy anchor and shaft
                if (piston.anchorObject != null)
                    UnityEngine.Object.Destroy(piston.anchorObject);
                if (piston.rodObject != null)
                    UnityEngine.Object.Destroy(piston.rodObject);

                // Clear all piston cells in 16x16 area
                for (int dy = 0; dy < BlockSize; dy++)
                {
                    for (int dx = 0; dx < BlockSize; dx++)
                    {
                        int cx = piston.baseCellX + dx;
                        int cy = piston.baseCellY + dy;
                        byte mat = world.GetCell(cx, cy);
                        if (Materials.IsPiston(mat))
                        {
                            world.SetCell(cx, cy, Materials.Air);
                            world.MarkDirty(cx, cy);
                        }
                        terrainColliders.MarkChunkDirtyAt(cx, cy);
                    }
                }

                UpdateChunksStructureFlag(piston.baseCellX, piston.baseCellY);
                pistons.RemoveAt(i);
                return true;
            }
            return false;
        }

        // =====================================================================
        // Motor update (called each physics step)
        // =====================================================================

        public void UpdateMotors()
        {
            float desiredStrokeT = CalculateDesiredStrokeT();
            int desiredFill = Mathf.RoundToInt(desiredStrokeT * MaxTravel);

            for (int i = 0; i < pistons.Count; i++)
            {
                var piston = pistons[i];
                if (piston.armCluster == null || piston.armCluster.rb == null) continue;

                int currentFill = piston.lastFillExtent;

                if (desiredFill > currentFill)
                {
                    // Extending — push materials ahead and advance one cell
                    if (TryPushAndExtend(piston))
                    {
                        WriteFillSlice(piston, currentFill);
                        piston.lastFillExtent = currentFill + 1;
                        piston.currentStrokeT = (currentFill + 1f) / MaxTravel;
                    }
                }
                else if (desiredFill < currentFill)
                {
                    // Retracting — always succeeds
                    ClearFillSlice(piston, currentFill - 1);
                    piston.lastFillExtent = currentFill - 1;
                    piston.currentStrokeT = (currentFill - 1f) / MaxTravel;
                }

                // Move plate
                Vector2 targetPos = Vector2.Lerp(
                    piston.retractedWorldPos, piston.extendedWorldPos,
                    piston.currentStrokeT);
                piston.armCluster.rb.MovePosition(targetPos);

                // Update fill collider
                UpdateFillCollider(piston);
            }
        }

        // =====================================================================
        // Visual update (called each frame)
        // =====================================================================

        public void UpdateVisuals()
        {
            for (int i = 0; i < pistons.Count; i++)
            {
                var piston = pistons[i];
                if (piston.rodRenderer == null || piston.armCluster == null) continue;

                int fillExtent = piston.lastFillExtent;
                if (fillExtent <= 0)
                {
                    piston.rodRenderer.enabled = false;
                    continue;
                }

                piston.rodRenderer.enabled = true;
                float length = fillExtent * CoordinateUtils.CellToWorldScale;
                Vector2 center = CalculateShaftCenter(piston, fillExtent);
                piston.rodObject.transform.position = new Vector3(center.x, center.y, 0);

                bool horizontal = piston.direction == PistonDirection.Right ||
                                  piston.direction == PistonDirection.Left;
                piston.rodObject.transform.localScale = horizontal
                    ? new Vector3(length, ShaftThicknessWorld, 1f)
                    : new Vector3(ShaftThicknessWorld, length, 1f);
            }
        }

        // =====================================================================
        // Query
        // =====================================================================

        public bool HasPistonAt(int cellX, int cellY)
        {
            for (int i = 0; i < pistons.Count; i++)
            {
                var p = pistons[i];
                if (cellX >= p.baseCellX && cellX < p.baseCellX + BlockSize &&
                    cellY >= p.baseCellY && cellY < p.baseCellY + BlockSize)
                    return true;
            }
            return false;
        }

        // =====================================================================
        // Fill area operations
        // =====================================================================

        private void WriteFillSlice(PistonData piston, int fillIndex)
        {
            GetFillSliceCells(piston, fillIndex,
                out int startX, out int startY, out int dx, out int dy, out int count);

            for (int i = 0; i < count; i++)
            {
                int cx = startX + dx * i;
                int cy = startY + dy * i;
                if (world.IsInBounds(cx, cy))
                {
                    world.SetCell(cx, cy, Materials.PistonBase);
                    world.MarkDirty(cx, cy);
                    terrainColliders.MarkChunkDirtyAt(cx, cy);
                }
            }
        }

        private void ClearFillSlice(PistonData piston, int fillIndex)
        {
            GetFillSliceCells(piston, fillIndex,
                out int startX, out int startY, out int dx, out int dy, out int count);

            for (int i = 0; i < count; i++)
            {
                int cx = startX + dx * i;
                int cy = startY + dy * i;
                if (world.IsInBounds(cx, cy))
                {
                    world.SetCell(cx, cy, Materials.Air);
                    world.MarkDirty(cx, cy);
                    terrainColliders.MarkChunkDirtyAt(cx, cy);

                    // Wake neighboring cells so materials fall/flow into the gap
                    if (world.IsInBounds(cx, cy - 1)) world.MarkDirty(cx, cy - 1);
                    if (world.IsInBounds(cx, cy + 1)) world.MarkDirty(cx, cy + 1);
                    if (world.IsInBounds(cx - 1, cy)) world.MarkDirty(cx - 1, cy);
                    if (world.IsInBounds(cx + 1, cy)) world.MarkDirty(cx + 1, cy);
                }
            }
        }

        private void GetFillSliceCells(PistonData piston, int fillIndex,
            out int startX, out int startY, out int dx, out int dy, out int count)
        {
            int gx = piston.baseCellX;
            int gy = piston.baseCellY;
            count = BlockSize;

            switch (piston.direction)
            {
                case PistonDirection.Right:
                    startX = gx + BaseThickness + fillIndex;
                    startY = gy;
                    dx = 0; dy = 1;
                    break;
                case PistonDirection.Left:
                    startX = gx + BlockSize - BaseThickness - 1 - fillIndex;
                    startY = gy;
                    dx = 0; dy = 1;
                    break;
                case PistonDirection.Down:
                    startX = gx;
                    startY = gy + BaseThickness + fillIndex;
                    dx = 1; dy = 0;
                    break;
                case PistonDirection.Up:
                    startX = gx;
                    startY = gy + BlockSize - BaseThickness - 1 - fillIndex;
                    dx = 1; dy = 0;
                    break;
                default:
                    startX = startY = dx = dy = 0;
                    break;
            }
        }

        // =====================================================================
        // Stalling logic
        // =====================================================================

        /// <summary>
        /// Attempts to push materials ahead of the plate and extend one cell.
        /// For each leading-edge cell, shifts the chain of non-static cells forward
        /// by one in the push direction. Stalls if any row is completely blocked
        /// (hits a static material or world edge with no air gap).
        /// </summary>
        private bool TryPushAndExtend(PistonData piston)
        {
            int n = piston.lastFillExtent;
            if (n >= MaxTravel) return false;

            GetLeadingEdgeCells(piston, n,
                out int startX, out int startY,
                out int iterDx, out int iterDy,
                out int pushDx, out int pushDy, out int count);

            // First pass: validate all rows can be pushed
            for (int i = 0; i < count; i++)
            {
                int cx = startX + iterDx * i;
                int cy = startY + iterDy * i;

                if (!world.IsInBounds(cx, cy)) return false;

                byte mat = world.GetCell(cx, cy);
                if (mat == Materials.Air)
                {
                    pushChainBuffer[i] = 0;
                    continue;
                }

                // Walk in push direction to find air or blocker
                int chainLen = 1;
                bool foundAir = false;
                const int maxScan = 64;

                for (int s = 1; s <= maxScan; s++)
                {
                    int sx = cx + pushDx * s;
                    int sy = cy + pushDy * s;

                    if (!world.IsInBounds(sx, sy)) break;

                    byte sMat = world.GetCell(sx, sy);
                    if (sMat == Materials.Air)
                    {
                        foundAir = true;
                        break;
                    }

                    // Static materials block the push chain
                    if (world.materials[sMat].behaviour == BehaviourType.Static)
                        break;

                    chainLen++;
                }

                if (!foundAir) return false;
                pushChainBuffer[i] = chainLen;
            }

            // Second pass: push cells (shift from far end to near end to avoid overwriting)
            for (int i = 0; i < count; i++)
            {
                if (pushChainBuffer[i] == 0) continue;

                int cx = startX + iterDx * i;
                int cy = startY + iterDy * i;

                for (int s = pushChainBuffer[i]; s >= 1; s--)
                {
                    int fromX = cx + pushDx * (s - 1);
                    int fromY = cy + pushDy * (s - 1);
                    int toX = cx + pushDx * s;
                    int toY = cy + pushDy * s;

                    byte cellMat = world.GetCell(fromX, fromY);
                    world.SetCell(toX, toY, cellMat);
                    world.MarkDirty(toX, toY);
                }

                // Clear the leading edge cell (fill/plate sync will overwrite)
                world.SetCell(cx, cy, Materials.Air);
                world.MarkDirty(cx, cy);
            }

            return true;
        }

        private void GetLeadingEdgeCells(PistonData piston, int fillExtent,
            out int startX, out int startY,
            out int iterDx, out int iterDy,
            out int pushDx, out int pushDy, out int count)
        {
            int gx = piston.baseCellX;
            int gy = piston.baseCellY;
            count = BlockSize;

            switch (piston.direction)
            {
                case PistonDirection.Right:
                    startX = gx + BaseThickness + PlateThickness + fillExtent;
                    startY = gy;
                    iterDx = 0; iterDy = 1;
                    pushDx = 1; pushDy = 0;
                    break;
                case PistonDirection.Left:
                    startX = gx + BlockSize - BaseThickness - PlateThickness - 1 - fillExtent;
                    startY = gy;
                    iterDx = 0; iterDy = 1;
                    pushDx = -1; pushDy = 0;
                    break;
                case PistonDirection.Down:
                    startX = gx;
                    startY = gy + BaseThickness + PlateThickness + fillExtent;
                    iterDx = 1; iterDy = 0;
                    pushDx = 0; pushDy = 1;
                    break;
                case PistonDirection.Up:
                    startX = gx;
                    startY = gy + BlockSize - BaseThickness - PlateThickness - 1 - fillExtent;
                    iterDx = 1; iterDy = 0;
                    pushDx = 0; pushDy = -1;
                    break;
                default:
                    startX = startY = iterDx = iterDy = pushDx = pushDy = 0;
                    break;
            }
        }

        // =====================================================================
        // Plate cluster creation
        // =====================================================================

        private List<ClusterPixel> CreatePlatePixels(PistonDirection direction)
        {
            var pixels = new List<ClusterPixel>();

            switch (direction)
            {
                case PistonDirection.Right:
                case PistonDirection.Left:
                    // 2 wide x 16 tall: localX = 0,1; localY = -7 to 8
                    for (int ly = -7; ly <= 8; ly++)
                        for (int lx = 0; lx < PlateThickness; lx++)
                            pixels.Add(new ClusterPixel((short)lx, (short)ly, Materials.PistonArm));
                    break;

                case PistonDirection.Down:
                case PistonDirection.Up:
                    // 16 wide x 2 tall: localX = -8 to 7; localY = 0, -1
                    for (int ly = 0; ly >= -(PlateThickness - 1); ly--)
                        for (int lx = -8; lx <= 7; lx++)
                            pixels.Add(new ClusterPixel((short)lx, (short)ly, Materials.PistonArm));
                    break;
            }

            return pixels;
        }

        private Vector2 CalculatePlateWorldPos(int gridX, int gridY, PistonDirection direction, bool retracted)
        {
            float cx, cy;
            float half = BlockSize / 2f;

            switch (direction)
            {
                case PistonDirection.Right:
                    cx = retracted ? gridX + BaseThickness : gridX + BlockSize - PlateThickness;
                    cy = gridY + half;
                    break;
                case PistonDirection.Left:
                    cx = retracted ? gridX + BlockSize - BaseThickness - PlateThickness : gridX;
                    cy = gridY + half;
                    break;
                case PistonDirection.Down:
                    cx = gridX + half;
                    cy = retracted ? gridY + BaseThickness : gridY + BlockSize - PlateThickness;
                    break;
                case PistonDirection.Up:
                    cx = gridX + half;
                    cy = retracted ? gridY + BlockSize - BaseThickness - PlateThickness : gridY;
                    break;
                default:
                    cx = gridX; cy = gridY;
                    break;
            }

            return CoordinateUtils.CellToWorld(cx, cy, world.width, world.height);
        }

        // =====================================================================
        // Base bar
        // =====================================================================

        private void WriteBaseBar(int gridX, int gridY, PistonDirection direction)
        {
            ForEachBaseBarCell(gridX, gridY, direction, (cx, cy) =>
            {
                world.SetCell(cx, cy, Materials.PistonBase);
                world.MarkDirty(cx, cy);
                terrainColliders.MarkChunkDirtyAt(cx, cy);
            });
        }

        private void ClearBaseBar(int gridX, int gridY, PistonDirection direction)
        {
            ForEachBaseBarCell(gridX, gridY, direction, (cx, cy) =>
            {
                world.SetCell(cx, cy, Materials.Air);
                world.MarkDirty(cx, cy);
            });
        }

        private void ForEachBaseBarCell(int gridX, int gridY, PistonDirection direction, Action<int, int> action)
        {
            switch (direction)
            {
                case PistonDirection.Right:
                    for (int dy = 0; dy < BlockSize; dy++)
                        for (int dx = 0; dx < BaseThickness; dx++)
                            action(gridX + dx, gridY + dy);
                    break;
                case PistonDirection.Left:
                    for (int dy = 0; dy < BlockSize; dy++)
                        for (int dx = BlockSize - BaseThickness; dx < BlockSize; dx++)
                            action(gridX + dx, gridY + dy);
                    break;
                case PistonDirection.Down:
                    for (int dy = 0; dy < BaseThickness; dy++)
                        for (int dx = 0; dx < BlockSize; dx++)
                            action(gridX + dx, gridY + dy);
                    break;
                case PistonDirection.Up:
                    for (int dy = BlockSize - BaseThickness; dy < BlockSize; dy++)
                        for (int dx = 0; dx < BlockSize; dx++)
                            action(gridX + dx, gridY + dy);
                    break;
            }
        }

        // =====================================================================
        // Anchor colliders
        // =====================================================================

        private void ConfigureBaseCollider(BoxCollider2D collider, PistonDirection direction)
        {
            float scale = CoordinateUtils.CellToWorldScale;

            switch (direction)
            {
                case PistonDirection.Right:
                    collider.offset = new Vector2(-7f * scale, 0f);
                    collider.size = new Vector2(BaseThickness * scale, BlockSize * scale);
                    break;
                case PistonDirection.Left:
                    collider.offset = new Vector2(7f * scale, 0f);
                    collider.size = new Vector2(BaseThickness * scale, BlockSize * scale);
                    break;
                case PistonDirection.Down:
                    collider.offset = new Vector2(0f, 7f * scale);
                    collider.size = new Vector2(BlockSize * scale, BaseThickness * scale);
                    break;
                case PistonDirection.Up:
                    collider.offset = new Vector2(0f, -7f * scale);
                    collider.size = new Vector2(BlockSize * scale, BaseThickness * scale);
                    break;
            }
        }

        private void UpdateFillCollider(PistonData piston)
        {
            int fillExtent = piston.lastFillExtent;

            if (fillExtent <= 0)
            {
                piston.fillCollider.enabled = false;
                return;
            }

            piston.fillCollider.enabled = true;
            float scale = CoordinateUtils.CellToWorldScale;
            float halfBlock = BlockSize / 2f;

            switch (piston.direction)
            {
                case PistonDirection.Right:
                {
                    float centerCellX = BaseThickness + fillExtent / 2f - halfBlock;
                    piston.fillCollider.offset = new Vector2(centerCellX * scale, 0f);
                    piston.fillCollider.size = new Vector2(fillExtent * scale, BlockSize * scale);
                    break;
                }
                case PistonDirection.Left:
                {
                    float centerCellX = BlockSize - BaseThickness - fillExtent / 2f - halfBlock;
                    piston.fillCollider.offset = new Vector2(centerCellX * scale, 0f);
                    piston.fillCollider.size = new Vector2(fillExtent * scale, BlockSize * scale);
                    break;
                }
                case PistonDirection.Down:
                {
                    float centerCellY = BaseThickness + fillExtent / 2f - halfBlock;
                    piston.fillCollider.offset = new Vector2(0f, -centerCellY * scale);
                    piston.fillCollider.size = new Vector2(BlockSize * scale, fillExtent * scale);
                    break;
                }
                case PistonDirection.Up:
                {
                    float centerCellY = BlockSize - BaseThickness - fillExtent / 2f - halfBlock;
                    piston.fillCollider.offset = new Vector2(0f, -centerCellY * scale);
                    piston.fillCollider.size = new Vector2(BlockSize * scale, fillExtent * scale);
                    break;
                }
            }
        }

        // =====================================================================
        // Shaft visual helpers
        // =====================================================================

        private Vector2 CalculateShaftCenter(PistonData piston, int fillExtent)
        {
            float cx, cy;
            int gx = piston.baseCellX;
            int gy = piston.baseCellY;
            float half = BlockSize / 2f;

            switch (piston.direction)
            {
                case PistonDirection.Right:
                    cx = gx + BaseThickness + fillExtent / 2f;
                    cy = gy + half;
                    break;
                case PistonDirection.Left:
                    cx = gx + BlockSize - BaseThickness - fillExtent / 2f;
                    cy = gy + half;
                    break;
                case PistonDirection.Down:
                    cx = gx + half;
                    cy = gy + BaseThickness + fillExtent / 2f;
                    break;
                case PistonDirection.Up:
                    cx = gx + half;
                    cy = gy + BlockSize - BaseThickness - fillExtent / 2f;
                    break;
                default:
                    cx = gx; cy = gy;
                    break;
            }

            return CoordinateUtils.CellToWorld(cx, cy, world.width, world.height);
        }

        // =====================================================================
        // Chunk management
        // =====================================================================

        private void MarkChunksHasStructure(int cellX, int cellY)
        {
            int startChunkX = cellX / CellWorld.ChunkSize;
            int startChunkY = cellY / CellWorld.ChunkSize;
            int endChunkX = (cellX + BlockSize - 1) / CellWorld.ChunkSize;
            int endChunkY = (cellY + BlockSize - 1) / CellWorld.ChunkSize;

            for (int cy = startChunkY; cy <= endChunkY; cy++)
            {
                for (int cx = startChunkX; cx <= endChunkX; cx++)
                {
                    if (cx >= 0 && cx < world.chunksX && cy >= 0 && cy < world.chunksY)
                    {
                        int idx = cy * world.chunksX + cx;
                        ChunkState chunk = world.chunks[idx];
                        chunk.flags |= ChunkFlags.HasStructure;
                        world.chunks[idx] = chunk;
                    }
                }
            }
        }

        private void UpdateChunksStructureFlag(int cellX, int cellY)
        {
            int startChunkX = cellX / CellWorld.ChunkSize;
            int startChunkY = cellY / CellWorld.ChunkSize;
            int endChunkX = (cellX + BlockSize - 1) / CellWorld.ChunkSize;
            int endChunkY = (cellY + BlockSize - 1) / CellWorld.ChunkSize;

            for (int chunkY = startChunkY; chunkY <= endChunkY; chunkY++)
            {
                for (int chunkX = startChunkX; chunkX <= endChunkX; chunkX++)
                {
                    if (chunkX >= 0 && chunkX < world.chunksX && chunkY >= 0 && chunkY < world.chunksY)
                    {
                        int chunkStartX = chunkX * CellWorld.ChunkSize;
                        int chunkStartY = chunkY * CellWorld.ChunkSize;
                        int chunkEndX = Math.Min(chunkStartX + CellWorld.ChunkSize, world.width);
                        int chunkEndY = Math.Min(chunkStartY + CellWorld.ChunkSize, world.height);

                        bool hasStructure = false;
                        for (int y = chunkStartY; y < chunkEndY && !hasStructure; y++)
                            for (int x = chunkStartX; x < chunkEndX && !hasStructure; x++)
                                if (HasPistonAt(x, y))
                                    hasStructure = true;

                        int idx = chunkY * world.chunksX + chunkX;
                        ChunkState chunk = world.chunks[idx];
                        if (hasStructure)
                            chunk.flags |= ChunkFlags.HasStructure;
                        else
                            chunk.flags &= unchecked((byte)~ChunkFlags.HasStructure);
                        world.chunks[idx] = chunk;
                    }
                }
            }
        }

        // =====================================================================
        // Dispose
        // =====================================================================

        public void Dispose()
        {
            for (int i = pistons.Count - 1; i >= 0; i--)
            {
                var piston = pistons[i];
                if (piston.armCluster != null)
                {
                    clusterManager?.Unregister(piston.armCluster);
                    UnityEngine.Object.Destroy(piston.armCluster.gameObject);
                }
                if (piston.anchorObject != null)
                    UnityEngine.Object.Destroy(piston.anchorObject);
                if (piston.rodObject != null)
                    UnityEngine.Object.Destroy(piston.rodObject);
            }
            pistons.Clear();
        }
    }
}
