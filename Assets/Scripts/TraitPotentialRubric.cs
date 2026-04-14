using UnityEngine;

/// <summary>
/// Core trait <b>potential</b> only: 0–5 stars. XP gaps are intentionally steep vs skills.
/// Cumulative XP[L] = minimum total XP to <b>reach</b> star level L (L=0 → no XP required).
/// </summary>
public static class TraitPotentialRubric
{
    public const int MaxTraitLevel = 5;

    /// <summary>Minimum total XP to have reached level L (index 0..5).</summary>
    private static readonly int[] CumulativeXpAtLevel =
    {
        0,
        1_200,
        5_000,
        14_000,
        35_000,
        85_000
    };

    public static int GetCumulativeXpForLevel(int level)
    {
        level = Mathf.Clamp(level, 0, MaxTraitLevel);
        return CumulativeXpAtLevel[level];
    }

    /// <summary>Alias: XP threshold at the start of <paramref name="level"/>.</summary>
    public static int GetTotalXpForTraitLevel(int level) => GetCumulativeXpForLevel(level);

    public static int GetTraitLevelFromXp(int xp)
    {
        if (xp <= 0)
            return 0;
        xp = Mathf.Min(xp, CumulativeXpAtLevel[MaxTraitLevel]);
        for (int L = MaxTraitLevel; L >= 1; L--)
        {
            if (xp >= CumulativeXpAtLevel[L])
                return L;
        }

        return 0;
    }

    public static int GetXpToNextTraitLevel(int xp)
    {
        int level = GetTraitLevelFromXp(xp);
        if (level >= MaxTraitLevel)
            return 0;
        return CumulativeXpAtLevel[level + 1] - xp;
    }

    public static float GetFractionIntoCurrentTraitLevel(int xp)
    {
        int level = GetTraitLevelFromXp(xp);
        if (level >= MaxTraitLevel)
            return 1f;
        int low = CumulativeXpAtLevel[level];
        int high = CumulativeXpAtLevel[level + 1];
        if (high <= low)
            return 1f;
        return Mathf.Clamp01((float)(xp - low) / (high - low));
    }

    public static int GetMaxStorableTraitXp() => CumulativeXpAtLevel[MaxTraitLevel];

    /// <summary>
    /// Maps legacy trait XP (100×L² curve, levels 0–10) onto the new 0–5 potential curve,
    /// preserving approximate progress within the old band when possible.
    /// </summary>
    public static int MigrateLegacyTraitXp(int legacyXp)
    {
        if (legacyXp <= 0)
            return 0;

        const int legacyMaxTotalXp = 100 * 10 * 10;
        const int legacyTier5Threshold = 100 * 5 * 5;

        // Legacy stars 5–10 (XP ≥ 2500): compress into new stars 5–max potential.
        if (legacyXp >= legacyTier5Threshold)
        {
            float u = Mathf.Clamp01((legacyXp - legacyTier5Threshold) / (float)(legacyMaxTotalXp - legacyTier5Threshold));
            int migrated = Mathf.RoundToInt(Mathf.Lerp(GetCumulativeXpForLevel(5), GetMaxStorableTraitXp(), u));
            return Mathf.Clamp(migrated, 0, GetMaxStorableTraitXp());
        }

        int oldLevel = Mathf.Clamp(Mathf.FloorToInt(Mathf.Sqrt(legacyXp / 100f)), 0, 10);
        int oldLow = 100 * oldLevel * oldLevel;
        int oldHigh = 100 * (oldLevel + 1) * (oldLevel + 1);
        if (oldHigh <= oldLow)
            return GetCumulativeXpForLevel(Mathf.Min(oldLevel, MaxTraitLevel));

        float t = Mathf.Clamp01((legacyXp - oldLow) / (float)(oldHigh - oldLow));
        int newLow = GetCumulativeXpForLevel(Mathf.Min(oldLevel, MaxTraitLevel));
        int newHigh = GetCumulativeXpForLevel(Mathf.Min(oldLevel + 1, MaxTraitLevel));
        int migratedBand = Mathf.RoundToInt(newLow + t * (newHigh - newLow));
        return Mathf.Clamp(migratedBand, 0, GetMaxStorableTraitXp());
    }
}
