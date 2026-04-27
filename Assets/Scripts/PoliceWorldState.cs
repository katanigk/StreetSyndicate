using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Authoritative in-memory state for the police simulation (cases, intel, roster, time, shifts).
/// Serialized via <see cref="CaptureJson"/> for saves. Player-facing text stays out of the UI layer here.
/// </summary>
public static class PoliceWorldState
{
    public const int SnapshotFormatVersion = 2;

    public static bool IsBootstrapped;
    public static int BootstrapSeed;
    public static int ScheduleLastBuiltForDay = -1;

    public static PoliceHeadquartersProfile Organization;

    public static List<OfficerProfile> Officers = new List<OfficerProfile>();
    public static List<OfficerCareerProfile> OfficerCareers = new List<OfficerCareerProfile>();
    public static List<OfficerCareerEvent> OfficerCareerEvents = new List<OfficerCareerEvent>();

    public static List<CaseFile> CaseFiles = new List<CaseFile>();
    public static List<IntelItem> IntelItems = new List<IntelItem>();
    public static List<SourceProfile> IntelSources = new List<SourceProfile>();
    public static List<IntelligenceAssessment> IntelAssessments = new List<IntelligenceAssessment>();
    public static List<EvidenceItem> EvidenceItems = new List<EvidenceItem>();
    public static List<LogisticsInventoryItem> LogInventory = new List<LogisticsInventoryItem>();
    public static List<LogisticsStorage> LogStorages = new List<LogisticsStorage>();
    public static List<LogisticsRoute> LogRoutes = new List<LogisticsRoute>();
    public static List<LogisticsShipment> LogShipments = new List<LogisticsShipment>();
    public static List<LogisticsTransportUnit> LogTransportUnits = new List<LogisticsTransportUnit>();
    public static List<LogisticsSupplyNeed> LogSupplyNeeds = new List<LogisticsSupplyNeed>();
    public static List<LogisticsIncident> LogIncidents = new List<LogisticsIncident>();
    public static List<InternalReviewCase> InternalReviews = new List<InternalReviewCase>();
    public static List<InternalRiskProfile> InternalRiskByOfficer = new List<InternalRiskProfile>();
    public static List<SuspicionRecord> SuspicionRecords = new List<SuspicionRecord>();
    public static List<PoliceRecordEntry> Records = new List<PoliceRecordEntry>();
    public static List<PoliceStationLivingState> StationStates = new List<PoliceStationLivingState>();
    public static List<PoliceStationRelation> StationRelations = new List<PoliceStationRelation>();

    public static PoliceInternalStateStore InternalStore = new PoliceInternalStateStore();
    public static List<PoliceDayShiftPlan> ActiveShiftPlans = new List<PoliceDayShiftPlan>();

    public static void ClearAll()
    {
        IsBootstrapped = false;
        BootstrapSeed = 0;
        ScheduleLastBuiltForDay = -1;
        Organization = null;
        Officers.Clear();
        OfficerCareers.Clear();
        OfficerCareerEvents.Clear();
        CaseFiles.Clear();
        IntelItems.Clear();
        IntelSources.Clear();
        IntelAssessments.Clear();
        EvidenceItems.Clear();
        LogInventory.Clear();
        LogStorages.Clear();
        LogRoutes.Clear();
        LogShipments.Clear();
        LogTransportUnits.Clear();
        LogSupplyNeeds.Clear();
        LogIncidents.Clear();
        InternalReviews.Clear();
        InternalRiskByOfficer.Clear();
        SuspicionRecords.Clear();
        Records.Clear();
        StationStates.Clear();
        StationRelations.Clear();
        ActiveShiftPlans.Clear();
        InternalStore = new PoliceInternalStateStore();
    }

