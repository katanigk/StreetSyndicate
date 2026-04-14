using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// In-world legal codex: chapters + sections with penalty ranges (min-max).
/// Designed to be readable by the player and drive legal/judicial systems.
/// </summary>
public static class LegalCodex
{
    [Serializable]
    public readonly struct PenaltyRange
    {
        public readonly int FineMinUsd;
        public readonly int FineMaxUsd;
        public readonly int PrisonMinWeeks;
        public readonly int PrisonMaxWeeks;
        public readonly bool ForfeiturePossible;
        public readonly bool LicenseSanctionsPossible;

        public PenaltyRange(
            int fineMinUsd,
            int fineMaxUsd,
            int prisonMinWeeks,
            int prisonMaxWeeks,
            bool forfeiturePossible = false,
            bool licenseSanctionsPossible = false)
        {
            FineMinUsd = Math.Max(0, fineMinUsd);
            FineMaxUsd = Math.Max(FineMinUsd, fineMaxUsd);
            PrisonMinWeeks = Math.Max(0, prisonMinWeeks);
            PrisonMaxWeeks = Math.Max(PrisonMinWeeks, prisonMaxWeeks);
            ForfeiturePossible = forfeiturePossible;
            LicenseSanctionsPossible = licenseSanctionsPossible;
        }

        public string ToDisplayString()
        {
            StringBuilder sb = new StringBuilder();
            if (FineMaxUsd > 0)
            {
                sb.Append("Fine: $");
                sb.Append(FineMinUsd.ToString("N0"));
                sb.Append("–$");
                sb.Append(FineMaxUsd.ToString("N0"));
            }
            else
                sb.Append("Fine: —");

            sb.Append("  |  ");

            if (PrisonMaxWeeks > 0)
            {
                sb.Append("Prison: ");
                sb.Append(PrisonMinWeeks);
                sb.Append("–");
                sb.Append(PrisonMaxWeeks);
                sb.Append(" weeks");
            }
            else
                sb.Append("Prison: —");

            if (ForfeiturePossible)
                sb.Append("  |  Forfeiture: possible");
            if (LicenseSanctionsPossible)
                sb.Append("  |  License sanctions: possible");
            return sb.ToString();
        }
    }

    [Serializable]
    public sealed class Section
    {
        public string Id;
        public string TitleEn;
        public string SummaryEn;
        public PenaltyRange Penalty;

        /// <summary>
        /// Whether this section is treated as "case backbone" (dropping it collapses the case).
        /// This supports the gameplay rule: primary charge dropped => case dismissed.
        /// </summary>
        public bool BackboneCharge;
    }

    [Serializable]
    public sealed class Chapter
    {
        public string Id;
        public string TitleEn;
        public readonly List<Section> Sections = new List<Section>();
    }

    public static readonly List<Chapter> Chapters = BuildDefaultChapters();

    public static int PageCount => 1 + Chapters.Count; // 0 = TOC, 1..N = chapters

    public static string GetPageTitleEn(int pageIndex)
    {
        if (pageIndex <= 0)
            return "Legal codex — table of contents";
        int c = pageIndex - 1;
        if (c < 0 || c >= Chapters.Count)
            return "Legal codex";
        return "Chapter " + (c + 1) + " — " + Chapters[c].TitleEn;
    }

