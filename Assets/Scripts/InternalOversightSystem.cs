using System;
using System.Collections.Generic;
using UnityEngine;

public enum TriggerType
{
    CitizenComplaint,
    OfficerComplaint,
    LethalForceAutoReview,
    SevereInjuryReview,
    BrokenEvidenceChain,
    ContradictoryReports,
    MissingDocumentation,
    SuspicionOfBribery,
    SuspicionOfLeak,
    ImproperSearch,
    ImproperArrest,
    ImproperDetention,
    PlantedEvidenceSuspicion,
    HiddenEvidenceSuspicion,
    SupervisorInitiated,
    PoliticalInitiated
}

public enum InternalSeverity
{
    Minor,
    Moderate,
    Major,
    Critical
}

public enum InternalReviewStatus
{
    Intake,
    Screening,
    Review,
    Investigation,
    OutcomeIssued,
    Closed
}

public enum InternalOutcomeType
{
    NoFinding,
    TechnicalFault,
    Misconduct,
    CorruptionConfirmed,
    Inconclusive,
    CoveredUp,
    Escalated
}

public enum InternalSanctionType
{
    None,
    InternalNote,
    Reprimand,
    Demotion,
    PromotionFreeze,
    Suspension,
    Reassignment,
    InternalCriminalInvestigation,
    ScapegoatDisposition,
    QuietClosurePolitical
}

[Serializable]
public class InternalRiskProfile
{
    public string officerId;
    public int misconductRisk;     // 0..100
    public int corruptionRisk;     // 0..100
    public int reportIntegrity;    // 0..100
    public int forceDiscipline;    // 0..100
    public int evidenceIntegrity;  // 0..100
    public int leakRisk;           // 0..100
    public int politicalProtection;// 0..100
    public int blackmailExposure;  // 0..100
}

[Serializable]
public class InternalReviewCase
{
    public string reviewId;
    public TriggerType triggerType;
    public InternalSeverity severity;
    public InternalReviewStatus status;
    public long openedAt;
    public string openedBy;
    public List<string> targetOfficerIds = new List<string>();
    public List<string> relatedActionIds = new List<string>();
    public List<string> relatedCaseIds = new List<string>();
    public List<string> relatedEvidenceIds = new List<string>();
    public List<string> complaintIds = new List<string>();
    public List<string> suspectedViolationTypes = new List<string>();
    public List<string> corruptionIndicators = new List<string>();
    public int politicalSensitivity; // 0..100
    public InternalOutcomeType outcomeType = InternalOutcomeType.Inconclusive;
    public InternalSanctionType sanctionType = InternalSanctionType.None;
    public string notes;
}

[Serializable]
public struct InternalAutoReviewContext
{
    public bool lethalForceUsed;
    public bool severeInjury;
    public bool evidenceChainBroken;
    public bool complaintReceived;
    public bool hasViolationFlag;
    public bool seriousActionMissingReport;
    public bool corruptionIntelReceived;
    public int contradictionBetweenOfficers; // 0..100
    public int repeatedBorderlineActions;    // count
}

public static class InternalReviewOpenResolver
{
    public static InternalReviewCase Open(
        TriggerType triggerType,
        string openedBy,
        IEnumerable<string> targetOfficerIds = null,
        IEnumerable<string> relatedCaseIds = null,
        IEnumerable<string> relatedActionIds = null,
        IEnumerable<string> relatedEvidenceIds = null,
        string notes = null)
    {
        InternalReviewCase review = new InternalReviewCase
        {
            reviewId = "ia_" + Guid.NewGuid().ToString("N"),
            triggerType = triggerType,
            severity = InternalSeverity.Moderate,
            status = InternalReviewStatus.Intake,
            openedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            openedBy = openedBy,
            notes = notes ?? string.Empty
        };

        if (targetOfficerIds != null) review.targetOfficerIds.AddRange(targetOfficerIds);
        if (relatedCaseIds != null) review.relatedCaseIds.AddRange(relatedCaseIds);
        if (relatedActionIds != null) review.relatedActionIds.AddRange(relatedActionIds);
        if (relatedEvidenceIds != null) review.relatedEvidenceIds.AddRange(relatedEvidenceIds);

        review.severity = InternalSeverityResolver.Resolve(review);
        review.status = InternalReviewStatus.Screening;
        return review;
    }
}

