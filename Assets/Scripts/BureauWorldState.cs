using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Live federal Bureau (Central Serious Crime Unit) state: roster, org, files, and audit logs. Player-facing leaks go through a visibility layer later.</summary>
public static class BureauWorldState
{
    public const int SnapshotVersion = 4;
    public static bool IsBootstrapped;
    public static int BootstrapSeed;
    public static BureauOperationalStatus bureauStatus = BureauOperationalStatus.Dormant;
    public static int currentHeat;
    public static int politicalPressure;
    public static int publicExposure;
    public static int federalAggressionLevel;
    public static string activeStrategy = string.Empty;
    public static readonly List<FederalAgentProfile> Roster = new List<FederalAgentProfile>();
    public static readonly List<FederalDivision> Divisions = new List<FederalDivision>();
    public static readonly List<FederalFieldTeam> FieldTeams = new List<FederalFieldTeam>();
    /// <summary>Active <see cref="FederalCaseFileBureau"/> entries (federal; not local police case files).</summary>
    public static readonly List<FederalCaseFileBureau> FederalCases = new List<FederalCaseFileBureau>();
    public static readonly List<FederalIntelRecordBureau> FederalIntel = new List<FederalIntelRecordBureau>();
    public static readonly List<FederalFacility> Facilities = new List<FederalFacility>();
    public static FederalBudgetStateBureau Budget = new FederalBudgetStateBureau();
    public static readonly List<FederalAccessLog> AccessLogs = new List<FederalAccessLog>();
    public static readonly List<FederalTakeoverLog> TakeoverLogs = new List<FederalTakeoverLog>();
    public static readonly List<FederalUndercoverOperation> UndercoverOperations = new List<FederalUndercoverOperation>();
    public static readonly List<FederalReviewTicket> PendingReviews = new List<FederalReviewTicket>();
    public static readonly List<FederalWorkflowRequest> PendingWorkflowRequests = new List<FederalWorkflowRequest>();
    public static readonly List<FederalWorkflowOperation> ActiveWorkflowOperations = new List<FederalWorkflowOperation>();
    public static readonly List<FederalWorkflowOperation> CompletedWorkflowOperations = new List<FederalWorkflowOperation>();
    public static readonly List<FederalWorkflowLog> FederalWorkflowLogs = new List<FederalWorkflowLog>();
    public static readonly List<FederalPolicyDeviationRecord> FederalDeviationRecords = new List<FederalPolicyDeviationRecord>();
    public static readonly List<FederalOperationLog> FederalOperationLogs = new List<FederalOperationLog>();
    public static int lastRuntimeDay;
    public static int lastSelectedStrategyInt;
    public static string lastPrimaryTargetId = string.Empty;
    public static readonly List<FederalDailyReport> dailyReports = new List<FederalDailyReport>();
    public static int federalRuntimeInterest01;
    public static int federalRuntimeExposureEvents01;
    public static readonly List<string> federalRuntimeLog = new List<string>();
    // Federal policy layer + director calibration (v4+ snapshot)
    public static readonly List<FederalPolicyProfile> activeFederalPolicies = new List<FederalPolicyProfile>();
    public static readonly List<FederalLeadershipAuthorityProfile> leadershipAuthorityProfiles = new List<FederalLeadershipAuthorityProfile>();
    public static readonly List<FederalPolicyConflictRecord> policyConflictRecords = new List<FederalPolicyConflictRecord>();
    public static readonly List<FederalDirectorPolicyCalibration> directorPolicyCalibrations = new List<FederalDirectorPolicyCalibration>();
    public static readonly List<FederalIntelItem> federalIntelItems = new List<FederalIntelItem>();
    public static readonly List<FederalSourceProfile> federalSourceProfiles = new List<FederalSourceProfile>();
    public static readonly List<FederalDisinformationEvent> federalDisinformationEvents = new List<FederalDisinformationEvent>();
    public static int lastDirectorCalibrationRecordDay = -1;
    public static int currentDirectorPolicyModeInt;
    public static int lastAggregatedPolicyInfluence0to100;
    public static int lastFieldCultureResistance0to100;

