using FamilyBusiness.CityGen.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Execution phase: timed day, then summary overlay, then back to planning.
/// Runs ordered operations (Scout, Surveillance, Collect) from Ops tab.
/// </summary>
public class ExecutionBootstrap : MonoBehaviour
{
    [SerializeField] private Transform scoutTargetPoint;
    [SerializeField] private Transform surveillanceTargetPoint;
    [SerializeField] private Transform collectTargetPoint;

    private float _executionSecondsLeft;
    private bool _summaryShown;
    private string _opsSummaryText = "No operations resolved.";
    private Vector2 _summaryScroll;

    private void Start()
    {
        GameOverlayMenu.EnsureExists();
        GameModeManager.EnsureExists();

        GameSessionState.IsDaySummaryShowing = false;
        _executionSecondsLeft = GameSessionState.ExecutionDayDurationSeconds;
        _summaryShown = false;

        GameModeManager.Instance.SetMode(GameModeManager.GameMode.Action);

        ResolveTargetPoints();
        RunOrderedOperations();
    }

    private void ResolveTargetPoints()
    {
        if (scoutTargetPoint == null)
        {
            GameObject found = GameObject.Find("ScoutPoint");
            if (found != null) scoutTargetPoint = found.transform;
        }
        if (surveillanceTargetPoint == null)
            surveillanceTargetPoint = scoutTargetPoint;
        if (collectTargetPoint == null)
            collectTargetPoint = scoutTargetPoint;
    }

    private void RunOrderedOperations()
    {
        UnitMover[] movers = FindObjectsByType<UnitMover>(FindObjectsSortMode.None);
        if (GameSessionState.OrderedOperations.Count == 0)
        {
            GameSessionState.LastDayMissionsCompleted = 0;
            GameSessionState.LastDayMissionsFailed = 0;
            GameSessionState.LastDaySoldiersReleased = 0;
            _opsSummaryText = "No operations were queued.";
            return;
        }

        int completed = 0;
        int failed = 0;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("Operation results:");

        PlayerCharacterProfile boss = PlayerRunState.Character;
        int repBefore = boss != null ? Mathf.Clamp(boss.PublicReputation, -100, 100) : 0;
        for (int i = 0; i < GameSessionState.OrderedOperations.Count; i++)
        {
            OperationType op = GameSessionState.OrderedOperations[i];
            OperationResolution res = OperationResolutionSystem.Resolve(op, boss);
            CrewReputationSystem.ApplyOperationOutcome(op, res);
            PoliceInvestigationSystem.ProcessOperationOutcome(op, res, boss);
            if (res.Success) completed++;
            else failed++;

            string line = "• " + OperationRegistry.GetName(op) + ": " +
                OutcomeTierMapper.GetDisplayName(res.Tier) +
                "  |  " + DerivedSkillProgression.GetDisplayName(res.LinkedSkill) +
                "  |  obj " + Mathf.RoundToInt(res.ObjectiveScore) +
                " / cons " + Mathf.RoundToInt(res.ConsequenceScore) +
                "  (" + OperationRegistry.GetTraitName(res.PrimaryTrait) + "/" +
                OperationRegistry.GetTraitName(res.SecondaryTrait) + ")";
            sb.AppendLine(line);

            if (i == 0 && movers.Length > 0)
            {
                Vector3 target = GetTargetForOperation(op);
                foreach (UnitMover mover in movers)
                    mover.MoveToWorldPosition(target);
            }
        }

        GameSessionState.LastDayMissionsCompleted = completed;
        GameSessionState.LastDayMissionsFailed = failed;
        GameSessionState.LastDaySoldiersReleased = failed > completed ? 1 : 0;
        int repAfter = boss != null ? Mathf.Clamp(boss.PublicReputation, -100, 100) : repBefore;
        int repDelta = repAfter - repBefore;
        sb.AppendLine();
        sb.AppendLine("Crew reputation: " + repBefore + " -> " + repAfter +
                      " (" + (repDelta >= 0 ? "+" : string.Empty) + repDelta + ")");
        _opsSummaryText = sb.ToString();

        ApplySandboxDiscoveryAfterOps();
    }

    static void ApplySandboxDiscoveryAfterOps()
    {
        if (!GameSessionState.SingleBlockSandboxEnabled)
            return;
        GameSessionState.EnsureActiveCityData();
        CityData city = GameSessionState.ActiveCityData;
        if (city == null || GameSessionState.OrderedOperations.Count == 0)
            return;

        MicroBlockAnchorResolver.EnsureCrewHomeBlockAnchored(city);
        int home = MicroBlockWorldState.CrewHomeBlockId;
        if (home < 0)
            return;

        for (int i = 0; i < GameSessionState.OrderedOperations.Count; i++)
        {
            OperationType op = GameSessionState.OrderedOperations[i];
            switch (op)
            {
                case OperationType.Scout:
                case OperationType.Surveillance:
                case OperationType.Collect:
                    int dest = GameSessionState.GetOperationTargetBlockId(op);
                    SandboxMapDiscovery.RevealTravelPathAndDestination(city, home, dest);
                    break;
            }
        }
    }