    public static void ResetForNewGame(int cityMapSeed)
    {
        ClearAll();
        BootstrapSeed = cityMapSeed;
        PoliceRosterGenerator.Build(cityMapSeed, out var hq, out var officers, out var careers, out var risk);
        Organization = hq;
        Officers = officers;
        OfficerCareers = careers;
        InternalRiskByOfficer = risk;
        int day = GameSessionState.CurrentDay >= 1 ? GameSessionState.CurrentDay : 1;
        foreach (OfficerProfile o in Officers)
        {
            if (o == null)
                continue;
            InternalStore.GetOrCreateBudget(o.OfficerId, day);
        }
        IsBootstrapped = true;
        ActiveShiftPlans.Clear();
        PoliceShiftScheduleBuilder.BuildShiftsForDay(Organization, Officers, OfficerCareers, day, InternalStore, ActiveShiftPlans);
        ScheduleLastBuiltForDay = day;
        for (int p = 0; p < ActiveShiftPlans.Count; p++)
            PoliceShiftScheduleBuilder.ApplyShiftHoursForPlan(ActiveShiftPlans[p], day, InternalStore);
        PoliceLogisticsSystem.EnsureBootstrapped(Organization, day);
        PoliceStationLivingSystem.EnsureBootstrapped(day);
    }

    public static void EnsureBootstrappedForSession(int cityMapSeed, int dayIndex)
    {
        if (IsBootstrapped)
            return;
        ResetForNewGame(cityMapSeed);
    }

    public static OfficerProfile GetOfficer(string officerId)
    {
        if (string.IsNullOrEmpty(officerId))
            return null;
        for (int i = 0; i < Officers.Count; i++)
        {
            OfficerProfile o = Officers[i];
            if (o != null && string.Equals(o.OfficerId, officerId, StringComparison.OrdinalIgnoreCase))
                return o;
        }
        return null;
    }

    public static OfficerCareerProfile GetCareer(string officerId)
    {
        if (string.IsNullOrEmpty(officerId))
            return null;
        for (int i = 0; i < OfficerCareers.Count; i++)
        {
            OfficerCareerProfile c = OfficerCareers[i];
            if (c != null && string.Equals(c.officerId, officerId, StringComparison.OrdinalIgnoreCase))
                return c;
        }
        return null;
    }

    public static string CaptureJson()
    {
        var s = new PoliceStateSnapshot
        {
            formatVersion = SnapshotFormatVersion,
            seed = BootstrapSeed,
            scheduleLastDay = ScheduleLastBuiltForDay,
            organization = Organization,
            officers = new List<OfficerProfile>(Officers),
            careers = new List<OfficerCareerProfile>(OfficerCareers),
            careerEvents = new List<OfficerCareerEvent>(OfficerCareerEvents),
            caseFiles = new List<CaseFile>(CaseFiles),
            intelItems = new List<IntelItem>(IntelItems),
            intelSources = new List<SourceProfile>(IntelSources),
            intelAssessments = new List<IntelligenceAssessment>(IntelAssessments),
            evidence = new List<EvidenceItem>(EvidenceItems),
            logInventory = new List<LogisticsInventoryItem>(LogInventory),
            logStorages = new List<LogisticsStorage>(LogStorages),
            logRoutes = new List<LogisticsRoute>(LogRoutes),
            logShipments = new List<LogisticsShipment>(LogShipments),
            logTransportUnits = new List<LogisticsTransportUnit>(LogTransportUnits),
            logSupplyNeeds = new List<LogisticsSupplyNeed>(LogSupplyNeeds),
            logIncidents = new List<LogisticsIncident>(LogIncidents),
            internalReviews = new List<InternalReviewCase>(InternalReviews),
            internalRisk = new List<InternalRiskProfile>(InternalRiskByOfficer),
            suspicions = new List<SuspicionRecord>(SuspicionRecords),
            records = new List<PoliceRecordEntry>(Records),
            stationStates = new List<PoliceStationLivingState>(StationStates),
            stationRelations = new List<PoliceStationRelation>(StationRelations),
            shiftPlans = new List<PoliceDayShiftPlan>(ActiveShiftPlans),
            timeBudgets = new List<ActorTimeBudgetEntry>(),
            exposures = new List<ExposureEntry>()
        };

        foreach (var kv in InternalStore.TimeBudgetsByOfficerId)
            s.timeBudgets.Add(new ActorTimeBudgetEntry { key = kv.Key, value = kv.Value });
        foreach (var kv in InternalStore.ExposureByRecordId)
            s.exposures.Add(new ExposureEntry { key = kv.Key, flags = (int)kv.Value });

        return JsonUtility.ToJson(s, true);
    }

