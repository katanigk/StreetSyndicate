using UnityEngine;

/// <summary>
/// Procedural scaffolding: who tries what, and the criminal pipeline (1920s–30s tone).
/// </summary>
public enum CriminalCasePhase
{
    Intelligence = 0,
    Investigation = 1,
    Charging = 2,
    PretrialCustodyOrBail = 3,
    Trial = 4,
    Sentencing = 5,
    Appeal = 6,
    Enforcement = 7
}

/// <summary>High-level bucket for choosing trial mode and agency behavior.</summary>
public enum LegalCaseCategory
{
    CriminalFelony = 0,
    CriminalMisdemeanor = 1,
    TaxAdministrative = 2,
    TaxCriminal = 3,
    MunicipalInfraction = 4
}

/// <summary>Jury of peers vs judge alone — Capone-era courts mixed both.</summary>
public enum TrialMode
{
    Jury = 0,
    Bench = 1
}

/// <summary>
/// Resolves trial mode from category + rough severity (0 light … 3 severe).
/// </summary>
public static class LegalProcedureRules
{
    public static TrialMode ResolveTrialMode(LegalCaseCategory category, int severity0To3)
    {
        severity0To3 = Mathf.Clamp(severity0To3, 0, 3);

        switch (category)
        {
            case LegalCaseCategory.TaxAdministrative:
            case LegalCaseCategory.MunicipalInfraction:
                return TrialMode.Bench;

            case LegalCaseCategory.TaxCriminal:
                // Willful evasion can be dramatic; use jury for the worst tier.
                return severity0To3 >= 2 ? TrialMode.Jury : TrialMode.Bench;

            case LegalCaseCategory.CriminalMisdemeanor:
                return TrialMode.Bench;

            case LegalCaseCategory.CriminalFelony:
            default:
                if (severity0To3 >= 2)
                    return TrialMode.Jury;
                return TrialMode.Bench;
        }
    }

    public static string GetTrialModeLabelEn(TrialMode mode)
    {
        return mode == TrialMode.Jury ? "Jury trial" : "Bench trial (judge alone)";
    }

    public static string DescribeCriminalPipelineEn()
    {
        return
            "<b>Criminal pipeline (typical)</b>\n" +
            "1) Intelligence / street rumor → 2) Investigation & warrants → 3) Charging / indictment → " +
            "4) Bail or custody → 5) Trial → 6) Sentence → 7) Appeal (optional) → 8) Prison / fines / probation.\n\n" +
            "<b>Tax pipeline (Al Capone pattern)</b>\n" +
            "1) Books & estimates → 2) Assessment & interest → 3) Liens / freezes / seizures → " +
            "4) Administrative hearing → 5) If willful — criminal referral → jury or bench per severity.\n\n" +
            "<size=90%><i>Agencies:</i> local police handle street crime; federal agents pick up interstate and " +
            "major conspiracy; revenue officers run the tax line even when violence never sticks.</size>";
    }
}
