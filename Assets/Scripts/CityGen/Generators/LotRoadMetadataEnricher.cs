using System.Collections.Generic;
using FamilyBusiness.CityGen.Core;
using FamilyBusiness.CityGen.Data;
using FamilyBusiness.CityGen.Utils;
using UnityEngine;

namespace FamilyBusiness.CityGen.Generators
{
    /// <summary>
    /// Batch 5.5: road adjacency, frontage heuristics, accessibility, and placement flags — geometry unchanged.
    /// </summary>
    public sealed class LotRoadMetadataEnricher
    {
        struct RoadSeg
        {
            public int EdgeId;
            public Vector2 A;
            public Vector2 B;
            public RoadEdgeKind Kind;
        }

        public void Enrich(CityData city, CityGenerationConfig config, CitySeed stageSeed)
        {
            if (city.Lots.Count == 0)
                return;

            float touch = Mathf.Max(0.25f, config.lotRoadTouchDistanceCells);
            float blockEps = Mathf.Max(0.05f, config.lotBlockEdgeEpsilonCells);
            float cityScale = CityDiagonal(city);
            float invScale = 1f / Mathf.Max(1e-4f, cityScale);

            var segs = BuildRoadSegments(city);

            foreach (LotData lot in city.Lots)
            {
                lot.AdjacentRoadEdgeIds.Clear();
                lot.TouchesRoad = false;
                lot.NearestRoadDistance = float.MaxValue;
                lot.FrontageLength = 0f;
                lot.FrontageRoadKind = RoadEdgeKind.Unknown;
                lot.HasMajorRoadFrontage = false;
                lot.FrontageSideIndex = -1;
                lot.TouchesBlockEdge = false;
                lot.SupportsLargeBuilding = false;
                lot.SupportsStreetFacingBuilding = false;
                lot.SupportsBackLotUse = false;
                lot.SizeClass = LotSizeClass.Unknown;
                lot.AccessibilityScore = 0f;

                BlockData block = FindBlock(city, lot.BlockId);
                if (block == null)
                    continue;

                lot.TouchesBlockEdge = LotTouchesBlockEdge(lot, block, blockEps);

                Vector2 center = (lot.Min + lot.Max) * 0.5f;
                var adjacent = new HashSet<int>();

                foreach (RoadSeg s in segs)
                {
                    float dAabb = MinDistSegmentToAabb(s.A, s.B, lot.Min, lot.Max);
                    float dCenter = RoadGraphGeometry.DistancePointToSegment(center, s.A, s.B);
                    if (dCenter < lot.NearestRoadDistance)
                        lot.NearestRoadDistance = dCenter;

                    if (dAabb <= touch)
                    {
                        adjacent.Add(s.EdgeId);
                        lot.TouchesRoad = true;
                    }
                }

                foreach (int id in adjacent)
                    lot.AdjacentRoadEdgeIds.Add(id);
                lot.AdjacentRoadEdgeIds.Sort();

                ComputeFrontage(lot, segs, touch, adjacent);
                ClassifySize(lot, config);
                PlacementFlags(lot, config);
                lot.AccessibilityScore = ComputeAccessibility(lot, city, invScale);
            }

            _ = stageSeed;
        }

        static float CityDiagonal(CityData city)
        {
            Vector2 d = city.Boundary.Max - city.Boundary.Min;
            return Mathf.Sqrt(d.x * d.x + d.y * d.y);
        }

        static List<RoadSeg> BuildRoadSegments(CityData city)
        {
            var list = new List<RoadSeg>();
            foreach (RoadEdge e in city.RoadEdges)
            {
                RoadNode na = FindNode(city, e.FromNodeId);
                RoadNode nb = FindNode(city, e.ToNodeId);
                if (na == null || nb == null)
                    continue;
                list.Add(new RoadSeg
                {
                    EdgeId = e.Id,
                    A = na.Position,
                    B = nb.Position,
                    Kind = e.Kind
                });
            }

            return list;
        }

        static RoadNode FindNode(CityData city, int id)
        {
            for (int i = 0; i < city.RoadNodes.Count; i++)
            {
                if (city.RoadNodes[i].Id == id)
                    return city.RoadNodes[i];
            }

            return null;
        }

        static BlockData FindBlock(CityData city, int blockId)
        {
            for (int i = 0; i < city.Blocks.Count; i++)
            {
                if (city.Blocks[i].Id == blockId)
                    return city.Blocks[i];
            }

            return null;
        }

