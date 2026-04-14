using System;
using UnityEngine;

/// <summary>UI helpers when trait potential is skill-derived (no separate trait XP bar).</summary>
public static class TraitPotentialPresentation
{
    public static float GetAggregateGateSkillProgress01(PlayerCharacterProfile profile, CoreTrait trait)
    {
        if (profile == null)
            return 0f;

        ReadOnlySpan<DerivedSkill> gate = TraitPotentialFromSkills.GetPrimaryGateSkills(trait);
        if (gate.Length > 0)
        {
            float sum = 0f;
            for (int i = 0; i < gate.Length; i++)
                sum += DerivedSkillProgression.GetSkillProgressBarFill01(profile, gate[i]);
            return sum / gate.Length;
        }

        float acc = 0f;
        int n = 0;
        for (int s = 0; s < DerivedSkillProgression.SkillCount; s++)
        {
            var sk = (DerivedSkill)s;
            ReadOnlySpan<DerivedSkillTraitMatrix.Influence> infl = DerivedSkillTraitMatrix.GetInfluences(sk);
            for (int i = 0; i < infl.Length; i++)
            {
                if (infl[i].Trait == trait && infl[i].Weight > 0f)
                {
                    acc += DerivedSkillProgression.GetSkillProgressBarFill01(profile, sk);
                    n++;
                    break;
                }
            }
        }

        return n > 0 ? acc / n : 0f;
    }
}
