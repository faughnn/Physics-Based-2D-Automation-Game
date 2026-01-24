using System.Collections.Generic;
using UnityEngine;

namespace FallingSand
{
    /// <summary>
    /// Factory for creating cluster GameObjects with proper physics setup.
    /// </summary>
    public static class ClusterFactory
    {
        /// <summary>
        /// Create a cluster from a list of pixels at a world position.
        /// </summary>
        public static ClusterData CreateCluster(
            List<ClusterPixel> pixels,
            Vector2 worldPosition,
            ClusterManager manager)
        {
            if (pixels == null || pixels.Count == 0)
            {
                Debug.LogError("[ClusterFactory] Cannot create cluster with no pixels");
                return null;
            }

            // 1. Create GameObject
            GameObject go = new GameObject($"Cluster_{manager.AllocateId()}");
            go.transform.position = new Vector3(worldPosition.x, worldPosition.y, 0);

            // 2. Add Rigidbody2D
            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 1f;
            rb.linearDamping = 0.1f;
            rb.angularDamping = 0.1f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // 3. Generate polygon outline from pixels using marching squares
            // Marching squares outputs in cell units, need to scale to world units
            Vector2[] outline = MarchingSquares.GenerateOutline(pixels);

            // Scale polygon from cell units to world units
            for (int i = 0; i < outline.Length; i++)
            {
                outline[i] = CoordinateUtils.ScaleCellToWorld(outline[i]);
            }

            if (outline.Length < 3)
            {
                // Fallback: create a simple box collider
                Debug.LogWarning("[ClusterFactory] Marching squares produced insufficient vertices, using box collider");
                BoxCollider2D box = go.AddComponent<BoxCollider2D>();
                Vector2 size = CoordinateUtils.ScaleCellToWorld(CalculateBounds(pixels));
                box.size = size;
            }
            else
            {
                // 4. Add PolygonCollider2D with generated outline
                PolygonCollider2D poly = go.AddComponent<PolygonCollider2D>();
                poly.SetPath(0, outline);

                // Set physics material
                PhysicsMaterial2D material = new PhysicsMaterial2D("ClusterMaterial");
                material.friction = manager.defaultFriction;
                material.bounciness = manager.defaultBounciness;
                poly.sharedMaterial = material;
            }

            // 5. Add ClusterData component
            ClusterData data = go.AddComponent<ClusterData>();
            data.clusterId = ushort.Parse(go.name.Split('_')[1]);
            data.pixels = new List<ClusterPixel>(pixels);
            data.rb = rb;
            data.polyCollider = go.GetComponent<PolygonCollider2D>();

            // 5b. Calculate bounding radius (max distance from center to any pixel corner)
            float maxRadius = 0f;
            foreach (var p in pixels)
            {
                // +1 because pixel extends from localX to localX+1
                float r = Mathf.Max(Mathf.Abs(p.localX) + 1, Mathf.Abs(p.localY) + 1);
                if (r > maxRadius) maxRadius = r;
            }
            data.localRadius = maxRadius;

            // 6. Calculate physics properties from pixels
            CalculatePhysicsProperties(data, manager.defaultDensity);

            // 7. Register with manager
            manager.Register(data);

            return data;
        }

        /// <summary>
        /// Create a cluster from a region in the world grid.
        /// Extracts pixels from the world and converts them to a cluster.
        /// </summary>
        public static ClusterData CreateClusterFromRegion(
            CellWorld world,
            int startX, int startY,
            int regionWidth, int regionHeight,
            ClusterManager manager)
        {
            List<ClusterPixel> pixels = new List<ClusterPixel>();

            // Calculate center of the region in cell coordinates
            float cellCenterX = startX + regionWidth / 2f;
            float cellCenterY = startY + regionHeight / 2f;

            // Convert cell center to world coordinates
            Vector2 worldCenter = CoordinateUtils.CellToWorld(cellCenterX, cellCenterY, world.width, world.height);
            float worldCenterX = worldCenter.x;
            float worldCenterY = worldCenter.y;

            // Extract non-air cells from the region
            for (int y = startY; y < startY + regionHeight; y++)
            {
                for (int x = startX; x < startX + regionWidth; x++)
                {
                    if (!world.IsInBounds(x, y))
                        continue;

                    int index = y * world.width + x;
                    Cell cell = world.cells[index];

                    if (cell.materialId != Materials.Air && cell.ownerId == 0)
                    {
                        // Convert to local coordinates relative to center
                        // Use Unity convention (Y+ = up) by flipping Y
                        // Cell grid: Y+ = down, so we negate the Y offset
                        short localX = (short)(x - cellCenterX);
                        short localY = (short)(cellCenterY - y);  // Flipped for Unity convention

                        pixels.Add(new ClusterPixel(localX, localY, cell.materialId));

                        // Clear the cell from the world (it's now part of the cluster)
                        cell.materialId = Materials.Air;
                        cell.ownerId = 0;
                        world.cells[index] = cell;
                    }
                }
            }

            if (pixels.Count == 0)
            {
                Debug.LogWarning("[ClusterFactory] No pixels found in region");
                return null;
            }

            return CreateCluster(pixels, new Vector2(worldCenterX, worldCenterY), manager);
        }

