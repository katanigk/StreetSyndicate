using System;
using System.Collections.Generic;
using UnityEngine;

public enum EvidenceType
{
    Physical,
    Documentary,
    Testimonial,
    Intelligence,
    Observational,
    Behavioral
}

public enum EvidenceStrengthLevel
{
    None,
    Weak,
    Moderate,
    Strong,
    Critical
}

public enum EvidenceLegalStatus
{
    Lawful,
    Questionable,
    Unlawful,
    Unknown
}

public enum EvidenceChainStatus
{
    Intact,
    Partial,
    Broken,
    Compromised
}

public enum EvidenceRiskLevel
{
    Low,
    Medium,
    High
}

public enum EvidenceWeightKind
{
    PrimaryEvidence,
    SupportingEvidence,
    CorroboratingEvidence,
    ContextEvidence,
    TriggerEvidence
}

public enum EvidenceStatementType
{
    Voluntary,
    Guided,
    Pressured,
    Coerced,
    Incentivized,
    AnonymousSourceLinked
}

[Serializable]
public class EvidenceItem
{
    public string evidenceId;
    public EvidenceType evidenceType;
    public string subtype;
    public List<string> linkedCaseIds = new List<string>();
    public List<string> linkedSubjects = new List<string>();
    public List<string> linkedLocations = new List<string>();
    public string discoveredByOfficerId;
    public long discoveredAt;
    public string discoveredLocationId;
    public string obtainedByActionId;
    public string sourceType;
    public string sourceId;
    public string rawDescription;
    public string interpretedMeaning;
    public EvidenceStrengthLevel strength;
    public EvidenceLegalStatus legalStatus = EvidenceLegalStatus.Unknown;
    public EvidenceChainStatus chainStatus = EvidenceChainStatus.Intact;
    public EvidenceRiskLevel contaminationRisk = EvidenceRiskLevel.Low;
    public EvidenceRiskLevel tamperRisk = EvidenceRiskLevel.Low;
    public int visibilityLevel; // 0..100
    public string notes;

    public EvidenceWeightKind weightKind = EvidenceWeightKind.SupportingEvidence;

    // 0..100 score channels
    public int directness;
    public int reliability;
    public int specificity;
    public int corroboration;
    public int integrity;
    public int strengthScore;
}

[Serializable]
public class TestimonialEvidence
{
    public string witnessId;
    public string statementId;
    public EvidenceStatementType statementType;
    public int reliability; // 0..100
    public int coercionRisk; // 0..100
    public int contradictionRisk; // 0..100
    public int retractionRisk; // 0..100
    public List<string> linkedSubjects = new List<string>();
}

[Serializable]
public class ConfessionEvidenceMeta
{
    public string confessionMethod;
    public int coercionRisk; // 0..100
    public bool corroborationRequired = true;
    public int legalIntegrityImpact; // -100..100
}

[Serializable]
public class ChainOfCustodyRecord
{
    public string recordId;
    public string evidenceId;
    public string fromActorId;
    public string toActorId;
    public long transferredAt;
    public string transferReason;
    public string sealStatus;
    public string notes;
}

public static class EvidenceCreationResolver
{
    public static EvidenceItem Create(
        EvidenceType type,
        string subtype,
        string discoveredByOfficerId,
        string discoveredLocationId,
        string obtainedByActionId,
        string sourceType,
        string sourceId,
        string rawDescription)
    {
        return new EvidenceItem
        {
            evidenceId = "ev_" + Guid.NewGuid().ToString("N"),
            evidenceType = type,
            subtype = subtype ?? string.Empty,
            discoveredByOfficerId = discoveredByOfficerId,
            discoveredAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            discoveredLocationId = discoveredLocationId,
            obtainedByActionId = obtainedByActionId,
            sourceType = sourceType ?? string.Empty,
            sourceId = sourceId ?? string.Empty,
            rawDescription = rawDescription ?? string.Empty,
            interpretedMeaning = string.Empty,
            strength = EvidenceStrengthLevel.None,
            legalStatus = EvidenceLegalStatus.Unknown,
            chainStatus = EvidenceChainStatus.Intact,
            contaminationRisk = EvidenceRiskLevel.Low,
            tamperRisk = EvidenceRiskLevel.Low,
            visibilityLevel = 20,
            directness = 20,
            reliability = 40,
            specificity = 40,
            corroboration = 0,
            integrity = 70
        };
    }
}

