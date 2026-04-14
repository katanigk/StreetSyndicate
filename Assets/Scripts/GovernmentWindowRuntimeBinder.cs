using System;
using System.Collections.Generic;
using System.Text;
using FamilyBusiness.CityGen.Data;
using FamilyBusiness.CityGen.Government.Windows;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Batch 14: binds <see cref="GovernmentInstitutionShellView"/> to Batch 13 window models (discovery-safe strings only).
/// No simulation — UI population and optional click callbacks only.
/// </summary>
public static class GovernmentWindowRuntimeBinder
{
    /// <summary>Invoked when a right-pane action row is clicked (key from model).</summary>
    public static event Action<string> ActionClicked;

    public static void ApplyPoliceShell(GovernmentInstitutionShellView shell, CityData city, PoliceWindowMode mode,
        string selectedStableId, Action<string> onLeftItemSelected)
    {
        if (shell == null)
            return;

        PoliceWindowStateModel model = PoliceWindowModelBuilder.Build(city, mode, selectedStableId);
        shell.ClearLeftList();
        shell.ClearRightActions();

        if (shell.LeftPanelTitle != null)
            shell.LeftPanelTitle.text = PoliceLeftHeader(mode);

        FillLeftList(shell, model.LeftItems, onLeftItemSelected);
        ApplyCenterFromPolice(shell, model);
        FillRightActions(shell, model.RightActions);
    }

    public static void ApplyFederalShell(GovernmentInstitutionShellView shell, CityData city, FederalWindowMode mode,
        string selectedStableId, Action<string> onLeftItemSelected)
    {
        if (shell == null)
            return;

        FederalWindowStateModel model = FederalWindowModelBuilder.Build(city, mode, selectedStableId);
        shell.ClearLeftList();
        shell.ClearRightActions();

        if (shell.LeftPanelTitle != null)
            shell.LeftPanelTitle.text = FederalLeftHeader(mode);

        FillLeftList(shell, model.LeftItems, onLeftItemSelected);
        ApplyCenterFromFederal(shell, model);
        FillRightActions(shell, model.RightActions);
    }

    public static void ApplyCourtShell(GovernmentInstitutionShellView shell, CityData city, CourtWindowMode mode)
    {
        if (shell == null)
            return;

        CourtWindowStateModel model = CourtWindowModelBuilder.Build(city, mode, null);
        shell.ClearLeftList();
        shell.ClearRightActions();

        if (shell.LeftPanelTitle != null)
            shell.LeftPanelTitle.text = CourtLeftHeader(mode, model.IsReservedSlotMode);

        FillLeftList(shell, model.LeftItems, null);
        ApplyCenterPlaceholder(shell, model.CenterFallbackTitle, model.CenterFallbackBody);
        FillRightActions(shell, model.RightActions);
    }

    static string PoliceLeftHeader(PoliceWindowMode m) =>
        m switch
        {
            PoliceWindowMode.Deployment => "<b>Police facilities</b>",
            PoliceWindowMode.Personnel => "<b>Personnel</b>",
            PoliceWindowMode.Cases => "<b>Cases</b>",
            PoliceWindowMode.Pressure => "<b>Pressure</b>",
            _ => "<b>Police</b>"
        };

    static string FederalLeftHeader(FederalWindowMode m) =>
        m switch
        {
            FederalWindowMode.Deployment => "<b>Federal facilities</b>",
            FederalWindowMode.Personnel => "<b>Personnel</b>",
            FederalWindowMode.Cases => "<b>Cases</b>",
            FederalWindowMode.Interest => "<b>Interest</b>",
            _ => "<b>Federal</b>"
        };

    static string CourtLeftHeader(CourtWindowMode m, bool reserved) =>
        m switch
        {
            CourtWindowMode.Proceedings => "<b>Proceedings</b>",
            CourtWindowMode.Personnel => "<b>Personnel</b>",
            CourtWindowMode.Reserved1 or CourtWindowMode.Reserved2 when reserved => "<b>Reserved</b>",
            _ => "<b>Court</b>"
        };

    static void ApplyCenterFromPolice(GovernmentInstitutionShellView shell, PoliceWindowStateModel model)
    {
        if (model.UsesCenterPlaceholder || model.DeploymentDetail == null)
            ApplyCenterPlaceholder(shell, model.CenterFallbackTitle, model.CenterFallbackBody);
        else
            ApplyCenterDeployment(shell, model.DeploymentDetail);
    }

    static void ApplyCenterFromFederal(GovernmentInstitutionShellView shell, FederalWindowStateModel model)
    {
        if (model.UsesCenterPlaceholder || model.DeploymentDetail == null)
            ApplyCenterPlaceholder(shell, model.CenterFallbackTitle, model.CenterFallbackBody);
        else
            ApplyCenterDeployment(shell, model.DeploymentDetail);
    }

