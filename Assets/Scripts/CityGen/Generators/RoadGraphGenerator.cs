using System.Collections.Generic;
using FamilyBusiness.CityGen.Core;
using FamilyBusiness.CityGen.Data;
using FamilyBusiness.CityGen.Utils;
using UnityEngine;

namespace FamilyBusiness.CityGen.Generators
{
    /// <summary>
    /// Batch 3: internal road graph — major skeleton (anchor MST + extras), secondary branches, polygon + macro feature awareness.
    /// </summary>
    public sealed class RoadGraphGenerator
    {
        public void Generate(CityData city, CityGenerationConfig config, CitySeed stageSeed)
        {
            city.RoadNodes.Clear();
            city.RoadEdges.Clear();

            IReadOnlyList<Vector2> poly = city.MacroBoundary.Vertices;
            if (poly == null || poly.Count < 3)
            {
                BuildFallbackAabbRing(city);
                return;
            }

            var ctx = new BuildContext(city, config, stageSeed, poly);
            ctx.Run();

            FinalizeTopology(city);
            EnsureConnectivity(city, config, stageSeed);
            FinalizeTopology(city);
            LogIfDisconnected(city);
        }

        static void BuildFallbackAabbRing(CityData city)
        {
            Vector2 min = city.Boundary.Min;
            Vector2 max = city.Boundary.Max;
            for (int i = 0; i < 4; i++)
                city.RoadNodes.Add(new RoadNode { Id = i, Position = Vector2.zero });

            city.RoadNodes[0].Position = new Vector2(min.x, min.y);
            city.RoadNodes[1].Position = new Vector2(max.x, min.y);
            city.RoadNodes[2].Position = new Vector2(max.x, max.y);
            city.RoadNodes[3].Position = new Vector2(min.x, max.y);

            for (int i = 0; i < 4; i++)
            {
                int j = (i + 1) % 4;
                city.RoadEdges.Add(new RoadEdge
                {
                    Id = i,
                    FromNodeId = i,
                    ToNodeId = j,
                    Kind = RoadEdgeKind.Major,
                    Length = RoadGraphGeometry.SegmentLength(city.RoadNodes[i].Position, city.RoadNodes[j].Position)
                });
            }

            FinalizeTopology(city);
        }

        static void FinalizeTopology(CityData city)
        {
            foreach (RoadNode n in city.RoadNodes)
                n.ConnectedEdgeIds.Clear();

            foreach (RoadEdge e in city.RoadEdges)
            {
                RoadNode a = FindNodeById(city, e.FromNodeId);
                RoadNode b = FindNodeById(city, e.ToNodeId);
                if (a != null)
                    a.ConnectedEdgeIds.Add(e.Id);
                if (b != null)
                    b.ConnectedEdgeIds.Add(e.Id);
            }
        }

        static RoadNode FindNodeById(CityData city, int id)
        {
            for (int i = 0; i < city.RoadNodes.Count; i++)
            {
                if (city.RoadNodes[i].Id == id)
                    return city.RoadNodes[i];
            }

            return null;
        }

        static void LogIfDisconnected(CityData city)
        {
            if (city.RoadNodes.Count == 0)
                return;
            int comp = CountComponents(city);
            if (comp > 1)
                UnityEngine.Debug.LogWarning($"[RoadGraphGenerator] City road graph has {comp} disconnected components (seed={city.Seed}).");
        }

        static int CountComponents(CityData city)
        {
            var seen = new HashSet<int>();
            int components = 0;
            foreach (RoadNode n in city.RoadNodes)
            {
                if (seen.Contains(n.Id))
                    continue;
                components++;
                var q = new Queue<int>();
                q.Enqueue(n.Id);
                seen.Add(n.Id);
                while (q.Count > 0)
                {
                    int cur = q.Dequeue();
                    RoadNode cn = FindNodeById(city, cur);
                    if (cn == null) continue;
                    foreach (int eid in cn.ConnectedEdgeIds)
                    {
                        RoadEdge e = FindEdgeById(city, eid);
                        if (e == null) continue;
                        int o = e.FromNodeId == cur ? e.ToNodeId : e.FromNodeId;
                        if (seen.Add(o))
                            q.Enqueue(o);
                    }
                }
            }

            return components;
        }

