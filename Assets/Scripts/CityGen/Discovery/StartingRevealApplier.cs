using FamilyBusiness.CityGen.Core;
using FamilyBusiness.CityGen.Data;
using UnityEngine;

namespace FamilyBusiness.CityGen.Discovery
{
    /// <summary>
    /// Batch 10: one-shot reveal around the run start position (uses <see cref="DiscoveryProximityReveal.Mode.Starting"/> radii).
    /// </summary>
    public static class StartingRevealApplier
    {
        public static void Apply(CityData city, CityGenerationConfig config, Vector2 planStartCells)
        {
            if (city == null || config == null)
                return;
            DiscoveryProximityReveal.ApplyAroundPlanPosition(city, config, planStartCells,
                DiscoveryProximityReveal.Mode.Starting);
        }
    }
}
