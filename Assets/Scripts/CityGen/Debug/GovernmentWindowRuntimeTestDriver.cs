using FamilyBusiness.CityGen.Data;
using FamilyBusiness.CityGen.Government.Windows;
using UnityEngine;

namespace FamilyBusiness.CityGen.Debug
{
    /// <summary>
    /// Batch 14: assigns <see cref="GovernmentRuntimeCitySource.ActiveCity"/> and optionally refreshes open government modals.
    /// Use with <see cref="CityDebugRenderer"/> + planning <c>Police</c> / <c>Federal Bureau</c> / <c>Court</c> windows.
    /// </summary>
    public sealed class GovernmentWindowRuntimeTestDriver : MonoBehaviour
    {
        [SerializeField] CityDebugRenderer cityRenderer;
        [SerializeField] PlanningShellController planningShell;

        [ContextMenu("Government UI / Bind ActiveCity from renderer")]
        public void BindActiveCityFromRenderer()
        {
            CityData city = cityRenderer != null ? cityRenderer.City : null;
            GovernmentRuntimeCitySource.ActiveCity = city;
            if (city == null || city.GovernmentData == null)
                UnityEngine.Debug.LogWarning(
                    "[GovernmentWindowRuntimeTestDriver] No city or GovernmentData — generate city and refresh extraction first.",
                    this);
            else
                UnityEngine.Debug.Log("[GovernmentWindowRuntimeTestDriver] GovernmentRuntimeCitySource.ActiveCity bound.", this);
        }

        [ContextMenu("Government UI / Bind city + refresh open government windows")]
        public void BindAndRefreshOpenWindows()
        {
            BindActiveCityFromRenderer();
            if (planningShell != null)
                planningShell.RefreshGovernmentWindowsIfOpen();
        }
    }
}