        static DistrictData FindDistrict(CityData city, int districtId)
        {
            for (int i = 0; i < city.Districts.Count; i++)
            {
                if (city.Districts[i].Id == districtId)
                    return city.Districts[i];
            }

            return null;
        }

        static bool LotTouchesBlockEdge(LotData lot, BlockData block, float eps)
        {
            return Mathf.Abs(lot.Min.x - block.Min.x) <= eps
                   || Mathf.Abs(lot.Max.x - block.Max.x) <= eps
                   || Mathf.Abs(lot.Min.y - block.Min.y) <= eps
                   || Mathf.Abs(lot.Max.y - block.Max.y) <= eps;
        }

        static float MinDistSegmentToAabb(Vector2 a, Vector2 b, Vector2 mn, Vector2 mx)
        {
            if (SegmentIntersectsAabb(a, b, mn, mx))
                return 0f;
            float best = float.MaxValue;
            for (int i = 0; i <= 12; i++)
            {
                float t = i / 12f;
                Vector2 p = Vector2.Lerp(a, b, t);
                best = Mathf.Min(best, DistPointToAabb(p, mn, mx));
            }

            Vector2 c0 = new Vector2(mn.x, mn.y);
            Vector2 c1 = new Vector2(mx.x, mn.y);
            Vector2 c2 = new Vector2(mx.x, mx.y);
            Vector2 c3 = new Vector2(mn.x, mx.y);
            best = Mathf.Min(best, RoadGraphGeometry.DistancePointToSegment(c0, a, b));
            best = Mathf.Min(best, RoadGraphGeometry.DistancePointToSegment(c1, a, b));
            best = Mathf.Min(best, RoadGraphGeometry.DistancePointToSegment(c2, a, b));
            best = Mathf.Min(best, RoadGraphGeometry.DistancePointToSegment(c3, a, b));
            return best;
        }

        static float DistPointToAabb(Vector2 p, Vector2 mn, Vector2 mx)
        {
            float cx = Mathf.Clamp(p.x, mn.x, mx.x);
            float cy = Mathf.Clamp(p.y, mn.y, mx.y);
            return Vector2.Distance(p, new Vector2(cx, cy));
        }

        static bool SegmentIntersectsAabb(Vector2 p0, Vector2 p1, Vector2 mn, Vector2 mx)
        {
            if (PointInAabb(p0, mn, mx) || PointInAabb(p1, mn, mx))
                return true;
            Vector2 a = new Vector2(mn.x, mn.y);
            Vector2 b = new Vector2(mx.x, mn.y);
            Vector2 c = new Vector2(mx.x, mx.y);
            Vector2 d = new Vector2(mn.x, mx.y);
            return SegmentsCross(p0, p1, a, b) || SegmentsCross(p0, p1, b, c) || SegmentsCross(p0, p1, c, d) ||
                   SegmentsCross(p0, p1, d, a);
        }

        static bool PointInAabb(Vector2 p, Vector2 mn, Vector2 mx) =>
            p.x >= mn.x && p.x <= mx.x && p.y >= mn.y && p.y <= mx.y;

        static bool SegmentsCross(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            float o1 = Orient(a, b, c);
            float o2 = Orient(a, b, d);
            float o3 = Orient(c, d, a);
            float o4 = Orient(c, d, b);
            if (o1 * o2 < 0f && o3 * o4 < 0f)
                return true;
            return Mathf.Abs(o1) < 1e-6f && OnSegment(a, b, c)
                   || Mathf.Abs(o2) < 1e-6f && OnSegment(a, b, d)
                   || Mathf.Abs(o3) < 1e-6f && OnSegment(c, d, a)
                   || Mathf.Abs(o4) < 1e-6f && OnSegment(c, d, b);
        }

        static float Orient(Vector2 a, Vector2 b, Vector2 c) =>
            (b.y - a.y) * (c.x - b.x) - (b.x - a.x) * (c.y - b.y);

        static bool OnSegment(Vector2 a, Vector2 b, Vector2 p) =>
            p.x <= Mathf.Max(a.x, b.x) + 1e-5f && p.x >= Mathf.Min(a.x, b.x) - 1e-5f
                                                && p.y <= Mathf.Max(a.y, b.y) + 1e-5f &&
                                                p.y >= Mathf.Min(a.y, b.y) - 1e-5f;

        static float MinDistSegmentPair(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2) =>
            Mathf.Min(
                Mathf.Min(RoadGraphGeometry.DistancePointToSegment(a1, b1, b2),
                    RoadGraphGeometry.DistancePointToSegment(a2, b1, b2)),
                Mathf.Min(RoadGraphGeometry.DistancePointToSegment(b1, a1, a2),
                    RoadGraphGeometry.DistancePointToSegment(b2, a1, a2)));

