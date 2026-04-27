using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class IntakeRecord
{
    public string intakeId;
    public string sourceKind; // intel / incident / complaint / command
    public string reporterId;
    public string subjectId;
    public SuspicionSubjectType subjectType;
    public string locationId;
    public long at;
    public int initialReliability; // 0..100
    public int urgency; // 0..100
    public string summary;
}

public enum InitialAssessmentDecision
{
    Dismissed,
    LoggedOnly,
    SuspicionRecordCreated,
    PreliminaryCaseOpened
}

[Serializable]
public class WorkflowStageResult
{
    public bool Success;
    public string Message;
}

[Serializable]
public class PoliceWorkflowRunResult
{
    public IntakeRecord Intake;
    public InitialAssessmentDecision AssessmentDecision;
    public SuspicionEvaluationResult Suspicion;
    public CaseFile CaseFile;
    public PoliceActionResolution LegalResolution;
    public PoliceOutcomeBundle OperationalResolution;
    public EvidenceItem GeneratedEvidence;
    public InternalReviewCase InternalReview;
    public WorkflowStageResult CaseUpdate;
    public WorkflowStageResult WorldReaction;
    public WorkflowStageResult DecayAndEscalation;
}

public static class IntakeResolver
{
    public static IntakeRecord Create(
        string sourceKind,
        string reporterId,
        string subjectId,
        SuspicionSubjectType subjectType,
        string locationId,
        int initialReliability,
        int urgency,
        string summary)
    {
        return new IntakeRecord
        {
            intakeId = "intake_" + Guid.NewGuid().ToString("N"),
            sourceKind = sourceKind ?? "unknown",
            reporterId = reporterId,
            subjectId = subjectId,
            subjectType = subjectType,
            locationId = locationId,
            at = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            initialReliability = Mathf.Clamp(initialReliability, 0, 100),
            urgency = Mathf.Clamp(urgency, 0, 100),
            summary = summary ?? string.Empty
        };
    }
}

public static class InitialAssessmentResolver
{
    public static InitialAssessmentDecision Resolve(IntakeRecord intake)
    {
        if (intake == null)
            return InitialAssessmentDecision.Dismissed;
        if (intake.initialReliability < 15 && intake.urgency < 20)
            return InitialAssessmentDecision.Dismissed;
        if (intake.initialReliability < 30)
            return InitialAssessmentDecision.LoggedOnly;
        if (intake.initialReliability < 55)
            return InitialAssessmentDecision.SuspicionRecordCreated;
        return InitialAssessmentDecision.PreliminaryCaseOpened;
    }
}

public static class SuspicionResolver
{
    public static SuspicionEvaluationResult Resolve(
        SuspicionRecord record,
        OfficerProfile officer,
        bool complementaryFriskIndicatorPresent,
        bool additionalEscalationFactorPresent)
    {
        return PoliceReasonableSuspicion.Evaluate(
            record,
            officer,
            complementaryFriskIndicatorPresent,
            additionalEscalationFactorPresent,
            actionEscalatedBeyondScore: false,
            civilianComplaintLodged: false,
            reportMismatchDetected: false,
            officerDisputeDetected: false);
    }
}

public static class InvestigativeActionResolver
{
    public static ActionType SelectActionFromSuspicion(SuspicionEvaluationResult suspicion)
    {
        return suspicion.LegalUsability switch
        {
            LegalUsabilityLevel.Insufficient => ActionType.Approach,
            LegalUsabilityLevel.ContactOnly => ActionType.Question,
            LegalUsabilityLevel.DetainAllowed => ActionType.Detain,
            LegalUsabilityLevel.FriskEligible => ActionType.FriskSearch,
            LegalUsabilityLevel.EscalationCandidate => ActionType.ExtendedSurveillance,
            _ => ActionType.Approach
        };
    }
}

public static class AuthorizationResolver
{
    public static PoliceActionResolution Resolve(PoliceActionRequest request)
    {
        return PoliceLegalCodexRules.ResolvePoliceActionLegality(request);
    }
}

public static class PoliceOperationResolver
{
    public static PoliceOutcomeBundle Resolve(
        OfficerProfile officer,
        object target,
        PoliceActionContext context,
        LegalGroundState legalGround,
        PoliceAuthorizationLevel authorizationLevel,
        PoliceDocumentationQuality documentationQuality,
        PoliceActionType actionType)
    {
        return PoliceActionResolver.Resolve(officer, target, context, legalGround, authorizationLevel, documentationQuality, actionType);
    }
}