        static RoadEdge FindEdgeById(CityData city, int id)
        {
            for (int i = 0; i < city.RoadEdges.Count; i++)
            {
                if (city.RoadEdges[i].Id == id)
                    return city.RoadEdges[i];
            }

            return null;
        }

        static void EnsureConnectivity(CityData city, CityGenerationConfig config, CitySeed stageSeed)
        {
            if (city.RoadNodes.Count < 2)
                return;

            var seen = new HashSet<int>();
            var comps = new List<List<int>>();
            RebuildComponents(city, seen, comps);

            if (comps.Count <= 1)
                return;

            int repairAttempts = 0;
            while (comps.Count > 1 && repairAttempts < 32)
            {
                repairAttempts++;
                var candidates = new List<(float d, int a, int b)>();
                float minD = config.minRoadSegmentLength * 0.75f;
                for (int i = 0; i < comps.Count; i++)
                {
                    for (int j = i + 1; j < comps.Count; j++)
                    {
                        foreach (int ia in comps[i])
                        {
                            RoadNode na = FindNodeById(city, ia);
                            if (na == null) continue;
                            foreach (int ib in comps[j])
                            {
                                RoadNode nb = FindNodeById(city, ib);
                                if (nb == null) continue;
                                float d = RoadGraphGeometry.SegmentLength(na.Position, nb.Position);
                                if (d >= minD)
                                    candidates.Add((d, ia, ib));
                            }
                        }
                    }
                }

                if (candidates.Count == 0)
                    break;

                candidates.Sort((x, y) =>
                {
                    int c = x.d.CompareTo(y.d);
                    return c != 0 ? c : x.a != y.a ? x.a.CompareTo(y.a) : x.b.CompareTo(y.b);
                });

                var ctx = new BuildContext(city, config, stageSeed, city.MacroBoundary.Vertices, resumeFromCity: true);
                bool added = false;
                int tryCap = Mathf.Min(candidates.Count, 48);
                for (int t = 0; t < tryCap; t++)
                {
                    (float _, int ia, int ib) = candidates[t];
                    if (ctx.TryForceBridgeBetweenNodeIds(ia, ib))
                    {
                        added = true;
                        break;
                    }
                }

                if (!added)
                    break;

                RebuildComponents(city, seen, comps);
            }
        }

        static void RebuildComponents(CityData city, HashSet<int> seen, List<List<int>> comps)
        {
            comps.Clear();
            seen.Clear();
            foreach (RoadNode n in city.RoadNodes)
            {
                if (seen.Contains(n.Id))
                    continue;
                var comp = new List<int>();
                var q = new Queue<int>();
                q.Enqueue(n.Id);
                seen.Add(n.Id);
                while (q.Count > 0)
                {
                    int cur = q.Dequeue();
                    comp.Add(cur);
                    RoadNode cn = FindNodeById(city, cur);
                    if (cn == null) continue;
                    foreach (int eid in cn.ConnectedEdgeIds)
                    {
                        RoadEdge e = FindEdgeById(city, eid);
                        if (e == null) continue;
                        int o = e.FromNodeId == cur ? e.ToNodeId : e.FromNodeId;
                        if (seen.Add(o))
                            q.Enqueue(o);
                    }
                }

                comps.Add(comp);
            }
        }

        sealed class BuildContext
        {
            readonly CityData _city;
            readonly CityGenerationConfig _config;
            readonly System.Random _rng;
            readonly IReadOnlyList<Vector2> _poly;
            readonly Vector2 _centroid;
            readonly HashSet<long> _undirectedEdges = new HashSet<long>();

