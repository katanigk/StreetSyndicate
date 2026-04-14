using FamilyBusiness.CityGen.Data;

namespace FamilyBusiness.CityGen.Government
{
    /// <summary>
    /// Batch 12: maps anchor <see cref="InstitutionKind"/> to government UI systems.
    /// Bank, rail, and dock institutions are intentionally excluded from the government bar scope.
    /// </summary>
    public static class InstitutionGovernmentMapping
    {
        /// <summary>
        /// True when this institution should appear in extracted government facility registries.
        /// </summary>
        public static bool TryGetGovernmentSystem(InstitutionKind kind, out GovernmentSystemKind system)
        {
            switch (kind)
            {
                case InstitutionKind.PoliceStation:
                    system = GovernmentSystemKind.Police;
                    return true;
                case InstitutionKind.FederalOffice:
                    system = GovernmentSystemKind.Federal;
                    return true;
                case InstitutionKind.Courthouse:
                    system = GovernmentSystemKind.Court;
                    return true;
                case InstitutionKind.Prison:
                    system = GovernmentSystemKind.Prison;
                    return true;
                case InstitutionKind.CityHall:
                    system = GovernmentSystemKind.CityHall;
                    return true;
                case InstitutionKind.Hospital:
                    system = GovernmentSystemKind.Hospital;
                    return true;
                case InstitutionKind.TaxOffice:
                    system = GovernmentSystemKind.Tax;
                    return true;
                default:
                    system = GovernmentSystemKind.Unknown;
                    return false;
            }
        }

        public static bool IsExcludedFromGovernmentBar(InstitutionKind kind) =>
            !TryGetGovernmentSystem(kind, out _);
    }
}
