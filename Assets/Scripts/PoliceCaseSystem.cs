using System;
using System.Collections.Generic;
using UnityEngine;

public enum PoliceCaseType
{
    Incident,
    Subject,
    Organization,
    Location,
    Strategic
}

public enum PoliceCaseStatus
{
    Preliminary,
    Active,
    Operational,
    Dormant,
    Frozen,
    Closed,
    Archived,
    ReferredForward,
    Compromised
}

public enum CasePriority
{
    Low,
    Medium,
    High,
    Critical
}

public enum CaseHeatLevel
{
    Low,
    Medium,
    High,
    Critical
}

public enum CaseLegalIntegrity
{
    Clean,
    Questionable,
    Contaminated,
    Compromised
}

public enum CaseEvidenceStrength
{
    None,
    Weak,
    Moderate,
    Strong,
    Overwhelming
}

public enum CaseStrengthBand
{
    Weak,
    Developing,
    Solid,
    Strong,
    Critical
}

public enum CaseEntityRole
{
    Suspect,
    Witness,
    Victim,
    OfficerInvolved,
    Source,
    RelatedEntity
}

public enum CaseIrregularityType
{
    WeakOpeningGround,
    ImproperSearch,
    ImproperDetention,
    ImproperArrest,
    MissingApproval,
    MissingDocumentation,
    BrokenEvidenceChain,
    FalseReport,
    CoercedStatement,
    CorruptionInfluence,
    IntelManipulation
}

public enum CaseIrregularitySeverity
{
    Minor,
    Moderate,
    Severe
}

public enum CaseEscalationCapability
{
    BasicInquiry,
    IdentificationAndQuestioning,
    StructuredSurveillance,
    ApprovalRequestTrack,
    TacticalOperations,
    FullEnforcement
}

[Serializable]
public class CaseLinkedEntity
{
    public string EntityId;
    public SuspicionSubjectType EntityType;
    public CaseEntityRole Role;
}

[Serializable]
public class GroundsLogEntry
{
    public long At;
    public string OfficerId;
    public LegalGroundType Ground;
    public string Notes;
}

[Serializable]
public class ActionLogEntry
{
    public long At;
    public string ActionId;
    public ActionType ActionType;
    public string OfficerId;
    public string Summary;
}

[Serializable]
public class ReportLogEntry
{
    public long At;
    public string ReportId;
    public string ReportType;
    public string OfficerId;
    public bool IsComplete;
}

[Serializable]
public class IrregularityRecord
{
    public CaseIrregularityType irregularityType;
    public CaseIrregularitySeverity severity;
    public string linkedActionId;
    public string linkedOfficerId;
    public long discoveredAt;
    public bool hiddenUntilDiscovered;
    public int impactOnCase; // 0..100
}

[Serializable]
public class CaseFile
{
    public string caseId;
    public PoliceCaseType caseType;
    public string title;
    public PoliceCaseStatus status;
    public long openedAt;
    public string openedByOfficerId;
    public string owningStationId;
    public string assignedLeadOfficerId;
    public string supervisingOfficerId;
    public List<CaseLinkedEntity> linkedEntities = new List<CaseLinkedEntity>();
    public List<string> linkedCases = new List<string>();
    public CasePriority casePriority = CasePriority.Medium;
    public CaseHeatLevel caseHeat = CaseHeatLevel.Low;
    public CaseLegalIntegrity legalIntegrity = CaseLegalIntegrity.Clean;
    public CaseEvidenceStrength evidenceStrength = CaseEvidenceStrength.None;
    public int intelligenceWeight; // 0..100
    public int corruptionRisk; // 0..100
    public int publicVisibility; // 0..100
    public string notes;

    // Score channels (0..100)
    public int evidenceStrengthScore;
    public int legalIntegrityScore;
    public int intelligenceSupportScore;
    public int internalReliabilityScore;
    public int fragilityScore;
    public int caseStrengthScore;
    public CaseStrengthBand caseStrengthBand = CaseStrengthBand.Weak;

