using System;
using System.Collections.Generic;
using UnityEngine;

public enum FederalPolicyOwnerScope
{
    BureauWide = 0,
    DeputyPortfolio = 1,
    Division = 2,
    Unit = 3,
    Team = 4,
    Operation = 5
}

public enum FederalPolicyDomain
{
    OrganizedCrime = 0,
    Prohibition = 1,
    DangerousSubstances = 2,
    PoliceRelations = 3,
    UseOfForce = 4,
    LethalForce = 5,
    UndercoverOps = 6,
    Informants = 7,
    EvidenceHandling = 8,
    PublicExposure = 9,
    PoliticalRisk = 10,
    BudgetUse = 11,
    BlackCashTolerance = 12,
    CaseTakeover = 13,
    Raids = 14,
    Surveillance = 15,
    Detentions = 16,
    CoverUps = 17
}

public enum FederalPolicyStance
{
    Avoid = 0,
    Restrict = 1,
    Neutral = 2,
    Prefer = 3,
    Prioritize = 4
}

public enum FederalPolicyStrictness
{
    Advisory = 0,
    Preferred = 1,
    Mandatory = 2,
    Forbidden = 3
}

public enum FederalPolicyVisibility
{
    Unknown = 0,
    Rumored = 1,
    PartiallyKnown = 2,
    Known = 3,
    Confirmed = 4
}

/// <summary>Director operating mode after calibration; drives <see cref="FederalStrategyBiasResolver"/>.</summary>
public enum FederalDirectorPolicyMode
{
    AggressiveEnforcement = 0,
    SilentInfiltration = 1,
    PublicLegitimacy = 2,
    DamageControl = 3,
    PoliticalSurvival = 4,
    StrategicDecapitation = 5,
    PoliceOverride = 6
}

[Serializable]
public class FederalPolicyProfile
{
    public string policyId;
    public string ownerAgentId;
    public int ownerRank;
    public int ownerScopeInt; // FederalPolicyOwnerScope
    public int domainInt; // FederalPolicyDomain
    public int stanceInt;
    public int strictnessInt;
    public int priority;
    public bool active;
    public bool publiclyDeclared;
    public int visibilityInt;
    public int deputyPortfolioInt; // when scope = DeputyPortfolio, else 0
    public string divisionId;
}

[Serializable]
public class FederalLeadershipAuthorityProfile
{
    public string agentId;
    public int respectFromSubordinates;
    public int fearFromSubordinates;
    public int legitimacy;
    public int policyClarity;
    public int enforcementConsistency;
    public int punishmentCredibility;
    public int politicalProtection;
}

[Serializable]
public class FederalPolicyConflictRecord
{
    public string conflictId;
    public List<string> involvedPolicyIds = new List<string>();
    public string highestAuthorityPolicyId;
    public string fieldDominantPolicyId;
    public int conflictSeverity;
    public string resolvedBy; // e.g. Director|Strictness
    public int dayIndex;
    public long createdTicks;
}

[Serializable]
public class FederalPolicyInfluenceResult
{
    public int influenceScore0to100;
    public int ownerAuthority0to100;
    public int strictnessWeight0to100;
    public int fieldResistance0to100;
    public string primaryPolicyId;
    public int domainInt;
}

[Serializable]
public class FederalDirectorPolicyCalibration
{
    public string calibrationId;
    public string directorAgentId;
    public int day;
    public int year;
    public int quarter; // 1-4
    public int ministerPressure;
    public int governorPressure;
    public int electionPressure;
    public int pressPressure;
    public int publicTrust0to100;
    public int organizedCrimeThreat;
    public int visibleSuccessDemand;
    public int policeFailurePressure;
    public int recentScandalRisk;
    public int budgetPressure;
    public int realImpactScore0to100;
    public int politicalAppearanceScore0to100;
    public int selectedPolicyModeInt;
    public int declaredPolicyModeInt;
    public int hiddenPolicyModeInt; // 0 = unused (future)
    public List<string> reasonTags = new List<string>();
    public int visibilityLevel; // FederalPolicyVisibility
    public long createdAtTicks;
}
