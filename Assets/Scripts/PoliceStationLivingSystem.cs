using System;
using System.Collections.Generic;
using UnityEngine;

public enum PoliceStationStance
{
    Neutral = 0,
    Hostile = 1,
    Cooperative = 2,
    Fearful = 3,
    Penetrated = 4,
    CorruptionProne = 5,
    UnderFederalPressure = 6
}

public enum PoliceRelationTargetType
{
    PlayerOrganization = 0,
    CrimeOrganization = 1,
    FederalBureau = 2,
    CityHall = 3,
    Public = 4
}

[Serializable]
public class PoliceStationRelation
{
    public string relationId;
    public string stationId;
    public PoliceRelationTargetType targetType;
    public string targetId;
    public int relationScore; // -100..100
    public PoliceStationStance stance;
    public int confidence; // 0..100
    public int pressure; // 0..100
    public int corruptionSignal; // 0..100
    public int updatedDay;
}

[Serializable]
public class PoliceStationLivingState
{
    public string stationId;
    public string commanderOfficerId;
    public int operationalTempo; // 0..100
    public int activeOfficerCount;
    public int scheduledShiftCount;
    public int localCaseLoad; // 0..100
    public int localEquipmentStress; // 0..100
    public int localDiscipline; // 0..100
    public int corruptionPressure; // 0..100
    public int publicPressure; // 0..100
    public int federalPressure; // 0..100
    public int mistakesRisk; // 0..100
    public int burnoutTrend; // -100..100
    public int lastUpdatedDay;
}

public static class PoliceStationLivingSystem
{
    public static void EnsureBootstrapped(int dayIndex)
    {
        if (PoliceWorldState.Organization == null || PoliceWorldState.Organization.Stations == null)
            return;

        for (int i = 0; i < PoliceWorldState.Organization.Stations.Count; i++)
        {
            PoliceStationProfile station = PoliceWorldState.Organization.Stations[i];
            if (station == null || string.IsNullOrWhiteSpace(station.StationId))
                continue;
            station.OperationalTempo = Mathf.Clamp(station.OperationalTempo <= 0 ? 45 : station.OperationalTempo, 0, 100);
            EnsureStationState(station, dayIndex);
            EnsureBaselineRelations(station.StationId, dayIndex);
        }
    }

    public static void AdvanceDay(int dayIndex)
    {
        for (int i = 0; i < PoliceWorldState.StationStates.Count; i++)
        {
            PoliceStationLivingState state = PoliceWorldState.StationStates[i];
            if (state == null)
                continue;

            PoliceStationProfile station = FindStation(state.stationId);
            if (station == null)
                continue;

            state.activeOfficerCount = CountOfficersForStation(state.stationId);
            state.scheduledShiftCount = CountShiftsForStation(state.stationId);
            state.localCaseLoad = ComputeCaseLoad(state.stationId);
            state.localEquipmentStress = ComputeEquipmentStress(station);
            state.corruptionPressure = station.Corruption;
            state.publicPressure = Mathf.Clamp(GameSessionState.PolicePressureDisplayValue(), 0, 100);
            state.federalPressure = Mathf.Clamp(BureauWorldState.currentFederalAggression0to100, 0, 100);

            int targetTempo = Mathf.Clamp(
                35 + state.localCaseLoad / 2 + state.publicPressure / 4 + state.federalPressure / 5,
                20, 95);
            state.operationalTempo = Mathf.Clamp(Mathf.RoundToInt(state.operationalTempo * 0.55f + targetTempo * 0.45f), 0, 100);
            station.OperationalTempo = state.operationalTempo;
            state.mistakesRisk = Mathf.Clamp(
                Mathf.RoundToInt(state.localCaseLoad * 0.25f + state.localEquipmentStress * 0.2f + state.operationalTempo * 0.25f + state.corruptionPressure * 0.15f),
                0, 100);
            state.localDiscipline = Mathf.Clamp(
                100 - Mathf.RoundToInt(state.corruptionPressure * 0.4f + state.mistakesRisk * 0.35f),
                0, 100);
            state.burnoutTrend = Mathf.Clamp(state.operationalTempo - 50 + Mathf.RoundToInt(state.localCaseLoad * 0.15f), -100, 100);
            state.lastUpdatedDay = dayIndex;

            station.Workload = state.localCaseLoad;
            station.EquipmentReadiness = Mathf.Clamp(100 - state.localEquipmentStress, 0, 100);
            station.Professionalism = Mathf.Clamp((state.localDiscipline + station.IntelligenceReadiness) / 2, 0, 100);
            station.Manpower = Mathf.Clamp(Mathf.RoundToInt(state.activeOfficerCount * 100f / Mathf.Max(12, PoliceWorldState.Officers.Count)), 0, 100);

            UpdateStationRelations(state, dayIndex);
        }
    }

