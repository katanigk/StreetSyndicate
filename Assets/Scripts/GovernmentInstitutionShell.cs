using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared layout for all government institution windows: left list, center detail, right actions, bottom mode bar.
/// See design doc: FAMILY BUSINESS — master government window template.
/// </summary>
public enum GovernmentBodyId
{
    Police = 0,
    CityHall = 1,
    Court = 2,
    Prison = 3,
    FederalPolice = 4,
    Hospital = 5,
    Tax = 6
}

/// <summary>How much the player/org has discovered about a list object (UI + logic hook).</summary>
public enum DiscoveryExposure
{
    Unknown = 0,
    Rumored = 1,
    Known = 2,
    PartiallyExposed = 3,
    DeeplyExposed = 4,
    InfiltratedOrControlled = 5
}

public sealed class GovernmentInstitutionShellView
{
    public RectTransform Root;
    public TextMeshProUGUI LeftPanelTitle;
    public ScrollRect LeftScroll;
    public RectTransform LeftContent;
    public ScrollRect CenterScroll;
    public TextMeshProUGUI CenterBody;
    public RectTransform RightActionsRoot;
    public RectTransform BottomModesRoot;
    public readonly List<Button> ModeButtons = new List<Button>();
    public readonly List<Button> LeftListButtons = new List<Button>();
    public readonly List<Button> RightActionButtons = new List<Button>();

    public void ClearLeftList()
    {
        for (int i = LeftListButtons.Count - 1; i >= 0; i--)
        {
            if (LeftListButtons[i] != null && LeftListButtons[i].gameObject != null)
                Object.Destroy(LeftListButtons[i].gameObject);
        }
        LeftListButtons.Clear();
    }

    public void ClearRightActions()
    {
        for (int i = RightActionButtons.Count - 1; i >= 0; i--)
        {
            if (RightActionButtons[i] != null && RightActionButtons[i].gameObject != null)
                Object.Destroy(RightActionButtons[i].gameObject);
        }
        RightActionButtons.Clear();
    }

    public void ClearModeButtons()
    {
        for (int i = ModeButtons.Count - 1; i >= 0; i--)
        {
            if (ModeButtons[i] != null && ModeButtons[i].gameObject != null)
                Object.Destroy(ModeButtons[i].gameObject);
        }
        ModeButtons.Clear();
    }
}

/// <summary>Runtime builder for the government window shell (UGUI).</summary>
public static class GovernmentInstitutionShell
{
    // Narrow side rails — most reading happens in the center pane.
    private const float LeftWidth = 196f;
    private const float RightWidth = 228f;
    private const float BottomModeBarHeight = 52f;