public static class EvidenceLegalityResolver
{
    public static EvidenceLegalStatus ResolveFromAction(PoliceActionResolution actionResolution)
    {
        if (actionResolution == null)
            return EvidenceLegalStatus.Unknown;
        return actionResolution.legalityLevel switch
        {
            LegalityLevel.Lawful => EvidenceLegalStatus.Lawful,
            LegalityLevel.Borderline => EvidenceLegalStatus.Questionable,
            LegalityLevel.Unlawful => EvidenceLegalStatus.Unlawful,
            _ => EvidenceLegalStatus.Unknown
        };
    }

    public static void ApplyLegality(EvidenceItem item, PoliceActionResolution actionResolution)
    {
        if (item == null)
            return;
        item.legalStatus = ResolveFromAction(actionResolution);
    }
}

public static class EvidenceChainResolver
{
    public static ChainOfCustodyRecord Transfer(
        EvidenceItem item,
        string fromActorId,
        string toActorId,
        string reason,
        string sealStatus,
        string notes)
    {
        if (item == null)
            return null;

        ChainOfCustodyRecord rec = new ChainOfCustodyRecord
        {
            recordId = "chain_" + Guid.NewGuid().ToString("N"),
            evidenceId = item.evidenceId,
            fromActorId = fromActorId,
            toActorId = toActorId,
            transferredAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            transferReason = reason ?? string.Empty,
            sealStatus = sealStatus ?? "Unknown",
            notes = notes ?? string.Empty
        };

        if (string.IsNullOrWhiteSpace(fromActorId) || string.IsNullOrWhiteSpace(toActorId))
            item.chainStatus = EvidenceChainStatus.Partial;
        return rec;
    }

    public static void BreakChain(EvidenceItem item, bool compromised = false)
    {
        if (item == null)
            return;
        item.chainStatus = compromised ? EvidenceChainStatus.Compromised : EvidenceChainStatus.Broken;
    }
}

public static class EvidenceContaminationResolver
{
    public static void ApplyPhysicalContamination(EvidenceItem item, int severity01to100)
    {
        if (item == null)
            return;
        int s = Mathf.Clamp(severity01to100, 0, 100);
        item.contaminationRisk = ToRiskLevel(s);
        item.integrity = Mathf.Clamp(item.integrity - Mathf.RoundToInt(s * 0.35f), 0, 100);
    }

    public static void ApplyDocumentationContamination(EvidenceItem item, int severity01to100)
    {
        if (item == null)
            return;
        int s = Mathf.Clamp(severity01to100, 0, 100);
        item.contaminationRisk = MaxRisk(item.contaminationRisk, ToRiskLevel(s));
        item.integrity = Mathf.Clamp(item.integrity - Mathf.RoundToInt(s * 0.30f), 0, 100);
        if (s >= 70 && item.chainStatus == EvidenceChainStatus.Intact)
            item.chainStatus = EvidenceChainStatus.Partial;
    }

    private static EvidenceRiskLevel ToRiskLevel(int score)
    {
        if (score < 34) return EvidenceRiskLevel.Low;
        if (score < 67) return EvidenceRiskLevel.Medium;
        return EvidenceRiskLevel.High;
    }

    private static EvidenceRiskLevel MaxRisk(EvidenceRiskLevel a, EvidenceRiskLevel b)
    {
        return (EvidenceRiskLevel)Mathf.Max((int)a, (int)b);
    }
}

public static class EvidenceTamperResolver
{
    public static void MarkPossibleTamper(EvidenceItem item, bool severe)
    {
        if (item == null)
            return;
        item.tamperRisk = severe ? EvidenceRiskLevel.High : EvidenceRiskLevel.Medium;
        item.chainStatus = severe ? EvidenceChainStatus.Compromised : EvidenceChainStatus.Partial;
        item.integrity = Mathf.Clamp(item.integrity - (severe ? 40 : 20), 0, 100);
    }
}

public static class EvidenceStrengthResolver
{
    public static int ComputeStrengthScore(EvidenceItem item)
    {
        if (item == null)
            return 0;

        int score =
            Mathf.RoundToInt(item.directness * 0.25f) +
            Mathf.RoundToInt(item.reliability * 0.20f) +
            Mathf.RoundToInt(item.specificity * 0.20f) +
            Mathf.RoundToInt(item.corroboration * 0.20f) +
            Mathf.RoundToInt(item.integrity * 0.15f);
        return Mathf.Clamp(score, 0, 100);
    }