public static class ReportResolver
{
    public static List<string> BuildReports(PoliceActionResolution legalResolution, PoliceOutcomeBundle operation)
    {
        List<string> reports = new List<string>();
        if (legalResolution == null)
            return reports;
        if (legalResolution.requiredDocumentationLevel == DocumentationLevel.Basic) reports.Add("PoliceActionReport.Basic");
        if (legalResolution.requiredDocumentationLevel == DocumentationLevel.Full) reports.Add("PoliceActionReport.Full");
        if (legalResolution.requiredDocumentationLevel == DocumentationLevel.FullWithReview) reports.Add("PoliceActionReport.FullWithReview");
        if (operation.ExposureOutcomeScore >= 60) reports.Add("ExposureFollowUpReport");
        if (operation.InternalConsequenceScore >= 55) reports.Add("CommandReviewMemo");
        return reports;
    }
}

public static class InternalReviewTriggerResolver
{
    public static List<TriggerType> ResolveTriggers(
        PoliceActionResolution legalResolution,
        bool complaintReceived,
        bool severeInjury,
        bool lethalForceUsed,
        bool evidenceChainBroken,
        int contradictionBetweenOfficers,
        bool seriousActionMissingReport,
        bool corruptionIntelReceived,
        int repeatedBorderlineActions)
    {
        InternalAutoReviewContext ctx = new InternalAutoReviewContext
        {
            lethalForceUsed = lethalForceUsed,
            severeInjury = severeInjury,
            evidenceChainBroken = evidenceChainBroken,
            complaintReceived = complaintReceived,
            hasViolationFlag = legalResolution != null && legalResolution.generatedFlags != null && legalResolution.generatedFlags.Length > 0,
            seriousActionMissingReport = seriousActionMissingReport,
            corruptionIntelReceived = corruptionIntelReceived,
            contradictionBetweenOfficers = contradictionBetweenOfficers,
            repeatedBorderlineActions = repeatedBorderlineActions
        };
        return InternalAutoReviewTriggers.CollectTriggers(ctx);
    }
}

public static class CaseStateResolver
{
    public static WorkflowStageResult Apply(CaseFile caseFile, PoliceActionResolution legalResolution, string actionId, ActionType actionType, string officerId)
    {
        if (caseFile == null || legalResolution == null)
            return new WorkflowStageResult { Success = false, Message = "Missing case or legality resolution." };
        CaseUpdateResolver.ApplyActionResolution(caseFile, legalResolution, actionId, actionType, officerId, "Workflow update");
        return new WorkflowStageResult { Success = true, Message = "Case updated from action + legality outputs." };
    }
}

public static class WorldReactionResolver
{
    public static WorkflowStageResult Apply(PoliceOutcomeBundle operation, CaseFile caseFile)
    {
        if (caseFile == null)
            return new WorkflowStageResult { Success = false, Message = "No case for world reaction." };

        // Placeholder reaction channels until faction/world systems hook in.
        if (operation.ExposureOutcomeScore >= 60)
            caseFile.caseHeat = CaseHeatLevel.High;
        else if (operation.ExposureOutcomeScore >= 35)
            caseFile.caseHeat = CaseHeatLevel.Medium;
        else
            caseFile.caseHeat = CaseHeatLevel.Low;

        if (operation.InternalConsequenceScore >= 60)
            caseFile.corruptionRisk = Mathf.Clamp(caseFile.corruptionRisk + 8, 0, 100);

        return new WorkflowStageResult { Success = true, Message = "World-reaction placeholders applied (heat/corruption channels)." };
    }
}

public static class PoliceWorkflowOrchestrator
{
    /// <summary>
    /// Minimal end-to-end pass that wires all core police systems in order.
    /// </summary>
    public static PoliceWorkflowRunResult Run(
        IntakeRecord intake,
        OfficerProfile officer,
        SuspicionRecord suspicionRecord,
        PoliceActionRequest legalRequest,
        PoliceActionContext actionContext,
        CaseFile existingCase = null)
    {
        PoliceWorkflowRunResult result = new PoliceWorkflowRunResult();
        result.Intake = intake;
        result.AssessmentDecision = InitialAssessmentResolver.Resolve(intake);

        result.Suspicion = SuspicionResolver.Resolve(
            suspicionRecord,
            officer,
            complementaryFriskIndicatorPresent: legalRequest != null && legalRequest.specificIndicatorForSearch,
            additionalEscalationFactorPresent: legalRequest != null && legalRequest.emergencyClaimed);

        ActionType selectedAction = InvestigativeActionResolver.SelectActionFromSuspicion(result.Suspicion);
        if (legalRequest != null)
            legalRequest.actionType = selectedAction;

        result.CaseFile = existingCase;
        if (result.CaseFile == null && result.AssessmentDecision == InitialAssessmentDecision.PreliminaryCaseOpened)
        {
            result.CaseFile = CaseOpenResolver.OpenCase(
                PoliceCaseType.Subject,
                "Workflow case: " + intake?.summary,
                officer?.OfficerId,
                suspicionRecord?.stationId,
                officer?.OfficerId,
                officer?.OfficerId,
                LegalGroundType.ReasonableSuspicion,
                "Opened from workflow assessment.");
        }

        result.LegalResolution = AuthorizationResolver.Resolve(legalRequest);

        PoliceActionType operationType = MapToOperationAction(selectedAction);
        result.OperationalResolution = PoliceOperationResolver.Resolve(
            officer,
            target: null,
            context: actionContext,
            legalGround: MapLegalGround(legalRequest),
            authorizationLevel: MapAuthorizationLevel(result.LegalResolution),
            documentationQuality: MapDocumentationQuality(result.LegalResolution),
            actionType: operationType);

        if (result.CaseFile != null)
        {
            string actionId = "act_" + Guid.NewGuid().ToString("N");
            result.CaseUpdate = CaseStateResolver.Apply(result.CaseFile, result.LegalResolution, actionId, selectedAction, officer?.OfficerId);
            result.WorldReaction = WorldReactionResolver.Apply(result.OperationalResolution, result.CaseFile);
            CaseDecayResolver.ApplyDecay(result.CaseFile, idleTurns: 0);
            result.DecayAndEscalation = new WorkflowStageResult { Success = true, Message = "Decay checked, escalation state refreshed." };
        }
        else
        {
            result.CaseUpdate = new WorkflowStageResult { Success = false, Message = "No case allocated in this run." };
            result.WorldReaction = new WorkflowStageResult { Success = false, Message = "No case for world reaction pass." };
            result.DecayAndEscalation = new WorkflowStageResult { Success = false, Message = "No case for decay/escalation pass." };
        }

        return result;
    }

