using FamilyBusiness.CityGen.Discovery;
using UnityEngine;

namespace FamilyBusiness.CityGen.Data
{
    /// <summary>
    /// Placed anchor institution on a reserved lot (Batch 6).
    /// </summary>
    public sealed class InstitutionData
    {
        public int Id { get; set; }
        public InstitutionKind Kind { get; set; }
        public string DisplayName { get; set; }

        public int DistrictId { get; set; } = -1;
        public DistrictKind DistrictKind { get; set; } = DistrictKind.Unknown;
        public int BlockId { get; set; } = -1;
        public int LotId { get; set; } = -1;

        /// <summary>Lot center in plan cells.</summary>
        public Vector2 Position;

        public float PlacementScore;

        /// <summary>Optional tier hint for gameplay filters (may be Unknown).</summary>
        public LotSizeClass PreferredSizeClass { get; set; } = LotSizeClass.Unknown;

        public string TagsPlaceholder { get; set; }

        /// <summary>Batch 9: exposure of this institution to the player narrative layer.</summary>
        public InstitutionDiscoveryData Discovery { get; set; }
    }
}
