using System.Collections.Generic;
using FamilyBusiness.CityGen.Core;
using FamilyBusiness.CityGen.Data;
using FamilyBusiness.CityGen.Utils;
using UnityEngine;

namespace FamilyBusiness.CityGen.Generators
{
    /// <summary>
    /// Batch 4: coarse axis-aligned block grid inside the macro footprint (district cells, not final lots).
    /// </summary>
    public sealed class BlockGenerator
    {
        public void Generate(CityData city, CityGenerationConfig config, CitySeed stageSeed)
        {
            city.Blocks.Clear();

            Vector2 bmin = city.Boundary.Min;
            Vector2 bmax = city.Boundary.Max;
            float cell = Mathf.Max(config.blockGridCellSizeCells, config.minimumBlockSizeCells * 0.5f);
            IReadOnlyList<Vector2> poly = city.MacroBoundary.Vertices;
            bool usePoly = poly != null && poly.Count >= 3;

            int nextId = 0;
            for (float x = bmin.x; x < bmax.x - 1e-4f; x += cell)
            {
                for (float y = bmin.y; y < bmax.y - 1e-4f; y += cell)
                {
                    Vector2 mn = new Vector2(x, y);
                    Vector2 mx = new Vector2(Mathf.Min(x + cell, bmax.x), Mathf.Min(y + cell, bmax.y));
                    Vector2 c = (mn + mx) * 0.5f;

                    if (usePoly && !PolygonUtility.PointInPolygon(c, poly))
                        continue;
                    if (BlockCenterInExcludedWater(city, c, config.blockWaterExclusionPaddingCells))
                        continue;

                    city.Blocks.Add(new BlockData
                    {
                        Id = nextId++,
                        DistrictId = -1,
                        DistrictKind = DistrictKind.Unknown,
                        Min = mn,
                        Max = mx
                    });
                }
            }

            if (city.Blocks.Count == 0)
                AddFallbackBlock(city, config, ref nextId);

            _ = stageSeed;
        }

        static bool BlockCenterInExcludedWater(CityData city, Vector2 center, float pad)
        {
            foreach (MacroFeatureData f in city.MacroFeatures)
            {
                if (f.Kind != MacroFeatureKind.River && f.Kind != MacroFeatureKind.Coastline)
                    continue;
                if (f.Path == null || f.Path.Count < 2)
                    continue;
                float half = f.WidthCells * 0.5f + pad;
                float d = RoadGraphGeometry.MinDistancePointToPolyline(center, f.Path);
                if (d < half)
                    return true;
            }

            return false;
        }

        static void AddFallbackBlock(CityData city, CityGenerationConfig config, ref int nextId)
        {
            Vector2 min = city.Boundary.Min;
            Vector2 max = city.Boundary.Max;
            float pad = Mathf.Clamp(config.minimumBlockSizeCells * 0.25f, 2f, Mathf.Min(max.x - min.x, max.y - min.y) * 0.25f);
            var block = new BlockData
            {
                Id = nextId++,
                DistrictId = -1,
                DistrictKind = DistrictKind.Unknown,
                Min = min + Vector2.one * pad,
                Max = max - Vector2.one * pad
            };

            if (block.Max.x <= block.Min.x || block.Max.y <= block.Min.y)
            {
                block.Min = min;
                block.Max = max;
            }

            city.Blocks.Add(block);
        }
    }
}