    public static void ClearAll()
    {
        IsBootstrapped = false;
        BootstrapSeed = 0;
        bureauStatus = BureauOperationalStatus.Dormant;
        currentHeat = 0;
        politicalPressure = 0;
        publicExposure = 0;
        federalAggressionLevel = 0;
        activeStrategy = string.Empty;
        Roster.Clear();
        Divisions.Clear();
        FieldTeams.Clear();
        FederalCases.Clear();
        FederalIntel.Clear();
        Facilities.Clear();
        Budget = new FederalBudgetStateBureau();
        AccessLogs.Clear();
        TakeoverLogs.Clear();
        UndercoverOperations.Clear();
        PendingReviews.Clear();
        PendingWorkflowRequests.Clear();
        ActiveWorkflowOperations.Clear();
        CompletedWorkflowOperations.Clear();
        FederalWorkflowLogs.Clear();
        FederalDeviationRecords.Clear();
        FederalOperationLogs.Clear();
        lastRuntimeDay = 0;
        lastSelectedStrategyInt = 0;
        lastPrimaryTargetId = string.Empty;
        dailyReports.Clear();
        federalRuntimeInterest01 = 0;
        federalRuntimeExposureEvents01 = 0;
        federalRuntimeLog.Clear();
        activeFederalPolicies.Clear();
        leadershipAuthorityProfiles.Clear();
        policyConflictRecords.Clear();
        directorPolicyCalibrations.Clear();
        federalIntelItems.Clear();
        federalSourceProfiles.Clear();
        federalDisinformationEvents.Clear();
        lastDirectorCalibrationRecordDay = -1;
        currentDirectorPolicyModeInt = 0;
        lastAggregatedPolicyInfluence0to100 = 0;
        lastFieldCultureResistance0to100 = 0;
    }

    public static void ResetForNewGame(int cityMapSeed)
    {
        ClearAll();
        IsBootstrapped = true;
        BootstrapSeed = cityMapSeed;
        BureauRosterBootstrap.BuildIntoWorldState(cityMapSeed);
        Budget = new FederalBudgetStateBureau
        {
            officialBudgetMinor = 2_000_000,
            classifiedFundMinor = 200_000,
            federalBlackCashMinor = 0,
            monthlyAllocationMinor = 120_000
        };
        currentHeat = 5;
        politicalPressure = 5;
        publicExposure = 3;
        federalAggressionLevel = 8;
        activeStrategy = "watch_and_probe";
        bureauStatus = BureauOperationalStatus.Watching;
        FederalPolicyResolver.EnsureSeeded(cityMapSeed);
        SyncToLegacySessionEngagement();
    }

    public static void EnsureBootstrappedForSession(int cityMapSeed, int dayIndex)
    {
        if (IsBootstrapped)
            return;
        ResetForNewGame(cityMapSeed);
    }

    public static FederalAgentProfile GetAgent(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;
        for (int i = 0; i < Roster.Count; i++)
        {
            if (Roster[i] != null && string.Equals(Roster[i].agentId, id, StringComparison.OrdinalIgnoreCase))
                return Roster[i];
        }
        return null;
    }

    public static bool TryArchiveCase(string caseId, string reason = null)
    {
        var c = FindCase(caseId);
        if (c == null || c.isDestroyed) return false;
        c.status = FederalCaseStatusBureau.Archived;
        AppendCaseActivity(c, "ArchiveCase", "Case archived", reason, false);
        if (!string.IsNullOrEmpty(reason))
            LogCaseNote(c, "Archived: " + reason);
        return true;
    }

    public static bool TryDestroyCase(string caseId, string reason = null)
    {
        var c = FindCase(caseId);
        if (c == null) return false;
        c.status = FederalCaseStatusBureau.Destroyed;
        c.isDestroyed = true;
        c.evidenceStrength = 0;
        c.intelStrength = 0;
        c.legalIntegrity = 0;
        c.linkedPoliceCaseIds.Clear();
        AppendCaseActivity(c, "DestroyCase", "Case destroyed", reason, false);
        if (!string.IsNullOrEmpty(reason))
            LogCaseNote(c, "Destroyed: " + reason);
        return true;
    }

    public static bool TryRecordPartialTransfer(
        string caseId,
        string destinationAuthority,
        string transferSummary,
        List<string> transferredEvidenceIds,
        int dayIndex)
    {
        var c = FindCase(caseId);
        if (c == null || c.isDestroyed) return false;
        if (c.externalTransfers == null) c.externalTransfers = new List<FederalCaseExternalTransferRecord>();
        c.externalTransfers.Add(new FederalCaseExternalTransferRecord
        {
            packetId = "fw_pkt_" + Guid.NewGuid().ToString("N").Substring(0, 10),
            caseId = c.caseId,
            destinationAuthority = destinationAuthority ?? "Unknown",
            transferSummary = transferSummary ?? string.Empty,
            transferredEvidenceIds = transferredEvidenceIds != null ? new List<string>(transferredEvidenceIds) : new List<string>(),
            createdAtTicks = DateTime.UtcNow.Ticks,
            dayIndex = dayIndex
        });
        AppendCaseActivity(c, "TransferPacket", "Partial transfer sent to " + (destinationAuthority ?? "Unknown"), transferSummary, true);
        while (c.externalTransfers.Count > 40) c.externalTransfers.RemoveAt(0);
        return true;
    }

