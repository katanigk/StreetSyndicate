using System;
using UnityEngine;

[Flags]
public enum OfficerPersonalityFlags
{
    None = 0,
    Aggressive = 1 << 0,
    Coward = 1 << 1,
    Thorough = 1 << 2,
    Lazy = 1 << 3,
    Corrupt = 1 << 4,
    Ambitious = 1 << 5,
    Loyal = 1 << 6,
    Paranoid = 1 << 7
}

public enum PoliceActionType
{
    DetectSuspicion,
    StreetStop,
    Detain,
    PatDownSearch,
    VehicleSearch,
    StructureSearch,
    Arrest,
    Chase,
    InterviewWitness,
    InterrogateSuspect,
    CrossCheckIntel,
    HandleInformant,
    SurveillanceTail,
    Raid,
    CaseManagement,
    WriteReport,
    EvidenceChainHandling,
    RequestAuthorization
}

public enum LegalGroundState
{
    None,
    Borderline,
    Established
}

public enum PoliceAuthorizationLevel
{
    OfficerSolo,
    Supervisor,
    Warrant,
    Emergency
}

public enum PoliceDocumentationQuality
{
    Missing,
    Partial,
    Complete,
    Falsified
}

[Serializable]
public class OfficerProfile
{
    public string OfficerId;
    public string DisplayName;
    public string Role;
    public string Rank;
    public PoliceDutyRole DutyRole = PoliceDutyRole.PatrolOfficer;
    public string StationId;
    public string DepartmentId;
    public string TeamId;

    // Same universal trait system (0..100), no separate police stats.
    public int Strength;
    public int Agility;
    public int Intelligence;
    public int Charisma;
    public int MentalResilience;
    public int Determination;

    // Unified skill space; value = stars 0..10 by DerivedSkill ordinal.
    public int[] SkillLevels = new int[DerivedSkillProgression.SkillCount];

    public OfficerPersonalityFlags Personality;

    // Corruption/system channels.
    public int CorruptionVulnerability; // 0..100
    public int CorruptionPressure;      // 0..100 (bribe/blackmail/environment pressure)
    public int SystemLoyalty;           // 0..100

    // Momentary state.
    public int Fatigue; // 0..100
    public int Stress;  // 0..100
    public int Injury;  // 0..100
    public int Anger;   // 0..100
    public int Fear;    // 0..100

    public int GetTrait(CoreTrait trait)
    {
        switch (trait)
        {
            case CoreTrait.Strength: return Mathf.Clamp(Strength, 0, 100);
            case CoreTrait.Agility: return Mathf.Clamp(Agility, 0, 100);
            case CoreTrait.Intelligence: return Mathf.Clamp(Intelligence, 0, 100);
            case CoreTrait.Charisma: return Mathf.Clamp(Charisma, 0, 100);
            case CoreTrait.MentalResilience: return Mathf.Clamp(MentalResilience, 0, 100);
            case CoreTrait.Determination: return Mathf.Clamp(Determination, 0, 100);
            default: return 0;
        }
    }

    public int GetSkillLevel(DerivedSkill skill)
    {
        if (SkillLevels == null || SkillLevels.Length != DerivedSkillProgression.SkillCount)
            SkillLevels = new int[DerivedSkillProgression.SkillCount];
        int i = (int)skill;
        if (i < 0 || i >= SkillLevels.Length)
            return 0;
        return Mathf.Clamp(SkillLevels[i], 0, StarRubric.MaxLevel);
    }
}

[Serializable]
public struct PoliceActionContext
{
    public int CommanderPressure; // 0..100
    public int StationCorruption; // 0..100
    public int OperationalLoad;   // 0..100

    // Relation to target in the same universal relation language.
    public int TargetAffinity; // -100..100
    public int TargetFear;     // 0..100
    public int TargetRespect;  // -100..100
    public int TargetInterest; // -100..100

    public bool HasDirectOrder;
    public bool SharedInterestWithOutsideActor;
}

[Serializable]
public struct PoliceLegalityRisk
{
    public bool HasViolationRisk;
    public int ViolationRiskScore; // 0..100
    public string Summary;
}

