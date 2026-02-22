using System.Collections.Generic;
using UnityEngine;

namespace FallingSand
{
    /// <summary>
    /// Handles compression detection and fracture logic for clusters.
    /// Extracted from ClusterManager for single-responsibility.
    /// </summary>
    public static class ClusterFracturer
    {
        // Compression detection settings
        private const float MinCrushImpulse = 5f;
        private const float OpposingDotThreshold = -0.5f;
        private const int CrushFrameThreshold = 30;
        private const int MinPixelsToFracture = 3;

        // Reusable buffers
        private static readonly ContactPoint2D[] contactBuffer = new ContactPoint2D[16];
        private static readonly List<ClusterData> clustersToFracture = new List<ClusterData>();

        /// <summary>
        /// Check all dynamic clusters for opposing compression contacts.
        /// Clusters under sustained compression are fractured into smaller pieces.
        /// </summary>
        public static void CheckCompressionAndFracture(ClusterManager manager)
        {
            clustersToFracture.Clear();

            foreach (var cluster in manager.AllClusters)
            {
                if (cluster.rb == null) continue;
                if (cluster.rb.IsSleeping()) continue;
                if (cluster.isMachinePart) continue;
                if (cluster.pixels.Count < MinPixelsToFracture * 2) continue;

                int contactCount = cluster.rb.GetContacts(contactBuffer);
                if (contactCount < 2)
                {
                    cluster.crushPressureFrames = 0;
                    continue;
                }

                bool hasOpposingPressure = false;
                for (int i = 0; i < contactCount && !hasOpposingPressure; i++)
                {
                    if (contactBuffer[i].normalImpulse < MinCrushImpulse) continue;

                    for (int j = i + 1; j < contactCount; j++)
                    {
                        if (contactBuffer[j].normalImpulse < MinCrushImpulse) continue;

                        float dot = Vector2.Dot(contactBuffer[i].normal, contactBuffer[j].normal);
                        if (dot < OpposingDotThreshold)
                        {
                            hasOpposingPressure = true;
                            break;
                        }
                    }
                }

                if (hasOpposingPressure)
                {
                    cluster.crushPressureFrames++;
                    if (cluster.crushPressureFrames > CrushFrameThreshold)
                        clustersToFracture.Add(cluster);
                }
                else
                {
                    cluster.crushPressureFrames = 0;
                }
            }

            for (int i = 0; i < clustersToFracture.Count; i++)
            {
                FractureCluster(clustersToFracture[i], manager);
            }
        }