    private static void EnsureStationState(PoliceStationProfile station, int dayIndex)
    {
        for (int i = 0; i < PoliceWorldState.StationStates.Count; i++)
        {
            if (PoliceWorldState.StationStates[i] != null &&
                string.Equals(PoliceWorldState.StationStates[i].stationId, station.StationId, StringComparison.OrdinalIgnoreCase))
                return;
        }

        PoliceWorldState.StationStates.Add(new PoliceStationLivingState
        {
            stationId = station.StationId,
            commanderOfficerId = station.CommanderOfficerId,
            operationalTempo = Mathf.Clamp(station.OperationalTempo <= 0 ? 45 : station.OperationalTempo, 0, 100),
            activeOfficerCount = CountOfficersForStation(station.StationId),
            scheduledShiftCount = 0,
            localCaseLoad = Mathf.Clamp(station.Workload, 0, 100),
            localEquipmentStress = Mathf.Clamp(100 - station.EquipmentReadiness, 0, 100),
            localDiscipline = Mathf.Clamp(100 - station.Corruption, 0, 100),
            corruptionPressure = Mathf.Clamp(station.Corruption, 0, 100),
            publicPressure = Mathf.Clamp(GameSessionState.PolicePressureDisplayValue(), 0, 100),
            federalPressure = Mathf.Clamp(BureauWorldState.currentFederalAggression0to100, 0, 100),
            mistakesRisk = 25,
            burnoutTrend = 0,
            lastUpdatedDay = dayIndex
        });
    }

    private static void EnsureBaselineRelations(string stationId, int dayIndex)
    {
        EnsureRelation(stationId, PoliceRelationTargetType.PlayerOrganization, "player_org", dayIndex);
        EnsureRelation(stationId, PoliceRelationTargetType.FederalBureau, "federal_bureau", dayIndex);
        EnsureRelation(stationId, PoliceRelationTargetType.CityHall, "city_hall", dayIndex);
        EnsureRelation(stationId, PoliceRelationTargetType.Public, "public", dayIndex);
    }

    private static void EnsureRelation(string stationId, PoliceRelationTargetType targetType, string targetId, int dayIndex)
    {
        for (int i = 0; i < PoliceWorldState.StationRelations.Count; i++)
        {
            PoliceStationRelation r = PoliceWorldState.StationRelations[i];
            if (r == null) continue;
            if (!string.Equals(r.stationId, stationId, StringComparison.OrdinalIgnoreCase)) continue;
            if (r.targetType != targetType) continue;
            if (string.Equals(r.targetId, targetId, StringComparison.OrdinalIgnoreCase)) return;
        }

        PoliceWorldState.StationRelations.Add(new PoliceStationRelation
        {
            relationId = "st_rel_" + Guid.NewGuid().ToString("N"),
            stationId = stationId,
            targetType = targetType,
            targetId = targetId,
            relationScore = targetType == PoliceRelationTargetType.Public ? 10 : 0,
            stance = PoliceStationStance.Neutral,
            confidence = 50,
            pressure = 10,
            corruptionSignal = 0,
            updatedDay = dayIndex
        });
    }

    private static void UpdateStationRelations(PoliceStationLivingState state, int dayIndex)
    {
        for (int i = 0; i < PoliceWorldState.StationRelations.Count; i++)
        {
            PoliceStationRelation rel = PoliceWorldState.StationRelations[i];
            if (rel == null || !string.Equals(rel.stationId, state.stationId, StringComparison.OrdinalIgnoreCase))
                continue;

            int pressure = state.publicPressure / 2 + state.federalPressure / 3 + state.localCaseLoad / 3;
            rel.pressure = Mathf.Clamp(pressure, 0, 100);
            rel.corruptionSignal = Mathf.Clamp(state.corruptionPressure, 0, 100);

            int drift = 0;
            if (rel.targetType == PoliceRelationTargetType.PlayerOrganization)
                drift = -Mathf.RoundToInt(state.operationalTempo * 0.1f) - Mathf.RoundToInt(state.publicPressure * 0.05f);
            else if (rel.targetType == PoliceRelationTargetType.FederalBureau)
                drift = -Mathf.RoundToInt(state.corruptionPressure * 0.08f) - Mathf.RoundToInt(state.federalPressure * 0.1f);
            else if (rel.targetType == PoliceRelationTargetType.Public)
                drift = Mathf.RoundToInt((state.localDiscipline - state.mistakesRisk) * 0.06f);
            else if (rel.targetType == PoliceRelationTargetType.CityHall)
                drift = -Mathf.RoundToInt(state.publicPressure * 0.04f);

            rel.relationScore = Mathf.Clamp(rel.relationScore + drift, -100, 100);
            rel.stance = ResolveStance(rel, state);
            rel.updatedDay = dayIndex;
        }
    }

