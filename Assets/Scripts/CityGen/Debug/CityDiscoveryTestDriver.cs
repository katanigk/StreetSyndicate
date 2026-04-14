using FamilyBusiness.CityGen.Core;
using FamilyBusiness.CityGen.Data;
using FamilyBusiness.CityGen.Discovery;
using UnityEngine;

namespace FamilyBusiness.CityGen.Debug
{
    /// <summary>
    /// Batch 10: editor/play-mode hooks to exercise starting and movement discovery (iteration only).
    /// </summary>
    [ExecuteAlways]
    public sealed class CityDiscoveryTestDriver : MonoBehaviour
    {
        [SerializeField] CityDebugRenderer cityRenderer;
        [SerializeField] CityGenerationConfig generationConfig;
        [Tooltip("Plan-space position (city generator cells), XY on ground plane.")]
        [SerializeField] Vector2 testPlanPositionCells = new Vector2(64f, 64f);
        [SerializeField] float nudgeStepCells = 6f;
        [SerializeField] float metersPerPlanUnit = 1f;

        MovementDiscoveryService _movement = new MovementDiscoveryService();

        public Vector2 TestPlanPositionCells
        {
            get => testPlanPositionCells;
            set => testPlanPositionCells = value;
        }

        CityData City => cityRenderer != null ? cityRenderer.City : null;

        [ContextMenu("Discovery/Reset to generated defaults")]
        public void ResetDiscoveryToDefaults()
        {
            CityData c = City;
            if (c == null || generationConfig == null)
            {
                UnityEngine.Debug.LogWarning("[CityDiscoveryTestDriver] Need CityDebugRenderer with City + CityGenerationConfig.");
                return;
            }

            CityDiscoveryReset.ReapplyDefaults(c, generationConfig);
            UnityEngine.Debug.Log("[CityDiscoveryTestDriver] Discovery reset.");
        }

        [ContextMenu("Discovery/Apply starting reveal at test position")]
        public void ApplyStartingRevealAtTestPosition()
        {
            CityData c = City;
            if (c == null || generationConfig == null)
            {
                UnityEngine.Debug.LogWarning("[CityDiscoveryTestDriver] Need CityDebugRenderer with City + CityGenerationConfig.");
                return;
            }

            StartingRevealApplier.Apply(c, generationConfig, testPlanPositionCells);
            UnityEngine.Debug.Log($"[CityDiscoveryTestDriver] Starting reveal @ plan {testPlanPositionCells}");
        }

        [ContextMenu("Discovery/Apply movement reveal at test position")]
        public void ApplyMovementRevealAtTestPosition()
        {
            CityData c = City;
            if (c == null || generationConfig == null)
            {
                UnityEngine.Debug.LogWarning("[CityDiscoveryTestDriver] Need CityDebugRenderer with City + CityGenerationConfig.");
                return;
            }

            _movement.RevealAroundPlanPosition(c, generationConfig, testPlanPositionCells);
            UnityEngine.Debug.Log($"[CityDiscoveryTestDriver] Movement reveal @ plan {testPlanPositionCells}");
        }

        [ContextMenu("Discovery/Nudge test position +X")]
        public void NudgeTestX() => testPlanPositionCells += new Vector2(nudgeStepCells, 0f);

        [ContextMenu("Discovery/Nudge test position +Y")]
        public void NudgeTestY() => testPlanPositionCells += new Vector2(0f, nudgeStepCells);

        void OnDrawGizmos()
        {
            if (!drawTestPositionGizmo)
                return;
            Vector3 world = transform.TransformPoint(new Vector3(
                testPlanPositionCells.x * metersPerPlanUnit,
                testPlanGizmoHeight,
                testPlanPositionCells.y * metersPerPlanUnit));
            Gizmos.color = new Color(1f, 0.92f, 0.2f, 0.95f);
            Gizmos.DrawSphere(world, testPlanMarkerRadius * metersPerPlanUnit);

            if (!drawRevealRadiusGizmos || generationConfig == null)
                return;

            float m = metersPerPlanUnit;
            DrawDisc(world, generationConfig.movementDistrictRevealRadiusCells * m,
                new Color(0.3f, 0.85f, 1f, 0.35f));
            DrawDisc(world, generationConfig.movementBuildingRevealRadiusCells * m,
                new Color(0.5f, 1f, 0.45f, 0.3f));
            DrawDisc(world, generationConfig.movementInstitutionRevealRadiusCells * m,
                new Color(1f, 0.55f, 0.35f, 0.3f));

            if (drawStartingRevealRadiusGizmos)
            {
                DrawDisc(world, generationConfig.startingDistrictRevealRadiusCells * m,
                    new Color(0.85f, 0.35f, 1f, 0.28f));
                DrawDisc(world, generationConfig.startingBuildingRevealRadiusCells * m,
                    new Color(0.55f, 0.95f, 0.4f, 0.22f));
                DrawDisc(world, generationConfig.startingInstitutionRevealRadiusCells * m,
                    new Color(1f, 0.4f, 0.55f, 0.22f));
            }
        }

        [SerializeField] bool drawTestPositionGizmo = true;
        [SerializeField] bool drawRevealRadiusGizmos = true;
        [SerializeField] bool drawStartingRevealRadiusGizmos;
        [SerializeField] float testPlanGizmoHeight = 0.15f;
        [SerializeField] float testPlanMarkerRadius = 2f;

        static void DrawDisc(Vector3 centerWorld, float radiusWorld, Color c)
        {
            if (radiusWorld <= 0.01f)
                return;
            Gizmos.color = c;
            const int n = 36;
            Vector3 prev = centerWorld + new Vector3(radiusWorld, 0f, 0f);
            for (int i = 1; i <= n; i++)
            {
                float t = i / (float)n * Mathf.PI * 2f;
                Vector3 next = centerWorld + new Vector3(Mathf.Cos(t) * radiusWorld, 0f, Mathf.Sin(t) * radiusWorld);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
    }
}
