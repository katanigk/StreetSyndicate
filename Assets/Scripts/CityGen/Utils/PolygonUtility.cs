using System.Collections.Generic;
using UnityEngine;

namespace FamilyBusiness.CityGen.Utils
{
    public static class PolygonUtility
    {
        /// <summary>Ray-cast parity test; boundary hits count as inside for robustness with grid use.</summary>
        public static bool PointInPolygon(Vector2 p, System.Collections.Generic.IReadOnlyList<Vector2> poly)
        {
            if (poly == null || poly.Count < 3)
                return false;

            bool inside = false;
            int n = poly.Count;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                Vector2 pi = poly[i];
                Vector2 pj = poly[j];
                if (((pi.y > p.y) != (pj.y > p.y)) &&
                    (p.x < (pj.x - pi.x) * (p.y - pi.y) / (pj.y - pi.y + 1e-8f) + pi.x))
                    inside = !inside;
            }

            return inside;
        }

        public static Vector2 ClampToward(Vector2 from, Vector2 to, float t)
        {
            return Vector2.Lerp(from, to, Mathf.Clamp01(t));
        }

        /// <summary>Shortest distance from point to polygon boundary segments (open polyline as closed ring).</summary>
        public static float MinDistanceToPolygonBoundary(Vector2 p, IReadOnlyList<Vector2> poly)
        {
            if (poly == null || poly.Count < 2)
                return float.MaxValue;
            float best = float.MaxValue;
            int n = poly.Count;
            for (int i = 0; i < n; i++)
            {
                Vector2 a = poly[i];
                Vector2 b = poly[(i + 1) % n];
                float d = DistancePointToSegment(p, a, b);
                if (d < best)
                    best = d;
            }

            return best;
        }

        public static float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Vector2.Dot(p - a, ab) / (ab.sqrMagnitude + 1e-8f);
            t = Mathf.Clamp01(t);
            Vector2 proj = a + ab * t;
            return Vector2.Distance(p, proj);
        }

        /// <summary>Monotone chain convex hull; returns vertices CCW without duplicate closing point.</summary>
        public static void BuildConvexHull(IReadOnlyList<Vector2> input, List<Vector2> into)
        {
            into.Clear();
            if (input == null || input.Count < 3)
            {
                if (input != null)
                    into.AddRange(input);
                return;
            }

            var pts = new List<Vector2>(input);
            pts.Sort((a, b) =>
            {
                int c = a.x.CompareTo(b.x);
                return c != 0 ? c : a.y.CompareTo(b.y);
            });

            var lower = new List<Vector2>();
            foreach (Vector2 q in pts)
            {
                while (lower.Count >= 2 && Cross(lower[lower.Count - 2], lower[lower.Count - 1], q) <= 0f)
                    lower.RemoveAt(lower.Count - 1);
                lower.Add(q);
            }

            var upper = new List<Vector2>();
            for (int i = pts.Count - 1; i >= 0; i--)
            {
                Vector2 q = pts[i];
                while (upper.Count >= 2 && Cross(upper[upper.Count - 2], upper[upper.Count - 1], q) <= 0f)
                    upper.RemoveAt(upper.Count - 1);
                upper.Add(q);
            }

            lower.RemoveAt(lower.Count - 1);
            upper.RemoveAt(upper.Count - 1);
            into.AddRange(lower);
            into.AddRange(upper);
        }

        static float Cross(Vector2 o, Vector2 a, Vector2 b) =>
            (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
    }
}
