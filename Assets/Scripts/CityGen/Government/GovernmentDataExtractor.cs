using FamilyBusiness.CityGen.Data;
using FamilyBusiness.CityGen.Discovery;
using UnityEngine;

namespace FamilyBusiness.CityGen.Government
{
    /// <summary>
    /// Batch 12: builds <see cref="CityGovernmentData"/> from live <see cref="CityData"/> institutions.
    /// Call <see cref="Refresh"/> after discovery mutations (starting reveal, movement intel, etc.).
    /// </summary>
    public static class GovernmentDataExtractor
    {
        /// <summary>Rebuilds government registries and assigns <see cref="CityData.GovernmentData"/>.</summary>
        public static CityGovernmentData Refresh(CityData city, float metersPerPlanUnit = 1f, float worldY = 0f)
        {
            if (city == null)
                return null;

            var data = new CityGovernmentData();
            data.ClearAndRebuildIndices();

            foreach (InstitutionData inst in city.Institutions)
            {
                if (!InstitutionGovernmentMapping.TryGetGovernmentSystem(inst.Kind, out GovernmentSystemKind system))
                    continue;

                GovernmentFacilityData row = BuildRow(inst, system, metersPerPlanUnit, worldY);
                data.AddFacility(row);
            }

            city.GovernmentData = data;
            return data;
        }

        static GovernmentFacilityData BuildRow(InstitutionData inst, GovernmentSystemKind system, float metersPerPlanUnit,
            float worldY)
        {
            var f = new GovernmentFacilityData
            {
                SourceInstitutionId = inst.Id,
                GovernmentSystem = system,
                InstitutionKind = inst.Kind,
                DistrictId = inst.DistrictId,
                DistrictKind = inst.DistrictKind,
                BlockId = inst.BlockId,
                LotId = inst.LotId,
                PlanPositionCells = inst.Position,
                WorldPosition = new Vector3(inst.Position.x * metersPerPlanUnit, worldY, inst.Position.y * metersPerPlanUnit),
                AuthoritativeDisplayName = inst.DisplayName,
                EffectiveDisplayNameForPlayer = DiscoveryDisplayText.GetInstitutionDisplayName(inst),
                GameplayTagsPlaceholder = inst.TagsPlaceholder,
                IsLowProfileCivicKind = inst.Kind == InstitutionKind.Prison || inst.Kind == InstitutionKind.FederalOffice,
                IsPublicFacingCivicKind = inst.Kind != InstitutionKind.Prison && inst.Kind != InstitutionKind.FederalOffice
            };

            InstitutionDiscoveryData d = inst.Discovery;
            if (d != null)
            {
                f.DiscoveryStateSnapshot = d.State;
                f.InstitutionKindKnownSnapshot = d.InstitutionKindKnown;
                f.ExactLocationKnownSnapshot = d.ExactLocationKnown;
                f.IsVisibleOnMapNow = DiscoveryVisibility.ShouldShowInstitutionOnMap(inst);
                f.CanShowExactInstitutionKindNow = DiscoveryVisibility.CanShowExactInstitutionKind(inst);
                f.CanShowInstitutionDetailedInfoNow = DiscoveryVisibility.CanShowInstitutionDetailedInfo(inst);
                f.ShouldAppearAsRumorOnlyNow = DiscoveryVisibility.ShouldAppearAsRumorOnly(inst);
                f.PlayerHasIntelAtLeastRumored = DiscoveryStateOrdering.IsAtLeast(d.State, DiscoveryState.Rumored);
            }
            else
            {
                f.DiscoveryStateSnapshot = DiscoveryState.Unknown;
                f.IsVisibleOnMapNow = true;
                f.CanShowExactInstitutionKindNow = true;
                f.CanShowInstitutionDetailedInfoNow = true;
                f.ShouldAppearAsRumorOnlyNow = false;
                f.PlayerHasIntelAtLeastRumored = true;
                f.IsPublicFacingCivicKind = !f.IsLowProfileCivicKind;
            }

            return f;
        }
    }
}