    private static PoliceActionType MapToOperationAction(ActionType a)
    {
        return a switch
        {
            ActionType.Approach => PoliceActionType.StreetStop,
            ActionType.Question => PoliceActionType.InterviewWitness,
            ActionType.RequestIdentification => PoliceActionType.StreetStop,
            ActionType.Detain => PoliceActionType.Detain,
            ActionType.FriskSearch => PoliceActionType.PatDownSearch,
            ActionType.VehicleSearch => PoliceActionType.VehicleSearch,
            ActionType.PropertySearch => PoliceActionType.StructureSearch,
            ActionType.Arrest => PoliceActionType.Arrest,
            ActionType.ShortSurveillance => PoliceActionType.SurveillanceTail,
            ActionType.ExtendedSurveillance => PoliceActionType.SurveillanceTail,
            ActionType.Interrogate => PoliceActionType.InterrogateSuspect,
            ActionType.Raid => PoliceActionType.Raid,
            _ => PoliceActionType.DetectSuspicion
        };
    }

    private static LegalGroundState MapLegalGround(PoliceActionRequest req)
    {
        if (req == null || req.legalGrounds == null || req.legalGrounds.Length == 0)
            return LegalGroundState.None;
        int strength = 0;
        for (int i = 0; i < req.legalGrounds.Length; i++)
        {
            int s = req.legalGrounds[i] switch
            {
                LegalGroundType.Complaint => 1,
                LegalGroundType.ReasonableSuspicion => 2,
                LegalGroundType.IntelligenceLead => 3,
                LegalGroundType.CaughtInTheAct => 4,
                LegalGroundType.Warrant => 5,
                LegalGroundType.Emergency => 5,
                _ => 0
            };
            if (s > strength) strength = s;
        }
        if (strength >= 4) return LegalGroundState.Established;
        if (strength >= 2) return LegalGroundState.Borderline;
        return LegalGroundState.None;
    }

    private static PoliceAuthorizationLevel MapAuthorizationLevel(PoliceActionResolution legal)
    {
        if (legal == null) return PoliceAuthorizationLevel.OfficerSolo;
        return legal.requiredApprovalLevel switch
        {
            ApprovalLevel.None => PoliceAuthorizationLevel.OfficerSolo,
            ApprovalLevel.Sergeant => PoliceAuthorizationLevel.Supervisor,
            ApprovalLevel.Lieutenant => PoliceAuthorizationLevel.Supervisor,
            ApprovalLevel.Captain => PoliceAuthorizationLevel.Supervisor,
            ApprovalLevel.WarrantOnly => PoliceAuthorizationLevel.Warrant,
            _ => PoliceAuthorizationLevel.OfficerSolo
        };
    }

    private static PoliceDocumentationQuality MapDocumentationQuality(PoliceActionResolution legal)
    {
        if (legal == null) return PoliceDocumentationQuality.Missing;
        return legal.requiredDocumentationLevel switch
        {
            DocumentationLevel.None => PoliceDocumentationQuality.Partial,
            DocumentationLevel.Basic => PoliceDocumentationQuality.Partial,
            DocumentationLevel.Full => PoliceDocumentationQuality.Complete,
            DocumentationLevel.FullWithReview => PoliceDocumentationQuality.Complete,
            _ => PoliceDocumentationQuality.Partial
        };
    }
}
