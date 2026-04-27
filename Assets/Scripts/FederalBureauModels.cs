using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>High-level mode of the federal Bureau; drives tone and which actions are in play.</summary>
public enum BureauOperationalStatus
{
    Dormant = 0,
    Watching = 1,
    Active = 2,
    Aggressive = 3,
    Crisis = 4
}

public enum FederalAgentAssignment
{
    Field = 0,
    Intelligence = 1,
    OrganizedCrime = 2,
    ProhibitionAndSubstances = 3,
    Operations = 4,
    FacilitiesAndLogistics = 5,
    PoliticalAndLegal = 6,
    InternalControl = 7,
    Undercover = 8
}

public enum FederalCoverStatus
{
    OpenIdentity = 0,
    ClassifiedIdentity = 1,
    Undercover = 2,
    DeepCover = 3,
    Burned = 4
}

public enum FederalDivisionType
{
    OrganizedCrimeUnit = 0,
    ProhibitionAndDangerousSubstancesUnit = 1,
    FederalIntelligenceUnit = 2,
    FederalOperationsUnit = 3,
    FacilitiesAndLogisticsUnit = 4,
    PoliticalAndLegalAffairsUnit = 5,
    InternalControlUnit = 6,
    StrategicCasesUnit = 7
}

public enum FederalFieldTeamStatus
{
    Available = 0,
    Assigned = 1,
    Undercover = 2,
    InOperation = 3,
    Resting = 4,
    Compromised = 5,
    Suspended = 6,
    Dead = 7
}

public enum FederalCaseTypeBureau
{
    OrganizedCrime = 0,
    ProhibitionDangerousSubstances = 1,
    Smuggling = 2,
    Corruption = 3,
    FederalStrategic = 4,
    PoliceFailure = 5
}

public enum FederalCaseTakeoverStatus
{
    None = 0,
    Requested = 1,
    InProgress = 2,
    Completed = 3,
    Blocked = 4
}

public enum FederalCaseStatusBureau
{
    Active = 0,
    Archived = 1,
    Destroyed = 2
}

public enum FederalTargetBehaviorArchetype
{
    Standard = 0,
    Paranoid = 1,
    Conspiratorial = 2,
    RecklessEgo = 3,
    Indifferent = 4,
    Cautious = 5
}

public enum FederalLeaderStatus
{
    Active = 0,
    Wanted = 1,
    Captured = 2,
    Killed = 3,
    Removed = 4,
    Retired = 5,
    Missing = 6
}

public enum FederalLeadershipTransitionReason
{
    None = 0,
    Assassinated = 1,
    DeadNatural = 2,
    RemovedByCouncil = 3,
    RetiredVoluntary = 4,
    ForcedRetirement = 5,
    CapturedAndDisconnected = 6,
    InternalCoup = 7
}

public enum FederalFacilityTypeBureau
{
    PublicHQ = 0,
    SafeHouse = 1,
    Storage = 2,
    InterrogationRoom = 3,
    ObservationPost = 4,
    UndercoverApartment = 5,
    EvidenceCache = 6,
    BlackSite = 7
}

public enum FederalFacilityBudgetSource
{
    OfficialBudget = 0,
    ClassifiedFund = 1,
    FederalBlackCash = 2,
    Unknown = 3
}

public enum FederalSpendingSource
{
    Official = 0,
    Classified = 1,
    /// <summary>Illegal; always paired with a violation in audit (see authority resolver).</summary>
    BlackCash = 2
}

[Serializable]
public class FederalSpendingLogEntry
{
    public string entryId;
    public int dayIndex;
    public int amountMinorUnits;
    public FederalSpendingSource source;
    public string memo;
    public string authorizedByAgentId;
    public int suspiciousRisk01;
}

[Serializable]
public class FederalAccessLog
{
    public string logId;
    public int dayIndex;
    public string agentId;
    public string policeCaseId;
    public string accessKind;
    public string materialSummary;
    public string notes;
}

[Serializable]
public class FederalTakeoverLog
{
    public string logId;
    public int dayIndex;
    public string federalCaseId;
    public string policeCaseId;
    public string leadAgentId;
    public string approvedByAgentId;
    public int takeoverStatusInt;
}

