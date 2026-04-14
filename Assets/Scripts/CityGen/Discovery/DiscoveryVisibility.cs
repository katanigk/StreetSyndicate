using FamilyBusiness.CityGen.Data;

namespace FamilyBusiness.CityGen.Discovery
{
    /// <summary>Read-only exposure rules for UI / simulation (Batch 9). Does not mutate city truth.</summary>
    public static class DiscoveryVisibility
    {
        public static bool IsVisibleOnMap(DistrictData d) => IsDistrictVisibleOnMap(d);

        public static bool IsDistrictVisibleOnMap(DistrictData d)
        {
            if (d?.Discovery == null)
                return true;
            if (d.Discovery.MapVisibilityOverride.HasValue)
                return d.Discovery.MapVisibilityOverride.Value;
            return DiscoveryStateOrdering.IsAtLeast(d.Discovery.State,
                d.Discovery.Discoverability?.MinStateVisibleOnMap ?? DiscoveryState.Rumored);
        }

        public static bool CanShowDistrictDetailedStats(DistrictData d) =>
            d?.Discovery != null && d.Discovery.DetailedMetadataUnlocked;

        public static bool IsVisibleOnMap(InstitutionData i) => ShouldShowInstitutionOnMap(i);

        public static bool ShouldShowInstitutionOnMap(InstitutionData i)
        {
            if (i?.Discovery == null)
                return true;
            if (!DiscoveryStateOrdering.IsAtLeast(i.Discovery.State,
                    i.Discovery.Discoverability?.MinStateVisibleOnMap ?? DiscoveryState.Rumored))
                return false;
            return i.Discovery.ExactLocationKnown;
        }

        public static bool CanShowInstitutionLabel(InstitutionData i) =>
            i?.Discovery != null && DiscoveryStateOrdering.IsAtLeast(i.Discovery.State, DiscoveryState.Rumored);

        public static bool CanShowExactInstitutionKind(InstitutionData i) =>
            i?.Discovery != null && i.Discovery.InstitutionKindKnown &&
            DiscoveryStateOrdering.IsAtLeast(i.Discovery.State,
                i.Discovery.Discoverability?.MinStateForExactTypeOrKind ?? DiscoveryState.Known);

        public static bool CanShowInstitutionDetailedInfo(InstitutionData i) =>
            i?.Discovery != null && i.Discovery.InternalDetailsKnown &&
            DiscoveryStateOrdering.IsAtLeast(i.Discovery.State,
                i.Discovery.Discoverability?.MinStateForInstitutionInternals ?? DiscoveryState.PartiallyExposed);

        public static bool IsVisibleOnMap(BuildingData b) => ShouldShowBuildingMarker(b);

        public static bool ShouldShowBuildingMarker(BuildingData b)
        {
            if (b?.Discovery == null)
                return true;
            return DiscoveryStateOrdering.IsAtLeast(b.Discovery.State,
                b.Discovery.Discoverability?.MinStateVisibleOnMap ?? DiscoveryState.Rumored);
        }

        public static bool CanShowBuildingLabel(BuildingData b) => ShouldShowBuildingMarker(b);

        public static bool CanShowExactBuildingType(BuildingData b) =>
            b?.Discovery != null && b.Discovery.FrontBusinessTypeKnown && b.Discovery.BuildingCategoryKnown &&
            DiscoveryStateOrdering.IsAtLeast(b.Discovery.State,
                b.Discovery.Discoverability?.MinStateForExactTypeOrKind ?? DiscoveryState.Known);

        public static bool CanShowCrimeProfile(BuildingData b) =>
            b?.Discovery != null && b.Discovery.CrimeProfileKnown && b.Crime != null &&
            DiscoveryStateOrdering.IsAtLeast(b.Discovery.State,
                b.Discovery.Discoverability?.MinStateForCrimeProfile ?? DiscoveryState.PartiallyExposed);

        public static bool ShouldAppearAsPlaceholder(BuildingData b) =>
            b?.Discovery != null &&
            DiscoveryStateOrdering.IsAtLeast(b.Discovery.State, DiscoveryState.Rumored) &&
            !CanShowExactBuildingType(b);

        public static bool ShouldAppearAsRumorOnly(InstitutionData i) =>
            i?.Discovery != null &&
            DiscoveryStateOrdering.IsAtLeast(i.Discovery.State, DiscoveryState.Rumored) &&
            !i.Discovery.ExactLocationKnown;

        public static bool CanShowDetailedInfo(DistrictData d) => CanShowDistrictDetailedStats(d);

        public static bool CanShowDetailedInfo(InstitutionData i) => CanShowInstitutionDetailedInfo(i);

        public static bool CanShowDetailedInfo(BuildingData b) =>
            b?.Discovery != null &&
            DiscoveryStateOrdering.IsAtLeast(b.Discovery.State, DiscoveryState.PartiallyExposed);
    }
}
