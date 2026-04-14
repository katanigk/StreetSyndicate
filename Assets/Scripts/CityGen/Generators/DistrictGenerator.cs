using System.Collections.Generic;
using FamilyBusiness.CityGen.Core;
using FamilyBusiness.CityGen.Data;
using FamilyBusiness.CityGen.Utils;
using UnityEngine;

namespace FamilyBusiness.CityGen.Generators
{
    /// <summary>
    /// Batch 4: anchor- and geography-informed districts owning coarse blocks, with gameplay metadata.
    /// </summary>
    public sealed class DistrictGenerator
    {
        struct Seed
        {
            public Vector2 Pos;
            public DistrictKind Kind;
            public int Priority;
        }

        struct BlockFeat
        {
            public Vector2 Center;
            public float EdgeT;
            public float RailT;
            public float WaterT;
            public float IndustrialT;
            public float DowntownT;
            public float RoadT;
        }

        public void Generate(CityData city, CityGenerationConfig config, CitySeed stageSeed)
        {
            city.Districts.Clear();
            if (city.Blocks.Count == 0)
            {
                AddSingletonDistrict(city, config);
                return;
            }

            var rng = stageSeed.Fork("districts").CreateSystemRandom();
            IReadOnlyList<Vector2> poly = EffectivePoly(city);
            Vector2 cityCenter = ComputeCityCenter(city, poly);
            float cityScale = CityScale(city);
            float invScale = 1f / Mathf.Max(1e-4f, cityScale);

            Vector2 downtownAnchor = FindDowntownAnchor(city, cityCenter);
            bool hasWater = HasRiverOrCoast(city);
            int target = rng.Next(config.districtCountMin, config.districtCountMax + 1);
            target = Mathf.Clamp(target, 1, 64);
            int maxDistrictsByBlocks = Mathf.Max(1, city.Blocks.Count / Mathf.Max(1, config.minBlocksPerDistrict));
            target = Mathf.Min(target, maxDistrictsByBlocks);
            target = Mathf.Min(target, city.Blocks.Count);
            target = Mathf.Max(target, 1);

            var seeds = BuildSeeds(city, config, rng, poly, cityCenter, downtownAnchor, hasWater);
            EnsureDowntownSeed(seeds, downtownAnchor, poly, cityCenter);
            MergeSeeds(seeds, config.districtSeedMergeDistanceCells);
            TrimSeedsToTarget(seeds, target, rng);
            ExpandSeedsIfNeeded(seeds, target, rng, poly, city.Boundary, cityCenter);

            var districts = new List<DistrictData>();
            for (int i = 0; i < seeds.Count; i++)
            {
                Seed s = seeds[i];
                districts.Add(new DistrictData
                {
                    Id = i,
                    Kind = s.Kind,
                    CenterPosition = s.Pos,
                    Name = "District"
                });
            }

            var feats = new BlockFeat[city.Blocks.Count];
            PrecomputeBlockFeatures(city, poly, downtownAnchor, cityScale, feats);

            var assignment = new int[city.Blocks.Count];
            for (int it = 0; it < config.districtLloydIterations; it++)
            {
                AssignBlocksToDistricts(city, districts, config, feats, invScale, downtownAnchor, assignment);
                UpdateCenters(city, districts, assignment, downtownAnchor);
            }

            AssignBlocksToDistricts(city, districts, config, feats, invScale, downtownAnchor, assignment);
            EnforceMinBlocks(city, districts, config, assignment, invScale, downtownAnchor, feats);
            FinalizeBlockOwnership(city, districts, assignment);
            RenumberDistrictsAndBlocks(city, districts);
            foreach (DistrictData d in city.Districts)
            {
                ComputeMetadata(d, stageSeed.Fork("district_meta_" + d.Id));
                d.Name = BuildDistrictName(d, cityCenter);
                BuildOutlineFromBlocks(city, d);
            }
        }

