using System;
using System.Collections.Generic;
using UnityEngine;

public enum IntelSourceType
{
    Human,
    Technical,
    Institutional,
    OpenSource
}

public enum IntelMotivationType
{
    Money,
    Fear,
    Revenge,
    Ideology,
    Survival,
    Promotion,
    Coercion,
    Unknown
}

public enum IntelSourceStatus
{
    Active,
    Dormant,
    Burned,
    Compromised,
    Retired
}

public enum IntelAssessedLevel
{
    Rumor,
    WeakLead,
    PlausibleLead,
    Corroborated,
    StrongIntel,
    ConfirmedOperationalIntel
}

public enum IntelActionabilityLevel
{
    NotActionable,
    ObservationCandidate,
    SurveillanceCandidate,
    CaseCandidate,
    OperationalCandidate
}

public enum IntelContentType
{
    PersonInfo,
    PlaceInfo,
    OrganizationInfo,
    FutureEvent,
    FinancialMovement,
    LogisticsMovement,
    MeetingPattern
}

public enum IntelRecommendedAction
{
    Wait,
    Observe,
    RunShortSurveillance,
    RunExtendedSurveillance,
    OpenCase,
    RequestWarrant,
    PrepareOperation
}

[Serializable]
public class SourceProfile
{
    public string id;
    public IntelSourceType type;
    public int baseReliability; // 0..100
    public int accessLevel;     // 0..100
    public int volatility;      // 0..100
    public int corruptionRisk;  // 0..100
    public int exposureRisk;    // 0..100
    public IntelMotivationType motivationType;
    public string currentHandlerId;
    public IntelSourceStatus status = IntelSourceStatus.Active;
}

[Serializable]
public class IntelItem
{
    public string id;
    public string sourceId;
    public SuspicionSubjectType subjectType;
    public string subjectId;
    public IntelContentType contentType;
    public string content;
    public long receivedAt;
    public float freshness;   // 0..1
    public float reliability; // 0..1 effective
    public float specificity; // 0..1
    public string assessedByOfficerId;
    public IntelAssessedLevel assessedLevel = IntelAssessedLevel.Rumor;
    public int corroborationCount;
    public IntelActionabilityLevel actionabilityLevel = IntelActionabilityLevel.NotActionable;
    public string linkedCaseId;
}

[Serializable]
public class IntelligenceAssessment
{
    public string intelItemId;
    public string analystId;
    public string assessmentText;
    public int threatLevel;     // 0..100
    public int confidenceLevel; // 0..100
    public IntelRecommendedAction recommendedAction;
    public long createdAt;
}

public static class IntelligenceCollectionResolver
{
    public static IntelItem CollectRawIntel(
        SourceProfile source,
        SuspicionSubjectType subjectType,
        string subjectId,
        IntelContentType contentType,
        string content)
    {
        float baseRel = source != null ? Mathf.Clamp01(source.baseReliability / 100f) : 0.35f;
        float volatilityPenalty = source != null ? Mathf.Clamp01(source.volatility / 100f) * 0.35f : 0.2f;
        float effectiveRel = Mathf.Clamp01(baseRel * (1f - volatilityPenalty));

        return new IntelItem
        {
            id = Guid.NewGuid().ToString("N"),
            sourceId = source?.id,
            subjectType = subjectType,
            subjectId = subjectId,
            contentType = contentType,
            content = content ?? string.Empty,
            receivedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            freshness = 1f,
            reliability = effectiveRel,
            specificity = GuessSpecificity(content),
            assessedLevel = IntelAssessedLevel.Rumor,
            corroborationCount = 0,
            actionabilityLevel = IntelActionabilityLevel.NotActionable
        };
    }

    private static float GuessSpecificity(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return 0.2f;
        string c = content.Trim();
        if (c.Length > 120) return 0.8f;
        if (c.Length > 60) return 0.6f;
        return 0.4f;
    }
}

