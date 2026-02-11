using System.Collections.Generic;
using UnityEngine;

namespace FallingSand
{
    /// <summary>
    /// Manages static colliders for terrain (stone, etc.) so clusters can collide with them.
    /// Uses chunk-based collider generation - each chunk with static materials gets a PolygonCollider2D.
    /// </summary>
    public class TerrainColliderManager : MonoBehaviour
    {
        [Header("Debug")]
        public bool logColliderUpdates = true;

        private CellWorld world;
        private GameObject collidersParent;

        // One collider per chunk that has static materials
        private Dictionary<int, PolygonCollider2D> chunkColliders = new Dictionary<int, PolygonCollider2D>();

        // Track which chunks need collider updates
        private HashSet<int> dirtyChunks = new HashSet<int>();

        /// <summary>
        /// Initialize with reference to the cell world.
        /// </summary>
        public void Initialize(CellWorld cellWorld)
        {
            world = cellWorld;

            // Create parent object for colliders
            collidersParent = new GameObject("TerrainColliders");
            collidersParent.transform.SetParent(transform);

            // Create world boundary colliders
            CreateWorldBoundaries();

            // Chunk colliders are generated on-demand when static materials are drawn

        }

        /// <summary>
        /// Create edge colliders around the world boundaries.
        /// </summary>
        private void CreateWorldBoundaries()
        {
            GameObject boundaryObj = new GameObject("WorldBoundaries");
            boundaryObj.transform.SetParent(collidersParent.transform);

            // World spans from -width to +width in X, -height to +height in Y (due to CellToWorldScale)
            float halfWidth = world.width;   // width * CoordinateUtils.CellToWorldScale / 2 = width
            float halfHeight = world.height; // height * CoordinateUtils.CellToWorldScale / 2 = height

            // Create edge collider for boundaries
            EdgeCollider2D edge = boundaryObj.AddComponent<EdgeCollider2D>();

            // Define boundary points (clockwise from bottom-left)
            Vector2[] points = new Vector2[]
            {
                new Vector2(-halfWidth, -halfHeight),  // bottom-left
                new Vector2(-halfWidth, halfHeight),   // top-left
                new Vector2(halfWidth, halfHeight),    // top-right
                new Vector2(halfWidth, -halfHeight),   // bottom-right
                new Vector2(-halfWidth, -halfHeight),  // back to start (closed loop)
            };

            edge.points = points;

        }

        /// <summary>
        /// Mark a chunk as needing collider regeneration.
        /// Call this when static materials are added/removed in a chunk.
        /// </summary>
        public void MarkChunkDirty(int chunkIndex)
        {
            dirtyChunks.Add(chunkIndex);
        }

        /// <summary>
        /// Mark a chunk dirty by cell position.
        /// </summary>
        public void MarkChunkDirtyAt(int cellX, int cellY)
        {
            int chunkX = cellX / CellWorld.ChunkSize;
            int chunkY = cellY / CellWorld.ChunkSize;
            int chunkIndex = chunkY * world.chunksX + chunkX;
            MarkChunkDirty(chunkIndex);
        }

        /// <summary>
        /// Process all dirty chunks immediately and clear the dirty set.
        /// Called by SimulationManager before physics to ensure colliders are fresh.
        /// </summary>
        public void ProcessDirtyChunks()
        {
            if (dirtyChunks.Count == 0) return;

            foreach (int chunkIndex in dirtyChunks)
            {
                UpdateChunkCollider(chunkIndex);
            }
            dirtyChunks.Clear();
        }

