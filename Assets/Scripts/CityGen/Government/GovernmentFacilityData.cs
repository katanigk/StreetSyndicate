using FamilyBusiness.CityGen.Data;
using FamilyBusiness.CityGen.Discovery;
using UnityEngine;

namespace FamilyBusiness.CityGen.Government
{
    /// <summary>
    /// Batch 12: thin read model over one <see cref="InstitutionData"/> for future government windows.
    /// Refresh when discovery changes via <see cref="GovernmentDataExtractor"/>.
    /// </summary>
    public sealed class GovernmentFacilityData
    {
        public int SourceInstitutionId { get; set; } = -1;
        public GovernmentSystemKind GovernmentSystem { get; set; } = GovernmentSystemKind.Unknown;
        public InstitutionKind InstitutionKind { get; set; } = InstitutionKind.Unknown;

        public int DistrictId { get; set; } = -1;
        public DistrictKind DistrictKind { get; set; } = DistrictKind.Unknown;
        public int BlockId { get; set; } = -1;
        public int LotId { get; set; } = -1;

        public Vector2 PlanPositionCells;
        public Vector3 WorldPosition;

        /// <summary>Generator / narrative truth name (not player-facing when fogged).</summary>
        public string AuthoritativeDisplayName { get; set; }

        /// <summary>Label safe to show given current discovery (uses <see cref="DiscoveryDisplayText"/> rules).</summary>
        public string EffectiveDisplayNameForPlayer { get; set; }

        /// <summary>Snapshot at extraction time; re-extract after discovery updates.</summary>
        public DiscoveryState DiscoveryStateSnapshot { get; set; } = DiscoveryState.Unknown;

        public bool InstitutionKindKnownSnapshot { get; set; }
        public bool ExactLocationKnownSnapshot { get; set; }

        /// <summary>From <see cref="DiscoveryVisibility.ShouldShowInstitutionOnMap"/>.</summary>
        public bool IsVisibleOnMapNow { get; set; }

        public bool CanShowExactInstitutionKindNow { get; set; }
        public bool CanShowInstitutionDetailedInfoNow { get; set; }
        public bool ShouldAppearAsRumorOnlyNow { get; set; }

        /// <summary>At least rumored — use for government window list rows (Unknown = hide row entirely).</summary>
        public bool PlayerHasIntelAtLeastRumored { get; set; }

        /// <summary>Same as <see cref="PlayerHasIntelAtLeastRumored"/>; UI-friendly alias.</summary>
        public bool ShouldAppearInGovernmentWindow => PlayerHasIntelAtLeastRumored;

        /// <summary>Prison / federal-style defaults (low-profile discoverability in generator).</summary>
        public bool IsLowProfileCivicKind { get; set; }

        /// <summary>Obvious civic landmark profile vs <see cref="IsLowProfileCivicKind"/> (tax/police/hospital/court/city hall).</summary>
        public bool IsPublicFacingCivicKind { get; set; }

        public string GameplayTagsPlaceholder { get; set; }
    }
}
