using System;
using System.Collections.Generic;

/// <summary>Runtime invariants: never crash; return <see cref="FederalWorkflowValidationResult"/> style flags.</summary>
public static class FederalBureauRuntimeInvariants
{
    public static void TryValidateRequestPlan(
        FederalActionType at,
        BureauOperationalStatus st,
        string targetId,
        string reason,
        string relatedPoliceCaseId,
        string relatedFederalCaseId,
        List<string> outIssues)
    {
        if (outIssues == null) return;
        if (string.IsNullOrEmpty(targetId) && at != FederalActionType.UseClassifiedFund && at != FederalActionType.CreateRegisteredFacility)
            outIssues.Add("Request needs target for this action type.");
        if (string.IsNullOrEmpty(reason))
            outIssues.Add("Request needs reason text.");

        if (at == FederalActionType.FederalRaid)
        {
            if (st != BureauOperationalStatus.Active && st != BureauOperationalStatus.Aggressive && st != BureauOperationalStatus.Crisis)
                outIssues.Add("FederalRaid: bureau must be Active, Aggressive, or Crisis.");
        }
        if (at == FederalActionType.TakeOverPoliceCase)
        {
            if (string.IsNullOrEmpty(relatedPoliceCaseId))
                outIssues.Add("TakeOverPoliceCase: relatedPoliceCaseId required.");
        }
        if (at == FederalActionType.DeepCoverInsertion)
        {
            if (string.IsNullOrEmpty(relatedFederalCaseId) && string.IsNullOrEmpty(relatedPoliceCaseId))
                outIssues.Add("DeepCoverInsertion: case id link expected.");
        }
        if (at == FederalActionType.SecurityThreatRemoval)
        {
            if (string.IsNullOrEmpty(relatedFederalCaseId))
                outIssues.Add("SecurityThreatRemoval: related federal case required.");
            if (st == BureauOperationalStatus.Dormant || st == BureauOperationalStatus.Watching)
                outIssues.Add("SecurityThreatRemoval: bureau must be Active/Aggressive/Crisis.");
        }
    }

    public static bool CoverUpAllowance(bool hasPriorExposureOrDeviation)
    {
        return hasPriorExposureOrDeviation;
    }
}