[Serializable]
public struct PoliceOutcomeBundle
{
    public int OperationalOutcomeScore; // 0..100
    public int ExposureOutcomeScore;    // 0..100 (higher = more exposed traces)
    public int LegalIntegrityScore;     // 0..100
    public int InternalConsequenceScore;// 0..100 (higher = more internal trouble)

    public int SkillMixScore;           // 0..100
    public int TraitContributionScore;  // 0..100
    public int PersonalityModifier;     // -100..100 effective delta
    public int CorruptionInfluence;     // 0..100

    public PoliceLegalityRisk LegalityRisk;
}

public static class PoliceActionResolver
{
    private struct ActionSpec
    {
        public DerivedSkill[] Skills;
        public CoreTrait[] Traits;
    }

    public static PoliceOutcomeBundle Resolve(
        OfficerProfile officer,
        object target,
        PoliceActionContext context,
        LegalGroundState legalGround,
        PoliceAuthorizationLevel authorizationLevel,
        PoliceDocumentationQuality documentationQuality,
        PoliceActionType actionType)
    {
        if (officer == null)
            return default;

        NormalizeOfficer(officer);
        ActionSpec spec = BuildActionSpec(actionType);

        int skillMix = ComputeSkillMix(officer, spec.Skills);
        int traitScore = ComputeTraitContribution(officer, spec.Traits);
        int personalityDelta = ComputePersonalityModifier(officer, actionType);
        int momentaryDelta = ComputeMomentaryStateModifier(officer, actionType, context);
        int corruptionInfluence = ComputeCorruptionInfluence(officer, context);
        PoliceLegalityRisk legalityRisk = ComputeLegalityRisk(legalGround, authorizationLevel, documentationQuality, corruptionInfluence);

        int roleAffinity = PoliceOrganizationPilotFactory.GetRoleWeightForAction(officer.DutyRole, actionType);
        int operational = Mathf.Clamp(skillMix + Mathf.RoundToInt(traitScore * 0.65f) + personalityDelta + momentaryDelta + roleAffinity - Mathf.RoundToInt(corruptionInfluence * 0.15f), 0, 100);
        int exposure = Mathf.Clamp(20 + Mathf.RoundToInt((100 - skillMix) * 0.35f) + Mathf.RoundToInt(Mathf.Max(0, personalityDelta) * 0.3f) + legalityRisk.ViolationRiskScore / 4, 0, 100);
        int legalIntegrity = Mathf.Clamp(100 - legalityRisk.ViolationRiskScore - Mathf.RoundToInt(corruptionInfluence * 0.25f), 0, 100);
        int internalConsequence = Mathf.Clamp(legalityRisk.ViolationRiskScore / 2 + exposure / 3 + Mathf.RoundToInt(corruptionInfluence * 0.35f), 0, 100);

        _ = target; // placeholder hook for future target-specific modifiers

        return new PoliceOutcomeBundle
        {
            OperationalOutcomeScore = operational,
            ExposureOutcomeScore = exposure,
            LegalIntegrityScore = legalIntegrity,
            InternalConsequenceScore = internalConsequence,
            SkillMixScore = skillMix,
            TraitContributionScore = traitScore,
            PersonalityModifier = personalityDelta + momentaryDelta,
            CorruptionInfluence = corruptionInfluence,
            LegalityRisk = legalityRisk
        };
    }

