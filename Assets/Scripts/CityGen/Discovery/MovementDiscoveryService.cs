using FamilyBusiness.CityGen.Core;
using FamilyBusiness.CityGen.Data;
using FamilyBusiness.CityGen.Government;
using UnityEngine;

namespace FamilyBusiness.CityGen.Discovery
{
    /// <summary>
    /// Batch 10: call from gameplay when characters move — promotes discovery monotonically via <see cref="DiscoveryProximityReveal"/>.
    /// Batch 12: optionally refreshes <see cref="CityData.GovernmentData"/> after each movement reveal (same plan/world scale as your scene).
    /// </summary>
    public sealed class MovementDiscoveryService
    {
        public void RevealAroundPlanPosition(CityData city, CityGenerationConfig config, Vector2 planCells,
            float metersPerPlanUnit = 1f, float worldY = 0f, bool refreshGovernmentData = true)
        {
            DiscoveryProximityReveal.ApplyAroundPlanPosition(city, config, planCells,
                DiscoveryProximityReveal.Mode.Movement);
            if (refreshGovernmentData)
                GovernmentDataExtractor.Refresh(city, metersPerPlanUnit, worldY);
        }

        public void RevealAroundActor(CityData city, CityGenerationConfig config, Vector3 worldPosition,
            Transform cityRoot, float metersPerPlanUnit, float worldY = 0f, bool refreshGovernmentData = true)
        {
            if (cityRoot == null || metersPerPlanUnit <= 1e-5f)
                return;
            Vector3 local = cityRoot.InverseTransformPoint(worldPosition);
            var plan = new Vector2(local.x / metersPerPlanUnit, local.z / metersPerPlanUnit);
            RevealAroundPlanPosition(city, config, plan, metersPerPlanUnit, worldY, refreshGovernmentData);
        }

        public void OnDistrictEntry(DistrictData entered)
        {
            if (entered == null)
                return;
            DiscoveryReveal.ApplyProximityRevealDistrict(entered);
        }

        /// <summary>Overload for gameplay callers that already hold city/config for future gating.</summary>
        public void OnDistrictEntry(CityData city, CityGenerationConfig config, DistrictData entered)
        {
            _ = city;
            _ = config;
            OnDistrictEntry(entered);
        }

        public void RevealNearbyBuildings(CityData city, CityGenerationConfig config, Vector2 planCells) =>
            RevealAroundPlanPosition(city, config, planCells);

        public void RevealNearbyInstitutions(CityData city, CityGenerationConfig config, Vector2 planCells) =>
            RevealAroundPlanPosition(city, config, planCells);
    }
}
