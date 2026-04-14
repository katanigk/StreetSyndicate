using FamilyBusiness.CityGen.Data;
using FamilyBusiness.CityGen.Discovery;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FamilyBusiness.CityGen.Debug
{
    /// <summary>
    /// Scene helper: draws <see cref="CityData"/> layers via gizmos. Generation stays pure; this is visualization only.
    /// </summary>
    [ExecuteAlways]
    public sealed class CityDebugRenderer : MonoBehaviour
    {
        [SerializeField] CityDebugSettings settings;
        [SerializeField] float metersPerPlanUnit = 1f;
        [SerializeField] float yLift = 0.02f;
        [Tooltip("Index into CityData.Buildings for inspector / focused scene readout.")]
        [SerializeField] int crimeDebugFocusBuildingIndex = -1;

        CityData _city;

        public CityData City => _city;

        public void SetCityData(CityData city) => _city = city;

        public void SetSettings(CityDebugSettings s) => settings = s;

        void OnDrawGizmos()
        {
            if (_city == null)
                return;

            if (settings == null)
            {
                DrawBoundaryDefault();
                DrawMacroFeaturesDefault();
                DrawMacroAnchorsDefault();
                DrawRoadsDefault();
                DrawDistrictsDefault();
                DrawBlocksDefault();
                DrawLotsDefault();
                DrawInstitutionsDefault();
                DrawRegularBuildingsDefault();
                return;
            }

            if (settings.drawCityBoundary)
                DrawCityBoundary();
            if (settings.drawMacroWater)
                DrawMacroWaterFeatures();
            if (settings.drawMacroRail)
                DrawMacroRailFeatures();
            if (settings.drawMacroAnchors)
                DrawMacroAnchors();
            if (settings.drawRoads)
                DrawRoads();
            if (settings.drawDistricts)
                DrawDistricts();
            if (settings.drawBlocks)
                DrawBlocks();
            if (settings.drawLots)
                DrawLots();
            if (settings.drawAnchorInstitutions)
                DrawInstitutions();
            if (settings.drawRegularBuildings)
                DrawRegularBuildings();
#if UNITY_EDITOR
            DrawDistrictLabelsEditor();
            DrawLotMetadataLabelsEditor();
            DrawInstitutionLabelsEditor();
            DrawRegularBuildingLabelsEditor();
            DrawBuildingCrimeValueLabelsEditor();
            DrawBuildingCrimeFocusLabelEditor();
            DrawDiscoveryStateBuildingLabelsEditor();
#endif
        }

        Vector3 PlanToWorld(Vector2 p)
        {
            Vector3 local = new Vector3(p.x * metersPerPlanUnit, yLift, p.y * metersPerPlanUnit);
            return transform.TransformPoint(local);
        }

        // --- Macro layers ---

        public void DrawCityBoundary()
        {
            if (settings == null) return;
            Gizmos.color = settings.boundaryColor;
            if (_city.MacroBoundary.Vertices.Count >= 3)
                DrawPolygonOpenOrClosed(_city.MacroBoundary.Vertices, closed: true);
            else
                DrawRectangle(_city.Boundary.Min, _city.Boundary.Max);
        }

        public void DrawMacroWaterFeatures()
        {
            if (settings == null) return;
            foreach (MacroFeatureData f in _city.MacroFeatures)
            {
                if (f.Kind == MacroFeatureKind.RailCorridor)
                    continue;
                Gizmos.color = f.Kind == MacroFeatureKind.Coastline ? settings.macroCoastColor : settings.macroRiverColor;
                DrawThickPolyline(f.Path, f.WidthCells * metersPerPlanUnit * 0.5f);
            }
        }

        public void DrawMacroRailFeatures()
        {
            if (settings == null) return;
            foreach (MacroFeatureData f in _city.MacroFeatures)
            {
                if (f.Kind != MacroFeatureKind.RailCorridor)
                    continue;
                Gizmos.color = settings.macroRailColor;
                DrawThickPolyline(f.Path, f.WidthCells * metersPerPlanUnit * 0.35f);
            }
        }

        public void DrawMacroAnchors()
        {
            if (settings == null) return;
            Gizmos.color = settings.macroAnchorColor;
            float r = settings.macroAnchorGizmoRadius * metersPerPlanUnit;
            foreach (MacroAnchorPointData a in _city.MacroAnchors)
            {
                Vector3 c = PlanToWorld(a.Position);
                Gizmos.DrawSphere(c, r);
            }
        }

        // --- Legacy / pipeline layers ---

        public void DrawRoads()
        {
            if (settings == null || _city.RoadNodes.Count == 0) return;
            float h = settings.roadGizmoHeight;

            foreach (RoadEdge e in _city.RoadEdges)
            {
                if (e.Kind == RoadEdgeKind.Major && !settings.drawRoadMajor)
                    continue;
                if (e.Kind == RoadEdgeKind.Secondary && !settings.drawRoadSecondary)
                    continue;
                if (e.Kind != RoadEdgeKind.Major && e.Kind != RoadEdgeKind.Secondary)
                    continue;

                Gizmos.color = e.Kind == RoadEdgeKind.Major ? settings.roadMajorColor : settings.roadSecondaryColor;
                RoadNode a = FindNode(e.FromNodeId);
                RoadNode b = FindNode(e.ToNodeId);
                if (a == null || b == null) continue;
                Vector3 p0 = PlanToWorld(a.Position) + Vector3.up * h;
                Vector3 p1 = PlanToWorld(b.Position) + Vector3.up * h;
                Gizmos.DrawLine(p0, p1);
            }

            if (settings.drawRoadNodes)
            {
                Gizmos.color = settings.roadNodeColor;
                float r = settings.roadNodeGizmoRadius * metersPerPlanUnit;
                foreach (RoadNode n in _city.RoadNodes)
                    Gizmos.DrawSphere(PlanToWorld(n.Position) + Vector3.up * h, r);
            }
        }

        public void DrawDistricts()
        {
            if (settings == null) return;
            float hullLift = settings.blockGizmoHeight * metersPerPlanUnit * 0.5f;
            foreach (DistrictData d in _city.Districts)
            {
                if (settings.drawDistrictHull && d.Outline != null && d.Outline.Count >= 3)
                {
                    Color c = DistrictTypeColor(d.Kind, settings);
                    c.a = Mathf.Clamp01(settings.districtHullColor.a + 0.1f);
                    if (settings.drawDiscoveryDistrictHullTint && d.Discovery != null)
                    {
                        Color disc = DiscoveryDebugPalette.StateColor(d.Discovery.State);
                        float t = Mathf.Clamp01(settings.discoveryDistrictTintBlend);
                        c = Color.Lerp(c, disc, t);
                        c.a = Mathf.Clamp01(Mathf.Lerp(c.a, disc.a, t));
                    }

                    Gizmos.color = c;
                    DrawPolygonOpenOrClosedLifted(d.Outline, closed: true, hullLift);
                }

                if (settings.drawDistrictCenters)
                {
                    Gizmos.color = settings.districtCenterColor;
                    float r = settings.districtCenterGizmoRadius * metersPerPlanUnit;
                    Vector3 c = PlanToWorld(d.CenterPosition) + Vector3.up * (hullLift * 2f + 0.02f);
                    Gizmos.DrawSphere(c, r);
                }
            }
        }

        public void DrawBlocks()
        {
            if (settings == null) return;
            float lift = settings.blockGizmoHeight * metersPerPlanUnit;
            foreach (BlockData b in _city.Blocks)
            {
                if (settings.drawBlocksColoredByDistrict)
                    Gizmos.color = DistrictTypeColor(b.DistrictKind, settings);
                else
                    Gizmos.color = settings.blockColor;
                DrawRectangleLifted(b.Min, b.Max, lift);
            }
        }

        public void DrawInstitutions()
        {
            if (settings == null || _city.Institutions.Count == 0) return;
            float lift = settings.anchorInstitutionGizmoHeight * metersPerPlanUnit;
            float r = settings.anchorInstitutionMarkerRadius * metersPerPlanUnit;
            foreach (InstitutionData a in _city.Institutions)
            {
                Color col;
                if (settings.drawDiscoveryInstitutionMarkerTint && a.Discovery != null)
                {
                    Color baseC = settings.drawInstitutionsByKindColor
                        ? InstitutionKindColor(a.Kind, settings)
                        : settings.anchorInstitutionColor;
                    Color disc = DiscoveryDebugPalette.StateColor(a.Discovery.State);
                    col = Color.Lerp(baseC, disc, 0.55f);
                }
                else
                    col = settings.drawInstitutionsByKindColor
                        ? InstitutionKindColor(a.Kind, settings)
                        : settings.anchorInstitutionColor;
                Gizmos.color = col;
                Vector3 p = PlanToWorld(a.Position) + Vector3.up * lift;
                Gizmos.DrawSphere(p, r);
            }
        }

        public void DrawRegularBuildings()
        {
            if (settings == null || _city.Buildings.Count == 0) return;
            float lift = settings.regularBuildingGizmoHeight * metersPerPlanUnit;
            float r = settings.regularBuildingMarkerRadius * metersPerPlanUnit;
            foreach (BuildingData b in _city.Buildings)
            {
                Color markerColor;
                if (settings.drawBuildingCrimeMetricHeatmap && settings.buildingCrimeHeatmapMetric != BuildingCrimeDebugMetric.None &&
                    b.Crime != null)
                {
                    float t = SampleCrimeMetric(b.Crime, settings.buildingCrimeHeatmapMetric);
                    markerColor = Color.Lerp(settings.buildingCrimeHeatmapLow, settings.buildingCrimeHeatmapHigh,
                        Mathf.Clamp01(t));
                }
                else if (settings.drawBuildingCrimeMetricHeatmap && b.Crime == null)
                    markerColor = new Color(0.35f, 0.35f, 0.38f, 0.65f);
                else if (settings.drawDiscoveryBuildingMarkerTint && b.Discovery != null)
                {
                    Color baseC = settings.drawRegularBuildingsByCategoryColor
                        ? BuildingCategoryColor(b.Category, settings)
                        : settings.buildingColorCommercial;
                    Color disc = DiscoveryDebugPalette.StateColor(b.Discovery.State);
                    markerColor = Color.Lerp(baseC, disc, 0.55f);
                }
                else
                    markerColor = settings.drawRegularBuildingsByCategoryColor
                        ? BuildingCategoryColor(b.Category, settings)
                        : settings.buildingColorCommercial;

                Gizmos.color = markerColor;
                Vector3 c = PlanToWorld(b.FootprintCenter) + Vector3.up * lift;
                if (settings.drawRegularBuildingLotFill && !b.IsUndeveloped)
                {
                    Vector2 half = b.FootprintSize * 0.5f;
                    Vector2 mn = b.FootprintCenter - half;
                    Vector2 mx = b.FootprintCenter + half;
                    Color fill = markerColor;
                    fill.a = Mathf.Clamp01(fill.a * 0.35f);
                    Gizmos.color = fill;
                    DrawRectangleLifted(mn, mx, lift * 0.5f);
                    Gizmos.color = markerColor;
                }

                Gizmos.DrawSphere(c, b.IsUndeveloped ? r * 0.55f : r);
            }
        }

        public void DrawLots()
        {
            if (settings == null || _city.Lots.Count == 0) return;
            float lift = settings.lotGizmoHeight * metersPerPlanUnit;
            float markerR = settings.lotMajorFrontageMarkerRadius * metersPerPlanUnit;
            foreach (LotData lot in _city.Lots)
            {
                if (settings.drawLotReservationOverlay)
                {
                    Gizmos.color = lot.IsReserved ? settings.lotReservedInstitutionColor : settings.lotRegularPlacedColor;
                }
                else if (settings.drawLotAccessibilityHeatmap)
                    Gizmos.color = Color.Lerp(settings.lotAccessLowColor, settings.lotAccessHighColor,
                        Mathf.Clamp01(lot.AccessibilityScore));
                else if (settings.drawLotRoadTouchHighlight)
                    Gizmos.color = lot.TouchesRoad ? settings.lotRoadTouchColor : settings.lotNoRoadColor;
                else
                    Gizmos.color = settings.lotColor;
                DrawRectangleLifted(lot.Min, lot.Max, lift);

                if (settings.drawLotMajorFrontageMarker && lot.HasMajorRoadFrontage)
                {
                    Gizmos.color = settings.lotMajorFrontageMarkerColor;
                    Vector2 fp = LotFrontageMidpoint(lot);
                    Gizmos.DrawSphere(PlanToWorld(fp) + Vector3.up * (lift + 0.03f), markerR);
                }
            }
        }

        static Vector2 LotFrontageMidpoint(LotData lot)
        {
            float mx = (lot.Min.x + lot.Max.x) * 0.5f;
            float my = (lot.Min.y + lot.Max.y) * 0.5f;
            switch (lot.FrontageSideIndex)
            {
                case 0: return new Vector2(mx, lot.Min.y);
                case 1: return new Vector2(lot.Max.x, my);
                case 2: return new Vector2(mx, lot.Max.y);
                case 3: return new Vector2(lot.Min.x, my);
                default: return new Vector2(mx, my);
            }
        }

#if UNITY_EDITOR
        void DrawDistrictLabelsEditor()
        {
            if (settings == null || _city == null || !settings.drawDistrictLabels || !settings.drawDistricts)
                return;
            float lift = settings.blockGizmoHeight * metersPerPlanUnit + 0.2f;
            foreach (DistrictData d in _city.Districts)
            {
                Vector3 p = PlanToWorld(d.CenterPosition) + Vector3.up * lift;
                Color col = DistrictTypeColor(d.Kind, settings);
                Handles.color = col;
                var st = new GUIStyle(EditorStyles.boldLabel);
                st.normal.textColor = col;
                string dtxt = d.Name + " (" + d.Kind + ")";
                if (settings.drawDiscoveryStateOnDistrictLabels && d.Discovery != null)
                    dtxt += $"\n{d.Discovery.State}";
                Handles.Label(p, dtxt, st);
            }
        }

        void DrawLotMetadataLabelsEditor()
        {
            if (settings == null || _city == null || !settings.drawLotMetadataLabels || !settings.drawLots)
                return;
            float lift = settings.lotGizmoHeight * metersPerPlanUnit + 0.25f;
            foreach (LotData lot in _city.Lots)
            {
                Vector3 p = PlanToWorld((lot.Min + lot.Max) * 0.5f) + Vector3.up * lift;
                string txt = $"A:{lot.AccessibilityScore:0.00} F:{lot.FrontageLength:0.#}";
                if (lot.TouchesRoad)
                    txt += " R";
                if (lot.HasMajorRoadFrontage)
                    txt += " M";
                var st = new GUIStyle(EditorStyles.miniLabel);
                st.normal.textColor = new Color(0.9f, 0.9f, 0.85f, 1f);
                Handles.Label(p, txt, st);
            }
        }

        void DrawInstitutionLabelsEditor()
        {
            if (settings == null || _city == null || !settings.drawAnchorInstitutionLabels ||
                !settings.drawAnchorInstitutions)
                return;
            float lift = settings.anchorInstitutionGizmoHeight * metersPerPlanUnit + 0.35f;
            foreach (InstitutionData a in _city.Institutions)
            {
                Vector3 p = PlanToWorld(a.Position) + Vector3.up * lift;
                Color col = settings.drawInstitutionsByKindColor
                    ? InstitutionKindColor(a.Kind, settings)
                    : settings.anchorInstitutionColor;
                var st = new GUIStyle(EditorStyles.boldLabel);
                st.normal.textColor = col;
                string dist = FindDistrictName(_city, a.DistrictId);
                string txt = $"{a.DisplayName}\n{a.Kind} · {dist}";
                if (settings.drawDiscoveryStateOnInstitutionLabels && a.Discovery != null)
                    txt += $"\nDiscovery: {a.Discovery.State}";
                Handles.Label(p, txt, st);
            }
        }

        void DrawDiscoveryStateBuildingLabelsEditor()
        {
            if (settings == null || _city == null || !settings.drawDiscoveryStateBuildingLabels ||
                !settings.drawRegularBuildings)
                return;
            float lift = settings.regularBuildingGizmoHeight * metersPerPlanUnit + 0.72f;
            foreach (BuildingData b in _city.Buildings)
            {
                if (b.Discovery == null)
                    continue;
                Vector3 p = PlanToWorld(b.FootprintCenter) + Vector3.up * lift;
                var st = new GUIStyle(EditorStyles.miniLabel);
                st.normal.textColor = DiscoveryDebugPalette.StateColor(b.Discovery.State);
                Handles.Label(p, b.Discovery.State.ToString(), st);
            }
        }

        void DrawRegularBuildingLabelsEditor()
        {
            if (settings == null || _city == null || !settings.drawRegularBuildingLabels || !settings.drawRegularBuildings)
                return;
            float lift = settings.regularBuildingGizmoHeight * metersPerPlanUnit + 0.4f;
            foreach (BuildingData b in _city.Buildings)
            {
                Vector3 p = PlanToWorld(b.FootprintCenter) + Vector3.up * lift;
                Color col = BuildingCategoryColor(b.Category, settings);
                var st = new GUIStyle(EditorStyles.miniLabel);
                st.normal.textColor = col;
                Handles.Label(p, $"{b.FrontBusinessType}\n{b.Kind} · {b.Category}", st);
            }
        }

        void DrawBuildingCrimeValueLabelsEditor()
        {
            if (settings == null || _city == null || !settings.drawBuildingCrimeValueLabels || !settings.drawRegularBuildings)
                return;
            float lift = settings.regularBuildingGizmoHeight * metersPerPlanUnit + 0.55f;
            foreach (BuildingData b in _city.Buildings)
            {
                if (b.Crime == null)
                    continue;
                Vector3 p = PlanToWorld(b.FootprintCenter) + Vector3.up * lift;
                var st = new GUIStyle(EditorStyles.miniLabel);
                st.normal.textColor = new Color(0.95f, 0.85f, 0.55f, 1f);
                BuildingCrimeProfileData c = b.Crime;
                Handles.Label(p,
                    $"F{c.FrontBusinessPotential:0.00} St{c.StoragePotential:0.00} Ld{c.LaunderingPotential:0.00} Ex{c.ExtortionPotential:0.00} BM{c.BlackMarketSuitability:0.00}",
                    st);
            }
        }

        void DrawBuildingCrimeFocusLabelEditor()
        {
            if (_city == null || _city.Buildings.Count == 0)
                return;

            int idx;
            if (settings != null)
            {
                if (!settings.drawRegularBuildings || !settings.drawBuildingCrimeFocusDetailLabel)
                    return;
                idx = settings.buildingCrimeFocusListIndex;
            }
            else
                idx = crimeDebugFocusBuildingIndex;

            if (idx < 0 || idx >= _city.Buildings.Count)
                return;

            BuildingData b = _city.Buildings[idx];
            if (b.Crime == null)
                return;
            float gh = settings != null ? settings.regularBuildingGizmoHeight : 0.08f;
            float lift = gh * metersPerPlanUnit + 1.1f;
            Vector3 p = PlanToWorld(b.FootprintCenter) + Vector3.up * lift;
            var st = new GUIStyle(EditorStyles.boldLabel);
            st.normal.textColor = Color.white;
            BuildingCrimeProfileData c = b.Crime;
            Handles.Label(p,
                $"[Crime focus #{idx}] {b.Kind}\n" +
                $"Front {c.FrontBusinessPotential:0.00}  Storage {c.StoragePotential:0.00}  Back {c.BackroomPotential:0.00}\n" +
                $"Laundry {c.LaunderingPotential:0.00}  Extort {c.ExtortionPotential:0.00}  BM {c.BlackMarketSuitability:0.00}\n" +
                $"Meet {c.MeetingPotential:0.00}  Police {c.PoliceVisibility:0.00}  Inf {c.NeighborhoodInfluenceValue:0.00}  Log {c.LogisticsValue:0.00}\n" +
                $"Flags: Front={c.CanActAsFront} Store={c.CanStoreContraband} Meet={c.CanHostMeeting} Laundry={c.CanSupportLaundering} Risk={c.IsHighRiskIfUsedIllegally}",
                st);
        }
#endif

        public static float SampleCrimeMetric(BuildingCrimeProfileData c, BuildingCrimeDebugMetric m)
        {
            if (c == null)
                return 0f;
            return m switch
            {
                BuildingCrimeDebugMetric.FrontBusiness => c.FrontBusinessPotential,
                BuildingCrimeDebugMetric.Storage => c.StoragePotential,
                BuildingCrimeDebugMetric.Backroom => c.BackroomPotential,
                BuildingCrimeDebugMetric.Laundering => c.LaunderingPotential,
                BuildingCrimeDebugMetric.Extortion => c.ExtortionPotential,
                BuildingCrimeDebugMetric.BlackMarket => c.BlackMarketSuitability,
                BuildingCrimeDebugMetric.Meeting => c.MeetingPotential,
                BuildingCrimeDebugMetric.PoliceVisibility => c.PoliceVisibility,
                BuildingCrimeDebugMetric.NeighborhoodInfluence => c.NeighborhoodInfluenceValue,
                BuildingCrimeDebugMetric.Logistics => c.LogisticsValue,
                _ => 0f
            };
        }

        // --- Defaults when no ScriptableObject assigned ---

        void DrawBoundaryDefault()
        {
            Gizmos.color = Color.white;
            if (_city.MacroBoundary.Vertices.Count >= 3)
                DrawPolygonOpenOrClosed(_city.MacroBoundary.Vertices, closed: true);
            else
                DrawRectangle(_city.Boundary.Min, _city.Boundary.Max);
        }

        void DrawMacroFeaturesDefault()
        {
            foreach (MacroFeatureData f in _city.MacroFeatures)
            {
                Gizmos.color = f.Kind switch
                {
                    MacroFeatureKind.Coastline => new Color(0.2f, 0.55f, 0.85f, 0.9f),
                    MacroFeatureKind.River => new Color(0.15f, 0.4f, 0.9f, 0.9f),
                    MacroFeatureKind.RailCorridor => new Color(0.4f, 0.3f, 0.2f, 0.9f),
                    _ => Color.gray
                };
                DrawPolylineStrip(f.Path);
            }
        }

        void DrawMacroAnchorsDefault()
        {
            Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.95f);
            float r = 2f * metersPerPlanUnit;
            foreach (MacroAnchorPointData a in _city.MacroAnchors)
                Gizmos.DrawSphere(PlanToWorld(a.Position), r);
        }

        void DrawRoadsDefault()
        {
            float h = 0.05f;
            foreach (RoadEdge e in _city.RoadEdges)
            {
                RoadNode a = FindNode(e.FromNodeId);
                RoadNode b = FindNode(e.ToNodeId);
                if (a == null || b == null) continue;
                Gizmos.color = e.Kind == RoadEdgeKind.Major
                    ? new Color(0.95f, 0.55f, 0.2f, 0.95f)
                    : new Color(0.55f, 0.62f, 0.72f, 0.85f);
                Vector3 p0 = PlanToWorld(a.Position) + Vector3.up * h;
                Vector3 p1 = PlanToWorld(b.Position) + Vector3.up * h;
                Gizmos.DrawLine(p0, p1);
            }
        }

        void DrawDistrictsDefault()
        {
            float lift = 0.04f * metersPerPlanUnit;
            foreach (DistrictData d in _city.Districts)
            {
                if (d.Outline == null || d.Outline.Count < 3)
                    continue;
                Gizmos.color = DistrictTypeColorDefault(d.Kind);
                DrawPolygonOpenOrClosedLifted(d.Outline, closed: true, lift);
            }
        }

        void DrawBlocksDefault()
        {
            float lift = 0.03f * metersPerPlanUnit;
            foreach (BlockData b in _city.Blocks)
            {
                Gizmos.color = DistrictTypeColorDefault(b.DistrictKind);
                DrawRectangleLifted(b.Min, b.Max, lift);
            }
        }

        void DrawLotsDefault()
        {
            if (_city.Lots.Count == 0) return;
            Gizmos.color = new Color(0.35f, 0.85f, 0.45f, 0.55f);
            float lift = 0.06f * metersPerPlanUnit;
            foreach (LotData lot in _city.Lots)
                DrawRectangleLifted(lot.Min, lot.Max, lift);
        }

        void DrawInstitutionsDefault()
        {
            if (_city.Institutions.Count == 0) return;
            float lift = 0.1f * metersPerPlanUnit;
            float r = 2f * metersPerPlanUnit;
            foreach (InstitutionData a in _city.Institutions)
            {
                Gizmos.color = InstitutionKindColorDefault(a.Kind);
                Gizmos.DrawSphere(PlanToWorld(a.Position) + Vector3.up * lift, r);
            }
        }

        void DrawRegularBuildingsDefault()
        {
            if (_city.Buildings.Count == 0) return;
            float lift = 0.08f * metersPerPlanUnit;
            float r = 1.1f * metersPerPlanUnit;
            foreach (BuildingData b in _city.Buildings)
            {
                Gizmos.color = BuildingCategoryColorDefault(b.Category);
                Gizmos.DrawSphere(PlanToWorld(b.FootprintCenter) + Vector3.up * lift, b.IsUndeveloped ? r * 0.55f : r);
            }
        }

        static string FindDistrictName(CityData city, int districtId)
        {
            for (int i = 0; i < city.Districts.Count; i++)
            {
                if (city.Districts[i].Id == districtId)
                    return city.Districts[i].Name ?? "District";
            }

            return "?";
        }

        static Color InstitutionKindColor(InstitutionKind k, CityDebugSettings s)
        {
            switch (k)
            {
                case InstitutionKind.PoliceStation: return s.institutionColorPolice;
                case InstitutionKind.Courthouse: return s.institutionColorCourthouse;
                case InstitutionKind.Prison: return s.institutionColorPrison;
                case InstitutionKind.Hospital: return s.institutionColorHospital;
                case InstitutionKind.CityHall: return s.institutionColorCityHall;
                case InstitutionKind.TaxOffice: return s.institutionColorTax;
                case InstitutionKind.FederalOffice: return s.institutionColorFederal;
                case InstitutionKind.Bank: return s.institutionColorBank;
                case InstitutionKind.RailStation: return s.institutionColorRail;
                case InstitutionKind.DockAuthority: return s.institutionColorDock;
                default: return s.anchorInstitutionColor;
            }
        }

        static Color BuildingCategoryColor(BuildingCategory c, CityDebugSettings s)
        {
            switch (c)
            {
                case BuildingCategory.Undeveloped: return s.buildingColorUndeveloped;
                case BuildingCategory.Commercial: return s.buildingColorCommercial;
                case BuildingCategory.Industrial: return s.buildingColorIndustrial;
                case BuildingCategory.Residential: return s.buildingColorResidential;
                case BuildingCategory.Civic: return s.buildingColorCivic;
                case BuildingCategory.MixedUse: return s.buildingColorMixedUse;
                default: return s.buildingColorCommercial;
            }
        }

        static Color BuildingCategoryColorDefault(BuildingCategory c)
        {
            switch (c)
            {
                case BuildingCategory.Undeveloped: return new Color(0.55f, 0.58f, 0.52f, 0.8f);
                case BuildingCategory.Commercial: return new Color(0.85f, 0.55f, 0.35f, 0.9f);
                case BuildingCategory.Industrial: return new Color(0.5f, 0.48f, 0.45f, 0.9f);
                case BuildingCategory.Residential: return new Color(0.45f, 0.7f, 0.85f, 0.9f);
                case BuildingCategory.Civic: return new Color(0.55f, 0.75f, 0.5f, 0.9f);
                case BuildingCategory.MixedUse: return new Color(0.75f, 0.55f, 0.8f, 0.9f);
                default: return new Color(0.8f, 0.8f, 0.8f, 0.85f);
            }
        }

        static Color InstitutionKindColorDefault(InstitutionKind k)
        {
            switch (k)
            {
                case InstitutionKind.PoliceStation: return new Color(0.35f, 0.45f, 0.85f, 0.95f);
                case InstitutionKind.Courthouse: return new Color(0.55f, 0.35f, 0.75f, 0.95f);
                case InstitutionKind.Prison: return new Color(0.45f, 0.45f, 0.5f, 0.95f);
                case InstitutionKind.Hospital: return new Color(0.9f, 0.35f, 0.4f, 0.95f);
                case InstitutionKind.CityHall: return new Color(0.95f, 0.75f, 0.3f, 0.95f);
                case InstitutionKind.TaxOffice: return new Color(0.5f, 0.7f, 0.45f, 0.95f);
                case InstitutionKind.FederalOffice: return new Color(0.35f, 0.55f, 0.65f, 0.95f);
                case InstitutionKind.Bank: return new Color(0.85f, 0.75f, 0.35f, 0.95f);
                case InstitutionKind.RailStation: return new Color(0.65f, 0.4f, 0.25f, 0.95f);
                case InstitutionKind.DockAuthority: return new Color(0.25f, 0.55f, 0.85f, 0.95f);
                default: return new Color(0.95f, 0.4f, 0.85f, 0.9f);
            }
        }

        // --- Helpers ---

        static Color DistrictTypeColor(DistrictKind k, CityDebugSettings s)
        {
            switch (k)
            {
                case DistrictKind.DowntownCommercial: return s.districtColorDowntown;
                case DistrictKind.Industrial: return s.districtColorIndustrial;
                case DistrictKind.WorkingClass: return s.districtColorWorkingClass;
                case DistrictKind.Residential: return s.districtColorResidential;
                case DistrictKind.Wealthy: return s.districtColorWealthy;
                case DistrictKind.DocksPort: return s.districtColorDocks;
                case DistrictKind.FringeOuterEdge: return s.districtColorFringe;
                default: return s.blockColor;
            }
        }

        static Color DistrictTypeColorDefault(DistrictKind k)
        {
            switch (k)
            {
                case DistrictKind.DowntownCommercial: return new Color(0.95f, 0.75f, 0.35f, 0.5f);
                case DistrictKind.Industrial: return new Color(0.55f, 0.5f, 0.48f, 0.5f);
                case DistrictKind.WorkingClass: return new Color(0.65f, 0.45f, 0.55f, 0.48f);
                case DistrictKind.Residential: return new Color(0.45f, 0.65f, 0.85f, 0.48f);
                case DistrictKind.Wealthy: return new Color(0.75f, 0.85f, 0.55f, 0.48f);
                case DistrictKind.DocksPort: return new Color(0.35f, 0.55f, 0.8f, 0.5f);
                case DistrictKind.FringeOuterEdge: return new Color(0.55f, 0.55f, 0.6f, 0.42f);
                default: return new Color(0.85f, 0.85f, 0.88f, 0.35f);
            }
        }

        void DrawRectangle(Vector2 min, Vector2 max)
        {
            DrawRectangleLifted(min, max, 0f);
        }

        void DrawRectangleLifted(Vector2 min, Vector2 max, float worldYExtra)
        {
            Vector3 u = Vector3.up * worldYExtra;
            Vector3 c0 = PlanToWorld(new Vector2(min.x, min.y)) + u;
            Vector3 c1 = PlanToWorld(new Vector2(max.x, min.y)) + u;
            Vector3 c2 = PlanToWorld(new Vector2(max.x, max.y)) + u;
            Vector3 c3 = PlanToWorld(new Vector2(min.x, max.y)) + u;
            Gizmos.DrawLine(c0, c1);
            Gizmos.DrawLine(c1, c2);
            Gizmos.DrawLine(c2, c3);
            Gizmos.DrawLine(c3, c0);
        }

        void DrawPolygonOpenOrClosed(System.Collections.Generic.List<Vector2> outline, bool closed)
        {
            DrawPolygonOpenOrClosedLifted(outline, closed, 0f);
        }

        void DrawPolygonOpenOrClosedLifted(System.Collections.Generic.List<Vector2> outline, bool closed, float worldYExtra)
        {
            if (outline == null || outline.Count < 2) return;
            Vector3 u = Vector3.up * worldYExtra;
            int n = closed ? outline.Count : outline.Count - 1;
            for (int i = 0; i < n; i++)
            {
                Vector2 a = outline[i];
                Vector2 b = outline[(i + 1) % outline.Count];
                Gizmos.DrawLine(PlanToWorld(a) + u, PlanToWorld(b) + u);
            }
        }

        void DrawPolylineStrip(System.Collections.Generic.List<Vector2> path)
        {
            if (path == null || path.Count < 2) return;
            for (int i = 0; i < path.Count - 1; i++)
                Gizmos.DrawLine(PlanToWorld(path[i]), PlanToWorld(path[i + 1]));
        }

        void DrawThickPolyline(System.Collections.Generic.List<Vector2> path, float halfWidthWorld)
        {
            if (path == null || path.Count < 2) return;
            halfWidthWorld = Mathf.Max(0.02f, halfWidthWorld);
            Vector3 up = transform.TransformDirection(Vector3.up).normalized;
            for (int i = 0; i < path.Count - 1; i++)
            {
                Vector3 a = PlanToWorld(path[i]);
                Vector3 b = PlanToWorld(path[i + 1]);
                Vector3 seg = b - a;
                if (seg.sqrMagnitude < 1e-8f)
                    continue;
                Vector3 side = Vector3.Cross(up, seg.normalized).normalized * halfWidthWorld;
                Vector3 aL = a + side, aR = a - side;
                Vector3 bL = b + side, bR = b - side;
                Gizmos.DrawLine(aL, bL);
                Gizmos.DrawLine(aR, bR);
                Gizmos.DrawLine(aL, aR);
                Gizmos.DrawLine(bL, bR);
            }
        }

        RoadNode FindNode(int id)
        {
            for (int i = 0; i < _city.RoadNodes.Count; i++)
            {
                if (_city.RoadNodes[i].Id == id)
                    return _city.RoadNodes[i];
            }
            return null;
        }
    }
}
