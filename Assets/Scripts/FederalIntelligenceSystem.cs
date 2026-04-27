using System;
using System.Collections.Generic;
using UnityEngine;

public enum FederalIntelSourceType
{
    Informant = 0,
    UndercoverAgent = 1,
    DoubleAgent = 2,
    FieldObservation = 3,
    Press = 4,
    PoliceFile = 5,
    TaxFile = 6,
    CityRecord = 7,
    BusinessRecord = 8,
    PrisonSource = 9,
    HospitalSource = 10,
    AnonymousTip = 11,
    CoercedSource = 12,
    IllegalExtraction = 13,
    Unknown = 14
}

public enum FederalIntelCollectionMethod
{
    VoluntaryReport = 0,
    PaidInformation = 1,
    BlackmailThreat = 2,
    UndercoverContact = 3,
    Surveillance = 4,
    Tail = 5,
    DocumentAccess = 6,
    PoliceCaseAccess = 7,
    PressScan = 8,
    Interrogation = 9,
    CoercedInterrogation = 10,
    Torture = 11,
    IllegalEntry = 12,
    Wiretap = 13,
    BlackCashPurchase = 14
}

public enum FederalIntelVerificationStatus
{
    Unverified = 0,
    PartiallyVerified = 1,
    Corroborated = 2,
    Contradicted = 3,
    ProvenFalse = 4,
    OperationallyConfirmed = 5
}

public enum FederalIntelActionability
{
    None = 0,
    ArchiveOnly = 1,
    WatchTarget = 2,
    OpenActiveCase = 3,
    RecruitSource = 4,
    StartSurveillance = 5,
    PrepareOperation = 6,
    ImmediateAction = 7,
    SecurityThreat = 8
}

public enum FederalSourceStatus
{
    Potential = 0,
    Active = 1,
    Trusted = 2,
    Unstable = 3,
    Compromised = 4,
    Burned = 5,
    Dead = 6,
    TurnedAgainstBureau = 7,
    Unknown = 8
}

public enum FederalSourceMotivation
{
    Money = 0,
    Fear = 1,
    Revenge = 2,
    Ideology = 3,
    Survival = 4,
    Blackmail = 5,
    ReducedPunishment = 6,
    Protection = 7,
    Ambition = 8,
    PersonalHatred = 9
}

public enum FederalIntelTruthState
{
    True = 0,
    HalfTrue = 1,
    False = 2,
    Disinformation = 3,
    Stale = 4,
    PartialView = 5
}

public enum FederalIntelClassification
{
    Unclassified = 0,
    LevelA = 1,  // juniors
    LevelB = 2,  // intermediate
    LevelC = 3,  // seniors
    TopSecret = 4
}

[Serializable]
public class FederalSourceProfile
{
    public string sourceId;
    public int sourceTypeInt;
    public string realCharacterId;
    public string codeName;
    public string coverIdentityId;
    public string originalOrganizationId;
    public string handlerAgentId;
    public int currentStatusInt;
    public int motivationInt;
    public int accessLevel;
    public int reliability;
    public int loyaltyToBureau;
    public int loyaltyToOriginalOrg;
    public int fearLevel;
    public int greedLevel;
    public int blackmailPressure;
    public int exposureRisk;
    public int counterIntelRisk;
    public int usefulness;
    public long lastContactAt;
    public string origin;
    public string background;
    public int classificationLevelInt;
    public string exposureReason;
}

[Serializable]
public class FederalIntelItem
{
    public string intelId;
    public string sourceId;
    public int sourceTypeInt;
    public int collectionMethodInt;
    public int targetTypeInt;
    public string targetId;
    public string contentSummary;
    public string rawContent;
    public long receivedAt;
    public string collectedByAgentId;
    public string handlerAgentId;
    public int reliability;
    public int specificity;
    public int freshness;
    public int verificationStatusInt;
    public int deceptionRisk;
    public int counterIntelRisk;
    public int legalRisk;
    public int pressRisk;
    public int politicalRisk;
    public int exposureRisk;
    public int nationalSecurityRisk;
    public List<string> linkedFederalCaseIds = new List<string>();
    public List<string> linkedPoliceCaseIds = new List<string>();
    public int actionabilityInt;
    public int truthStateInt;
    public int classificationLevelInt;
}

[Serializable]
public class FederalDisinformationEvent
{
    public string eventId;
    public string intelId;
    public string targetId;
    public int dayIndex;
    public int severity;
    public string narrative;
}

