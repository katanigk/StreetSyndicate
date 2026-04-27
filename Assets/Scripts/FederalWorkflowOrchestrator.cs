using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>End-to-end federal workflow: request → authority (<see cref="FederalAuthorityResolver"/>) → approver → operation → stub execution → logs → consequences.</summary>
public static class FederalWorkflowOrchestrator
{
    public static string NewRequestId() => "fw_req_" + Guid.NewGuid().ToString("N");
    public static string NewOperationId() => "fw_op_" + Guid.NewGuid().ToString("N");
    public static string NewLogId() => "fw_log_" + Guid.NewGuid().ToString("N");
    public static string NewDeviationId() => "fw_dev_" + Guid.NewGuid().ToString("N");

    public static FederalActionRequest BuildActionRequest(FederalWorkflowRequest w)
    {
        string caseId = !string.IsNullOrEmpty(w.relatedCaseId) ? w.relatedCaseId : (w.relatedPoliceCaseId ?? string.Empty);
        return new FederalActionRequest
        {
            actionType = w.actionType,
            requestingAgentId = w.requestedByAgentId,
            targetType = (int)w.targetType,
            targetId = w.targetId ?? string.Empty,
            caseId = caseId,
            facilityId = w.facilityId ?? string.Empty,
            requestedBudgetMinor = w.requestedBudgetMinor,
            requiresSecrecy = w.secrecyRequired,
            emergencyClaimed = w.emergencyClaimed,
            hasWarrant = w.hasWarrant,
            hasPoliceAccessLog = w.hasPoliceAccessLog,
            hasTakeoverLog = w.hasTakeoverLog,
            isLargeScaleOp = w.isLargeScaleOp,
            targetIsPublicFigure = w.targetIsPublicFigure
        };
    }

    static FederalAuthorityResolution CloneResolution(FederalAuthorityResolution r)
    {
        var c = r;
        c.violationFlags = r.violationFlags != null ? new List<string>(r.violationFlags) : new List<string>();
        return c;
    }

    public static string CreateRequest(FederalWorkflowRequest req)
    {
        if (string.IsNullOrEmpty(req.requestId))
            req.requestId = NewRequestId();
        req.createdAtTicks = DateTime.UtcNow.Ticks;
        req.createdAtDay = GameSessionState.CurrentDay >= 1 ? GameSessionState.CurrentDay : 1;
        if (string.IsNullOrEmpty(req.targetId)) req.targetId = string.Empty;
        req.phase = FederalWorkflowRequestPhase.Draft;
        BureauWorldState.PendingWorkflowRequests.Add(req);
        AppendLog(NewLogId(), null, req.requestId, "CreateRequest", req.requestedByAgentId, "created");
        return req.requestId;
    }

    public static bool TryResolveAuthority(string requestId, out string error)
    {
        error = null;
        var req = FindPendingRequest(requestId);
        if (req == null) { error = "Request not found."; return false; }
        var agent = BureauWorldState.GetAgent(req.requestedByAgentId);
        if (agent == null) { error = "Requester not in roster."; return false; }
        var actionReq = BuildActionRequest(req);
        var res = FederalAuthorityResolver.Resolve(actionReq, agent);
        req.hasAuthorityStep = true;
        req.authorityStep = CloneResolution(res);
        req.authorityAllowed = res.isAllowed;
        req.phase = FederalWorkflowRequestPhase.AuthorityResolved;
        AppendLog(NewLogId(), null, requestId, "ResolveAuthority", req.requestedByAgentId, res.summary ?? string.Empty);
        return true;
    }

    public static bool TryFindApproverForRequest(string requestId, out string error)
    {
        error = null;
        var req = FindPendingRequest(requestId);
        if (req == null) { error = "Request not found."; return false; }
        if (!req.hasAuthorityStep) { error = "Resolve authority first."; return false; }
        if (!req.authorityAllowed)
        {
            req.approverStatus = FederalApproverLookupStatus.MissingApprover;
            error = "Authority denied — no approver stage.";
            return false;
        }
        var res = req.authorityStep;
        if (FederalApproverResolver.TryFindApprover(req.requestedByAgentId, res, req.actionType, out string approverId, out var st))
        {
            req.approverStatus = st;
            req.approverAgentId = approverId;
            if (st == FederalApproverLookupStatus.MissingApprover)
            {
                error = "No suitable approver in roster.";
                return false;
            }
            req.phase = FederalWorkflowRequestPhase.ApproverResolved;
            AppendLog(NewLogId(), null, requestId, "FindApprover", approverId, "approver assigned");
            return true;
        }
        req.approverStatus = FederalApproverLookupStatus.MissingApprover;
        req.approverAgentId = null;
        error = "MissingApprover";
        return false;
    }

