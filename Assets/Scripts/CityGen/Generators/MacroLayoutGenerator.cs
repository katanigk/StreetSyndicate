using System.Collections.Generic;
using FamilyBusiness.CityGen.Core;
using FamilyBusiness.CityGen.Data;
using FamilyBusiness.CityGen.Utils;
using UnityEngine;

namespace FamilyBusiness.CityGen.Generators
{
    /// <summary>
    /// Batch 2: Ashkelton macro footprint — irregular boundary, optional water + rail, macro planning anchors.
    /// </summary>
    public sealed class MacroLayoutGenerator
    {
        static readonly MacroAnchorKind[] s_anchorKindOrder =
        {
            MacroAnchorKind.DowntownCore,
            MacroAnchorKind.Docks,
            MacroAnchorKind.Industrial,
            MacroAnchorKind.WealthyResidential,
            MacroAnchorKind.ResidentialSpread,
            MacroAnchorKind.RailYard
        };

        public void Generate(CityData city, CityGenerationConfig config, CitySeed stageSeed)
        {
            var rng = stageSeed.CreateSystemRandom();
            city.MacroBoundary.Clear();
            city.MacroFeatures.Clear();
            city.MacroAnchors.Clear();

            float w = config.cityWidthCells;
            float h = config.cityHeightCells;
            float ir = Mathf.Clamp01(config.boundaryIrregularity) * Mathf.Min(w, h);

            BuildIrregularBoundary(city.MacroBoundary.Vertices, w, h, ir, rng, stageSeed);

            city.Boundary = city.MacroBoundary.ToAxisAlignedBoundary();

            bool addWater = config.forceWaterFeature || stageSeed.NextFloat01(rng) < config.waterFeatureChance;
            MacroFeatureData waterFeature = null;
            if (addWater)
            {
                bool river = stageSeed.NextInt(rng, 0, 2) == 0;
                if (river)
                    waterFeature = BuildRiver(config, rng, stageSeed, w, h);
                else
                    waterFeature = BuildCoastline(config, rng, stageSeed, w, h);
                if (waterFeature != null)
                    city.MacroFeatures.Add(waterFeature);
            }

            bool addRail = config.forceRailFeature || stageSeed.NextFloat01(rng) < config.railFeatureChance;
            if (addRail)
                city.MacroFeatures.Add(BuildRailCorridor(city, config, rng, stageSeed, w, h));

            int minA = Mathf.Min(config.macroAnchorCountMin, config.macroAnchorCountMax);
            int maxA = Mathf.Max(config.macroAnchorCountMin, config.macroAnchorCountMax);
            int anchorCount = Mathf.Clamp(
                stageSeed.NextInt(rng, minA, maxA + 1),
                minA,
                maxA);

            IReadOnlyList<Vector2> poly = city.MacroBoundary.Vertices;
            Vector2 centroid = city.MacroBoundary.Centroid();
            Vector2 railMid = GetRailMidpoint(city);
            Vector2 dockHint = GetDockHint(waterFeature, w, h, centroid);

            for (int i = 0; i < anchorCount; i++)
            {
                MacroAnchorKind kind = s_anchorKindOrder[i % s_anchorKindOrder.Length];
                Vector2 p = PlaceMacroAnchor(kind, centroid, railMid, dockHint, w, h, rng, stageSeed, poly);
                p = EnsureInsidePolygon(p, centroid, poly);
                city.MacroAnchors.Add(new MacroAnchorPointData
                {
                    Id = i,
                    Kind = kind,
                    Position = p,
                    Label = config.cityDisplayName + " · " + kind
                });
            }
        }

