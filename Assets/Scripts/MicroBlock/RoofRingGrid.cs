using System.Collections.Generic;
using FamilyBusiness.CityGen.Data;
using UnityEngine;

/// <summary>
/// Maps a lot inside an 8-cell ring (3×3 minus center) to slot 0..7 for autotiled roof art.
/// Plan space: +x right, +y north (up on screen). Cell (0,0) = southwest, (2,2) = northeast.
/// </summary>
public static class RoofRingGrid
{
    /// <summary>
    /// Sandbox city builder creates lots in ix-then-iy order (skipping center). Eight lots per block
    /// get sequential ids; index k in Id order maps to ring slot (TL,T,TR,L,R,BL,B,BR).
    /// </summary>
    static readonly int[] SandboxCreationIndexToRingSlot = { 5, 3, 0, 6, 1, 7, 4, 2 };

    /// <summary>
    /// Prefer this in sandbox: <paramref name="ringLots"/> must be the eight lots of one block sorted by <see cref="LotData.Id"/> ascending only.
    /// </summary>
    public static bool TryGetRingSlotFromSandboxCreationOrder(LotData lot, IReadOnlyList<LotData> ringLotsSortedById, out int slotIndex)
    {
        slotIndex = 0;
        if (lot == null || ringLotsSortedById == null || ringLotsSortedById.Count != 8)
            return false;

        for (int k = 0; k < 8; k++)
        {
            if (ringLotsSortedById[k].Id == lot.Id)
            {
                slotIndex = SandboxCreationIndexToRingSlot[k];
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Order: TL, T, TR, L, R, BL, B, BR — matches inspector list on <see cref="BlockRoofVisualConfig.denseResidentialRingTiles"/>.
    /// </summary>
    public static bool TryGetRingSlotIndex(LotData lot, IReadOnlyList<LotData> ringLots, out int slotIndex)
    {
        slotIndex = 0;
        if (lot == null || ringLots == null || ringLots.Count != 8)
            return false;

        Vector2 gmin = ringLots[0].Min;
        Vector2 gmax = ringLots[0].Max;
        for (int i = 1; i < ringLots.Count; i++)
        {
            gmin = Vector2.Min(gmin, ringLots[i].Min);
            gmax = Vector2.Max(gmax, ringLots[i].Max);
        }

        float spanX = gmax.x - gmin.x;
        float spanY = gmax.y - gmin.y;
        if (spanX < 1e-5f || spanY < 1e-5f)
            return false;

        float cw = spanX / 3f;
        float ch = spanY / 3f;
        float mx = (lot.Min.x + lot.Max.x) * 0.5f;
        float my = (lot.Min.y + lot.Max.y) * 0.5f;
        int ix = Mathf.Clamp(Mathf.FloorToInt((mx - gmin.x) / cw + 1e-4f), 0, 2);
        int iy = Mathf.Clamp(Mathf.FloorToInt((my - gmin.y) / ch + 1e-4f), 0, 2);
        if (ix == 1 && iy == 1)
            return false;

        slotIndex = SlotIndexFromGrid(ix, iy);
        return true;
    }

    /// <summary>Maps grid (ix, iy) to 0..7 list index.</summary>
    public static int SlotIndexFromGrid(int ix, int iy)
    {
        if (ix == 0 && iy == 2)
            return 0;
        if (ix == 1 && iy == 2)
            return 1;
        if (ix == 2 && iy == 2)
            return 2;
        if (ix == 0 && iy == 1)
            return 3;
        if (ix == 2 && iy == 1)
            return 4;
        if (ix == 0 && iy == 0)
            return 5;
        if (ix == 1 && iy == 0)
            return 6;
        if (ix == 2 && iy == 0)
            return 7;
        return 0;
    }
}
