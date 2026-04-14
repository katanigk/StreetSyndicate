using System;
using System.Collections.Generic;
using System.Text;

[Serializable]
public enum ArrestCause
{
    Unknown = 0,

    // Street / violence
    Assault = 10,
    ArmedThreats = 11,
    WeaponsPossession = 12,
    AttemptedMurder = 13,
    Homicide = 14,

    // Vice / contraband
    NarcoticsPossession = 20,
    NarcoticsTrafficking = 21,
    ContrabandSmuggling = 22,

    // Organized crime / rackets
    Extortion = 30,
    ProtectionRacket = 31,
    Kidnapping = 32,
    Robbery = 33,
    Burglary = 34,
    Arson = 35,
    RacketeeringConspiracy = 36,

    // Corruption / financial
    Bribery = 40,
    MoneyLaundering = 41,
    Fraud = 42,
    TaxEvasion = 43,

    // Justice system interference
    Obstruction = 50,
    WitnessTampering = 51,
    EvidenceTampering = 52,

    // Administrative
    OutstandingWarrant = 60,
    ProbationViolation = 61
}

public static class ArrestCauseUtility
{
    public static string ToDisplayLabel(ArrestCause cause)
    {
        switch (cause)
        {
            case ArrestCause.Assault: return "Assault / violent incident";
            case ArrestCause.ArmedThreats: return "Armed threats / intimidation";
            case ArrestCause.WeaponsPossession: return "Illegal weapons possession";
            case ArrestCause.AttemptedMurder: return "Attempted murder";
            case ArrestCause.Homicide: return "Homicide investigation";

            case ArrestCause.NarcoticsPossession: return "Narcotics possession";
            case ArrestCause.NarcoticsTrafficking: return "Narcotics trafficking";
            case ArrestCause.ContrabandSmuggling: return "Contraband smuggling";

            case ArrestCause.Extortion: return "Extortion";
            case ArrestCause.ProtectionRacket: return "Protection racket";
            case ArrestCause.Kidnapping: return "Kidnapping";
            case ArrestCause.Robbery: return "Robbery";
            case ArrestCause.Burglary: return "Burglary";
            case ArrestCause.Arson: return "Arson";
            case ArrestCause.RacketeeringConspiracy: return "Racketeering / conspiracy";

            case ArrestCause.Bribery: return "Bribery / corruption";
            case ArrestCause.MoneyLaundering: return "Money laundering";
            case ArrestCause.Fraud: return "Fraud";
            case ArrestCause.TaxEvasion: return "Tax evasion";

            case ArrestCause.Obstruction: return "Obstruction of justice";
            case ArrestCause.WitnessTampering: return "Witness tampering";
            case ArrestCause.EvidenceTampering: return "Evidence tampering";

            case ArrestCause.OutstandingWarrant: return "Outstanding warrant";
            case ArrestCause.ProbationViolation: return "Probation / parole violation";
        }
        return "Unknown cause";
    }

    public static string FormatArrestReasonLine(ArrestRecord record)
    {
        if (record == null)
            return string.Empty;

        if (record.IsCaseCollapsed())
            return "Arrest case: dismissed (primary charge dropped)";

        ArrestCause primary = record.GetEffectivePrimary();
        if (primary == ArrestCause.Unknown)
            return "Arrest reason: Unknown";

        StringBuilder sb = new StringBuilder();
        sb.Append("Arrest reason: ");
        sb.Append(ToDisplayLabel(primary));

        List<ArrestCause> bonus = record.BonusCauses;
        if (bonus != null && bonus.Count > 0)
        {
            bool first = true;
            for (int i = 0; i < bonus.Count; i++)
            {
                ArrestCause b = bonus[i];
                if (b == ArrestCause.Unknown || b == primary)
                    continue;
                if (record.DroppedBonusCauses != null && record.DroppedBonusCauses.Contains(b))
                    continue;
                sb.Append(first ? " (+ " : ", ");
                sb.Append(ToDisplayLabel(b));
                first = false;
            }
            if (!first)
                sb.Append(")");
        }

        // Mitigation note (dropped bonus charges reduce sentence severity).
        if (record.DroppedBonusCauses != null && record.DroppedBonusCauses.Count > 0)
        {
            int activeDropped = 0;
            for (int i = 0; i < record.DroppedBonusCauses.Count; i++)
            {
                ArrestCause d = record.DroppedBonusCauses[i];
                if (d == ArrestCause.Unknown || d == primary)
                    continue;
                if (bonus != null && bonus.Contains(d))
                    activeDropped++;
            }
            if (activeDropped > 0)
                sb.Append(" (mitigation: +" + activeDropped + " dropped bonus)"); // player-facing hint
        }

        return sb.ToString();
    }
}

