using System.Text;
using FamilyBusiness.CityGen.Data;
using FamilyBusiness.CityGen.Government;
using FamilyBusiness.CityGen.Government.Windows;
using UnityEngine;

namespace FamilyBusiness.CityGen.Debug
{
    /// <summary>Batch 13: build and log government window view-models from a preview city.</summary>
    [ExecuteAlways]
    public sealed class GovernmentWindowModelTestDriver : MonoBehaviour
    {
        public enum TestSystem
        {
            Police = 0,
            Federal = 1,
            Court = 2
        }

        [SerializeField] CityDebugRenderer cityRenderer;
        [SerializeField] TestSystem system = TestSystem.Police;
        [SerializeField] PoliceWindowMode policeMode = PoliceWindowMode.Deployment;
        [SerializeField] FederalWindowMode federalMode = FederalWindowMode.Deployment;
        [SerializeField] CourtWindowMode courtMode = CourtWindowMode.Proceedings;
        [SerializeField] string selectedInstitutionStableId;

        [ContextMenu("Government windows / Build model + log summary")]
        public void BuildAndLog()
        {
            CityData c = cityRenderer != null ? cityRenderer.City : null;
            if (c == null)
            {
                UnityEngine.Debug.LogWarning("[GovernmentWindowModelTestDriver] Assign CityDebugRenderer with a city.", this);
                return;
            }

            var sb = new StringBuilder();
            switch (system)
            {
                case TestSystem.Police:
                {
                    PoliceWindowStateModel m = PoliceWindowModelBuilder.Build(c, policeMode, selectedInstitutionStableId);
                    sb.AppendLine($"[Police] mode={policeMode} left={m.LeftItems.Count} selected={m.SelectedItemId}");
                    sb.AppendLine(
                        $"  center: {(m.DeploymentDetail != null ? m.DeploymentDetail.EffectiveTitle : m.CenterFallbackTitle)}");
                    sb.AppendLine($"  placeholder={m.UsesCenterPlaceholder} actions={m.RightActions.Count}");
                    break;
                }
                case TestSystem.Federal:
                {
                    FederalWindowStateModel m = FederalWindowModelBuilder.Build(c, federalMode, selectedInstitutionStableId);
                    sb.AppendLine($"[Federal] mode={federalMode} left={m.LeftItems.Count} selected={m.SelectedItemId}");
                    sb.AppendLine(
                        $"  center: {(m.DeploymentDetail != null ? m.DeploymentDetail.EffectiveTitle : m.CenterFallbackTitle)}");
                    sb.AppendLine($"  placeholder={m.UsesCenterPlaceholder} actions={m.RightActions.Count}");
                    break;
                }
                case TestSystem.Court:
                {
                    CourtWindowStateModel m = CourtWindowModelBuilder.Build(c, courtMode, selectedInstitutionStableId);
                    sb.AppendLine($"[Court] mode={courtMode} left={m.LeftItems.Count} reserved={m.IsReservedSlotMode}");
                    sb.AppendLine($"  center: {m.CenterFallbackTitle}");
                    sb.AppendLine($"  actions={m.RightActions.Count}");
                    break;
                }
            }

            UnityEngine.Debug.Log(sb.ToString(), this);
        }
    }
}
