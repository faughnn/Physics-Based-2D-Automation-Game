using UnityEngine;

namespace FallingSand
{
    /// <summary>
    /// Smoothly follows a target transform while clamping to world bounds.
    /// Prevents the camera from showing areas outside the cell world.
    /// Attach to the Main Camera.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Follow Settings")]
        [SerializeField] private float smoothSpeed = 8f;
        [SerializeField] private Vector2 deadZone = new Vector2(50f, 30f);  // World units
        [SerializeField] private float lookAheadFactor = 0f;  // 0 = no look-ahead

        [Header("Bounds")]
        [SerializeField] private bool clampToBounds = true;

        // World bounds
        private float worldMinX, worldMaxX;
        private float worldMinY, worldMaxY;

        // Cached camera properties
        private Camera cam;
        private float viewportHalfWidth;
        private float viewportHalfHeight;

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        /// <summary>
        /// Initialize camera bounds. Call after camera ortho size is set.
        /// </summary>
        /// <param name="followTarget">Transform to follow</param>
        /// <param name="minX">World minimum X</param>
        /// <param name="maxX">World maximum X</param>
        /// <param name="minY">World minimum Y</param>
        /// <param name="maxY">World maximum Y</param>
        public void Initialize(Transform followTarget, float minX, float maxX, float minY, float maxY)
        {
            target = followTarget;
            worldMinX = minX;
            worldMaxX = maxX;
            worldMinY = minY;
            worldMaxY = maxY;

            // Ensure camera is available
            if (cam == null)
                cam = GetComponent<Camera>();

            // Calculate viewport dimensions from camera
            viewportHalfHeight = cam.orthographicSize;
            viewportHalfWidth = viewportHalfHeight * cam.aspect;

            // Snap to target immediately on start
            if (target != null)
            {
                Vector3 targetPos = GetTargetPosition();
                targetPos = ClampPosition(targetPos);
                transform.position = new Vector3(targetPos.x, targetPos.y, transform.position.z);
            }
        }

        /// <summary>
        /// Set the target transform to follow.
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        private void LateUpdate()
        {
            if (target == null || cam == null) return;

            // Get desired camera position based on target
            Vector3 targetPos = GetTargetPosition();

            // Apply dead zone - only move camera if target exceeds dead zone boundary.
            // When target is outside dead zone, we move the desired position to the
            // edge of the dead zone, creating a "buffer" before camera movement starts.
            Vector3 currentPos = transform.position;
            Vector3 delta = targetPos - currentPos;

            // Dead zone check (horizontal)
            if (Mathf.Abs(delta.x) < deadZone.x)
                targetPos.x = currentPos.x;
            else
                targetPos.x = targetPos.x - Mathf.Sign(delta.x) * deadZone.x;

            // Dead zone check (vertical)
            if (Mathf.Abs(delta.y) < deadZone.y)
                targetPos.y = currentPos.y;
            else
                targetPos.y = targetPos.y - Mathf.Sign(delta.y) * deadZone.y;

            // Clamp to world bounds
            if (clampToBounds)
            {
                targetPos = ClampPosition(targetPos);
            }

            // Smooth follow
            Vector3 smoothed = Vector3.Lerp(currentPos, targetPos, smoothSpeed * Time.deltaTime);

            // Preserve Z position
            smoothed.z = transform.position.z;

            transform.position = smoothed;
        }

        private Vector3 GetTargetPosition()
        {
            Vector3 pos = target.position;

            // Optional: Look-ahead based on target velocity (if it has a Rigidbody2D)
            if (lookAheadFactor > 0)
            {
                Rigidbody2D rb = target.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    pos.x += rb.linearVelocity.x * lookAheadFactor;
                    pos.y += rb.linearVelocity.y * lookAheadFactor;
                }
            }

            return pos;
        }

        private Vector3 ClampPosition(Vector3 pos)
        {
            // Camera center must stay within bounds such that edges don't exceed world bounds
            float clampedX = Mathf.Clamp(pos.x,
                worldMinX + viewportHalfWidth,
                worldMaxX - viewportHalfWidth);

            float clampedY = Mathf.Clamp(pos.y,
                worldMinY + viewportHalfHeight,
                worldMaxY - viewportHalfHeight);

            return new Vector3(clampedX, clampedY, pos.z);
        }

        /// <summary>
        /// Immediately snap camera to target position (no smoothing).
        /// </summary>
        public void SnapToTarget()
        {
            if (target == null) return;

            Vector3 targetPos = GetTargetPosition();
            if (clampToBounds)
            {
                targetPos = ClampPosition(targetPos);
            }
            transform.position = new Vector3(targetPos.x, targetPos.y, transform.position.z);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (cam == null) cam = GetComponent<Camera>();
            if (cam == null) return;

            // Draw viewport bounds
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;

            Gizmos.color = Color.green;
            Vector3 pos = transform.position;
            Gizmos.DrawWireCube(pos, new Vector3(halfW * 2, halfH * 2, 0));

            // Draw dead zone
            if (deadZone.magnitude > 0)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(pos, new Vector3(deadZone.x * 2, deadZone.y * 2, 0));
            }
        }
#endif
    }
}
