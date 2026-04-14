using FamilyBusiness.CityGen.Data;
using UnityEngine;

/// <summary>
/// Per-operation time budget within one calendar day (one End Turn = <see cref="HoursPerGameday"/> h).
/// Travel uses Manhattan distance between <see cref="BlockData"/> centroids in plan cells; vehicle raises effective cells/hour.
/// Execution time scales down with deployable squad size (parallel work on site).
/// Travel uses vehicle vs. foot seasonal rules from <see cref="SeasonGameplayModifiers"/>; execution time uses execution multipliers only.
/// </summary>
public static class OperationTimingSystem
{
    /// <summary>One turn = one calendar day; hour budget for mission load display.</summary>
    public const float HoursPerGameday = 24f;

    /// <summary>Foot / transit baseline across the abstract city grid.</summary>
    public const float FootCellsPerHour = 12f;

    /// <summary>Crew vehicle (or ride) baseline.</summary>
    public const float VehicleCellsPerHour = 42f;

    /// <summary>Minimum logistics when home and target are the same block.</summary>
    public const float SameBlockTravelHoursMin = 0.35f;

    public static float BaseExecutionHours(OperationType op)
    {
        switch (op)
        {
            case OperationType.Scout:
                return 18f;
            case OperationType.Surveillance:
                return 36f;
            case OperationType.Collect:
                return 14f;
            default:
                return 20f;
        }
    }

    /// <summary>Larger squad → less wall-clock work (lookouts, lifts, parallel watch).</summary>
    public static float SquadExecutionWorkFactor(int squadSize)
    {
        int n = Mathf.Clamp(squadSize, 1, 8);
        return 1f / Mathf.Sqrt(n);
    }

    public static Vector2? GetBlockCentroid(CityData city, int blockId)
    {
        if (city == null || blockId < 0)
            return null;
        for (int i = 0; i < city.Blocks.Count; i++)
        {
            BlockData b = city.Blocks[i];
            if (b.Id != blockId)
                continue;
            return (b.Min + b.Max) * 0.5f;
        }

        return null;
    }

    public static float TravelHoursBetweenBlocks(CityData city, int fromBlockId, int toBlockId, bool hasVehicle)
    {
        if (fromBlockId < 0 || toBlockId < 0)
            return SameBlockTravelHoursMin;
        if (fromBlockId == toBlockId)
            return SameBlockTravelHoursMin;

        Vector2? a = GetBlockCentroid(city, fromBlockId);
        Vector2? b = GetBlockCentroid(city, toBlockId);
        if (!a.HasValue || !b.HasValue)
            return Mathf.Max(SameBlockTravelHoursMin, 1.5f);

        float manhattan = Mathf.Abs(a.Value.x - b.Value.x) + Mathf.Abs(a.Value.y - b.Value.y);
        float speed = hasVehicle ? VehicleCellsPerHour : FootCellsPerHour;
        float h = manhattan / Mathf.Max(0.5f, speed);
        return Mathf.Max(SameBlockTravelHoursMin, h);
    }

    public static float ExecutionHours(OperationType op, int squadSize)
    {
        return BaseExecutionHours(op) * SquadExecutionWorkFactor(squadSize);
    }

    public static void EstimateMissionHours(
        CityData city,
        int homeBlockId,
        int targetBlockId,
        OperationType op,
        int squadSize,
        bool hasVehicle,
        GameSeason season,
        in WeatherSnapshot weather,
        out float travelHours,
        out float executionHours)
    {
        float travelBase = TravelHoursBetweenBlocks(city, homeBlockId, targetBlockId, hasVehicle);
        float execBase = ExecutionHours(op, squadSize);
        float tMul = hasVehicle
            ? SeasonGameplayModifiers.GetVehicleTravelDurationMultiplier(season)
            : SeasonGameplayModifiers.GetFootTravelDurationMultiplier(season);
        float eMul = SeasonGameplayModifiers.GetExecutionDurationMultiplier(season);
        float ws = weather.HasGameplayEffects ? Mathf.Clamp01(weather.Severity01) : 0f;
        float weatherDelay = 1f + ws * 0.12f;
        travelHours = travelBase * tMul * weatherDelay;
        executionHours = execBase * eMul * (1f + ws * 0.08f);

        if (!hasVehicle && weather.HeatWave && weather.Severity01 >= 0.44f)
        {
            travelHours *= 1.26f;
            executionHours *= 1.10f;
        }
    }

    public static int ResolveTargetBlockIdForSpot(CityData city, MicroBlockSpotRuntime spot, int fallbackBlockId)
    {
        if (spot == null || city == null || spot.AnchorLotId < 0)
            return fallbackBlockId;
        for (int i = 0; i < city.Lots.Count; i++)
        {
            LotData lot = city.Lots[i];
            if (lot.Id == spot.AnchorLotId)
                return lot.BlockId;
        }

        return fallbackBlockId;
    }

    /// <summary>Lead plus up to four other <see cref="CharacterStatus.Available"/> members.</summary>
    public static int ComputeDeployableSquadSize(int leadMemberIndex)
    {
        if (PersonnelRegistry.Members == null || PersonnelRegistry.Members.Count == 0)
            return 1;
        leadMemberIndex = Mathf.Clamp(leadMemberIndex, 0, PersonnelRegistry.Members.Count - 1);

        int extras = 0;
        const int maxExtras = 4;
        for (int i = 0; i < PersonnelRegistry.Members.Count; i++)
        {
            if (i == leadMemberIndex)
                continue;
            CrewMember m = PersonnelRegistry.Members[i];
            if (m == null)
                continue;
            CharacterStatus st = m.GetResolvedStatus();
            if (st != CharacterStatus.Available)
                continue;
            extras++;
            if (extras >= maxExtras)
                break;
        }

        return 1 + extras;
    }

    public static string FormatEstimatesLine(float travelHours, float executionHours, int squadSize, bool hasVehicle, GameSeason season, in WeatherSnapshot weather)
    {
        float total = travelHours + executionHours;
        float dayPct = total / HoursPerGameday * 100f;
        string mode = hasVehicle ? "car" : "foot";
        string seasonEn = GameCalendarSystem.GetSeasonNameEn(season);
        string slip = string.Empty;
        if (!hasVehicle)
        {
            float slipH = SeasonGameplayModifiers.GetFootSlipHazard(season);
            if (slipH >= 0.12f)
                slip = " · slip " + (slipH < 0.38f ? "low" : slipH < 0.49f ? "med" : "high");
        }

        string wx = string.Empty;
        if (weather.HasGameplayEffects)
        {
            string line = GameWeatherResolver.BuildHudWeatherLine(weather);
            if (!string.IsNullOrEmpty(line))
                wx = " · " + line;
        }

        return "  <size=92%><color=#a8c4d8>Travel ~" + FormatHoursShort(travelHours)
               + " · Work ~" + FormatHoursShort(executionHours)
               + " · Total ~" + FormatHoursShort(total)
               + " (~" + dayPct.ToString("0") + "% day) · squad " + squadSize + " · " + mode
               + " · " + seasonEn + slip + wx + "</color></size>";
    }

    static string FormatHoursShort(float h)
    {
        if (h < 24f)
            return h.ToString("0.#") + "h";
        float d = h / 24f;
        return d.ToString("0.#") + "d";
    }
}