            int _nextNodeId;
            int _nextEdgeId;

            public BuildContext(CityData city, CityGenerationConfig config, CitySeed stageSeed, IReadOnlyList<Vector2> poly,
                bool resumeFromCity = false)
            {
                _city = city;
                _config = config;
                _rng = stageSeed.Fork(resumeFromCity ? "road_graph_resume" : "road_graph_build").CreateSystemRandom();
                _poly = poly;
                _centroid = city.MacroBoundary.Centroid();
                _nextNodeId = 0;
                _nextEdgeId = 0;

                if (!resumeFromCity)
                    return;

                foreach (RoadNode n in _city.RoadNodes)
                    _nextNodeId = Mathf.Max(_nextNodeId, n.Id + 1);
                foreach (RoadEdge e in _city.RoadEdges)
                {
                    _nextEdgeId = Mathf.Max(_nextEdgeId, e.Id + 1);
                    _undirectedEdges.Add(EdgeKey(e.FromNodeId, e.ToNodeId));
                }
            }

            public void Run()
            {
                var anchors = CollectAnchors();
                if (anchors.Count == 0)
                    anchors.Add(_centroid);

                BuildPeripheralAccess(anchors);
                BuildMajorNetwork(anchors);
                BuildSecondaryNetwork(anchors);
            }

            List<Vector2> CollectAnchors()
            {
                var list = new List<Vector2>();
                foreach (MacroAnchorPointData a in _city.MacroAnchors)
                    list.Add(RoadGraphGeometry.ProjectIntoPolygon(a.Position, _poly, _centroid));

                if (list.Count == 0)
                    list.Add(_centroid);
                return list;
            }

            void BuildPeripheralAccess(List<Vector2> anchors)
            {
                if (_poly.Count < 3)
                    return;

                int bestEdge = 0;
                float bestLen = -1f;
                for (int i = 0; i < _poly.Count; i++)
                {
                    Vector2 a = _poly[i];
                    Vector2 b = _poly[(i + 1) % _poly.Count];
                    float len = RoadGraphGeometry.SegmentLength(a, b);
                    if (len > bestLen)
                    {
                        bestLen = len;
                        bestEdge = i;
                    }
                }

                Vector2 mid = (_poly[bestEdge] + _poly[(bestEdge + 1) % _poly.Count]) * 0.5f;
                Vector2 gate = Vector2.Lerp(mid, _centroid, 0.12f);
                gate = RoadGraphGeometry.ProjectIntoPolygon(gate, _poly, _centroid);

                Vector2 downtown = anchors[0];
                TryAddEdgeBetweenPositions(gate, downtown, RoadEdgeKind.Major, 0);
            }

            void BuildMajorNetwork(List<Vector2> anchors)
            {
                int n = anchors.Count;
                var edges = new List<(int i, int j, float len)>();
                for (int i = 0; i < n; i++)
                {
                    for (int j = i + 1; j < n; j++)
                    {
                        float len = RoadGraphGeometry.SegmentLength(anchors[i], anchors[j]);
                        edges.Add((i, j, len));
                    }
                }

                edges.Sort((a, b) =>
                {
                    int c = a.len.CompareTo(b.len);
                    if (c != 0) return c;
                    c = a.i.CompareTo(b.i);
                    return c != 0 ? c : a.j.CompareTo(b.j);
                });

                var uf = new UnionFind(n);
                int mstEdges = 0;
                foreach ((int i, int j, float len) in edges)
                {
                    if (uf.AreConnected(i, j))
                        continue;
                    if (!TryAddEdgeBetweenPositions(anchors[i], anchors[j], RoadEdgeKind.Major, 0))
                        continue;
                    uf.Union(i, j);
                    mstEdges++;
                    if (mstEdges >= n - 1)
                        break;
                }

                int extras = Mathf.Max(0, _config.majorRoadExtraEdgesBeyondMst);
                for (int k = edges.Count - 1; k >= 0 && extras > 0; k--)
                {
                    (int i, int j, float len) = edges[k];
                    if (len < _config.minRoadSegmentLength * 4f)
                        continue;
                    if (!uf.AreConnected(i, j))
                        continue;
                    if (TryAddEdgeBetweenPositions(anchors[i], anchors[j], RoadEdgeKind.Major, 0))
                        extras--;
                }
            }