public static class InternalSeverityResolver
{
    public static InternalSeverity Resolve(InternalReviewCase review)
    {
        if (review == null)
            return InternalSeverity.Minor;

        return review.triggerType switch
        {
            TriggerType.LethalForceAutoReview => InternalSeverity.Critical,
            TriggerType.SevereInjuryReview => InternalSeverity.Major,
            TriggerType.BrokenEvidenceChain => InternalSeverity.Major,
            TriggerType.PlantedEvidenceSuspicion => InternalSeverity.Critical,
            TriggerType.HiddenEvidenceSuspicion => InternalSeverity.Critical,
            TriggerType.SuspicionOfBribery => InternalSeverity.Major,
            TriggerType.SuspicionOfLeak => InternalSeverity.Major,
            TriggerType.ImproperArrest => InternalSeverity.Major,
            TriggerType.ImproperSearch => InternalSeverity.Moderate,
            TriggerType.ImproperDetention => InternalSeverity.Moderate,
            TriggerType.MissingDocumentation => InternalSeverity.Minor,
            TriggerType.ContradictoryReports => InternalSeverity.Moderate,
            TriggerType.PoliticalInitiated => review.politicalSensitivity >= 70 ? InternalSeverity.Critical : InternalSeverity.Major,
            _ => InternalSeverity.Moderate
        };
    }
}

public static class InternalReportAuditResolver
{
    public static int ComputeReportIntegrityScore(bool reportMissing, bool contradictions, int completenessPercent)
    {
        int score = Mathf.Clamp(completenessPercent, 0, 100);
        if (reportMissing) score -= 55;
        if (contradictions) score -= 30;
        return Mathf.Clamp(score, 0, 100);
    }
}

public static class InternalEvidenceAuditResolver
{
    public static int ComputeEvidenceIntegrityScore(EvidenceItem[] evidenceItems)
    {
        if (evidenceItems == null || evidenceItems.Length == 0)
            return 60;
        int total = 0;
        int count = 0;
        for (int i = 0; i < evidenceItems.Length; i++)
        {
            EvidenceItem e = evidenceItems[i];
            if (e == null)
                continue;
            int local = e.integrity;
            if (e.chainStatus == EvidenceChainStatus.Broken) local -= 25;
            if (e.chainStatus == EvidenceChainStatus.Compromised) local -= 40;
            if (e.tamperRisk == EvidenceRiskLevel.High) local -= 30;
            total += Mathf.Clamp(local, 0, 100);
            count++;
        }
        if (count == 0)
            return 60;
        return Mathf.Clamp(Mathf.RoundToInt(total / (float)count), 0, 100);
    }
}

public static class InternalForceAuditResolver
{
    public static int ComputeForceDisciplineScore(bool forceUsed, bool lethalForceUsed, bool severeInjury, bool actionLawful)
    {
        int score = 85;
        if (forceUsed) score -= 10;
        if (lethalForceUsed) score -= 20;
        if (severeInjury) score -= 20;
        if (!actionLawful) score -= 25;
        return Mathf.Clamp(score, 0, 100);
    }
}

public static class InternalCorruptionResolver
{
    public static int ComputeCorruptionSuspicionScore(
        int officerCorruptionRisk,
        bool briberySignal,
        bool leakSignal,
        bool plantedEvidenceSignal,
        bool hiddenEvidenceSignal)
    {
        int score = Mathf.Clamp(officerCorruptionRisk, 0, 100);
        if (briberySignal) score += 25;
        if (leakSignal) score += 20;
        if (plantedEvidenceSignal) score += 30;
        if (hiddenEvidenceSignal) score += 25;
        return Mathf.Clamp(score, 0, 100);
    }
}

public static class InternalOutcomeResolver
{
    public static InternalOutcomeType Resolve(
        InternalReviewCase review,
        int reportIntegrityScore,
        int evidenceIntegrityScore,
        int forceDisciplineScore,
        int corruptionSuspicionScore,
        bool policyCoverUpSignal)
    {
        if (review == null)
            return InternalOutcomeType.Inconclusive;

        if (policyCoverUpSignal)
            return InternalOutcomeType.CoveredUp;
        if (corruptionSuspicionScore >= 80)
            return InternalOutcomeType.CorruptionConfirmed;
        if (review.severity == InternalSeverity.Critical && (reportIntegrityScore < 35 || evidenceIntegrityScore < 35 || forceDisciplineScore < 35))
            return InternalOutcomeType.Escalated;
        if (reportIntegrityScore < 45 || evidenceIntegrityScore < 45 || forceDisciplineScore < 45)
            return InternalOutcomeType.Misconduct;
        if (reportIntegrityScore < 65 || evidenceIntegrityScore < 65)
            return InternalOutcomeType.TechnicalFault;
        return InternalOutcomeType.NoFinding;
    }
}