    private Vector3 GetTargetForOperation(OperationType op)
    {
        Transform t = op switch
        {
            OperationType.Scout => scoutTargetPoint,
            OperationType.Surveillance => surveillanceTargetPoint,
            OperationType.Collect => collectTargetPoint,
            _ => scoutTargetPoint
        };
        return t != null ? t.position : Vector3.zero;
    }

    private void Update()
    {
        if (_summaryShown)
            return;

        _executionSecondsLeft -= Time.deltaTime;
        if (_executionSecondsLeft <= 0f)
            ShowDaySummary();
    }

    private void ShowDaySummary()
    {
        _summaryShown = true;
        GameSessionState.IsDaySummaryShowing = true;
    }

    private void OnGUI()
    {
        if (!_summaryShown)
        {
            GUILayout.BeginArea(new Rect(12, 12, 280, 40));
            GUILayout.Label("Execution — time left: " + Mathf.CeilToInt(Mathf.Max(0f, _executionSecondsLeft)) + " s");
            GUILayout.EndArea();
            return;
        }

        float w = 480f;
        float h = 420f;
        Rect box = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
        GUI.Box(box, GUIContent.none);
        GUILayout.BeginArea(box);
        GUILayout.Label("Day summary", GUI.skin.box);
        GUILayout.Space(8);
        _summaryScroll = GUILayout.BeginScrollView(_summaryScroll, GUILayout.ExpandHeight(true));
        GUILayout.Label(GameCalendarSystem.FormatPlanningHudLine(GameSessionState.CurrentDay));
        GUILayout.Label("Opposition scale: " + GameCalendarSystem.GetOppositionMultiplier(GameSessionState.CurrentDay).ToString("F2") + "×");
        GUILayout.Space(8);
        GUILayout.Label("Missions completed: " + GameSessionState.LastDayMissionsCompleted);
        GUILayout.Label("Missions failed: " + GameSessionState.LastDayMissionsFailed);
        GUILayout.Label("Soldiers released / rotated: " + GameSessionState.LastDaySoldiersReleased);
        GUILayout.Space(12);
        GUILayout.Label(_opsSummaryText);
        GUILayout.Space(12);
        GUILayout.Label("Read the report, then continue to planning.");
        GUILayout.EndScrollView();
        GUILayout.Space(8);
        if (GUILayout.Button("Continue", GUILayout.Height(44f)))
            ReturnToPlanningAfterSummary();
        GUILayout.EndArea();
    }

    public void ReturnToPlanningAfterSummary()
    {
        GameSessionState.IsDaySummaryShowing = false;
        int previousWeek = GameSessionState.CurrentDay;
        GameSessionState.CurrentDay++;
        OpsPlanningRhythmState.EnsureCalendarDay(GameSessionState.CurrentDay);
        MicroBlockDayAdvanceHooks.AfterDayIncremented(previousWeek, GameSessionState.CurrentDay);
        ApplyMonthlyPrisonXpIfNeeded(previousWeek, GameSessionState.CurrentDay);
        GameSessionState.OrderedOperations.Clear();
        GameSessionState.ClearOperationAssignees();
        GameSessionState.ScoutMissionOrdered = false;
        GameModeManager.EnsureExists();
        GameModeManager.Instance.SetMode(GameModeManager.GameMode.Management);

        SceneManager.LoadScene("PlanningScene");
    }

    private static void ApplyMonthlyPrisonXpIfNeeded(int previousWeek, int currentWeek)
    {
        int prevMonth = GameCalendarSystem.GetMonth(previousWeek);
        int curMonth = GameCalendarSystem.GetMonth(currentWeek);
        if (prevMonth == curMonth)
            return;

        // Baseline prison progression: +50 XP per month while incarcerated.
        for (int i = 0; i < PersonnelRegistry.Members.Count; i++)
        {
            CrewMember member = PersonnelRegistry.Members[i];
            if (member == null || !CharacterStatusUtility.IsIncarcerated(member.GetResolvedStatus()))
                continue;
            member.PrisonTrainingXp += 50;
        }
        // Uniform prison progression for now: everyone in prison gets the same monthly XP bucket.
    }
}