    public static bool TryRecordApprovalDecision(string requestId, FederalApprovalDecision decision, out string error)
    {
        error = null;
        var req = FindPendingRequest(requestId);
        if (req == null) { error = "Request not found."; return false; }
        req.approvalDecision = decision;
        req.hasApprovalDecision = true;
        if (decision == FederalApprovalDecision.Deny) req.phase = FederalWorkflowRequestPhase.Denied;
        AppendLog(NewLogId(), null, requestId, "ApprovalDecision", req.approverAgentId, decision.ToString());
        return true;
    }

    public static bool TrySpawnOperationFromApprovedRequest(string requestId, out FederalWorkflowOperation op, out string error)
    {
        op = null;
        error = null;
        var req = FindPendingRequest(requestId);
        if (req == null) { error = "Request not found."; return false; }
        if (!req.hasAuthorityStep || !req.authorityAllowed) { error = "Authority not allowed."; return false; }
        if (req.approverStatus == FederalApproverLookupStatus.MissingApprover || string.IsNullOrEmpty(req.approverAgentId))
        { error = "Missing approver."; return false; }
        if (req.hasApprovalDecision && req.approvalDecision == FederalApprovalDecision.Deny)
        { error = "Request denied."; return false; }

        op = new FederalWorkflowOperation
        {
            operationId = NewOperationId(),
            requestId = req.requestId,
            actionType = req.actionType,
            status = FederalOperationStatus.Assigned,
            requestedByAgentId = req.requestedByAgentId,
            approvedByAgentId = req.approverAgentId,
            responsibleAgentId = req.approverAgentId,
            targetType = req.targetType,
            targetId = req.targetId,
            relatedCaseId = req.relatedCaseId,
            relatedPoliceCaseId = req.relatedPoliceCaseId,
            facilityId = req.facilityId,
            hasAuthorizationResolution = true,
            authorizationResolution = CloneResolution(req.authorityStep),
            startedAtTicks = DateTime.UtcNow.Ticks,
            outcomeInt = (int)FederalOperationOutcome.PartialSuccess,
            summaryLine = "Spawned from workflow (stub).",
        };
        if (string.IsNullOrEmpty(op.approvedByAgentId))
        {
            error = "Invariant: no approvedByAgentId.";
            return false;
        }

        AssignAgentsStub(op, req);
        ResolveApplicablePoliciesStub(op);
        op.complianceBandInt = (int)FederalComplianceResolver.Resolve(
            BureauWorldState.GetAgent(req.requestedByAgentId), op.operationId, req.createdAtDay,
            BureauWorldState.BootstrapSeed, 0, 0);
        op.fieldDecision = BuildFieldDecisionStub(op);
        op.hasFieldDecision = op.fieldDecision != null;

        var val = FederalWorkflowValidation.ValidateOperationInvariants(op);
        if (!val.isValid)
        {
            error = "Validation: " + string.Join("; ", val.issues);
            return false;
        }

        req.phase = FederalWorkflowRequestPhase.PromotedToOperation;
        BureauWorldState.PendingWorkflowRequests.Remove(req);
        BureauWorldState.ActiveWorkflowOperations.Add(op);

        AppendLog(NewLogId(), op.operationId, requestId, "SpawnOperation", op.approvedByAgentId, "active");
        return true;
    }

    static void AssignAgentsStub(FederalWorkflowOperation op, FederalWorkflowRequest req)
    {
        op.executedByAgentIds.Clear();
        for (int i = 0; i < BureauWorldState.Roster.Count && op.executedByAgentIds.Count < 2; i++)
        {
            var a = BureauWorldState.Roster[i];
            if (a == null) continue;
            if (string.Equals(a.agentId, req.requestedByAgentId, StringComparison.OrdinalIgnoreCase)) continue;
            if (!a.availableForField) continue;
            op.executedByAgentIds.Add(a.agentId);
        }
        if (op.executedByAgentIds.Count == 0)
            op.executedByAgentIds.Add(req.requestedByAgentId);
    }

