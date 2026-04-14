using System;
using System.Collections.Generic;

/// <summary>
/// Single poor block, separate from full-city CityGen until we merge. Reset on New Game; future: save delta in <see cref="SaveGameData"/>.
/// </summary>
public static class MicroBlockWorldState
{
    public static readonly List<MicroBlockSpotRuntime> Spots = new List<MicroBlockSpotRuntime>(24);

    /// <summary>Rent in whole dollars per in-game week (planning day cycle).</summary>
    public static int CrewWeeklyRentUsd = 8;

    /// <summary>Rent prepaid through this planning day (inclusive). Next day may owe.</summary>
    public static int CrewRentPrepaidThroughDay = 1;

    public static string LandlordSpotStableId = "spot_rooming_landlord";
    public static string LandlordDisplayName = "Mrs. O'Brien";

    /// <summary>CityGen <see cref="FamilyBusiness.CityGen.Data.BlockData.Id"/> for the crew's home micro-block footprint; -1 until city exists.</summary>
    public static int CrewHomeBlockId = -1;

    /// <summary>
    /// Sandbox macro map: which coarse zone each <see cref="FamilyBusiness.CityGen.Data.BlockData.Id"/> represents (one Residential = crew home block).
    /// Filled on new game; used by <see cref="OpsBigMapLotZoneResolver"/>.
    /// </summary>
    public static readonly Dictionary<int, OpsBigMapLotZoneResolver.ZoneKind> SandboxMacroZoneByBlockId =
        new Dictionary<int, OpsBigMapLotZoneResolver.ZoneKind>(8);

    public static bool TryGetSandboxMacroZone(int blockId, out OpsBigMapLotZoneResolver.ZoneKind zone) =>
        SandboxMacroZoneByBlockId.TryGetValue(blockId, out zone);

    public static void Clear()
    {
        Spots.Clear();
        MicroBlockKnowledgeStore.Clear();
        CrewWeeklyRentUsd = 8;
        CrewRentPrepaidThroughDay = 1;
        LandlordSpotStableId = "spot_rooming_landlord";
        LandlordDisplayName = "Mrs. O'Brien";
        CrewHomeBlockId = -1;
        SandboxMacroZoneByBlockId.Clear();
    }

    /// <summary>After advancing <see cref="GameSessionState.CurrentDay"/>, rent may be due.</summary>
    public static bool IsRentOverdueForCurrentDay()
    {
        return GameSessionState.CurrentDay > CrewRentPrepaidThroughDay;
    }

    public static void PayRentWeekInAdvanceFromSessionCash()
    {
        int cash = GameSessionState.CrewCash;
        if (cash < CrewWeeklyRentUsd)
            return;
        GameSessionState.CrewCash = cash - CrewWeeklyRentUsd;
        CrewRentPrepaidThroughDay = GameSessionState.CurrentDay + 7;
    }

    public static MicroBlockSpotRuntime FindById(string stableId)
    {
        for (int i = 0; i < Spots.Count; i++)
        {
            if (Spots[i] != null && Spots[i].StableId == stableId)
                return Spots[i];
        }

        return null;
    }

    public static MicroBlockSpotRuntime FindSpotByAnchorLotId(int lotId)
    {
        if (lotId < 0)
            return null;
        for (int i = 0; i < Spots.Count; i++)
        {
            MicroBlockSpotRuntime s = Spots[i];
            if (s != null && s.AnchorLotId == lotId)
                return s;
        }

        return null;
    }
}
