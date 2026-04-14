/// <summary>
/// Six-tier resolution from objective line + consequence line (design: Family Business core revision).
/// </summary>
public enum OutcomeTier
{
    CriticalSuccess = 0,
    Success = 1,
    PartialSuccess = 2,
    CleanFailure = 3,
    Failure = 4,
    DisastrousFailure = 5
}

/// <summary>
/// Maps paired scores to a tier; thresholds are tuning knobs.
/// </summary>
public static class OutcomeTierMapper
{
    /// <summary>Higher = objective better achieved.</summary>
    public const float ObjectiveOutstanding = 22f;

    public const float ObjectivePass = 0f;

    /// <summary>Higher = worse fallout (more exposure / noise / heat).</summary>
    public const float ConsequenceMinimalMax = 8f;

    public const float ConsequenceManageableMax = 22f;

    public static OutcomeTier Map(float objectiveScore, float consequenceScore)
    {
        bool pass = objectiveScore >= ObjectivePass;
        bool outstanding = objectiveScore >= ObjectiveOutstanding;
        bool consMinimal = consequenceScore <= ConsequenceMinimalMax;
        bool consSevere = consequenceScore > ConsequenceManageableMax;

        if (outstanding && consMinimal)
            return OutcomeTier.CriticalSuccess;
        if (pass && consMinimal)
            return OutcomeTier.Success;
        if (pass && !consMinimal)
            return OutcomeTier.PartialSuccess;
        if (!pass && consMinimal)
            return OutcomeTier.CleanFailure;
        if (!pass && !consMinimal && !consSevere)
            return OutcomeTier.Failure;
        return OutcomeTier.DisastrousFailure;
    }

    /// <summary>Critical / Success / Partial — goal achieved (possibly with complications).</summary>
    public static bool MeetsObjectiveLine(OutcomeTier tier)
    {
        return tier == OutcomeTier.CriticalSuccess ||
               tier == OutcomeTier.Success ||
               tier == OutcomeTier.PartialSuccess;
    }

    public static float GetOutcomeXpMultiplier(OutcomeTier tier)
    {
        switch (tier)
        {
            case OutcomeTier.CriticalSuccess: return 1.35f;
            case OutcomeTier.Success: return 1.00f;
            case OutcomeTier.PartialSuccess: return 0.90f;
            case OutcomeTier.CleanFailure: return 0.70f;
            case OutcomeTier.Failure: return 0.55f;
            default: return 0.40f;
        }
    }

    public static string GetDisplayName(OutcomeTier tier)
    {
        switch (tier)
        {
            case OutcomeTier.CriticalSuccess: return "Critical success";
            case OutcomeTier.Success: return "Success";
            case OutcomeTier.PartialSuccess: return "Partial success";
            case OutcomeTier.CleanFailure: return "Clean failure";
            case OutcomeTier.Failure: return "Failure";
            default: return "Disastrous failure";
        }
    }
}
