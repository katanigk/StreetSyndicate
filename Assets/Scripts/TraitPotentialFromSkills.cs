using System;
using UnityEngine;

/// <summary>
/// Core trait <b>potential</b> (0–5): odd-tier thresholds use the greater of max gate-skill bank XP and
/// cumulative trait-directed practice (full amount before split into skills).
/// </summary>
public static class TraitPotentialFromSkills
{
    /// <summary>Skills whose <see cref="SkillPotentialRules.GetPrimaryTrait"/> matches — gate potential for that trait.</summary>
    public static ReadOnlySpan<DerivedSkill> GetPrimaryGateSkills(CoreTrait trait)
    {
        switch (trait)
        {
            case CoreTrait.Strength:
                return new[] { DerivedSkill.Brawling, DerivedSkill.Intimidation };
            case CoreTrait.Agility:
                return new[] { DerivedSkill.Firearms, DerivedSkill.Stealth, DerivedSkill.Driving };
            case CoreTrait.Intelligence:
                return new[] { DerivedSkill.Lockpicking, DerivedSkill.Surveillance, DerivedSkill.Logistics, DerivedSkill.Medicine, DerivedSkill.Sabotage, DerivedSkill.Analysis, DerivedSkill.Legal, DerivedSkill.Finance };
            case CoreTrait.Charisma:
                return new[] { DerivedSkill.Negotiation, DerivedSkill.Deception, DerivedSkill.Leadership, DerivedSkill.Persuasion };
            case CoreTrait.MentalResilience:
                return Array.Empty<DerivedSkill>();
            case CoreTrait.Determination:
                return Array.Empty<DerivedSkill>();
            default:
                return Array.Empty<DerivedSkill>();
        }
    }

    public static int GetMaxBankXpAmongGateSkills(PlayerCharacterProfile profile, CoreTrait trait)
    {
        if (profile == null || profile.DerivedSkillXp == null || profile.DerivedSkillXp.Length != DerivedSkillProgression.SkillCount)
            return 0;

        ReadOnlySpan<DerivedSkill> gate = GetPrimaryGateSkills(trait);
        int maxBank = 0;
        if (gate.Length > 0)
        {
            for (int i = 0; i < gate.Length; i++)
            {
                int xp = profile.DerivedSkillXp[(int)gate[i]];
                if (xp > maxBank)
                    maxBank = xp;
            }
        }
        else
            maxBank = MaxBankXpWhereTraitInfluences(profile, trait);

        return maxBank;
    }

    public static int GetPotentialTier(PlayerCharacterProfile profile, CoreTrait trait)
    {
        int maxBank = GetMaxBankXpAmongGateSkills(profile, trait);
        int directed = PlayerCharacterProfile.GetDirectedTraitPracticeXp(profile, trait);
        int effectiveXp = Mathf.Max(maxBank, directed);
        int bankT = Mathf.Clamp(StarRubric.GetPotentialTierUnlockedByMaxBankXp(effectiveXp), 0, TraitPotentialRubric.MaxTraitLevel);
        int interviewCap = PlayerCharacterProfile.GetInterviewPotentialCeilingTier(profile, trait);
        return Mathf.Min(bankT, interviewCap);
    }

    private static int MaxBankXpWhereTraitInfluences(PlayerCharacterProfile profile, CoreTrait trait)
    {
        int maxBank = 0;
        for (int s = 0; s < DerivedSkillProgression.SkillCount; s++)
        {
            var sk = (DerivedSkill)s;
            ReadOnlySpan<DerivedSkillTraitMatrix.Influence> infl = DerivedSkillTraitMatrix.GetInfluences(sk);
            bool touches = false;
            for (int i = 0; i < infl.Length; i++)
            {
                if (infl[i].Trait == trait && infl[i].Weight > 0f)
                {
                    touches = true;
                    break;
                }
            }

            if (!touches)
                continue;
            int xp = profile.DerivedSkillXp[(int)sk];
            if (xp > maxBank)
                maxBank = xp;
        }

        return maxBank;
    }
}
