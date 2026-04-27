using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>High-level day focus for the federal Bureau runtime; one primary choice per day.</summary>
public enum FederalDailyStrategy
{
    Observe = 0,
    Infiltrate = 1,
    Pressure = 2,
    BuildCase = 3,
    TakeOverPoliceCase = 4,
    Strike = 5,
    LayLow = 6,
    CoverUp = 7
}

[Serializable]
public class FederalBureauWorldScan
{
    public int dayIndex;
    public int organizationStageInt;
    public int playerThreatScore;
    public int blackCash;
    public int publicReputationHint;
    public int activePoliceCaseCount;
    public int activeFederalCaseCount;
    public int speakeasyPressureHint;
    public int dryEnforcementHint;
    public int policeStuckCasesHint;
    public int publicExposureHint;
    public int priorBureauHeat;
    public int priorPoliticalPressure;
    public int priorFederalAggression;
}

[Serializable]
public class FederalDailyReport
{
    public int day;
    public int bureauStatusBeforeInt;
    public int bureauStatusAfterInt;
    public int selectedStrategyInt;
    public int federalInterestScore;
    public List<string> selectedTargets = new List<string>();
    public List<string> generatedRequestIds = new List<string>();
    public List<string> completedOperationIds = new List<string>();
    public List<string> newFederalCaseIds = new List<string>();
    public List<string> updatedFederalCaseIds = new List<string>();
    public int exposureChange;
    public int politicalPressureChange;
    public int policyInfluence0to100;
    public int fieldCultureResistance0to100;
    public int directorPolicyModeInt;
    public string notes;
}
