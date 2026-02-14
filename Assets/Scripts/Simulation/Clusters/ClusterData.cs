using System.Collections.Generic;
using UnityEngine;

namespace FallingSand
{
    /// <summary>
    /// Component attached to cluster GameObjects. Holds pixel data and cached references.
    /// Unity's Rigidbody2D handles physics, this holds our pixel/grid sync data.
    /// </summary>
    public class ClusterData : MonoBehaviour
    {
        [Header("Identity")]
        public ushort clusterId;              // Unique ID (1-65535, 0 = invalid)

        [Header("Pixel Data")]
        public List<ClusterPixel> pixels;     // Pixels relative to center of mass
        public float localRadius;             // Max distance from center to any pixel corner (for broad-phase checks)

        [Header("Cached References")]
        public Rigidbody2D rb;
        public PolygonCollider2D polyCollider;

        // Pixel lookup grid for inverse mapping (gap-free sync)
        private byte[] pixelLookup;
        private int lookupMinX, lookupMinY;
        private int lookupWidth, lookupHeight;
        private bool lookupBuilt;

        // Sync state tracking for sleep optimization
        [HideInInspector] public bool isPixelsSynced;
        [HideInInspector] public Vector2 lastSyncedPosition;
        [HideInInspector] public float lastSyncedRotation;

        // Manual sleep tracking (physics solver maintains equilibrium velocity, so we force sleep)
        [HideInInspector] public int lowVelocityFrames;

        // Belt interaction - set by BeltManager before physics, used by ClusterManager after physics
        [HideInInspector] public bool isOnBelt;

        // Lift interaction - set by LiftManager before physics, used by ClusterManager after physics
        [HideInInspector] public bool isOnLift;

        // Machine part - arm clusters driven by joints should never be force-slept
        [HideInInspector] public bool isMachinePart;

        // Crush tracking - counts consecutive frames of opposing compression contacts
        [HideInInspector] public int crushPressureFrames;

        /// <summary>
        /// World position from Rigidbody2D.
        /// </summary>
        public Vector2 Position => rb != null ? rb.position : (Vector2)transform.position;

        /// <summary>
        /// Rotation in radians from Rigidbody2D.
        /// </summary>
        public float RotationRad => rb != null ? rb.rotation * Mathf.Deg2Rad : 0f;

        /// <summary>
        /// Linear velocity from Rigidbody2D.
        /// </summary>
        public Vector2 Velocity => rb != null ? rb.linearVelocity : Vector2.zero;

        // Local bounding box (valid after BuildPixelLookup)
        public int LocalMinX => lookupMinX;
        public int LocalMaxX => lookupMinX + lookupWidth - 1;
        public int LocalMinY => lookupMinY;
        public int LocalMaxY => lookupMinY + lookupHeight - 1;

        /// <summary>
        /// Build pixel lookup grid for inverse mapping. Called lazily, cached until pixels change.
        /// </summary>
        public void BuildPixelLookup()
        {
            if (lookupBuilt) return;

            if (pixels == null || pixels.Count == 0)
            {
                lookupWidth = 0;
                lookupHeight = 0;
                lookupBuilt = true;
                return;
            }

            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;
            foreach (var p in pixels)
            {
                if (p.localX < minX) minX = p.localX;
                if (p.localX > maxX) maxX = p.localX;
                if (p.localY < minY) minY = p.localY;
                if (p.localY > maxY) maxY = p.localY;
            }

            lookupMinX = minX;
            lookupMinY = minY;
            lookupWidth = maxX - minX + 1;
            lookupHeight = maxY - minY + 1;

            pixelLookup = new byte[lookupWidth * lookupHeight];

            foreach (var p in pixels)
            {
                int idx = (p.localY - lookupMinY) * lookupWidth + (p.localX - lookupMinX);
                pixelLookup[idx] = p.materialId;
            }

            lookupBuilt = true;
        }

        /// <summary>
        /// Get the material at a local pixel position. Returns Air (0) if no pixel exists there.
        /// </summary>
        public byte GetPixelMaterialAt(int localX, int localY)
        {
            int gx = localX - lookupMinX;
            int gy = localY - lookupMinY;
            if (gx < 0 || gx >= lookupWidth || gy < 0 || gy >= lookupHeight)
                return Materials.Air;
            return pixelLookup[gy * lookupWidth + gx];
        }

        /// <summary>
        /// Transform a local pixel position to cell grid coordinates.
        /// The cluster position is in Unity world coordinates, this converts to cell grid coords.
        /// </summary>
        /// <param name="pixel">Local pixel offset</param>
        /// <param name="worldWidth">Width of the cell world (in cells)</param>
        /// <param name="worldHeight">Height of the cell world (in cells)</param>
        public Vector2Int LocalToWorldCell(ClusterPixel pixel, int worldWidth, int worldHeight)
        {
            float cos = Mathf.Cos(RotationRad);
            float sin = Mathf.Sin(RotationRad);

            // Local pixel coords are in Unity convention (Y+ = up)
            // Use standard rotation formula (same as Unity's transform rotation)
            float rotatedX = pixel.localX * cos - pixel.localY * sin;
            float rotatedY = pixel.localX * sin + pixel.localY * cos;

            // Position is in Unity world coords, convert to cell coords
            // World X: ranges from -worldWidth to +worldWidth
            // World Y: ranges from +worldHeight to -worldHeight (Y flipped)
            Vector2 pos = Position;

            // Convert world position to cell position using RoundToInt for pixel placement
            Vector2 cellCenter = CoordinateUtils.WorldToCellFloat(pos, worldWidth, worldHeight);

            // Add rotated X offset directly, but SUBTRACT rotated Y offset
            // because cell grid Y+ = down, while Unity Y+ = up
            return new Vector2Int(
                Mathf.RoundToInt(cellCenter.x + rotatedX),
                Mathf.RoundToInt(cellCenter.y - rotatedY)
            );
        }

        /// <summary>
        /// Legacy method - use LocalToWorldCell for grid sync.
        /// This assumes position is already in cell coords (used internally by some methods).
        /// </summary>
        public Vector2Int LocalToWorld(ClusterPixel pixel)
        {
            float cos = Mathf.Cos(RotationRad);
            float sin = Mathf.Sin(RotationRad);

            // Rotate local position
            float rotatedX = pixel.localX * cos - pixel.localY * sin;
            float rotatedY = pixel.localX * sin + pixel.localY * cos;

            // Translate to world and round to grid
            Vector2 pos = Position;
            return new Vector2Int(
                Mathf.RoundToInt(pos.x + rotatedX),
                Mathf.RoundToInt(pos.y + rotatedY)
            );
        }

        /// <summary>
        /// Transform a local position (float) to world position (float).
        /// Used for polygon collider vertices.
        /// </summary>
        public Vector2 LocalToWorldFloat(Vector2 localPos)
        {
            float cos = Mathf.Cos(RotationRad);
            float sin = Mathf.Sin(RotationRad);

            float rotatedX = localPos.x * cos - localPos.y * sin;
            float rotatedY = localPos.x * sin + localPos.y * cos;

            return Position + new Vector2(rotatedX, rotatedY);
        }

        private void Awake()
        {
            if (pixels == null)
                pixels = new List<ClusterPixel>();
        }

        private void OnValidate()
        {
            // Cache references in editor
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (polyCollider == null) polyCollider = GetComponent<PolygonCollider2D>();
        }
    }
}
