using FamilyBusiness.CityGen.Data;
using FamilyBusiness.CityGen.Generators;
using FamilyBusiness.CityGen.Government;

namespace FamilyBusiness.CityGen.Core
{
    /// <summary>
    /// Orchestrates the procedural city pipeline: macro → roads → blocks → districts → lots → lot road metadata → anchor institutions → regular buildings → crime potential → discovery defaults (Batch 9–10) → government facility extraction (Batch 12).
    /// After generation, run <see cref="CityWorldEntryBuilder.BuildWorldEntry"/> (Batch 11) to pick gang start and call <see cref="FamilyBusiness.CityGen.Discovery.StartingRevealApplier.Apply"/> once, or replicate that flow in your bootstrap.
    /// </summary>
    public sealed class CityGenerator
    {
        readonly MacroLayoutGenerator _macroLayout = new MacroLayoutGenerator();
        readonly RoadGraphGenerator _roadGraph = new RoadGraphGenerator();
        readonly BlockGenerator _blocks = new BlockGenerator();
        readonly DistrictGenerator _districts = new DistrictGenerator();
        readonly LotGenerator _lots = new LotGenerator();
        readonly LotRoadMetadataEnricher _lotRoadMeta = new LotRoadMetadataEnricher();
        readonly AnchorInstitutionGenerator _anchorInstitutions = new AnchorInstitutionGenerator();
        readonly BuildingPlacementGenerator _regularBuildings = new BuildingPlacementGenerator();
        readonly BuildingCrimePotentialGenerator _buildingCrime = new BuildingCrimePotentialGenerator();
        readonly DiscoveryDefaultsApplier _discoveryDefaults = new DiscoveryDefaultsApplier();

        /// <summary>Uses a random seed (non-deterministic across calls).</summary>
        public CityData Generate(CityGenerationConfig config) =>
            Generate(config, CitySeed.FromRandom());

        /// <summary>Deterministic for a given config + seed.</summary>
        public CityData Generate(CityGenerationConfig config, CitySeed seed)
        {
            var city = new CityData(seed.Value);

            if (config.singleBlockSandboxMap)
            {
                SingleBlockSandboxCityBuilder.Build(city, config, seed);
                _lotRoadMeta.Enrich(city, config, seed.Fork("lots_road_meta"));
                _regularBuildings.Generate(city, config, seed.Fork("regular_buildings"));
                _buildingCrime.Generate(city, config, seed.Fork("building_crime_potential"));
                _discoveryDefaults.Apply(city, config, seed.Fork("discovery_defaults"));
                GovernmentDataExtractor.Refresh(city);
                return city;
            }

            _macroLayout.Generate(city, config, seed.Fork("macro_layout"));
            _roadGraph.Generate(city, config, seed.Fork("road_graph"));
            _blocks.Generate(city, config, seed.Fork("blocks"));
            _districts.Generate(city, config, seed.Fork("districts"));
            _lots.Generate(city, config, seed.Fork("lots"));
            _lotRoadMeta.Enrich(city, config, seed.Fork("lots_road_meta"));
            _anchorInstitutions.Generate(city, config, seed.Fork("institutions"));
            _regularBuildings.Generate(city, config, seed.Fork("regular_buildings"));
            _buildingCrime.Generate(city, config, seed.Fork("building_crime_potential"));
            _discoveryDefaults.Apply(city, config, seed.Fork("discovery_defaults"));
            GovernmentDataExtractor.Refresh(city);

            return city;
        }
    }
}
