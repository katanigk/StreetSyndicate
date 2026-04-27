using System.Text;

/// <summary>
/// Textual City Charter for the 1920s–30s fictional setting: rights + when they yield (public safety, lawful tax, etc.).
/// Shown as its own page inside <see cref="LegalCodex"/>.
/// </summary>
public static class CityConstitutionalCharter
{
    public const string PageTitleEn = "City Charter — fundamental rights";

    public readonly struct FundamentalRight
    {
        public readonly string Id;
        public readonly string TitleEn;
        public readonly string BodyEn;
        /// <summary>When this right may be limited by statute or emergency (in-world).</summary>
        public readonly string LimitationsEn;

        public FundamentalRight(string id, string titleEn, string bodyEn, string limitationsEn)
        {
            Id = id;
            TitleEn = titleEn;
            BodyEn = bodyEn;
            LimitationsEn = limitationsEn;
        }
    }

    public static readonly FundamentalRight[] Rights =
    {
        new FundamentalRight(
            "CR-1",
            "Equality before the law",
            "No person shall be denied the equal protection of the laws on account of creed, origin, or station. " +
            "In practice, influence and money bend outcomes — but the text remains the public standard.",
            "Narrow occupational classifications and tax brackets are allowed if rationally tied to revenue or safety."),

        new FundamentalRight(
            "CR-2",
            "Liberty of movement",
            "Citizens may travel within the city and between districts freely, subject to lawful regulation.",
            "Curfews, quarantines, cordons, and lawful arrest supersede movement when declared with process."),

        new FundamentalRight(
            "CR-3",
            "Due process and fair hearing",
            "No deprivation of liberty or property without notice and a hearing before a competent tribunal, " +
            "save as provided for summary offenses and lawful tax collection.",
            "Tax assessments and certain administrative seizures may proceed on statutory timelines; criminal trials keep fuller process."),

        new FundamentalRight(
            "CR-4",
            "Security of papers and effects",
            "Warrants are required for searches of homes and private papers except where exigent circumstances are recorded.",
            "Revenue agents may demand business books under statute; resistance escalates to court orders and penalties."),

        new FundamentalRight(
            "CR-5",
            "Property and forfeiture",
            "Property shall not be taken for public use without just compensation; criminal forfeiture requires adjudication.",
            "Contraband, proceeds of crime, and assets tied to unpaid tax may be seized under chapters TAX/FIN after process."),

        new FundamentalRight(
            "CR-6",
            "Speech and the press",
            "Speech is free; incitement to immediate violence and corrupt solicitation may be punished.",
            "Newspapers that print libel or racket ads still get sued — and sometimes visited."),

        new FundamentalRight(
            "CR-7",
            "Bear arms (regulated)",
            "The right to keep arms is recognized for lawful defense and sport, within licensing and peace-officer rules.",
            "Concealed carry, machine guns, and felon possession are restricted by city and federal chapters.")
    };

    public static string BuildFullPageEn()
    {
        var sb = new StringBuilder(4_000);
        sb.AppendLine("<b><size=120%>The City Charter (excerpts)</size></b>");
        sb.AppendLine("<size=90%><i>Adopted in the early 20th century; supreme over ordinary statutes unless a " +
                      "narrow, explicit exception is authorized.</i></size>");
        sb.AppendLine();
        sb.AppendLine(LawHierarchy.DescribePrecedenceEn());
        sb.AppendLine();
        for (int i = 0; i < Rights.Length; i++)
        {
            FundamentalRight r = Rights[i];
            sb.AppendLine("<b>" + r.Id + " — " + r.TitleEn + "</b>");
            sb.AppendLine(r.BodyEn);
            sb.AppendLine("<size=90%><color=#3A2B1F>Limitations:</color> " + r.LimitationsEn + "</size>");
            sb.AppendLine();
        }

        sb.AppendLine("<size=90%><i>Note:</i> This is an in-world document. Courts, juries, and fixers interpret the gaps.</size>");
        return sb.ToString();
    }
}