    public List<GroundsLogEntry> groundsLog = new List<GroundsLogEntry>();
    public List<CaseLinkedEntity> subjectList = new List<CaseLinkedEntity>();
    public List<string> evidenceList = new List<string>();
    public List<string> intelList = new List<string>();
    public List<ActionLogEntry> actionLog = new List<ActionLogEntry>();
    public List<ReportLogEntry> reportLog = new List<ReportLogEntry>();
    public List<IrregularityRecord> irregularities = new List<IrregularityRecord>();
}

public static class CaseOpenResolver
{
    public static CaseFile OpenCase(
        PoliceCaseType caseType,
        string title,
        string openedByOfficerId,
        string owningStationId,
        string assignedLeadOfficerId,
        string supervisingOfficerId,
        LegalGroundType openingGround,
        string openingNotes = null)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        CaseFile file = new CaseFile
        {
            caseId = "case_" + Guid.NewGuid().ToString("N"),
            caseType = caseType,
            title = string.IsNullOrWhiteSpace(title) ? caseType + " Case" : title.Trim(),
            status = PoliceCaseStatus.Preliminary,
            openedAt = now,
            openedByOfficerId = openedByOfficerId,
            owningStationId = owningStationId,
            assignedLeadOfficerId = assignedLeadOfficerId,
            supervisingOfficerId = supervisingOfficerId,
            evidenceStrengthScore = 0,
            legalIntegrityScore = 75,
            intelligenceSupportScore = 15,
            internalReliabilityScore = 60,
            fragilityScore = 20,
            notes = openingNotes ?? string.Empty
        };

        file.groundsLog.Add(new GroundsLogEntry
        {
            At = now,
            OfficerId = openedByOfficerId,
            Ground = openingGround,
            Notes = string.IsNullOrWhiteSpace(openingNotes) ? "Case opened." : openingNotes.Trim()
        });

        CaseStrengthResolver.RecomputeCaseStrength(file);
        return file;
    }
}

public static class CaseUpdateResolver
{
    public static void ApplyActionResolution(CaseFile file, PoliceActionResolution actionResolution, string actionId, ActionType actionType, string officerId, string summary)
    {
        if (file == null || actionResolution == null)
            return;

        file.actionLog.Add(new ActionLogEntry
        {
            At = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ActionId = actionId,
            ActionType = actionType,
            OfficerId = officerId,
            Summary = summary ?? string.Empty
        });

        if (actionResolution.legalityLevel == LegalityLevel.Lawful)
            file.legalIntegrityScore = Mathf.Clamp(file.legalIntegrityScore + 2, 0, 100);
        else if (actionResolution.legalityLevel == LegalityLevel.Borderline)
            file.legalIntegrityScore = Mathf.Clamp(file.legalIntegrityScore - 6, 0, 100);
        else
            file.legalIntegrityScore = Mathf.Clamp(file.legalIntegrityScore - 15, 0, 100);

        file.fragilityScore = Mathf.Clamp(file.fragilityScore + Mathf.RoundToInt(actionResolution.riskOfViolation * 10f), 0, 100);

        if (actionResolution.misconductSeverity != MisconductSeverity.None)
        {
            file.irregularities.Add(new IrregularityRecord
            {
                irregularityType = ResolveIrregularityType(actionResolution.generatedFlags),
                severity = ResolveIrregularitySeverity(actionResolution.misconductSeverity),
                linkedActionId = actionId,
                linkedOfficerId = officerId,
                discoveredAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                hiddenUntilDiscovered = false,
                impactOnCase = actionResolution.misconductSeverity switch
                {
                    MisconductSeverity.Minor => 8,
                    MisconductSeverity.Moderate => 18,
                    MisconductSeverity.Severe => 35,
                    _ => 0
                }
            });
        }

        CaseIntegrityResolver.RecomputeLegalIntegrity(file);
        CaseStrengthResolver.RecomputeCaseStrength(file);
        CaseEscalationResolver.UpdateCaseStatusByStrength(file);
    }

