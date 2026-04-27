using System;
using UnityEngine;

public enum DerivedSkill
{
    Brawling,
    Firearms,
    Stealth,
    Driving,
    Lockpicking,
    Surveillance,
    Negotiation,
    Intimidation,
    Deception,
    Logistics,
    Leadership,
    Medicine,
    Sabotage,
    Analysis,
    Legal,
    Finance,
    Persuasion
}

/// <summary>
/// Bank XP per skill is monotonic. Odd milestones (1,3,5,7,9) “pay” into the gate; display uses bank minus per-skill paid bands.
/// </summary>
public static class DerivedSkillProgression
{
    public const int SkillCount = 17;

    /// <summary>
    /// New profiles only: do not grant skill bank XP from normalized Physical bars — that formula gives every skill
    /// the same high tier and forces T=2+ on all core traits. Potential and skill bank should come from
    /// interrogation bonuses (<see cref="TraitToSkillDistribution"/>) and field play instead. Set to 1–2 only if you want a tiny intrinsic seed.
    /// </summary>
    public const int MaxIntrinsicSeedStarLevel = 0;

    public static string GetDisplayName(DerivedSkill skill)
    {
        switch (skill)
        {
            case DerivedSkill.Brawling: return "Brawling";
            case DerivedSkill.Firearms: return "Firearms";
            case DerivedSkill.Stealth: return "Stealth";
            case DerivedSkill.Driving: return "Driving";
            case DerivedSkill.Lockpicking: return "Lockpicking";
            case DerivedSkill.Surveillance: return "Surveillance";
            case DerivedSkill.Negotiation: return "Negotiation";
            case DerivedSkill.Intimidation: return "Intimidation";
            case DerivedSkill.Deception: return "Deception";
            case DerivedSkill.Logistics: return "Logistics";
            case DerivedSkill.Leadership: return "Leadership";
            case DerivedSkill.Medicine: return "Medicine";
            case DerivedSkill.Sabotage: return "Sabotage";
            case DerivedSkill.Analysis: return "Analysis";
            case DerivedSkill.Legal: return "Legal";
            case DerivedSkill.Finance: return "Finance";
            case DerivedSkill.Persuasion: return "Persuasion";
            default: return skill.ToString();
        }
    }

    public static void EnsureSkillXpInitialized(PlayerCharacterProfile profile)
    {
        if (profile == null)
            return;

        if (profile.DerivedSkillXp == null)
            profile.DerivedSkillXp = new int[SkillCount];
        else if (profile.DerivedSkillXp.Length != SkillCount)
        {
            int[] resized = new int[SkillCount];
            int copy = Mathf.Min(profile.DerivedSkillXp.Length, SkillCount);
            for (int i = 0; i < copy; i++)
                resized[i] = profile.DerivedSkillXp[i];
            profile.DerivedSkillXp = resized;
        }

        if (profile.SkillRubricVersion < 1)
        {
            for (int i = 0; i < SkillCount; i++)
            {
                int oldXp = profile.DerivedSkillXp[i];
                if (oldXp <= 0)
                    continue;
                int lvl = StarRubric.GetLegacyLevelFromOldQuadraticXp(oldXp);
                profile.DerivedSkillXp[i] = StarRubric.GetTotalXpForLevel(lvl);
            }

            profile.SkillRubricVersion = 1;
        }

        bool any = false;
        for (int i = 0; i < SkillCount; i++)
        {
            if (profile.DerivedSkillXp[i] > 0)
            {
                any = true;
                break;
            }
        }

        if (any)
        {
            ClampSkillBankToGlobalMax(profile);
            return;
        }

        CoreTraitProgression.EnsureRubricsInitialized(profile);
        for (int i = 0; i < SkillCount; i++)
        {
            var sk = (DerivedSkill)i;
            int derivedFromTraits = DerivedSkillTraitMatrix.GetTraitDerivedStarLevelFromIntrinsicBars(profile, sk);
            derivedFromTraits = Mathf.Min(derivedFromTraits, MaxIntrinsicSeedStarLevel);
            profile.DerivedSkillXp[i] = StarRubric.GetTotalXpForLevel(derivedFromTraits);
        }

        ClampSkillBankToGlobalMax(profile);
    }

    public static int GetSkillXp(PlayerCharacterProfile profile, DerivedSkill skill)
    {
        EnsureSkillXpInitialized(profile);
        int i = (int)skill;
        if (i < 0 || i >= SkillCount)
            return 0;
        return profile.DerivedSkillXp[i];
    }

    /// <summary>XP driving visible stars after gate payments for this skill’s own bank.</summary>
    public static int GetDisplaySkillXp(PlayerCharacterProfile profile, DerivedSkill skill)
    {
        int bank = GetSkillXp(profile, skill);
        int paid = StarRubric.GetPerSkillGatePaymentXp(bank);
        return Mathf.Max(0, bank - paid);
    }

    public static void SetSkillXp(PlayerCharacterProfile profile, DerivedSkill skill, int xp)
    {
        EnsureSkillXpInitialized(profile);
        int i = (int)skill;
        if (i < 0 || i >= SkillCount)
            return;
        int maxXp = StarRubric.GetTotalXpForLevel(StarRubric.MaxLevel);
        profile.DerivedSkillXp[i] = Mathf.Clamp(xp, 0, maxXp);
        ClampSkillBankToGlobalMax(profile);
    }