        static void AddSingletonDistrict(CityData city, CityGenerationConfig config)
        {
            var d = new DistrictData
            {
                Id = 0,
                Name = config.cityDisplayName,
                Kind = DistrictKind.Residential,
                CenterPosition = ComputeCityCenter(city, EffectivePoly(city))
            };
            if (city.MacroBoundary.Vertices.Count >= 3)
            {
                foreach (Vector2 v in city.MacroBoundary.Vertices)
                    d.Outline.Add(v);
            }
            else
            {
                Vector2 mn = city.Boundary.Min, mx = city.Boundary.Max;
                d.Outline.Add(new Vector2(mn.x, mn.y));
                d.Outline.Add(new Vector2(mx.x, mn.y));
                d.Outline.Add(new Vector2(mx.x, mx.y));
                d.Outline.Add(new Vector2(mn.x, mx.y));
            }

            if (city.Blocks.Count > 0)
            {
                city.Blocks[0].DistrictId = 0;
                city.Blocks[0].DistrictKind = d.Kind;
                d.BlockIds.Add(city.Blocks[0].Id);
            }

            ComputeMetadata(d, CitySeed.FromExplicit(city.Seed).Fork("district_single"));
            city.Districts.Add(d);
        }

        static IReadOnlyList<Vector2> EffectivePoly(CityData city) =>
            city.MacroBoundary.Vertices.Count >= 3 ? city.MacroBoundary.Vertices : null;

        static Vector2 ComputeCityCenter(CityData city, IReadOnlyList<Vector2> poly)
        {
            if (poly != null && poly.Count >= 3)
            {
                Vector2 s = Vector2.zero;
                for (int i = 0; i < poly.Count; i++)
                    s += poly[i];
                return s / poly.Count;
            }

            return (city.Boundary.Min + city.Boundary.Max) * 0.5f;
        }

        static float CityScale(CityData city)
        {
            Vector2 d = city.Boundary.Max - city.Boundary.Min;
            return Mathf.Sqrt(d.x * d.x + d.y * d.y);
        }

        static Vector2 FindDowntownAnchor(CityData city, Vector2 fallback)
        {
            foreach (MacroAnchorPointData a in city.MacroAnchors)
            {
                if (a.Kind == MacroAnchorKind.DowntownCore)
                    return a.Position;
            }

            return fallback;
        }

        static bool HasRiverOrCoast(CityData city)
        {
            foreach (MacroFeatureData f in city.MacroFeatures)
            {
                if (f.Kind == MacroFeatureKind.River || f.Kind == MacroFeatureKind.Coastline)
                    return true;
            }

            return false;
        }

        static List<Seed> BuildSeeds(CityData city, CityGenerationConfig config, System.Random rng,
            IReadOnlyList<Vector2> poly, Vector2 cityCenter, Vector2 downtownAnchor, bool hasWater)
        {
            var list = new List<Seed>();

            foreach (MacroAnchorPointData a in city.MacroAnchors)
            {
                Vector2 p = poly != null
                    ? RoadGraphGeometry.ProjectIntoPolygon(a.Position, poly, cityCenter)
                    : a.Position;
                DistrictKind k = AnchorToDistrictKind(a.Kind, hasWater);
                list.Add(new Seed { Pos = p, Kind = k, Priority = KindPriority(k) });
            }

            if (poly != null && poly.Count >= 3)
            {
                int fringeCount = Mathf.Clamp(1 + rng.Next(0, 3), 1, 4);
                for (int i = 0; i < fringeCount; i++)
                {
                    float t = (float)((i + rng.NextDouble()) / fringeCount);
                    Vector2 onEdge = PointOnPolygonRing(poly, t);
                    Vector2 inward = (cityCenter - onEdge).normalized;
                    Vector2 p = onEdge + inward * (config.districtSeedMergeDistanceCells * 0.65f);
                    p = RoadGraphGeometry.ProjectIntoPolygon(p, poly, cityCenter);
                    list.Add(new Seed
                    {
                        Pos = p,
                        Kind = DistrictKind.FringeOuterEdge,
                        Priority = KindPriority(DistrictKind.FringeOuterEdge)
                    });
                }
            }

            list.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            return list;
        }

