using FamilyBusiness.CityGen.Discovery;
using UnityEngine;

namespace FamilyBusiness.CityGen.Data
{
    /// <summary>
    /// Regular building or undeveloped parcel record on a lot (Batch 7).
    /// </summary>
    public sealed class BuildingData
    {
        public int Id { get; set; }
        public BuildingKind Kind { get; set; }
        public BuildingCategory Category { get; set; }
        public BuildingLegalProfile LegalProfile { get; set; }

        public int LotId { get; set; } = -1;
        public int BlockId { get; set; } = -1;
        public int DistrictId { get; set; } = -1;
        public DistrictKind DistrictKind { get; set; } = DistrictKind.Unknown;

        public Vector2 FootprintCenter;
        public Vector2 FootprintSize;

        /// <summary>Primary visible business / use label for UI.</summary>
        public string FrontBusinessType { get; set; }

        public float PlacementScore;
        public float HiddenUsePotential;

        public bool CanSupportBackroom;
        public bool CanSupportStorage;
        public bool CanSupportFrontBusiness;

        public string GameplayTagsPlaceholder { get; set; }

        /// <summary>Batch 8: heuristic crime-facing gameplay potential (null if generation skipped).</summary>
        public BuildingCrimeProfileData Crime { get; set; }

        public bool IsUndeveloped =>
            Kind == BuildingKind.EmptyLot || Kind == BuildingKind.VacantParcel || Kind == BuildingKind.Yard ||
            Kind == BuildingKind.ReservedFutureParcel;

        /// <summary>Batch 9: what the crew knows about this lot; <see cref="Kind"/> / <see cref="Crime"/> stay authoritative.</summary>
        public BuildingDiscoveryData Discovery { get; set; }
    }
}
