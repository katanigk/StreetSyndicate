using UnityEngine;

/// <summary>Aggregates world scan into a 0–100+ federal interest score (deterministic inputs only).</summary>
public static class FederalInterestScoreResolver
{
    public static int Compute(FederalBureauWorldScan s, int citySeed)
    {
        if (s == null)
            return 0;
        int orgThreat = 0;
        if (s.organizationStageInt >= (int)GameSessionState.OrganizationStage.CrimeFamily)
            orgThreat = 22;
        else if (s.organizationStageInt >= (int)GameSessionState.OrganizationStage.Crew)
            orgThreat = 12;
        orgThreat += Mathf.Min(25, s.playerThreatScore / 2);

        int prohibition = Mathf.Min(18, s.blackCash / 2500);
        int substance = Mathf.Min(15, s.speakeasyPressureHint);
        int oc = Mathf.Min(12, s.dryEnforcementHint);
        int policeFailure = Mathf.Min(20, s.policeStuckCasesHint);
        int political = Mathf.Min(12, s.priorPoliticalPressure / 5);
        int strategic = 5 + (Mathf.Abs(Mix(s.dayIndex, citySeed)) % 8);
        int exposurePenalty = Mathf.Min(25, s.publicExposureHint / 3);
        int resourceCost = Mathf.Min(8, s.activeFederalCaseCount * 2);

        int sum = orgThreat + prohibition + substance + oc + policeFailure + political + strategic
                  - exposurePenalty - resourceCost;
        return Mathf.Clamp(sum, 0, 100);
    }

    static int Mix(int a, int b)
    {
        unchecked
        {
            return a * 0x45D9F3B ^ b * 0x27D4EB2D;
        }
    }
}
