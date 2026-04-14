using UnityEngine;

/// <summary>
/// End-of-day police posture: persistent pressure decays on quiet days; street-stop chance uses
/// interrogation custody risk + ambient pressure (probability check, not a visible die).
/// </summary>
public static class PoliceStreetPressureDaily
{
    const int QuietDayPressureDecay = 2;
    const int QuietDayCustodyDecay = 1;
    const int StreetStopPressureBumpMin = 3;
    const int StreetStopPressureBumpMaxExtra = 5;
    const int StreetStopCustodyRelief = 5;

    public static void ProcessAfterDayAdvanced(int previousDay, int newDay)
    {
        _ = previousDay;
        _ = newDay;

        if (PersonnelRegistry.Members == null || PersonnelRegistry.Members.Count == 0)
            return;

        if (CharacterStatusUtility.IsIncarcerated(PersonnelRegistry.Members[0].GetResolvedStatus()))
            return;

        bool quietDay = GameSessionState.LastDayMissionsCompleted == 0 && GameSessionState.LastDayMissionsFailed == 0;

        if (quietDay && GameSessionState.PolicePressure > 0)
            GameSessionState.ApplyGameplayPolicePressureDelta(-QuietDayPressureDecay);

        if (quietDay && GameSessionState.InterrogationCustodyRisk > 0)
            GameSessionState.InterrogationCustodyRisk =
                Mathf.Max(0, GameSessionState.InterrogationCustodyRisk - QuietDayCustodyDecay);

        float p = ComputeStreetStopProbability01();
        if (Random.value >= p)
            return;

        int custody = Mathf.Max(0, GameSessionState.InterrogationCustodyRisk);
        int extraBump = custody <= 0 ? 0 : Mathf.Min(StreetStopPressureBumpMaxExtra, custody / 8);
        GameSessionState.ApplyGameplayPolicePressureDelta(StreetStopPressureBumpMin + extraBump);
        GameSessionState.InterrogationCustodyRisk = Mathf.Max(0, GameSessionState.InterrogationCustodyRisk - StreetStopCustodyRelief);

        GameSessionState.LastPoliceInvestigationUpdate =
            "Street stop: a patrol pulls you aside for a papers check. Your name is on a watch list — heat ticks up, then they let you walk.";
    }

    /// <summary>Single end-of-day probability (0..1) for a street stop / shake-down event.</summary>
    public static float ComputeStreetStopProbability01()
    {
        float p = 0.018f;
        p += Mathf.Clamp01(GameSessionState.InterrogationCustodyRisk / 220f);
        p += Mathf.Clamp01(GameSessionState.PolicePressureDisplayValue() / 450f);
        if (GameSessionState.BossKnownToPolice)
            p += 0.048f;
        return Mathf.Clamp01(p);
    }
}
