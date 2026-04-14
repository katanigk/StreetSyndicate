using System.Collections.Generic;
using FamilyBusiness.CityGen.Data;
using FamilyBusiness.CityGen.Discovery;

namespace FamilyBusiness.CityGen.Government.Windows
{
    /// <summary>Batch 13: shared mapping from <see cref="GovernmentFacilityData"/> to window rows (no extra simulation).</summary>
    internal static class GovernmentWindowBuilderShared
    {
        internal const string InstitutionIdPrefix = "inst:";

        internal static DistrictData FindDistrict(CityData city, int districtId)
        {
            if (city == null || districtId < 0)
                return null;
            foreach (DistrictData d in city.Districts)
            {
                if (d.Id == districtId)
                    return d;
            }

            return null;
        }

        internal static string StableInstitutionId(int institutionId) => InstitutionIdPrefix + institutionId;

        internal static bool TryParseInstitutionStableId(string stableId, out int institutionId)
        {
            institutionId = -1;
            if (string.IsNullOrEmpty(stableId) || !stableId.StartsWith(InstitutionIdPrefix))
                return false;
            return int.TryParse(stableId.Substring(InstitutionIdPrefix.Length), out institutionId);
        }

        internal static GovernmentWindowListDiscoveryTier TierForFacility(GovernmentFacilityData f)
        {
            if (f.IsVisibleOnMapNow)
                return GovernmentWindowListDiscoveryTier.MapVisible;
            if (f.ShouldAppearAsRumorOnlyNow)
                return GovernmentWindowListDiscoveryTier.RumoredIntel;
            if (DiscoveryStateOrdering.IsAtLeast(f.DiscoveryStateSnapshot, DiscoveryState.Known))
                return GovernmentWindowListDiscoveryTier.KnownWithoutMapPin;
            return GovernmentWindowListDiscoveryTier.RumoredIntel;
        }

        internal static string DistrictLineForFacility(CityData city, GovernmentFacilityData f)
        {
            DistrictData d = FindDistrict(city, f.DistrictId);
            return d != null ? DiscoveryDisplayText.GetDistrictDisplayName(d) : DiscoveryDisplayText.UnknownDistrict;
        }

        internal static GovernmentWindowListItemModel ToFacilityListItem(CityData city, GovernmentFacilityData f,
            GovernmentSystemKind system, bool selected)
        {
            return new GovernmentWindowListItemModel
            {
                StableId = StableInstitutionId(f.SourceInstitutionId),
                DisplayLabel = f.EffectiveDisplayNameForPlayer,
                Subtitle = DistrictLineForFacility(city, f),
                Category = GovernmentWindowObjectCategory.GovernmentFacility,
                DiscoveryTier = TierForFacility(f),
                IsSelected = selected,
                SourceInstitutionId = f.SourceInstitutionId,
                DistrictId = f.DistrictId,
                DistrictKind = f.DistrictKind,
                GovernmentSystem = system
            };
        }

        internal static GovernmentWindowFacilityDeploymentDetailModel BuildDeploymentDetail(CityData city,
            GovernmentFacilityData f)
        {
            DistrictData d = FindDistrict(city, f.DistrictId);
            string districtDisplay = d != null ? DiscoveryDisplayText.GetDistrictDisplayName(d) : DiscoveryDisplayText.UnknownDistrict;

            string kindDisplay = f.CanShowExactInstitutionKindNow
                ? FriendlyInstitutionKind(f.InstitutionKind)
                : DiscoveryDisplayText.UnknownInstitution;

            return new GovernmentWindowFacilityDeploymentDetailModel
            {
                EffectiveTitle = f.EffectiveDisplayNameForPlayer,
                FacilityKindDisplay = kindDisplay,
                DistrictDisplay = districtDisplay,
                IsVisibleOnMapNow = f.IsVisibleOnMapNow,
                IsRumorOnly = f.ShouldAppearAsRumorOnlyNow,
                IsLowProfile = f.IsLowProfileCivicKind,
                CanShowExactKind = f.CanShowExactInstitutionKindNow,
                CanShowDetailedInfo = f.CanShowInstitutionDetailedInfoNow,
                SubDepartmentsPlaceholder = GovernmentWindowPlaceholderCopy.SubDepartmentsLater
            };
        }

        internal static string FriendlyInstitutionKind(InstitutionKind k) =>
            k switch
            {
                InstitutionKind.PoliceStation => "Police station",
                InstitutionKind.FederalOffice => "Federal office",
                InstitutionKind.Courthouse => "Courthouse",
                InstitutionKind.Prison => "Prison",
                InstitutionKind.Hospital => "Hospital",
                InstitutionKind.CityHall => "City hall",
                InstitutionKind.TaxOffice => "Tax office",
                InstitutionKind.Bank => "Bank",
                InstitutionKind.RailStation => "Rail station",
                InstitutionKind.DockAuthority => "Dock authority",
                _ => k.ToString()
            };

        internal static List<GovernmentWindowActionModel> StandardCrewActionPlaceholders()
        {
            string r = GovernmentWindowPlaceholderCopy.ActionNotImplemented;
            return new List<GovernmentWindowActionModel>
            {
                new GovernmentWindowActionModel
                    { ActionKey = "learn", DisplayLabel = "Learn", IsEnabled = false, DisabledReason = r },
                new GovernmentWindowActionModel
                    { ActionKey = "observe", DisplayLabel = "Observe", IsEnabled = false, DisabledReason = r },
                new GovernmentWindowActionModel
                    { ActionKey = "infiltrate", DisplayLabel = "Infiltrate", IsEnabled = false, DisabledReason = r },
                new GovernmentWindowActionModel
                    { ActionKey = "influence", DisplayLabel = "Influence", IsEnabled = false, DisabledReason = r }
            };
        }
    }
}
