using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>End-to-end federal action workflow (Bureau). Serialized into <see cref="BureauWorldState"/> snapshot.</summary>
public enum FederalTargetType
{
    Person = 0,
    Organization = 1,
    Location = 2,
    Property = 3,
    PoliceCase = 4,
    FederalCase = 5,
    Facility = 6,
    Evidence = 7,
    Unknown = 8
}

public enum FederalOperationStatus
{
    Requested = 0,
    AwaitingApproval = 1,
    Approved = 2,
    Denied = 3,
    MissingApprover = 4,
    Assigned = 5,
    InProgress = 6,
    Completed = 7,
    Failed = 8,
    Compromised = 9,
    Cancelled = 10
}

public enum FederalOperationOutcome
{
    ExceptionalSuccess = 0,
    Success = 1,
    PartialSuccess = 2,
    CleanFailure = 3,
    Failure = 4,
    SevereFailure = 5
}

public enum FederalFieldChoice
{
    FollowOrders = 0,
    FlexibleInterpretation = 1,
    TacticalOverride = 2,
    IgnorePolicy = 3,
    ExceedAuthority = 4,
    AbortMission = 5,
    CoverUpDeviation = 6
}

public enum FederalApprovalDecision
{
    Approve = 0,
    Deny = 1,
    Delay = 2,
    RequestMoreIntel = 3,
    ModifyRequest = 4
}

public enum FederalApproverLookupStatus
{
    Found = 0,
    MissingApprover = 1
}

public enum FederalWorkflowRequestPhase
{
    Draft = 0,
    AuthorityResolved = 1,
    ApproverResolved = 2,
    Denied = 3,
    PromotedToOperation = 4
}

public enum FederalDeviationType
{
    PolicyDeviation = 0,
    AuthorityDeviation = 1,
    LegalViolation = 2,
    ForceDeviation = 3,
    SecrecyDeviation = 4,
    BudgetDeviation = 5,
    EvidenceDeviation = 6,
    FacilityDeviation = 7,
    ChainOfCommandDeviation = 8,
    CoverUp = 9
}

public enum FederalExposureFlag
{
    None = 0,
    PoliceNoticed = 1,
    CriminalsSuspectFederalPresence = 2,
    AgentIdentityRisk = 3,
    AgentIdentityExposed = 4,
    PublicIncident = 5,
    PressAttention = 6,
    PoliticalHeat = 7
}

public enum FederalViolationFlag
{
    MissingApproval = 0,
    WrongApprover = 1,
    MissingWarrant = 2,
    IllegalBlackCashUse = 3,
    UnregisteredFacilityUse = 4,
    ExcessiveForce = 5,
    UnauthorizedKilling = 6,
    AgentExposure = 7,
    Mismanaged = 8,
    FalseReport = 9,
    EvidenceTampering = 10,
    PoliceObstruction = 11,
    PoliticalOverreach = 12
}

public enum FederalComplianceBand
{
    FullCompliance = 0,
    CompliantInterpretation = 1,
    SoftDeviation = 2,
    HardDeviation = 3,
    OpenViolation = 4
}

[Serializable]
public class FederalWorkflowRequest
{
    public string requestId;
    public FederalActionType actionType;
    public string requestedByAgentId;
    public FederalTargetType targetType;
    public string targetId;
    public string relatedCaseId;
    public string relatedPoliceCaseId;
    public string facilityId;
    public int requestedBudgetMinor;
    public int urgencyLevel;
    public bool secrecyRequired;
    public bool emergencyClaimed;
    public string reason;
    public long createdAtTicks;
    public int createdAtDay;
    public FederalWorkflowRequestPhase phase;
    public bool hasAuthorityStep;
    public FederalAuthorityResolution authorityStep;
    public bool authorityAllowed;
    public FederalApproverLookupStatus approverStatus;
    public string approverAgentId;
    public FederalApprovalDecision approvalDecision;
    public bool hasApprovalDecision;
    // Mirrors for <see cref="FederalActionRequest"/> (defaults until UI wires flags).
    public bool hasWarrant;
    public bool hasPoliceAccessLog;
    public bool hasTakeoverLog;
    public bool isLargeScaleOp;
    public bool targetIsPublicFigure;
}

[Serializable]
public class FederalFieldDecision
{
    public string operationId;
    public string teamLeadAgentId;
    public int policyComplianceIntent;
    public bool expectedPolicyConflict;
    public string fieldInterpretation;
    public int deviationRisk;
    public List<int> possibleDeviationTypeInts = new List<int>();
    public FederalFieldChoice finalFieldChoice;
    public string reasonTag;
}

[Serializable]
public class FederalPolicyDeviationRecord
{
    public string deviationId;
    public string operationId;
    public List<string> agentIds = new List<string>();
    public string leadAgentId;
    public string approvedByAgentId;
    public string responsibleAgentId;
    public string policyId;
    public List<int> deviationTypeInts = new List<int>();
    public int severity;
    public int intent; // 0 unknown, 1 intentional, 2 mistake, 3 stress
    public bool wasDetected;
    public int detectionLevel;
    public string reasonTag;
    public List<string> immediateConsequences = new List<string>();
    public long createdAtTicks;
}

[Serializable]
public class FederalWorkflowOperation
{
    public string operationId;
    public string requestId;
    public FederalActionType actionType;
    public FederalOperationStatus status;
    public string requestedByAgentId;
    public string approvedByAgentId;
    public string responsibleAgentId;
    public string supervisingAgentId;
    public List<string> executedByAgentIds = new List<string>();
    public FederalTargetType targetType;
    public string targetId;
    public string relatedCaseId;
    public string relatedPoliceCaseId;
    public string facilityId;
    public FederalAuthorityResolution authorizationResolution;
    public bool hasAuthorizationResolution;
    public List<string> applicablePolicies = new List<string>();
    public FederalFieldDecision fieldDecision;
    public bool hasFieldDecision;
    public List<FederalPolicyDeviationRecord> deviationRecords = new List<FederalPolicyDeviationRecord>();
    public int budgetSourceInt; // mirror FederalSpendingSource / facility budget; stub
    public int documentationLevelInt;
    public int secrecyLevel;
    public long startedAtTicks;
    public long completedAtTicks;
    public int outcomeInt; // FederalOperationOutcome
    public List<int> violationFlags = new List<int>();
    public List<int> exposureFlags = new List<int>();
    public int complianceBandInt; // FederalComplianceBand
    public int operationalResultSub;
    public int legalResultSub;
    public int secrecyResultSub;
    public int politicalResultSub;
    public int evidenceIntelResultSub;
    public string summaryLine;
}

[Serializable]
public class FederalWorkflowLog
{
    public string logId;
    public string operationId;
    public string requestId;
    public long timestampTicks;
    public string eventType;
    public string actorAgentId;
    public string notes;
}

[Serializable]
public class FederalOperationLog
{
    public string logId;
    public long timestampTicks;
    public string operationId;
    public FederalActionType actionType;
    public int statusInt;
    public string requestedByAgentId;
    public string approvedByAgentId;
    public string responsibleAgentId;
    public string supervisingAgentId;
    public List<string> executedByAgentIds = new List<string>();
    public int targetTypeInt;
    public string targetId;
    public int outcomeInt;
    public List<int> violationFlags = new List<int>();
    public List<int> exposureFlags = new List<int>();
}