    private static PoliceStationStance ResolveStance(PoliceStationRelation rel, PoliceStationLivingState state)
    {
        if (state.federalPressure >= 75 && rel.targetType == PoliceRelationTargetType.FederalBureau)
            return PoliceStationStance.UnderFederalPressure;
        if (state.corruptionPressure >= 70)
            return PoliceStationStance.CorruptionProne;
        if (rel.targetType == PoliceRelationTargetType.PlayerOrganization && rel.relationScore <= -45)
            return PoliceStationStance.Hostile;
        if (rel.targetType == PoliceRelationTargetType.PlayerOrganization && rel.relationScore >= 35)
            return PoliceStationStance.Penetrated;
        if (rel.targetType == PoliceRelationTargetType.Public && rel.relationScore <= -25)
            return PoliceStationStance.Fearful;
        if (rel.relationScore >= 25)
            return PoliceStationStance.Cooperative;
        return PoliceStationStance.Neutral;
    }

    private static PoliceStationProfile FindStation(string stationId)
    {
        if (PoliceWorldState.Organization == null || PoliceWorldState.Organization.Stations == null)
            return null;
        for (int i = 0; i < PoliceWorldState.Organization.Stations.Count; i++)
        {
            PoliceStationProfile st = PoliceWorldState.Organization.Stations[i];
            if (st != null && string.Equals(st.StationId, stationId, StringComparison.OrdinalIgnoreCase))
                return st;
        }
        return null;
    }

    private static int CountOfficersForStation(string stationId)
    {
        int count = 0;
        for (int i = 0; i < PoliceWorldState.Officers.Count; i++)
        {
            OfficerProfile o = PoliceWorldState.Officers[i];
            if (o != null && string.Equals(o.StationId, stationId, StringComparison.OrdinalIgnoreCase))
                count++;
        }
        return count;
    }

    private static int CountShiftsForStation(string stationId)
    {
        int count = 0;
        for (int i = 0; i < PoliceWorldState.ActiveShiftPlans.Count; i++)
        {
            PoliceDayShiftPlan p = PoliceWorldState.ActiveShiftPlans[i];
            if (p == null || p.blocks == null)
                continue;
            for (int b = 0; b < p.blocks.Count; b++)
            {
                PoliceShiftBlock block = p.blocks[b];
                if (block == null || string.IsNullOrWhiteSpace(block.officerId))
                    continue;
                OfficerProfile o = PoliceWorldState.GetOfficer(block.officerId);
                if (o != null && string.Equals(o.StationId, stationId, StringComparison.OrdinalIgnoreCase))
                    count++;
            }
        }
        return count;
    }

    private static int ComputeCaseLoad(string stationId)
    {
        int total = 0;
        int activeCases = 0;
        for (int i = 0; i < PoliceWorldState.CaseFiles.Count; i++)
        {
            CaseFile c = PoliceWorldState.CaseFiles[i];
            if (c == null || !string.Equals(c.owningStationId, stationId, StringComparison.OrdinalIgnoreCase))
                continue;
            activeCases++;
            int weight = c.status == PoliceCaseStatus.Operational ? 22 :
                (c.status == PoliceCaseStatus.Active ? 16 : 8);
            total += weight;
        }
        if (activeCases == 0)
            return 20;
        return Mathf.Clamp(total, 0, 100);
    }

    private static int ComputeEquipmentStress(PoliceStationProfile station)
    {
        if (station == null)
            return 40;
        int stress = 100 - station.EquipmentReadiness;
        stress += Mathf.RoundToInt(station.Workload * 0.25f);
        return Mathf.Clamp(stress, 0, 100);
    }
}
