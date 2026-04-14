using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Main menu + character creation (IMGUI). First scene in build should load this.
/// </summary>
public class MainMenuFlowController : MonoBehaviour
{
    public const string MainMenuSceneName = "MainMenuScene";
    public const string PlanningSceneName = "PlanningScene";

    /// <summary>Set before LoadScene(MainMenu) to open on New Game Setup — same as Main Menu → New Game (Random vs Manual).</summary>
    public static bool OpenToNewGameSetup;

    private enum ScreenMode
    {
        MainMenu,
        NewGameSetup,
        Options,
        About,
        CharacterIdentity,
        /// <summary>Manual new game: narrative before the police-interview questionnaire.</summary>
        CharacterStoryIntro,
        CharacterQuestions
    }

    private ScreenMode _mode = ScreenMode.MainMenu;
    private Vector2 _scrollQuestions;
    private Vector2 _scrollCharacterStory;
    private int _storyIntroSegmentIndex;
    private bool _storyIntroVoicePart1Played;
    private bool _storyIntroVoicePart2Played;
    /// <summary>Unscaled time when Part 1 dub may start; &lt; 0 = not scheduled yet.</summary>
    private float _storyIntroVoicePart1PlayAtUnscaled = -1f;
    /// <summary>Unscaled time when Part 2 dub may start; &lt; 0 = not scheduled yet.</summary>
    private float _storyIntroVoicePart2PlayAtUnscaled = -1f;
    /// <summary>Active one-shot voice GO; cleared when stopped or after clip ends (Unity fake-null).</summary>
    private GameObject _storyIntroVoiceActive;
    private Vector2 _scrollMain;
    private Vector2 _scrollLoadList;
    private Vector2 _identityInsightScroll;
    private int _currentQuestionIndex;
    private bool _showLoadList;

    private string _draftName = "Boss";
    private string _interrogationTypedName = string.Empty;
    private bool _lastNightSilenceLocked;
    private bool _lastNightNoMemoryLocked;
    private bool _silenceRouteVagueChosen;
    private bool _silenceRouteLawyerCustody;
    private bool _aggressiveRouteCustody;
    private bool _coopManualNamesMode;
    private int _coopReasonChoice = -1;
    private int _coopCarOwnerChoice = -1;

    private enum CoopPostCarColorPhase
    {
        None = 0,
        EveningWhere = 1,
        BarContradiction = 2,
        BarWhatHappened = 3,
        BarSnitchOffer = 4,
        BarFirmBarKnowledgeTrap = 5
    }

    private CoopPostCarColorPhase _coopPostCarColorPhase;
    private int _coopEveningWhereChoice = -1;
    private int _coopBarContradictionChoice = -1;
    /// <summary>After firm bar denial: 0 stay silent on how you knew, 1 admit you knew / were there; -1 unset.</summary>
    private int _coopBarFirmKnowledgeTrapChoice = -1;
    private bool _coopBossCustodyFromBarTrapSilence;
    private bool _coopBossCustodyFromBarTrapLawyer;
    private string _coopInventedBarName = string.Empty;
    private int _coopInterrogationPressureAdjustment;
    private int _coopBarWhatHappenedChoice = -1;
    private int _coopSnitchChoice = -1;
    private int _coopBailUsd;

    private static readonly string[] CoopInventedBarPool =
    {
        "The Brass Ledger",
        "Harbor & Hinge",
        "The Painted Stool",
        "Low Tide Tap",
        "Copper Rail Club",
        "The Quiet Measure",
        "Ashkelton Arms",
        "North Dock Social"
    };
    private int _draftAccentIndex = -1; // -1 = Surprise me (random); else 0..AccentColorCount-1
    private enum IdentityInsightKind { None, Trait, Skill }
    private IdentityInsightKind _identityInsightKind;
    private int _identityInsightIndex;
    private int _resolvedRandomAccentIndex = -1;
    private string _draftPortraitResourcePath = "BossPortrait";
    private readonly int[] _draftAnswers = new int[PersonalityQuestionnaire.QuestionCount];
    private bool _manualCrewSetup = true;
    private int _selectedNewGameSetupMode = -1; // -1 none, 0 random, 1 manual
    private readonly string[] _draftPartnerNames = { "Vince", "Rico", "Marek" };
    private readonly int[] _draftPartnerSkillIndices = { 0, 1, 2 };
    private readonly string[] _partnerSkillOptions =
    {
        "Firearms, Driving",
        "Stealth, Lockpicking",
        "Intimidation, Combat",
        "Surveillance, Disguise",
        "Negotiation, Bribes",
        "Forgery, Documents"
    };

    /// <summary>Resources portrait keys for boss face selection (order = carousel order). <c>BossPortraitF*</c> = female.</summary>
    private static readonly string[] BossPortraitResourceOptions = DealerPortraitNaming.BossPlayerPortraitResourceKeys;
    private float _masterVolume = 1f;
    private float _musicVolume = 0.8f;
    private float _sfxVolume = 0.8f;
    private bool _fullscreen;
    private int _qualityIndex;

    private const string PrefMasterVolume = "opt_master_volume";
    private const string PrefMusicVolume = "opt_music_volume";
    private const string PrefSfxVolume = "opt_sfx_volume";
    private const string PrefFullscreen = "opt_fullscreen";
    private const string PrefQuality = "opt_quality";

    [Header("Story intro voice (Part 1 of 5)")]
    [Tooltip("Drag dub from Assets/Audio/Voice/Dubbing (e.g. intro). If empty, tries Resources/Audio/Voice/Dubbing/intro")]
    [SerializeField] private AudioClip _storyIntroVoiceClip;

    [Header("Story intro voice (Part 2 of 5)")]
    [Tooltip("Drag dub for paragraph 2 (e.g. intro2). If empty, tries Resources/Audio/Voice/Dubbing/intro2")]
    [SerializeField] private AudioClip _storyIntroVoiceClipPart2;

    [SerializeField] [Range(0f, 1f)] private float _storyIntroVoiceVolume = 1f;

    [Tooltip("After opening a story part, wait this long (seconds, real time) before starting dub. Max must be >= Min.")]
    [SerializeField] private float _storyIntroVoiceStartDelayMin = 1f;
    [SerializeField] private float _storyIntroVoiceStartDelayMax = 1.5f;

    [Header("Main Menu Background (Resources)")]
    [SerializeField] private string _mainMenuBackgroundResource = "MainMenuBackground";
    [Tooltip("Shown only during police interview / questionnaire (CharacterQuestions). Falls back to main menu art if missing.")]
    [SerializeField] private string _interrogationBackgroundResource = "InterrogationRoomBackground";
    [SerializeField] private string _mainMenuLogoResource = "LOGO";
    [SerializeField] private Vector2 _mainMenuLogoOffset = new Vector2(-52f, 8f);
    [SerializeField] private float _mainMenuLogoWidth = 720f;

    private static Texture2D _pixel;
    private static Texture2D _circleCornerMask;
    private static Texture2D _circleFillMask;
    private static readonly Dictionary<string, Texture2D> _portraitCircleThumbCache = new Dictionary<string, Texture2D>();
    private static GUIStyle _menuButtonStyle;
    private static GUIStyle _loadItemButtonStyle;
    private static GUIStyle _questionTitleStyle;
    private static GUIStyle _questionHintStyle;
    private static GUIStyle _choiceButtonStyle;
    private static GUIStyle _setupCardTitleStyle;
    private static GUIStyle _setupCardBodyStyle;
    private static GUIStyle _setupCardEffectStyle;
    private static GUIStyle _setupHeaderHintStyle;
    private static GUIStyle _newGameBottomButtonStyle;
    private static GUIStyle _characterIdentitySectionHeaderStyle;
    private static GUIStyle _characterIdentityFieldLabelStyle;
    private static GUIStyle _characterIdentityScreenTitleStyle;
    private static GUIStyle _characterIdentityPanelStyle;
    private static GUIStyle _characterIdentityMiddlePanelStyle;
    private static GUIStyle _characterIdentityInsightColumnStyle;
    private static GUIStyle _characterIdentityBonusInnerStyle;
    private static Font _menuFont;

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

