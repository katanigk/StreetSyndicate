namespace FamilyBusiness.CityGen.Data
{
    /// <summary>
    /// Heuristic criminal-gameplay potential for a regular building (Batch 8). Values are in [0, 1]; not simulated economy.
    /// </summary>
    public sealed class BuildingCrimeProfileData
    {
        public float FrontBusinessPotential;
        public float StoragePotential;
        public float BackroomPotential;
        public float LaunderingPotential;
        public float ExtortionPotential;
        public float BlackMarketSuitability;
        public float MeetingPotential;
        /// <summary>How visible / scrutinized illegal use would be (not police AI).</summary>
        public float PoliceVisibility;
        public float NeighborhoodInfluenceValue;
        public float LogisticsValue;

        public bool CanActAsFront;
        public bool CanStoreContraband;
        public bool CanHostMeeting;
        public bool CanSupportLaundering;
        public bool IsHighRiskIfUsedIllegally;
    }
}
