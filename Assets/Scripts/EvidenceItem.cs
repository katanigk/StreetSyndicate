using System;

[Serializable]
public enum EvidenceType
{
    Forensic = 0,          // fingerprints, shoeprints, basic ballistics, fibers/blood (no DNA)
    Physical = 1,          // weapon, clothing, tools, marked bills
    Testimony = 2,         // witness statement, affidavit, confession
    Circumstantial = 3,    // pattern, presence, informant lead
    DocumentsRecords = 4   // station report, receipts, bank slips, ledgers, letters, landline call logs
}

[Serializable]
public enum EvidenceDirectness
{
    Indirect = 0,
    Direct = 1
}

[Serializable]
public enum EvidenceChainOfCustody
{
    Clean = 0,
    Questionable = 1,
    Broken = 2
}

[Serializable]
public class EvidenceItem
{
    public EvidenceType Type;
    public EvidenceDirectness Directness;
    public EvidenceChainOfCustody Chain = EvidenceChainOfCustody.Clean;

    /// <summary>0..100: objective strength before legal attacks.</summary>
    public int Strength;

    /// <summary>0..100: chance to be excluded / weakened in court.</summary>
    public int AdmissibilityRisk;

    public string Summary = "";
    public int CollectedAtDay = -1;

    public static EvidenceItem Create(EvidenceType type, int strength, int admissibilityRisk, string summary, int day,
        EvidenceDirectness directness = EvidenceDirectness.Indirect,
        EvidenceChainOfCustody chain = EvidenceChainOfCustody.Clean)
    {
        return new EvidenceItem
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

