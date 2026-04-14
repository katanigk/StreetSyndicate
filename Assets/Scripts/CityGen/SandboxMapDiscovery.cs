using System.Collections.Generic;
using FamilyBusiness.CityGen.Data;
using UnityEngine;

/// <summary>
/// After execution-day ops (Scout / Surveillance / Collect), marks sandbox blocks along travel from crew home
/// to the op target so macro fog can clear on the Ops map without relying only on Chebyshev distance from home.
/// </summary>
public static class SandboxMapDiscovery
{
    /// <summary>
    /// Reveals every grid block on a Bresenham line from <paramref name="crewHomeBlockId"/> to <paramref name="destBlockId"/> (inclusive).
    /// </summary>
    public static void RevealTravelPathAndDestination(CityData city, int crewHomeBlockId, int destBlockId)
    {
        if (city == null || crewHomeBlockId < 0 || destBlockId < 0)
            return;
        if (!GameSessionState.SingleBlockSandboxEnabled)
            return;

        OpsCityGenMapView.BuildSandboxGridAxes(city, out List<float> gxs, out List<float> gys);
        if (gxs == null || gys == null)
            return;

        if (!TryGetBlockGridCoords(city, crewHomeBlockId, gxs, gys, out int hx, out int hy))
            return;
        if (!TryGetBlockGridCoords(city, destBlockId, gxs, gys, out int bx, out int by))
            return;

        int x0 = hx;
        int y0 = hy;
        int x1 = bx;
        int y1 = by;
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            int bid = FindBlockIdAtGridCell(city, gxs, gys, x0, y0);
            if (bid >= 0)
                GameSessionState.RevealSandboxBlock(bid);

            if (x0 == x1 && y0 == y1)
                break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    static bool TryGetBlockGridCoords(CityData city, int blockId, List<float> gxs, List<float> gys, out int cx, out int cy)
    {
        cx = cy = -1;
        if (city?.Blocks == null)
            return false;
        for (int i = 0; i < city.Blocks.Count; i++)
        {
            if (city.Blocks[i].Id != blockId)
                continue;
            return OpsCityGenMapView.TryGetBlockGridCell(city.Blocks[i], gxs, gys, out cx, out cy);
        }

        return false;
    }

    static int FindBlockIdAtGridCell(CityData city, List<float> gxs, List<float> gys, int cx, int cy)
    {
        if (city?.Blocks == null)
            return -1;
        for (int i = 0; i < city.Blocks.Count; i++)
        {
            BlockData b = city.Blocks[i];
            if (OpsCityGenMapView.TryGetBlockGridCell(b, gxs, gys, out int bcx, out int bcy) && bcx == cx && bcy == cy)
                return b.Id;
        }

        return -1;
    }
}