    public static void Recompute(EvidenceItem item)
    {
        if (item == null)
            return;

        item.directness = Mathf.Clamp(item.directness, 0, 100);
        item.reliability = Mathf.Clamp(item.reliability, 0, 100);
        item.specificity = Mathf.Clamp(item.specificity, 0, 100);
        item.corroboration = Mathf.Clamp(item.corroboration, 0, 100);
        item.integrity = Mathf.Clamp(item.integrity, 0, 100);

        item.strengthScore = ComputeStrengthScore(item);
        item.strength = item.strengthScore switch
        {
            < 10 => EvidenceStrengthLevel.None,
            < 35 => EvidenceStrengthLevel.Weak,
            < 60 => EvidenceStrengthLevel.Moderate,
            < 85 => EvidenceStrengthLevel.Strong,
            _ => EvidenceStrengthLevel.Critical
        };
    }
}

public static class EvidenceLinkResolver
{
    public static void LinkToCase(EvidenceItem item, string caseId)
    {
        if (item == null || string.IsNullOrWhiteSpace(caseId))
            return;
        if (!item.linkedCaseIds.Contains(caseId))
            item.linkedCaseIds.Add(caseId);
    }

    public static void LinkSubject(EvidenceItem item, string subjectId)
    {
        if (item == null || string.IsNullOrWhiteSpace(subjectId))
            return;
        if (!item.linkedSubjects.Contains(subjectId))
            item.linkedSubjects.Add(subjectId);
    }

    public static void LinkLocation(EvidenceItem item, string locationId)
    {
        if (item == null || string.IsNullOrWhiteSpace(locationId))
            return;
        if (!item.linkedLocations.Contains(locationId))
            item.linkedLocations.Add(locationId);
    }
}

public static class EvidenceToCaseResolver
{
    public static void ApplyEvidenceToCase(CaseFile file, EvidenceItem item, bool contradictionWithExistingEvidence)
    {
        if (file == null || item == null)
            return;

        EvidenceStrengthResolver.Recompute(item);
        if (!file.evidenceList.Contains(item.evidenceId))
            file.evidenceList.Add(item.evidenceId);

        int evidenceGain = item.strength switch
        {
            EvidenceStrengthLevel.None => 0,
            EvidenceStrengthLevel.Weak => 4,
            EvidenceStrengthLevel.Moderate => 8,
            EvidenceStrengthLevel.Strong => 14,
            EvidenceStrengthLevel.Critical => 20,
            _ => 0
        };
        file.evidenceStrengthScore = Mathf.Clamp(file.evidenceStrengthScore + evidenceGain, 0, 100);

        // Legal hooks
        if (item.legalStatus == EvidenceLegalStatus.Unlawful)
            file.legalIntegrityScore = Mathf.Clamp(file.legalIntegrityScore - 18, 0, 100);
        else if (item.legalStatus == EvidenceLegalStatus.Questionable)
            file.legalIntegrityScore = Mathf.Clamp(file.legalIntegrityScore - 8, 0, 100);
        else if (item.legalStatus == EvidenceLegalStatus.Lawful)
            file.legalIntegrityScore = Mathf.Clamp(file.legalIntegrityScore + 3, 0, 100);

        // Chain hooks
        if (item.chainStatus == EvidenceChainStatus.Broken || item.chainStatus == EvidenceChainStatus.Compromised)
        {
            file.fragilityScore = Mathf.Clamp(file.fragilityScore + 18, 0, 100);
            file.internalReliabilityScore = Mathf.Clamp(file.internalReliabilityScore - 12, 0, 100);
            file.irregularities.Add(new IrregularityRecord
            {
                irregularityType = CaseIrregularityType.BrokenEvidenceChain,
                severity = CaseIrregularitySeverity.Severe,
                linkedActionId = item.obtainedByActionId,
                linkedOfficerId = item.discoveredByOfficerId,
                discoveredAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                hiddenUntilDiscovered = false,
                impactOnCase = 30
            });
        }

        // Tamper/contamination hooks
        if (item.tamperRisk == EvidenceRiskLevel.High)
            file.corruptionRisk = Mathf.Clamp(file.corruptionRisk + 15, 0, 100);
        if (item.contaminationRisk == EvidenceRiskLevel.High)
            file.fragilityScore = Mathf.Clamp(file.fragilityScore + 10, 0, 100);

        // Contradiction hook
        if (contradictionWithExistingEvidence)
        {
            file.fragilityScore = Mathf.Clamp(file.fragilityScore + 12, 0, 100);
            file.internalReliabilityScore = Mathf.Clamp(file.internalReliabilityScore - 8, 0, 100);
            file.notes = (file.notes ?? string.Empty) + "\n[ReviewNeeded] Evidence contradiction detected for " + item.evidenceId;
        }

        CaseIntegrityResolver.RecomputeLegalIntegrity(file);
        CaseStrengthResolver.RecomputeCaseStrength(file);
        CaseEscalationResolver.UpdateCaseStatusByStrength(file);
    }
}
