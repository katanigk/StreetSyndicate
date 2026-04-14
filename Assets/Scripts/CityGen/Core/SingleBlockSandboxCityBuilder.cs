using FamilyBusiness.CityGen.Data;
using UnityEngine;

namespace FamilyBusiness.CityGen.Core
{
    /// <summary>
    /// Sandbox city: coarse blocks (default 3×4 = 12), each with an 8-lot ring on a 3×3 grid (center cell is courtyard — no lot).
    /// </summary>
    public static class SingleBlockSandboxCityBuilder
    {
        public static void Build(CityData city, CityGenerationConfig config, CitySeed seed)
        {
            int blocksX = Mathf.Clamp(config.singleBlockSandboxBlocksAlongX, 1, 8);
            int blocksY = Mathf.Clamp(config.singleBlockSandboxBlocksAlongY, 1, 8);
            float blockSq = Mathf.Clamp(config.singleBlockSandboxBlockSquareCells, 20f, 128f);
            float gap = Mathf.Clamp(config.singleBlockSandboxInterBlockGapCells, 0f, 16f);

            float stride = blockSq + gap;
            float extentX = blocksX * blockSq + Mathf.Max(0, blocksX - 1) * gap;
            float extentY = blocksY * blockSq + Mathf.Max(0, blocksY - 1) * gap;

            city.Boundary = new CityBoundary(Vector2.zero, new Vector2(extentX, extentY));

            city.MacroBoundary.Clear();
            city.MacroFeatures.Clear();
            city.MacroAnchors.Clear();
            city.RoadNodes.Clear();
            city.RoadEdges.Clear();
            city.Districts.Clear();
            city.Blocks.Clear();
            city.Lots.Clear();
            city.Anchors.Clear();
            city.Institutions.Clear();
            city.Buildings.Clear();

            Vector2 mn = Vector2.zero;
            Vector2 mx = new Vector2(extentX, extentY);
            city.MacroBoundary.Vertices.Add(new Vector2(mn.x, mn.y));
            city.MacroBoundary.Vertices.Add(new Vector2(mx.x, mn.y));
            city.MacroBoundary.Vertices.Add(new Vector2(mx.x, mx.y));
            city.MacroBoundary.Vertices.Add(new Vector2(mn.x, mx.y));

            const int districtId = 0;
            var district = new DistrictData
            {
                Id = districtId,
                Name = "Railway Cut",
                Kind = DistrictKind.WorkingClass,
                CenterPosition = (mn + mx) * 0.5f,
                WealthLevel = 0.35f,
                DensityLevel = 0.72f,
                CrimeBaseline = 0.55f,
                PoliceBaseline = 0.42f,
                CommercialValue = 0.4f,
                LogisticsValue = 0.35f
            };

            for (int bi = 0; bi < blocksX * blocksY; bi++)
                district.BlockIds.Add(bi);

            district.Outline.Add(new Vector2(mn.x, mn.y));
            district.Outline.Add(new Vector2(mx.x, mn.y));
            district.Outline.Add(new Vector2(mx.x, mx.y));
            district.Outline.Add(new Vector2(mn.x, mx.y));
            city.Districts.Add(district);

            float inset = Mathf.Clamp(config.lotInsetFromBlockCells, 0f, blockSq * 0.12f);
            const int nx = 3;
            const int ny = 3;

            int nextLotId = 0;
            int blockIndex = 0;
            for (int by = 0; by < blocksY; by++)
            {
                for (int bx = 0; bx < blocksX; bx++)
                {
                    Vector2 bmn = new Vector2(bx * stride, by * stride);
                    Vector2 bmx = new Vector2(bx * stride + blockSq, by * stride + blockSq);

                    city.Blocks.Add(new BlockData
                    {
                        Id = blockIndex,
                        DistrictId = districtId,
                        DistrictKind = DistrictKind.WorkingClass,
                        Min = bmn,
                        Max = bmx
                    });

                    Vector2 innerMin = bmn + Vector2.one * inset;
                    Vector2 innerMax = bmx - Vector2.one * inset;
                    float iw = innerMax.x - innerMin.x;
                    float ih = innerMax.y - innerMin.y;
                    float cw = iw / nx;
                    float ch = ih / ny;

                    for (int ix = 0; ix < nx; ix++)
                    {
                        for (int iy = 0; iy < ny; iy++)
                        {
                            if (ix == 1 && iy == 1)
                                continue;

                            Vector2 lmn = new Vector2(innerMin.x + ix * cw, innerMin.y + iy * ch);
                            Vector2 lmx = new Vector2(innerMin.x + (ix + 1) * cw, innerMin.y + (iy + 1) * ch);
                            var lot = new LotData
                            {
                                Id = nextLotId++,
                                BlockId = blockIndex,
                                DistrictId = districtId,
                                DistrictKind = DistrictKind.WorkingClass,
                                Min = lmn,
                                Max = lmx
                            };
                            lot.Outline.Clear();
                            lot.Outline.Add(new Vector2(lmn.x, lmn.y));
                            lot.Outline.Add(new Vector2(lmx.x, lmn.y));
                            lot.Outline.Add(new Vector2(lmx.x, lmx.y));
                            lot.Outline.Add(new Vector2(lmn.x, lmx.y));
                            city.Lots.Add(lot);
                        }
                    }

                    blockIndex++;
                }
            }

            city.GangStartPlanPosition = (mn + mx) * 0.5f;
            city.GangStartPlanValid = true;
            city.LastStartingGangSpawn = null;

            _ = seed;
        }
    }
}
