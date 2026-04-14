using System;
using UnityEngine;

/// <summary>
/// <para><b>Conservation (no double-spend):</b> A single XP grant from an action is a <i>finite budget</i>.
/// It must be split across skills (or traits) with weights that sum to 1 — you cannot award the full amount independently
/// to every skill that touches the same trait, or you would inflate total XP (e.g. 100 Strength XP cannot become
/// 100 to Intimidation <i>and</i> 100 to Brawling from the same event).</para>
/// <para><b>Skill → trait feed:</b> When moving XP from a skill into underlying traits, use
/// <see cref="DerivedSkillTraitMatrix.GetInfluences"/> weights; each unit is counted once along the path.</para>
/// <para><b>Current build:</b> Traits have XP; skills are mostly <i>derived</i> from trait levels until per-skill XP is stored.
/// Operation outcomes still call <see cref="CoreTraitProgression.AddPractice"/> on traits directly — future work can
/// route through <see cref="DistributeIntegerBudgetByWeights"/> instead.</para>
/// </summary>
public static class XpProgressionRules
{
    /// <summary>
    /// Splits <paramref name="totalXp"/> into integer shares proportional to <paramref name="weights"/> (must sum to ~1).
    /// Sum of returned values always equals <paramref name="totalXp"/> (largest-remainder method).
    /// </summary>
    public static int[] DistributeIntegerBudgetByWeights(int totalXp, ReadOnlySpan<float> weights)
    {
        if (weights.Length == 0)
            return Array.Empty<int>();

        if (totalXp <= 0)
        {
            int[] zeros = new int[weights.Length];
            return zeros;
        }

        float sumW = 0f;
        for (int i = 0; i < weights.Length; i++)
            sumW += Mathf.Max(0f, weights[i]);

        if (sumW <= 1e-6f)
        {
            int[] equal = new int[weights.Length];
            int baseShare = totalXp / weights.Length;
            int rem = totalXp % weights.Length;
            for (int i = 0; i < weights.Length; i++)
                equal[i] = baseShare + (i < rem ? 1 : 0);
            return equal;
        }

        int n = weights.Length;
        float[] norm = new float[n];
        for (int i = 0; i < n; i++)
            norm[i] = Mathf.Max(0f, weights[i]) / sumW;

        float[] exact = new float[n];
        int[] floors = new int[n];
        int allocated = 0;
        for (int i = 0; i < n; i++)
        {
            exact[i] = totalXp * norm[i];
            floors[i] = Mathf.FloorToInt(exact[i]);
            allocated += floors[i];
        }

        int remainder = totalXp - allocated;
        if (remainder <= 0)
            return floors;

        float[] frac = new float[n];
        for (int i = 0; i < n; i++)
            frac[i] = exact[i] - floors[i];

        while (remainder > 0)
        {
            int best = 0;
            float bestFrac = -1f;
            for (int i = 0; i < n; i++)
            {
                if (frac[i] > bestFrac)
                {
                    bestFrac = frac[i];
                    best = i;
                }
            }
            floors[best]++;
            frac[best] = -1f;
            remainder--;
        }

        return floors;
    }

    /// <summary>
    /// Splits <paramref name="totalXp"/> across traits according to <see cref="DerivedSkillTraitMatrix"/> (weights sum to 1).
    /// </summary>
    public static int[] DistributeXpToTraitsForSkill(int totalXp, DerivedSkill skill)
    {
        ReadOnlySpan<DerivedSkillTraitMatrix.Influence> influences = DerivedSkillTraitMatrix.GetInfluences(skill);
        if (influences.Length == 0)
            return Array.Empty<int>();

        float[] w = new float[influences.Length];
        for (int i = 0; i < influences.Length; i++)
            w[i] = influences[i].Weight;

        return DistributeIntegerBudgetByWeights(totalXp, w);
    }
}