    public static bool TryTransitionCaseLeadership(
        string caseId,
        string newLeaderId,
        FederalLeadershipTransitionReason reason,
        int dayIndex,
        out string error)
    {
        error = null;
        var c = FindCase(caseId);
        if (c == null || c.isDestroyed)
        {
            error = "Case not found/destroyed.";
            return false;
        }

        // Hard rule: "Wanted" status by itself is never enough to replace leadership.
        if ((FederalLeaderStatus)c.targetLeaderStatusInt == FederalLeaderStatus.Wanted
            && reason == FederalLeadershipTransitionReason.None)
        {
            error = "Wanted status alone cannot trigger leadership replacement.";
            return false;
        }

        if (reason == FederalLeadershipTransitionReason.None)
        {
            error = "Leadership transition requires explicit valid reason.";
            return false;
        }

        if (string.IsNullOrEmpty(newLeaderId))
        {
            error = "New leader id is required.";
            return false;
        }

        c.targetLeaderId = newLeaderId;
        c.targetLeaderStatusInt = (int)FederalLeaderStatus.Active;
        c.leadershipTransitionReasonInt = (int)reason;
        c.leadershipTransitionDayIndex = dayIndex;
        c.currentlyControlledByBureau = false; // reset leverage assumptions on leadership change
        c.dynamicControlLeverage0to100 = Mathf.Clamp(c.dynamicControlLeverage0to100 - 20, 0, 100);
        AppendCaseActivity(c, "LeadershipTransition", "Leadership changed", reason.ToString(), true);
        return true;
    }

    static FederalCaseFileBureau FindCase(string caseId)
    {
        if (string.IsNullOrEmpty(caseId)) return null;
        for (int i = 0; i < FederalCases.Count; i++)
        {
            var c = FederalCases[i];
            if (c != null && string.Equals(c.caseId, caseId, StringComparison.OrdinalIgnoreCase))
                return c;
        }
        return null;
    }

    static void LogCaseNote(FederalCaseFileBureau c, string note)
    {
        if (c == null || string.IsNullOrEmpty(note)) return;
        federalRuntimeLog.Add("[Case " + c.caseId + "] " + note);
        while (federalRuntimeLog.Count > 48) federalRuntimeLog.RemoveAt(0);
    }

    static void AppendCaseActivity(FederalCaseFileBureau c, string actionType, string narrative, string reason, bool executed)
    {
        if (c == null) return;
        if (c.activityLog == null) c.activityLog = new List<FederalCaseActivityRecord>();
        c.activityLog.Add(new FederalCaseActivityRecord
        {
            activityId = "fcase_act_" + Guid.NewGuid().ToString("N").Substring(0, 10),
            dayIndex = GameSessionState.CurrentDay,
            operationId = string.Empty,
            actionType = actionType ?? string.Empty,
            narrative = narrative ?? string.Empty,
            reason = reason ?? string.Empty,
            wasExecuted = executed,
            createdAtTicks = DateTime.UtcNow.Ticks
        });
        while (c.activityLog.Count > 120) c.activityLog.RemoveAt(0);
    }

    public static void SyncToLegacySessionEngagement()
    {
        if (!IsBootstrapped)
            return;
        switch (bureauStatus)
        {
            case BureauOperationalStatus.Dormant:
                GameSessionState.FederalBureauState = GameSessionState.FederalBureauEngagement.Dormant;
                return;
            case BureauOperationalStatus.Watching:
                GameSessionState.FederalBureauState = GameSessionState.FederalBureauEngagement.Watching;
                return;
            case BureauOperationalStatus.Active:
            case BureauOperationalStatus.Aggressive:
            case BureauOperationalStatus.Crisis:
            default:
                GameSessionState.FederalBureauState = GameSessionState.FederalBureauEngagement.Active;
                return;
        }
    }

