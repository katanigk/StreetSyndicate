using System.Collections.Generic;

namespace FamilyBusiness.CityGen.Government.Windows
{
    /// <summary>Batch 13: full federal window pane payload for UI binding.</summary>
    public sealed class FederalWindowStateModel
    {
        public FederalWindowMode ActiveMode { get; set; }
        public List<GovernmentWindowListItemModel> LeftItems { get; } = new List<GovernmentWindowListItemModel>();
        public string SelectedItemId { get; set; }
        public GovernmentWindowFacilityDeploymentDetailModel DeploymentDetail { get; set; }
        public string CenterFallbackTitle { get; set; }
        public string CenterFallbackBody { get; set; }
        public List<GovernmentWindowActionModel> RightActions { get; } = new List<GovernmentWindowActionModel>();
        public bool UsesCenterPlaceholder { get; set; }
    }
}
