using System.Collections.Generic;
using UnityEngine;

namespace Jovian.ZoneSystem {
    public static class PolygonUtils {
        /// <summary>
        ///     Ray-casting point-in-polygon test (Jordan curve theorem).
        ///     Works on any plane — caller projects the world position first via MapPlaneUtility.
        ///     Handles edge and vertex cases robustly.
        /// </summary>
        /// <param name="point">2D point already projected onto the polygon's plane.</param>
        /// <param name="polygon">Polygon vertices in the same 2D space.</param>
        public static bool PointInPolygon(Vector2 point, List<Vector2> polygon) {
            if(polygon == null || polygon.Count < 3) {
                return false;
            }

            float px = point.x;
            float py = point.y;
            bool inside = false;
            int count = polygon.Count;
            int j = count - 1;

            for(int i = 0; i < count; i++) {
                float xi = polygon[i].x, yi = polygon[i].y;
                float xj = polygon[j].x, yj = polygon[j].y;

                // Crossing test: does the edge (j→i) cross the horizontal ray from point?
                bool crosses = (yi > py) != (yj > py) &&
                    px < ((xj - xi) * (py - yi) / (yj - yi)) + xi;

                if(crosses) {
                    inside = !inside;
                }

                j = i;
            }

            return inside;
        }

        /// <summary>
        ///     Overload that accepts a world position and projects it onto the given plane
        ///     before testing — this is the primary API used by ZoneManager.
        /// </summary>
        public static bool PointInPolygon(Vector3 worldPos, List<Vector2> polygon, MapPlane plane) {
            Vector2 projected = MapPlaneUtility.ProjectToPlane(worldPos, plane);
            return PointInPolygon(projected, polygon);
        }

        /// <summary>
        ///     Returns the centroid of a polygon (for label placement in the editor).
        /// </summary>
        public static Vector2 Centroid(List<Vector2> polygon) {
            if(polygon == null || polygon.Count == 0) {
                return Vector2.zero;
            }

            Vector2 sum = Vector2.zero;
            foreach(Vector2 pt in polygon) {
                sum += pt;
            }
            return sum / polygon.Count;
        }

        /// <summary>
        ///     Returns the approximate axis-aligned bounding box of a polygon.
        ///     Useful for a cheap pre-check before running the full ray-cast test.
        /// </summary>
        public static (Vector2 min, Vector2 max) Bounds(List<Vector2> polygon) {
            if(polygon == null || polygon.Count == 0) {
                return (Vector2.zero, Vector2.zero);
            }

            Vector2 min = polygon[0], max = polygon[0];
            foreach(Vector2 pt in polygon) {
                if(pt.x < min.x) {
                    min.x = pt.x;
                }
                if(pt.y < min.y) {
                    min.y = pt.y;
                }
                if(pt.x > max.x) {
                    max.x = pt.x;
                }
                if(pt.y > max.y) {
                    max.y = pt.y;
                }
            }
            return (min, max);
        }

        /// <summary>
        ///     Fast AABB pre-check. Call this before PointInPolygon to skip the
        ///     ray-cast for points clearly outside the bounding box.
        /// </summary>
        public static bool PointInBounds(Vector2 point, Vector2 min, Vector2 max) {
            return point.x >= min.x && point.x <= max.x &&
                point.y >= min.y && point.y <= max.y;
        }

        /// <summary>
        ///     Ear-clipping triangulation for simple (non-self-intersecting) polygons.
        ///     Returns a list of triangle index triplets into the original vertex list.
        ///     Supports both convex and concave polygons.
        /// </summary>
        public static List<int> Triangulate(List<Vector2> polygon) {
            List<int> triangles = new List<int>();
            int n = polygon.Count;
            if(n < 3) {
                return triangles;
            }

            // Build index list
            List<int> indices = new List<int>(n);
            bool clockwise = SignedArea(polygon) < 0f;
            for(int i = 0; i < n; i++) {
                indices.Add(clockwise ? i : n - 1 - i);
            }

            int remaining = n;
            int failSafe = remaining * 2;

            int v = remaining - 1;
            while(remaining > 2) {
                if(failSafe-- <= 0) {
                    break;
                }

                int u = v;
                if(u >= remaining) {
                    u = 0;
                }
                v = u + 1;
                if(v >= remaining) {
                    v = 0;
                }
                int w = v + 1;
                if(w >= remaining) {
                    w = 0;
                }

                if(IsEar(polygon, indices, u, v, w, remaining)) {
                    triangles.Add(indices[u]);
                    triangles.Add(indices[v]);
                    triangles.Add(indices[w]);
                    indices.RemoveAt(v);
                    remaining--;
                    failSafe = remaining * 2;
                }
            }

            return triangles;
        }

        private static float SignedArea(List<Vector2> polygon) {
            float area = 0f;
            int count = polygon.Count;
            for(int i = 0; i < count; i++) {
                Vector2 a = polygon[i];
                Vector2 b = polygon[(i + 1) % count];
                area += (b.x - a.x) * (b.y + a.y);
            }
            return area;
        }

        private static bool IsEar(List<Vector2> polygon, List<int> indices, int u, int v, int w, int remaining) {
            Vector2 a = polygon[indices[u]];
            Vector2 b = polygon[indices[v]];
            Vector2 c = polygon[indices[w]];

            // Must be convex (counter-clockwise winding after we've ensured CCW order)
            float cross = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
            if(cross <= 0f) {
                return false;
            }

            // No other vertex must be inside this triangle
            for(int p = 0; p < remaining; p++) {
                if(p == u || p == v || p == w) {
                    continue;
                }
                if(PointInTriangle(polygon[indices[p]], a, b, c)) {
                    return false;
                }
            }

            return true;
        }

        private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c) {
            float d1 = (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
            float d2 = (p.x - c.x) * (b.y - c.y) - (b.x - c.x) * (p.y - c.y);
            float d3 = (p.x - a.x) * (c.y - a.y) - (c.x - a.x) * (p.y - a.y);
            bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
            return !(hasNeg && hasPos);
        }
    }
}