    public static string BuildCodexPageBodyEn(int pageIndex)
    {
        if (pageIndex <= 0)
            return BuildTableOfContentsIntroEn();
        int c = pageIndex - 1;
        if (c < 0 || c >= Chapters.Count)
            return "<i>Page not found.</i>";

        Chapter ch = Chapters[c];
        StringBuilder sb = new StringBuilder(2_000);
        sb.AppendLine("<b><size=120%>" + ch.TitleEn + "</size></b>");
        sb.AppendLine("<size=90%><i>Penalties are ranges (min–max).</i></size>");
        sb.AppendLine();
        for (int i = 0; i < ch.Sections.Count; i++)
        {
            Section sec = ch.Sections[i];
            sb.AppendLine("<b>" + sec.Id + " — " + sec.TitleEn + "</b>" +
                          (sec.BackboneCharge ? "  <color=#E8C96A>(BACKBONE)</color>" : ""));
            sb.AppendLine(sec.SummaryEn);
            sb.AppendLine("<size=90%><color=#3A2B1F>Penalty range:</color> " + sec.Penalty.ToDisplayString() + "</size>");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    public static string BuildTableOfContentsIntroEn()
    {
        return
            "<b>Legal codex — table of contents</b>\n" +
            "<size=90%><i>An in-world law book. Penalties are ranges (min–max) so judges and lawyers have room to work.</i></size>\n\n" +
            "Select a chapter below to jump to the right page.\n\n" +
            "<size=90%><i>Gameplay rules:</i>\n" +
            "• Primary charge dropped -> case dismissed\n" +
            "• Bonus charge dropped -> sentence mitigation</size>";
    }

    public static List<(string label, int pageIndex)> BuildTocJumpTargetsEn()
    {
        List<(string, int)> list = new List<(string, int)>();
        for (int c = 0; c < Chapters.Count; c++)
        {
            list.Add(("Chapter " + (c + 1) + ": " + Chapters[c].TitleEn, c + 1));
        }
        return list;
    }

    public static string BuildTableOfContentsEn()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<b>Legal codex — table of contents</b>");
        sb.AppendLine("<size=90%><i>An in-world law book. Penalties are ranges (min–max) so judges and lawyers have room to work.</i></size>");
        sb.AppendLine();

        for (int c = 0; c < Chapters.Count; c++)
        {
            Chapter ch = Chapters[c];
            sb.AppendLine("<b>Chapter " + (c + 1) + ":</b> " + ch.TitleEn);
            for (int s = 0; s < ch.Sections.Count; s++)
            {
                Section sec = ch.Sections[s];
                sb.AppendLine("  • " + sec.Id + " — " + sec.TitleEn);
            }
            sb.AppendLine();
        }

        sb.AppendLine("<size=90%><i>Tip:</i> dropping a backbone section collapses the case. Dropping bonus sections reduces sentencing.</size>");
        return sb.ToString();
    }

    public static string BuildCodexBookEn()
    {
        StringBuilder sb = new StringBuilder(8_000);
        sb.AppendLine("<b>The City's Legal Codex</b>");
        sb.AppendLine("<size=90%><i>Chapters contain sections and penalty ranges (min–max). Ranges exist to prevent automatic sentencing and to create legal play.</i></size>");
        sb.AppendLine();

        for (int c = 0; c < Chapters.Count; c++)
        {
            Chapter ch = Chapters[c];
            sb.AppendLine("<b><size=120%>Chapter " + (c + 1) + " — " + ch.TitleEn + "</size></b>");
            sb.AppendLine();

            for (int i = 0; i < ch.Sections.Count; i++)
            {
                Section sec = ch.Sections[i];
                sb.AppendLine("<b>" + sec.Id + " — " + sec.TitleEn + "</b>" + (sec.BackboneCharge ? "  <color=#E8C96A>(BACKBONE)</color>" : ""));
                sb.AppendLine(sec.SummaryEn);
                sb.AppendLine("<size=90%><color=#3A2B1F>Penalty range:</color> " + sec.Penalty.ToDisplayString() + "</size>");
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    public static string BuildPenaltySummaryEn()
    {
        int total = 0;
        int backbone = 0;
        int maxFine = 0;
        int maxWeeks = 0;
        for (int c = 0; c < Chapters.Count; c++)
        {
            Chapter ch = Chapters[c];
            for (int i = 0; i < ch.Sections.Count; i++)
            {
                Section s = ch.Sections[i];
                total++;
                if (s.BackboneCharge) backbone++;
                maxFine = Math.Max(maxFine, s.Penalty.FineMaxUsd);
                maxWeeks = Math.Max(maxWeeks, s.Penalty.PrisonMaxWeeks);
            }
        }

        return
            "<b>Codex summary</b>\n" +
            "• Sections: " + total + "\n" +
            "• Backbone sections: " + backbone + "\n" +
            "• Max fine in codex: $" + maxFine.ToString("N0") + "\n" +
            "• Max prison in codex: " + maxWeeks + " weeks\n\n" +
            "<size=90%><i>Note:</i> This is an in-world codex. Ranges are meant for judging, deals, and appeals.</size>";
    }

    /// <summary>
    /// Minimal mapping from arrest causes to codex section IDs (initial).
    /// Expand over time.
    /// </summary>
    public static string GetCodexSectionIdForCause(ArrestCause cause)
    {
        return cause switch
        {
            ArrestCause.Assault => "BOD-1",
            ArrestCause.ArmedThreats => "WPN-2",
            ArrestCause.WeaponsPossession => "WPN-1",
            ArrestCause.AttemptedMurder => "BOD-2",
            ArrestCause.Homicide => "BOD-3",
            ArrestCause.NarcoticsPossession => "VIC-1",
            ArrestCause.NarcoticsTrafficking => "VIC-2",
            ArrestCause.ContrabandSmuggling => "VIC-3",
            ArrestCause.Extortion => "ORG-1",
            ArrestCause.ProtectionRacket => "ORG-2",
            ArrestCause.Kidnapping => "BOD-4",
            ArrestCause.Robbery => "PRP-2",
            ArrestCause.Burglary => "PRP-1",
            ArrestCause.Arson => "PRP-3",
            ArrestCause.RacketeeringConspiracy => "ORG-0",
            ArrestCause.Bribery => "COR-1",
            ArrestCause.MoneyLaundering => "FIN-2",
            ArrestCause.Fraud => "FIN-1",
            ArrestCause.TaxEvasion => "TAX-1",
            ArrestCause.Obstruction => "JUS-1",
            ArrestCause.WitnessTampering => "JUS-2",
            ArrestCause.EvidenceTampering => "JUS-3",
            ArrestCause.OutstandingWarrant => "ADM-2",
            ArrestCause.ProbationViolation => "ADM-3",
            _ => "—"
        };
    }

    private static List<Chapter> BuildDefaultChapters()
    {
        // Numbers are intentionally rough ranges for early gameplay tuning.
        List<Chapter> ch = new List<Chapter>();

        Chapter admin = new Chapter { Id = "ADM", TitleEn = "Administrative / municipal offenses" };
        admin.Sections.Add(new Section
        {
            Id = "ADM-1",
            TitleEn = "Licensing, safety, and sanitation violations",
            SummaryEn = "Operating a business without proper licensing, failing safety requirements, or violating municipal orders. Usually resolved via fines and license sanctions, but can escalate when public safety is involved.",
            Penalty = new PenaltyRange(500, 15_000, 0, 6, forfeiturePossible: false, licenseSanctionsPossible: true),
            BackboneCharge = false
        });
        admin.Sections.Add(new Section
        {
            Id = "ADM-2",
            TitleEn = "Outstanding warrant / failure to appear",
            SummaryEn = "Failure to appear for questioning/court or an outstanding warrant. A technical custody basis that keeps the system active until status is resolved.",
            Penalty = new PenaltyRange(0, 5_000, 0, 8, forfeiturePossible: false),
            BackboneCharge = false
        });
        admin.Sections.Add(new Section
        {
            Id = "ADM-3",
            TitleEn = "Probation / parole / release conditions violation",
            SummaryEn = "Violating conditions set by a prior proceeding (check-ins, weapon restrictions, travel limits, attendance). Usually leads to short custody and stricter conditions.",
            Penalty = new PenaltyRange(0, 10_000, 2, 18, forfeiturePossible: false),
            BackboneCharge = false
        });
        ch.Add(admin);

        Chapter property = new Chapter { Id = "PRP", TitleEn = "Property offenses" };
        property.Sections.Add(new Section
        {
            Id = "PRP-1",
            TitleEn = "Burglary",
            SummaryEn = "Entering property without permission to steal or cause damage. Severity increases with tools, planning, or public safety impact.",
            Penalty = new PenaltyRange(1_000, 40_000, 4, 26, forfeiturePossible: true),
            BackboneCharge = false
        });
        property.Sections.Add(new Section
        {
            Id = "PRP-2",
            TitleEn = "Robbery",
            SummaryEn = "Taking property through threat, coercion, or violence. Sits between property and bodily harm, so ranges are higher.",
            Penalty = new PenaltyRange(2_000, 80_000, 12, 60, forfeiturePossible: true),
            BackboneCharge = false
        });
        property.Sections.Add(new Section
        {
            Id = "PRP-3",
            TitleEn = "Arson",
            SummaryEn = "Damaging property through fire. Escalates strongly when human life or infrastructure is endangered.",
            Penalty = new PenaltyRange(2_000, 120_000, 10, 80, forfeiturePossible: true),
            BackboneCharge = false
        });
        ch.Add(property);

        Chapter bodily = new Chapter { Id = "BOD", TitleEn = "Bodily offenses" };
        bodily.Sections.Add(new Section
        {
            Id = "BOD-1",
            TitleEn = "Assault",
            SummaryEn = "Harm to a person's body. Severity depends on injury, weapon use, and context (self-defense, retaliation, intimidation).",
            Penalty = new PenaltyRange(0, 30_000, 6, 52, forfeiturePossible: false),
            BackboneCharge = false
        });
        bodily.Sections.Add(new Section
        {
            Id = "BOD-2",
            TitleEn = "Attempted murder",
            SummaryEn = "An intentional act meant to cause death that did not complete. A heavy charge with long prison exposure and poor deals without strong counsel.",
            Penalty = new PenaltyRange(0, 150_000, 52, 260, forfeiturePossible: false),
            BackboneCharge = false
        });
        bodily.Sections.Add(new Section
        {
            Id = "BOD-3",
            TitleEn = "Homicide / homicide investigation",
            SummaryEn = "A lethal case. Even at the investigation stage, the system converges around it. Lawyers, deals, and appeals become critical.",
            Penalty = new PenaltyRange(0, 250_000, 104, 520, forfeiturePossible: true),
            BackboneCharge = false
        });
        bodily.Sections.Add(new Section
        {
            Id = "BOD-4",
            TitleEn = "Kidnapping",
            SummaryEn = "Unlawfully restraining a person. Treated as severe, especially with threats, harm, or ransom.",
            Penalty = new PenaltyRange(0, 180_000, 52, 312, forfeiturePossible: true),
            BackboneCharge = false
        });
        ch.Add(bodily);

        Chapter tax = new Chapter { Id = "TAX", TitleEn = "Tax offenses" };
        tax.Sections.Add(new Section
        {
            Id = "TAX-1",
            TitleEn = "Tax evasion",
            SummaryEn = "Non-reporting, false reporting, or hiding income. Usually starts with heavy fines but can lead to prison when systematic, willful, or large-scale.",
            Penalty = new PenaltyRange(10_000, 400_000, 0, 78, forfeiturePossible: true),
            BackboneCharge = false
        });
        ch.Add(tax);

        Chapter finance = new Chapter { Id = "FIN", TitleEn = "Financial offenses & corruption" };
        finance.Sections.Add(new Section
        {
            Id = "FIN-1",
            TitleEn = "Fraud",
            SummaryEn = "Misrepresentation to gain profit. Penalties combine restitution/fines with prison depending on scope and pattern.",
            Penalty = new PenaltyRange(2_000, 250_000, 0, 104, forfeiturePossible: true),
            BackboneCharge = false
        });
        finance.Sections.Add(new Section
        {
            Id = "FIN-2",
            TitleEn = "Money laundering",
            SummaryEn = "Concealing the origin of money/assets. Often comes with forfeiture exposure and deep audits. Lawyers and accountants become strategic assets.",
            Penalty = new PenaltyRange(5_000, 600_000, 12, 156, forfeiturePossible: true),
            BackboneCharge = false
        });
        finance.Sections.Add(new Section
        {
            Id = "COR-1",
            TitleEn = "Bribery / corruption",
            SummaryEn = "Give-and-take with the system. Powerful when it works, devastating when exposed: it burns connections and escalates consequences.",
            Penalty = new PenaltyRange(5_000, 200_000, 4, 78, forfeiturePossible: true),
            BackboneCharge = false
        });
        ch.Add(finance);

        Chapter justice = new Chapter { Id = "JUS", TitleEn = "Justice system interference" };
        justice.Sections.Add(new Section
        {
            Id = "JUS-1",
            TitleEn = "Obstruction of justice",
            SummaryEn = "Interfering with investigation/enforcement through concealment, pressure, or intermediaries. Often appears as a bonus charge that worsens the case.",
            Penalty = new PenaltyRange(0, 120_000, 4, 60, forfeiturePossible: false),
            BackboneCharge = false
        });
        justice.Sections.Add(new Section
        {
            Id = "JUS-2",
            TitleEn = "Witness tampering",
            SummaryEn = "Pressuring, bribing, or threatening a witness. A high-value charge to break for sentencing reduction.",
            Penalty = new PenaltyRange(0, 160_000, 8, 78, forfeiturePossible: false),
            BackboneCharge = false
        });
        justice.Sections.Add(new Section
        {
            Id = "JUS-3",
            TitleEn = "Evidence tampering",
            SummaryEn = "Breaking chain of custody, destroying/hiding evidence, or forgery. Common as a bonus charge that raises exposure, so strong counsel will target it.",
            Penalty = new PenaltyRange(0, 140_000, 6, 72, forfeiturePossible: false),
            BackboneCharge = false
        });
        ch.Add(justice);

        Chapter weapons = new Chapter { Id = "WPN", TitleEn = "Weapons offenses" };
        weapons.Sections.Add(new Section
        {
            Id = "WPN-1",
            TitleEn = "Illegal weapons possession",
            SummaryEn = "Possession/carry without license or in violation of conditions. Often added as a bonus after a search/seizure.",
            Penalty = new PenaltyRange(0, 90_000, 4, 52, forfeiturePossible: true),
            BackboneCharge = false
        });
        weapons.Sections.Add(new Section
        {
            Id = "WPN-2",
            TitleEn = "Armed threats / intimidation",
            SummaryEn = "Using a displayed weapon or threats to force an outcome. Escalates exposure even without shots fired.",
            Penalty = new PenaltyRange(0, 120_000, 8, 78, forfeiturePossible: true),
            BackboneCharge = false
        });
        ch.Add(weapons);

        Chapter vice = new Chapter { Id = "VIC", TitleEn = "Narcotics & contraband" };
        vice.Sections.Add(new Section
        {
            Id = "VIC-1",
            TitleEn = "Narcotics possession",
            SummaryEn = "Possession of an illegal substance. Usually a fine + short custody; escalates quickly with quantity or intent to distribute.",
            Penalty = new PenaltyRange(500, 60_000, 0, 26, forfeiturePossible: true),
            BackboneCharge = false
        });
        vice.Sections.Add(new Section
        {
            Id = "VIC-2",
            TitleEn = "Narcotics trafficking",
            SummaryEn = "Distribution/trafficking. Systemically increases scrutiny and escalation pressure.",
            Penalty = new PenaltyRange(2_000, 220_000, 12, 130, forfeiturePossible: true),
            BackboneCharge = false
        });
        vice.Sections.Add(new Section
        {
            Id = "VIC-3",
            TitleEn = "Contraband smuggling",
            SummaryEn = "Smuggling through docks/warehouses/routes. Commonly involves seizures and forfeiture, sometimes full-chain investigations.",
            Penalty = new PenaltyRange(2_000, 300_000, 8, 104, forfeiturePossible: true),
            BackboneCharge = false
        });
        ch.Add(vice);

        Chapter org = new Chapter { Id = "ORG", TitleEn = "Organized crime" };
        org.Sections.Add(new Section
        {
            Id = "ORG-0",
            TitleEn = "Racketeering / conspiracy (umbrella)",
            SummaryEn = "An umbrella section tying events, networks, and command. If it falls, the entire case tends to collapse — a typical backbone section.",
            Penalty = new PenaltyRange(0, 500_000, 26, 260, forfeiturePossible: true),
            BackboneCharge = true
        });
        org.Sections.Add(new Section
        {
            Id = "ORG-1",
            TitleEn = "Extortion",
            SummaryEn = "Demanding/collecting through coercion or threats. Often paired with armed intimidation, which escalates exposure.",
            Penalty = new PenaltyRange(0, 180_000, 8, 104, forfeiturePossible: true),
            BackboneCharge = false
        });
        org.Sections.Add(new Section
        {
            Id = "ORG-2",
            TitleEn = "Protection racket",
            SummaryEn = "Systematic collection from businesses. Exposure scales with scope, violence, and network, and often stacks with the umbrella section.",
            Penalty = new PenaltyRange(0, 220_000, 6, 130, forfeiturePossible: true),
            BackboneCharge = false
        });
        ch.Add(org);

        return ch;
    }
}

