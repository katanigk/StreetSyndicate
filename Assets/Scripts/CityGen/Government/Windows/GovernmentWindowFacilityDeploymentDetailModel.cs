namespace FamilyBusiness.CityGen.Government.Windows
{
    /// <summary>Batch 13: center-pane focus for a selected government facility (deployment mode).</summary>
    public sealed class GovernmentWindowFacilityDeploymentDetailModel
    {
        public string EffectiveTitle { get; set; }
        public string FacilityKindDisplay { get; set; }
        public string DistrictDisplay { get; set; }
        public bool IsVisibleOnMapNow { get; set; }
        public bool IsRumorOnly { get; set; }
        public bool IsLowProfile { get; set; }
        public bool CanShowExactKind { get; set; }
        public bool CanShowDetailedInfo { get; set; }
        public string SubDepartmentsPlaceholder { get; set; }
    }
}