    public static GovernmentInstitutionShellView Build(Transform parent, Color panelTint)
    {
        var view = new GovernmentInstitutionShellView();

        GameObject root = new GameObject("GovernmentInstitutionShell");
        root.transform.SetParent(parent, false);
        RectTransform rootRt = root.AddComponent<RectTransform>();
        StretchFull(rootRt);
        view.Root = rootRt;

        // --- Bottom mode bar (anchored bottom) ---
        GameObject bottomGo = new GameObject("BottomModeBar");
        bottomGo.transform.SetParent(root.transform, false);
        RectTransform bottomRt = bottomGo.AddComponent<RectTransform>();
        bottomRt.anchorMin = new Vector2(0f, 0f);
        bottomRt.anchorMax = new Vector2(1f, 0f);
        bottomRt.pivot = new Vector2(0.5f, 0f);
        bottomRt.sizeDelta = new Vector2(0f, BottomModeBarHeight);
        bottomRt.anchoredPosition = Vector2.zero;
        view.BottomModesRoot = bottomRt;

        Image bottomBg = bottomGo.AddComponent<Image>();
        bottomBg.color = new Color(panelTint.r * 0.85f, panelTint.g * 0.85f, panelTint.b * 0.9f, 0.95f);
        bottomBg.raycastTarget = false;

        HorizontalLayoutGroup bottomH = bottomGo.AddComponent<HorizontalLayoutGroup>();
        bottomH.padding = new RectOffset(10, 10, 6, 6);
        bottomH.spacing = 8f;
        bottomH.childAlignment = TextAnchor.MiddleCenter;
        bottomH.childControlWidth = true;
        bottomH.childControlHeight = true;
        bottomH.childForceExpandWidth = true;
        bottomH.childForceExpandHeight = false;

        // --- Main tri-pane (above bottom bar) ---
        GameObject mainGo = new GameObject("MainTriPane");
        mainGo.transform.SetParent(root.transform, false);
        RectTransform mainRt = mainGo.AddComponent<RectTransform>();
        mainRt.anchorMin = Vector2.zero;
        mainRt.anchorMax = Vector2.one;
        mainRt.offsetMin = new Vector2(0f, BottomModeBarHeight + 4f);
        mainRt.offsetMax = Vector2.zero;

        HorizontalLayoutGroup mainH = mainGo.AddComponent<HorizontalLayoutGroup>();
        mainH.padding = new RectOffset(8, 8, 8, 8);
        mainH.spacing = 10f;
        mainH.childAlignment = TextAnchor.UpperLeft;
        // Must control child widths so the center column's flexibleWidth=1 actually absorbs free space;
        // otherwise fixed left/right stay minimal and the rest of the modal stays empty.
        mainH.childControlWidth = true;
        mainH.childControlHeight = true;
        mainH.childForceExpandWidth = false;
        mainH.childForceExpandHeight = true;

        LayoutElement leftLe;
        Image leftBg;
        VerticalLayoutGroup leftV;
        GameObject leftHeader;
        ScrollRect leftSr;
        RectTransform leftContent;

        GameObject leftCol = new GameObject("LeftColumn");
        leftCol.transform.SetParent(mainGo.transform, false);
        leftLe = leftCol.AddComponent<LayoutElement>();
        leftLe.preferredWidth = LeftWidth;
        leftLe.minWidth = LeftWidth;
        leftLe.flexibleWidth = 0f;
        leftLe.flexibleHeight = 1f;
        leftBg = leftCol.AddComponent<Image>();
        leftBg.color = new Color(0.06f, 0.07f, 0.09f, 0.55f);
        leftBg.raycastTarget = false;
        leftV = leftCol.AddComponent<VerticalLayoutGroup>();
        leftV.childAlignment = TextAnchor.UpperLeft;
        leftV.childControlWidth = true;
        leftV.childControlHeight = false;
        leftV.childForceExpandWidth = true;
        leftV.childForceExpandHeight = false;
        leftV.spacing = 6f;
        leftV.padding = new RectOffset(8, 8, 8, 8);

        leftHeader = new GameObject("LeftHeader");
        leftHeader.transform.SetParent(leftCol.transform, false);
        TextMeshProUGUI leftHdrTmp = leftHeader.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            leftHdrTmp.font = TMP_Settings.defaultFontAsset;
        leftHdrTmp.text = "<b>Selection</b>";
        leftHdrTmp.fontSize = 14f;
        leftHdrTmp.fontStyle = FontStyles.Bold;
        leftHdrTmp.color = new Color(0.88f, 0.9f, 0.94f, 0.95f);
        leftHdrTmp.alignment = TextAlignmentOptions.TopLeft;
        LayoutElement leftHdrLe = leftHeader.AddComponent<LayoutElement>();
        leftHdrLe.minHeight = 22f;
        leftHdrLe.preferredHeight = 22f;
        view.LeftPanelTitle = leftHdrTmp;

        GameObject leftScrollHost = new GameObject("LeftScrollHost");
        leftScrollHost.transform.SetParent(leftCol.transform, false);
        LayoutElement leftScrollLe = leftScrollHost.AddComponent<LayoutElement>();
        leftScrollLe.flexibleHeight = 1f;
        leftScrollLe.minHeight = 80f;

        leftSr = CreateVerticalScroll(leftScrollHost.transform, "LeftListScroll", out leftContent);
        RectTransform lsRt = leftSr.GetComponent<RectTransform>();
        StretchFull(lsRt);
        view.LeftScroll = leftSr;
        view.LeftContent = leftContent;
        VerticalLayoutGroup leftListV = leftContent.gameObject.AddComponent<VerticalLayoutGroup>();
        leftListV.spacing = 6f;
        leftListV.childAlignment = TextAnchor.UpperLeft;
        leftListV.childControlWidth = true;
        leftListV.childControlHeight = false;
        leftListV.childForceExpandWidth = true;
        leftListV.childForceExpandHeight = false;
        ContentSizeFitter leftListCf = leftContent.gameObject.AddComponent<ContentSizeFitter>();
        leftListCf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        leftListCf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Center column
        GameObject centerCol = new GameObject("CenterColumn");
        centerCol.transform.SetParent(mainGo.transform, false);
        LayoutElement cLe = centerCol.AddComponent<LayoutElement>();
        cLe.flexibleWidth = 1f;
        cLe.flexibleHeight = 1f;
        cLe.minWidth = 200f;
        Image cBg = centerCol.AddComponent<Image>();
        cBg.color = new Color(0.05f, 0.06f, 0.08f, 0.35f);
        cBg.raycastTarget = false;

        ScrollRect cSr = CreateVerticalScroll(centerCol.transform, "CenterScroll", out RectTransform cContent);
        RectTransform cSrRt = cSr.GetComponent<RectTransform>();
        StretchFull(cSrRt);
        TextMeshProUGUI centerTmp = cContent.gameObject.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            centerTmp.font = TMP_Settings.defaultFontAsset;
        centerTmp.richText = true;
        centerTmp.fontSize = 17f;
        centerTmp.alignment = TextAlignmentOptions.TopLeft;
        centerTmp.textWrappingMode = TextWrappingModes.Normal;
        centerTmp.color = new Color(0.86f, 0.88f, 0.92f, 1f);
        centerTmp.text = "";
        LayoutElement cTmpLe = centerTmp.gameObject.AddComponent<LayoutElement>();
        cTmpLe.minWidth = 100f;
        ContentSizeFitter cFitter = cContent.gameObject.AddComponent<ContentSizeFitter>();
        cFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        cFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        view.CenterScroll = cSr;
        view.CenterBody = centerTmp;

        // Right column
        GameObject rightCol = new GameObject("RightColumn");
        rightCol.transform.SetParent(mainGo.transform, false);
        LayoutElement rLe = rightCol.AddComponent<LayoutElement>();
        rLe.preferredWidth = RightWidth;
        rLe.minWidth = RightWidth;
        rLe.flexibleWidth = 0f;
        rLe.flexibleHeight = 1f;
        Image rBg = rightCol.AddComponent<Image>();
        rBg.color = new Color(0.06f, 0.07f, 0.09f, 0.55f);
        rBg.raycastTarget = false;

        VerticalLayoutGroup rightV = rightCol.AddComponent<VerticalLayoutGroup>();
        rightV.padding = new RectOffset(8, 8, 8, 8);
        rightV.spacing = 8f;
        rightV.childAlignment = TextAnchor.UpperLeft;
        rightV.childControlWidth = true;
        rightV.childControlHeight = false;
        rightV.childForceExpandWidth = true;
        rightV.childForceExpandHeight = false;

        GameObject rightHdr = new GameObject("RightHeader");
        rightHdr.transform.SetParent(rightCol.transform, false);
        TextMeshProUGUI rHdrTmp = rightHdr.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            rHdrTmp.font = TMP_Settings.defaultFontAsset;
        rHdrTmp.text = "<b>Actions</b>";
        rHdrTmp.fontSize = 14f;
        rHdrTmp.fontStyle = FontStyles.Bold;
        rHdrTmp.color = new Color(0.88f, 0.9f, 0.94f, 0.95f);
        LayoutElement rHdrLe = rightHdr.AddComponent<LayoutElement>();
        rHdrLe.minHeight = 22f;

        GameObject rightScrollHost = new GameObject("RightActionsHost");
        rightScrollHost.transform.SetParent(rightCol.transform, false);
        LayoutElement rScrollLe = rightScrollHost.AddComponent<LayoutElement>();
        rScrollLe.flexibleHeight = 1f;
        rScrollLe.minHeight = 80f;

        ScrollRect rSr = CreateVerticalScroll(rightScrollHost.transform, "RightActionsScroll", out RectTransform rContent);
        RectTransform rSrRt = rSr.GetComponent<RectTransform>();
        StretchFull(rSrRt);
        VerticalLayoutGroup rContentV = rContent.gameObject.AddComponent<VerticalLayoutGroup>();
        rContentV.spacing = 6f;
        rContentV.childAlignment = TextAnchor.UpperLeft;
        rContentV.childControlWidth = true;
        rContentV.childControlHeight = false;
        rContentV.childForceExpandWidth = true;
        rContentV.childForceExpandHeight = false;
        ContentSizeFitter rCf = rContent.gameObject.AddComponent<ContentSizeFitter>();
        rCf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        rCf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        view.RightActionsRoot = rContent;

        return view;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static ScrollRect CreateVerticalScroll(Transform parent, string name, out RectTransform contentRt)
    {
        GameObject scrollGo = new GameObject(name);
        scrollGo.transform.SetParent(parent, false);
        ScrollRect sr = scrollGo.AddComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;

        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollGo.transform, false);
        RectTransform vRt = viewport.AddComponent<RectTransform>();
        StretchFull(vRt);
        Image vImg = viewport.AddComponent<Image>();
        vImg.color = new Color(1f, 1f, 1f, 0.02f);
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        sr.viewport = vRt;

        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        contentRt = content.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0f, 0f);
        sr.content = contentRt;

        return sr;
    }
}
