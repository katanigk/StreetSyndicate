using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// In-world legal codex: chapters + sections with penalty ranges (min-max).
/// Designed to be readable by the player and drive legal/judicial systems.
/// </summary>
public static class LegalCodex
{
    /// <summary>Domain tag for tooling / future UI filters (substantive chapters only).</summary>
    public enum LegalDomain
    {
        Unspecified = 0,
        PolicePowersProcedure = 10,
        EvidenceProcedure = 11,
        AdministrativeMunicipal = 1,
        Property = 2,
        Bodily = 3,
        Tax = 4,
        FinanceCorruption = 5,
        JusticeInterference = 6,
        Weapons = 7,
        ViceContraband = 8,
        OrganizedCrime = 9,
        /// <summary>National Security Act — Central Serious Crime Unit (federal “Bureau” authority layer).</summary>
        NationalSecurityCentralUnit = 12
    }

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
        public LegalDomain Domain = LegalDomain.Unspecified;
        public readonly List<Section> Sections = new List<Section>();
    }

    public static readonly List<Chapter> Chapters = BuildDefaultChapters();

    /// <summary>0 = TOC, 1 = City Charter, 2.. = substantive chapters.</summary>
    public static int PageCount => 2 + Chapters.Count;

    public static string GetPageTitleEn(int pageIndex)
    {
        if (pageIndex <= 0)
            return "Legal codex — table of contents";
        if (pageIndex == 1)
            return CityConstitutionalCharter.PageTitleEn;
        int c = pageIndex - 2;
        if (c < 0 || c >= Chapters.Count)
            return "Legal codex";
        return "Chapter " + (c + 1) + " — " + Chapters[c].TitleEn;
    }

    public static string BuildCodexPageBodyEn(int pageIndex)
    {
        if (pageIndex <= 0)
            return BuildTableOfContentsIntroEn();
        if (pageIndex == 1)
            return CityConstitutionalCharter.BuildFullPageEn();
        int c = pageIndex - 2;
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

    /// <summary>
    /// Splits one logical codex page into left/right facing pages for an open-book UI.
    /// Page 0 keeps chapter buttons in the UI layer; right column gets a short header only.
    /// </summary>
    public static void BuildCodexPageSpreadEn(int pageIndex, out string leftPageRichText, out string rightPageRichText)
    {
        if (pageIndex <= 0)
        {
            leftPageRichText = BuildTocLeftFacingPageEn();
            rightPageRichText = BuildTocRightFacingPageEn();
            return;
        }

        string full = BuildCodexPageBodyEn(pageIndex);
        SplitRichTextIntoSpread(full, out leftPageRichText, out rightPageRichText);
    }

    private static string BuildTocLeftFacingPageEn()
    {
        return
            "<b>Legal codex</b>\n" +
            "<size=92%><i>In-world rules for police powers, evidence law, criminal charges, and sentencing bands (1920s–30s style).</i></size>\n\n" +
            "<b>City Charter</b> is <b>page 2</b> in this book — fundamental rights sit above ordinary statutes unless a narrow exception is written into law.\n\n" +
            "<size=92%>Use <b>Prev / Next</b> under the pages to turn leaves. <b>Esc</b> closes the codex; click outside the spread to dismiss. " +
            "To close from the desk, press the <b>closed codex book button</b> in the Legal sidebar again (the small cover icon — not the open pages). " +
            "On the facing page, tap a chapter line to jump.</size>";
    }

    private static string BuildTocRightFacingPageEn()
    {
        return "<b>Chapters</b>\n<size=88%><i>Tap a line in the list below to open that chapter.</i></size>";
    }

    /// <summary>Splits on blank-line paragraphs first; otherwise splits near half length on a newline.</summary>
    private static void SplitRichTextIntoSpread(string full, out string left, out string right)
    {
        left = string.Empty;
        right = string.Empty;
        if (string.IsNullOrEmpty(full))
            return;

        string trimmed = full.Trim();
        string[] paras = trimmed.Split(new[] { "\n\n" }, StringSplitOptions.None);
        if (paras.Length >= 2)
        {
            int target = trimmed.Length / 2;
            int acc = 0;
            int splitAfter = 0;
            for (int i = 0; i < paras.Length; i++)
            {
                acc += paras[i].Length;
                if (i < paras.Length - 1)
                    acc += 2;
                if (acc >= target)
                {
                    splitAfter = i + 1;
                    break;
                }
            }

            splitAfter = Math.Max(1, Math.Min(splitAfter, paras.Length - 1));
            left = string.Join("\n\n", paras, 0, splitAfter).Trim();
            right = string.Join("\n\n", paras, splitAfter, paras.Length - splitAfter).Trim();
            return;
        }

        int mid = trimmed.Length / 2;
        int cut = trimmed.LastIndexOf('\n', mid);
        if (cut < mid / 2)
            cut = mid;
        left = trimmed.Substring(0, cut).TrimEnd();
        right = trimmed.Substring(cut).TrimStart();
    }

    public static string BuildTableOfContentsIntroEn()
    {
        return
            "<b>Legal codex — table of contents</b>\n" +
            "<size=90%><i>An in-world law book. Penalties are ranges (min–max) so judges and lawyers have room to work.</i></size>\n\n" +
            "<b>Page 1:</b> City Charter — fundamental rights (supreme over ordinary statutes when they conflict).\n\n" +
            "<b>Structure:</b> Police Powers Code -> Law of Evidence -> National Security Act (federal unit) -> Criminal Code.\n\n" +
            "Select a chapter below to jump to the right page.\n\n" +
            "<size=90%><i>Gameplay rules:</i>\n" +
            "• Primary charge dropped -> case dismissed\n" +
            "• Bonus charge dropped -> sentence mitigation</size>";
    }

    public static List<(string label, int pageIndex)> BuildTocJumpTargetsEn()
    {
        List<(string, int)> list = new List<(string, int)>();
        list.Add(("City Charter — fundamental rights", 1));
        for (int c = 0; c < Chapters.Count; c++)
        {
            list.Add(("Chapter " + (c + 1) + ": " + Chapters[c].TitleEn, c + 2));
        }
        return list;
    }

    public static string BuildTableOfContentsEn()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<b>Legal codex — table of contents</b>");
        sb.AppendLine("<size=90%><i>An in-world law book. Penalties are ranges (min–max) so judges and lawyers have room to work.</i></size>");
        sb.AppendLine();
        sb.AppendLine("<b>City Charter:</b> fundamental rights & hierarchy");
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
        sb.AppendLine(CityConstitutionalCharter.BuildFullPageEn());
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

        Chapter police = new Chapter { Id = "POL", TitleEn = "Police Powers Code (authority, triggers, approvals)", Domain = LegalDomain.PolicePowersProcedure };
        police.Sections.Add(new Section
        {
            Id = "POL-1",
            TitleEn = "Legality baseline for police action",
            SummaryEn = "Police action is lawful only when legal ground, authorizing power, and required documentation are all present. Missing any pillar does not auto-erase the event, but downgrades legal weight and increases internal review exposure.",
            Penalty = new PenaltyRange(0, 25_000, 0, 52, forfeiturePossible: false, licenseSanctionsPossible: true),
            BackboneCharge = false
        });
        police.Sections.Add(new Section
        {
            Id = "POL-2",
            TitleEn = "Permitted escalation ladder",
            SummaryEn = "Contact, identification request, temporary detention, frisk, deeper search, and arrest must follow proportional escalation. High-intrusion actions require stronger grounds and elevated approval thresholds.",
            Penalty = new PenaltyRange(0, 30_000, 0, 78, forfeiturePossible: false, licenseSanctionsPossible: true),
            BackboneCharge = false
        });
        police.Sections.Add(new Section
        {
            Id = "POL-3",
            TitleEn = "Emergency exception and mandatory review",
            SummaryEn = "Emergency can temporarily bypass parts of normal approval flow only where delay creates immediate danger. Every emergency claim requires immediate justification note and post-action supervisor validation.",
            Penalty = new PenaltyRange(0, 40_000, 2, 104, forfeiturePossible: false, licenseSanctionsPossible: true),
            BackboneCharge = false
        });
        police.Sections.Add(new Section
        {
            Id = "POL-4",
            TitleEn = "Use of force limits",
            SummaryEn = "Force is lawful only when tied to a legal objective, necessary under conditions, and proportionate to threat. Lethal force is restricted to immediate threat-to-life scenarios and always triggers full review.",
            Penalty = new PenaltyRange(0, 75_000, 8, 208, forfeiturePossible: false, licenseSanctionsPossible: true),
            BackboneCharge = false
        });
        ch.Add(police);

        Chapter evidenceLaw = new Chapter { Id = "EVD", TitleEn = "Law of Evidence (admissibility, chain, integrity)", Domain = LegalDomain.EvidenceProcedure };
        evidenceLaw.Sections.Add(new Section
        {
            Id = "EVD-1",
            TitleEn = "Evidence admissibility classes",
            SummaryEn = "Evidence is classified as lawful, questionable, unlawful, or unknown based on acquisition path, authority, and scope compliance. Admissibility weight changes accordingly in case strength and courtroom challenge.",
            Penalty = new PenaltyRange(0, 20_000, 0, 52, forfeiturePossible: false, licenseSanctionsPossible: true),
            BackboneCharge = false
        });
        evidenceLaw.Sections.Add(new Section
        {
            Id = "EVD-2",
            TitleEn = "Chain of custody obligations",
            SummaryEn = "Significant evidence must be tracked from discovery through transfer, storage, and presentation. Broken or compromised chain materially increases fragility and can collapse prosecution leverage.",
            Penalty = new PenaltyRange(0, 30_000, 0, 78, forfeiturePossible: false, licenseSanctionsPossible: true),
            BackboneCharge = false
        });
        evidenceLaw.Sections.Add(new Section
        {
            Id = "EVD-3",
            TitleEn = "Contamination, tampering, planting, concealment",
            SummaryEn = "Physical/documentary contamination, planted evidence, concealed evidence, or integrity manipulation are treated as severe procedural breaches and may trigger internal affairs, disciplinary, and criminal exposure.",
            Penalty = new PenaltyRange(0, 120_000, 12, 260, forfeiturePossible: false, licenseSanctionsPossible: true),
            BackboneCharge = true
        });
        ch.Add(evidenceLaw);
        AppendNationalSecurityActChapters(ch);

        Chapter admin = new Chapter { Id = "ADM", TitleEn = "Criminal Code — administrative / municipal offenses", Domain = LegalDomain.AdministrativeMunicipal };
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

        Chapter property = new Chapter { Id = "PRP", TitleEn = "Criminal Code — property offenses", Domain = LegalDomain.Property };
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

        Chapter bodily = new Chapter { Id = "BOD", TitleEn = "Criminal Code — bodily offenses", Domain = LegalDomain.Bodily };
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

        Chapter tax = new Chapter { Id = "TAX", TitleEn = "Criminal Code — tax offenses", Domain = LegalDomain.Tax };
        tax.Sections.Add(new Section
        {
            Id = "TAX-1",
            TitleEn = "Tax evasion",
            SummaryEn = "Non-reporting, false reporting, or hiding income. Usually starts with heavy fines but can lead to prison when systematic, willful, or large-scale.",
            Penalty = new PenaltyRange(10_000, 400_000, 0, 78, forfeiturePossible: true),
            BackboneCharge = false
        });
        ch.Add(tax);

        Chapter finance = new Chapter { Id = "FIN", TitleEn = "Criminal Code — financial offenses & corruption", Domain = LegalDomain.FinanceCorruption };
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

        Chapter justice = new Chapter { Id = "JUS", TitleEn = "Criminal Code — justice system interference", Domain = LegalDomain.JusticeInterference };
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

        Chapter weapons = new Chapter { Id = "WPN", TitleEn = "Criminal Code — weapons offenses", Domain = LegalDomain.Weapons };
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

        Chapter vice = new Chapter { Id = "VIC", TitleEn = "Criminal Code — narcotics & contraband", Domain = LegalDomain.ViceContraband };
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

        Chapter org = new Chapter { Id = "ORG", TitleEn = "Criminal Code — organized crime", Domain = LegalDomain.OrganizedCrime };
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

    private static void AppendNationalSecurityActChapters(List<Chapter> ch)
    {
        PenaltyRange n = new PenaltyRange(0, 0, 0, 0);
        PenaltyRange serious = new PenaltyRange(0, 500_000, 52, 520, forfeiturePossible: true);

        Chapter ns1 = new Chapter
        {
            Id = "NSA",
            TitleEn = "National Security Act — I. General, establishment, definitions",
            Domain = LegalDomain.NationalSecurityCentralUnit
        };
        ns1.Sections.Add(new Section
        {
            Id = "NSA-1",
            TitleEn = "Short title, purpose, and who is bound",
            SummaryEn = "This Act may be cited as the National Security Act (Central Unit for Serious Crime). It establishes, empowers, and limits a federal body — publicly known as the Bureau — to protect the state and the city from serious crime, organized crime, Prohibition-scale alcohol enforcement, intercity smuggling, contraband, systemic corruption, and any criminal activity of a scale that local police are not designed to handle alone. The Act binds the Director, deputy directors, operational agents, support staff, and registered unit facilities, including classified sites.",
            Penalty = n,
            BackboneCharge = false
        });
        ns1.Sections.Add(new Section
        {
            Id = "NSA-2",
            TitleEn = "The Unit and the Director",
            SummaryEn = "The Unit is the central serious-crime body commonly called the Bureau. The Director of the Central Unit is the top federal official of the Unit, a rank comparable to the national chief of local police, reporting directly to the National Security Minister (not to the local police chief).",
            Penalty = n,
            BackboneCharge = false
        });
        ns1.Sections.Add(new Section
        {
            Id = "NSA-3",
            TitleEn = "Sworn field agents and federal authority",
            SummaryEn = "A sworn agent of the Unit is a field employee with statutory federal authority under this Act, including the powers described later in surface work, precinct access, and file transfer. Bureau **rank titles** (Field Agent, Senior Field Agent, Special Agent, Supervising Special Agent, and above) are **not** local police “detective” or precinct ranks; they are a separate federal line.",
            Penalty = n,
            BackboneCharge = false
        });
        ns1.Sections.Add(new Section
        {
            Id = "NSA-4",
            TitleEn = "Undercover agent (concealment, not open identity)",
            SummaryEn = "An “undercover agent” means a federal agent, Unit employee, or human source directed by the Unit who conducts infiltration, collection, disruption, or dismantling while operating under a fictional, concealed, or otherwise **non-open** cover. Field operators who are openly and continuously visible as law-enforcement in their true role are not treated as “undercover” for this title.",
            Penalty = n,
            BackboneCharge = false
        });
        ns1.Sections.Add(new Section
        {
            Id = "NSA-5",
            TitleEn = "Serious crimes, organized crime, and “crime family” in-game",
            SummaryEn = "Serious crimes include: organized patterns; Prohibition and contraband (alcohol, drugs, controlled substances) at material scale; large smuggling; systemic corruption; coordinated economic crime. For in-game law, a criminal **organization that reaches game tier 4+** is treated as a “crime family” (exact tier table is a separate design chart). Dry-law and contraband volumes below statutory thresholds are generally not federal targets unless they tie to organized crime.",
            Penalty = n,
            BackboneCharge = false
        });
        ns1.Sections.Add(new Section
        {
            Id = "NSA-6",
            TitleEn = "Prohibited substances and materials (including dry law)",
            SummaryEn = "“Prohibited material” means alcohol and derivatives under the dry law, and listed narcotics/controlled drugs and any other substance classed as prohibited or emergency-banned. Federal powers attach when conduct reaches federal thresholds or organized channels described elsewhere in this Act.",
            Penalty = n,
            BackboneCharge = false
        });
        ns1.Sections.Add(new Section
        {
            Id = "NSA-7",
            TitleEn = "Unit facilities, classified sites, and registered protocol",
            SummaryEn = "A “Unit facility” includes offices, storage, interview rooms, safe-houses, meeting points, and operational flats assigned to the Unit. A “classified facility” is a facility whose address, function, or existence is not generally visible to the public, local police, or outsiders without authorization. Facilities funded from the official budget must be recorded; operating unregistered facilities, or long-term detention/interrogation without a classified log when required, is a severe deviation path.",
            Penalty = n,
            BackboneCharge = false
        });
        ns1.Sections.Add(new Section
        {
            Id = "NSA-8",
            TitleEn = "“Authorized act” and exposure of a covert operator",
            SummaryEn = "An “authorized act” is one pre-approved as this Act requires, or justified in the field to protect cover, prevent the immediate exposure of an undercover operator, or prevent an immediate life-safety risk. “Exposure of an operator” is any disclosure of identity, role, assignment, or operations that can endanger an undercover agent, compromise a case, or burn classified work.",
            Penalty = n,
            BackboneCharge = false
        });
        ns1.Sections.Add(new Section
        {
            Id = "NSA-9",
            TitleEn = "Establishment, subordination, and appointment of the Director",
            SummaryEn = "The Central Unit is a federal, independent law-enforcement body for serious crime. The Unit is subordinate to the Director, who is subordinate to the National Security Minister. The local police chief and the Director are not in each other’s command chain, but the Director is appointed and may be removed by the Minister, commonly on a Governor’s recommendation.",
            Penalty = n,
            BackboneCharge = false
        });
        ns1.Sections.Add(new Section
        {
            Id = "NSA-10",
            TitleEn = "Supremacy, savings clause, and public authorities",
            SummaryEn = "Where the Unit is lawfully engaged, this Act’s federal rule layer prevails over the local police code on the same subject-matter, without abolishing the local police’s ordinary municipal powers. Public bodies, including the local police, must assist the Unit within its federal mandate.",
            Penalty = n,
            BackboneCharge = false
        });
        ch.Add(ns1);

        Chapter ns2 = new Chapter
        {
            Id = "NSB",
            TitleEn = "National Security Act — II. Jurisdiction, field command, local police",
            Domain = LegalDomain.NationalSecurityCentralUnit
        };
        ns2.Sections.Add(new Section
        {
            Id = "NSB-1",
            TitleEn = "Nationwide and city-wide federal jurisdiction",
            SummaryEn = "The Unit may operate in all state territory, including the city, when federal jurisdiction exists under the serious-crime, residual, and special assignments described in the Act. The Unit is not a petty-crime service unless a petty event ties to a serious, organized, or contraband case.",
            Penalty = n,
            BackboneCharge = false
        });
        ns2.Sections.Add(new Section
        {
            Id = "NSB-2",
            TitleEn = "Residual and “taken-over” jurisdiction (local failure or avoidance)",
            SummaryEn = "The Act authorizes the Unit to act where local police: cannot handle effectively; decline to handle; are suspected of corrupt interference; or have repeatedly failed, making federal assumption appropriate.",
            Penalty = n,
            BackboneCharge = false
        });
        ns2.Sections.Add(new Section
        {
            Id = "NSB-3",
            TitleEn = "Command at the scene when both Unit and local police are present",
            SummaryEn = "If a federal agent and local officers are at the same scene, the agent is the incident commander **while the event is, or plausibly may be, within Unit authority**. Local command remains for purely local events unless a lawful federal takeover (case transfer, joint tasking, or superior orders) is activated.",
            Penalty = n,
            BackboneCharge = false
        });
        ns2.Sections.Add(new Section
        {
            Id = "NSB-4",
            TitleEn = "Duty to assist, precinct entry, and document access",
            SummaryEn = "Local police must assist federal surface operations, including by producing files, evidence, and intelligence, and by granting access to a precinct. An agent may review any police file, case material, and evidence, except for officers’ private personnel files unrelated to the file or act under review.",
            Penalty = n,
            BackboneCharge = false
        });
        ns2.Sections.Add(new Section
        {
            Id = "NSB-5",
            TitleEn = "Station access log and police resistance channels",
            SummaryEn = "Every time an agent reviews or removes a police file or evidence, the precinct must log agent code/name, time, material class, and whether the item was taken, copied, or flagged. Non-cooperation is a serious breach, but a police commander may file a post-hoc command or political objection through proper channels. Delivery of the required materials to the federal layer must still occur.",
            Penalty = n,
            BackboneCharge = false
        });
        ns2.Sections.Add(new Section
        {
            Id = "NSB-6",
            TitleEn = "Arrest, seizure, and extended forfeiture gates",
            SummaryEn = "Agents may arrest on sufficient federal cause (organized patterns, Prohibition or contraband, smuggling, endangering a covert program, public safety, obstruction of a federal op, or warrant/federal order). The Unit may seize dry-law, narcotics, instruments of production, transport, documents, cash, weapons, and any property with a real link to a serious case. High-impact or long-duration forfeiture requires an authorized command officer or written order, as the approval chapter describes.",
            Penalty = n,
            BackboneCharge = false
        });
        ns2.Sections.Add(new Section
        {
            Id = "NSB-7",
            TitleEn = "Federal search in furtherance of Unit work (replaces the old “warrant bypass for serious crime” draft)",
            SummaryEn = "A **search that is in furtherance of the work of the Unit’s agents** may be conducted on approval by an **authorized unit officer** as defined in the Ranks and Approvals chapter. Deeper, sensitive invasions of privacy (such as a deep, sustained home search or deep wiretap) still require the warrant/authorization triggers listed separately — this section does not erase the warrant list for the highest-sensitivity class of intrusions.",
            Penalty = n,
            BackboneCharge = false
        });
        ns2.Sections.Add(new Section
        {
            Id = "NSB-8",
            TitleEn = "Warrant/approval still required: deep privacy, politics, and sensitive class",
            SummaryEn = "A warrant (or a legally equivalent high-authorization national instrument) is still required for: deep, warrant-grade entry into a private home without a true emergency; deep and sustained eavesdropping; operations targeting senior political, judicial, prosecutorial, or public figures; very broad seizure of plausibly lawful property; and any operation the Director marks as an ultra-sensitive class. Conflicts are resolved in favor of this list when the invasion type matches its description, even if the subject matter is a serious case.",
            Penalty = n,
            BackboneCharge = false
        });
        ns2.Sections.Add(new Section
        {
            Id = "NSB-9",
            TitleEn = "Case and command transfer from the local police",
            SummaryEn = "The Unit may take a file from the local police when the matter is serious, organized, Prohibition, contraband, corruption, or otherwise federal. The police must supply the full file tree, reports, evidence lists, intel, suspect lists, and related documents. A police command may only resist through political/judicial escalation **after the fact** — the transfer channel itself must be honored.",
            Penalty = n,
            BackboneCharge = false
        });
        ns2.Sections.Add(new Section
        {
            Id = "NSB-10",
            TitleEn = "Speakeasy, dry-law thresholds, and “crime family” operations",
            SummaryEn = "A speakeasy or equivalent venue is a legal federal target when it systematically sells or serves dry-law alcohol. Statutory de minimis **non-engagement** thresholds (example: below five liquid liters or half a kilogram solid per week) apply unless the conduct ties to org crime, at which case lower weight still counts. A strategic strike against a crime family normally requires a Director (or lawfully delegated) line as elsewhere provided.",
            Penalty = n,
            BackboneCharge = false
        });
        ch.Add(ns2);

        Chapter ns3 = new Chapter
        {
            Id = "NSC",
            TitleEn = "National Security Act — III. Intel, cover, use of force, money, records, review",
            Domain = LegalDomain.NationalSecurityCentralUnit
        };
        ns3.Sections.Add(new Section
        {
            Id = "NSC-1",
            TitleEn = "Intelligence, sources, deep recruitment, and undercover entry",
            SummaryEn = "The Unit may lawfully run intelligence, sources, co-opted players, and double-game agents, and may recruit or insert operators into a criminal org, business, or public body if needed for a serious file. A deep cover entry to an organization is approved per the federal approval ladder, not a Field Agent on their own say-so.",
            Penalty = n,
            BackboneCharge = false
        });
        ns3.Sections.Add(new Section
        {
            Id = "NSC-2",
            TitleEn = "Undercover / covered-operation criminal immunity (and limits)",
            SummaryEn = "A state operator of the Unit may receive immunity only for in-role conduct that was pre-authorized, necessary to keep cover, unavoidable to prevent exposure, or necessary to protect life, subject to a clean paper-trail. This immunity does not automatically apply to criminal sources, operated offenders, or external collaborators; their acts remain their own legal exposure unless the Unit chooses policy discretion in handling.",
            Penalty = n,
            BackboneCharge = false
        });
        ns3.Sections.Add(new Section
        {
            Id = "NSC-2A",
            TitleEn = "Security threat removal (judicially authorized extreme measure)",
            SummaryEn = "Security threat removal is an exceptional, last-resort action against a real national-security threat where no reasonable alternative remains. It is legally distinct from field self-defense lethal force. It requires: active federal case, strong evidence base, written Director approval, and a final judicial order from the Head of Judiciary defining the approved scope.",
            Penalty = n,
            BackboneCharge = false
        });
        ns3.Sections.Add(new Section
        {
            Id = "NSC-2B",
            TitleEn = "Extraction of operated source from external custody",
            SummaryEn = "The Unit may order transfer of an operated source from police, prison, or other state custody through a sealed command order. Transfer does not grant blanket immunity for prior offenses. Receiving authority must keep transfer and source relationship confidential unless judicially ordered otherwise.",
            Penalty = n,
            BackboneCharge = false
        });
        ns3.Sections.Add(new Section
        {
            Id = "NSC-3",
            TitleEn = "Exposing a Unit undercover operator — serious federal crime",
            SummaryEn = "Revealing a federal undercover program or operator in a way that endangers life, burns an operation, or discloses a classified line is a grave offense. In-world sentencing is severe and may be enhanced if physical harm results.",
            Penalty = serious,
            BackboneCharge = true
        });
        ns3.Sections.Add(new Section
        {
            Id = "NSC-4",
            TitleEn = "Use of force, lethal rules, and classified use-of-force packet",
            SummaryEn = "Force is permitted only to execute a lawful act, to protect a lawful cover, to protect life, or to prevent a covert burn that creates a safety catastrophe. Lethal force is limited to an immediate, concrete threat to life, an imminent lethal exposure of a cover, or a catastrophic break of a high-value federal operation, not as punishment, deterrence, or trial avoidance.",
            Penalty = n,
            BackboneCharge = false
        });
        ns3.Sections.Add(new Section
        {
            Id = "NSC-5",
            TitleEn = "Official budget, confidential ops fund, and “black money” ban",
            SummaryEn = "A formal federal budget, plus a **classified operational fund** taken from the legal budget, are the lawful money lanes. “Black money” — unregistered street cash in federal hands or spent like an official line — is **not lawful Unit money** for any officer: no one has statutory authority to bless black money, even if leadership turns a blind eye. That choice is a deviation, not a recognized exception.",
            Penalty = n,
            BackboneCharge = false
        });
        ns3.Sections.Add(new Section
        {
            Id = "NSC-6",
            TitleEn = "Records and three-tier logging (open / classified / internal-op log)",
            SummaryEn = "Every act must be logged at one of three levels: public/open record, classified file, or internal-operations log. The identity of covert agents, true facility addresses, and sensitive tradecraft are classified. A missing log entry is, by default, a serious deviation, unless a narrow emergency proof shows immediate registration would have burned a live operator.",
            Penalty = n,
            BackboneCharge = false
        });
        ns3.Sections.Add(new Section
        {
            Id = "NSC-7",
            TitleEn = "Oversight — external review talks to the Director, not street agents",
            SummaryEn = "Complaint and review bodies (Governor, City Attorney, the Minister, appointed boards, and similar) work **with the Director and the Unit’s command system**, not with field agents, UC identities, and street sources, in order to **not expose** operators in an open, parallel track. The Director (or a written delegate) is the public-law interface for the Unit’s own command responsibilities in a review, while the street layer stays shielded to protect covers.",
            Penalty = n,
            BackboneCharge = false
        });
        ns3.Sections.Add(new Section
        {
            Id = "NSC-8",
            TitleEn = "Deviations (class system) and personal vs command liability",
            SummaryEn = "A light, medium, or serious deviation (missed log, unapproved op, unregistered site, off-books money, cover burn, false evidence, hiding a subject, or obstructing a review) triggers investigation. An operator is personally answerable; a command officer who pre-approved, knew, winked, or buried the paper shares command liability, potentially more severe in corruption or cover-up.",
            Penalty = n,
            BackboneCharge = false
        });
        ch.Add(ns3);

        Chapter ns4 = new Chapter
        {
            Id = "NSD",
            TitleEn = "National Security Act — IV. Ranks, authorized officers, and approvals (federal table)",
            Domain = LegalDomain.NationalSecurityCentralUnit
        };
        ns4.Sections.Add(new Section
        {
            Id = "NSD-1",
            TitleEn = "The seven federal ranks of the Central Unit (summary)",
            SummaryEn =
                "The Unit is not a precinct ladder. Ranks, with English designations used in federal documents: (1) Field Agent — field worker, no approval authority. (2) Senior Field Agent — leads small field slices, not an approval officer. (3) Special Agent — core case officer; on scene, takes command with local police when the event is within Unit remit; may read precinct material; not a default approval authority for the heaviest moves. (4) Supervising Special Agent / field-cell lead — the **lowest true “authorized officer”** in the default rule-set. (5) Unit Chief — sub-unit chief; **senior command** for a vertical; may approve deep, multi-cell moves inside a portfolio with limits. (6) Deputy Director — four portfolios: Operations, Intelligence, Budget & Facilities, Political & Legal. (7) Director — unit leader; parallel in stature to the national local-police chief; Minister line; city-wide, nation-wide, strategic authority.",
            Penalty = n,
            BackboneCharge = false
        });
        ns4.Sections.Add(new Section
        {
            Id = "NSD-2",
            TitleEn = "“Authorized officer” and the written, time-bounded down-delegation",
            SummaryEn = "An “authorized officer” is **Supervising Special Agent or above**, or a **lower** agent who has an **express, written, time-limited** federal mission order to approve a **specific** power or class of act. Without that, Field and Senior Field Agents execute but do not approve. Special Agents are not, by default, the statutory approval class — they are the on-scene federal face, not the warrant-style approval hub.",
            Penalty = n,
            BackboneCharge = false
        });
        ns4.Sections.Add(new Section
        {
            Id = "NSD-3",
            TitleEn = "“Senior command officer” (Unit Chief+)",
            SummaryEn = "A “senior command officer” (Unit Chief, Deputy Director, or Director) is the class for deep institutional moves: new budget line, new registered facility, city-wide and political-risk moves, and the largest operations against a crime family, subject to portfolio split between deputies and a capstone line for the Director on national-strategic actions.",
            Penalty = n,
            BackboneCharge = false
        });
        ns4.Sections.Add(new Section
        {
            Id = "NSD-4",
            TitleEn = "Deputy Director, Budget, Facilities, and Logistics — the only class budget gate",
            SummaryEn = "The **Deputy Director for Budget, Facilities & Logistics** is, by this Act, the class that may approve **major** budget, classified-fund, large asset, inter-unit moves, and opening **new** registered sites or a **new** budget line. A Unit Chief may only spend a **line already** assigned to the Unit chief’s desk and may **request** an uplift — a Unit Chief is **not** free to unilaterally open a new federal wallet line without that deputy. Black money is never a lawful class under any title.",
            Penalty = n,
            BackboneCharge = false
        });
        ns4.Sections.Add(new Section
        {
            Id = "NSD-5",
            TitleEn = "Deputy — Operations, Intelligence, Political & Legal (short remit)",
            SummaryEn = "The Operations deputy holds city-wide, strike-force, high-risk surface authority; the Intelligence deputy governs long-running, highly sensitive, double-agent, and inter-agency “keep-away” law; the Political & Legal deputy manages Minister/Governor/City Attorney, press, court packets, and controlled disclosure of a classified line — still without dragging street UC into an open, parallel, face-to-face review in place of the Director’s channel unless a narrow statute forces it.",
            Penalty = n,
            BackboneCharge = false
        });
        ns4.Sections.Add(new Section
        {
            Id = "NSD-6",
            TitleEn = "Approval map (federal) — the practical ladder your engine should encode",
            SummaryEn =
                "Short / light surveillance and hands-on work: field grades execute; Special Agent runs the scene. Long surveillance, a standing source, a registered safe-house **use** (not first creation), a standard federal **search in furtherance of work**, a pre-planned arrest, and a contained seizure: **at least a Supervising Special Agent** (or written mission order). A **new** deep source inside a **crime family**, a deep UCO, a wide raid, a **file pull** from a police vault when politically hot, a **strategic** anti–crime-family op, a **new** big budget, a **new** registered facility, or a large cross-unit move: **Unit Chief and/or a Deputy (by portfolio)**, and **Director** for a full strategic war-level line in the highest band. A court- or politics-sensitive hit on a judge, a senior cop, a politician, or a general officer class is **Deputy Political & Legal +, at times, Director**.",
            Penalty = n,
            BackboneCharge = false
        });
        ns4.Sections.Add(new Section
        {
            Id = "NSD-7",
            TitleEn = "Unity with the rest of the city code",
            SummaryEn = "Ranks in this title are not local-police constable ranks, but the federal line may still appear in a courtroom, in a file header, and in a political fight over who signed a line. Game code should use FederalBureauRank and FederalDeputyPortfolio (see FederalBureauStructure) in resolvers, and cross-check the local police layer only for joint scenes and file-transfer rules — not a merge of the two rank ladders.",
            Penalty = n,
            BackboneCharge = false
        });
        ch.Add(ns4);
    }
}