    private static Texture2D CircleCornerMask
    {
        get
        {
            if (_circleCornerMask == null)
            {
                const int size = 64;
                _circleCornerMask = new Texture2D(size, size, TextureFormat.RGBA32, false);
                _circleCornerMask.filterMode = FilterMode.Bilinear;
                _circleCornerMask.wrapMode = TextureWrapMode.Clamp;
                Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
                float radius = size * 0.5f - 1f;
                float radiusSq = radius * radius;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x - center.x;
                        float dy = y - center.y;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        // Smooth feather at the edge so circles don't look harsh.
                        float alpha = Mathf.InverseLerp(radius - 1.6f, radius + 0.8f, dist);
                        _circleCornerMask.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(alpha)));
                    }
                }
                _circleCornerMask.Apply(false);
            }

            return _circleCornerMask;
        }
    }

    private static Texture2D CircleFillMask
    {
        get
        {
            if (_circleFillMask == null)
            {
                const int size = 64;
                _circleFillMask = new Texture2D(size, size, TextureFormat.RGBA32, false);
                _circleFillMask.filterMode = FilterMode.Bilinear;
                _circleFillMask.wrapMode = TextureWrapMode.Clamp;
                Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
                float radius = size * 0.5f - 1f;
                float radiusSq = radius * radius;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x - center.x;
                        float dy = y - center.y;
                        bool inside = (dx * dx + dy * dy) <= radiusSq;
                        _circleFillMask.SetPixel(x, y, inside ? Color.white : new Color(1f, 1f, 1f, 0f));
                    }
                }
                _circleFillMask.Apply(false);
            }

            return _circleFillMask;
        }
    }

    private static GUIStyle MenuButtonStyle
    {
        get
        {
            if (_menuButtonStyle == null)
            {
                Font f = GetBuiltInMenuFont();
                _menuButtonStyle = new GUIStyle(GUI.skin.label);
                _menuButtonStyle.font = f;
                _menuButtonStyle.fontSize = 30;
                _menuButtonStyle.fontStyle = FontStyle.Bold;
                _menuButtonStyle.alignment = TextAnchor.MiddleLeft;
                _menuButtonStyle.padding = new RectOffset(0, 0, 4, 4);
                _menuButtonStyle.normal.textColor = new Color(0.9f, 0.9f, 0.88f, 0.95f);
                _menuButtonStyle.hover.textColor = Color.white;
                _menuButtonStyle.active.textColor = new Color(0.78f, 0.8f, 0.84f, 0.95f);
            }

            return _menuButtonStyle;
        }
    }

    private static GUIStyle LoadItemButtonStyle
    {
        get
        {
            if (_loadItemButtonStyle == null)
            {
                Font f = GetBuiltInMenuFont();
                _loadItemButtonStyle = new GUIStyle(GUI.skin.button);
                _loadItemButtonStyle.font = f;
                _loadItemButtonStyle.fontSize = 18;
                _loadItemButtonStyle.fontStyle = FontStyle.Bold;
                _loadItemButtonStyle.alignment = TextAnchor.MiddleLeft;
                _loadItemButtonStyle.wordWrap = false;
                _loadItemButtonStyle.padding = new RectOffset(12, 10, 8, 8);
                _loadItemButtonStyle.normal.background = Pixel;
                _loadItemButtonStyle.hover.background = Pixel;
                _loadItemButtonStyle.active.background = Pixel;
                _loadItemButtonStyle.normal.textColor = new Color(0.92f, 0.92f, 0.9f, 0.95f);
                _loadItemButtonStyle.hover.textColor = Color.white;
                _loadItemButtonStyle.active.textColor = new Color(0.84f, 0.86f, 0.9f, 0.95f);
            }

            return _loadItemButtonStyle;
        }
    }

    private static Font GetBuiltInMenuFont()
    {
        if (_menuFont != null)
            return _menuFont;

        _menuFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_menuFont == null)
            _menuFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return _menuFont;
    }

    private static GUIStyle QuestionTitleStyle
    {
        get
        {
            if (_questionTitleStyle == null)
            {
                Font f = GetBuiltInMenuFont();
                _questionTitleStyle = new GUIStyle(GUI.skin.label);
                _questionTitleStyle.font = f;
                _questionTitleStyle.fontSize = 34;
                _questionTitleStyle.fontStyle = FontStyle.Bold;
                _questionTitleStyle.wordWrap = true;
                _questionTitleStyle.alignment = TextAnchor.UpperLeft;
                _questionTitleStyle.normal.textColor = new Color(0.93f, 0.93f, 0.9f, 0.98f);
            }

            return _questionTitleStyle;
        }
    }

    private static GUIStyle QuestionHintStyle
    {
        get
        {
            if (_questionHintStyle == null)
            {
                _questionHintStyle = new GUIStyle(GUI.skin.label);
                _questionHintStyle.fontSize = 17;
                _questionHintStyle.wordWrap = true;
                _questionHintStyle.normal.textColor = new Color(0.82f, 0.86f, 0.94f, 0.95f);
            }

            return _questionHintStyle;
        }
    }

    private static GUIStyle ChoiceButtonStyle
    {
        get
        {
            if (_choiceButtonStyle == null)
            {
                _choiceButtonStyle = new GUIStyle(GUI.skin.button);
                _choiceButtonStyle.fontSize = 22;
                _choiceButtonStyle.alignment = TextAnchor.MiddleLeft;
                _choiceButtonStyle.wordWrap = true;
                _choiceButtonStyle.padding = new RectOffset(18, 14, 12, 12);
                _choiceButtonStyle.fixedHeight = 58f;
            }

            return _choiceButtonStyle;
        }
    }

    private static GUIStyle SetupCardTitleStyle
    {
        get
        {
            if (_setupCardTitleStyle == null)
            {
                Font f = GetBuiltInMenuFont();
                _setupCardTitleStyle = new GUIStyle(GUI.skin.label);
                _setupCardTitleStyle.font = f;
                _setupCardTitleStyle.fontSize = 24;
                _setupCardTitleStyle.fontStyle = FontStyle.Bold;
                _setupCardTitleStyle.wordWrap = true;
                _setupCardTitleStyle.alignment = TextAnchor.UpperLeft;
                _setupCardTitleStyle.normal.textColor = new Color(0.95f, 0.95f, 0.92f, 0.98f);
            }
            return _setupCardTitleStyle;
        }
    }

    private static GUIStyle SetupCardBodyStyle
    {
        get
        {
            if (_setupCardBodyStyle == null)
            {
                _setupCardBodyStyle = new GUIStyle(GUI.skin.label);
                _setupCardBodyStyle.fontSize = 18;
                _setupCardBodyStyle.wordWrap = true;
                _setupCardBodyStyle.normal.textColor = new Color(0.86f, 0.87f, 0.9f, 0.96f);
            }
            return _setupCardBodyStyle;
        }
    }

    private static GUIStyle _storyIntroBodyStyle;
    private static GUIStyle _storyIntroHeadlineStyle;

    /// <summary>Large readable body for the opening story (English).</summary>
    private static GUIStyle StoryIntroBodyStyle
    {
        get
        {
            if (_storyIntroBodyStyle == null)
            {
                Font f = GetBuiltInMenuFont();
                _storyIntroBodyStyle = new GUIStyle(SetupCardBodyStyle);
                _storyIntroBodyStyle.font = f;
                _storyIntroBodyStyle.fontSize = 24;
                _storyIntroBodyStyle.fontStyle = FontStyle.Normal;
                _storyIntroBodyStyle.alignment = TextAnchor.UpperLeft;
                _storyIntroBodyStyle.wordWrap = true;
                _storyIntroBodyStyle.richText = false;
                _storyIntroBodyStyle.normal.textColor = new Color(0.92f, 0.93f, 0.96f, 0.98f);
            }

            return _storyIntroBodyStyle;
        }
    }

    /// <summary>Title line under the box on the story screen.</summary>
    private static GUIStyle StoryIntroHeadlineStyle
    {
        get
        {
            if (_storyIntroHeadlineStyle == null)
            {
                Font f = GetBuiltInMenuFont();
                _storyIntroHeadlineStyle = new GUIStyle(QuestionHintStyle);
                _storyIntroHeadlineStyle.font = f;
                _storyIntroHeadlineStyle.fontSize = 22;
                _storyIntroHeadlineStyle.fontStyle = FontStyle.Italic;
                _storyIntroHeadlineStyle.wordWrap = true;
                _storyIntroHeadlineStyle.alignment = TextAnchor.UpperLeft;
                _storyIntroHeadlineStyle.normal.textColor = new Color(0.78f, 0.82f, 0.9f, 0.92f);
            }

            return _storyIntroHeadlineStyle;
        }
    }

    private static GUIStyle SetupCardEffectStyle
    {
        get
        {
            if (_setupCardEffectStyle == null)
            {
                _setupCardEffectStyle = new GUIStyle(SetupCardBodyStyle);
                _setupCardEffectStyle.fontSize = 17;
                _setupCardEffectStyle.fontStyle = FontStyle.Bold;
                _setupCardEffectStyle.normal.textColor = new Color(0.96f, 0.82f, 0.62f, 0.98f);
            }
            return _setupCardEffectStyle;
        }
    }

    private static GUIStyle SetupHeaderHintStyle
    {
        get
        {
            if (_setupHeaderHintStyle == null)
            {
                _setupHeaderHintStyle = new GUIStyle(QuestionHintStyle);
                _setupHeaderHintStyle.fontSize = 19;
                _setupHeaderHintStyle.normal.textColor = new Color(0.9f, 0.9f, 0.86f, 0.94f);
            }
            return _setupHeaderHintStyle;
        }
    }

    private static GUIStyle NewGameBottomButtonStyle
    {
        get
        {
            if (_newGameBottomButtonStyle == null)
            {
                _newGameBottomButtonStyle = new GUIStyle(GUI.skin.button);
                _newGameBottomButtonStyle.fontSize = 20;
                _newGameBottomButtonStyle.fontStyle = FontStyle.Bold;
                _newGameBottomButtonStyle.alignment = TextAnchor.MiddleCenter;
                _newGameBottomButtonStyle.normal.textColor = new Color(0.96f, 0.95f, 0.92f, 1f);
                _newGameBottomButtonStyle.hover.textColor = Color.white;
                _newGameBottomButtonStyle.active.textColor = new Color(0.92f, 0.89f, 0.82f, 1f);
            }
            return _newGameBottomButtonStyle;
        }
    }

    private static GUIStyle CharacterIdentitySectionHeaderStyle
    {
        get
        {
            if (_characterIdentitySectionHeaderStyle == null)
            {
                _characterIdentitySectionHeaderStyle = new GUIStyle(GUI.skin.label);
                Font f = GetBuiltInMenuFont();
                if (f != null)
                    _characterIdentitySectionHeaderStyle.font = f;
                _characterIdentitySectionHeaderStyle.fontStyle = FontStyle.Bold;
                _characterIdentitySectionHeaderStyle.fontSize = 19;
                _characterIdentitySectionHeaderStyle.alignment = TextAnchor.MiddleLeft;
                _characterIdentitySectionHeaderStyle.normal.textColor = new Color(0.93f, 0.86f, 0.72f, 0.98f);
            }
            return _characterIdentitySectionHeaderStyle;
        }
    }

    private static GUIStyle CharacterIdentityFieldLabelStyle
    {
        get
        {
            if (_characterIdentityFieldLabelStyle == null)
            {
                _characterIdentityFieldLabelStyle = new GUIStyle(GUI.skin.label);
                _characterIdentityFieldLabelStyle.fontSize = 15;
                _characterIdentityFieldLabelStyle.wordWrap = true;
                _characterIdentityFieldLabelStyle.normal.textColor = new Color(0.78f, 0.8f, 0.84f, 0.92f);
            }
            return _characterIdentityFieldLabelStyle;
        }
    }

    private static void DrawCharacterIdentityHairline()
    {
        Rect line = GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(true));
        GUI.color = new Color(0.98f, 0.84f, 0.52f, 0.28f);
        GUI.DrawTexture(line, Pixel);
        GUI.color = Color.white;
    }

    private static GUIStyle CharacterIdentityScreenTitleStyle
    {
        get
        {
            if (_characterIdentityScreenTitleStyle == null)
            {
                _characterIdentityScreenTitleStyle = new GUIStyle(GUI.skin.box);
                Font f = GetBuiltInMenuFont();
                if (f != null)
                    _characterIdentityScreenTitleStyle.font = f;
                _characterIdentityScreenTitleStyle.fontStyle = FontStyle.Bold;
                _characterIdentityScreenTitleStyle.fontSize = 24;
                _characterIdentityScreenTitleStyle.alignment = TextAnchor.MiddleCenter;
                _characterIdentityScreenTitleStyle.padding = new RectOffset(8, 8, 6, 6);
                _characterIdentityScreenTitleStyle.normal.textColor = new Color(0.94f, 0.92f, 0.88f, 1f);
            }
            return _characterIdentityScreenTitleStyle;
        }
    }

    /// <summary>Tight box padding so dark panels hug content.</summary>
    private static GUIStyle CharacterIdentityPanelStyle
    {
        get
        {
            if (_characterIdentityPanelStyle == null)
            {
                _characterIdentityPanelStyle = new GUIStyle(GUI.skin.box);
                _characterIdentityPanelStyle.padding = new RectOffset(5, 5, 5, 5);
                _characterIdentityPanelStyle.margin = new RectOffset(0, 0, 0, 0);
            }
            return _characterIdentityPanelStyle;
        }
    }

    private static GUIStyle CharacterIdentityBonusInnerStyle
    {
        get
        {
            if (_characterIdentityBonusInnerStyle == null)
            {
                _characterIdentityBonusInnerStyle = new GUIStyle(GUI.skin.box);
                _characterIdentityBonusInnerStyle.padding = new RectOffset(4, 4, 4, 4);
                _characterIdentityBonusInnerStyle.margin = new RectOffset(0, 0, 0, 0);
            }
            return _characterIdentityBonusInnerStyle;
        }
    }

    /// <summary>Build & bonuses outer — minimal padding.</summary>
    private static GUIStyle CharacterIdentityMiddlePanelStyle
    {
        get
        {
            if (_characterIdentityMiddlePanelStyle == null)
            {
                _characterIdentityMiddlePanelStyle = new GUIStyle(GUI.skin.box);
                _characterIdentityMiddlePanelStyle.padding = new RectOffset(3, 3, 3, 3);
                _characterIdentityMiddlePanelStyle.margin = new RectOffset(0, 0, 0, 0);
            }
            return _characterIdentityMiddlePanelStyle;
        }
    }

    /// <summary>Trait/skill insight strip — horizontal inset comes from DrawIdentityInsightColumn (cm-based); vertical only here.</summary>
    private static GUIStyle CharacterIdentityInsightColumnStyle
    {
        get
        {
            if (_characterIdentityInsightColumnStyle == null)
            {
                _characterIdentityInsightColumnStyle = new GUIStyle(GUI.skin.box);
                _characterIdentityInsightColumnStyle.padding = new RectOffset(0, 0, 6, 8);
                _characterIdentityInsightColumnStyle.margin = new RectOffset(0, 0, 0, 0);
            }
            return _characterIdentityInsightColumnStyle;
        }
    }

    private static float CmToScreenPixels(float cm)
    {
        float dpi = Screen.dpi > 1f ? Screen.dpi : 96f;
        return cm / 2.54f * dpi;
    }

    /// <summary>Target ~3 cm per side, clamped so a minimum text column remains on narrow layouts.</summary>
    private static float GetIdentityInsightSideMarginPx(float columnWidth)
    {
        float target = CmToScreenPixels(3f);
        float maxSide = Mathf.Max(0f, (columnWidth - 96f) * 0.5f);
        return Mathf.Clamp(target, 0f, maxSide);
    }

    private static GUIStyle _characterIdentityDisplayNameLabelStyle;
    private static GUIStyle CharacterIdentityDisplayNameLabelStyle
    {
        get
        {
            if (_characterIdentityDisplayNameLabelStyle == null)
            {
                _characterIdentityDisplayNameLabelStyle = new GUIStyle(GUI.skin.label);
                Font f = GetBuiltInMenuFont();
                if (f != null)
                    _characterIdentityDisplayNameLabelStyle.font = f;
                _characterIdentityDisplayNameLabelStyle.fontStyle = FontStyle.Bold;
                _characterIdentityDisplayNameLabelStyle.fontSize = 18;
                _characterIdentityDisplayNameLabelStyle.normal.textColor = new Color(0.92f, 0.88f, 0.78f, 0.98f);
            }
            return _characterIdentityDisplayNameLabelStyle;
        }
    }

    private static GUIStyle _identityStatLinkStyle;
    private static GUIStyle IdentityStatLinkStyle
    {
        get
        {
            if (_identityStatLinkStyle == null)
            {
                _identityStatLinkStyle = new GUIStyle(GUI.skin.label);
                _identityStatLinkStyle.fontSize = 17;
                _identityStatLinkStyle.richText = true;
                _identityStatLinkStyle.normal.textColor = new Color(0.94f, 0.94f, 0.94f, 1f);
                _identityStatLinkStyle.normal.background = null;
                _identityStatLinkStyle.hover.background = null;
                _identityStatLinkStyle.active.background = null;
                _identityStatLinkStyle.hover.textColor = new Color(1f, 0.86f, 0.45f, 1f);
                _identityStatLinkStyle.active.textColor = new Color(1f, 0.92f, 0.65f, 1f);
                _identityStatLinkStyle.alignment = TextAnchor.MiddleLeft;
            }
            return _identityStatLinkStyle;
        }
    }

    private static GUIStyle _identityInsightBodyStyle;
    private static GUIStyle IdentityInsightBodyStyle
    {
        get
        {
            if (_identityInsightBodyStyle == null)
            {
                _identityInsightBodyStyle = new GUIStyle(GUI.skin.label);
                _identityInsightBodyStyle.fontSize = 16;
                _identityInsightBodyStyle.wordWrap = true;
                _identityInsightBodyStyle.normal.textColor = new Color(0.86f, 0.88f, 0.9f, 0.95f);
            }
            return _identityInsightBodyStyle;
        }
    }

    private static GUIStyle _identityInsightTitleStyle;
    private static GUIStyle IdentityInsightTitleStyle
    {
        get
        {
            if (_identityInsightTitleStyle == null)
            {
                _identityInsightTitleStyle = new GUIStyle(TraitSectionHeaderStyle);
                _identityInsightTitleStyle.fontSize = 17;
                _identityInsightTitleStyle.wordWrap = true;
            }
            return _identityInsightTitleStyle;
        }
    }

    private void Awake()
    {
        if (GameOverlayMenu.Instance != null)
            Destroy(GameOverlayMenu.Instance.gameObject);

        GameModeManager.EnsureExists();
        if (GameModeManager.Instance != null)
            GameModeManager.Instance.SetMode(GameModeManager.GameMode.Management);

        if (OpenToNewGameSetup)
        {
            OpenToNewGameSetup = false;
            _selectedNewGameSetupMode = -1;
            _mode = ScreenMode.NewGameSetup;
        }

        LoadOptions();
    }

    private void OnDisable()
    {
        StopStoryIntroVoice();
    }

    private void BeginCharacterCreationFromMenu()
    {
        _draftName = "Boss";
        _interrogationTypedName = string.Empty;
        _lastNightSilenceLocked = false;
        _lastNightNoMemoryLocked = false;
        _silenceRouteVagueChosen = false;
        _silenceRouteLawyerCustody = false;
        _aggressiveRouteCustody = false;
        _coopManualNamesMode = false;
        _coopReasonChoice = -1;
        _coopCarOwnerChoice = -1;
        _coopPostCarColorPhase = CoopPostCarColorPhase.None;
        _coopEveningWhereChoice = -1;
        _coopBarContradictionChoice = -1;
        _coopBarFirmKnowledgeTrapChoice = -1;
        _coopBossCustodyFromBarTrapSilence = false;
        _coopBossCustodyFromBarTrapLawyer = false;
        _coopInventedBarName = string.Empty;
        _coopInterrogationPressureAdjustment = 0;
        _coopBarWhatHappenedChoice = -1;
        _coopSnitchChoice = -1;
        _coopBailUsd = 0;
        _draftAccentIndex = 0;
        _resolvedRandomAccentIndex = -1;
        _identityInsightKind = IdentityInsightKind.None;
        _identityInsightIndex = 0;
        _draftPortraitResourcePath = "BossPortrait";
        for (int i = 0; i < _draftAnswers.Length; i++)
            _draftAnswers[i] = -1;
        _currentQuestionIndex = 0;
        _scrollCharacterStory = Vector2.zero;
        _storyIntroSegmentIndex = 0;
        _storyIntroVoicePart1Played = false;
        _storyIntroVoicePart2Played = false;
        _storyIntroVoicePart1PlayAtUnscaled = -1f;
        _storyIntroVoicePart2PlayAtUnscaled = -1f;
        _mode = ScreenMode.CharacterStoryIntro;
    }

    private void OnGUI()
    {
        DrawBackground(_mode);

        switch (_mode)
        {
            case ScreenMode.MainMenu:
                DrawMainMenu();
                break;
            case ScreenMode.NewGameSetup:
                DrawNewGameSetup();
                break;
            case ScreenMode.Options:
                DrawOptions();
                break;
            case ScreenMode.About:
                DrawAbout();
                break;
            case ScreenMode.CharacterIdentity:
                DrawCharacterIdentity();
                break;
            case ScreenMode.CharacterStoryIntro:
                DrawCharacterStoryIntro();
                break;
            case ScreenMode.CharacterQuestions:
                DrawCharacterQuestions();
                break;
        }
    }

    private static Texture2D _cachedMainMenuBg;
    private string _cachedMainMenuBgKey;
    private static Texture2D _cachedInterrogationBg;
    private string _cachedInterrogationBgKey;
    private static Texture2D _cachedMainMenuLogo;
    private string _cachedMainMenuLogoKey;

    private Texture2D LoadMainMenuBackground()
    {
        if (_cachedMainMenuBg != null && _cachedMainMenuBgKey == _mainMenuBackgroundResource)
            return _cachedMainMenuBg;

        string[] candidateKeys =
        {
            _mainMenuBackgroundResource,
            "MainMenuBackground",
            "mainmenutest",
            "main menu"
        };

        Texture2D t = null;
        for (int i = 0; i < candidateKeys.Length; i++)
        {
            string key = candidateKeys[i];
            if (string.IsNullOrWhiteSpace(key))
                continue;

            t = Resources.Load<Texture2D>(key.Trim());
        if (t == null)
        {
                Sprite s = Resources.Load<Sprite>(key.Trim());
            if (s != null)
                t = s.texture;
        }

            if (t != null)
            {
                _cachedMainMenuBgKey = key.Trim();
                break;
            }
        }

        _cachedMainMenuBg = t;
        return _cachedMainMenuBg;
    }

    private Texture2D LoadInterrogationBackground()
    {
        if (_cachedInterrogationBg != null && _cachedInterrogationBgKey == _interrogationBackgroundResource)
            return _cachedInterrogationBg;

        if (string.IsNullOrWhiteSpace(_interrogationBackgroundResource))
        {
            _cachedInterrogationBg = null;
            _cachedInterrogationBgKey = string.Empty;
            return null;
        }

        string key = _interrogationBackgroundResource.Trim();
        Texture2D t = Resources.Load<Texture2D>(key);
        if (t == null)
        {
            Sprite s = Resources.Load<Sprite>(key);
            if (s != null)
                t = s.texture;
        }

        _cachedInterrogationBg = t;
        _cachedInterrogationBgKey = _interrogationBackgroundResource;
        return _cachedInterrogationBg;
    }

    private Texture2D LoadMainMenuLogo()
    {
        if (_cachedMainMenuLogo != null && _cachedMainMenuLogoKey == _mainMenuLogoResource)
            return _cachedMainMenuLogo;

        Texture2D t = null;
        if (!string.IsNullOrWhiteSpace(_mainMenuLogoResource))
        {
            string key = _mainMenuLogoResource.Trim();
            t = Resources.Load<Texture2D>(key);
            if (t == null)
            {
                Sprite s = Resources.Load<Sprite>(key);
                if (s != null)
                    t = s.texture;
            }
        }

        _cachedMainMenuLogo = t;
        _cachedMainMenuLogoKey = _mainMenuLogoResource;
        return _cachedMainMenuLogo;
    }

    private void DrawBackground(ScreenMode mode)
    {
        float w = Screen.width;
        float h = Screen.height;
        bool useImage = mode == ScreenMode.MainMenu;
        if (useImage)
        {
            Texture2D bg = LoadMainMenuBackground();
            if (bg != null)
            {
                GUI.DrawTexture(new Rect(0, 0, w, h), bg, ScaleMode.ScaleAndCrop);
                return;
            }
        }

        if (mode == ScreenMode.CharacterQuestions)
        {
            Texture2D ibg = LoadInterrogationBackground();
            if (ibg != null)
            {
                DrawNoirWoodBackground(w, h, ibg);
                return;
            }
        }

        DrawNoirWoodBackground(w, h);
    }

    private void DrawNoirWoodBackground(float w, float h, Texture2D overrideBaseTexture = null)
    {
        Texture2D bg = overrideBaseTexture != null ? overrideBaseTexture : LoadMainMenuBackground();
        Color old = GUI.color;
        if (bg != null)
        {
            // Reuse the office art with a darker bronze tint for questionnaire screens.
            GUI.color = new Color(0.78f, 0.65f, 0.52f, 1f);
            GUI.DrawTexture(new Rect(0, 0, w, h), bg, ScaleMode.ScaleAndCrop);
        }
        else
        {
            GUI.color = new Color(0.11f, 0.08f, 0.06f, 1f);
            GUI.DrawTexture(new Rect(0, 0, w, h), Pixel);
        }

        // Dark cinematic pass.
        GUI.color = new Color(0f, 0f, 0f, 0.58f);
        GUI.DrawTexture(new Rect(0, 0, w, h), Pixel);

        // Side vignette bands for depth.
        GUI.color = new Color(0f, 0f, 0f, 0.26f);
        GUI.DrawTexture(new Rect(0, 0, w * 0.20f, h), Pixel);
        GUI.DrawTexture(new Rect(w * 0.82f, 0, w * 0.18f, h), Pixel);
        GUI.color = old;
    }

    private void DrawMainMenu()
    {
        float menuW = _showLoadList ? 540f : 470f;
        float x = 56f;
        float y = 8f;
        float logoHeight = DrawMainMenuLogo(x, y, menuW);
        float menuH = logoHeight + (_showLoadList ? 430f : 330f);
        float buttonsY = y + (logoHeight > 0f ? logoHeight - 6f : 16f);

        GUILayout.BeginArea(new Rect(x, y, menuW, menuH));
        GUILayout.Space(logoHeight > 0f ? logoHeight - 6f : 16f);

        if (_showLoadList)
        {
            DrawMenuButtonsPanel(x + 2f, buttonsY, 280f, 430f);
            DrawLoadListPanel();
            GUILayout.EndArea();
            return;
        }

        DrawMenuButtonsPanel(x + 2f, buttonsY, 280f, 330f);

        bool hasSave = GameSave.HasSaveFile();
        GUI.enabled = hasSave;
        if (GUILayout.Button("Continue", MenuButtonStyle, GUILayout.Height(48f)))
            OnContinue();
        GUI.enabled = true;

        GUILayout.Space(8f);
        if (GUILayout.Button("New Game", MenuButtonStyle, GUILayout.Height(48f)))
            _mode = ScreenMode.NewGameSetup;

        GUILayout.Space(8f);
        GUI.enabled = hasSave;
        if (GUILayout.Button("Load", MenuButtonStyle, GUILayout.Height(48f)))
            _showLoadList = true;
        GUI.enabled = true;

        GUILayout.Space(8f);
        if (GUILayout.Button("Options", MenuButtonStyle, GUILayout.Height(48f)))
            _mode = ScreenMode.Options;

        GUILayout.Space(8f);
        if (GUILayout.Button("About", MenuButtonStyle, GUILayout.Height(48f)))
            _mode = ScreenMode.About;

        GUILayout.Space(8f);
        if (GUILayout.Button("Exit", MenuButtonStyle, GUILayout.Height(48f)))
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        GUILayout.EndArea();
    }

    private void DrawNewGameSetup()
    {
        float w = Screen.width;
        float h = Screen.height;
        float panelW = Mathf.Min(1320f, w - 52f);
        float x = (w - panelW) * 0.5f;
        float y = 64f;
        Rect area = new Rect(x, y, panelW, h - 96f);

        GUI.Label(new Rect(area.x, area.y, area.width, 52f), "New Game Setup", QuestionTitleStyle);
        GUI.Label(
            new Rect(area.x, area.y + 46f, area.width, 54f),
            "Choose how your first crew is created. Each option changes your opening story and how much control you have from day one.",
            SetupHeaderHintStyle);

        float gap = 28f;
        float cardWidth = (area.width - gap) * 0.5f;
        float cardY = area.y + 126f;
        const float continueH = 46f;
        const float backH = 44f;
        const float buttonsGap = 8f;
        const float bottomSafeMargin = 26f;
        float bottomButtonsTop = h - bottomSafeMargin - (continueH + buttonsGap + backH);
        float maxCardHeight = bottomButtonsTop - cardY - 18f;
        float cardHeight = Mathf.Clamp(maxCardHeight, 300f, 390f);
        Rect leftCard = new Rect(area.x, cardY, cardWidth, cardHeight);
        Rect rightCard = new Rect(area.x + cardWidth + gap, cardY, cardWidth, cardHeight);

        bool randomSelected = _selectedNewGameSetupMode == 0;
        bool manualSelected = _selectedNewGameSetupMode == 1;

        if (DrawSetupCard(
            leftCard,
            "Random Creation",
            "Full surprise. You are thrown into the situation and start managing immediately.",
            "Consequence: the game chooses your boss profile, first three fighters, names, and specialties.",
            randomSelected))
        {
            _selectedNewGameSetupMode = 0;
        }

        if (DrawSetupCard(
            rightCard,
            "Manual Creation",
            "You answer a questionnaire and shape your opening crew with intention.",
            "Consequence: your choices can strongly affect your family's survival style and long-term identity.",
            manualSelected,
            "Recommended for first run"))
        {
            _selectedNewGameSetupMode = 1;
        }

        float bottomWidth = Mathf.Min(520f, area.width * 0.52f);
        float bottomX = area.x + (area.width - bottomWidth) * 0.5f;
        Rect continueRect = new Rect(bottomX, bottomButtonsTop, bottomWidth, continueH);
        GUI.enabled = _selectedNewGameSetupMode >= 0;
        if (GUI.Button(continueRect, "Continue", NewGameBottomButtonStyle))
        {
            if (_selectedNewGameSetupMode == 0)
            {
                _manualCrewSetup = false;
                StartRandomNewGame();
            }
            else
            {
                _manualCrewSetup = true;
                BeginCharacterCreationFromMenu();
            }
        }
        GUI.enabled = true;

        Rect backRect = new Rect(bottomX, continueRect.yMax + buttonsGap, bottomWidth, backH);
        if (GUI.Button(backRect, "Back to menu", NewGameBottomButtonStyle))
        {
            _selectedNewGameSetupMode = -1;
            _mode = ScreenMode.MainMenu;
        }
    }

    private bool DrawSetupCard(Rect cardRect, string title, string body, string footer, bool selected, string badge = null)
    {
        bool hovered = cardRect.Contains(Event.current.mousePosition);
        bool clicked = Event.current.type == EventType.MouseDown && Event.current.button == 0 && hovered;

        Color old = GUI.color;
        GUI.color = hovered || selected ? new Color(0.1f, 0.08f, 0.06f, 0.94f) : new Color(0.08f, 0.08f, 0.08f, 0.86f);
        GUI.DrawTexture(cardRect, Pixel);
        Color frame = selected
            ? new Color(0.88f, 0.67f, 0.40f, 0.98f)
            : (hovered ? new Color(0.72f, 0.55f, 0.34f, 0.95f) : new Color(0.42f, 0.33f, 0.22f, 0.9f));
        GUI.color = frame;
        GUI.DrawTexture(new Rect(cardRect.x, cardRect.y, cardRect.width, 2f), Pixel);
        GUI.DrawTexture(new Rect(cardRect.x, cardRect.yMax - 2f, cardRect.width, 2f), Pixel);
        GUI.DrawTexture(new Rect(cardRect.x, cardRect.y, 2f, cardRect.height), Pixel);
        GUI.DrawTexture(new Rect(cardRect.xMax - 2f, cardRect.y, 2f, cardRect.height), Pixel);
        GUI.color = old;

        Rect contentRect = new Rect(cardRect.x + 16f, cardRect.y + 14f, cardRect.width - 32f, cardRect.height - 24f);
        GUILayout.BeginArea(contentRect);
        if (!string.IsNullOrEmpty(badge))
        {
            Rect badgeRect = GUILayoutUtility.GetRect(Mathf.Min(300f, contentRect.width - 8f), 28f, GUILayout.Height(28f));
            Color badgeOld = GUI.color;
            GUI.color = new Color(0.44f, 0.30f, 0.16f, 0.92f);
            GUI.DrawTexture(badgeRect, Pixel);
            GUI.color = badgeOld;
            GUI.Label(new Rect(badgeRect.x + 10f, badgeRect.y + 4f, badgeRect.width - 12f, 22f), badge, SetupCardEffectStyle);
            GUILayout.Space(8f);
        }
        else
        {
            GUILayout.Space(36f);
        }

        GUILayout.Label(title, SetupCardTitleStyle, GUILayout.ExpandWidth(true));
        GUILayout.Space(8f);
        GUILayout.Label(body, SetupCardBodyStyle, GUILayout.ExpandWidth(true));
        GUILayout.Space(8f);
        GUILayout.Label(footer, SetupCardEffectStyle, GUILayout.ExpandWidth(true));
        GUILayout.FlexibleSpace();
        GUILayout.Label(selected ? "Selected" : "Tap card to select", QuestionHintStyle);
        GUILayout.EndArea();

        return clicked;
    }

    private static void DrawMenuButtonsPanel(float x, float y, float width, float height)
    {
        Rect panelRect = new Rect(x - 8f, y - 6f, width, height);
        Color oldColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.34f);
        GUI.DrawTexture(panelRect, Pixel);
        GUI.color = oldColor;
    }

    private float DrawMainMenuLogo(float menuX, float menuY, float menuWidth)
    {
        Texture2D logo = LoadMainMenuLogo();
        if (logo == null)
            return 0f;

        float targetW = Mathf.Max(64f, _mainMenuLogoWidth);
        float aspect = logo.height > 0 ? (float)logo.width / logo.height : 2f;
        float targetH = targetW / Mathf.Max(0.01f, aspect);
        float centeredX = menuX + ((menuWidth - targetW) * 0.5f);
        Rect rect = new Rect(centeredX + _mainMenuLogoOffset.x, menuY + _mainMenuLogoOffset.y, targetW, targetH);
        Rect shadowRect = new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height);
        Color old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.45f);
        GUI.DrawTexture(shadowRect, logo, ScaleMode.ScaleToFit, true);
        GUI.color = old;
        GUI.DrawTexture(rect, logo, ScaleMode.ScaleToFit, true);
        return targetH + _mainMenuLogoOffset.y;
    }

    private void DrawOptions()
    {
        float w = Screen.width;
        float stripe = Mathf.Min(w * 0.42f, 520f);
        GUILayout.BeginArea(new Rect(32f, 40f, w - stripe - 64f, Screen.height - 80f));
        GUILayout.Label("Options", QuestionTitleStyle);
        GUILayout.Space(12f);

        GUILayout.Label("Master Volume: " + Mathf.RoundToInt(_masterVolume * 100f) + "%");
        float newMaster = GUILayout.HorizontalSlider(_masterVolume, 0f, 1f, GUILayout.Width(420f));
        if (!Mathf.Approximately(newMaster, _masterVolume))
        {
            _masterVolume = newMaster;
            ApplyOptions();
        }
        GUILayout.Space(10f);

        GUILayout.Label("Music Volume: " + Mathf.RoundToInt(_musicVolume * 100f) + "%");
        float newMusic = GUILayout.HorizontalSlider(_musicVolume, 0f, 1f, GUILayout.Width(420f));
        if (!Mathf.Approximately(newMusic, _musicVolume))
        {
            _musicVolume = newMusic;
            SaveOptions();
        }
        GUILayout.Space(10f);

        GUILayout.Label("SFX Volume: " + Mathf.RoundToInt(_sfxVolume * 100f) + "%");
        float newSfx = GUILayout.HorizontalSlider(_sfxVolume, 0f, 1f, GUILayout.Width(420f));
        if (!Mathf.Approximately(newSfx, _sfxVolume))
        {
            _sfxVolume = newSfx;
            SaveOptions();
        }
        GUILayout.Space(14f);

        bool newFullscreen = GUILayout.Toggle(_fullscreen, "Fullscreen");
        if (newFullscreen != _fullscreen)
        {
            _fullscreen = newFullscreen;
            ApplyOptions();
        }
        GUILayout.Space(10f);

        string[] qualityNames = QualitySettings.names;
        if (qualityNames != null && qualityNames.Length > 0)
        {
            GUILayout.Label("Quality");
            GUILayout.BeginHorizontal();
            for (int i = 0; i < qualityNames.Length; i++)
            {
                bool selected = i == _qualityIndex;
                if (GUILayout.Toggle(selected, qualityNames[i], GUI.skin.button, GUILayout.Height(34f)))
                {
                    if (_qualityIndex != i)
                    {
                        _qualityIndex = i;
                        ApplyOptions();
                    }
                }
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.EndArea();

        GUILayout.BeginArea(new Rect(w - stripe + 24f, 40f, stripe - 48f, Screen.height - 80f));
        GUILayout.Space(8f);
        if (GUILayout.Button("Back to menu", GUILayout.Height(44f)))
            _mode = ScreenMode.MainMenu;
        GUILayout.EndArea();
    }

    private void DrawAbout()
    {
        float w = Screen.width;
        float stripe = Mathf.Min(w * 0.42f, 520f);
        GUILayout.BeginArea(new Rect(32f, 40f, w - stripe - 64f, Screen.height - 80f));
        GUILayout.Label("About", QuestionTitleStyle);
        GUILayout.Space(16f);
        GUILayout.Label("Family Business", QuestionHintStyle);
        GUILayout.Label("Developer / Publisher: G.K Studios", QuestionHintStyle);
        GUILayout.Label("Version: " + Application.version, QuestionHintStyle);
        GUILayout.Space(12f);
        GUILayout.Label("All rights reserved. Copyright (c) G.K Studios.", WrappedStyle());
        GUILayout.Space(8f);
        GUILayout.Label("This game and all associated assets, code, design, and content are protected by copyright law. Unauthorized copying, distribution, modification, resale, or reverse engineering is prohibited.", WrappedStyle());
        GUILayout.Space(8f);
        GUILayout.Label("Third-party tools, engines, libraries, and trademarks remain the property of their respective owners.", WrappedStyle());
        GUILayout.EndArea();

        GUILayout.BeginArea(new Rect(w - stripe + 24f, 40f, stripe - 48f, Screen.height - 80f));
        GUILayout.Space(8f);
        if (GUILayout.Button("Back to menu", GUILayout.Height(44f)))
            _mode = ScreenMode.MainMenu;
        GUILayout.EndArea();
    }

    private void DrawLoadListPanel()
    {
        GUILayout.Label("Load Game", QuestionTitleStyle);
        GUILayout.Label("Choose a save slot", QuestionHintStyle);
        GUILayout.Space(8f);

        string[] files = GameSave.GetSaveFilePaths();
        if (files.Length == 0)
        {
            GUILayout.Label("No save files found.", QuestionHintStyle);
            GUILayout.Space(10f);
        }
        else
        {
            Rect bgRect = GUILayoutUtility.GetRect(500f, 360f, GUILayout.ExpandWidth(true));
            Color old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.32f);
            GUI.DrawTexture(bgRect, Pixel);
            GUI.color = old;

            GUILayout.BeginArea(new Rect(bgRect.x + 6f, bgRect.y + 6f, bgRect.width - 12f, bgRect.height - 12f));
            _scrollLoadList = GUILayout.BeginScrollView(_scrollLoadList);
            for (int i = 0; i < files.Length; i++)
            {
                string path = files[i];
                string name = Path.GetFileNameWithoutExtension(path);
                string stamp = File.GetLastWriteTime(path).ToString("yyyy-MM-dd HH:mm");
                string label = name + "  -  " + stamp;
                if (GUILayout.Button(label, LoadItemButtonStyle, GUILayout.Height(40f)))
                {
                    if (!GameSave.TryLoadFromPath(path, out string err))
                        Debug.LogWarning("[MainMenu] Load failed: " + err);
                }
                GUILayout.Space(4f);
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        GUILayout.Space(10f);
        if (GUILayout.Button("Back", MenuButtonStyle, GUILayout.Height(44f)))
            _showLoadList = false;
    }

    private static void OnSave()
    {
        if (GameSave.TrySave(out string err))
            Debug.Log("[MainMenu] Saved.");
        else
            Debug.LogWarning("[MainMenu] Save failed: " + err);
    }

    private static void OnSaveAndExit()
    {
        if (GameSave.TrySave(out string err))
            Debug.Log("[MainMenu] Saved.");
        else
            Debug.LogWarning("[MainMenu] Save failed: " + err);
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static void OnContinue()
    {
        if (GameSave.TryLoad(out string err))
            Debug.Log("[MainMenu] Continue — loaded.");
        else
            Debug.LogWarning("[MainMenu] Continue failed: " + err);
    }

    private static void OnLoad()
    {
        if (GameSave.TryLoad(out string err))
            Debug.Log("[MainMenu] Load — OK");
        else
            Debug.LogWarning("[MainMenu] Load failed: " + err);
    }

    private void LoadOptions()
    {
        _masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefMasterVolume, 1f));
        _musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefMusicVolume, 0.8f));
        _sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefSfxVolume, 0.8f));
        _fullscreen = PlayerPrefs.GetInt(PrefFullscreen, Screen.fullScreen ? 1 : 0) == 1;
        int maxQuality = Mathf.Max(0, QualitySettings.names.Length - 1);
        _qualityIndex = Mathf.Clamp(PlayerPrefs.GetInt(PrefQuality, QualitySettings.GetQualityLevel()), 0, maxQuality);
        ApplyOptions();
    }

    private void SaveOptions()
    {
        PlayerPrefs.SetFloat(PrefMasterVolume, _masterVolume);
        PlayerPrefs.SetFloat(PrefMusicVolume, _musicVolume);
        PlayerPrefs.SetFloat(PrefSfxVolume, _sfxVolume);
        PlayerPrefs.SetInt(PrefFullscreen, _fullscreen ? 1 : 0);
        PlayerPrefs.SetInt(PrefQuality, _qualityIndex);
        PlayerPrefs.Save();
    }

    private void ApplyOptions()
    {
        AudioListener.volume = _masterVolume;
        Screen.fullScreen = _fullscreen;
        QualitySettings.SetQualityLevel(_qualityIndex, true);
        SaveOptions();
    }

    private static readonly CoreTrait[] IdentityCoreTraitOrder =
    {
        CoreTrait.Strength, CoreTrait.Agility, CoreTrait.Intelligence, CoreTrait.Charisma, CoreTrait.MentalResilience, CoreTrait.Determination
    };

    private static readonly DerivedSkill[] IdentityDerivedSkillOrder =
    {
        DerivedSkill.Brawling, DerivedSkill.Firearms, DerivedSkill.Stealth, DerivedSkill.Driving,
        DerivedSkill.Lockpicking, DerivedSkill.Surveillance, DerivedSkill.Negotiation, DerivedSkill.Intimidation,
        DerivedSkill.Deception, DerivedSkill.Logistics, DerivedSkill.Leadership,
        DerivedSkill.Medicine, DerivedSkill.Sabotage
    };

    private string GetIdentityInsightTitle()
    {
        if (_identityInsightKind == IdentityInsightKind.None)
            return "";
        if (_identityInsightKind == IdentityInsightKind.Trait)
        {
            if (_identityInsightIndex < 0 || _identityInsightIndex >= IdentityCoreTraitOrder.Length)
                return "Core trait";
            return OperationRegistry.GetTraitName(IdentityCoreTraitOrder[_identityInsightIndex]);
        }
        if (_identityInsightKind == IdentityInsightKind.Skill)
        {
            if (_identityInsightIndex < 0 || _identityInsightIndex >= IdentityDerivedSkillOrder.Length)
                return "Core skill";
            return DerivedSkillProgression.GetDisplayName(IdentityDerivedSkillOrder[_identityInsightIndex]);
        }
        return "";
    }

    private string GetIdentityInsightBody()
    {
        if (_identityInsightKind == IdentityInsightKind.None)
            return "";
        if (_identityInsightKind == IdentityInsightKind.Trait)
        {
            if (_identityInsightIndex < 0 || _identityInsightIndex >= IdentityCoreTraitOrder.Length)
                return "";
            return TraitSkillInsightTexts.GetCoreTraitInsight(IdentityCoreTraitOrder[_identityInsightIndex]);
        }
        if (_identityInsightKind == IdentityInsightKind.Skill)
        {
            if (_identityInsightIndex < 0 || _identityInsightIndex >= IdentityDerivedSkillOrder.Length)
                return "";
            return TraitSkillInsightTexts.GetDerivedSkillInsight(IdentityDerivedSkillOrder[_identityInsightIndex]);
        }
        return "";
    }

    /// <summary>To the right of Core skills; text only after a trait/skill click.</summary>
    private void DrawIdentityInsightColumn(float colW)
    {
        Color prevOuter = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.06f, 0.06f, 0.08f, 0.94f);
        float sideMargin = GetIdentityInsightSideMarginPx(colW);
        float innerW = Mathf.Max(64f, colW - 2f * sideMargin);
        float vBar = GUI.skin.verticalScrollbar != null && GUI.skin.verticalScrollbar.fixedWidth > 0.5f
            ? GUI.skin.verticalScrollbar.fixedWidth + 2f
            : 18f;
        float bodyLineW = Mathf.Max(48f, innerW - vBar);

        GUILayout.BeginVertical(CharacterIdentityInsightColumnStyle, GUILayout.Width(colW), GUILayout.MinHeight(112f));
        GUILayout.BeginHorizontal();
        GUILayout.Space(sideMargin);
        GUILayout.BeginVertical(GUILayout.Width(innerW));
        GUILayout.Space(2f);

        if (_identityInsightKind != IdentityInsightKind.None)
        {
            string title = GetIdentityInsightTitle();
            if (!string.IsNullOrEmpty(title))
                GUILayout.Label(title, IdentityInsightTitleStyle, GUILayout.Width(innerW));
            GUILayout.Space(3f);
            _identityInsightScroll = GUILayout.BeginScrollView(_identityInsightScroll, false, true, GUILayout.Width(innerW), GUILayout.Height(200f));
            GUILayout.Label(GetIdentityInsightBody(), IdentityInsightBodyStyle, GUILayout.Width(bodyLineW));
            GUILayout.EndScrollView();
        }
        else
        {
            GUILayout.Space(80f);
        }

        GUILayout.Space(2f);
        GUILayout.EndVertical();
        GUILayout.Space(sideMargin);
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
        GUI.backgroundColor = prevOuter;
    }

    private static GUIStyle _portraitArrowButtonStyle;
    private static GUIStyle PortraitArrowButtonStyle
    {
        get
        {
            if (_portraitArrowButtonStyle == null)
            {
                _portraitArrowButtonStyle = new GUIStyle(GUI.skin.button);
                _portraitArrowButtonStyle.fontSize = 22;
                _portraitArrowButtonStyle.fontStyle = FontStyle.Bold;
                _portraitArrowButtonStyle.alignment = TextAnchor.MiddleCenter;
            }

            return _portraitArrowButtonStyle;
        }
    }

    private int GetBossPortraitOptionIndex()
    {
        string p = _draftPortraitResourcePath ?? "";
        p = DealerPortraitNaming.NormalizeBossPlayerPickerKey(p.Trim());
        for (int i = 0; i < BossPortraitResourceOptions.Length; i++)
        {
            if (BossPortraitResourceOptions[i] == p)
                return i;
        }

        return 0;
    }

    private void CycleBossPortrait(int delta)
    {
        int n = BossPortraitResourceOptions.Length;
        int idx = GetBossPortraitOptionIndex();
        idx = ((idx + delta) % n + n) % n;
        _draftPortraitResourcePath = BossPortraitResourceOptions[idx];
    }

    private void DrawCharacterIdentity()
    {
        float w = Screen.width;
        float leftPad = 22f;
        float rightPad = 22f;
        float areaW = w - leftPad - rightPad;
        float columnGap = 10f;
        float leftColW = Mathf.Min(360f, Mathf.Max(210f, (areaW - columnGap) * 0.33f));
        float middleW = areaW - leftColW - columnGap;
        if (middleW < 220f)
        {
            leftColW = Mathf.Max(200f, leftColW - (220f - middleW));
            middleW = areaW - leftColW - columnGap;
        }

        GUILayout.BeginArea(new Rect(leftPad, 36f, areaW, Screen.height - 72f));
        GUILayout.Label("Look at you", CharacterIdentityScreenTitleStyle, GUILayout.Height(40f), GUILayout.ExpandWidth(true));
        GUILayout.Space(8f);

        GUILayout.BeginHorizontal();
        // --- Left: visuals (portrait, name, accent, face picker) ---
        Color prevLeftOuter = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.06f, 0.06f, 0.08f, 0.94f);
        GUILayout.BeginVertical(CharacterIdentityPanelStyle, GUILayout.Width(leftColW));
        GUILayout.Space(8f);
        GUILayout.Label("Look & identity", CharacterIdentitySectionHeaderStyle);
        GUILayout.Space(4f);
        DrawCharacterIdentityHairline();
        GUILayout.Space(6f);
        GUILayout.Label("Preview", CharacterIdentityFieldLabelStyle);
        GUILayout.Space(3f);
        const float previewH = 320f;
        const float arrowW = 44f;
        float rowInnerW = Mathf.Max(200f, leftColW - 16f);
        Rect rowRect = GUILayoutUtility.GetRect(rowInnerW, previewH, GUILayout.ExpandWidth(true));
        float gap = 8f;
        Rect leftBtnRect = new Rect(rowRect.x, rowRect.y, arrowW, rowRect.height);
        Rect rightBtnRect = new Rect(rowRect.xMax - arrowW, rowRect.y, arrowW, rowRect.height);
        Rect pr = new Rect(rowRect.x + arrowW + gap, rowRect.y, rowRect.width - 2f * arrowW - 2f * gap, rowRect.height);

        if (GUI.Button(leftBtnRect, "◀", PortraitArrowButtonStyle))
            CycleBossPortrait(-1);
        if (GUI.Button(rightBtnRect, "▶", PortraitArrowButtonStyle))
            CycleBossPortrait(1);

        Texture2D portrait = LoadPortraitTexture(_draftPortraitResourcePath);
        Color prev2 = GUI.color;
        GUI.color = new Color(0.12f, 0.12f, 0.14f, 1f);
        GUI.DrawTexture(pr, Pixel);
        Rect previewInnerRect = new Rect(pr.x + 4f, pr.y + 4f, pr.width - 8f, pr.height - 8f);
        Rect accentFrameRect = new Rect(pr.x + 2f, pr.y + 2f, pr.width - 4f, pr.height - 4f);
        if (portrait != null)
        {
            GUI.color = Color.white;
            DrawPortraitPreviewFill(previewInnerRect, portrait);
            Color accent = PlayerCharacterProfile.GetAccentColor(GetEffectiveAccentIndex());
            GUI.color = accent;
            GUI.DrawTexture(new Rect(accentFrameRect.x, accentFrameRect.y, accentFrameRect.width, 2f), Pixel);
            GUI.DrawTexture(new Rect(accentFrameRect.x, accentFrameRect.yMax - 2f, accentFrameRect.width, 2f), Pixel);
            GUI.DrawTexture(new Rect(accentFrameRect.x, accentFrameRect.y, 2f, accentFrameRect.height), Pixel);
            GUI.DrawTexture(new Rect(accentFrameRect.xMax - 2f, accentFrameRect.y, 2f, accentFrameRect.height), Pixel);
        }
        else
        {
            Color ac = PlayerCharacterProfile.GetAccentColor(GetEffectiveAccentIndex());
            GUI.color = ac;
            GUI.DrawTexture(previewInnerRect, Pixel);
            GUI.color = new Color(0.2f, 0.2f, 0.22f, 0.85f);
            GUI.DrawTexture(previewInnerRect, Pixel);
            GUI.color = prev2;
            GUI.Label(new Rect(previewInnerRect.x + 8f, previewInnerRect.y + previewInnerRect.height * 0.4f, previewInnerRect.width - 16f, 60f), "Place portrait in Assets/Resources");
        }
        GUI.color = prev2;

        GUILayout.Space(6f);
        GUILayout.Label(
            "Portrait " + (GetBossPortraitOptionIndex() + 1) + " / " + BossPortraitResourceOptions.Length + " — use arrows to browse",
            CharacterIdentityFieldLabelStyle);

        GUILayout.Space(10f);
        GUILayout.Label("What they'll call you", CharacterIdentityDisplayNameLabelStyle);
        GUILayout.Space(4f);
        _draftName = GUILayout.TextField(_draftName, 48, GUILayout.Height(36f), GUILayout.ExpandWidth(true));

        GUILayout.Space(10f);
        DrawCharacterIdentityHairline();
        GUILayout.Space(8f);
        GUILayout.EndVertical();
        GUI.backgroundColor = prevLeftOuter;

        GUILayout.Space(columnGap);

        // --- Middle: stats, skills, bonuses (content) ---
        Color prevMidOuter = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.06f, 0.06f, 0.08f, 0.94f);
        GUILayout.BeginVertical(CharacterIdentityMiddlePanelStyle, GUILayout.Width(middleW));
        GUILayout.Space(6f);
        GUILayout.Label("Build & bonuses", CharacterIdentitySectionHeaderStyle);
        GUILayout.Space(3f);
        DrawCharacterIdentityHairline();
        GUILayout.Space(4f);
        if (middleW >= 500f)
        {
            float insW = Mathf.Clamp(middleW * 0.28f, 120f, 220f);
            DrawTraitStarsPanel(middleW - 6f, framed: false, insightColumnWidth: insW);
        }
        else
        {
            DrawTraitStarsPanel(middleW - 8f, framed: false);
            GUILayout.Space(6f);
            DrawIdentityInsightColumn(middleW - 8f);
        }
        GUILayout.Space(4f);
        Color prevBonusBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.05f, 0.05f, 0.07f, 0.98f);
        GUILayout.BeginVertical(CharacterIdentityBonusInnerStyle);
        GUILayout.Space(4f);
        GUILayout.Label("Bonuses", TraitSectionHeaderStyle);
        GUILayout.Space(4f);
        GUILayout.Label("From questionnaire answers (detailed breakdown later).", IdentityInsightBodyStyle);
        GUILayout.Space(4f);
        GUILayout.EndVertical();
        GUI.backgroundColor = prevBonusBg;
        GUILayout.Space(6f);
        GUILayout.EndVertical();
        GUI.backgroundColor = prevMidOuter;

        GUILayout.EndHorizontal();

        GUILayout.FlexibleSpace();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Back", GUILayout.Height(42f), GUILayout.Width(168f)))
            _mode = ScreenMode.CharacterQuestions;
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Start your journey", GUILayout.Height(52f), GUILayout.MinWidth(220f)))
            StartManualNewGame();
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }

    private static GUIStyle _traitStarsStyle;
    private static GUIStyle _traitSectionHeaderStyle;

    private static GUIStyle TraitStarsStyle
    {
        get
        {
            if (_traitStarsStyle == null)
            {
                _traitStarsStyle = new GUIStyle(GUI.skin.label);
                _traitStarsStyle.richText = true;
                _traitStarsStyle.fontSize = 17;
                _traitStarsStyle.wordWrap = false;
                _traitStarsStyle.normal.textColor = new Color(0.94f, 0.94f, 0.94f, 1f);
            }
            return _traitStarsStyle;
        }
    }

    private static GUIStyle TraitSectionHeaderStyle
    {
        get
        {
            if (_traitSectionHeaderStyle == null)
            {
                _traitSectionHeaderStyle = new GUIStyle(TraitStarsStyle);
                _traitSectionHeaderStyle.fontStyle = FontStyle.Bold;
                _traitSectionHeaderStyle.fontSize = 18;
                _traitSectionHeaderStyle.normal.textColor = new Color(0.95f, 0.9f, 0.78f, 1f);
            }
            return _traitSectionHeaderStyle;
        }
    }

    private void DrawTraitStarsPanel(float contentWidth, bool framed = true, float insightColumnWidth = 0f)
    {
        PlayerCharacterProfile profile = PersonalityQuestionnaire.BuildProfile(_draftName, GetEffectiveAccentIndex(), _draftAnswers, _draftPortraitResourcePath);
        ApplyInterviewBonusesToProfile(profile);
        CoreTraitProgression.EnsureRubricsInitialized(profile);
        CoreTrait[] traits = IdentityCoreTraitOrder;

        float labelW = Mathf.Clamp(contentWidth * 0.42f, 100f, 150f);
        bool stackVertically = contentWidth < 520f;
        const float dividerColW = 4f;
        float innerColW = stackVertically
            ? contentWidth - 8f
            : Mathf.Max(200f, (contentWidth - 24f) * 0.5f - 4f);
        float traitsColW;
        float skillsColW;
        float insightPanelW = insightColumnWidth;
        if (!stackVertically && insightColumnWidth > 0.5f)
        {
            // traits | div | skills (tight) | div | insight — right bar flush after stars; insight takes the rest.
            float inner = contentWidth - dividerColW * 2f;
            const float skillsCap = 270f;
            float minInsight = Mathf.Clamp(insightColumnWidth, 100f, 220f);
            traitsColW = Mathf.Clamp(inner * 0.33f, 168f, 248f);
            float maxSkills = inner - traitsColW - minInsight;
            skillsColW = Mathf.Min(skillsCap, Mathf.Max(0f, maxSkills));
            insightPanelW = inner - traitsColW - skillsColW;
            if (insightPanelW < minInsight)
            {
                traitsColW = Mathf.Max(130f, inner - minInsight - skillsColW);
                insightPanelW = inner - traitsColW - skillsColW;
            }
        }
        else
        {
            traitsColW = stackVertically
                ? innerColW
                : Mathf.Clamp(contentWidth * 0.32f, 160f, 240f);
            skillsColW = stackVertically
                ? innerColW
                : Mathf.Max(200f, contentWidth - traitsColW - dividerColW);
        }
        float minRowH = stackVertically ? 260f : 320f;

        Color prevBg = GUI.backgroundColor;
        if (framed)
        {
            GUI.backgroundColor = new Color(0.06f, 0.06f, 0.08f, 0.92f);
            GUILayout.BeginVertical(CharacterIdentityPanelStyle, GUILayout.Width(contentWidth));
            GUILayout.Space(6f);
        }
        else
        {
            GUILayout.BeginVertical(GUILayout.Width(contentWidth));
        }

        void DrawCoreTraitRows()
        {
            GUILayout.Label("Core traits", TraitSectionHeaderStyle);
        GUILayout.Space(4f);
            GUILayout.Label(
                "Potential 0–5: unlocks at odd skill XP thresholds (1,3,5,7,9) on pillar progress — max among gate skills or full trait-tagged amounts (before split). Cap per skill = 2 x potential; odd tiers reset visible fill while bank keeps growing.",
                CharacterIdentityFieldLabelStyle);
            GUILayout.Space(10f);
        for (int i = 0; i < traits.Length; i++)
        {
            int level = CoreTraitProgression.GetLevel(profile, traits[i]);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(OperationRegistry.GetTraitName(traits[i]) + ":", IdentityStatLinkStyle, GUILayout.Width(labelW), GUILayout.Height(24f)))
                {
                    _identityInsightKind = IdentityInsightKind.Trait;
                    _identityInsightIndex = i;
                }
                GUILayout.Label(PotentialStarRichText.Build(level, TraitPotentialRubric.MaxTraitLevel), TraitStarsStyle, GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();
                if (i < traits.Length - 1) GUILayout.Space(4f);
            }
        }

        void DrawDerivedSkillRows()
        {
            GUILayout.Label("Success potential", TraitSectionHeaderStyle);
            GUILayout.Space(4f);
            GUILayout.Label(
                "Stars show current level vs cap (cap = 2 × the matching core trait’s potential).",
                CharacterIdentityFieldLabelStyle);
            GUILayout.Space(6f);
            DerivedSkill[] derivedSkills = IdentityDerivedSkillOrder;
            for (int si = 0; si < derivedSkills.Length; si++)
            {
                DerivedSkill ds = derivedSkills[si];
                int skillLevel = DerivedSkillProgression.GetLevel(profile, ds);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(DerivedSkillProgression.GetDisplayName(ds) + ":", IdentityStatLinkStyle, GUILayout.Width(labelW), GUILayout.Height(24f)))
                {
                    _identityInsightKind = IdentityInsightKind.Skill;
                    _identityInsightIndex = si;
                }
                int capSk = SkillPotentialRules.GetSkillCapStars(profile, ds);
                GUILayout.Label(SkillStarRichText.Build(skillLevel, capSk, StarRubric.MaxLevel), TraitStarsStyle, GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();
                if (si < derivedSkills.Length - 1)
                    GUILayout.Space(4f);
            }
        }

        if (stackVertically)
        {
            GUILayout.BeginVertical(GUILayout.MinHeight(minRowH));
            GUILayout.BeginVertical(GUILayout.Width(traitsColW));
            DrawCoreTraitRows();
            GUILayout.EndVertical();
            GUILayout.Space(5f);
            Rect divH = GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(true));
            GUI.color = new Color(0.98f, 0.84f, 0.52f, 0.35f);
            GUI.DrawTexture(divH, Pixel);
            GUI.color = Color.white;
            GUILayout.Space(5f);
            GUILayout.BeginVertical(GUILayout.Width(skillsColW));
            DrawDerivedSkillRows();
            GUILayout.EndVertical();
        GUILayout.EndVertical();
        }
        else
        {
            GUILayout.BeginHorizontal(GUILayout.MinHeight(minRowH));
            GUILayout.BeginVertical(GUILayout.Width(traitsColW));
            DrawCoreTraitRows();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUILayout.Width(dividerColW), GUILayout.ExpandHeight(true));
            Rect dividerRect = GUILayoutUtility.GetRect(2f, minRowH, GUILayout.Width(2f), GUILayout.ExpandHeight(true));
            GUI.color = new Color(0.98f, 0.84f, 0.52f, 0.4f);
            GUI.DrawTexture(dividerRect, Pixel);
            GUI.color = Color.white;
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUILayout.Width(skillsColW));
            DrawDerivedSkillRows();
            GUILayout.EndVertical();

            if (insightColumnWidth > 0.5f)
            {
                GUILayout.BeginVertical(GUILayout.Width(dividerColW), GUILayout.ExpandHeight(true));
                Rect dividerRectInsight = GUILayoutUtility.GetRect(2f, minRowH, GUILayout.Width(2f), GUILayout.ExpandHeight(true));
                GUI.color = new Color(0.98f, 0.84f, 0.52f, 0.4f);
                GUI.DrawTexture(dividerRectInsight, Pixel);
                GUI.color = Color.white;
                GUILayout.EndVertical();
                DrawIdentityInsightColumn(insightPanelW);
            }

            GUILayout.EndHorizontal();
        }

        GUILayout.EndVertical();
        if (framed)
            GUI.backgroundColor = prevBg;
    }


    private void DrawCharacterStoryIntro()
    {
        int segTotal = CharacterOpeningStory.StorySegmentCount;
        if (segTotal <= 0)
            return;

        _storyIntroSegmentIndex = Mathf.Clamp(_storyIntroSegmentIndex, 0, segTotal - 1);
        int segAtStart = _storyIntroSegmentIndex;

        float delayMin = Mathf.Max(0f, _storyIntroVoiceStartDelayMin);
        float delayMax = Mathf.Max(delayMin, _storyIntroVoiceStartDelayMax);

        if (_storyIntroSegmentIndex == 0 && !_storyIntroVoicePart1Played)
        {
            if (_storyIntroVoicePart1PlayAtUnscaled < 0f)
                _storyIntroVoicePart1PlayAtUnscaled = Time.unscaledTime + Random.Range(delayMin, delayMax);
            if (Time.unscaledTime >= _storyIntroVoicePart1PlayAtUnscaled)
            {
                TryPlayStoryIntroVoicePart1();
                _storyIntroVoicePart1Played = true;
                _storyIntroVoicePart1PlayAtUnscaled = -1f;
            }
        }

        if (_storyIntroSegmentIndex == 1 && !_storyIntroVoicePart2Played)
        {
            if (_storyIntroVoicePart2PlayAtUnscaled < 0f)
                _storyIntroVoicePart2PlayAtUnscaled = Time.unscaledTime + Random.Range(delayMin, delayMax);
            if (Time.unscaledTime >= _storyIntroVoicePart2PlayAtUnscaled)
            {
                TryPlayStoryIntroVoicePart2();
                _storyIntroVoicePart2Played = true;
                _storyIntroVoicePart2PlayAtUnscaled = -1f;
            }
        }

        float w = Screen.width;
        float h = Screen.height;
        float pad = 40f;
        float panelW = Mathf.Min(1020f, w - pad * 2f);
        float x = (w - panelW) * 0.5f;
        float y = 28f;
        float panelH = h - y - 28f;

        GUILayout.BeginArea(new Rect(x, y, panelW, panelH));
        GUILayout.Label(CharacterOpeningStory.ScreenTitleEn, QuestionTitleStyle, GUILayout.Height(56f), GUILayout.ExpandWidth(true));
        GUILayout.Space(6f);
        GUILayout.Label(CharacterOpeningStory.ScreenSubtitleEn, StoryIntroHeadlineStyle, GUILayout.ExpandWidth(true));
        GUILayout.Space(4f);
        GUILayout.Label(
            "Part " + (_storyIntroSegmentIndex + 1) + " of " + segTotal,
            StoryIntroHeadlineStyle,
            GUILayout.ExpandWidth(true));
        GUILayout.Space(14f);

        // Keep enough room for both bottom button rows so they never get clipped.
        const float reservedBottomUiH = 300f;
        float scrollH = panelH - reservedBottomUiH;
        if (scrollH < 120f)
            scrollH = 120f;

        _scrollCharacterStory = GUILayout.BeginScrollView(_scrollCharacterStory, GUILayout.Height(scrollH), GUILayout.ExpandWidth(true));
        GUILayout.Label(CharacterOpeningStory.StorySegmentsEnglish[_storyIntroSegmentIndex], StoryIntroBodyStyle, GUILayout.ExpandWidth(true));
        GUILayout.EndScrollView();

        GUILayout.Space(14f);
        GUILayout.BeginHorizontal();
        GUI.enabled = _storyIntroSegmentIndex > 0;
        if (GUILayout.Button(CharacterOpeningStory.ButtonPreviousEn, NewGameBottomButtonStyle, GUILayout.Height(48f), GUILayout.MinWidth(140f)))
        {
            _storyIntroSegmentIndex--;
            _scrollCharacterStory = Vector2.zero;
        }
        GUI.enabled = true;

        GUILayout.FlexibleSpace();

        bool lastPart = _storyIntroSegmentIndex >= segTotal - 1;
        if (!lastPart)
        {
            if (GUILayout.Button(CharacterOpeningStory.ButtonNextEn, NewGameBottomButtonStyle, GUILayout.Height(48f), GUILayout.MinWidth(160f)))
            {
                _storyIntroSegmentIndex++;
                _scrollCharacterStory = Vector2.zero;
            }
        }
        else
        {
            if (GUILayout.Button(CharacterOpeningStory.ButtonContinueEn, NewGameBottomButtonStyle, GUILayout.Height(48f), GUILayout.MinWidth(260f)))
            {
                StopStoryIntroVoice();
                _mode = ScreenMode.CharacterQuestions;
            }
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(12f);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(CharacterOpeningStory.ButtonSkipIntroEn, NewGameBottomButtonStyle, GUILayout.Height(44f), GUILayout.MinWidth(180f)))
        {
            StopStoryIntroVoice();
            _mode = ScreenMode.CharacterQuestions;
        }
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(CharacterOpeningStory.ButtonBackToSetupEn, NewGameBottomButtonStyle, GUILayout.Height(44f), GUILayout.MinWidth(200f)))
        {
            StopStoryIntroVoice();
            _mode = ScreenMode.NewGameSetup;
        }
        GUILayout.EndHorizontal();

        GUILayout.EndArea();

        // Leaving a story segment (Previous/Next): stop dub for that "window"; reset so re-enter can replay.
        if (segAtStart != _storyIntroSegmentIndex)
        {
            StopStoryIntroVoice();
            if (segAtStart == 0)
            {
                _storyIntroVoicePart1Played = false;
                _storyIntroVoicePart1PlayAtUnscaled = -1f;
            }
            if (segAtStart == 1)
            {
                _storyIntroVoicePart2Played = false;
                _storyIntroVoicePart2PlayAtUnscaled = -1f;
            }
        }
    }

    /// <summary>
    /// Plays once when Part 1 is shown. Assign <see cref="_storyIntroVoiceClip"/> in the inspector,
    /// or place an AudioClip at Resources path Audio/Voice/Dubbing/intro (no extension in path).
    /// In the editor, also tries Assets/Audio/Voice/Dubbing/intro.* so the clip need not be in Resources.
    /// </summary>
    private void TryPlayStoryIntroVoicePart1()
    {
        AudioClip clip = _storyIntroVoiceClip;
        if (clip == null)
            clip = Resources.Load<AudioClip>("Audio/Voice/Dubbing/intro");
#if UNITY_EDITOR
        if (clip == null)
        {
            clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Voice/Dubbing/intro.mp3");
            if (clip == null)
                clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Voice/Dubbing/intro.wav");
            if (clip == null)
                clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Voice/Dubbing/intro.ogg");
            if (clip == null)
            {
                string[] guids = AssetDatabase.FindAssets("intro t:AudioClip", new[] { "Assets/Audio/Voice/Dubbing" });
                if (guids.Length > 0)
                    clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
        }
#endif

        if (clip == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning(
                "[MainMenu] Story intro voice (part 1): assign 'Story Intro Voice Clip' on MainMenuFlowController, " +
                "or add Resources/Audio/Voice/Dubbing/intro (imported AudioClip).");
#endif
            return;
        }

        PlayStoryIntroVoice2D(clip, "StoryIntroVoice_P1");
    }

    /// <summary>
    /// Plays once when Part 2 is shown. Assign <see cref="_storyIntroVoiceClipPart2"/> or Resources/Audio/Voice/Dubbing/intro2.
    /// </summary>
    private void TryPlayStoryIntroVoicePart2()
    {
        AudioClip clip = _storyIntroVoiceClipPart2;
        if (clip == null)
            clip = Resources.Load<AudioClip>("Audio/Voice/Dubbing/intro2");
#if UNITY_EDITOR
        if (clip == null)
        {
            clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Voice/Dubbing/intro2.mp3");
            if (clip == null)
                clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Voice/Dubbing/intro2.wav");
            if (clip == null)
                clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Voice/Dubbing/intro2.ogg");
            if (clip == null)
            {
                string[] guids = AssetDatabase.FindAssets("intro2 t:AudioClip", new[] { "Assets/Audio/Voice/Dubbing" });
                if (guids.Length > 0)
                    clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
        }
#endif

        if (clip == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning(
                "[MainMenu] Story intro voice (part 2): assign 'Story Intro Voice Clip Part2' on MainMenuFlowController, " +
                "or add Resources/Audio/Voice/Dubbing/intro2 (imported AudioClip).");
#endif
            return;
        }

        PlayStoryIntroVoice2D(clip, "StoryIntroVoice_P2");
    }

    /// <summary>Stops any story-intro dub immediately (e.g. user left the intro screen).</summary>
    private void StopStoryIntroVoice()
    {
        if (_storyIntroVoiceActive == null)
            return;
        AudioSource src = _storyIntroVoiceActive.GetComponent<AudioSource>();
        if (src != null)
        {
            src.Stop();
            src.clip = null;
        }
        Destroy(_storyIntroVoiceActive);
        _storyIntroVoiceActive = null;
    }

    private void PlayStoryIntroVoice2D(AudioClip clip, string goName)
    {
        StopStoryIntroVoice();
        GameObject go = new GameObject(goName);
        _storyIntroVoiceActive = go;
        AudioSource src = go.AddComponent<AudioSource>();
        src.spatialBlend = 0f;
        src.volume = _storyIntroVoiceVolume;
        src.clip = clip;
        src.Play();
        Destroy(go, clip.length + 0.1f);
    }

    /// <summary>
    /// Q3 "silence ladder" UI: last night was silence, and on name the player either stayed silent or picked a system alias —
    /// same follow-up as silence + silence (not the standard stabbing question set).
    /// </summary>
    private bool IsSilenceStyleStabbingQuestionContext()
    {
        if (_draftAnswers.Length <= PersonalityQuestionnaire.LastNightQuestionIndex)
            return false;
        int nameChoice = _draftAnswers[PersonalityQuestionnaire.NameQuestionIndex];
        int lastNight = _draftAnswers[PersonalityQuestionnaire.LastNightQuestionIndex];
        return lastNight == 2 && (nameChoice == 2 || nameChoice == 1);
    }

    private void DrawCharacterQuestions()
    {
        float w = Screen.width;
        float stripe = Mathf.Min(w * 0.42f, 520f);
        GUILayout.BeginArea(new Rect(32f, 32f, w - stripe - 64f, Screen.height - 64f));
        GUILayout.Label("Police interview — record on file", GUI.skin.box);
        bool cooperativeRouteActive =
            _draftAnswers.Length > PersonalityQuestionnaire.LastNightQuestionIndex &&
            _draftAnswers[PersonalityQuestionnaire.LastNightQuestionIndex] == 0;
        int coopRouteMaxQuestionIndex = PersonalityQuestionnaire.CooperativeInterviewLastLinearDraftIndex;
        int interviewMaxQuestionIndex = cooperativeRouteActive ? coopRouteMaxQuestionIndex : (PersonalityQuestionnaire.QuestionCount - 1);
        _currentQuestionIndex = Mathf.Clamp(_currentQuestionIndex, 0, interviewMaxQuestionIndex);
        int q = _currentQuestionIndex;
        int answerIndex = Mathf.Max(-1, _draftAnswers[q]);

        bool coopPostCarEvening =
            cooperativeRouteActive && _coopPostCarColorPhase == CoopPostCarColorPhase.EveningWhere;
        bool coopPostCarBarLie =
            cooperativeRouteActive && _coopPostCarColorPhase == CoopPostCarColorPhase.BarContradiction;
        bool coopPostBarWhatHappened =
            cooperativeRouteActive && _coopPostCarColorPhase == CoopPostCarColorPhase.BarWhatHappened;
        bool coopPostBarSnitch =
            cooperativeRouteActive && _coopPostCarColorPhase == CoopPostCarColorPhase.BarSnitchOffer;
        bool coopPostCarFirmTrap =
            cooperativeRouteActive && _coopPostCarColorPhase == CoopPostCarColorPhase.BarFirmBarKnowledgeTrap;

        GUILayout.Space(10f);
        if (q == 0)
        {
            GUILayout.Label(PersonalityQuestionnaire.InterrogationSessionIntro, QuestionHintStyle);
        GUILayout.Space(14f);
        }

        int progressDenominator = cooperativeRouteActive
            ? PersonalityQuestionnaire.CooperativeInterviewLastLinearDraftIndex + 1
            : PersonalityQuestionnaire.QuestionCount;
        string questionProgressLabel = "Question " + (q + 1) + " / " + progressDenominator;
        if (coopPostCarEvening)
            questionProgressLabel = "Interview follow-up — last evening";
        else if (coopPostCarBarLie)
            questionProgressLabel = "Interview follow-up — records";
        else if (coopPostBarWhatHappened)
            questionProgressLabel = "Interview follow-up — bar incident";
        else if (coopPostBarSnitch)
            questionProgressLabel = "Interview follow-up — detectives' offer";
        else if (coopPostCarFirmTrap)
            questionProgressLabel = "Interview follow-up — records";
        GUILayout.Label(questionProgressLabel, GUI.skin.label);
        GUILayout.Space(8f);
        bool silenceRoutePrompt =
            q == PersonalityQuestionnaire.StabbingWhoQuestionIndex &&
            IsSilenceStyleStabbingQuestionContext();
        bool aggressiveForgetRoutePrompt =
            q == PersonalityQuestionnaire.StabbingWhoQuestionIndex &&
            _draftAnswers.Length > PersonalityQuestionnaire.LastNightQuestionIndex &&
            _draftAnswers[PersonalityQuestionnaire.LastNightQuestionIndex] == 1;
        bool cooperativeNamesPrompt =
            q == PersonalityQuestionnaire.StabbingWhoQuestionIndex &&
            _draftAnswers.Length > PersonalityQuestionnaire.LastNightQuestionIndex &&
            _draftAnswers[PersonalityQuestionnaire.LastNightQuestionIndex] == 0;
        bool cooperativeReasonPrompt =
            q == PersonalityQuestionnaire.CoopReasonQuestionIndex &&
            _draftAnswers.Length > PersonalityQuestionnaire.LastNightQuestionIndex &&
            _draftAnswers[PersonalityQuestionnaire.LastNightQuestionIndex] == 0;
        bool cooperativeCarPrompt =
            q == PersonalityQuestionnaire.CoopCarOwnerQuestionIndex &&
            _draftAnswers.Length > PersonalityQuestionnaire.LastNightQuestionIndex &&
            _draftAnswers[PersonalityQuestionnaire.LastNightQuestionIndex] == 0;
        bool cooperativeCarColorPrompt =
            q == PersonalityQuestionnaire.CoopCarColorQuestionIndex &&
            _coopPostCarColorPhase == CoopPostCarColorPhase.None &&
            _draftAnswers.Length > PersonalityQuestionnaire.LastNightQuestionIndex &&
            _draftAnswers[PersonalityQuestionnaire.LastNightQuestionIndex] == 0;

        string introLine = PersonalityQuestionnaire.QuestionIntroRefs[q];
        if (q == PersonalityQuestionnaire.LastNightQuestionIndex && _draftAnswers[0] == 2)
            introLine = "You stay silent? Fine. Police pressure rises by 5 for your whole crew. If you won't speak, we call you Mr. X.";
        if (silenceRoutePrompt)
            introLine = "You are silent again? Great. Last chance before this becomes a custody file.";
        if (aggressiveForgetRoutePrompt)
            introLine = "Don't remember? Let us refresh your memory, " + GetInterrogationDisplayName() + ". Yesterday, you murdered a man.";
        if (cooperativeNamesPrompt)
            introLine = "Good. Names. Who are the three friends who were with you?";
        if (cooperativeReasonPrompt)
            introLine = "Now tell us why all of you came to this city.";
        if (cooperativeCarPrompt)
            introLine = "They push the travel log across the table. How did your whole group actually get to Ashkelton?";
        if (cooperativeCarColorPrompt)
            introLine = "One more box on the form: the vehicle. What color was the car you came in?";
        if (coopPostCarEvening)
            introLine =
                "You told us you spent the evening at home with your friends. That has to match the street. " +
                "So — where were you really?";
        if (coopPostCarBarLie)
        {
            introLine =
                "Records put your crew at \"" + _coopInventedBarName + "\" last night. " +
                CoopEveningPlacePhrase(_coopEveningWhereChoice) +
                " So you claim you were not at that bar?";
        }
        if (coopPostCarFirmTrap)
        {
            introLine =
                "One detective taps the file. \"You were adamant you were not there. So how did you know " +
                _coopInventedBarName + " was a bar?\"";
        }
        if (coopPostBarWhatHappened)
            introLine =
                "So you admit you were at the bar? Good — cooperation goes in the file. " +
                "Walk us through what happened in that room.";
        if (coopPostBarSnitch)
            introLine =
                "Here is the shape of it: a known crook took a blade that night. His people want a name. " +
                "The city wants quiet. We can work with you — or we can work around you.";
        GUILayout.Label(introLine, aggressiveForgetRoutePrompt ? QuestionTitleStyle : QuestionHintStyle);
        GUILayout.Space(10f);
        if (aggressiveForgetRoutePrompt)
        {
            GUILayout.Label(
                "One detective leans over the table, the other closes the door and kills the hallway noise. " +
                "They wait for your first crack in the story.",
                QuestionHintStyle);
            GUILayout.Space(10f);
        }
        string titleLine = PersonalityQuestionnaire.QuestionTitles[q];
        if (q == PersonalityQuestionnaire.LastNightQuestionIndex || q == PersonalityQuestionnaire.StabbingWhoQuestionIndex)
            titleLine = titleLine.Replace("{NAME}", GetInterrogationDisplayName());
        if (silenceRoutePrompt)
            titleLine = "Last chance. Choose now, " + GetInterrogationDisplayName() + ".";
        if (cooperativeNamesPrompt)
            titleLine = "What are their names?";
        if (cooperativeReasonPrompt)
            titleLine = "Why did you all come here?";
        if (cooperativeCarPrompt)
            titleLine = "How did you get to Ashkelton?";
        if (cooperativeCarColorPrompt)
            titleLine = "What color was the car?";
        if (coopPostCarEvening)
            titleLine = "You said home with friends — where were you?";
        if (coopPostCarBarLie)
            titleLine = "You were not at \"" + _coopInventedBarName + "\"?";
        if (coopPostCarFirmTrap)
            titleLine = "How did you know what kind of place it was?";
        if (coopPostBarWhatHappened)
            titleLine = "What happened at the bar?";
        if (coopPostBarSnitch)
            titleLine = "Will you cooperate with the department?";
        if (!aggressiveForgetRoutePrompt)
        {
            GUILayout.Label(titleLine, QuestionTitleStyle);
        GUILayout.Space(8f);
        }
        GUILayout.Label("Pick the answer that fits your story — not the one that sounds heroic.", QuestionHintStyle);
        GUILayout.Space(18f);

        if (q == 0)
        {
            GUILayout.Label("1) Write your name", QuestionHintStyle);
            _interrogationTypedName = GUILayout.TextField(_interrogationTypedName, 28, GUILayout.Height(38f), GUILayout.ExpandWidth(true));
            GUILayout.Space(6f);
            GUI.enabled = !string.IsNullOrWhiteSpace(_interrogationTypedName);
            if (GUILayout.Button("Use this name", ChoiceButtonStyle))
            {
                _draftName = _interrogationTypedName.Trim();
                _draftAnswers[0] = 0;
            }
            GUI.enabled = true;
            GUILayout.Space(8f);

            bool aliasSelected = answerIndex == 1;
            Color oldAlias = GUI.backgroundColor;
            if (aliasSelected)
                GUI.backgroundColor = new Color(0.23f, 0.31f, 0.48f, 1f);
            if (GUILayout.Button((aliasSelected ? "● " : "  ") + "Invent alias", ChoiceButtonStyle))
            {
                _draftName = GenerateAliasName();
                _interrogationTypedName = _draftName;
                _draftAnswers[0] = 1;
            }
            GUI.backgroundColor = oldAlias;
            GUILayout.Space(8f);

            bool silenceSelected = answerIndex == 2;
            Color oldSilent = GUI.backgroundColor;
            if (silenceSelected)
                GUI.backgroundColor = new Color(0.23f, 0.31f, 0.48f, 1f);
            if (GUILayout.Button((silenceSelected ? "● " : "  ") + "Silence", ChoiceButtonStyle))
            {
                _draftName = "Mr. X";
                _interrogationTypedName = _draftName;
                _draftAnswers[0] = 2;
            }
            GUI.backgroundColor = oldSilent;
        }
        else if (cooperativeNamesPrompt)
        {
            bool manualSelected = answerIndex == 0;
            Color oldManual = GUI.backgroundColor;
            if (manualSelected)
                GUI.backgroundColor = new Color(0.23f, 0.31f, 0.48f, 1f);
            if (GUILayout.Button((manualSelected ? "● " : "  ") + "Tell their names (manual input)", ChoiceButtonStyle))
            {
                _draftAnswers[q] = 0;
                _coopManualNamesMode = true;
            }
            GUI.backgroundColor = oldManual;
            GUILayout.Space(8f);

            bool autoSelected = answerIndex == 1;
            Color oldAuto = GUI.backgroundColor;
            if (autoSelected)
                GUI.backgroundColor = new Color(0.23f, 0.31f, 0.48f, 1f);
            if (GUILayout.Button((autoSelected ? "● " : "  ") + "Make up names automatically", ChoiceButtonStyle))
            {
                _draftAnswers[q] = 1;
                _coopManualNamesMode = false;
                GeneratePartnerNamesIfNeeded();
            }
            GUI.backgroundColor = oldAuto;

            if (_coopManualNamesMode)
            {
                GUILayout.Space(10f);
                for (int i = 0; i < _draftPartnerNames.Length; i++)
                {
                    GUILayout.Label("Friend " + (i + 1) + " name", QuestionHintStyle);
                    _draftPartnerNames[i] = GUILayout.TextField(_draftPartnerNames[i] ?? string.Empty, 32, GUILayout.Height(34f));
                    GUILayout.Space(4f);
                }
            }
        }
        else if (cooperativeReasonPrompt)
        {
            string[] reasonChoices =
            {
                "Find work.",
                "Start a new page.",
                "Try our luck here.",
                "We came for a student fair / exhibition."
            };
            for (int i = 0; i < reasonChoices.Length; i++)
            {
                bool selected = answerIndex == i;
                Color old = GUI.backgroundColor;
                if (selected)
                    GUI.backgroundColor = new Color(0.23f, 0.31f, 0.48f, 1f);
                string letter = ((char)('A' + i)).ToString();
                if (GUILayout.Button((selected ? "● " : "  ") + letter + ". " + reasonChoices[i], ChoiceButtonStyle))
                {
                    _draftAnswers[q] = i;
                    _coopReasonChoice = i;
                }
                GUI.backgroundColor = old;
                GUILayout.Space(8f);
            }
        }
        else if (cooperativeCarPrompt)
        {
            string[] owners =
            {
                "In my car (I'm the one driving).",
                "In Friend 1's car — " + SafePartnerName(0),
                "In Friend 2's car — " + SafePartnerName(1),
                "In Friend 3's car — " + SafePartnerName(2)
            };
            for (int i = 0; i < owners.Length; i++)
            {
                bool selected = answerIndex == i;
                Color old = GUI.backgroundColor;
                if (selected)
                    GUI.backgroundColor = new Color(0.23f, 0.31f, 0.48f, 1f);
                string letter = ((char)('A' + i)).ToString();
                if (GUILayout.Button((selected ? "● " : "  ") + letter + ". " + owners[i], ChoiceButtonStyle))
                {
                    _draftAnswers[q] = i;
                    _coopCarOwnerChoice = i;
                }
                GUI.backgroundColor = old;
                GUILayout.Space(8f);
            }
        }
        else if (cooperativeCarColorPrompt)
        {
            for (int i = 0; i < PlayerCharacterProfile.AccentColorCount; i++)
            {
                bool selected = answerIndex == i;
                Color old = GUI.backgroundColor;
                if (selected)
                    GUI.backgroundColor = new Color(0.23f, 0.31f, 0.48f, 1f);
                string letter = ((char)('A' + i)).ToString();
                string label = PlayerCharacterProfile.AccentColorLabels[i];
                if (GUILayout.Button((selected ? "● " : "  ") + letter + ". " + label, ChoiceButtonStyle))
                    _draftAnswers[q] = i;
                GUI.backgroundColor = old;
                GUILayout.Space(8f);
            }
        }
        else if (coopPostCarEvening)
        {
            string[] evening =
            {
                "At a bar — drinks, noise, nothing serious.",
                "A restaurant — sit-down meal, we kept it quiet.",
                "The park — air, walking, no receipts.",
                "The gym — we were on the floor, not in anyone's business."
            };
            for (int i = 0; i < evening.Length; i++)
            {
                bool selected = _coopEveningWhereChoice == i;
                Color old = GUI.backgroundColor;
                if (selected)
                    GUI.backgroundColor = new Color(0.23f, 0.31f, 0.48f, 1f);
                string letter = ((char)('A' + i)).ToString();
                if (GUILayout.Button((selected ? "● " : "  ") + letter + ". " + evening[i], ChoiceButtonStyle))
                    _coopEveningWhereChoice = i;
                GUI.backgroundColor = old;
                GUILayout.Space(8f);
            }
        }
        else if (coopPostCarBarLie)
        {
            bool lieSelected = _coopBarContradictionChoice == 0;
            Color oldLie = GUI.backgroundColor;
            if (lieSelected)
                GUI.backgroundColor = new Color(0.23f, 0.31f, 0.48f, 1f);
            if (GUILayout.Button((lieSelected ? "● " : "  ") + "A. * (Firm) Yes — we were not at that bar.", ChoiceButtonStyle))
                _coopBarContradictionChoice = 0;
            GUI.backgroundColor = oldLie;
            GUILayout.Space(8f);

            bool confessSelected = _coopBarContradictionChoice == 1;
            Color oldConfess = GUI.backgroundColor;
            if (confessSelected)
                GUI.backgroundColor = new Color(0.23f, 0.31f, 0.48f, 1f);
            if (GUILayout.Button((confessSelected ? "● " : "  ") + "B. I am sorry — I did not tell the truth. We were there.", ChoiceButtonStyle))
                _coopBarContradictionChoice = 1;
            GUI.backgroundColor = oldConfess;
            GUILayout.Space(8f);

            bool unawareSelected = _coopBarContradictionChoice == 2;
            Color oldUnaware = GUI.backgroundColor;
            if (unawareSelected)
                GUI.backgroundColor = new Color(0.23f, 0.31f, 0.48f, 1f);
            if (GUILayout.Button((unawareSelected ? "● " : "  ") + "C. No! What is this place — we never heard of it.", ChoiceButtonStyle))
                _coopBarContradictionChoice = 2;
            GUI.backgroundColor = oldUnaware;
        }
        else if (coopPostCarFirmTrap)
        {
            bool silenceSelected = _coopBarFirmKnowledgeTrapChoice == 0;
            Color oldS = GUI.backgroundColor;
            if (silenceSelected)
                GUI.backgroundColor = new Color(0.23f, 0.31f, 0.48f, 1f);
            if (GUILayout.Button((silenceSelected ? "● " : "  ") + "A. Stay silent — give them nothing more.", ChoiceButtonStyle))
                _coopBarFirmKnowledgeTrapChoice = 0;
            GUI.backgroundColor = oldS;
            GUILayout.Space(8f);

            bool lawyerSelected = _coopBarFirmKnowledgeTrapChoice == 1;
            Color oldL = GUI.backgroundColor;
            if (lawyerSelected)
                GUI.backgroundColor = new Color(0.23f, 0.31f, 0.48f, 1f);
            if (GUILayout.Button((lawyerSelected ? "● " : "  ") + "B. Demand a lawyer — public defender, now.", ChoiceButtonStyle))
                _coopBarFirmKnowledgeTrapChoice = 1;
            GUI.backgroundColor = oldL;
            GUILayout.Space(8f);

            bool confessCleanSelected = _coopBarFirmKnowledgeTrapChoice == 2;
            Color oldC = GUI.backgroundColor;
            if (confessCleanSelected)
                GUI.backgroundColor = new Color(0.23f, 0.31f, 0.48f, 1f);
            if (GUILayout.Button((confessCleanSelected ? "● " : "  ") + "C. Fine — we were at that bar. I admit it.", ChoiceButtonStyle))
                _coopBarFirmKnowledgeTrapChoice = 2;
            GUI.backgroundColor = oldC;
        }
        else if (coopPostBarWhatHappened)
        {
            string[] happened =
            {
                "Truth: we sat with friends; one vanished and we went looking. Some people moved on a friend — we stepped in. " +
                "It turned into a brawl; I saw blood and we ran. I do not know who did what after that.",
                "A different story — not what really happened (fabricate)."
            };
            for (int i = 0; i < happened.Length; i++)
            {
                bool selected = _coopBarWhatHappenedChoice == i;
                Color old = GUI.backgroundColor;
                if (selected)
                    GUI.backgroundColor = new Color(0.23f, 0.31f, 0.48f, 1f);
                string letter = ((char)('A' + i)).ToString();
                if (GUILayout.Button((selected ? "● " : "  ") + letter + ". " + happened[i], ChoiceButtonStyle))
                    _coopBarWhatHappenedChoice = i;
                GUI.backgroundColor = old;
                GUILayout.Space(8f);
            }
        }
        else if (coopPostBarSnitch)
        {
            bool yesSelected = _coopSnitchChoice == 0;
            Color oldY = GUI.backgroundColor;
            if (yesSelected)
                GUI.backgroundColor = new Color(0.23f, 0.31f, 0.48f, 1f);
            if (GUILayout.Button((yesSelected ? "● " : "  ") + "A. Yes — I will cooperate on your terms.", ChoiceButtonStyle))
                _coopSnitchChoice = 0;
            GUI.backgroundColor = oldY;
            GUILayout.Space(8f);

            bool noSelected = _coopSnitchChoice == 1;
            Color oldN = GUI.backgroundColor;
            if (noSelected)
                GUI.backgroundColor = new Color(0.23f, 0.31f, 0.48f, 1f);
            if (GUILayout.Button((noSelected ? "● " : "  ") + "B. No — I am not your informant.", ChoiceButtonStyle))
                _coopSnitchChoice = 1;
            GUI.backgroundColor = oldN;
        }
        else if (silenceRoutePrompt)
        {
            bool breakSelected = answerIndex == 0;
            Color oldBreak = GUI.backgroundColor;
            if (breakSelected)
                GUI.backgroundColor = new Color(0.23f, 0.31f, 0.48f, 1f);
            if (GUILayout.Button((breakSelected ? "● " : "  ") + "Break silence", ChoiceButtonStyle))
            {
                _draftAnswers[q] = 0;
            }
            GUI.backgroundColor = oldBreak;
            GUILayout.Space(8f);

            bool vagueSelected = answerIndex == 1;
            Color oldVague = GUI.backgroundColor;
            if (vagueSelected)
                GUI.backgroundColor = new Color(0.23f, 0.31f, 0.48f, 1f);
            if (GUILayout.Button((vagueSelected ? "● " : "  ") + "Give vague answers", ChoiceButtonStyle))
            {
                _draftAnswers[q] = 1;
            }
            GUI.backgroundColor = oldVague;
            GUILayout.Space(8f);

            bool lawyerSelected = answerIndex == 2;
            Color oldLawyer = GUI.backgroundColor;
            if (lawyerSelected)
                GUI.backgroundColor = new Color(0.23f, 0.31f, 0.48f, 1f);
            if (GUILayout.Button((lawyerSelected ? "● " : "  ") + "Request public defender and remain silent", ChoiceButtonStyle))
            {
                _draftAnswers[q] = 2;
            }
            GUI.backgroundColor = oldLawyer;
        }
        else if (aggressiveForgetRoutePrompt)
        {
            bool denySelected = answerIndex == 0;
            Color oldDeny = GUI.backgroundColor;
            if (denySelected)
                GUI.backgroundColor = new Color(0.23f, 0.31f, 0.48f, 1f);
            if (GUILayout.Button((denySelected ? "● " : "  ") + "No way. That's not me!", ChoiceButtonStyle))
                _draftAnswers[q] = 0;
            GUI.backgroundColor = oldDeny;
            GUILayout.Space(8f);

            bool legalSelected = answerIndex == 1;
            Color oldLegal = GUI.backgroundColor;
            if (legalSelected)
                GUI.backgroundColor = new Color(0.23f, 0.31f, 0.48f, 1f);
            if (GUILayout.Button((legalSelected ? "● " : "  ") + "You'll have to prove that absurd claim first.", ChoiceButtonStyle))
                _draftAnswers[q] = 1;
            GUI.backgroundColor = oldLegal;
        }
        else
        {
        string[] choices = PersonalityQuestionnaire.ChoiceLabels[q];
        for (int c = 0; c < choices.Length; c++)
        {
                bool lockedSilentChoice = q == PersonalityQuestionnaire.LastNightQuestionIndex &&
                                         c == 2 &&
                                         _lastNightSilenceLocked;
                bool lockedNoMemoryChoice = q == PersonalityQuestionnaire.LastNightQuestionIndex &&
                                           c == 1 &&
                                           _lastNightNoMemoryLocked;
            bool selected = answerIndex == c;
            Color old = GUI.backgroundColor;
            if (selected)
                GUI.backgroundColor = new Color(0.23f, 0.31f, 0.48f, 1f);

            string letter = ((char)('A' + c)).ToString();
                string lockTag = (lockedSilentChoice || lockedNoMemoryChoice) ? " (locked)" : "";
                string label = (selected ? "● " : "  ") + letter + ". " + choices[c] + lockTag;
                GUI.enabled = !lockedSilentChoice && !lockedNoMemoryChoice;
            if (GUILayout.Button(label, ChoiceButtonStyle))
            {
                _draftAnswers[q] = c;
            }
                GUI.enabled = true;

            GUI.backgroundColor = old;
            GUILayout.Space(8f);
            }
        }

        if (answerIndex >= 0)
        {
            GUILayout.Space(6f);
            string preview = string.Empty;
            if (silenceRoutePrompt)
            {
                if (answerIndex == 0)
                    preview = "Consequence: returns to last-night question; silence option is locked.";
                else if (answerIndex == 1)
                    preview = "Consequence: +100 Charisma XP, +5 police pressure, returns to last-night question with silence locked.";
                else
                    preview = "Consequence: public defender assigned; immediate custody transfer for the boss.";
            }
            else if (aggressiveForgetRoutePrompt)
            {
                if (answerIndex == 0)
                    preview = "Consequence: returns to 'what did you do yesterday?' with 'I don't remember' and 'Silence' locked.";
                else
                    preview = "Consequence: detectives close the binder and move you to custody. +100 Mental, +100 Determination, +50 Charisma XP.";
            }
            else if (cooperativeNamesPrompt)
            {
                preview = answerIndex == 0
                    ? "Consequence: you manually set the three friend names."
                    : "Consequence: system generates friend names automatically.";
            }
            else if (cooperativeReasonPrompt)
            {
                if (answerIndex == 0) preview = "Consequence: everyone gets +50 Strength potential.";
                else if (answerIndex == 1) preview = "Consequence: everyone gets +50 Determination potential.";
                else if (answerIndex == 2)
                    preview = "Consequence: weighted random XP (15–200, seven tiers — larger rolls are rarer), then a random core trait; boss and each of the three friends roll separately.";
                else preview = "Consequence: everyone gets +50 Intelligence potential.";
            }
            else if (cooperativeCarPrompt)
            {
                preview = answerIndex <= 0
                    ? "Consequence: you came in your own car; the boss is recorded as having that vehicle."
                    : "Consequence: you came in " + SafePartnerName(answerIndex - 1) + "'s car; that friend is recorded with the vehicle.";
            }
            else if (cooperativeCarColorPrompt)
            {
                int ci = Mathf.Clamp(answerIndex, 0, PlayerCharacterProfile.AccentColorCount - 1);
                preview = "Consequence: police record the car as " + PlayerCharacterProfile.AccentColorLabels[ci] +
                          " — this becomes your crew's identifying (accent) color.";
            }
            else if (coopPostCarEvening)
            {
                if (_coopEveningWhereChoice < 0)
                    preview = string.Empty;
                else if (_coopEveningWhereChoice == 0)
                    preview = "Consequence: truthful bar — police interest drops (can go below zero for a buffer).";
                else if (_coopEveningWhereChoice == 1)
                    preview = "Consequence: you +50 Charisma XP; each friend +50 XP in a random trait; bar contradiction follow-up.";
                else if (_coopEveningWhereChoice == 2)
                    preview = "Consequence: you and all three friends +50 Agility XP; bar contradiction follow-up.";
                else if (_coopEveningWhereChoice == 3)
                    preview = "Consequence: you and all three friends +50 Strength XP; bar contradiction follow-up.";
                else
                    preview = string.Empty;
            }
            else if (coopPostCarBarLie)
            {
                if (_coopBarContradictionChoice < 0)
                    preview = string.Empty;
                else if (_coopBarContradictionChoice == 0)
                    preview =
                        "Consequence: * Trap next — how did you know it was a bar? Silence → custody +50 Mental + crew search heat +20. " +
                        "Lawyer → custody + public defender (Legal) +50 Mental + heat +40. Admit bar → clean path like pressing B on the last screen.";
                else if (_coopBarContradictionChoice == 2)
                    preview =
                        "Consequence: you +50 Charisma & +50 Mental; each friend +100 rubric XP (usually split across 2 traits); " +
                        "$1000 bail; police heat rises.";
                else
                    preview =
                        "Consequence: evening trait bonus removed; same police-pressure relief as choosing the bar truthfully; " +
                        "then — what happened at the bar.";
            }
            else if (coopPostCarFirmTrap)
            {
                if (_coopBarFirmKnowledgeTrapChoice < 0)
                    preview = string.Empty;
                else if (_coopBarFirmKnowledgeTrapChoice == 0)
                    preview =
                        "Consequence: +50 Mental XP; police heat rises; crew watch intensifies (+20 street/custody tension); " +
                        "boss taken to custody — then a detective mutters: \"Silent as a fish?\" (שותק כמו דג?)";
                else if (_coopBarFirmKnowledgeTrapChoice == 1)
                    preview =
                        "Consequence: +50 Mental XP; a public defender is assigned (Legal tab); heat rises; crew tension +40; boss to custody.";
                else
                    preview =
                        "Consequence: resets the firm-deny path — as if you admitted the bar on the last screen: evening bonuses drop, " +
                        "police pressure eases, then — what happened at the bar.";
            }
            else if (coopPostBarWhatHappened)
            {
                if (_coopBarWhatHappenedChoice < 0)
                    preview = string.Empty;
                else if (_coopBarWhatHappenedChoice == 0)
                    preview =
                        "Consequence: detectives believe you; they describe a stabbing and offer cooperation — accept or refuse.";
                else
                    preview = "Consequence: fabricated story; police pressure rises; released on bail ($1000).";
            }
            else if (coopPostBarSnitch)
            {
                if (_coopSnitchChoice < 0)
                    preview = string.Empty;
                else if (_coopSnitchChoice == 0)
                    preview = "Consequence: you are flagged as a police contact — new options later; file stays ugly but useful.";
                else
                    preview =
                        "Consequence: heavy police heat; a bent tip points a rival family at you — street war risk.";
            }
            else
            {
                preview = PersonalityQuestionnaire.GetImmediateConsequencePreview(q, answerIndex, _draftAnswers);
            }
            if (!string.IsNullOrEmpty(preview))
                GUILayout.Label(preview, QuestionHintStyle);
        }

        GUILayout.Space(10f);
        bool hasAnswerForCurrent = q >= 0 && q < _draftAnswers.Length && _draftAnswers[q] >= 0;
        if (coopPostCarEvening)
            hasAnswerForCurrent = _coopEveningWhereChoice >= 0;
        else if (coopPostCarBarLie)
            hasAnswerForCurrent = _coopBarContradictionChoice >= 0;
        else if (coopPostCarFirmTrap)
            hasAnswerForCurrent = _coopBarFirmKnowledgeTrapChoice >= 0;
        else if (coopPostBarWhatHappened)
            hasAnswerForCurrent = _coopBarWhatHappenedChoice >= 0;
        else if (coopPostBarSnitch)
            hasAnswerForCurrent = _coopSnitchChoice >= 0;

        bool canAdvanceFromHere;
        if (coopPostCarEvening)
            canAdvanceFromHere = _coopEveningWhereChoice >= 0;
        else if (coopPostCarBarLie)
            canAdvanceFromHere = _coopBarContradictionChoice >= 0;
        else if (coopPostCarFirmTrap)
            canAdvanceFromHere = _coopBarFirmKnowledgeTrapChoice >= 0;
        else if (coopPostBarWhatHappened)
            canAdvanceFromHere = _coopBarWhatHappenedChoice >= 0;
        else if (coopPostBarSnitch)
            canAdvanceFromHere = _coopSnitchChoice >= 0;
        else
            canAdvanceFromHere = hasAnswerForCurrent &&
                                 (_currentQuestionIndex < interviewMaxQuestionIndex ||
                                  (cooperativeCarColorPrompt && cooperativeRouteActive));
        if (cooperativeNamesPrompt && _coopManualNamesMode)
            canAdvanceFromHere = canAdvanceFromHere && AreCoopPartnerNamesFilled();
        GUI.enabled = canAdvanceFromHere;
        if (GUILayout.Button("Confirm answer and continue to next question", NewGameBottomButtonStyle, GUILayout.Height(44f)))
        {
            if (silenceRoutePrompt)
            {
                if (answerIndex == 0)
                {
                    _lastNightSilenceLocked = true;
                    _currentQuestionIndex = PersonalityQuestionnaire.LastNightQuestionIndex;
                }
                else if (answerIndex == 1)
                {
                    _silenceRouteVagueChosen = true;
                    _lastNightSilenceLocked = true;
                    _currentQuestionIndex = PersonalityQuestionnaire.LastNightQuestionIndex;
                }
                else if (answerIndex == 2)
                {
                    _silenceRouteLawyerCustody = true;
                    _mode = ScreenMode.CharacterIdentity;
                }
            }
            else if (aggressiveForgetRoutePrompt)
            {
                if (answerIndex == 0)
                {
                    _lastNightSilenceLocked = true;
                    _lastNightNoMemoryLocked = true;
                    _currentQuestionIndex = PersonalityQuestionnaire.LastNightQuestionIndex;
                }
                else if (answerIndex == 1)
                {
                    _aggressiveRouteCustody = true;
                    _mode = ScreenMode.CharacterIdentity;
                }
            }
            else
            {
                if (coopPostBarSnitch)
                {
                    if (_coopSnitchChoice == 1)
                        _coopInterrogationPressureAdjustment += 20;
                    _coopPostCarColorPhase = CoopPostCarColorPhase.None;
                    _mode = ScreenMode.CharacterIdentity;
                }
                else if (coopPostBarWhatHappened)
                {
                    if (_coopBarWhatHappenedChoice == 0)
                    {
                        _coopSnitchChoice = -1;
                        _coopPostCarColorPhase = CoopPostCarColorPhase.BarSnitchOffer;
                    }
                    else
                    {
                        _coopInterrogationPressureAdjustment += 15;
                        _coopBailUsd += 1000;
                        _coopPostCarColorPhase = CoopPostCarColorPhase.None;
                        _mode = ScreenMode.CharacterIdentity;
                    }
                }
                else if (coopPostCarFirmTrap)
                {
                    if (_coopBarFirmKnowledgeTrapChoice == 0)
                    {
                        _coopBossCustodyFromBarTrapSilence = true;
                        _coopBossCustodyFromBarTrapLawyer = false;
                        _coopInterrogationPressureAdjustment = 14;
                        _coopBailUsd = 0;
                        _coopPostCarColorPhase = CoopPostCarColorPhase.None;
                        _mode = ScreenMode.CharacterIdentity;
                    }
                    else if (_coopBarFirmKnowledgeTrapChoice == 1)
                    {
                        _coopBossCustodyFromBarTrapSilence = false;
                        _coopBossCustodyFromBarTrapLawyer = true;
                        _coopInterrogationPressureAdjustment = 16;
                        _coopBailUsd = 0;
                        _coopPostCarColorPhase = CoopPostCarColorPhase.None;
                        _mode = ScreenMode.CharacterIdentity;
                    }
                    else
                    {
                        _coopBossCustodyFromBarTrapSilence = false;
                        _coopBossCustodyFromBarTrapLawyer = false;
                        _coopBarContradictionChoice = 1;
                        _coopBarFirmKnowledgeTrapChoice = -1;
                        _coopInterrogationPressureAdjustment = -10;
                        _coopBarWhatHappenedChoice = -1;
                        _coopSnitchChoice = -1;
                        _coopPostCarColorPhase = CoopPostCarColorPhase.BarWhatHappened;
                    }
                }
                else if (coopPostCarEvening)
                {
                    if (_coopEveningWhereChoice == 0)
                    {
                        _coopBarContradictionChoice = -1;
                        _coopInterrogationPressureAdjustment = -10;
                        _coopBarWhatHappenedChoice = -1;
                        _coopSnitchChoice = -1;
                        _coopPostCarColorPhase = CoopPostCarColorPhase.BarWhatHappened;
                    }
                    else
                    {
                        _coopInventedBarName = PickCoopInventedBarName();
                        _coopInterrogationPressureAdjustment = 0;
                        _coopBarContradictionChoice = -1;
                        _coopPostCarColorPhase = CoopPostCarColorPhase.BarContradiction;
                    }
                }
                else if (coopPostCarBarLie)
                {
                    if (_coopBarContradictionChoice == 0)
                    {
                        _coopBarFirmKnowledgeTrapChoice = -1;
                        _coopBossCustodyFromBarTrapSilence = false;
                        _coopBossCustodyFromBarTrapLawyer = false;
                        _coopInterrogationPressureAdjustment = 0;
                        _coopPostCarColorPhase = CoopPostCarColorPhase.BarFirmBarKnowledgeTrap;
                    }
                    else if (_coopBarContradictionChoice == 1)
                    {
                        _coopInterrogationPressureAdjustment = -10;
                        _coopBarWhatHappenedChoice = -1;
                        _coopSnitchChoice = -1;
                        _coopPostCarColorPhase = CoopPostCarColorPhase.BarWhatHappened;
                    }
                    else
                    {
                        _coopInterrogationPressureAdjustment = 25;
                        _coopBailUsd += 1000;
                        _coopPostCarColorPhase = CoopPostCarColorPhase.None;
                        _mode = ScreenMode.CharacterIdentity;
                    }
                }
                else if (cooperativeCarColorPrompt && cooperativeRouteActive)
                {
                    // Same index as formal "family color" question so saves / UI stay consistent.
                    if (_draftAnswers.Length > PersonalityQuestionnaire.ColorQuestionIndex &&
                        answerIndex >= 0 && answerIndex < PlayerCharacterProfile.AccentColorCount)
                        _draftAnswers[PersonalityQuestionnaire.ColorQuestionIndex] = answerIndex;
                    _coopPostCarColorPhase = CoopPostCarColorPhase.EveningWhere;
                }
                else if (cooperativeCarPrompt && cooperativeRouteActive)
                {
                    _currentQuestionIndex++;
                }
                else
                {
                    _currentQuestionIndex++;
                }
            }
        }
        GUI.enabled = true;

        GUILayout.EndArea();

        GUILayout.BeginArea(new Rect(w - stripe + 24f, 40f, stripe - 48f, Screen.height - 80f));
        GUILayout.Label("Interview", GUI.skin.box);
        GUILayout.Space(10f);
        if (GUILayout.Button("I want to control everything", GUILayout.Height(40f)))
        {
            for (int i = 0; i < _draftAnswers.Length; i++)
                _draftAnswers[i] = 0;
            _coopPostCarColorPhase = CoopPostCarColorPhase.None;
            _coopEveningWhereChoice = -1;
            _coopBarContradictionChoice = -1;
            _coopBarFirmKnowledgeTrapChoice = -1;
            _coopBossCustodyFromBarTrapSilence = false;
            _coopBossCustodyFromBarTrapLawyer = false;
            _coopInventedBarName = string.Empty;
            _coopInterrogationPressureAdjustment = 0;
            _coopBarWhatHappenedChoice = -1;
            _coopSnitchChoice = -1;
            _coopBailUsd = 0;
        }
        GUILayout.Space(6f);
        if (GUILayout.Button("Skip", GUILayout.Height(40f)))
        {
            for (int i = 0; i < _draftAnswers.Length; i++)
            {
                int maxChoice = PersonalityQuestionnaire.ChoiceLabels[i].Length - 1;
                _draftAnswers[i] = Random.Range(0, maxChoice + 1);
            }
            ApplyQuestionOneNameSelection();
            if (_draftAnswers.Length > PersonalityQuestionnaire.LastNightQuestionIndex &&
                _draftAnswers[PersonalityQuestionnaire.LastNightQuestionIndex] == 0)
                RandomizeCoopPostCarForSkip();
            _mode = ScreenMode.CharacterIdentity;
        }
        GUILayout.Space(6f);
        GUI.enabled = _currentQuestionIndex > 0 && _coopPostCarColorPhase == CoopPostCarColorPhase.None;
        if (GUILayout.Button("Previous question", GUILayout.Height(40f)))
            _currentQuestionIndex = Mathf.Max(0, _currentQuestionIndex - 1);
        GUI.enabled = true;

        GUILayout.Space(6f);
        GUI.enabled = _currentQuestionIndex < interviewMaxQuestionIndex && _coopPostCarColorPhase == CoopPostCarColorPhase.None;
        if (GUILayout.Button("Next question", GUILayout.Height(40f)))
            _currentQuestionIndex = Mathf.Min(interviewMaxQuestionIndex, _currentQuestionIndex + 1);
        GUI.enabled = true;

        GUILayout.Space(14f);
        bool allAnswered = true;
        int completionMax = interviewMaxQuestionIndex;
        for (int i = 0; i <= completionMax && i < _draftAnswers.Length; i++)
        {
            if (_draftAnswers[i] < 0) { allAnswered = false; break; }
        }
        if (cooperativeRouteActive && _coopManualNamesMode)
            allAnswered = allAnswered && AreCoopPartnerNamesFilled();
        if (cooperativeRouteActive)
            allAnswered = allAnswered && CoopPostCarInterrogationResolved();
        GUI.enabled = allAnswered;
        if (GUILayout.Button("Next — choose face", GUILayout.Height(44f)))
            _mode = ScreenMode.CharacterIdentity;
        GUI.enabled = true;

        if (GUILayout.Button("Back to menu", GUILayout.Height(36f)))
            _mode = ScreenMode.MainMenu;

        GUILayout.EndArea();
    }

    private static GUIStyle WrappedStyle()
    {
        GUIStyle s = new GUIStyle(GUI.skin.label);
        s.wordWrap = true;
        return s;
    }

    private void StartManualNewGame()
    {
        ApplyQuestionOneNameSelection();
        PlayerCharacterProfile profile = PersonalityQuestionnaire.BuildProfile(_draftName, GetEffectiveAccentIndex(), _draftAnswers, _draftPortraitResourcePath);
        ApplyInterviewBonusesToProfile(profile);
        SeedStartingCrew(profile, !_manualCrewSetup);
        StartGameWithProfile(profile);
    }

    private void GeneratePartnerNamesIfNeeded()
    {
        CharacterNamePools.FillDistinctFullNames(_draftPartnerNames, _draftPartnerNames.Length);
    }

    private string SafePartnerName(int idx)
    {
        if (idx < 0 || idx >= _draftPartnerNames.Length)
            return "Friend";
        string s = _draftPartnerNames[idx];
        return string.IsNullOrWhiteSpace(s) ? "Friend" : s.Trim();
    }

    private bool AreCoopPartnerNamesFilled()
    {
        for (int i = 0; i < _draftPartnerNames.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(_draftPartnerNames[i]))
                return false;
        }
        return true;
    }

    private void ApplyQuestionOneNameSelection()
    {
        int firstAnswer = (_draftAnswers.Length > 0) ? _draftAnswers[0] : -1;
        if (firstAnswer == 1)
        {
            if (string.IsNullOrWhiteSpace(_draftName) || _draftName == "Boss")
                _draftName = GenerateAliasName();
            _interrogationTypedName = _draftName;
        }
        else if (firstAnswer == 2)
        {
            _draftName = "Mr. X";
            _interrogationTypedName = _draftName;
        }
        else if (!string.IsNullOrWhiteSpace(_interrogationTypedName))
        {
            _draftName = _interrogationTypedName.Trim();
        }
    }

    private void ApplyInterviewBonusesToProfile(PlayerCharacterProfile profile)
    {
        if (profile == null || _draftAnswers.Length == 0)
            return;

        int firstAnswer = _draftAnswers[0];
        bool aliasThenLastNightSilence =
            firstAnswer == 1 &&
            _draftAnswers.Length > PersonalityQuestionnaire.LastNightQuestionIndex &&
            _draftAnswers[PersonalityQuestionnaire.LastNightQuestionIndex] == 2;

        if (firstAnswer == 2)
            CoreTraitProgression.AddPractice(profile, CoreTrait.MentalResilience, 100);
        else if (firstAnswer == 1)
        {
            // Invent alias then silence on "last night" is treated like double silence (no Charisma bump for the alias).
            if (aliasThenLastNightSilence)
                CoreTraitProgression.AddPractice(profile, CoreTrait.MentalResilience, 100);
            else
                CoreTraitProgression.AddPractice(profile, CoreTrait.Charisma, 100);
        }

        if (_draftAnswers.Length > PersonalityQuestionnaire.LastNightQuestionIndex)
        {
            int secondAnswer = _draftAnswers[PersonalityQuestionnaire.LastNightQuestionIndex];
            if (secondAnswer == 2)
                CoreTraitProgression.AddPractice(profile, CoreTrait.MentalResilience, 100);
        }

        if (_draftAnswers.Length > PersonalityQuestionnaire.StabbingWhoQuestionIndex)
        {
            int thirdAnswer = _draftAnswers[PersonalityQuestionnaire.StabbingWhoQuestionIndex];
            if (thirdAnswer == 2)
                CoreTraitProgression.AddPractice(profile, CoreTrait.MentalResilience, 100);
        }

        if (_silenceRouteVagueChosen)
            CoreTraitProgression.AddPractice(profile, CoreTrait.Charisma, 100);

        if (_silenceRouteLawyerCustody)
            CoreTraitProgression.AddPractice(profile, CoreTrait.Determination, 100);

        if (_aggressiveRouteCustody)
        {
            CoreTraitProgression.AddPractice(profile, CoreTrait.MentalResilience, 100);
            CoreTraitProgression.AddPractice(profile, CoreTrait.Determination, 100);
            CoreTraitProgression.AddPractice(profile, CoreTrait.Charisma, 50);
        }

        // Cooperative route bonuses (Q2 = "I went out with my three friends")
        if (_draftAnswers.Length > PersonalityQuestionnaire.LastNightQuestionIndex &&
            _draftAnswers[PersonalityQuestionnaire.LastNightQuestionIndex] == 0 &&
            _coopReasonChoice >= 0)
        {
            // Mapping: 0=work, 1=open new chapter, 2=try our luck, 3=student fair/exhibition.
            if (_coopReasonChoice == 0)
                CoreTraitProgression.AddPractice(profile, CoreTrait.Strength, 50);
            else if (_coopReasonChoice == 1)
                CoreTraitProgression.AddPractice(profile, CoreTrait.Determination, 50);
            else if (_coopReasonChoice == 2)
            {
                EnsureCoopPartnerBonusArray(profile);
                EnsureCoopTryLuckPartnerRollArrays(profile);

                int bossXp = RollTryOurLuckPracticeXp();
                int bossTrait = Random.Range(0, 6);
                profile.CoopTryLuckGrantedXp = bossXp;
                profile.CoopTryLuckTraitIndex = bossTrait;
                CoreTraitProgression.AddPractice(profile, (CoreTrait)bossTrait, bossXp);

                for (int p = 0; p < 3; p++)
                {
                    int xp = RollTryOurLuckPracticeXp();
                    int traitOrdinal = Random.Range(0, 6);
                    profile.CoopTryLuckPartnerGrantedXp[p] = xp;
                    profile.CoopTryLuckPartnerTraitIndex[p] = traitOrdinal;
                    AddPartnerRubricBonus(profile, p, (CoreTrait)traitOrdinal, xp);
                }
            }
            else if (_coopReasonChoice == 3)
                CoreTraitProgression.AddPractice(profile, CoreTrait.Intelligence, 50);
        }

        if (_draftAnswers.Length > PersonalityQuestionnaire.LastNightQuestionIndex &&
            _draftAnswers[PersonalityQuestionnaire.LastNightQuestionIndex] == 0 &&
            _coopEveningWhereChoice >= 0)
        {
            EnsureCoopPartnerBonusArray(profile);

            bool confessToBar = _coopEveningWhereChoice > 0 && _coopBarContradictionChoice == 1;
            bool barTrapSilenceOrLawyer = _coopEveningWhereChoice > 0 && _coopBarContradictionChoice == 0 &&
                                          (_coopBarFirmKnowledgeTrapChoice == 0 || _coopBarFirmKnowledgeTrapChoice == 1);
            bool barUnawarePlace = _coopEveningWhereChoice > 0 && _coopBarContradictionChoice == 2;

            if (!confessToBar)
            {
                if (_coopEveningWhereChoice == 1)
                {
                    CoreTraitProgression.AddPractice(profile, CoreTrait.Charisma, 50);
                    if (profile.CoopEveningRestaurantTraitRoll == null || profile.CoopEveningRestaurantTraitRoll.Length != 3)
                    {
                        profile.CoopEveningRestaurantTraitRoll = new int[3];
                        for (int p = 0; p < 3; p++)
                            profile.CoopEveningRestaurantTraitRoll[p] = Random.Range(0, 6);
                    }
                    for (int p = 0; p < 3; p++)
                        AddPartnerRubricBonus(profile, p, (CoreTrait)profile.CoopEveningRestaurantTraitRoll[p], 50);
                }
                else if (_coopEveningWhereChoice == 2)
                {
                    CoreTraitProgression.AddPractice(profile, CoreTrait.Agility, 50);
                    for (int p = 0; p < 3; p++)
                        AddPartnerRubricBonus(profile, p, CoreTrait.Agility, 50);
                }
                else if (_coopEveningWhereChoice == 3)
                {
                    CoreTraitProgression.AddPractice(profile, CoreTrait.Strength, 50);
                    for (int p = 0; p < 3; p++)
                        AddPartnerRubricBonus(profile, p, CoreTrait.Strength, 50);
                }
            }

            if (barTrapSilenceOrLawyer)
                CoreTraitProgression.AddPractice(profile, CoreTrait.MentalResilience, 50);

            if (barUnawarePlace)
            {
                CoreTraitProgression.AddPractice(profile, CoreTrait.Charisma, 50);
                CoreTraitProgression.AddPractice(profile, CoreTrait.MentalResilience, 50);
                EnsureBarUnawarePartnerSplitRollArrays(profile);
                for (int p = 0; p < 3; p++)
                    ApplyBarUnawarePartnerSplitRollIfNeeded(profile, p);
            }

            bool barIncidentTruth =
                _coopBarWhatHappenedChoice == 0 &&
                (_coopEveningWhereChoice == 0 ||
                 (_coopEveningWhereChoice > 0 && _coopBarContradictionChoice == 1));
            if (barIncidentTruth)
            {
                CoreTraitProgression.AddPractice(profile, CoreTrait.Charisma, 25);
                CoreTraitProgression.AddPractice(profile, CoreTrait.MentalResilience, 25);
            }
        }
    }

    private static void EnsureCoopPartnerBonusArray(PlayerCharacterProfile profile)
    {
        if (profile == null)
            return;
        if (profile.CoopPartnerRubricBonusXp == null || profile.CoopPartnerRubricBonusXp.Length != 18)
            profile.CoopPartnerRubricBonusXp = new int[18];
    }

    private static readonly int[] TryOurLuckXpPool = { 15, 30, 45, 60, 75, 100, 200 };

    /// <summary>Pick one tier; weight ∝ 1/xp so higher amounts are less likely.</summary>
    private static int RollTryOurLuckPracticeXp()
    {
        int sum = 0;
        for (int i = 0; i < TryOurLuckXpPool.Length; i++)
            sum += 10000 / TryOurLuckXpPool[i];
        int r = Random.Range(0, sum);
        for (int i = 0; i < TryOurLuckXpPool.Length; i++)
        {
            int w = 10000 / TryOurLuckXpPool[i];
            r -= w;
            if (r < 0)
                return TryOurLuckXpPool[i];
        }
        return TryOurLuckXpPool[TryOurLuckXpPool.Length - 1];
    }

    private static void EnsureCoopTryLuckPartnerRollArrays(PlayerCharacterProfile profile)
    {
        if (profile == null)
            return;
        if (profile.CoopTryLuckPartnerGrantedXp == null || profile.CoopTryLuckPartnerGrantedXp.Length != 3)
            profile.CoopTryLuckPartnerGrantedXp = new int[3];
        if (profile.CoopTryLuckPartnerTraitIndex == null || profile.CoopTryLuckPartnerTraitIndex.Length != 3)
            profile.CoopTryLuckPartnerTraitIndex = new int[] { -1, -1, -1 };
    }

    private static void AddPartnerRubricBonus(PlayerCharacterProfile profile, int partnerIndex, CoreTrait trait, int amount)
    {
        if (profile == null || amount == 0 || partnerIndex < 0 || partnerIndex > 2)
            return;
        EnsureCoopPartnerBonusArray(profile);
        int i = partnerIndex * 6 + (int)trait;
        profile.CoopPartnerRubricBonusXp[i] += amount;
    }

    private static void GrantPartnersRandomRubricXp(PlayerCharacterProfile profile, int amount)
    {
        for (int p = 0; p < 3; p++)
            AddPartnerRubricBonus(profile, p, (CoreTrait)Random.Range(0, 6), amount);
    }

    private const float BarUnawareTwoTraitSplitChance = 0.72f;

    private static void EnsureBarUnawarePartnerSplitRollArrays(PlayerCharacterProfile profile)
    {
        if (profile == null)
            return;
        if (profile.CoopBarUnawarePartnerSplitTrait0 == null || profile.CoopBarUnawarePartnerSplitTrait0.Length != 3)
            profile.CoopBarUnawarePartnerSplitTrait0 = new int[] { -1, -1, -1 };
        if (profile.CoopBarUnawarePartnerSplitTrait1 == null || profile.CoopBarUnawarePartnerSplitTrait1.Length != 3)
            profile.CoopBarUnawarePartnerSplitTrait1 = new int[] { -1, -1, -1 };
        if (profile.CoopBarUnawarePartnerSplitXp0 == null || profile.CoopBarUnawarePartnerSplitXp0.Length != 3)
            profile.CoopBarUnawarePartnerSplitXp0 = new int[3];
        if (profile.CoopBarUnawarePartnerSplitXp1 == null || profile.CoopBarUnawarePartnerSplitXp1.Length != 3)
            profile.CoopBarUnawarePartnerSplitXp1 = new int[3];
    }

    /// <summary>100 rubric XP per partner, usually split across two distinct core traits.</summary>
    private static void ApplyBarUnawarePartnerSplitRollIfNeeded(PlayerCharacterProfile profile, int partnerIndex)
    {
        if (profile == null || partnerIndex < 0 || partnerIndex > 2)
            return;
        EnsureCoopPartnerBonusArray(profile);
        EnsureBarUnawarePartnerSplitRollArrays(profile);
        if (profile.CoopBarUnawarePartnerSplitTrait0[partnerIndex] >= 0)
            return;

        if (Random.value < BarUnawareTwoTraitSplitChance)
        {
            int t0 = Random.Range(0, 6);
            int t1;
            do
            {
                t1 = Random.Range(0, 6);
            } while (t1 == t0);
            int x0 = Random.Range(1, 100);
            int x1 = 100 - x0;
            profile.CoopBarUnawarePartnerSplitTrait0[partnerIndex] = t0;
            profile.CoopBarUnawarePartnerSplitTrait1[partnerIndex] = t1;
            profile.CoopBarUnawarePartnerSplitXp0[partnerIndex] = x0;
            profile.CoopBarUnawarePartnerSplitXp1[partnerIndex] = x1;
            AddPartnerRubricBonus(profile, partnerIndex, (CoreTrait)t0, x0);
            AddPartnerRubricBonus(profile, partnerIndex, (CoreTrait)t1, x1);
        }
        else
        {
            int t = Random.Range(0, 6);
            profile.CoopBarUnawarePartnerSplitTrait0[partnerIndex] = t;
            profile.CoopBarUnawarePartnerSplitTrait1[partnerIndex] = -1;
            profile.CoopBarUnawarePartnerSplitXp0[partnerIndex] = 100;
            profile.CoopBarUnawarePartnerSplitXp1[partnerIndex] = 0;
            AddPartnerRubricBonus(profile, partnerIndex, (CoreTrait)t, 100);
        }
    }

    private string GetInterrogationDisplayName()
    {
        if (!string.IsNullOrWhiteSpace(_draftName))
            return _draftName.Trim();
        if (!string.IsNullOrWhiteSpace(_interrogationTypedName))
            return _interrogationTypedName.Trim();
        return "Mr. X";
    }

    private static string GenerateAliasName()
    {
        return CharacterNamePools.RandomFullName();
    }

    private static string PickCoopInventedBarName()
    {
        return CoopInventedBarPool[Random.Range(0, CoopInventedBarPool.Length)];
    }

    private static string CoopEveningPlacePhrase(int eveningChoice)
    {
        switch (eveningChoice)
        {
            case 1: return "You said a restaurant";
            case 2: return "You said the park";
            case 3: return "You said the gym";
            default: return "You said you were not at a bar";
        }
    }

    private bool CoopPostCarInterrogationResolved()
    {
        if (!IsCooperativeInterviewRoute())
            return true;
        if (_draftAnswers.Length <= PersonalityQuestionnaire.CoopCarColorQuestionIndex ||
            _draftAnswers[PersonalityQuestionnaire.CoopCarColorQuestionIndex] < 0)
            return true;
        if (_coopPostCarColorPhase != CoopPostCarColorPhase.None)
            return false;
        if (_coopEveningWhereChoice < 0)
            return false;
        if (_coopEveningWhereChoice == 0)
        {
            if (_coopBarWhatHappenedChoice < 0)
                return false;
            if (_coopBarWhatHappenedChoice == 0 && _coopSnitchChoice < 0)
                return false;
        }
        else
        {
            if (_coopBarContradictionChoice < 0)
                return false;
            if (_coopBarContradictionChoice == 2)
                return true;
            if (_coopBarContradictionChoice == 1)
            {
                if (_coopBarWhatHappenedChoice < 0)
                    return false;
                if (_coopBarWhatHappenedChoice == 0 && _coopSnitchChoice < 0)
                    return false;
            }
        }
        return true;
    }

    private void RandomizeCoopPostCarForSkip()
    {
        _coopPostCarColorPhase = CoopPostCarColorPhase.None;
        _coopBailUsd = 0;
        _coopEveningWhereChoice = Random.Range(0, 4);
        if (_coopEveningWhereChoice == 0)
        {
            _coopBarContradictionChoice = -1;
            _coopInventedBarName = string.Empty;
            _coopBarWhatHappenedChoice = Random.Range(0, 2);
            _coopSnitchChoice = _coopBarWhatHappenedChoice == 0 ? Random.Range(0, 2) : -1;
            _coopInterrogationPressureAdjustment = -10;
            if (_coopBarWhatHappenedChoice == 1)
                _coopInterrogationPressureAdjustment += 15;
            if (_coopBarWhatHappenedChoice == 0 && _coopSnitchChoice == 1)
                _coopInterrogationPressureAdjustment += 20;
        }
        else
        {
            _coopInventedBarName = PickCoopInventedBarName();
            _coopBossCustodyFromBarTrapSilence = false;
            _coopBossCustodyFromBarTrapLawyer = false;
            _coopBarFirmKnowledgeTrapChoice = -1;
            _coopBarContradictionChoice = Random.Range(0, 3);
            if (_coopBarContradictionChoice == 0)
            {
                int trapRoll = Random.Range(0, 3);
                if (trapRoll == 0)
                {
                    _coopBossCustodyFromBarTrapSilence = true;
                    _coopInterrogationPressureAdjustment = 14;
                    _coopBailUsd = 0;
                    _coopBarFirmKnowledgeTrapChoice = 0;
                }
                else if (trapRoll == 1)
                {
                    _coopBossCustodyFromBarTrapLawyer = true;
                    _coopInterrogationPressureAdjustment = 16;
                    _coopBailUsd = 0;
                    _coopBarFirmKnowledgeTrapChoice = 1;
                }
                else
                {
                    _coopBarContradictionChoice = 1;
                    _coopInterrogationPressureAdjustment = -10;
                    _coopBarFirmKnowledgeTrapChoice = -1;
                    _coopBarWhatHappenedChoice = Random.Range(0, 2);
                    _coopSnitchChoice = _coopBarWhatHappenedChoice == 0 ? Random.Range(0, 2) : -1;
                    if (_coopBarWhatHappenedChoice == 1)
                        _coopInterrogationPressureAdjustment += 15;
                    if (_coopBarWhatHappenedChoice == 0 && _coopSnitchChoice == 1)
                        _coopInterrogationPressureAdjustment += 20;
                }
                if (trapRoll < 2)
                {
                    _coopBarWhatHappenedChoice = -1;
                    _coopSnitchChoice = -1;
                }
            }
            else if (_coopBarContradictionChoice == 1)
            {
                _coopInterrogationPressureAdjustment = -10;
                _coopBarWhatHappenedChoice = Random.Range(0, 2);
                _coopSnitchChoice = _coopBarWhatHappenedChoice == 0 ? Random.Range(0, 2) : -1;
                if (_coopBarWhatHappenedChoice == 1)
                    _coopInterrogationPressureAdjustment += 15;
                if (_coopBarWhatHappenedChoice == 0 && _coopSnitchChoice == 1)
                    _coopInterrogationPressureAdjustment += 20;
            }
            else
            {
                _coopInterrogationPressureAdjustment = 25;
                _coopBailUsd = 1000;
                _coopBarWhatHappenedChoice = -1;
                _coopSnitchChoice = -1;
            }
        }
    }

    private bool IsCooperativeInterviewRoute()
    {
        return _draftAnswers.Length > PersonalityQuestionnaire.LastNightQuestionIndex &&
               _draftAnswers[PersonalityQuestionnaire.LastNightQuestionIndex] == 0;
    }

    private int GetEffectiveAccentIndex()
    {
        // Cooperative route: car color pick in interrogation is the crew / family accent color.
        if (IsCooperativeInterviewRoute() &&
            _draftAnswers.Length > PersonalityQuestionnaire.CoopCarColorQuestionIndex)
        {
            int carColor = _draftAnswers[PersonalityQuestionnaire.CoopCarColorQuestionIndex];
            if (carColor >= 0 && carColor < PlayerCharacterProfile.AccentColorCount)
                return carColor;
        }

        int cq = PersonalityQuestionnaire.ColorQuestionIndex;
        if (_draftAnswers.Length > cq)
        {
            int c = _draftAnswers[cq];
            if (c >= 0 && c < PlayerCharacterProfile.AccentColorCount)
                return c;
        }

        if (_draftAccentIndex >= 0)
            return Mathf.Clamp(_draftAccentIndex, 0, PlayerCharacterProfile.AccentColorCount - 1);

        if (_resolvedRandomAccentIndex < 0 || _resolvedRandomAccentIndex >= PlayerCharacterProfile.AccentColorCount)
            _resolvedRandomAccentIndex = Random.Range(0, PlayerCharacterProfile.AccentColorCount);
        return _resolvedRandomAccentIndex;
    }

    private void StartRandomNewGame()
    {
        int randomAccent = Random.Range(0, PlayerCharacterProfile.AccentColorCount);
        string randomPortrait = BossPortraitResourceOptions[Random.Range(0, BossPortraitResourceOptions.Length)];
        bool femalePortrait = DealerPortraitNaming.IsFemalePortraitResourceKey(randomPortrait);
        string randomBossName = CharacterNamePools.RandomFullName(femalePortrait);

        int[] randomAnswers = new int[PersonalityQuestionnaire.QuestionCount];
        for (int i = 0; i < randomAnswers.Length; i++)
        {
            int maxChoice = PersonalityQuestionnaire.ChoiceLabels[i].Length;
            randomAnswers[i] = Random.Range(0, maxChoice);
        }

        PlayerCharacterProfile profile = PersonalityQuestionnaire.BuildProfile(randomBossName, randomAccent, randomAnswers, randomPortrait);
        SeedStartingCrew(profile, true);
        StartGameWithProfile(profile);
    }

    private void SeedStartingCrew(PlayerCharacterProfile profile, bool randomizeAll)
    {
        bool associatesDisclosed =
            profile?.QuestionnaireAnswers != null &&
            profile.QuestionnaireAnswers.Length > PersonalityQuestionnaire.LastNightQuestionIndex &&
            profile.QuestionnaireAnswers[PersonalityQuestionnaire.LastNightQuestionIndex] == 0;

        PersonnelRegistry.Members.Clear();
        PersonnelRegistry.Members.Add(new CrewMember
        {
            Name = (profile?.DisplayName ?? "Boss") + " (Boss)",
            Role = "Boss",
            Status = "Available",
            Loyalty = "Unquestioned",
            Skills = "Command, Strategy",
            PersonalReputation = 18,
            Satisfaction = CrewSatisfactionLevel.Satisfied
        });

        string[] randomCrewFullNames = new string[3];
        if (randomizeAll)
            CharacterNamePools.FillDistinctFullNames(randomCrewFullNames, 3);

        for (int i = 0; i < 3; i++)
        {
            string name = randomizeAll
                ? randomCrewFullNames[i]
                : (string.IsNullOrWhiteSpace(_draftPartnerNames[i]) ? "Fighter " + (i + 1) : _draftPartnerNames[i].Trim());
            int skillIdx;
            if (randomizeAll)
            {
                skillIdx = Random.Range(0, _partnerSkillOptions.Length);
            }
            else if (!associatesDisclosed)
            {
                // If player withheld associate details in interrogation, partner specialties are auto-assigned.
                skillIdx = Random.Range(0, _partnerSkillOptions.Length);
            }
            else if (profile?.QuestionnaireAnswers != null &&
                     profile.QuestionnaireAnswers.Length > PersonalityQuestionnaire.FirstPartnerQuestionIndex + i)
            {
                int ans = profile.QuestionnaireAnswers[PersonalityQuestionnaire.FirstPartnerQuestionIndex + i];
                if (ans < 0)
                    skillIdx = Random.Range(0, _partnerSkillOptions.Length);
                else
                    skillIdx = PersonalityQuestionnaire.GetPartnerSkillIndexFromAnswer(ans);
            }
            else
            {
                skillIdx = Mathf.Clamp(_draftPartnerSkillIndices[i], 0, _partnerSkillOptions.Length - 1);
            }

            int[] rubricRow = new int[6];
            int[] flatBonus = profile?.CoopPartnerRubricBonusXp;
            if (flatBonus != null && flatBonus.Length == 18)
            {
                for (int t = 0; t < 6; t++)
                    rubricRow[t] = flatBonus[i * 6 + t];
            }

            PersonnelRegistry.Members.Add(new CrewMember
            {
                Name = name,
                Role = "Soldier",
                Status = "Available",
                Loyalty = randomizeAll ? "Reliable" : "Loyal",
                Skills = _partnerSkillOptions[skillIdx],
                PersonalReputation = randomizeAll ? Random.Range(4, 20) : 12 + i * 3,
                Satisfaction = CrewSatisfactionLevel.Neutral,
                InterrogationRubricBonusXp = rubricRow
            });
        }
    }

    private void StartGameWithProfile(PlayerCharacterProfile profile)
    {
        GameSessionState.ResetForNewGame();
        ApplyInterrogationConsequences(profile);
        PlayerRunState.SetCharacter(profile);
        GameModeManager.EnsureExists();
        if (GameModeManager.Instance != null)
            GameModeManager.Instance.SetMode(GameModeManager.GameMode.Management);
        if (GameOverlayMenu.Instance != null)
            GameOverlayMenu.Instance.CloseAllPanels();
        SceneManager.LoadScene(PlanningSceneName);
    }

    private void ApplyInterrogationConsequences(PlayerCharacterProfile profile)
    {
        PersonalityQuestionnaire.InterrogationOutcome outcome = PersonalityQuestionnaire.ResolveInterrogationOutcome(profile?.QuestionnaireAnswers);
        GameSessionState.PolicePressure = outcome.PolicePressure;
        GameSessionState.BossKnownToPolice = outcome.BossKnownToPolice;
        GameSessionState.BossStartsInPrison = outcome.Detained == PersonalityQuestionnaire.DetainedTarget.Boss;
        GameSessionState.InterrogationCaseNote = outcome.CaseNote;
        GameSessionState.InterrogationCustodyRisk = outcome.CustodyRisk;
        GameSessionState.AssociatesDisclosedInInterview = outcome.AssociatesDisclosed;
        GameSessionState.BlackCash = outcome.StartingBlackCash;

        if (_silenceRouteVagueChosen)
        {
            GameSessionState.ApplyGameplayPolicePressureDelta(5);
            GameSessionState.InterrogationCaseNote =
                "Interrogation stalled into vague answers. Police raised watch posture and kept pressure on the file.";
        }

        // Cooperative route (Q2 = with three friends): store choices for later partner/crew resolution.
        if (profile?.QuestionnaireAnswers != null &&
            profile.QuestionnaireAnswers.Length > PersonalityQuestionnaire.LastNightQuestionIndex &&
            profile.QuestionnaireAnswers[PersonalityQuestionnaire.LastNightQuestionIndex] == 0)
        {
            GameSessionState.CooperativeReasonChoice = _coopReasonChoice;
            GameSessionState.CooperativeCarOwnerChoice = _coopCarOwnerChoice;

            if (_coopCarOwnerChoice <= 0)
                GameSessionState.CooperativeCarOwnerName = (profile?.DisplayName ?? "Boss") + " (me)";
            else
                GameSessionState.CooperativeCarOwnerName = SafePartnerName(_coopCarOwnerChoice - 1);

            int vc = profile.QuestionnaireAnswers.Length > PersonalityQuestionnaire.CoopCarColorQuestionIndex
                ? profile.QuestionnaireAnswers[PersonalityQuestionnaire.CoopCarColorQuestionIndex]
                : -1;
            GameSessionState.CooperativeVehicleColorIndex =
                (vc >= 0 && vc < PlayerCharacterProfile.AccentColorCount) ? vc : -1;

            if (_coopReasonChoice == 2)
            {
                GameSessionState.CoopTryLuckGrantedXp = profile.CoopTryLuckGrantedXp;
                GameSessionState.CoopTryLuckTraitIndex = profile.CoopTryLuckTraitIndex;
                bool partnerRollsOk = profile.CoopTryLuckPartnerGrantedXp != null && profile.CoopTryLuckPartnerGrantedXp.Length == 3 &&
                                      profile.CoopTryLuckPartnerTraitIndex != null && profile.CoopTryLuckPartnerTraitIndex.Length == 3;
                for (int i = 0; i < 3; i++)
                {
                    if (partnerRollsOk)
                    {
                        GameSessionState.CoopTryLuckPartnerGrantedXp[i] = profile.CoopTryLuckPartnerGrantedXp[i];
                        GameSessionState.CoopTryLuckPartnerTraitIndex[i] = profile.CoopTryLuckPartnerTraitIndex[i];
                    }
                    else
                    {
                        GameSessionState.CoopTryLuckPartnerGrantedXp[i] = 0;
                        GameSessionState.CoopTryLuckPartnerTraitIndex[i] = -1;
                    }
                }
            }
            else
            {
                GameSessionState.CoopTryLuckGrantedXp = 0;
                GameSessionState.CoopTryLuckTraitIndex = -1;
                for (int i = 0; i < 3; i++)
                {
                    GameSessionState.CoopTryLuckPartnerGrantedXp[i] = 0;
                    GameSessionState.CoopTryLuckPartnerTraitIndex[i] = -1;
                }
            }

            if (_coopEveningWhereChoice >= 0)
            {
                GameSessionState.PolicePressure = Mathf.Clamp(
                    GameSessionState.PolicePressure + _coopInterrogationPressureAdjustment, -100, 100);

                bool barIncidentTruth =
                    _coopBarWhatHappenedChoice == 0 &&
                    (_coopEveningWhereChoice == 0 ||
                     (_coopEveningWhereChoice > 0 && _coopBarContradictionChoice == 1));
                if (barIncidentTruth && GameSessionState.PolicePressure >= 0)
                {
                    GameSessionState.PolicePressure =
                        Mathf.Clamp(GameSessionState.PolicePressure - 12, -100, 100);
                    GameSessionState.CoopPoliceHeatRisesSlowly = true;
                }

                if (_coopBossCustodyFromBarTrapSilence)
                    GameSessionState.InterrogationCustodyRisk += 20;
                if (_coopBossCustodyFromBarTrapLawyer)
                    GameSessionState.InterrogationCustodyRisk += 40;

                GameSessionState.CoopInventedBarName = _coopInventedBarName ?? string.Empty;
                GameSessionState.CoopEveningWhereChoice = _coopEveningWhereChoice;
                GameSessionState.CoopBarContradictionChoice = _coopBarContradictionChoice;
                GameSessionState.CoopBarWhatHappenedChoice = _coopBarWhatHappenedChoice;
                GameSessionState.CoopSnitchChoice = _coopSnitchChoice;

                GameSessionState.InterrogationBailUsd = Mathf.Max(0, _coopBailUsd);
                GameSessionState.BlackCash = Mathf.Max(0, GameSessionState.BlackCash - GameSessionState.InterrogationBailUsd);

                GameSessionState.BossIsPoliceInformant =
                    _coopBarWhatHappenedChoice == 0 && _coopSnitchChoice == 0;
                if (GameSessionState.BossIsPoliceInformant)
                {
                    GameSessionState.BossKnownToPolice = true;
                    GameSessionState.BossSnitchKnownToRivalGangs = false;
                    GameSessionState.BossSnitchStreetRevealAtDay =
                        Mathf.Max(1, GameSessionState.CurrentDay) + GameSessionState.BossSnitchStreetRumorDelayDays;
                }
                else
                {
                    GameSessionState.BossSnitchKnownToRivalGangs = false;
                    GameSessionState.BossSnitchStreetRevealAtDay = -1;
                }

                int accent = Mathf.Clamp(profile.AccentColorIndex, 0, PlayerCharacterProfile.AccentColorCount - 1);
                GameSessionState.RivalCrimeFamilyAccentIndex = -1;
                if (_coopBarWhatHappenedChoice == 0 && _coopSnitchChoice == 1)
                {
                    int rival = accent;
                    if (PlayerCharacterProfile.AccentColorCount > 1)
                    {
                        do
                        {
                            rival = Random.Range(0, PlayerCharacterProfile.AccentColorCount);
                        } while (rival == accent);
                    }
                    GameSessionState.RivalCrimeFamilyAccentIndex = rival;
                }

                System.Text.StringBuilder extra = new System.Text.StringBuilder();
                if (GameSessionState.InterrogationBailUsd > 0)
                    extra.Append(" Posted $" + GameSessionState.InterrogationBailUsd.ToString() + " bail; black cash took the hit.");
                if (_coopEveningWhereChoice > 0 && _coopBarContradictionChoice == 0 &&
                    !_coopBossCustodyFromBarTrapSilence && !_coopBossCustodyFromBarTrapLawyer)
                    extra.Append(" You held the lie on record; bail bought hours, not trust — watch squads add your name to their board.");
                if (_coopEveningWhereChoice > 0 && _coopBarContradictionChoice == 2)
                    extra.Append(" You claimed you never heard of the venue; the file notes the performance — uniforms tighten the lens.");
                if (GameSessionState.BossIsPoliceInformant)
                    extra.Append(" The desk files you as a willing contact — useful, and dangerous.");
                if (_coopBarWhatHappenedChoice == 0 && _coopSnitchChoice == 1)
                    extra.Append(
                        " You walked away from the offer; word still moves. A rival family with a different colorset thinks you know who held the blade.");

                if (extra.Length > 0)
                {
                    if (string.IsNullOrEmpty(GameSessionState.InterrogationCaseNote))
                        GameSessionState.InterrogationCaseNote = extra.ToString().Trim();
                    else
                        GameSessionState.InterrogationCaseNote =
                            GameSessionState.InterrogationCaseNote.Trim() + extra;
                }

                if (GameSessionState.RivalCrimeFamilyAccentIndex < 0)
                {
                    const string vagueRivalWarn =
                        "Uniforms leave it vague: watch your back in this district — someone already owns pieces of the street.";
                    if (string.IsNullOrEmpty(GameSessionState.InterrogationCaseNote))
                        GameSessionState.InterrogationCaseNote = vagueRivalWarn;
                    else
                        GameSessionState.InterrogationCaseNote =
                            GameSessionState.InterrogationCaseNote.Trim() + " " + vagueRivalWarn;
                }
            }
        }

        string detainedName = string.Empty;
        if (_coopBossCustodyFromBarTrapSilence || _coopBossCustodyFromBarTrapLawyer)
        {
            GameSessionState.BossStartsInPrison = true;
            GameSessionState.BossPrisonPhase = GameSessionState.PrisonLegalPhase.BeforeTrial;
            GameSessionState.BossKnownToPolice = true;
            GameSessionState.InterrogationCustodyRisk = Mathf.Max(GameSessionState.InterrogationCustodyRisk, 30);
            if (_coopBossCustodyFromBarTrapLawyer)
                GameSessionState.InterrogationPublicDefenderName = CharacterNamePools.RandomFullName();
            else
                GameSessionState.InterrogationPublicDefenderName = string.Empty;
            string trapCustodyTail = _coopBossCustodyFromBarTrapLawyer
                ? "Bar-records trap: you demanded counsel. " + GameSessionState.InterrogationPublicDefenderName +
                  " is assigned as public defender (see Legal). Boss moved to custody."
                : "Bar-records trap: you stayed silent under pressure. A detective sneers — \"Silent as a fish?\" / \"שותק כמו דג?\" — then they book you.";
            if (string.IsNullOrEmpty(GameSessionState.InterrogationCaseNote))
                GameSessionState.InterrogationCaseNote = trapCustodyTail;
            else
                GameSessionState.InterrogationCaseNote = GameSessionState.InterrogationCaseNote.Trim() + " " + trapCustodyTail;
            detainedName = (profile?.DisplayName ?? "Boss") + " (Boss)";
            if (PersonnelRegistry.Members.Count > 0)
            {
                PersonnelRegistry.Members[0].SetStatus(CharacterStatus.Detained);
                PersonnelRegistry.Members[0].Arrest =
                    ArrestRecord.CreateDefault(
                        ArrestCause.Obstruction,
                        GameSessionState.AgencyId.Police,
                        GameSessionState.CurrentDay,
                        "Booked after bar-contradiction trap in interrogation.",
                        ArrestCause.EvidenceTampering);
                PersonnelRegistry.Members[0].Loyalty = "Under Pressure";
            }
        }
        else if (_silenceRouteLawyerCustody)
        {
            GameSessionState.BossStartsInPrison = true;
            GameSessionState.BossPrisonPhase = GameSessionState.PrisonLegalPhase.BeforeTrial;
            GameSessionState.BossKnownToPolice = true;
            GameSessionState.InterrogationCustodyRisk = Mathf.Max(GameSessionState.InterrogationCustodyRisk, 30);
            GameSessionState.InterrogationCustodyRisk += 35;
            GameSessionState.InterrogationPublicDefenderName = CharacterNamePools.RandomFullName();
            GameSessionState.InterrogationCaseNote =
                "Player requested a public defender and remained silent. " + GameSessionState.InterrogationPublicDefenderName +
                " is assigned (Legal tab). Immediate transfer to custody; police widen the net on known associates.";
            detainedName = (profile?.DisplayName ?? "Boss") + " (Boss)";
            if (PersonnelRegistry.Members.Count > 0)
            {
                PersonnelRegistry.Members[0].SetStatus(CharacterStatus.Detained);
                PersonnelRegistry.Members[0].Arrest =
                    ArrestRecord.CreateDefault(
                        ArrestCause.Obstruction,
                        GameSessionState.AgencyId.Police,
                        GameSessionState.CurrentDay,
                        "Booked after interrogation route.",
                        ArrestCause.EvidenceTampering);
                PersonnelRegistry.Members[0].Loyalty = "Under Pressure";
            }
        }
        else if (_aggressiveRouteCustody)
        {
            GameSessionState.BossStartsInPrison = true;
            GameSessionState.BossPrisonPhase = GameSessionState.PrisonLegalPhase.BeforeTrial;
            GameSessionState.BossKnownToPolice = true;
            GameSessionState.InterrogationCustodyRisk = Mathf.Max(GameSessionState.InterrogationCustodyRisk, 30);
            GameSessionState.InterrogationCaseNote =
                "Player challenged the accusation through legal deflection. Detectives closed the room and moved the boss to custody.";
            detainedName = (profile?.DisplayName ?? "Boss") + " (Boss)";
            if (PersonnelRegistry.Members.Count > 0)
            {
                PersonnelRegistry.Members[0].SetStatus(CharacterStatus.Detained);
                PersonnelRegistry.Members[0].Arrest =
                    ArrestRecord.CreateDefault(
                        ArrestCause.Obstruction,
                        GameSessionState.AgencyId.Police,
                        GameSessionState.CurrentDay,
                        "Booked after interrogation route.",
                        ArrestCause.EvidenceTampering);
                PersonnelRegistry.Members[0].Loyalty = "Under Pressure";
            }
        }
        else if (outcome.Detained == PersonalityQuestionnaire.DetainedTarget.Boss)
        {
            detainedName = (profile?.DisplayName ?? "Boss") + " (Boss)";
            GameSessionState.BossPrisonPhase = GameSessionState.PrisonLegalPhase.BeforeTrial;
            if (PersonnelRegistry.Members.Count > 0)
            {
                PersonnelRegistry.Members[0].SetStatus(CharacterStatus.Detained);
                PersonnelRegistry.Members[0].Arrest =
                    ArrestRecord.CreateDefault(
                        ArrestCause.OutstandingWarrant,
                        GameSessionState.AgencyId.Police,
                        GameSessionState.CurrentDay,
                        "Picked up on an outstanding warrant.",
                        ArrestCause.RacketeeringConspiracy);
                PersonnelRegistry.Members[0].Loyalty = "Feared";
            }
        }
        else if (outcome.Detained >= PersonalityQuestionnaire.DetainedTarget.Partner1 &&
                 outcome.Detained <= PersonalityQuestionnaire.DetainedTarget.Partner3)
        {
            int partnerSlot = (int)outcome.Detained - (int)PersonalityQuestionnaire.DetainedTarget.Partner1 + 1;
            int crewIndex = partnerSlot; // 0=Boss, 1..3 are associates.
            if (crewIndex >= 0 && crewIndex < PersonnelRegistry.Members.Count)
            {
                CrewMember detained = PersonnelRegistry.Members[crewIndex];
                detained.SetStatus(CharacterStatus.Detained);
                detained.Arrest =
                    ArrestRecord.CreateDefault(
                        ArrestCause.WeaponsPossession,
                        GameSessionState.AgencyId.Police,
                        GameSessionState.CurrentDay,
                        "Traffic stop turned into a weapons search.",
                        ArrestCause.ArmedThreats);
                detained.Loyalty = "Shaken";
                detainedName = detained.Name;
            }
        }

        GameSessionState.InitialDetainedCharacterName = detainedName;
    }

    private static Texture2D LoadPortraitTexture(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
            return null;
        return DealerPortraitNaming.LoadPortraitTexture(resourcePath);
    }

    private static void DrawPortraitPreviewFill(Rect targetRect, Texture2D portrait)
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
            // Cover: crop vertically. Bias UV toward texture top so faces / full head stay in frame.
            float viewH = texAspect / rectAspect;
            float yCenter = (1f - viewH) * 0.5f;
            const float topBias = 0.18f;
            float y = Mathf.Clamp(yCenter + topBias, 0f, 1f - viewH);
            uv = new Rect(0f, y, 1f, viewH);
        }

        GUI.DrawTextureWithTexCoords(targetRect, portrait, uv, true);
        GUI.color = prev;
    }

    private static Texture2D GetCircularPortraitThumb(string portraitPath, Texture2D source)
    {
        if (source == null || string.IsNullOrWhiteSpace(portraitPath))
            return null;

        Rect uv = GetPortraitThumbUv(portraitPath);
        string key = portraitPath + "|" + uv.x.ToString("F3") + "|" + uv.y.ToString("F3") + "|" + uv.width.ToString("F3") + "|" + uv.height.ToString("F3");
        if (_portraitCircleThumbCache.TryGetValue(key, out Texture2D cached) && cached != null)
            return cached;

        Texture2D readable = CreateReadableCopy(source);
        if (readable == null)
            return null;

        const int size = 84;
        Texture2D circleThumb = new Texture2D(size, size, TextureFormat.RGBA32, false);
        circleThumb.filterMode = FilterMode.Point;
        circleThumb.wrapMode = TextureWrapMode.Clamp;

        float center = (size - 1) * 0.5f;
        float radius = center - 0.5f;
        float radiusSq = radius * radius;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float distSq = dx * dx + dy * dy;
                if (distSq > radiusSq)
                {
                    circleThumb.SetPixel(x, y, new Color(0f, 0f, 0f, 0f));
                    continue;
                }

                float nx = (x + 0.5f) / size;
                float ny = (y + 0.5f) / size;
                float u = uv.x + nx * uv.width;
                float v = uv.y + ny * uv.height;
                int px = Mathf.Clamp(Mathf.RoundToInt(u * (readable.width - 1)), 0, readable.width - 1);
                int py = Mathf.Clamp(Mathf.RoundToInt(v * (readable.height - 1)), 0, readable.height - 1);
                Color c = readable.GetPixel(px, py);
                c.a = 1f;
                circleThumb.SetPixel(x, y, c);
            }
        }

        circleThumb.Apply(false);
        Object.Destroy(readable);
        _portraitCircleThumbCache[key] = circleThumb;
        return circleThumb;
    }

    private static Texture2D CreateReadableCopy(Texture2D source)
    {
        if (source == null)
            return null;

        RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
        RenderTexture prev = RenderTexture.active;
        Graphics.Blit(source, rt);
        RenderTexture.active = rt;

        Texture2D copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
        copy.Apply(false);

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        return copy;
    }

    private static Rect GetPortraitThumbUv(string portraitPath)
    {
        // Keep fixed zoom for all portraits; adjust only framing position (x/y).
        string k = string.IsNullOrWhiteSpace(portraitPath)
            ? "BossPortrait"
            : DealerPortraitNaming.NormalizeToCarouselKey(portraitPath);

        const float fixedSize = 0.60f;
        float x = 0.20f;
        float y = 0.34f;
        float zoom = fixedSize;

        // Per-portrait overrides for better face framing when source images differ.
        switch (k)
        {
            case "Dealer":
                x = 0.20f; y = 0.34f;
                break;
            case "Dealer2":
                x = 0.24f; y = 0.36f;
                break;
            case "Dealer3":
                x = 0.23f; y = 0.35f;
                break;
            case "Dealer4":
                x = 0.16f; y = 0.34f;
                break;
            case "Dealer5":
                x = 0.15f; y = 0.35f;
                break;
            case "Dealer6":
                x = 0.24f; y = 0.36f;
                break;
            case "Dealer7":
                x = 0.13f; y = 0.33f;
                break;
            case "Dealer8":
                x = 0.12f; y = 0.33f;
                break;
            // Legacy BossPortrait9 maps here; tighter zoom matches old 10th male asset.
            case "Dealer9":
                x = 0.14f;
                y = 0.34f;
                zoom = 0.52f;
                break;
            case "DealerF":
            case "DealerF1":
            case "DealerF2":
                x = 0.18f; y = 0.30f;
                zoom = 0.54f;
                break;
        }

        Rect uv = new Rect(x, y, zoom, zoom);

        uv.x = Mathf.Clamp01(uv.x);
        uv.y = Mathf.Clamp01(uv.y);
        uv.width = Mathf.Clamp(uv.width, 0.01f, 1f - uv.x);
        uv.height = Mathf.Clamp(uv.height, 0.01f, 1f - uv.y);
        return uv;
    }
}
