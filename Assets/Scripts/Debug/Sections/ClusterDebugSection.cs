using System;
using UnityEngine;

namespace FallingSand.Debugging
{
    /// <summary>
    /// Flags for which cluster gizmos to display.
    /// </summary>
    [Flags]
    public enum ClusterGizmoFlags
    {
        None = 0,
        PolygonOutlines = 1 << 0,
        CenterOfMass = 1 << 1,
        VelocityVectors = 1 << 2,
        PixelPositions = 1 << 3,
        BoundingCircles = 1 << 4,
        All = PolygonOutlines | CenterOfMass | VelocityVectors | PixelPositions | BoundingCircles
    }

    /// <summary>
    /// Debug section for cluster physics.
    /// Shows cluster stats and gizmos for cluster visualization.
    /// </summary>
    public class ClusterDebugSection : DebugSectionBase
    {
        public override string SectionName => "Clusters";
        public override int Priority => 30;

        private readonly ClusterManager clusterManager;
        private readonly CellWorld world;

        private int cachedActiveCount;
        private int cachedTotalPixels;
        private int cachedDisplacements;
        private float cachedPhysicsMs;
        private float cachedSyncMs;

        /// <summary>
        /// Which gizmos to display.
        /// </summary>
        public ClusterGizmoFlags GizmoFlags { get; set; } =
            ClusterGizmoFlags.PolygonOutlines |
            ClusterGizmoFlags.CenterOfMass |
            ClusterGizmoFlags.VelocityVectors;

        public ClusterDebugSection(ClusterManager clusterManager, CellWorld world)
        {
            this.clusterManager = clusterManager;
            this.world = world;
        }

        protected override void UpdateCachedValues()
        {
            if (clusterManager != null)
            {
                cachedActiveCount = clusterManager.ActiveCount;
                cachedTotalPixels = clusterManager.TotalPixelCount;
                cachedDisplacements = clusterManager.DisplacementsThisFrame;
                cachedPhysicsMs = clusterManager.PhysicsTimeMs;
                cachedSyncMs = clusterManager.SyncTimeMs;
            }
        }

        public override int DrawGUI(GUIStyle labelStyle, float x, float y, float lineHeight)
        {
            if (labelStyle == null) return 5;

            float width = 260f;
            int lines = 0;

            DrawLabel($"Active Clusters: {cachedActiveCount}", x, y + lines * lineHeight, width, lineHeight, labelStyle, Color.white);
            lines++;

            DrawLabel($"Total Pixels: {cachedTotalPixels}", x, y + lines * lineHeight, width, lineHeight, labelStyle, Color.white);
            lines++;

            DrawLabel($"Displacements/frame: {cachedDisplacements}", x, y + lines * lineHeight, width, lineHeight, labelStyle, Color.white);
            lines++;

            Color physicsColor = GetPerformanceColor(cachedPhysicsMs, 2f, 5f);
            DrawLabel($"Physics: {cachedPhysicsMs:F2}ms", x, y + lines * lineHeight, width, lineHeight, labelStyle, physicsColor);
            lines++;

            Color syncColor = GetPerformanceColor(cachedSyncMs, 1f, 3f);
            DrawLabel($"Sync: {cachedSyncMs:F2}ms", x, y + lines * lineHeight, width, lineHeight, labelStyle, syncColor);
            lines++;

            return lines;
        }

        public override void DrawGizmos()
        {
            if (clusterManager == null || world == null) return;

            foreach (var cluster in clusterManager.AllClusters)
            {
                if (cluster == null) continue;

                Vector3 pos3 = new Vector3(cluster.Position.x, cluster.Position.y, 0);

                // Polygon outline (green)
                if ((GizmoFlags & ClusterGizmoFlags.PolygonOutlines) != 0 && cluster.polyCollider != null)
                {
                    Gizmos.color = Color.green;
                    DrawPolygonOutline(cluster);
                }

                // Center of mass (yellow)
                if ((GizmoFlags & ClusterGizmoFlags.CenterOfMass) != 0 && cluster.rb != null)
                {
                    Gizmos.color = Color.yellow;
                    Vector3 com = new Vector3(
                        cluster.rb.worldCenterOfMass.x,
                        cluster.rb.worldCenterOfMass.y, 0);
                    Gizmos.DrawWireSphere(com, 2f);
                }

                // Velocity vector (red arrow)
                if ((GizmoFlags & ClusterGizmoFlags.VelocityVectors) != 0 && cluster.rb != null)
                {
                    Gizmos.color = Color.red;
                    Vector3 vel = new Vector3(cluster.Velocity.x, cluster.Velocity.y, 0);
                    if (vel.magnitude > 0.5f)
                    {
                        Gizmos.DrawLine(pos3, pos3 + vel);
                        // Arrowhead
                        Vector3 right = Quaternion.Euler(0, 0, 30) * -vel.normalized * 4f;
                        Vector3 left = Quaternion.Euler(0, 0, -30) * -vel.normalized * 4f;
                        Gizmos.DrawLine(pos3 + vel, pos3 + vel + right);
                        Gizmos.DrawLine(pos3 + vel, pos3 + vel + left);
                    }
                }

                // Pixel positions (blue)
                if ((GizmoFlags & ClusterGizmoFlags.PixelPositions) != 0)
                {
                    Gizmos.color = new Color(0, 0.5f, 1f, 0.5f);
                    foreach (var pixel in cluster.pixels)
                    {
                        Vector2Int cellPos = cluster.LocalToWorldCell(pixel, world.width, world.height);
                        Vector2 pixelWorld = CellToWorld(new Vector2(cellPos.x, cellPos.y));
                        Gizmos.DrawCube(new Vector3(pixelWorld.x, pixelWorld.y, 0), Vector3.one * 1.5f);
                    }
                }

                // Bounding circle (cyan)
                if ((GizmoFlags & ClusterGizmoFlags.BoundingCircles) != 0)
                {
                    Gizmos.color = Color.cyan;
                    float radius = CalculateBoundingRadius(cluster);
                    Gizmos.DrawWireSphere(pos3, radius);
                }
            }
        }

        private void DrawPolygonOutline(ClusterData cluster)
        {
            if (cluster.polyCollider == null) return;

            Vector2[] points = cluster.polyCollider.points;
            if (points.Length < 2) return;

            for (int i = 0; i < points.Length; i++)
            {
                Vector2 worldA = cluster.LocalToWorldFloat(points[i]);
                Vector2 worldB = cluster.LocalToWorldFloat(points[(i + 1) % points.Length]);

                Gizmos.DrawLine(
                    new Vector3(worldA.x, worldA.y, 0),
                    new Vector3(worldB.x, worldB.y, 0));
            }
        }

        private float CalculateBoundingRadius(ClusterData cluster)
        {
            float maxDist = 0;
            foreach (var pixel in cluster.pixels)
            {
                float dist = Mathf.Sqrt(pixel.localX * pixel.localX + pixel.localY * pixel.localY);
                if (dist > maxDist) maxDist = dist;
            }
            return maxDist + 1;
        }

        private Vector2 CellToWorld(Vector2 cellPos)
        {
            if (world == null) return Vector2.zero;
            float worldX = cellPos.x * 2f - world.width;
            float worldY = world.height - cellPos.y * 2f;
            return new Vector2(worldX, worldY);
        }
    }
}
