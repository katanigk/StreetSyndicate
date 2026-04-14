namespace FamilyBusiness.CityGen.Discovery
{
    /// <summary>Player knowledge wrapper for an anchor institution (Batch 9).</summary>
    public sealed class InstitutionDiscoveryData
    {
        public DiscoveryState State = DiscoveryState.Unknown;
        public DiscoverabilityProfileData Discoverability { get; set; } = new DiscoverabilityProfileData();

        public bool InstitutionKindKnown;
        public bool ExactLocationKnown;
        public bool InternalDetailsKnown;
    }
}
