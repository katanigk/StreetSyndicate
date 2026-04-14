using FamilyBusiness.CityGen.Core;
using FamilyBusiness.CityGen.Data;
using FamilyBusiness.CityGen.Discovery;

namespace FamilyBusiness.CityGen.Generators
{
    /// <summary>
    /// Batch 9–10: attaches discovery layers. Cold start (Batch 10) keeps the city hidden until starting/movement reveal.
    /// </summary>
    public sealed class DiscoveryDefaultsApplier
    {
        public void Apply(CityData city, CityGenerationConfig config, CitySeed seed)
        {
            if (!config.applyDiscoveryDefaults)
            {
                ClearDiscovery(city);
                return;
            }

            if (config.applyColdStartDiscoveryDefaults)
                ApplyColdStart(city);
            else
                ApplyWarmStart(city);

            _ = seed;
        }

        /// <summary>Re-run <see cref="Apply"/> with the same rules (e.g. after tests).</summary>
        public void ReapplyDefaults(CityData city, CityGenerationConfig config, CitySeed seed) =>
            Apply(city, config, seed);

        static void ClearDiscovery(CityData city)
        {
            foreach (DistrictData d in city.Districts)
                d.Discovery = null;
            foreach (InstitutionData i in city.Institutions)
                i.Discovery = null;
            foreach (BuildingData b in city.Buildings)
                b.Discovery = null;
        }

        /// <summary>Batch 9 legacy: broad Rumored districts; landmarks Known.</summary>
        static void ApplyWarmStart(CityData city)
        {
            foreach (DistrictData d in city.Districts)
            {
                d.Discovery = new DistrictDiscoveryData
                {
                    State = DiscoveryState.Rumored,
                    Discoverability = DiscoverabilityTemplates.DistrictDefault().Clone(),
                    HasBeenPhysicallyEntered = false,
                    MapVisibilityOverride = null
                };
            }

            foreach (InstitutionData i in city.Institutions)
                ApplyInstitutionWarm(i);

            foreach (BuildingData b in city.Buildings)
                ApplyBuildingWarm(city, b);
        }

        /// <summary>Batch 10: hidden baseline until starting reveal / movement.</summary>
        static void ApplyColdStart(CityData city)
        {
            foreach (DistrictData d in city.Districts)
            {
                d.Discovery = new DistrictDiscoveryData
                {
                    State = DiscoveryState.Unknown,
                    Discoverability = DiscoverabilityTemplates.DistrictDefault().Clone(),
                    HasBeenPhysicallyEntered = false,
                    MapVisibilityOverride = null
                };
            }

            foreach (InstitutionData i in city.Institutions)
                ApplyInstitutionCold(i);

            foreach (BuildingData b in city.Buildings)
                ApplyBuildingCold(city, b);
        }

        static void ApplyInstitutionWarm(InstitutionData i)
        {
            var disc = new InstitutionDiscoveryData();

            switch (i.Kind)
            {
                case InstitutionKind.PoliceStation:
                case InstitutionKind.Hospital:
                case InstitutionKind.CityHall:
                case InstitutionKind.Courthouse:
                case InstitutionKind.RailStation:
                case InstitutionKind.Bank:
                case InstitutionKind.TaxOffice:
                case InstitutionKind.DockAuthority:
                    disc.State = DiscoveryState.Known;
                    disc.Discoverability = DiscoverabilityTemplates.InstitutionPublicLandmark().Clone();
                    disc.InstitutionKindKnown = true;
                    disc.ExactLocationKnown = true;
                    disc.InternalDetailsKnown = false;
                    break;

                case InstitutionKind.Prison:
                case InstitutionKind.FederalOffice:
                    disc.State = DiscoveryState.Rumored;
                    disc.Discoverability = DiscoverabilityTemplates.InstitutionLowProfile().Clone();
                    disc.InstitutionKindKnown = false;
                    disc.ExactLocationKnown = false;
                    disc.InternalDetailsKnown = false;
                    break;

                default:
                    disc.State = DiscoveryState.Unknown;
                    disc.Discoverability = DiscoverabilityTemplates.DistrictDefault().Clone();
                    break;
            }

            i.Discovery = disc;
        }

        static void ApplyInstitutionCold(InstitutionData i)
        {
            var disc = new InstitutionDiscoveryData
            {
                State = DiscoveryState.Unknown,
                InstitutionKindKnown = false,
                ExactLocationKnown = false,
                InternalDetailsKnown = false
            };

            switch (i.Kind)
            {
                case InstitutionKind.PoliceStation:
                case InstitutionKind.Hospital:
                case InstitutionKind.CityHall:
                case InstitutionKind.Courthouse:
                case InstitutionKind.RailStation:
                case InstitutionKind.Bank:
                case InstitutionKind.TaxOffice:
                case InstitutionKind.DockAuthority:
                    disc.Discoverability = DiscoverabilityTemplates.InstitutionPublicLandmark().Clone();
                    break;
                case InstitutionKind.Prison:
                case InstitutionKind.FederalOffice:
                    disc.Discoverability = DiscoverabilityTemplates.InstitutionLowProfile().Clone();
                    break;
                default:
                    disc.Discoverability = DiscoverabilityTemplates.DistrictDefault().Clone();
                    break;
            }

            i.Discovery = disc;
        }

        static void ApplyBuildingWarm(CityData city, BuildingData b)
        {
            LotData lot = FindLot(city, b.LotId);
            var disc = new BuildingDiscoveryData
            {
                State = DiscoveryState.Unknown,
                Discoverability = DiscoverabilityTemplates.BuildingStreetfront().Clone(),
                FrontBusinessTypeKnown = false,
                BuildingCategoryKnown = false,
                CrimeProfileKnown = false,
                OwnershipOrInternalUseKnown = false
            };

            if (!b.IsUndeveloped && lot != null && lot.TouchesRoad)
            {
                disc.State = DiscoveryState.Rumored;
                disc.Discoverability = DiscoverabilityTemplates.BuildingStreetfront().Clone();
            }

            if (b.IsUndeveloped)
            {
                disc.Discoverability = DiscoverabilityTemplates.BuildingHidden().Clone();
                disc.State = DiscoveryState.Unknown;
            }

            b.Discovery = disc;
        }

        static void ApplyBuildingCold(CityData city, BuildingData b)
        {
            LotData lot = FindLot(city, b.LotId);
            var disc = new BuildingDiscoveryData
            {
                State = DiscoveryState.Unknown,
                FrontBusinessTypeKnown = false,
                BuildingCategoryKnown = false,
                CrimeProfileKnown = false,
                OwnershipOrInternalUseKnown = false
            };

            if (b.IsUndeveloped)
                disc.Discoverability = DiscoverabilityTemplates.BuildingHidden().Clone();
            else if (lot != null && lot.TouchesRoad)
                disc.Discoverability = DiscoverabilityTemplates.BuildingStreetfront().Clone();
            else
                disc.Discoverability = DiscoverabilityTemplates.BuildingStreetfront().Clone();

            b.Discovery = disc;
        }

        static LotData FindLot(CityData city, int lotId)
        {
            foreach (LotData l in city.Lots)
            {
                if (l.Id == lotId)
                    return l;
            }

            return null;
        }
    }
}