        static void EnsureDowntownSeed(List<Seed> seeds, Vector2 downtownAnchor, IReadOnlyList<Vector2> poly, Vector2 cityCenter)
        {
            for (int i = 0; i < seeds.Count; i++)
            {
                if (seeds[i].Kind == DistrictKind.DowntownCommercial)
                    return;
            }

            Vector2 p = poly != null
                ? RoadGraphGeometry.ProjectIntoPolygon(downtownAnchor, poly, cityCenter)
                : downtownAnchor;
            seeds.Insert(0, new Seed
            {
                Pos = p,
                Kind = DistrictKind.DowntownCommercial,
                Priority = KindPriority(DistrictKind.DowntownCommercial)
            });
        }

        static DistrictKind AnchorToDistrictKind(MacroAnchorKind a, bool hasWater)
        {
            switch (a)
            {
                case MacroAnchorKind.DowntownCore: return DistrictKind.DowntownCommercial;
                case MacroAnchorKind.Industrial: return DistrictKind.Industrial;
                case MacroAnchorKind.Docks: return hasWater ? DistrictKind.DocksPort : DistrictKind.Industrial;
                case MacroAnchorKind.WealthyResidential: return DistrictKind.Wealthy;
                case MacroAnchorKind.ResidentialSpread: return DistrictKind.Residential;
                case MacroAnchorKind.RailYard: return DistrictKind.WorkingClass;
                default: return DistrictKind.Residential;
            }
        }

        static int KindPriority(DistrictKind k)
        {
            switch (k)
            {
                case DistrictKind.DowntownCommercial: return 100;
                case DistrictKind.DocksPort: return 92;
                case DistrictKind.Industrial: return 88;
                case DistrictKind.Wealthy: return 72;
                case DistrictKind.WorkingClass: return 65;
                case DistrictKind.Residential: return 58;
                case DistrictKind.FringeOuterEdge: return 45;
                default: return 40;
            }
        }

        static Vector2 PointOnPolygonRing(IReadOnlyList<Vector2> poly, float t01)
        {
            t01 = Mathf.Repeat(t01, 1f);
            int n = poly.Count;
            float total = 0f;
            var segLens = new float[n];
            for (int i = 0; i < n; i++)
            {
                Vector2 a = poly[i];
                Vector2 b = poly[(i + 1) % n];
                segLens[i] = Vector2.Distance(a, b);
                total += segLens[i];
            }

            if (total < 1e-4f)
                return poly[0];
            float u = t01 * total;
            for (int i = 0; i < n; i++)
            {
                if (u <= segLens[i] || i == n - 1)
                {
                    float lt = segLens[i] > 1e-4f ? u / segLens[i] : 0f;
                    return Vector2.Lerp(poly[i], poly[(i + 1) % n], Mathf.Clamp01(lt));
                }

                u -= segLens[i];
            }

            return poly[0];
        }

        static void MergeSeeds(List<Seed> seeds, float mergeDist)
        {
            mergeDist = Mathf.Max(4f, mergeDist);
            var merged = new List<Seed>();
            foreach (Seed s in seeds)
            {
                bool absorbed = false;
                for (int i = 0; i < merged.Count; i++)
                {
                    if (Vector2.Distance(merged[i].Pos, s.Pos) > mergeDist)
                        continue;
                    Seed m = merged[i];
                    if (s.Priority > m.Priority)
                    {
                        m.Kind = s.Kind;
                        m.Priority = s.Priority;
                    }

                    m.Pos = (m.Pos + s.Pos) * 0.5f;
                    merged[i] = m;
                    absorbed = true;
                    break;
                }

                if (!absorbed)
                    merged.Add(s);
            }

            seeds.Clear();
            seeds.AddRange(merged);
            seeds.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }

