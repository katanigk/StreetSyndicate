using System.Collections.Generic;
using FamilyBusiness.CityGen.Data;
using FamilyBusiness.CityGen.Government.Windows;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Builds the planning shell under MainArea at runtime (title, context strip, left / scrollable center / right).
/// Wires top tab buttons; institution icon row centered in BottomBar.
/// </summary>
public class PlanningShellController : MonoBehaviour
{
    private const string InstitutionDockName = "InstitutionDock";

    /// <summary>All institution hit targets are square of this size (px); circle sprite fills it for a true disk.</summary>
    private const float InstitutionButtonDiameter = 56f;

    /// <summary>Enforced minimum so a scene/prefab with spacing left at 0 does not pack icons flush.</summary>
    private const float InstitutionDockMinHorizontalGap = 48f;

    [Header("Institution dock (bottom bar icon row)")]
    [Tooltip("Left/bottom insets from BottomBar stretch anchors (Edit Mode values persist; Play Mode tweaks do not).")]
    [SerializeField] private Vector2 _institutionDockOffsetMin = new Vector2(18f, 12f);
    [Tooltip("Right/top insets as negative deltas from anchor max (e.g. -16, -20 for Right 16, Top 20).")]
    [SerializeField] private Vector2 _institutionDockOffsetMax = new Vector2(-16f, -20f);
    [SerializeField] private Vector3 _institutionDockLocalScale = new Vector3(1.1f, 1.1f, 1.1f);
    [Tooltip("HorizontalLayoutGroup padding (use ints — RectOffset cannot be serialized/initialized safely on MonoBehaviour).")]
    [SerializeField] private int _institutionDockPadLeft = 10;
    [SerializeField] private int _institutionDockPadRight = 10;
    [SerializeField] private int _institutionDockPadTop = 5;
    [SerializeField] private int _institutionDockPadBottom = 5;
    [Tooltip("Horizontal gap between institution icons (px). Never below built-in minimum; enforced spacer width between buttons.")]
    [SerializeField] private float _institutionDockSpacing = 64f;

    [Header("Bottom bar (RectTransform — applied at runtime; tune here in Edit Mode, not during Play)")]
    [SerializeField] private bool _applyBottomBarRectLayout = true;
    [SerializeField] private Vector2 _bottomBarAnchorMin = new Vector2(0f, 0f);
    [SerializeField] private Vector2 _bottomBarAnchorMax = new Vector2(1f, 0f);
    [SerializeField] private Vector2 _bottomBarPivot = new Vector2(0.5f, 0.5f);
    [SerializeField] private Vector2 _bottomBarAnchoredPosition = new Vector2(0f, 50f);
    [Tooltip("Stretch bottom bar: Size Delta X should be 0 for full screen width. Y = bar height.")]
    [SerializeField] private Vector2 _bottomBarSizeDelta = new Vector2(0f, 110f);

    private TextMeshProUGUI _titleText;
    private TextMeshProUGUI _contextText;
    private TextMeshProUGUI _topMetricsText;
    private TextMeshProUGUI _metricDirtyCashText;
    private TextMeshProUGUI _metricAccountCashText;
    private TextMeshProUGUI _metricFamilyRepText;
    private TextMeshProUGUI _metricCrewMoraleText;

    private Image _metricDirtyCashIcon;
    private Image _metricAccountCashIcon;
    private Image _metricFamilyRepIcon;
    private Image _metricCrewMoraleIcon;

    private bool _metricBarIconsReloadAttempted;

    private RectTransform _familyRepPairRect;
    private TextMeshProUGUI _leftText;
    private TextMeshProUGUI _centerText;
    private TextMeshProUGUI _rightText;
    private Transform _centerScrollContentRoot;
    private Transform _centerScrollViewportRoot;
    private ScrollRect _centerScrollRect;
    private Image _centerScrollViewportImage;

    // News: newspaper UI (replaces the old plain-text News view).
    private GameObject _newsPaperRoot;
    private TextMeshProUGUI _newsMastheadText;
    private TextMeshProUGUI _newsDateLineText;
    private readonly List<Button> _newsSectionButtons = new List<Button>();
    private TextMeshProUGUI _newsBodyText;
    private Image _newsPaperBackground;

    private enum NewsSectionId
    {
        FrontPage = 0,
        Finance = 1,
        LawAndJustice = 2,
        Crime = 3,
        Obituaries = 4,
        Federal = 5
    }

    private NewsSectionId _newsActiveSection = NewsSectionId.FrontPage;

    // Optional headline font (TMP font asset) for the newspaper.
    // To use: create a TMP Font Asset from Times New Roman and place it under:
    //   Assets/Resources/Fonts/Times New Roman SDF.asset
    // Then this will load automatically.
    private const string NewsHeadlineFontResourcePath = "UI/fonts/Times New Roman SDF";
    private static TMP_FontAsset _cachedNewsHeadlineFont;

    private static TMP_FontAsset LoadNewsHeadlineFont()
    {
        if (_cachedNewsHeadlineFont != null)
            return _cachedNewsHeadlineFont;
        _cachedNewsHeadlineFont = Resources.Load<TMP_FontAsset>(NewsHeadlineFontResourcePath);
        return _cachedNewsHeadlineFont;
    }
    private Transform _leftColumnRoot;
    private Transform _personnelMemberListRoot;
    private readonly List<Button> _personnelMemberRowButtons = new List<Button>();
    private readonly List<Image> _personnelMemberRowImages = new List<Image>();
    private static Sprite _cachedUiHitSprite;
    private int _personnelSelectedMemberIndex = -1;
    private Button _legalCodexToggleButton;
    private LayoutElement _legalLeftTopSpacer;
    private LayoutElement _legalLeftBottomSpacer;
    private Image _legalCodexBookImage;
    private LayoutElement _legalCodexBookLe;
    private Button _legalCodexBookButton;
    private TextMeshProUGUI _legalCodexBookCaptionText;
    private static bool _warnedMissingCodexBookSprite;

    // Sidebar thumbnail: unchanged small book art (not the full open spread).
    private const string CodexSidebarBookResourcePath = "UI/Icons/Street Codex (Black Ledger)";
    // Full-screen codex modal: wide open-book spread.
    private const string CodexModalOpenBookResourcePath = "UI/Icons/Street Codex_Open";
    private const string CodexBookLegacyResourcePath = "UI/Icons/icon_codex_open_sheet";
    /// <summary>Pixels past the left column so dim/panel clear the closed-book hit area in the Legal sidebar.</summary>
    private const float CodexLeftSidebarExtraGutterPx = 12f;

    // Codex book modal (paged, open-book spread)
    private GameObject _codexModalRoot;
    private RectTransform _codexPanelRt;
    private TextMeshProUGUI _codexTitleText;
    private TextMeshProUGUI _codexLeftPageText;
    private TextMeshProUGUI _codexRightPageText;
    private RectTransform _codexTocRoot;
    private readonly List<Button> _codexTocButtons = new List<Button>();
    private Button _codexPagePrevButton;
    private Button _codexPageNextButton;
    private int _codexPageIndex;
    private RectTransform _codexDimDismissRt;
    /// <summary>Width/height of open-book sprite for sizing the modal panel (synced in <see cref="ApplyCodexModalLeftSidebarInset"/>).</summary>
    private float _codexOpenBookAspect = 1.78f;

    private GameObject _institutionModalRoot;
    private RectTransform _institutionModalPanelRt;
    private Image _institutionModalPanelImage;
    private Color _institutionModalPanelColorDefault;
    private TextMeshProUGUI _institutionModalTitle;
    private TextMeshProUGUI _institutionModalBody;
    private GameObject _institutionPoliceContentRoot;
    private GovernmentInstitutionShellView _policeShell;
    /// <summary>Police left-list selection: facility index in <see cref="GameSessionState.PoliceFacilities"/>, or -1 = none.</summary>
    private int _policeSelectedFacilityIndex = -1;
    /// <summary>Police personnel mode: selected rank/role bucket index in <see cref="PolicePersonnelRankBuckets"/>, or -1 = none.</summary>
    private int _policePersonnelBucketIndex = -1;

    private static readonly string[] PolicePersonnelRankBuckets =
    {
        "Senior command & brass",
        "Junior officers",
        "Detectives",
        "Investigators",
        "Patrol",
        "Staff / admin",
        "Evidence & chain-of-custody",
        "Traffic & checkpoints"
    };

    private enum PoliceModalTabId
    {
        KnownSituation = 0,
        KnownPeopleLocations = 1,
        KnownCasesOps = 2,
        KnownWeaknessesOpportunities = 3,
        AvailableActions = 4,
        OutcomeLog = 5
    }

    private PoliceModalTabId _policeModalTab = PoliceModalTabId.KnownSituation;

    /// <summary>Batch 14: left-list selection for police window when bound to <see cref="CityData.GovernmentData"/>.</summary>
    private string _policeCityGenStableSelection;

    /// <summary>Batch 14: left-list selection for Federal Bureau stub when city data is bound.</summary>
    private string _federalCityGenStableSelection;

    /// <summary>Institution modal title currently shown (for refresh after discovery).</summary>
    private string _activeInstitutionModalTitle;

    private static Sprite _planningCircleSprite;
    private static Sprite _cachedAoWRadialWoodDiskSprite;
    private static Sprite _planningUiWhiteSprite;
    private static Sprite _fallbackPoliceInstitutionSprite;
    private Button _prisonInstitutionButton;
    private Image _prisonInstitutionCircle;
    private Outline _prisonInstitutionOutline;
    private Image _prisonInstitutionGlow;
    private RectTransform _prisonInstitutionGlowRt;
    private bool _prisonAlertAcknowledged = true;
    private int _lastKnownImprisonedCount;
    private bool _prisonAlertActive;
    private int _prisonAlertStartedWeek = -1;
    private static readonly HashSet<string> _knownImprisonedMembers = new HashSet<string>();
    private CrewMember _prisonShellShownInmate;
    private int _prisonShellTrainingPick = -1;

    /// <summary>Caches Resources loads (including failed loads).</summary>
    private static readonly Dictionary<string, Sprite> _institutionIconCache = new Dictionary<string, Sprite>();

    /// <summary>Tri-pane government shells (same template as Police) for dock institutions except Police. Prison uses the same shell with real custody UI.</summary>
    private sealed class StubGovernmentInstitutionUi
    {
        public GameObject Root;
        public GovernmentInstitutionShellView Shell;
        public int SelectedTabIndex;
        public Color ModalPanelColor;
        public string[] ModeLabels;
    }

    private readonly Dictionary<string, StubGovernmentInstitutionUi> _stubGovernmentUis = new Dictionary<string, StubGovernmentInstitutionUi>();

    private PlanningTabId _current;

    private GameObject _missionRow;
    private GameObject _missionQueueOrderStrip;
    private GameObject _threeColumnRow;
    private GameObject _opsStageOverlayRoot;
    private RectTransform _opsStagePanelRt;
    private RectTransform _opsMapGridRoot;
    private RectTransform _opsMapAreaRt;
    /// <summary>Contain-fit is baked into map geometry; only user scroll-zoom uses <see cref="_opsMapGridRoot"/> scale.</summary>
    private float _opsMapUserZoom = 1f;
    private Vector2 _opsMapRebuildViewport;
    private Vector2 _opsMapViewportFallback = new Vector2(720f, 520f);
    private GameObject _opsNeighborhoodOverlayRoot;
    private TextMeshProUGUI _opsNeighborhoodTitleText;
    private RectTransform _opsNeighborhoodGridRt;

    /// <summary>Coarse block selected on the center map; the left column mirrors its 3×3 lots (no separate pop-out).</summary>
    private int _opsCenterMapSelectedBlockId = -1;

    private int _opsLastMacroPaintedBlockId = int.MinValue;

    private RectTransform _opsBlockMapGridRoot;
    private TextMeshProUGUI _opsCertainInfoText;
    private ScrollRect _opsCertainScroll;
    private TextMeshProUGUI _opsRumorsText;
    private RectTransform _opsActionsContentRoot;
    /// <summary>Selected façade on the home block / city map; empty = none.</summary>
    private string _opsSelectedSpotStableId = string.Empty;

    private GameObject _opsCrewPickModalRoot;
    private TextMeshProUGUI _opsCrewPickTitle;
    private RectTransform _opsCrewPickListContent;
    private Button _opsCrewPickConfirmBtn;
    private Button _opsCrewPickRemoveBtn;
    private TextMeshProUGUI _opsCrewPickConfirmLabel;
    private OperationType _opsCrewPickOperation;
    private int _opsCrewPickSelectedIndex = -1;
    private readonly List<Image> _opsCrewPickRowBackgrounds = new List<Image>(16);
    private TextMeshProUGUI _opsCrewPickDetailText;
    private RectTransform _opsCrewPickExtrasContent;
    private Button _opsCrewPickVehicleBtn;
    private TextMeshProUGUI _opsCrewPickVehicleBtnLabel;
    private Button _opsCrewPickLookoutBtn;
    private TextMeshProUGUI _opsCrewPickLookoutBtnLabel;
    private Button _opsCrewPickDriverBtn;
    private TextMeshProUGUI _opsCrewPickDriverBtnLabel;
    private bool _opsCrewPickMissionVehicle = true;
    private bool[] _opsCrewPickExtraOn = System.Array.Empty<bool>();
    private int _opsCrewPickLookoutIdx = -1;
    private int _opsCrewPickDriverIdx = -1;

    private const float OpsMapUserZoomMin = 1f;
    private const float OpsMapUserZoomMax = 16f;

    private int _opsCachedOpsCitySeed = int.MinValue;
    private int _opsBuiltCityDataRevision = int.MinValue;
    private int _opsMapFocusedLotId = -1;
    private int _opsLastMapPaintedFocusLotId = int.MinValue;
    private readonly Dictionary<OperationType, Button> _opButtons = new Dictionary<OperationType, Button>();

    private readonly Dictionary<PlanningTabId, TabVisualState> _tabVisuals = new Dictionary<PlanningTabId, TabVisualState>();

    private struct TabVisualState
    {
        public Image Background;
        public Color NormalBg;
        /// <summary>When set, selection uses sprites (pressed) instead of tinting the wood image.</summary>
        public TopTabButtonSpriteDriver TabSpriteDriver;
    }

    [Header("End Turn Button (runtime-built)")]
    [SerializeField] private Vector2 _endTurnOffset = new Vector2(-16f, 52f);
    [SerializeField] private Vector2 _endTurnSize = new Vector2(100f, 36f);

    [Header("Day Label (left of End Turn)")]
    [SerializeField] private float _dayLabelGap = 12f;
    [SerializeField] private Vector2 _dayLabelSize = new Vector2(340f, 72f);

    [Header("Character HUD (AoW style, runtime-built)")]
    [SerializeField] private bool _enableAoWCharacterHud = true;
    [Tooltip("Legacy: ignored when the AoW root stretches to BottomBar (current default).")]
    [SerializeField] private Vector2 _aowHudSize = new Vector2(360f, 126f);
    [Tooltip("Turn strip: X = extra nudge after auto-place left of portrait; Y = vertical nudge. Portrait position uses _aowPortraitClusterOffset only.")]
    [SerializeField] private Vector2 _aowHudOffset = new Vector2(0f, 4f);
    [SerializeField] private bool _aowHudShowPanelBackground = false;
    [SerializeField] private bool _aowHudShowHeaderLabel = false;
    [SerializeField] private bool _aowHudOwnsTurnControls = true;
    [Tooltip("Turn strip (Times + Next Turn) width/height. Width should fit only those two controls (portrait is in PortraitCluster).")]
    [SerializeField] private Vector2 _aowTurnStripSize = new Vector2(410f, 148f);
    [SerializeField] private float _aowTurnStripGap = 12f;
    [Tooltip("Extra horizontal margin (px) reserved in modal width clearance between turn strip and portrait cluster.")]
    [SerializeField] private float _aowNextTurnPortraitGapPx = 24f;
    [Tooltip("Boss portrait ring diameter (px). Radial action chips use _aowRadialButtonDiameter separately.")]
    [SerializeField] private Vector2 _aowPortraitSize = new Vector2(400f, 400f);
    [SerializeField] private float _aowPortraitYOffset = 12f;
    // Distance from the bottom of the turn strip (smaller = lower on screen; can be negative to go below).
    [SerializeField] private float _aowTurnControlsYOffset = -18f;
    // X offset applied to the "Next Turn + Times" block only (negative = move left).
    [SerializeField] private float _aowTurnControlsXOffset = -12f;
    // Extra X offset for the times label only (negative = move left).
    [SerializeField] private float _aowTimesXOffset = -12f;
    [Header("AoW turn controls (manual slot placement)")]
    [SerializeField] private Vector2 _aowTimesSlotAnchoredPosition = new Vector2(220f, -68f);
    [SerializeField] private Vector2 _aowTimesSlotSize = new Vector2(360f, 90f);
    [SerializeField] private Vector2 _aowNextTurnSlotAnchoredPosition = new Vector2(1050f, -178f);
    [SerializeField] private Vector2 _aowNextTurnSlotSize = new Vector2(158f, 104f);
    [Tooltip("When on, Next Turn X is from TurnStrip right edge (recommended). When off, uses anchor top-left + _aowNextTurnSlotAnchoredPosition.x.")]
    [SerializeField] private bool _aowNextTurnLayoutFromStripRight = true;
    [Tooltip("Distance left from TurnStrip right edge to NextTurn slot center (px). Smaller = further right toward portrait.")]
    [SerializeField] private float _aowNextTurnCenterXPxFromStripRight = 680f;
    [Tooltip("Extra pixels added to Next Turn X (after right-edge layout). Positive = move right.")]
    [SerializeField] private float _aowNextTurnExtraOffsetXPx = -55f;
    [Tooltip("When on, Next Turn position is derived from the portrait rect (left edge − gap) so it cannot overlap the portrait; ignores strip-right X math.")]
    [SerializeField] private bool _aowNextTurnSnapLeftOfPortrait = true;
    [Tooltip("After snap, vertical offset from portrait cluster bounds center (px). Negative = lower.")]
    [SerializeField] private float _aowNextTurnSnapVerticalOffsetPx = -28f;
    [Tooltip("Move date slot right by physical distance (cm).")]
    [SerializeField] private float _aowTimesNudgeRightCm = 8f;
    [Tooltip("Move date slot up by physical distance (cm).")]
    [SerializeField] private float _aowTimesNudgeUpCm = 0f;
    [Tooltip("Move portrait cluster right by physical distance (cm).")]
    [SerializeField] private float _aowPortraitNudgeRightCm = 8f;
    [Tooltip("Move portrait cluster down by physical distance (cm).")]
    [SerializeField] private float _aowPortraitNudgeDownCm = 2f;
    [Tooltip("Legacy: used only when _aowNextTurnLayoutFromStripRight is off. When using right-edge layout, use _aowNextTurnExtraOffsetXPx (px) instead — cm here was easy to set too high (~hundreds of px).")]
    [SerializeField] private float _aowNextTurnNudgeRightCm = 0f;
    [Tooltip("Move Next Turn slot up by physical distance (cm).")]
    [SerializeField] private float _aowNextTurnNudgeUpCm = 0f;
    /// <summary>Vertical offset from the center of TimesSlot in px (positive = down, negative = up).</summary>
    [SerializeField] private float _aowTimesTopInsetPx = 0f;
    [Tooltip("Hard downward shift for date text (px). Applied at runtime even when old Inspector values linger.")]
    [SerializeField] private float _aowTimesForceDownPx = 26f;
    /// <summary>Extra distance down from the computed inset, in cm (uses screen DPI). 0 = none.</summary>
    [SerializeField] private float _aowTimesExtraDownCm = 0f;
    [Tooltip("Inset from the physical bottom edge (~cm).")]
    [SerializeField] private float _aowBottomMarginCm = 1f;
    [Tooltip("Inset from the physical right edge (~cm). AoW turn strip anchors bottom-right to this margin.")]
    [SerializeField] private float _aowRightMarginCm = 4.5f;
    [Tooltip("Portrait cluster anchor nudge from bottom-right (px). More negative X = further left; +Y lifts (reduces bottom clip). Turn strip follows automatically to the left.")]
    [SerializeField] private Vector2 _aowPortraitClusterOffset = new Vector2(-14f, 48f);
    [Tooltip("Extra height on the portrait cluster rect so the ring/radials can extend above the BottomBar without clipping.")]
    [SerializeField] private float _aowPortraitClusterExtraHeightPx = 56f;
    [Tooltip("Extra pixels allowed when fitting the ring vs BottomBar height (larger portrait without clipping the bottom).")]
    [SerializeField] private float _aowPortraitFitExtraBleedPx = 40f;
    [Header("Next Turn (AoW strip)")]
    [Tooltip("Resources path without extension, under a Resources folder (e.g. UI/Icons/Next turn → Assets/Resources/UI/Icons/Next turn.png).")]
    [SerializeField] private string _aowNextTurnIconResourcesPath = "UI/Icons/Next turn";
    [SerializeField] private float _aowNextTurnIconMaxWidthPx = 200f;
    [SerializeField] private float _aowNextTurnIconMaxHeightPx = 112f;
    [SerializeField] private float _aowFallbackDpi = 96f;

    private GameObject _aowHudRoot;
    private RawImage _aowPortraitRaw;
    private Button _aowNextTurnButton;
    private readonly Button[] _aowActionButtons = new Button[3];
    private Button _endTurnButton;
    private TextMeshProUGUI _aowTimeLabel;
    private RectTransform _aowTimesRect;
    private RectTransform _aowTimesSlotRt;
    private RectTransform _aowNextTurnSlotRt;
    private RectTransform _aowTurnStripRt;
    private RectTransform _aowPortraitClusterRt;
    private RectTransform _aowPortraitRingRt;
    private LayoutElement _aowPortraitSlotLe;
    private int _aowTimesLayoutApplyFrames;
    [Header("Ops panel layout (edge-to-edge in MainArea; optional insets in px). MainArea already clears tab strip + bottom bar; AoW HUD is drawn above Ops via BringPlanningInteractiveChromeToFront.")]
    [SerializeField] private float _opsPanelSafeInsetLeft = 0f;
    [SerializeField] private float _opsPanelSafeInsetBottom = 0f;
    [SerializeField] private float _opsPanelSafeInsetRight = 0f;
    [SerializeField] private float _opsPanelSafeInsetTop = 0f;

    [Header("AoW radial buttons")]
    [SerializeField] private float _aowRadialButtonDiameter = 50f;
    [SerializeField] private float _aowRadialRadius = 0f;
    [SerializeField] private float _aowRadialEdgeGap = 2f;

    [Header("AoW portrait ring art (Resources path, no extension)")]
    [SerializeField] private string _aowPortraitRingSpritePath = "UI/HUD/portrait_ring_thin";
    [Tooltip("Optional round button skin under portrait (Resources path). Empty = procedural wood disk matching BottomBar.")]
    [SerializeField] private string _aowRadialButtonSpritePath = "";

    private void Awake()
    {
        // Set before any OnGUI so the legacy IMGUI portrait does not flash in Planning.
        GameOverlayMenu.HideBossPortraitHudButton = _enableAoWCharacterHud;
    }

    private void OnDestroy()
    {
        GameOverlayMenu.HideBossPortraitHudButton = false;
    }

    private void Start()
    {
        EnsurePlanningCameraBackgroundBlack();

        BuildTopTabStrip();

        RectTransform mainArea = GameObject.Find("MainArea")?.GetComponent<RectTransform>();
        if (mainArea == null)
        {
            Debug.LogError("PlanningShellController: MainArea not found.");
            return;
        }

        // PlanningScene: MainArea has a full-rect background Image with raycastTarget on. That steals UI hits
        // in gaps / when child sorting is ambiguous — crew rows never receive clicks. Children handle their own hits.
        DisableMainAreaBackgroundRaycast(mainArea.gameObject);
        ApplyMainAreaBackgroundSkin(mainArea.gameObject);

        if (GameSessionState.CityMapSeed == 0)
            GameSessionState.CityMapSeed = Random.Range(1, int.MaxValue);
        GameSessionState.EnsureActiveCityData();
        GameSessionState.EnsureMicroBlockReady();

        EnsureRootCanvasScale();
        EnsurePlanningCanvasScaler();
        ApplyBottomBarLayoutAndSkin();
        // AoW + turn controls before BuildUi so a failure in Ops/micro-block UI cannot skip the bottom HUD.
        BuildEndTurnButton();
        BuildDayLabel();
        BuildAoWCharacterHud();

        BuildUi(mainArea);
        BuildInstitutionDock();
        BuildInstitutionModal();

        EnsurePlanningCanvasOrder();

        Canvas.ForceUpdateCanvases();
        RectTransform bottomBar = GameObject.Find("BottomBar")?.GetComponent<RectTransform>();
        if (bottomBar != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(bottomBar);
        RectTransform canvasRt = GameObject.Find("Canvas")?.GetComponent<RectTransform>();
        if (canvasRt != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(canvasRt);

        SyncAoWTurnStripToBottomBar();

        ShowTab(PlanningTabId.Overview);
        _lastKnownImprisonedCount = CountIncarceratedCrewMembers();
        _prisonAlertAcknowledged = _lastKnownImprisonedCount <= 0;
        _prisonAlertActive = false;
        _prisonAlertStartedWeek = -1;

        if (PlayerRunState.HasCharacter && PlayerRunState.Character != null)
            PersonnelRegistry.SyncBossSlotFromProfileAndCustody(PlayerRunState.Character);
        GameSessionState.ApplyBossCustodyLegalPhaseFromTrialFlag();
    }

#if UNITY_EDITOR
    [ContextMenu("Build HUD In Editor")]
    private void BuildHudInEditor()
    {
        if (Application.isPlaying)
            return;
        EnsureRootCanvasScale();
        EnsurePlanningCanvasScaler();
        ApplyBottomBarLayoutAndSkin();
        RectTransform mainArea = GameObject.Find("MainArea")?.GetComponent<RectTransform>();
        if (mainArea != null)
        {
            DisableMainAreaBackgroundRaycast(mainArea.gameObject);
            ApplyMainAreaBackgroundSkin(mainArea.gameObject);
        }
        BuildAoWCharacterHud();
        SyncAoWTurnStripToBottomBar();
        BuildInstitutionDock();
        RectTransform bb = GameObject.Find("BottomBar")?.GetComponent<RectTransform>();
        if (bb != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(bb);
        EditorUtility.SetDirty(gameObject);
        if (gameObject.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }

    [ContextMenu("Clear HUD Preview")]
    private void ClearHudPreviewInEditor()
    {
        if (Application.isPlaying)
            return;
        Transform canvasTf = GameObject.Find("Canvas")?.transform;
        if (canvasTf != null)
        {
            Transform legacyHud = canvasTf.Find("AoWCharacterHud");
            if (legacyHud != null)
                DestroyObjectSafe(legacyHud.gameObject);
        }
        RectTransform bottomBar = GameObject.Find("BottomBar")?.GetComponent<RectTransform>();
        if (bottomBar != null)
        {
            Transform onBar = bottomBar.Find("AoWCharacterHud");
            if (onBar != null)
                DestroyObjectSafe(onBar.gameObject);
            Transform dock = bottomBar.Find(InstitutionDockName);
            if (dock != null)
                DestroyObjectSafe(dock.gameObject);
        }
        EditorUtility.SetDirty(gameObject);
        if (gameObject.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
#endif

    private void BuildAoWCharacterHud()
    {
        if (!_enableAoWCharacterHud)
            return;

        Transform canvasTf = GameObject.Find("Canvas")?.transform;
        RectTransform bottomBar = GameObject.Find("BottomBar")?.GetComponent<RectTransform>();
        if (bottomBar == null)
            return;

        if (canvasTf != null)
        {
            Transform legacyHud = canvasTf.Find("AoWCharacterHud");
            if (legacyHud != null)
                DestroyObjectSafe(legacyHud.gameObject);
        }

        Transform existingOnBar = bottomBar.Find("AoWCharacterHud");
        if (existingOnBar != null)
            DestroyObjectSafe(existingOnBar.gameObject);

        _aowHudRoot = new GameObject("AoWCharacterHud", typeof(RectTransform));
        _aowHudRoot.transform.SetParent(bottomBar.transform, false);
        _aowHudRoot.transform.SetAsLastSibling();

        RectTransform rootRt = _aowHudRoot.GetComponent<RectTransform>();
        // Full BottomBar width so the turn strip can anchor bottom-right with safe margins (portrait + radials stay on-screen).
        rootRt.anchorMin = new Vector2(0f, 0f);
        rootRt.anchorMax = new Vector2(1f, 1f);
        rootRt.pivot = new Vector2(0.5f, 0.5f);
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        if (_aowHudShowPanelBackground)
        {
            Image bg = _aowHudRoot.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.07f, 0.09f, 0.86f);
            bg.raycastTarget = false;
            Outline bgOutline = _aowHudRoot.AddComponent<Outline>();
            bgOutline.effectColor = new Color(0f, 0f, 0f, 0.55f);
            bgOutline.effectDistance = new Vector2(1f, -1f);
        }

        if (_aowHudShowHeaderLabel)
        {
            // Header label (optional).
            GameObject headerGo = new GameObject("Header", typeof(RectTransform));
            headerGo.transform.SetParent(_aowHudRoot.transform, false);
            RectTransform headerRt = headerGo.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.sizeDelta = new Vector2(0f, 24f);
            headerRt.anchoredPosition = new Vector2(0f, -6f);
            TextMeshProUGUI headerTmp = headerGo.AddComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null)
                headerTmp.font = TMP_Settings.defaultFontAsset;
            headerTmp.text = "Next Unit";
            headerTmp.fontSize = 16f;
            headerTmp.fontStyle = FontStyles.Bold;
            headerTmp.alignment = TextAlignmentOptions.Top;
            headerTmp.color = new Color(0.92f, 0.93f, 0.96f, 0.9f);
            headerTmp.raycastTarget = false;
            headerTmp.margin = new Vector4(14f, 0f, 14f, 0f);
        }

        // Turn controls root (manual): TimesSlot + NextTurnSlot use explicit anchored positions.
        GameObject stripGo = new GameObject("TurnStrip", typeof(RectTransform));
        stripGo.transform.SetParent(_aowHudRoot.transform, false);
        RectTransform stripRt = stripGo.GetComponent<RectTransform>();
        StretchFull(stripRt);
        _aowTurnStripRt = stripRt;

        // Times / date (left side of strip).
        GameObject timeSlot = new GameObject("TimesSlot", typeof(RectTransform));
        timeSlot.transform.SetParent(stripGo.transform, false);
        RectTransform timeSlotRt = timeSlot.GetComponent<RectTransform>();
        _aowTimesSlotRt = timeSlotRt;
        LayoutElement timeLe = timeSlot.AddComponent<LayoutElement>();
        timeLe.ignoreLayout = true;

        GameObject timeGo = new GameObject("Times", typeof(RectTransform));
        timeGo.transform.SetParent(timeSlot.transform, false);
        RectTransform timeRt = timeGo.GetComponent<RectTransform>();
        _aowTimesRect = timeRt;
        _aowTimesLayoutApplyFrames = 8;
        ApplyAoWTimesLayoutNow();

        _aowTimeLabel = timeGo.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            _aowTimeLabel.font = TMP_Settings.defaultFontAsset;
        _aowTimeLabel.text = GameCalendarSystem.FormatPlanningHudLine(GameSessionState.CurrentDay);
        _aowTimeLabel.fontSize = 16f;
        _aowTimeLabel.alignment = TextAlignmentOptions.Right;
        _aowTimeLabel.color = new Color(0.94f, 0.94f, 0.92f, 0.9f);
        _aowTimeLabel.raycastTarget = false;
        _aowTimeLabel.margin = new Vector4(0f, 0f, 0f, 0f);

        TurnManager tmForTimes = Object.FindFirstObjectByType<TurnManager>();
        if (tmForTimes != null)
            tmForTimes.dayLabel = _aowTimeLabel;

        // Next Turn (right side of strip, next to portrait).
        GameObject nextTurnSlot = new GameObject("NextTurnSlot", typeof(RectTransform));
        nextTurnSlot.transform.SetParent(stripGo.transform, false);
        RectTransform nextTurnSlotRt = nextTurnSlot.GetComponent<RectTransform>();
        _aowNextTurnSlotRt = nextTurnSlotRt;
        LayoutElement nextTurnLe = nextTurnSlot.AddComponent<LayoutElement>();
        nextTurnLe.ignoreLayout = true;
        nextTurnLe.preferredWidth = _aowNextTurnSlotSize.x;
        nextTurnLe.minWidth = _aowNextTurnSlotSize.x;
        nextTurnLe.preferredHeight = _aowNextTurnSlotSize.y;
        nextTurnLe.minHeight = _aowNextTurnSlotSize.y;

        Button nextTurnRect = CreateAoWNextTurnControl(nextTurnSlot.transform, nextTurnLe);
        nextTurnRect.onClick.AddListener(() => InvokePlanningEndTurnFromUi());
        ApplyAoWTurnStripAnchoredPosition();

        float stripHForPortrait = _aowTurnStripSize.y;
        if (bottomBar.rect.height > 1f)
            stripHForPortrait = bottomBar.rect.height;

        GameObject portraitClusterGo = new GameObject("PortraitCluster", typeof(RectTransform));
        portraitClusterGo.transform.SetParent(_aowHudRoot.transform, false);
        RectTransform portraitClusterRt = portraitClusterGo.GetComponent<RectTransform>();
        portraitClusterRt.anchorMin = new Vector2(1f, 0f);
        portraitClusterRt.anchorMax = new Vector2(1f, 0f);
        portraitClusterRt.pivot = new Vector2(1f, 0f);
        _aowPortraitClusterRt = portraitClusterRt;

        // Portrait ring (mask + border) — under PortraitCluster (not TurnStrip), so moving it does not reflow Times/Next Turn.
        GameObject portraitSlot = new GameObject("PortraitSlot", typeof(RectTransform));
        portraitSlot.transform.SetParent(portraitClusterGo.transform, false);
        RectTransform portraitSlotRt = portraitSlot.GetComponent<RectTransform>();
        portraitSlotRt.anchorMin = Vector2.zero;
        portraitSlotRt.anchorMax = Vector2.one;
        portraitSlotRt.offsetMin = Vector2.zero;
        portraitSlotRt.offsetMax = Vector2.zero;
        LayoutElement portraitLe = portraitSlot.AddComponent<LayoutElement>();
        _aowPortraitSlotLe = portraitLe;
        float fitH = stripHForPortrait + Mathf.Max(0f, _aowPortraitFitExtraBleedPx);
        float clusterH = stripHForPortrait + Mathf.Max(0f, _aowPortraitClusterExtraHeightPx);
        float radialBtnD0 = Mathf.Clamp(_aowRadialButtonDiameter, 28f, 92f);
        float ringD0 = Mathf.Min(_aowPortraitSize.x, _aowPortraitSize.y, fitH - 4f);
        ringD0 = Mathf.Max(56f, ringD0);
        float portraitR0 = ringD0 * 0.5f;
        float rOrb0 = _aowRadialRadius > 1e-3f
            ? Mathf.Max(10f, _aowRadialRadius)
            : portraitR0 + radialBtnD0 * 0.5f + Mathf.Max(0f, _aowRadialEdgeGap);
        float portraitCellW = Mathf.Max(_aowPortraitSize.x, ringD0 + rOrb0 + 18f);
        portraitLe.preferredWidth = portraitCellW;
        portraitLe.minWidth = portraitCellW;
        portraitLe.preferredHeight = clusterH;
        portraitLe.minHeight = clusterH;
        portraitClusterRt.sizeDelta = new Vector2(portraitCellW, clusterH);
        ApplyAoWPortraitClusterAnchoredPosition();

        GameObject portraitRoot = new GameObject("PortraitRing", typeof(RectTransform));
        portraitRoot.transform.SetParent(portraitSlot.transform, false);
        RectTransform prt = portraitRoot.GetComponent<RectTransform>();
        _aowPortraitRingRt = prt;
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        float ringD = Mathf.Min(_aowPortraitSize.x, _aowPortraitSize.y, fitH - 4f);
        ringD = Mathf.Max(56f, ringD);
        prt.sizeDelta = new Vector2(ringD, ringD);
        prt.anchoredPosition = new Vector2(0f, _aowPortraitYOffset);

        // 4 distinct buttons:
        // - Big portrait ring: acts like the old top HUD portrait (wired later). For now: click feedback only.
        // - 3 small radial buttons: separate buttons (wired later). For now: click feedback only.
        float radialBtnD = Mathf.Clamp(_aowRadialButtonDiameter, 28f, 92f);
        float portraitR = Mathf.Min(prt.sizeDelta.x, prt.sizeDelta.y) * 0.5f;

        // Visual-only root so pressing the big portrait doesn't scale the small action buttons.
        GameObject portraitVisualGo = new GameObject("PortraitVisual", typeof(RectTransform));
        portraitVisualGo.transform.SetParent(portraitRoot.transform, false);
        RectTransform portraitVisualRt = portraitVisualGo.GetComponent<RectTransform>();
        StretchFull(portraitVisualRt);

        GameObject hitGo = new GameObject("PortraitHit", typeof(RectTransform));
        hitGo.transform.SetParent(portraitRoot.transform, false);
        RectTransform hitRt = hitGo.GetComponent<RectTransform>();
        hitRt.anchorMin = new Vector2(0.5f, 0.5f);
        hitRt.anchorMax = new Vector2(0.5f, 0.5f);
        hitRt.pivot = new Vector2(0.5f, 0.5f);
        hitRt.sizeDelta = prt.sizeDelta;
        hitRt.anchoredPosition = Vector2.zero;
        // Must be above the portrait so hover/press tint is visible.
        hitRt.SetAsLastSibling();

        Image hitImg = hitGo.AddComponent<Image>();
        // Use a subtle overlay so hover/press feedback is visible.
        hitImg.sprite = GetPlanningCircleSprite();
        hitImg.type = Image.Type.Simple;
        hitImg.preserveAspect = true;
        hitImg.color = new Color(1f, 1f, 1f, 0f);
        hitImg.raycastTarget = true;
        Button portraitBtn = hitGo.AddComponent<Button>();
        portraitBtn.targetGraphic = hitImg;
        portraitBtn.transition = Selectable.Transition.ColorTint;
        ColorBlock pcb = portraitBtn.colors;
        pcb.normalColor = new Color(1f, 1f, 1f, 0f);
        pcb.highlightedColor = new Color(1f, 1f, 1f, 0.18f);
        pcb.pressedColor = new Color(1f, 1f, 1f, 0.28f);
        pcb.colorMultiplier = 1f;
        portraitBtn.colors = pcb;
        portraitBtn.onClick.AddListener(() =>
        {
            if (GameOverlayMenu.Instance != null)
                GameOverlayMenu.Instance.ToggleBossInfoWindow();
        });
        // Scale only the portrait visuals (not the small action buttons).
        ButtonPressScale pressScale = hitGo.AddComponent<ButtonPressScale>();
        pressScale.SetScaleTarget(portraitVisualGo.transform);

        Image ringBg = portraitVisualGo.AddComponent<Image>();
        ringBg.sprite = GetPlanningCircleSprite();
        ringBg.preserveAspect = true;
        ringBg.color = new Color(0.14f, 0.15f, 0.18f, 1f);
        ringBg.raycastTarget = false;

        GameObject maskGo = new GameObject("PortraitMask", typeof(RectTransform));
        maskGo.transform.SetParent(portraitVisualGo.transform, false);
        RectTransform maskRt = maskGo.GetComponent<RectTransform>();
        StretchFull(maskRt);
        Image maskImg = maskGo.AddComponent<Image>();
        maskImg.sprite = GetPlanningCircleSprite();
        maskImg.preserveAspect = true;
        maskImg.color = Color.white;
        // Important: do not block clicks on the portrait button.
        maskImg.raycastTarget = false;
        Mask mask = maskGo.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        GameObject rawGo = new GameObject("Portrait", typeof(RectTransform));
        rawGo.transform.SetParent(maskGo.transform, false);
        RectTransform rawRt = rawGo.GetComponent<RectTransform>();
        rawRt.anchorMin = Vector2.zero;
        rawRt.anchorMax = Vector2.one;
        rawRt.offsetMin = new Vector2(6f, 6f);
        rawRt.offsetMax = new Vector2(-6f, -6f);
        _aowPortraitRaw = rawGo.AddComponent<RawImage>();
        _aowPortraitRaw.color = Color.white;
        _aowPortraitRaw.raycastTarget = false;
        _aowPortraitRaw.texture = TryLoadBossPortraitTexture();

        GameObject ringBorderGo = new GameObject("Border", typeof(RectTransform));
        ringBorderGo.transform.SetParent(portraitVisualGo.transform, false);
        RectTransform borderRt = ringBorderGo.GetComponent<RectTransform>();
        StretchFull(borderRt);
        Image borderImg = ringBorderGo.AddComponent<Image>();
        Sprite ringSprite = TryLoadHudSprite(_aowPortraitRingSpritePath);
        borderImg.sprite = ringSprite != null ? ringSprite : GetPlanningCircleSprite();
        borderImg.preserveAspect = true;
        // If we have authored art, keep it un-tinted. Otherwise keep the warm placeholder tint.
        borderImg.color = ringSprite != null ? Color.white : new Color(0.98f, 0.84f, 0.52f, 0.52f);
        borderImg.raycastTarget = false;

        // Radial buttons around portrait (AoW style).
        // If radius is 0, auto-place buttons right at the portrait edge (+ a tiny gap),
        // so they don't overlap or touch the portrait ring.
        float r = _aowRadialRadius > 1e-3f
            ? Mathf.Max(10f, _aowRadialRadius)
            : portraitR + radialBtnD * 0.5f + Mathf.Max(0f, _aowRadialEdgeGap);

        // Order / angles chosen to match references: actions at left/top-left/top, Next Turn at bottom.
        Vector2[] offsets =
        {
            new Vector2(-r, 0f),                         // Action 1 (left)
            new Vector2(-r * 0.70f, r * 0.70f),          // Action 2 (top-left)
            new Vector2(0f, r),                          // Action 3 (top)
            new Vector2(0f, -r)                          // Next Turn (bottom)
        };

        Sprite radialSkin = TryLoadHudSprite(_aowRadialButtonSpritePath);
        if (radialSkin == null)
            radialSkin = GetAoWRadialWoodDiskSprite();

        // Radial buttons are separate buttons (click feedback only for now; actions wired later).
        Button a1 = CreateAoWRadialButton(portraitRoot.transform, "Action1", radialBtnD, prt.sizeDelta * 0.5f + offsets[0], radialSkin, interactable: true);
        Button a2 = CreateAoWRadialButton(portraitRoot.transform, "Action2", radialBtnD, prt.sizeDelta * 0.5f + offsets[1], radialSkin, interactable: true);
        Button a3 = CreateAoWRadialButton(portraitRoot.transform, "Action3", radialBtnD, prt.sizeDelta * 0.5f + offsets[2], radialSkin, interactable: true);
        _aowActionButtons[0] = a1;
        _aowActionButtons[1] = a2;
        _aowActionButtons[2] = a3;

        if (_aowActionButtons[0] != null) _aowActionButtons[0].onClick.AddListener(() => { });
        if (_aowActionButtons[1] != null) _aowActionButtons[1].onClick.AddListener(() => { });
        if (_aowActionButtons[2] != null) _aowActionButtons[2].onClick.AddListener(() => { });

        // Keep a reference to the rect "Next Turn" button as the canonical next-turn action.
        _aowNextTurnButton = nextTurnRect;

        // Portrait above TurnStrip (times/date), but Next Turn above portrait — without nested Canvas (nested Canvas broke Scene layout).
        portraitClusterGo.transform.SetAsLastSibling();
        if (_aowNextTurnSlotRt != null && _aowHudRoot != null)
        {
            _aowNextTurnSlotRt.SetParent(_aowHudRoot.transform, false);
            _aowNextTurnSlotRt.SetAsLastSibling();
        }

        ApplyAoWPortraitFitToStrip(stripHForPortrait);
        ApplyAoWPortraitClusterAnchoredPosition();
        ApplyAoWTurnStripAnchoredPosition();
    }

    private static void DestroyObjectSafe(Object obj)
    {
        if (obj == null)
            return;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Object.DestroyImmediate(obj);
            return;
        }
#endif
        Object.Destroy(obj);
    }

    private Button CreateAoWNextTurnControl(Transform nextTurnSlotTransform, LayoutElement nextTurnLe)
    {
        Sprite spr = TryLoadNextTurnIconSprite(_aowNextTurnIconResourcesPath);
        if (spr == null)
        {
            nextTurnLe.preferredWidth = 158f;
            nextTurnLe.minWidth = 158f;
            Button fallback = CreateAoWRectButton(nextTurnSlotTransform, "Next Turn");
            RectTransform frt = fallback.GetComponent<RectTransform>();
            frt.anchorMin = new Vector2(0.5f, 0.5f);
            frt.anchorMax = new Vector2(0.5f, 0.5f);
            frt.pivot = new Vector2(0.5f, 0.5f);
            frt.sizeDelta = new Vector2(154f, 46f);
            frt.anchoredPosition = Vector2.zero;
            return fallback;
        }

        float capH = Mathf.Min(_aowNextTurnIconMaxHeightPx, Mathf.Max(48f, _aowTurnStripSize.y - 6f));
        float capW = Mathf.Max(48f, _aowNextTurnIconMaxWidthPx);
        float sh = spr.rect.height;
        float sw = spr.rect.width;
        float aspect = sw / Mathf.Max(1f, sh);
        float h = capH;
        float w = h * aspect;
        if (w > capW)
        {
            w = capW;
            h = w / Mathf.Max(1e-4f, aspect);
        }

        nextTurnLe.preferredWidth = Mathf.Max(158f, w + 8f);
        nextTurnLe.minWidth = nextTurnLe.preferredWidth;

        GameObject go = new GameObject("NextTurnBtn", typeof(RectTransform));
        go.transform.SetParent(nextTurnSlotTransform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = Vector2.zero;

        Image img = go.AddComponent<Image>();
        img.sprite = spr;
        img.type = Image.Type.Simple;
        img.preserveAspect = true;
        img.raycastTarget = true;

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        PlanningUiButtonStyle.ApplyAoWRadialWoodButton(btn, img);
        go.AddComponent<ButtonPressScale>();
        return btn;
    }

    /// <summary>Loads single or multi-sprite PNG from Resources (matches e.g. &quot;Next turn_0&quot; slices).</summary>
    private static Sprite TryLoadNextTurnIconSprite(string resourcesPathNoExt)
    {
        if (string.IsNullOrWhiteSpace(resourcesPathNoExt))
            return null;
        string p = resourcesPathNoExt.Trim();
        Sprite s = Resources.Load<Sprite>(p);
        if (s != null)
            return s;
        Sprite[] all = Resources.LoadAll<Sprite>(p);
        if (all != null && all.Length > 0)
        {
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null)
                    continue;
                string n = all[i].name;
                if (n.IndexOf("next", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return all[i];
            }
            return all[0];
        }
        // Alternate filename casing (e.g. NEXT TURN.png in Icons).
        string[] alts =
        {
            "UI/Icons/NEXT TURN",
            "Icons/Next turn",
            "Icons/NEXT TURN"
        };
        for (int a = 0; a < alts.Length; a++)
        {
            s = Resources.Load<Sprite>(alts[a]);
            if (s != null)
                return s;
            all = Resources.LoadAll<Sprite>(alts[a]);
            if (all != null && all.Length > 0)
                return all[0];
        }
        return null;
    }

    private static Button CreateAoWRectButton(Transform parent, string label)
    {
        GameObject go = new GameObject(label.Replace(" ", "") + "Btn", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, 26f);

        Image img = go.AddComponent<Image>();
        img.raycastTarget = true;
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        PlanningUiButtonStyle.ApplyStandardRectButton(btn, img);

        GameObject textGo = new GameObject("Label", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        RectTransform tr = textGo.GetComponent<RectTransform>();
        StretchFull(tr);
        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = label;
        tmp.fontSize = 17f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = PlanningUiButtonStyle.LabelPrimary;
        tmp.raycastTarget = false;
        tmp.margin = new Vector4(6f, 2f, 6f, 2f);

        go.AddComponent<ButtonPressScale>();
        return btn;
    }

    private static Button CreateAoWRadialButton(Transform parent, string nameStem, float diameter, Vector2 anchoredCenterInParent, Sprite radialSkin, bool interactable = true)
    {
        GameObject go = new GameObject(nameStem + "Btn", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(diameter, diameter);
        rt.anchoredPosition = anchoredCenterInParent;

        Image circle = go.AddComponent<Image>();
        circle.sprite = radialSkin != null ? radialSkin : GetAoWRadialWoodDiskSprite();
        circle.type = Image.Type.Simple;
        circle.preserveAspect = true;
        circle.raycastTarget = interactable;
        circle.color = Color.white;
        if (!interactable)
            PlanningUiButtonStyle.ApplyOutline(circle);

        Button btn = null;
        if (interactable)
        {
            btn = go.AddComponent<Button>();
            btn.targetGraphic = circle;
            PlanningUiButtonStyle.ApplyAoWRadialWoodButton(btn, circle);
        }

        // Icons can be wired later; avoid the old "•" on dark fill (read as a broken placeholder).
        GameObject glyphGo = new GameObject("Glyph", typeof(RectTransform));
        glyphGo.transform.SetParent(go.transform, false);
        RectTransform grt = glyphGo.GetComponent<RectTransform>();
        StretchFull(grt);
        TextMeshProUGUI tmp = glyphGo.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = nameStem == "NextTurn" ? "⟳" : string.Empty;
        tmp.fontSize = Mathf.Clamp(Mathf.RoundToInt(diameter * 0.55f), 18, 36);
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = PlanningUiButtonStyle.LabelPrimary;
        tmp.raycastTarget = false;

        if (interactable)
        {
            go.AddComponent<ButtonPressScale>();
            return btn;
        }
        return null;
    }

    private static Texture2D TryLoadBossPortraitTexture()
    {
        string key = "BossPortrait";
        PlayerCharacterProfile p = PlayerRunState.Character;
        if (p != null && !string.IsNullOrWhiteSpace(p.PortraitResourcePath))
            key = p.PortraitResourcePath.Trim();
        Texture2D tex = DealerPortraitNaming.LoadPortraitTexture(key);
        if (tex != null)
            return tex;
        // Hard fallback so portrait never renders as a white disk when profile mapping fails.
        tex = Resources.Load<Texture2D>("BossPortrait");
        if (tex != null)
            return tex;
        return Resources.Load<Texture2D>("BagPortrait");
    }

    private static Sprite TryLoadHudSprite(string resourcesPathNoExt)
    {
        if (string.IsNullOrWhiteSpace(resourcesPathNoExt))
            return null;
        return Resources.Load<Sprite>(resourcesPathNoExt.Trim());
    }

    private const string MetricsResourcesFolder = "UI/Metrics";
    private static readonly Dictionary<string, Sprite> s_metricsSpriteFromTextureCache = new Dictionary<string, Sprite>();

    /// <summary>
    /// Metric bar PNGs under <c>Resources/UI/Metrics</c> are often <b>Multiple</b> sprites (<c>crew_morale_0</c>, etc.).
    /// <see cref="Resources.Load{T}"/> on the texture path alone often returns null → white square. Tries sub-sprites,
    /// folder scan, then <see cref="Texture2D"/> + <see cref="Sprite.Create"/> if the asset is not imported as Sprite.
    /// </summary>
    private static Sprite LoadMetricsBarSprite(string resourcesPathNoExt)
    {
        if (string.IsNullOrWhiteSpace(resourcesPathNoExt))
            return null;
        string path = resourcesPathNoExt.Trim();
        int slash = path.LastIndexOf('/');
        string baseName = slash >= 0 ? path.Substring(slash + 1) : path;

        Sprite direct = Resources.Load<Sprite>(path);
        if (direct != null)
            return direct;

        Sprite[] all = Resources.LoadAll<Sprite>(path);
        Sprite picked = PickMetricSpriteFromSlices(all, baseName, allowLargestAreaFallback: true);
        if (picked != null)
            return picked;

        // Folder scan: match by name only (never "largest in folder" — would steal another metric's art).
        Sprite[] folderSprites = Resources.LoadAll<Sprite>(MetricsResourcesFolder);
        picked = PickMetricSpriteFromSlices(folderSprites, baseName, allowLargestAreaFallback: false);
        if (picked != null)
            return picked;

        // Last resort: texture not set to Sprite (2D) — build a runtime sprite (cached per path).
        if (s_metricsSpriteFromTextureCache.TryGetValue(path, out Sprite cached))
            return cached;

        Texture2D tex = Resources.Load<Texture2D>(path);
        if (tex != null)
        {
            Sprite rt = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            rt.name = baseName + "_runtime";
            s_metricsSpriteFromTextureCache[path] = rt;
            return rt;
        }

        Debug.LogWarning("PlanningShellController: metric icon not found for Resources path '" + path +
            "'. Place file at Assets/Resources/" + path + ".png (Texture Type: Sprite 2D UI, or any texture for runtime fallback).");
        return null;
    }

    private static Sprite PickMetricSpriteFromSlices(Sprite[] all, string baseName, bool allowLargestAreaFallback)
    {
        if (all == null || all.Length == 0 || string.IsNullOrEmpty(baseName))
            return null;

        string slice0 = baseName + "_0";
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && string.Equals(all[i].name, slice0, System.StringComparison.OrdinalIgnoreCase))
                return all[i];
        }

        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].name.StartsWith(baseName, System.StringComparison.OrdinalIgnoreCase))
                return all[i];
        }

        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null)
                continue;
            Texture2D t = all[i].texture;
            if (t != null && string.Equals(t.name, baseName, System.StringComparison.OrdinalIgnoreCase))
                return all[i];
        }

        if (!allowLargestAreaFallback)
            return null;

        Sprite best = null;
        float bestArea = 0f;
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null)
                continue;
            Rect r = all[i].rect;
            float a = r.width * r.height;
            if (a > bestArea)
            {
                bestArea = a;
                best = all[i];
            }
        }

        return best;
    }

    private void TryReloadMetricBarIconIfEmpty(Image img, string resourcesPathNoExt)
    {
        if (img == null)
            return;
        if (img.sprite != null)
            return;
        Sprite s = LoadMetricsBarSprite(resourcesPathNoExt);
        if (s != null)
            img.sprite = s;
    }

    private const string BottomBarWoodSpritePath = "UI/Chrome/bottom_bar_wood";
    private const string MainAreaBackgroundSpritePath = "UI/Chrome/main";
    private const string TopTabsBarWoodSpritePath = "UI/Chrome/up_bottom_bar_wood";
    /// <summary>Per-tab button art (filename as authored: up_botton_wood.png).</summary>
    private const string TopTabButtonSpritePath = "UI/Chrome/up_botton_wood";
    /// <summary>Hover art: up_botton_wood2.png — digit appended, no underscore before it.</summary>
    private const string TopTabButtonHoverSpritePath = "UI/Chrome/up_botton_wood2";
    /// <summary>Pressed art: up_botton_wood3.png</summary>
    private const string TopTabButtonPressedSpritePath = "UI/Chrome/up_botton_wood3";

    private static Sprite _cachedBottomBarWoodSprite;
    private static Sprite _cachedTopTabsBarWoodSprite;
    private static Sprite _cachedTopTabButtonSprite;
    private static bool _topTabButtonSpriteLookupDone;
    private static Sprite _cachedTopTabButtonHoverSprite;
    private static bool _topTabButtonHoverSpriteLookupDone;
    private static Sprite _cachedTopTabButtonPressedSprite;
    private static bool _topTabButtonPressedSpriteLookupDone;
    /// <summary>
    /// Positions BottomBar from serialized layout (matches your Inspector target) and applies wood sprite.
    /// </summary>
    private void ApplyBottomBarLayoutAndSkin()
    {
        RectTransform bottomBar = GameObject.Find("BottomBar")?.GetComponent<RectTransform>();
        if (bottomBar == null)
            return;

        if (_applyBottomBarRectLayout)
        {
            bottomBar.anchorMin = _bottomBarAnchorMin;
            bottomBar.anchorMax = _bottomBarAnchorMax;
            bottomBar.pivot = _bottomBarPivot;
            bottomBar.anchoredPosition = _bottomBarAnchoredPosition;
            bottomBar.sizeDelta = _bottomBarSizeDelta;
        }

        Sprite wood = GetBottomBarWoodSprite();
        if (wood == null)
            return;

        Image img = bottomBar.GetComponent<Image>();
        if (img == null)
            img = bottomBar.gameObject.AddComponent<Image>();

        img.sprite = wood;
        img.type = wood.border.sqrMagnitude > 0.01f ? Image.Type.Sliced : Image.Type.Simple;
        img.preserveAspect = false;
        img.color = Color.white;
        img.raycastTarget = false;
    }

    private float BottomBarLayoutHeightForChrome => Mathf.Max(1f, _bottomBarSizeDelta.y);

    private static Sprite GetBottomBarWoodSprite()
    {
        if (_cachedBottomBarWoodSprite != null)
            return _cachedBottomBarWoodSprite;

        Sprite fromRes = Resources.Load<Sprite>(BottomBarWoodSpritePath);
        if (fromRes == null)
        {
            Sprite[] slices = Resources.LoadAll<Sprite>(BottomBarWoodSpritePath);
            if (slices != null && slices.Length > 0)
                fromRes = slices[0];
        }

        if (fromRes != null)
        {
            _cachedBottomBarWoodSprite = fromRes;
            return fromRes;
        }

        _cachedBottomBarWoodSprite = BuildProceduralBottomBarWoodSprite();
        return _cachedBottomBarWoodSprite;
    }

    private static Sprite GetTopTabsBarWoodSprite()
    {
        if (_cachedTopTabsBarWoodSprite != null)
            return _cachedTopTabsBarWoodSprite;

        Sprite fromRes = Resources.Load<Sprite>(TopTabsBarWoodSpritePath);
        if (fromRes == null)
        {
            Sprite[] slices = Resources.LoadAll<Sprite>(TopTabsBarWoodSpritePath);
            if (slices != null && slices.Length > 0)
                fromRes = slices[0];
        }

        _cachedTopTabsBarWoodSprite = fromRes;
        return fromRes;
    }

    private static Sprite GetTopTabButtonSprite()
    {
        if (_topTabButtonSpriteLookupDone)
            return _cachedTopTabButtonSprite;
        _topTabButtonSpriteLookupDone = true;

        Sprite fromRes = Resources.Load<Sprite>(TopTabButtonSpritePath);
        if (fromRes == null)
        {
            Sprite[] slices = Resources.LoadAll<Sprite>(TopTabButtonSpritePath);
            if (slices != null && slices.Length > 0)
                fromRes = slices[0];
        }

        _cachedTopTabButtonSprite = fromRes;
        return _cachedTopTabButtonSprite;
    }

    private static Sprite GetTopTabButtonHoverSprite()
    {
        if (_topTabButtonHoverSpriteLookupDone)
            return _cachedTopTabButtonHoverSprite;
        _topTabButtonHoverSpriteLookupDone = true;

        Sprite fromRes = Resources.Load<Sprite>(TopTabButtonHoverSpritePath);
        if (fromRes == null)
        {
            Sprite[] slices = Resources.LoadAll<Sprite>(TopTabButtonHoverSpritePath);
            if (slices != null && slices.Length > 0)
                fromRes = slices[0];
        }

        _cachedTopTabButtonHoverSprite = fromRes;
        return _cachedTopTabButtonHoverSprite;
    }

    private static Sprite GetTopTabButtonPressedSprite()
    {
        if (_topTabButtonPressedSpriteLookupDone)
            return _cachedTopTabButtonPressedSprite;
        _topTabButtonPressedSpriteLookupDone = true;

        Sprite fromRes = Resources.Load<Sprite>(TopTabButtonPressedSpritePath);
        if (fromRes == null)
        {
            Sprite[] slices = Resources.LoadAll<Sprite>(TopTabButtonPressedSpritePath);
            if (slices != null && slices.Length > 0)
                fromRes = slices[0];
        }

        _cachedTopTabButtonPressedSprite = fromRes;
        return _cachedTopTabButtonPressedSprite;
    }

    private static Sprite BuildProceduralBottomBarWoodSprite()
    {
        const int w = 512;
        const int h = 112;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        const float seed = 12.345f;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float u = x / (float)w;
                float v = y / (float)h;
                float n = Mathf.PerlinNoise(x * 0.018f + seed, y * 0.11f + seed * 0.3f);
                float grain = Mathf.PerlinNoise(x * 0.42f + seed * 2f, y * 0.07f);
                float band = Mathf.Sin(y * 0.28f + Mathf.Sin(x * 0.015f + seed) * 2.2f) * 0.5f + 0.5f;
                Color dark = new Color(0.18f, 0.11f, 0.06f, 1f);
                Color mid = new Color(0.38f, 0.24f, 0.13f, 1f);
                Color light = new Color(0.52f, 0.34f, 0.18f, 1f);
                Color baseC = Color.Lerp(dark, light, n);
                baseC = Color.Lerp(baseC, baseC * 0.72f, grain * 0.45f);
                baseC *= Mathf.Lerp(0.88f, 1.08f, band);
                float edge = Mathf.Clamp01(Mathf.Min(y, h - 1 - y) / (h * 0.22f));
                baseC *= Mathf.Lerp(0.5f, 1f, edge);
                // Subtle horizontal polish sheen
                float sheen = Mathf.Pow(Mathf.Sin((u + v * 0.08f) * Mathf.PI), 8f) * 0.12f;
                baseC += new Color(sheen, sheen * 0.95f, sheen * 0.8f, 0f);
                tex.SetPixel(x, y, baseC);
            }
        }

        tex.Apply();
        tex.hideFlags = HideFlags.HideAndDontSave;
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }

    private void Update()
    {
        RefreshPrisonAlertPulse();
        TryOpsMapScrollZoom();
        if (_codexModalRoot != null && _codexModalRoot.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            HideCodexBook();
    }

    private void LateUpdate()
    {
        if (_aowTimesLayoutApplyFrames <= 0 || _aowTimesRect == null)
            return;
        _aowTimesLayoutApplyFrames--;
        ApplyAoWTimesLayoutNow();
    }

    /// <summary>Refresh portrait fitting + manual slot placement after BottomBar size settles.</summary>
    private void SyncAoWTurnStripToBottomBar()
    {
        if (!_enableAoWCharacterHud || _aowTurnStripRt == null)
            return;
        RectTransform bb = GameObject.Find("BottomBar")?.GetComponent<RectTransform>();
        if (bb == null)
            return;
        float h = bb.rect.height;
        if (h < 1f)
            h = Mathf.Max(80f, _bottomBarSizeDelta.y);
        h = Mathf.Max(80f, h);

        if (_aowPortraitClusterRt != null)
        {
            float clusterH = h + Mathf.Max(0f, _aowPortraitClusterExtraHeightPx);
            Vector2 psd = _aowPortraitClusterRt.sizeDelta;
            _aowPortraitClusterRt.sizeDelta = new Vector2(psd.x, clusterH);
            Transform ps = _aowPortraitClusterRt.Find("PortraitSlot");
            if (ps != null)
            {
                LayoutElement ple = ps.GetComponent<LayoutElement>();
                if (ple != null)
                {
                    ple.preferredHeight = clusterH;
                    ple.minHeight = clusterH;
                }
            }
        }

        ApplyAoWPortraitFitToStrip(h);
        ApplyAoWPortraitClusterAnchoredPosition();
        ApplyAoWTurnStripAnchoredPosition();

        _aowTimesLayoutApplyFrames = 8;
        ApplyAoWTimesLayoutNow();
    }

    /// <summary>Manual placement for turn controls slots (matches runtime tuning from Inspector).</summary>
    private void ApplyAoWTurnStripAnchoredPosition()
    {
        if (_aowTurnStripRt == null)
            return;
        float dpi = Screen.dpi;
        if (dpi <= 1f)
            dpi = Mathf.Max(24f, _aowFallbackDpi);
        float cmToPx = 0.3937008f * dpi;
        float timesRightPx = Mathf.Max(0f, _aowTimesNudgeRightCm) * cmToPx;
        float timesUpPx = _aowTimesNudgeUpCm * cmToPx;
        float nextTurnRightPx = Mathf.Max(0f, _aowNextTurnNudgeRightCm) * cmToPx;
        float nextTurnUpPx = Mathf.Max(0f, _aowNextTurnNudgeUpCm) * cmToPx;
        if (_aowTimesSlotRt != null)
        {
            _aowTimesSlotRt.anchorMin = new Vector2(0f, 1f);
            _aowTimesSlotRt.anchorMax = new Vector2(0f, 1f);
            _aowTimesSlotRt.pivot = new Vector2(0.5f, 0.5f);
            _aowTimesSlotRt.sizeDelta = _aowTimesSlotSize;
            _aowTimesSlotRt.anchoredPosition = _aowTimesSlotAnchoredPosition + _aowHudOffset + new Vector2(timesRightPx, timesUpPx);
        }
        if (_aowNextTurnSlotRt != null)
        {
            _aowNextTurnSlotRt.sizeDelta = _aowNextTurnSlotSize;
            _aowNextTurnSlotRt.pivot = new Vector2(0.5f, 0.5f);
            float nextY = _aowNextTurnSlotAnchoredPosition.y + _aowHudOffset.y + nextTurnUpPx;
            bool didSnap = false;
            if (_aowNextTurnSnapLeftOfPortrait && _aowPortraitClusterRt != null && _aowHudRoot != null)
            {
                RectTransform hudRt = _aowHudRoot.GetComponent<RectTransform>();
                if (hudRt != null && TryComputeNextTurnAnchoredSnapToPortrait(hudRt, out Vector2 snapPos))
                {
                    _aowNextTurnSlotRt.anchorMin = new Vector2(0.5f, 0.5f);
                    _aowNextTurnSlotRt.anchorMax = new Vector2(0.5f, 0.5f);
                    _aowNextTurnSlotRt.anchoredPosition = snapPos;
                    didSnap = true;
                }
            }
            if (!didSnap)
            {
                if (_aowNextTurnLayoutFromStripRight)
                {
                    // Anchor top-right of AoW (same as strip): negative X moves the slot left from the bottom bar's right edge.
                    // Do not add nextTurnRightPx here — _aowNextTurnNudgeRightCm converts to ~37 px per cm and values like 15 cm
                    // wipe out -_aowNextTurnCenterXPxFromStripRight, flipping X positive and floating the control off-screen right.
                    _aowNextTurnSlotRt.anchorMin = new Vector2(1f, 1f);
                    _aowNextTurnSlotRt.anchorMax = new Vector2(1f, 1f);
                    float fromRight = -_aowNextTurnCenterXPxFromStripRight + _aowHudOffset.x + _aowNextTurnExtraOffsetXPx;
                    _aowNextTurnSlotRt.anchoredPosition = new Vector2(fromRight, nextY);
                }
                else
                {
                    _aowNextTurnSlotRt.anchorMin = new Vector2(0f, 1f);
                    _aowNextTurnSlotRt.anchorMax = new Vector2(0f, 1f);
                    _aowNextTurnSlotRt.anchoredPosition =
                        _aowNextTurnSlotAnchoredPosition + _aowHudOffset + new Vector2(nextTurnRightPx + _aowNextTurnExtraOffsetXPx, nextTurnUpPx);
                }
            }
        }
    }

    /// <summary>
    /// Places Next Turn centered horizontally so its right side stays left of the portrait’s left edge (prevents overlap when portrait moves).
    /// Uses bounds in AoW root space; requires both under the same parent <see cref="_aowHudRoot"/>.
    /// </summary>
    private bool TryComputeNextTurnAnchoredSnapToPortrait(RectTransform hudRt, out Vector2 anchoredPosition)
    {
        anchoredPosition = default;
        if (_aowPortraitClusterRt == null || hudRt == null)
            return false;
        Bounds pb = RectTransformUtility.CalculateRelativeRectTransformBounds(hudRt.transform, _aowPortraitClusterRt.transform);
        float gap = Mathf.Max(4f, _aowNextTurnPortraitGapPx);
        float halfW = _aowNextTurnSlotSize.x * 0.5f;
        float cx = pb.min.x - gap - halfW;
        float cy = pb.center.y + _aowNextTurnSnapVerticalOffsetPx;
        anchoredPosition = new Vector2(cx, cy);
        return true;
    }

    /// <summary>Portrait cluster (ring + radials): anchor nudge from bottom-right; turn strip follows on X.</summary>
    private void ApplyAoWPortraitClusterAnchoredPosition()
    {
        if (_aowPortraitClusterRt == null)
            return;
        float dpi = Screen.dpi;
        if (dpi <= 1f)
            dpi = Mathf.Max(24f, _aowFallbackDpi);
        float cmToPx = 0.3937008f * dpi;
        float rightPx = _aowRightMarginCm * cmToPx;
        float bottomPx = _aowBottomMarginCm * cmToPx;
        float portraitRightPx = Mathf.Max(0f, _aowPortraitNudgeRightCm) * cmToPx;
        float portraitDownPx = Mathf.Max(0f, _aowPortraitNudgeDownCm) * cmToPx;
        _aowPortraitClusterRt.anchoredPosition = new Vector2(
            -rightPx + _aowPortraitClusterOffset.x + portraitRightPx,
            bottomPx + _aowPortraitClusterOffset.y - portraitDownPx);
    }

    /// <summary>
    /// Scales the boss portrait ring to fit inside the turn strip so the strip stays on the wood bar (no vertical overflow).
    /// </summary>
    private void ApplyAoWPortraitFitToStrip(float stripH)
    {
        if (_aowPortraitRingRt == null)
            return;
        stripH = Mathf.Max(40f, stripH);
        float fitH = stripH + Mathf.Max(0f, _aowPortraitFitExtraBleedPx);
        float pad = 4f;
        float ringD = Mathf.Min(_aowPortraitSize.x, _aowPortraitSize.y, fitH - pad);
        ringD = Mathf.Max(56f, ringD);
        _aowPortraitRingRt.sizeDelta = new Vector2(ringD, ringD);

        Transform hitTr = _aowPortraitRingRt.Find("PortraitHit");
        if (hitTr != null)
        {
            RectTransform hrt = hitTr.GetComponent<RectTransform>();
            if (hrt != null)
                hrt.sizeDelta = new Vector2(ringD, ringD);
        }

        float portraitR = ringD * 0.5f;
        float radialBtnD = Mathf.Clamp(_aowRadialButtonDiameter, 28f, 92f);
        float r = _aowRadialRadius > 1e-3f
            ? Mathf.Max(10f, _aowRadialRadius)
            : portraitR + radialBtnD * 0.5f + Mathf.Max(0f, _aowRadialEdgeGap);
        Vector2[] offsets =
        {
            new Vector2(-r, 0f),
            new Vector2(-r * 0.70f, r * 0.70f),
            new Vector2(0f, r),
        };
        string[] radialNames = { "Action1Btn", "Action2Btn", "Action3Btn" };
        Vector2 half = new Vector2(ringD, ringD) * 0.5f;
        for (int i = 0; i < radialNames.Length; i++)
        {
            Transform t = _aowPortraitRingRt.Find(radialNames[i]);
            if (t == null)
                continue;
            RectTransform rt = t.GetComponent<RectTransform>();
            if (rt != null)
                rt.anchoredPosition = half + offsets[i];
        }

        if (_aowPortraitSlotLe != null)
        {
            float cellW = Mathf.Max(_aowPortraitSize.x, ringD + r + 18f);
            _aowPortraitSlotLe.minWidth = cellW;
            _aowPortraitSlotLe.preferredWidth = cellW;
            if (_aowPortraitClusterRt != null)
            {
                Vector2 csd = _aowPortraitClusterRt.sizeDelta;
                _aowPortraitClusterRt.sizeDelta = new Vector2(cellW, csd.y);
            }
        }
    }

    /// <summary>Date/weather: right-aligned, vertically centered in TimesSlot (wood strip between gold bands).</summary>
    private void ApplyAoWTimesLayoutNow()
    {
        if (_aowTimesRect == null)
            return;
        float dpi = Screen.dpi > 1f ? Screen.dpi : Mathf.Max(24f, _aowFallbackDpi);
        float extraDownPx = _aowTimesExtraDownCm * 0.3937008f * dpi;
        // Hard runtime lift so date text visibly moves up even if scene has stale serialized offsets.
        float y = _aowTimesTopInsetPx + extraDownPx - _aowTimesForceDownPx + 24f;
        float x = _aowTurnControlsXOffset + _aowTimesXOffset;
        _aowTimesRect.anchorMin = new Vector2(1f, 0.5f);
        _aowTimesRect.anchorMax = new Vector2(1f, 0.5f);
        _aowTimesRect.pivot = new Vector2(1f, 0.5f);
        _aowTimesRect.sizeDelta = new Vector2(220f, 88f);
        _aowTimesRect.anchoredPosition = new Vector2(x, y);
    }

    /// <summary>
    /// Scene default: MainArea uses a stretch Image as dimmed backdrop with raycastTarget enabled,
    /// covering the whole planning content. That intercepts pointer events meant for child controls.
    /// </summary>
    private static void DisableMainAreaBackgroundRaycast(GameObject mainAreaGo)
    {
        if (mainAreaGo == null)
            return;
        Image bg = mainAreaGo.GetComponent<Image>();
        if (bg != null)
            bg.raycastTarget = false;
    }

    /// <summary>Fill center area with black + optional chrome image (Resources/UI/Chrome/main).</summary>
    private static void ApplyMainAreaBackgroundSkin(GameObject mainAreaGo)
    {
        if (mainAreaGo == null)
            return;
        Image bg = mainAreaGo.GetComponent<Image>();
        if (bg == null)
            bg = mainAreaGo.AddComponent<Image>();
        Sprite main = LoadMainAreaBackgroundSprite();
        bg.sprite = main;
        bg.color = main != null ? Color.white : Color.black;
        bg.type = main != null && main.border.sqrMagnitude > 0.01f ? Image.Type.Sliced : Image.Type.Simple;
        bg.preserveAspect = false;
        bg.raycastTarget = false;
    }

    private static Sprite LoadMainAreaBackgroundSprite()
    {
        // `main.png` is imported as Multiple in this project, so prefer exact slices first.
        Sprite[] mainSlices = Resources.LoadAll<Sprite>(MainAreaBackgroundSpritePath);
        if (mainSlices != null && mainSlices.Length > 0)
        {
            Sprite best = mainSlices[0];
            float bestArea = best.rect.width * best.rect.height;
            for (int i = 1; i < mainSlices.Length; i++)
            {
                Sprite slice = mainSlices[i];
                if (slice == null)
                    continue;
                float area = slice.rect.width * slice.rect.height;
                if (area > bestArea)
                {
                    best = slice;
                    bestArea = area;
                }
            }
            return best;
        }

        Sprite s = TryLoadHudSprite(MainAreaBackgroundSpritePath);
        if (s != null)
            return s;
        s = TryLoadHudSprite("UI/Chrome/Main");
        if (s != null)
            return s;

        Texture2D t = Resources.Load<Texture2D>(MainAreaBackgroundSpritePath);
        if (t == null)
            t = Resources.Load<Texture2D>("UI/Chrome/Main");
        if (t == null)
            t = Resources.Load<Texture2D>("MainMenuBackground");
        if (t == null)
        {
            Debug.LogWarning("PlanningShellController: missing Resources/UI/Chrome/main.png (or Main.png).");
            return null;
        }
        return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
    }

    /// <summary>
    /// Letterboxing / aspect mismatch exposes the camera clear color; default Unity blue shows around UI edges.
    /// </summary>
    private static void EnsurePlanningCameraBackgroundBlack()
    {
        Camera cam = Camera.main;
        if (cam == null)
            cam = Object.FindFirstObjectByType<Camera>();
        if (cam == null)
            return;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
    }

    /// <summary>Root Canvas scale (0,0,0) breaks layout — reset to one.</summary>
    private static void EnsureRootCanvasScale()
    {
        RectTransform canvasRt = GameObject.Find("Canvas")?.GetComponent<RectTransform>();
        if (canvasRt == null)
            return;
        if (canvasRt.localScale.sqrMagnitude < 1e-6f)
            canvasRt.localScale = Vector3.one;
    }

    /// <summary>
    /// Planning scene shipped with <see cref="CanvasScaler.ScaleMode.ConstantPixelSize"/> — on 1080p+ screens
    /// all TMP/layout sizes stay ~12–16 <b>hardware pixels</b>, so Ops looks microscopic and “blurry” when scaled.
    /// Force scale-with-screen so font/layout units track display size.
    /// </summary>
    private static void EnsurePlanningCanvasScaler()
    {
        GameObject canvasGo = GameObject.Find("Canvas");
        if (canvasGo == null)
            return;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        if (scaler == null)
            return;

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        // Prefer matching reference width so the planning shell spans the full window width (fewer side gutters).
        scaler.matchWidthOrHeight = 0f;
        scaler.scaleFactor = 1f;

        RectTransform canvasRt = canvasGo.GetComponent<RectTransform>();
        if (canvasRt != null)
        {
            canvasRt.anchorMin = Vector2.zero;
            canvasRt.anchorMax = Vector2.one;
            canvasRt.offsetMin = Vector2.zero;
            canvasRt.offsetMax = Vector2.zero;
            canvasRt.pivot = new Vector2(0.5f, 0.5f);
            canvasRt.anchoredPosition = Vector2.zero;
        }
    }

    private static void EnsurePlanningCanvasOrder()
    {
        GameObject canvasGo = GameObject.Find("Canvas");
        if (canvasGo == null)
            return;

        Transform root = canvasGo.transform;
        Transform main = root.Find("MainArea");
        Transform top = root.Find("TopTabsBar");
        Transform bottom = root.Find("BottomBar");
        if (main != null)
            main.SetSiblingIndex(0);
        if (top != null)
            top.SetSiblingIndex(1);
        if (bottom != null)
            bottom.SetSiblingIndex(2);
    }

    /// <summary>
    /// Replaces any manual tab objects under TopTabsBar with nine wired tabs (avoids duplicate names / wrong counts).
    /// </summary>
    private void BuildTopTabStrip()
    {
        GameObject barGo = GameObject.Find("TopTabsBar");
        if (barGo == null)
        {
            Debug.LogWarning("PlanningShellController: TopTabsBar not found — tab strip skipped.");
            return;
        }

        RectTransform barRt = barGo.GetComponent<RectTransform>();
        if (barRt == null)
        {
            Debug.LogError("PlanningShellController: TopTabsBar has no RectTransform — tab strip skipped.");
            return;
        }

        Image barBackground = barGo.GetComponent<Image>();
        if (barBackground == null)
            barBackground = barGo.AddComponent<Image>();
        Sprite topWood = GetTopTabsBarWoodSprite();
        if (topWood != null)
        {
            barBackground.sprite = topWood;
            barBackground.type = topWood.border.sqrMagnitude > 0.01f ? Image.Type.Sliced : Image.Type.Simple;
            barBackground.preserveAspect = false;
            barBackground.color = Color.white;
            barBackground.raycastTarget = false;
        }
        else
        {
            barBackground.sprite = null;
            barBackground.color = new Color(0f, 0f, 0f, 1f);
        }

        for (int i = barRt.childCount - 1; i >= 0; i--)
            Destroy(barRt.GetChild(i).gameObject);

        _tabVisuals.Clear();

        HorizontalLayoutGroup h = barGo.GetComponent<HorizontalLayoutGroup>();
        if (h == null)
            h = barGo.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 4f;
        h.padding = new RectOffset(10, 10, 6, 6);
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = false;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = false;

        // Spacer eats leftover width on the left so tab buttons sit to the right (toward TopBarMetrics / cash).
        GameObject tabSpacerGo = new GameObject("TabLeftSpacer");
        tabSpacerGo.transform.SetParent(barRt, false);
        LayoutElement tabSpacerLe = tabSpacerGo.AddComponent<LayoutElement>();
        tabSpacerLe.minWidth = 0f;
        tabSpacerLe.preferredWidth = 0f;
        tabSpacerLe.flexibleWidth = 1f;
        tabSpacerLe.preferredHeight = 42f;
        tabSpacerLe.flexibleHeight = 0f;

        Canvas barCanvas = barGo.GetComponent<Canvas>();
        if (barCanvas == null)
            barCanvas = barGo.AddComponent<Canvas>();
        barCanvas.overrideSorting = true;
        barCanvas.sortingOrder = 40;
        if (barGo.GetComponent<GraphicRaycaster>() == null)
            barGo.AddComponent<GraphicRaycaster>();

        (string objectName, string label, PlanningTabId tab)[] tabs =
        {
            ("OverviewTabButton", "Overview", PlanningTabId.Overview),
            ("NewsTabButton", "News", PlanningTabId.News),
            ("IntelligenceTabButton", "Intel", PlanningTabId.Intelligence),
            ("PersonnelTabButton", "Crew", PlanningTabId.Personnel),
            ("OperationsTabButton", "Ops", PlanningTabId.Operations),
            ("DiplomacyTabButton", "Diplo", PlanningTabId.Diplomacy),
            ("BusinessTabButton", "Biz", PlanningTabId.Business),
            ("LogisticsTabButton", "Log", PlanningTabId.Logistics),
            ("LegalTabButton", "Legal", PlanningTabId.Legal),
            ("FinanceTabButton", "Fin", PlanningTabId.Finance),
        };

        Sprite tabBtnSprite = GetTopTabButtonSprite();
        Sprite tabBtnHoverSprite = GetTopTabButtonHoverSprite();
        Sprite tabBtnPressedSprite = GetTopTabButtonPressedSprite();

        foreach (var entry in tabs)
        {
            GameObject go = new GameObject(entry.objectName);
            go.transform.SetParent(barRt, false);

            LayoutElement le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 72f;
            le.minWidth = 52f;
            le.preferredHeight = 42f;

            Image bg = go.AddComponent<Image>();
            Color normalBg;
            TopTabButtonSpriteDriver tabSpriteDriver = null;
            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;
            if (tabBtnSprite != null)
            {
                bg.sprite = tabBtnSprite;
                bg.type = tabBtnSprite.border.sqrMagnitude > 0.01f ? Image.Type.Sliced : Image.Type.Simple;
                bg.preserveAspect = false;
                normalBg = Color.white;
                bg.color = normalBg;
                bg.raycastTarget = true;
                if (tabBtnHoverSprite != null || tabBtnPressedSprite != null)
                {
                    // SpriteSwap on nested Canvas tabs is flaky; drive sprites from pointer events instead.
                    btn.transition = Selectable.Transition.None;
                    ColorBlock cb = btn.colors;
                    cb.normalColor = Color.white;
                    cb.highlightedColor = Color.white;
                    cb.pressedColor = Color.white;
                    cb.selectedColor = Color.white;
                    cb.disabledColor = PlanningUiButtonStyle.RectDisabled;
                    cb.colorMultiplier = 1f;
                    btn.colors = cb;
                    tabSpriteDriver = go.AddComponent<TopTabButtonSpriteDriver>();
                    tabSpriteDriver.Configure(bg, tabBtnSprite, tabBtnHoverSprite, tabBtnPressedSprite);
                }
                else
                    PlanningUiButtonStyle.ApplyColorTint(btn, normalBg);
            }
            else
            {
                normalBg = PlanningUiButtonStyle.RectFill;
                bg.color = normalBg;
                bg.raycastTarget = true;
                PlanningUiButtonStyle.ApplyStandardRectButton(btn, bg);
            }

            GameObject labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            RectTransform lrt = labelGo.AddComponent<RectTransform>();
            StretchFull(lrt);
            TextMeshProUGUI tmp = labelGo.AddComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null)
                tmp.font = TMP_Settings.defaultFontAsset;
            tmp.text = entry.label;
            tmp.fontSize = 17f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = PlanningUiButtonStyle.LabelPrimary;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.margin = new Vector4(3f, 2f, 3f, 2f);

            // Legal tab: add illustrative codex-book icon inside the tab button.
            if (entry.tab == PlanningTabId.Legal)
            {
                Sprite[] codexSprites = Resources.LoadAll<Sprite>(CodexBookLegacyResourcePath);
                Sprite codexIcon = codexSprites != null && codexSprites.Length > 0 ? codexSprites[0] : null;
                if (codexIcon != null)
                {
                    GameObject iconGo = new GameObject("Icon");
                    iconGo.transform.SetParent(go.transform, false);
                    RectTransform irt = iconGo.AddComponent<RectTransform>();
                    irt.anchorMin = new Vector2(0f, 0.5f);
                    irt.anchorMax = new Vector2(0f, 0.5f);
                    irt.pivot = new Vector2(0f, 0.5f);
                    irt.anchoredPosition = new Vector2(8f, 0f);
                    irt.sizeDelta = new Vector2(24f, 24f);

                    Image iconImg = iconGo.AddComponent<Image>();
                    iconImg.sprite = codexIcon;
                    iconImg.preserveAspect = true;
                    iconImg.raycastTarget = false;
                    iconImg.color = Color.white;

                    // Reserve space for icon; symmetric vertical insets so "Legal" stays visually centered in the text band.
                    lrt.offsetMin = new Vector2(30f, 2f);
                    lrt.offsetMax = new Vector2(-5f, -2f);
                    tmp.alignment = TextAlignmentOptions.Center;
                }
            }

            _tabVisuals[entry.tab] = new TabVisualState
            {
                Background = bg,
                NormalBg = normalBg,
                TabSpriteDriver = tabSpriteDriver
            };

            WireTabButton(go, entry.tab);
        }

        // Metrics in the top bar itself (under TopTabsBar).
        GameObject metricsGo = new GameObject("TopBarMetrics");
        metricsGo.transform.SetParent(barRt, false);
        RectTransform metricsRt = metricsGo.AddComponent<RectTransform>();
        // Pin metrics to bottom-right of TopTabsBar to avoid overlapping other HUD elements.
        metricsRt.anchorMin = new Vector2(1f, 0f);
        metricsRt.anchorMax = new Vector2(1f, 0f);
        metricsRt.pivot = new Vector2(1f, 0f);
        metricsRt.sizeDelta = new Vector2(900f, 60f);
        // Move the whole metrics block left together (negative X from bottom-right anchor).
        metricsRt.anchoredPosition = new Vector2(-245f, -2f);

        LayoutElement ignore = metricsGo.AddComponent<LayoutElement>();
        ignore.ignoreLayout = true;

        HorizontalLayoutGroup hmetrics = metricsGo.AddComponent<HorizontalLayoutGroup>();
        hmetrics.spacing = 24f;
        hmetrics.childAlignment = TextAnchor.MiddleRight;
        hmetrics.childControlWidth = false;
        hmetrics.childControlHeight = false;
        hmetrics.childForceExpandWidth = false;
        hmetrics.childForceExpandHeight = false;

        // Each item is: "value" then "icon".
        CreateTopMetricPair(metricsGo.transform,
            "DirtyCash",
            out _metricDirtyCashText,
            out _metricDirtyCashIcon,
            LoadMetricsBarSprite("UI/Metrics/dirty_cash"),
            out _);

        CreateTopMetricPair(metricsGo.transform,
            "AccountCash",
            out _metricAccountCashText,
            out _metricAccountCashIcon,
            LoadMetricsBarSprite("UI/Metrics/account_cash"),
            out _);

        CreateTopMetricPair(metricsGo.transform,
            "FamilyRep",
            out _metricFamilyRepText,
            out _metricFamilyRepIcon,
            LoadMetricsBarSprite("UI/Metrics/family_rep"),
            out _familyRepPairRect);

        CreateTopMetricPair(metricsGo.transform,
            "CrewMorale",
            out _metricCrewMoraleText,
            out _metricCrewMoraleIcon,
            LoadMetricsBarSprite("UI/Metrics/crew_morale"),
            out _);

        // After all children are built, keep wood visible (or black fallback if sprite failed to load).
        if (barBackground != null)
        {
            if (barBackground.sprite != null)
                barBackground.color = Color.white;
            else
                barBackground.color = new Color(0f, 0f, 0f, 1f);
        }

        // After one layout pass, nudge only FamilyRep slightly left (visual tweak).
        StartCoroutine(NudgeFamilyRepNextFrame());
    }

    private System.Collections.IEnumerator NudgeFamilyRepNextFrame()
    {
        yield return null;
        if (_familyRepPairRect != null)
            _familyRepPairRect.anchoredPosition += new Vector2(-12f, 0f);
    }

    private void CreateTopMetricPair(
        Transform parent,
        string name,
        out TextMeshProUGUI valueText,
        out Image iconImg,
        Sprite sprite,
        out RectTransform pairRect)
    {
        GameObject pairGo = new GameObject(name);
        pairGo.transform.SetParent(parent, false);

        // Pair layout so we always get: numeric value -> icon.
        HorizontalLayoutGroup pairLayout = pairGo.AddComponent<HorizontalLayoutGroup>();
        pairLayout.spacing = 14f;
        pairLayout.childAlignment = TextAnchor.MiddleCenter;
        pairLayout.childControlWidth = false;
        pairLayout.childControlHeight = false;
        pairLayout.childForceExpandWidth = false;
        pairLayout.childForceExpandHeight = false;

        pairRect = pairGo.GetComponent<RectTransform>();
        if (pairRect == null)
            pairRect = pairGo.AddComponent<RectTransform>();

        GameObject tGo = new GameObject("Value");
        tGo.transform.SetParent(pairGo.transform, false);
        RectTransform tRt = tGo.AddComponent<RectTransform>();
        tRt.sizeDelta = new Vector2(132f, 32f);
        LayoutElement tLe = tGo.AddComponent<LayoutElement>();
        tLe.preferredWidth = 132f;
        tLe.preferredHeight = 32f;

        valueText = tGo.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            valueText.font = TMP_Settings.defaultFontAsset;
        valueText.fontSize = 15f;
        valueText.alignment = TextAlignmentOptions.Right;
        valueText.color = new Color(0.95f, 0.95f, 0.92f, 1f);
        valueText.raycastTarget = false;
        valueText.richText = false;

        GameObject iGo = new GameObject("Icon");
        iGo.transform.SetParent(pairGo.transform, false);
        RectTransform iRt = iGo.AddComponent<RectTransform>();
        iRt.sizeDelta = new Vector2(34f, 34f);
        LayoutElement iLe = iGo.AddComponent<LayoutElement>();
        iLe.preferredWidth = 34f;
        iLe.preferredHeight = 34f;

        iconImg = iGo.AddComponent<Image>();
        iconImg.sprite = sprite;
        iconImg.type = Image.Type.Simple;
        iconImg.preserveAspect = true;
        iconImg.color = Color.white;
        iconImg.raycastTarget = false;
    }

    private void ApplyTabSelectionVisual(PlanningTabId selected)
    {
        foreach (KeyValuePair<PlanningTabId, TabVisualState> kv in _tabVisuals)
        {
            bool on = kv.Key == selected;
            TabVisualState state = kv.Value;
            if (state.Background != null)
            {
                if (state.TabSpriteDriver != null)
                {
                    // Tinting the wood Image hides the hover/pressed sprites — drive selection via sprites instead.
                    state.Background.color = Color.white;
                    state.TabSpriteDriver.SetSelected(on);
                }
                else
                {
                    state.Background.color = on
                        ? PlanningUiButtonStyle.TabSelectedFill
                        : state.NormalBg;
                }
            }
        }
    }

    private void BuildUi(RectTransform mainArea)
    {
        for (int i = mainArea.childCount - 1; i >= 0; i--)
        {
            Transform ch = mainArea.GetChild(i);
            if (ch != null && ch.name == "PlanningShell")
                Destroy(ch.gameObject);
        }

        GameObject shell = new GameObject("PlanningShell");
        shell.transform.SetParent(mainArea, false);
        RectTransform shellRt = shell.AddComponent<RectTransform>();
        StretchFull(shellRt);

        VerticalLayoutGroup v = shell.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(12, 12, 12, 12);
        v.spacing = 8f;
        v.childAlignment = TextAnchor.UpperCenter;
        v.childControlHeight = true;
        v.childControlWidth = true;
        v.childForceExpandHeight = false;
        v.childForceExpandWidth = true;

        _titleText = CreateTmp(shell.transform, "Title", 26f, FontStyles.Bold);
        LayoutElement leTitle = _titleText.gameObject.AddComponent<LayoutElement>();
        leTitle.preferredHeight = 40f;

        _contextText = CreateTmp(shell.transform, "ContextStrip", 16f, FontStyles.Normal);
        _contextText.textWrappingMode = TextWrappingModes.Normal;
        LayoutElement leCtx = _contextText.gameObject.AddComponent<LayoutElement>();
        leCtx.preferredHeight = 52f;

        _topMetricsText = CreateTmp(shell.transform, "TopMetrics", 13f, FontStyles.Normal);
        _topMetricsText.alignment = TextAlignmentOptions.TopRight;
        LayoutElement leTop = _topMetricsText.gameObject.AddComponent<LayoutElement>();
        leTop.preferredHeight = 28f;
        // We show metrics in the actual top UI bar (TopTabsBar), so hide this internal shell text.
        _topMetricsText.gameObject.SetActive(false);

        BuildMissionPrepRow(shell.transform);

        GameObject row = new GameObject("ThreeColumnRow");
        row.transform.SetParent(shell.transform, false);
        HorizontalLayoutGroup h = row.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 10f;
        h.childAlignment = TextAnchor.UpperLeft;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = true;

        LayoutElement leRow = row.AddComponent<LayoutElement>();
        leRow.flexibleHeight = 1f;
        leRow.minHeight = 180f;

        GameObject leftCol = new GameObject("LeftColumn");
        leftCol.transform.SetParent(row.transform, false);
        _leftColumnRoot = leftCol.transform;
        VerticalLayoutGroup leftV = leftCol.AddComponent<VerticalLayoutGroup>();
        leftV.spacing = 10f;
        leftV.padding = new RectOffset(0, 0, 0, 0);
        leftV.childAlignment = TextAnchor.UpperLeft;
        // Must be true so flexible-height children (e.g. crew roster list) receive vertical space; false collapses them to ~0.
        leftV.childControlHeight = true;
        leftV.childControlWidth = true;
        // Allow a flexible spacer to push the codex book to the bottom (enabled only on Legal tab).
        leftV.childForceExpandHeight = true;
        leftV.childForceExpandWidth = true;

        LayoutElement leLC = leftCol.AddComponent<LayoutElement>();
        leLC.preferredWidth = 260f;
        leLC.flexibleWidth = 0f;
        leLC.flexibleHeight = 1f;

        // Center column is a later sibling and its Viewport Image is raycast-enabled; it can win hit tests over
        // the left strip if rects overlap. Nested Canvas sorts this subtree above the center column.
        EnsureLeftColumnRaycastPriorityCanvas();

        // Spacer used to push the Legal left-side block lower (enabled only on Legal tab).
        GameObject spacerGo = new GameObject("LegalTopSpacer");
        spacerGo.transform.SetParent(leftCol.transform, false);
        _legalLeftTopSpacer = spacerGo.AddComponent<LayoutElement>();
        _legalLeftTopSpacer.preferredHeight = 140f;
        _legalLeftTopSpacer.minHeight = 0f;
        _legalLeftTopSpacer.flexibleHeight = 0f;
        spacerGo.SetActive(false);

        _legalCodexToggleButton = CreateBarButton(leftCol.transform, "Open Codex");
        RectTransform lbr = _legalCodexToggleButton.GetComponent<RectTransform>();
        if (lbr != null)
            lbr.sizeDelta = new Vector2(0f, 84f);
        LayoutElement codexLe = _legalCodexToggleButton.GetComponent<LayoutElement>();
        if (codexLe != null)
        {
            codexLe.preferredWidth = 260f;
            codexLe.minWidth = 260f;
            codexLe.preferredHeight = 84f;
            codexLe.minHeight = 84f;
        }
        _legalCodexToggleButton.onClick.RemoveAllListeners();
        _legalCodexToggleButton.onClick.AddListener(ToggleLegalSidebarCodex);
        TryApplyCodexButtonArt(_legalCodexToggleButton);
        _legalCodexToggleButton.gameObject.SetActive(false);

        _leftText = CreateTmp(leftCol.transform, "LeftPanel", 15f, FontStyles.Normal);
        _leftText.alignment = TextAlignmentOptions.TopLeft;
        _leftText.textWrappingMode = TextWrappingModes.Normal;
        LayoutElement leL = _leftText.gameObject.AddComponent<LayoutElement>();
        leL.flexibleWidth = 1f;
        leL.flexibleHeight = 1f;

        GameObject personnelListGo = new GameObject("PersonnelMemberList");
        personnelListGo.transform.SetParent(leftCol.transform, false);
        personnelListGo.transform.SetSiblingIndex(_leftText.transform.GetSiblingIndex() + 1);
        _personnelMemberListRoot = personnelListGo.transform;
        VerticalLayoutGroup pmV = personnelListGo.AddComponent<VerticalLayoutGroup>();
        pmV.spacing = 6f;
        pmV.padding = new RectOffset(0, 0, 0, 0);
        pmV.childAlignment = TextAnchor.UpperLeft;
        pmV.childControlHeight = true;
        pmV.childControlWidth = true;
        pmV.childForceExpandWidth = true;
        pmV.childForceExpandHeight = false;
        LayoutElement lePm = personnelListGo.AddComponent<LayoutElement>();
        lePm.flexibleWidth = 1f;
        lePm.flexibleHeight = 1f;
        lePm.minHeight = 120f;
        lePm.preferredHeight = 280f;

        TextMeshProUGUI pmHead = CreateTmp(personnelListGo.transform, "MembersTitle", 15f, FontStyles.Bold);
        pmHead.text = "Members";
        pmHead.alignment = TextAlignmentOptions.Left;
        pmHead.raycastTarget = false;
        LayoutElement lePmHead = pmHead.gameObject.AddComponent<LayoutElement>();
        lePmHead.preferredHeight = 26f;
        lePmHead.minHeight = 22f;
        lePmHead.flexibleHeight = 0f;

        personnelListGo.SetActive(false);

        // Flexible spacer used to push the codex book/caption to the very bottom (Legal only).
        GameObject bottomSpacerGo = new GameObject("LegalBottomSpacer");
        bottomSpacerGo.transform.SetParent(leftCol.transform, false);
        _legalLeftBottomSpacer = bottomSpacerGo.AddComponent<LayoutElement>();
        _legalLeftBottomSpacer.preferredHeight = 0f;
        _legalLeftBottomSpacer.minHeight = 0f;
        _legalLeftBottomSpacer.flexibleHeight = 1f;
        bottomSpacerGo.SetActive(false);

        // Codex book block pinned to bottom (book button + tight caption under it).
        GameObject codexBlockGo = new GameObject("LegalCodexBlock");
        codexBlockGo.transform.SetParent(leftCol.transform, false);
        RectTransform blockRt = codexBlockGo.AddComponent<RectTransform>();
        blockRt.sizeDelta = new Vector2(0f, 0f);
        LayoutElement blockLe = codexBlockGo.AddComponent<LayoutElement>();
        blockLe.preferredWidth = 260f;
        blockLe.minWidth = 260f;
        blockLe.preferredHeight = 186f; // book + caption + small spacing
        blockLe.minHeight = 186f;
        blockLe.flexibleWidth = 0f;
        blockLe.flexibleHeight = 0f;

        VerticalLayoutGroup blockV = codexBlockGo.AddComponent<VerticalLayoutGroup>();
        blockV.spacing = 4f;
        blockV.padding = new RectOffset(0, 0, 0, 0);
        blockV.childAlignment = TextAnchor.LowerCenter;
        blockV.childControlWidth = true;
        blockV.childControlHeight = false;
        blockV.childForceExpandWidth = true;
        blockV.childForceExpandHeight = false;

        // Book button (bigger and anchored to the bottom of its block).
        GameObject bookGo = new GameObject("LegalCodexBookButton");
        bookGo.transform.SetParent(codexBlockGo.transform, false);
        RectTransform bookRt = bookGo.AddComponent<RectTransform>();
        bookRt.sizeDelta = new Vector2(0f, 160f);
        _legalCodexBookLe = bookGo.AddComponent<LayoutElement>();
        _legalCodexBookLe.preferredWidth = 260f;
        _legalCodexBookLe.minWidth = 260f;
        _legalCodexBookLe.preferredHeight = 160f;
        _legalCodexBookLe.minHeight = 160f;
        _legalCodexBookLe.flexibleWidth = 0f;
        _legalCodexBookLe.flexibleHeight = 0f;

        _legalCodexBookImage = bookGo.AddComponent<Image>();
        _legalCodexBookImage.preserveAspect = true;
        _legalCodexBookImage.raycastTarget = true;
        _legalCodexBookImage.color = Color.white;

        _legalCodexBookButton = bookGo.AddComponent<Button>();
        _legalCodexBookButton.targetGraphic = _legalCodexBookImage;
        _legalCodexBookButton.transition = Selectable.Transition.ColorTint;
        ColorBlock bookColors = _legalCodexBookButton.colors;
        bookColors.normalColor = Color.white;
        bookColors.highlightedColor = Color.Lerp(Color.white, PlanningUiButtonStyle.RectHighlight, 0.35f);
        bookColors.pressedColor = Color.Lerp(Color.white, PlanningUiButtonStyle.RectPressed, 0.45f);
        bookColors.selectedColor = Color.white;
        bookColors.disabledColor = PlanningUiButtonStyle.RectDisabled;
        bookColors.fadeDuration = 0.09f;
        _legalCodexBookButton.colors = bookColors;
        _legalCodexBookButton.onClick.RemoveAllListeners();
        _legalCodexBookButton.onClick.AddListener(ToggleLegalSidebarCodex);

        // Press feel like our other buttons.
        ButtonPressScale press = bookGo.AddComponent<ButtonPressScale>();
        press.SetScaleTarget(bookGo.transform);

        // Caption directly under the book (tight spacing controlled by the block layout group).
        GameObject captionGo = new GameObject("LegalCodexCaption");
        captionGo.transform.SetParent(codexBlockGo.transform, false);
        RectTransform capRt = captionGo.AddComponent<RectTransform>();
        capRt.sizeDelta = new Vector2(0f, 18f);
        LayoutElement capLe = captionGo.AddComponent<LayoutElement>();
        capLe.preferredWidth = 260f;
        capLe.minWidth = 260f;
        capLe.preferredHeight = 18f;
        capLe.minHeight = 18f;
        capLe.flexibleWidth = 0f;
        capLe.flexibleHeight = 0f;

        _legalCodexBookCaptionText = captionGo.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            _legalCodexBookCaptionText.font = TMP_Settings.defaultFontAsset;
        _legalCodexBookCaptionText.text = "LAW CODEX";
        _legalCodexBookCaptionText.fontSize = 12.5f;
        _legalCodexBookCaptionText.fontStyle = FontStyles.Bold;
        _legalCodexBookCaptionText.alignment = TextAlignmentOptions.Center;
        _legalCodexBookCaptionText.color = PlanningUiButtonStyle.LabelPrimary;
        _legalCodexBookCaptionText.raycastTarget = false;

        codexBlockGo.SetActive(false);

        RefreshLegalCodexBookIcon();

        _centerText = BuildScrollableCenterColumn(row.transform);
        BuildNewsNewspaperUi();

        _rightText = CreateTmp(row.transform, "RightPanel", 15f, FontStyles.Normal);
        _rightText.alignment = TextAlignmentOptions.TopLeft;
        _rightText.textWrappingMode = TextWrappingModes.Normal;
        LayoutElement leR = _rightText.gameObject.AddComponent<LayoutElement>();
        leR.preferredWidth = 220f;
        leR.flexibleWidth = 0f;
        leR.flexibleHeight = 1f;

        _threeColumnRow = row;
        BuildOpsStageOverlay(mainArea);
        BuildCodexBookModal();
        BuildOpsCrewAssignModal();
    }

    /// <summary>
    /// Legal sidebar only: closed codex / Open Codex control. Opens the spread or closes if already open.
    /// The modal open-book graphic does not wire this handler.
    /// </summary>
    private void ToggleLegalSidebarCodex()
    {
        if (_codexModalRoot != null && _codexModalRoot.activeSelf)
        {
            HideCodexBook();
            return;
        }

        ShowCodexBookAtPage(0);
    }

    private void RefreshLegalCodexBookIcon()
    {
        if (_legalCodexBookImage == null)
            return;

        Sprite book = LoadCodexSidebarBookSprite();
        _legalCodexBookImage.sprite = book;
        bool show = book != null && _current == PlanningTabId.Legal;
        if (_legalCodexBookImage != null)
            _legalCodexBookImage.gameObject.SetActive(show);
        if (_legalCodexBookCaptionText != null)
            _legalCodexBookCaptionText.gameObject.SetActive(show);
        // Parent block visibility follows the book image game object.
        if (_legalCodexBookImage != null && _legalCodexBookImage.transform.parent != null)
            _legalCodexBookImage.transform.parent.gameObject.SetActive(show);

        if (book == null && !_warnedMissingCodexBookSprite)
        {
            _warnedMissingCodexBookSprite = true;
            Debug.LogWarning("[Codex] Sidebar book sprite not found. Tried " + CodexSidebarBookResourcePath + ", " +
                             CodexBookLegacyResourcePath +
                             ". Ensure PNGs are under Assets/Resources/UI/Icons/ and imported as Sprite.");
        }
    }

    /// <summary>Small book on the Legal tab — same art as before the open-spread modal.</summary>
    private static Sprite LoadCodexSidebarBookSprite()
    {
        string sidebarPath = CodexSidebarBookResourcePath;
        string sidebarFileOnly = "Street Codex (Black Ledger)";
        string legacyBasePath = CodexBookLegacyResourcePath;
        string legacyFileNameOnly = "icon_codex_open_sheet";
        string[] candidates =
        {
            sidebarPath,
            sidebarPath + "_0",
            sidebarFileOnly,
            sidebarFileOnly + "_0",
            "UI/Icons/" + sidebarFileOnly,
            "UI/Icons/" + sidebarFileOnly + "_0",
            legacyBasePath,
            legacyBasePath + "_0",
            legacyFileNameOnly,
            legacyFileNameOnly + "_0",
            "UI/Icons/" + legacyFileNameOnly,
            "UI/Icons/" + legacyFileNameOnly + "_0"
        };

        return LoadCodexSpriteFromResourceCandidates(candidates);
    }

    /// <summary>Full modal background: open spread first, then same fallback as sidebar.</summary>
    private static Sprite LoadCodexModalOpenBookSprite()
    {
        string openPath = CodexModalOpenBookResourcePath;
        string openFileOnly = "Street Codex_Open";
        string[] openCandidates =
        {
            openPath,
            openPath + "_0",
            openFileOnly,
            openFileOnly + "_0",
            "UI/Icons/" + openFileOnly,
            "UI/Icons/" + openFileOnly + "_0"
        };

        Sprite s = LoadCodexSpriteFromResourceCandidates(openCandidates);
        if (s != null)
            return s;
        return LoadCodexSidebarBookSprite();
    }

    private static Sprite LoadCodexSpriteFromResourceCandidates(string[] candidates)
    {
        if (candidates == null || candidates.Length == 0)
            return null;

        for (int i = 0; i < candidates.Length; i++)
        {
            Sprite sp = Resources.Load<Sprite>(candidates[i]);
            if (sp != null)
                return sp;
        }

        for (int i = 0; i < candidates.Length; i++)
        {
            Sprite[] all = Resources.LoadAll<Sprite>(candidates[i]);
            if (all == null || all.Length == 0)
                continue;

            for (int k = 0; k < all.Length; k++)
            {
                if (all[k] != null && all[k].name != null && all[k].name.EndsWith("_0"))
                    return all[k];
            }
            for (int k = 0; k < all.Length; k++)
            {
                if (all[k] != null)
                    return all[k];
            }
        }

        return null;
    }

    private static void TryApplyCodexButtonArt(Button btn)
    {
        if (btn == null)
            return;

        Image img = btn.GetComponent<Image>();
        if (img == null)
            return;

        // Expect a sprite (single) or multiple sprites imported under Resources.
        Sprite book = LoadCodexSidebarBookSprite();
        if (book == null)
            return;

        // Keep the button as a normal UI button (text stays); icon is rendered above the button separately.
        img.sprite = null;
        img.preserveAspect = false;
        PlanningUiButtonStyle.ApplyStandardRectButton(btn, img);
        if (btn.gameObject.GetComponent<ButtonPressScale>() == null)
            btn.gameObject.AddComponent<ButtonPressScale>();

        // Remove any legacy inline icon if present (older builds).
        Transform oldIcon = btn.transform.Find("CodexIcon");
        if (oldIcon != null)
            Object.Destroy(oldIcon.gameObject);
    }

    private void UpdateLegalCodexUiIfNeeded()
    {
        if (_current != PlanningTabId.Legal)
            return;

        string counselLine = string.IsNullOrWhiteSpace(GameSessionState.InterrogationPublicDefenderName)
            ? string.Empty
            : "\n• <b>Public defender:</b> " + GameSessionState.InterrogationPublicDefenderName + "\n";
        _leftText.text =
            "<b>Legal desk</b>\n" +
            "• Retainers\n" +
            "• Active cases\n" +
            "• Appeals" +
            counselLine +
            "\n" +
            "<size=90%><i>Open the codex book to study chapters, sections, and penalty ranges.</i></size>";
        _centerText.text =
            "<b>Legal system</b>\n\n" +
            "The codex is the rulebook that powers arrests, charges, and sentencing.\n\n" +
            "• Primary charge dropped -> case dismissed\n" +
            "• Bonus charge dropped -> sentence mitigation\n\n" +
            "<i>Open the codex from the <b>closed book button</b> in the sidebar; turn pages with Prev/Next under the spread. " +
            "Close with Esc, click outside the spread, or press that same <b>sidebar</b> book button again.</i>";
        _rightText.text =
            "<b>Quick note</b>\n" +
            "Penalties are ranges (min–max) so judges and lawyers have room to work.\n\n" +
            "<size=90%><i>Next:</i> link each arrest cause to codex sections for full integration.</i></size>";
    }

    /// <summary>
    /// Dimmer and book panel start to the <em>right</em> of the planning left column so the
    /// <b>closed</b> sidebar book stays clickable (same <see cref="ToggleLegalSidebarCodex"/> as open).
    /// </summary>
    private void ApplyCodexModalLeftSidebarInset()
    {
        if (_codexDimDismissRt == null || _codexPanelRt == null)
            return;

        TryRecoverPlanningShellReferencesFromHierarchy();
        float leftGutter = 280f;
        if (_leftColumnRoot != null)
        {
            Canvas.ForceUpdateCanvases();
            var lr = _leftColumnRoot as RectTransform;
            if (lr != null)
            {
                leftGutter = lr.rect.width + CodexLeftSidebarExtraGutterPx;
                leftGutter = Mathf.Clamp(leftGutter, 220f, 420f);
            }
        }

        _codexDimDismissRt.offsetMin = new Vector2(leftGutter, 0f);
        _codexDimDismissRt.offsetMax = Vector2.zero;

        float maxW = Mathf.Max(180f, Screen.width * 0.985f - leftGutter - 16f);
        float maxH = Screen.height * 0.92f;
        float aspect = Mathf.Max(0.5f, _codexOpenBookAspect);
        float hFromW = maxW / aspect;
        float panelH = hFromW <= maxH ? hFromW : maxH;
        float panelW = panelH * aspect;
        if (hFromW > maxH)
            panelW = maxH * aspect;

        _codexPanelRt.sizeDelta = new Vector2(panelW, panelH);
        // Shift spread center so it lives in the region to the right of the gutter.
        _codexPanelRt.anchoredPosition = new Vector2(leftGutter * 0.5f, 0f);
    }

    private void BuildCodexBookModal()
    {
        Canvas canvas = GameObject.Find("Canvas")?.GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("PlanningShellController: Canvas not found — codex modal skipped.");
            return;
        }

        Transform old = canvas.transform.Find("CodexModalRoot");
        if (old != null)
            Destroy(old.gameObject);

        GameObject root = new GameObject("CodexModalRoot");
        root.transform.SetParent(canvas.transform, false);
        RectTransform rootRt = root.AddComponent<RectTransform>();
        StretchFull(rootRt);
        root.SetActive(false);
        _codexModalRoot = root;

        // Dimmer: separate child, inset from the left so the Legal sidebar (closed book) stays hit-testable.
        GameObject dimGo = new GameObject("CodexDim");
        dimGo.transform.SetParent(root.transform, false);
        _codexDimDismissRt = dimGo.AddComponent<RectTransform>();
        StretchFull(_codexDimDismissRt);
        _codexDimDismissRt.SetAsFirstSibling();
        Image dim = dimGo.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.55f);
        dim.raycastTarget = true;
        Button dimDismiss = dimGo.AddComponent<Button>();
        dimDismiss.targetGraphic = dim;
        dimDismiss.transition = Selectable.Transition.None;
        dimDismiss.onClick.AddListener(HideCodexBook);

        Sprite openSprite = LoadCodexModalOpenBookSprite();
        if (openSprite != null && openSprite.rect.height > 1f)
            _codexOpenBookAspect = openSprite.rect.width / openSprite.rect.height;

        GameObject panel = new GameObject("CodexPanel");
        panel.transform.SetParent(root.transform, false);
        panel.transform.SetAsLastSibling();
        RectTransform prt = panel.AddComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        _codexPanelRt = prt;

        Image pimg = panel.AddComponent<Image>();
        pimg.color = new Color(0f, 0f, 0f, 0f);
        // Invisible; book art + scroll/TOC handle hits. Do not steal clicks over the (excluded) left sidebar.
        pimg.raycastTarget = false;

        ApplyCodexModalLeftSidebarInset();

        // Open-book art (full bleed inside panel)
        GameObject bookGo = new GameObject("CodexOpenBookArt");
        bookGo.transform.SetParent(panel.transform, false);
        RectTransform bookRt = bookGo.AddComponent<RectTransform>();
        StretchFull(bookRt);
        Image bookImg = bookGo.AddComponent<Image>();
        bookImg.sprite = openSprite;
        bookImg.preserveAspect = false;
        bookImg.color = Color.white;
        // Must block raycasts so clicks on parchment (outside scroll columns) don't fall through to the dim dismiss layer.
        bookImg.raycastTarget = true;

        // Two-page text area (inset for leather frame + spine)
        GameObject dockGo = new GameObject("PagesDock");
        dockGo.transform.SetParent(panel.transform, false);
        RectTransform dockRt = dockGo.AddComponent<RectTransform>();
        // Inset text from printed page edges; leave bottom band for Prev/Next.
        dockRt.anchorMin = new Vector2(0.095f, 0.15f);
        dockRt.anchorMax = new Vector2(0.905f, 0.86f);
        dockRt.offsetMin = Vector2.zero;
        dockRt.offsetMax = Vector2.zero;

        HorizontalLayoutGroup hlg = dockGo.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 0f;
        hlg.padding = new RectOffset(0, 0, 0, 0);
        hlg.childAlignment = TextAnchor.UpperLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        GameObject leftCol = new GameObject("LeftPage");
        leftCol.transform.SetParent(dockGo.transform, false);
        LayoutElement leL = leftCol.AddComponent<LayoutElement>();
        leL.flexibleWidth = 1f;
        leL.flexibleHeight = 1f;
        BuildCodexScrollPageColumn(leftCol.transform, "LeftScroll", out _codexLeftPageText);

        GameObject rightCol = new GameObject("RightPage");
        rightCol.transform.SetParent(dockGo.transform, false);
        LayoutElement leR = rightCol.AddComponent<LayoutElement>();
        leR.flexibleWidth = 1f;
        leR.flexibleHeight = 1f;
        ScrollRect rightScroll = BuildCodexScrollPageColumn(rightCol.transform, "RightScroll", out _codexRightPageText);
        RectTransform rightContent = rightScroll.content;

        // TOC buttons: right-hand scroll, below the right page header text
        GameObject tocGo = new GameObject("TocButtons");
        tocGo.transform.SetParent(rightContent.transform, false);
        RectTransform tocRt = tocGo.AddComponent<RectTransform>();
        tocRt.sizeDelta = new Vector2(0f, 10f);
        VerticalLayoutGroup tocV = tocGo.AddComponent<VerticalLayoutGroup>();
        tocV.padding = new RectOffset(4, 10, 2, 4);
        tocV.spacing = 2f;
        tocV.childAlignment = TextAnchor.UpperLeft;
        tocV.childControlWidth = true;
        tocV.childControlHeight = false;
        tocV.childForceExpandWidth = true;
        tocV.childForceExpandHeight = false;
        _codexTocRoot = tocRt;
        _codexTocButtons.Clear();

        var tocTargets = LegalCodex.BuildTocJumpTargetsEn();
        for (int i = 0; i < tocTargets.Count; i++)
        {
            int capturedPage = tocTargets[i].pageIndex;
            Button b = CreateCodexParchmentLinkRow(tocGo.transform, tocTargets[i].label);
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(() => ShowCodexBookAtPage(capturedPage));
            _codexTocButtons.Add(b);
        }

        // Title strip (draggable) — after dock so it draws above the page area
        GameObject titleGo = new GameObject("Title");
        titleGo.transform.SetParent(panel.transform, false);
        RectTransform trt = titleGo.AddComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.08f, 1f);
        trt.anchorMax = new Vector2(0.92f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.anchoredPosition = new Vector2(0f, -6f);
        trt.sizeDelta = new Vector2(0f, 40f);
        _codexTitleText = titleGo.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            _codexTitleText.font = TMP_Settings.defaultFontAsset;
        _codexTitleText.fontSize = 22f;
        _codexTitleText.fontStyle = FontStyles.Bold;
        _codexTitleText.alignment = TextAlignmentOptions.Center;
        _codexTitleText.color = new Color(0.18f, 0.13f, 0.09f, 0.95f);
        _codexTitleText.raycastTarget = true;
        RegisterDraggableModalTitle(titleGo, prt);

        // Page navigation (built in code — independent of book PNG artwork)
        GameObject navGo = new GameObject("CodexPageNav");
        navGo.transform.SetParent(panel.transform, false);
        RectTransform navRt = navGo.AddComponent<RectTransform>();
        navRt.anchorMin = new Vector2(0.08f, 0.03f);
        navRt.anchorMax = new Vector2(0.92f, 0.11f);
        navRt.offsetMin = Vector2.zero;
        navRt.offsetMax = Vector2.zero;
        HorizontalLayoutGroup navH = navGo.AddComponent<HorizontalLayoutGroup>();
        navH.padding = new RectOffset(8, 8, 2, 2);
        navH.spacing = 12f;
        navH.childAlignment = TextAnchor.MiddleCenter;
        navH.childControlWidth = false;
        navH.childControlHeight = true;
        navH.childForceExpandWidth = false;
        navH.childForceExpandHeight = false;

        _codexPagePrevButton = CreateCodexParchmentNavButton(navGo.transform, "‹ Prev");
        _codexPagePrevButton.onClick.RemoveAllListeners();
        _codexPagePrevButton.onClick.AddListener(() => ShowCodexBookAtPage(_codexPageIndex - 1));

        GameObject navSpacer = new GameObject("NavSpacer");
        navSpacer.transform.SetParent(navGo.transform, false);
        LayoutElement navSpLe = navSpacer.AddComponent<LayoutElement>();
        navSpLe.flexibleWidth = 1f;
        navSpLe.minWidth = 8f;

        _codexPageNextButton = CreateCodexParchmentNavButton(navGo.transform, "Next ›");
        _codexPageNextButton.onClick.RemoveAllListeners();
        _codexPageNextButton.onClick.AddListener(() => ShowCodexBookAtPage(_codexPageIndex + 1));

        GameObject closeGo = new GameObject("CodexClose");
        closeGo.transform.SetParent(panel.transform, false);
        RectTransform crtClose = closeGo.AddComponent<RectTransform>();
        crtClose.anchorMin = new Vector2(1f, 1f);
        crtClose.anchorMax = new Vector2(1f, 1f);
        crtClose.pivot = new Vector2(1f, 1f);
        crtClose.anchoredPosition = new Vector2(-10f, -8f);
        crtClose.sizeDelta = new Vector2(120f, 36f);
        Image closeImg = closeGo.AddComponent<Image>();
        closeImg.color = new Color(0.42f, 0.28f, 0.16f, 0.14f);
        closeImg.raycastTarget = true;
        Outline closeOl = closeGo.AddComponent<Outline>();
        closeOl.effectColor = new Color(0.22f, 0.14f, 0.09f, 0.4f);
        closeOl.effectDistance = new Vector2(1f, -1f);
        Button close = closeGo.AddComponent<Button>();
        close.targetGraphic = closeImg;
        close.transition = Selectable.Transition.ColorTint;
        ColorBlock closeCb = close.colors;
        closeCb.normalColor = Color.white;
        closeCb.highlightedColor = new Color(1.06f, 0.98f, 0.92f, 1f);
        closeCb.pressedColor = new Color(0.88f, 0.80f, 0.72f, 1f);
        closeCb.selectedColor = Color.white;
        closeCb.disabledColor = new Color(0.7f, 0.7f, 0.7f, 0.5f);
        closeCb.fadeDuration = 0.07f;
        close.colors = closeCb;
        close.onClick.RemoveAllListeners();
        close.onClick.AddListener(HideCodexBook);
        GameObject closeLabel = new GameObject("Label");
        closeLabel.transform.SetParent(closeGo.transform, false);
        RectTransform clRt = closeLabel.AddComponent<RectTransform>();
        StretchFull(clRt);
        TextMeshProUGUI closeTmp = closeLabel.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            closeTmp.font = TMP_Settings.defaultFontAsset;
        closeTmp.text = "Close";
        closeTmp.fontSize = 15f;
        closeTmp.alignment = TextAlignmentOptions.Center;
        closeTmp.color = new Color(0.14f, 0.09f, 0.06f, 0.9f);
        closeTmp.raycastTarget = false;
    }

    /// <summary>TOC line: ink-on-parchment look; hover is a faint wash only (no black slab).</summary>
    private static Button CreateCodexParchmentLinkRow(Transform parent, string label)
    {
        const float rowH = 40f;
        GameObject go = new GameObject("CodexParchRow");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, rowH);
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.minHeight = rowH;
        le.preferredHeight = rowH;
        le.flexibleWidth = 1f;

        Image bg = go.AddComponent<Image>();
        bg.color = new Color(0.45f, 0.30f, 0.16f, 0.03f);
        bg.raycastTarget = true;

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.transition = Selectable.Transition.ColorTint;
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.12f, 1.02f, 0.92f, 1f);
        cb.pressedColor = new Color(0.82f, 0.72f, 0.58f, 1f);
        cb.selectedColor = Color.white;
        cb.disabledColor = new Color(0.75f, 0.75f, 0.75f, 0.45f);
        cb.fadeDuration = 0.07f;
        btn.colors = cb;
        btn.navigation = new Navigation { mode = Navigation.Mode.None };

        GameObject textGo = new GameObject("Label");
        textGo.transform.SetParent(go.transform, false);
        RectTransform tr = textGo.AddComponent<RectTransform>();
        tr.anchorMin = new Vector2(0f, 0f);
        tr.anchorMax = new Vector2(1f, 1f);
        tr.offsetMin = new Vector2(2f, 0f);
        tr.offsetMax = new Vector2(-6f, 0f);
        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = label;
        tmp.fontSize = 15f;
        tmp.fontStyle = FontStyles.Normal;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.color = new Color(0.14f, 0.09f, 0.055f, 0.94f);
        tmp.raycastTarget = false;
        if (go.GetComponent<ButtonPressScale>() == null)
            go.AddComponent<ButtonPressScale>();
        return btn;
    }

    /// <summary>Small parchment chip for Prev/Next — warm tint, not planning HUD black.</summary>
    private static Button CreateCodexParchmentNavButton(Transform parent, string label)
    {
        GameObject go = new GameObject("CodexParchNav");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(128f, 34f);
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 128f;
        le.preferredHeight = 34f;
        le.minWidth = 112f;

        Image bg = go.AddComponent<Image>();
        bg.color = new Color(0.40f, 0.26f, 0.14f, 0.16f);
        bg.raycastTarget = true;
        Outline ol = go.AddComponent<Outline>();
        ol.effectColor = new Color(0.22f, 0.14f, 0.09f, 0.38f);
        ol.effectDistance = new Vector2(1f, -1f);

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.transition = Selectable.Transition.ColorTint;
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.08f, 0.98f, 0.90f, 1f);
        cb.pressedColor = new Color(0.86f, 0.76f, 0.64f, 1f);
        cb.selectedColor = Color.white;
        cb.disabledColor = new Color(0.72f, 0.72f, 0.72f, 0.45f);
        cb.fadeDuration = 0.07f;
        btn.colors = cb;

        GameObject textGo = new GameObject("Label");
        textGo.transform.SetParent(go.transform, false);
        RectTransform tr = textGo.AddComponent<RectTransform>();
        StretchFull(tr);
        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = label;
        tmp.fontSize = 15f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.13f, 0.085f, 0.055f, 0.95f);
        tmp.raycastTarget = false;
        if (go.GetComponent<ButtonPressScale>() == null)
            go.AddComponent<ButtonPressScale>();
        return btn;
    }

    /// <summary>One scroll column inside the open-book spread.</summary>
    private static ScrollRect BuildCodexScrollPageColumn(Transform parent, string scrollName, out TextMeshProUGUI pageTmp)
    {
        GameObject scrollGo = new GameObject(scrollName);
        scrollGo.transform.SetParent(parent, false);
        RectTransform srt = scrollGo.AddComponent<RectTransform>();
        StretchFull(srt);
        LayoutElement sle = scrollGo.AddComponent<LayoutElement>();
        sle.flexibleWidth = 1f;
        sle.flexibleHeight = 1f;

        ScrollRect sr = scrollGo.AddComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 32f;

        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollGo.transform, false);
        RectTransform vrt = viewport.AddComponent<RectTransform>();
        StretchFull(vrt);
        Image vimg = viewport.AddComponent<Image>();
        vimg.color = new Color(1f, 1f, 1f, 0.02f);
        vimg.raycastTarget = true;
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        sr.viewport = vrt;

        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        RectTransform crt = content.AddComponent<RectTransform>();
        crt.anchorMin = new Vector2(0f, 1f);
        crt.anchorMax = new Vector2(1f, 1f);
        crt.pivot = new Vector2(0.5f, 1f);
        crt.anchoredPosition = Vector2.zero;
        crt.sizeDelta = new Vector2(0f, 400f);
        sr.content = crt;

        VerticalLayoutGroup cv = content.AddComponent<VerticalLayoutGroup>();
        cv.padding = new RectOffset(24, 24, 12, 20);
        cv.spacing = 6f;
        cv.childAlignment = TextAnchor.UpperLeft;
        cv.childControlWidth = true;
        cv.childControlHeight = false;
        cv.childForceExpandWidth = true;
        cv.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject textGo = new GameObject("PageText");
        textGo.transform.SetParent(content.transform, false);
        pageTmp = textGo.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            pageTmp.font = TMP_Settings.defaultFontAsset;
        pageTmp.fontSize = 16f;
        pageTmp.alignment = TextAlignmentOptions.TopLeft;
        pageTmp.textWrappingMode = TextWrappingModes.Normal;
        // Tighter, more even line rhythm on parchment (avoids "floating" ragged look).
        pageTmp.lineSpacing = 4f;
        pageTmp.paragraphSpacing = 2f;
        pageTmp.margin = new Vector4(0f, 0f, 0f, 0f);
        pageTmp.color = new Color(0.20f, 0.14f, 0.10f, 1f);
        pageTmp.text = "";
        pageTmp.raycastTarget = false;
        LayoutElement tle = textGo.AddComponent<LayoutElement>();
        tle.flexibleWidth = 1f;
        tle.minHeight = 32f;

        return sr;
    }

    private void ShowCodexBookAtPage(int pageIndex)
    {
        if (_codexModalRoot == null || _codexTitleText == null || _codexLeftPageText == null || _codexRightPageText == null)
            return;

        int max = Mathf.Max(1, LegalCodex.PageCount) - 1;
        _codexPageIndex = Mathf.Clamp(pageIndex, 0, max);

        _codexTitleText.text = LegalCodex.GetPageTitleEn(_codexPageIndex) +
                               "  <size=78%><color=#5C4A38>(" + (_codexPageIndex + 1) + "/" + (max + 1) + ")</color></size>";

        LegalCodex.BuildCodexPageSpreadEn(_codexPageIndex, out string left, out string right);
        _codexLeftPageText.text = left;
        _codexRightPageText.text = right;

        ScrollRect leftSr = _codexLeftPageText.GetComponentInParent<ScrollRect>();
        ScrollRect rightSr = _codexRightPageText.GetComponentInParent<ScrollRect>();
        if (leftSr != null)
            leftSr.verticalNormalizedPosition = 1f;
        if (rightSr != null)
            rightSr.verticalNormalizedPosition = 1f;

        if (_codexTocRoot != null)
            _codexTocRoot.gameObject.SetActive(_codexPageIndex == 0);

        if (_codexPagePrevButton != null)
            _codexPagePrevButton.interactable = _codexPageIndex > 0;
        if (_codexPageNextButton != null)
            _codexPageNextButton.interactable = _codexPageIndex < max;

        ApplyCodexModalLeftSidebarInset();
        _codexModalRoot.SetActive(true);
        _codexModalRoot.transform.SetAsLastSibling();
        BringPlanningInteractiveChromeToFront();
    }

    private void HideCodexBook()
    {
        if (_codexModalRoot != null)
            _codexModalRoot.SetActive(false);
    }

    /// <summary>
    /// Full-area overlay as a child of <paramref name="mainArea"/> so it always matches the planning content rect.
    /// </summary>
    private void BuildOpsStageOverlay(RectTransform mainArea)
    {
        _opsStageOverlayRoot = new GameObject("OpsStageOverlay");
        _opsStageOverlayRoot.transform.SetParent(mainArea, false);
        _opsStageOverlayRoot.transform.SetAsLastSibling();
        RectTransform overlayRt = _opsStageOverlayRoot.AddComponent<RectTransform>();
        StretchFull(overlayRt);
        _opsStageOverlayRoot.SetActive(false);

        GameObject panelGo = new GameObject("OpsStagePanel");
        panelGo.transform.SetParent(_opsStageOverlayRoot.transform, false);
        RectTransform pr = panelGo.AddComponent<RectTransform>();
        _opsStagePanelRt = pr;
        pr.anchorMin = Vector2.zero;
        pr.anchorMax = Vector2.one;
        pr.offsetMin = new Vector2(_opsPanelSafeInsetLeft, _opsPanelSafeInsetBottom);
        pr.offsetMax = new Vector2(-_opsPanelSafeInsetRight, -_opsPanelSafeInsetTop);

        Image panelBg = panelGo.AddComponent<Image>();
        panelBg.color = new Color(0.14f, 0.13f, 0.12f, 0.98f);
        Outline outline = panelGo.AddComponent<Outline>();
        outline.effectColor = new Color(0.42f, 0.38f, 0.30f, 0.92f);
        outline.effectDistance = new Vector2(2f, -2f);

        VerticalLayoutGroup vPanel = panelGo.AddComponent<VerticalLayoutGroup>();
        vPanel.padding = new RectOffset(10, 10, 10, 10);
        vPanel.spacing = 8f;
        vPanel.childAlignment = TextAnchor.UpperCenter;
        vPanel.childControlHeight = true;
        vPanel.childControlWidth = true;
        vPanel.childForceExpandHeight = true;
        vPanel.childForceExpandWidth = true;

        GameObject mainRowGo = new GameObject("OpsMainRow", typeof(RectTransform));
        mainRowGo.transform.SetParent(panelGo.transform, false);
        HorizontalLayoutGroup hMain = mainRowGo.AddComponent<HorizontalLayoutGroup>();
        hMain.spacing = 8f;
        hMain.padding = new RectOffset(0, 0, 0, 0);
        hMain.childAlignment = TextAnchor.UpperLeft;
        hMain.childControlHeight = true;
        hMain.childControlWidth = true;
        hMain.childForceExpandHeight = true;
        hMain.childForceExpandWidth = false;
        LayoutElement leMainRow = mainRowGo.AddComponent<LayoutElement>();
        leMainRow.flexibleHeight = 1f;
        leMainRow.flexibleWidth = 1f;
        leMainRow.minHeight = 200f;

        BuildOpsLeftColumn(mainRowGo.transform);

        GameObject mapAreaGo = new GameObject("OpsMapArea");
        mapAreaGo.transform.SetParent(mainRowGo.transform, false);
        RectTransform mapAreaRt = mapAreaGo.AddComponent<RectTransform>();
        StretchFull(mapAreaRt);
        LayoutElement leMapArea = mapAreaGo.AddComponent<LayoutElement>();
        leMapArea.flexibleHeight = 1f;
        leMapArea.flexibleWidth = 1f;
        leMapArea.minWidth = 180f;
        leMapArea.minHeight = 160f;
        VerticalLayoutGroup vMapArea = mapAreaGo.AddComponent<VerticalLayoutGroup>();
        vMapArea.spacing = 6f;
        vMapArea.padding = new RectOffset(0, 0, 0, 0);
        vMapArea.childAlignment = TextAnchor.UpperCenter;
        vMapArea.childControlHeight = true;
        vMapArea.childControlWidth = true;
        vMapArea.childForceExpandHeight = false;
        vMapArea.childForceExpandWidth = true;

        GameObject mapViewportGo = new GameObject("OpsMapViewport", typeof(RectTransform));
        mapViewportGo.transform.SetParent(mapAreaGo.transform, false);
        RectTransform mapVpRt = mapViewportGo.GetComponent<RectTransform>();
        StretchFull(mapVpRt);
        LayoutElement leMapVp = mapViewportGo.AddComponent<LayoutElement>();
        leMapVp.flexibleHeight = 1f;
        leMapVp.flexibleWidth = 1f;
        leMapVp.minHeight = 120f;
        Image mapVpBg = mapViewportGo.AddComponent<Image>();
        mapVpBg.color = new Color(0.045f, 0.045f, 0.05f, 0.96f);
        mapVpBg.raycastTarget = true;
        mapViewportGo.AddComponent<RectMask2D>();

        GameObject contentGo = new GameObject("MapGridContent");
        contentGo.transform.SetParent(mapViewportGo.transform, false);
        RectTransform contentRt = contentGo.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0.5f, 0.5f);
        contentRt.anchorMax = new Vector2(0.5f, 0.5f);
        contentRt.pivot = new Vector2(0.5f, 0.5f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(400f, 400f);

        _opsMapAreaRt = mapVpRt;
        _opsMapGridRoot = contentRt;

        OpsMapPanDriver panDriver = mapViewportGo.AddComponent<OpsMapPanDriver>();
        panDriver.GridRoot = contentRt;
        panDriver.Shell = this;

        GameObject rumorsOuter = new GameObject("OpsRumorsRow", typeof(RectTransform));
        rumorsOuter.transform.SetParent(mapAreaGo.transform, false);
        LayoutElement leRumOuter = rumorsOuter.AddComponent<LayoutElement>();
        leRumOuter.preferredHeight = 108f;
        leRumOuter.minHeight = 88f;
        leRumOuter.flexibleHeight = 0f;
        leRumOuter.flexibleWidth = 1f;
        VerticalLayoutGroup vRum = rumorsOuter.AddComponent<VerticalLayoutGroup>();
        vRum.spacing = 4f;
        vRum.padding = new RectOffset(4, 4, 4, 4);
        vRum.childAlignment = TextAnchor.UpperLeft;
        vRum.childControlWidth = true;
        vRum.childControlHeight = true;
        vRum.childForceExpandWidth = true;
        vRum.childForceExpandHeight = false;

        GameObject rumorsTitleGo = new GameObject("RumorsTitle", typeof(RectTransform));
        rumorsTitleGo.transform.SetParent(rumorsOuter.transform, false);
        LayoutElement leRTitle = rumorsTitleGo.AddComponent<LayoutElement>();
        leRTitle.preferredHeight = 24f;
        leRTitle.minHeight = 22f;
        TextMeshProUGUI rumTitle = CreateTmp(rumorsTitleGo.transform, "RumorsTitleTmp", 15f, FontStyles.Bold);
        rumTitle.text = "Rumors & uncertain tips";
        rumTitle.color = new Color(0.82f, 0.78f, 0.70f, 1f);
        rumTitle.alignment = TextAlignmentOptions.Left;
        StretchFull(rumTitle.rectTransform);

        GameObject rumorsBodyGo = new GameObject("RumorsBody", typeof(RectTransform));
        rumorsBodyGo.transform.SetParent(rumorsOuter.transform, false);
        LayoutElement leRBody = rumorsBodyGo.AddComponent<LayoutElement>();
        leRBody.flexibleHeight = 0f;
        leRBody.minHeight = 44f;
        leRBody.preferredHeight = 52f;
        leRBody.flexibleWidth = 1f;
        _opsRumorsText = CreateTmp(rumorsBodyGo.transform, "RumorsText", 13f, FontStyles.Normal);
        _opsRumorsText.alignment = TextAlignmentOptions.TopLeft;
        _opsRumorsText.color = new Color(0.82f, 0.80f, 0.74f, 1f);
        _opsRumorsText.lineSpacing = 2f;
        _opsRumorsText.richText = true;
        _opsRumorsText.textWrappingMode = TextWrappingModes.Normal;
        StretchFull(_opsRumorsText.rectTransform);

        BuildOpsActionsColumn(mainRowGo.transform);

        BuildOpsNeighborhoodDetailOverlay(_opsStageOverlayRoot.transform);
    }

    private void BuildOpsLeftColumn(Transform mainRowParent)
    {
        GameObject leftGo = new GameObject("OpsLeftColumn", typeof(RectTransform));
        leftGo.transform.SetParent(mainRowParent, false);
        LayoutElement leLeft = leftGo.AddComponent<LayoutElement>();
        leLeft.preferredWidth = 300f;
        leLeft.minWidth = 268f;
        leLeft.flexibleWidth = 0f;
        leLeft.flexibleHeight = 1f;
        VerticalLayoutGroup vLeft = leftGo.AddComponent<VerticalLayoutGroup>();
        vLeft.spacing = 6f;
        vLeft.padding = new RectOffset(2, 2, 2, 2);
        vLeft.childAlignment = TextAnchor.UpperLeft;
        vLeft.childControlHeight = true;
        vLeft.childControlWidth = true;
        vLeft.childForceExpandWidth = true;
        vLeft.childForceExpandHeight = false;

        GameObject blockTitleGo = new GameObject("BlockMapTitle", typeof(RectTransform));
        blockTitleGo.transform.SetParent(leftGo.transform, false);
        LayoutElement leBT = blockTitleGo.AddComponent<LayoutElement>();
        leBT.preferredHeight = 24f;
        leBT.minHeight = 22f;
        TextMeshProUGUI blockTitle = CreateTmp(blockTitleGo.transform, "BlockMapTitleTmp", 15f, FontStyles.Bold);
        blockTitle.text = "Block zoom (center map pick)";
        blockTitle.color = new Color(0.88f, 0.84f, 0.76f, 1f);
        blockTitle.alignment = TextAlignmentOptions.Left;
        StretchFull(blockTitle.rectTransform);

        GameObject blockMapHost = new GameObject("BlockMapHost", typeof(RectTransform));
        blockMapHost.transform.SetParent(leftGo.transform, false);
        LayoutElement leBM = blockMapHost.AddComponent<LayoutElement>();
        leBM.preferredHeight = 228f;
        leBM.minHeight = 196f;
        leBM.flexibleHeight = 0f;
        leBM.flexibleWidth = 1f;
        Image bmBg = blockMapHost.AddComponent<Image>();
        bmBg.color = new Color(0.05f, 0.05f, 0.055f, 0.92f);
        blockMapHost.AddComponent<RectMask2D>();

        GameObject blockGridGo = new GameObject("BlockMapLayout", typeof(RectTransform));
        blockGridGo.transform.SetParent(blockMapHost.transform, false);
        RectTransform bgr = blockGridGo.GetComponent<RectTransform>();
        StretchFull(bgr);
        bgr.offsetMin = new Vector2(4f, 4f);
        bgr.offsetMax = new Vector2(-4f, -4f);
        _opsBlockMapGridRoot = bgr;

        GameObject certainTitleGo = new GameObject("CertainTitle", typeof(RectTransform));
        certainTitleGo.transform.SetParent(leftGo.transform, false);
        LayoutElement leCT = certainTitleGo.AddComponent<LayoutElement>();
        leCT.preferredHeight = 24f;
        leCT.minHeight = 22f;
        TextMeshProUGUI certTitle = CreateTmp(certainTitleGo.transform, "CertainTitleTmp", 15f, FontStyles.Bold);
        certTitle.text = "What you know for sure";
        certTitle.color = new Color(0.88f, 0.84f, 0.76f, 1f);
        certTitle.alignment = TextAlignmentOptions.Left;
        StretchFull(certTitle.rectTransform);

        GameObject certainBodyGo = new GameObject("CertainBody", typeof(RectTransform));
        certainBodyGo.transform.SetParent(leftGo.transform, false);
        LayoutElement leCB = certainBodyGo.AddComponent<LayoutElement>();
        leCB.flexibleHeight = 1f;
        leCB.minHeight = 96f;
        leCB.flexibleWidth = 1f;
        ScrollRect cScr = certainBodyGo.AddComponent<ScrollRect>();
        cScr.horizontal = false;
        cScr.vertical = true;
        cScr.movementType = ScrollRect.MovementType.Clamped;
        cScr.scrollSensitivity = 22f;
        _opsCertainScroll = cScr;

        GameObject cVp = new GameObject("CertainViewport", typeof(RectTransform));
        cVp.transform.SetParent(certainBodyGo.transform, false);
        RectTransform cVprt = cVp.GetComponent<RectTransform>();
        StretchFull(cVprt);
        Image cVpImg = cVp.AddComponent<Image>();
        cVpImg.color = new Color(0.06f, 0.06f, 0.065f, 0.35f);
        cVpImg.raycastTarget = true;
        cVp.AddComponent<Mask>().showMaskGraphic = false;
        cScr.viewport = cVprt;

        GameObject cContent = new GameObject("CertainContent", typeof(RectTransform));
        cContent.transform.SetParent(cVp.transform, false);
        RectTransform cCr = cContent.GetComponent<RectTransform>();
        cCr.anchorMin = new Vector2(0f, 1f);
        cCr.anchorMax = new Vector2(1f, 1f);
        cCr.pivot = new Vector2(0.5f, 1f);
        cCr.anchoredPosition = Vector2.zero;
        cCr.offsetMin = new Vector2(4f, 0f);
        cCr.offsetMax = new Vector2(-4f, 0f);
        VerticalLayoutGroup cV = cContent.AddComponent<VerticalLayoutGroup>();
        cV.childAlignment = TextAnchor.UpperLeft;
        cV.childControlWidth = true;
        cV.childControlHeight = true;
        cV.childForceExpandWidth = true;
        cV.spacing = 0f;
        ContentSizeFitter cCsf = cContent.AddComponent<ContentSizeFitter>();
        cCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        cCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        cScr.content = cCr;

        _opsCertainInfoText = CreateTmp(cContent.transform, "CertainText", 14f, FontStyles.Normal);
        _opsCertainInfoText.alignment = TextAlignmentOptions.TopLeft;
        _opsCertainInfoText.color = new Color(0.88f, 0.86f, 0.80f, 1f);
        _opsCertainInfoText.richText = true;
        _opsCertainInfoText.textWrappingMode = TextWrappingModes.Normal;
        _opsCertainInfoText.lineSpacing = 4f;
        LayoutElement leTxt = _opsCertainInfoText.gameObject.AddComponent<LayoutElement>();
        leTxt.flexibleWidth = 1f;
        leTxt.minWidth = 0f;
        ContentSizeFitter txtFitter = _opsCertainInfoText.gameObject.AddComponent<ContentSizeFitter>();
        txtFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        txtFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
    }

    private void BuildOpsActionsColumn(Transform mainRowParent)
    {
        GameObject rightGo = new GameObject("OpsActionsColumn", typeof(RectTransform));
        rightGo.transform.SetParent(mainRowParent, false);
        LayoutElement leR = rightGo.AddComponent<LayoutElement>();
        leR.preferredWidth = 228f;
        leR.minWidth = 196f;
        leR.flexibleWidth = 0f;
        leR.flexibleHeight = 1f;
        VerticalLayoutGroup vR = rightGo.AddComponent<VerticalLayoutGroup>();
        vR.spacing = 6f;
        vR.padding = new RectOffset(4, 4, 4, 4);
        vR.childAlignment = TextAnchor.UpperLeft;
        vR.childControlHeight = true;
        vR.childControlWidth = true;
        vR.childForceExpandWidth = true;
        vR.childForceExpandHeight = false;

        GameObject actTitleGo = new GameObject("ActionsTitle", typeof(RectTransform));
        actTitleGo.transform.SetParent(rightGo.transform, false);
        LayoutElement leAT = actTitleGo.AddComponent<LayoutElement>();
        leAT.preferredHeight = 24f;
        leAT.minHeight = 22f;
        TextMeshProUGUI actTitle = CreateTmp(actTitleGo.transform, "ActionsTitleTmp", 15f, FontStyles.Bold);
        actTitle.text = "Actions";
        actTitle.color = new Color(0.88f, 0.84f, 0.76f, 1f);
        actTitle.alignment = TextAlignmentOptions.Left;
        StretchFull(actTitle.rectTransform);

        GameObject actionsScrollGo = new GameObject("ActionsScroll", typeof(RectTransform));
        actionsScrollGo.transform.SetParent(rightGo.transform, false);
        LayoutElement leASc = actionsScrollGo.AddComponent<LayoutElement>();
        leASc.flexibleHeight = 1f;
        leASc.minHeight = 120f;
        leASc.flexibleWidth = 1f;
        ScrollRect ascr = actionsScrollGo.AddComponent<ScrollRect>();
        ascr.horizontal = false;
        ascr.vertical = true;
        ascr.movementType = ScrollRect.MovementType.Clamped;
        ascr.scrollSensitivity = 18f;

        GameObject avp = new GameObject("ActionsViewport", typeof(RectTransform));
        avp.transform.SetParent(actionsScrollGo.transform, false);
        RectTransform avprt = avp.GetComponent<RectTransform>();
        StretchFull(avprt);
        Image avpImg = avp.AddComponent<Image>();
        avpImg.color = new Color(0.08f, 0.08f, 0.09f, 0.55f);
        avpImg.raycastTarget = true;
        Mask am = avp.AddComponent<Mask>();
        am.showMaskGraphic = false;
        ascr.viewport = avprt;

        GameObject acContent = new GameObject("ActionsContent", typeof(RectTransform));
        acContent.transform.SetParent(avp.transform, false);
        RectTransform acRt = acContent.GetComponent<RectTransform>();
        acRt.anchorMin = new Vector2(0f, 1f);
        acRt.anchorMax = new Vector2(1f, 1f);
        acRt.pivot = new Vector2(0.5f, 1f);
        acRt.anchoredPosition = Vector2.zero;
        acRt.offsetMin = new Vector2(2f, 0f);
        acRt.offsetMax = new Vector2(-2f, 0f);
        VerticalLayoutGroup av = acContent.AddComponent<VerticalLayoutGroup>();
        av.spacing = 6f;
        av.childAlignment = TextAnchor.UpperLeft;
        av.childControlWidth = true;
        av.childControlHeight = true;
        av.childForceExpandWidth = true;
        av.childForceExpandHeight = false;
        ContentSizeFitter acf = acContent.AddComponent<ContentSizeFitter>();
        acf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        acf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        ascr.content = acRt;
        _opsActionsContentRoot = acRt;
    }

    private void RefreshOpsDashboard()
    {
        if (_opsBlockMapGridRoot == null || _opsCertainInfoText == null || _opsRumorsText == null || _opsActionsContentRoot == null)
            return;

        GameSessionState.EnsureMicroBlockReady();
        GameSessionState.EnsureActiveCityData();

        int home = MicroBlockWorldState.CrewHomeBlockId;
        if (_opsCenterMapSelectedBlockId < 0 && home >= 0)
            _opsCenterMapSelectedBlockId = home;

        MicroBlockSpotRuntime selected = FindOpsSpotByStableId(_opsSelectedSpotStableId);
        if (selected == null)
        {
            bool allowDefaultToCrewRoom = true;
            if (_opsCenterMapSelectedBlockId >= 0 && home >= 0 && _opsCenterMapSelectedBlockId != home)
                allowDefaultToCrewRoom = false;

            if (_opsMapFocusedLotId >= 0)
            {
                LotData focusLot = FindOpsLotById(GameSessionState.ActiveCityData, _opsMapFocusedLotId);
                if (focusLot != null)
                {
                    if (focusLot.BlockId != home || MicroBlockWorldState.FindSpotByAnchorLotId(focusLot.Id) == null)
                        allowDefaultToCrewRoom = false;
                }
            }

            if (allowDefaultToCrewRoom)
            {
                selected = FindOpsSpotByStableId("spot_crew_room");
                if (selected == null && MicroBlockWorldState.Spots.Count > 0)
                    selected = MicroBlockWorldState.Spots[0];
                _opsSelectedSpotStableId = selected != null ? selected.StableId : string.Empty;
            }
        }

        if (selected != null && selected.AnchorLotId >= 0)
            _opsMapFocusedLotId = selected.AnchorLotId;

        RebuildOpsBlockMapCells();
        RefreshOpsCertainAndRumorsPanels();
        RebuildOpsActionsPanel();

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_opsBlockMapGridRoot);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_opsActionsContentRoot);
        if (_opsMapAreaRt != null && _opsMapAreaRt.parent is RectTransform mapColRt)
            LayoutRebuilder.ForceRebuildLayoutImmediate(mapColRt);
    }

    private void SelectOpsSpot(MicroBlockSpotRuntime spot)
    {
        if (spot == null || string.IsNullOrEmpty(spot.StableId))
            return;
        _opsSelectedSpotStableId = spot.StableId;
        _opsMapFocusedLotId = spot.AnchorLotId >= 0 ? spot.AnchorLotId : -1;
        if (_opsMapFocusedLotId >= 0)
        {
            GameSessionState.EnsureActiveCityData();
            LotData l = FindOpsLotById(GameSessionState.ActiveCityData, _opsMapFocusedLotId);
            if (l != null)
                _opsCenterMapSelectedBlockId = l.BlockId;
        }

        if (_current == PlanningTabId.Operations)
            RebuildOpsCityMapIfNeeded();
        RefreshOpsDashboard();
    }

    private static MicroBlockSpotRuntime FindOpsSpotByStableId(string stableId)
    {
        if (string.IsNullOrEmpty(stableId))
            return null;
        for (int i = 0; i < MicroBlockWorldState.Spots.Count; i++)
        {
            MicroBlockSpotRuntime s = MicroBlockWorldState.Spots[i];
            if (s != null && s.StableId == stableId)
                return s;
        }

        return null;
    }

    private static LotData FindOpsLotById(CityData city, int lotId)
    {
        if (city == null || city.Lots == null || lotId < 0)
            return null;
        for (int i = 0; i < city.Lots.Count; i++)
        {
            LotData l = city.Lots[i];
            if (l.Id == lotId)
                return l;
        }

        return null;
    }

    private static int GetOpsCityBlockIdForSpot(MicroBlockSpotRuntime spot)
    {
        if (spot == null || spot.AnchorLotId < 0)
            return -1;
        GameSessionState.EnsureActiveCityData();
        LotData lot = FindOpsLotById(GameSessionState.ActiveCityData, spot.AnchorLotId);
        return lot != null ? lot.BlockId : -1;
    }

    /// <summary>
    /// Sandbox: the spot’s block is still outside the revealed ring on the Ops map — same as macro fog tiles.
    /// Recon/surveillance OK; door-specific work waits until the block is on-map.
    /// </summary>
    private static bool IsOpsSpotHiddenUnderSandboxMacroFog(MicroBlockSpotRuntime spot)
    {
        if (spot == null)
            return false;
        GameSessionState.EnsureActiveCityData();
        if (GameSessionState.ActiveCityData == null || !GameSessionState.SingleBlockSandboxEnabled)
            return false;
        int blockId = GetOpsCityBlockIdForSpot(spot);
        if (blockId < 0)
            return false;
        return OpsCityGenMapView.IsSandboxMacroBlockUnderFog(GameSessionState.ActiveCityData, blockId);
    }

    private void RebuildOpsBlockMapCells()
    {
        RectTransform contentRt = _opsBlockMapGridRoot;
        GameSessionState.EnsureActiveCityData();
        int blockId = _opsCenterMapSelectedBlockId >= 0
            ? _opsCenterMapSelectedBlockId
            : MicroBlockWorldState.CrewHomeBlockId;
        if (blockId < 0 || GameSessionState.ActiveCityData?.Lots == null)
            return;

        bool hasAny = false;
        for (int i = 0; i < GameSessionState.ActiveCityData.Lots.Count; i++)
        {
            if (GameSessionState.ActiveCityData.Lots[i].BlockId == blockId)
            {
                hasAny = true;
                break;
            }
        }

        if (!hasAny)
            return;

        Canvas.ForceUpdateCanvases();
        if (contentRt.parent is RectTransform hostRt)
            LayoutRebuilder.ForceRebuildLayoutImmediate(hostRt);
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);

        float vw = contentRt.rect.width;
        float vh = contentRt.rect.height;
        if (vw < 2f || vh < 2f)
        {
            vw = 220f;
            vh = 180f;
        }

        int focusLot = _opsMapFocusedLotId;
        if (focusLot < 0 && !string.IsNullOrEmpty(_opsSelectedSpotStableId))
        {
            MicroBlockSpotRuntime sp = FindOpsSpotByStableId(_opsSelectedSpotStableId);
            if (sp != null && sp.AnchorLotId >= 0)
                focusLot = sp.AnchorLotId;
        }

        Vector2 vp = new Vector2(Mathf.Max(48f, vw), Mathf.Max(48f, vh));
        bool stripRoads = GameSessionState.SingleBlockSandboxEnabled;
        OpsCityGenMapView.RebuildLotsForBlock(contentRt, GameSessionState.ActiveCityData, blockId, OnOpsBlockZoomLotClicked,
            MicroBlockWorldState.CrewHomeBlockId, focusLot, vp, drawRoads: !stripRoads);
    }

    private void RefreshOpsCertainAndRumorsPanels()
    {
        if (string.IsNullOrEmpty(_opsSelectedSpotStableId))
        {
            _opsCertainInfoText.text = BuildOpsNoSelectionCertainText();
            _opsRumorsText.text = "<i>No façade selected — tap a cell in the block zoom strip (top left).</i>";
            OpsRefreshCertainScrollLayout();
            return;
        }

        MicroBlockSpotRuntime spot = FindOpsSpotByStableId(_opsSelectedSpotStableId);
        if (spot == null)
        {
            _opsCertainInfoText.text = "";
            _opsRumorsText.text = "";
            OpsRefreshCertainScrollLayout();
            return;
        }

        var sb = new System.Text.StringBuilder(768);
        sb.AppendLine("<b>").Append(spot.SurfacePublicName).Append("</b>");
        sb.AppendLine("<size=100%><color=#c8c4bc>").Append(spot.SurfaceShortBlurb).Append("</color></size>");
        if (spot.AnchorLotId >= 0 && GameSessionState.ActiveCityData != null)
        {
            LotData mapLot = FindOpsLotById(GameSessionState.ActiveCityData, spot.AnchorLotId);
            sb.Append("<size=96%>City map: lot #").Append(spot.AnchorLotId);
            if (mapLot != null)
                sb.Append(" · block ").Append(mapLot.BlockId);
            sb.AppendLine("</size>");
        }
        sb.AppendLine();
        sb.AppendLine(MicroBlockKnowledgeStore.BuildGenericCertainKnowledgeBullets());
        sb.AppendLine();
        sb.Append(MicroBlockKnowledgeStore.BuildCertainKnowledgeTextForSpot(spot.StableId));

        if (spot.Kind == MicroBlockSpotKind.CrewSharedRoom)
        {
            bool overdue = MicroBlockWorldState.IsRentOverdueForCurrentDay();
            string status = overdue
                ? "<color=#e07070><b>Rent overdue</b></color> — pay " + MicroBlockWorldState.LandlordDisplayName
                : "<color=#8fcf8f>Prepaid through day " + MicroBlockWorldState.CrewRentPrepaidThroughDay + "</color>";
            sb.AppendLine();
            sb.AppendLine("<b>Weekly rent</b> $" + MicroBlockWorldState.CrewWeeklyRentUsd + " · " + status);
            sb.AppendLine("<i>Crew cash (clean):</i> $" + GameSessionState.CrewCash);
        }

        _opsCertainInfoText.text = sb.ToString();
        _opsRumorsText.text = MicroBlockKnowledgeStore.BuildRumorDigestTwoLinesForSpot(spot.StableId);
        OpsRefreshCertainScrollLayout();
    }

    private void OpsRefreshCertainScrollLayout()
    {
        if (_opsCertainInfoText == null)
            return;
        _opsCertainInfoText.ForceMeshUpdate();
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_opsCertainInfoText.rectTransform);
        if (_opsCertainInfoText.transform.parent is RectTransform contentRt)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);
        if (_opsCertainScroll != null)
            _opsCertainScroll.verticalNormalizedPosition = 1f;
    }

    private string BuildOpsNoSelectionCertainText()
    {
        GameSessionState.EnsureActiveCityData();
        if (_opsMapFocusedLotId >= 0 && GameSessionState.ActiveCityData != null)
        {
            LotData lot = FindOpsLotById(GameSessionState.ActiveCityData, _opsMapFocusedLotId);
            if (lot != null && lot.BlockId != MicroBlockWorldState.CrewHomeBlockId)
                return "<i>This lot is outside the crew home block — no local façade on file.</i>";
            if (lot != null && lot.BlockId == MicroBlockWorldState.CrewHomeBlockId && MicroBlockWorldState.FindSpotByAnchorLotId(lot.Id) == null)
                return "<i>This parcel has no linked façade (fewer spots than lots).</i>";
        }

        if (_opsCenterMapSelectedBlockId >= 0)
            return "<i>Tap a cell in the block zoom strip — it mirrors block " + _opsCenterMapSelectedBlockId +
                   " from the center map. Intel and actions follow the cell you pick.</i>";

        return "<i>Choose a block on the center map, then a cell in the zoom strip.</i>";
    }

    /// <summary>
    /// Multi-line hint rows must size to content; fixed LayoutElement heights clip TMP and the next button draws on top of text.
    /// </summary>
    private static void AddOpsActionsFlexibleHintRow(Transform parent, string goName, string richText, float fontSize, Color color,
        FontStyles fontStyle = FontStyles.Italic)
    {
        GameObject go = new GameObject(goName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = Vector2.zero;
        rt.localScale = Vector3.one;

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.flexibleWidth = 1f;
        le.minWidth = 0f;

        ContentSizeFitter csf = go.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        tmp.fontSize = fontSize;
        tmp.fontStyle = fontStyle;
        tmp.text = richText;
        tmp.color = color;
        tmp.richText = true;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.margin = new Vector4(4f, 2f, 4f, 6f);
        tmp.raycastTarget = false;
    }

    private void RebuildOpsActionsPanel()
    {
        for (int i = _opsActionsContentRoot.childCount - 1; i >= 0; i--)
            Destroy(_opsActionsContentRoot.GetChild(i).gameObject);

        MicroBlockSpotRuntime spot = FindOpsSpotByStableId(_opsSelectedSpotStableId);
        if (spot == null)
        {
            if (_opsCenterMapSelectedBlockId >= 0)
            {
                AddOpsActionsFlexibleHintRow(_opsActionsContentRoot, "BlockDispatchHint",
                    "No façade cell pinned yet — use the block zoom strip for a door or address. " +
                    "You can still send the crew to block <b>" + _opsCenterMapSelectedBlockId + "</b> now:\n" +
                    "<size=92%><color=#a0a0a0>Attack & pressure need a pinned cell below.</color></size>",
                    13f, new Color(0.72f, 0.70f, 0.65f, 1f));

                Button scoutBtn = CreateGovernmentShellRowButton(_opsActionsContentRoot, "Scout — map approaches & streets", 14.5f, 44f);
                scoutBtn.onClick.RemoveAllListeners();
                scoutBtn.onClick.AddListener(() => OpenOpsCrewAssignModal(OperationType.Scout));
                Button survBtn = CreateGovernmentShellRowButton(_opsActionsContentRoot, "Surveillance — watch the block", 14.5f, 44f);
                survBtn.onClick.RemoveAllListeners();
                survBtn.onClick.AddListener(() => OpenOpsCrewAssignModal(OperationType.Surveillance));
                Button collectSoon = CreateGovernmentShellRowButton(_opsActionsContentRoot, "Collect / pickup — pin a façade cell first", 14f, 40f);
                collectSoon.interactable = false;
                return;
            }

            AddOpsActionsFlexibleHintRow(_opsActionsContentRoot, "NoSelectionHint", "Select a place on the block map.", 13.5f,
                new Color(0.7f, 0.68f, 0.62f, 1f));
            return;
        }

        if (IsOpsSpotHiddenUnderSandboxMacroFog(spot))
        {
            AddOpsActionsFlexibleHintRow(_opsActionsContentRoot, "FogSpotHint",
                "<b>Off-map block</b> — this façade is in a block that isn’t drawn on the city map yet. " +
                "You can still queue <b>approach & watch</b>; door work and pressure wait until the block is in sight on the center map.",
                12.8f, new Color(0.78f, 0.72f, 0.62f, 1f));

            Button fogScout = CreateGovernmentShellRowButton(_opsActionsContentRoot, "Scout — map approaches & streets", 14.5f, 44f);
            fogScout.onClick.RemoveAllListeners();
            fogScout.onClick.AddListener(() => OpenOpsCrewAssignModal(OperationType.Scout));
            Button fogSurv = CreateGovernmentShellRowButton(_opsActionsContentRoot, "Surveillance — watch the block", 14.5f, 44f);
            fogSurv.onClick.RemoveAllListeners();
            fogSurv.onClick.AddListener(() => OpenOpsCrewAssignModal(OperationType.Surveillance));
            Button fogCollect = CreateGovernmentShellRowButton(_opsActionsContentRoot, "Collect / pickup — unlocks when block is on the map", 14f, 40f);
            fogCollect.interactable = false;

            AddOpsActionsFlexibleHintRow(_opsActionsContentRoot, "FogAttackNote",
                "<color=#a0a0a0>Attack & pressure: need the block visible on the center map.</color>", 12f,
                new Color(0.65f, 0.64f, 0.6f, 1f));

            Button fogDetailBtn = CreateGovernmentShellRowButton(_opsActionsContentRoot, "View façade notes (partial intel)", 15.5f, 46f);
            fogDetailBtn.onClick.RemoveAllListeners();
            MicroBlockSpotRuntime capFogSpot = spot;
            fogDetailBtn.onClick.AddListener(() => OpenMicroBlockSpotSurfaceDetail(capFogSpot));
            return;
        }

        OpsPlaceActionCatalog.Node[] roots = OpsPlaceActionCatalog.Roots;
        for (int i = 0; i < roots.Length; i++)
            AddOpsPlaceActionBranch(_opsActionsContentRoot, roots[i], 0);

        Button detailBtn = CreateGovernmentShellRowButton(_opsActionsContentRoot, "View full façade notes", 16f, 48f);
        detailBtn.onClick.RemoveAllListeners();
        MicroBlockSpotRuntime capSpot = spot;
        detailBtn.onClick.AddListener(() => OpenMicroBlockSpotSurfaceDetail(capSpot));

        if (spot.Kind == MicroBlockSpotKind.CrewSharedRoom)
        {
            Button payBtn = CreateGovernmentShellRowButton(_opsActionsContentRoot, "Pay week's rent", 16f, 48f);
            payBtn.onClick.RemoveAllListeners();
            payBtn.onClick.AddListener(() =>
            {
                MicroBlockWorldState.PayRentWeekInAdvanceFromSessionCash();
                RefreshOpsDashboard();
                UpdateTopBarMetrics();
            });
            payBtn.interactable = GameSessionState.CrewCash >= MicroBlockWorldState.CrewWeeklyRentUsd;
        }
    }

    private void AddOpsPlaceActionBranch(Transform parent, OpsPlaceActionCatalog.Node node, int depth)
    {
        if (node == null)
            return;

        if (node.Children != null && node.Children.Length > 0)
        {
            // Avoid U+25B6/U+25BC — not in default LiberationSans SDF; TMP logs warnings and shows □.
            string shut = "> " + node.Label;
            string open = "v " + node.Label;
            Button hdr = CreateGovernmentShellRowButton(parent, shut, 15f, 38f);
            TextMeshProUGUI hdrTmp = hdr.GetComponentInChildren<TextMeshProUGUI>();

            GameObject kidsGo = new GameObject("Kids_" + node.Id, typeof(RectTransform));
            kidsGo.transform.SetParent(parent, false);
            kidsGo.SetActive(false);
            RectTransform kidsRt = kidsGo.GetComponent<RectTransform>();
            kidsRt.anchorMin = new Vector2(0f, 1f);
            kidsRt.anchorMax = new Vector2(1f, 1f);
            kidsRt.pivot = new Vector2(0.5f, 1f);
            kidsRt.sizeDelta = Vector2.zero;
            LayoutElement leK = kidsGo.AddComponent<LayoutElement>();
            leK.flexibleWidth = 1f;
            leK.minHeight = 2f;
            VerticalLayoutGroup vk = kidsGo.AddComponent<VerticalLayoutGroup>();
            vk.spacing = 4f;
            vk.padding = new RectOffset(8 + depth * 10, 2, 0, 2);
            vk.childAlignment = TextAnchor.UpperLeft;
            vk.childControlWidth = true;
            vk.childControlHeight = true;
            vk.childForceExpandWidth = true;
            vk.childForceExpandHeight = false;
            ContentSizeFitter ksf = kidsGo.AddComponent<ContentSizeFitter>();
            ksf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ksf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            hdr.onClick.RemoveAllListeners();
            hdr.onClick.AddListener(() =>
            {
                bool show = !kidsGo.activeSelf;
                kidsGo.SetActive(show);
                if (hdrTmp != null)
                    hdrTmp.text = show ? open : shut;
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(_opsActionsContentRoot);
            });

            for (int i = 0; i < node.Children.Length; i++)
                AddOpsPlaceActionBranch(kidsGo.transform, node.Children[i], depth + 1);
            return;
        }

        string lab = node.Label;
        if (!node.Operation.HasValue)
            lab += " (soon)";
        Button leaf = CreateGovernmentShellRowButton(parent, lab, 14f, 36f);
        leaf.interactable = node.Operation.HasValue;
        if (node.Operation.HasValue)
        {
            OperationType op = node.Operation.Value;
            leaf.onClick.RemoveAllListeners();
            leaf.onClick.AddListener(() => OpenOpsCrewAssignModal(op));
        }
    }

    private void OpenMicroBlockSpotSurfaceDetail(MicroBlockSpotRuntime spot)
    {
        if (_opsNeighborhoodOverlayRoot == null || _opsNeighborhoodGridRt == null || _opsNeighborhoodTitleText == null)
            return;

        _opsNeighborhoodTitleText.text = spot.SurfacePublicName;

        for (int i = _opsNeighborhoodGridRt.childCount - 1; i >= 0; i--)
            Destroy(_opsNeighborhoodGridRt.GetChild(i).gameObject);

        GameObject body = new GameObject("MicroBlockSpotBody");
        body.transform.SetParent(_opsNeighborhoodGridRt, false);
        RectTransform brt = body.AddComponent<RectTransform>();
        StretchFull(brt);
        brt.offsetMin = new Vector2(8f, 8f);
        brt.offsetMax = new Vector2(-8f, -8f);
        TextMeshProUGUI tmp = body.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        tmp.fontSize = 15f;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.color = new Color(0.85f, 0.83f, 0.78f, 1f);
        tmp.richText = true;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        string mapAnchor = spot.AnchorLotId >= 0
            ? "\n\n<b>City map</b> — lot #" + spot.AnchorLotId + " (within crew home block)"
            : "\n\n<i>No city lot linked for this façade (fewer lots than spots in block).</i>";
        string fogBanner = IsOpsSpotHiddenUnderSandboxMacroFog(spot)
            ? "<color=#d4b896><b>Off-map block</b></color> — full street layout and door timing aren’t on your center map yet; " +
              "notes below are walk-by / hearsay until you close distance or finish a scout.\n\n"
            : string.Empty;
        tmp.text = fogBanner + "<b>Street face</b>\n" + spot.SurfaceShortBlurb + "\n\n" +
            MicroBlockKnowledgeStore.BuildFactsUiTextForSpot(spot.StableId) + mapAnchor;
        _opsNeighborhoodOverlayRoot.SetActive(true);
    }

    private void BuildOpsNeighborhoodDetailOverlay(Transform overlayRootTransform)
    {
        _opsNeighborhoodOverlayRoot = new GameObject("NeighborhoodDetailOverlay");
        _opsNeighborhoodOverlayRoot.transform.SetParent(overlayRootTransform, false);
        _opsNeighborhoodOverlayRoot.transform.SetAsLastSibling();
        RectTransform overlayRt = _opsNeighborhoodOverlayRoot.AddComponent<RectTransform>();
        StretchFull(overlayRt);
        _opsNeighborhoodOverlayRoot.SetActive(false);

        GameObject dimGo = new GameObject("Dim");
        dimGo.transform.SetParent(_opsNeighborhoodOverlayRoot.transform, false);
        RectTransform dimRt = dimGo.AddComponent<RectTransform>();
        StretchFull(dimRt);
        Image dimImg = dimGo.AddComponent<Image>();
        dimImg.color = new Color(0f, 0f, 0f, 0.55f);
        Button dimBtn = dimGo.AddComponent<Button>();
        dimBtn.targetGraphic = dimImg;
        dimBtn.transition = Selectable.Transition.None;
        dimBtn.onClick.AddListener(CloseOpsNeighborhoodDetail);

        GameObject shell = new GameObject("NeighborhoodPanel");
        shell.transform.SetParent(_opsNeighborhoodOverlayRoot.transform, false);
        RectTransform shRt = shell.AddComponent<RectTransform>();
        shRt.anchorMin = new Vector2(0.05f, 0.06f);
        shRt.anchorMax = new Vector2(0.95f, 0.94f);
        shRt.offsetMin = Vector2.zero;
        shRt.offsetMax = Vector2.zero;
        Image shImg = shell.AddComponent<Image>();
        shImg.color = new Color(0.11f, 0.11f, 0.12f, 0.99f);
        Outline shOut = shell.AddComponent<Outline>();
        shOut.effectColor = new Color(0.35f, 0.55f, 0.32f, 0.85f);
        shOut.effectDistance = new Vector2(2f, -2f);

        VerticalLayoutGroup v = shell.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(14, 14, 12, 12);
        v.spacing = 10f;
        v.childAlignment = TextAnchor.UpperCenter;
        v.childControlHeight = true;
        v.childControlWidth = true;
        v.childForceExpandHeight = false;
        v.childForceExpandWidth = true;

        GameObject titleGo = new GameObject("Title");
        titleGo.transform.SetParent(shell.transform, false);
        LayoutElement leTitle = titleGo.AddComponent<LayoutElement>();
        leTitle.preferredHeight = 28f;
        leTitle.flexibleHeight = 0f;
        _opsNeighborhoodTitleText = titleGo.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            _opsNeighborhoodTitleText.font = TMP_Settings.defaultFontAsset;
        _opsNeighborhoodTitleText.fontSize = 18f;
        _opsNeighborhoodTitleText.fontStyle = FontStyles.Bold;
        _opsNeighborhoodTitleText.alignment = TextAlignmentOptions.Center;
        _opsNeighborhoodTitleText.color = new Color(0.9f, 0.88f, 0.82f, 1f);
        _opsNeighborhoodTitleText.text = "Neighborhood";

        GameObject hintGo = new GameObject("Hint");
        hintGo.transform.SetParent(shell.transform, false);
        LayoutElement leHint = hintGo.AddComponent<LayoutElement>();
        leHint.preferredHeight = 22f;
        TextMeshProUGUI hintTmp = hintGo.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            hintTmp.font = TMP_Settings.defaultFontAsset;
        hintTmp.fontSize = 13f;
        hintTmp.alignment = TextAlignmentOptions.Center;
        hintTmp.color = new Color(0.65f, 0.63f, 0.58f, 0.95f);
        hintTmp.text =
            "CityGen map: lots by district · dark lines = roads · scroll to zoom · tap a lot for details · click outside to close";

        GameObject gridHost = new GameObject("SubLotGridHost");
        gridHost.transform.SetParent(shell.transform, false);
        LayoutElement leGridHost = gridHost.AddComponent<LayoutElement>();
        leGridHost.flexibleHeight = 1f;
        leGridHost.minHeight = 120f;

        GameObject gridGo = new GameObject("LotDetailHost");
        gridGo.transform.SetParent(gridHost.transform, false);
        _opsNeighborhoodGridRt = gridGo.AddComponent<RectTransform>();
        StretchFull(_opsNeighborhoodGridRt);

        GameObject closeRow = new GameObject("CloseRow");
        closeRow.transform.SetParent(shell.transform, false);
        LayoutElement leClose = closeRow.AddComponent<LayoutElement>();
        leClose.preferredHeight = 36f;
        Button closeExplicit = CreateBarButton(closeRow.transform, "Close");
        closeExplicit.onClick.RemoveAllListeners();
        closeExplicit.onClick.AddListener(CloseOpsNeighborhoodDetail);
    }

    /// <summary>Left strip block zoom: same lot pick as center map (single code path via <see cref="OpsCityGenMapView.RebuildLotsForBlock"/>).</summary>
    private void OnOpsBlockZoomLotClicked(LotData lot)
    {
        if (lot == null)
            return;
        MicroBlockSpotRuntime spot = MicroBlockWorldState.FindSpotByAnchorLotId(lot.Id);
        if (spot != null)
            SelectOpsSpot(spot);
    }

    /// <summary>Center map block click → left column shows that block’s 3×3; detail panels update when a cell is chosen.</summary>
    private void OnOpsMacroBlockClicked(BlockData block)
    {
        if (block == null)
            return;
        GameSessionState.EnsureActiveCityData();
        if (GameSessionState.ActiveCityData == null)
            return;

        _opsCenterMapSelectedBlockId = block.Id;
        _opsSelectedSpotStableId = string.Empty;
        _opsMapFocusedLotId = -1;

        if (_current == PlanningTabId.Operations)
            RebuildOpsCityMapIfNeeded();
        RefreshOpsDashboard();
    }

    private Vector2 GetOpsMapViewportForRebuild()
    {
        if (_opsMapAreaRt == null)
            return _opsMapViewportFallback;
        Rect r = _opsMapAreaRt.rect;
        if (r.width >= 16f && r.height >= 16f)
        {
            _opsMapViewportFallback = new Vector2(r.width, r.height);
            return _opsMapViewportFallback;
        }

        return _opsMapViewportFallback;
    }

    private void RebuildOpsCityMapIfNeeded()
    {
        if (_opsMapGridRoot == null)
            return;

        int seed = GameSessionState.CityMapSeed;
        if (seed == 0)
        {
            seed = Random.Range(1, int.MaxValue);
            GameSessionState.CityMapSeed = seed;
        }

        GameSessionState.EnsureActiveCityData();

        Vector2 vp = GetOpsMapViewportForRebuild();
        const float vpEps = 8f;
        bool vpDirty = _opsMapRebuildViewport.sqrMagnitude < 0.5f
            || Mathf.Abs(vp.x - _opsMapRebuildViewport.x) > vpEps
            || Mathf.Abs(vp.y - _opsMapRebuildViewport.y) > vpEps;

        int rev = GameSessionState.ActiveCityDataRevision;
        bool structuralRebuild = seed != _opsCachedOpsCitySeed
            || rev != _opsBuiltCityDataRevision
            || _opsMapGridRoot.childCount == 0;

        int focusForMap = _opsMapFocusedLotId;
        if (focusForMap < 0 && !string.IsNullOrEmpty(_opsSelectedSpotStableId))
        {
            MicroBlockSpotRuntime sp = FindOpsSpotByStableId(_opsSelectedSpotStableId);
            if (sp != null && sp.AnchorLotId >= 0)
                focusForMap = sp.AnchorLotId;
        }

        bool needRebuild = structuralRebuild
            || vpDirty
            || _opsLastMacroPaintedBlockId != _opsCenterMapSelectedBlockId;
        if (needRebuild)
        {
            if (structuralRebuild)
            {
                _opsCachedOpsCitySeed = seed;
                _opsBuiltCityDataRevision = rev;
                _opsMapUserZoom = 1f;
            }

            Canvas.ForceUpdateCanvases();
            vp = GetOpsMapViewportForRebuild();
            OpsCityGenMapView.RebuildBlockMacro(_opsMapGridRoot, GameSessionState.ActiveCityData, OnOpsMacroBlockClicked,
                MicroBlockWorldState.CrewHomeBlockId, _opsCenterMapSelectedBlockId, vp);
            _opsLastMacroPaintedBlockId = _opsCenterMapSelectedBlockId;

            _opsMapRebuildViewport = vp;
            _opsLastMapPaintedFocusLotId = focusForMap;
            _opsMapGridRoot.anchoredPosition = Vector2.zero;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_opsMapGridRoot);
        }

        ApplyOpsMapScaleToFitCityGen();
    }

    private void TryOpsMapScrollZoom()
    {
        if (_current != PlanningTabId.Operations || _opsStageOverlayRoot == null || !_opsStageOverlayRoot.activeSelf)
            return;
        if (_opsNeighborhoodOverlayRoot != null && _opsNeighborhoodOverlayRoot.activeSelf)
            return;
        if (_opsMapAreaRt == null || _opsMapGridRoot == null || _opsMapGridRoot.sizeDelta.x < 2f)
            return;

        Vector2 screenPos = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : (Vector2)Input.mousePosition;
        if (!RectTransformUtility.RectangleContainsScreenPoint(_opsMapAreaRt, screenPos, null))
            return;

        float wheel = Mouse.current != null ? Mouse.current.scroll.ReadValue().y : Input.mouseScrollDelta.y * 25f;
        if (Mathf.Abs(wheel) < 0.01f)
            return;

        float factor = wheel > 0f ? 1.1f : 1f / 1.1f;
        _opsMapUserZoom = Mathf.Clamp(_opsMapUserZoom * factor, OpsMapUserZoomMin, OpsMapUserZoomMax);
        ApplyOpsMapZoomOnly();
    }

    private void ApplyOpsMapZoomOnly()
    {
        if (_opsMapGridRoot == null)
            return;
        _opsMapGridRoot.localScale = Vector3.one * _opsMapUserZoom;
        ClampOpsMapPanToBounds();
    }

    private void OpenOpsLotDetail(LotData lot)
    {
        if (lot == null)
            return;

        GameSessionState.EnsureActiveCityData();

        int homeBlock = MicroBlockWorldState.CrewHomeBlockId;
        if (lot.BlockId == homeBlock)
        {
            MicroBlockSpotRuntime spot = MicroBlockWorldState.FindSpotByAnchorLotId(lot.Id);
            if (spot != null)
            {
                SelectOpsSpot(spot);
                if (_opsNeighborhoodOverlayRoot != null && _opsNeighborhoodOverlayRoot.activeSelf)
                    CloseOpsNeighborhoodDetail();
                return;
            }
        }

        _opsMapFocusedLotId = lot.Id;
        _opsSelectedSpotStableId = string.Empty;
        if (_current == PlanningTabId.Operations)
            RebuildOpsCityMapIfNeeded();
        RefreshOpsDashboard();
        if (_opsNeighborhoodOverlayRoot != null && _opsNeighborhoodOverlayRoot.activeSelf)
            CloseOpsNeighborhoodDetail();
    }

    private static string BuildOpsLotDetailBody(LotData lot)
    {
        var sb = new System.Text.StringBuilder(256);
        sb.AppendLine("Block " + lot.BlockId + " · district id " + lot.DistrictId);
        sb.AppendLine("Area: " + lot.AreaCells.ToString("0.##") + " plan cells");
        sb.AppendLine("Touches road: " + (lot.TouchesRoad ? "yes" : "no") + " · frontage " + lot.FrontageRoadKind +
            " (" + lot.FrontageLength.ToString("0.##") + ")");
        if (lot.IsReserved)
        {
            sb.AppendLine();
            sb.AppendLine("<b>Reserved</b> for " + lot.ReservedForKind);
            if (lot.ReservedByInstitutionId >= 0)
                sb.AppendLine("Institution id: " + lot.ReservedByInstitutionId);
        }

        if (lot.RegularBuildingId >= 0)
            sb.AppendLine("Building id: " + lot.RegularBuildingId);

        return sb.ToString().TrimEnd();
    }

    private void CloseOpsNeighborhoodDetail()
    {
        if (_opsNeighborhoodOverlayRoot != null)
            _opsNeighborhoodOverlayRoot.SetActive(false);
    }

    /// <summary>
    /// Contain-fit is baked in <see cref="OpsCityGenMapView.Rebuild"/>; here we only apply user zoom and re-clamp pan.
    /// </summary>
    private void ApplyOpsMapScaleToFitCityGen()
    {
        if (_opsMapAreaRt == null || _opsMapGridRoot == null)
            return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_opsMapGridRoot);

        Rect r = _opsMapAreaRt.rect;
        if (r.width < 16f || r.height < 16f)
        {
            StartCoroutine(DelayedApplyOpsMapScaleCityGen());
            return;
        }

        _opsMapGridRoot.anchorMin = _opsMapGridRoot.anchorMax = new Vector2(0.5f, 0.5f);
        _opsMapGridRoot.pivot = new Vector2(0.5f, 0.5f);
        if (_opsMapUserZoom < OpsMapUserZoomMin)
            _opsMapUserZoom = OpsMapUserZoomMin;
        _opsMapGridRoot.localScale = Vector3.one * _opsMapUserZoom;
        ClampOpsMapPanToBounds();
    }

    /// <summary>Keeps the map content covering the viewport — no gray peeking past the edges when panning/zooming.</summary>
    private void ClampOpsMapPanToBounds()
    {
        if (_opsMapGridRoot == null || _opsMapAreaRt == null)
            return;

        RectTransform grid = _opsMapGridRoot;
        RectTransform viewport = _opsMapAreaRt;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(grid);

        float halfW = 0.5f * grid.rect.width * Mathf.Abs(grid.localScale.x);
        float halfH = 0.5f * grid.rect.height * Mathf.Abs(grid.localScale.y);

        Rect vr = viewport.rect;
        float vhW = 0.5f * Mathf.Max(1f, vr.width);
        float vhH = 0.5f * Mathf.Max(1f, vr.height);

        Vector2 ap = grid.anchoredPosition;

        if (halfW > vhW + 0.5f)
        {
            float minX = vhW - halfW;
            float maxX = -vhW + halfW;
            ap.x = Mathf.Clamp(ap.x, minX, maxX);
        }
        else
            ap.x = 0f;

        if (halfH > vhH + 0.5f)
        {
            float minY = vhH - halfH;
            float maxY = -vhH + halfH;
            ap.y = Mathf.Clamp(ap.y, minY, maxY);
        }
        else
            ap.y = 0f;

        grid.anchoredPosition = ap;
    }

    private System.Collections.IEnumerator DelayedApplyOpsMapScaleCityGen()
    {
        yield return null;
        _opsMapRebuildViewport = Vector2.zero;
        if (_current == PlanningTabId.Operations)
            RebuildOpsCityMapIfNeeded();
        else
            ApplyOpsMapScaleToFitCityGen();
    }

    private void ApplyPlanningTabChromeVisibility(PlanningTabId tab)
    {
        bool ops = tab == PlanningTabId.Operations;
        if (_opsStageOverlayRoot != null)
            _opsStageOverlayRoot.SetActive(ops);

        if (ops)
            EnsureOpsOverlayMatchesMainArea();

        if (!ops)
            CloseOpsNeighborhoodDetail();

        if (ops)
            BringPlanningInteractiveChromeToFront();

        // Keep title + context for News too — hiding them made the tab feel "empty" (only grey center).
        bool showChrome = !ops;
        if (_titleText != null)
            _titleText.gameObject.SetActive(showChrome);
        if (_contextText != null)
            _contextText.gameObject.SetActive(showChrome);

        // The main content row should remain visible for all non-ops tabs (including News).
        if (_threeColumnRow != null)
            _threeColumnRow.SetActive(!ops);
    }

    private TextMeshProUGUI BuildScrollableCenterColumn(Transform rowParent)
    {
        GameObject scrollRoot = new GameObject("CenterScrollView");
        scrollRoot.transform.SetParent(rowParent, false);
        RectTransform scrollRt = scrollRoot.AddComponent<RectTransform>();
        scrollRt.sizeDelta = Vector2.zero;

        LayoutElement leScroll = scrollRoot.AddComponent<LayoutElement>();
        leScroll.flexibleWidth = 1f;
        leScroll.flexibleHeight = 1f;
        leScroll.minWidth = 220f;

        ScrollRect scroll = scrollRoot.AddComponent<ScrollRect>();
        _centerScrollRect = scroll;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.scrollSensitivity = 24f;

        GameObject viewportGo = new GameObject("Viewport");
        viewportGo.transform.SetParent(scrollRoot.transform, false);
        RectTransform viewportRt = viewportGo.AddComponent<RectTransform>();
        StretchFull(viewportRt);
        Image vpImg = viewportGo.AddComponent<Image>();
        vpImg.color = new Color(0f, 0f, 0f, 0.12f);
        vpImg.raycastTarget = true;
        _centerScrollViewportImage = vpImg;
        _centerScrollViewportRoot = viewportGo.transform;
        Mask mask = viewportGo.AddComponent<Mask>();
        mask.showMaskGraphic = true;

        GameObject contentGo = new GameObject("Content");
        contentGo.transform.SetParent(viewportGo.transform, false);
        _centerScrollContentRoot = contentGo.transform;
        RectTransform contentRt = contentGo.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.offsetMin = new Vector2(8f, 0f);
        contentRt.offsetMax = new Vector2(-8f, 0f);

        ContentSizeFitter fitter = contentGo.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        LayoutElement leContent = contentGo.AddComponent<LayoutElement>();
        leContent.minWidth = 200f;

        TextMeshProUGUI tmp = CreateTmp(contentGo.transform, "CenterText", 15f, FontStyles.Normal);
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.overflowMode = TextOverflowModes.Overflow;

        RectTransform tmpRt = tmp.rectTransform;
        tmpRt.anchorMin = new Vector2(0f, 1f);
        tmpRt.anchorMax = new Vector2(1f, 1f);
        tmpRt.pivot = new Vector2(0.5f, 1f);
        tmpRt.offsetMin = Vector2.zero;
        tmpRt.offsetMax = Vector2.zero;
        tmpRt.sizeDelta = Vector2.zero;

        LayoutElement leTmp = tmp.gameObject.AddComponent<LayoutElement>();
        leTmp.minWidth = 240f;
        leTmp.flexibleWidth = 1f;

        scroll.viewport = viewportRt;
        scroll.content = contentRt;
        return tmp;
    }

    private void BuildNewsNewspaperUi()
    {
        if (_centerScrollViewportRoot == null)
        {
            Debug.LogWarning("PlanningShellController: News newspaper UI skipped — center scroll viewport missing.");
            return;
        }

        Transform scrollHost = _centerScrollViewportRoot.parent;
        if (scrollHost == null)
        {
            Debug.LogWarning("PlanningShellController: News newspaper UI skipped — center ScrollRect root missing.");
            return;
        }

        if (_newsPaperRoot != null)
            Destroy(_newsPaperRoot);

        // Must include RectTransform at creation — plain new GameObject() + SetParent does not reliably get a RectTransform
        // in the same frame, and AddComponent<Image>() then throws MissingComponentException.
        GameObject root = new GameObject("NewsNewspaperRoot", typeof(RectTransform));
        // Sibling of Viewport under CenterScrollView: same on-screen rect as the viewport but NOT under Mask/stencil
        // (fixes invisible paper when masking + null sprite interact badly on some setups).
        root.transform.SetParent(scrollHost, false);
        root.transform.SetAsLastSibling();
        _newsPaperRoot = root;
        root.SetActive(false);

        RectTransform rt = (RectTransform)root.transform;
        StretchFull(rt);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.localScale = Vector3.one;

        _newsPaperBackground = root.AddComponent<Image>();
        _newsPaperBackground.sprite = GetPlanningUiWhiteSprite();
        _newsPaperBackground.type = Image.Type.Simple;
        _newsPaperBackground.color = new Color(0.93f, 0.92f, 0.88f, 0.98f);
        _newsPaperBackground.raycastTarget = false;

        Outline o = root.AddComponent<Outline>();
        o.effectColor = new Color(0f, 0f, 0f, 0.22f);
        o.effectDistance = new Vector2(2f, -2f);

        VerticalLayoutGroup v = root.AddComponent<VerticalLayoutGroup>();
        v.childAlignment = TextAnchor.UpperLeft;
        v.childControlWidth = true;
        v.childControlHeight = false;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = true;
        v.spacing = 10f;
        v.padding = new RectOffset(18, 18, 16, 18);

        GameObject mastGo = new GameObject("Masthead", typeof(RectTransform));
        mastGo.transform.SetParent(root.transform, false);
        _newsMastheadText = mastGo.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset headlineFont = LoadNewsHeadlineFont();
        if (headlineFont != null)
            _newsMastheadText.font = headlineFont;
        else if (TMP_Settings.defaultFontAsset != null)
            _newsMastheadText.font = TMP_Settings.defaultFontAsset;
        _newsMastheadText.richText = true;
        _newsMastheadText.fontSize = 34f;
        _newsMastheadText.fontStyle = FontStyles.Bold;
        _newsMastheadText.alignment = TextAlignmentOptions.Center;
        _newsMastheadText.color = new Color(0.08f, 0.08f, 0.08f, 1f);
        _newsMastheadText.text = "New Temperance Post";
        LayoutElement mastLe = mastGo.AddComponent<LayoutElement>();
        mastLe.minHeight = 44f;

        GameObject dateGo = new GameObject("DateLine", typeof(RectTransform));
        dateGo.transform.SetParent(root.transform, false);
        _newsDateLineText = dateGo.AddComponent<TextMeshProUGUI>();
        if (headlineFont != null)
            _newsDateLineText.font = headlineFont;
        else if (TMP_Settings.defaultFontAsset != null)
            _newsDateLineText.font = TMP_Settings.defaultFontAsset;
        _newsDateLineText.richText = true;
        _newsDateLineText.fontSize = 14f;
        _newsDateLineText.alignment = TextAlignmentOptions.Center;
        _newsDateLineText.color = new Color(0.12f, 0.12f, 0.12f, 0.9f);
        _newsDateLineText.text = "";
        LayoutElement dateLe = dateGo.AddComponent<LayoutElement>();
        dateLe.minHeight = 18f;

        GameObject secGo = new GameObject("Sections", typeof(RectTransform));
        secGo.transform.SetParent(root.transform, false);
        HorizontalLayoutGroup sh = secGo.AddComponent<HorizontalLayoutGroup>();
        sh.spacing = 8f;
        sh.padding = new RectOffset(0, 0, 0, 0);
        sh.childAlignment = TextAnchor.MiddleCenter;
        // Fixed-width section chips: center the row as a block inside the full-width strip.
        sh.childControlWidth = false;
        sh.childControlHeight = true;
        sh.childForceExpandWidth = false;
        sh.childForceExpandHeight = false;
        LayoutElement secLe = secGo.AddComponent<LayoutElement>();
        secLe.minHeight = 44f;

        _newsSectionButtons.Clear();
        (string label, NewsSectionId id)[] entries =
        {
            ("Front Page", NewsSectionId.FrontPage),
            ("Finance", NewsSectionId.Finance),
            ("Law & Justice", NewsSectionId.LawAndJustice),
            ("Crime", NewsSectionId.Crime),
            ("Obituaries", NewsSectionId.Obituaries),
            ("Federal", NewsSectionId.Federal)
        };
        for (int i = 0; i < entries.Length; i++)
        {
            NewsSectionId captured = entries[i].id;
            Button b = CreateBarButton(secGo.transform, entries[i].label);
            RectTransform br = b.GetComponent<RectTransform>();
            if (br != null)
                br.sizeDelta = new Vector2(0f, 42f);
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(() =>
            {
                _newsActiveSection = captured;
                RefreshNewsNewspaper();
            });
            _newsSectionButtons.Add(b);
        }

        GameObject divGo = new GameObject("Divider", typeof(RectTransform));
        divGo.transform.SetParent(root.transform, false);
        Image divImg = divGo.AddComponent<Image>();
        divImg.sprite = GetPlanningUiWhiteSprite();
        divImg.type = Image.Type.Simple;
        divImg.color = new Color(0f, 0f, 0f, 0.18f);
        divImg.raycastTarget = false;
        LayoutElement divLe = divGo.AddComponent<LayoutElement>();
        divLe.minHeight = 2f;
        divLe.preferredHeight = 2f;

        GameObject bodyGo = new GameObject("Body", typeof(RectTransform));
        bodyGo.transform.SetParent(root.transform, false);
        _newsBodyText = bodyGo.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            _newsBodyText.font = TMP_Settings.defaultFontAsset;
        _newsBodyText.richText = true;
        _newsBodyText.fontSize = 16f;
        _newsBodyText.alignment = TextAlignmentOptions.TopLeft;
        _newsBodyText.textWrappingMode = TextWrappingModes.Normal;
        _newsBodyText.color = new Color(0.10f, 0.10f, 0.10f, 0.98f);
        _newsBodyText.text = "";
        LayoutElement bodyLe = bodyGo.AddComponent<LayoutElement>();
        bodyLe.minHeight = 0f;
        bodyLe.flexibleHeight = 1f;
    }

    private void RefreshNewsNewspaper()
    {
        if (_newsPaperRoot == null || _newsBodyText == null)
            return;

        if (_newsDateLineText != null)
        {
            _newsDateLineText.text =
                GameCalendarSystem.FormatPlanningHudLineShort(GameSessionState.CurrentDay) +
                " · City Edition";
        }

        for (int i = 0; i < _newsSectionButtons.Count; i++)
        {
            Button b = _newsSectionButtons[i];
            if (b == null) continue;
            Image img = b.GetComponent<Image>();
            if (img == null) continue;
            bool active = (int)_newsActiveSection == i;
            img.color = active ? new Color(0.18f, 0.18f, 0.18f, 0.92f) : new Color(0.12f, 0.12f, 0.12f, 0.65f);
        }

        GameSessionState.GenerateLocalPaperBlurbIfNeeded(force: false);
        string localPaper = string.IsNullOrWhiteSpace(GameSessionState.LastLocalPaperBlurb)
            ? "<i>No fresh local chatter.</i>"
            : GameSessionState.LastLocalPaperBlurb.Trim();
        string blotter = string.IsNullOrWhiteSpace(GameSessionState.LastPoliceInvestigationUpdate)
            ? "<i>No fresh police activity on your file.</i>"
            : GameSessionState.LastPoliceInvestigationUpdate.Trim();

        string header = "<size=120%><b>" + _newsActiveSection + "</b></size>\n\n";
        switch (_newsActiveSection)
        {
            case NewsSectionId.FrontPage:
                _newsBodyText.text =
                    "<size=160%><b>GANG WARFARE COMES TO TOWN!</b></size>\n" +
                    "<size=90%><i>City in corruption scandal as prohibition takes hold</i></size>\n\n" +
                    "<b>Local paper</b>\n" + localPaper + "\n\n" +
                    "<b>Police blotter</b>\n" + blotter + "\n\n" +
                    "<size=90%><i>1920s rule:</i> no report -> no investigation; no witness/HUMINT -> no suspect linkage.</size>";
                break;

            case NewsSectionId.Finance:
                _newsBodyText.text =
                    header +
                    "• <b>Market</b>: commodity prices wobble under enforcement pressure.\n" +
                    "• <b>Banks</b>: tighter questions on large deposits and unusual transfers.\n" +
                    "• <b>City contracts</b>: rumors of a bid scandal tied to ward politics.\n\n" +
                    "<size=90%><i>Tip:</i> moving money cleanly reduces long-term exposure.</size>";
                break;

            case NewsSectionId.LawAndJustice:
                _newsBodyText.text =
                    header +
                    "• <b>Courthouse</b>: judges debate sentencing ranges amid public outrage.\n" +
                    "• <b>Docket</b>: delays grow as evidence handling becomes a battleground.\n" +
                    "• <b>Legal codex</b>: penalty ranges drive negotiations and motions.\n\n" +
                    "<size=90%><i>Hook:</i> open the Law Codex in the LEGAL tab to study ranges.</size>";
                break;

            case NewsSectionId.Crime:
                _newsBodyText.text =
                    header +
                    "• <b>District</b>: whispered protection rackets and backroom deals.\n" +
                    "• <b>Street</b>: patrol patterns shift after a string of incidents.\n\n" +
                    "<b>Police blotter</b>\n" +
                    blotter;
                break;

            case NewsSectionId.Obituaries:
                _newsBodyText.text =
                    header +
                    "• <b>In memoriam</b>: a dockworker, a bookie, and a respected grocer.\n" +
                    "• <b>Services</b>: private, closed casket, family requests discretion.\n\n" +
                    "<size=90%><i>Note:</i> obituaries can hint at grudges, heirs, and leverage.</size>";
                break;

            case NewsSectionId.Federal:
                _newsBodyText.text =
                    header +
                    "• <b>Rumor</b>: a federal unit is mapping supply chains and intermediaries.\n" +
                    "• <b>Mail & rail</b>: inspectors increase random checks on freight.\n\n" +
                    "<b>Intel hint</b>\n" +
                    GameSessionState.GetAgencyIntelHint(GameSessionState.AgencyId.FederalBureau);
                break;
        }
    }

    private void BuildMissionPrepRow(Transform shell)
    {
        _missionRow = new GameObject("MissionPrepRow");
        _missionRow.transform.SetParent(shell, false);

        HorizontalLayoutGroup h = _missionRow.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 10f;
        h.padding = new RectOffset(0, 0, 0, 0);
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = false;
        h.childControlHeight = true;

        LayoutElement leRow = _missionRow.AddComponent<LayoutElement>();
        leRow.preferredHeight = 40f;
        leRow.minHeight = 36f;

        TextMeshProUGUI hint = CreateTmp(_missionRow.transform, "MissionHint", 14f, FontStyles.Normal);
        hint.text = "Tonight (execution):";
        LayoutElement leHint = hint.gameObject.AddComponent<LayoutElement>();
        leHint.preferredWidth = 150f;
        leHint.minWidth = 120f;

        foreach (OperationType op in System.Enum.GetValues(typeof(OperationType)))
        {
            Button btn = CreateOpButton(_missionRow.transform, op);
            _opButtons[op] = btn;
        }

        _missionRow.SetActive(false);

        _missionQueueOrderStrip = new GameObject("MissionQueueOrderStrip");
        _missionQueueOrderStrip.transform.SetParent(shell, false);
        LayoutElement leQ = _missionQueueOrderStrip.AddComponent<LayoutElement>();
        leQ.preferredHeight = 0f;
        leQ.minHeight = 0f;
        leQ.flexibleWidth = 1f;
        _missionQueueOrderStrip.SetActive(false);
    }

    private void RebuildMissionQueueOrderStrip()
    {
        if (_missionQueueOrderStrip == null)
            return;
        for (int i = _missionQueueOrderStrip.transform.childCount - 1; i >= 0; i--)
            Destroy(_missionQueueOrderStrip.transform.GetChild(i).gameObject);

        int n = GameSessionState.OrderedOperations.Count;
        if (n < 2)
        {
            _missionQueueOrderStrip.SetActive(false);
            LayoutElement le = _missionQueueOrderStrip.GetComponent<LayoutElement>();
            if (le != null)
                le.preferredHeight = 0f;
            return;
        }

        _missionQueueOrderStrip.SetActive(true);
        LayoutElement leStrip = _missionQueueOrderStrip.GetComponent<LayoutElement>();
        if (leStrip != null)
            leStrip.preferredHeight = 34f;

        HorizontalLayoutGroup h = _missionQueueOrderStrip.GetComponent<HorizontalLayoutGroup>();
        if (h == null)
        {
            h = _missionQueueOrderStrip.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 6f;
            h.padding = new RectOffset(0, 0, 2, 0);
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlHeight = true;
            h.childControlWidth = false;
        }

        TextMeshProUGUI hdr = CreateTmp(_missionQueueOrderStrip.transform, "QHint", 12.5f, FontStyles.Italic);
        hdr.text = "Run order:";
        hdr.color = new Color(0.75f, 0.73f, 0.68f, 1f);
        hdr.gameObject.AddComponent<LayoutElement>().preferredWidth = 72f;

        for (int i = 0; i < n; i++)
        {
            OperationType op = GameSessionState.OrderedOperations[i];
            int idx = i;
            TextMeshProUGUI lab = CreateTmp(_missionQueueOrderStrip.transform, "Q_" + i, 12f, FontStyles.Normal);
            lab.text = (i + 1) + "." + OperationRegistry.GetName(op);
            lab.color = new Color(0.88f, 0.86f, 0.8f, 1f);
            lab.gameObject.AddComponent<LayoutElement>().preferredWidth = 108f;

            if (i > 0)
            {
                Button up = CreateBarButton(_missionQueueOrderStrip.transform, "↑");
                LayoutElement leU = up.gameObject.AddComponent<LayoutElement>();
                leU.preferredWidth = 36f;
                int swapWith = idx - 1;
                up.onClick.RemoveAllListeners();
                up.onClick.AddListener(() =>
                {
                    GameSessionState.SwapQueuedOperationsOrder(idx, swapWith);
                    RebuildMissionQueueOrderStrip();
                    if (_current == PlanningTabId.Operations && _centerText != null)
                        _centerText.text = BuildOperationsCenterText();
                });
            }

            if (i < n - 1)
            {
                Button dn = CreateBarButton(_missionQueueOrderStrip.transform, "↓");
                LayoutElement leD = dn.gameObject.AddComponent<LayoutElement>();
                leD.preferredWidth = 36f;
                int swapWith = idx + 1;
                dn.onClick.RemoveAllListeners();
                dn.onClick.AddListener(() =>
                {
                    GameSessionState.SwapQueuedOperationsOrder(idx, swapWith);
                    RebuildMissionQueueOrderStrip();
                    if (_current == PlanningTabId.Operations && _centerText != null)
                        _centerText.text = BuildOperationsCenterText();
                });
            }
        }

        Canvas.ForceUpdateCanvases();
    }

    private Button CreateOpButton(Transform parent, OperationType op)
    {
        string label = OperationRegistry.GetName(op) + ": OFF";
        Button btn = CreateBarButton(parent, label);
        LayoutElement le = btn.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = 120f;
        le.minHeight = 36f;
        btn.onClick.AddListener(() => OpenOpsCrewAssignModal(op));
        return btn;
    }

    private void UpdateAllOpButtonLabels()
    {
        foreach (var kv in _opButtons)
        {
            TextMeshProUGUI tmp = kv.Value.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                string suffix = string.Empty;
                if (OperationRegistry.IsOrdered(kv.Key))
                {
                    int ai = GameSessionState.GetOperationAssignee(kv.Key);
                    if (ai >= 0 && ai < PersonnelRegistry.Members.Count)
                        suffix = " — " + OpsFormatMemberShortName(PersonnelRegistry.Members[ai]);
                }

                tmp.text = OperationRegistry.GetName(kv.Key) + ": " + (OperationRegistry.IsOrdered(kv.Key) ? "ON" : "OFF") + suffix;
            }
        }

        RebuildMissionQueueOrderStrip();
    }

    private static string OpsFormatMemberShortName(CrewMember m)
    {
        if (m == null || string.IsNullOrEmpty(m.Name))
            return "?";
        string n = m.Name.Trim();
        return n.Length <= 22 ? n : n.Substring(0, 20) + "…";
    }

    private static string BuildOperationsCenterText()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("<b>Ops board</b>");
        sb.AppendLine("<size=92%><color=#b0b0b0>End Turn = one calendar day (" + OperationTimingSystem.HoursPerGameday.ToString("0") +
                       " h). Travel uses block-to-block distance; work time shrinks with squad size.</color></size>");
        if (GameSessionState.OrderedOperations.Count == 0)
        {
            sb.AppendLine("• No mission toggled — use the buttons above to queue operations.");
        }
        else
        {
            GameSessionState.EnsureActiveCityData();
            CityData city = GameSessionState.ActiveCityData;
            int homeBlock = MicroBlockWorldState.CrewHomeBlockId;

            foreach (OperationType op in GameSessionState.OrderedOperations)
            {
                CoreTrait primary = OperationRegistry.GetPrimaryTrait(op);
                CoreTrait secondary = OperationRegistry.GetSecondaryTrait(op);
                int ai = GameSessionState.GetOperationAssignee(op);
                bool vehicle = GameSessionState.OperationMissionUsesCrewVehicle.TryGetValue(op, out bool mv)
                    ? mv
                    : GameSessionState.ResolveCrewOpsVehicle();
                string who = string.Empty;
                if (ai >= 0 && ai < PersonnelRegistry.Members.Count)
                    who = " <size=95%>(lead: " + OpsFormatMemberShortName(PersonnelRegistry.Members[ai]) + ")</size>";
                sb.AppendLine("• <b>" + OperationRegistry.GetName(op) + "</b> — " + OperationRegistry.GetDescription(op) + who);
                sb.AppendLine("  Main trait: " + OperationRegistry.GetTraitName(primary) + " | Support: " + OperationRegistry.GetTraitName(secondary));

                int targetBlock = GameSessionState.GetOperationTargetBlockId(op);
                int squad = GameSessionState.GetOperationSquadSizeForDisplay(op, ai);
                GameSeason season = GameCalendarSystem.GetSeason(GameSessionState.CurrentDay);
                WeatherSnapshot wx = GameWeatherResolver.Resolve(GameSessionState.CurrentDay);
                OperationTimingSystem.EstimateMissionHours(city, homeBlock, targetBlock, op, squad, vehicle, season, in wx, out float travelH, out float execH);
                sb.AppendLine(OperationTimingSystem.FormatEstimatesLine(travelH, execH, squad, vehicle, season, in wx));
            }
        }
        sb.AppendLine();
        sb.AppendLine("<i>Contingencies and load vs. other jobs — next.</i>");
        return sb.ToString();
    }

    private static string BuildOpsRightPanel()
    {
        var sb = new System.Text.StringBuilder();
        PlayerCharacterProfile boss = PlayerRunState.Character;
        sb.AppendLine("<b>Status</b>");
        sb.AppendLine("• Queued: " + GameSessionState.OrderedOperations.Count);
        sb.AppendLine("• Heat: Low");
        foreach (OperationType op in System.Enum.GetValues(typeof(OperationType)))
            sb.AppendLine("• " + OperationRegistry.GetName(op) + ": " + (OperationRegistry.IsOrdered(op) ? "<b>ON</b>" : "off"));
        if (boss != null)
        {
            CoreTraitProgression.EnsureRubricsInitialized(boss);
            sb.AppendLine();
            sb.AppendLine("<b>Boss — core traits</b>");
            foreach (CoreTrait t in System.Enum.GetValues(typeof(CoreTrait)))
            {
                int level = CoreTraitProgression.GetLevel(boss, t);
                sb.AppendLine("• " + OperationRegistry.GetTraitName(t) + ": " + level + "/5");
            }
            sb.AppendLine();
            sb.AppendLine("<b>Boss — derived skills</b> <i>(weighted from traits)</i>");
            foreach (DerivedSkill ds in System.Enum.GetValues(typeof(DerivedSkill)))
            {
                int sl = DerivedSkillProgression.GetLevel(boss, ds);
                sb.AppendLine("• " + DerivedSkillProgression.GetDisplayName(ds) + ": " + sl + "/10");
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Same behavior as the legacy BottomBar End Turn button (<see cref="TurnManager.EndTurn"/>).
    /// Used by AoW "Next Turn" when <see cref="_aowHudOwnsTurnControls"/> skips building <see cref="_endTurnButton"/>.
    /// </summary>
    private static void InvokePlanningEndTurnFromUi()
    {
        TurnManager tm = Object.FindFirstObjectByType<TurnManager>();
        if (tm != null)
            tm.EndTurn();
        else if (PlanningFlowController.Instance != null)
            PlanningFlowController.Instance.TrySubmitOrTogglePlanningReady();
        else
            SceneManager.LoadScene("MainScene");
    }

    private void BuildEndTurnButton()
    {
        if (_enableAoWCharacterHud && _aowHudOwnsTurnControls)
            return;
        RectTransform bottom = GameObject.Find("BottomBar")?.GetComponent<RectTransform>();
        if (bottom == null)
            return;

        Transform oldEndTurn = bottom.Find("EndTurnButton");
        if (oldEndTurn != null)
            Object.Destroy(oldEndTurn.gameObject);

        GameObject endTurnGo = new GameObject("EndTurnButton");
        endTurnGo.transform.SetParent(bottom, false);

        RectTransform rt = endTurnGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = _endTurnOffset;
        rt.sizeDelta = _endTurnSize;

        Image img = endTurnGo.AddComponent<Image>();
        Button btn = endTurnGo.AddComponent<Button>();
        btn.targetGraphic = img;
        PlanningUiButtonStyle.ApplyStandardRectButton(btn, img);
        endTurnGo.AddComponent<ButtonPressScale>();
        _endTurnButton = btn;

        GameObject textGo = new GameObject("Label");
        textGo.transform.SetParent(endTurnGo.transform, false);
        RectTransform textRt = textGo.AddComponent<RectTransform>();
        StretchFull(textRt);
        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = "End Turn";
        tmp.fontSize = 16f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = PlanningUiButtonStyle.LabelPrimary;

        TurnManager tm = Object.FindFirstObjectByType<TurnManager>();
        if (tm != null)
            btn.onClick.AddListener(() => tm.EndTurn());
        else
            btn.onClick.AddListener(() => InvokePlanningEndTurnFromUi());
    }

    private void BuildDayLabel()
    {
        if (_enableAoWCharacterHud && _aowHudOwnsTurnControls)
            return;
        RectTransform bottom = GameObject.Find("BottomBar")?.GetComponent<RectTransform>();
        if (bottom == null)
            return;

        Transform oldDay = bottom.Find("DayLabel");
        if (oldDay != null)
            Object.Destroy(oldDay.gameObject);

        GameObject dayGo = new GameObject("DayLabel");
        dayGo.transform.SetParent(bottom, false);

        RectTransform rt = dayGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        float x = _endTurnOffset.x - _endTurnSize.x - _dayLabelGap;
        rt.anchoredPosition = new Vector2(x, _endTurnOffset.y);
        rt.sizeDelta = _dayLabelSize;

        TextMeshProUGUI tmp = dayGo.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = GameCalendarSystem.FormatPlanningHudLine(GameSessionState.CurrentDay);
        tmp.fontSize = 16f;
        tmp.alignment = TextAlignmentOptions.TopRight;
        tmp.color = new Color(0.95f, 0.95f, 0.92f);

        TurnManager tm = Object.FindFirstObjectByType<TurnManager>();
        if (tm != null)
            tm.dayLabel = tmp;
    }

    private static Button CreateBarButton(Transform parent, string label)
    {
        GameObject go = new GameObject("Btn_" + label.Replace(" ", ""));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(128f, 36f);

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 128f;
        le.preferredHeight = 36f;
        le.minWidth = 128f;
        le.minHeight = 36f;

        Image img = go.AddComponent<Image>();
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        PlanningUiButtonStyle.ApplyStandardRectButton(btn, img);
        go.AddComponent<ButtonPressScale>();

        GameObject textGo = new GameObject("Label");
        textGo.transform.SetParent(go.transform, false);
        RectTransform tr = textGo.AddComponent<RectTransform>();
        StretchFull(tr);
        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = label;
        tmp.fontSize = 16f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = PlanningUiButtonStyle.LabelPrimary;
        return btn;
    }

    /// <summary>Full-width row button for government shell left/right lists (wraps label, no 128px clip).</summary>
    private static Button CreateGovernmentShellRowButton(Transform parent, string label, float fontSize = 15f, float rowHeight = 52f)
    {
        string safeName = "Btn_Gov_" + Mathf.Abs(label.GetHashCode());
        GameObject go = new GameObject(safeName);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, rowHeight);

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 0f;
        le.flexibleWidth = 1f;
        le.minWidth = 0f;
        le.preferredHeight = rowHeight;
        le.minHeight = rowHeight;
        le.flexibleHeight = 0f;

        Image img = go.AddComponent<Image>();
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        PlanningUiButtonStyle.ApplyStandardRectButton(btn, img);
        go.AddComponent<ButtonPressScale>();

        GameObject textGo = new GameObject("Label");
        textGo.transform.SetParent(go.transform, false);
        RectTransform tr = textGo.AddComponent<RectTransform>();
        StretchFull(tr);
        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        tmp.richText = true;
        tmp.text = label;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.color = PlanningUiButtonStyle.LabelPrimary;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.margin = new Vector4(10f, 6f, 10f, 6f);
        return btn;
    }

    /// <summary>
    /// Unity UI Images with no sprite often fail to participate in graphic raycasts; assign a 1×1 white sprite.
    /// </summary>
    private static void EnsureRaycastHitSprite(Image img)
    {
        if (img == null || img.sprite != null)
            return;
        if (_cachedUiHitSprite == null)
        {
            Texture2D t = Texture2D.whiteTexture;
            _cachedUiHitSprite = Sprite.Create(t, new Rect(0f, 0f, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
        }

        img.sprite = _cachedUiHitSprite;
        img.type = Image.Type.Simple;
    }

    /// <summary>Crew tab: full-width row — <see cref="Button"/> + solid sprite Image (Input System UI works reliably with Button).</summary>
    private Image CreatePersonnelMemberRow(Transform parent, string richTextLabel, int memberIndex, float rowHeight = 56f)
    {
        GameObject go = new GameObject("PersonnelMemberRow_" + memberIndex);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, rowHeight);

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 0f;
        le.flexibleWidth = 1f;
        le.minWidth = 0f;
        le.preferredHeight = rowHeight;
        le.minHeight = rowHeight;
        le.flexibleHeight = 0f;

        Image img = go.AddComponent<Image>();
        EnsureRaycastHitSprite(img);
        img.raycastTarget = true;
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        PlanningUiButtonStyle.ApplyStandardRectButton(btn, img);
        PlanningUiButtonStyle.ApplyOutline(img);
        int captured = memberIndex;
        btn.onClick.AddListener(() => OnPersonnelMemberRowClicked(captured));

        GameObject textGo = new GameObject("Label");
        textGo.transform.SetParent(go.transform, false);
        RectTransform tr = textGo.AddComponent<RectTransform>();
        StretchFull(tr);
        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        tmp.richText = true;
        tmp.text = richTextLabel;
        tmp.fontSize = 14f;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.color = PlanningUiButtonStyle.LabelPrimary;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.margin = new Vector4(10f, 6f, 10f, 6f);
        tmp.raycastTarget = false;
        return img;
    }

    /// <summary>
    /// Rebinds shell references from <c>MainArea/PlanningShell/...</c> when cached Transforms were lost
    /// (duplicate controller, domain reload edge cases, or destroyed references).
    /// </summary>
    private void TryRecoverPlanningShellReferencesFromHierarchy()
    {
        Transform mainAreaTf = GameObject.Find("MainArea")?.transform;
        if (mainAreaTf == null)
            return;

        Transform shellTf = null;
        for (int i = 0; i < mainAreaTf.childCount; i++)
        {
            Transform ch = mainAreaTf.GetChild(i);
            if (ch != null && ch.name == "PlanningShell")
            {
                shellTf = ch;
                break;
            }
        }

        if (shellTf == null)
            return;

        Transform rowTf = shellTf.Find("ThreeColumnRow");
        if (rowTf != null && (_threeColumnRow == null || !_threeColumnRow))
            _threeColumnRow = rowTf.gameObject;

        Transform leftCol = rowTf != null ? rowTf.Find("LeftColumn") : null;
        if (leftCol == null)
        {
            foreach (Transform t in mainAreaTf.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != "LeftColumn" || t.parent == null || t.parent.name != "ThreeColumnRow")
                    continue;
                leftCol = t;
                break;
            }
        }

        if (leftCol != null && (_leftColumnRoot == null || !_leftColumnRoot))
            _leftColumnRoot = leftCol;

        if (_leftColumnRoot != null && (_leftText == null || !_leftText))
        {
            Transform lp = _leftColumnRoot.Find("LeftPanel");
            if (lp != null)
                _leftText = lp.GetComponent<TextMeshProUGUI>();
        }

        if (_leftColumnRoot != null && (_personnelMemberListRoot == null || !_personnelMemberListRoot))
        {
            Transform pm = _leftColumnRoot.Find("PersonnelMemberList");
            if (pm != null)
                _personnelMemberListRoot = pm;
        }
    }

    /// <summary>
    /// Runtime safety: if the shell was built from an older scene/prefab without <see cref="_personnelMemberListRoot"/>,
    /// create the roster list under the left column so Crew tab always shows clickable members.
    /// </summary>
    private void EnsurePersonnelMemberListRootExists()
    {
        TryRecoverPlanningShellReferencesFromHierarchy();

        if (_leftColumnRoot == null || !_leftColumnRoot)
            return;

        EnsureLeftColumnRaycastPriorityCanvas();

        if (_personnelMemberListRoot != null && _personnelMemberListRoot)
            return;

        Transform existingPm = _leftColumnRoot.Find("PersonnelMemberList");
        if (existingPm != null)
        {
            _personnelMemberListRoot = existingPm;
            return;
        }

        GameObject personnelListGo = new GameObject("PersonnelMemberList");
        personnelListGo.transform.SetParent(_leftColumnRoot, false);
        if (_leftText != null)
            personnelListGo.transform.SetSiblingIndex(_leftText.transform.GetSiblingIndex() + 1);
        else
            personnelListGo.transform.SetAsFirstSibling();
        _personnelMemberListRoot = personnelListGo.transform;
        VerticalLayoutGroup pmV = personnelListGo.AddComponent<VerticalLayoutGroup>();
        pmV.spacing = 6f;
        pmV.padding = new RectOffset(0, 0, 0, 0);
        pmV.childAlignment = TextAnchor.UpperLeft;
        pmV.childControlHeight = true;
        pmV.childControlWidth = true;
        pmV.childForceExpandWidth = true;
        pmV.childForceExpandHeight = false;
        LayoutElement lePm = personnelListGo.AddComponent<LayoutElement>();
        lePm.flexibleWidth = 1f;
        lePm.flexibleHeight = 1f;
        lePm.minHeight = 120f;
        lePm.preferredHeight = 280f;

        TextMeshProUGUI pmHead = CreateTmp(personnelListGo.transform, "MembersTitle", 15f, FontStyles.Bold);
        pmHead.text = "Members";
        pmHead.alignment = TextAlignmentOptions.Left;
        pmHead.raycastTarget = false;
        LayoutElement lePmHead = pmHead.gameObject.AddComponent<LayoutElement>();
        lePmHead.preferredHeight = 26f;
        lePmHead.minHeight = 22f;
        lePmHead.flexibleHeight = 0f;

        personnelListGo.SetActive(false);
    }

    /// <summary>
    /// Left column must sort above the center ScrollRect viewport for raycasts (see <see cref="BuildScrollableCenterColumn"/>).
    /// Safe to call multiple times.
    /// </summary>
    private void EnsureLeftColumnRaycastPriorityCanvas()
    {
        if (_leftColumnRoot == null)
            return;
        GameObject go = _leftColumnRoot.gameObject;
        if (go.GetComponent<Canvas>() != null)
            return;
        Canvas c = go.AddComponent<Canvas>();
        c.overrideSorting = true;
        c.sortingOrder = 30;
        go.AddComponent<GraphicRaycaster>();
    }

    private void ClearPersonnelMemberList()
    {
        if (_personnelMemberListRoot == null)
            return;
        for (int i = _personnelMemberListRoot.childCount - 1; i >= 0; i--)
        {
            Transform c = _personnelMemberListRoot.GetChild(i);
            if (c != null && c.name.StartsWith("PersonnelMemberRow_", System.StringComparison.Ordinal))
                Destroy(c.gameObject);
        }

        _personnelMemberRowButtons.Clear();
        _personnelMemberRowImages.Clear();
    }

    private void RebuildPersonnelMemberList()
    {
        if (_personnelMemberListRoot == null)
            return;
        ClearPersonnelMemberList();
        List<CrewMember> members = PersonnelRegistry.Members;
        if (members == null)
            return;

        for (int i = 0; i < members.Count; i++)
        {
            CrewMember m = members[i];
            if (m == null)
                continue;
            string roleLine = string.IsNullOrWhiteSpace(m.Role) ? "" : m.Role;
            string label = "<b>" + m.Name + "</b>\n<size=12><color=#C8C2B8>" + roleLine + "</color></size>";
            Image img = CreatePersonnelMemberRow(_personnelMemberListRoot, label, i);
            Button rowBtn = img != null ? img.GetComponent<Button>() : null;
            if (rowBtn != null)
                _personnelMemberRowButtons.Add(rowBtn);
            _personnelMemberRowImages.Add(img);
        }

        _personnelMemberListRoot.SetAsLastSibling();
        RefreshPersonnelMemberSelectionVisuals();
    }

    private void OnPersonnelMemberRowClicked(int index)
    {
        if (PersonnelRegistry.Members == null || index < 0 || index >= PersonnelRegistry.Members.Count)
            return;
        _personnelSelectedMemberIndex = index;
        RefreshPersonnelMemberSelectionVisuals();
        RefreshPersonnelCenterPanel();
    }

    private void RefreshPersonnelMemberSelectionVisuals()
    {
        for (int i = 0; i < _personnelMemberRowImages.Count; i++)
        {
            Image img = _personnelMemberRowImages[i];
            if (img == null)
                continue;
            bool sel = i == _personnelSelectedMemberIndex;
            Color fill = sel ? PlanningUiButtonStyle.TabSelectedFill : PlanningUiButtonStyle.RectFill;
            img.color = fill;
            if (i < _personnelMemberRowButtons.Count && _personnelMemberRowButtons[i] != null)
            {
                Button b = _personnelMemberRowButtons[i];
                ColorBlock cb = b.colors;
                cb.normalColor = fill;
                b.colors = cb;
            }
        }
    }

    private void RefreshPersonnelCenterPanel()
    {
        if (_centerText == null)
            return;
        if (PersonnelRegistry.Members == null || PersonnelRegistry.Members.Count == 0)
        {
            _centerText.text = "<b>Profile</b>\n<i>No crew on file.</i>";
            return;
        }

        if (_personnelSelectedMemberIndex < 0 || _personnelSelectedMemberIndex >= PersonnelRegistry.Members.Count)
            _personnelSelectedMemberIndex = 0;

        _centerText.text = PersonnelRegistry.BuildProfileDetail(_personnelSelectedMemberIndex);
        if (_centerScrollRect != null)
            _centerScrollRect.verticalNormalizedPosition = 1f;
    }

    private void BuildInstitutionDock()
    {
        RectTransform bottomBar = GameObject.Find("BottomBar")?.GetComponent<RectTransform>();
        if (bottomBar == null)
        {
            Debug.LogWarning("PlanningShellController: BottomBar not found — institution dock skipped.");
            return;
        }

        Transform canvasTf = GameObject.Find("Canvas")?.transform;
        if (canvasTf != null)
        {
            Transform onCanvas = canvasTf.Find(InstitutionDockName);
            if (onCanvas != null)
                DestroyObjectSafe(onCanvas.gameObject);
        }

        Transform legacyOnBar = bottomBar.Find(InstitutionDockName);
        if (legacyOnBar != null)
            DestroyObjectSafe(legacyOnBar.gameObject);

        // Left→right matches the planning dock reference: federal / civic / revenue / police / justice / health / corrections.
        (string title, string stem, string glyph, Color tint, string body)[] entries =
        {
            ("Federal Bureau", "institution_shin_bet", "F", new Color(0.25f, 0.32f, 0.48f, 1f),
                "<b>Federal Bureau</b>\n\nInterstate task forces, RICO-adjacent files, wire rooms, and sealed exhibits. " +
                "Opens the federal overlay on warrants, informants, and anything that crosses state lines."),
            ("City Hall", "institution_city_hall", "M", new Color(0.38f, 0.48f, 0.52f, 1f),
                "<b>City Hall</b>\n\nPermits, inspectors, zoning variances, and who gets the next ribbon-cutting. " +
                "Use it to lean on departments or launder legitimacy through civic contracts."),
            ("Taxes", "institution_taxes", "T", new Color(0.35f, 0.48f, 0.38f, 1f),
                "<b>Taxes</b>\n\nFilings, audits, liens, and revenue officers who remember every discrepancy. " +
                "Where clean books meet uncomfortable questions about cash businesses."),
            ("Police", "institution_police", "P", new Color(0.32f, 0.42f, 0.58f, 1f),
                "<b>Police</b>\n\nPrecinct posture, investigations, warrants, and street heat. " +
                "Wire taps, stakeouts, and the occasional envelope — all logged somewhere."),
            ("Court", "institution_court", "D", new Color(0.48f, 0.40f, 0.32f, 1f),
                "<b>Court</b>\n\nDockets, judges, motions, continuances, and sentencing ranges. " +
                "The slow grind where your crew’s freedom gets a case number."),
            ("Hospital", "institution_hospital", "H", new Color(0.52f, 0.36f, 0.38f, 1f),
                "<b>Hospital</b>\n\nTrauma bays, discreet wards, and medical records that never ask enough questions. " +
                "Beds for gunshot crews and leverage over anyone on a gurney."),
            ("Prison", "institution_prison", "L", new Color(0.28f, 0.28f, 0.32f, 1f),
                "<b>Prison</b>\n\nIntake, classification, yard politics, and transfer windows. " +
                "Where family business meets barbed wire and rival blocks."),
        };

        // Stretch across BottomBar so the row can be centered horizontally and vertically in the strip.
        GameObject dockGo = new GameObject(InstitutionDockName, typeof(RectTransform));
        dockGo.transform.SetParent(bottomBar, false);
        dockGo.transform.SetAsFirstSibling();
        RectTransform dockRt = dockGo.GetComponent<RectTransform>();
        StretchFull(dockRt);
        dockRt.pivot = new Vector2(0.5f, 0.5f);
        dockRt.offsetMin = _institutionDockOffsetMin;
        dockRt.offsetMax = _institutionDockOffsetMax;
        dockGo.transform.localScale = _institutionDockLocalScale;

        float gapPx = Mathf.Max(InstitutionDockMinHorizontalGap, _institutionDockSpacing);

        HorizontalLayoutGroup h = dockGo.AddComponent<HorizontalLayoutGroup>();
        // Use spacer elements only (spacing + spacers would double the gap).
        h.spacing = 0f;
        h.padding = new RectOffset(
            _institutionDockPadLeft,
            _institutionDockPadRight,
            _institutionDockPadTop,
            _institutionDockPadBottom);
        h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlWidth = false;
        h.childControlHeight = false;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = false;

        for (int i = 0; i < entries.Length; i++)
        {
            if (i > 0)
                InsertInstitutionDockHorizontalGap(dockGo.transform, gapPx);

            var e = entries[i];
            string capturedTitle = e.title;
            string capturedBody = e.body;
            Sprite icon = GetInstitutionIconSprite(e.stem);
            Button instBtn = CreateRoundInstitutionButton(dockGo.transform, i, e.glyph, e.tint,
                () => ToggleInstitutionWindow(capturedTitle, capturedBody), icon, InstitutionButtonDiameter);
            if (capturedTitle == "Prison")
            {
                _prisonInstitutionButton = instBtn;
                if (_prisonInstitutionButton != null)
                {
                    _prisonInstitutionCircle = _prisonInstitutionButton.GetComponent<Image>();
                    _prisonInstitutionOutline = _prisonInstitutionButton.GetComponent<Outline>();
                    EnsurePrisonGlowVisual();
                }
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(dockRt);
    }

    private static void InsertInstitutionDockHorizontalGap(Transform parent, float width)
    {
        if (width < 0.5f)
            return;

        GameObject gap = new GameObject("DockHGap", typeof(RectTransform));
        gap.transform.SetParent(parent, false);
        RectTransform rt = gap.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, InstitutionButtonDiameter);

        LayoutElement le = gap.AddComponent<LayoutElement>();
        le.minWidth = width;
        le.preferredWidth = width;
        le.flexibleWidth = 0f;
        le.minHeight = InstitutionButtonDiameter;
        le.preferredHeight = InstitutionButtonDiameter;
        le.flexibleHeight = 0f;
    }

    /// <summary>Loads <c>Resources/{stem}</c> or <c>UI/Institutions/{stem}</c>; police falls back to procedural badge if missing.</summary>
    private static Sprite GetInstitutionIconSprite(string resourceStem)
    {
        if (_institutionIconCache.TryGetValue(resourceStem, out Sprite cached))
            return cached;

        Sprite fromFile = TryLoadInstitutionSpriteFromResources(resourceStem);
        if (fromFile != null)
        {
            _institutionIconCache[resourceStem] = fromFile;
            return fromFile;
        }

        if (resourceStem == "institution_police")
        {
            if (_fallbackPoliceInstitutionSprite != null)
            {
                _institutionIconCache[resourceStem] = _fallbackPoliceInstitutionSprite;
                return _fallbackPoliceInstitutionSprite;
            }

            _fallbackPoliceInstitutionSprite = BuildFallbackPoliceBadgeSprite();
            _institutionIconCache[resourceStem] = _fallbackPoliceInstitutionSprite;
            return _fallbackPoliceInstitutionSprite;
        }

        _institutionIconCache[resourceStem] = null;
        return null;
    }

    private static Sprite TryLoadInstitutionSpriteFromResources(string stem)
    {
        string[] paths = { stem, "UI/Institutions/" + stem };
        string preferredSliceName = stem + "_0";

        foreach (string path in paths)
        {
            Sprite[] slices = Resources.LoadAll<Sprite>(path);
            if (slices != null && slices.Length > 0)
            {
                Sprite best = PickBestInstitutionSlice(slices, preferredSliceName);
                if (best != null)
                    return best;
            }

            Sprite direct = Resources.Load<Sprite>(path);
            if (direct != null)
                return direct;
        }

        return null;
    }

    /// <summary>Prefer the usual Multi-mode slice name; else largest rect (avoids tiny junk slices).</summary>
    private static Sprite PickBestInstitutionSlice(Sprite[] slices, string preferredName)
    {
        if (slices == null || slices.Length == 0)
            return null;

        for (int i = 0; i < slices.Length; i++)
        {
            if (slices[i] != null && slices[i].name == preferredName)
                return slices[i];
        }

        Sprite best = null;
        float bestArea = 0f;
        for (int i = 0; i < slices.Length; i++)
        {
            if (slices[i] == null)
                continue;
            float a = RectArea(slices[i]);
            if (best == null || a > bestArea)
            {
                bestArea = a;
                best = slices[i];
            }
        }

        return best;
    }

    private static float RectArea(Sprite s)
    {
        Rect r = s.rect;
        return r.width * r.height;
    }

    private static Sprite BuildFallbackPoliceBadgeSprite()
    {
        // Large high-contrast center so the icon stays readable inside a ~18px UI rect (tiny old star vanished).
        const int n = 96;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        float cx = n * 0.5f - 0.5f;
        float cy = n * 0.5f - 0.5f;
        Color rim = new Color(0.16f, 0.26f, 0.4f, 1f);
        Color body = new Color(0.32f, 0.48f, 0.7f, 1f);
        Color bodyDeep = new Color(0.22f, 0.36f, 0.55f, 1f);
        Color starBlue = new Color(0.14f, 0.22f, 0.38f, 1f);

        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                if (d > 45f)
                    tex.SetPixel(x, y, Color.clear);
                else if (d > 41f)
                    tex.SetPixel(x, y, rim);
                else if (d > 30f)
                    tex.SetPixel(x, y, body);
                else if (d > 26f)
                    tex.SetPixel(x, y, bodyDeep);
                else
                    tex.SetPixel(x, y, Color.white);
            }
        }

        // Thick 8-ray burst on the white disk (cos(8θ) peaks on axes/diagonals — readable when scaled down).
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                if (d > 25f || d < 5f)
                    continue;

                float ang = Mathf.Atan2(y - cy, x - cx);
                if (Mathf.Abs(Mathf.Cos(8f * ang)) > 0.78f)
                    tex.SetPixel(x, y, starBlue);
            }
        }

        tex.Apply();
        tex.hideFlags = HideFlags.HideAndDontSave;
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }

    private Button CreateRoundInstitutionButton(Transform parent, int index, string glyph, Color tint, UnityEngine.Events.UnityAction onClick, Sprite iconOverlay = null, float diameter = 28f)
    {
        GameObject go = new GameObject("InstitutionBtn_" + index);
        go.transform.SetParent(parent, false);

        float d = diameter;
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredWidth = d;
        le.preferredHeight = d;
        le.minWidth = d;
        le.minHeight = d;

        Image circle = go.AddComponent<Image>();
        RectTransform rt = circle.rectTransform;
        // Centered cell in HorizontalLayoutGroup (avoids drifting low vs MiddleCenter).
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(d, d);
        rt.anchoredPosition = Vector2.zero;

        circle.sprite = GetPlanningCircleSprite();
        circle.type = Image.Type.Simple;
        circle.raycastTarget = true;
        circle.preserveAspect = true;

        Color circleNormal = iconOverlay != null
            ? PlanningUiButtonStyle.RectFill
            : Color.Lerp(tint, PlanningUiButtonStyle.RectFill, 0.25f);

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = circle;
        PlanningUiButtonStyle.ApplyStandardCircleButton(btn, circle, circleNormal);
        btn.onClick.AddListener(onClick);

        // Adds tactile feedback: scale down while pressing.
        go.AddComponent<ButtonPressScale>();

        if (iconOverlay != null)
        {
            GameObject iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(go.transform, false);
            RectTransform irt = iconGo.AddComponent<RectTransform>();
            irt.anchorMin = Vector2.zero;
            irt.anchorMax = Vector2.one;
            // Keep art inside the disk so neighbors do not look “fused” (negative insets bleed past layout width).
            irt.offsetMin = new Vector2(5f, 6f);
            irt.offsetMax = new Vector2(-5f, -4f);
            Image iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = iconOverlay;
            iconImg.preserveAspect = true;
            iconImg.color = Color.white;
            iconImg.raycastTarget = false;
        }

        GameObject glyphGo = new GameObject("Glyph");
        glyphGo.transform.SetParent(go.transform, false);
        RectTransform grt = glyphGo.AddComponent<RectTransform>();
        StretchFull(grt);
        TextMeshProUGUI gtmp = glyphGo.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            gtmp.font = TMP_Settings.defaultFontAsset;
        gtmp.text = iconOverlay != null ? string.Empty : glyph;
        gtmp.fontSize = Mathf.Clamp(Mathf.RoundToInt(d * 0.38f), 15, 26);
        gtmp.fontStyle = FontStyles.Bold;
        gtmp.alignment = TextAlignmentOptions.Center;
        gtmp.color = PlanningUiButtonStyle.LabelPrimary;
        gtmp.raycastTarget = false;
        return btn;
    }

    private static Sprite GetPlanningCircleSprite()
    {
        if (_planningCircleSprite != null)
            return _planningCircleSprite;

        const int n = 64;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        float cx = n * 0.5f - 0.5f;
        float cy = n * 0.5f - 0.5f;
        float r = n * 0.45f;
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                float a = d < r - 1.5f ? 1f : d > r + 0.5f ? 0f : Mathf.Clamp01((r - d) / 2f + 0.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        tex.Apply();
        tex.hideFlags = HideFlags.HideAndDontSave;
        _planningCircleSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        return _planningCircleSprite;
    }

    /// <summary>Round AoW action chips: wood-toned disk (matches procedural BottomBar palette) instead of grey fill + bullet glyph.</summary>
    private static Sprite GetAoWRadialWoodDiskSprite()
    {
        if (_cachedAoWRadialWoodDiskSprite != null)
            return _cachedAoWRadialWoodDiskSprite;

        const int n = 112;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Trilinear;
        float cx = (n - 1) * 0.5f;
        float cy = (n - 1) * 0.5f;
        float r0 = n * 0.46f;
        const float seed = 5.31f;
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01((r0 + 1.25f - dist) / 2.5f);
                if (a < 0.02f)
                {
                    tex.SetPixel(x, y, new Color(0f, 0f, 0f, 0f));
                    continue;
                }

                float t = Mathf.Clamp01(dist / Mathf.Max(0.001f, r0));
                float n1 = Mathf.PerlinNoise(x * 0.08f + seed, y * 0.08f + seed * 0.2f);
                float n2 = Mathf.PerlinNoise(x * 0.22f, y * 0.18f + seed);
                float grain = (n2 - 0.5f) * 0.18f;
                Color dark = new Color(0.20f, 0.12f, 0.07f, 1f);
                Color light = new Color(0.55f, 0.36f, 0.19f, 1f);
                Color c = Color.Lerp(dark, light, Mathf.Clamp01(n1 + grain));
                float lit = Mathf.Clamp01((-dx - dy * 0.35f) / r0 * 0.35f + 0.84f);
                c *= lit;
                float rim = Mathf.SmoothStep(0f, 1f, (t - 0.58f) / 0.38f);
                Color gold = new Color(0.72f, 0.58f, 0.26f, 1f);
                c = Color.Lerp(c, Color.Lerp(c, gold, 0.72f), rim * 0.78f);
                c.a = a;
                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        tex.hideFlags = HideFlags.HideAndDontSave;
        _cachedAoWRadialWoodDiskSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        _cachedAoWRadialWoodDiskSprite.name = "AoWRadialWoodDisk";
        return _cachedAoWRadialWoodDiskSprite;
    }

    /// <summary>Solid quad for runtime UI Images — some Unity versions draw null-sprite fills unreliably under Mask/stencil.</summary>
    private static Sprite GetPlanningUiWhiteSprite()
    {
        if (_planningUiWhiteSprite != null)
            return _planningUiWhiteSprite;

        const int n = 4;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
                tex.SetPixel(x, y, Color.white);
        }

        tex.Apply();
        tex.hideFlags = HideFlags.HideAndDontSave;
        _planningUiWhiteSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        _planningUiWhiteSprite.name = "PlanningUiWhiteQuad";
        return _planningUiWhiteSprite;
    }

    private void BuildOpsCrewAssignModal()
    {
        Canvas canvas = GameObject.Find("Canvas")?.GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("PlanningShellController: Canvas not found — crew assign modal skipped.");
            return;
        }

        Transform old = canvas.transform.Find("OpsCrewAssignModalRoot");
        if (old != null)
            Destroy(old.gameObject);

        GameObject root = new GameObject("OpsCrewAssignModalRoot");
        root.transform.SetParent(canvas.transform, false);
        RectTransform rootRt = root.AddComponent<RectTransform>();
        StretchFull(rootRt);
        root.SetActive(false);
        _opsCrewPickModalRoot = root;

        Image dim = root.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.52f);
        dim.raycastTarget = true;
        dim.sprite = GetPlanningUiWhiteSprite();
        Button dimBtn = root.AddComponent<Button>();
        dimBtn.targetGraphic = dim;
        dimBtn.transition = Selectable.Transition.None;
        dimBtn.onClick.AddListener(CloseOpsCrewAssignModal);

        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(root.transform, false);
        RectTransform prt = panel.AddComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(720f, 520f);

        Image pimg = panel.AddComponent<Image>();
        pimg.sprite = GetPlanningUiWhiteSprite();
        pimg.color = new Color(0.12f, 0.12f, 0.14f, 0.99f);
        pimg.raycastTarget = true;
        Outline po = panel.AddComponent<Outline>();
        po.effectColor = new Color(0.5f, 0.52f, 0.6f, 0.85f);
        po.effectDistance = new Vector2(2f, -2f);

        GameObject titleGo = new GameObject("Title");
        titleGo.transform.SetParent(panel.transform, false);
        RectTransform tRt = titleGo.AddComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0f, 1f);
        tRt.anchorMax = new Vector2(1f, 1f);
        tRt.pivot = new Vector2(0.5f, 1f);
        tRt.anchoredPosition = new Vector2(0f, -10f);
        tRt.sizeDelta = new Vector2(-28f, 38f);
        _opsCrewPickTitle = titleGo.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            _opsCrewPickTitle.font = TMP_Settings.defaultFontAsset;
        _opsCrewPickTitle.fontSize = 21f;
        _opsCrewPickTitle.fontStyle = FontStyles.Bold;
        _opsCrewPickTitle.alignment = TextAlignmentOptions.Center;
        _opsCrewPickTitle.color = new Color(0.94f, 0.92f, 0.88f, 1f);
        _opsCrewPickTitle.text = "Assign crew";
        _opsCrewPickTitle.raycastTarget = true;
        RegisterDraggableModalTitle(titleGo, prt);

        GameObject workArea = new GameObject("WorkArea", typeof(RectTransform));
        workArea.transform.SetParent(panel.transform, false);
        RectTransform waRt = workArea.GetComponent<RectTransform>();
        waRt.anchorMin = new Vector2(0f, 0f);
        waRt.anchorMax = new Vector2(1f, 1f);
        waRt.offsetMin = new Vector2(12f, 58f);
        waRt.offsetMax = new Vector2(-12f, -50f);
        HorizontalLayoutGroup hWork = workArea.AddComponent<HorizontalLayoutGroup>();
        hWork.spacing = 12f;
        hWork.padding = new RectOffset(4, 4, 2, 4);
        hWork.childAlignment = TextAnchor.UpperLeft;
        hWork.childControlWidth = true;
        hWork.childControlHeight = true;
        hWork.childForceExpandWidth = false;
        hWork.childForceExpandHeight = true;

        GameObject leftCol = new GameObject("LeftColumn", typeof(RectTransform));
        leftCol.transform.SetParent(workArea.transform, false);
        StretchFull(leftCol.GetComponent<RectTransform>());
        LayoutElement leLeft = leftCol.AddComponent<LayoutElement>();
        leLeft.minWidth = 220f;
        leLeft.preferredWidth = 248f;
        leLeft.flexibleWidth = 0f;
        leLeft.flexibleHeight = 1f;

        GameObject scrollGo = new GameObject("MemberScroll", typeof(RectTransform));
        scrollGo.transform.SetParent(leftCol.transform, false);
        RectTransform sRt = scrollGo.GetComponent<RectTransform>();
        StretchFull(sRt);
        ScrollRect sR = scrollGo.AddComponent<ScrollRect>();
        sR.horizontal = false;
        sR.vertical = true;
        sR.movementType = ScrollRect.MovementType.Clamped;
        sR.scrollSensitivity = 22f;

        GameObject vp = new GameObject("Viewport", typeof(RectTransform));
        vp.transform.SetParent(scrollGo.transform, false);
        RectTransform vRt = vp.GetComponent<RectTransform>();
        StretchFull(vRt);
        Image vImg = vp.AddComponent<Image>();
        vImg.color = new Color(0.06f, 0.06f, 0.07f, 0.5f);
        vImg.raycastTarget = true;
        vImg.sprite = GetPlanningUiWhiteSprite();
        vp.AddComponent<Mask>().showMaskGraphic = false;
        sR.viewport = vRt;

        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(vp.transform, false);
        RectTransform cRt = content.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0f, 1f);
        cRt.anchorMax = new Vector2(1f, 1f);
        cRt.pivot = new Vector2(0.5f, 1f);
        cRt.anchoredPosition = Vector2.zero;
        cRt.offsetMin = new Vector2(4f, 0f);
        cRt.offsetMax = new Vector2(-4f, 0f);
        VerticalLayoutGroup vLay = content.AddComponent<VerticalLayoutGroup>();
        vLay.spacing = 6f;
        vLay.padding = new RectOffset(2, 2, 4, 8);
        vLay.childAlignment = TextAnchor.UpperLeft;
        vLay.childControlWidth = true;
        vLay.childControlHeight = true;
        vLay.childForceExpandWidth = true;
        vLay.childForceExpandHeight = false;
        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        sR.content = cRt;
        _opsCrewPickListContent = cRt;

        GameObject rightCol = new GameObject("RightColumn", typeof(RectTransform));
        rightCol.transform.SetParent(workArea.transform, false);
        StretchFull(rightCol.GetComponent<RectTransform>());
        LayoutElement leRight = rightCol.AddComponent<LayoutElement>();
        leRight.minWidth = 300f;
        leRight.flexibleWidth = 1f;
        leRight.flexibleHeight = 1f;
        VerticalLayoutGroup vRight = rightCol.AddComponent<VerticalLayoutGroup>();
        vRight.spacing = 8f;
        vRight.padding = new RectOffset(2, 2, 0, 4);
        vRight.childAlignment = TextAnchor.UpperLeft;
        vRight.childControlWidth = true;
        vRight.childControlHeight = true;
        vRight.childForceExpandWidth = true;
        vRight.childForceExpandHeight = false;

        GameObject detailHost = new GameObject("DetailHost", typeof(RectTransform));
        detailHost.transform.SetParent(rightCol.transform, false);
        LayoutElement leDet = detailHost.AddComponent<LayoutElement>();
        leDet.minHeight = 100f;
        leDet.preferredHeight = 140f;
        leDet.flexibleHeight = 0f;
        leDet.flexibleWidth = 1f;
        _opsCrewPickDetailText = detailHost.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            _opsCrewPickDetailText.font = TMP_Settings.defaultFontAsset;
        _opsCrewPickDetailText.fontSize = 13f;
        _opsCrewPickDetailText.alignment = TextAlignmentOptions.TopLeft;
        _opsCrewPickDetailText.color = new Color(0.86f, 0.84f, 0.78f, 1f);
        _opsCrewPickDetailText.textWrappingMode = TextWrappingModes.Normal;
        _opsCrewPickDetailText.richText = true;
        StretchFull(detailHost.GetComponent<RectTransform>());
        detailHost.GetComponent<RectTransform>().offsetMin = new Vector2(2f, 2f);
        detailHost.GetComponent<RectTransform>().offsetMax = new Vector2(-2f, -2f);

        _opsCrewPickVehicleBtn = CreateGovernmentShellRowButton(rightCol.transform, "Mission vehicle: ON", 13.5f, 36f);
        _opsCrewPickVehicleBtnLabel = _opsCrewPickVehicleBtn.GetComponentInChildren<TextMeshProUGUI>();
        _opsCrewPickVehicleBtn.onClick.RemoveAllListeners();
        _opsCrewPickVehicleBtn.onClick.AddListener(() =>
        {
            _opsCrewPickMissionVehicle = !_opsCrewPickMissionVehicle;
            OpsCrewPickRefreshVehicleButtonLabel();
            OpsCrewPickRefreshDetailSummary();
        });

        GameObject exScrollGo = new GameObject("ExtrasScroll", typeof(RectTransform));
        exScrollGo.transform.SetParent(rightCol.transform, false);
        StretchFull(exScrollGo.GetComponent<RectTransform>());
        LayoutElement leEx = exScrollGo.AddComponent<LayoutElement>();
        leEx.minHeight = 72f;
        leEx.preferredHeight = 120f;
        leEx.flexibleWidth = 1f;
        leEx.flexibleHeight = 1f;
        ScrollRect exSr = exScrollGo.AddComponent<ScrollRect>();
        exSr.horizontal = false;
        exSr.vertical = true;
        GameObject exVp = new GameObject("Viewport", typeof(RectTransform));
        exVp.transform.SetParent(exScrollGo.transform, false);
        RectTransform exVrt = exVp.GetComponent<RectTransform>();
        StretchFull(exVrt);
        Image exVimg = exVp.AddComponent<Image>();
        exVimg.color = new Color(0.07f, 0.07f, 0.08f, 0.55f);
        exVimg.sprite = GetPlanningUiWhiteSprite();
        exVp.AddComponent<Mask>().showMaskGraphic = false;
        exSr.viewport = exVrt;
        GameObject exContent = new GameObject("ExtrasContent", typeof(RectTransform));
        exContent.transform.SetParent(exVp.transform, false);
        RectTransform exCrt = exContent.GetComponent<RectTransform>();
        exCrt.anchorMin = new Vector2(0f, 1f);
        exCrt.anchorMax = new Vector2(1f, 1f);
        exCrt.pivot = new Vector2(0.5f, 1f);
        exCrt.offsetMin = new Vector2(2f, 0f);
        exCrt.offsetMax = new Vector2(-2f, 0f);
        VerticalLayoutGroup exVlay = exContent.AddComponent<VerticalLayoutGroup>();
        exVlay.spacing = 4f;
        exVlay.childAlignment = TextAnchor.UpperLeft;
        exVlay.childControlWidth = true;
        exVlay.childControlHeight = true;
        exVlay.childForceExpandWidth = true;
        exVlay.childForceExpandHeight = false;
        ContentSizeFitter exCsf = exContent.AddComponent<ContentSizeFitter>();
        exCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        exSr.content = exCrt;
        _opsCrewPickExtrasContent = exCrt;

        GameObject loRow = new GameObject("LookoutRow", typeof(RectTransform));
        loRow.transform.SetParent(rightCol.transform, false);
        LayoutElement leLo = loRow.AddComponent<LayoutElement>();
        leLo.preferredHeight = 34f;
        leLo.minHeight = 32f;
        leLo.flexibleWidth = 1f;
        Image loImg = loRow.AddComponent<Image>();
        loImg.sprite = GetPlanningUiWhiteSprite();
        loImg.color = new Color(0.18f, 0.18f, 0.2f, 0.98f);
        _opsCrewPickLookoutBtn = loRow.AddComponent<Button>();
        _opsCrewPickLookoutBtn.targetGraphic = loImg;
        PlanningUiButtonStyle.ApplyColorTint(_opsCrewPickLookoutBtn, loImg.color);
        GameObject loLbl = new GameObject("Label");
        loLbl.transform.SetParent(loRow.transform, false);
        RectTransform loLrt = loLbl.AddComponent<RectTransform>();
        StretchFull(loLrt);
        loLrt.offsetMin = new Vector2(8f, 4f);
        loLrt.offsetMax = new Vector2(-8f, -4f);
        _opsCrewPickLookoutBtnLabel = loLbl.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            _opsCrewPickLookoutBtnLabel.font = TMP_Settings.defaultFontAsset;
        _opsCrewPickLookoutBtnLabel.fontSize = 13f;
        _opsCrewPickLookoutBtnLabel.alignment = TextAlignmentOptions.MidlineLeft;
        _opsCrewPickLookoutBtnLabel.color = PlanningUiButtonStyle.LabelPrimary;
        _opsCrewPickLookoutBtn.onClick.RemoveAllListeners();
        _opsCrewPickLookoutBtn.onClick.AddListener(OpsCrewPickCycleLookout);

        GameObject drRow = new GameObject("DriverRow", typeof(RectTransform));
        drRow.transform.SetParent(rightCol.transform, false);
        LayoutElement leDr = drRow.AddComponent<LayoutElement>();
        leDr.preferredHeight = 34f;
        leDr.minHeight = 32f;
        leDr.flexibleWidth = 1f;
        Image drImg = drRow.AddComponent<Image>();
        drImg.sprite = GetPlanningUiWhiteSprite();
        drImg.color = new Color(0.18f, 0.18f, 0.2f, 0.98f);
        _opsCrewPickDriverBtn = drRow.AddComponent<Button>();
        _opsCrewPickDriverBtn.targetGraphic = drImg;
        PlanningUiButtonStyle.ApplyColorTint(_opsCrewPickDriverBtn, drImg.color);
        GameObject drLbl = new GameObject("Label");
        drLbl.transform.SetParent(drRow.transform, false);
        RectTransform drLrt = drLbl.AddComponent<RectTransform>();
        StretchFull(drLrt);
        drLrt.offsetMin = new Vector2(8f, 4f);
        drLrt.offsetMax = new Vector2(-8f, -4f);
        _opsCrewPickDriverBtnLabel = drLbl.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            _opsCrewPickDriverBtnLabel.font = TMP_Settings.defaultFontAsset;
        _opsCrewPickDriverBtnLabel.fontSize = 13f;
        _opsCrewPickDriverBtnLabel.alignment = TextAlignmentOptions.MidlineLeft;
        _opsCrewPickDriverBtnLabel.color = PlanningUiButtonStyle.LabelPrimary;
        _opsCrewPickDriverBtn.onClick.RemoveAllListeners();
        _opsCrewPickDriverBtn.onClick.AddListener(OpsCrewPickCycleDriver);

        GameObject footer = new GameObject("Footer", typeof(RectTransform));
        footer.transform.SetParent(panel.transform, false);
        RectTransform fRt = footer.GetComponent<RectTransform>();
        fRt.anchorMin = new Vector2(0f, 0f);
        fRt.anchorMax = new Vector2(1f, 0f);
        fRt.pivot = new Vector2(0.5f, 0f);
        fRt.anchoredPosition = new Vector2(0f, 12f);
        fRt.sizeDelta = new Vector2(-28f, 48f);
        HorizontalLayoutGroup hFoot = footer.AddComponent<HorizontalLayoutGroup>();
        hFoot.spacing = 10f;
        hFoot.padding = new RectOffset(4, 4, 4, 4);
        hFoot.childAlignment = TextAnchor.MiddleCenter;
        hFoot.childControlHeight = true;
        hFoot.childControlWidth = false;
        hFoot.childForceExpandHeight = true;
        hFoot.childForceExpandWidth = false;

        _opsCrewPickRemoveBtn = CreateBarButton(footer.transform, "Remove from queue");
        _opsCrewPickRemoveBtn.onClick.RemoveAllListeners();
        _opsCrewPickRemoveBtn.onClick.AddListener(OpsCrewPickRemoveFromQueue);

        Button cancelBtn = CreateBarButton(footer.transform, "Cancel");
        cancelBtn.onClick.RemoveAllListeners();
        cancelBtn.onClick.AddListener(CloseOpsCrewAssignModal);

        _opsCrewPickConfirmBtn = CreateBarButton(footer.transform, "Confirm");
        _opsCrewPickConfirmBtn.onClick.RemoveAllListeners();
        _opsCrewPickConfirmBtn.onClick.AddListener(OpsCrewPickConfirm);
        _opsCrewPickConfirmLabel = _opsCrewPickConfirmBtn.GetComponentInChildren<TextMeshProUGUI>();
    }

    private void OpenOpsCrewAssignModal(OperationType op)
    {
        if (_opsCrewPickModalRoot == null)
            return;
        OpsPlanningRhythmState.EnsureCalendarDay(GameSessionState.CurrentDay);
        _opsCrewPickOperation = op;
        _opsCrewPickSelectedIndex = GameSessionState.GetOperationAssignee(op);
        if (_opsCrewPickSelectedIndex < 0 || _opsCrewPickSelectedIndex >= PersonnelRegistry.Members.Count)
            _opsCrewPickSelectedIndex = -1;

        OpsCrewPickLoadSessionStateForOperation(op);

        if (_opsCrewPickTitle != null)
            _opsCrewPickTitle.text = "Mission — " + OperationRegistry.GetName(op);

        OpsCrewPickRefreshVehicleButtonLabel();
        OpsCrewPickRebuildMemberRows();
        OpsCrewPickRefreshFooter();

        _opsCrewPickModalRoot.SetActive(true);
        _opsCrewPickModalRoot.transform.SetAsLastSibling();
    }

    private void CloseOpsCrewAssignModal()
    {
        if (_opsCrewPickModalRoot != null)
            _opsCrewPickModalRoot.SetActive(false);
    }

    private void OpsCrewPickLoadSessionStateForOperation(OperationType op)
    {
        int n = PersonnelRegistry.Members != null ? PersonnelRegistry.Members.Count : 0;
        _opsCrewPickExtraOn = n > 0 ? new bool[n] : System.Array.Empty<bool>();
        int lead = GameSessionState.GetOperationAssignee(op);
        _opsCrewPickMissionVehicle = GameSessionState.OperationMissionUsesCrewVehicle.TryGetValue(op, out bool mv)
            ? mv
            : GameSessionState.ResolveCrewOpsVehicle();
        _opsCrewPickLookoutIdx = GameSessionState.OperationLookoutMemberIndex.TryGetValue(op, out int lo) ? lo : -1;
        _opsCrewPickDriverIdx = GameSessionState.OperationDriverMemberIndex.TryGetValue(op, out int dr) ? dr : -1;

        if (lead < 0 || lead >= n)
        {
            _opsCrewPickLookoutIdx = -1;
            _opsCrewPickDriverIdx = -1;
            return;
        }

        if (lead >= 0 && GameSessionState.OperationExtraMemberIndices.TryGetValue(op, out var extras) && extras != null)
        {
            for (int i = 0; i < extras.Count; i++)
            {
                int xi = extras[i];
                if (xi >= 0 && xi < _opsCrewPickExtraOn.Length && xi != lead)
                    _opsCrewPickExtraOn[xi] = true;
            }
        }

        OpsCrewPickClampRolesToSquad();
    }

    private void OpsCrewPickClampRolesToSquad()
    {
        System.Collections.Generic.List<int> squad = OpsCrewPickBuildSquadList();
        bool Contains(int idx)
        {
            for (int i = 0; i < squad.Count; i++)
            {
                if (squad[i] == idx)
                    return true;
            }

            return false;
        }

        if (_opsCrewPickLookoutIdx >= 0 && !Contains(_opsCrewPickLookoutIdx))
            _opsCrewPickLookoutIdx = -1;
        if (_opsCrewPickDriverIdx >= 0 && !Contains(_opsCrewPickDriverIdx))
            _opsCrewPickDriverIdx = -1;
    }

    private System.Collections.Generic.List<int> OpsCrewPickBuildSquadList()
    {
        var list = new System.Collections.Generic.List<int>(8);
        if (_opsCrewPickSelectedIndex < 0)
            return list;
        list.Add(_opsCrewPickSelectedIndex);
        for (int i = 0; i < _opsCrewPickExtraOn.Length; i++)
        {
            if (! _opsCrewPickExtraOn[i])
                continue;
            if (i == _opsCrewPickSelectedIndex)
                continue;
            list.Add(i);
        }

        return list;
    }

    private int OpsCrewPickCountSquadMembers()
    {
        return OpsCrewPickBuildSquadList().Count;
    }

    private void OpsCrewPickRefreshVehicleButtonLabel()
    {
        if (_opsCrewPickVehicleBtnLabel != null)
            _opsCrewPickVehicleBtnLabel.text = _opsCrewPickMissionVehicle ? "Mission vehicle: ON" : "Mission vehicle: OFF";
    }

    private void OpsCrewPickRefreshRoleButtonLabels()
    {
        if (_opsCrewPickLookoutBtnLabel != null)
        {
            string name = "— none —";
            if (_opsCrewPickLookoutIdx >= 0 && _opsCrewPickLookoutIdx < PersonnelRegistry.Members.Count &&
                PersonnelRegistry.Members[_opsCrewPickLookoutIdx] != null)
                name = PersonnelRegistry.Members[_opsCrewPickLookoutIdx].Name;
            _opsCrewPickLookoutBtnLabel.text = "Lookout (police / witnesses / runner): " + name;
        }

        if (_opsCrewPickDriverBtnLabel != null)
        {
            string dname = "— none — (everyone dismounts)";
            if (_opsCrewPickDriverIdx >= 0 && _opsCrewPickDriverIdx < PersonnelRegistry.Members.Count &&
                PersonnelRegistry.Members[_opsCrewPickDriverIdx] != null)
                dname = PersonnelRegistry.Members[_opsCrewPickDriverIdx].Name + " (stays in vehicle)";
            _opsCrewPickDriverBtnLabel.text = "Driver: " + dname;
        }
    }

    private void OpsCrewPickCycleLookout()
    {
        var opts = new System.Collections.Generic.List<int> { -1 };
        var squad = OpsCrewPickBuildSquadList();
        for (int i = 0; i < squad.Count; i++)
            opts.Add(squad[i]);
        int cur = opts.IndexOf(_opsCrewPickLookoutIdx);
        if (cur < 0)
            cur = 0;
        int next = (cur + 1) % opts.Count;
        _opsCrewPickLookoutIdx = opts[next];
        if (_opsCrewPickDriverIdx == _opsCrewPickLookoutIdx && _opsCrewPickLookoutIdx >= 0)
            _opsCrewPickDriverIdx = -1;
        OpsCrewPickRefreshRoleButtonLabels();
        OpsCrewPickRefreshDetailSummary();
    }

    private void OpsCrewPickCycleDriver()
    {
        var opts = new System.Collections.Generic.List<int> { -1 };
        var squad = OpsCrewPickBuildSquadList();
        for (int i = 0; i < squad.Count; i++)
            opts.Add(squad[i]);
        int cur = opts.IndexOf(_opsCrewPickDriverIdx);
        if (cur < 0)
            cur = 0;
        int next = (cur + 1) % opts.Count;
        _opsCrewPickDriverIdx = opts[next];
        if (_opsCrewPickDriverIdx == _opsCrewPickLookoutIdx && _opsCrewPickDriverIdx >= 0)
            _opsCrewPickLookoutIdx = -1;
        OpsCrewPickRefreshRoleButtonLabels();
        OpsCrewPickRefreshDetailSummary();
    }

    private void OpsCrewPickRebuildExtraToggles()
    {
        if (_opsCrewPickExtrasContent == null)
            return;
        for (int i = _opsCrewPickExtrasContent.childCount - 1; i >= 0; i--)
            Destroy(_opsCrewPickExtrasContent.GetChild(i).gameObject);

        int n = PersonnelRegistry.Members.Count;
        if (_opsCrewPickExtraOn.Length != n)
        {
            var na = new bool[n];
            for (int i = 0; i < Mathf.Min(n, _opsCrewPickExtraOn.Length); i++)
                na[i] = _opsCrewPickExtraOn[i];
            _opsCrewPickExtraOn = na;
        }

        for (int i = 0; i < n; i++)
        {
            if (i == _opsCrewPickSelectedIndex)
                continue;
            CrewMember m = PersonnelRegistry.Members[i];
            int idx = i;
            bool locked = m != null && CharacterStatusUtility.IsIncarcerated(m.GetResolvedStatus());
            string nm = m != null ? m.Name : "?";
            string lab = "<b>" + nm + "</b>  " + (_opsCrewPickExtraOn[idx] ? "<color=#7fcf8f>with mission</color>" : "<color=#888888>not assigned</color>");
            Button row = CreateGovernmentShellRowButton(_opsCrewPickExtrasContent, lab, 12.5f, 30f);
            row.interactable = !locked;
            row.onClick.RemoveAllListeners();
            row.onClick.AddListener(() =>
            {
                if (locked)
                    return;
                _opsCrewPickExtraOn[idx] = !_opsCrewPickExtraOn[idx];
                OpsCrewPickClampRolesToSquad();
                OpsCrewPickRebuildExtraToggles();
                OpsCrewPickRefreshRoleButtonLabels();
                OpsCrewPickRefreshDetailSummary();
            });
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_opsCrewPickExtrasContent);
    }

    private bool OpsCrewPickTryComputeMissionHours(out float travelH, out float execH, out float totalH)
    {
        travelH = execH = totalH = 0f;
        if (_opsCrewPickSelectedIndex < 0)
            return false;
        GameSessionState.EnsureActiveCityData();
        CityData city = GameSessionState.ActiveCityData;
        if (city == null)
            return false;

        MicroBlockSpotRuntime spot = FindOpsSpotByStableId(_opsSelectedSpotStableId);
        int home = MicroBlockWorldState.CrewHomeBlockId;
        int targetBlock = spot != null
            ? OperationTimingSystem.ResolveTargetBlockIdForSpot(city, spot, home)
            : (_opsCenterMapSelectedBlockId >= 0 ? _opsCenterMapSelectedBlockId : home);

        int squad = Mathf.Max(1, OpsCrewPickCountSquadMembers());
        GameSeason season = GameCalendarSystem.GetSeason(GameSessionState.CurrentDay);
        WeatherSnapshot wx = GameWeatherResolver.Resolve(GameSessionState.CurrentDay);
        OperationTimingSystem.EstimateMissionHours(city, home, targetBlock, _opsCrewPickOperation, squad, _opsCrewPickMissionVehicle,
            season, in wx, out travelH, out execH);
        totalH = travelH + execH;
        return true;
    }

    private void OpsCrewPickRefreshDetailSummary()
    {
        if (_opsCrewPickDetailText == null)
            return;
        OpsPlanningRhythmState.EnsureCalendarDay(GameSessionState.CurrentDay);
        var sb = new System.Text.StringBuilder(900);
        sb.AppendLine("<b>Lead</b> — left column. Everyone on this mission is under their command for this op.");
        sb.AppendLine();

        if (_opsCrewPickSelectedIndex < 0)
        {
            sb.AppendLine("<i>Pick a lead from the list.</i>");
            _opsCrewPickDetailText.text = sb.ToString();
            return;
        }

        CrewMember lead = PersonnelRegistry.Members[_opsCrewPickSelectedIndex];
        if (lead != null)
            sb.AppendLine(OpsPlanningRhythmState.BuildWeeklyRestStubLine(lead));

        if (!OpsCrewPickTryComputeMissionHours(out float travelH, out float execH, out float totalH))
        {
            sb.AppendLine("<i>Could not estimate hours (no city?).</i>");
            _opsCrewPickDetailText.text = sb.ToString();
            return;
        }

        sb.AppendLine("<b>Time</b> — travel ~" + travelH.ToString("0.#") + " h · on-site work ~" + execH.ToString("0.#") +
                      " h · <b>total ~" + totalH.ToString("0.#") + " h</b>");
        sb.AppendLine("<size=92%>Squad size " + OpsCrewPickCountSquadMembers() + " · vehicle " +
                      (_opsCrewPickMissionVehicle ? "on" : "off") + "</size>");
        sb.AppendLine();

        float committed = OpsPlanningRhythmState.GetCommittedHoursToday(_opsCrewPickSelectedIndex);
        sb.AppendLine(OpsPlanningRhythmState.BuildFatigueAdvisory(committed, totalH));

        _opsCrewPickDetailText.text = sb.ToString();
    }

    private void OpsCrewPickRebuildMemberRows()
    {
        if (_opsCrewPickListContent == null)
            return;
        _opsCrewPickRowBackgrounds.Clear();
        for (int i = _opsCrewPickListContent.childCount - 1; i >= 0; i--)
            Destroy(_opsCrewPickListContent.GetChild(i).gameObject);

        for (int i = 0; i < PersonnelRegistry.Members.Count; i++)
        {
            CrewMember m = PersonnelRegistry.Members[i];
            int idx = i;
            GameObject row = new GameObject("MemberRow_" + i, typeof(RectTransform));
            row.transform.SetParent(_opsCrewPickListContent, false);
            LayoutElement leRow = row.AddComponent<LayoutElement>();
            leRow.minHeight = 44f;
            leRow.preferredHeight = 46f;
            leRow.flexibleWidth = 1f;

            Image bg = row.AddComponent<Image>();
            bg.sprite = GetPlanningUiWhiteSprite();
            bg.color = new Color(0.16f, 0.16f, 0.18f, 0.95f);
            bg.raycastTarget = true;
            _opsCrewPickRowBackgrounds.Add(bg);

            Button btn = row.AddComponent<Button>();
            btn.targetGraphic = bg;
            PlanningUiButtonStyle.ApplyColorTint(btn, bg.color);
            row.AddComponent<ButtonPressScale>();

            bool locked = m != null && CharacterStatusUtility.IsIncarcerated(m.GetResolvedStatus());
            btn.interactable = !locked;

            GameObject labelGo = new GameObject("Label");
            labelGo.transform.SetParent(row.transform, false);
            RectTransform lRt = labelGo.AddComponent<RectTransform>();
            StretchFull(lRt);
            lRt.offsetMin = new Vector2(10f, 4f);
            lRt.offsetMax = new Vector2(-10f, -4f);
            TextMeshProUGUI tmp = labelGo.AddComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null)
                tmp.font = TMP_Settings.defaultFontAsset;
            string status = m != null ? CharacterStatusUtility.ToDisplayLabel(m.GetResolvedStatus()) : "?";
            tmp.text = (m != null ? m.Name : "?") + "  <size=90%><color=#a8a8a8>· " + (m != null ? m.Role : "") + " · " + status + "</color></size>";
            if (locked)
                tmp.text += " <color=#c07070><i>(unavailable)</i></color>";
            tmp.fontSize = 15f;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = new Color(0.9f, 0.88f, 0.84f, 1f);
            tmp.richText = true;
            tmp.raycastTarget = false;

            btn.onClick.AddListener(() =>
            {
                if (locked)
                    return;
                _opsCrewPickSelectedIndex = idx;
                OpsCrewPickClampRolesToSquad();
                OpsCrewPickRebuildExtraToggles();
                OpsCrewPickRefreshRowHighlights();
                OpsCrewPickRefreshFooter();
                OpsCrewPickRefreshRoleButtonLabels();
                OpsCrewPickRefreshDetailSummary();
            });
        }

        OpsCrewPickRefreshRowHighlights();
        OpsCrewPickRebuildExtraToggles();
        OpsCrewPickRefreshRoleButtonLabels();
        OpsCrewPickRefreshDetailSummary();
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_opsCrewPickListContent);
    }

    private void OpsCrewPickRefreshRowHighlights()
    {
        for (int i = 0; i < _opsCrewPickRowBackgrounds.Count; i++)
        {
            Image img = _opsCrewPickRowBackgrounds[i];
            if (img == null)
                continue;
            bool sel = i == _opsCrewPickSelectedIndex;
            img.color = sel ? new Color(0.22f, 0.28f, 0.38f, 0.98f) : new Color(0.16f, 0.16f, 0.18f, 0.95f);
            Button b = img.GetComponent<Button>();
            if (b != null)
                PlanningUiButtonStyle.ApplyColorTint(b, img.color);
        }
    }

    private void OpsCrewPickRefreshFooter()
    {
        bool ordered = GameSessionState.OrderedOperations.Contains(_opsCrewPickOperation);
        if (_opsCrewPickRemoveBtn != null)
            _opsCrewPickRemoveBtn.gameObject.SetActive(ordered);

        bool canPick = _opsCrewPickSelectedIndex >= 0 && _opsCrewPickSelectedIndex < PersonnelRegistry.Members.Count;
        if (canPick)
        {
            CrewMember m = PersonnelRegistry.Members[_opsCrewPickSelectedIndex];
            if (m != null && CharacterStatusUtility.IsIncarcerated(m.GetResolvedStatus()))
                canPick = false;
        }

        if (_opsCrewPickConfirmBtn != null)
            _opsCrewPickConfirmBtn.interactable = canPick;

        if (_opsCrewPickConfirmLabel != null)
            _opsCrewPickConfirmLabel.text = ordered ? "Update lead" : "Add to queue";
    }

    private void OpsCrewPickConfirm()
    {
        if (_opsCrewPickSelectedIndex < 0 || _opsCrewPickSelectedIndex >= PersonnelRegistry.Members.Count)
            return;
        CrewMember m = PersonnelRegistry.Members[_opsCrewPickSelectedIndex];
        if (m != null && CharacterStatusUtility.IsIncarcerated(m.GetResolvedStatus()))
            return;

        OperationType op = _opsCrewPickOperation;
        GameSessionState.RollbackOperationPlanningHoursIfAny(op);

        if (!GameSessionState.OrderedOperations.Contains(op))
            GameSessionState.OrderedOperations.Add(op);
        GameSessionState.SetOperationAssignee(op, _opsCrewPickSelectedIndex);
        if (op == OperationType.Scout)
            GameSessionState.ScoutMissionOrdered = true;

        GameSessionState.EnsureActiveCityData();
        MicroBlockSpotRuntime spot = FindOpsSpotByStableId(_opsSelectedSpotStableId);
        int home = MicroBlockWorldState.CrewHomeBlockId;
        int targetBlock = spot != null
            ? OperationTimingSystem.ResolveTargetBlockIdForSpot(GameSessionState.ActiveCityData, spot, home)
            : (_opsCenterMapSelectedBlockId >= 0 ? _opsCenterMapSelectedBlockId : home);
        int squadCount = OpsCrewPickCountSquadMembers();
        string spotId = spot != null ? spot.StableId : string.Empty;
        GameSessionState.SetOperationMissionMeta(op, targetBlock, spotId, squadCount);

        var extras = new System.Collections.Generic.List<int>();
        for (int i = 0; i < _opsCrewPickExtraOn.Length; i++)
        {
            if (!_opsCrewPickExtraOn[i] || i == _opsCrewPickSelectedIndex)
                continue;
            extras.Add(i);
        }

        GameSessionState.OperationExtraMemberIndices[op] = extras;
        GameSessionState.OperationMissionUsesCrewVehicle[op] = _opsCrewPickMissionVehicle;
        GameSessionState.OperationLookoutMemberIndex[op] = _opsCrewPickLookoutIdx;
        GameSessionState.OperationDriverMemberIndex[op] = _opsCrewPickDriverIdx;

        if (!OpsCrewPickTryComputeMissionHours(out _, out _, out float totalH))
            totalH = 0f;
        GameSessionState.OperationPlannedWallHours[op] = totalH;

        System.Collections.Generic.List<int> squad = OpsCrewPickBuildSquadList();
        OpsPlanningRhythmState.AddMissionWallHoursForMembers(squad, totalH);

        UpdateAllOpButtonLabels();
        if (_current == PlanningTabId.Operations && _centerText != null)
            _centerText.text = BuildOperationsCenterText();
        CloseOpsCrewAssignModal();
    }

    private void OpsCrewPickRemoveFromQueue()
    {
        OperationType op = _opsCrewPickOperation;
        GameSessionState.RollbackOperationPlanningHoursIfAny(op);
        GameSessionState.OrderedOperations.Remove(op);
        GameSessionState.RemoveOperationAssignee(op);
        GameSessionState.RemoveOperationMissionMeta(op);
        if (op == OperationType.Scout)
            GameSessionState.ScoutMissionOrdered = false;

        UpdateAllOpButtonLabels();
        if (_current == PlanningTabId.Operations && _centerText != null)
            _centerText.text = BuildOperationsCenterText();
        CloseOpsCrewAssignModal();
    }

    private void BuildInstitutionModal()
    {
        Canvas canvas = GameObject.Find("Canvas")?.GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("PlanningShellController: Canvas not found — institution modal skipped.");
            return;
        }

        Transform old = canvas.transform.Find("InstitutionModalRoot");
        if (old != null)
            Destroy(old.gameObject);

        GameObject root = new GameObject("InstitutionModalRoot");
        root.transform.SetParent(canvas.transform, false);
        RectTransform rootRt = root.AddComponent<RectTransform>();
        StretchFull(rootRt);
        root.SetActive(false);
        _institutionModalRoot = root;

        Image dim = root.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.55f);
        dim.raycastTarget = true;

        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(root.transform, false);
        RectTransform prt = panel.AddComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(540f, 380f);
        _institutionModalPanelRt = prt;

        Image pimg = panel.AddComponent<Image>();
        pimg.color = new Color(0.11f, 0.12f, 0.15f, 0.99f);
        pimg.raycastTarget = true;
        _institutionModalPanelImage = pimg;
        _institutionModalPanelColorDefault = pimg.color;

        Outline po = panel.AddComponent<Outline>();
        po.effectColor = new Color(0.45f, 0.5f, 0.58f, 0.75f);
        po.effectDistance = new Vector2(2f, -2f);

        GameObject titleGo = new GameObject("Title");
        titleGo.transform.SetParent(panel.transform, false);
        RectTransform trt = titleGo.AddComponent<RectTransform>();
        trt.anchorMin = new Vector2(0f, 1f);
        trt.anchorMax = new Vector2(1f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.anchoredPosition = new Vector2(0f, -16f);
        trt.sizeDelta = new Vector2(-32f, 44f);
        _institutionModalTitle = titleGo.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            _institutionModalTitle.font = TMP_Settings.defaultFontAsset;
        _institutionModalTitle.fontSize = 26f;
        _institutionModalTitle.fontStyle = FontStyles.Bold;
        _institutionModalTitle.alignment = TextAlignmentOptions.Center;
        _institutionModalTitle.color = new Color(0.95f, 0.95f, 0.92f);
        _institutionModalTitle.text = "";
        _institutionModalTitle.raycastTarget = true;
        RegisterDraggableModalTitle(titleGo, prt);

        GameObject bodyGo = new GameObject("Body");
        bodyGo.transform.SetParent(panel.transform, false);
        RectTransform brt = bodyGo.AddComponent<RectTransform>();
        brt.anchorMin = new Vector2(0f, 0f);
        brt.anchorMax = new Vector2(1f, 1f);
        brt.offsetMin = new Vector2(24f, 64f);
        brt.offsetMax = new Vector2(-24f, -72f);
        _institutionModalBody = bodyGo.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            _institutionModalBody.font = TMP_Settings.defaultFontAsset;
        _institutionModalBody.fontSize = 18f;
        _institutionModalBody.alignment = TextAlignmentOptions.TopLeft;
        _institutionModalBody.textWrappingMode = TextWrappingModes.Normal;
        _institutionModalBody.color = new Color(0.82f, 0.84f, 0.88f, 1f);
        _institutionModalBody.text = "Content coming soon.";

        BuildPoliceModalContent(panel.transform);
        BuildStubGovernmentInstitutionShells(panel.transform);

        GameObject closeGo = new GameObject("CloseButton");
        closeGo.transform.SetParent(panel.transform, false);
        RectTransform crt = closeGo.AddComponent<RectTransform>();
        crt.anchorMin = new Vector2(1f, 1f);
        crt.anchorMax = new Vector2(1f, 1f);
        crt.pivot = new Vector2(1f, 1f);
        crt.anchoredPosition = new Vector2(-12f, -10f);
        crt.sizeDelta = new Vector2(40f, 36f);
        Image cimg = closeGo.AddComponent<Image>();
        Button cbtn = closeGo.AddComponent<Button>();
        cbtn.targetGraphic = cimg;
        PlanningUiButtonStyle.ApplyStandardRectButton(cbtn, cimg);
        cbtn.onClick.AddListener(HideInstitutionWindow);
        closeGo.AddComponent<ButtonPressScale>();
        GameObject clabel = new GameObject("Label");
        clabel.transform.SetParent(closeGo.transform, false);
        RectTransform clRt = clabel.AddComponent<RectTransform>();
        StretchFull(clRt);
        TextMeshProUGUI ctmp = clabel.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            ctmp.font = TMP_Settings.defaultFontAsset;
        // TMP may not contain the '✕' glyph; use plain X to avoid missing-character warnings.
        ctmp.text = "X";
        ctmp.fontSize = 22f;
        ctmp.alignment = TextAlignmentOptions.Center;
        ctmp.color = PlanningUiButtonStyle.LabelPrimary;
        ctmp.raycastTarget = false;
    }

    /// <summary>Second click on the same dock icon closes the modal; another icon switches content.</summary>
    private void ToggleInstitutionWindow(string title, string body)
    {
        if (_institutionModalRoot != null
            && _institutionModalRoot.activeSelf
            && _activeInstitutionModalTitle == title)
        {
            HideInstitutionWindow();
            return;
        }

        ShowInstitutionWindow(title, body);
    }

    private void ShowInstitutionWindow(string title, string body)
    {
        if (_institutionModalRoot == null || _institutionModalTitle == null || _institutionModalBody == null)
            return;

        _activeInstitutionModalTitle = title;
        _institutionModalTitle.text = title;
        _institutionModalBody.text = body;
        bool prison = title == "Prison";
        bool police = title == "Police";
        bool stubGov = IsStubGovernmentInstitution(title);
        bool wideTriPane = police || stubGov;
        if (prison)
        {
            _prisonAlertAcknowledged = true;
            _prisonAlertActive = false;
        }
        if (_institutionModalPanelImage != null)
        {
            if (police)
                _institutionModalPanelImage.color = new Color(0.08f, 0.1f, 0.16f, 0.99f);
            else if (stubGov && _stubGovernmentUis.TryGetValue(title, out StubGovernmentInstitutionUi stubCol))
                _institutionModalPanelImage.color = stubCol.ModalPanelColor;
            else
                _institutionModalPanelImage.color = _institutionModalPanelColorDefault;
        }
        ApplyInstitutionModalSize(wideTriPane);
        _institutionModalBody.gameObject.SetActive(!police && !stubGov);
        if (_institutionPoliceContentRoot != null)
            _institutionPoliceContentRoot.SetActive(police);
        SetStubGovernmentInstitutionRootsActive(stubGov, stubGov ? title : null);
        if (police)
            RefreshPoliceModalContent();
        if (stubGov)
            RefreshStubGovernmentShell(title);
        _institutionModalRoot.SetActive(true);
        _institutionModalRoot.transform.SetAsLastSibling();
        // Keep bottom dock + AoW (portrait, Next Turn) above the dimmer + panel so they stay visible and clickable.
        BringPlanningInteractiveChromeToFront();
    }

    private void BringPlanningInteractiveChromeToFront()
    {
        Transform canvasTf = null;
        if (_institutionModalRoot != null && _institutionModalRoot.transform.parent != null)
            canvasTf = _institutionModalRoot.transform.parent;
        else if (_codexModalRoot != null && _codexModalRoot.transform.parent != null)
            canvasTf = _codexModalRoot.transform.parent;
        else
            canvasTf = GameObject.Find("Canvas")?.transform;
        if (canvasTf == null)
            return;
        Transform bottomBar = canvasTf.Find("BottomBar");
        if (bottomBar != null)
            bottomBar.SetAsLastSibling();
        if (_aowHudRoot != null)
            _aowHudRoot.transform.SetAsLastSibling();
        // Full-screen codex must stay above bottom bar / AoW strip or Prev/Next and lower page hits are blocked.
        if (_codexModalRoot != null && _codexModalRoot.activeSelf)
            _codexModalRoot.transform.SetAsLastSibling();
    }

    private void HideInstitutionWindow()
    {
        _activeInstitutionModalTitle = null;
        if (_institutionModalRoot != null)
            _institutionModalRoot.SetActive(false);
    }

    /// <summary>
    /// Batch 14: call after <see cref="GovernmentRuntimeCitySource"/> or discovery updates while a government modal is open.
    /// </summary>
    public void RefreshGovernmentWindowsIfOpen()
    {
        if (_institutionModalRoot == null || !_institutionModalRoot.activeSelf)
            return;
        if (string.IsNullOrEmpty(_activeInstitutionModalTitle))
            return;

        if (_activeInstitutionModalTitle == "Police")
            RefreshPoliceModalContent();
        else if (IsStubGovernmentInstitution(_activeInstitutionModalTitle))
            RefreshStubGovernmentShell(_activeInstitutionModalTitle);
    }

    private void ApplyInstitutionModalSize(bool wideTriPane)
    {
        if (_institutionModalPanelRt == null)
            return;

        if (!wideTriPane)
        {
            _institutionModalPanelRt.sizeDelta = new Vector2(540f, 380f);
            _institutionModalPanelRt.anchoredPosition = Vector2.zero;
            return;
        }

        GetPlanningCanvasDimensions(out float canvasW, out float canvasH);

        const float wFrac = 0.99f;
        const float hFrac = 0.90f;
        float desiredW = canvasW * wFrac;
        float h = canvasH * hFrac;

        // Tri-pane: narrow left/right rails (GovernmentInstitutionShell) + minimum center column.
        const float idealMinTriPaneWidth = 660f;
        const float leftGutterPx = 12f;
        float hudClearance = GetInstitutionModalRightHudClearancePx();
        // ~5 cm wider per side (physical approximation via DPI); user-requested expansion past the strict HUD box.
        float dpiForCm = Screen.dpi > 1f ? Screen.dpi : 96f;
        const float extraWidthCmPerSide = 10f;
        float extraSidePx = extraWidthCmPerSide * 0.3937008f * dpiForCm;
        float maxWByHud = canvasW - 2f * hudClearance + 2f * extraSidePx;
        float maxWByGutter = canvasW - 2f * leftGutterPx;
        float maxW = Mathf.Min(desiredW, maxWByGutter);
        if (maxWByHud > 80f)
            maxW = Mathf.Min(maxW, maxWByHud);
        float minW = Mathf.Min(idealMinTriPaneWidth, maxW);
        float w = Mathf.Clamp(desiredW, minW, maxW);

        const float topGutterPx = 32f;
        float bottomBlock = BottomBarLayoutHeightForChrome + (_enableAoWCharacterHud ? Mathf.Max(88f, _aowTurnStripSize.y * 0.5f) : 28f);
        float maxH = Mathf.Max(300f, canvasH - topGutterPx - bottomBlock);
        h = Mathf.Min(h, maxH);

        _institutionModalPanelRt.sizeDelta = new Vector2(w, h);
        _institutionModalPanelRt.anchoredPosition = Vector2.zero;
    }

    /// <summary>Usable pixel size for sizing fullscreen overlays (prefers <see cref="Canvas.pixelRect"/>).</summary>
    private static void GetPlanningCanvasDimensions(out float width, out float height)
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            Rect pr = canvas.pixelRect;
            if (pr.width > 2f && pr.height > 2f)
            {
                width = pr.width;
                height = pr.height;
                return;
            }

            RectTransform rt = canvas.GetComponent<RectTransform>();
            if (rt != null && rt.rect.width > 2f && rt.rect.height > 2f)
            {
                width = rt.rect.width;
                height = rt.rect.height;
                return;
            }
        }

        width = Screen.width;
        height = Screen.height;
    }

    /// <summary>
    /// Width in canvas pixels from the right edge that must stay clear for <see cref="BuildAoWCharacterHud"/>.
    /// Used with centered modals: <c>maxWidth = canvasW - 2f * value</c>.
    /// </summary>
    private float GetInstitutionModalRightHudClearancePx()
    {
        if (!_enableAoWCharacterHud || !_aowHudOwnsTurnControls)
            return 36f;

        // Turn strip is bottom-right; keep tri-pane modals from overlapping that horizontal band from the right.
        float dpi = Screen.dpi;
        if (dpi <= 1f)
            dpi = Mathf.Max(24f, _aowFallbackDpi);
        float cmToPx = 0.3937008f * dpi;
        float rightEdgePad = _aowRightMarginCm * cmToPx + Mathf.Abs(_aowPortraitClusterOffset.x);
        float portraitR = Mathf.Min(_aowPortraitSize.x, _aowPortraitSize.y) * 0.5f;
        float radialStickout = _aowRadialRadius > 0.01f
            ? Mathf.Max(0f, _aowRadialRadius - portraitR + _aowRadialEdgeGap)
            : Mathf.Max(0f, _aowRadialButtonDiameter * 0.5f + _aowRadialEdgeGap - portraitR * 0.35f);
        float portraitBand = Mathf.Min(_aowPortraitSize.x, _aowPortraitSize.y) * 0.52f;
        float clearance = _aowTurnStripSize.x + portraitBand + rightEdgePad + 16f + Mathf.Clamp(radialStickout, 0f, 48f)
            + Mathf.Max(0f, _aowNextTurnPortraitGapPx);
        return Mathf.Max(200f, clearance);
    }

    private void BuildPoliceModalContent(Transform panel)
    {
        GameObject policeRoot = new GameObject("PoliceContent");
        policeRoot.transform.SetParent(panel, false);
        RectTransform pr = policeRoot.AddComponent<RectTransform>();
        pr.anchorMin = new Vector2(0f, 0f);
        pr.anchorMax = new Vector2(1f, 1f);
        pr.offsetMin = new Vector2(24f, 64f);
        pr.offsetMax = new Vector2(-24f, -72f);
        policeRoot.SetActive(false);
        _institutionPoliceContentRoot = policeRoot;

        Color policeTint = new Color(0.08f, 0.1f, 0.16f, 1f);
        _policeShell = GovernmentInstitutionShell.Build(policeRoot.transform, policeTint);

        (string label, PoliceModalTabId tab)[] modeEntries =
        {
            ("Known Situation", PoliceModalTabId.KnownSituation),
            ("People & Locations", PoliceModalTabId.KnownPeopleLocations),
            ("Cases & Ops", PoliceModalTabId.KnownCasesOps),
            ("Weaknesses", PoliceModalTabId.KnownWeaknessesOpportunities),
            ("Actions", PoliceModalTabId.AvailableActions),
            ("LOG", PoliceModalTabId.OutcomeLog)
        };
        for (int i = 0; i < modeEntries.Length; i++)
        {
            PoliceModalTabId capturedTab = modeEntries[i].tab;
            Button b = CreateBarButton(_policeShell.BottomModesRoot, modeEntries[i].label);
            RectTransform br = b.GetComponent<RectTransform>();
            if (br != null)
                br.sizeDelta = new Vector2(0f, 40f);
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(() =>
            {
                _policeModalTab = capturedTab;
                _policeSelectedFacilityIndex = -1;
                _policePersonnelBucketIndex = -1;
                _policeCityGenStableSelection = null;
                RefreshPoliceModalContent();
            });
            _policeShell.ModeButtons.Add(b);
        }
    }

    private static bool IsStubGovernmentInstitution(string title)
    {
        if (string.IsNullOrEmpty(title))
            return false;
        return title == "Federal Bureau"
               || title == "City Hall"
               || title == "Taxes"
               || title == "Court"
               || title == "Hospital"
               || title == "Prison";
    }

    private void SetStubGovernmentInstitutionRootsActive(bool anyVisible, string onlyTitle)
    {
        foreach (KeyValuePair<string, StubGovernmentInstitutionUi> kv in _stubGovernmentUis)
        {
            if (kv.Value?.Root == null)
                continue;
            bool on = anyVisible && onlyTitle != null && kv.Key == onlyTitle;
            kv.Value.Root.SetActive(on);
        }
    }

    private void BuildStubGovernmentInstitutionShells(Transform panel)
    {
        _stubGovernmentUis.Clear();
        // Panel + shell bar tints follow each institution’s hue (infrastructure only — swap copy/data later).
        BuildOneStubGovernmentShell(panel, "Federal Bureau",
            new Color(0.09f, 0.11f, 0.18f, 0.99f),
            new Color(0.12f, 0.14f, 0.22f, 1f),
            "Deployment", "Personnel", "Cases", "Interest");
        BuildOneStubGovernmentShell(panel, "City Hall",
            new Color(0.10f, 0.14f, 0.16f, 0.99f),
            new Color(0.14f, 0.18f, 0.21f, 1f),
            "Overview", "Departments", "Leverage");
        BuildOneStubGovernmentShell(panel, "Taxes",
            new Color(0.09f, 0.14f, 0.11f, 0.99f),
            new Color(0.12f, 0.19f, 0.14f, 1f),
            "Overview", "Filings", "Audits");
        BuildOneStubGovernmentShell(panel, "Court",
            new Color(0.14f, 0.11f, 0.09f, 0.99f),
            new Color(0.20f, 0.16f, 0.13f, 1f),
            "Proceedings", "Personnel", "Reserved", "Reserved");
        BuildOneStubGovernmentShell(panel, "Hospital",
            new Color(0.15f, 0.10f, 0.11f, 0.99f),
            new Color(0.20f, 0.13f, 0.14f, 1f),
            "Overview", "Records", "Capacity");
        BuildOneStubGovernmentShell(panel, "Prison",
            new Color(0.26f, 0.1f, 0.09f, 0.99f),
            new Color(0.32f, 0.14f, 0.12f, 1f),
            "Before trial", "After trial");
    }

    private void BuildOneStubGovernmentShell(Transform panel, string title, Color modalPanelColor, Color shellBottomBarTint,
        params string[] modeLabels)
    {
        if (modeLabels == null || modeLabels.Length == 0)
            modeLabels = new[] { "Overview" };

        GameObject root = new GameObject("InstitutionContent_" + title.Replace(" ", ""));
        root.transform.SetParent(panel, false);
        RectTransform pr = root.AddComponent<RectTransform>();
        pr.anchorMin = new Vector2(0f, 0f);
        pr.anchorMax = new Vector2(1f, 1f);
        pr.offsetMin = new Vector2(24f, 64f);
        pr.offsetMax = new Vector2(-24f, -72f);
        root.SetActive(false);

        GovernmentInstitutionShellView shell = GovernmentInstitutionShell.Build(root.transform, shellBottomBarTint);
        var ui = new StubGovernmentInstitutionUi
        {
            Root = root,
            Shell = shell,
            SelectedTabIndex = 0,
            ModalPanelColor = modalPanelColor,
            ModeLabels = modeLabels
        };
        _stubGovernmentUis[title] = ui;

        for (int i = 0; i < modeLabels.Length; i++)
        {
            int capTab = i;
            string capTitle = title;
            Button b = CreateBarButton(shell.BottomModesRoot, modeLabels[i]);
            RectTransform br = b.GetComponent<RectTransform>();
            if (br != null)
                br.sizeDelta = new Vector2(0f, 40f);
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(() =>
            {
                if (_stubGovernmentUis.TryGetValue(capTitle, out StubGovernmentInstitutionUi u))
                {
                    u.SelectedTabIndex = capTab;
                    if (capTitle == "Federal Bureau")
                        _federalCityGenStableSelection = null;
                    RefreshStubGovernmentShell(capTitle);
                }
            });
            shell.ModeButtons.Add(b);
        }

        if (title == "Prison")
        {
            for (int i = 0; i < shell.ModeButtons.Count; i++)
            {
                if (shell.ModeButtons[i] != null)
                    shell.ModeButtons[i].interactable = false;
            }
        }
    }

    private void RefreshStubGovernmentShell(string title)
    {
        if (!_stubGovernmentUis.TryGetValue(title, out StubGovernmentInstitutionUi ui) || ui.Shell == null)
            return;
        if (title == "Prison")
        {
            RefreshPrisonInstitutionShell(ui);
            return;
        }

        GovernmentInstitutionShellView shell = ui.Shell;
        for (int i = 0; i < shell.ModeButtons.Count; i++)
        {
            Button b = shell.ModeButtons[i];
            if (b == null)
                continue;
            Image img = b.GetComponent<Image>();
            if (img == null)
                continue;
            bool active = i == ui.SelectedTabIndex;
            img.color = active
                ? new Color(0.16f, 0.18f, 0.24f, 0.95f)
                : new Color(0.12f, 0.13f, 0.17f, 0.75f);
        }

        if (title == "Federal Bureau" && GovernmentRuntimeCitySource.HasRenderableGovernmentData)
        {
            var federalMode = (FederalWindowMode)Mathf.Clamp(ui.SelectedTabIndex, 0, (int)FederalWindowMode.Interest);
            GovernmentWindowRuntimeBinder.ApplyFederalShell(shell, GovernmentRuntimeCitySource.ActiveCity, federalMode,
                _federalCityGenStableSelection,
                id =>
                {
                    _federalCityGenStableSelection = id;
                    RefreshStubGovernmentShell(title);
                });
            return;
        }

        if (title == "Court" && GovernmentRuntimeCitySource.HasRenderableGovernmentData)
        {
            var courtMode = (CourtWindowMode)Mathf.Clamp(ui.SelectedTabIndex, 0, (int)CourtWindowMode.Reserved2);
            GovernmentWindowRuntimeBinder.ApplyCourtShell(shell, GovernmentRuntimeCitySource.ActiveCity, courtMode);
            return;
        }

        shell.ClearLeftList();
        shell.ClearRightActions();

        string[] labels = ui.ModeLabels ?? new string[0];
        string tabName = ui.SelectedTabIndex >= 0 && ui.SelectedTabIndex < labels.Length
            ? labels[ui.SelectedTabIndex]
            : "—";

        if (shell.LeftPanelTitle != null)
            shell.LeftPanelTitle.text = "<b>Register</b>";

        string stubNarrative = title switch
        {
            "Federal Bureau" =>
                "Federal desks track wire affidavits, interstate priors, and sealed inquiries. " +
                "Use the register for targets; detail panes fill in when city data is available.",
            "City Hall" =>
                "Departments sit behind budget lines — permits, inspectors, franchises. " +
                "Leverage is who owes a vote, who needs a variance, and who can stall a contract.",
            "Taxes" =>
                "Filings anchor audit timelines; liens and revenue notices follow real balances. " +
                "Escalation paths run from desk review to field collections.",
            "Court" =>
                "Docket slices show arraignment vs. motion practice. " +
                "Personnel covers clerks and prosecutors attached to your headlines.",
            "Hospital" =>
                "Records tie admissions to injury codes; capacity tracks beds and diversion risk. " +
                "Quiet wings matter as much as surgical throughput.",
            _ =>
                "Institutional index: left list, center narrative, right actions —same shell as Police when data binds."
        };

        shell.CenterBody.text = "<b>" + title + "</b>\n<b>" + tabName + "</b>\n\n" + stubNarrative;
    }

    private void RefreshPrisonInstitutionShell(StubGovernmentInstitutionUi ui)
    {
        GameSessionState.ApplyBossCustodyLegalPhaseFromTrialFlag();
        bool beforePhase = GameSessionState.BossPrisonPhase == GameSessionState.PrisonLegalPhase.BeforeTrial;
        ui.SelectedTabIndex = beforePhase ? 0 : 1;

        GovernmentInstitutionShellView shell = ui.Shell;
        for (int i = 0; i < shell.ModeButtons.Count; i++)
        {
            Button b = shell.ModeButtons[i];
            if (b == null)
                continue;
            Image img = b.GetComponent<Image>();
            if (img == null)
                continue;
            bool active = i == ui.SelectedTabIndex;
            img.color = active
                ? new Color(0.16f, 0.18f, 0.24f, 0.95f)
                : new Color(0.12f, 0.13f, 0.17f, 0.75f);
        }

        shell.ClearLeftList();
        shell.ClearRightActions();

        if (shell.LeftPanelTitle != null)
            shell.LeftPanelTitle.text = "<b>In custody</b>";

        if (CountIncarceratedCrewMembers() <= 0)
        {
            _prisonShellShownInmate = null;
            _prisonShellTrainingPick = -1;
            shell.CenterBody.text =
                "<b>Prison</b>\n\n<i>No crew members are in custody.</i>\n\n<size=90%>Released on bail or not held — this panel lists active inmates only.</size>";
            return;
        }

        bool bossInCustody = CharacterStatusUtility.IsIncarcerated(GameSessionState.GetPlayerBossResolvedCustodyStatus());
        CrewMember firstIncarcerated = null;
        for (int i = 0; i < PersonnelRegistry.Members.Count; i++)
        {
            CrewMember m = PersonnelRegistry.Members[i];
            if (m == null || !CharacterStatusUtility.IsIncarcerated(m.GetResolvedStatus()))
                continue;
            if (firstIncarcerated == null)
                firstIncarcerated = m;

            CrewMember capM = m;
            string label = string.IsNullOrWhiteSpace(m.Name) ? ("Member " + (i + 1)) : m.Name.Trim();
            if (IsBossMember(m))
                label = "<b>" + label + "</b> <size=85%>(boss)</size>";

            Button lb = CreateGovernmentShellRowButton(shell.LeftContent, label);
            lb.onClick.AddListener(() =>
            {
                _prisonShellShownInmate = capM;
                SyncPrisonShellSelectionFromInmate(_prisonShellShownInmate);
                RefreshPrisonInstitutionShell(ui);
            });
            shell.LeftListButtons.Add(lb);
        }

        if (_prisonShellShownInmate == null ||
            !CharacterStatusUtility.IsIncarcerated(_prisonShellShownInmate.GetResolvedStatus()))
        {
            _prisonShellShownInmate = bossInCustody && PersonnelRegistry.Members.Count > 0
                ? PersonnelRegistry.Members[0]
                : firstIncarcerated;
        }

        SyncPrisonShellSelectionFromInmate(_prisonShellShownInmate);

        CrewMember shown = _prisonShellShownInmate;
        string name = shown != null && !string.IsNullOrWhiteSpace(shown.Name)
            ? shown.Name.Trim()
            : (PlayerRunState.Character?.DisplayName ?? "Inmate");

        string phaseHeader = beforePhase ? "Before trial" : "After trial";
        string statusLine = shown != null
            ? CharacterStatusUtility.ToDisplayLabel(shown.GetResolvedStatus())
            : "—";

        string phaseNote = "";
        if (shown != null && IsBossMember(shown) && !GameSessionState.BossCustodyTrialCompleted)
            phaseNote = "\n\n<size=90%><i>Pre-trial detention only. Post-trial prison rubric unlocks after a trial verdict in play.</i></size>";
        else if (shown != null && IsBossMember(shown) && GameSessionState.BossCustodyTrialCompleted)
            phaseNote = "\n\n<size=90%><i>Post-trial incarceration lane.</i></size>";

        string causeLine = "";
        if (shown != null && shown.GetResolvedStatus() == CharacterStatus.Detained)
        {
            string line = ArrestCauseUtility.FormatArrestReasonLine(shown.Arrest);
            if (!string.IsNullOrEmpty(line))
                causeLine = "\n\n<b>Arrest</b>\n" + line;
        }

        string autoLine = "";
        if (shown != null && !IsBossMember(shown))
        {
            bool auto = shown.PrisonTrainingAuto;
            autoLine = "\nAuto training: <b>" + (auto ? "ON" : "OFF") + "</b>";
        }

        string focusLine = BuildPrisonShellFocusLineForUi(shown);

        shell.CenterBody.text =
            "<b>Prison</b>\n<size=90%>Legal phase: <b>" + phaseHeader + "</b> (read-only — follows game state).</size>\n\n" +
            "<b>" + name + "</b>\n" +
            "Status: <b>" + statusLine + "</b>" + causeLine + autoLine +
            "\n\n<b>Current training focus</b>\n" + focusLine + phaseNote;

        if (shown != null && !IsBossMember(shown))
        {
            Button autoBtn = CreateGovernmentShellRowButton(shell.RightActionsRoot,
                shown.PrisonTrainingAuto ? "Auto training: ON" : "Auto training: OFF");
            autoBtn.onClick.AddListener(() =>
            {
                if (_prisonShellShownInmate == null)
                    return;
                _prisonShellShownInmate.PrisonTrainingAuto = !_prisonShellShownInmate.PrisonTrainingAuto;
                if (_prisonShellShownInmate.PrisonTrainingAuto)
                    _prisonShellShownInmate.PrisonTrainingFocusIndex = -1;
                SyncPrisonShellSelectionFromInmate(_prisonShellShownInmate);
                RefreshPrisonInstitutionShell(ui);
            });
            shell.RightActionButtons.Add(autoBtn);
        }

        if (shown == null)
            return;

        string[] trainingLabels = GetPrisonShellTrainingLabels(shown);
        for (int ti = 0; ti < trainingLabels.Length; ti++)
        {
            if (trainingLabels[ti] == "—")
                continue;
            int capTi = ti;
            Button tb = CreateGovernmentShellRowButton(shell.RightActionsRoot, trainingLabels[ti]);
            bool allowed = PrisonShellIsTrainingOptionAllowed(shown, ti);
            tb.interactable = allowed && PrisonShellAllowManualTrainingPick(shown);
            ApplyPrisonShellTrainingRowSelected(tb, capTi == _prisonShellTrainingPick);
            tb.onClick.AddListener(() =>
            {
                if (_prisonShellShownInmate == null)
                    return;
                _prisonShellTrainingPick = capTi;
                _prisonShellShownInmate.PrisonTrainingAuto = false;
                _prisonShellShownInmate.PrisonTrainingFocusIndex = capTi;
                RefreshPrisonInstitutionShell(ui);
            });
            shell.RightActionButtons.Add(tb);
        }
    }

    private static bool IsBossMember(CrewMember m)
    {
        if (m == null || PersonnelRegistry.Members == null || PersonnelRegistry.Members.Count == 0)
            return false;
        return m == PersonnelRegistry.Members[0];
    }

    private static bool IsDetainedPhaseForMember(CrewMember m)
    {
        return m != null && m.GetResolvedStatus() == CharacterStatus.Detained;
    }

    private static int ClampPrisonShellFocusForStatus(int focusIndex, CharacterStatus st)
    {
        if (st == CharacterStatus.Detained)
            return Mathf.Clamp(focusIndex, 0, 1);
        return Mathf.Clamp(focusIndex, 0, 3);
    }

    private void SyncPrisonShellSelectionFromInmate(CrewMember inmate)
    {
        if (inmate == null)
        {
            _prisonShellTrainingPick = -1;
            return;
        }

        if (inmate.PrisonTrainingAuto && inmate.PrisonTrainingFocusIndex < 0)
            inmate.PrisonTrainingFocusIndex = 0;

        if (inmate.PrisonTrainingFocusIndex < 0)
        {
            _prisonShellTrainingPick = -1;
            return;
        }

        _prisonShellTrainingPick = ClampPrisonShellFocusForStatus(inmate.PrisonTrainingFocusIndex, inmate.GetResolvedStatus());
    }

    private static string[] GetPrisonShellTrainingLabels(CrewMember shown)
    {
        if (shown != null && !IsBossMember(shown))
        {
            if (IsDetainedPhaseForMember(shown))
                return new[] { "Train Strength", "Train Agility", "—", "—" };
            return new[] { "Train Strength", "Train Agility", "Train Intelligence", "Train Charisma" };
        }

        if (GameSessionState.BossPrisonPhase == GameSessionState.PrisonLegalPhase.BeforeTrial)
            return new[] { "Train Strength", "Train Agility", "—", "—" };
        return new[] { "Train Strength", "Train Agility", "Train Intelligence", "Train Charisma" };
    }

    private string BuildPrisonShellFocusLineForUi(CrewMember shown)
    {
        if (shown == null)
            return "<i>None selected</i>";
        if (shown.PrisonTrainingAuto && !IsBossMember(shown))
            return "<i>Auto (engine picks monthly)</i>";
        string[] labels = GetPrisonShellTrainingLabels(shown);
        if (_prisonShellTrainingPick < 0 || _prisonShellTrainingPick >= labels.Length || labels[_prisonShellTrainingPick] == "—")
            return "<i>None selected</i>";
        return labels[_prisonShellTrainingPick];
    }

    private static bool PrisonShellAllowManualTrainingPick(CrewMember shown)
    {
        if (shown == null)
            return false;
        return !shown.PrisonTrainingAuto || IsBossMember(shown);
    }

    private static bool PrisonShellIsTrainingOptionAllowed(CrewMember shown, int optionIndex)
    {
        if (shown == null)
            return false;
        CharacterStatus st = shown.GetResolvedStatus();
        if (st == CharacterStatus.Detained)
            return optionIndex <= 1;
        return optionIndex <= 3;
    }

    private static void ApplyPrisonShellTrainingRowSelected(Button btn, bool selected)
    {
        if (btn == null)
            return;
        Image img = btn.GetComponent<Image>();
        if (img != null)
        {
            img.color = selected
                ? PlanningUiButtonStyle.RectHighlight
                : PlanningUiButtonStyle.RectFill;
        }
    }

    private enum PoliceIntelCertainty
    {
        Rumor = 0,
        Estimated = 1,
        Revealed = 2
    }

    private enum PoliceExternalActionOutcome
    {
        Success = 0,
        PartialSuccess = 1,
        SilentFailure = 2,
        LoudFailure = 3
    }

    private sealed class PoliceExternalActionDefinition
    {
        public string actionId;
        public string label;
        public bool available;
        public string unavailableReason;
    }

    private void AddPoliceShellAction(string label, bool available = true, string unavailableReason = "")
    {
        if (_policeShell == null || _policeShell.RightActionsRoot == null)
            return;
        string uiLabel = available || string.IsNullOrEmpty(unavailableReason)
            ? label
            : label + " — locked (" + unavailableReason + ")";
        Button a = CreateGovernmentShellRowButton(_policeShell.RightActionsRoot, uiLabel);
        string cap = label;
        a.interactable = available;
        a.onClick.AddListener(() => Debug.Log("[Police] Action (stub): " + cap));
        _policeShell.RightActionButtons.Add(a);
    }

    private void PoliceShell_AddStationRightActions()
    {
        AddPoliceShellAction("Gather intel (stub)");
        AddPoliceShellAction("Watch / tail (stub)");
        AddPoliceShellAction("Assault station (stub)");
        AddPoliceShellAction("Break-in: documents (stub)");
        AddPoliceShellAction("Break-in: free detainee (stub)");
        AddPoliceShellAction("Break-in: gear / evidence (stub)");
    }

    private void PoliceShell_AddPersonnelBucketRightActions()
    {
        AddPoliceShellAction("Intel / develop source (stub)");
        AddPoliceShellAction("Surveillance (stub)");
        AddPoliceShellAction("Violent hit (stub)");
        AddPoliceShellAction("Kidnap (stub)");
        AddPoliceShellAction("Bribe (stub)");
        AddPoliceShellAction("Threaten / lean on (stub)");
    }

    private void RefreshPoliceModalContent()
    {
        if (_policeShell == null || _policeShell.CenterBody == null)
            return;

        GameSessionState.EnsurePoliceFacilitiesInitialized();
        GameSessionState.GenerateLocalPaperBlurbIfNeeded(force: false);

        for (int i = 0; i < _policeShell.ModeButtons.Count; i++)
        {
            Button b = _policeShell.ModeButtons[i];
            if (b == null) continue;
            Image img = b.GetComponent<Image>();
            if (img == null) continue;
            bool active = (int)_policeModalTab == i;
            img.color = active ? new Color(0.16f, 0.18f, 0.24f, 0.95f) : new Color(0.12f, 0.13f, 0.17f, 0.75f);
        }

        _policeShell.ClearLeftList();
        _policeShell.ClearRightActions();

        if (_policeShell.LeftPanelTitle != null)
        {
            _policeShell.LeftPanelTitle.text = _policeModalTab switch
            {
                PoliceModalTabId.KnownSituation => "<b>Known situation</b>",
                PoliceModalTabId.KnownPeopleLocations => "<b>Known people & locations</b>",
                PoliceModalTabId.KnownCasesOps => "<b>Known cases & operations</b>",
                PoliceModalTabId.KnownWeaknessesOpportunities => "<b>Weaknesses & opportunities</b>",
                PoliceModalTabId.AvailableActions => "<b>Available actions</b>",
                PoliceModalTabId.OutcomeLog => "<b>LOG / logistics view</b>",
                _ => "<b>—</b>"
            };
        }

        switch (_policeModalTab)
        {
            case PoliceModalTabId.KnownSituation:
                PoliceShell_BuildOverviewMode();
                break;
            case PoliceModalTabId.KnownPeopleLocations:
                PoliceShell_BuildStationsMode();
                break;
            case PoliceModalTabId.KnownCasesOps:
                PoliceShell_BuildCasesMode();
                break;
            case PoliceModalTabId.KnownWeaknessesOpportunities:
                PoliceShell_BuildActionsMode();
                break;
            case PoliceModalTabId.AvailableActions:
                PoliceShell_BuildAvailableActionsMode();
                break;
            default:
                PoliceShell_BuildOutcomeLogMode();
                break;
        }
    }

    private void PoliceShell_BuildOverviewMode()
    {
        Button b = CreateGovernmentShellRowButton(_policeShell.LeftContent, "City & posture");
        b.onClick.AddListener(() =>
        {
            _policeSelectedFacilityIndex = -1;
            PoliceShell_ShowOverviewCenter();
            PoliceShell_ShowOverviewActions();
        });
        _policeShell.LeftListButtons.Add(b);

        PoliceShell_ShowOverviewCenter();
        PoliceShell_ShowOverviewActions();
    }

    private void PoliceShell_ShowOverviewCenter()
    {
        int discoveredPrecincts = 0;
        int totalPrecincts = 0;
        for (int i = 0; i < GameSessionState.PoliceFacilities.Count; i++)
        {
            var f = GameSessionState.PoliceFacilities[i];
            if (f == null) continue;
            if (f.Type != GameSessionState.PoliceFacilityType.Precinct) continue;
            totalPrecincts++;
            if (f.DiscoveryLevel > 0) discoveredPrecincts++;
        }

        string paper = string.IsNullOrWhiteSpace(GameSessionState.LastLocalPaperBlurb)
            ? "<i>No fresh local chatter.</i>"
            : GameSessionState.LastLocalPaperBlurb.Trim();

        _policeShell.CenterBody.text =
            "<b>Known situation</b>\n\n" +
            "• Pressure: <b>" + GameSessionState.FormatPolicePressureLabel() + "</b> (" + GameSessionState.PolicePressureDisplayValue() + "/100)\n" +
            "• Street posture: <b>" + GameSessionState.FormatStreetStopRiskLabel() + "</b> (" + GameSessionState.StreetStopRiskDisplayValue() + "/100)\n" +
            "• Intel: " + GameSessionState.GetAgencyIntelHint(GameSessionState.AgencyId.Police) + "\n" +
            "• Precincts discovered: " + discoveredPrecincts + "/" + totalPrecincts + "\n\n" +
            "<b>Local paper</b>\n" + paper + "\n\n" +
            "<size=90%><i>Certainty lanes:</i> Rumor / Estimated / Revealed. " +
            "External window never shows internal police truth directly.</size>";
    }

    private void PoliceShell_ShowOverviewActions()
    {
        AddPoliceShellAction("Observe street posture");
        AddPoliceShellAction("Gather rumor (HUMINT)");
        AddPoliceShellAction("Open News tab");
    }

    private void PoliceShell_BuildStationsMode()
    {
        bool any = false;
        for (int i = 0; i < GameSessionState.PoliceFacilities.Count; i++)
        {
            var f = GameSessionState.PoliceFacilities[i];
            if (f == null) continue;
            if (f.Type == GameSessionState.PoliceFacilityType.Precinct && f.DiscoveryLevel <= 0)
                continue;

            int capIdx = i;
            string name = string.IsNullOrWhiteSpace(f.DisplayName)
                ? (f.Type == GameSessionState.PoliceFacilityType.HQ ? "Police HQ" : "Precinct " + f.FacilityNumber)
                : f.DisplayName;

            Button lb = CreateGovernmentShellRowButton(_policeShell.LeftContent, name);
            lb.onClick.AddListener(() =>
            {
                _policeSelectedFacilityIndex = capIdx;
                PoliceShell_ShowFacilityDetail(capIdx);
                _policeShell.ClearRightActions();
                PoliceShell_AddStationRightActions();
            });
            _policeShell.LeftListButtons.Add(lb);
            any = true;
        }

        if (!any)
        {
            _policeShell.CenterBody.text =
                "<b>Stations</b>\n\n<i>No station discovered yet.</i>\n\n<size=90%>Try the <b>News</b> tab — local paper mentions can reveal precincts.</size>";
            AddPoliceShellAction("Read paper (News) (stub)");
            return;
        }

        if (_policeSelectedFacilityIndex < 0 ||
            _policeSelectedFacilityIndex >= GameSessionState.PoliceFacilities.Count)
        {
            _policeShell.CenterBody.text = "<b>Stations</b>\n\n<i>Select a facility from the left.</i>";
            AddPoliceShellAction("Map: known stations (stub)");
            return;
        }

        PoliceShell_ShowFacilityDetail(_policeSelectedFacilityIndex);
        PoliceShell_AddStationRightActions();
    }

    private void PoliceShell_ShowFacilityDetail(int facilityIndex)
    {
        if (facilityIndex < 0 || facilityIndex >= GameSessionState.PoliceFacilities.Count)
        {
            _policeShell.CenterBody.text = "<i>Invalid selection.</i>";
            return;
        }

        var f = GameSessionState.PoliceFacilities[facilityIndex];
        if (f == null)
        {
            _policeShell.CenterBody.text = "<i>Missing data.</i>";
            return;
        }

        string name = string.IsNullOrWhiteSpace(f.DisplayName)
            ? (f.Type == GameSessionState.PoliceFacilityType.HQ ? "Police HQ" : "Precinct " + f.FacilityNumber)
            : f.DisplayName;

        int disc = Mathf.Clamp(f.DiscoveryLevel, 0, 5);
        int acc = Mathf.Clamp(f.AccessLevel, 0, 5);

        _policeShell.CenterBody.text =
            "<b>" + name + "</b>\n\n" +
            "<b>Known people & locations</b>\n" +
            "• Commander: <i>" + (disc >= 2 ? "Not wired to data yet" : "Unknown") + "</i>\n" +
            "• Location: <i>" + (disc >= 1 ? "Not wired to data yet" : "Undisclosed") + "</i>\n" +
            "• Areas of responsibility: <i>" + (disc >= 3 ? "Not wired to data yet" : "Partially undisclosed") + "</i>\n" +
            "• Discovery (knowledge of station): " + disc + "/5\n" +
            "• Access (approach / infiltration): " + acc + "/5\n" +
            "• Certainty: " + SafePoliceCertaintyLabel(ResolveFacilityCertainty(f)) + "\n" +
            (string.IsNullOrWhiteSpace(f.LastPublicMention)
                ? ""
                : "\n<b>Public mention</b>\n" + f.LastPublicMention.Trim() + "\n") +
            "\n<size=90%><i>Right column: operational moves vs this station — many need access and resources.</i></size>";
    }

    private void PoliceShell_BuildPersonnelOrgMode()
    {
        for (int b = 0; b < PolicePersonnelRankBuckets.Length; b++)
        {
            int capB = b;
            string label = PolicePersonnelRankBuckets[b];
            Button lb = CreateGovernmentShellRowButton(_policeShell.LeftContent, label);
            lb.onClick.AddListener(() =>
            {
                _policePersonnelBucketIndex = capB;
                PoliceShell_ShowPersonnelBucketCenter(capB);
                _policeShell.ClearRightActions();
                PoliceShell_AddPersonnelBucketRightActions();
            });
            _policeShell.LeftListButtons.Add(lb);
        }

        if (_policePersonnelBucketIndex < 0 || _policePersonnelBucketIndex >= PolicePersonnelRankBuckets.Length)
        {
            _policeShell.CenterBody.text =
                "<b>Personnel — org-wide</b>\n\n" +
                "<i>Pick a rank / role bucket on the left.</i>\n\n" +
                "<size=90%>The center will list collected intel (photos, home, shifts, ties) for people in that bucket. " +
                "Right: framed actions — intel, violence, kidnapping, bribery, threats, etc. (stub).</size>";
            AddPoliceShellAction("Broad police intel (stub)");
            return;
        }

        PoliceShell_ShowPersonnelBucketCenter(_policePersonnelBucketIndex);
        PoliceShell_AddPersonnelBucketRightActions();
    }

    private void PoliceShell_ShowPersonnelBucketCenter(int bucketIndex)
    {
        if (bucketIndex < 0 || bucketIndex >= PolicePersonnelRankBuckets.Length)
        {
            _policeShell.CenterBody.text = "<i>Invalid selection.</i>";
            return;
        }

        string bucket = PolicePersonnelRankBuckets[bucketIndex];
        _policeShell.CenterBody.text =
            "<b>" + bucket + "</b>\n\n" +
            "<b>Intel dossier (aggregate)</b>\n" +
            "• Identified people: <i>none yet — FOG</i>\n" +
            "• Photos / visual: <i>not collected</i>\n" +
            "• Residence / phone: <i>not collected</i>\n" +
            "• Shifts / routine: <i>not collected</i>\n" +
            "• Pressure points: <i>not collected</i>\n\n" +
            "<size=90%><i>Next step:</i> name list in center → pick an officer → full detail + targeted actions. " +
            "Right column stays in intel, violence, and soft coercion.</size>";
    }

    private void PoliceShell_BuildCasesMode()
    {
        Button b = CreateGovernmentShellRowButton(_policeShell.LeftContent, "Active interest (you)");
        b.onClick.AddListener(PoliceShell_ShowCasesBlotter);
        _policeShell.LeftListButtons.Add(b);

        PoliceShell_ShowCasesBlotter();
    }

    private void PoliceShell_ShowCasesBlotter()
    {
        string blotter = string.IsNullOrWhiteSpace(GameSessionState.LastPoliceInvestigationUpdate)
            ? "<i>No fresh police activity on your file.</i>"
            : GameSessionState.LastPoliceInvestigationUpdate.Trim();
        _policeShell.CenterBody.text =
            "<b>Known cases & operations</b>\n\n" +
            "• Blotter: " + blotter + "\n" +
            "• Certainty: " + SafePoliceCertaintyLabel(ResolveInvestigationCertainty()) + "\n\n" +
            "<size=90%>Cases and evidence are unified in one lane. " +
            "You only see what was discovered, leaked, or inferred.</size>";
        _policeShell.ClearRightActions();
        AddPoliceShellAction("Counter-intel");
        AddPoliceShellAction("Lawyer consult");
    }

    private void PoliceShell_BuildActionsMode()
    {
        Button b1 = CreateGovernmentShellRowButton(_policeShell.LeftContent, "Weakness map");
        b1.onClick.AddListener(PoliceShell_ShowActionsPressurePanel);
        _policeShell.LeftListButtons.Add(b1);

        Button b2 = CreateGovernmentShellRowButton(_policeShell.LeftContent, "Opportunity board");
        b2.onClick.AddListener(PoliceShell_ShowActionsEvidencePanel);
        _policeShell.LeftListButtons.Add(b2);

        PoliceShell_ShowActionsPressurePanel();
    }

    private void PoliceShell_ShowActionsPressurePanel()
    {
        _policeShell.CenterBody.text =
            "<b>Known weaknesses</b>\n\n" +
            "• Station overload: " + (GameSessionState.PolicePressureDisplayValue() >= 55 ? "Estimated" : "Rumor") + "\n" +
            "• Thin shifts: " + (GameSessionState.StreetStopRiskDisplayValue() >= 45 ? "Revealed" : "Estimated") + "\n" +
            "• Internal coordination friction: Estimated\n" +
            "• Vulnerable witnesses / chain points: Rumor\n\n" +
            "<size=90%>Weakness lane is external knowledge only, not police internal certainty.</size>";
        _policeShell.ClearRightActions();
        AddPoliceShellAction("Search bribable officer", HasDiscoveredPoliceOfficer(), "no identified officer");
        AddPoliceShellAction("Probe weak station shift", HasDiscoveredPoliceFacility(), "no known station");
    }

    private void PoliceShell_ShowActionsEvidencePanel()
    {
        _policeShell.CenterBody.text =
            "<b>Known opportunities</b>\n\n" +
            "• Case file theft window: " + (CanRunDocumentTheft() ? "Estimated" : "Rumor") + "\n" +
            "• Political pressure channel: Estimated\n" +
            "• Patrol ambush exposure: " + (GameSessionState.StreetStopRiskDisplayValue() >= 50 ? "Revealed" : "Estimated") + "\n\n" +
            "<size=90%>Every opportunity raises both potential gains and trace risk.</size>";
        _policeShell.ClearRightActions();
        AddPoliceShellAction("Steal document", CanRunDocumentTheft(), "insufficient intel or crew");
        AddPoliceShellAction("Plant disinformation", CanRunDisinformationAction(), "intel network too weak");
    }

    private void PoliceShell_BuildAvailableActionsMode()
    {
        _policeShell.CenterBody.text =
            "<b>Available actions</b>\n\n" +
            "Actions unlock only when opening conditions are met.\n\n" +
            BuildPoliceActionAvailabilityBoard();
        _policeShell.ClearRightActions();
        var defs = BuildPoliceExternalActionDefinitions();
        for (int i = 0; i < defs.Count; i++)
            AddPoliceShellAction(defs[i].label, defs[i].available, defs[i].unavailableReason);
    }

    private void PoliceShell_BuildOutcomeLogMode()
    {
        int inTransit = 0;
        int delayed = 0;
        int incidents = PoliceWorldState.LogIncidents != null ? PoliceWorldState.LogIncidents.Count : 0;
        if (PoliceWorldState.LogShipments != null)
        {
            for (int i = 0; i < PoliceWorldState.LogShipments.Count; i++)
            {
                var s = PoliceWorldState.LogShipments[i];
                if (s == null) continue;
                if (s.status == LogisticsShipmentStatus.InTransit) inTransit++;
                if (s.status == LogisticsShipmentStatus.Delayed) delayed++;
            }
        }
        PoliceIntelCertainty certainty = ResolveInvestigationCertainty();
        _policeShell.CenterBody.text =
            "<b>LOG (Logistics)</b>\n\n" +
            "• Active shipments: " + inTransit + "\n" +
            "• Delayed shipments: " + delayed + "\n" +
            "• Logistics incidents: " + incidents + "\n\n" +
            "• Last police update: " +
            (string.IsNullOrWhiteSpace(GameSessionState.LastPoliceInvestigationUpdate)
                ? "No confirmed outcome yet."
                : GameSessionState.LastPoliceInvestigationUpdate.Trim()) + "\n" +
            "• Certainty: " + SafePoliceCertaintyLabel(certainty) + "\n\n" +
            "<b>Outcome classes</b>\n" +
            "• Success\n" +
            "• PartialSuccess\n" +
            "• SilentFailure\n" +
            "• LoudFailure\n\n" +
            "<size=90%>LOG is logistics, not records journal. REC will hold history/audit trails.</size>";
        _policeShell.ClearRightActions();
        AddPoliceShellAction("Review delayed shipment");
    }

    private List<PoliceExternalActionDefinition> BuildPoliceExternalActionDefinitions()
    {
        bool hasStation = HasDiscoveredPoliceFacility();
        bool hasOfficer = HasDiscoveredPoliceOfficer();
        bool hasCrew = PersonnelRegistry.Members != null && PersonnelRegistry.Members.Count > 0;
        bool hasBudget = GameSessionState.CrewCash >= 100;
        bool hasIntel = GameSessionState.PlayerIntelNetworkRating >= 15;
        return new List<PoliceExternalActionDefinition>
        {
            new PoliceExternalActionDefinition { actionId = "BribeOfficer", label = "BribeOfficer", available = hasOfficer && hasBudget && hasCrew, unavailableReason = "needs identified officer + budget + crew" },
            new PoliceExternalActionDefinition { actionId = "BlackmailOfficer", label = "BlackmailOfficer", available = hasOfficer && hasIntel, unavailableReason = "needs identified officer + pressure intel" },
            new PoliceExternalActionDefinition { actionId = "RecruitPoliceSource", label = "RecruitPoliceSource", available = hasOfficer && hasCrew, unavailableReason = "needs identified officer + free handler" },
            new PoliceExternalActionDefinition { actionId = "StealCaseFile", label = "StealCaseFile", available = CanRunDocumentTheft(), unavailableReason = "needs station access + field crew" },
            new PoliceExternalActionDefinition { actionId = "DisruptInvestigation", label = "DisruptInvestigation", available = hasStation && hasCrew, unavailableReason = "needs known target operation" },
            new PoliceExternalActionDefinition { actionId = "RemoveEvidence", label = "RemoveEvidence", available = hasStation && hasIntel, unavailableReason = "needs evidence lane intel" },
            new PoliceExternalActionDefinition { actionId = "ThreatenWitness", label = "ThreatenWitness", available = hasIntel && hasCrew, unavailableReason = "needs witness intelligence" },
            new PoliceExternalActionDefinition { actionId = "TrackOfficer", label = "TrackOfficer", available = hasOfficer && hasCrew, unavailableReason = "needs identified officer + tails team" },
            new PoliceExternalActionDefinition { actionId = "AttackPatrol", label = "AttackPatrol", available = hasStation && hasCrew, unavailableReason = "needs known patrol route" },
            new PoliceExternalActionDefinition { actionId = "SpreadDisinformation", label = "SpreadDisinformation", available = CanRunDisinformationAction(), unavailableReason = "intel network too weak" },
            new PoliceExternalActionDefinition { actionId = "UsePoliticalConnection", label = "UsePoliticalConnection", available = hasBudget, unavailableReason = "needs political budget" }
        };
    }

    private string BuildPoliceActionAvailabilityBoard()
    {
        var defs = BuildPoliceExternalActionDefinitions();
        int available = 0;
        for (int i = 0; i < defs.Count; i++)
        {
            if (defs[i].available)
                available++;
        }
        int blocked = defs.Count - available;
        return
            "• Available now: " + available + "\n" +
            "• Locked: " + blocked + "\n" +
            "• KnowledgeGain / ExposureRisk / SuspicionGain apply to each action\n" +
            "• External data never bypasses discovery rules";
    }

    private bool HasDiscoveredPoliceFacility()
    {
        for (int i = 0; i < GameSessionState.PoliceFacilities.Count; i++)
        {
            var f = GameSessionState.PoliceFacilities[i];
            if (f != null && f.DiscoveryLevel > 0)
                return true;
        }
        return false;
    }

    private bool HasDiscoveredPoliceOfficer()
    {
        for (int i = 0; i < GameSessionState.PoliceFacilities.Count; i++)
        {
            var f = GameSessionState.PoliceFacilities[i];
            if (f != null && f.DiscoveryLevel >= 2)
                return true;
        }
        return false;
    }

    private bool CanRunDocumentTheft()
    {
        return HasDiscoveredPoliceFacility()
               && PersonnelRegistry.Members != null
               && PersonnelRegistry.Members.Count >= 2
               && GameSessionState.PlayerIntelNetworkRating >= 10;
    }

    private bool CanRunDisinformationAction()
    {
        return GameSessionState.PlayerIntelNetworkRating >= 25 && GameSessionState.CrewCash >= 50;
    }

    private PoliceIntelCertainty ResolveFacilityCertainty(GameSessionState.PoliceFacilityRecord f)
    {
        if (f == null) return PoliceIntelCertainty.Rumor;
        if (f.DiscoveryLevel >= 3) return PoliceIntelCertainty.Revealed;
        if (f.DiscoveryLevel >= 1) return PoliceIntelCertainty.Estimated;
        return PoliceIntelCertainty.Rumor;
    }

    private PoliceIntelCertainty ResolveInvestigationCertainty()
    {
        if (!string.IsNullOrWhiteSpace(GameSessionState.LastPoliceInvestigationUpdate))
            return PoliceIntelCertainty.Revealed;
        if (GameSessionState.PlayerIntelNetworkRating >= 25)
            return PoliceIntelCertainty.Estimated;
        return PoliceIntelCertainty.Rumor;
    }

    private string SafePoliceCertaintyLabel(PoliceIntelCertainty c)
    {
        if (c == PoliceIntelCertainty.Rumor) return "Rumor";
        if (c == PoliceIntelCertainty.Estimated) return "Estimated";
        return "Revealed";
    }

    private static int CountIncarceratedCrewMembers()
    {
        int count = 0;
        for (int i = 0; i < PersonnelRegistry.Members.Count; i++)
        {
            CrewMember member = PersonnelRegistry.Members[i];
            if (member != null && CharacterStatusUtility.IsIncarcerated(member.GetResolvedStatus()))
                count++;
        }
        return count;
    }

    private void EnsurePrisonGlowVisual()
    {
        if (_prisonInstitutionButton == null)
            return;
        Transform old = _prisonInstitutionButton.transform.Find("PrisonGlow");
        if (old != null)
            Destroy(old.gameObject);

        GameObject glow = new GameObject("PrisonGlow");
        glow.transform.SetParent(_prisonInstitutionButton.transform, false);
        glow.transform.SetSiblingIndex(0);
        _prisonInstitutionGlowRt = glow.AddComponent<RectTransform>();
        _prisonInstitutionGlowRt.anchorMin = new Vector2(0.5f, 0.5f);
        _prisonInstitutionGlowRt.anchorMax = new Vector2(0.5f, 0.5f);
        _prisonInstitutionGlowRt.pivot = new Vector2(0.5f, 0.5f);
        _prisonInstitutionGlowRt.sizeDelta = new Vector2(InstitutionButtonDiameter + 8f, InstitutionButtonDiameter + 8f);
        _prisonInstitutionGlow = glow.AddComponent<Image>();
        _prisonInstitutionGlow.sprite = GetPlanningCircleSprite();
        _prisonInstitutionGlow.preserveAspect = true;
        _prisonInstitutionGlow.raycastTarget = false;
        _prisonInstitutionGlow.color = new Color(1f, 0.45f, 0.08f, 0f);
    }

    private void RefreshPrisonAlertPulse()
    {
        int imprisonedCount = CountIncarceratedCrewMembers();
        HashSet<string> currentImprisoned = new HashSet<string>();
        bool newImprisonedDetected = false;
        for (int i = 0; i < PersonnelRegistry.Members.Count; i++)
        {
            CrewMember member = PersonnelRegistry.Members[i];
            if (member == null || !CharacterStatusUtility.IsIncarcerated(member.GetResolvedStatus()))
                continue;
            string key = string.IsNullOrWhiteSpace(member.Name) ? ("#" + i) : member.Name.Trim();
            currentImprisoned.Add(key);
            if (!_knownImprisonedMembers.Contains(key))
                newImprisonedDetected = true;
        }

        if (newImprisonedDetected)
        {
            _prisonAlertAcknowledged = false;
            _prisonAlertActive = true;
            _prisonAlertStartedWeek = GameSessionState.CurrentDay;
        }

        if (imprisonedCount <= 0)
        {
            _prisonAlertAcknowledged = true;
            _prisonAlertActive = false;
            _prisonAlertStartedWeek = -1;
        }

        _knownImprisonedMembers.Clear();
        foreach (string k in currentImprisoned)
            _knownImprisonedMembers.Add(k);
        _lastKnownImprisonedCount = imprisonedCount;

        // Alert persists until player opens Prison OR next turn passes.
        if (_prisonAlertActive && _prisonAlertStartedWeek >= 0 && GameSessionState.CurrentDay > _prisonAlertStartedWeek)
        {
            _prisonAlertAcknowledged = true;
            _prisonAlertActive = false;
        }
        if (_prisonAlertAcknowledged)
            _prisonAlertActive = false;

        bool shouldPulse = imprisonedCount > 0 && _prisonAlertActive;
        if (_prisonInstitutionOutline != null)
        {
            if (shouldPulse)
            {
                float t = (Mathf.Sin(Time.unscaledTime * 4.2f) + 1f) * 0.5f;
                _prisonInstitutionOutline.effectColor = Color.Lerp(
                    new Color(0.95f, 0.38f, 0.04f, 0.42f),
                    new Color(1f, 0.72f, 0.22f, 0.95f),
                    t);
                float d = Mathf.Lerp(1.2f, 2.2f, t);
                _prisonInstitutionOutline.effectDistance = new Vector2(d, -d);
            }
            else
            {
                _prisonInstitutionOutline.effectColor = new Color(0.08f, 0.09f, 0.12f, 0.85f);
                _prisonInstitutionOutline.effectDistance = Vector2.zero;
            }
        }

        if (_prisonInstitutionCircle != null)
        {
            if (shouldPulse)
            {
                float t = (Mathf.Sin(Time.unscaledTime * 4.2f) + 1f) * 0.5f;
                _prisonInstitutionCircle.color = Color.Lerp(
                    new Color(0.14f, 0.15f, 0.18f, 1f),
                    new Color(0.32f, 0.2f, 0.12f, 1f),
                    t);
            }
            else
            {
                _prisonInstitutionCircle.color = new Color(0.14f, 0.15f, 0.18f, 1f);
            }
        }

        if (_prisonInstitutionGlow != null && _prisonInstitutionGlowRt != null)
        {
            if (shouldPulse)
            {
                float t = (Mathf.Sin(Time.unscaledTime * 4.2f) + 1f) * 0.5f;
                _prisonInstitutionGlow.color = new Color(1f, 0.48f, 0.08f, Mathf.Lerp(0.14f, 0.32f, t));
                float s = Mathf.Lerp(1.02f, 1.12f, t);
                _prisonInstitutionGlowRt.localScale = new Vector3(s, s, 1f);
            }
            else
            {
                _prisonInstitutionGlow.color = new Color(1f, 0.48f, 0.08f, 0f);
                _prisonInstitutionGlowRt.localScale = Vector3.one;
            }
        }
    }

    private static TextMeshProUGUI CreateTmp(Transform parent, string name, float fontSize, FontStyles style)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = Vector2.zero;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = PlanningUiButtonStyle.LabelPrimary;
        tmp.richText = true;
        return tmp;
    }

    private static void RegisterDraggableModalTitle(GameObject titleGo, RectTransform panelToMove)
    {
        if (titleGo == null || panelToMove == null)
            return;
        UiDraggablePanelHeader drag = titleGo.GetComponent<UiDraggablePanelHeader>();
        if (drag == null)
            drag = titleGo.AddComponent<UiDraggablePanelHeader>();
        drag.Initialize(panelToMove);
        TextMeshProUGUI tmp = titleGo.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
            tmp.raycastTarget = true;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    private static void CopyRectTransform(RectTransform src, RectTransform dst)
    {
        if (src == null || dst == null)
            return;
        dst.anchorMin = src.anchorMin;
        dst.anchorMax = src.anchorMax;
        dst.pivot = src.pivot;
        dst.anchoredPosition = src.anchoredPosition;
        dst.sizeDelta = src.sizeDelta;
        dst.localScale = src.localScale;
        dst.localRotation = src.localRotation;
    }

    /// <summary>Keeps Ops overlay aligned with MainArea (resize / safe area) and reapplies optional insets.</summary>
    private void EnsureOpsOverlayMatchesMainArea()
    {
        if (_opsStageOverlayRoot == null)
            return;
        RectTransform mainArea = GameObject.Find("MainArea")?.GetComponent<RectTransform>();
        RectTransform overlayRt = _opsStageOverlayRoot.GetComponent<RectTransform>();
        if (mainArea == null || overlayRt == null)
            return;

        if (overlayRt.parent != mainArea)
            overlayRt.SetParent(mainArea, false);

        StretchFull(overlayRt);
        overlayRt.SetAsLastSibling();

        if (_opsStagePanelRt != null)
        {
            _opsStagePanelRt.anchorMin = Vector2.zero;
            _opsStagePanelRt.anchorMax = Vector2.one;
            _opsStagePanelRt.offsetMin = new Vector2(_opsPanelSafeInsetLeft, _opsPanelSafeInsetBottom);
            _opsStagePanelRt.offsetMax = new Vector2(-_opsPanelSafeInsetRight, -_opsPanelSafeInsetTop);
        }
    }

    private void WireTabButton(GameObject go, PlanningTabId tab)
    {
        if (go == null)
            return;

        Button button = go.GetComponent<Button>();
        if (button == null)
            button = go.AddComponent<Button>();

        if (button.targetGraphic == null)
        {
            Image img = go.GetComponent<Image>();
            if (img != null)
                button.targetGraphic = img;
            else
            {
                TextMeshProUGUI tmp = go.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null)
                    button.targetGraphic = tmp;
            }
        }

        button.transition = Selectable.Transition.ColorTint;
        PlanningTabId captured = tab;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => ShowTab(captured));
    }

    public void ShowTab(PlanningTabId tab)
    {
        _current = tab;
        if (_personnelMemberListRoot != null)
            _personnelMemberListRoot.gameObject.SetActive(false);
        if (tab != PlanningTabId.Operations)
        {
            _opsMapFocusedLotId = -1;
            _opsLastMapPaintedFocusLotId = int.MinValue;
            _opsCenterMapSelectedBlockId = -1;
            _opsLastMacroPaintedBlockId = int.MinValue;
        }

        ApplyPlanningTabChromeVisibility(tab);

        // Restore tri-pane chrome (News hides the whole left column so the paper is screen-centered).
        if (_leftColumnRoot != null)
            _leftColumnRoot.gameObject.SetActive(true);

        // Default: show the legacy center text unless a tab explicitly uses a custom root.
        if (_newsPaperRoot != null)
            _newsPaperRoot.SetActive(false);
        if (_centerText != null)
            _centerText.gameObject.SetActive(true);
        if (_leftText != null)
            _leftText.gameObject.SetActive(true);
        if (_rightText != null)
            _rightText.gameObject.SetActive(true);
        if (_centerScrollViewportImage != null)
            _centerScrollViewportImage.color = new Color(0f, 0f, 0f, 0.12f);

        switch (tab)
        {
            case PlanningTabId.Overview:
                _titleText.text = "Overview — your empire";
                _contextText.text = "Campaign day " + GameSessionState.CurrentDay + "\n" + GameCalendarSystem.FormatPlanningHudLine(GameSessionState.CurrentDay);
                _leftText.text = BuildOverviewLeftPanel();
                _centerText.text = BuildOverviewCenterPanel();
                _rightText.text = BuildOverviewRightPanel();
                break;

            case PlanningTabId.News:
                _titleText.text = "News — city narrative";
                _contextText.text = "What you want the city to believe · headlines · leaks · pressure on press";
                _leftText.text = "<b>Sections</b>\n• Front page\n• District\n• Police blotter";

                // Safety: if the newspaper UI didn't build (domain reload / ordering), build it now.
                if (_newsPaperRoot == null)
                {
                    if (_centerText != null && _centerText.transform != null)
                    {
                        // CenterText -> Content -> Viewport -> ScrollView
                        if (_centerScrollContentRoot == null)
                            _centerScrollContentRoot = _centerText.transform.parent;
                        if (_centerScrollViewportRoot == null && _centerText.transform.parent != null && _centerText.transform.parent.parent != null)
                            _centerScrollViewportRoot = _centerText.transform.parent.parent;
                        if (_centerScrollViewportImage == null && _centerScrollViewportRoot != null)
                            _centerScrollViewportImage = _centerScrollViewportRoot.GetComponent<Image>();
                        if (_centerScrollRect == null && _centerScrollViewportRoot != null && _centerScrollViewportRoot.parent != null)
                            _centerScrollRect = _centerScrollViewportRoot.parent.GetComponent<ScrollRect>();
                    }
                    BuildNewsNewspaperUi();
                }
                if (_newsPaperRoot != null)
                    _newsPaperRoot.SetActive(true);
                if (_centerText != null)
                    _centerText.gameObject.SetActive(false);

                // Newspaper: hide side columns entirely so the center strip (and paper) spans symmetrically.
                if (_leftColumnRoot != null)
                    _leftColumnRoot.gameObject.SetActive(false);
                if (_leftText != null)
                    _leftText.gameObject.SetActive(false);
                if (_rightText != null)
                    _rightText.gameObject.SetActive(false);
                if (_centerScrollViewportImage != null)
                    _centerScrollViewportImage.color = new Color(0f, 0f, 0f, 0f);
                if (_newsPaperBackground != null)
                    _newsPaperBackground.color = new Color(0.95f, 0.94f, 0.90f, 0.99f); // warm newspaper paper

                RefreshNewsNewspaper();
                if (_newsPaperRoot != null)
                    _newsPaperRoot.transform.SetAsLastSibling();
                if (_centerScrollViewportRoot != null && _centerScrollViewportRoot.parent is RectTransform scrollHostRt)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(scrollHostRt);
                if (_centerScrollRect != null)
                {
                    _centerScrollRect.verticalNormalizedPosition = 1f;
                    _centerScrollRect.movementType = ScrollRect.MovementType.Clamped;
                }
                // Right column hidden on this tab; alerts can be brought into the paper later.
                break;

            case PlanningTabId.Intelligence:
                _titleText.text = "Intelligence — wires & rumors";
                _contextText.text = BuildIntelContextLine();
                _leftText.text = BuildIntelLeftPanel();
                _centerText.text = BuildIntelCenterPanel();
                _rightText.text = BuildIntelRightPanel();
                break;

            case PlanningTabId.Personnel:
                if (PlayerRunState.Character != null)
                    PersonnelRegistry.SyncBossSlotFromProfileAndCustody(PlayerRunState.Character);
                TryRecoverPlanningShellReferencesFromHierarchy();
                _titleText.text = "Personnel — crew & hierarchy";
                _contextText.text = PersonnelRegistry.BuildContextStripSummary();
                if (_leftText != null)
                    _leftText.gameObject.SetActive(false);
                EnsurePersonnelMemberListRootExists();
                if (_personnelMemberListRoot != null)
                {
                    _personnelMemberListRoot.gameObject.SetActive(true);
                    int n = PersonnelRegistry.Members != null ? PersonnelRegistry.Members.Count : 0;
                    if (_personnelSelectedMemberIndex < 0 || _personnelSelectedMemberIndex >= n)
                        _personnelSelectedMemberIndex = 0;
                    RebuildPersonnelMemberList();
                }
                else
                {
                    Debug.LogWarning(
                        "[PlanningShell] Personnel tab: LeftColumn / PersonnelMemberList missing. " +
                        "Ensure one PlanningShellController runs BuildUi (MainArea → PlanningShell) or check for duplicate shell controllers.");
                    if (_leftText != null)
                    {
                        _leftText.gameObject.SetActive(true);
                        _leftText.text = PersonnelRegistry.BuildRosterNamesColumn();
                    }
                }

                RefreshPersonnelCenterPanel();
                if (_leftColumnRoot is RectTransform lcr)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(lcr);

                _rightText.text = PersonnelRegistry.BuildRightPanelStub();
                break;

            case PlanningTabId.Operations:
                GameSessionState.SyncScoutToOrderedOps();
                UpdateAllOpButtonLabels();
                RefreshOpsDashboard();
                RebuildOpsCityMapIfNeeded();
                break;

            case PlanningTabId.Diplomacy:
            {
                string warRisk = GameSessionState.UnderworldWarDeclaredOnPlayerFamily
                    ? "<color=#FF8A7A>High — open war</color>"
                    : "Low";
                _titleText.text = "Diplomacy — factions & deals";
                _contextText.text = "Treaties · tribute · truces · back channels";
                _leftText.text = "<b>Factions</b>\n• City hall\n• Unions\n• Rivals";
                _centerText.text =
                    "<b>Relationship board</b>\n\n" +
                    "Rows are factions; columns track <b>standing</b>, <b>tribute due</b>, and <b>pending offers</b> " +
                    "(protection, territory swaps, hit stand-downs). Back-channel slots unlock with intel.\n\n" +
                    "<size=92%>Green is workable; red means shots are already flying — check Ops for reprisals.</size>";
                _rightText.text =
                    "<b>Standing</b>\n" +
                    "• Active deals: 0\n" +
                    "• War risk: " + warRisk + "\n" +
                    "• Street heat: " + GameSessionState.FormatStreetStopRiskLabel();
                break;
            }

            case PlanningTabId.Business:
                _titleText.text = "Business — fronts & off-books";
                _contextText.text = "Legit fronts · gray revenue · exposure and audits";
                _leftText.text = "<b>Legal</b>\n• Restaurants\n• Garages\n\n<b>Off-books</b>\n• Street tax\n• Gaming";
                _centerText.text =
                    "<b>P&L overlay</b>\n\n" +
                    "Legit storefronts carry payroll, suppliers, and licenses; off-books lines show street pulls, gaming, " +
                    "and protection skim. <b>Exposure</b> rises when auditors cross paths with crews carrying cash.\n\n" +
                    "<size=92%>Tie clean revenue to laundering windows; spike audit risk pulls tax eyes.</size>";
                _rightText.text =
                    "<b>Snapshot</b>\n" +
                    "• Legit weekly (fronts): — assign in Ops\n" +
                    "• Audit pressure: Low\n" +
                    "• Police interest: " + GameSessionState.FormatPolicePressureLabel() + "\n" +
                    "• Street stop risk: " + GameSessionState.FormatStreetStopRiskLabel() + "\n" +
                    "• Dirty float: " + GameSessionState.FormatBlackCashDisplay();
                break;

            case PlanningTabId.Logistics:
                _titleText.text = "Logistics — routes & stash";
                _contextText.text = "Warehousing · transport · supply for crews and fronts";
                _leftText.text = "<b>Nodes</b>\n• Garages\n• Warehouses\n• Drops";
                _centerText.text =
                    "<b>Supply graph</b>\n\n" +
                    "Nodes hold <b>capacity</b> (cold storage, vehicles, cash rooms). Edges are routes with travel time, " +
                    "tolls, and <b>interdiction</b> odds from police activity and rival hijacks.\n\n" +
                    "<size=92%>Queue convoys from Ops; empty legs burn money and attention.</size>";
                _rightText.text =
                    "<b>Throughput</b>\n" +
                    "• Weekly manifest: — (queue Ops runs)\n" +
                    "• Seizure risk: Low\n" +
                    "• Police interest: " + GameSessionState.FormatPolicePressureLabel();
                break;

            case PlanningTabId.Legal:
                _titleText.text = "Legal — city codex";
                _contextText.text = "Rulebook · chapters & sections · penalty ranges (min–max)";
                // Book is the primary button; keep the old text button hidden (fallback only).
                if (_legalCodexToggleButton != null)
                    _legalCodexToggleButton.gameObject.SetActive(false);
                if (_legalLeftTopSpacer != null)
                    _legalLeftTopSpacer.gameObject.SetActive(true);
                if (_legalLeftBottomSpacer != null)
                    _legalLeftBottomSpacer.gameObject.SetActive(true);
                RefreshLegalCodexBookIcon();
                UpdateLegalCodexUiIfNeeded();
                break;

            case PlanningTabId.Finance:
                _titleText.text = "Finance — books & laundry";
                _contextText.text = "Cash · laundering · investments · debt service";
                _leftText.text = "<b>Ledgers</b>\n• Operating\n• Laundry\n• War chest";
                _centerText.text =
                    "<b>Treasury</b>\n\n" +
                    "Operating covers payroll and street pulls; laundry tracks wash cycles and fee drag; war chest holds " +
                    "long-term binds and debt service. Monthly <b>burn</b> stacks payroll, interest, and protection costs.\n\n" +
                    "<size=92%>Clean accounts live here; dirty cash stays in metrics until washed.</size>";
                _rightText.text =
                    "<b>Balances</b>\n" +
                    "• Accounts (clean): " + GameSessionState.FormatCrewCashDisplay() + "\n" +
                    "• Cash in hand (dirty): " + GameSessionState.FormatBlackCashDisplay() + "\n" +
                    "• Runway: — (set payroll & debt in ledgers)\n" +
                    "• Audit exposure: Low";
                break;

            default:
                _titleText.text = tab.ToString();
                _contextText.text = "Tab not wired yet.";
                _leftText.text = "";
                _centerText.text = "";
                _rightText.text = "";
                break;
        }

        if (tab != PlanningTabId.Legal)
        {
            if (_legalCodexToggleButton != null)
                _legalCodexToggleButton.gameObject.SetActive(false);
            if (_legalLeftTopSpacer != null)
                _legalLeftTopSpacer.gameObject.SetActive(false);
            if (_legalLeftBottomSpacer != null)
                _legalLeftBottomSpacer.gameObject.SetActive(false);
            if (_legalCodexBookImage != null)
                _legalCodexBookImage.gameObject.SetActive(false);
            if (_legalCodexBookCaptionText != null)
                _legalCodexBookCaptionText.gameObject.SetActive(false);
        }

        // Refresh top bar metrics (value -> icon).
        UpdateTopBarMetrics();

        RefreshMissionRow();
        ApplyTabSelectionVisual(tab);

        if (tab == PlanningTabId.Operations)
            StartCoroutine(DelayedOpsMapScaleAfterLayout());
    }

    private System.Collections.IEnumerator DelayedOpsMapScaleAfterLayout()
    {
        yield return null;
        ApplyOpsMapScaleToFitCityGen();
    }

    private void UpdateTopBarMetrics()
    {
        if (!_metricBarIconsReloadAttempted)
        {
            _metricBarIconsReloadAttempted = true;
            TryReloadMetricBarIconIfEmpty(_metricDirtyCashIcon, "UI/Metrics/dirty_cash");
            TryReloadMetricBarIconIfEmpty(_metricAccountCashIcon, "UI/Metrics/account_cash");
            TryReloadMetricBarIconIfEmpty(_metricFamilyRepIcon, "UI/Metrics/family_rep");
            TryReloadMetricBarIconIfEmpty(_metricCrewMoraleIcon, "UI/Metrics/crew_morale");
        }

        if (_metricDirtyCashText != null)
            _metricDirtyCashText.text = "$" + GameSessionState.BlackCash.ToString("N0");

        if (_metricAccountCashText != null)
        {
            int bank = GameSessionState.CrewCash;
            _metricAccountCashText.text = bank > 0 ? ("$" + bank.ToString("N0")) : "-";
        }

        if (_metricFamilyRepText != null)
        {
            int rep = PlayerRunState.Character?.PublicReputation ?? 0;
            _metricFamilyRepText.text = rep >= 0 ? ("+" + rep.ToString("N0")) : rep.ToString("N0");
        }
        if (_metricCrewMoraleText != null)
            _metricCrewMoraleText.text = ComputeCrewMoraleLabel();
    }

    private string ComputeCrewMoraleLabel()
    {
        int total = PersonnelRegistry.Members.Count;
        if (total <= 0)
            return "—";

        int highCount = 0;
        int lowCount = 0;

        for (int i = 0; i < PersonnelRegistry.Members.Count; i++)
        {
            CrewMember m = PersonnelRegistry.Members[i];
            if (m == null)
                continue;
            var s = m.Satisfaction;
            if (s == CrewSatisfactionLevel.VerySatisfied || s == CrewSatisfactionLevel.Satisfied)
                highCount++;
            else if (s == CrewSatisfactionLevel.VeryUnsatisfied || s == CrewSatisfactionLevel.Unsatisfied)
                lowCount++;
        }

        float highRatio = highCount / (float)total;
        float lowRatio = lowCount / (float)total;

        if (highRatio >= 0.6f)
            return "High";
        if (lowRatio >= 0.45f)
            return "Low";
        return "Medium";
    }

    private void RefreshMissionRow()
    {
        if (_missionRow == null)
            return;
        bool ops = _current == PlanningTabId.Operations;
        _missionRow.SetActive(ops);
        if (!ops || _missionQueueOrderStrip == null)
        {
            if (_missionQueueOrderStrip != null)
                _missionQueueOrderStrip.SetActive(false);
            return;
        }

        RebuildMissionQueueOrderStrip();
    }

    private string BuildOverviewLeftPanel()
    {
        string bossName = PlayerRunState.Character?.DisplayName ?? "Boss";
        int crewCount = PersonnelRegistry.Members != null ? PersonnelRegistry.Members.Count : 0;
        return "<b>Boss</b>\n• " + bossName +
            "\n\n<b>Crew</b>\n• $" + GameSessionState.CrewCash + " on hand\n• " + crewCount + " members";
    }

    private string BuildOverviewCenterPanel()
    {
        var rep = GetLatestFederalDailyReport();
        var intelEntries = GetPlayerVisibleIntelEntries();
        string text = "<b>Last day summary</b>\n\n";
        if (rep == null)
        {
            text += "No federal daily report yet.\nAdvance one day to generate operational summary.";
        }
        else
        {
            text += "• Bureau strategy: " + SafeDailyStrategyLabel(rep.selectedStrategyInt) + "\n";
            text += "• Federal interest: " + rep.federalInterestScore + "/100\n";
            text += "• Requests generated: " + (rep.generatedRequestIds != null ? rep.generatedRequestIds.Count : 0) + "\n";
            text += "• Operations completed: " + (rep.completedOperationIds != null ? rep.completedOperationIds.Count : 0) + "\n";
            text += "• New federal cases: " + (rep.newFederalCaseIds != null ? rep.newFederalCaseIds.Count : 0) + "\n";
            text += "• Updated federal cases: " + (rep.updatedFederalCaseIds != null ? rep.updatedFederalCaseIds.Count : 0) + "\n";
            if (!string.IsNullOrEmpty(rep.notes))
                text += "\n<b>Field notes:</b>\n" + rep.notes.Trim();
        }

        int imprisonedCount = CountIncarceratedCrewMembers();
        if (imprisonedCount > 0)
        {
            text += "\n\n<b>Prison alert:</b> " + imprisonedCount +
                " crew member" + (imprisonedCount == 1 ? "" : "s") +
                " in custody or sentenced.";
        }
        if (GameSessionState.UnderworldWarDeclaredOnPlayerFamily)
        {
            text += "\n\n<b>Open war:</b> rival crime families declared war on your organization.";
        }
        int shared = 0;
        int compartment = 0;
        for (int i = 0; i < intelEntries.Count; i++)
        {
            if (intelEntries[i].scope == IntelKnowledgeScope.Shared) shared++;
            else compartment++;
        }
        text += "\n\n<b>Intel dissemination</b>\n" +
            "• Shared: " + shared + "\n" +
            "• Compartment: " + compartment;
        return text;
    }

    private string BuildOverviewRightPanel()
    {
        string detained = string.IsNullOrEmpty(GameSessionState.InitialDetainedCharacterName)
            ? "None"
            : GameSessionState.InitialDetainedCharacterName;
        var rep = GetLatestFederalDailyReport();
        string exposure = rep == null ? "—" : (rep.exposureChange >= 0 ? "+" + rep.exposureChange : rep.exposureChange.ToString());
        string pressure = rep == null ? "—" : (rep.politicalPressureChange >= 0 ? "+" + rep.politicalPressureChange : rep.politicalPressureChange.ToString());
        return
            "<b>Quick status</b>\n" +
            "• Cash: $" + GameSessionState.CrewCash + "\n" +
            "• Campaign day: " + GameSessionState.CurrentDay + "\n" +
            "• Police pressure: " + GameSessionState.FormatPolicePressureLabel() + " (" + GameSessionState.PolicePressureDisplayValue() + "/100)\n" +
            "• Street stop risk: <b>" + GameSessionState.FormatStreetStopRiskLabel() + "</b> (" + GameSessionState.StreetStopRiskDisplayValue() + "/100)\n" +
            "• Federal exposure delta: " + exposure + "\n" +
            "• Political pressure delta: " + pressure + "\n" +
            "• Detained: " + detained +
            (GameSessionState.BossIsPoliceInformant ? "\n• <color=#E8C96A>Boss: Police informant</color>" : "") +
            (GameSessionState.BossSnitchKnownToRivalGangs ? "\n• <color=#FF6B5C>Boss: Snitch</color>" : "");
    }

    private string BuildIntelContextLine()
    {
        int known = GetPlayerVisibleIntelEntries().Count;
        return "Field intel known to your people: " + known + " item" + (known == 1 ? "" : "s");
    }

    private string BuildIntelLeftPanel()
    {
        var entries = GetPlayerVisibleIntelEntries();
        int visible = entries.Count;
        int verified = 0;
        int urgent = 0;
        int shared = 0;
        int compartment = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            var item = entries[i].item;
            if (item.verificationStatusInt >= (int)FederalIntelVerificationStatus.Corroborated) verified++;
            if (item.actionabilityInt >= (int)FederalIntelActionability.PrepareOperation) urgent++;
            if (entries[i].scope == IntelKnowledgeScope.Shared) shared++;
            else compartment++;
        }
        return "<b>Intel feeds (known)</b>\n" +
            "• Visible items: " + visible + "\n" +
            "• Corroborated+: " + verified + "\n" +
            "• Urgent leads: " + urgent + "\n" +
            "• Shared: " + shared + "\n" +
            "• Compartment: " + compartment + "\n\n" +
            "<size=92%>Player sees only what own network knows.</size>";
    }

    private string BuildIntelCenterPanel()
    {
        var entries = GetPlayerVisibleIntelEntries();
        if (entries.Count == 0)
            return "<b>Intel desk</b>\n\nNo known actionable intel right now.\nBuild network, pay sources, and validate rumors.";

        string s = "<b>Intel desk (known items)</b>\n\n";
        int take = Mathf.Min(8, entries.Count);
        for (int i = 0; i < take; i++)
        {
            var x = entries[i].item;
            s += "• [" + SafeVerificationLabel(x.verificationStatusInt) + "] " +
                SafeActionabilityLabel(x.actionabilityInt) + " · " +
                SafeTargetLabel(x.targetId) + " · " +
                SafeKnowledgeScopeLabel(entries[i].scope) + " (" + entries[i].holderCount + ")\n";
            if (!string.IsNullOrEmpty(x.contentSummary))
                s += "  " + x.contentSummary + "\n";
        }
        if (entries.Count > take)
            s += "\n<size=90%>+" + (entries.Count - take) + " more known items.</size>";
        return s;
    }

    private string BuildIntelRightPanel()
    {
        int burn = 0;
        int disinfo = 0;
        for (int i = 0; i < BureauWorldState.federalSourceProfiles.Count; i++)
        {
            var src = BureauWorldState.federalSourceProfiles[i];
            if (src == null) continue;
            if (src.currentStatusInt == (int)FederalSourceStatus.Compromised
                || src.currentStatusInt == (int)FederalSourceStatus.Burned)
                burn++;
        }
        var visible = GetPlayerVisibleIntelEntries();
        for (int i = 0; i < visible.Count; i++)
        {
            if (visible[i].item != null && visible[i].item.truthStateInt == (int)FederalIntelTruthState.Disinformation)
                disinfo++;
        }
        return "<b>Intel alerts</b>\n" +
            "• Source burn risk: " + (burn > 0 ? "Rising" : "Low") + "\n" +
            "• Known disinfo flags: " + disinfo + "\n" +
            "• Police ears: " + GameSessionState.FormatPolicePressureLabel();
    }

    private enum IntelKnowledgeScope
    {
        Shared = 0,
        Compartment = 1
    }

    private sealed class PlayerIntelVisibleEntry
    {
        public FederalIntelItem item;
        public IntelKnowledgeScope scope;
        public int holderCount;
    }

    private List<PlayerIntelVisibleEntry> GetPlayerVisibleIntelEntries()
    {
        var outList = new List<PlayerIntelVisibleEntry>();
        var items = GetPlayerVisibleIntelItems();
        int crewCount = Mathf.Max(1, PersonnelRegistry.Members != null ? PersonnelRegistry.Members.Count : 1);
        for (int i = 0; i < items.Count; i++)
        {
            var x = items[i];
            var scope = ResolveKnowledgeScope(x);
            int holders = scope == IntelKnowledgeScope.Shared
                ? crewCount
                : Mathf.Clamp(1 + Mathf.RoundToInt(crewCount * 0.3f), 1, Mathf.Max(1, crewCount - 1));
            outList.Add(new PlayerIntelVisibleEntry
            {
                item = x,
                scope = scope,
                holderCount = holders
            });
        }
        return outList;
    }

    private List<FederalIntelItem> GetPlayerVisibleIntelItems()
    {
        var outList = new List<FederalIntelItem>();
        int network = Mathf.Clamp(GameSessionState.PlayerIntelNetworkRating, 0, 100);
        int maxItems = Mathf.Clamp(2 + network / 15, 2, 20);
        for (int i = BureauWorldState.federalIntelItems.Count - 1; i >= 0 && outList.Count < maxItems; i--)
        {
            var x = BureauWorldState.federalIntelItems[i];
            if (x == null) continue;
            bool publicLeak = x.pressRisk >= 40 || x.exposureRisk >= 55;
            bool networkCanSee = x.reliability >= Mathf.Max(10, 70 - network) || x.actionabilityInt >= (int)FederalIntelActionability.StartSurveillance;
            if (publicLeak || networkCanSee)
                outList.Add(x);
        }
        return outList;
    }

    private IntelKnowledgeScope ResolveKnowledgeScope(FederalIntelItem item)
    {
        if (item == null)
            return IntelKnowledgeScope.Compartment;
        if (item.actionabilityInt >= (int)FederalIntelActionability.PrepareOperation)
            return IntelKnowledgeScope.Shared;
        if (item.verificationStatusInt >= (int)FederalIntelVerificationStatus.Corroborated && item.exposureRisk < 65)
            return IntelKnowledgeScope.Shared;
        return IntelKnowledgeScope.Compartment;
    }

    private string SafeKnowledgeScopeLabel(IntelKnowledgeScope scope)
    {
        if (scope == IntelKnowledgeScope.Shared) return "Shared";
        return "Compartment";
    }

    private FederalDailyReport GetLatestFederalDailyReport()
    {
        if (BureauWorldState.dailyReports == null || BureauWorldState.dailyReports.Count == 0)
            return null;
        return BureauWorldState.dailyReports[BureauWorldState.dailyReports.Count - 1];
    }

    private string SafeVerificationLabel(int statusInt)
    {
        if (statusInt == (int)FederalIntelVerificationStatus.Unverified) return "Unverified";
        if (statusInt == (int)FederalIntelVerificationStatus.PartiallyVerified) return "PartiallyVerified";
        if (statusInt == (int)FederalIntelVerificationStatus.Corroborated) return "Corroborated";
        if (statusInt == (int)FederalIntelVerificationStatus.Contradicted) return "Contradicted";
        if (statusInt == (int)FederalIntelVerificationStatus.ProvenFalse) return "ProvenFalse";
        if (statusInt == (int)FederalIntelVerificationStatus.OperationallyConfirmed) return "OperationallyConfirmed";
        return "Unknown";
    }

    private string SafeActionabilityLabel(int a)
    {
        if (a == (int)FederalIntelActionability.ArchiveOnly) return "Archive";
        if (a == (int)FederalIntelActionability.WatchTarget) return "Watch";
        if (a == (int)FederalIntelActionability.OpenActiveCase) return "OpenCase";
        if (a == (int)FederalIntelActionability.RecruitSource) return "RecruitSource";
        if (a == (int)FederalIntelActionability.StartSurveillance) return "Surveillance";
        if (a == (int)FederalIntelActionability.PrepareOperation) return "PrepareOperation";
        if (a == (int)FederalIntelActionability.ImmediateAction) return "ImmediateAction";
        if (a == (int)FederalIntelActionability.SecurityThreat) return "SecurityThreat";
        return "None";
    }

    private string SafeDailyStrategyLabel(int s)
    {
        if (s == (int)FederalDailyStrategy.Observe) return "Observe";
        if (s == (int)FederalDailyStrategy.Infiltrate) return "Infiltrate";
        if (s == (int)FederalDailyStrategy.Pressure) return "Pressure";
        if (s == (int)FederalDailyStrategy.BuildCase) return "BuildCase";
        if (s == (int)FederalDailyStrategy.TakeOverPoliceCase) return "TakeOverPoliceCase";
        if (s == (int)FederalDailyStrategy.Strike) return "Strike";
        if (s == (int)FederalDailyStrategy.LayLow) return "LayLow";
        if (s == (int)FederalDailyStrategy.CoverUp) return "CoverUp";
        return "Unknown";
    }

    private string SafeTargetLabel(string targetId)
    {
        if (string.IsNullOrEmpty(targetId)) return "UnknownTarget";
        if (targetId.StartsWith("corrupt:")) return "CorruptionTarget";
        if (targetId.StartsWith("police:")) return "PoliceCaseTarget";
        if (targetId.StartsWith("federal:")) return "FederalCaseTarget";
        return targetId;
    }

    public PlanningTabId CurrentTab => _current;

    /// <summary>Left-button drag to pan the ops view (inverted delta so screen drag matches expected “move on map” direction).</summary>
    private sealed class OpsMapPanDriver : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public RectTransform GridRoot;
        public PlanningShellController Shell;

        public void OnBeginDrag(PointerEventData eventData)
        {
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (GridRoot == null || Shell == null)
                return;
            if (Shell._opsNeighborhoodOverlayRoot != null && Shell._opsNeighborhoodOverlayRoot.activeSelf)
                return;
            GridRoot.anchoredPosition += eventData.delta;
            Shell.ClampOpsMapPanToBounds();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (Shell == null)
                return;
            Shell.ClampOpsMapPanToBounds();
        }
    }

    /// <summary>Drives normal / hover / pressed sprites for planning top tabs (Button transition alone was unreliable here).</summary>
    private sealed class TopTabButtonSpriteDriver : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private Image _img;
        private Sprite _normal;
        private Sprite _hover;
        private Sprite _pressed;
        private bool _hovered;
        private bool _held;
        private bool _selected;

        public void Configure(Image img, Sprite normal, Sprite hover, Sprite pressed)
        {
            _img = img;
            _normal = normal;
            _hover = hover;
            _pressed = pressed;
            Refresh();
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            Refresh();
        }

        private void Refresh()
        {
            if (_img == null || _normal == null)
                return;
            Sprite pick;
            if (_selected)
            {
                if (_hovered && _hover != null)
                    pick = _hover;
                else if (_pressed != null)
                    pick = _pressed;
                else
                    pick = _normal;
            }
            else if (_held && _pressed != null)
                pick = _pressed;
            else if (_held)
                pick = _hover != null ? _hover : _normal;
            else if (_hovered && _hover != null)
                pick = _hover;
            else
                pick = _normal;
            _img.sprite = pick;
            _img.type = pick != null && pick.border.sqrMagnitude > 0.01f ? Image.Type.Sliced : Image.Type.Simple;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            Refresh();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            Refresh();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;
            _held = true;
            Refresh();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;
            _held = false;
            Refresh();
        }
    }
}
