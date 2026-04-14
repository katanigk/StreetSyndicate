using FamilyBusiness.CityGen.Core;
using FamilyBusiness.CityGen.Data;
using UnityEngine;

namespace FamilyBusiness.CityGen.Generators
{
    /// <summary>
    /// Batch 5: uniform axis-aligned lot grid inside each block (inset + equal split). Deterministic from geometry + config.
    /// </summary>
    public sealed class LotGenerator
    {
        public void Generate(CityData city, CityGenerationConfig config, CitySeed stageSeed)
        {
            city.Lots.Clear();
            int nextLotId = 0;
            float inset = Mathf.Max(0f, config.lotInsetFromBlockCells);
            float target = Mathf.Max(1f, config.lotTargetCellSizeCells);
            float minLot = Mathf.Max(0.5f, config.minLotSizeCells);
            int maxPerBlock = Mathf.Max(0, config.maxLotsPerBlock);

            for (int bi = 0; bi < city.Blocks.Count; bi++)
            {
                BlockData block = city.Blocks[bi];
                Vector2 innerMin = block.Min + Vector2.one * inset;
                Vector2 innerMax = block.Max - Vector2.one * inset;
                if (innerMax.x - innerMin.x < minLot || innerMax.y - innerMin.y < minLot)
                {
                    innerMin = block.Min;
                    innerMax = block.Max;
                }

                float iw = innerMax.x - innerMin.x;
                float ih = innerMax.y - innerMin.y;
                if (iw < 1e-4f || ih < 1e-4f)
                    continue;

                int nx = Mathf.Max(1, Mathf.FloorToInt(iw / target));
                int ny = Mathf.Max(1, Mathf.FloorToInt(ih / target));
                float cw = iw / nx;
                float ch = ih / ny;

                while (nx > 1 && cw < minLot)
                {
                    nx--;
                    cw = iw / nx;
                }

                while (ny > 1 && ch < minLot)
                {
                    ny--;
                    ch = ih / ny;
                }

                if (maxPerBlock > 0)
                    ReduceLotDivisions(ref nx, ref ny, iw, ih, minLot, maxPerBlock);

                cw = iw / nx;
                ch = ih / ny;

                for (int ix = 0; ix < nx; ix++)
                {
                    for (int iy = 0; iy < ny; iy++)
                    {
                        Vector2 mn = new Vector2(innerMin.x + ix * cw, innerMin.y + iy * ch);
                        Vector2 mx = new Vector2(innerMin.x + (ix + 1) * cw, innerMin.y + (iy + 1) * ch);
                        if (mx.x - mn.x < minLot * 0.99f || mx.y - mn.y < minLot * 0.99f)
                            continue;

                        var lot = new LotData
                        {
                            Id = nextLotId++,
                            BlockId = block.Id,
                            DistrictId = block.DistrictId,
                            DistrictKind = block.DistrictKind,
                            Min = mn,
                            Max = mx
                        };
                        FillLotOutline(lot);
                        city.Lots.Add(lot);
                    }
                }
            }

            _ = stageSeed;
        }

        static void ReduceLotDivisions(ref int nx, ref int ny, float iw, float ih, float minLot, int maxCount)
        {
            while (nx * ny > maxCount)
            {
                float wIfDropX = nx > 1 ? iw / (nx - 1) : float.PositiveInfinity;
                float hIfDropY = ny > 1 ? ih / (ny - 1) : float.PositiveInfinity;
                bool canX = nx > 1 && wIfDropX >= minLot;
                bool canY = ny > 1 && hIfDropY >= minLot;
                if (!canX && !canY)
                    break;
                if (canX && canY)
                {
                    if (nx >= ny)
                        nx--;
                    else
                        ny--;
                }
                else if (canX)
                    nx--;
                else
                    ny--;
            }
        }

        static void FillLotOutline(LotData lot)
        {
            lot.Outline.Clear();
            Vector2 mn = lot.Min;
            Vector2 mx = lot.Max;
            lot.Outline.Add(new Vector2(mn.x, mn.y));
            lot.Outline.Add(new Vector2(mx.x, mn.y));
            lot.Outline.Add(new Vector2(mx.x, mx.y));
            lot.Outline.Add(new Vector2(mn.x, mx.y));
        }
    }
}
