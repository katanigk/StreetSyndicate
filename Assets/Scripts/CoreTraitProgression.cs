using UnityEngine;

/// <summary>
/// Core trait <b>potential</b> (0–5 stars) is <b>not</b> stored as trait XP — it is derived from gate skills and
/// cumulative trait-directed practice (<see cref="TraitPotentialFromSkills"/>). Physical / float bars are narrative stats only.
/// </summary>
public static class CoreTraitProgression
{
    public const int MaxRubricLevel = TraitPotentialRubric.MaxTraitLevel;

    public static float GetValue(PlayerCharacterProfile profile, CoreTrait trait)
    {
        if (profile == null)
            return 0f;

        switch (trait)
        {
            case CoreTrait.Strength: return profile.Physical;
            case CoreTrait.Agility: return profile.Agility;
            case CoreTrait.Intelligence: return profile.Intelligence;
            case CoreTrait.Charisma: return profile.Charisma;
            case CoreTrait.MentalResilience: return profile.MentalResilience;
            case CoreTrait.Determination: return profile.Determination;
            default: return 0f;
        }
    }

    /// <summary>Legacy fields — potential is skill-derived; kept for save compatibility only.</summary>
    public static int GetXp(PlayerCharacterProfile profile, CoreTrait trait)
    {
        return 0;
    }

    public static void SetXp(PlayerCharacterProfile profile, CoreTrait trait, int xp)
    {
        if (profile == null)
            return;

        int safe = Mathf.Clamp(Mathf.Max(0, xp), 0, TraitPotentialRubric.GetMaxStorableTraitXp());
        switch (trait)
        {
            case CoreTrait.Strength: profile.StrengthXp = safe; break;
            case CoreTrait.Agility: profile.AgilityXp = safe; break;
            case CoreTrait.Intelligence: profile.IntelligenceXp = safe; break;
            case CoreTrait.Charisma: profile.CharismaXp = safe; break;
            case CoreTrait.MentalResilience: profile.MentalResilienceXp = safe; break;
            case CoreTrait.Determination: profile.DeterminationXp = safe; break;
        }
    }

    public static void EnsureRubricsInitialized(PlayerCharacterProfile profile)
    {
        if (profile == null)
            return;

        if (profile.TraitXpRubricVersion < 1)
            MigrateTraitXpToPotentialRubric(profile);

        if (profile.TraitXpRubricVersion < 2)
        {
            profile.StrengthXp = 0;
            profile.AgilityXp = 0;
            profile.IntelligenceXp = 0;
            profile.CharismaXp = 0;
            profile.MentalResilienceXp = 0;
            profile.DeterminationXp = 0;
            profile.TraitXpRubricVersion = 2;
        }
    }

    private static void MigrateTraitXpToPotentialRubric(PlayerCharacterProfile profile)
    {
        foreach (CoreTrait t in System.Enum.GetValues(typeof(CoreTrait)))
        {
            int raw = GetLegacyTraitXp(profile, t);
            if (raw <= 0)
                continue;
            int migrated = TraitPotentialRubric.MigrateLegacyTraitXp(raw);
            SetXp(profile, t, migrated);
        }

        profile.TraitXpRubricVersion = 1;
    }

    private static int GetLegacyTraitXp(PlayerCharacterProfile profile, CoreTrait trait)
    {
        switch (trait)
        {
            case CoreTrait.Strength: return profile.StrengthXp;
            case CoreTrait.Agility: return profile.AgilityXp;
            case CoreTrait.Intelligence: return profile.IntelligenceXp;
            case CoreTrait.Charisma: return profile.CharismaXp;
            case CoreTrait.MentalResilience: return profile.MentalResilienceXp;
            case CoreTrait.Determination: return profile.DeterminationXp;
            default: return 0;
        }
    }

    public static int GetLevel(PlayerCharacterProfile profile, CoreTrait trait)
    {
        return TraitPotentialFromSkills.GetPotentialTier(profile, trait);
    }

    public static int GetXpToNextLevel(PlayerCharacterProfile profile, CoreTrait trait)
    {
        return 0;
    }

    public static float GetEffectiveValue(PlayerCharacterProfile profile, CoreTrait trait)
    {
        float baseValue = GetValue(profile, trait);
        int level = GetLevel(profile, trait);
        return baseValue + level * 4f;
    }

    /// <summary>Routes trait-tagged XP into linked derived skills (design: numbers live on skills only).</summary>
    public static void AddPractice(PlayerCharacterProfile profile, CoreTrait trait, int amount)
    {
        TraitToSkillDistribution.AddTraitPracticeAsSkillXp(profile, trait, amount);
    }
}
