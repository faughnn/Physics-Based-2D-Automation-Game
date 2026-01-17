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

        [Header("Cached References")]
        public Rigidbody2D rb;
        public PolygonCollider2D polyCollider;

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

            // Convert world position to cell position
            // cellX = (worldX + worldWidth) / 2
            // cellY = (worldHeight - worldY) / 2
            float cellCenterX = (pos.x + worldWidth) / 2f;
            float cellCenterY = (worldHeight - pos.y) / 2f;

            // Add rotated X offset directly, but SUBTRACT rotated Y offset
            // because cell grid Y+ = down, while Unity Y+ = up
            return new Vector2Int(
                Mathf.RoundToInt(cellCenterX + rotatedX),
                Mathf.RoundToInt(cellCenterY - rotatedY)
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
