using System.Collections.Generic;
using UnityEngine;

namespace FallingSand
{
    /// <summary>
    /// Generates polygon outlines from pixel data using the Marching Squares algorithm.
    /// Used to create PolygonCollider2D shapes for clusters.
    /// </summary>
    public static class MarchingSquares
    {
        /// <summary>
        /// Generate a polygon outline from a list of cluster pixels.
        /// Returns vertices in local space (relative to center of mass at origin).
        /// </summary>
        public static Vector2[] GenerateOutline(List<ClusterPixel> pixels)
        {
            if (pixels == null || pixels.Count == 0)
                return new Vector2[0];

            // Find bounds of the pixel region
            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;

            foreach (var pixel in pixels)
            {
                if (pixel.localX < minX) minX = pixel.localX;
                if (pixel.localX > maxX) maxX = pixel.localX;
                if (pixel.localY < minY) minY = pixel.localY;
                if (pixel.localY > maxY) maxY = pixel.localY;
            }

            // Create a binary grid (1 = solid, 0 = empty)
            // Add 1-cell padding around edges for marching squares
            int gridWidth = maxX - minX + 3;
            int gridHeight = maxY - minY + 3;
            bool[,] grid = new bool[gridWidth, gridHeight];

            // Fill the grid
            foreach (var pixel in pixels)
            {
                int gx = pixel.localX - minX + 1;
                int gy = pixel.localY - minY + 1;
                grid[gx, gy] = true;
            }

            // Generate contour using marching squares
            List<Vector2> contour = TraceContour(grid, gridWidth, gridHeight, minX - 1, minY - 1);

            // Simplify the contour (remove collinear points)
            List<Vector2> simplified = SimplifyContour(contour, 0.1f);

            return simplified.ToArray();
        }

        // Edge indices: 0=top, 1=right, 2=bottom, 3=left
        // For each config (1-14), maps entry edge -> exit edge
        // Saddle cases (5, 10) need center sampling to resolve ambiguity
        //
        // Corner bits (from GetSquareConfig):
        //   1---2      Bit 1 = top-left, Bit 2 = top-right
        //   |   |      Bit 4 = bottom-right, Bit 8 = bottom-left
        //   8---4
        //
        private static readonly int[,] EdgeTable = new int[16, 4]
        {
            // config: [entry from top(0), right(1), bottom(2), left(3)] -> exit edge
            // -1 means invalid entry for this config
            { -1, -1, -1, -1 }, // 0: empty - no edges
            {  3, -1, -1,  0 }, // 1: top-left only - edges: left(3), top(0)
            {  1,  0, -1, -1 }, // 2: top-right only - edges: top(0), right(1)
            { -1,  3, -1,  1 }, // 3: top row solid - edges: left(3), right(1)
            { -1,  2,  1, -1 }, // 4: bottom-right only - edges: right(1), bottom(2)
            { -1, -1, -1, -1 }, // 5: saddle (handled specially)
            {  2, -1,  0, -1 }, // 6: right column solid - edges: top(0), bottom(2)
            { -1, -1,  3,  2 }, // 7: only bottom-left empty - edges: left(3), bottom(2)
            { -1, -1,  3,  2 }, // 8: bottom-left only - edges: left(3), bottom(2)
            {  2, -1,  0, -1 }, // 9: left column solid - edges: top(0), bottom(2)
            { -1, -1, -1, -1 }, // 10: saddle (handled specially)
            { -1,  2,  1, -1 }, // 11: only bottom-right empty - edges: right(1), bottom(2)
            { -1,  3, -1,  1 }, // 12: bottom row solid - edges: left(3), right(1)
            {  1,  0, -1, -1 }, // 13: only top-right empty - edges: top(0), right(1)
            {  3, -1, -1,  0 }, // 14: only top-left empty - edges: left(3), top(0)
            { -1, -1, -1, -1 }, // 15: full - no edges
        };

