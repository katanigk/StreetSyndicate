namespace FamilyBusiness.CityGen.Discovery
{
    /// <summary>Factory presets for <see cref="DiscoverabilityProfileData"/> (Batch 9).</summary>
    public static class DiscoverabilityTemplates
    {
        public static DiscoverabilityProfileData DistrictDefault()
        {
            return new DiscoverabilityProfileData
            {
                MaxReachableState = DiscoveryState.Infiltrated,
                SupportsRumor = true,
                RequiresProximityForKnown = true,
                RequiresInfiltrationForDeepExposure = false,
                MinStateVisibleOnMap = DiscoveryState.Rumored,
                MinStateForExactTypeOrKind = DiscoveryState.Known,
                MinStateForCrimeProfile = DiscoveryState.PartiallyExposed,
                MinStateForDistrictSimStats = DiscoveryState.PartiallyExposed,
                MinStateForInstitutionInternals = DiscoveryState.PartiallyExposed
            };
        }

        public static DiscoverabilityProfileData InstitutionPublicLandmark()
        {
            var p = DistrictDefault();
            p.RequiresProximityForKnown = false;
            p.MinStateVisibleOnMap = DiscoveryState.Known;
            return p;
        }

        public static DiscoverabilityProfileData InstitutionLowProfile()
        {
            var p = DistrictDefault();
            p.MinStateVisibleOnMap = DiscoveryState.Known;
            p.RequiresInfiltrationForDeepExposure = true;
            p.MinStateForInstitutionInternals = DiscoveryState.DeeplyExposed;
            return p;
        }

        public static DiscoverabilityProfileData BuildingStreetfront()
        {
            return new DiscoverabilityProfileData
            {
                MaxReachableState = DiscoveryState.Infiltrated,
                SupportsRumor = true,
                RequiresProximityForKnown = true,
                RequiresInfiltrationForDeepExposure = true,
                MinStateVisibleOnMap = DiscoveryState.Rumored,
                MinStateForExactTypeOrKind = DiscoveryState.Known,
                MinStateForCrimeProfile = DiscoveryState.PartiallyExposed,
                MinStateForDistrictSimStats = DiscoveryState.Known,
                MinStateForInstitutionInternals = DiscoveryState.DeeplyExposed
            };
        }

        public static DiscoverabilityProfileData BuildingHidden()
        {
            var p = BuildingStreetfront();
            p.MinStateVisibleOnMap = DiscoveryState.Known;
            p.SupportsRumor = false;
            return p;
        }
    }
}