        static void TrimSeedsToTarget(List<Seed> seeds, int target, System.Random rng)
        {
            _ = rng;
            while (seeds.Count > target)
            {
                int drop = -1;
                int lowPri = int.MaxValue;
                for (int i = 0; i < seeds.Count; i++)
                {
                    if (seeds[i].Kind == DistrictKind.DowntownCommercial)
                        continue;
                    if (seeds[i].Priority < lowPri)
                    {
                        lowPri = seeds[i].Priority;
                        drop = i;
                    }
                }

                if (drop < 0)
                    seeds.RemoveAt(seeds.Count - 1);
                else
                    seeds.RemoveAt(drop);
            }
        }

        static void ExpandSeedsIfNeeded(List<Seed> seeds, int target, System.Random rng,
            IReadOnlyList<Vector2> poly, CityBoundary bounds, Vector2 cityCenter)
        {
            int guard = 0;
            while (seeds.Count < target && guard++ < 64)
            {
                Vector2 p = SampleInteriorPoint(rng, poly, bounds, cityCenter);
                bool dup = false;
                for (int i = 0; i < seeds.Count; i++)
                {
                    if (Vector2.Distance(seeds[i].Pos, p) < 8f)
                    {
                        dup = true;
                        break;
                    }
                }

                if (dup)
                    continue;
                DistrictKind k = (seeds.Count & 1) == 0 ? DistrictKind.Residential : DistrictKind.FringeOuterEdge;
                seeds.Add(new Seed { Pos = p, Kind = k, Priority = KindPriority(k) });
            }
        }

        static Vector2 SampleInteriorPoint(System.Random rng, IReadOnlyList<Vector2> poly, CityBoundary bounds, Vector2 cityCenter)
        {
            for (int t = 0; t < 48; t++)
            {
                float x = Mathf.Lerp(bounds.Min.x, bounds.Max.x, (float)rng.NextDouble());
                float y = Mathf.Lerp(bounds.Min.y, bounds.Max.y, (float)rng.NextDouble());
                Vector2 p = new Vector2(x, y);
                if (poly != null && poly.Count >= 3)
                {
                    if (!PolygonUtility.PointInPolygon(p, poly))
                        continue;
                    p = RoadGraphGeometry.ProjectIntoPolygon(p, poly, cityCenter);
                }

                return p;
            }

            return cityCenter;
        }

        static void PrecomputeBlockFeatures(CityData city, IReadOnlyList<Vector2> poly, Vector2 downtownAnchor,
            float cityScale, BlockFeat[] into)
        {
            float scale = Mathf.Max(1e-4f, cityScale);
            for (int bi = 0; bi < city.Blocks.Count; bi++)
            {
                BlockData b = city.Blocks[bi];
                Vector2 c = (b.Min + b.Max) * 0.5f;
                float bd = poly != null
                    ? PolygonUtility.MinDistanceToPolygonBoundary(c, poly)
                    : EdgeDistanceToAabb(c, city.Boundary);
                float edgeT = Mathf.Clamp01(1f - bd / (scale * 0.22f));
                float railD = MinDistToFeatureKind(city, c, MacroFeatureKind.RailCorridor);
                float waterD = MinDistToWaterFeatures(city, c);
                float indD = MinDistToAnchorKinds(city, c,
                    MacroAnchorKind.Industrial, MacroAnchorKind.RailYard);
                float dt = Vector2.Distance(c, downtownAnchor) / scale;
                float roadD = MinDistToRoadNetwork(city, c);
                float roadT = 1f / (1f + 5f * roadD / scale);

                into[bi] = new BlockFeat
                {
                    Center = c,
                    EdgeT = edgeT,
                    RailT = Mathf.Clamp01(1f - railD / (scale * 0.18f)),
                    WaterT = Mathf.Clamp01(1f - waterD / (scale * 0.25f)),
                    IndustrialT = Mathf.Clamp01(1f - indD / (scale * 0.2f)),
                    DowntownT = dt,
                    RoadT = roadT
                };
            }
        }

