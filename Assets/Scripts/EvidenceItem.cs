using System;

/// <summary>Arrest / pre-trial evidence entry (simple). Distinct from <see cref="EvidenceItem"/> in <see cref="PoliceEvidenceSystem"/> (case evidence system).</summary>
[Serializable]
public enum ArrestEvidenceType
{
    Forensic = 0,
    Physical = 1,
    Testimony = 2,
    Circumstantial = 3,
    DocumentsRecords = 4
}

[Serializable]
public enum ArrestEvidenceDirectness
{
    Indirect = 0,
    Direct = 1
}

[Serializable]
public enum ArrestEvidenceChainState
{
    Clean = 0,
    Questionable = 1,
    Broken = 2
}

[Serializable]
public class ArrestEvidenceItem
{
    public ArrestEvidenceType Type;
    public ArrestEvidenceDirectness Directness;
    public ArrestEvidenceChainState Chain = ArrestEvidenceChainState.Clean;

    /// <summary>0..100: objective strength before legal attacks.</summary>
    public int Strength;

    /// <summary>0..100: chance to be excluded / weakened in court.</summary>
    public int AdmissibilityRisk;

    public string Summary = "";
    public int CollectedAtDay = -1;

    public static ArrestEvidenceItem Create(ArrestEvidenceType type, int strength, int admissibilityRisk, string summary, int day,
        ArrestEvidenceDirectness directness = ArrestEvidenceDirectness.Indirect,
        ArrestEvidenceChainState chain = ArrestEvidenceChainState.Clean)
    {
        return new ArrestEvidenceItem
        {
            Type = type,
            Strength = Math.Clamp(strength, 0, 100),
            AdmissibilityRisk = Math.Clamp(admissibilityRisk, 0, 100),
            Summary = summary ?? "",
            CollectedAtDay = day,
            Directness = directness,
            Chain = chain
        };
    }
}

