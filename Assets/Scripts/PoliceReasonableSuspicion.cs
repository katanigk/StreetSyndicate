using System;
using System.Collections.Generic;
using UnityEngine;

public enum SuspicionSubjectType
{
    Person,
    Vehicle,
    Place
}

public enum SuspicionLevel
{
    None,
    Weak,
    Reasonable,
    Strong,
    Critical
}

public enum LegalUsabilityLevel
{
    Insufficient,
    ContactOnly,
    DetainAllowed,
    FriskEligible,
    EscalationCandidate
}

public enum SuspicionFactorType
{
    EventDescriptionMatch,
    TimeLocationProximity,
    BehaviorAnomaly,
    IntelSignal,
    TargetHistory,
    EnvironmentalContext,
    VerifiedInnocentExplanation,
    MisidentificationEvidence,
    CalmConsistentBehavior,
    InvalidAppearanceOnly,
    InvalidSocialGroupOnly,
    InvalidGutFeelingOnly
}

public enum SuspicionSourceType
{
    PatrolObservation,
    Witness,
    Complaint,
    IntelligenceSource,
    CaseFile,
    SensorOrSystem,
    OtherOfficer
}

[Serializable]
public class SuspicionFactor
{
    public SuspicionFactorType factorType;
    public SuspicionSourceType sourceType;
    public int value; // Suggested impact magnitude before multipliers; can be negative.
    public float reliability = 1f; // 0..1
    public float freshness = 1f;   // 0..1
    public string notes;
}

[Serializable]
public class SuspicionRecord
{
    public SuspicionSubjectType subjectType;
    public string subjectId;
    public string officerId;
    public string stationId;
    public int currentScore;
    public SuspicionLevel suspicionLevel;
    public LegalUsabilityLevel legalUsabilityLevel;
    public SuspicionFactor[] factors = Array.Empty<SuspicionFactor>();
    public long lastUpdated;
}

[Serializable]
public struct SuspicionEvaluationResult
{
    public int Score;
    public SuspicionLevel Level;
    public LegalUsabilityLevel LegalUsability;
    public bool InternalReviewSuggested;
    public string[] InternalReviewReasons;
}

public static class PoliceReasonableSuspicion
{
    public static SuspicionEvaluationResult Evaluate(
        SuspicionRecord record,
        OfficerProfile officer,
        bool complementaryFriskIndicatorPresent,
        bool additionalEscalationFactorPresent,
        bool actionEscalatedBeyondScore,
        bool civilianComplaintLodged,
        bool reportMismatchDetected,
        bool officerDisputeDetected)
    {
        if (record == null)
            return default;

        int score = ComputeScoreFromFactors(record.factors, officer);
        score = Mathf.Clamp(score, 0, 100);

        // Rule: only invalid factors cannot create reasonable suspicion.
        if (OnlyInvalidFactorsPresent(record.factors))
            score = Mathf.Min(score, 19);

        SuspicionLevel level = ResolveLevel(score);
        LegalUsabilityLevel usability = ResolveLegalUsability(score, complementaryFriskIndicatorPresent, additionalEscalationFactorPresent);

        List<string> reviewReasons = new List<string>();
        if (actionEscalatedBeyondScore && score < 40)
            reviewReasons.Add("Escalation executed below reasonable threshold.");
        if (civilianComplaintLodged)
            reviewReasons.Add("Civilian complaint attached to suspicion chain.");
        if (reportMismatchDetected)
            reviewReasons.Add("Report content mismatch against recorded factors.");
        if (officerDisputeDetected)
            reviewReasons.Add("Officer account discrepancy detected.");

        record.currentScore = score;
        record.suspicionLevel = level;
        record.legalUsabilityLevel = usability;
        record.lastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        return new SuspicionEvaluationResult
        {
            Score = score,
            Level = level,
            LegalUsability = usability,
            InternalReviewSuggested = reviewReasons.Count > 0,
            InternalReviewReasons = reviewReasons.ToArray()
        };
    }