        /// <summary>
        /// Regenerate the collider for a specific chunk.
        /// </summary>
        private void UpdateChunkCollider(int chunkIndex)
        {
            int chunkX = chunkIndex % world.chunksX;
            int chunkY = chunkIndex / world.chunksX;

            // Collect static pixels in this chunk
            List<ClusterPixel> staticPixels = CollectStaticPixels(chunkX, chunkY);

            if (staticPixels.Count == 0)
            {
                // No static materials - remove collider if it exists
                if (chunkColliders.TryGetValue(chunkIndex, out PolygonCollider2D existing))
                {
                    Destroy(existing.gameObject);
                    chunkColliders.Remove(chunkIndex);
                }
                return;
            }

            // Generate polygon outlines using marching squares (handles disconnected regions)
            List<Vector2[]> outlines = MarchingSquares.GenerateOutlines(staticPixels);

            if (outlines.Count == 0)
            {
                // No valid outlines - remove collider if it exists
                if (chunkColliders.TryGetValue(chunkIndex, out PolygonCollider2D stale))
                {
                    Destroy(stale.gameObject);
                    chunkColliders.Remove(chunkIndex);
                }
                return;
            }

            // Scale and offset each outline to world coordinates
            int chunkCellX = chunkX * CellWorld.ChunkSize;
            int chunkCellY = chunkY * CellWorld.ChunkSize;

            for (int o = 0; o < outlines.Count; o++)
            {
                Vector2[] outline = outlines[o];
                for (int i = 0; i < outline.Length; i++)
                {
                    float cellX = outline[i].x + chunkCellX + CellWorld.ChunkSize / 2f;
                    float cellY = outline[i].y + chunkCellY + CellWorld.ChunkSize / 2f;
                    outline[i] = CoordinateUtils.CellToWorld(cellX, cellY, world.width, world.height);
                }
            }

            // Get or create collider
            PolygonCollider2D collider;
            if (chunkColliders.TryGetValue(chunkIndex, out collider))
            {
                // Update existing - set path count first to clear stale paths
                collider.pathCount = outlines.Count;
                for (int i = 0; i < outlines.Count; i++)
                    collider.SetPath(i, outlines[i]);
            }
            else
            {
                // Create new
                GameObject colliderObj = new GameObject($"ChunkCollider_{chunkX}_{chunkY}");
                colliderObj.transform.SetParent(collidersParent.transform);
                colliderObj.transform.position = Vector3.zero;

                collider = colliderObj.AddComponent<PolygonCollider2D>();
                collider.pathCount = outlines.Count;
                for (int i = 0; i < outlines.Count; i++)
                    collider.SetPath(i, outlines[i]);

                chunkColliders[chunkIndex] = collider;
            }
        }

        /// <summary>
        /// Collect all static material pixels in a chunk.
        /// Returns positions in local chunk coordinates.
        /// </summary>
        private List<ClusterPixel> CollectStaticPixels(int chunkX, int chunkY)
        {
            List<ClusterPixel> pixels = new List<ClusterPixel>();

            int baseX = chunkX * CellWorld.ChunkSize;
            int baseY = chunkY * CellWorld.ChunkSize;

            // Center of chunk in local coords
            int centerX = CellWorld.ChunkSize / 2;
            int centerY = CellWorld.ChunkSize / 2;

            for (int ly = 0; ly < CellWorld.ChunkSize; ly++)
            {
                int cellY = baseY + ly;
                if (cellY >= world.height) continue;

                for (int lx = 0; lx < CellWorld.ChunkSize; lx++)
                {
                    int cellX = baseX + lx;
                    if (cellX >= world.width) continue;

                    int index = cellY * world.width + cellX;
                    Cell cell = world.cells[index];

                    // Skip air and cluster-owned cells
                    if (cell.materialId == Materials.Air || cell.ownerId != 0)
                        continue;

                    // Check if static and not passable (lifts are static but passable)
                    // Also skip piston base/arm - they use their own colliders via MachineManager
                    MaterialDef mat = world.materials[cell.materialId];
                    if (mat.behaviour == BehaviourType.Static &&
                        (mat.flags & MaterialFlags.Passable) == 0 &&
                        !Materials.IsPiston(cell.materialId))
                    {
                        // Store in local coordinates relative to chunk center
                        short localX = (short)(lx - centerX);
                        short localY = (short)(ly - centerY);
                        pixels.Add(new ClusterPixel(localX, localY, cell.materialId));
                    }
                }
            }

            return pixels;
        }

        /// <summary>
        /// Force regeneration of all chunk colliders.
        /// </summary>
        public void RegenerateAllColliders()
        {
            for (int i = 0; i < world.chunksX * world.chunksY; i++)
            {
                dirtyChunks.Add(i);
            }
        }

        /// <summary>
        /// Process all dirty chunks with logging. Call after level load.
        /// </summary>
        public void ProcessDirtyChunksWithLog()
        {
            ProcessDirtyChunks();
            Debug.Log($"[TerrainColliderManager] Processed all dirty chunks, {chunkColliders.Count} colliders active");
        }

        private void OnDestroy()
        {
            if (collidersParent != null)
            {
                Destroy(collidersParent);
            }
        }
    }
}
