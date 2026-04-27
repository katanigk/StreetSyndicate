using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FederalWorkflowValidationResult
{
    public bool isValid;
    public List<string> issues = new List<string>();
    public List<int> flags = new List<int>();
}

public static class FederalWorkflowValidation
{
    public static void Report(FederalWorkflowValidationResult r, string message)
    {
        if (r == null)
            return;
        r.isValid = false;
        r.issues.Add(message);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.LogError("[FederalWorkflow] " + message);
#endif
    }

    public static FederalWorkflowValidationResult ValidateOperationInvariants(FederalWorkflowOperation o)
    {
        var r = new FederalWorkflowValidationResult { isValid = true };
        if (o == null)
        {
            Report(r, "Operation is null.");
            return r;
        }
        if (string.IsNullOrEmpty(o.requestedByAgentId))
            Report(r, "Invariant: operation requires requestedByAgentId.");
        if (o.status == FederalOperationStatus.Approved
            || o.status == FederalOperationStatus.Assigned
            || o.status == FederalOperationStatus.InProgress
            || o.status == FederalOperationStatus.Completed)
        {
            if (string.IsNullOrEmpty(o.approvedByAgentId))
                Report(r, "Invariant: active operation requires approvedByAgentId.");
        }
        if ((o.status == FederalOperationStatus.Assigned
             || o.status == FederalOperationStatus.InProgress
             || o.status == FederalOperationStatus.Completed)
            && (o.executedByAgentIds == null || o.executedByAgentIds.Count == 0))
            Report(r, "Invariant: assigned+ requires executedByAgentIds.");
        if (o.actionType == FederalActionType.SecurityThreatRemoval)
        {
            if (!o.hasAuthorizationResolution)
                Report(r, "Invariant: SecurityThreatRemoval requires explicit authorization resolution.");
            else
            {
                if (!o.authorizationResolution.requiresDirectorApproval)
                    Report(r, "Invariant: SecurityThreatRemoval requires director approval.");
                if (!o.authorizationResolution.requiresWarrant)
                    Report(r, "Invariant: SecurityThreatRemoval requires judicial order flag.");
            }
        }
        return r;
    }
}
