using FamilyBusiness.CityGen.Data;

namespace FamilyBusiness.CityGen.Discovery
{
    /// <summary>Placeholder strings for fog-compliant UI (Batch 9). Not localization-ready.</summary>
    public static class DiscoveryDisplayText
    {
        public const string UnknownBuilding = "Unknown Building";
        public const string UnknownInstitution = "Unidentified Facility";
        public const string RumoredInstitution = "Rumored Official Presence";
        public const string UnknownDistrict = "Uncharted Area";
        public const string DistrictDetailsLimited = "Known District — details limited";

        public static string GetDistrictDisplayName(DistrictData d)
        {
            if (d == null)
                return UnknownDistrict;
            if (d.Discovery == null || d.Discovery.State == DiscoveryState.Unknown)
                return UnknownDistrict;
            if (!DiscoveryVisibility.CanShowDistrictDetailedStats(d))
                return string.IsNullOrEmpty(d.Name) ? DistrictDetailsLimited : $"{d.Name} (limited)";
            return string.IsNullOrEmpty(d.Name) ? "District" : d.Name;
        }

        public static string GetInstitutionDisplayName(InstitutionData i)
        {
            if (i == null)
                return UnknownInstitution;
            if (i.Discovery == null)
                return i.DisplayName ?? UnknownInstitution;
            if (i.Discovery.State == DiscoveryState.Unknown)
                return UnknownInstitution;
            if (DiscoveryVisibility.ShouldAppearAsRumorOnly(i))
                return RumoredInstitution;
            if (!i.Discovery.InstitutionKindKnown)
                return UnknownInstitution;
            return string.IsNullOrEmpty(i.DisplayName) ? i.Kind.ToString() : i.DisplayName;
        }

        public static string GetBuildingFrontLabel(BuildingData b)
        {
            if (b == null)
                return UnknownBuilding;
            if (b.Discovery == null)
                return string.IsNullOrEmpty(b.FrontBusinessType) ? UnknownBuilding : b.FrontBusinessType;
            if (!DiscoveryVisibility.CanShowExactBuildingType(b))
                return UnknownBuilding;
            return string.IsNullOrEmpty(b.FrontBusinessType) ? UnknownBuilding : b.FrontBusinessType;
        }

        public static string PlaceholderBuildingCategory() => "Unidentified";
    }
}