        /// <summary>
        /// Trace the contour of a binary grid using marching squares.
        /// Uses proper edge-following algorithm.
        /// </summary>
        private static List<Vector2> TraceContour(bool[,] grid, int width, int height, int offsetX, int offsetY)
        {
            List<Vector2> contour = new List<Vector2>();

            // Find starting point: scan for a boundary cell on the left edge
            // This guarantees we start with a known entry direction
            int startX = -1, startY = -1;
            int startEdge = -1;

            // Scan from bottom-left, looking for first boundary cell
            // We prefer to start at corners/edges where we have a known valid entry direction
            for (int y = 0; y < height - 1 && startX < 0; y++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    int config = GetSquareConfig(grid, x, y);
                    if (config == 0 || config == 15) continue; // Skip empty/full

                    // Find a valid entry edge for this config
                    for (int e = 0; e < 4; e++)
                    {
                        if (config == 5 || config == 10)
                        {
                            // Saddles have all edges valid
                            startX = x;
                            startY = y;
                            startEdge = e;
                            break;
                        }
                        else if (EdgeTable[config, e] != -1)
                        {
                            startX = x;
                            startY = y;
                            startEdge = e;
                            break;
                        }
                    }
                    if (startX >= 0) break;
                }
            }

            if (startX < 0 || startEdge < 0)
            {
                // No contour found
                return contour;
            }

            // Trace the contour by following edges
            int x1 = startX, y1 = startY;
            int entryEdge = startEdge;
            int maxIterations = width * height * 4;
            int iterations = 0;

            // Track visited cells with their entry edges to detect loops
            HashSet<long> visited = new HashSet<long>();

            do
            {
                // Create a key that includes position AND entry edge
                long key = ((long)x1 << 20) | ((long)y1 << 4) | (uint)entryEdge;
                if (visited.Contains(key))
                    break;
                visited.Add(key);

                int config = GetSquareConfig(grid, x1, y1);

                // Skip empty or full cells (shouldn't happen in a proper trace)
                if (config == 0 || config == 15)
                    break;

                // Get exit edge (handles saddle cases specially)
                int exitEdge = GetExitEdge(config, entryEdge, grid, x1, y1);
                if (exitEdge < 0)
                    break; // Invalid transition

                // Add the edge crossing point to contour
                Vector2 point = GetEdgeMidpoint(x1, y1, exitEdge, offsetX, offsetY);
                if (contour.Count == 0 || Vector2.Distance(contour[contour.Count - 1], point) > 0.01f)
                {
                    contour.Add(point);
                }

                // Move to the adjacent cell sharing this exit edge
                int nextX = x1, nextY = y1;
                int nextEntry = -1;
                switch (exitEdge)
                {
                    case 0: nextY = y1 + 1; nextEntry = 2; break; // exit top -> enter next from bottom
                    case 1: nextX = x1 + 1; nextEntry = 3; break; // exit right -> enter next from left
                    case 2: nextY = y1 - 1; nextEntry = 0; break; // exit bottom -> enter next from top
                    case 3: nextX = x1 - 1; nextEntry = 1; break; // exit left -> enter next from right
                }

                // Check bounds
                if (nextX < 0 || nextX >= width - 1 || nextY < 0 || nextY >= height - 1)
                    break;

                x1 = nextX;
                y1 = nextY;
                entryEdge = nextEntry;
                iterations++;
            }
            while ((x1 != startX || y1 != startY || entryEdge != startEdge) && iterations < maxIterations);

            return contour;
        }

        /// <summary>
        /// Get the exit edge for a given config and entry edge.
        /// Handles saddle cases (5, 10) with center sampling.
        /// </summary>
        private static int GetExitEdge(int config, int entryEdge, bool[,] grid, int x, int y)
        {
            // Handle saddle cases with center sampling
            if (config == 5)
            {
                // Config 5: top-left and bottom-right solid
                // Saddle - check center to determine connectivity
                bool centerSolid = IsCenterSolid(grid, x, y);
                if (centerSolid)
                {
                    // Connected diagonally: treat as two separate paths
                    // top-left group: entry from left(3) -> exit top(0), entry from top(0) -> exit left(3)
                    // bottom-right group: entry from right(1) -> exit bottom(2), entry from bottom(2) -> exit right(1)
                    switch (entryEdge)
                    {
                        case 0: return 3; // top -> left
                        case 1: return 2; // right -> bottom
                        case 2: return 1; // bottom -> right
                        case 3: return 0; // left -> top
                    }
                }
                else
                {
                    // Not connected: horizontal paths
                    switch (entryEdge)
                    {
                        case 0: return 1; // top -> right
                        case 1: return 0; // right -> top
                        case 2: return 3; // bottom -> left
                        case 3: return 2; // left -> bottom
                    }
                }
            }
            else if (config == 10)
            {
                // Config 10: top-right and bottom-left solid
                bool centerSolid = IsCenterSolid(grid, x, y);
                if (centerSolid)
                {
                    switch (entryEdge)
                    {
                        case 0: return 1; // top -> right
                        case 1: return 0; // right -> top
                        case 2: return 3; // bottom -> left
                        case 3: return 2; // left -> bottom
                    }
                }
                else
                {
                    switch (entryEdge)
                    {
                        case 0: return 3; // top -> left
                        case 1: return 2; // right -> bottom
                        case 2: return 1; // bottom -> right
                        case 3: return 0; // left -> top
                    }
                }
            }

            // Standard case: use lookup table
            return EdgeTable[config, entryEdge];
        }