        static void GetLotSide(int side, LotData lot, out Vector2 s0, out Vector2 s1)
        {
            Vector2 mn = lot.Min;
            Vector2 mx = lot.Max;
            switch (side)
            {
                case 0:
                    s0 = new Vector2(mn.x, mn.y);
                    s1 = new Vector2(mx.x, mn.y);
                    break;
                case 1:
                    s0 = new Vector2(mx.x, mn.y);
                    s1 = new Vector2(mx.x, mx.y);
                    break;
                case 2:
                    s0 = new Vector2(mn.x, mx.y);
                    s1 = new Vector2(mx.x, mx.y);
                    break;
                default:
                    s0 = new Vector2(mn.x, mx.y);
                    s1 = new Vector2(mn.x, mn.y);
                    break;
            }
        }

        static float SideLength(int side, LotData lot)
        {
            float w = lot.Max.x - lot.Min.x;
            float h = lot.Max.y - lot.Min.y;
            return (side == 0 || side == 2) ? w : h;
        }

        static void ComputeFrontage(LotData lot, List<RoadSeg> segs, float touch, HashSet<int> adjacent)
        {
            var sideBest = new[] { float.MaxValue, float.MaxValue, float.MaxValue, float.MaxValue };
            var sideKind = new[]
            {
                RoadEdgeKind.Unknown, RoadEdgeKind.Unknown, RoadEdgeKind.Unknown, RoadEdgeKind.Unknown
            };
            var sideMajor = new[] { false, false, false, false };

            foreach (RoadSeg s in segs)
            {
                for (int side = 0; side < 4; side++)
                {
                    GetLotSide(side, lot, out Vector2 ls0, out Vector2 ls1);
                    float d = MinDistSegmentPair(ls0, ls1, s.A, s.B);
                    if (d > touch * 1.35f)
                        continue;
                    if (d < sideBest[side])
                    {
                        sideBest[side] = d;
                        sideKind[side] = s.Kind;
                    }

                    if (d <= touch && s.Kind == RoadEdgeKind.Major)
                        sideMajor[side] = true;
                }
            }

            int best = -1;
            float bestD = float.MaxValue;
            for (int side = 0; side < 4; side++)
            {
                if (sideBest[side] >= float.MaxValue - 1f)
                    continue;
                if (sideBest[side] < bestD)
                {
                    bestD = sideBest[side];
                    best = side;
                }
            }

            if (best < 0 && adjacent.Count > 0)
            {
                float bestMid = float.MaxValue;
                int bestSide = -1;
                foreach (int eid in lot.AdjacentRoadEdgeIds)
                {
                    if (!TryGetSeg(segs, eid, out RoadSeg s))
                        continue;
                    Vector2 mid = (s.A + s.B) * 0.5f;
                    for (int side = 0; side < 4; side++)
                    {
                        GetLotSide(side, lot, out Vector2 ls0, out Vector2 ls1);
                        float dm = RoadGraphGeometry.DistancePointToSegment(mid, ls0, ls1);
                        if (dm < bestMid)
                        {
                            bestMid = dm;
                            bestSide = side;
                        }
                    }
                }

                best = bestSide;
                if (best >= 0)
                {
                    foreach (int eid in lot.AdjacentRoadEdgeIds)
                    {
                        if (!TryGetSeg(segs, eid, out RoadSeg s))
                            continue;
                        GetLotSide(best, lot, out Vector2 ls0, out Vector2 ls1);
                        if (MinDistSegmentPair(ls0, ls1, s.A, s.B) > touch * 1.35f)
                            continue;
                        sideKind[best] = StrongerKind(sideKind[best], s.Kind);
                        if (s.Kind == RoadEdgeKind.Major)
                            sideMajor[best] = true;
                    }
                }
            }

            if (best < 0)
                return;

            lot.FrontageSideIndex = best;
            lot.FrontageLength = SideLength(best, lot);
            lot.FrontageRoadKind = sideKind[best];
            lot.HasMajorRoadFrontage = sideMajor[best];

            foreach (int eid in lot.AdjacentRoadEdgeIds)
            {
                if (!TryGetSeg(segs, eid, out RoadSeg s))
                    continue;
                GetLotSide(best, lot, out Vector2 ls0, out Vector2 ls1);
                if (MinDistSegmentPair(ls0, ls1, s.A, s.B) <= touch * 1.2f)
                    lot.FrontageRoadKind = StrongerKind(lot.FrontageRoadKind, s.Kind);
                if (s.Kind == RoadEdgeKind.Major &&
                    MinDistSegmentPair(ls0, ls1, s.A, s.B) <= touch * 1.2f)
                    lot.HasMajorRoadFrontage = true;
            }
        }