    static void ResolveApplicablePoliciesStub(FederalWorkflowOperation op)
    {
        op.applicablePolicies.Clear();
        op.applicablePolicies.Add("bureau:director_line_default");
        op.applicablePolicies.Add("unit:field_safety_baseline");
    }

    static FederalFieldDecision BuildFieldDecisionStub(FederalWorkflowOperation op)
    {
        return new FederalFieldDecision
        {
            operationId = op.operationId,
            teamLeadAgentId = op.executedByAgentIds.Count > 0 ? op.executedByAgentIds[0] : op.requestedByAgentId,
            finalFieldChoice = FederalFieldChoice.FollowOrders,
            fieldInterpretation = "stub: follow stack orders",
            deviationRisk = 5,
            reasonTag = "FieldStub"
        };
    }

    public static bool RunStubExecution(string operationId, int currentDay, out string error)
    {
        error = null;
        var op = FindActiveOperation(operationId);
        if (op == null) { error = "Operation not active."; return false; }
        op.status = FederalOperationStatus.InProgress;
        FederalWorkflowValidation.ValidateOperationInvariants(op);

        // Stub outcome (deterministic, but not always success): arrest/raid can fail with escape.
        int roll = Mathf.Abs(StubOutcomeHash(op, currentDay)) % 100;
        bool highRiskTacticalAction = op.actionType == FederalActionType.FederalArrest || op.actionType == FederalActionType.FederalRaid;
        int failThreshold = highRiskTacticalAction ? 28 : 12;
        int severeThreshold = highRiskTacticalAction ? 8 : 3;
        if (roll < severeThreshold)
        {
            op.outcomeInt = (int)FederalOperationOutcome.SevereFailure;
            op.operationalResultSub = 0;
            op.legalResultSub = 0;
            op.secrecyResultSub = 0;
            op.politicalResultSub = -2;
            op.evidenceIntelResultSub = 0;
            op.exposureFlags.Add((int)FederalExposureFlag.PublicIncident);
            op.exposureFlags.Add((int)FederalExposureFlag.CriminalsSuspectFederalPresence);
            op.status = FederalOperationStatus.Completed;
            op.summaryLine = "Stub severe failure: target escaped and operation exposed.";
        }
        else if (roll < failThreshold)
        {
            op.outcomeInt = (int)FederalOperationOutcome.Failure;
            op.operationalResultSub = 0;
            op.legalResultSub = 1;
            op.secrecyResultSub = 0;
            op.politicalResultSub = -1;
            op.evidenceIntelResultSub = 1;
            op.exposureFlags.Add((int)FederalExposureFlag.CriminalsSuspectFederalPresence);
            op.status = FederalOperationStatus.Completed;
            op.summaryLine = "Stub failure: target evaded capture.";
        }
        else
        {
            op.outcomeInt = (int)FederalOperationOutcome.Success;
            op.operationalResultSub = 1;
            op.legalResultSub = 1;
            op.secrecyResultSub = 1;
            op.politicalResultSub = 0;
            op.evidenceIntelResultSub = 1;
            op.status = FederalOperationStatus.Completed;
            op.summaryLine = "Stub execution complete.";
        }
        op.completedAtTicks = DateTime.UtcNow.Ticks;

        BureauWorldState.ActiveWorkflowOperations.Remove(op);
        BureauWorldState.CompletedWorkflowOperations.Add(op);

        AppendLog(NewLogId(), operationId, op.requestId, "ExecuteStub", op.requestedByAgentId, "completed");
        WriteOperationLogSnapshot(op, currentDay);
        ApplyBasicConsequences(op, currentDay);
        return true;
    }

    static int StubOutcomeHash(FederalWorkflowOperation op, int day)
    {
        unchecked
        {
            int h = day * 0x1B873593;
            h ^= op != null && op.operationId != null ? op.operationId.GetHashCode() : 0;
            h ^= op != null && op.targetId != null ? op.targetId.GetHashCode() : 0;
            h ^= (int)(op != null ? op.actionType : FederalActionType.OpenFederalCase) * 0x27D4EB2D;
            h ^= BureauWorldState.currentHeat * 31;
            h ^= BureauWorldState.publicExposure * 17;
            return h;
        }
    }

