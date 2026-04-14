using FamilyBusiness.CityGen.Core;
using FamilyBusiness.CityGen.Data;
using FamilyBusiness.CityGen.Discovery;
using FamilyBusiness.CityGen.Generators;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FamilyBusiness.CityGen.Debug
{
    /// <summary>
    /// Batch 11: preview gang spawn selection and starting reveal against a <see cref="CityDebugRenderer"/> city.
    /// </summary>
    [ExecuteAlways]
    public sealed class StartingGangSpawnTestDriver : MonoBehaviour
    {
        [SerializeField] CityDebugRenderer cityRenderer;
        [SerializeField] CityGenerationConfig generationConfig;
        [SerializeField] float metersPerPlanUnit = 1f;
        [SerializeField] float worldSpawnY;
        [SerializeField] bool drawStartMarker = true;
        [SerializeField] bool drawMemberMarkers = true;
        [SerializeField] float startMarkerRadius = 2.2f;
        [SerializeField] float memberMarkerRadius = 1f;
        [SerializeField] bool drawSpawnLabels;

        StartingGangSpawnData _previewSpawn;

        CityData City => cityRenderer != null ? cityRenderer.City : null;

        [ContextMenu("Gang spawn / Select only (no reveal)")]
        public void SelectSpawnOnly()
        {
            CityData c = City;
            if (c == null || generationConfig == null)
            {
                UnityEngine.Debug.LogWarning("[StartingGangSpawnTestDriver] Assign CityDebugRenderer (with city) and config.", this);
                return;
            }

            _previewSpawn = StartingGangSpawnSelector.Select(c, generationConfig, metersPerPlanUnit, worldSpawnY);
            UnityEngine.Debug.Log(
                $"[StartingGangSpawnTestDriver] Spawn profile={_previewSpawn.SpawnProfile} district={_previewSpawn.StartDistrictKind} building={_previewSpawn.UsesBuildingBasedSpawn} plan={_previewSpawn.StartPlanPosition}");
        }

        [ContextMenu("Gang spawn / Build world entry (select + starting reveal)")]
        public void BuildWorldEntryWithReveal()
        {
            CityData c = City;
            if (c == null || generationConfig == null)
            {
                UnityEngine.Debug.LogWarning("[StartingGangSpawnTestDriver] Assign CityDebugRenderer (with city) and config.", this);
                return;
            }

            _previewSpawn = CityWorldEntryBuilder.BuildWorldEntry(c, generationConfig, metersPerPlanUnit, worldSpawnY,
                applyStartingReveal: true, forceReveal: true);
            UnityEngine.Debug.Log($"[StartingGangSpawnTestDriver] World entry @ plan {_previewSpawn.StartPlanPosition}");
        }

        [ContextMenu("Gang spawn / Apply starting reveal at cached / last spawn")]
        public void ApplyStartingRevealAtPreview()
        {
            CityData c = City;
            if (c == null || generationConfig == null)
            {
                UnityEngine.Debug.LogWarning("[StartingGangSpawnTestDriver] Assign CityDebugRenderer (with city) and config.", this);
                return;
            }

            StartingGangSpawnData s = _previewSpawn ?? c.LastStartingGangSpawn;
            if (s == null)
            {
                UnityEngine.Debug.LogWarning("[StartingGangSpawnTestDriver] Run Select or Build first.");
                return;
            }

            StartingRevealApplier.Apply(c, generationConfig, s.StartPlanPosition);
            c.StartingRevealAppliedAtWorldEntry = true;
            UnityEngine.Debug.Log($"[StartingGangSpawnTestDriver] Starting reveal @ {s.StartPlanPosition}");
        }

        void OnDrawGizmos()
        {
            StartingGangSpawnData s = _previewSpawn ?? City?.LastStartingGangSpawn;
            if (s == null)
                return;

            Matrix4x4 old = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;

            if (drawStartMarker)
            {
                Gizmos.color = new Color(0.2f, 0.95f, 0.35f, 0.95f);
                Vector3 w = LocalWorld(s.StartWorldPosition);
                Gizmos.DrawSphere(w, startMarkerRadius * metersPerPlanUnit);
            }

            if (drawMemberMarkers && s.GangMemberWorldPositions.Count > 0)
            {
                Gizmos.color = new Color(0.35f, 0.75f, 1f, 0.9f);
                float r = memberMarkerRadius * metersPerPlanUnit;
                for (int i = 0; i < s.GangMemberWorldPositions.Count; i++)
                {
                    Vector3 p = LocalWorld(s.GangMemberWorldPositions[i]);
                    Gizmos.DrawSphere(p, r);
                }
            }

            Gizmos.matrix = old;

#if UNITY_EDITOR
            if (drawSpawnLabels)
            {
                string text =
                    $"{s.StartDistrictKind}\n{s.SpawnProfile}\nBuilding: {(s.UsesBuildingBasedSpawn ? "yes" : "lot/fallback")}";
                Handles.Label(LocalWorld(s.StartWorldPosition) + Vector3.up * (2f * metersPerPlanUnit), text);
            }
#endif
        }

        Vector3 LocalWorld(Vector3 worldOffset) =>
            transform.TransformPoint(new Vector3(worldOffset.x, worldOffset.y, worldOffset.z));
    }
}
