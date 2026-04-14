namespace FamilyBusiness.CityGen.Discovery
{
    /// <summary>
    /// What could be learned about an entity and minimum state gates (Batch 9). Separate from current <see cref="DiscoveryState"/>.
    /// </summary>
    public sealed class DiscoverabilityProfileData
    {
        /// <summary>Caps promotions from reveal hooks.</summary>
        public DiscoveryState MaxReachableState = DiscoveryState.DeeplyExposed;

        public bool SupportsRumor = true;

        /// <summary>If true, gameplay should not promote past <see cref="DiscoveryState.Rumored"/> without physical entry.</summary>
        public bool RequiresProximityForKnown = true;

        /// <summary>If true, <see cref="DiscoveryState.DeeplyExposed"/>+ needs infiltration-style gameplay.</summary>
        public bool RequiresInfiltrationForDeepExposure = false;

        public DiscoveryState MinStateVisibleOnMap = DiscoveryState.Rumored;
        public DiscoveryState MinStateForExactTypeOrKind = DiscoveryState.Known;
        public DiscoveryState MinStateForCrimeProfile = DiscoveryState.PartiallyExposed;
        public DiscoveryState MinStateForDistrictSimStats = DiscoveryState.PartiallyExposed;
        public DiscoveryState MinStateForInstitutionInternals = DiscoveryState.PartiallyExposed;

        public DiscoverabilityProfileData Clone()
        {
            return new DiscoverabilityProfileData
            {
                MaxReachableState = MaxReachableState,
                SupportsRumor = SupportsRumor,
                RequiresProximityForKnown = RequiresProximityForKnown,
                RequiresInfiltrationForDeepExposure = RequiresInfiltrationForDeepExposure,
                MinStateVisibleOnMap = MinStateVisibleOnMap,
                MinStateForExactTypeOrKind = MinStateForExactTypeOrKind,
                MinStateForCrimeProfile = MinStateForCrimeProfile,
                MinStateForDistrictSimStats = MinStateForDistrictSimStats,
                MinStateForInstitutionInternals = MinStateForInstitutionInternals
            };
        }
    }
}
