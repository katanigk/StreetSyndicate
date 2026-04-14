using FamilyBusiness.CityGen.Data;
using FamilyBusiness.CityGen.Discovery;
using FamilyBusiness.CityGen.Generators;
using FamilyBusiness.CityGen.Government;

namespace FamilyBusiness.CityGen.Core
{
    /// <summary>
    /// Batch 11: after <see cref="CityGenerator.Generate"/>, selects gang start, optionally applies
    /// <see cref="StartingRevealApplier.Apply"/> once, and returns structured spawn data for gameplay bootstrap.
    /// </summary>
    public static class CityWorldEntryBuilder
    {
        /// <summary>
        /// Selects starting gang spawn, writes plan start to <see cref="CityData"/>, and applies starting reveal
        /// when <paramref name="applyStartingReveal"/> is true and (forced or not yet applied).
        /// </summary>
        public static StartingGangSpawnData BuildWorldEntry(CityData city, CityGenerationConfig config,
            float metersPerPlanUnit = 1f, float worldSpawnY = 0f, bool applyStartingReveal = true, bool forceReveal = false)
        {
            if (city == null || config == null)
                return new StartingGangSpawnData();

            StartingGangSpawnData spawn = StartingGangSpawnSelector.Select(city, config, metersPerPlanUnit, worldSpawnY);

            city.GangStartPlanPosition = spawn.StartPlanPosition;
            city.GangStartPlanValid = true;
            city.LastStartingGangSpawn = spawn;

            if (applyStartingReveal && (forceReveal || !city.StartingRevealAppliedAtWorldEntry))
            {
                StartingRevealApplier.Apply(city, config, spawn.StartPlanPosition);
                city.StartingRevealAppliedAtWorldEntry = true;
            }

            GovernmentDataExtractor.Refresh(city, metersPerPlanUnit, worldSpawnY);

            return spawn;
        }
    }
}