        /// <summary>
        /// Check if the center of a 2x2 cell should be considered solid.
        /// Used for resolving saddle case ambiguity.
        /// </summary>
        private static bool IsCenterSolid(bool[,] grid, int x, int y)
        {
            // Count solid corners
            int solidCount = 0;
            if (grid[x, y]) solidCount++;
            if (grid[x + 1, y]) solidCount++;
            if (grid[x, y + 1]) solidCount++;
            if (grid[x + 1, y + 1]) solidCount++;

            // If exactly 2 diagonally opposite corners are solid, use average
            // For saddle cases, we connect diagonals if both corners of a diagonal are solid
            return solidCount >= 2;
        }

        /// <summary>
        /// Get the midpoint of a cell edge.
        /// </summary>
        private static Vector2 GetEdgeMidpoint(int cellX, int cellY, int edge, int offsetX, int offsetY)
        {
            float fx = cellX + offsetX;
            float fy = cellY + offsetY;

            switch (edge)
            {
                case 0: return new Vector2(fx + 0.5f, fy + 1f);   // top
                case 1: return new Vector2(fx + 1f, fy + 0.5f);   // right
                case 2: return new Vector2(fx + 0.5f, fy);        // bottom
                case 3: return new Vector2(fx, fy + 0.5f);        // left
                default: return new Vector2(fx + 0.5f, fy + 0.5f); // center (fallback)
            }
        }

        /// <summary>
        /// Get the marching squares configuration for a 2x2 cell.
        /// Returns a 4-bit value (0-15) based on which corners are solid.
        /// </summary>
        private static int GetSquareConfig(bool[,] grid, int x, int y)
        {
            int config = 0;
            if (grid[x, y + 1]) config |= 1;      // top-left
            if (grid[x + 1, y + 1]) config |= 2;  // top-right
            if (grid[x + 1, y]) config |= 4;      // bottom-right
            if (grid[x, y]) config |= 8;          // bottom-left
            return config;
        }

        /// <summary>
        /// Simplify a contour by removing nearly collinear points.
        /// Uses the Ramer-Douglas-Peucker algorithm.
        /// </summary>
        private static List<Vector2> SimplifyContour(List<Vector2> points, float epsilon)
        {
            if (points.Count < 3)
                return points;

            // Find the point with maximum distance from the line between first and last
            float maxDist = 0;
            int maxIndex = 0;

            Vector2 first = points[0];
            Vector2 last = points[points.Count - 1];

            for (int i = 1; i < points.Count - 1; i++)
            {
                float dist = PerpendicularDistance(points[i], first, last);
                if (dist > maxDist)
                {
                    maxDist = dist;
                    maxIndex = i;
                }
            }

            // If max distance is greater than epsilon, recursively simplify
            if (maxDist > epsilon)
            {
                List<Vector2> left = SimplifyContour(points.GetRange(0, maxIndex + 1), epsilon);
                List<Vector2> right = SimplifyContour(points.GetRange(maxIndex, points.Count - maxIndex), epsilon);

                // Combine (removing duplicate point)
                List<Vector2> result = new List<Vector2>(left);
                result.RemoveAt(result.Count - 1);
                result.AddRange(right);
                return result;
            }
            else
            {
                // Just keep endpoints
                return new List<Vector2> { first, last };
            }
        }

        /// <summary>
        /// Calculate perpendicular distance from point to line.
        /// </summary>
        private static float PerpendicularDistance(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
        {
            float dx = lineEnd.x - lineStart.x;
            float dy = lineEnd.y - lineStart.y;

            if (dx == 0 && dy == 0)
                return Vector2.Distance(point, lineStart);

            float t = ((point.x - lineStart.x) * dx + (point.y - lineStart.y) * dy) / (dx * dx + dy * dy);
            t = Mathf.Clamp01(t);

            Vector2 projection = new Vector2(lineStart.x + t * dx, lineStart.y + t * dy);
            return Vector2.Distance(point, projection);
        }
    }
}