    public static void WriteOperationLogSnapshot(FederalWorkflowOperation op, int day)
    {
        var l = new FederalOperationLog
        {
            logId = NewLogId(),
            timestampTicks = DateTime.UtcNow.Ticks,
            operationId = op.operationId,
            actionType = op.actionType,
            statusInt = (int)op.status,
            requestedByAgentId = op.requestedByAgentId,
            approvedByAgentId = op.approvedByAgentId,
            responsibleAgentId = op.responsibleAgentId,
            supervisingAgentId = op.supervisingAgentId,
            executedByAgentIds = new List<string>(op.executedByAgentIds),
            targetTypeInt = (int)op.targetType,
            targetId = op.targetId,
            outcomeInt = op.outcomeInt,
        };
        for (int i = 0; i < op.violationFlags.Count; i++) l.violationFlags.Add(op.violationFlags[i]);
        for (int i = 0; i < op.exposureFlags.Count; i++) l.exposureFlags.Add(op.exposureFlags[i]);
        BureauWorldState.FederalOperationLogs.Add(l);
    }

    static void AppendLog(string logId, string opId, string requestId, string eventType, string actor, string notes)
    {
        BureauWorldState.FederalWorkflowLogs.Add(new FederalWorkflowLog
        {
            logId = logId,
            operationId = opId,
            requestId = requestId,
            timestampTicks = DateTime.UtcNow.Ticks,
            eventType = eventType,
            actorAgentId = actor,
            notes = notes
        });
    }

    static void ApplyBasicConsequences(FederalWorkflowOperation op, int day)
    {
        if (op == null) return;
        if (op.outcomeInt == (int)FederalOperationOutcome.Failure || op.outcomeInt == (int)FederalOperationOutcome.SevereFailure)
        {
            BureauWorldState.currentHeat = Mathf.Min(100, BureauWorldState.currentHeat + 2);
            return;
        }
        BureauWorldState.federalAggressionLevel = Mathf.Clamp(BureauWorldState.federalAggressionLevel, 0, 100);
    }

    public static bool RunStubFullPipeline(
        string requestId,
        int currentDay,
        out FederalWorkflowOperation op,
        out string error)
    {
        op = null;
        error = null;
        if (!TryResolveAuthority(requestId, out error)) return false;
        if (!TryFindApproverForRequest(requestId, out error))
        {
            TryRecordApprovalDecision(requestId, FederalApprovalDecision.Deny, out _);
            return false;
        }
        TryRecordApprovalDecision(requestId, FederalApprovalDecision.Approve, out _);
        if (!TrySpawnOperationFromApprovedRequest(requestId, out op, out error) || op == null)
            return false;
        return RunStubExecution(op.operationId, currentDay, out error);
    }

    static FederalWorkflowRequest FindPendingRequest(string id)
    {
        for (int i = 0; i < BureauWorldState.PendingWorkflowRequests.Count; i++)
        {
            if (BureauWorldState.PendingWorkflowRequests[i] != null
                && string.Equals(BureauWorldState.PendingWorkflowRequests[i].requestId, id, StringComparison.OrdinalIgnoreCase))
                return BureauWorldState.PendingWorkflowRequests[i];
        }
        return null;
    }

    static FederalWorkflowOperation FindActiveOperation(string opId)
    {
        for (int i = 0; i < BureauWorldState.ActiveWorkflowOperations.Count; i++)
        {
            if (BureauWorldState.ActiveWorkflowOperations[i] != null
                && string.Equals(BureauWorldState.ActiveWorkflowOperations[i].operationId, opId, StringComparison.OrdinalIgnoreCase))
                return BureauWorldState.ActiveWorkflowOperations[i];
        }
        return null;
    }

    // --- Named pipeline steps (for callers / future day loop) ---
    public static void ResolveApplicablePolicies(FederalWorkflowOperation op) => ResolveApplicablePoliciesStub(op);

    public static void ResolveFieldCompliance(FederalWorkflowOperation op, int dayIndex)
    {
        if (op == null) return;
        var reqBy = BureauWorldState.GetAgent(op.requestedByAgentId);
        op.complianceBandInt = (int)FederalComplianceResolver.Resolve(
            reqBy, op.operationId, dayIndex, BureauWorldState.BootstrapSeed, 0, 0);
    }

    public static void ResolveFieldDeviation(FederalWorkflowOperation op)
    {
        if (op == null) return;
        op.fieldDecision = BuildFieldDecisionStub(op);
        op.hasFieldDecision = op.fieldDecision != null;
    }

    /// <summary>Same as <see cref="RunStubExecution"/> — placeholder field execution until operations are simulated.</summary>
    public static bool ExecuteOperationStub(string operationId, int currentDay, out string error) =>
        RunStubExecution(operationId, currentDay, out error);
}
