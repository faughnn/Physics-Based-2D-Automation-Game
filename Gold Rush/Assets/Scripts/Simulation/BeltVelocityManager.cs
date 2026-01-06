using UnityEngine;
using System.Collections.Generic;
using GoldRush.Core;

namespace GoldRush.Simulation
{
    public struct BeltSurface
    {
        public int MinX, MaxX;     // Horizontal bounds (inclusive)
        public int SurfaceY;       // Y coordinate where particles rest (just above belt)
        public bool MovesRight;    // Direction of belt movement
        public object Owner;       // Reference to owning belt (for unregistration)
        public HashSet<MaterialType> BlockedMaterials;  // null = move all, non-null = only move these types
    }

    // Represents a cell to be shifted
    struct ShiftCell
    {
        public int X, Y;
        public int DestX;
        public MaterialType Type;
        public Vector2 Velocity;
        public float SubPosX, SubPosY;
    }

    public class BeltVelocityManager
    {
        private static BeltVelocityManager _instance;
        public static BeltVelocityManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new BeltVelocityManager();
                return _instance;
            }
        }

        private List<BeltSurface> beltSurfaces = new List<BeltSurface>();
        private int frameCounter = 0;
        private HashSet<uint> clustersToMove = new HashSet<uint>();
        private Dictionary<(int, int), ShiftCell> cellsToShiftDict = new Dictionary<(int, int), ShiftCell>();
        private List<ShiftCell> cellsToShift = new List<ShiftCell>();

        public void RegisterBeltSurface(BeltSurface surface)
        {
            beltSurfaces.Add(surface);
        }

        public void UnregisterBeltSurface(object owner)
        {
            beltSurfaces.RemoveAll(s => s.Owner == owner);
        }

        // Called every frame from SimulationGrid.Update()
        public void Update(SimulationGrid grid)
        {
            if (beltSurfaces.Count == 0)
                return;

            frameCounter++;
            if (frameCounter >= GameSettings.BeltShiftInterval)
            {
                BulkShiftAllBelts(grid);
                frameCounter = 0;
            }
        }

        private void BulkShiftAllBelts(SimulationGrid grid)
        {
            clustersToMove.Clear();
            cellsToShiftDict.Clear();
            cellsToShift.Clear();

            // Phase 1: Collect all single cells (note clusters but don't move yet)
            // Uses dictionary to deduplicate - overlapping belts won't double-collect cells
            foreach (var belt in beltSurfaces)
            {
                CollectBeltContents(grid, belt);
            }

            // Convert dictionary to list for sorting
            cellsToShift.AddRange(cellsToShiftDict.Values);

            if (cellsToShift.Count > 0)
            {
                Debug.Log($"[BeltShift] Collected {cellsToShift.Count} cells (deduped from {beltSurfaces.Count} belts), {clustersToMove.Count} clusters");
            }

            // Phase 2: Clear all collected single cells
            int clearedCount = 0;
            int alreadyAir = 0;
            foreach (var cell in cellsToShift)
            {
                MaterialType before = grid.Get(cell.X, cell.Y);
                if (before != MaterialType.Air)
                {
                    grid.Set(cell.X, cell.Y, MaterialType.Air);
                    MaterialType after = grid.Get(cell.X, cell.Y);
                    if (after == MaterialType.Air)
                        clearedCount++;
                    else
                        Debug.LogWarning($"[BeltShift] FAILED to clear ({cell.X},{cell.Y})! Was {before}, now {after}");
                }
                else
                {
                    alreadyAir++;
                }
            }

            if (cellsToShift.Count > 0)
            {
                Debug.Log($"[BeltShift] Cleared {clearedCount}, already air {alreadyAir}");
            }

            // Phase 3: Move clusters (now that single cells are cleared)
            foreach (uint clusterId in clustersToMove)
            {
                var cluster = grid.ClusterManager.GetCluster(clusterId);
                if (cluster.HasValue)
                {
                    int direction = GetClusterBeltDirection(cluster.Value);
                    if (direction != 0)
                    {
                        grid.ClusterManager.MoveClusterDirect(clusterId, direction);
                    }
                }
            }

            // Phase 4: Sort cells so leading edge places first
            cellsToShift.Sort((a, b) => {
                bool aMovesRight = a.DestX > a.X;
                bool bMovesRight = b.DestX > b.X;

                if (aMovesRight && bMovesRight)
                    return b.DestX.CompareTo(a.DestX);
                else if (!aMovesRight && !bMovesRight)
                    return a.DestX.CompareTo(b.DestX);
                else
                    return 0;
            });

            // Phase 5: Place cells at shifted positions
            int placedDest = 0, placedOrig = 0, lost = 0;
            foreach (var cell in cellsToShift)
            {
                int result = PlaceShiftedCellLogged(grid, cell);
                if (result == 1) placedDest++;
                else if (result == 2) placedOrig++;
                else lost++;
            }

            if (cellsToShift.Count > 0)
            {
                Debug.Log($"[BeltShift] Placed: {placedDest} dest, {placedOrig} orig, {lost} lost");
            }
        }

        private int GetClusterBeltDirection(ClusterData cluster)
        {
            // Check which belt this cluster is on and return its direction
            foreach (var belt in beltSurfaces)
            {
                // Skip if belt has filter and cluster type isn't in it
                if (belt.BlockedMaterials != null && !belt.BlockedMaterials.Contains(cluster.Type))
                    continue;

                // Check if cluster's bottom row is at belt surface
                int clusterBottomY = cluster.OriginY + cluster.Size - 1;
                if (clusterBottomY == belt.SurfaceY)
                {
                    // Check if cluster overlaps belt X range
                    int clusterMinX = cluster.OriginX;
                    int clusterMaxX = cluster.OriginX + cluster.Size - 1;
                    if (clusterMaxX >= belt.MinX && clusterMinX <= belt.MaxX)
                    {
                        return belt.MovesRight ? 1 : -1;
                    }
                }
            }
            return 0;
        }

        private void CollectBeltContents(SimulationGrid grid, BeltSurface belt)
        {
            int direction = belt.MovesRight ? 1 : -1;

            // Scan the entire belt area and above
            for (int x = belt.MinX; x <= belt.MaxX; x++)
            {
                // Scan from surface upward (decreasing Y) to find all stacked material
                for (int y = belt.SurfaceY; y >= 0; y--)
                {
                    MaterialType type = grid.Get(x, y);

                    if (type == MaterialType.Air || !MaterialProperties.IsSimulated(type))
                    {
                        // Hit air or non-simulated - stop scanning this column upward
                        break;
                    }

                    // Check for cluster - note it for later (don't move during collection)
                    uint clusterId = grid.GetClusterID(x, y);
                    if (clusterId != 0)
                    {
                        // Check if belt should move this cluster type
                        if (belt.BlockedMaterials != null)
                        {
                            var clusterData = grid.ClusterManager.GetCluster(clusterId);
                            if (!clusterData.HasValue || !belt.BlockedMaterials.Contains(clusterData.Value.Type))
                                continue;
                        }
                        clustersToMove.Add(clusterId);
                        continue;
                    }

                    // Skip if this belt has a filter and doesn't want this material
                    if (belt.BlockedMaterials != null && !belt.BlockedMaterials.Contains(type))
                        continue;

                    // Collect single cell for shifting (dictionary deduplicates overlapping belts)
                    var key = (x, y);
                    if (!cellsToShiftDict.ContainsKey(key))
                    {
                        int destX = x + direction;
                        cellsToShiftDict[key] = new ShiftCell
                        {
                            X = x,
                            Y = y,
                            DestX = destX,
                            Type = type,
                            Velocity = grid.GetVelocity(x, y),
                            SubPosX = 0,
                            SubPosY = 0
                        };
                    }
                }
            }
        }

        // Returns: 1=placed at dest, 2=placed at orig, 0=lost
        private int PlaceShiftedCellLogged(SimulationGrid grid, ShiftCell cell)
        {
            // Try to place at shifted position
            if (grid.InBounds(cell.DestX, cell.Y) &&
                grid.Get(cell.DestX, cell.Y) == MaterialType.Air &&
                !grid.IsBlockedByInfrastructure(cell.DestX, cell.Y, cell.Type))
            {
                grid.Set(cell.DestX, cell.Y, cell.Type);
                grid.SetVelocity(cell.DestX, cell.Y, cell.Velocity);
                grid.WakeCell(cell.DestX, cell.Y);
                return 1;
            }
            else if (grid.Get(cell.X, cell.Y) == MaterialType.Air)
            {
                // Can't place at destination - put back at original position
                grid.Set(cell.X, cell.Y, cell.Type);
                grid.SetVelocity(cell.X, cell.Y, cell.Velocity);
                grid.WakeCell(cell.X, cell.Y);
                return 2;
            }
            else
            {
                // Both taken
                MaterialType atDest = grid.InBounds(cell.DestX, cell.Y) ? grid.Get(cell.DestX, cell.Y) : MaterialType.Air;
                MaterialType atOrig = grid.Get(cell.X, cell.Y);
                Debug.LogWarning($"[BeltShift] LOST ({cell.X},{cell.Y})->{cell.DestX}: dest={atDest}, orig={atOrig}");
                return 0;
            }
        }

        public void Clear()
        {
            beltSurfaces.Clear();
            cellsToShiftDict.Clear();
            cellsToShift.Clear();
            clustersToMove.Clear();
            frameCounter = 0;
        }

        public int SurfaceCount => beltSurfaces.Count;
    }
}