[Serializable]
public class FederalUndercoverOperation
{
    public string operationId;
    public string codeName;
    public string caseId;
    public string leadAgentId;
    public int secrecy01;
    public int operationalRisk01;
    public bool isActive;
    public int openedDay;
}

[Serializable]
public class FederalReviewTicket
{
    public string reviewId;
    public string caseId;
    public string reason;
    public int openedAtDay;
    public int priority01;
    public string assignedAgentId;
}

[Serializable]
public class FederalAgentCareerStub
{
    public float serviceYears;
    public int promotionScore;
    public int commendations;
    public int reprimands;
    public int suspensionCount;
}

[Serializable]
public class FederalAgentProfile
{
    public string agentId;
    public string fullName;
    public FederalBureauRank rank;
    public FederalDeputyPortfolio deputyPortfolio;
    public FederalAgentAssignment assignment;
    /// <summary>Line unit: one of <see cref="FederalBureauStructure.FederalBureauDivisionIds"/>. <c>null</c> or empty for Director and Deputy Directors (not under a specific division).</summary>
    public string divisionId;
    public string teamId;
    public FederalCoverStatus coverStatus;
    public bool availableForField;
    public int strength;
    public int agility;
    public int intelligence;
    public int charisma;
    public int mentalResilience;
    public int determination;
    public int[] skillLevels = new int[DerivedSkillProgression.SkillCount];
    public int personalityFlags;
    public FederalAgentCareerStub career;
    public int secrecyRisk;
    public int corruptionRisk;
    public int blackmailRisk;
    public int fieldReputation;
    public int internalReputation;
}

[Serializable]
public class FederalDivision
{
    public string divisionId;
    public FederalDivisionType divisionType;
    public string chiefAgentId;
    public string deputyDirectorLiaisonId;
    public List<string> agentIds = new List<string>();
    public List<string> activeCaseIds = new List<string>();
    public List<string> activeOperationIds = new List<string>();
    public string policyProfile;
}

[Serializable]
public class FederalFieldTeam
{
    public string teamId;
    public string teamName;
    public string leadAgentId;
    public List<string> memberIds = new List<string>();
    public string assignedCaseId;
    public string assignedOperationId;
    public FederalFieldTeamStatus currentStatus;
    public int secrecyLevel;
    public int operationalRisk;
}

/// <summary>
/// Federal Bureau case file (strategic / national) — use this name to avoid clashing with local police <c>CaseFile</c> / <c>PoliceEvidenceSystem</c> case evidence. Lives in <see cref="BureauWorldState.FederalCases"/>.
/// </summary>
[Serializable]
public class FederalCaseFileBureau
{
    public string caseId;
    public FederalCaseTypeBureau caseType;
    public int targetTypeInt;
    public string targetId;
    public string owningDivisionId;
    public string leadAgentId;
    public string supervisingAgentId;
    public FederalCaseStatusBureau status;
    public int priority;
    public int evidenceStrength;
    public int intelStrength;
    public int legalIntegrity;
    public int secrecyLevel;
    public int politicalRisk;
    public int nationalSecurityThreat;
    public int targetBehaviorArchetypeInt;
    public string targetLeaderId;
    public int targetLeaderStatusInt;
    public int dynamicControlLeverage0to100;
    public bool currentlyControlledByBureau;
    public bool targetAwareOfBureauAttention;
    public string lastAwarenessReason;
    public bool isDestroyed;
    public List<string> linkedPoliceCaseIds = new List<string>();
    public List<FederalCaseExternalTransferRecord> externalTransfers = new List<FederalCaseExternalTransferRecord>();
    public List<FederalCaseActivityRecord> activityLog = new List<FederalCaseActivityRecord>();
    public int leadershipTransitionReasonInt;
    public int leadershipTransitionDayIndex;
    public FederalCaseTakeoverStatus takeoverStatus;
}

[Serializable]
public class FederalCaseExternalTransferRecord
{
    public string packetId;
    public string caseId;
    public string destinationAuthority; // Police, Prosecution, TaxAuthority, Court
    public string transferSummary;
    public List<string> transferredEvidenceIds = new List<string>();
    public long createdAtTicks;
    public int dayIndex;
}

[Serializable]
public class FederalCaseActivityRecord
{
    public string activityId;
    public int dayIndex;
    public string operationId;
    public string actionType;
    public string narrative;
    public string reason;
    public bool wasExecuted;
    public long createdAtTicks;
}

