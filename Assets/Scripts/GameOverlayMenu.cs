using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// ESC or gear opens a local menu (settings). Does not pause simulation for other players (no Time.timeScale).
/// Uses Input System for Escape because legacy Input may be disabled when the project uses the new Input System.
/// </summary>
public class GameOverlayMenu : MonoBehaviour
{
    /// <summary>
    /// When true, the legacy top-right portrait button is hidden (e.g. replaced by a custom HUD).
    /// </summary>
    public static bool HideBossPortraitHudButton;

    private enum BossPanelSection
    {
        Inventory,
        Background,
        Relations,
        /// <summary>Combined core traits + derived skills side panel.</summary>
        TraitsAndSkills
    }

    /// <summary>Sub-view when Boss "Inventory" section is open: character loadout vs crew logistics/stash.</summary>
    private enum BossInventorySubPanel
    {
        Loadout,
        Logistics
    }

    public static GameOverlayMenu Instance { get; private set; }

    private const string PlanningSceneName = "PlanningScene";
    private const string MainMenuSceneName = "MainMenuScene";

    private static GUIStyle _wrappedLabelStyle;
    private static GUIStyle _portraitStyle;
    private static GUIStyle _sectionHeaderStyle;
    private static GUIStyle _traitsSectionHeaderStyle;
    private static GUIStyle _starLineStyle;
    private static GUIStyle _slotTitleStyle;
    private static GUIStyle _slotValueStyle;
    private static GUIStyle _slotMetaStyle;
    private static GUIStyle _panelHeaderStyle;
    private static GUIStyle _panelSubHeaderStyle;
    private static GUIStyle _hudCashStyle;
    private static Texture2D _bossPortraitTexture;
    private static string _bossPortraitKey;
    private static Texture2D _bagPortraitTexture;
    private static string _bagPortraitKey;
    private static Texture2D _pixel;

    private bool _menuOpen;
    private bool _bossInfoOpen;
    private bool _editLoadoutOpen;
    private float _suppressMenuToggleUntilTime;
    private bool _waitForEscapeRelease;
    private float _lastSceneLoadedAtTime;
    private string _lastSceneLoadedName;
    private Vector2 _loadoutScroll;
    private Vector2 _bossProfileScroll;
    private Vector2 _bossMiscSideScroll;
    private Vector2 _bossCoreTraitsSideScroll;
    private Vector2 _bossDerivedSkillsSideScroll;
    private Vector2 _bossCoreTraitInsightBodyScroll;
    private Vector2 _bossDerivedSkillInsightBodyScroll;
    private CoreTrait? _bossCoreTraitInsightFocus;
    private DerivedSkill? _bossDerivedSkillInsightFocus;
    private Vector2 _bossLogisticsScroll;
    private BossPanelSection _bossSection = BossPanelSection.Inventory;
    private BossInventorySubPanel _bossInventorySubPanel = BossInventorySubPanel.Loadout;

    /// <summary>Top-left of Boss Profile IMGUI window; x &lt; 0 = use default placement on next layout.</summary>
    private Vector2 _bossProfileWindowPos = new Vector2(-1f, -1f);
    private bool _bossProfileWindowDragging;
    private Vector2 _bossProfileWindowDragGrabOffset;
    private Vector2 _menuWindowPos = new Vector2(-1f, -1f);
    private bool _menuWindowDragging;
    private Vector2 _menuWindowDragGrabOffset;
    private bool _relationMemberInfoOpen;
    private CrewMember _relationMemberFocus;
    private Vector2 _relationMemberInfoScroll;
    private Vector2 _relationMemberWindowPos = new Vector2(-1f, -1f);
    private bool _relationMemberWindowDragging;
    private Vector2 _relationMemberWindowDragGrabOffset;

    private static Texture2D Pixel
    {
        get
        {
            if (_pixel == null)
            {
                _pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                _pixel.SetPixel(0, 0, Color.white);
                _pixel.Apply(false);
            }

            return _pixel;
        }
    }

    private static GUIStyle WrappedLabelStyle
    {
        get
        {
            if (_wrappedLabelStyle == null)
            {
                _wrappedLabelStyle = new GUIStyle(GUI.skin.label);
                _wrappedLabelStyle.wordWrap = true;
            }

            return _wrappedLabelStyle;
        }
    }

    private static GUIStyle PortraitStyle
    {
        get
        {
            if (_portraitStyle == null)
            {
                _portraitStyle = new GUIStyle(GUI.skin.button);
                _portraitStyle.alignment = TextAnchor.MiddleCenter;
                _portraitStyle.fontSize = 16;
                _portraitStyle.fontStyle = FontStyle.Bold;
                _portraitStyle.padding = new RectOffset(0, 0, 0, 0);
            }

            return _portraitStyle;
        }
    }

    private static GUIStyle SectionHeaderStyle
    {
        get
        {
            if (_sectionHeaderStyle == null)
            {
                _sectionHeaderStyle = new GUIStyle(GUI.skin.label);
                _sectionHeaderStyle.fontStyle = FontStyle.Bold;
            }

            return _sectionHeaderStyle;
        }
    }

    private static GUIStyle TraitsSectionHeaderStyle
    {
        get
        {
            if (_traitsSectionHeaderStyle == null)
            {
                _traitsSectionHeaderStyle = new GUIStyle(GUI.skin.label);
                _traitsSectionHeaderStyle.fontStyle = FontStyle.Bold;
                _traitsSectionHeaderStyle.fontSize = 15;
            }

            return _traitsSectionHeaderStyle;
        }
    }

    private static GUIStyle StarLineStyle
    {
        get
        {
            if (_starLineStyle == null)
            {
                _starLineStyle = new GUIStyle(GUI.skin.label);
                _starLineStyle.richText = true;
                _starLineStyle.fontSize = 11;
            }

            return _starLineStyle;
        }
    }

    private static GUIStyle _bossProfileStatNameStyle;
    private static GUIStyle BossProfileStatNameStyle
    {
        get
        {
            if (_bossProfileStatNameStyle == null)
            {
                _bossProfileStatNameStyle = new GUIStyle(GUI.skin.label);
                _bossProfileStatNameStyle.fontSize = 14;
                _bossProfileStatNameStyle.normal.textColor = new Color(0.9f, 0.92f, 0.96f, 1f);
            }
            return _bossProfileStatNameStyle;
        }
    }

    private static GUIStyle _bossProfileStatStarsStyle;
    private static GUIStyle _bossProfileActionLabelStyle;
    private static GUIStyle BossProfileActionLabelStyle
    {
        get
        {
            if (_bossProfileActionLabelStyle == null)
            {
                _bossProfileActionLabelStyle = new GUIStyle(GUI.skin.label);
                _bossProfileActionLabelStyle.richText = true;
                _bossProfileActionLabelStyle.alignment = TextAnchor.MiddleLeft;
                _bossProfileActionLabelStyle.fontSize = 13;
            }

            return _bossProfileActionLabelStyle;
        }
    }

    private static GUIStyle BossProfileStatStarsStyle
    {
        get
        {
            if (_bossProfileStatStarsStyle == null)
            {
                _bossProfileStatStarsStyle = new GUIStyle(GUI.skin.label);
                _bossProfileStatStarsStyle.richText = true;
                _bossProfileStatStarsStyle.fontSize = 15;
                _bossProfileStatStarsStyle.alignment = TextAnchor.MiddleRight;
            }
            return _bossProfileStatStarsStyle;
        }
    }

    private static GUIStyle _bossProfileHintStyle;
    private static GUIStyle BossProfileHintStyle
    {
        get
        {
            if (_bossProfileHintStyle == null)
            {
                _bossProfileHintStyle = new GUIStyle(GUI.skin.label);
                _bossProfileHintStyle.fontSize = 13;
                _bossProfileHintStyle.wordWrap = true;
                _bossProfileHintStyle.normal.textColor = new Color(0.78f, 0.82f, 0.88f, 0.95f);
            }
            return _bossProfileHintStyle;
        }
    }

    private static GUIStyle _bossIdentityCardStyle;
    private static GUIStyle _bossStatusLabelStyle;
    private static GUIStyle _bossStatusValueStyle;
    private static GUIStyle _relationsNameButtonStyle;
    private static GUIStyle _relationsStatusSmallStyle;
    private static GUIStyle BossIdentityCardStyle
    {
        get
        {
            if (_bossIdentityCardStyle == null)
            {
                _bossIdentityCardStyle = new GUIStyle(GUI.skin.box);
                _bossIdentityCardStyle.padding = new RectOffset(10, 10, 10, 10);
            }
            return _bossIdentityCardStyle;
        }
    }

    private static GUIStyle BossStatusLabelStyle
    {
        get
        {
            if (_bossStatusLabelStyle == null)
            {
                _bossStatusLabelStyle = new GUIStyle(GUI.skin.label);
                _bossStatusLabelStyle.fontSize = 14;
                _bossStatusLabelStyle.fontStyle = FontStyle.Bold;
                _bossStatusLabelStyle.normal.textColor = new Color(0.86f, 0.9f, 0.96f, 0.98f);
            }
            return _bossStatusLabelStyle;
        }
    }

    private static GUIStyle BossStatusValueStyle
    {
        get
        {
            if (_bossStatusValueStyle == null)
            {
                _bossStatusValueStyle = new GUIStyle(GUI.skin.label);
                _bossStatusValueStyle.fontSize = 16;
                _bossStatusValueStyle.fontStyle = FontStyle.Bold;
                _bossStatusValueStyle.normal.textColor = new Color(0.98f, 0.86f, 0.6f, 1f);
            }
            return _bossStatusValueStyle;
        }
    }

    private static GUIStyle RelationsNameButtonStyle
    {
        get
        {
            if (_relationsNameButtonStyle == null)
            {
                _relationsNameButtonStyle = new GUIStyle(GUI.skin.button);
                _relationsNameButtonStyle.alignment = TextAnchor.MiddleLeft;
                _relationsNameButtonStyle.fontSize = 13;
                _relationsNameButtonStyle.padding = new RectOffset(8, 8, 6, 6);
            }
            return _relationsNameButtonStyle;
        }
    }

    private static GUIStyle RelationsStatusSmallStyle
    {
        get
        {
            if (_relationsStatusSmallStyle == null)
            {
                _relationsStatusSmallStyle = new GUIStyle(GUI.skin.label);
                _relationsStatusSmallStyle.fontSize = 11;
                _relationsStatusSmallStyle.normal.textColor = new Color(0.78f, 0.82f, 0.88f, 0.92f);
                _relationsStatusSmallStyle.wordWrap = true;
            }
            return _relationsStatusSmallStyle;
        }
    }

    private static GUIStyle SlotTitleStyle
    {
        get
        {
            if (_slotTitleStyle == null)
            {
                _slotTitleStyle = new GUIStyle(GUI.skin.label);
                _slotTitleStyle.fontStyle = FontStyle.Bold;
                _slotTitleStyle.fontSize = 12;
                _slotTitleStyle.normal.textColor = new Color(0.86f, 0.89f, 0.94f, 0.98f);
            }

            return _slotTitleStyle;
        }
    }

    private static GUIStyle SlotValueStyle
    {
        get
        {
            if (_slotValueStyle == null)
            {
                _slotValueStyle = new GUIStyle(GUI.skin.label);
                _slotValueStyle.wordWrap = true;
                _slotValueStyle.fontSize = 13;
                _slotValueStyle.normal.textColor = new Color(0.97f, 0.98f, 1f, 0.98f);
            }

            return _slotValueStyle;
        }
    }