            void BuildSecondaryNetwork(List<Vector2> anchors)
            {
                int target = Mathf.Max(0, _config.secondaryRoadTargetEdges);
                int sweep = 0;
                while (CountSecondaryEdges() < target && sweep < 48)
                {
                    sweep++;
                    bool any = false;

                    for (int ni = 0; ni < _city.RoadNodes.Count; ni++)
                    {
                        RoadNode node = _city.RoadNodes[ni];
                        if (!NodeTouchesMajor(node))
                            continue;

                        for (int a = 0; a < _config.secondaryBranchAttemptsPerNode; a++)
                        {
                            float ang = (float)(_rng.NextDouble() * Mathf.PI * 2.0);
                            float len = _config.secondaryRoadTypicalLength *
                                        Mathf.Lerp(0.65f, 1.15f, (float)_rng.NextDouble());
                            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                            Vector2 end = node.Position + dir * len;
                            end = RoadGraphGeometry.ProjectIntoPolygon(end, _poly, _centroid);
                            if (TryAddEdgeBetweenPositions(node.Position, end, RoadEdgeKind.Secondary, 0))
                            {
                                any = true;
                                if (CountSecondaryEdges() >= target)
                                    return;
                            }
                        }
                    }

                    for (int i = 0; i < anchors.Count && CountSecondaryEdges() < target; i++)
                    {
                        for (int j = i + 1; j < anchors.Count; j++)
                        {
                            float d = RoadGraphGeometry.SegmentLength(anchors[i], anchors[j]);
                            if (d > _config.secondaryRoadTypicalLength * 2.2f)
                                continue;
                            if (d < _config.minRoadSegmentLength)
                                continue;
                            if (TryAddEdgeBetweenPositions(anchors[i], anchors[j], RoadEdgeKind.Secondary, 0))
                            {
                                any = true;
                                if (CountSecondaryEdges() >= target)
                                    return;
                            }
                        }
                    }

                    if (!any)
                        break;
                }
            }

            int CountSecondaryEdges()
            {
                int c = 0;
                for (int i = 0; i < _city.RoadEdges.Count; i++)
                {
                    if (_city.RoadEdges[i].Kind == RoadEdgeKind.Secondary)
                        c++;
                }

                return c;
            }

            bool NodeTouchesMajor(RoadNode node)
            {
                for (int i = 0; i < _city.RoadEdges.Count; i++)
                {
                    RoadEdge e = _city.RoadEdges[i];
                    if (e.Kind != RoadEdgeKind.Major)
                        continue;
                    if (e.FromNodeId == node.Id || e.ToNodeId == node.Id)
                        return true;
                }

                return false;
            }

            public bool TryForceBridgeBetweenNodeIds(int idA, int idB)
            {
                RoadNode a = FindNodeById(_city, idA);
                RoadNode b = FindNodeById(_city, idB);
                if (a == null || b == null)
                    return false;
                return TryAddEdgeBetweenPositions(a.Position, b.Position, RoadEdgeKind.Secondary, 0, force: true);
            }

