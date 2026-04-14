using FamilyBusiness.CityGen.Data;
using FamilyBusiness.CityGen.Debug;
using UnityEngine;

namespace FamilyBusiness.CityGen.Core
{
    /// <summary>
    /// Lightweight in-scene preview: runs <see cref="CityGenerator"/> and pushes <see cref="CityData"/> to a <see cref="CityDebugRenderer"/>.
    /// Optional; safe to remove from scenes for builds.
    /// </summary>
    [ExecuteAlways]
    public sealed class CityMacroLayoutPreviewDriver : MonoBehaviour
    {
        [SerializeField] CityGenerationConfig config;
        [SerializeField] CityDebugRenderer debugRenderer;
        [SerializeField] bool generateOnPlay = true;
        [SerializeField] int previewSeed = 42_001;
        [SerializeField] bool useRandomSeed;
        [Tooltip("After generate, run Batch 11 world entry (gang start + StartingRevealApplier).")]
        [SerializeField] bool applyWorldEntryAfterGenerate = true;

        readonly CityGenerator _generator = new CityGenerator();

        void Start()
        {
            if (generateOnPlay && Application.isPlaying)
                Regenerate();
        }

        [ContextMenu("Regenerate macro preview")]
        public void Regenerate()
        {
            if (config == null)
            {
                UnityEngine.Debug.LogWarning("[CityMacroLayoutPreviewDriver] Assign a CityGenerationConfig.", this);
                return;
            }

            CitySeed seed = useRandomSeed ? CitySeed.FromRandom() : CitySeed.FromExplicit(previewSeed);
            CityData city = _generator.Generate(config, seed);

            if (applyWorldEntryAfterGenerate)
                CityWorldEntryBuilder.BuildWorldEntry(city, config, metersPerPlanUnit: 1f, worldSpawnY: 0f,
                    applyStartingReveal: true, forceReveal: true);

            if (debugRenderer != null)
                debugRenderer.SetCityData(city);
            else
                UnityEngine.Debug.LogWarning("[CityMacroLayoutPreviewDriver] Assign a CityDebugRenderer to see gizmos.", this);
        }
    }
}