    private static ActionSpec BuildActionSpec(PoliceActionType actionType)
    {
        switch (actionType)
        {
            case PoliceActionType.DetectSuspicion:
                return NewSpec(
                    new[] { DerivedSkill.Surveillance, DerivedSkill.Analysis },
                    new[] { CoreTrait.Intelligence, CoreTrait.MentalResilience });
            case PoliceActionType.StreetStop:
                return NewSpec(
                    new[] { DerivedSkill.Persuasion, DerivedSkill.Leadership },
                    new[] { CoreTrait.Charisma, CoreTrait.MentalResilience });
            case PoliceActionType.Detain:
                return NewSpec(
                    new[] { DerivedSkill.Leadership, DerivedSkill.Brawling },
                    new[] { CoreTrait.Strength, CoreTrait.Charisma, CoreTrait.MentalResilience });
            case PoliceActionType.PatDownSearch:
                return NewSpec(
                    new[] { DerivedSkill.Surveillance, DerivedSkill.Analysis },
                    new[] { CoreTrait.Intelligence, CoreTrait.Agility });
            case PoliceActionType.VehicleSearch:
                return NewSpec(
                    new[] { DerivedSkill.Surveillance, DerivedSkill.Logistics, DerivedSkill.Analysis },
                    new[] { CoreTrait.Intelligence, CoreTrait.Determination });
            case PoliceActionType.StructureSearch:
                return NewSpec(
                    new[] { DerivedSkill.Surveillance, DerivedSkill.Leadership, DerivedSkill.Analysis },
                    new[] { CoreTrait.Intelligence, CoreTrait.MentalResilience, CoreTrait.Determination });
            case PoliceActionType.Arrest:
                return NewSpec(
                    new[] { DerivedSkill.Leadership, DerivedSkill.Brawling, DerivedSkill.Firearms },
                    new[] { CoreTrait.Strength, CoreTrait.Agility, CoreTrait.MentalResilience });
            case PoliceActionType.Chase:
                return NewSpec(
                    new[] { DerivedSkill.Driving, DerivedSkill.Firearms, DerivedSkill.Leadership, DerivedSkill.Surveillance },
                    new[] { CoreTrait.Agility, CoreTrait.MentalResilience, CoreTrait.Determination });
            case PoliceActionType.InterviewWitness:
                return NewSpec(
                    new[] { DerivedSkill.Persuasion, DerivedSkill.Deception, DerivedSkill.Analysis },
                    new[] { CoreTrait.Charisma, CoreTrait.Intelligence });
            case PoliceActionType.InterrogateSuspect:
                return NewSpec(
                    new[] { DerivedSkill.Analysis, DerivedSkill.Persuasion, DerivedSkill.Intimidation, DerivedSkill.Deception },
                    new[] { CoreTrait.Intelligence, CoreTrait.Charisma, CoreTrait.MentalResilience });
            case PoliceActionType.CrossCheckIntel:
                return NewSpec(
                    new[] { DerivedSkill.Analysis, DerivedSkill.Surveillance },
                    new[] { CoreTrait.Intelligence, CoreTrait.Determination });
            case PoliceActionType.HandleInformant:
                return NewSpec(
                    new[] { DerivedSkill.Persuasion, DerivedSkill.Deception, DerivedSkill.Finance, DerivedSkill.Analysis },
                    new[] { CoreTrait.Charisma, CoreTrait.Intelligence });
            case PoliceActionType.SurveillanceTail:
                return NewSpec(
                    new[] { DerivedSkill.Surveillance, DerivedSkill.Stealth, DerivedSkill.Driving },
                    new[] { CoreTrait.Agility, CoreTrait.Intelligence, CoreTrait.MentalResilience });
            case PoliceActionType.Raid:
                return NewSpec(
                    new[] { DerivedSkill.Leadership, DerivedSkill.Firearms, DerivedSkill.Brawling, DerivedSkill.Logistics, DerivedSkill.Analysis },
                    new[] { CoreTrait.Charisma, CoreTrait.Agility, CoreTrait.MentalResilience, CoreTrait.Determination });
            case PoliceActionType.CaseManagement:
                return NewSpec(
                    new[] { DerivedSkill.Analysis, DerivedSkill.Legal, DerivedSkill.Logistics, DerivedSkill.Leadership },
                    new[] { CoreTrait.Intelligence, CoreTrait.Determination });
            case PoliceActionType.WriteReport:
                return NewSpec(
                    new[] { DerivedSkill.Legal, DerivedSkill.Analysis },
                    new[] { CoreTrait.Intelligence, CoreTrait.Determination });
            case PoliceActionType.EvidenceChainHandling:
                return NewSpec(
                    new[] { DerivedSkill.Legal, DerivedSkill.Logistics, DerivedSkill.Analysis },
                    new[] { CoreTrait.Intelligence, CoreTrait.Determination });
            case PoliceActionType.RequestAuthorization:
                return NewSpec(
                    new[] { DerivedSkill.Persuasion, DerivedSkill.Leadership, DerivedSkill.Legal },
                    new[] { CoreTrait.Charisma, CoreTrait.Intelligence });
            default:
                return NewSpec(Array.Empty<DerivedSkill>(), Array.Empty<CoreTrait>());
        }
    }

