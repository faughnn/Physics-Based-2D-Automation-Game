using UnityEngine;
using UnityEngine.InputSystem;

namespace FallingSand
{
    /// <summary>
    /// Displays a 45-degree arc near the edge of the active tool's range circle,
    /// centered on the player-to-cursor direction. Fades in as the cursor approaches
    /// the range boundary and turns red when out of range.
    /// </summary>
    public class ToolRangeIndicator : MonoBehaviour
    {
        private const float ArcDegrees = 45f;
        private const int ArcSegments = 32;
        private const float FadeStartRatio = 0.8f;  // Arc appears at 80% of range
        private const float LineWidth = 3f;

        private static readonly Color ShovelColor = new Color(1f, 0.6f, 0f);  // Orange
        private static readonly Color GrabberColor = new Color(0f, 0.8f, 0.2f);  // Green
        private static readonly Color OutOfRangeColor = Color.red;

        private PlayerController player;
        private DiggingController digging;
        private CellGrabSystem grabSystem;
        private Camera mainCamera;
        private Mouse mouse;

        private LineRenderer lineRenderer;

        public void Initialize(PlayerController player, DiggingController digging, CellGrabSystem grabSystem)
        {
            this.player = player;
            this.digging = digging;
            this.grabSystem = grabSystem;

            mainCamera = Camera.main;
            mouse = Mouse.current;

            CreateLineRenderer();
        }

        private void CreateLineRenderer()
        {
            var arcObj = new GameObject("ToolRangeArc");
            arcObj.transform.SetParent(transform, false);

            lineRenderer = arcObj.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 0;
            lineRenderer.startWidth = LineWidth;
            lineRenderer.endWidth = LineWidth;
            lineRenderer.numCapVertices = 4;

            // Use a simple unlit material
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.sortingOrder = 15;
        }

        private void LateUpdate()
        {
            if (player == null || mouse == null || mainCamera == null)
            {
                Hide();
                return;
            }

            // Determine active tool and its range/color
            ToolType tool = player.EquippedTool;
            float range;
            Color toolColor;

            switch (tool)
            {
                case ToolType.Shovel:
                    range = digging != null ? digging.MaxDigDistance : 200f;
                    toolColor = ShovelColor;
                    break;
                case ToolType.Grabber:
                    range = grabSystem != null ? grabSystem.MaxGrabDistance : 1000f;
                    toolColor = GrabberColor;
                    break;
                default:
                    Hide();
                    return;
            }

            // Compute distance from player to cursor
            Vector2 mouseScreen = mouse.position.ReadValue();
            Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, 0));
            Vector2 playerPos = transform.position;
            Vector2 toMouse = (Vector2)mouseWorld - playerPos;
            float distance = toMouse.magnitude;

            float ratio = distance / range;

            // Hidden below 80% of range
            if (ratio < FadeStartRatio)
            {
                Hide();
                return;
            }

            // Compute alpha and color
            float alpha;
            Color arcColor;

            if (ratio <= 1f)
            {
                // Fading in: 80% -> 100%
                alpha = (ratio - FadeStartRatio) / (1f - FadeStartRatio);
                arcColor = toolColor;
            }
            else
            {
                // Out of range
                alpha = 1f;
                arcColor = OutOfRangeColor;
            }

            arcColor.a = alpha;

            // Build the arc centered on player-to-mouse direction
            float centerAngle = Mathf.Atan2(toMouse.y, toMouse.x) * Mathf.Rad2Deg;
            float halfArc = ArcDegrees * 0.5f;
            float startAngle = centerAngle - halfArc;
            float angleStep = ArcDegrees / ArcSegments;

            int pointCount = ArcSegments + 1;
            lineRenderer.positionCount = pointCount;
            lineRenderer.startColor = arcColor;
            lineRenderer.endColor = arcColor;

            for (int i = 0; i < pointCount; i++)
            {
                float angle = (startAngle + angleStep * i) * Mathf.Deg2Rad;
                Vector2 point = playerPos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * range;
                lineRenderer.SetPosition(i, new Vector3(point.x, point.y, 0f));
            }

            lineRenderer.enabled = true;
        }

        private void Hide()
        {
            if (lineRenderer != null)
                lineRenderer.enabled = false;
        }
    }
}
