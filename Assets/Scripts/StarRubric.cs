using UnityEngine;

/// <summary>
/// Skill star rubric only (0–10). Cumulative XP uses tier costs 200, then ×3 each step (200→600→1800…).
/// </summary>
public static class StarRubric
{
    public const int MaxLevel = 10;

    private static readonly int[] CumulativeXpAtLevel;

    static StarRubric()
    {
        CumulativeXpAtLevel = new int[MaxLevel + 1];
        CumulativeXpAtLevel[0] = 0;
        int step = 200;
        for (int L = 1; L <= MaxLevel; L++)
        {
            CumulativeXpAtLevel[L] = CumulativeXpAtLevel[L - 1] + step;
            step *= 3;
        }
    }

    public static int GetTotalXpForLevel(int level)
    {
        int l = Mathf.Clamp(level, 0, MaxLevel);
        return CumulativeXpAtLevel[l];
    }

    public static int GetLevelFromXp(int xp)
    {
        if (xp <= 0)
            return 0;
        xp = Mathf.Min(xp, CumulativeXpAtLevel[MaxLevel]);
        for (int L = MaxLevel; L >= 1; L--)
        {
            if (xp >= CumulativeXpAtLevel[L])
                return L;
        }

        return 0;
    }

    public static int GetXpToAdvanceFromLevel(int fromLevel)
    {
        fromLevel = Mathf.Clamp(fromLevel, 0, MaxLevel);
        if (fromLevel >= MaxLevel)
            return 0;
        return GetTotalXpForLevel(fromLevel + 1) - GetTotalXpForLevel(fromLevel);
    }

    public static float GetFractionIntoCurrentLevel(int xp)
    {
        int level = GetLevelFromXp(xp);
        if (level >= MaxLevel)
            return 1f;
        int low = GetTotalXpForLevel(level);
        int high = GetTotalXpForLevel(level + 1);
        if (high <= low)
            return 1f;
        return Mathf.Clamp01((float)(xp - low) / (high - low));
    }

    /// <summary>Total bank XP required to unlock core potential tier T (1..5): threshold for skill stars 1,3,5,7,9.</summary>
    public static int GetTotalXpForPotentialTierUnlock(int potentialTier)
    {
        if (potentialTier <= 0)
            return 0;
        int oddSkillLevel = 2 * potentialTier - 1;
        return GetTotalXpForLevel(Mathf.Clamp(oddSkillLevel, 1, MaxLevel));
    }

    /// <summary>Highest potential T (0..5) such that max bank XP across gate skills reached the odd-tier threshold.</summary>
    public static int GetPotentialTierUnlockedByMaxBankXp(int maxBankXp)
    {
        if (maxBankXp <= 0)
            return 0;
        int best = 0;
        for (int t = 1; t <= 5; t++)
        {
            if (maxBankXp >= GetTotalXpForPotentialTierUnlock(t))
                best = t;
        }

        return best;
    }

    /// <summary>
    /// XP from this skill’s bank already “paid” into the gate (sum of completed odd bands 1→3→5→7→9 as deltas).
    /// Display progression uses (bank - this).
    /// </summary>
    public static int GetPerSkillGatePaymentXp(int bank)
    {
        if (bank <= 0)
            return 0;
        int sum = 0;
        int prevThreshold = 0;
        for (int oddL = 1; oddL <= 9; oddL += 2)
        {
            int th = GetTotalXpForLevel(oddL);
            if (bank >= th)
            {
                sum += th - prevThreshold;
                prevThreshold = th;
            }
        }

        return sum;
    }

    /// <summary>Legacy curve: level from total XP under 100×L² (pre skill-rubric v1).</summary>
    public static int GetLegacyLevelFromOldQuadraticXp(int xp)
    {
        if (xp <= 0)
            return 0;
        int l = Mathf.FloorToInt(Mathf.Sqrt(xp / 100f));
        return Mathf.Clamp(l, 0, MaxLevel);
    }
}
