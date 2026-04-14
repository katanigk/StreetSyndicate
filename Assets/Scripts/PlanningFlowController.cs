using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Planning flow: submit/cancel readiness, sync with other players, then load execution together.
/// </summary>
public class PlanningFlowController : MonoBehaviour
{
    public static PlanningFlowController Instance { get; private set; }

    [SerializeField] [Tooltip("Set to 4 etc. to test waiting for others. With 1, execution starts right after you submit.")]
    private int totalPlayersForSync = 1;

    [SerializeField] [Tooltip("Editor / dev: simulate other players ready (fills remaining slots).")]
    private KeyCode debugSimulateOthersReadyKey = KeyCode.F1;
    [SerializeField] [Tooltip("Temporary: skip visual execution scene and resolve operations instantly in planning.")]
    private bool resolveExecutionBehindTheScenes = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        GameSessionState.TotalPlayers = Mathf.Max(1, totalPlayersForSync);
        GameOverlayMenu.EnsureExists();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(debugSimulateOthersReadyKey))
            DebugSimulateOtherPlayersReady();
#endif
    }

    /// <summary>
    /// Same as End Turn button: first press = ready and wait for others; press again = cancel.
    /// </summary>
    public void TrySubmitOrTogglePlanningReady()
    {
        if (GameSessionState.LocalPlayerPlanningReady)
        {
            CancelLocalPlanningReady();
            return;
        }

        GameSessionState.LocalPlayerPlanningReady = true;
        GameSessionState.ReadyPlayersCount++;

        if (GameSessionState.AllPlayersSubmittedPlanning())
            BeginExecutionPhase();
    }

    private void CancelLocalPlanningReady()
    {
        if (!GameSessionState.LocalPlayerPlanningReady)
            return;

        GameSessionState.LocalPlayerPlanningReady = false;
        GameSessionState.ReadyPlayersCount--;
        if (GameSessionState.ReadyPlayersCount < 0)
            GameSessionState.ReadyPlayersCount = 0;
    }

#if UNITY_EDITOR
    private void DebugSimulateOtherPlayersReady()
    {
        GameSessionState.LocalPlayerPlanningReady = true;
        GameSessionState.ReadyPlayersCount = GameSessionState.TotalPlayers;
        if (GameSessionState.AllPlayersSubmittedPlanning())
            BeginExecutionPhase();
    }
#endif

    public void SetScoutMission(bool ordered)
    {
        GameSessionState.ScoutMissionOrdered = ordered;
    }

    public void CancelPlanningFromMenu()
    {
        CancelLocalPlanningReady();
    }

    public void BeginExecutionPhase()
    {
        GameSessionState.SyncScoutToOrderedOps();
        GameSessionState.ResetPlanningSubmissionState();
        if (resolveExecutionBehindTheScenes)
        {
            ResolveExecutionSilently();
            SceneManager.LoadScene("PlanningScene");
            return;
        }

        SceneManager.LoadScene("MainScene");
    }

    private static void ResolveExecutionSilently()
    {
        PlayerCharacterProfile boss = PlayerRunState.Character;
        int completed = 0;
        int failed = 0;
        int repBefore = boss != null ? Mathf.Clamp(boss.PublicReputation, -100, 100) : 0;

        for (int i = 0; i < GameSessionState.OrderedOperations.Count; i++)
        {
            OperationType op = GameSessionState.OrderedOperations[i];
            OperationResolution res = OperationResolutionSystem.Resolve(op, boss);
            CrewReputationSystem.ApplyOperationOutcome(op, res);
            if (res.Success) completed++;
            else failed++;
        }

        GameSessionState.LastDayMissionsCompleted = completed;
        GameSessionState.LastDayMissionsFailed = failed;
        GameSessionState.LastDaySoldiersReleased = failed > completed ? 1 : 0;
        int repAfter = boss != null ? Mathf.Clamp(boss.PublicReputation, -100, 100) : repBefore;
        _ = repAfter - repBefore; // kept for parity with visual flow; no popup in silent mode.

        int prevDay = GameSessionState.CurrentDay;
        GameSessionState.CurrentDay++;
        OpsPlanningRhythmState.EnsureCalendarDay(GameSessionState.CurrentDay);
        MicroBlockDayAdvanceHooks.AfterDayIncremented(prevDay, GameSessionState.CurrentDay);
        GameSessionState.OrderedOperations.Clear();
        GameSessionState.ClearOperationAssignees();
        GameSessionState.ScoutMissionOrdered = false;
    }

    private void OnGUI()
    {
        if (!GameSessionState.LocalPlayerPlanningReady)
            return;

        if (GameSessionState.AllPlayersSubmittedPlanning())
            return;

        GUILayout.BeginArea(new Rect(12, 12, 520, 80));
        GUILayout.Label(
            "Waiting for other players: " + GameSessionState.ReadyPlayersCount + " / " + GameSessionState.TotalPlayers +
            " — ESC or End Turn again to cancel.");
        GUILayout.EndArea();
    }
}