    public static string CaptureJson()
    {
        var s = new FederalBureauSnapshot
        {
            formatVersion = SnapshotVersion,
            citySeed = BootstrapSeed,
            bureauStatus = bureauStatus,
            currentHeat = currentHeat,
            politicalPressure = politicalPressure,
            publicExposure = publicExposure,
            federalAggressionLevel = federalAggressionLevel,
            activeStrategy = activeStrategy ?? string.Empty
        };
        s.roster.AddRange(Roster);
        s.divisions.AddRange(Divisions);
        s.fieldTeams.AddRange(FieldTeams);
        s.cases.AddRange(FederalCases);
        s.intel.AddRange(FederalIntel);
        s.facilities.AddRange(Facilities);
        s.budget = Budget;
        s.accessLogs.AddRange(AccessLogs);
        s.takeoverLogs.AddRange(TakeoverLogs);
        s.undercoverOperations.AddRange(UndercoverOperations);
        s.pendingReviews.AddRange(PendingReviews);
        s.pendingWorkflowRequests.AddRange(PendingWorkflowRequests);
        s.activeWorkflowOperations.AddRange(ActiveWorkflowOperations);
        s.completedWorkflowOperations.AddRange(CompletedWorkflowOperations);
        s.federalWorkflowLogs.AddRange(FederalWorkflowLogs);
        s.federalDeviationRecords.AddRange(FederalDeviationRecords);
        s.federalOperationLogs.AddRange(FederalOperationLogs);
        s.lastRuntimeDay = lastRuntimeDay;
        s.lastSelectedStrategyInt = lastSelectedStrategyInt;
        s.lastPrimaryTargetId = lastPrimaryTargetId ?? string.Empty;
        s.dailyReports.AddRange(dailyReports);
        s.federalRuntimeInterest01 = federalRuntimeInterest01;
        s.federalRuntimeExposureEvents01 = federalRuntimeExposureEvents01;
        s.activeFederalPolicies.AddRange(activeFederalPolicies);
        s.leadershipAuthorityProfiles.AddRange(leadershipAuthorityProfiles);
        s.policyConflictRecords.AddRange(policyConflictRecords);
        s.directorPolicyCalibrations.AddRange(directorPolicyCalibrations);
        s.federalIntelItems.AddRange(federalIntelItems);
        s.federalSourceProfiles.AddRange(federalSourceProfiles);
        s.federalDisinformationEvents.AddRange(federalDisinformationEvents);
        s.lastDirectorCalibrationRecordDay = lastDirectorCalibrationRecordDay;
        s.currentDirectorPolicyModeInt = currentDirectorPolicyModeInt;
        s.lastAggregatedPolicyInfluence0to100 = lastAggregatedPolicyInfluence0to100;
        s.lastFieldCultureResistance0to100 = lastFieldCultureResistance0to100;
        return JsonUtility.ToJson(s, true);
    }