    private static GUIStyle SlotMetaStyle
    {
        get
        {
            if (_slotMetaStyle == null)
            {
                _slotMetaStyle = new GUIStyle(GUI.skin.label);
                _slotMetaStyle.fontSize = 12;
                _slotMetaStyle.normal.textColor = new Color(0.82f, 0.85f, 0.9f, 0.96f);
            }

            return _slotMetaStyle;
        }
    }

    private static GUIStyle PanelHeaderStyle
    {
        get
        {
            if (_panelHeaderStyle == null)
            {
                _panelHeaderStyle = new GUIStyle(GUI.skin.label);
                _panelHeaderStyle.fontStyle = FontStyle.Bold;
                _panelHeaderStyle.fontSize = 15;
                _panelHeaderStyle.normal.textColor = new Color(0.93f, 0.95f, 1f, 1f);
            }

            return _panelHeaderStyle;
        }
    }

    private static GUIStyle PanelSubHeaderStyle
    {
        get
        {
            if (_panelSubHeaderStyle == null)
            {
                _panelSubHeaderStyle = new GUIStyle(GUI.skin.label);
                _panelSubHeaderStyle.fontSize = 11;
                _panelSubHeaderStyle.normal.textColor = new Color(0.77f, 0.81f, 0.88f, 0.94f);
            }

            return _panelSubHeaderStyle;
        }
    }

