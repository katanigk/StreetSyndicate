using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ActorTimeBudget
{
    public string ActorId;
    public int DayIndex;
    public float HoursUsedToday;
    public float HoursRestedToday;
    public float Fatigue; // 0..100
    public float Stress;  // 0..100
}

public static class ActorTimeBudgetRules
{
    // Shared world rule: police actors follow the same day-hour law as crime-world actors.
    public const float DailyHoursBudget = 24f;
    public const float MinRecommendedRestHours = 7f;

    public static bool CanScheduleAction(ActorTimeBudget budget, float actionHours, out string reason)
    {
        reason = string.Empty;
        if (budget == null)
        {
            reason = "Missing time budget.";
            return false;
        }
        if (actionHours <= 0f)
        {
            reason = "Action hours must be positive.";
            return false;
        }

        float remaining = Mathf.Max(0f, DailyHoursBudget - budget.HoursUsedToday - budget.HoursRestedToday);
        if (actionHours > remaining)
        {
            reason = "Not enough hours remaining in current day.";
            return false;
        }
        return true;
    }

    public static void ApplyAction(ActorTimeBudget budget, float actionHours, float stressGain = 6f, float fatigueGain = 8f)
    {
        if (budget == null || actionHours <= 0f)
            return;

        budget.HoursUsedToday = Mathf.Clamp(budget.HoursUsedToday + actionHours, 0f, DailyHoursBudget);
        float intensity = Mathf.Clamp01(actionHours / 8f);
        budget.Stress = Mathf.Clamp(budget.Stress + stressGain * intensity, 0f, 100f);
        budget.Fatigue = Mathf.Clamp(budget.Fatigue + fatigueGain * intensity, 0f, 100f);
    }

    public static void ApplyRest(ActorTimeBudget budget, float restHours)
    {
        if (budget == null || restHours <= 0f)
            return;

        budget.HoursRestedToday = Mathf.Clamp(budget.HoursRestedToday + restHours, 0f, DailyHoursBudget);
        float recovery = Mathf.Clamp01(restHours / 8f);
        budget.Stress = Mathf.Clamp(budget.Stress - 22f * recovery, 0f, 100f);
        budget.Fatigue = Mathf.Clamp(budget.Fatigue - 28f * recovery, 0f, 100f);
    }

    public static void BeginNewDay(ActorTimeBudget budget, int newDayIndex)
    {
        if (budget == null)
            return;
        budget.DayIndex = newDayIndex;
        budget.HoursUsedToday = 0f;
        budget.HoursRestedToday = 0f;
    }
}

public enum PoliceKnowledgeDomain
{
    Personnel,
    Cases,
    Intelligence,
    Evidence,
    InternalOversight,
    Operations,
    Command
}

public enum PoliceKnowledgeSensitivity
{
    Team,
    Department,
    Station,
    MultiStation,
    CityStrategic
}

[Flags]
public enum PoliceExposureFlags
{
    None = 0,
    RevealedByAssignment = 1 << 0,
    RevealedByLeak = 1 << 1,
    RevealedByCrossUnitTask = 1 << 2,
    RevealedByCommandBriefing = 1 << 3,
    RevealedByInvestigation = 1 << 4
}

[Serializable]
public struct PoliceViewerContext
{
    public string OfficerId;
    public PoliceRank Rank;
    public PoliceCoreRole Role;
    public string StationId;
    public string DepartmentId;
    public string TeamId;
}

[Serializable]
public struct PoliceKnowledgeRecordMeta
{
    public string RecordId;
    public PoliceKnowledgeDomain Domain;
    public PoliceKnowledgeSensitivity Sensitivity;
    public string OwningStationId;
    public string OwningDepartmentId;
    public string OwningTeamId;
}