        static bool TryGetSeg(List<RoadSeg> segs, int edgeId, out RoadSeg seg)
        {
            for (int i = 0; i < segs.Count; i++)
            {
                if (segs[i].EdgeId == edgeId)
                {
                    seg = segs[i];
                    return true;
                }
            }

            seg = default;
            return false;
        }

        static RoadEdgeKind StrongerKind(RoadEdgeKind a, RoadEdgeKind b)
        {
            int pa = KindPri(a);
            int pb = KindPri(b);
            return pb > pa ? b : a;
        }

        static int KindPri(RoadEdgeKind k)
        {
            switch (k)
            {
                case RoadEdgeKind.Major: return 3;
                case RoadEdgeKind.Secondary: return 2;
                case RoadEdgeKind.Alley: return 1;
                default: return 0;
            }
        }

        static void ClassifySize(LotData lot, CityGenerationConfig config)
        {
            float a = lot.AreaCells;
            if (a <= 0f)
            {
                lot.SizeClass = LotSizeClass.Unknown;
                return;
            }

            if (a <= config.lotSizeClassSmallMaxAreaCells)
                lot.SizeClass = LotSizeClass.Small;
            else if (a <= config.lotSizeClassMediumMaxAreaCells)
                lot.SizeClass = LotSizeClass.Medium;
            else if (a <= config.lotSizeClassLargeMaxAreaCells)
                lot.SizeClass = LotSizeClass.Large;
            else
                lot.SizeClass = LotSizeClass.Oversize;
        }

        static void PlacementFlags(LotData lot, CityGenerationConfig config)
        {
            float w = lot.Max.x - lot.Min.x;
            float h = lot.Max.y - lot.Min.y;
            float depth = Mathf.Max(w, h);
            float streetMin = Mathf.Max(0.5f, config.lotMinStreetFrontageCells);
            float largeMin = Mathf.Max(1f, config.lotLargeBuildingMinAreaCells);
            float backMinA = Mathf.Max(1f, config.lotBackUseMinAreaCells);
            float backMinD = Mathf.Max(1f, config.lotBackUseMinDepthCells);

            lot.SupportsLargeBuilding = lot.AreaCells >= largeMin;
            lot.SupportsStreetFacingBuilding = lot.TouchesRoad && lot.FrontageSideIndex >= 0 &&
                                               lot.FrontageLength >= streetMin;
            lot.SupportsBackLotUse = lot.AreaCells >= backMinA &&
                                     (!lot.SupportsStreetFacingBuilding ||
                                      depth >= backMinD + streetMin * 0.5f);
        }

        static float ComputeAccessibility(LotData lot, CityData city, float invScale)
        {
            float s = 0f;
            if (lot.TouchesRoad)
                s += 0.38f;
            if (lot.HasMajorRoadFrontage)
                s += 0.28f;
            else if (TouchesSecondaryAdjacent(city, lot))
                s += 0.14f;

            float near = 1f - Mathf.Clamp01(lot.NearestRoadDistance * invScale * 2.2f);
            if (!lot.TouchesRoad)
                s += near * 0.12f;

            DistrictData d = FindDistrict(city, lot.DistrictId);
            if (d != null)
            {
                Vector2 c = (lot.Min + lot.Max) * 0.5f;
                float dc = Vector2.Distance(c, d.CenterPosition) * invScale;
                s += Mathf.Clamp01(1f - dc * 0.85f) * 0.12f;
            }

            if (lot.TouchesBlockEdge)
                s += 0.08f;

            return Mathf.Clamp01(s);
        }

        static bool TouchesSecondaryAdjacent(CityData city, LotData lot)
        {
            for (int i = 0; i < lot.AdjacentRoadEdgeIds.Count; i++)
            {
                RoadEdge e = FindEdge(city, lot.AdjacentRoadEdgeIds[i]);
                if (e != null && e.Kind == RoadEdgeKind.Secondary)
                    return true;
            }

            return false;
        }

        static RoadEdge FindEdge(CityData city, int id)
        {
            for (int i = 0; i < city.RoadEdges.Count; i++)
            {
                if (city.RoadEdges[i].Id == id)
                    return city.RoadEdges[i];
            }

            return null;
        }
    }
}