public static class SourceHandlingResolver
{
    public static float ComputeSourceOperationalQuality(SourceProfile source, OfficerProfile handler)
    {
        if (source == null)
            return 0f;
        float reliability = Mathf.Clamp01(source.baseReliability / 100f);
        float access = Mathf.Clamp01(source.accessLevel / 100f);
        float volatilityPenalty = Mathf.Clamp01(source.volatility / 100f) * 0.3f;
        float corruptionPenalty = Mathf.Clamp01(source.corruptionRisk / 100f) * 0.2f;
        float handlerQuality = 0.5f;
        if (handler != null)
        {
            handlerQuality = Mathf.Clamp01(
                handler.GetSkillLevel(DerivedSkill.Persuasion) / 10f * 0.30f +
                handler.GetSkillLevel(DerivedSkill.Deception) / 10f * 0.20f +
                handler.GetSkillLevel(DerivedSkill.Analysis) / 10f * 0.20f +
                handler.GetSkillLevel(DerivedSkill.Finance) / 10f * 0.10f +
                handler.Intelligence / 100f * 0.20f);
        }
        return Mathf.Clamp01((reliability * 0.35f) + (access * 0.25f) + (handlerQuality * 0.40f) - volatilityPenalty - corruptionPenalty);
    }
}

public static class IntelAssessmentResolver
{
    public static IntelligenceAssessment Assess(IntelItem item, OfficerProfile analyst)
    {
        if (item == null)
            return null;

        float analystQuality = 0.45f;
        if (analyst != null)
        {
            analystQuality = Mathf.Clamp01(
                analyst.GetSkillLevel(DerivedSkill.Analysis) / 10f * 0.45f +
                analyst.GetSkillLevel(DerivedSkill.Surveillance) / 10f * 0.20f +
                analyst.GetSkillLevel(DerivedSkill.Legal) / 10f * 0.20f +
                analyst.Intelligence / 100f * 0.15f);
        }

        float confidence = Mathf.Clamp01(item.reliability * item.specificity * item.freshness * Mathf.Lerp(0.8f, 1.15f, analystQuality));
        int confidencePct = Mathf.RoundToInt(confidence * 100f);
        int threat = Mathf.RoundToInt(Mathf.Clamp01((item.specificity * 0.4f) + (item.reliability * 0.3f) + (item.corroborationCount * 0.1f)) * 100f);

        item.assessedByOfficerId = analyst?.OfficerId;
        item.assessedLevel = ResolveAssessedLevel(confidence, item.corroborationCount);
        item.actionabilityLevel = ResolveActionability(item.assessedLevel);

        return new IntelligenceAssessment
        {
            intelItemId = item.id,
            analystId = analyst?.OfficerId,
            assessmentText = "Intel assessed from raw to " + item.assessedLevel + ".",
            threatLevel = threat,
            confidenceLevel = confidencePct,
            recommendedAction = ResolveRecommendation(item.assessedLevel, threat),
            createdAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
    }

    private static IntelAssessedLevel ResolveAssessedLevel(float confidence, int corroborationCount)
    {
        float c = Mathf.Clamp01(confidence + corroborationCount * 0.08f);
        if (c < 0.20f) return IntelAssessedLevel.Rumor;
        if (c < 0.35f) return IntelAssessedLevel.WeakLead;
        if (c < 0.52f) return IntelAssessedLevel.PlausibleLead;
        if (c < 0.68f) return IntelAssessedLevel.Corroborated;
        if (c < 0.82f) return IntelAssessedLevel.StrongIntel;
        return IntelAssessedLevel.ConfirmedOperationalIntel;
    }

    private static IntelActionabilityLevel ResolveActionability(IntelAssessedLevel level)
    {
        return level switch
        {
            IntelAssessedLevel.Rumor => IntelActionabilityLevel.NotActionable,
            IntelAssessedLevel.WeakLead => IntelActionabilityLevel.ObservationCandidate,
            IntelAssessedLevel.PlausibleLead => IntelActionabilityLevel.SurveillanceCandidate,
            IntelAssessedLevel.Corroborated => IntelActionabilityLevel.CaseCandidate,
            IntelAssessedLevel.StrongIntel => IntelActionabilityLevel.CaseCandidate,
            IntelAssessedLevel.ConfirmedOperationalIntel => IntelActionabilityLevel.OperationalCandidate,
            _ => IntelActionabilityLevel.NotActionable
        };
    }

    private static IntelRecommendedAction ResolveRecommendation(IntelAssessedLevel level, int threat)
    {
        if (level == IntelAssessedLevel.ConfirmedOperationalIntel && threat >= 65)
            return IntelRecommendedAction.PrepareOperation;
        if (level == IntelAssessedLevel.StrongIntel)
            return IntelRecommendedAction.OpenCase;
        if (level == IntelAssessedLevel.Corroborated)
            return IntelRecommendedAction.RunExtendedSurveillance;
        if (level == IntelAssessedLevel.PlausibleLead)
            return IntelRecommendedAction.RunShortSurveillance;
        if (level == IntelAssessedLevel.WeakLead)
            return IntelRecommendedAction.Observe;
        return IntelRecommendedAction.Wait;
    }
}

public static class IntelCorroborationResolver
{
    public static void ApplyCorroboration(IntelItem item, int corroboratingHits)
    {
        if (item == null || corroboratingHits <= 0)
            return;
        item.corroborationCount = Mathf.Max(0, item.corroborationCount + corroboratingHits);
        float bonus = Mathf.Clamp(corroboratingHits * 0.06f, 0f, 0.30f);
        item.reliability = Mathf.Clamp01(item.reliability + bonus);
    }
}

public static class IntelDecayResolver
{
    public static void ApplyDecayPerTurn(IntelItem item, int turns = 1)
    {
        if (item == null || turns <= 0)
            return;
        for (int i = 0; i < turns; i++)
        {
            float decay = GetDecayPerTurn(item.contentType, item.assessedLevel);
            item.freshness = Mathf.Clamp01(item.freshness - decay);
            if (item.corroborationCount <= 0)
                item.reliability = Mathf.Clamp01(item.reliability - decay * 0.5f);
        }
    }