        static void BuildIrregularBoundary(List<Vector2> verts, float w, float h, float ir, System.Random rng, CitySeed stageSeed)
        {
            float Ox() => (stageSeed.NextFloat01(rng) * 2f - 1f) * ir;
            float Oy() => (stageSeed.NextFloat01(rng) * 2f - 1f) * ir;

            // CCW from bottom-left
            verts.Add(new Vector2(0f + Ox() * 0.6f, 0f + Oy()));
            verts.Add(new Vector2(w * (0.42f + 0.08f * (stageSeed.NextFloat01(rng) - 0.5f)) + Ox(), 0f + Oy() * 0.7f));
            verts.Add(new Vector2(w + Ox(), 0f + Oy() * 0.5f));
            verts.Add(new Vector2(w + Ox() * 0.5f, h * (0.38f + 0.1f * (stageSeed.NextFloat01(rng) - 0.5f)) + Oy()));
            verts.Add(new Vector2(w + Ox(), h + Oy()));
            verts.Add(new Vector2(w * (0.48f + 0.1f * (stageSeed.NextFloat01(rng) - 0.5f)) + Ox(), h + Oy()));
            verts.Add(new Vector2(0f + Ox() * 0.4f, h + Oy()));
            verts.Add(new Vector2(0f + Ox(), h * (0.55f + 0.12f * (stageSeed.NextFloat01(rng) - 0.5f)) + Oy()));
        }

        static MacroFeatureData BuildRiver(CityGenerationConfig config, System.Random rng, CitySeed stageSeed, float w, float h)
        {
            float x0 = w * Mathf.Lerp(0.18f, 0.35f, stageSeed.NextFloat01(rng));
            float x1 = w * Mathf.Lerp(0.55f, 0.82f, stageSeed.NextFloat01(rng));
            float xm = w * Mathf.Lerp(0.35f, 0.65f, stageSeed.NextFloat01(rng)) + (stageSeed.NextFloat01(rng) - 0.5f) * w * 0.08f;

            var f = new MacroFeatureData
            {
                Id = 0,
                Kind = MacroFeatureKind.River,
                Label = config.cityDisplayName + " River (macro)",
                WidthCells = config.riverApproxWidthCells
            };
            f.Path.Add(new Vector2(x0, h));
            f.Path.Add(new Vector2(xm, h * 0.62f + (stageSeed.NextFloat01(rng) - 0.5f) * h * 0.08f));
            f.Path.Add(new Vector2((x0 + x1) * 0.5f + (stageSeed.NextFloat01(rng) - 0.5f) * w * 0.06f, h * 0.38f));
            f.Path.Add(new Vector2(x1, h * 0.12f + (stageSeed.NextFloat01(rng) - 0.5f) * h * 0.06f));
            f.Path.Add(new Vector2(x1 + (stageSeed.NextFloat01(rng) - 0.5f) * w * 0.04f, 0f));
            return f;
        }

        static MacroFeatureData BuildCoastline(CityGenerationConfig config, System.Random rng, CitySeed stageSeed, float w, float h)
        {
            float depth = Mathf.Clamp(config.coastStripDepthCells, 2f, w * 0.35f);
            float inland = w - depth;
            float wobble0 = (stageSeed.NextFloat01(rng) - 0.5f) * depth * 0.35f;
            float wobble1 = (stageSeed.NextFloat01(rng) - 0.5f) * depth * 0.45f;
            float wobble2 = (stageSeed.NextFloat01(rng) - 0.5f) * depth * 0.35f;

            var f = new MacroFeatureData
            {
                Id = 0,
                Kind = MacroFeatureKind.Coastline,
                Label = config.cityDisplayName + " Coast (macro, east)",
                WidthCells = depth
            };
            f.Path.Add(new Vector2(inland + wobble0, 0f));
            f.Path.Add(new Vector2(inland + wobble1, h * 0.33f));
            f.Path.Add(new Vector2(inland + wobble2, h * 0.66f));
            f.Path.Add(new Vector2(inland + wobble0 * 0.8f, h));
            return f;
        }