        static float EdgeDistanceToAabb(Vector2 c, CityBoundary bounds)
        {
            float dx = Mathf.Min(c.x - bounds.Min.x, bounds.Max.x - c.x);
            float dy = Mathf.Min(c.y - bounds.Min.y, bounds.Max.y - c.y);
            return Mathf.Max(0f, Mathf.Min(dx, dy));
        }

        static float MinDistToFeatureKind(CityData city, Vector2 p, MacroFeatureKind kind)
        {
            float best = float.MaxValue;
            foreach (MacroFeatureData f in city.MacroFeatures)
            {
                if (f.Kind != kind || f.Path == null || f.Path.Count < 2)
                    continue;
                float d = RoadGraphGeometry.MinDistancePointToPolyline(p, f.Path);
                if (d < best)
                    best = d;
            }

            return best >= float.MaxValue - 1f ? city.Boundary.Max.x : best;
        }

        static float MinDistToWaterFeatures(CityData city, Vector2 p)
        {
            float best = float.MaxValue;
            foreach (MacroFeatureData f in city.MacroFeatures)
            {
                if (f.Kind != MacroFeatureKind.River && f.Kind != MacroFeatureKind.Coastline)
                    continue;
                if (f.Path == null || f.Path.Count < 2)
                    continue;
                float d = RoadGraphGeometry.MinDistancePointToPolyline(p, f.Path);
                if (d < best)
                    best = d;
            }

            return best >= float.MaxValue - 1f ? city.Boundary.Max.x : best;
        }

        static float MinDistToAnchorKinds(CityData city, Vector2 p, MacroAnchorKind a, MacroAnchorKind b)
        {
            float best = float.MaxValue;
            foreach (MacroAnchorPointData anchor in city.MacroAnchors)
            {
                if (anchor.Kind != a && anchor.Kind != b)
                    continue;
                float d = Vector2.Distance(p, anchor.Position);
                if (d < best)
                    best = d;
            }

            return best >= float.MaxValue - 1f ? city.Boundary.Max.x : best;
        }