        /// <summary>
        /// Calculate physics properties (mass, center of mass, moment of inertia) from pixels.
        /// </summary>
        private static void CalculatePhysicsProperties(ClusterData cluster, float densityScale)
        {
            if (cluster.pixels.Count == 0) return;

            float totalMass = 0;
            float sumX = 0, sumY = 0;
            float momentOfInertia = 0;

            // First pass: calculate mass and center of mass
            foreach (var pixel in cluster.pixels)
            {
                // Each pixel has mass based on material density
                // For simplicity, using 1 unit mass per pixel
                float mass = densityScale;
                totalMass += mass;
                sumX += pixel.localX * mass;
                sumY += pixel.localY * mass;
            }

            // Center of mass in cell units, then scaled to world units
            Vector2 centerOfMassCell = new Vector2(sumX / totalMass, sumY / totalMass);
            Vector2 centerOfMass = CoordinateUtils.ScaleCellToWorld(centerOfMassCell);

            // Second pass: calculate moment of inertia around center of mass
            // Use world units for distance
            foreach (var pixel in cluster.pixels)
            {
                float dx = CoordinateUtils.ScaleCellToWorld(pixel.localX - centerOfMassCell.x);
                float dy = CoordinateUtils.ScaleCellToWorld(pixel.localY - centerOfMassCell.y);
                float rSquared = dx * dx + dy * dy;
                momentOfInertia += densityScale * rSquared;
            }

            // Apply to Rigidbody2D
            cluster.rb.mass = totalMass;
            cluster.rb.centerOfMass = centerOfMass;
            cluster.rb.inertia = Mathf.Max(momentOfInertia, 0.1f); // Minimum inertia to prevent instability
        }

        /// <summary>
        /// Calculate the bounding box size of a pixel list.
        /// </summary>
        private static Vector2 CalculateBounds(List<ClusterPixel> pixels)
        {
            if (pixels.Count == 0)
                return Vector2.one;

            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;

            foreach (var pixel in pixels)
            {
                if (pixel.localX < minX) minX = pixel.localX;
                if (pixel.localX > maxX) maxX = pixel.localX;
                if (pixel.localY < minY) minY = pixel.localY;
                if (pixel.localY > maxY) maxY = pixel.localY;
            }

            return new Vector2(maxX - minX + 1, maxY - minY + 1);
        }

        /// <summary>
        /// Create a test cluster with a circular shape.
        /// </summary>
        public static ClusterData CreateCircleCluster(
            Vector2 worldPosition,
            int radius,
            byte materialId,
            ClusterManager manager)
        {
            List<ClusterPixel> pixels = new List<ClusterPixel>();

            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x * x + y * y <= radius * radius)
                    {
                        pixels.Add(new ClusterPixel((short)x, (short)y, materialId));
                    }
                }
            }

            return CreateCluster(pixels, worldPosition, manager);
        }

        /// <summary>
        /// Create a test cluster with a square shape.
        /// </summary>
        public static ClusterData CreateSquareCluster(
            Vector2 worldPosition,
            int size,
            byte materialId,
            ClusterManager manager)
        {
            List<ClusterPixel> pixels = new List<ClusterPixel>();
            int half = size / 2;

            for (int y = -half; y <= half; y++)
            {
                for (int x = -half; x <= half; x++)
                {
                    pixels.Add(new ClusterPixel((short)x, (short)y, materialId));
                }
            }

            return CreateCluster(pixels, worldPosition, manager);
        }

        /// <summary>
        /// Create a test cluster with an L-shape.
        /// </summary>
        public static ClusterData CreateLShapeCluster(
            Vector2 worldPosition,
            int size,
            byte materialId,
            ClusterManager manager)
        {
            List<ClusterPixel> pixels = new List<ClusterPixel>();
            int half = size / 2;

            // Vertical bar
            for (int y = -half; y <= half; y++)
            {
                for (int x = -half; x <= -half + size / 3; x++)
                {
                    pixels.Add(new ClusterPixel((short)x, (short)y, materialId));
                }
            }

            // Horizontal bar (bottom)
            for (int x = -half; x <= half; x++)
            {
                for (int y = -half; y <= -half + size / 3; y++)
                {
                    if (!pixels.Exists(p => p.localX == x && p.localY == y))
                    {
                        pixels.Add(new ClusterPixel((short)x, (short)y, materialId));
                    }
                }
            }

            return CreateCluster(pixels, worldPosition, manager);
        }
    }
}
