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
        [Header("Settings")]
        [Tooltip("How often to check for chunk updates (frames)")]
        public int updateInterval = 1;  // Every frame for debugging

        [Header("Debug")]
        public bool logColliderUpdates = true;

        private CellWorld world;
        private GameObject collidersParent;

        // One collider per chunk that has static materials
        private Dictionary<int, PolygonCollider2D> chunkColliders = new Dictionary<int, PolygonCollider2D>();

        // Track which chunks need collider updates
        private HashSet<int> dirtyChunks = new HashSet<int>();

        // Frame counter for periodic updates
        private int frameCount = 0;

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

        private void Update()
        {
            frameCount++;

            // Periodic update of dirty chunks
            if (frameCount % updateInterval == 0 && dirtyChunks.Count > 0)
            {
                UpdateDirtyChunks();
            }
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
        /// Update colliders for all dirty chunks.
        /// </summary>
        private void UpdateDirtyChunks()
        {
            // Process a limited number per frame to avoid hitches
            int maxPerFrame = 4;
            int processed = 0;

            foreach (int chunkIndex in dirtyChunks)
            {
                UpdateChunkCollider(chunkIndex);
                processed++;

                if (processed >= maxPerFrame)
                    break;
            }

            // Remove processed chunks
            var toRemove = new List<int>();
            foreach (int chunkIndex in dirtyChunks)
            {
                toRemove.Add(chunkIndex);
                if (toRemove.Count >= maxPerFrame)
                    break;
            }
            foreach (int idx in toRemove)
            {
                dirtyChunks.Remove(idx);
            }
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

            // Generate polygon outline using marching squares
            Vector2[] outline = MarchingSquares.GenerateOutline(staticPixels);

            if (outline.Length < 3)
            {
                // Not enough points for a polygon
                return;
            }

            // Scale and offset the outline to world coordinates
            // Chunk origin in cells
            int chunkCellX = chunkX * CellWorld.ChunkSize;
            int chunkCellY = chunkY * CellWorld.ChunkSize;

            // Convert cell coords to world coords
            // worldX = cellX * 2 - worldWidth
            // worldY = worldHeight - cellY * 2
            for (int i = 0; i < outline.Length; i++)
            {
                // outline is in local cell coordinates relative to chunk
                // Add chunk offset, then convert to world
                float cellX = outline[i].x + chunkCellX + CellWorld.ChunkSize / 2f;
                float cellY = outline[i].y + chunkCellY + CellWorld.ChunkSize / 2f;

                outline[i] = CoordinateUtils.CellToWorld(cellX, cellY, world.width, world.height);
            }

            // Get or create collider
            PolygonCollider2D collider;
            if (chunkColliders.TryGetValue(chunkIndex, out collider))
            {
                // Update existing
                collider.SetPath(0, outline);
            }
            else
            {
                // Create new
                GameObject colliderObj = new GameObject($"ChunkCollider_{chunkX}_{chunkY}");
                colliderObj.transform.SetParent(collidersParent.transform);
                colliderObj.transform.position = Vector3.zero;

                collider = colliderObj.AddComponent<PolygonCollider2D>();
                collider.SetPath(0, outline);

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
                    MaterialDef mat = world.materials[cell.materialId];
                    if (mat.behaviour == BehaviourType.Static &&
                        (mat.flags & MaterialFlags.Passable) == 0)
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
        /// Process all dirty chunks immediately (blocking).
        /// Call this after level load to ensure colliders exist before gameplay.
        /// </summary>
        public void ProcessAllDirtyChunksNow()
        {
            foreach (int chunkIndex in dirtyChunks)
            {
                UpdateChunkCollider(chunkIndex);
            }
            dirtyChunks.Clear();
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