    public static void AddIntel(CaseFile file, IntelItem intelItem)
    {
        if (file == null || intelItem == null)
            return;
        file.intelList.Add(intelItem.id);
        int add = intelItem.assessedLevel switch
        {
            IntelAssessedLevel.Rumor => 1,
            IntelAssessedLevel.WeakLead => 3,
            IntelAssessedLevel.PlausibleLead => 6,
            IntelAssessedLevel.Corroborated => 10,
            IntelAssessedLevel.StrongIntel => 14,
            IntelAssessedLevel.ConfirmedOperationalIntel => 18,
            _ => 2
        };
        file.intelligenceSupportScore = Mathf.Clamp(file.intelligenceSupportScore + add, 0, 100);
        file.intelligenceWeight = Mathf.Clamp(file.intelligenceWeight + add / 2, 0, 100);
        CaseStrengthResolver.RecomputeCaseStrength(file);
    }

    public static void AddEvidence(CaseFile file, string evidenceId, int qualityContribution = 10)
    {
        if (file == null || string.IsNullOrWhiteSpace(evidenceId))
            return;
        file.evidenceList.Add(evidenceId.Trim());
        file.evidenceStrengthScore = Mathf.Clamp(file.evidenceStrengthScore + Mathf.Clamp(qualityContribution, 1, 30), 0, 100);
        CaseStrengthResolver.RecomputeCaseStrength(file);
        CaseEscalationResolver.UpdateCaseStatusByStrength(file);
    }

    private static CaseIrregularityType ResolveIrregularityType(ViolationFlag[] flags)
    {
        if (flags == null || flags.Length == 0)
            return CaseIrregularityType.MissingDocumentation;
        for (int i = 0; i < flags.Length; i++)
        {
            switch (flags[i])
            {
                case ViolationFlag.WeakGroundForAction: return CaseIrregularityType.WeakOpeningGround;
                case ViolationFlag.ImproperSearch: return CaseIrregularityType.ImproperSearch;
                case ViolationFlag.ImproperDetentionLength: return CaseIrregularityType.ImproperDetention;
                case ViolationFlag.ImproperArrest: return CaseIrregularityType.ImproperArrest;
                case ViolationFlag.MissingApproval: return CaseIrregularityType.MissingApproval;
                case ViolationFlag.FalseReportRisk: return CaseIrregularityType.FalseReport;
                case ViolationFlag.EvidenceChainRisk: return CaseIrregularityType.BrokenEvidenceChain;
            }
        }
        return CaseIrregularityType.MissingDocumentation;
    }

    private static CaseIrregularitySeverity ResolveIrregularitySeverity(MisconductSeverity severity)
    {
        return severity switch
        {
            MisconductSeverity.Minor => CaseIrregularitySeverity.Minor,
            MisconductSeverity.Moderate => CaseIrregularitySeverity.Moderate,
            MisconductSeverity.Severe => CaseIrregularitySeverity.Severe,
            _ => CaseIrregularitySeverity.Minor
        };
    }
}

public static class CaseLinkResolver
{
    public static void LinkCases(CaseFile a, CaseFile b)
    {
        if (a == null || b == null || a.caseId == b.caseId)
            return;
        if (!a.linkedCases.Contains(b.caseId))
            a.linkedCases.Add(b.caseId);
        if (!b.linkedCases.Contains(a.caseId))
            b.linkedCases.Add(a.caseId);
    }
}

public static class CaseIntegrityResolver
{
    public static void RecomputeLegalIntegrity(CaseFile file)
    {
        if (file == null)
            return;

        int penalty = 0;
        for (int i = 0; i < file.irregularities.Count; i++)
        {
            IrregularityRecord ir = file.irregularities[i];
            if (ir == null)
                continue;
            penalty += ir.severity switch
            {
                CaseIrregularitySeverity.Minor => 3,
                CaseIrregularitySeverity.Moderate => 8,
                CaseIrregularitySeverity.Severe => 16,
                _ => 3
            };
        }

        int missingReports = 0;
        for (int i = 0; i < file.reportLog.Count; i++)
        {
            ReportLogEntry r = file.reportLog[i];
            if (r != null && !r.IsComplete)
                missingReports++;
        }
        penalty += missingReports * 3;

        file.legalIntegrityScore = Mathf.Clamp(file.legalIntegrityScore - penalty, 0, 100);
        file.legalIntegrity = file.legalIntegrityScore switch
        {
            >= 75 => CaseLegalIntegrity.Clean,
            >= 50 => CaseLegalIntegrity.Questionable,
            >= 30 => CaseLegalIntegrity.Contaminated,
            _ => CaseLegalIntegrity.Compromised
        };
    }
}