public static class FederalIntelAccessControl
{
    public static int MaxClassificationForRank(FederalBureauRank rank)
    {
        if (rank >= FederalBureauRank.DeputyDirector) return (int)FederalIntelClassification.TopSecret;
        if (rank >= FederalBureauRank.UnitChief) return (int)FederalIntelClassification.LevelC;
        if (rank >= FederalBureauRank.SpecialAgent) return (int)FederalIntelClassification.LevelB;
        return (int)FederalIntelClassification.LevelA;
    }

    public static bool CanAccess(FederalAgentProfile agent, int classificationLevelInt)
    {
        if (agent == null) return false;
        int max = MaxClassificationForRank(agent.rank);
        return classificationLevelInt <= max;
    }
}

public static class FederalIntelViewResolver
{
    public static string GetVisibleSourceLabel(FederalAgentProfile viewer, FederalSourceProfile source)
    {
        if (source == null) return "unknown-source";
        bool canSee = FederalIntelAccessControl.CanAccess(viewer, source.classificationLevelInt);
        if (!canSee)
            return string.IsNullOrEmpty(source.codeName) ? "redacted-source" : source.codeName;
        return string.IsNullOrEmpty(source.realCharacterId) ? (source.codeName ?? "source") : source.realCharacterId;
    }

    public static FederalIntelItem BuildRedactedIntelView(FederalAgentProfile viewer, FederalIntelItem original)
    {
        if (original == null) return null;
        bool canSee = FederalIntelAccessControl.CanAccess(viewer, original.classificationLevelInt);
        if (canSee) return original;
        return new FederalIntelItem
        {
            intelId = original.intelId,
            sourceId = "redacted",
            sourceTypeInt = original.sourceTypeInt,
            collectionMethodInt = original.collectionMethodInt,
            targetTypeInt = original.targetTypeInt,
            targetId = "redacted-target",
            contentSummary = "[REDACTED] " + (original.contentSummary ?? string.Empty),
            rawContent = string.Empty,
            receivedAt = original.receivedAt,
            collectedByAgentId = "redacted",
            handlerAgentId = "redacted",
            reliability = original.reliability,
            specificity = original.specificity,
            freshness = original.freshness,
            verificationStatusInt = original.verificationStatusInt,
            deceptionRisk = original.deceptionRisk,
            counterIntelRisk = original.counterIntelRisk,
            legalRisk = original.legalRisk,
            pressRisk = original.pressRisk,
            politicalRisk = original.politicalRisk,
            exposureRisk = original.exposureRisk,
            nationalSecurityRisk = original.nationalSecurityRisk,
            actionabilityInt = original.actionabilityInt,
            truthStateInt = original.truthStateInt,
            classificationLevelInt = original.classificationLevelInt
        };
    }
}

public static class FederalIntelInvariants
{
    public static bool ValidateSource(FederalSourceProfile s, out string issue)
    {
        issue = null;
        if (s == null) { issue = "SourceProfile is null."; return false; }
        if (string.IsNullOrEmpty(s.sourceId)) { issue = "SourceProfile missing sourceId."; return false; }
        if (string.IsNullOrEmpty(s.realCharacterId) &&
            s.sourceTypeInt != (int)FederalIntelSourceType.Unknown &&
            s.sourceTypeInt != (int)FederalIntelSourceType.AnonymousTip)
        { issue = "SourceProfile requires realCharacterId unless Unknown/Anonymous."; return false; }
        if (s.sourceTypeInt == (int)FederalIntelSourceType.UndercoverAgent && string.IsNullOrEmpty(s.coverIdentityId))
        { issue = "UndercoverAgent requires coverIdentityId."; return false; }
        if (s.sourceTypeInt == (int)FederalIntelSourceType.DoubleAgent && string.IsNullOrEmpty(s.originalOrganizationId))
        { issue = "DoubleAgent requires originalOrganizationId."; return false; }
        if (s.currentStatusInt == (int)FederalSourceStatus.Compromised && string.IsNullOrEmpty(s.exposureReason))
        { issue = "Compromised source must carry exposureReason."; return false; }
        return true;
    }