    static void ApplyCenterPlaceholder(GovernmentInstitutionShellView shell, string title, string body)
    {
        if (shell.CenterBody == null)
            return;
        string t = string.IsNullOrEmpty(title) ? "—" : title;
        string b = string.IsNullOrEmpty(body) ? "" : body;
        shell.CenterBody.text = "<b>" + t + "</b>\n\n" + b;
    }

    static void ApplyCenterDeployment(GovernmentInstitutionShellView shell, GovernmentWindowFacilityDeploymentDetailModel d)
    {
        if (shell.CenterBody == null || d == null)
            return;

        var sb = new StringBuilder();
        sb.Append("<b>").Append(d.EffectiveTitle).Append("</b>\n");
        sb.Append("<size=90%>").Append(d.FacilityKindDisplay).Append("</size>\n\n");
        sb.Append("<b>District</b>\n").Append(d.DistrictDisplay).Append("\n\n");
        sb.Append("• Map pin: <b>").Append(d.IsVisibleOnMapNow ? "Yes" : "No").Append("</b>\n");
        sb.Append("• Rumor-only: <b>").Append(d.IsRumorOnly ? "Yes" : "No").Append("</b>\n");
        sb.Append("• Low-profile: <b>").Append(d.IsLowProfile ? "Yes" : "No").Append("</b>\n");
        sb.Append("• Exact type shown: <b>").Append(d.CanShowExactKind ? "Yes" : "No").Append("</b>\n");
        sb.Append("• Detailed intel: <b>").Append(d.CanShowDetailedInfo ? "Yes" : "No").Append("</b>\n\n");
        sb.Append("<b>Internal</b>\n<size=90%>").Append(d.SubDepartmentsPlaceholder).Append("</size>");
        shell.CenterBody.text = sb.ToString();
    }

    static void FillLeftList(GovernmentInstitutionShellView shell, List<GovernmentWindowListItemModel> items,
        Action<string> onLeftItemSelected)
    {
        if (shell.LeftContent == null)
            return;

        for (int i = 0; i < items.Count; i++)
        {
            GovernmentWindowListItemModel item = items[i];
            string tier = TierLabel(item.DiscoveryTier);
            string label = "<b>" + item.DisplayLabel + "</b>\n<size=85%><color=#A8ABB3>" + item.Subtitle +
                           "</color></size>\n<size=75%><color=#7C7F88>" + tier + "</size>";

            Button b = CreateShellRowButton(shell.LeftContent, label);
            Image img = b.GetComponent<Image>();
            if (img != null && item.IsSelected)
                img.color = PlanningUiButtonStyle.TabSelectedFill;

            string cap = item.StableId;
            if (onLeftItemSelected != null)
            {
                b.onClick.AddListener(() => onLeftItemSelected.Invoke(cap));
            }

            shell.LeftListButtons.Add(b);
        }
    }

    static string TierLabel(GovernmentWindowListDiscoveryTier t) =>
        t switch
        {
            GovernmentWindowListDiscoveryTier.MapVisible => "Intel: map-visible",
            GovernmentWindowListDiscoveryTier.KnownWithoutMapPin => "Intel: known (no pin)",
            _ => "Intel: rumored"
        };

    static void FillRightActions(GovernmentInstitutionShellView shell, List<GovernmentWindowActionModel> actions)
    {
        if (shell.RightActionsRoot == null)
            return;

        for (int i = 0; i < actions.Count; i++)
        {
            GovernmentWindowActionModel a = actions[i];
            string label = a.DisplayLabel;
            if (!a.IsEnabled && !string.IsNullOrEmpty(a.DisabledReason))
                label += "\n<size=75%><color=#888888>" + a.DisabledReason + "</color></size>";

            Button b = CreateShellRowButton(shell.RightActionsRoot, label);
            b.interactable = a.IsEnabled;
            string key = a.ActionKey ?? "action";
            b.onClick.AddListener(() =>
            {
                Debug.Log("[GovernmentWindow] Action (stub): " + key);
                ActionClicked?.Invoke(key);
            });
            shell.RightActionButtons.Add(b);
        }
    }

    static Button CreateShellRowButton(Transform parent, string label)
    {
        string safeName = "Btn_GovBind_" + Mathf.Abs(string.IsNullOrEmpty(label) ? 0 : label.GetHashCode());
        GameObject go = new GameObject(safeName);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, 52f);

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 0f;
        le.flexibleWidth = 1f;
        le.minWidth = 0f;
        le.preferredHeight = 52f;
        le.minHeight = 52f;
        le.flexibleHeight = 0f;

        Image img = go.AddComponent<Image>();
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        PlanningUiButtonStyle.ApplyStandardRectButton(btn, img);
        go.AddComponent<ButtonPressScale>();

        GameObject textGo = new GameObject("Label");
        textGo.transform.SetParent(go.transform, false);
        RectTransform tr = textGo.AddComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        tmp.richText = true;
        tmp.text = label;
        tmp.fontSize = 15f;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.color = PlanningUiButtonStyle.LabelPrimary;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.margin = new Vector4(10f, 6f, 10f, 6f);
        return btn;
    }
}
