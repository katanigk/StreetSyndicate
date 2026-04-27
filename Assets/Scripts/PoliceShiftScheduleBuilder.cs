using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>One 8h block in a 24/7 station rotation (simplified: three blocks per calendar day).</summary>
[Serializable]
public class PoliceShiftBlock
{
    public int blockIndex; // 0=night, 1=day, 2=swing
    public int hourStart;  // 0-23
    public int hourEnd;    // exclusive, wraps
    public List<string> assignedOfficerIds = new List<string>();
}

[Serializable]
public class PoliceDayShiftPlan
{
    public int dayIndex;
    public string stationId;
    public List<PoliceShiftBlock> blocks = new List<PoliceShiftBlock>();
}

/// <summary>Builds 24/7 shift coverage: every block lists assigned officers; applies scheduled hours to <see cref="ActorTimeBudget"/>.</summary>
public static class PoliceShiftScheduleBuilder
{
    public const int BlockCount = 3;
    public const float HoursPerBlock = 8f;

    public static void BuildShiftsForDay(
        PoliceHeadquartersProfile hq,
        List<OfficerProfile> officers,
        List<OfficerCareerProfile> careers,
        int dayIndex,
        PoliceInternalStateStore store,
        List<PoliceDayShiftPlan> outPlans)
    {
        outPlans.Clear();
        if (hq == null || officers == null || store == null)
            return;
        for (int s = 0; s < hq.Stations.Count; s++)
        {
            PoliceStationProfile st = hq.Stations[s];
            if (st == null)
                continue;
            var plan = new PoliceDayShiftPlan { dayIndex = dayIndex, stationId = st.StationId };
            var activeAtStation = new List<OfficerProfile>();
            for (int o = 0; o < officers.Count; o++)
            {
                OfficerProfile op = officers[o];
                if (op == null || !string.Equals(op.StationId, st.StationId, StringComparison.OrdinalIgnoreCase))
                    continue;
                OfficerCareerProfile cr = FindCareer(careers, op.OfficerId);
                if (cr == null)
                    continue;
                if (cr.careerStatus == OfficerCareerStatus.Dead || cr.careerStatus == OfficerCareerStatus.Retired ||
                    cr.careerStatus == OfficerCareerStatus.Suspended)
                    continue;
                if (cr.injuryStatus == InjuryStatus.Severe || cr.injuryStatus == InjuryStatus.Disabled)
                    continue;
                activeAtStation.Add(op);
            }
            if (activeAtStation.Count == 0)
            {
                outPlans.Add(plan);
                continue;
            }
            for (int b = 0; b < BlockCount; b++)
            {
                int startHour = b * 8;
                int endHour = b == BlockCount - 1 ? 24 : (b + 1) * 8;
                var ph = new PoliceShiftBlock
                {
                    blockIndex = b,
                    hourStart = startHour,
                    hourEnd = endHour
                };
                for (int i = 0; i < activeAtStation.Count; i++)
                {
                    if (i % BlockCount == b)
                        ph.assignedOfficerIds.Add(activeAtStation[i].OfficerId);
                }
                if (ph.assignedOfficerIds.Count == 0)
                {
                    int pick = (b + dayIndex) % activeAtStation.Count;
                    ph.assignedOfficerIds.Add(activeAtStation[pick].OfficerId);
                }
                plan.blocks.Add(ph);
            }
            outPlans.Add(plan);
        }
    }

    static OfficerCareerProfile FindCareer(List<OfficerCareerProfile> careers, string id)
    {
        if (careers == null)
            return null;
        for (int i = 0; i < careers.Count; i++)
        {
            var c = careers[i];
            if (c != null && string.Equals(c.officerId, id, StringComparison.OrdinalIgnoreCase))
                return c;
        }
        return null;
    }

    /// <summary>Consumes <see cref="ActorTimeBudgetRules.DailyHoursBudget"/> hours per assigned block after <see cref="ActorTimeBudgetRules.BeginNewDay"/>.</summary>
    public static void ApplyShiftHoursForPlan(PoliceDayShiftPlan plan, int dayIndex, PoliceInternalStateStore store)
    {
        if (plan == null || store == null)
            return;
        for (int b = 0; b < plan.blocks.Count; b++)
        {
            PoliceShiftBlock ph = plan.blocks[b];
            if (ph == null)
                continue;
            float h = ph.hourEnd - ph.hourStart;
            if (h <= 0f)
                h = HoursPerBlock;
            h = HoursPerBlock;
            for (int i = 0; i < ph.assignedOfficerIds.Count; i++)
            {
                string oid = ph.assignedOfficerIds[i];
                ActorTimeBudget ab = store.GetOrCreateBudget(oid, dayIndex);
                if (ab == null)
                    continue;
                if (!ActorTimeBudgetRules.CanScheduleAction(ab, h, out _))
                    h = Mathf.Max(0.5f, ActorTimeBudgetRules.DailyHoursBudget - ab.HoursUsedToday - ab.HoursRestedToday);
                ActorTimeBudgetRules.ApplyAction(ab, h, stressGain: 5.5f, fatigueGain: 7f);
            }
        }
    }
}