    private static ActionSpec NewSpec(DerivedSkill[] skills, CoreTrait[] traits)
    {
        ActionSpec spec;
        spec.Skills = skills;
        spec.Traits = traits;
        return spec;
    }

    private static int ComputeSkillMix(OfficerProfile officer, DerivedSkill[] skills)
    {
        if (skills == null || skills.Length == 0)
            return 0;
        float sum = 0f;
        for (int i = 0; i < skills.Length; i++)
            sum += officer.GetSkillLevel(skills[i]) * 10f;
        return Mathf.Clamp(Mathf.RoundToInt(sum / skills.Length), 0, 100);
    }

    private static int ComputeTraitContribution(OfficerProfile officer, CoreTrait[] traits)
    {
        if (traits == null || traits.Length == 0)
            return 0;
        float sum = 0f;
        for (int i = 0; i < traits.Length; i++)
            sum += officer.GetTrait(traits[i]);
        return Mathf.Clamp(Mathf.RoundToInt(sum / traits.Length), 0, 100);
    }

    private static int ComputePersonalityModifier(OfficerProfile officer, PoliceActionType actionType)
    {
        int delta = 0;
        OfficerPersonalityFlags p = officer.Personality;

        if ((p & OfficerPersonalityFlags.Aggressive) != 0)
        {
            if (actionType == PoliceActionType.Detain || actionType == PoliceActionType.Arrest || actionType == PoliceActionType.Raid || actionType == PoliceActionType.Chase)
                delta += 8;
            if (actionType == PoliceActionType.InterviewWitness)
                delta -= 6;
        }
        if ((p & OfficerPersonalityFlags.Coward) != 0)
        {
            if (actionType == PoliceActionType.Arrest || actionType == PoliceActionType.Chase || actionType == PoliceActionType.Raid)
                delta -= 10;
            if (actionType == PoliceActionType.WriteReport)
                delta += 2;
        }
        if ((p & OfficerPersonalityFlags.Thorough) != 0)
        {
            if (actionType == PoliceActionType.StructureSearch || actionType == PoliceActionType.CaseManagement || actionType == PoliceActionType.EvidenceChainHandling || actionType == PoliceActionType.WriteReport)
                delta += 10;
        }
        if ((p & OfficerPersonalityFlags.Lazy) != 0)
        {
            if (actionType == PoliceActionType.VehicleSearch || actionType == PoliceActionType.StructureSearch || actionType == PoliceActionType.CaseManagement || actionType == PoliceActionType.WriteReport)
                delta -= 12;
        }
        if ((p & OfficerPersonalityFlags.Paranoid) != 0)
        {
            if (actionType == PoliceActionType.DetectSuspicion || actionType == PoliceActionType.SurveillanceTail)
                delta += 7;
            if (actionType == PoliceActionType.StreetStop)
                delta += 3;
        }
        if ((p & OfficerPersonalityFlags.Ambitious) != 0)
        {
            if (actionType == PoliceActionType.CaseManagement || actionType == PoliceActionType.RequestAuthorization)
                delta += 6;
        }
        if ((p & OfficerPersonalityFlags.Loyal) != 0)
        {
            if (actionType == PoliceActionType.WriteReport || actionType == PoliceActionType.EvidenceChainHandling)
                delta += 4;
        }

        return Mathf.Clamp(delta, -30, 30);
    }

    private static int ComputeMomentaryStateModifier(OfficerProfile officer, PoliceActionType actionType, PoliceActionContext context)
    {
        int fatiguePenalty = Mathf.RoundToInt(Mathf.Clamp01(officer.Fatigue / 100f) * 12f);
        int stressPenalty = Mathf.RoundToInt(Mathf.Clamp01((officer.Stress + context.OperationalLoad) / 200f) * 10f);
        int injuryPenalty = Mathf.RoundToInt(Mathf.Clamp01(officer.Injury / 100f) * 16f);
        int fearPenalty = Mathf.RoundToInt(Mathf.Clamp01(officer.Fear / 100f) * 8f);
        int angerSpike = 0;

        if (actionType == PoliceActionType.Arrest || actionType == PoliceActionType.Detain || actionType == PoliceActionType.Raid)
            angerSpike = Mathf.RoundToInt(Mathf.Clamp01(officer.Anger / 100f) * 6f);

        return Mathf.Clamp(-fatiguePenalty - stressPenalty - injuryPenalty - fearPenalty + angerSpike, -40, 10);
    }

