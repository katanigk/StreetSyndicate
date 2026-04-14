namespace FamilyBusiness.CityGen.Discovery
{
    /// <summary>Player knowledge wrapper for a regular building (Batch 9).</summary>
    public sealed class BuildingDiscoveryData
    {
        public DiscoveryState State = DiscoveryState.Unknown;
        public DiscoverabilityProfileData Discoverability { get; set; } = new DiscoverabilityProfileData();

        public bool FrontBusinessTypeKnown;
        public bool BuildingCategoryKnown;
        public bool CrimeProfileKnown;

        /// <summary>Reserved for future ownership / internal use systems (Batch 9 placeholder).</summary>
        public bool OwnershipOrInternalUseKnown;
    }
}
