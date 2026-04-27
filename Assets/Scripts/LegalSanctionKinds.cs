/// <summary>
/// Sanctions the legal system can apply (substantive outcomes + enforcement).
/// Used for future UI and automation; not every sanction is wired to gameplay yet.
/// </summary>
public enum LegalSanctionKind
{
    None = 0,
    Fine = 1,
    Restitution = 2,
    AssetFreeze = 3,
    SeizureForfeiture = 4,
    LicenseRevocationOrSuspension = 5,
    TravelRestriction = 6,
    PretrialDetention = 7,
    Imprisonment = 8,
    ProbationOrParole = 9,
    CriminalRecordEntry = 10
}
