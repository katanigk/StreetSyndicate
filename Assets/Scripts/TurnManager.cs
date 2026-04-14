using UnityEngine;
using TMPro;

public class TurnManager : MonoBehaviour
{
    public int currentDay = 1;
    public TextMeshProUGUI dayLabel;

    private void OnEnable()
    {
        GameModeManager.EnsureExists();
        GameModeManager.Instance.SetMode(GameModeManager.GameMode.Management);

        currentDay = GameSessionState.CurrentDay;
        UpdateDayLabel();
    }

    /// <summary>
    /// Wired to "End Turn" in planning: submit readiness (or cancel if already submitted).
    /// </summary>
    public void EndTurn()
    {
        if (PlanningFlowController.Instance != null)
            PlanningFlowController.Instance.TrySubmitOrTogglePlanningReady();
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainScene");
    }

    private void UpdateDayLabel()
    {
        if (dayLabel != null)
        {
            currentDay = GameSessionState.CurrentDay;
            dayLabel.text = GameCalendarSystem.FormatPlanningHudLine(GameSessionState.CurrentDay);
        }
    }
}