public static class InternalSanctionResolver
{
    public static InternalSanctionType Resolve(InternalOutcomeType outcome, InternalSeverity severity, bool politicalShieldActive)
    {
        if (politicalShieldActive && (outcome == InternalOutcomeType.Misconduct || outcome == InternalOutcomeType.CorruptionConfirmed))
            return InternalSanctionType.QuietClosurePolitical;

        return outcome switch
        {
            InternalOutcomeType.NoFinding => InternalSanctionType.None,
            InternalOutcomeType.TechnicalFault => severity == InternalSeverity.Minor ? InternalSanctionType.InternalNote : InternalSanctionType.Reprimand,
            InternalOutcomeType.Misconduct => severity switch
            {
                InternalSeverity.Minor => InternalSanctionType.Reprimand,
                InternalSeverity.Moderate => InternalSanctionType.PromotionFreeze,
                InternalSeverity.Major => InternalSanctionType.Suspension,
                _ => InternalSanctionType.Reassignment
            },
            InternalOutcomeType.CorruptionConfirmed => InternalSanctionType.InternalCriminalInvestigation,
            InternalOutcomeType.CoveredUp => InternalSanctionType.ScapegoatDisposition,
            InternalOutcomeType.Escalated => InternalSanctionType.Reassignment,
            _ => InternalSanctionType.None
        };
    }
}

public static class InternalCoverUpResolver
{
    public static bool ShouldCoverUp(
        InternalReviewCase review,
        int stationCorruption,
        int oversightCorruption,
        int politicalPressure,
        int targetPoliticalProtection)
    {
        if (review == null)
            return false;
        float score =
            stationCorruption * 0.25f +
            oversightCorruption * 0.25f +
            politicalPressure * 0.25f +
            targetPoliticalProtection * 0.25f;
        if (review.severity == InternalSeverity.Critical)
            score -= 10f; // harder to suppress, not impossible
        return score >= 68f;
    }
}

public static class InternalAutoReviewTriggers
{
    public static List<TriggerType> CollectTriggers(InternalAutoReviewContext ctx)
    {
        List<TriggerType> list = new List<TriggerType>();
        if (ctx.lethalForceUsed) list.Add(TriggerType.LethalForceAutoReview);
        if (ctx.severeInjury) list.Add(TriggerType.SevereInjuryReview);
        if (ctx.evidenceChainBroken) list.Add(TriggerType.BrokenEvidenceChain);
        if (ctx.complaintReceived && ctx.hasViolationFlag) list.Add(TriggerType.CitizenComplaint);
        if (ctx.contradictionBetweenOfficers >= 55) list.Add(TriggerType.ContradictoryReports);
        if (ctx.seriousActionMissingReport) list.Add(TriggerType.MissingDocumentation);
        if (ctx.corruptionIntelReceived) list.Add(TriggerType.SuspicionOfBribery);
        if (ctx.repeatedBorderlineActions >= 3) list.Add(TriggerType.SupervisorInitiated);
        return list;
    }
}

