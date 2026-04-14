using UnityEngine;

public readonly struct OperationResolution
{
    public readonly OperationType Operation;
    public readonly bool Success;
    public readonly OutcomeTier Tier;
    public readonly float ObjectiveScore;
    public readonly float ConsequenceScore;
    public readonly float Chance;
    public readonly CoreTrait PrimaryTrait;
    public readonly CoreTrait SecondaryTrait;
    public readonly DerivedSkill LinkedSkill;

    public OperationResolution(
        OperationType operation,
        bool success,
        OutcomeTier tier,
        float objectiveScore,
        float consequenceScore,
        float chance,
        CoreTrait primaryTrait,
        CoreTrait secondaryTrait,
        DerivedSkill linkedSkill)
    {
        Operation = operation;
        Success = success;
        Tier = tier;
        ObjectiveScore = objectiveScore;
        ConsequenceScore = consequenceScore;
        Chance = chance;
        PrimaryTrait = primaryTrait;
        SecondaryTrait = secondaryTrait;
        LinkedSkill = linkedSkill;
    }
}

/// <summary>
/// Resolves operations with dual scores + tier; trains primary skills and traits.
/// </summary>
public static class OperationResolutionSystem
{
    public static OperationResolution Resolve(OperationType op, PlayerCharacterProfile bossProfile)
    {
        CoreTraitProgression.EnsureRubricsInitialized(bossProfile);
        DerivedSkillProgression.EnsureSkillXpInitialized(bossProfile);

        CoreTrait primary = OperationRegistry.GetPrimaryTrait(op);
        CoreTrait secondary = OperationRegistry.GetSecondaryTrait(op);
        DerivedSkill skill = OperationRegistry.GetDerivedSkill(op);

        float difficultyScore = GetDifficultyScore(op);
        float resistanceScore = GetYearResistanceScore();
        int requiredStars = OperationRegistry.GetSuggestedSkillRequirement(op);
        int currentStars = DerivedSkillProgression.GetLevel(bossProfile, skill);
        float mismatch = SkillPotentialRules.GetComplexityMismatchPenalty(requiredStars, currentStars);

        float skillEffective = DerivedSkillProgression.GetEffectiveActionScore(bossProfile, skill);
        float moraleMod = 0f;
        if (PersonnelRegistry.Members != null && PersonnelRegistry.Members.Count > 0 &&
            PersonnelRegistry.Members[0] != null)
            moraleMod = CrewMoraleSystem.GetMoralePerformanceMultiplier(PersonnelRegistry.Members[0].PersonalMorale);

        float repMod = GetReputationObjectiveMod(op, bossProfile);
        float statePenalty = GetBossStatePenalty(bossProfile);
        float randomVar = Random.Range(-6f, 6f);

        float objectiveScore = skillEffective + moraleMod * 10f + repMod - difficultyScore - resistanceScore - mismatch + randomVar - statePenalty;

        float exposureBase = GetExposureBase(op);
        float pressure = resistanceScore * 0.35f;
        float controlFactor = Mathf.Clamp01(skillEffective / 120f) * 8f;
        float consRandom = Random.Range(-5f, 5f);
        float consequenceScore = exposureBase + pressure + statePenalty * 0.5f - controlFactor + consRandom;

        OutcomeTier tier = OutcomeTierMapper.Map(objectiveScore, consequenceScore);
        bool success = OutcomeTierMapper.MeetsObjectiveLine(tier);

        float chanceEstimate = Mathf.Clamp01(0.35f + objectiveScore / 120f);

        float outcomeFactor = OutcomeTierMapper.GetOutcomeXpMultiplier(tier);
        float diffFactor = GetDifficultyFactor(op);
        int baseSkillXp = success ? 42 : 18;
        DerivedSkillProgression.ApplySkillPractice(bossProfile, skill, baseSkillXp, outcomeFactor, diffFactor);

        CoreTraitProgression.AddPractice(bossProfile, primary, success ? 10 : 4);
        CoreTraitProgression.AddPractice(bossProfile, secondary, success ? 5 : 2);

        return new OperationResolution(
            op,
            success,
            tier,
            objectiveScore,
            consequenceScore,
            chanceEstimate,
            primary,
            secondary,
            skill);
    }

    private static float GetDifficultyFactor(OperationType op)
    {
        return op switch
        {
            OperationType.Collect => 1.3f,
            OperationType.Surveillance => 1.15f,
            _ => 1f
        };
    }

    private static float GetDifficultyScore(OperationType op)
    {
        return op switch
        {
            OperationType.Scout => 18f,
            OperationType.Surveillance => 24f,
            OperationType.Collect => 26f,
            _ => 22f
        };
    }

    private static float GetYearResistanceScore()
    {
        return GameCalendarSystem.GetOppositionChancePenalty(GameSessionState.CurrentDay) * 90f;
    }

    private static float GetExposureBase(OperationType op)
    {
        return op switch
        {
            OperationType.Scout => 10f,
            OperationType.Surveillance => 8f,
            OperationType.Collect => 16f,
            _ => 12f
        };
    }

    private static float GetReputationObjectiveMod(OperationType op, PlayerCharacterProfile bossProfile)
    {
        if (bossProfile == null)
            return 0f;
        int crewRep = Mathf.Clamp(bossProfile.PublicReputation, -100, 100);
        return op switch
        {
            OperationType.Collect => crewRep * 0.04f,
            OperationType.Surveillance => crewRep * 0.02f,
            _ => crewRep * 0.015f
        };
    }

    /// <summary>Fatigue/stress on boss profile — stub until boss has state fields.</summary>
    private static float GetBossStatePenalty(PlayerCharacterProfile bossProfile)
    {
        return 0f;
    }
}
