using UnityEngine;

namespace FamilyBusiness.CityGen.Debug
{
    /// <summary>
    /// Toggles for gizmo / debug drawing layers. Reference from <see cref="CityGenerationConfig"/> or assign on <see cref="CityDebugRenderer"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "CityDebugSettings", menuName = "Family Business/City Gen/City Debug Settings")]
    public sealed class CityDebugSettings : ScriptableObject
    {
        [Header("Layers")]
        public bool drawCityBoundary = true;
        public bool drawMacroWater = true;
        public bool drawMacroRail = true;
        public bool drawMacroAnchors = true;
        public bool drawRoads = true;
        public bool drawRoadMajor = true;
        public bool drawRoadSecondary = true;
        public bool drawRoadNodes = false;
        public bool drawDistricts = true;
        public bool drawDistrictHull = true;
        public bool drawDistrictCenters = true;
        public bool drawDistrictLabels = true;
        public bool drawBlocks = true;
        public bool drawBlocksColoredByDistrict = true;
        public bool drawLots = true;
        public bool drawLotRoadTouchHighlight = false;
        public bool drawLotAccessibilityHeatmap = false;
        public bool drawLotMajorFrontageMarker = false;
        public bool drawLotMetadataLabels = false;
        public bool drawAnchorInstitutions = true;
        public bool drawAnchorInstitutionLabels = true;
        public bool drawInstitutionsByKindColor = true;
        public bool drawRegularBuildings = true;
        public bool drawRegularBuildingLotFill = false;
        public bool drawRegularBuildingsByCategoryColor = true;
        public bool drawRegularBuildingLabels = false;
        [Tooltip("Tint lots: institution-reserved vs regular (non-reserved) placement.")]
        public bool drawLotReservationOverlay = false;

        [Header("Style")]
        public Color boundaryColor = new Color(1f, 1f, 1f, 0.85f);
        public Color macroRiverColor = new Color(0.15f, 0.45f, 0.85f, 0.95f);
        public Color macroCoastColor = new Color(0.2f, 0.55f, 0.75f, 0.9f);
        public Color macroRailColor = new Color(0.35f, 0.28f, 0.22f, 0.95f);
        public Color macroAnchorColor = new Color(1f, 0.92f, 0.35f, 0.95f);
        public float macroAnchorGizmoRadius = 2.5f;
        public Color roadMajorColor = new Color(0.95f, 0.55f, 0.2f, 0.95f);
        public Color roadSecondaryColor = new Color(0.55f, 0.62f, 0.72f, 0.85f);
        public Color roadNodeColor = new Color(0.9f, 0.85f, 0.35f, 0.9f);
        public float roadNodeGizmoRadius = 0.65f;
        public Color districtHullColor = new Color(0.2f, 0.6f, 0.9f, 0.45f);
        public Color districtCenterColor = new Color(1f, 0.35f, 0.55f, 0.95f);
        public float districtCenterGizmoRadius = 1.8f;
        public Color blockColor = new Color(0.9f, 0.7f, 0.2f, 0.4f);
        public float blockGizmoHeight = 0.03f;
        public Color lotColor = new Color(0.35f, 0.85f, 0.45f, 0.65f);
        public Color lotRoadTouchColor = new Color(0.25f, 0.95f, 0.55f, 0.82f);
        public Color lotNoRoadColor = new Color(0.45f, 0.45f, 0.5f, 0.45f);
        public Color lotAccessLowColor = new Color(0.35f, 0.4f, 0.55f, 0.55f);
        public Color lotAccessHighColor = new Color(0.95f, 0.55f, 0.35f, 0.75f);
        public Color lotMajorFrontageMarkerColor = new Color(1f, 0.35f, 0.2f, 0.95f);
        public float lotMajorFrontageMarkerRadius = 0.55f;
        public float lotGizmoHeight = 0.06f;
        public Color anchorInstitutionColor = new Color(0.95f, 0.4f, 0.85f, 0.95f);
        public float anchorInstitutionMarkerRadius = 2.2f;
        public float anchorInstitutionGizmoHeight = 0.1f;

