using System;
using UnityEngine;

public static class FederalDirectorPolicyCalibrationEngine
{
    static int Mix(int a, int b) { unchecked { return a * 0x45D9F3B ^ b * 0x27D4EB2D; } }

    /// <summary>Transitive calibration for <paramref name="day"/>. Persists a snapshot when due (monthly) or on day 1.</summary>
    public static FederalDirectorPolicyCalibration CalibrateForDay(int day, int citySeed, int interest, FederalBureauWorldScan scan, bool forceRecord = false)
    {
        // Director id from spec / bootstrap
        const string did = "fbu_dir_01";
        if (!BureauWorldState.IsBootstrapped)
            return null;
        int year = 1 + day / 360;
        int q = 1 + ((day - 1) / 90) % 4;
        var cal = new FederalDirectorPolicyCalibration
        {
            calibrationId = "fdircal_" + day + "_" + (Mathf.Abs(Mix(day, citySeed)) % 1_000_000).ToString("D6"),
            directorAgentId = did,
            day = day,
            year = year,
            quarter = q,
            createdAtTicks = DateTime.UtcNow.Ticks
        };
        int minP = Mathf.Clamp(50 + BureauWorldState.politicalPressure / 2 - (scan != null ? scan.playerThreatScore / 4 : 0), 0, 100);
        int govP = Mathf.Clamp(40 + BureauWorldState.publicExposure, 0, 100);
        int elP = ((day + 7) % 60) < 8 ? 35 : 12; // small election-campaign stub window
        int prP = Mathf.Clamp(30 + interest / 3, 0, 100);
        cal.ministerPressure = minP;
        cal.governorPressure = govP;
        cal.electionPressure = elP;
        cal.pressPressure = prP;
        cal.publicTrust0to100 = Mathf.Clamp(80 - BureauWorldState.publicExposure - (scan != null ? scan.publicExposureHint : 0) / 2, 0, 100);
        cal.organizedCrimeThreat = scan != null ? Mathf.Clamp(scan.speakeasyPressureHint * 2 + (scan.organizationStageInt) * 5, 0, 100) : 40;
        cal.visibleSuccessDemand = Mathf.Clamp(45 + (100 - cal.publicTrust0to100) / 2, 0, 100);
        int policeFail = 0;
        if (scan != null)
            policeFail = Mathf.Min(100, (scan.policeStuckCasesHint * 4) + (scan.priorBureauHeat > 50 ? 15 : 0));
        cal.policeFailurePressure = policeFail;
        cal.recentScandalRisk = Mathf.Min(100, (BureauWorldState.currentHeat + BureauWorldState.publicExposure) / 2);
        if (BureauWorldState.Budget != null)
            cal.budgetPressure = Mathf.Clamp(50 - (BureauWorldState.Budget.classifiedFundMinor + BureauWorldState.Budget.officialBudgetMinor) / 1_000_000, 0, 100);
        else
            cal.budgetPressure = 20;
        cal.realImpactScore0to100 = Mathf.Clamp(interest * 2 / 3, 0, 100);
        cal.politicalAppearanceScore0to100 = Mathf.Clamp(30 + (100 - cal.realImpactScore0to100) / 2 + (cal.pressPressure) / 4, 0, 100);
        if (BureauWorldState.federalRuntimeExposureEvents01 > 0) { cal.recentScandalRisk = Mathf.Max(cal.recentScandalRisk, 50); }
        // Personality tie-breakers from hash of director
        int dirH = 0xA11;
        var dAgent = BureauWorldState.GetAgent(did);
        if (dAgent != null) dirH = dAgent.agentId != null ? dAgent.agentId.GetHashCode() ^ day : dirH;
        int personality = (dirH ^ citySeed) & 7;
        cal.selectedPolicyModeInt = (int)PickMode(cal, interest, day, citySeed, personality, scan);
        cal.declaredPolicyModeInt = cal.selectedPolicyModeInt; // v1: same; future split
        cal.hiddenPolicyModeInt = 0;
        cal.reasonTags.Clear();
        if (minP > 60) cal.reasonTags.Add("MinisterDemand");
        if (govP > 60) cal.reasonTags.Add("GovernorDemand");
        if (elP > 30) cal.reasonTags.Add("ElectionCycle");
        if (cal.recentScandalRisk > 55) cal.reasonTags.Add("PressScandal");
        if (cal.publicTrust0to100 < 40) cal.reasonTags.Add("PublicTrustLow");
        if (cal.organizedCrimeThreat > 65) cal.reasonTags.Add("OrganizedCrimeRising");
        if (cal.visibleSuccessDemand > 60) cal.reasonTags.Add("NeedVisibleWin");
        if (cal.policeFailurePressure > 55) cal.reasonTags.Add("PoliceFailure");
        if (cal.budgetPressure > 55) cal.reasonTags.Add("BudgetShortage");
        if (BureauWorldState.federalRuntimeExposureEvents01 > 2) cal.reasonTags.Add("RecentFederalFailure");
        if (personality >= 4) cal.reasonTags.Add("DirectorAmbition");
        if (BureauWorldState.currentHeat > 60) cal.reasonTags.Add("DirectorFear");
        if (BureauWorldState.Budget != null && BureauWorldState.Budget.suspiciousSpendingRisk > 30) cal.reasonTags.Add("DirectorCorruption");
        if (cal.reasonTags.Count == 0) cal.reasonTags.Add("DirectorAmbition");
        cal.visibilityLevel = (BureauWorldState.currentHeat < 30 && (dirH & 1) == 0)
            ? (int)FederalPolicyVisibility.Rumored
            : (int)FederalPolicyVisibility.PartiallyKnown;
        BureauWorldState.currentDirectorPolicyModeInt = cal.selectedPolicyModeInt;
        // Monthly snapshot
        int interval = 30;
        bool rec = forceRecord
            || BureauWorldState.lastDirectorCalibrationRecordDay < 0
            || (day - BureauWorldState.lastDirectorCalibrationRecordDay >= interval)
            || (cal.recentScandalRisk >= 80 && (day - BureauWorldState.lastDirectorCalibrationRecordDay >= 3));
        if (rec)
        {
            BureauWorldState.directorPolicyCalibrations.Add(cal);
            BureauWorldState.lastDirectorCalibrationRecordDay = day;
            while (BureauWorldState.directorPolicyCalibrations.Count > 32)
                BureauWorldState.directorPolicyCalibrations.RemoveAt(0);
        }
        return cal;
    }