    public static void ApplyJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return;
        try
        {
            var s = JsonUtility.FromJson<FederalBureauSnapshot>(json);
            if (s == null || s.formatVersion <= 0)
                return;
            // v1 save: workflow lists absent from JSON; treat as empty.
            IsBootstrapped = true;
            BootstrapSeed = s.citySeed;
            bureauStatus = s.bureauStatus;
            currentHeat = Mathf.Clamp(s.currentHeat, 0, 100);
            politicalPressure = Mathf.Clamp(s.politicalPressure, 0, 100);
            publicExposure = Mathf.Clamp(s.publicExposure, 0, 100);
            federalAggressionLevel = Mathf.Clamp(s.federalAggressionLevel, 0, 100);
            activeStrategy = s.activeStrategy ?? string.Empty;
            Roster.Clear();
            if (s.roster != null) Roster.AddRange(s.roster);
            Divisions.Clear();
            if (s.divisions != null) Divisions.AddRange(s.divisions);
            FieldTeams.Clear();
            if (s.fieldTeams != null) FieldTeams.AddRange(s.fieldTeams);
            FederalCases.Clear();
            if (s.cases != null) FederalCases.AddRange(s.cases);
            FederalIntel.Clear();
            if (s.intel != null) FederalIntel.AddRange(s.intel);
            Facilities.Clear();
            if (s.facilities != null) Facilities.AddRange(s.facilities);
            if (s.budget != null)
                Budget = s.budget;
            else
                Budget = new FederalBudgetStateBureau();
            AccessLogs.Clear();
            if (s.accessLogs != null) AccessLogs.AddRange(s.accessLogs);
            TakeoverLogs.Clear();
            if (s.takeoverLogs != null) TakeoverLogs.AddRange(s.takeoverLogs);
            UndercoverOperations.Clear();
            if (s.undercoverOperations != null) UndercoverOperations.AddRange(s.undercoverOperations);
            PendingReviews.Clear();
            if (s.pendingReviews != null) PendingReviews.AddRange(s.pendingReviews);
            PendingWorkflowRequests.Clear();
            if (s.pendingWorkflowRequests != null) PendingWorkflowRequests.AddRange(s.pendingWorkflowRequests);
            ActiveWorkflowOperations.Clear();
            if (s.activeWorkflowOperations != null) ActiveWorkflowOperations.AddRange(s.activeWorkflowOperations);
            CompletedWorkflowOperations.Clear();
            if (s.completedWorkflowOperations != null) CompletedWorkflowOperations.AddRange(s.completedWorkflowOperations);
            FederalWorkflowLogs.Clear();
            if (s.federalWorkflowLogs != null) FederalWorkflowLogs.AddRange(s.federalWorkflowLogs);
            FederalDeviationRecords.Clear();
            if (s.federalDeviationRecords != null) FederalDeviationRecords.AddRange(s.federalDeviationRecords);
            FederalOperationLogs.Clear();
            if (s.federalOperationLogs != null) FederalOperationLogs.AddRange(s.federalOperationLogs);
            if (s.formatVersion >= 3)
            {
                lastRuntimeDay = s.lastRuntimeDay;
                lastSelectedStrategyInt = s.lastSelectedStrategyInt;
                lastPrimaryTargetId = s.lastPrimaryTargetId ?? string.Empty;
                dailyReports.Clear();
                if (s.dailyReports != null) dailyReports.AddRange(s.dailyReports);
                federalRuntimeInterest01 = s.federalRuntimeInterest01;
                federalRuntimeExposureEvents01 = s.federalRuntimeExposureEvents01;
            }
            else
            {
                lastRuntimeDay = 0;
                lastSelectedStrategyInt = 0;
                lastPrimaryTargetId = string.Empty;
                dailyReports.Clear();
                federalRuntimeInterest01 = 0;
                federalRuntimeExposureEvents01 = 0;
            }
            if (s.formatVersion >= 4)
            {
                activeFederalPolicies.Clear();
                if (s.activeFederalPolicies != null) activeFederalPolicies.AddRange(s.activeFederalPolicies);
                leadershipAuthorityProfiles.Clear();
                if (s.leadershipAuthorityProfiles != null) leadershipAuthorityProfiles.AddRange(s.leadershipAuthorityProfiles);
                policyConflictRecords.Clear();
                if (s.policyConflictRecords != null) policyConflictRecords.AddRange(s.policyConflictRecords);
                directorPolicyCalibrations.Clear();
                if (s.directorPolicyCalibrations != null) directorPolicyCalibrations.AddRange(s.directorPolicyCalibrations);
                federalIntelItems.Clear();
                if (s.federalIntelItems != null) federalIntelItems.AddRange(s.federalIntelItems);
                federalSourceProfiles.Clear();
                if (s.federalSourceProfiles != null) federalSourceProfiles.AddRange(s.federalSourceProfiles);
                federalDisinformationEvents.Clear();
                if (s.federalDisinformationEvents != null) federalDisinformationEvents.AddRange(s.federalDisinformationEvents);
                lastDirectorCalibrationRecordDay = s.lastDirectorCalibrationRecordDay;
                currentDirectorPolicyModeInt = s.currentDirectorPolicyModeInt;
                lastAggregatedPolicyInfluence0to100 = s.lastAggregatedPolicyInfluence0to100;
                lastFieldCultureResistance0to100 = s.lastFieldCultureResistance0to100;
            }
            else
            {
                activeFederalPolicies.Clear();
                leadershipAuthorityProfiles.Clear();
                policyConflictRecords.Clear();
                directorPolicyCalibrations.Clear();
                federalIntelItems.Clear();
                federalSourceProfiles.Clear();
                federalDisinformationEvents.Clear();
                lastDirectorCalibrationRecordDay = -1;
                currentDirectorPolicyModeInt = 0;
                lastAggregatedPolicyInfluence0to100 = 0;
                lastFieldCultureResistance0to100 = 0;
            }
            SyncToLegacySessionEngagement();
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Bureau] Load snapshot failed: " + e.Message);
        }
    }
}
