using FamilyBusiness.CityGen.Core;
using FamilyBusiness.CityGen.Data;
using FamilyBusiness.CityGen.Generators;

namespace FamilyBusiness.CityGen.Discovery
{
    /// <summary>
    /// Batch 10: restore discovery layers to post-generation defaults (for tests / iteration).
    /// </summary>
    public static class CityDiscoveryReset
    {
        public static void ReapplyDefaults(CityData city, CityGenerationConfig config)
        {
            if (city == null || config == null)
                return;
            var applier = new DiscoveryDefaultsApplier();
            applier.Apply(city, config, CitySeed.FromExplicit(city.Seed));
        }
    }
}
