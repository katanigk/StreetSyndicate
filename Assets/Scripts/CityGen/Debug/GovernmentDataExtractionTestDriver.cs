using System.Collections.Generic;
using System.Text;
using FamilyBusiness.CityGen.Data;
using FamilyBusiness.CityGen.Government;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FamilyBusiness.CityGen.Debug
{
    /// <summary>Batch 12: inspect <see cref="CityGovernmentData"/> built from a preview city.</summary>
    [ExecuteAlways]
    public sealed class GovernmentDataExtractionTestDriver : MonoBehaviour
    {
        [SerializeField] CityDebugRenderer cityRenderer;
        [SerializeField] float metersPerPlanUnit = 1f;
        [SerializeField] float worldY;
        [SerializeField] bool drawFacilityMarkers = true;
        [SerializeField] float markerRadius = 1.8f;
        [SerializeField] bool drawLabelsBySystem;

        [ContextMenu("Government / Refresh extraction + log summary")]
        public void RefreshAndLog()
        {
            CityData c = cityRenderer != null ? cityRenderer.City : null;
            if (c == null)
            {
                UnityEngine.Debug.LogWarning("[GovernmentDataExtractionTestDriver] Assign CityDebugRenderer with a city.", this);
                return;
            }

            CityGovernmentData gov = GovernmentDataExtractor.Refresh(c, metersPerPlanUnit, worldY);
            var sb = new StringBuilder();
            sb.AppendLine($"[Government] Total facilities: {gov.AllFacilities.Count}");
            foreach (GovernmentSystemKind k in System.Enum.GetValues(typeof(GovernmentSystemKind)))
            {
                if (k == GovernmentSystemKind.Unknown)
                    continue;
                IReadOnlyList<GovernmentFacilityData> list = gov.GetFacilities(k);
                if (list.Count == 0)
                    continue;
                int vis = CountVisible(list);
                int intel = CountIntel(list);
                sb.AppendLine($"  {k}: count={list.Count} mapVisible={vis} intel≥rumor={intel}");
            }

            UnityEngine.Debug.Log(sb.ToString(), this);
        }

        static int CountVisible(IReadOnlyList<GovernmentFacilityData> list)
        {
            int n = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].IsVisibleOnMapNow)
                    n++;
            }

            return n;
        }

        static int CountIntel(IReadOnlyList<GovernmentFacilityData> list)
        {
            int n = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].PlayerHasIntelAtLeastRumored)
                    n++;
            }

            return n;
        }

        void OnDrawGizmos()
        {
            if (!drawFacilityMarkers)
                return;
            CityData c = cityRenderer != null ? cityRenderer.City : null;
            if (c?.GovernmentData == null)
                return;

            Matrix4x4 old = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            float r = markerRadius * metersPerPlanUnit;

            foreach (GovernmentFacilityData f in c.GovernmentData.AllFacilities)
            {
                Gizmos.color = ColorForSystem(f.GovernmentSystem);
                Vector3 w = transform.TransformPoint(f.WorldPosition);
                Gizmos.DrawSphere(w, r);
            }

            Gizmos.matrix = old;

#if UNITY_EDITOR
            if (drawLabelsBySystem)
            {
                foreach (GovernmentFacilityData f in c.GovernmentData.AllFacilities)
                {
                    Vector3 w = transform.TransformPoint(f.WorldPosition);
                    string line = $"{f.GovernmentSystem}\n{f.EffectiveDisplayNameForPlayer}";
                    Handles.Label(w + Vector3.up * (2f * metersPerPlanUnit), line);
                }
            }
#endif
        }

        static Color ColorForSystem(GovernmentSystemKind k) =>
            k switch
            {
                GovernmentSystemKind.Police => new Color(0.35f, 0.5f, 0.95f, 0.9f),
                GovernmentSystemKind.Federal => new Color(0.45f, 0.35f, 0.75f, 0.9f),
                GovernmentSystemKind.Court => new Color(0.75f, 0.45f, 0.35f, 0.9f),
                GovernmentSystemKind.Prison => new Color(0.4f, 0.4f, 0.45f, 0.9f),
                GovernmentSystemKind.CityHall => new Color(0.95f, 0.75f, 0.3f, 0.9f),
                GovernmentSystemKind.Hospital => new Color(0.95f, 0.35f, 0.4f, 0.9f),
                GovernmentSystemKind.Tax => new Color(0.45f, 0.75f, 0.45f, 0.9f),
                _ => Color.gray
            };
    }
}
