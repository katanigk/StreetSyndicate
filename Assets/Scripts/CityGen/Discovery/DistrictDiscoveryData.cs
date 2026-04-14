namespace FamilyBusiness.CityGen.Discovery
{
    /// <summary>Player knowledge wrapper for a district (Batch 9).</summary>
    public sealed class DistrictDiscoveryData
    {
        public DiscoveryState State = DiscoveryState.Unknown;
        public DiscoverabilityProfileData Discoverability { get; set; } = new DiscoverabilityProfileData();

        public bool HasBeenPhysicallyEntered;

        /// <summary>Optional gameplay override; if null, visibility is derived from state vs profile.</summary>
        public bool? MapVisibilityOverride;

        public bool DetailedMetadataUnlocked =>
            DiscoveryStateOrdering.IsAtLeast(State, Discoverability?.MinStateForDistrictSimStats ?? DiscoveryState.PartiallyExposed);
    }
}