    public static void AddSkillXpFlat(PlayerCharacterProfile profile, DerivedSkill skill, int delta)
    {
        if (profile == null || delta == 0)
            return;
        EnsureSkillXpInitialized(profile);
        int i = (int)skill;
        if (i < 0 || i >= SkillCount)
            return;
        int next = Mathf.Max(0, GetSkillXp(profile, skill) + delta);
        profile.DerivedSkillXp[i] = next;
        ClampSkillBankToGlobalMax(profile);
    }

    public static void ClampSkillBankToGlobalMax(PlayerCharacterProfile profile)
    {
        if (profile?.DerivedSkillXp == null || profile.DerivedSkillXp.Length != SkillCount)
            return;

        int maxXp = StarRubric.GetTotalXpForLevel(StarRubric.MaxLevel);
        for (int i = 0; i < SkillCount; i++)
        {
            if (profile.DerivedSkillXp[i] > maxXp)
                profile.DerivedSkillXp[i] = maxXp;
        }
    }

    /// <summary>Visible star level (0–10), capped by 2 × core potential.</summary>
    public static int GetLevel(PlayerCharacterProfile profile, DerivedSkill skill)
    {
        if (profile == null)
            return 0;
        EnsureSkillXpInitialized(profile);
        int displayXp = GetDisplaySkillXp(profile, skill);
        int rubricLevel = StarRubric.GetLevelFromXp(displayXp);
        int cap = SkillPotentialRules.GetSkillCapStars(profile, skill);
        return Mathf.Min(rubricLevel, cap);
    }

    public static int GetRawLevelFromXp(PlayerCharacterProfile profile, DerivedSkill skill)
    {
        if (profile == null)
            return 0;
        EnsureSkillXpInitialized(profile);
        return StarRubric.GetLevelFromXp(GetSkillXp(profile, skill));
    }

    /// <summary>Design §3.5 / §12 — base contribution to action resolution.</summary>
    public static float GetEffectiveActionScore(PlayerCharacterProfile profile, DerivedSkill skill)
    {
        if (profile == null)
            return 0f;
        EnsureSkillXpInitialized(profile);
        int stars = GetLevel(profile, skill);
        int p = SkillPotentialRules.GetPotentialTier(profile, SkillPotentialRules.GetPrimaryTrait(skill));
        int s = SkillPotentialRules.GetPotentialTier(profile, SkillPotentialRules.GetSecondaryTrait(skill));
        float skillBase = stars * 10f;
        float traitSupport = p * 6f + s * 3f;
        return skillBase + traitSupport;
    }

    /// <summary>
    /// 0–1 fill for HUD bars. Uses <b>raw</b> skill bank within the current star band so the bar does not go empty
    /// whenever <see cref="GetDisplaySkillXp"/> hits zero after odd-tier gate payments (stars still follow display XP).
    /// </summary>
    public static float GetSkillProgressBarFill01(PlayerCharacterProfile profile, DerivedSkill skill)
    {
        if (profile == null)
            return 0f;
        EnsureSkillXpInitialized(profile);
        CoreTrait primary = SkillPotentialRules.GetPrimaryTrait(skill);
        int t = SkillPotentialRules.GetPotentialTier(profile, primary);
        int bank = GetSkillXp(profile, skill);
        int capLevel = SkillPotentialRules.GetSkillCapStars(profile, skill);

        if (t <= 0)
        {
            int first = StarRubric.GetTotalXpForLevel(1);
            if (first <= 0)
                return 0f;
            return Mathf.Clamp01((float)bank / first);
        }

        if (capLevel <= 0)
            return 0f;

        int rawLevel = StarRubric.GetLevelFromXp(bank);
        int band = Mathf.Min(rawLevel, capLevel);
        if (band >= capLevel)
            return 1f;

        int low = StarRubric.GetTotalXpForLevel(band);
        int high = StarRubric.GetTotalXpForLevel(band + 1);
        if (high <= low)
            return 1f;
        return Mathf.Clamp01((float)(bank - low) / (high - low));
    }

    /// <summary>Award skill XP; bank can exceed display cap until next odd milestone (feeds the gate).</summary>
    public static void ApplySkillPractice(
        PlayerCharacterProfile profile,
        DerivedSkill skill,
        int baseSkillXp,
        float outcomeFactor,
        float difficultyFactor = 1f)
    {
        if (profile == null || baseSkillXp <= 0)
            return;

        EnsureSkillXpInitialized(profile);
        int bank = GetSkillXp(profile, skill);
        if (bank >= StarRubric.GetTotalXpForLevel(StarRubric.MaxLevel))
            return;

        int stars = GetLevel(profile, skill);
        float gm = SkillPotentialRules.GetGrowthMultiplier(profile, skill, stars, bank);
        if (gm <= 0f)
            return;

        int gain = Mathf.Max(0, Mathf.RoundToInt(baseSkillXp * outcomeFactor * difficultyFactor * gm));
        if (gain <= 0)
            return;

        int next = bank + gain;
        int maxBank = StarRubric.GetTotalXpForLevel(StarRubric.MaxLevel);
        SetSkillXp(profile, skill, Mathf.Min(next, maxBank));
    }
}