        static MacroFeatureData BuildRailCorridor(CityData city, CityGenerationConfig config, System.Random rng, CitySeed stageSeed, float w, float h)
        {
            float y0 = h * Mathf.Lerp(0.22f, 0.38f, stageSeed.NextFloat01(rng));
            float y1 = h * Mathf.Lerp(0.2f, 0.42f, stageSeed.NextFloat01(rng));
            float bend = (stageSeed.NextFloat01(rng) - 0.5f) * h * 0.06f;

            var f = new MacroFeatureData
            {
                Id = city.MacroFeatures.Count,
                Kind = MacroFeatureKind.RailCorridor,
                Label = config.cityDisplayName + " Rail (macro)",
                WidthCells = config.railCorridorWidthCells
            };
            f.Path.Add(new Vector2(0f, y0));
            f.Path.Add(new Vector2(w * 0.35f, y0 + bend * 0.5f));
            f.Path.Add(new Vector2(w * 0.72f, y1 - bend * 0.35f));
            f.Path.Add(new Vector2(w, y1));
            return f;
        }

        static Vector2 GetRailMidpoint(CityData city)
        {
            foreach (MacroFeatureData f in city.MacroFeatures)
            {
                if (f.Kind != MacroFeatureKind.RailCorridor || f.Path.Count == 0)
                    continue;
                int m = f.Path.Count / 2;
                return f.Path[m];
            }

            return city.MacroBoundary.Centroid();
        }

        static Vector2 GetDockHint(MacroFeatureData water, float w, float h, Vector2 centroid)
        {
            if (water != null && water.Path.Count > 0)
            {
                int idx = 0;
                float best = float.MaxValue;
                for (int i = 0; i < water.Path.Count; i++)
                {
                    if (water.Path[i].y < best)
                    {
                        best = water.Path[i].y;
                        idx = i;
                    }
                }

                return water.Path[idx];
            }

            return new Vector2(centroid.x, h * 0.06f);
        }

        static Vector2 PlaceMacroAnchor(
            MacroAnchorKind kind,
            Vector2 centroid,
            Vector2 railMid,
            Vector2 dockHint,
            float w,
            float h,
            System.Random rng,
            CitySeed stageSeed,
            IReadOnlyList<Vector2> poly)
        {
            switch (kind)
            {
                case MacroAnchorKind.DowntownCore:
                    return centroid;
                case MacroAnchorKind.Docks:
                    return dockHint;
                case MacroAnchorKind.Industrial:
                    return Vector2.Lerp(centroid, railMid, 0.55f) + Jitter(rng, stageSeed, w, h, 0.04f);
                case MacroAnchorKind.WealthyResidential:
                    return centroid + new Vector2(w * 0.14f * (stageSeed.NextFloat01(rng) - 0.35f), h * 0.1f * (stageSeed.NextFloat01(rng) - 0.25f));
                case MacroAnchorKind.ResidentialSpread:
                    return centroid + new Vector2(-w * 0.11f * stageSeed.NextFloat01(rng), -h * 0.07f * stageSeed.NextFloat01(rng));
                case MacroAnchorKind.RailYard:
                {
                    Vector2 dir = new Vector2(1f, (stageSeed.NextFloat01(rng) - 0.5f) * 0.08f).normalized;
                    Vector2 n = new Vector2(-dir.y, dir.x);
                    return railMid + n * Mathf.Min(w, h) * 0.05f + Jitter(rng, stageSeed, w, h, 0.02f);
                }
                default:
                    return centroid;
            }
        }

        static Vector2 Jitter(System.Random rng, CitySeed stageSeed, float w, float h, float scale)
        {
            return new Vector2(
                (stageSeed.NextFloat01(rng) - 0.5f) * w * scale,
                (stageSeed.NextFloat01(rng) - 0.5f) * h * scale);
        }

        static Vector2 EnsureInsidePolygon(Vector2 p, Vector2 centroid, IReadOnlyList<Vector2> poly)
        {
            if (PolygonUtility.PointInPolygon(p, poly))
                return p;

            for (int i = 0; i < 12; i++)
            {
                p = Vector2.Lerp(p, centroid, 0.22f);
                if (PolygonUtility.PointInPolygon(p, poly))
                    return p;
            }

            return centroid;
        }
    }
}