        /// <summary>
        /// Fracture a cluster into smaller pieces using crack-line partitioning.
        /// No pixels are removed — small groups merge into the largest to preserve all material.
        /// </summary>
        public static void FractureCluster(ClusterData cluster, ClusterManager manager)
        {
            var pixels = cluster.pixels;
            int pixelCount = pixels.Count;

            if (pixelCount < MinPixelsToFracture * 2) return;

            // Compute bounding box of pixels in local space
            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;
            foreach (var p in pixels)
            {
                if (p.localX < minX) minX = p.localX;
                if (p.localX > maxX) maxX = p.localX;
                if (p.localY < minY) minY = p.localY;
                if (p.localY > maxY) maxY = p.localY;
            }

            // Generate crack lines
            int numCracks = (pixelCount < 20) ? 1 : Random.Range(1, 3);
            float centerX = (minX + maxX) * 0.5f;
            float centerY = (minY + maxY) * 0.5f;
            float extentX = (maxX - minX + 1) * 0.3f;
            float extentY = (maxY - minY + 1) * 0.3f;

            Vector2[] crackPoints = new Vector2[numCracks];
            Vector2[] crackNormals = new Vector2[numCracks];
            for (int i = 0; i < numCracks; i++)
            {
                crackPoints[i] = new Vector2(
                    centerX + Random.Range(-extentX, extentX),
                    centerY + Random.Range(-extentY, extentY));
                float angle = Random.Range(0f, Mathf.PI);
                crackNormals[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }

            // Partition pixels by signed distance to each crack line
            int maxGroups = 1 << numCracks;
            int[] groupCounts = new int[maxGroups];
            int[] pixelGroups = new int[pixelCount];

            for (int pi = 0; pi < pixelCount; pi++)
            {
                var p = pixels[pi];
                int group = 0;
                for (int ci = 0; ci < numCracks; ci++)
                {
                    float dx = p.localX - crackPoints[ci].x;
                    float dy = p.localY - crackPoints[ci].y;
                    float signedDist = dx * crackNormals[ci].x + dy * crackNormals[ci].y;
                    if (signedDist >= 0f)
                        group |= (1 << ci);
                }
                pixelGroups[pi] = group;
                groupCounts[group]++;
            }

            // Merge small groups into the largest group
            int largestGroup = 0;
            for (int g = 1; g < maxGroups; g++)
            {
                if (groupCounts[g] > groupCounts[largestGroup])
                    largestGroup = g;
            }

            for (int g = 0; g < maxGroups; g++)
            {
                if (g == largestGroup) continue;
                if (groupCounts[g] > 0 && groupCounts[g] < MinPixelsToFracture)
                {
                    groupCounts[largestGroup] += groupCounts[g];
                    groupCounts[g] = 0;
                    for (int pi = 0; pi < pixelCount; pi++)
                    {
                        if (pixelGroups[pi] == g)
                            pixelGroups[pi] = largestGroup;
                    }
                }
            }

            // Viability check: need at least 2 non-empty groups
            int viableGroups = 0;
            for (int g = 0; g < maxGroups; g++)
            {
                if (groupCounts[g] >= MinPixelsToFracture)
                    viableGroups++;
            }
            if (viableGroups < 2) return;

            // Create sub-clusters for each non-empty group
            Vector2 origVelocity = cluster.Velocity;
            float origAngularVel = cluster.rb != null ? cluster.rb.angularVelocity : 0f;
            float origRotation = cluster.rb != null ? cluster.rb.rotation : 0f;

            for (int g = 0; g < maxGroups; g++)
            {
                if (groupCounts[g] == 0) continue;

                var groupPixels = new List<ClusterPixel>(groupCounts[g]);
                float sumLX = 0, sumLY = 0;
                for (int pi = 0; pi < pixelCount; pi++)
                {
                    if (pixelGroups[pi] != g) continue;
                    var p = pixels[pi];
                    groupPixels.Add(p);
                    sumLX += p.localX;
                    sumLY += p.localY;
                }

                float centroidLX = sumLX / groupPixels.Count;
                float centroidLY = sumLY / groupPixels.Count;

                float cos = Mathf.Cos(cluster.RotationRad);
                float sin = Mathf.Sin(cluster.RotationRad);
                float rotCX = centroidLX * cos - centroidLY * sin;
                float rotCY = centroidLX * sin + centroidLY * cos;
                Vector2 worldCentroid = cluster.Position + CoordinateUtils.ScaleCellToWorld(new Vector2(rotCX, rotCY));

                var subPixels = new List<ClusterPixel>(groupPixels.Count);
                for (int pi = 0; pi < groupPixels.Count; pi++)
                {
                    var p = groupPixels[pi];
                    short newLX = (short)Mathf.RoundToInt(p.localX - centroidLX);
                    short newLY = (short)Mathf.RoundToInt(p.localY - centroidLY);
                    subPixels.Add(new ClusterPixel(newLX, newLY, p.materialId));
                }

                ClusterData subCluster = ClusterFactory.CreateCluster(subPixels, worldCentroid, manager);
                if (subCluster != null && subCluster.rb != null)
                {
                    subCluster.rb.linearVelocity = origVelocity;
                    subCluster.rb.angularVelocity = origAngularVel;
                    subCluster.rb.rotation = origRotation;
                }
            }

            // Cleanup original cluster
            manager.Unregister(cluster);
            Object.Destroy(cluster.gameObject);
        }
    }
}