    /// <summary>
    /// Call from planning or execution if the scene might not include this component (e.g. missing script reference).
    /// </summary>
    public static void EnsureExists()
    {
        if (Instance != null)
            return;

        GameObject go = new GameObject("GameOverlayMenu");
        go.AddComponent<GameOverlayMenu>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Ensure we don't start a new scene with a leftover menu overlay open.
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Always start scenes clean; player can reopen the overlay via ESC/gear.
        CloseAllPanels();
        // Avoid edge cases where ESC state carries into the first frames after a scene load.
        _suppressMenuToggleUntilTime = Time.unscaledTime + 0.9f;
        _waitForEscapeRelease = true;
        _lastSceneLoadedAtTime = Time.unscaledTime;
        _lastSceneLoadedName = scene.name;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this)
            Instance = null;
    }

    private static bool WasEscapePressedThisFrame()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            return true;

        return Input.GetKeyDown(KeyCode.Escape);
    }

    private static bool IsEscapeHeld()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
            return keyboard.escapeKey.isPressed;
        return Input.GetKey(KeyCode.Escape);
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name != MainMenuSceneName)
            GameSessionState.RefreshBossSnitchStreetRumor();

        if (GameSessionState.IsDaySummaryShowing)
            return;
        if (SceneManager.GetActiveScene().name == MainMenuSceneName)
            return;

        // Planning scene uses a custom HUD; do not allow the full-screen IMGUI menu overlay
        // to hijack the screen (no gear button). ESC should open/close the menu intentionally.
        if (SceneManager.GetActiveScene().name == PlanningSceneName)
        {
            if (Time.unscaledTime < _suppressMenuToggleUntilTime)
            {
                // If the overlay was open during scene transition, force-close it so it can't "stick".
                if (_menuOpen || _bossInfoOpen || _editLoadoutOpen)
                    CloseAllPanels();
                return;
            }

            // Require the user to release ESC after scene transitions / menu actions.
            if (_waitForEscapeRelease)
            {
                if (IsEscapeHeld())
                    return;
                _waitForEscapeRelease = false;
            }

            if (WasEscapePressedThisFrame())
            {
                if (_menuOpen)
                    CloseAllPanels();
                else
                    OpenMenuOverlay();
            }
            return;
        }

        if (Time.unscaledTime < _suppressMenuToggleUntilTime)
            return;

        if (_waitForEscapeRelease)
        {
            if (IsEscapeHeld())
                return;
            _waitForEscapeRelease = false;
        }

        if (WasEscapePressedThisFrame())
        {
            _menuOpen = !_menuOpen;
            if (_menuOpen)
            {
                _bossInfoOpen = false;
                _editLoadoutOpen = false;
            }
        }
    }

    private void StartNewGame()
    {
        CloseAllPanels();
        // Give scene transitions time so IMGUI can't "stick" a frame over the next scene.
        _suppressMenuToggleUntilTime = Time.unscaledTime + 1.25f;
        _waitForEscapeRelease = true;
        PlayerRunState.ClearCharacter();
        GameSessionState.ResetForNewGame();
        // Same entry as Main Menu → New Game: New Game Setup (Random / Manual), not straight into the questionnaire.
        MainMenuFlowController.OpenToNewGameSetup = true;
        SceneManager.LoadScene(MainMenuSceneName);
    }

    private void SaveGame()
    {
        if (GameSave.TrySave(out string err))
            Debug.Log("[Menu] Game saved to " + GameSave.SaveFilePath);
        else
            Debug.LogWarning("[Menu] Save failed: " + err);
    }

    private void LoadGame()
    {
        if (GameSave.TryLoad(out string err))
        {
            _menuOpen = false;
            Debug.Log("[Menu] Game loaded.");
        }
        else
            Debug.LogWarning("[Menu] Load failed: " + err);
    }

    private void SaveAndExit()
    {
        _menuOpen = false;
        if (GameSave.TrySave(out string err))
            Debug.Log("[Menu] Game saved to " + GameSave.SaveFilePath + " — exiting.");
        else
            Debug.LogWarning("[Menu] Save failed before exit: " + err);
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnGUI()
    {
        if (GameSessionState.IsDaySummaryShowing)
            return;
        if (SceneManager.GetActiveScene().name == MainMenuSceneName)
            return;

        // Hard guard: during suppression windows (scene loads / New Game), never draw the overlay.
        // This prevents "sticky" menu frames when Unity's event order draws OnGUI before Update settles state.
        if (Time.unscaledTime < _suppressMenuToggleUntilTime)
        {
            if (_menuOpen || _bossInfoOpen || _editLoadoutOpen)
                CloseAllPanels();
            return;
        }

        // Safety: if we just entered planning, never allow the menu overlay to "stick" open.
        // It can still be opened intentionally via ESC after the first moment in the scene.
        if (SceneManager.GetActiveScene().name == PlanningSceneName)
        {
            if (_menuOpen && Time.unscaledTime - _lastSceneLoadedAtTime < 1.5f)
            {
                _menuOpen = false;
                return;
            }
        }

        GUI.depth = -1000;

        DrawBossPortraitButton();

        if (_hudCashStyle == null)
        {
            _hudCashStyle = new GUIStyle(GUI.skin.label);
            _hudCashStyle.fontSize = 18;
            _hudCashStyle.fontStyle = FontStyle.Bold;
            _hudCashStyle.alignment = TextAnchor.MiddleRight;
            _hudCashStyle.normal.textColor = new Color(0.95f, 0.88f, 0.45f, 1f);
        }

        // Moved to the Planning top bar metrics (via PlanningShellController).
        // Hide this legacy HUD cash label to avoid overlap.
        // GUI.Label(new Rect(Screen.width - 448f, 14f, 280f, 28f), GameSessionState.FormatCrewCashDisplay(), _hudCashStyle);

        bool isPlanning = SceneManager.GetActiveScene().name == PlanningSceneName;
        if (!isPlanning)
        {
            if (GUI.Button(new Rect(Screen.width - 56f, 12f, 44f, 44f), "\u2699"))
            {
                _menuOpen = !_menuOpen;
                if (_menuOpen)
                {
                    _bossInfoOpen = false;
                    _editLoadoutOpen = false;
                }
            }
        }
        else
        {
            // In planning: no gear button; menu overlay can still be opened intentionally via OpenMenuOverlay().
        }

        if (_bossInfoOpen)
            DrawBossInfoWindow();
        if (_relationMemberInfoOpen)
            DrawRelationMemberInfoWindow();

        if (!_menuOpen)
            return;

        Rect overlayRect = new Rect(0f, 0f, Screen.width, Screen.height);
        Color prev = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.Box(overlayRect, GUIContent.none);
        GUI.color = prev;

        Rect menuRect = GetMenuWindowRect();
        ProcessMenuWindowDrag(menuRect);
        menuRect = GetMenuWindowRect();
        DrawBossProfileWindowBackground(menuRect);
        GUI.Box(menuRect, GUIContent.none);
        GUILayout.BeginArea(new Rect(menuRect.x + 12f, menuRect.y + 12f, menuRect.width - 24f, menuRect.height - 24f));
        GUILayout.Label("Menu", GUI.skin.box, GUILayout.ExpandWidth(true));
        GUILayout.Space(8f);
        GUILayout.Label("Local menu only — simulation keeps running for everyone else (no global pause).", WrappedLabelStyle);
        GUILayout.Space(12f);

        if (GUILayout.Button("New Game", GUILayout.Height(34f)))
            StartNewGame();

        if (GUILayout.Button("Save", GUILayout.Height(34f)))
            SaveGame();

        bool hasSave = GameSave.HasSaveFile();
        GUI.enabled = hasSave;
        if (GUILayout.Button("Load", GUILayout.Height(34f)))
            LoadGame();
        GUI.enabled = true;

        if (GUILayout.Button("Settings", GUILayout.Height(34f)))
            Debug.Log("[Menu] Settings — TODO");

        if (GUILayout.Button("Save and Exit", GUILayout.Height(34f)))
            SaveAndExit();

        if (GUILayout.Button("Exit to Main Menu", GUILayout.Height(34f)))
        {
            _menuOpen = false;
            SceneManager.LoadScene(MainMenuSceneName);
        }

        if (GUILayout.Button("Exit", GUILayout.Height(34f)))
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        GUILayout.EndArea();
    }

    public void CloseAllPanels()
    {
        _menuOpen = false;
        _bossInfoOpen = false;
        _editLoadoutOpen = false;
        _relationMemberInfoOpen = false;
        _relationMemberFocus = null;
        _menuWindowDragging = false;
        _relationMemberWindowDragging = false;
    }

    public void OpenMenuOverlay()
    {
        _menuOpen = true;
        _bossInfoOpen = false;
        _editLoadoutOpen = false;
        _relationMemberInfoOpen = false;
        _relationMemberFocus = null;
        _menuWindowPos = new Vector2(-1f, -1f);
    }

    private void DrawBossPortraitButton()
    {
        if (HideBossPortraitHudButton)
            return;
        // Planning uses uGUI (tabs + optional AoW dock); legacy IMGUI portrait overlaps the top bar.
        if (SceneManager.GetActiveScene().name == PlanningSceneName)
            return;

        Rect r = new Rect(Screen.width - 108f, 12f, 44f, 44f);
        PlayerCharacterProfile p = PlayerRunState.Character;
        Texture2D portrait = GetBossPortraitTexture(p);

        Color oldBg = GUI.backgroundColor;
        Color oldContent = GUI.contentColor;
        Color oldColor = GUI.color;

        if (portrait != null)
        {
            GUI.backgroundColor = Color.white;
            if (GUI.Button(r, GUIContent.none, PortraitStyle))
            {
                _bossInfoOpen = !_bossInfoOpen;
                _menuOpen = false;
                if (_bossInfoOpen)
                    _bossProfileWindowPos = new Vector2(-1f, -1f);
            }
            GUI.DrawTexture(r, portrait, ScaleMode.ScaleToFit);
            Color accent = PlayerAccentTint.GetAccentColorOrNeutral();
            GUI.color = new Color(accent.r, accent.g, accent.b, 0.18f);
            GUI.DrawTexture(r, Pixel);
        }
        else
        {
            GUI.backgroundColor = PlayerAccentTint.GetAccentColorOrNeutral();
            GUI.contentColor = new Color(0.08f, 0.08f, 0.1f, 1f);

            string initials = GetInitials(p != null ? p.DisplayName : "Boss");
            if (GUI.Button(r, initials, PortraitStyle))
            {
                _bossInfoOpen = !_bossInfoOpen;
                _menuOpen = false;
                if (_bossInfoOpen)
                    _bossProfileWindowPos = new Vector2(-1f, -1f);
            }
        }

        GUI.backgroundColor = oldBg;
        GUI.contentColor = oldContent;
        GUI.color = oldColor;
    }

    public void ToggleBossInfoWindow()
    {
        _bossInfoOpen = !_bossInfoOpen;
        _menuOpen = false;
        if (_bossInfoOpen)
            _bossProfileWindowPos = new Vector2(-1f, -1f);
    }

    private static Texture2D GetBossPortraitTexture(PlayerCharacterProfile profile)
    {
        string key = "BossPortrait";
        if (profile != null && !string.IsNullOrWhiteSpace(profile.PortraitResourcePath))
            key = profile.PortraitResourcePath.Trim();

        if (_bossPortraitTexture != null && _bossPortraitKey == key)
            return _bossPortraitTexture;

        _bossPortraitKey = key;
        _bossPortraitTexture = DealerPortraitNaming.LoadPortraitTexture(key);
        return _bossPortraitTexture;
    }

    /// <summary>Same fill/crop treatment as character creation portrait preview (dark plate + cover).</summary>
    private static void DrawBossPortraitPreviewFill(Rect targetRect, Texture2D portrait)
    {
        if (portrait == null)
            return;

        Color prev = GUI.color;
        GUI.color = new Color(0.06f, 0.06f, 0.08f, 1f);
        GUI.DrawTexture(targetRect, Pixel);
        GUI.color = Color.white;

        float texAspect = portrait.width / (float)Mathf.Max(1, portrait.height);
        float rectAspect = targetRect.width / Mathf.Max(1f, targetRect.height);
        Rect uv = new Rect(0f, 0f, 1f, 1f);

        if (texAspect > rectAspect)
        {
            float viewW = rectAspect / texAspect;
            float x = (1f - viewW) * 0.5f;
            uv = new Rect(x, 0f, viewW, 1f);
        }
        else if (texAspect < rectAspect)
        {
            float viewH = texAspect / rectAspect;
            float yCenter = (1f - viewH) * 0.5f;
            const float topBias = 0.18f;
            float y = Mathf.Clamp(yCenter + topBias, 0f, 1f - viewH);
            uv = new Rect(0f, y, 1f, viewH);
        }

        GUI.DrawTextureWithTexCoords(targetRect, portrait, uv, true);
        GUI.color = prev;
    }

    private static void DrawRectBorderFrame(Rect r, float thickness)
    {
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, thickness), Pixel);
        GUI.DrawTexture(new Rect(r.x, r.yMax - thickness, r.width, thickness), Pixel);
        float midH = Mathf.Max(0f, r.height - 2f * thickness);
        GUI.DrawTexture(new Rect(r.x, r.y + thickness, thickness, midH), Pixel);
        GUI.DrawTexture(new Rect(r.xMax - thickness, r.y + thickness, thickness, midH), Pixel);
    }

    /// <summary>Charcoal bezel + warm inner hairline (no player-accent ring).</summary>
    private static void DrawBossPortraitAccentFrame(Rect imageRect)
    {
        Color old = GUI.color;
        Rect outer = new Rect(imageRect.x - 6f, imageRect.y - 6f, imageRect.width + 12f, imageRect.height + 12f);
        GUI.color = new Color(0.04f, 0.04f, 0.06f, 1f);
        DrawRectBorderFrame(outer, 3f);
        GUI.color = new Color(0.98f, 0.84f, 0.52f, 0.55f);
        DrawRectBorderFrame(imageRect, 1f);
        GUI.color = old;
    }

    private static void DrawBossProfileWindowBackground(Rect rect)
    {
        Color old = GUI.color;
        GUI.color = new Color(0.07f, 0.08f, 0.11f, 0.97f);
        GUI.DrawTexture(rect, Pixel);
        GUI.color = old;
    }

    private static void DrawBossSidePanelBackground(Rect rect)
    {
        Color old = GUI.color;
        GUI.color = new Color(0.09f, 0.10f, 0.13f, 0.98f);
        GUI.DrawTexture(rect, Pixel);
        GUI.color = old;
    }

    private const float BossProfileWindowW = 420f;
    private const float BossProfileWindowMaxH = 580f;

    private Rect GetBossProfileWindowRect()
    {
        float h = Mathf.Min(Screen.height - 40f, BossProfileWindowMaxH);
        float x;
        float y;
        if (_bossProfileWindowPos.x < 0f)
        {
            x = (Screen.width - BossProfileWindowW) * 0.5f;
            y = (Screen.height - h) * 0.5f;
        }
        else
        {
            x = _bossProfileWindowPos.x;
            y = _bossProfileWindowPos.y;
        }

        Rect r = new Rect(x, y, BossProfileWindowW, h);
        float maxX = Mathf.Max(0f, Screen.width - r.width);
        float maxY = Mathf.Max(0f, Screen.height - r.height);
        r.x = Mathf.Clamp(r.x, 0f, maxX);
        r.y = Mathf.Clamp(r.y, 0f, maxY);
        if (_bossProfileWindowPos.x >= 0f)
            _bossProfileWindowPos = new Vector2(r.x, r.y);
        return r;
    }

    /// <summary>Draggable title bar (excludes the close button hit area).</summary>
    private void ProcessBossProfileWindowDrag(Rect box)
    {
        Event e = Event.current;
        if (e == null)
            return;
        if (e.type != EventType.MouseDown && e.type != EventType.MouseDrag && e.type != EventType.MouseUp)
            return;

        const float pad = 12f;
        const float headerH = 28f;
        float titleW = Mathf.Max(40f, box.width - pad * 2f - 36f);
        Rect titleBar = new Rect(box.x + pad, box.y + pad, titleW, headerH);

        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0 && titleBar.Contains(e.mousePosition))
                {
                    if (_bossProfileWindowPos.x < 0f)
                        _bossProfileWindowPos = new Vector2(box.x, box.y);
                    _bossProfileWindowDragGrabOffset = e.mousePosition - _bossProfileWindowPos;
                    _bossProfileWindowDragging = true;
                    e.Use();
                }
                break;
            case EventType.MouseDrag:
                if (_bossProfileWindowDragging && e.button == 0)
                {
                    _bossProfileWindowPos = e.mousePosition - _bossProfileWindowDragGrabOffset;
                    e.Use();
                }
                break;
            case EventType.MouseUp:
                if (_bossProfileWindowDragging && e.button == 0)
                {
                    _bossProfileWindowDragging = false;
                    e.Use();
                }
                break;
        }
    }

    private void DrawBossInfoWindow()
    {
        PlayerCharacterProfile p = PlayerRunState.Character;
        Rect box = GetBossProfileWindowRect();
        ProcessBossProfileWindowDrag(box);
        box = GetBossProfileWindowRect();

        DrawBossProfileWindowBackground(box);
        GUI.Box(box, GUIContent.none);
        GUILayout.BeginArea(new Rect(box.x + 12f, box.y + 12f, box.width - 24f, box.height - 24f));
        GUILayout.BeginHorizontal();
        GUILayout.Label("Boss Profile", GUI.skin.box, GUILayout.Height(28f), GUILayout.ExpandWidth(true));
        if (GUILayout.Button("X", GUILayout.Width(28f), GUILayout.Height(28f)))
        {
            _bossInfoOpen = false;
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            return;
        }
        GUILayout.EndHorizontal();

        if (p == null)
        {
            GUILayout.Space(8f);
            GUILayout.Label("No boss profile loaded yet.");
            GUILayout.EndArea();
            return;
        }

        PersonnelRegistry.SyncBossSlotFromProfileAndCustody(p);
        GameSessionState.ApplyBossCustodyLegalPhaseFromTrialFlag();

        p.EnsureEquipmentDefaults();

        _bossProfileScroll = GUILayout.BeginScrollView(_bossProfileScroll);
        GUILayout.Space(8f);
        GUILayout.Label("Boss: " + p.DisplayName, SectionHeaderStyle);
        GUILayout.Space(8f);
        const float portraitW = 130f;
        const float portraitH = 170f;
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical(GUILayout.Width(162f), GUILayout.Height(portraitH + 22f));
        Texture2D portrait = GetBossPortraitTexture(p);
        if (portrait != null)
        {
            Rect pr = GUILayoutUtility.GetRect(portraitW, portraitH, GUILayout.ExpandWidth(false));
            DrawBossPortraitPreviewFill(pr, portrait);
            DrawBossPortraitAccentFrame(pr);
        }
        GUILayout.EndVertical();
        GUILayout.Space(10f);
        DrawBossSectionButtons();
        GUILayout.EndHorizontal();

        GUILayout.Space(10f);
        DrawBossStatusCard();
        DrawReputationCard(p);
        DrawBossPoliceRecordCard();
        DrawBossProfileActionBar(p);
        GUILayout.EndScrollView();

        GUILayout.EndArea();

        // Side panel is tab-driven for all sections.
        DrawSelectedSidePanel(p, box);
    }

    private static string BuildStars(int filledCount, int maxStars = 10)
    {
        int filled = Mathf.Clamp(filledCount, 0, maxStars);
        int empty = maxStars - filled;
        return "<color=#FFD65C>" + new string('★', filled) + "</color><color=#626A75>" + new string('★', empty) + "</color>";
    }

    private static string GetInitials(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return "B";

        string[] parts = fullName.Trim().Split(' ');
        if (parts.Length == 1)
            return parts[0].Substring(0, 1).ToUpperInvariant();

        string a = parts[0].Substring(0, 1).ToUpperInvariant();
        string b = parts[parts.Length - 1].Substring(0, 1).ToUpperInvariant();
        return a + b;
    }

    private static void DrawHandSlot(string title, PlayerCharacterProfile.HandLoadout hand)
    {
        GUILayout.Space(4f);
        GUILayout.Label(title, SectionHeaderStyle);
        GUILayout.Label("• Grip item: " + hand.GripItem);
        GUILayout.Label("• Utility item: " + hand.UtilityItem);
        GUILayout.Label("• Use condition: " + hand.UseCondition);
        GUILayout.Label("• Priority: " + hand.Priority);
    }

    private static void DrawLoadoutEditor(PlayerCharacterProfile p)
    {
        GUILayout.Space(8f);
        GUILayout.Label("Loadout Editor", SectionHeaderStyle);

        DrawTextFieldRow("Head slot", ref p.Equipment.HeadSlot);
        DrawTextFieldRow("Body slot", ref p.Equipment.BodyArmorSlot);

        GUILayout.Label("Right hand", SectionHeaderStyle);
        DrawTextFieldRow("Grip", ref p.Equipment.RightHand.GripItem);
        DrawTextFieldRow("Utility", ref p.Equipment.RightHand.UtilityItem);
        DrawTextFieldRow("Condition", ref p.Equipment.RightHand.UseCondition);
        DrawPriorityRow(ref p.Equipment.RightHand.Priority);

        GUILayout.Label("Left hand", SectionHeaderStyle);
        DrawTextFieldRow("Grip", ref p.Equipment.LeftHand.GripItem);
        DrawTextFieldRow("Utility", ref p.Equipment.LeftHand.UtilityItem);
        DrawTextFieldRow("Condition", ref p.Equipment.LeftHand.UseCondition);
        DrawPriorityRow(ref p.Equipment.LeftHand.Priority);

        DrawTextFieldRow("Accessory 1", ref p.Equipment.AccessorySlot1);
        DrawTextFieldRow("Accessory 2", ref p.Equipment.AccessorySlot2);
        DrawTextFieldRow("Bag slot", ref p.Equipment.BagSlot);
        DrawTextFieldRow("Bag condition", ref p.Equipment.BagCondition);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Bag extra capacity", GUILayout.Width(140f));
        if (GUILayout.Button("-", GUILayout.Width(24f)))
            p.Equipment.ExtraCapacity = Mathf.Max(0, p.Equipment.ExtraCapacity - 1);
        GUILayout.Label(p.Equipment.ExtraCapacity.ToString(), GUILayout.Width(24f));
        if (GUILayout.Button("+", GUILayout.Width(24f)))
            p.Equipment.ExtraCapacity = Mathf.Min(12, p.Equipment.ExtraCapacity + 1);
        GUILayout.EndHorizontal();

        int smallSlots = GetBagSmallSlotCount(p.Equipment);
        int largeSlots = GetBagLargeSlotCount(p.Equipment);
        int bagSlots = GetBagTotalSlotCount(p.Equipment);
        SyncBagItemsCapacity(p.Equipment, bagSlots);
        if (bagSlots > 0)
        {
            GUILayout.Space(6f);
            GUILayout.Label("Bag items (" + smallSlots + " small / " + largeSlots + " large)", SectionHeaderStyle);
            for (int i = 0; i < smallSlots; i++)
            {
                string item = p.Equipment.BagItems[i];
                DrawTextFieldRow("Small item " + (i + 1), ref item);
                p.Equipment.BagItems[i] = item;
            }
            for (int i = 0; i < largeSlots; i++)
            {
                int idx = smallSlots + i;
                string item = p.Equipment.BagItems[idx];
                DrawTextFieldRow("Large item " + (i + 1), ref item);
                p.Equipment.BagItems[idx] = item;
            }
        }
    }

    private static void DrawTextFieldRow(string label, ref string value)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(140f));
        value = GUILayout.TextField(value ?? string.Empty, 64);
        GUILayout.EndHorizontal();
    }

    private static void DrawPriorityRow(ref int value)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Priority", GUILayout.Width(140f));
        if (GUILayout.Button("-", GUILayout.Width(24f)))
            value = Mathf.Max(1, value - 1);
        GUILayout.Label(value.ToString(), GUILayout.Width(24f));
        if (GUILayout.Button("+", GUILayout.Width(24f)))
            value = Mathf.Min(9, value + 1);
        GUILayout.EndHorizontal();
    }

    private void DrawLoadoutSidePanel(PlayerCharacterProfile p, Rect bossRect)
    {
        const float sideW = 380f;
        float x = bossRect.x - sideW - 8f;
        if (x < 8f)
            x = 8f;
        Rect side = new Rect(x, bossRect.y, sideW, bossRect.height);
        Rect area = new Rect(side.x + 10f, side.y + 10f, side.width - 20f, side.height - 20f);

        if (_bossInventorySubPanel == BossInventorySubPanel.Loadout)
            DrawBagCargoSidePanel(p, side);

        DrawBossSidePanelBackground(side);
        GUI.Box(side, GUIContent.none);

        string invTitle = _bossInventorySubPanel == BossInventorySubPanel.Logistics
            ? "CREW LOGISTICS"
            : "TACTICAL LOADOUT";
        string invSub = _bossInventorySubPanel == BossInventorySubPanel.Logistics
            ? "Shared stash and supplies — assign into loadout when ready"
            : "Assign slots, priorities, and carry rules";
        DrawLoadoutPanelHeaderRect(new Rect(area.x, area.y, area.width, 56f), invTitle, invSub);

        const float tabBarH = 34f;
        GUILayout.BeginArea(new Rect(area.x, area.y + 62f, area.width, area.height - 62f));
        DrawInventorySubTabBar();

        if (_bossInventorySubPanel == BossInventorySubPanel.Logistics)
        {
            _bossLogisticsScroll = GUILayout.BeginScrollView(_bossLogisticsScroll);
            DrawCrewLogisticsPlaceholder(p);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            return;
        }

        GUILayout.BeginVertical(GUILayout.ExpandHeight(true));
        float editorH = _editLoadoutOpen ? 176f : 0f;
        float contentH = area.height - 62f;
        float footerH = 42f + editorH + 8f;
        float availGrid = Mathf.Max(120f, contentH - tabBarH - footerH);
        Rect gridRect = GUILayoutUtility.GetRect(1f, availGrid, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));
        DrawSlotGridLayoutRect(p, gridRect);

        Rect separator = GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(true));
        Color old = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, 0.12f);
        GUI.DrawTexture(separator, Pixel);
        GUI.color = old;

        GUILayout.Space(6f);
        if (GUILayout.Button(_editLoadoutOpen ? "Close Loadout Editor" : "Edit Loadout", GUILayout.Height(30f)))
            _editLoadoutOpen = !_editLoadoutOpen;
        if (_editLoadoutOpen)
        {
            _loadoutScroll = GUILayout.BeginScrollView(_loadoutScroll, GUILayout.Height(170f));
            DrawLoadoutEditor(p);
            GUILayout.EndScrollView();
        }
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void DrawInventorySubTabBar()
    {
        GUILayout.BeginHorizontal(GUILayout.Height(32f));
        Color prevBg = GUI.backgroundColor;
        GUI.backgroundColor = _bossInventorySubPanel == BossInventorySubPanel.Loadout
            ? new Color(0.12f, 0.15f, 0.22f, 1f)
            : new Color(0.07f, 0.09f, 0.13f, 1f);
        if (GUILayout.Button("Loadout", GUILayout.Height(28f)))
        {
            _bossInventorySubPanel = BossInventorySubPanel.Loadout;
        }

        GUI.backgroundColor = _bossInventorySubPanel == BossInventorySubPanel.Logistics
            ? new Color(0.12f, 0.15f, 0.22f, 1f)
            : new Color(0.07f, 0.09f, 0.13f, 1f);
        if (GUILayout.Button("LOG", GUILayout.Height(28f)))
        {
            _bossInventorySubPanel = BossInventorySubPanel.Logistics;
            _editLoadoutOpen = false;
        }
        GUI.backgroundColor = prevBg;
        GUILayout.EndHorizontal();
        GUILayout.Space(4f);
    }

    private static void DrawCrewLogisticsPlaceholder(PlayerCharacterProfile p)
    {
        Color prevBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.08f, 0.09f, 0.12f, 0.96f);
        GUILayout.BeginVertical(BossIdentityCardStyle);
        GUILayout.Label("Crew supply (stub)", SectionHeaderStyle);
        GUILayout.Label(
            "Items like rope, fuel, and bulk gear live in the crew stash first. From here you will assign them into this fighter's bag and tactical slots without leaving the profile.",
            WrappedLabelStyle);
        GUILayout.Space(8f);
        GUILayout.Label("<b>Coming next:</b> stash list, quantities, and drag-to-loadout.", WrappedLabelStyle);
        GUILayout.Space(12f);
        GUILayout.Label("— " + (p != null && !string.IsNullOrWhiteSpace(p.DisplayName) ? p.DisplayName : "Fighter") + " still sees only what is already in loadout above when you switch back to Loadout.", WrappedLabelStyle);
        GUILayout.EndVertical();
        GUI.backgroundColor = prevBg;
    }

    private void DrawSelectedSidePanel(PlayerCharacterProfile p, Rect bossRect)
    {
        switch (_bossSection)
        {
            case BossPanelSection.Inventory:
                DrawLoadoutSidePanel(p, bossRect);
                break;
            case BossPanelSection.Background:
                DrawBackgroundSidePanel(p, bossRect);
                break;
            case BossPanelSection.Relations:
                DrawRelationsSidePanel(bossRect);
                break;
            case BossPanelSection.TraitsAndSkills:
                DrawTraitsSkillsSplitSidePanels(p, bossRect);
                break;
        }
    }

    private void DrawBackgroundSidePanel(PlayerCharacterProfile p, Rect bossRect)
    {
        DrawSimpleSidePanel(bossRect, "BACKGROUND", ref _bossMiscSideScroll, () =>
        {
            DrawBackgroundCard(p);
        });
    }

    private void DrawRelationsSidePanel(Rect bossRect)
    {
        DrawSimpleSidePanel(bossRect, "RELATIONS", ref _bossMiscSideScroll, () =>
        {
            DrawRelationsCard();
        });
    }

    private void OpenRelationMemberInfo(CrewMember member)
    {
        if (member == null)
            return;
        _relationMemberFocus = member;
        _relationMemberInfoOpen = true;
        _relationMemberInfoScroll = Vector2.zero;
        _relationMemberWindowPos = new Vector2(-1f, -1f);
    }

    private static string RevealOrHidden(bool known, string value)
    {
        if (!known || string.IsNullOrWhiteSpace(value))
            return "Classified";
        return value.Trim();
    }

    private static string RevealOrUnknown(bool known, string value)
    {
        if (!known || string.IsNullOrWhiteSpace(value))
            return "Unknown";
        return value.Trim();
    }

    private static bool IsCrewFieldKnown(string value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               !string.Equals(value.Trim(), "Unknown", StringComparison.OrdinalIgnoreCase);
    }

    private static Rect GetCenteredWindowRect(float width, float maxHeight)
    {
        float h = Mathf.Min(Screen.height - 40f, maxHeight);
        float x = (Screen.width - width) * 0.5f;
        float y = (Screen.height - h) * 0.5f;
        return new Rect(x, y, width, h);
    }

    private Rect GetMenuWindowRect()
    {
        const float width = 280f;
        const float maxHeight = 500f;
        float h = Mathf.Min(Screen.height - 32f, maxHeight);
        float x;
        float y;
        if (_menuWindowPos.x < 0f)
        {
            x = (Screen.width - width) * 0.5f;
            y = (Screen.height - h) * 0.5f;
        }
        else
        {
            x = _menuWindowPos.x;
            y = _menuWindowPos.y;
        }

        Rect r = new Rect(x, y, width, h);
        float maxX = Mathf.Max(0f, Screen.width - r.width);
        float maxY = Mathf.Max(0f, Screen.height - r.height);
        r.x = Mathf.Clamp(r.x, 0f, maxX);
        r.y = Mathf.Clamp(r.y, 0f, maxY);
        if (_menuWindowPos.x >= 0f)
            _menuWindowPos = new Vector2(r.x, r.y);
        return r;
    }

    private static void ProcessWindowDrag(
        Rect box,
        ref Vector2 position,
        ref bool dragging,
        ref Vector2 dragOffset)
    {
        Event e = Event.current;
        if (e == null)
            return;
        if (e.type != EventType.MouseDown && e.type != EventType.MouseDrag && e.type != EventType.MouseUp)
            return;

        const float pad = 12f;
        const float headerH = 28f;
        float titleW = Mathf.Max(40f, box.width - pad * 2f - 36f);
        Rect titleBar = new Rect(box.x + pad, box.y + pad, titleW, headerH);

        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0 && titleBar.Contains(e.mousePosition))
                {
                    if (position.x < 0f)
                        position = new Vector2(box.x, box.y);
                    dragOffset = e.mousePosition - position;
                    dragging = true;
                    e.Use();
                }
                break;
            case EventType.MouseDrag:
                if (dragging && e.button == 0)
                {
                    position = e.mousePosition - dragOffset;
                    e.Use();
                }
                break;
            case EventType.MouseUp:
                if (dragging && e.button == 0)
                {
                    dragging = false;
                    e.Use();
                }
                break;
        }
    }

    private void ProcessMenuWindowDrag(Rect box)
    {
        ProcessWindowDrag(box, ref _menuWindowPos, ref _menuWindowDragging, ref _menuWindowDragGrabOffset);
    }

    private Rect GetRelationMemberWindowRect()
    {
        const float windowW = 470f;
        const float windowH = 560f;
        float h = Mathf.Min(Screen.height - 40f, windowH);
        float x;
        float y;
        if (_relationMemberWindowPos.x < 0f)
        {
            x = (Screen.width - windowW) * 0.5f;
            y = (Screen.height - h) * 0.5f;
        }
        else
        {
            x = _relationMemberWindowPos.x;
            y = _relationMemberWindowPos.y;
        }

        Rect r = new Rect(x, y, windowW, h);
        float maxX = Mathf.Max(0f, Screen.width - r.width);
        float maxY = Mathf.Max(0f, Screen.height - r.height);
        r.x = Mathf.Clamp(r.x, 0f, maxX);
        r.y = Mathf.Clamp(r.y, 0f, maxY);
        if (_relationMemberWindowPos.x >= 0f)
            _relationMemberWindowPos = new Vector2(r.x, r.y);
        return r;
    }

    private void DrawRelationMemberInfoWindow()
    {
        if (_relationMemberFocus == null)
        {
            _relationMemberInfoOpen = false;
            return;
        }

        Rect box = GetRelationMemberWindowRect();
        ProcessWindowDrag(box, ref _relationMemberWindowPos, ref _relationMemberWindowDragging, ref _relationMemberWindowDragGrabOffset);
        box = GetRelationMemberWindowRect();
        DrawBossProfileWindowBackground(box);
        GUI.Box(box, GUIContent.none);

        GUILayout.BeginArea(new Rect(box.x + 12f, box.y + 12f, box.width - 24f, box.height - 24f));
        GUILayout.BeginHorizontal();
        GUILayout.Label("Character Profile", GUI.skin.box, GUILayout.Height(28f), GUILayout.ExpandWidth(true));
        if (GUILayout.Button("X", GUILayout.Width(28f), GUILayout.Height(28f)))
        {
            _relationMemberInfoOpen = false;
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            return;
        }
        GUILayout.EndHorizontal();

        CrewMember member = _relationMemberFocus;
        _relationMemberInfoScroll = GUILayout.BeginScrollView(_relationMemberInfoScroll);
        GUILayout.Space(8f);

        bool knowsName = IsCrewFieldKnown(member.Name);
        bool knowsRole = IsCrewFieldKnown(member.Role);
        bool knowsStatus = IsCrewFieldKnown(member.Status);
        bool knowsLoyalty = IsCrewFieldKnown(member.Loyalty);
        bool knowsSkills = IsCrewFieldKnown(member.Skills);

        GUILayout.BeginVertical("box");
        GUILayout.Label("Identity", SectionHeaderStyle);
        GUILayout.Label("Name: " + RevealOrUnknown(knowsName, member.Name), WrappedLabelStyle);
        GUILayout.Label("Role: " + RevealOrUnknown(knowsRole, member.Role), WrappedLabelStyle);
        GUILayout.Label("Status: " + RevealOrUnknown(knowsStatus, member.Status), WrappedLabelStyle);
        GUILayout.EndVertical();

        GUILayout.Space(8f);
        GUILayout.BeginVertical("box");
        GUILayout.Label("Operational", SectionHeaderStyle);
        GUILayout.Label("Loyalty posture: " + RevealOrHidden(knowsLoyalty, member.Loyalty), WrappedLabelStyle);
        GUILayout.Label("Skills profile: " + RevealOrHidden(knowsSkills, member.Skills), WrappedLabelStyle);
        GUILayout.Label("Satisfaction: " + CrewReputationSystem.GetSatisfactionLabel(member.Satisfaction), WrappedLabelStyle);
        GUILayout.EndVertical();

        GUILayout.Space(8f);
        GUILayout.BeginVertical("box");
        GUILayout.Label("Known / Hidden", SectionHeaderStyle);
        GUILayout.Label("Known values appear as text. Unknown or unconfirmed values are masked.", WrappedLabelStyle);
        GUILayout.Label("As intel systems expand, more fields here will automatically reveal based on discovery.", WrappedLabelStyle);
        GUILayout.EndVertical();

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    /// <summary>Two separate side windows: core traits (left) and derived skills (next to boss).</summary>
    private void DrawTraitsSkillsSplitSidePanels(PlayerCharacterProfile p, Rect bossRect)
    {
        Rect traitsSide = ComputeBossSidePanelRect(bossRect, 1, BossTraitSkillSplitPanelW);
        Rect skillsSide = ComputeBossSidePanelRect(bossRect, 0, BossTraitSkillSplitPanelW);
        if (traitsSide.x < 8f)
            traitsSide.x = 8f;
        if (skillsSide.x < 8f)
            skillsSide.x = 8f;

        DrawCoreTraitsSidePanelWithInsight(traitsSide, p);
        DrawDerivedSkillsSidePanelWithInsight(skillsSide, p);
    }

    private void DrawCoreTraitsSidePanelWithInsight(Rect side, PlayerCharacterProfile p)
    {
        DrawBossSidePanelBackground(side);
        GUI.Box(side, GUIContent.none);
        GUILayout.BeginArea(new Rect(side.x + 10f, side.y + 10f, side.width - 20f, side.height - 20f));
        GUILayout.BeginVertical();
        GUILayout.Label("CORE TRAITS", GUI.skin.box);
        GUILayout.BeginVertical(GUILayout.ExpandHeight(true));
        _bossCoreTraitsSideScroll = GUILayout.BeginScrollView(_bossCoreTraitsSideScroll);
        DrawCoreTraitsCardSelectable(p);
        GUILayout.EndScrollView();
        GUILayout.EndVertical();
        DrawBossCoreTraitInsightFooter();
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void DrawDerivedSkillsSidePanelWithInsight(Rect side, PlayerCharacterProfile p)
    {
        DrawBossSidePanelBackground(side);
        GUI.Box(side, GUIContent.none);
        GUILayout.BeginArea(new Rect(side.x + 10f, side.y + 10f, side.width - 20f, side.height - 20f));
        GUILayout.BeginVertical();
        GUILayout.Label("DERIVED SKILLS", GUI.skin.box);
        GUILayout.BeginVertical(GUILayout.ExpandHeight(true));
        _bossDerivedSkillsSideScroll = GUILayout.BeginScrollView(_bossDerivedSkillsSideScroll);
        DrawDerivedSkillsCardSelectable(p);
        GUILayout.EndScrollView();
        GUILayout.EndVertical();
        DrawBossDerivedSkillInsightFooter();
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void DrawBossCoreTraitInsightFooter()
    {
        const float footerH = 132f;
        Color prevBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.05f, 0.06f, 0.09f, 0.92f);
        GUILayout.BeginVertical(BossIdentityCardStyle, GUILayout.Height(footerH));
        if (_bossCoreTraitInsightFocus == null)
        {
            GUILayout.Label("What it affects", TraitsSectionHeaderStyle);
            GUILayout.Label("Click a trait above — same detail as on the identity build screen.", BossProfileHintStyle);
        }
        else
        {
            GUILayout.Label(OperationRegistry.GetTraitName(_bossCoreTraitInsightFocus.Value), TraitsSectionHeaderStyle);
            _bossCoreTraitInsightBodyScroll = GUILayout.BeginScrollView(_bossCoreTraitInsightBodyScroll, GUILayout.Height(footerH - 44f));
            GUILayout.Label(TraitSkillInsightTexts.GetCoreTraitInsight(_bossCoreTraitInsightFocus.Value), WrappedLabelStyle);
            GUILayout.EndScrollView();
        }

        GUILayout.EndVertical();
        GUI.backgroundColor = prevBg;
    }

    private void DrawBossDerivedSkillInsightFooter()
    {
        const float footerH = 132f;
        Color prevBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.05f, 0.06f, 0.09f, 0.92f);
        GUILayout.BeginVertical(BossIdentityCardStyle, GUILayout.Height(footerH));
        if (_bossDerivedSkillInsightFocus == null)
        {
            GUILayout.Label("What it affects", TraitsSectionHeaderStyle);
            GUILayout.Label("Click a skill above for a full breakdown.", BossProfileHintStyle);
        }
        else
        {
            GUILayout.Label(DerivedSkillProgression.GetDisplayName(_bossDerivedSkillInsightFocus.Value), TraitsSectionHeaderStyle);
            _bossDerivedSkillInsightBodyScroll = GUILayout.BeginScrollView(_bossDerivedSkillInsightBodyScroll, GUILayout.Height(footerH - 44f));
            GUILayout.Label(TraitSkillInsightTexts.GetDerivedSkillInsight(_bossDerivedSkillInsightFocus.Value), WrappedLabelStyle);
            GUILayout.EndScrollView();
        }

        GUILayout.EndVertical();
        GUI.backgroundColor = prevBg;
    }

    private void DrawCoreTraitsCardSelectable(PlayerCharacterProfile p)
    {
        Color prevBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.08f, 0.09f, 0.12f, 0.96f);
        GUILayout.BeginVertical(BossIdentityCardStyle);
        GUILayout.Label("Core traits — potential (0–5 stars)", TraitsSectionHeaderStyle);
        GUILayout.Label(
            "Potential unlocks when linked skill bank XP reaches skill-star thresholds 1, 3, 5, 7, 9 (odd tiers). Each potential star allows up to 2 visible skill stars (10 at potential 5). Crossing an odd tier pays XP into the gate; you then refill visible progress on that skill.",
            BossProfileHintStyle);
        GUILayout.Space(4f);
        DrawRubricLineSelectable(p, CoreTrait.Strength);
        DrawRubricLineSelectable(p, CoreTrait.Agility);
        DrawRubricLineSelectable(p, CoreTrait.Intelligence);
        DrawRubricLineSelectable(p, CoreTrait.Charisma);
        DrawRubricLineSelectable(p, CoreTrait.MentalResilience);
        DrawRubricLineSelectable(p, CoreTrait.Determination);
        GUILayout.EndVertical();
        GUI.backgroundColor = prevBg;
    }

    private void DrawDerivedSkillsCardSelectable(PlayerCharacterProfile p)
    {
        Color prevBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.08f, 0.09f, 0.12f, 0.96f);
        GUILayout.BeginVertical(BossIdentityCardStyle);
        GUILayout.Label("Derived skills (0–10 stars)", TraitsSectionHeaderStyle);
        GUILayout.Label(
            "Gold filled = earned; gold hollow = still under cap; grey = above cap. Cap per skill = 2 x the relevant core potential stars.",
            BossProfileHintStyle);
        GUILayout.Space(4f);
        for (int i = 0; i < BossPanelDerivedSkillOrder.Length; i++)
            DrawDerivedSkillLineSelectable(p, BossPanelDerivedSkillOrder[i]);
        GUILayout.EndVertical();
        GUI.backgroundColor = prevBg;
    }

    private void DrawRubricLineSelectable(PlayerCharacterProfile p, CoreTrait trait)
    {
        Rect rowRect = GUILayoutUtility.GetRect(1f, 40f, GUILayout.ExpandWidth(true));
        Color accent = PlayerCharacterProfile.GetAccentColor(p.AccentColorIndex);
        if (GUI.Button(rowRect, GUIContent.none, GUIStyle.none))
            _bossCoreTraitInsightFocus = trait;

        CoreTraitProgression.EnsureRubricsInitialized(p);
        int level = CoreTraitProgression.GetLevel(p, trait);
        string name = OperationRegistry.GetTraitName(trait);
        bool sel = _bossCoreTraitInsightFocus == trait;
        GUI.color = sel ? new Color(accent.r, accent.g, accent.b, 1f) : Color.white;
        GUI.Label(new Rect(rowRect.x + 8f, rowRect.y + 3f, 188f, 18f), name + ":", BossProfileStatNameStyle);
        GUI.color = Color.white;
        GUI.Label(new Rect(rowRect.xMax - 200f, rowRect.y + 3f, 192f, 18f), PotentialStarRichText.Build(level, TraitPotentialRubric.MaxTraitLevel), BossProfileStatStarsStyle);

        Rect barRect = new Rect(rowRect.x + 8f, rowRect.y + 22f, rowRect.width - 16f, 4f);
        Color old = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, 0.12f);
        GUI.DrawTexture(barRect, Pixel);
        float fill01 = level >= TraitPotentialRubric.MaxTraitLevel ? 1f : TraitPotentialPresentation.GetAggregateGateSkillProgress01(p, trait);
        if (fill01 > 0f)
        {
            GUI.color = new Color(accent.r, accent.g, accent.b, 0.95f);
            Rect fillRect = new Rect(barRect.x, barRect.y, barRect.width * fill01, barRect.height);
            GUI.DrawTexture(fillRect, Pixel);
        }

        GUI.color = old;
        GUILayout.Space(5f);
    }

    private void DrawDerivedSkillLineSelectable(PlayerCharacterProfile p, DerivedSkill skill)
    {
        Rect rowRect = GUILayoutUtility.GetRect(1f, 40f, GUILayout.ExpandWidth(true));
        Color accent = PlayerCharacterProfile.GetAccentColor(p.AccentColorIndex);
        if (GUI.Button(rowRect, GUIContent.none, GUIStyle.none))
            _bossDerivedSkillInsightFocus = skill;

        CoreTraitProgression.EnsureRubricsInitialized(p);
        int level = DerivedSkillProgression.GetLevel(p, skill);
        int capStars = SkillPotentialRules.GetSkillCapStars(p, skill);
        float fill01 = DerivedSkillProgression.GetSkillProgressBarFill01(p, skill);
        string name = DerivedSkillProgression.GetDisplayName(skill);
        bool sel = _bossDerivedSkillInsightFocus == skill;
        GUI.color = sel ? new Color(accent.r, accent.g, accent.b, 1f) : Color.white;
        GUI.Label(new Rect(rowRect.x + 8f, rowRect.y + 3f, 188f, 18f), name + ":", BossProfileStatNameStyle);
        GUI.color = Color.white;
        GUI.Label(new Rect(rowRect.xMax - 200f, rowRect.y + 3f, 192f, 18f), SkillStarRichText.Build(level, capStars, StarRubric.MaxLevel), BossProfileStatStarsStyle);

        Rect barRect = new Rect(rowRect.x + 8f, rowRect.y + 22f, rowRect.width - 16f, 4f);
        Color oldBar = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, 0.12f);
        GUI.DrawTexture(barRect, Pixel);
        if (fill01 > 0f)
        {
            GUI.color = new Color(accent.r, accent.g, accent.b, 0.95f);
            Rect fillRect = new Rect(barRect.x, barRect.y, barRect.width * fill01, barRect.height);
            GUI.DrawTexture(fillRect, Pixel);
        }

        GUI.color = oldBar;
        GUILayout.Space(5f);
    }

    private const float BossSidePanelW = 380f;
    private const float BossTraitSkillSplitPanelW = 380f;
    private const float BossSidePanelGap = 8f;

    private static Rect ComputeBossSidePanelRect(Rect bossRect, int indexFromBoss, float sideW)
    {
        float rightEdge = bossRect.x - BossSidePanelGap;
        for (int i = 0; i < indexFromBoss; i++)
            rightEdge -= sideW + BossSidePanelGap;
        return new Rect(rightEdge - sideW, bossRect.y, sideW, bossRect.height);
    }

    private void DrawSimpleSidePanelAt(Rect side, string title, ref Vector2 scrollState, System.Action drawContent)
    {
        DrawBossSidePanelBackground(side);
        GUI.Box(side, GUIContent.none);
        GUILayout.BeginArea(new Rect(side.x + 10f, side.y + 10f, side.width - 20f, side.height - 20f));
        GUILayout.Label(title, GUI.skin.box);
        scrollState = GUILayout.BeginScrollView(scrollState);
        drawContent();
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawSimpleSidePanel(Rect bossRect, string title, ref Vector2 scrollState, System.Action drawContent, float sideWidth = BossSidePanelW)
    {
        Rect side = ComputeBossSidePanelRect(bossRect, 0, sideWidth);
        if (side.x < 8f)
            side.x = 8f;
        DrawSimpleSidePanelAt(side, title, ref scrollState, drawContent);
    }

    private static void DrawSlotGridLayoutRect(PlayerCharacterProfile p, Rect r)
    {
        float rowGap = 7f;
        float colGap = 7f;

        Color old = GUI.color;
        GUI.color = new Color(0.07f, 0.09f, 0.13f, 0.78f);
        GUI.DrawTexture(r, Pixel);
        GUI.color = new Color(1f, 1f, 1f, 0.16f);
        GUI.Box(r, GUIContent.none);
        GUI.color = old;

        float headH = Mathf.Max(44f, r.height * 0.14f);
        float accH = Mathf.Max(44f, r.height * 0.16f);
        float infoH = Mathf.Max(38f, r.height * 0.13f);
        float middleH = Mathf.Max(120f, r.height - headH - accH - infoH - rowGap * 4f);

        Rect head = new Rect(r.x + r.width * 0.22f, r.y + rowGap, r.width * 0.56f, headH);

        float middleY = head.yMax + rowGap;
        float leftW = r.width * 0.33f;
        float centerW = r.width * 0.30f;
        float rightW = r.width - leftW - centerW - colGap * 2f;
        Rect leftCol = new Rect(r.x, middleY, leftW, middleH);
        Rect centerCol = new Rect(leftCol.xMax + colGap, middleY, centerW, middleH);
        Rect rightCol = new Rect(centerCol.xMax + colGap, middleY, rightW, middleH);

        float handSlotH = (middleH - rowGap) * 0.5f;
        Rect leftGrip = new Rect(leftCol.x, leftCol.y, leftCol.width, handSlotH);
        Rect leftUtility = new Rect(leftCol.x, leftGrip.yMax + rowGap, leftCol.width, handSlotH);
        Rect rightGrip = new Rect(rightCol.x, rightCol.y, rightCol.width, handSlotH);
        Rect rightUtility = new Rect(rightCol.x, rightGrip.yMax + rowGap, rightCol.width, handSlotH);

        float bodyH = middleH * 0.58f;
        Rect body = new Rect(centerCol.x, centerCol.y, centerCol.width, bodyH);
        Rect bag = new Rect(centerCol.x, body.yMax + rowGap, centerCol.width, middleH - bodyH - rowGap);

        Rect accRow = new Rect(r.x, middleY + middleH + rowGap, r.width, accH);
        Rect acc1 = new Rect(accRow.x, accRow.y, (accRow.width - colGap) * 0.5f, accRow.height);
        Rect acc2 = new Rect(acc1.xMax + colGap, accRow.y, acc1.width, accRow.height);

        Rect info = new Rect(r.x, accRow.yMax + rowGap, r.width, infoH - 2f);

        DrawSlotBoxRect(head, "HEAD", p.Equipment.HeadSlot, 0.95f);
        DrawSlotBoxRect(leftGrip, "LEFT GRIP", p.Equipment.LeftHand.GripItem, 0.85f);
        DrawSlotBoxRect(leftUtility, "LEFT UTILITY", p.Equipment.LeftHand.UtilityItem, 0.75f);
        DrawSlotBoxRect(rightGrip, "RIGHT GRIP", p.Equipment.RightHand.GripItem, 0.85f);
        DrawSlotBoxRect(rightUtility, "RIGHT UTILITY", p.Equipment.RightHand.UtilityItem, 0.75f);
        DrawSlotBoxRect(body, "BODY", p.Equipment.BodyArmorSlot, 0.92f);
        Texture2D bagTexture = GetBagTexture(p);
        DrawSlotBoxRect(bag, "BAG", p.Equipment.BagSlot + " (+" + p.Equipment.ExtraCapacity + ")", 0.90f, bagTexture);
        DrawSlotBoxRect(acc1, "ACCESSORY 1", p.Equipment.AccessorySlot1, 0.78f);
        DrawSlotBoxRect(acc2, "ACCESSORY 2", p.Equipment.AccessorySlot2, 0.78f);

        GUI.color = new Color(0.12f, 0.15f, 0.21f, 0.9f);
        GUI.DrawTexture(info, Pixel);
        GUI.color = new Color(1f, 1f, 1f, 0.18f);
        GUI.Box(info, GUIContent.none);
        GUI.color = old;

        GUI.Label(new Rect(info.x + 8f, info.y + 3f, info.width - 16f, 16f),
            "Hand Priority  L:" + p.Equipment.LeftHand.Priority + "    R:" + p.Equipment.RightHand.Priority, SlotMetaStyle);
        GUI.Label(new Rect(info.x + 8f, info.y + 19f, info.width - 16f, 16f), "Bag Rule  " + p.Equipment.BagCondition, SlotMetaStyle);
    }

    private static void DrawSlotBoxRect(Rect r, string title, string value, float alpha, Texture2D slotImage = null)
    {
        Color old = GUI.color;
        GUI.color = new Color(0.06f, 0.09f, 0.14f, 0.96f);
        GUI.DrawTexture(r, Pixel);
        GUI.color = new Color(1f, 1f, 1f, alpha * 0.62f);
        GUI.Box(r, GUIContent.none);

        Color accent = PlayerAccentTint.GetAccentColorOrNeutral();
        accent.a = alpha * 0.7f;
        GUI.color = accent;
        GUI.DrawTexture(new Rect(r.x + 1f, r.y + 1f, 3f, Mathf.Max(18f, r.height - 2f)), Pixel);
        GUI.color = old;

        if (slotImage != null)
        {
            Rect imageRect = new Rect(r.x + 6f, r.y + 21f, r.width - 12f, Mathf.Max(16f, r.height - 28f));
            Color wash = PlayerAccentTint.GetAccentColorOrNeutral();
            Color prev = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(imageRect, slotImage, ScaleMode.ScaleToFit);
            GUI.color = new Color(wash.r, wash.g, wash.b, 0.10f);
            GUI.DrawTexture(imageRect, Pixel);
            GUI.color = prev;
        }

        Rect t1 = new Rect(r.x + 9f, r.y + 4f, r.width - 16f, 16f);
        float valueY = Mathf.Min(r.y + (slotImage != null ? 20f : 23f), r.y + r.height - 20f);
        Rect t2 = new Rect(r.x + 9f, valueY, r.width - 16f, r.height - (valueY - r.y) - 4f);
        GUI.Label(t1, title, SlotTitleStyle);
        GUI.Label(t2, string.IsNullOrWhiteSpace(value) ? "Empty" : value, SlotValueStyle);
    }

    private static Texture2D GetBagTexture(PlayerCharacterProfile p)
    {
        if (p == null || p.Equipment == null || string.IsNullOrWhiteSpace(p.Equipment.BagImageResourcePath))
            return null;

        string key = p.Equipment.BagImageResourcePath.Trim();
        if (_bagPortraitTexture != null && _bagPortraitKey == key)
            return _bagPortraitTexture;

        _bagPortraitTexture = Resources.Load<Texture2D>(key);
        _bagPortraitKey = key;
        return _bagPortraitTexture;
    }

    private static void DrawLoadoutPanelHeaderRect(Rect r)
    {
        DrawLoadoutPanelHeaderRect(r, "TACTICAL LOADOUT", "Assign slots, priorities, and carry rules");
    }

    private void DrawBagCargoSidePanel(PlayerCharacterProfile p, Rect loadoutRect)
    {
        int smallSlots = GetBagSmallSlotCount(p.Equipment);
        int largeSlots = GetBagLargeSlotCount(p.Equipment);
        int slots = GetBagTotalSlotCount(p.Equipment);
        SyncBagItemsCapacity(p.Equipment, slots);
        if (slots <= 0)
            return;

        const float sideW = 230f;
        float x = loadoutRect.x - sideW - 8f;
        if (x < 8f)
            x = 8f;
        Rect side = new Rect(x, loadoutRect.y, sideW, loadoutRect.height);
        Rect area = new Rect(side.x + 10f, side.y + 10f, side.width - 20f, side.height - 20f);

        DrawBossSidePanelBackground(side);
        GUI.Box(side, GUIContent.none);
        DrawLoadoutPanelHeaderRect(new Rect(area.x, area.y, area.width, 56f), "BAG CARGO", "Capacity: " + smallSlots + " small + " + largeSlots + " large");

        Rect grid = new Rect(area.x, area.y + 64f, area.width, area.height - 76f);
        DrawBagItemsGridRect(p.Equipment, grid, smallSlots, largeSlots);
    }

    private static void DrawBagItemsGridRect(PlayerCharacterProfile.EquipmentLoadout e, Rect r, int smallSlots, int largeSlots)
    {
        Color old = GUI.color;
        GUI.color = new Color(0.07f, 0.09f, 0.13f, 0.78f);
        GUI.DrawTexture(r, Pixel);
        GUI.color = new Color(1f, 1f, 1f, 0.16f);
        GUI.Box(r, GUIContent.none);
        GUI.color = old;

        float rowGap = 6f;
        float colGap = 6f;
        float y = r.y + rowGap;

        for (int i = 0; i < largeSlots; i++)
        {
            Rect largeCell = new Rect(r.x + 6f, y, r.width - 12f, Mathf.Max(54f, r.height * 0.26f));
            DrawSlotBoxRect(largeCell, "LARGE ITEM " + (i + 1), e.BagItems[smallSlots + i], 0.82f);
            y = largeCell.yMax + rowGap;
        }

        if (smallSlots <= 0)
            return;

        const int smallCols = 2;
        int smallRows = Mathf.CeilToInt(smallSlots / (float)smallCols);
        float smallW = (r.width - colGap) * 0.5f;
        float availableH = Mathf.Max(40f, r.yMax - y - rowGap);
        float smallH = Mathf.Max(40f, (availableH - rowGap * (smallRows + 1f)) / Mathf.Max(1, smallRows));

        int idx = 0;
        for (int row = 0; row < smallRows; row++)
        {
            for (int col = 0; col < smallCols; col++)
            {
                if (idx >= smallSlots)
                    break;

                float x = r.x + col * (smallW + colGap);
                float yy = y + rowGap + row * (smallH + rowGap);
                Rect cell = new Rect(x, yy, smallW, smallH);
                DrawSlotBoxRect(cell, "SMALL ITEM " + (idx + 1), e.BagItems[idx], 0.74f);
                idx++;
            }
        }
    }

    private static bool HasBag(string bagSlot)
    {
        if (string.IsNullOrWhiteSpace(bagSlot))
            return false;

        string normalized = bagSlot.Trim().ToLowerInvariant();
        if (normalized == "no bag" || normalized == "none" || normalized == "empty")
            return false;

        return true;
    }

    private static int GetBagSmallSlotCount(PlayerCharacterProfile.EquipmentLoadout e)
    {
        if (e == null || !HasBag(e.BagSlot))
            return 0;

        string b = e.BagSlot.ToLowerInvariant();
        if (b.Contains("messenger"))
            return 3 + Mathf.Max(0, e.ExtraCapacity);
        if (b.Contains("backpack") || b.Contains("rucksack"))
            return 4 + Mathf.Max(0, e.ExtraCapacity);
        if (b.Contains("duffel"))
            return 3 + Mathf.Max(0, e.ExtraCapacity);
        if (b.Contains("hand bag") || b.Contains("handbag") || b.Contains("satchel"))
            return 2 + Mathf.Max(0, e.ExtraCapacity);
        return 2 + Mathf.Max(0, e.ExtraCapacity);
    }

    private static int GetBagLargeSlotCount(PlayerCharacterProfile.EquipmentLoadout e)
    {
        if (e == null || !HasBag(e.BagSlot))
            return 0;

        string b = e.BagSlot.ToLowerInvariant();
        if (b.Contains("messenger"))
            return 1;
        if (b.Contains("backpack") || b.Contains("duffel"))
            return 1;
        return 0;
    }

    private static int GetBagTotalSlotCount(PlayerCharacterProfile.EquipmentLoadout e)
    {
        int total = GetBagSmallSlotCount(e) + GetBagLargeSlotCount(e);
        return Mathf.Clamp(total, 0, 16);
    }

    private static void SyncBagItemsCapacity(PlayerCharacterProfile.EquipmentLoadout e, int slotCount)
    {
        if (e == null)
            return;

        if (slotCount <= 0)
        {
            e.BagItems = System.Array.Empty<string>();
            return;
        }

        if (e.BagItems == null || e.BagItems.Length != slotCount)
        {
            string[] old = e.BagItems ?? System.Array.Empty<string>();
            string[] next = new string[slotCount];
            int copy = Mathf.Min(old.Length, next.Length);
            for (int i = 0; i < copy; i++)
                next[i] = old[i];
            for (int i = copy; i < next.Length; i++)
                next[i] = "Empty";
            e.BagItems = next;
        }
    }

    private static void DrawLoadoutPanelHeaderRect(Rect r, string title, string subtitle)
    {
        Color old = GUI.color;
        GUI.color = new Color(0.09f, 0.11f, 0.16f, 0.95f);
        GUI.DrawTexture(r, Pixel);
        GUI.color = new Color(1f, 1f, 1f, 0.2f);
        GUI.Box(r, GUIContent.none);

        Color accent = PlayerAccentTint.GetAccentColorOrNeutral();
        accent.a = 0.92f;
        GUI.color = accent;
        GUI.DrawTexture(new Rect(r.x + 1f, r.y + 1f, r.width - 2f, 2f), Pixel);
        GUI.color = old;

        GUI.Label(new Rect(r.x + 10f, r.y + 7f, r.width - 20f, 22f), title, PanelHeaderStyle);
        GUI.Label(new Rect(r.x + 10f, r.y + 30f, r.width - 20f, 18f), subtitle, PanelSubHeaderStyle);
    }

    private static void DrawOpaqueWindowBackground(Rect rect)
    {
        Color old = GUI.color;
        GUI.color = new Color(0.36f, 0.39f, 0.44f, 1f);
        GUI.DrawTexture(rect, Pixel);
        GUI.color = old;
    }

    private static void DrawReputationCard(PlayerCharacterProfile p)
    {
        int rep = Mathf.Clamp(p.PublicReputation, -100, 100);

        Color prevBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.08f, 0.09f, 0.12f, 0.96f);
        GUILayout.BeginVertical(BossIdentityCardStyle);
        GUILayout.Label("Crew Reputation", SectionHeaderStyle);
        GUILayout.Label("Leader reputation defines the organization's street name.", WrappedLabelStyle);
        GUILayout.Space(4f);
        GUILayout.Label("<b>" + GetReputationTitle(rep) + "</b>", StarLineStyle);
        GUILayout.Label(GetReputationDescription(rep), WrappedLabelStyle);
        GUILayout.EndVertical();
        GUI.backgroundColor = prevBg;
    }

    /// <summary>Shows when the boss accepted the detectives' cooperation offer in the cooperative interrogation path.</summary>
    private static void DrawBossPoliceRecordCard()
    {
        if (!GameSessionState.BossIsPoliceInformant)
            return;

        GUILayout.Space(10f);
        Color prevBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.14f, 0.1f, 0.06f, 0.96f);
        GUILayout.BeginVertical(BossIdentityCardStyle);
        GUILayout.Label("Police record", SectionHeaderStyle);
        GUILayout.Label(
            "The department lists you as a <b>registered police contact</b> after you agreed to cooperate on the bar incident. " +
            "While that stays quiet on the street, your <b>Status</b> shows <b>Police informant</b>. " +
            "If rival crews learn the same story, <b>Snitch</b> is added — and the underworld typically answers with open war, broken morale, and ruined street reputation.",
            WrappedLabelStyle);
        GUILayout.EndVertical();
        GUI.backgroundColor = prevBg;
    }

    private static void DrawBossStatusCard()
    {
        string status = GetBossNegativeStatusLabel();
        if (string.IsNullOrEmpty(status))
            return;

        Color prevBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.1f, 0.07f, 0.07f, 0.96f);
        GUILayout.BeginVertical(BossIdentityCardStyle);
        GUILayout.Label("Current Status", SectionHeaderStyle);
        GUILayout.Label("<b>" + status + "</b>", StarLineStyle);
        GUILayout.EndVertical();
        GUI.backgroundColor = prevBg;
    }

    private static string GetBossNegativeStatusLabel()
    {
        CharacterStatus status = GameSessionState.GetPlayerBossResolvedCustodyStatus();
        if (CharacterStatusUtility.IsNegative(status))
            return CharacterStatusUtility.ToDisplayLabel(status);

        // Fallback: if session flags say the boss is in custody but roster/name sync is off this frame,
        // still show the custody status in the boss profile.
        string dn = PlayerRunState.Character != null && !string.IsNullOrWhiteSpace(PlayerRunState.Character.DisplayName)
            ? PlayerRunState.Character.DisplayName.Trim()
            : "Boss";
        string detained = (GameSessionState.InitialDetainedCharacterName ?? string.Empty).Trim();
        bool sessionSaysBossDetained =
            GameSessionState.BossStartsInPrison ||
            (!string.IsNullOrEmpty(detained) &&
             (detained.IndexOf("(Boss)", StringComparison.OrdinalIgnoreCase) >= 0 ||
              string.Equals(detained, dn, StringComparison.OrdinalIgnoreCase) ||
              detained.StartsWith(dn, StringComparison.OrdinalIgnoreCase)));

        if (sessionSaysBossDetained)
        {
            CharacterStatus fallback = GameSessionState.BossCustodyTrialCompleted
                ? CharacterStatus.Imprisoned
                : CharacterStatus.Detained;
            return CharacterStatusUtility.ToDisplayLabel(fallback);
        }

        return string.Empty;
    }

    /// <summary>Small crate / pallet mark for logistics actions (IMGUI, no texture).</summary>
    private static void DrawLogisticsGlyph(Rect r, Color accent)
    {
        Color old = GUI.color;
        GUI.color = new Color(accent.r, accent.g, accent.b, 0.88f);
        GUI.DrawTexture(new Rect(r.x + 1f, r.y + 3f, r.width - 2f, r.height - 6f), Pixel);
        GUI.color = new Color(0.05f, 0.06f, 0.09f, 0.92f);
        float midX = r.x + r.width * 0.5f - 1f;
        GUI.DrawTexture(new Rect(midX, r.y + 5f, 2f, r.height - 10f), Pixel);
        GUI.DrawTexture(new Rect(r.x + 3f, r.y + r.height * 0.5f - 1f, r.width - 6f, 2f), Pixel);
        GUI.color = new Color(1f, 1f, 1f, 0.2f);
        GUI.DrawTexture(new Rect(r.x, r.y + 1f, r.width, 1f), Pixel);
        GUI.color = old;
    }

    private void DrawBossProfileActionBar(PlayerCharacterProfile p)
    {
        if (p == null)
            return;

        GUILayout.Space(12f);
        Color accent = PlayerCharacterProfile.GetAccentColor(p.AccentColorIndex);
        Rect br = GUILayoutUtility.GetRect(1f, 40f, GUILayout.ExpandWidth(true));
        Color prevBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.1f, 0.12f, 0.18f, 1f);
        if (GUI.Button(br, GUIContent.none))
        {
            _bossSection = BossPanelSection.Inventory;
            _bossInventorySubPanel = BossInventorySubPanel.Logistics;
            _editLoadoutOpen = false;
        }

        GUI.backgroundColor = prevBg;
        DrawLogisticsGlyph(new Rect(br.x + 10f, br.y + 8f, 24f, 24f), accent);
        string hex = ColorUtility.ToHtmlStringRGB(accent);
        GUI.Label(new Rect(br.x + 42f, br.y + 9f, br.width - 52f, 22f),
            "<color=#" + hex + "><b>LOG</b></color>  Edit loadout", BossProfileActionLabelStyle);
    }

    private void DrawBossSectionButtons()
    {
        GUILayout.BeginVertical(GUILayout.Width(170f), GUILayout.Height(170f));
        DrawSectionButton("Inventory", BossPanelSection.Inventory);
        DrawSectionButton("Background", BossPanelSection.Background);
        DrawSectionButton("Relations", BossPanelSection.Relations);
        DrawSectionButton("Traits & skills", BossPanelSection.TraitsAndSkills);
        GUILayout.Space(8f);
        string[] statusLines = GetBossProfileStatusLines(out bool hasStatus);
        if (hasStatus)
        {
            GUILayout.Label("Status:", BossStatusLabelStyle);
            for (int i = 0; i < statusLines.Length; i++)
                GUILayout.Label(statusLines[i], BossStatusValueStyle);
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndVertical();
    }

    /// <summary>Lines under boss profile "Status" — same column as section buttons (e.g. In prison, Police informant, Snitch).</summary>
    private static string[] GetBossProfileStatusLines(out bool hasAny)
    {
        System.Collections.Generic.List<string> lines = new System.Collections.Generic.List<string>();
        string negative = GetBossNegativeStatusLabel();
        if (!string.IsNullOrEmpty(negative))
            lines.Add(negative);
        if (GameSessionState.BossIsPoliceInformant)
            lines.Add("Police informant");
        if (GameSessionState.BossSnitchKnownToRivalGangs)
            lines.Add("Snitch");
        hasAny = lines.Count > 0;
        return lines.ToArray();
    }

    private void DrawSectionButton(string label, BossPanelSection section)
    {
        bool selected = _bossSection == section;
        Color old = GUI.backgroundColor;
        if (selected)
            GUI.backgroundColor = new Color(0.24f, 0.28f, 0.38f, 1f);
        if (GUILayout.Button(label, GUILayout.Height(26f)))
        {
            if (_bossSection == BossPanelSection.TraitsAndSkills && section != BossPanelSection.TraitsAndSkills)
            {
                _bossCoreTraitInsightFocus = null;
                _bossDerivedSkillInsightFocus = null;
            }

            _bossSection = section;
        }

        GUI.backgroundColor = old;
    }

    private static void DrawBackgroundCard(PlayerCharacterProfile p)
    {
        GUILayout.BeginVertical("box");
        GUILayout.Label("Background & Biography", SectionHeaderStyle);
        GUILayout.Label("Boss name: " + p.DisplayName, WrappedLabelStyle);
        GUILayout.Label("Identity was built from questionnaire answers and early street choices.", WrappedLabelStyle);
        GUILayout.Label("Long-term profile hooks: origin, key events, enemies, allies, habits.", WrappedLabelStyle);
        GUILayout.EndVertical();
    }

    private void DrawRelationsCard()
    {
        GUILayout.BeginVertical("box");
        GUILayout.Label("Relations & Connections", SectionHeaderStyle);
        DrawRelationBucket("Friendly", new Color(0.46f, 0.62f, 0.48f, 0.70f), RelationBucket.Friendly);
        DrawRelationBucket("Neutral", new Color(0.70f, 0.64f, 0.38f, 0.72f), RelationBucket.Neutral);
        DrawRelationBucket("Hostile", new Color(0.67f, 0.40f, 0.40f, 0.72f), RelationBucket.Hostile);
        GUILayout.EndVertical();
    }

    private enum RelationBucket
    {
        Friendly,
        Neutral,
        Hostile
    }

    private static bool IsBossSelfMember(int index, CrewMember m)
    {
        if (m == null)
            return false;
        if (index == 0)
            return true;
        if (!string.IsNullOrWhiteSpace(m.Role) && string.Equals(m.Role.Trim(), "Boss", StringComparison.OrdinalIgnoreCase))
            return true;
        if (!string.IsNullOrWhiteSpace(m.Name) && m.Name.IndexOf("(Boss)", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        return false;
    }

    private static bool IsStartingTrustedFriend(int index, CrewMember m)
    {
        if (m == null)
            return false;
        // Day-1 party baseline: the boss starts with three close friends.
        return index >= 1 && index <= 3;
    }

    private static RelationBucket ResolveRelationBucket(CrewMember m, int index)
    {
        if (IsStartingTrustedFriend(index, m))
            return RelationBucket.Friendly;

        if (m == null || string.IsNullOrWhiteSpace(m.Loyalty))
            return RelationBucket.Neutral;

        string loyalty = m.Loyalty.Trim().ToLowerInvariant();
        if (loyalty.Contains("readytoflip") || loyalty.Contains("disloyal") || loyalty.Contains("resentful") ||
            loyalty.Contains("enemy") || loyalty.Contains("hostile") || loyalty.Contains("betray"))
            return RelationBucket.Hostile;

        if (loyalty.Contains("devoted") || loyalty.Contains("loyal") || loyalty.Contains("attached") ||
            loyalty.Contains("friend") || loyalty.Contains("cooperative"))
            return RelationBucket.Friendly;

        return RelationBucket.Neutral;
    }

    private void DrawRelationBucket(string title, Color bg, RelationBucket bucket)
    {
        Color oldBg = GUI.backgroundColor;
        Color oldContent = GUI.contentColor;
        GUI.backgroundColor = bg;
        GUILayout.BeginVertical(BossIdentityCardStyle);
        GUI.backgroundColor = oldBg;

        GUI.contentColor = new Color(1f, 1f, 1f, 0.96f);
        GUILayout.Label(title, SectionHeaderStyle);
        GUI.contentColor = oldContent;
        bool any = false;
        for (int i = 0; i < PersonnelRegistry.Members.Count; i++)
        {
            CrewMember m = PersonnelRegistry.Members[i];
            if (IsBossSelfMember(i, m))
                continue;

            if (ResolveRelationBucket(m, i) != bucket)
                continue;

            any = true;
            GUI.backgroundColor = new Color(
                Mathf.Clamp01(bg.r * 0.72f),
                Mathf.Clamp01(bg.g * 0.72f),
                Mathf.Clamp01(bg.b * 0.72f),
                0.90f);
            GUILayout.BeginVertical("box");
            GUI.backgroundColor = oldBg;
            string name = string.IsNullOrWhiteSpace(m.Name) ? "(Unknown)" : m.Name;
            GUI.backgroundColor = new Color(
                Mathf.Clamp01(bg.r * 0.85f),
                Mathf.Clamp01(bg.g * 0.85f),
                Mathf.Clamp01(bg.b * 0.85f),
                0.95f);
            if (GUILayout.Button(name, RelationsNameButtonStyle))
                OpenRelationMemberInfo(m);
            GUI.backgroundColor = oldBg;

            string status = string.IsNullOrWhiteSpace(m.Loyalty) ? "Status: Unknown" : "Status: " + m.Loyalty.Trim();
            GUI.contentColor = new Color(0.92f, 0.94f, 0.98f, 0.94f);
            GUILayout.Label(status, RelationsStatusSmallStyle);
            GUI.contentColor = oldContent;
            GUILayout.EndVertical();
        }

        if (!any)
        {
            GUI.contentColor = new Color(0.90f, 0.92f, 0.96f, 0.88f);
            GUILayout.Label("No known entries.", RelationsStatusSmallStyle);
            GUI.contentColor = oldContent;
        }

        GUILayout.EndVertical();
        GUILayout.Space(8f);
    }

    private static readonly DerivedSkill[] BossPanelDerivedSkillOrder =
    {
        DerivedSkill.Brawling, DerivedSkill.Firearms, DerivedSkill.Stealth, DerivedSkill.Driving,
        DerivedSkill.Lockpicking, DerivedSkill.Surveillance, DerivedSkill.Negotiation, DerivedSkill.Intimidation,
        DerivedSkill.Deception, DerivedSkill.Logistics, DerivedSkill.Leadership,
        DerivedSkill.Medicine, DerivedSkill.Sabotage,
        DerivedSkill.Analysis, DerivedSkill.Legal, DerivedSkill.Finance, DerivedSkill.Persuasion
    };

    private static string GetReputationTitle(int rep)
    {
        if (rep <= -70)
            return "Filthy street junkie";
        if (rep <= -30)
            return "Washed-up nobody";
        if (rep <= -1)
            return "Toy criminal";
        if (rep <= 20)
            return "Unknown";
        if (rep <= 60)
            return "Recognized operator";
        if (rep <= 85)
            return "Feared figure";
        return "Untouchable terror";
    }

    private static string GetReputationDescription(int rep)
    {
        if (rep <= -70)
            return "Filthy street junkie dumped on a bench. No respect, no fear.";
        if (rep <= -30)
            return "Washed-up nobody. Laughed off by most people.";
        if (rep <= -1)
            return "Toy criminal. All pose, no fear factor.";
        if (rep <= 20)
            return "Unknown face. Barely a whisper in the neighborhood.";
        if (rep <= 60)
            return "Known operator. People start taking your name seriously.";
        if (rep <= 85)
            return "Feared local figure. People lower their voice in public.";
        return "Cartel-level terror. Saying your name can get a family erased.";
    }

}
