using System;
using UnityEngine;

/// <summary>Maps interrogation / op bonuses tagged as core traits into <see cref="DerivedSkill"/> XP.</summary>
public static class TraitToSkillDistribution
{
    public static void AddTraitPracticeAsSkillXp(PlayerCharacterProfile profile, CoreTrait trait, int amount)
    {
        if (profile == null || amount == 0)
            return;

        if (profile.TraitDirectedPracticeXp == null || profile.TraitDirectedPracticeXp.Length != 6)
            profile.TraitDirectedPracticeXp = new int[6];
        int ti = (int)trait;
        if (ti >= 0 && ti < 6 && amount > 0)
            profile.TraitDirectedPracticeXp[ti] += amount;

        DerivedSkillProgression.EnsureSkillXpInitialized(profile);
        ReadOnlySpan<DerivedSkill> gate = TraitPotentialFromSkills.GetPrimaryGateSkills(trait);
        if (gate.Length > 0)
        {
            int share = amount / gate.Length;
            int rem = amount % gate.Length;
            for (int i = 0; i < gate.Length; i++)
            {
                int a = share + (i < rem ? 1 : 0);
                DerivedSkillProgression.AddSkillXpFlat(profile, gate[i], a);
            }

            return;
        }

        DistributeByInfluenceWeights(profile, trait, amount);
    }

    private static void DistributeByInfluenceWeights(PlayerCharacterProfile profile, CoreTrait trait, int amount)
    {
        float sum = 0f;
        for (int s = 0; s < DerivedSkillProgression.SkillCount; s++)
        {
            var sk = (DerivedSkill)s;
            ReadOnlySpan<DerivedSkillTraitMatrix.Influence> infl = DerivedSkillTraitMatrix.GetInfluences(sk);
            for (int i = 0; i < infl.Length; i++)
            {
                if (infl[i].Trait == trait && infl[i].Weight > 0f)
                {
                    sum += infl[i].Weight;
                    break;
                }
            }
        }

        if (sum <= 1e-5f)
            return;

        int allocated = 0;
        DerivedSkill last = DerivedSkill.Brawling;
        for (int s = 0; s < DerivedSkillProgression.SkillCount; s++)
        {
            var sk = (DerivedSkill)s;
            float w = 0f;
            ReadOnlySpan<DerivedSkillTraitMatrix.Influence> infl = DerivedSkillTraitMatrix.GetInfluences(sk);
            for (int i = 0; i < infl.Length; i++)
            {
                if (infl[i].Trait == trait && infl[i].Weight > 0f)
                {
                    w = infl[i].Weight;
                    break;
                }
            }

            if (w <= 0f)
                continue;
            int share = Mathf.RoundToInt(amount * (w / sum));
            if (share > 0)
            {
                DerivedSkillProgression.AddSkillXpFlat(profile, sk, share);
                allocated += share;
            }

            last = sk;
        }

        int rest = amount - allocated;
        if (rest != 0)
            DerivedSkillProgression.AddSkillXpFlat(profile, last, rest);
    }
}