    private static int ComputeCorruptionInfluence(OfficerProfile officer, PoliceActionContext context)
    {
        float baseRisk = (officer.CorruptionVulnerability * 0.35f) + (officer.CorruptionPressure * 0.30f) + (context.StationCorruption * 0.20f);
        if ((officer.Personality & OfficerPersonalityFlags.Corrupt) != 0)
            baseRisk += 18f;
        if ((officer.Personality & OfficerPersonalityFlags.Loyal) != 0)
            baseRisk -= 8f;
        baseRisk -= officer.SystemLoyalty * 0.18f;
        if (context.SharedInterestWithOutsideActor)
            baseRisk += 10f;
        return Mathf.Clamp(Mathf.RoundToInt(baseRisk), 0, 100);
    }

    private static PoliceLegalityRisk ComputeLegalityRisk(
        LegalGroundState legalGround,
        PoliceAuthorizationLevel authorizationLevel,
        PoliceDocumentationQuality documentationQuality,
        int corruptionInfluence)
    {
        int risk = 0;
        if (legalGround == LegalGroundState.None)
            risk += 45;
        else if (legalGround == LegalGroundState.Borderline)
            risk += 20;

        if (authorizationLevel == PoliceAuthorizationLevel.OfficerSolo)
            risk += 20;
        else if (authorizationLevel == PoliceAuthorizationLevel.Supervisor)
            risk += 8;
        else if (authorizationLevel == PoliceAuthorizationLevel.Emergency)
            risk += 12;

        if (documentationQuality == PoliceDocumentationQuality.Missing)
            risk += 35;
        else if (documentationQuality == PoliceDocumentationQuality.Partial)
            risk += 18;
        else if (documentationQuality == PoliceDocumentationQuality.Falsified)
            risk += 45;

        risk += Mathf.RoundToInt(corruptionInfluence * 0.20f);
        risk = Mathf.Clamp(risk, 0, 100);

        PoliceLegalityRisk result;
        result.HasViolationRisk = risk >= 35;
        result.ViolationRiskScore = risk;
        result.Summary = risk switch
        {
            >= 80 => "Critical legality risk",
            >= 55 => "High legality risk",
            >= 35 => "Moderate legality risk",
            _ => "Low legality risk"
        };
        return result;
    }

    private static void NormalizeOfficer(OfficerProfile officer)
    {
        officer.Strength = Mathf.Clamp(officer.Strength, 0, 100);
        officer.Agility = Mathf.Clamp(officer.Agility, 0, 100);
        officer.Intelligence = Mathf.Clamp(officer.Intelligence, 0, 100);
        officer.Charisma = Mathf.Clamp(officer.Charisma, 0, 100);
        officer.MentalResilience = Mathf.Clamp(officer.MentalResilience, 0, 100);
        officer.Determination = Mathf.Clamp(officer.Determination, 0, 100);
        officer.CorruptionVulnerability = Mathf.Clamp(officer.CorruptionVulnerability, 0, 100);
        officer.CorruptionPressure = Mathf.Clamp(officer.CorruptionPressure, 0, 100);
        officer.SystemLoyalty = Mathf.Clamp(officer.SystemLoyalty, 0, 100);
        officer.Fatigue = Mathf.Clamp(officer.Fatigue, 0, 100);
        officer.Stress = Mathf.Clamp(officer.Stress, 0, 100);
        officer.Injury = Mathf.Clamp(officer.Injury, 0, 100);
        officer.Anger = Mathf.Clamp(officer.Anger, 0, 100);
        officer.Fear = Mathf.Clamp(officer.Fear, 0, 100);
    }
}