        [Header("Institution colors (Batch 6)")]
        public Color institutionColorPolice = new Color(0.35f, 0.45f, 0.85f, 0.95f);
        public Color institutionColorCourthouse = new Color(0.55f, 0.35f, 0.75f, 0.95f);
        public Color institutionColorPrison = new Color(0.45f, 0.45f, 0.5f, 0.95f);
        public Color institutionColorHospital = new Color(0.9f, 0.35f, 0.4f, 0.95f);
        public Color institutionColorCityHall = new Color(0.95f, 0.75f, 0.3f, 0.95f);
        public Color institutionColorTax = new Color(0.5f, 0.7f, 0.45f, 0.95f);
        public Color institutionColorFederal = new Color(0.35f, 0.55f, 0.65f, 0.95f);
        public Color institutionColorBank = new Color(0.85f, 0.75f, 0.35f, 0.95f);
        public Color institutionColorRail = new Color(0.65f, 0.4f, 0.25f, 0.95f);
        public Color institutionColorDock = new Color(0.25f, 0.55f, 0.85f, 0.95f);

        [Header("Regular buildings (Batch 7)")]
        public Color buildingColorUndeveloped = new Color(0.55f, 0.58f, 0.52f, 0.75f);
        public Color buildingColorCommercial = new Color(0.85f, 0.55f, 0.35f, 0.9f);
        public Color buildingColorIndustrial = new Color(0.5f, 0.48f, 0.45f, 0.9f);
        public Color buildingColorResidential = new Color(0.45f, 0.7f, 0.85f, 0.9f);
        public Color buildingColorCivic = new Color(0.55f, 0.75f, 0.5f, 0.9f);
        public Color buildingColorMixedUse = new Color(0.75f, 0.55f, 0.8f, 0.9f);
        public float regularBuildingMarkerRadius = 1.1f;
        public float regularBuildingGizmoHeight = 0.08f;
        public Color lotReservedInstitutionColor = new Color(0.95f, 0.35f, 0.45f, 0.55f);
        public Color lotRegularPlacedColor = new Color(0.35f, 0.82f, 0.48f, 0.5f);

        [Header("Building crime potential (Batch 8)")]
        [Tooltip("Color building markers by one crime scalar (overrides category colors when active).")]
        public bool drawBuildingCrimeMetricHeatmap = false;
        public BuildingCrimeDebugMetric buildingCrimeHeatmapMetric = BuildingCrimeDebugMetric.Laundering;
        [Tooltip("Scene labels: abbreviated crime values per building.")]
        public bool drawBuildingCrimeValueLabels = false;
        [Tooltip("Full crime readout for one building (editor Handles label).")]
        public bool drawBuildingCrimeFocusDetailLabel = false;
        [Tooltip("Index into CityData.Buildings (generation order) for the focus label.")]
        public int buildingCrimeFocusListIndex = -1;
        public Color buildingCrimeHeatmapLow = new Color(0.2f, 0.35f, 0.85f, 0.92f);
        public Color buildingCrimeHeatmapHigh = new Color(0.95f, 0.25f, 0.2f, 0.95f);

        [Header("Discovery / fog hooks (Batch 9)")]
        [Tooltip("Blend district hull toward discovery-state color (truth outline unchanged; tint is diagnostic).")]
        public bool drawDiscoveryDistrictHullTint = false;
        [Range(0f, 1f)] public float discoveryDistrictTintBlend = 0.45f;
        [Tooltip("Tint building markers by Discovery.State when not using crime heatmap.")]
        public bool drawDiscoveryBuildingMarkerTint = false;
        [Tooltip("Editor: append discovery state to institution labels.")]
        public bool drawDiscoveryStateOnInstitutionLabels = false;
        [Tooltip("Editor: mini labels on buildings with discovery state.")]
        public bool drawDiscoveryStateBuildingLabels = false;
        [Tooltip("Tint institution markers by discovery state (like buildings).")]
        public bool drawDiscoveryInstitutionMarkerTint = false;
        [Tooltip("Editor: append discovery state to district name labels.")]
        public bool drawDiscoveryStateOnDistrictLabels = false;

        [Header("District type colors (blocks / hull tint)")]
        public Color districtColorDowntown = new Color(0.95f, 0.75f, 0.35f, 0.55f);
        public Color districtColorIndustrial = new Color(0.55f, 0.5f, 0.48f, 0.55f);
        public Color districtColorWorkingClass = new Color(0.65f, 0.45f, 0.55f, 0.5f);
        public Color districtColorResidential = new Color(0.45f, 0.65f, 0.85f, 0.5f);
        public Color districtColorWealthy = new Color(0.75f, 0.85f, 0.55f, 0.5f);
        public Color districtColorDocks = new Color(0.35f, 0.55f, 0.8f, 0.55f);
        public Color districtColorFringe = new Color(0.55f, 0.55f, 0.6f, 0.45f);

        public float roadGizmoHeight = 0.05f;
    }
}
