using System.Collections.Generic;
using FamilyBusiness.CityGen.Data;
using UnityEngine;

/// <summary>
/// Maps each <see cref="MicroBlockSpotRuntime"/> to a <see cref="LotData.Id"/> inside <see cref="MicroBlockWorldState.CrewHomeBlockId"/>.
/// v1: deterministic order — spots list order ↔ home lots sorted by street frontage then size.
/// </summary>
public static class MicroBlockSpotLotBinder
{
    public static void BindSpotsToHomeBlockLots(CityData city)
    {
        for (int i = 0; i < MicroBlockWorldState.Spots.Count; i++)
            MicroBlockWorldState.Spots[i].AnchorLotId = -1;

        if (city == null || city.Lots == null || city.Lots.Count == 0)
            return;

        if (GameSessionState.SingleBlockSandboxEnabled
            && MicroBlockWorldState.Spots.Count == city.Lots.Count)
        {
            var all = new List<LotData>(city.Lots);
            all.Sort((a, b) =>
            {
                int c = a.BlockId.CompareTo(b.BlockId);
                return c != 0 ? c : a.Id.CompareTo(b.Id);
            });
            for (int i = 0; i < all.Count; i++)
                MicroBlockWorldState.Spots[i].AnchorLotId = all[i].Id;
            return;
        }

        int blockId = MicroBlockWorldState.CrewHomeBlockId;
        if (blockId < 0)
            return;

        var homeLots = new List<LotData>();
        for (int i = 0; i < city.Lots.Count; i++)
        {
            LotData lot = city.Lots[i];
            if (lot.BlockId == blockId)
                homeLots.Add(lot);
        }

        if (homeLots.Count == 0)
            return;

        homeLots.Sort(CompareHomeLotsForBinding);

        int n = Mathf.Min(MicroBlockWorldState.Spots.Count, homeLots.Count);
        for (int i = 0; i < n; i++)
            MicroBlockWorldState.Spots[i].AnchorLotId = homeLots[i].Id;
    }

    /// <summary>Home-block lots in binding order (for the Ops block map grid).</summary>
    public static List<LotData> GetHomeLotsSorted(CityData city)
    {
        var list = new List<LotData>();
        if (city == null || city.Lots == null || MicroBlockWorldState.CrewHomeBlockId < 0)
            return list;
        int blockId = MicroBlockWorldState.CrewHomeBlockId;
        for (int i = 0; i < city.Lots.Count; i++)
        {
            LotData lot = city.Lots[i];
            if (lot.BlockId == blockId)
                list.Add(lot);
        }

        list.Sort(CompareHomeLotsForBinding);
        return list;
    }

    static int CompareHomeLotsForBinding(LotData a, LotData b)
    {
        int c = b.TouchesRoad.CompareTo(a.TouchesRoad);
        if (c != 0)
            return c;
        c = b.AreaCells.CompareTo(a.AreaCells);
        if (c != 0)
            return c;
        return a.Id.CompareTo(b.Id);
    }
}
