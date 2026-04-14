using System.Collections.Generic;
using FamilyBusiness.CityGen.Data;

/// <summary>
/// Picks one <see cref="BlockData"/> as the crew's "home block" on the full city map (deterministic from generated city).
/// </summary>
public static class MicroBlockAnchorResolver
{
    public static void EnsureCrewHomeBlockAnchored(CityData city)
    {
        if (city == null || city.Blocks == null || city.Blocks.Count == 0 || city.Lots == null || city.Lots.Count == 0)
        {
            MicroBlockWorldState.CrewHomeBlockId = -1;
            return;
        }

        int keep = MicroBlockWorldState.CrewHomeBlockId;
        if (keep >= 0 && BlockExists(city, keep) && CountLotsInBlock(city, keep) > 0)
            return;

        MicroBlockWorldState.CrewHomeBlockId = PickHomeBlockId(city);
    }

    static bool BlockExists(CityData city, int blockId)
    {
        for (int i = 0; i < city.Blocks.Count; i++)
        {
            if (city.Blocks[i].Id == blockId)
                return true;
        }

        return false;
    }

    static int CountLotsInBlock(CityData city, int blockId)
    {
        int n = 0;
        for (int i = 0; i < city.Lots.Count; i++)
        {
            if (city.Lots[i].BlockId == blockId)
                n++;
        }

        return n;
    }

    static int PickHomeBlockId(CityData city)
    {
        var counts = new Dictionary<int, int>(city.Blocks.Count);
        for (int i = 0; i < city.Lots.Count; i++)
        {
            int bid = city.Lots[i].BlockId;
            counts[bid] = counts.TryGetValue(bid, out int c) ? c + 1 : 1;
        }

        int bestId = -1;
        int bestTier = int.MinValue;
        int bestLots = -1;

        for (int i = 0; i < city.Blocks.Count; i++)
        {
            BlockData b = city.Blocks[i];
            if (!counts.TryGetValue(b.Id, out int n) || n <= 0)
                continue;

            int tier = DistrictTier(b.DistrictKind);
            if (tier > bestTier ||
                (tier == bestTier && n > bestLots) ||
                (tier == bestTier && n == bestLots && (bestId < 0 || b.Id < bestId)))
            {
                bestTier = tier;
                bestLots = n;
                bestId = b.Id;
            }
        }

        return bestId;
    }

    static int DistrictTier(DistrictKind k)
    {
        return k switch
        {
            DistrictKind.WorkingClass => 100,
            DistrictKind.Residential => 80,
            DistrictKind.Industrial => 70,
            DistrictKind.DowntownCommercial => 50,
            DistrictKind.DocksPort => 50,
            DistrictKind.FringeOuterEdge => 40,
            DistrictKind.Wealthy => 35,
            _ => 30
        };
    }
}
