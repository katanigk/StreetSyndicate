using UnityEngine;

/// <summary>
/// Trait potential caps skill growth: max skill stars = <b>2 × T</b> (T = potential tier 0..5 from skills).
/// </summary>
public static class SkillPotentialRules
{
    public const int MaxPotentialTier = 5;

    public static int GetPotentialTier(PlayerCharacterProfile profile, CoreTrait trait)
    {
        if (profile == null)
            return 0;
        CoreTraitProgression.EnsureRubricsInitialized(profile);
        return Mathf.Min(CoreTraitProgression.GetLevel(profile, trait), MaxPotentialTier);
    }

    /// <summary>Primary / secondary for action resolution — aligns with design doc §3.4.</summary>
    public static CoreTrait GetPrimaryTrait(DerivedSkill skill)
    {
        switch (skill)
        {
            case DerivedSkill.Brawling: return CoreTrait.Strength;
            case DerivedSkill.Firearms: return CoreTrait.Agility;
            case DerivedSkill.Stealth: return CoreTrait.Agility;
            case DerivedSkill.Driving: return CoreTrait.Agility;
            case DerivedSkill.Lockpicking: return CoreTrait.Intelligence;
            case DerivedSkill.Surveillance: return CoreTrait.Intelligence;
            case DerivedSkill.Negotiation: return CoreTrait.Charisma;
            case DerivedSkill.Intimidation: return CoreTrait.Strength;
            case DerivedSkill.Deception: return CoreTrait.Charisma;
            case DerivedSkill.Logistics: return CoreTrait.Intelligence;
            case DerivedSkill.Leadership: return CoreTrait.Charisma;
            case DerivedSkill.Medicine: return CoreTrait.Intelligence;
            case DerivedSkill.Sabotage: return CoreTrait.Intelligence;
            default: return CoreTrait.Determination;
        }
    }

    public static CoreTrait GetSecondaryTrait(DerivedSkill skill)
    {
        switch (skill)
        {
            case DerivedSkill.Brawling: return CoreTrait.Agility;
            case DerivedSkill.Firearms: return CoreTrait.MentalResilience;
            case DerivedSkill.Stealth: return CoreTrait.Intelligence;
            case DerivedSkill.Driving: return CoreTrait.Intelligence;
            case DerivedSkill.Lockpicking: return CoreTrait.Agility;
            case DerivedSkill.Surveillance: return CoreTrait.MentalResilience;
            case DerivedSkill.Negotiation: return CoreTrait.Intelligence;
            case DerivedSkill.Intimidation: return CoreTrait.Charisma;
            case DerivedSkill.Deception: return CoreTrait.Intelligence;
            case DerivedSkill.Logistics: return CoreTrait.Determination;
            case DerivedSkill.Leadership: return CoreTrait.Determination;
            case DerivedSkill.Medicine: return CoreTrait.MentalResilience;
            case DerivedSkill.Sabotage: return CoreTrait.Agility;
            default: return CoreTrait.MentalResilience;
        }
    }

    /// <summary>Max skill stars allowed for this skill (2 × primary-trait potential).</summary>
    public static int GetSkillCapStars(PlayerCharacterProfile profile, DerivedSkill skill)
    {
        int t = GetPotentialTier(profile, GetPrimaryTrait(skill));
        return Mathf.Min(StarRubric.MaxLevel, t * 2);
    }

    public static int GetFastCap(PlayerCharacterProfile profile, DerivedSkill skill)
    {
        return GetSkillCapStars(profile, skill);
    }

    public static int GetOverpushCap(PlayerCharacterProfile profile, DerivedSkill skill)
    {
        return GetFastCap(profile, skill);
    }

    /// <summary>Practice continues toward odd gate milestones even when visible stars hit cap; only stops at max bank.</summary>
    public static float GetGrowthMultiplier(PlayerCharacterProfile profile, DerivedSkill skill, int skillStars, int skillBankXp)
    {
        if (skillBankXp >= StarRubric.GetTotalXpForLevel(StarRubric.MaxLevel))
            return 0f;
        return 1f;
    }

    /// <summary>§10.5 — harsh penalty when mission demands more skill than current stars.</summary>
    public static float GetComplexityMismatchPenalty(int requiredStars, int currentStars)
    {
        return Mathf.Max(0, requiredStars - currentStars) * 12f;
    }

    public static float GetCatastrophicRiskBonus(int requiredStars, int currentStars)
    {
        return Mathf.Max(0, requiredStars - currentStars - 1) * 8f;
    }
}