    public static bool ValidateIntel(FederalIntelItem x, out string issue)
    {
        issue = null;
        if (x == null) { issue = "IntelItem is null."; return false; }
        if (x.sourceTypeInt < 0) { issue = "IntelItem missing sourceType."; return false; }
        if (x.collectionMethodInt == (int)FederalIntelCollectionMethod.Torture
            || x.collectionMethodInt == (int)FederalIntelCollectionMethod.IllegalEntry
            || x.collectionMethodInt == (int)FederalIntelCollectionMethod.CoercedInterrogation)
        {
            if (x.legalRisk < 70) { issue = "Illegal extraction method requires high legalRisk."; return false; }
        }
        if (x.actionabilityInt == (int)FederalIntelActionability.ImmediateAction && x.reliability < 45)
        { issue = "ImmediateAction requires reliability >= 45."; return false; }
        if (x.actionabilityInt == (int)FederalIntelActionability.SecurityThreat && x.nationalSecurityRisk < 70)
        { issue = "SecurityThreat requires nationalSecurityRisk >= 70."; return false; }
        return true;
    }
}

public static class FederalIntelAssessmentResolver
{
    public static int ComputeIntelValue(FederalIntelItem x, FederalSourceProfile source)
    {
        if (x == null) return 0;
        int sourceAccess = source != null ? Mathf.Clamp(source.accessLevel, 0, 100) : 25;
        int corroboration = 0;
        if (x.verificationStatusInt == (int)FederalIntelVerificationStatus.Corroborated) corroboration = 20;
        else if (x.verificationStatusInt == (int)FederalIntelVerificationStatus.OperationallyConfirmed) corroboration = 30;
        else if (x.verificationStatusInt == (int)FederalIntelVerificationStatus.PartiallyVerified) corroboration = 10;
        else if (x.verificationStatusInt == (int)FederalIntelVerificationStatus.Contradicted) corroboration = -25;
        else if (x.verificationStatusInt == (int)FederalIntelVerificationStatus.ProvenFalse) corroboration = -40;
        int strategic = Mathf.Clamp(x.nationalSecurityRisk + x.actionabilityInt * 5, 0, 120);
        int v = x.reliability + x.specificity + x.freshness + sourceAccess + corroboration + strategic
                - x.deceptionRisk - x.counterIntelRisk - x.legalRisk / 2;
        return Mathf.Clamp(v / 3, 0, 100);
    }

    public static int DeriveActionabilityFromScore(int score, int nsRisk, int verificationStatusInt)
    {
        if (score < 25) return (int)FederalIntelActionability.ArchiveOnly;
        if (score < 45) return (int)FederalIntelActionability.WatchTarget;
        if (score < 60) return (int)FederalIntelActionability.StartSurveillance;
        if (score < 75) return (int)FederalIntelActionability.OpenActiveCase;
        if (score < 88) return (int)FederalIntelActionability.PrepareOperation;
        if (nsRisk >= 70 && verificationStatusInt >= (int)FederalIntelVerificationStatus.PartiallyVerified)
            return (int)FederalIntelActionability.SecurityThreat;
        return (int)FederalIntelActionability.ImmediateAction;
    }
}

public static class FederalDisinformationResolver
{
    public static bool ShouldMarkDisinformation(FederalIntelItem x, int counterIntelPressure0to100, int day, int seed)
    {
        if (x == null) return false;
        unchecked
        {
            int h = day * 0x1B873593 ^ seed * 0x27D4EB2D ^ (x.intelId != null ? x.intelId.GetHashCode() : 0);
            int roll = Mathf.Abs(h) % 100;
            int chance = Mathf.Clamp(counterIntelPressure0to100 / 2 + x.deceptionRisk / 2, 0, 85);
            return roll < chance;
        }
    }
}

public static class FederalCounterIntelResolver
{
    public static int ComputeCounterIntelPressure(
        FederalCaseFileBureau c,
        FederalTargetBehaviorArchetype behavior,
        int orgLevelHint,
        int bureauExposure0to100,
        int recentLeaksHint)
    {
        if (c == null) return 0;
        int paranoia = behavior == FederalTargetBehaviorArchetype.Paranoid ? 25 :
            behavior == FederalTargetBehaviorArchetype.Conspiratorial ? 30 : 10;
        int discipline = behavior == FederalTargetBehaviorArchetype.Cautious ? 20 : 10;
        int p = paranoia + discipline + Mathf.Clamp(orgLevelHint * 8, 0, 30) + recentLeaksHint
                + bureauExposure0to100 / 3 + Mathf.Clamp(c.politicalRisk / 2, 0, 40);
        return Mathf.Clamp(p, 0, 100);
    }
}