    static FederalDirectorPolicyMode PickMode(
        FederalDirectorPolicyCalibration c,
        int interest,
        int day,
        int citySeed,
        int personality,
        FederalBureauWorldScan scan)
    {
        if (c == null) return FederalDirectorPolicyMode.SilentInfiltration;
        if (c.recentScandalRisk >= 75 || c.pressPressure >= 80)
            return FederalDirectorPolicyMode.DamageControl;
        if (c.electionPressure > 28 && c.visibleSuccessDemand > 55)
            return FederalDirectorPolicyMode.PoliticalSurvival;
        if (c.policeFailurePressure >= 60 && c.policeFailurePressure >= c.organizedCrimeThreat)
            return FederalDirectorPolicyMode.PoliceOverride;
        if (c.publicTrust0to100 < 40 && c.pressPressure > 45)
            return FederalDirectorPolicyMode.PublicLegitimacy;
        if (c.organizedCrimeThreat >= 80 && c.realImpactScore0to100 < 50 && (Mathf.Abs(Mix(day, citySeed)) % 3) != 0)
            return FederalDirectorPolicyMode.StrategicDecapitation;
        if (c.organizedCrimeThreat > 60 && c.pressPressure > 60)
            return FederalDirectorPolicyMode.SilentInfiltration;
        if (c.ministerPressure + c.visibleSuccessDemand > 130 && interest > 50)
            return (personality & 1) == 0
                ? FederalDirectorPolicyMode.AggressiveEnforcement
                : FederalDirectorPolicyMode.PoliticalSurvival;
        if (c.governorPressure + c.recentScandalRisk > 120)
            return FederalDirectorPolicyMode.PublicLegitimacy;
        return FederalDirectorPolicyMode.SilentInfiltration;
    }
}
