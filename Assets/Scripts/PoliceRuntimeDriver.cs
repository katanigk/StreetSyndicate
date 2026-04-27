using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>End-of-day hook for the police engine: new day time budgets, shift build, resolvers' decay pass.</summary>
public static class PoliceRuntimeDriver
{
    public static void OnCalendarDayAdvanced(int previousDay, int newDay)
    {
        _ = previousDay;
        if (newDay < 1)
            return;
        if (!PoliceWorldState.IsBootstrapped)
        {
            PoliceWorldState.EnsureBootstrappedForSession(GameSessionState.CityMapSeed, newDay);
        }
        if (!PoliceWorldState.IsBootstrapped)
            return;

        foreach (var kv in PoliceWorldState.InternalStore.TimeBudgetsByOfficerId)
        {
            if (kv.Value == null)
                continue;
            ActorTimeBudgetRules.BeginNewDay(kv.Value, newDay);
        }
        PoliceWorldState.ActiveShiftPlans.Clear();
        PoliceShiftScheduleBuilder.BuildShiftsForDay(
            PoliceWorldState.Organization,
            PoliceWorldState.Officers,
            PoliceWorldState.OfficerCareers,
            newDay,
            PoliceWorldState.InternalStore,
            PoliceWorldState.ActiveShiftPlans);
        PoliceWorldState.ScheduleLastBuiltForDay = newDay;
        for (int p = 0; p < PoliceWorldState.ActiveShiftPlans.Count; p++)
        {
            PoliceShiftScheduleBuilder.ApplyShiftHoursForPlan(PoliceWorldState.ActiveShiftPlans[p], newDay, PoliceWorldState.InternalStore);
        }
        PoliceLogisticsSystem.EnsureBootstrapped(PoliceWorldState.Organization, newDay);
        PoliceLogisticsSystem.AdvanceDay(newDay);
        PoliceStationLivingSystem.EnsureBootstrapped(newDay);
        PoliceStationLivingSystem.AdvanceDay(newDay);
        for (int i = 0; i < PoliceWorldState.CaseFiles.Count; i++)
        {
            CaseFile c = PoliceWorldState.CaseFiles[i];
            if (c == null)
                continue;
            if (c.status == PoliceCaseStatus.Closed || c.status == PoliceCaseStatus.Archived)
                continue;
            if (c.status == PoliceCaseStatus.Active || c.status == PoliceCaseStatus.Operational)
                continue;
            CaseDecayResolver.ApplyDecay(c, 1);
        }
        for (int i = 0; i < PoliceWorldState.IntelItems.Count; i++)
        {
            IntelItem item = PoliceWorldState.IntelItems[i];
            if (item == null)
                continue;
            IntelDecayResolver.ApplyDecayPerTurn(item, 1);
        }
        for (int i = 0; i < PoliceWorldState.SuspicionRecords.Count; i++)
        {
            if (PoliceWorldState.SuspicionRecords[i] != null)
                PoliceReasonableSuspicion.ApplyDecayPerTurn(PoliceWorldState.SuspicionRecords[i], 1);
        }
    }
}
