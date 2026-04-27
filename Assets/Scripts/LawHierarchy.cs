using System;

/// <summary>
/// In-world hierarchy: higher tiers override lower ones when they conflict (constitutional rights vs ordinary statutes).
/// Values are ordered so <i>smaller</i> ordinal = <b>higher</b> legal force.
/// </summary>
public enum LawTier
{
    /// <summary>City Charter / bill of rights — top of stack.</summary>
    ConstitutionalCharter = 0,

    /// <summary>Federal acts (e.g. revenue, interstate crime) when the setting has a national layer.</summary>
    FederalStatute = 1,

    /// <summary>Consolidated criminal / civil code of the city-state.</summary>
    CityCode = 2,

    /// <summary>Municipal ordinances, licensing, local police orders.</summary>
    MunicipalOrdinance = 3
}

/// <summary>
/// Static rules for which text wins when two sources disagree.
/// </summary>
public static class LawHierarchy
{
    /// <summary>True if <paramref name="higher"/> should prevail over <paramref name="lower"/>.</summary>
    public static bool IsHigherThan(LawTier higher, LawTier lower)
    {
        return (int)higher < (int)lower;
    }

    /// <summary>
    /// When a right (Charter) conflicts with a statute, the Charter wins unless the statute explicitly
    /// authorizes a narrow limitation (public safety, tax assessment, etc.) — gameplay / narrative hook.
    /// </summary>
    public static LawTier ResolveStronger(LawTier a, LawTier b)
    {
        return IsHigherThan(a, b) ? a : b;
    }

    public static string DescribePrecedenceEn()
    {
        return
            "<b>Hierarchy (strongest first):</b>\n" +
            "1) City Charter — fundamental rights\n" +
            "2) Federal statute — where national law applies\n" +
            "3) City code — crimes, courts, penalties\n" +
            "4) Municipal ordinances — licensing, local rules\n\n" +
            "<size=90%><i>Conflict rule:</i> a lower tier cannot erase a higher right unless a higher " +
            "instrument itself authorizes a specific, limited exception (e.g. public safety, lawful tax levy).</size>";
    }
}
