namespace FamilyBusiness.CityGen.Discovery
{
    /// <summary>
    /// Player-facing knowledge tier for city entities (Batch 9). Truth data on districts/buildings/institutions is unchanged.
    /// </summary>
    public enum DiscoveryState
    {
        Unknown = 0,
        Rumored = 1,
        Known = 2,
        PartiallyExposed = 3,
        DeeplyExposed = 4,
        /// <summary>Future: faction control unlocks sustained intel.</summary>
        Controlled = 5,
        /// <summary>Future: deep cover / inside access.</summary>
        Infiltrated = 6
    }
}