public static class PoliceKnowledgeAccessResolver
{
    public static bool CanAccess(
        PoliceViewerContext viewer,
        PoliceKnowledgeRecordMeta record,
        PoliceExposureFlags exposureFlags)
    {
        // Global command can see all internal police data.
        if (viewer.Role == PoliceCoreRole.CityCommand || viewer.Rank >= PoliceRank.Commander)
            return true;

        // Explicit exposure channels can reveal otherwise restricted records.
        if ((exposureFlags & PoliceExposureFlags.RevealedByCommandBriefing) != 0)
            return true;
        if ((exposureFlags & PoliceExposureFlags.RevealedByInvestigation) != 0 &&
            viewer.Role == PoliceCoreRole.InternalOversightOfficer)
            return true;

        bool sameStation = string.Equals(viewer.StationId, record.OwningStationId, StringComparison.OrdinalIgnoreCase);
        bool sameDepartment = sameStation &&
                              string.Equals(viewer.DepartmentId, record.OwningDepartmentId, StringComparison.OrdinalIgnoreCase);
        bool sameTeam = sameDepartment &&
                        string.Equals(viewer.TeamId, record.OwningTeamId, StringComparison.OrdinalIgnoreCase);

        // Need-to-know baseline by sensitivity.
        switch (record.Sensitivity)
        {
            case PoliceKnowledgeSensitivity.Team:
                return sameTeam || HasExposure(exposureFlags);
            case PoliceKnowledgeSensitivity.Department:
                return sameDepartment || HasExposure(exposureFlags);
            case PoliceKnowledgeSensitivity.Station:
                if (!sameStation && !HasExposure(exposureFlags))
                    return false;
                // Junior officers do not automatically see all station data domains.
                if (viewer.Rank <= PoliceRank.SeniorConstable &&
                    (record.Domain == PoliceKnowledgeDomain.InternalOversight ||
                     record.Domain == PoliceKnowledgeDomain.Command))
                    return HasExposure(exposureFlags);
                return true;
            case PoliceKnowledgeSensitivity.MultiStation:
                if (viewer.Rank >= PoliceRank.Captain)
                    return true;
                return HasExposure(exposureFlags);
            case PoliceKnowledgeSensitivity.CityStrategic:
                if (viewer.Rank >= PoliceRank.Commander || viewer.Role == PoliceCoreRole.CityCommand)
                    return true;
                return (exposureFlags & PoliceExposureFlags.RevealedByCommandBriefing) != 0;
            default:
                return false;
        }
    }

    public static bool CanViewStationWindow(PoliceViewerContext viewer, string stationId)
    {
        if (viewer.Role == PoliceCoreRole.CityCommand || viewer.Rank >= PoliceRank.Commander)
            return true;
        if (string.Equals(viewer.StationId, stationId, StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }

    public static bool CanViewHqWindow(PoliceViewerContext viewer)
    {
        return viewer.Rank >= PoliceRank.Commander || viewer.Role == PoliceCoreRole.CityCommand;
    }

    private static bool HasExposure(PoliceExposureFlags flags)
    {
        return (flags & (PoliceExposureFlags.RevealedByAssignment |
                         PoliceExposureFlags.RevealedByLeak |
                         PoliceExposureFlags.RevealedByCrossUnitTask |
                         PoliceExposureFlags.RevealedByCommandBriefing |
                         PoliceExposureFlags.RevealedByInvestigation)) != 0;
    }
}

[Serializable]
public class PoliceInternalStateStore
{
    public Dictionary<string, ActorTimeBudget> TimeBudgetsByOfficerId = new Dictionary<string, ActorTimeBudget>();
    public Dictionary<string, PoliceExposureFlags> ExposureByRecordId = new Dictionary<string, PoliceExposureFlags>();

    public ActorTimeBudget GetOrCreateBudget(string officerId, int dayIndex)
    {
        if (string.IsNullOrWhiteSpace(officerId))
            return null;
        if (!TimeBudgetsByOfficerId.TryGetValue(officerId, out ActorTimeBudget budget))
        {
            budget = new ActorTimeBudget
            {
                ActorId = officerId,
                DayIndex = dayIndex,
                HoursUsedToday = 0f,
                HoursRestedToday = 0f,
                Fatigue = 0f,
                Stress = 0f
            };
            TimeBudgetsByOfficerId[officerId] = budget;
        }
        return budget;
    }

    public PoliceExposureFlags GetExposureForRecord(string recordId)
    {
        if (string.IsNullOrWhiteSpace(recordId))
            return PoliceExposureFlags.None;
        return ExposureByRecordId.TryGetValue(recordId, out PoliceExposureFlags f) ? f : PoliceExposureFlags.None;
    }

    public void AddExposure(string recordId, PoliceExposureFlags flags)
    {
        if (string.IsNullOrWhiteSpace(recordId) || flags == PoliceExposureFlags.None)
            return;
        PoliceExposureFlags cur = GetExposureForRecord(recordId);
        ExposureByRecordId[recordId] = cur | flags;
    }
}