        static float MinDistToRoadNetwork(CityData city, Vector2 p)
        {
            float best = float.MaxValue;
            foreach (RoadEdge e in city.RoadEdges)
            {
                RoadNode na = FindNode(city, e.FromNodeId);
                RoadNode nb = FindNode(city, e.ToNodeId);
                if (na == null || nb == null)
                    continue;
                float d = RoadGraphGeometry.DistancePointToSegment(p, na.Position, nb.Position);
                if (d < best)
                    best = d;
            }

            foreach (RoadNode n in city.RoadNodes)
            {
                float d = Vector2.Distance(p, n.Position);
                if (d < best)
                    best = d;
            }

            return best >= float.MaxValue - 1f ? city.Boundary.Max.x : best;
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

        static void AssignBlocksToDistricts(CityData city, List<DistrictData> districts, CityGenerationConfig config,
            BlockFeat[] feats, float invScale, Vector2 downtownAnchor, int[] assignment)
        {
            for (int bi = 0; bi < city.Blocks.Count; bi++)
            {
                BlockFeat f = feats[bi];
                int best = 0;
                float bestScore = float.MinValue;
                for (int di = 0; di < districts.Count; di++)
                {
                    float s = ScoreBlockForDistrict(districts[di].Kind, districts[di].CenterPosition, f, invScale,
                        config, downtownAnchor);
                    if (s > bestScore)
                    {
                        bestScore = s;
                        best = di;
                    }
                }

                assignment[bi] = best;
            }
        }

        static float ScoreBlockForDistrict(DistrictKind kind, Vector2 dCenter, BlockFeat f, float invScale,
            CityGenerationConfig config, Vector2 downtownAnchor)
        {
            float dc = Vector2.Distance(f.Center, dCenter) * invScale;
            float baseScore = -dc * dc * 22f;

            float dt = f.DowntownT;
            float wD = config.downtownCentralityWeight;
            float wR = config.industrialRailAffinity;
            float wWe = config.wealthyEdgeAversion;
            float wFr = config.fringeBoundaryAffinity;
            float wDock = config.docksWaterAffinity;

            switch (kind)
            {
                case DistrictKind.DowntownCommercial:
                    return baseScore - dt * 14f * wD + f.RoadT * 11f - f.EdgeT * 3f;
                case DistrictKind.Industrial:
                    return baseScore + f.RailT * 24f * wR + f.EdgeT * 6f - f.WaterT * 4f + (1f - f.IndustrialT) * 10f;
                case DistrictKind.DocksPort:
                    return baseScore + f.WaterT * 38f * wDock + f.RoadT * 8f + f.EdgeT * 5f;
                case DistrictKind.Wealthy:
                    return baseScore - f.IndustrialT * 28f - f.EdgeT * 18f * wWe - f.RailT * 10f + f.RoadT * 7f
                           - dt * 4f * wD;
                case DistrictKind.WorkingClass:
                    return baseScore + f.IndustrialT * 20f + f.RailT * 14f * wR + f.RoadT * 5f - f.EdgeT * 4f;
                case DistrictKind.Residential:
                    return baseScore - f.IndustrialT * 14f - Mathf.Abs(f.EdgeT - 0.42f) * 8f + f.RoadT * 9f
                           - dt * 6f;
                case DistrictKind.FringeOuterEdge:
                    return baseScore + f.EdgeT * 26f * wFr - f.RoadT * 7f - dt * 3f;
                default:
                    return baseScore;
            }
        }

        static void UpdateCenters(CityData city, List<DistrictData> districts, int[] assignment, Vector2 downtownAnchor)
        {
            var sum = new Vector2[districts.Count];
            var count = new int[districts.Count];
            for (int bi = 0; bi < city.Blocks.Count; bi++)
            {
                int d = assignment[bi];
                Vector2 c = (city.Blocks[bi].Min + city.Blocks[bi].Max) * 0.5f;
                sum[d] += c;
                count[d]++;
            }

            for (int i = 0; i < districts.Count; i++)
            {
                if (count[i] <= 0)
                    continue;
                Vector2 cen = sum[i] / count[i];
                if (districts[i].Kind == DistrictKind.DowntownCommercial)
                    cen = Vector2.Lerp(cen, downtownAnchor, 0.4f);
                districts[i].CenterPosition = cen;
            }
        }

        static void EnforceMinBlocks(CityData city, List<DistrictData> districts, CityGenerationConfig config,
            int[] assignment, float invScale, Vector2 downtownAnchor, BlockFeat[] feats)
        {
            for (int round = 0; round < 12; round++)
            {
                var counts = new int[districts.Count];
                for (int bi = 0; bi < assignment.Length; bi++)
                    counts[assignment[bi]]++;

                int small = -1;
                for (int i = 0; i < counts.Length; i++)
                {
                    if (counts[i] <= 0 || counts[i] >= config.minBlocksPerDistrict)
                        continue;
                    if (districts[i].Kind == DistrictKind.DowntownCommercial)
                        continue;
                    small = i;
                    break;
                }

                if (small < 0)
                    return;

                Vector2 smallCen = districts[small].CenterPosition;
                int bestTarget = -1;
                float bestD = float.MaxValue;
                for (int j = 0; j < districts.Count; j++)
                {
                    if (j == small)
                        continue;
                    float d = Vector2.Distance(districts[j].CenterPosition, smallCen);
                    if (d < bestD)
                    {
                        bestD = d;
                        bestTarget = j;
                    }
                }

                if (bestTarget < 0)
                    return;

                for (int bi = 0; bi < assignment.Length; bi++)
                {
                    if (assignment[bi] != small)
                        continue;
                    float bestScore = float.MinValue;
                    int pick = bestTarget;
                    for (int di = 0; di < districts.Count; di++)
                    {
                        if (di == small)
                            continue;
                        float s = ScoreBlockForDistrict(districts[di].Kind, districts[di].CenterPosition, feats[bi],
                            invScale, config, downtownAnchor);
                        if (s > bestScore)
                        {
                            bestScore = s;
                            pick = di;
                        }
                    }

                    assignment[bi] = pick;
                }

                UpdateCenters(city, districts, assignment, downtownAnchor);
            }
        }

        static void FinalizeBlockOwnership(CityData city, List<DistrictData> districts, int[] assignment)
        {
            foreach (DistrictData d in districts)
            {
                d.BlockIds.Clear();
            }

            for (int bi = 0; bi < city.Blocks.Count; bi++)
            {
                int di = assignment[bi];
                BlockData b = city.Blocks[bi];
                b.DistrictId = districts[di].Id;
                b.DistrictKind = districts[di].Kind;
                districts[di].BlockIds.Add(b.Id);
            }
        }

        static void RenumberDistrictsAndBlocks(CityData city, List<DistrictData> districts)
        {
            var alive = new List<DistrictData>();
            for (int i = 0; i < districts.Count; i++)
            {
                if (districts[i].BlockIds.Count > 0)
                    alive.Add(districts[i]);
            }

            city.Districts.Clear();
            for (int ni = 0; ni < alive.Count; ni++)
            {
                DistrictData d = alive[ni];
                int oldId = d.Id;
                d.Id = ni;
                for (int j = 0; j < city.Blocks.Count; j++)
                {
                    if (city.Blocks[j].DistrictId == oldId)
                    {
                        city.Blocks[j].DistrictId = ni;
                        city.Blocks[j].DistrictKind = d.Kind;
                    }
                }

                city.Districts.Add(d);
            }
        }

        static void ComputeMetadata(DistrictData d, CitySeed stageSeed)
        {
            var rng = stageSeed.CreateSystemRandom();
            float jitter() => (float)(rng.NextDouble() * 0.14 - 0.07f);
            float J() => jitter();

            switch (d.Kind)
            {
                case DistrictKind.DowntownCommercial:
                    d.WealthLevel = Clamp01(0.58f + J());
                    d.DensityLevel = Clamp01(0.88f + J());
                    d.CrimeBaseline = Clamp01(0.42f + J());
                    d.PoliceBaseline = Clamp01(0.72f + J());
                    d.CommercialValue = Clamp01(0.92f + J() * 0.5f);
                    d.LogisticsValue = Clamp01(0.55f + J());
                    break;
                case DistrictKind.Industrial:
                    d.WealthLevel = Clamp01(0.32f + J());
                    d.DensityLevel = Clamp01(0.52f + J());
                    d.CrimeBaseline = Clamp01(0.48f + J());
                    d.PoliceBaseline = Clamp01(0.38f + J());
                    d.CommercialValue = Clamp01(0.28f + J());
                    d.LogisticsValue = Clamp01(0.88f + J() * 0.5f);
                    break;
                case DistrictKind.DocksPort:
                    d.WealthLevel = Clamp01(0.4f + J());
                    d.DensityLevel = Clamp01(0.55f + J());
                    d.CrimeBaseline = Clamp01(0.52f + J());
                    d.PoliceBaseline = Clamp01(0.45f + J());
                    d.CommercialValue = Clamp01(0.72f + J());
                    d.LogisticsValue = Clamp01(0.9f + J() * 0.5f);
                    break;
                case DistrictKind.Wealthy:
                    d.WealthLevel = Clamp01(0.88f + J() * 0.5f);
                    d.DensityLevel = Clamp01(0.38f + J());
                    d.CrimeBaseline = Clamp01(0.22f + J());
                    d.PoliceBaseline = Clamp01(0.68f + J());
                    d.CommercialValue = Clamp01(0.45f + J());
                    d.LogisticsValue = Clamp01(0.35f + J());
                    break;
                case DistrictKind.WorkingClass:
                    d.WealthLevel = Clamp01(0.38f + J());
                    d.DensityLevel = Clamp01(0.68f + J());
                    d.CrimeBaseline = Clamp01(0.55f + J());
                    d.PoliceBaseline = Clamp01(0.48f + J());
                    d.CommercialValue = Clamp01(0.42f + J());
                    d.LogisticsValue = Clamp01(0.52f + J());
                    break;
                case DistrictKind.Residential:
                    d.WealthLevel = Clamp01(0.48f + J());
                    d.DensityLevel = Clamp01(0.62f + J());
                    d.CrimeBaseline = Clamp01(0.38f + J());
                    d.PoliceBaseline = Clamp01(0.52f + J());
                    d.CommercialValue = Clamp01(0.48f + J());
                    d.LogisticsValue = Clamp01(0.4f + J());
                    break;
                case DistrictKind.FringeOuterEdge:
                    d.WealthLevel = Clamp01(0.35f + J());
                    d.DensityLevel = Clamp01(0.35f + J());
                    d.CrimeBaseline = Clamp01(0.48f + J());
                    d.PoliceBaseline = Clamp01(0.32f + J());
                    d.CommercialValue = Clamp01(0.28f + J());
                    d.LogisticsValue = Clamp01(0.38f + J());
                    break;
                default:
                    d.WealthLevel = d.DensityLevel = d.CrimeBaseline = d.PoliceBaseline = 0.5f;
                    d.CommercialValue = d.LogisticsValue = 0.5f;
                    break;
            }
        }

        static float Clamp01(float v) => Mathf.Clamp01(v);

        static string BuildDistrictName(DistrictData d, Vector2 cityCenter)
        {
            Vector2 r = d.CenterPosition - cityCenter;
            string card = CardinalPrefix(r);

            switch (d.Kind)
            {
                case DistrictKind.DowntownCommercial:
                    return "Downtown";
                case DistrictKind.Industrial:
                    return string.IsNullOrEmpty(card) ? "Industrial Ward" : card + " Industrial";
                case DistrictKind.DocksPort:
                    return "Port District";
                case DistrictKind.Wealthy:
                    return card + " Heights";
                case DistrictKind.WorkingClass:
                    return card + " Yards";
                case DistrictKind.Residential:
                    return card + " Residential";
                case DistrictKind.FringeOuterEdge:
                    return card + " Fringe";
                default:
                    return "District " + d.Id;
            }
        }

        static string CardinalPrefix(Vector2 r)
        {
            if (Mathf.Abs(r.y) >= Mathf.Abs(r.x))
                return r.y >= 0f ? "North" : "South";
            return r.x >= 0f ? "East" : "West";
        }

        static void BuildOutlineFromBlocks(CityData city, DistrictData d)
        {
            d.Outline.Clear();
            var corners = new List<Vector2>();
            for (int i = 0; i < d.BlockIds.Count; i++)
            {
                BlockData b = FindBlockById(city, d.BlockIds[i]);
                if (b == null)
                    continue;
                corners.Add(b.Min);
                corners.Add(new Vector2(b.Max.x, b.Min.y));
                corners.Add(b.Max);
                corners.Add(new Vector2(b.Min.x, b.Max.y));
            }

            if (corners.Count < 3)
                return;
            PolygonUtility.BuildConvexHull(corners, d.Outline);
        }

        static BlockData FindBlockById(CityData city, int id)
        {
            for (int i = 0; i < city.Blocks.Count; i++)
            {
                if (city.Blocks[i].Id == id)
                    return city.Blocks[i];
            }

            return null;
        }
    }
}