[Serializable]
public class FederalIntelRecordBureau
{
    public string intelId;
    public int dayIndex;
    public string summary;
    public string relatedCaseId;
    public string divisionId;
    public int strength01;
}

[Serializable]
public class FederalFacility
{
    public string facilityId;
    public FederalFacilityTypeBureau facilityType;
    public string locationId;
    public bool isRegistered;
    public int secrecyLevel;
    public string controlledByDivisionId;
    public FederalFacilityBudgetSource budgetSource;
    public string currentUse;
    public int exposureRisk;
}

[Serializable]
public class FederalBudgetStateBureau
{
    public int officialBudgetMinor;
    public int classifiedFundMinor;
    public int federalBlackCashMinor;
    public int monthlyAllocationMinor;
    public List<FederalSpendingLogEntry> spendingLogs = new List<FederalSpendingLogEntry>();
    public int suspiciousSpendingRisk;
}

[Serializable]
public class FederalBureauSnapshot
{
    public int formatVersion;
    public int citySeed;
    public BureauOperationalStatus bureauStatus;
    public int currentHeat;
    public int politicalPressure;
    public int publicExposure;
    public int federalAggressionLevel;
    public string activeStrategy;
    public List<FederalAgentProfile> roster = new List<FederalAgentProfile>();
    public List<FederalDivision> divisions = new List<FederalDivision>();
    public List<FederalFieldTeam> fieldTeams = new List<FederalFieldTeam>();
    public List<FederalCaseFileBureau> cases = new List<FederalCaseFileBureau>();
    public List<FederalIntelRecordBureau> intel = new List<FederalIntelRecordBureau>();
    public List<FederalFacility> facilities = new List<FederalFacility>();
    public FederalBudgetStateBureau budget = new FederalBudgetStateBureau();
    public List<FederalAccessLog> accessLogs = new List<FederalAccessLog>();
    public List<FederalTakeoverLog> takeoverLogs = new List<FederalTakeoverLog>();
    public List<FederalUndercoverOperation> undercoverOperations = new List<FederalUndercoverOperation>();
    public List<FederalReviewTicket> pendingReviews = new List<FederalReviewTicket>();
    // Snapshot format v2+ — federal workflow pipeline
    public List<FederalWorkflowRequest> pendingWorkflowRequests = new List<FederalWorkflowRequest>();
    public List<FederalWorkflowOperation> activeWorkflowOperations = new List<FederalWorkflowOperation>();
    public List<FederalWorkflowOperation> completedWorkflowOperations = new List<FederalWorkflowOperation>();
    public List<FederalWorkflowLog> federalWorkflowLogs = new List<FederalWorkflowLog>();
    public List<FederalPolicyDeviationRecord> federalDeviationRecords = new List<FederalPolicyDeviationRecord>();
    public List<FederalOperationLog> federalOperationLogs = new List<FederalOperationLog>();
    public int lastRuntimeDay;
    public int lastSelectedStrategyInt;
    public string lastPrimaryTargetId;
    public List<FederalDailyReport> dailyReports = new List<FederalDailyReport>();
    public int federalRuntimeInterest01;
    public int federalRuntimeExposureEvents01;
    // v4+ — policy layer, leadership authority, conflicts, director calibration
    public List<FederalPolicyProfile> activeFederalPolicies = new List<FederalPolicyProfile>();
    public List<FederalLeadershipAuthorityProfile> leadershipAuthorityProfiles = new List<FederalLeadershipAuthorityProfile>();
    public List<FederalPolicyConflictRecord> policyConflictRecords = new List<FederalPolicyConflictRecord>();
    public List<FederalDirectorPolicyCalibration> directorPolicyCalibrations = new List<FederalDirectorPolicyCalibration>();
    public List<FederalIntelItem> federalIntelItems = new List<FederalIntelItem>();
    public List<FederalSourceProfile> federalSourceProfiles = new List<FederalSourceProfile>();
    public List<FederalDisinformationEvent> federalDisinformationEvents = new List<FederalDisinformationEvent>();
    public int lastDirectorCalibrationRecordDay;
    public int currentDirectorPolicyModeInt;
    public int lastAggregatedPolicyInfluence0to100;
    public int lastFieldCultureResistance0to100;
}