public static class InternalRiskProfileUpdater
{
    public static void ApplyReviewOutcome(
        InternalRiskProfile profile,
        InternalOutcomeType outcome,
        InternalSeverity severity)
    {
        if (profile == null)
            return;

        int sevMul = severity switch
        {
            InternalSeverity.Minor => 1,
            InternalSeverity.Moderate => 2,
            InternalSeverity.Major => 3,
            _ => 4
        };

        switch (outcome)
        {
            case InternalOutcomeType.NoFinding:
                profile.reportIntegrity = Mathf.Clamp(profile.reportIntegrity + 2, 0, 100);
                profile.forceDiscipline = Mathf.Clamp(profile.forceDiscipline + 1, 0, 100);
                break;
            case InternalOutcomeType.TechnicalFault:
                profile.misconductRisk = Mathf.Clamp(profile.misconductRisk + 2 * sevMul, 0, 100);
                profile.reportIntegrity = Mathf.Clamp(profile.reportIntegrity - 5 * sevMul, 0, 100);
                break;
            case InternalOutcomeType.Misconduct:
                profile.misconductRisk = Mathf.Clamp(profile.misconductRisk + 5 * sevMul, 0, 100);
                profile.forceDiscipline = Mathf.Clamp(profile.forceDiscipline - 6 * sevMul, 0, 100);
                profile.reportIntegrity = Mathf.Clamp(profile.reportIntegrity - 6 * sevMul, 0, 100);
                profile.blackmailExposure = Mathf.Clamp(profile.blackmailExposure + 4 * sevMul, 0, 100);
                break;
            case InternalOutcomeType.CorruptionConfirmed:
                profile.corruptionRisk = Mathf.Clamp(profile.corruptionRisk + 8 * sevMul, 0, 100);
                profile.leakRisk = Mathf.Clamp(profile.leakRisk + 6 * sevMul, 0, 100);
                profile.evidenceIntegrity = Mathf.Clamp(profile.evidenceIntegrity - 8 * sevMul, 0, 100);
                profile.blackmailExposure = Mathf.Clamp(profile.blackmailExposure + 7 * sevMul, 0, 100);
                break;
            case InternalOutcomeType.CoveredUp:
                profile.politicalProtection = Mathf.Clamp(profile.politicalProtection + 6 * sevMul, 0, 100);
                profile.blackmailExposure = Mathf.Clamp(profile.blackmailExposure + 6 * sevMul, 0, 100);
                profile.corruptionRisk = Mathf.Clamp(profile.corruptionRisk + 4 * sevMul, 0, 100);
                break;
            case InternalOutcomeType.Escalated:
                profile.misconductRisk = Mathf.Clamp(profile.misconductRisk + 4 * sevMul, 0, 100);
                profile.blackmailExposure = Mathf.Clamp(profile.blackmailExposure + 3 * sevMul, 0, 100);
                break;
        }
    }
}

public static class InternalToCaseEffects
{
    public static void Apply(CaseFile caseFile, InternalOutcomeType outcome, InternalSeverity severity)
    {
        if (caseFile == null)
            return;

        int sev = severity switch
        {
            InternalSeverity.Minor => 1,
            InternalSeverity.Moderate => 2,
            InternalSeverity.Major => 3,
            _ => 4
        };

        switch (outcome)
        {
            case InternalOutcomeType.TechnicalFault:
                caseFile.legalIntegrityScore = Mathf.Clamp(caseFile.legalIntegrityScore - 3 * sev, 0, 100);
                caseFile.internalReliabilityScore = Mathf.Clamp(caseFile.internalReliabilityScore - 4 * sev, 0, 100);
                caseFile.fragilityScore = Mathf.Clamp(caseFile.fragilityScore + 3 * sev, 0, 100);
                break;
            case InternalOutcomeType.Misconduct:
                caseFile.legalIntegrityScore = Mathf.Clamp(caseFile.legalIntegrityScore - 6 * sev, 0, 100);
                caseFile.internalReliabilityScore = Mathf.Clamp(caseFile.internalReliabilityScore - 7 * sev, 0, 100);
                caseFile.fragilityScore = Mathf.Clamp(caseFile.fragilityScore + 5 * sev, 0, 100);
                break;
            case InternalOutcomeType.CorruptionConfirmed:
            case InternalOutcomeType.CoveredUp:
                caseFile.legalIntegrityScore = Mathf.Clamp(caseFile.legalIntegrityScore - 8 * sev, 0, 100);
                caseFile.internalReliabilityScore = Mathf.Clamp(caseFile.internalReliabilityScore - 9 * sev, 0, 100);
                caseFile.corruptionRisk = Mathf.Clamp(caseFile.corruptionRisk + 7 * sev, 0, 100);
                caseFile.fragilityScore = Mathf.Clamp(caseFile.fragilityScore + 7 * sev, 0, 100);
                break;
        }

        CaseIntegrityResolver.RecomputeLegalIntegrity(caseFile);
        CaseStrengthResolver.RecomputeCaseStrength(caseFile);
        CaseEscalationResolver.UpdateCaseStatusByStrength(caseFile);
    }
}
