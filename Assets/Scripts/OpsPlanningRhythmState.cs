using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Planning-day load: hours each roster slot has committed to queued missions (parallel wall time per member).
/// Resets when the calendar day advances. Fatigue advisory uses fixed thresholds (design: same for everyone).
/// </summary>
public static class OpsPlanningRhythmState
{
    const float WarnAfterConsecutiveHours = 12f;
    const float SkillsSlipAfter = 16f;
    const float MoraleSlipAfter = 20f;
    const float FatalRiskAfter = 24f;

    static int _trackedCalendarDay = -1;
    static float[] _committedHoursByMemberIndex;

    static void EnsureBuckets()
    {
        int n = PersonnelRegistry.Members != null ? PersonnelRegistry.Members.Count : 0;
        n = Mathf.Max(n, 16);
        if (_committedHoursByMemberIndex == null || _committedHoursByMemberIndex.Length < n)
            _committedHoursByMemberIndex = new float[n];
    }

    public static void EnsureCalendarDay(int calendarDay)
    {
        if (_trackedCalendarDay == calendarDay)
            return;
        _trackedCalendarDay = calendarDay;
        EnsureBuckets();
        for (int i = 0; i < _committedHoursByMemberIndex.Length; i++)
            _committedHoursByMemberIndex[i] = 0f;
    }

    public static void ResetForNewGame()
    {
        _trackedCalendarDay = -1;
        _committedHoursByMemberIndex = null;
    }

    public static float GetCommittedHoursToday(int memberIndex)
    {
        EnsureCalendarDay(GameSessionState.CurrentDay);
        if (memberIndex < 0 || _committedHoursByMemberIndex == null || memberIndex >= _committedHoursByMemberIndex.Length)
            return 0f;
        return _committedHoursByMemberIndex[memberIndex];
    }

    public static void AddMissionWallHoursForMembers(IReadOnlyList<int> memberIndices, float hours)
    {
        if (memberIndices == null || hours <= 0f)
            return;
        EnsureCalendarDay(GameSessionState.CurrentDay);
        EnsureBuckets();
        for (int k = 0; k < memberIndices.Count; k++)
        {
            int i = memberIndices[k];
            if (i < 0)
                continue;
            while (i >= _committedHoursByMemberIndex.Length)
            {
                var grown = new float[_committedHoursByMemberIndex.Length * 2];
                System.Array.Copy(_committedHoursByMemberIndex, grown, _committedHoursByMemberIndex.Length);
                _committedHoursByMemberIndex = grown;
            }

            _committedHoursByMemberIndex[i] += hours;
        }
    }

    public static void SubtractMissionWallHoursForMembers(IReadOnlyList<int> memberIndices, float hours)
    {
        if (memberIndices == null || hours <= 0f)
            return;
        EnsureCalendarDay(GameSessionState.CurrentDay);
        if (_committedHoursByMemberIndex == null)
            return;
        for (int k = 0; k < memberIndices.Count; k++)
        {
            int i = memberIndices[k];
            if (i < 0 || i >= _committedHoursByMemberIndex.Length)
                continue;
            _committedHoursByMemberIndex[i] = Mathf.Max(0f, _committedHoursByMemberIndex[i] - hours);
        }
    }

    /// <summary>Lines for Ops modal / board after scheduling (cumulative = already committed + this mission).</summary>
    public static string BuildFatigueAdvisory(float cumulativeHoursForMember, float additionalMissionHours)
    {
        float after = cumulativeHoursForMember + additionalMissionHours;
        var lines = new List<string>(6);
        lines.Add("<size=92%><color=#b8b6b0>Hours today (after this mission): <b>" + after.ToString("0.#") + "</b> h / 24</color></size>");

        if (after > FatalRiskAfter)
            lines.Add("<color=#e07070><b>Severe fatigue risk</b> — over 24h on the clock: fatal mistake chance, rep &amp; fight spirit suffer.</color>");
        else if (after > MoraleSlipAfter)
            lines.Add("<color=#d8a070>Over 20h: morale &amp; grumbling escalate.</color>");
        else if (after > SkillsSlipAfter)
            lines.Add("<color=#c9b87a>Over 16h: skills &amp; sharpness slip.</color>");
        else if (after > WarnAfterConsecutiveHours)
            lines.Add("<color=#a8c4e8>Over 12h without rest: fatigue warning.</color>");

        if (after > 24f && additionalMissionHours > 0f)
            lines.Add("<color=#e09090><i>Scheduling past 24h — severe fatigue warning.</i></color>");

        return string.Join("\n", lines);
    }

    /// <summary>Stub: weekly rest minimum (24h) — extend with real rest ledger later.</summary>
    public static string BuildWeeklyRestStubLine(CrewMember m)
    {
        if (m == null)
            return string.Empty;
        int anchor = Mathf.Max(1, m.JoinedFamilyOnDay);
        int rel = GameSessionState.CurrentDay - anchor;
        int week = rel >= 0 ? rel / 7 : 0;
        return "<size=88%><color=#909090>Personal week #" + (week + 1) + " (since day " + anchor + ") · full rest target: <b>24h/week</b> <i>(ledger next)</i></color></size>";
    }
}