    public static int ComputeScoreFromFactors(SuspicionFactor[] factors, OfficerProfile officer)
    {
        if (factors == null || factors.Length == 0)
            return 0;

        float total = 0f;
        for (int i = 0; i < factors.Length; i++)
        {
            SuspicionFactor f = factors[i];
            if (f == null)
                continue;

            float baseImpact = ResolveBaseFactorImpact(f);
            float weighted = baseImpact * Mathf.Clamp01(f.reliability) * Mathf.Clamp01(f.freshness);
            weighted *= GetOfficerInterpretationMultiplier(officer, f);
            total += weighted;
        }

        // Small alignment bonus: three or more moderate factors aligning can cross threshold.
        int moderateCount = CountModeratePositiveFactors(factors);
        if (moderateCount >= 3)
            total += 8f;

        return Mathf.RoundToInt(total);
    }

    public static void ApplyDecayPerTurn(SuspicionRecord record, int turns = 1)
    {
        if (record == null || turns <= 0)
            return;

        int score = record.currentScore;
        for (int t = 0; t < turns; t++)
            score = Mathf.Max(0, score - ComputeDecayStep(record.factors));

        record.currentScore = score;
        record.suspicionLevel = ResolveLevel(score);
        record.legalUsabilityLevel = ResolveLegalUsability(score, complementaryFriskIndicatorPresent: false, additionalEscalationFactorPresent: false);
        record.lastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    public static string GetPlayerFacingBandText(SuspicionLevel level)
    {
        return level switch
        {
            SuspicionLevel.None => "No basis",
            SuspicionLevel.Weak => "Weak indication",
            SuspicionLevel.Reasonable => "Reasonable suspicion",
            SuspicionLevel.Strong => "Strong suspicion",
            SuspicionLevel.Critical => "Heavy suspicion",
            _ => "No basis"
        };
    }

    private static float ResolveBaseFactorImpact(SuspicionFactor factor)
    {
        if (factor == null)
            return 0f;

        if (factor.value != 0)
            return factor.value;

        return factor.factorType switch
        {
            SuspicionFactorType.EventDescriptionMatch => 20f,
            SuspicionFactorType.TimeLocationProximity => 15f,
            SuspicionFactorType.BehaviorAnomaly => 15f,
            SuspicionFactorType.IntelSignal => 20f,
            SuspicionFactorType.TargetHistory => 10f,
            SuspicionFactorType.EnvironmentalContext => 7f,
            SuspicionFactorType.VerifiedInnocentExplanation => -20f,
            SuspicionFactorType.MisidentificationEvidence => -25f,
            SuspicionFactorType.CalmConsistentBehavior => -10f,
            SuspicionFactorType.InvalidAppearanceOnly => 0f,
            SuspicionFactorType.InvalidSocialGroupOnly => 0f,
            SuspicionFactorType.InvalidGutFeelingOnly => 0f,
            _ => 0f
        };
    }

    private static bool OnlyInvalidFactorsPresent(SuspicionFactor[] factors)
    {
        if (factors == null || factors.Length == 0)
            return false;

        bool any = false;
        for (int i = 0; i < factors.Length; i++)
        {
            SuspicionFactor f = factors[i];
            if (f == null)
                continue;
            any = true;
            if (!IsInvalidStandAloneFactor(f.factorType))
                return false;
        }
        return any;
    }

    private static bool IsInvalidStandAloneFactor(SuspicionFactorType t)
    {
        return t == SuspicionFactorType.InvalidAppearanceOnly ||
               t == SuspicionFactorType.InvalidSocialGroupOnly ||
               t == SuspicionFactorType.InvalidGutFeelingOnly;
    }

    private static int CountModeratePositiveFactors(SuspicionFactor[] factors)
    {
        if (factors == null)
            return 0;
        int count = 0;
        for (int i = 0; i < factors.Length; i++)
        {
            SuspicionFactor f = factors[i];
            if (f == null)
                continue;
            float impact = ResolveBaseFactorImpact(f);
            if (impact >= 12f && impact <= 22f && !IsInvalidStandAloneFactor(f.factorType))
                count++;
        }
        return count;
    }

    private static SuspicionLevel ResolveLevel(int score)
    {
        if (score < 20) return SuspicionLevel.None;
        if (score < 40) return SuspicionLevel.Weak;
        if (score < 60) return SuspicionLevel.Reasonable;
        if (score < 80) return SuspicionLevel.Strong;
        return SuspicionLevel.Critical;
    }

    private static LegalUsabilityLevel ResolveLegalUsability(
        int score,
        bool complementaryFriskIndicatorPresent,
        bool additionalEscalationFactorPresent)
    {
        if (score < 20)
            return LegalUsabilityLevel.Insufficient;
        if (score < 40)
            return LegalUsabilityLevel.ContactOnly;
        if (score < 60)
            return LegalUsabilityLevel.DetainAllowed;
        if (score < 80)
            return complementaryFriskIndicatorPresent ? LegalUsabilityLevel.FriskEligible : LegalUsabilityLevel.DetainAllowed;

        if (additionalEscalationFactorPresent)
            return LegalUsabilityLevel.EscalationCandidate;
        return complementaryFriskIndicatorPresent ? LegalUsabilityLevel.FriskEligible : LegalUsabilityLevel.DetainAllowed;
    }

    private static float GetOfficerInterpretationMultiplier(OfficerProfile officer, SuspicionFactor factor)
    {
        if (officer == null || factor == null)
            return 1f;

        float m = 1f;
        bool paranoid = (officer.Personality & OfficerPersonalityFlags.Paranoid) != 0;
        bool thorough = (officer.Personality & OfficerPersonalityFlags.Thorough) != 0;
        bool lazy = (officer.Personality & OfficerPersonalityFlags.Lazy) != 0;
        bool aggressive = (officer.Personality & OfficerPersonalityFlags.Aggressive) != 0;
        bool corrupt = (officer.Personality & OfficerPersonalityFlags.Corrupt) != 0;

        if (paranoid && ResolveBaseFactorImpact(factor) > 0f)
            m += 0.10f;
        if (thorough && factor.factorType == SuspicionFactorType.IntelSignal)
            m += 0.06f;
        if (thorough && factor.factorType == SuspicionFactorType.BehaviorAnomaly)
            m -= 0.05f; // less jumpy on weak behavior signals
        if (lazy && ResolveBaseFactorImpact(factor) > 0f)
            m -= 0.12f;
        if (aggressive && factor.factorType == SuspicionFactorType.BehaviorAnomaly)
            m += 0.10f;
        if (corrupt && ResolveBaseFactorImpact(factor) > 0f)
            m += UnityEngine.Random.Range(-0.15f, 0.15f); // may inflate or suppress intentionally

        // Skills/traits shape quality of interpretation.
        float analysis = officer.GetSkillLevel(DerivedSkill.Analysis) / 10f;      // 0..1
        float surveillance = officer.GetSkillLevel(DerivedSkill.Surveillance) / 10f;
        float legal = officer.GetSkillLevel(DerivedSkill.Legal) / 10f;
        float intel = officer.Intelligence / 100f;
        float mental = officer.MentalResilience / 100f;

        float quality = (analysis * 0.30f) + (surveillance * 0.20f) + (legal * 0.20f) + (intel * 0.20f) + (mental * 0.10f);
        m *= Mathf.Lerp(0.92f, 1.10f, Mathf.Clamp01(quality));

        return Mathf.Clamp(m, 0.70f, 1.30f);
    }

    private static int ComputeDecayStep(SuspicionFactor[] factors)
    {
        if (factors == null || factors.Length == 0)
            return 6;

        int step = 0;
        for (int i = 0; i < factors.Length; i++)
        {
            SuspicionFactor f = factors[i];
            if (f == null)
                continue;

            int local = f.factorType switch
            {
                SuspicionFactorType.TimeLocationProximity => 8,
                SuspicionFactorType.BehaviorAnomaly => 5,
                SuspicionFactorType.IntelSignal => 3,
                SuspicionFactorType.TargetHistory => 2,
                SuspicionFactorType.EventDescriptionMatch => 4,
                SuspicionFactorType.EnvironmentalContext => 5,
                _ => 4
            };
            step += local;
        }

        int avg = Mathf.Max(2, Mathf.RoundToInt(step / (float)factors.Length));
        return Mathf.Clamp(avg, 2, 9);
    }
}
