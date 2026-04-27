using System;
using System.Collections.Generic;
using UnityEngine;

public enum OfficerCareerStatus
{
    Active,
    Promotable,
    Stalled,
    UnderReview,
    Suspended,
    Reassigned,
    BurnedOut,
    Corrupted,
    Retired,
    Dead
}

public enum BurnoutLevelBand
{
    Low,
    Noticeable,
    Heavy,
    Severe,
    CollapseRisk
}

public enum InjuryStatus
{
    None,
    Light,
    Moderate,
    Severe,
    Chronic,
    Disabled
}

public enum OfficerCareerEventType
{
    Commendation,
    PromotionReview,
    Injury,
    BurnoutSpike,
    InternalComplaint,
    CorruptionOffer,
    BlackmailAttempt,
    PartnerDeath,
    FailedOperationBlame,
    TransferOffer,
    ForcedReassignment,
    SuspensionHearing,
    RetirementOption
}

public enum CareerEventSeverity
{
    Minor,
    Moderate,
    Major,
    Critical
}

[Serializable]
public class OfficerCareerEvent
{
    public string eventId;
    public string officerId;
    public OfficerCareerEventType eventType;
    public CareerEventSeverity severity;
    public long createdAt;
    public string linkedCaseId;
    public string linkedActionId;
    public string notes;
    public bool hiddenUntilDiscovered;
}

[Serializable]
public class OfficerCareerProfile
{
    public string officerId;
    public float serviceYears;
    public PoliceRank currentRank;
    public PoliceCoreRole currentAssignment;

    public int promotionScore; // 0..100
    public List<string> promotionBlockers = new List<string>();
    public int commendationCount;
    public int reprimandCount;
    public int suspensionCount;

    public List<string> injuryHistory = new List<string>();
    public int traumaLevel; // 0..100
    public int burnoutLevel; // 0..100

    public int corruptionExposure; // 0..100
    public int corruptionInvolvement; // 0..100
    public int internalRisk; // 0..100

    public int internalReputation; // 0..100
    public int streetReputation;   // 0..100
    public int publicReputation;   // 0..100

    public InjuryStatus injuryStatus = InjuryStatus.None;
    public bool underReview;
    public OfficerCareerStatus careerStatus = OfficerCareerStatus.Active;
}

public static class OfficerCareerEventResolver
{
    public static OfficerCareerEvent Create(
        string officerId,
        OfficerCareerEventType eventType,
        CareerEventSeverity severity,
        string notes,
        string linkedCaseId = null,
        string linkedActionId = null,
        bool hiddenUntilDiscovered = false)
    {
        return new OfficerCareerEvent
        {
            eventId = "career_evt_" + Guid.NewGuid().ToString("N"),
            officerId = officerId,
            eventType = eventType,
            severity = severity,
            createdAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            linkedCaseId = linkedCaseId,
            linkedActionId = linkedActionId,
            notes = notes ?? string.Empty,
            hiddenUntilDiscovered = hiddenUntilDiscovered
        };
    }
}

public static class OfficerPromotionResolver
{
    public static int ComputePromotionScore(
        int professionalPerformance,
        int discipline,
        int commandTrust,
        int leadershipPotential,
        int publicOrOperationalCredit,
        int corruptionExposure)
    {
        int score =
            Mathf.RoundToInt(professionalPerformance * 0.30f) +
            Mathf.RoundToInt(discipline * 0.20f) +
            Mathf.RoundToInt(commandTrust * 0.20f) +
            Mathf.RoundToInt(leadershipPotential * 0.15f) +
            Mathf.RoundToInt(publicOrOperationalCredit * 0.10f) -
            Mathf.RoundToInt(corruptionExposure * 0.05f);
        return Mathf.Clamp(score, 0, 100);
    }

    public static bool IsPromotable(OfficerCareerProfile profile)
    {
        if (profile == null)
            return false;
        if (profile.promotionBlockers != null && profile.promotionBlockers.Count > 0)
            return false;
        if (profile.underReview || profile.careerStatus == OfficerCareerStatus.Suspended || profile.careerStatus == OfficerCareerStatus.Dead || profile.careerStatus == OfficerCareerStatus.Retired)
            return false;
        return profile.promotionScore >= 70;
    }
}

public static class OfficerBurnoutResolver
{
    public static void ApplyWorkloadImpact(OfficerCareerProfile profile, int workload01to100, int difficultShifts, bool stationCorrupt, bool majorFailure)
    {
        if (profile == null)
            return;
        int delta = Mathf.RoundToInt(workload01to100 * 0.08f) + Mathf.Clamp(difficultShifts, 0, 6) * 2;
        if (stationCorrupt) delta += 5;
        if (majorFailure) delta += 8;
        profile.burnoutLevel = Mathf.Clamp(profile.burnoutLevel + delta, 0, 100);
    }

