using FamilyBusiness.CityGen.Data;

namespace FamilyBusiness.CityGen.Government.Windows
{
    /// <summary>Batch 13: left-pane list row — discovery-safe labels only.</summary>
    public sealed class GovernmentWindowListItemModel
    {
        public string StableId { get; set; }
        public string DisplayLabel { get; set; }
        public string Subtitle { get; set; }
        public GovernmentWindowObjectCategory Category { get; set; } = GovernmentWindowObjectCategory.None;
        public GovernmentWindowListDiscoveryTier DiscoveryTier { get; set; } = GovernmentWindowListDiscoveryTier.RumoredIntel;
        public bool IsSelected { get; set; }
        public int SourceInstitutionId { get; set; } = -1;
        public int DistrictId { get; set; } = -1;
        public DistrictKind DistrictKind { get; set; } = DistrictKind.Unknown;
        public GovernmentSystemKind GovernmentSystem { get; set; } = GovernmentSystemKind.Unknown;
    }
}
