using FamilyBusiness.CityGen.Data;

namespace FamilyBusiness.CityGen.Government.Windows
{
    /// <summary>
    /// Batch 14: optional hook for gameplay/planning UI to supply the live <see cref="CityData"/> used by
    /// <see cref="GovernmentWindowRuntimeBinder"/>. Set from bootstrap when a generated city exists.
    /// </summary>
    public static class GovernmentRuntimeCitySource
    {
        public static CityData ActiveCity { get; set; }

        public static bool HasRenderableGovernmentData =>
            ActiveCity != null && ActiveCity.GovernmentData != null;
    }
}