public static class CaseStrengthResolver
{
    public static void RecomputeCaseStrength(CaseFile file)
    {
        if (file == null)
            return;

        int strength =
            Mathf.RoundToInt(file.evidenceStrengthScore * 0.35f) +
            Mathf.RoundToInt(file.legalIntegrityScore * 0.25f) +
            Mathf.RoundToInt(file.intelligenceSupportScore * 0.15f) +
            Mathf.RoundToInt(file.internalReliabilityScore * 0.15f) -
            Mathf.RoundToInt(file.fragilityScore * 0.10f);
        file.caseStrengthScore = Mathf.Clamp(strength, 0, 100);

        file.caseStrengthBand = file.caseStrengthScore switch
        {
            < 25 => CaseStrengthBand.Weak,
            < 45 => CaseStrengthBand.Developing,
            < 65 => CaseStrengthBand.Solid,
            < 85 => CaseStrengthBand.Strong,
            _ => CaseStrengthBand.Critical
        };

        file.evidenceStrength = file.evidenceStrengthScore switch
        {
            < 10 => CaseEvidenceStrength.None,
            < 30 => CaseEvidenceStrength.Weak,
            < 55 => CaseEvidenceStrength.Moderate,
            < 80 => CaseEvidenceStrength.Strong,
            _ => CaseEvidenceStrength.Overwhelming
        };
    }
}

public static class CaseDecayResolver
{
    public static void ApplyDecay(CaseFile file, int idleTurns)
    {
        if (file == null || idleTurns <= 0)
            return;

        int evidenceDecay = idleTurns * 2;
        int intelDecay = idleTurns * 3;
        int reliabilityDecay = idleTurns * 2;

        file.evidenceStrengthScore = Mathf.Clamp(file.evidenceStrengthScore - evidenceDecay, 0, 100);
        file.intelligenceSupportScore = Mathf.Clamp(file.intelligenceSupportScore - intelDecay, 0, 100);
        file.internalReliabilityScore = Mathf.Clamp(file.internalReliabilityScore - reliabilityDecay, 0, 100);
        file.fragilityScore = Mathf.Clamp(file.fragilityScore + idleTurns * 3, 0, 100);

        if (idleTurns >= 5 && file.status == PoliceCaseStatus.Active)
            file.status = PoliceCaseStatus.Dormant;
        if (idleTurns >= 10)
            file.status = PoliceCaseStatus.Frozen;

        CaseStrengthResolver.RecomputeCaseStrength(file);
    }
}

public static class CaseEscalationResolver
{
    public static CaseEscalationCapability ResolveCapability(CaseFile file)
    {
        if (file == null)
            return CaseEscalationCapability.BasicInquiry;

        if (file.caseStrengthScore < 25)
            return CaseEscalationCapability.BasicInquiry;
        if (file.caseStrengthScore < 45)
            return CaseEscalationCapability.IdentificationAndQuestioning;
        if (file.caseStrengthScore < 65)
            return CaseEscalationCapability.StructuredSurveillance;
        if (file.caseStrengthScore < 85)
            return CaseEscalationCapability.ApprovalRequestTrack;
        if (file.caseStrengthScore < 95)
            return CaseEscalationCapability.TacticalOperations;
        return CaseEscalationCapability.FullEnforcement;
    }

    public static void UpdateCaseStatusByStrength(CaseFile file)
    {
        if (file == null)
            return;
        if (file.legalIntegrity == CaseLegalIntegrity.Compromised && file.caseStrengthScore < 45)
        {
            file.status = PoliceCaseStatus.Compromised;
            return;
        }

        file.status = file.caseStrengthScore switch
        {
            < 20 => PoliceCaseStatus.Preliminary,
            < 55 => PoliceCaseStatus.Active,
            < 80 => PoliceCaseStatus.Operational,
            _ => PoliceCaseStatus.ReferredForward
        };
    }
}