    private static float GetDecayPerTurn(IntelContentType contentType, IntelAssessedLevel level)
    {
        float baseDecay = contentType switch
        {
            IntelContentType.FutureEvent => 0.16f,
            IntelContentType.LogisticsMovement => 0.12f,
            IntelContentType.PersonInfo => 0.08f,
            IntelContentType.PlaceInfo => 0.08f,
            IntelContentType.OrganizationInfo => 0.05f,
            _ => 0.09f
        };

        if (level >= IntelAssessedLevel.Corroborated)
            baseDecay *= 0.7f;
        if (level == IntelAssessedLevel.ConfirmedOperationalIntel)
            baseDecay *= 0.6f;
        return baseDecay;
    }
}

public static class IntelToSuspicionResolver
{
    /// <summary>
    /// Converts assessed intel into suspicion contribution:
    /// reliability * specificity * freshness * corroboration * analystQuality.
    /// </summary>
    public static SuspicionFactor BuildSuspicionFactor(
        IntelItem item,
        OfficerProfile analyst)
    {
        if (item == null)
            return null;

        float sourceReliability = Mathf.Clamp01(item.reliability);
        float informationSpecificity = Mathf.Clamp01(item.specificity);
        float freshness = Mathf.Clamp01(item.freshness);
        float crossCorroboration = Mathf.Clamp01(0.4f + item.corroborationCount * 0.15f);
        float analystQuality = 0.55f;
        if (analyst != null)
        {
            analystQuality = Mathf.Clamp01(
                analyst.GetSkillLevel(DerivedSkill.Analysis) / 10f * 0.45f +
                analyst.GetSkillLevel(DerivedSkill.Surveillance) / 10f * 0.20f +
                analyst.GetSkillLevel(DerivedSkill.Legal) / 10f * 0.20f +
                analyst.Intelligence / 100f * 0.15f);
        }

        float contribution = sourceReliability * informationSpecificity * freshness * crossCorroboration * Mathf.Lerp(0.75f, 1.2f, analystQuality);
        int value = Mathf.Clamp(Mathf.RoundToInt(contribution * 40f), 0, 35);

        return new SuspicionFactor
        {
            factorType = SuspicionFactorType.IntelSignal,
            sourceType = SuspicionSourceType.IntelligenceSource,
            value = value,
            reliability = sourceReliability,
            freshness = freshness,
            notes = "Intel " + item.assessedLevel + " contribution."
        };
    }
}

public static class IntelToCaseResolver
{
    public static bool ShouldOpenCase(IntelItem item, IntelligenceAssessment assessment)
    {
        if (item == null || assessment == null)
            return false;
        if (item.assessedLevel >= IntelAssessedLevel.StrongIntel)
            return true;
        if (item.assessedLevel == IntelAssessedLevel.Corroborated && assessment.threatLevel >= 60 && assessment.confidenceLevel >= 55)
            return true;
        return false;
    }
}
