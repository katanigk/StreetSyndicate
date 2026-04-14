using System.Collections.Generic;
using FamilyBusiness.CityGen.Data;
using UnityEngine;

namespace FamilyBusiness.CityGen.Utils
{
    /// <summary>
    /// Plan-space helpers for road segments vs macro polygon and macro features (Batch 3).
    /// </summary>
    public static class RoadGraphGeometry
    {
        public static Vector2 ProjectIntoPolygon(Vector2 p, IReadOnlyList<Vector2> poly, Vector2 centroid, int maxIterations = 14)
        {
            if (poly == null || poly.Count < 3)
                return p;
            Vector2 q = p;
            for (int i = 0; i < maxIterations; i++)
            {
                if (PolygonUtility.PointInPolygon(q, poly))
                    return q;
                q = Vector2.Lerp(q, centroid, 0.2f);
            }

            return centroid;
        }

        public static bool SegmentSamplesInsidePolygon(Vector2 a, Vector2 b, IReadOnlyList<Vector2> poly, int sampleCount)
        {
            if (poly == null || poly.Count < 3)
                return false;
            sampleCount = Mathf.Max(3, sampleCount);
            for (int i = 0; i <= sampleCount; i++)
            {
                float t = i / (float)sampleCount;
                Vector2 p = Vector2.Lerp(a, b, t);
                if (!PolygonUtility.PointInPolygon(p, poly))
                    return false;
            }

            return true;
        }

        /// <summary>Binary-search shorten B toward A until segment samples are inside polygon.</summary>
        public static bool TryShortenEndInsidePolygon(ref Vector2 a, ref Vector2 b, IReadOnlyList<Vector2> poly, int samples)
        {
            if (SegmentSamplesInsidePolygon(a, b, poly, samples))
                return true;
            float lo = 0f, hi = 1f;
            for (int i = 0; i < 12; i++)
            {
                float mid = (lo + hi) * 0.5f;
                Vector2 test = Vector2.Lerp(a, b, mid);
                if (SegmentSamplesInsidePolygon(a, test, poly, samples))
                    lo = mid;
                else
                    hi = mid;
            }

            b = Vector2.Lerp(a, b, lo);
            return SegmentSamplesInsidePolygon(a, b, poly, samples);
        }

        public static float MinDistancePointToPolyline(Vector2 p, IReadOnlyList<Vector2> path)
        {
            if (path == null || path.Count < 2)
                return float.MaxValue;
            float best = float.MaxValue;
            for (int i = 0; i < path.Count - 1; i++)
            {
                float d = DistancePointToSegment(p, path[i], path[i + 1]);
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

        public static float MinDistanceSegmentToPolyline(Vector2 a, Vector2 b, IReadOnlyList<Vector2> path, int samples)
        {
            if (path == null || path.Count < 2)
                return float.MaxValue;
            samples = Mathf.Max(4, samples);
            float best = float.MaxValue;
            for (int i = 0; i <= samples; i++)
            {
                float t = i / (float)samples;
                Vector2 p = Vector2.Lerp(a, b, t);
                float d = MinDistancePointToPolyline(p, path);
                if (d < best)
                    best = d;
            }

            return best;
        }

        public static bool SegmentTooCloseToWater(CityData city, Vector2 a, Vector2 b, float clearanceCells, int samples)
        {
            clearanceCells = Mathf.Max(0.5f, clearanceCells);
            foreach (MacroFeatureData f in city.MacroFeatures)
            {
                if (f.Kind == MacroFeatureKind.RailCorridor)
                    continue;
                float half = f.WidthCells * 0.5f + clearanceCells;
                float d = MinDistanceSegmentToPolyline(a, b, f.Path, samples);
                if (d < half)
                    return true;
            }

            return false;
        }

        public static bool SegmentCrossesRailBuffer(CityData city, Vector2 a, Vector2 b, float halfWidthCells, int samples)
        {
            halfWidthCells = Mathf.Max(0.25f, halfWidthCells);
            foreach (MacroFeatureData f in city.MacroFeatures)
            {
                if (f.Kind != MacroFeatureKind.RailCorridor)
                    continue;
                float d = MinDistanceSegmentToPolyline(a, b, f.Path, samples);
                if (d < halfWidthCells + 0.01f)
                    return true;
            }

            return false;
        }

        public static float SegmentLength(Vector2 a, Vector2 b) => Vector2.Distance(a, b);
    }
}