    public static void ApplyJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return;
        try
        {
            PoliceStateSnapshot s = JsonUtility.FromJson<PoliceStateSnapshot>(json);
            if (s == null || s.formatVersion <= 0)
                return;
            IsBootstrapped = true;
            BootstrapSeed = s.seed;
            ScheduleLastBuiltForDay = s.scheduleLastDay;
            Organization = s.organization;
            Officers = s.officers ?? new List<OfficerProfile>();
            OfficerCareers = s.careers ?? new List<OfficerCareerProfile>();
            OfficerCareerEvents = s.careerEvents ?? new List<OfficerCareerEvent>();
            CaseFiles = s.caseFiles ?? new List<CaseFile>();
            IntelItems = s.intelItems ?? new List<IntelItem>();
            IntelSources = s.intelSources ?? new List<SourceProfile>();
            IntelAssessments = s.intelAssessments ?? new List<IntelligenceAssessment>();
            EvidenceItems = s.evidence ?? new List<EvidenceItem>();
            LogInventory = s.logInventory ?? new List<LogisticsInventoryItem>();
            LogStorages = s.logStorages ?? new List<LogisticsStorage>();
            LogRoutes = s.logRoutes ?? new List<LogisticsRoute>();
            LogShipments = s.logShipments ?? new List<LogisticsShipment>();
            LogTransportUnits = s.logTransportUnits ?? new List<LogisticsTransportUnit>();
            LogSupplyNeeds = s.logSupplyNeeds ?? new List<LogisticsSupplyNeed>();
            LogIncidents = s.logIncidents ?? new List<LogisticsIncident>();
            InternalReviews = s.internalReviews ?? new List<InternalReviewCase>();
            InternalRiskByOfficer = s.internalRisk ?? new List<InternalRiskProfile>();
            SuspicionRecords = s.suspicions ?? new List<SuspicionRecord>();
            Records = s.records ?? new List<PoliceRecordEntry>();
            StationStates = s.stationStates ?? new List<PoliceStationLivingState>();
            StationRelations = s.stationRelations ?? new List<PoliceStationRelation>();
            ActiveShiftPlans = s.shiftPlans ?? new List<PoliceDayShiftPlan>();

            InternalStore = new PoliceInternalStateStore();
            if (s.timeBudgets != null)
            {
                for (int i = 0; i < s.timeBudgets.Count; i++)
                {
                    var e = s.timeBudgets[i];
                    if (e != null && !string.IsNullOrEmpty(e.key) && e.value != null)
                        InternalStore.TimeBudgetsByOfficerId[e.key] = e.value;
                }
            }
            if (s.exposures != null)
            {
                for (int i = 0; i < s.exposures.Count; i++)
                {
                    var e = s.exposures[i];
                    if (e != null && !string.IsNullOrEmpty(e.key))
                        InternalStore.ExposureByRecordId[e.key] = (PoliceExposureFlags)e.flags;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Police] Failed to load police state snapshot: " + e.Message);
        }
    }
}

[Serializable]
public class PoliceStateSnapshot
{
    public int formatVersion;
    public int seed;
    public int scheduleLastDay;
    public PoliceHeadquartersProfile organization;

    public List<OfficerProfile> officers;
    public List<OfficerCareerProfile> careers;
    public List<OfficerCareerEvent> careerEvents;

    public List<CaseFile> caseFiles;
    public List<IntelItem> intelItems;
    public List<SourceProfile> intelSources;
    public List<IntelligenceAssessment> intelAssessments;
    public List<EvidenceItem> evidence;
    public List<LogisticsInventoryItem> logInventory;
    public List<LogisticsStorage> logStorages;
    public List<LogisticsRoute> logRoutes;
    public List<LogisticsShipment> logShipments;
    public List<LogisticsTransportUnit> logTransportUnits;
    public List<LogisticsSupplyNeed> logSupplyNeeds;
    public List<LogisticsIncident> logIncidents;
    public List<InternalReviewCase> internalReviews;
    public List<InternalRiskProfile> internalRisk;
    public List<SuspicionRecord> suspicions;
    public List<PoliceRecordEntry> records;
    public List<PoliceStationLivingState> stationStates;
    public List<PoliceStationRelation> stationRelations;
    public List<PoliceDayShiftPlan> shiftPlans;
    public List<ActorTimeBudgetEntry> timeBudgets;
    public List<ExposureEntry> exposures;
}

[Serializable]
public class ActorTimeBudgetEntry
{
    public string key;
    public ActorTimeBudget value;
}

[Serializable]
public class ExposureEntry
{
    public string key;
    public int flags;
}