            bool TryAddEdgeBetweenPositions(Vector2 a, Vector2 b, RoadEdgeKind kind, int depth, bool force = false)
            {
                a = RoadGraphGeometry.ProjectIntoPolygon(a, _poly, _centroid);
                b = RoadGraphGeometry.ProjectIntoPolygon(b, _poly, _centroid);
                RoadGraphGeometry.TryShortenEndInsidePolygon(ref a, ref b, _poly, _config.segmentSampleCount);
                RoadGraphGeometry.TryShortenEndInsidePolygon(ref b, ref a, _poly, _config.segmentSampleCount);

                float len = RoadGraphGeometry.SegmentLength(a, b);
                if (len < _config.minRoadSegmentLength && !force)
                    return false;

                if (force)
                {
                    if (!RoadGraphGeometry.SegmentSamplesInsidePolygon(a, b, _poly, _config.segmentSampleCount))
                        return false;
                }
                else if (!SegmentValid(a, b, kind))
                {
                    if (depth >= _config.maxRoadSubdivideDepth)
                        return false;
                    Vector2 m = Vector2.Lerp(a, b, 0.5f);
                    m = RoadGraphGeometry.ProjectIntoPolygon(m, _poly, _centroid);
                    return TryAddEdgeBetweenPositions(a, m, kind, depth + 1, false)
                           || TryAddEdgeBetweenPositions(m, b, kind, depth + 1, false);
                }

                int na = GetOrCreateNode(a);
                int nb = GetOrCreateNode(b);
                if (na == nb)
                    return false;

                long key = EdgeKey(na, nb);
                if (!_undirectedEdges.Add(key))
                    return false;

                bool crossesRail = RoadGraphGeometry.SegmentCrossesRailBuffer(
                    _city, a, b, _config.railCorridorHalfWidthForCrossingCells, _config.segmentSampleCount);

                var edge = new RoadEdge
                {
                    Id = _nextEdgeId++,
                    FromNodeId = na,
                    ToNodeId = nb,
                    Kind = kind,
                    Length = RoadGraphGeometry.SegmentLength(
                        FindNodeById(_city, na).Position,
                        FindNodeById(_city, nb).Position),
                    CrossesMacroRailCorridor = crossesRail,
                    TagsPlaceholder = null
                };

                _city.RoadEdges.Add(edge);
                return true;
            }

            bool SegmentValid(Vector2 a, Vector2 b, RoadEdgeKind kind)
            {
                if (!RoadGraphGeometry.SegmentSamplesInsidePolygon(a, b, _poly, _config.segmentSampleCount))
                    return false;

                if (RoadGraphGeometry.SegmentTooCloseToWater(_city, a, b, _config.waterRoadClearanceCells,
                        _config.segmentSampleCount))
                    return false;

                return true;
            }

            int GetOrCreateNode(Vector2 p)
            {
                p = RoadGraphGeometry.ProjectIntoPolygon(p, _poly, _centroid);
                float merge = _config.roadNodeMergeDistance;
                float mergeSq = merge * merge;
                for (int i = 0; i < _city.RoadNodes.Count; i++)
                {
                    if ((_city.RoadNodes[i].Position - p).sqrMagnitude <= mergeSq)
                        return _city.RoadNodes[i].Id;
                }

                var node = new RoadNode { Id = _nextNodeId++, Position = p };
                _city.RoadNodes.Add(node);
                return node.Id;
            }

            static long EdgeKey(int a, int b)
            {
                if (a > b)
                    (a, b) = (b, a);
                return ((long)a << 32) | (uint)b;
            }
        }

        sealed class UnionFind
        {
            readonly int[] _p;
            readonly int[] _r;

            public UnionFind(int n)
            {
                _p = new int[n];
                _r = new int[n];
                for (int i = 0; i < n; i++)
                    _p[i] = i;
            }

            int Find(int x)
            {
                if (_p[x] != x)
                    _p[x] = Find(_p[x]);
                return _p[x];
            }

            public bool Union(int a, int b)
            {
                a = Find(a);
                b = Find(b);
                if (a == b)
                    return false;
                if (_r[a] < _r[b])
                    (a, b) = (b, a);
                _p[b] = a;
                if (_r[a] == _r[b])
                    _r[a]++;
                return true;
            }

            public bool AreConnected(int a, int b) => Find(a) == Find(b);
        }
    }
}