    public static BurnoutLevelBand ResolveBand(int burnoutLevel)
    {
        if (burnoutLevel < 20) return BurnoutLevelBand.Low;
        if (burnoutLevel < 40) return BurnoutLevelBand.Noticeable;
        if (burnoutLevel < 60) return BurnoutLevelBand.Heavy;
        if (burnoutLevel < 80) return BurnoutLevelBand.Severe;
        return BurnoutLevelBand.CollapseRisk;
    }
}

public static class OfficerTraumaResolver
{
    public static void ApplyTraumaEvent(OfficerCareerProfile profile, CareerEventSeverity severity, bool partnerDeath = false, bool nearDeath = false)
    {
        if (profile == null)
            return;
        int add = severity switch
        {
            CareerEventSeverity.Minor => 6,
            CareerEventSeverity.Moderate => 12,
            CareerEventSeverity.Major => 20,
            _ => 30
        };
        if (partnerDeath) add += 10;
        if (nearDeath) add += 12;
        profile.traumaLevel = Mathf.Clamp(profile.traumaLevel + add, 0, 100);
    }
}

public static class OfficerInjuryResolver
{
    public static void ApplyInjury(OfficerCareerProfile profile, InjuryStatus status, string injuryNote)
    {
        if (profile == null)
            return;
        profile.injuryStatus = status;
        if (!string.IsNullOrWhiteSpace(injuryNote))
            profile.injuryHistory.Add(injuryNote.Trim());
        if (status >= InjuryStatus.Severe)
            profile.burnoutLevel = Mathf.Clamp(profile.burnoutLevel + 10, 0, 100);
    }
}

public static class OfficerCorruptionProgressionResolver
{
    public static void ApplyExposure(OfficerCareerProfile profile, int exposureDelta, int involvementDelta = 0)
    {
        if (profile == null)
            return;
        profile.corruptionExposure = Mathf.Clamp(profile.corruptionExposure + exposureDelta, 0, 100);
        profile.corruptionInvolvement = Mathf.Clamp(profile.corruptionInvolvement + involvementDelta, 0, 100);
        profile.internalRisk = Mathf.Clamp(profile.internalRisk + Mathf.Max(0, exposureDelta / 2) + Mathf.Max(0, involvementDelta), 0, 100);
        if (profile.corruptionInvolvement >= 65)
            profile.careerStatus = OfficerCareerStatus.Corrupted;
    }
}

public static class OfficerSuspensionResolver
{
    public static void ApplySuspension(OfficerCareerProfile profile, bool fullSuspension, string reason)
    {
        if (profile == null)
            return;
        profile.suspensionCount++;
        profile.underReview = true;
        profile.careerStatus = OfficerCareerStatus.Suspended;
        profile.promotionBlockers.Add("Suspension: " + (string.IsNullOrWhiteSpace(reason) ? "unspecified" : reason));
        profile.internalReputation = Mathf.Clamp(profile.internalReputation - (fullSuspension ? 20 : 10), 0, 100);
        profile.publicReputation = Mathf.Clamp(profile.publicReputation - (fullSuspension ? 18 : 8), 0, 100);
    }
}

public static class OfficerTransferResolver
{
    public static void ApplyTransfer(OfficerCareerProfile profile, PoliceCoreRole newAssignment, bool forced)
    {
        if (profile == null)
            return;
        profile.currentAssignment = newAssignment;
        profile.careerStatus = OfficerCareerStatus.Reassigned;
        if (forced)
            profile.promotionBlockers.Add("Forced reassignment");
    }
}

public static class OfficerRetirementResolver
{
    public static void Retire(OfficerCareerProfile profile, bool honorable)
    {
        if (profile == null)
            return;
        profile.careerStatus = OfficerCareerStatus.Retired;
        if (!honorable)
            profile.internalReputation = Mathf.Clamp(profile.internalReputation - 10, 0, 100);
    }
}

public static class OfficerDeathResolver
{
    public static void MarkDead(OfficerCareerProfile profile, string notes)
    {
        if (profile == null)
            return;
        profile.careerStatus = OfficerCareerStatus.Dead;
        profile.underReview = false;
        if (!string.IsNullOrWhiteSpace(notes))
            profile.injuryHistory.Add("Fatal: " + notes.Trim());
    }
}

public static class OfficerReputationResolver
{
    public static void ApplyReputationDelta(
        OfficerCareerProfile profile,
        int internalDelta,
        int streetDelta,
        int publicDelta)
    {
        if (profile == null)
            return;
        profile.internalReputation = Mathf.Clamp(profile.internalReputation + internalDelta, 0, 100);
        profile.streetReputation = Mathf.Clamp(profile.streetReputation + streetDelta, 0, 100);
        profile.publicReputation = Mathf.Clamp(profile.publicReputation + publicDelta, 0, 100);
    }
}
