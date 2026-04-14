/// <summary>
/// Economic anchor for the game world (US dollars, Prohibition / late 1920s–30s feel).
/// Numbers here are <b>design choices</b> — not tied to one historical source, but kept internally consistent:
/// a big expense = many months of "straight" income so every dollar bites.
/// </summary>
public static class EconomyBaseline
{
    /// <summary>
    /// Reference monthly income for a "plain" worker (unskilled hire / part-time / rural / bottom urban tier).
    /// Historically, industrial averages in the 1920s could exceed full-time unskilled pay;
    /// $40/month is closer to poverty / slack work and fits a game where cars and big purchases feel out of reach without crime or capital.
    /// </summary>
    public const int ReferenceUnskilledMonthlyIncomeUsd = 40;

    /// <summary>Optional: skilled worker / "comfortable" family — for copy and comparisons, not a hard gate.</summary>
    public const int ReferenceSkilledMonthlyIncomeUsd = 120;

    /// <summary>How many months of "unskilled" wage cover a one-time cost (for display / tips).</summary>
    public static float MonthsOfUnskilledWage(int costUsd)
    {
        if (costUsd <= 0 || ReferenceUnskilledMonthlyIncomeUsd <= 0)
            return 0f;
        return costUsd / (float)ReferenceUnskilledMonthlyIncomeUsd;
    }

    /// <summary>Short UI string (e.g. vehicle tooltip). English — localize for Hebrew UI.</summary>
    public static string FormatMonthsOfWage(int costUsd)
    {
        float m = MonthsOfUnskilledWage(costUsd);
        if (m <= 0f)
            return string.Empty;
        if (m < 1f)
            return "Less than one month at the reference wage.";
        return "About " + m.ToString("F1") + " months of wage (reference " + ReferenceUnskilledMonthlyIncomeUsd + "$/month).";
    }
}
