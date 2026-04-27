using System;
using System.Collections.Generic;
using UnityEngine;

public enum PoliceRecordVisibility
{
    Internal = 0,
    ExternalLeak = 1,
    Public = 2
}

public enum PoliceRecordKind
{
    Decision = 0,
    ActionRequest = 1,
    Approval = 2,
    Execution = 3,
    Outcome = 4,
    CaseUpdate = 5,
    EvidenceAccess = 6,
    PersonnelEvent = 7,
    LogisticsEvent = 8,
    InternalReview = 9
}

[Serializable]
public class PoliceRecordEntry
{
    public string recordId;
    public string correlationId;
    public PoliceRecordKind kind;
    public PoliceRecordVisibility visibility;
    public int dayIndex;
    public long at;
    public string actorId;
    public string subjectId;
    public string subjectType;
    public string summary;
    public string details;
}

public static class PoliceRecordsSystem
{
    public static PoliceRecordEntry AddRecord(
        PoliceRecordKind kind,
        string correlationId,
        int dayIndex,
        string actorId,
        string subjectId,
        string subjectType,
        string summary,
        string details,
        PoliceRecordVisibility visibility = PoliceRecordVisibility.Internal)
    {
        PoliceRecordEntry rec = new PoliceRecordEntry
        {
            recordId = "prec_" + Guid.NewGuid().ToString("N"),
            correlationId = string.IsNullOrWhiteSpace(correlationId) ? ("corr_" + Guid.NewGuid().ToString("N")) : correlationId,
            kind = kind,
            visibility = visibility,
            dayIndex = Mathf.Max(1, dayIndex),
            at = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            actorId = actorId ?? string.Empty,
            subjectId = subjectId ?? string.Empty,
            subjectType = subjectType ?? string.Empty,
            summary = summary ?? string.Empty,
            details = details ?? string.Empty
        };
        PoliceWorldState.Records.Add(rec);
        return rec;
    }
}

[Serializable]
public class PoliceFlowResult
{
    public bool success;
    public string correlationId;
    public string message;
    public string caseId;
    public string shipmentId;
    public string evidenceId;
}

public static class PoliceOperationalFlows
{
    public static PoliceFlowResult OpenInvestigation(
        string actorOfficerId,
        string stationId,
        string targetId,
        string title,
        LegalGroundType openingGround)
    {
        int day = Mathf.Max(1, GameSessionState.CurrentDay);
        string correlationId = "corr_" + Guid.NewGuid().ToString("N");
        PoliceRecordsSystem.AddRecord(
            PoliceRecordKind.ActionRequest, correlationId, day, actorOfficerId, targetId, "Subject",
            "Open investigation requested", title, PoliceRecordVisibility.Internal);

        PoliceActionRequest req = new PoliceActionRequest
        {
            actionType = ActionType.OpenCase,
            officerId = actorOfficerId,
            targetId = targetId,
            targetType = "Subject",
            locationId = stationId,
            legalGrounds = new[] { openingGround },
            startTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        PoliceActionResolution legal = PoliceLegalCodexRules.ResolvePoliceActionLegality(req);
        PoliceRecordsSystem.AddRecord(
            PoliceRecordKind.Approval, correlationId, day, actorOfficerId, targetId, "Subject",
            legal.isActionAllowed ? "Open investigation approved" : "Open investigation denied",
            legal.legalitySummary, PoliceRecordVisibility.Internal);

        if (!legal.isActionAllowed)
        {
            PoliceRecordsSystem.AddRecord(
                PoliceRecordKind.Outcome, correlationId, day, actorOfficerId, targetId, "Subject",
                "Open investigation failed", "Legality gate denied request.", PoliceRecordVisibility.Internal);
            return new PoliceFlowResult { success = false, correlationId = correlationId, message = legal.legalitySummary };
        }

        CaseFile file = CaseOpenResolver.OpenCase(
            PoliceCaseType.Subject,
            string.IsNullOrWhiteSpace(title) ? "Investigation: " + targetId : title,
            actorOfficerId,
            stationId,
            actorOfficerId,
            actorOfficerId,
            openingGround,
            "Opened from operational flow.");
        file.status = PoliceCaseStatus.Active;
        file.linkedEntities.Add(new CaseLinkedEntity
        {
            EntityId = targetId,
            EntityType = SuspicionSubjectType.Person,
            Role = CaseEntityRole.Suspect
        });
        PoliceWorldState.CaseFiles.Add(file);

        PoliceRecordsSystem.AddRecord(
            PoliceRecordKind.Execution, correlationId, day, actorOfficerId, file.caseId, "Case",
            "Open investigation executed", "Case object created and attached to world state.", PoliceRecordVisibility.Internal);
        PoliceRecordsSystem.AddRecord(
            PoliceRecordKind.CaseUpdate, correlationId, day, actorOfficerId, file.caseId, "Case",
            "Case activated", "Case status set to Active.", PoliceRecordVisibility.Internal);

        return new PoliceFlowResult
        {
            success = true,
            correlationId = correlationId,
            caseId = file.caseId,
            message = "Investigation opened."
        };
    }

    public static PoliceFlowResult RequestEquipmentShipment(
        string actorOfficerId,
        string ownerOrganizationId,
        string originLocationId,
        string destinationLocationId,
        string routeId,
        string transportUnitId,
        int quantity,
        LogisticsShipmentPriority priority)
    {
        int day = Mathf.Max(1, GameSessionState.CurrentDay);
        string correlationId = "corr_" + Guid.NewGuid().ToString("N");
        PoliceRecordsSystem.AddRecord(
            PoliceRecordKind.ActionRequest, correlationId, day, actorOfficerId, destinationLocationId, "Location",
            "Equipment shipment requested", "Requesting logistics movement.", PoliceRecordVisibility.Internal);

        LogisticsShipment shipment = PoliceLogisticsSystem.CreatePreparingShipment(
            ownerOrganizationId,
            LogisticsCargoType.Equipment,
            quantity,
            originLocationId,
            destinationLocationId,
            routeId,
            transportUnitId,
            requestedByRole: "StationLogistics",
            approvedByRole: "Commander",
            priority: priority,
            chainOfCustodyRequired: false);
        shipment.cargoItems.Add("police_equipment_pack");

        bool ok = PoliceLogisticsSystem.TryApproveAndDispatch(shipment, day, out string reason);
        PoliceRecordsSystem.AddRecord(
            PoliceRecordKind.Approval, correlationId, day, actorOfficerId, shipment.shipmentId, "Shipment",
            ok ? "Shipment approved" : "Shipment denied",
            string.IsNullOrEmpty(reason) ? "Approval completed." : reason,
            PoliceRecordVisibility.Internal);

        if (!ok)
        {
            PoliceRecordsSystem.AddRecord(
                PoliceRecordKind.Outcome, correlationId, day, actorOfficerId, shipment.shipmentId, "Shipment",
                "Shipment flow failed", reason, PoliceRecordVisibility.Internal);
            return new PoliceFlowResult { success = false, correlationId = correlationId, message = reason };
        }

        PoliceRecordsSystem.AddRecord(
            PoliceRecordKind.LogisticsEvent, correlationId, day, actorOfficerId, shipment.shipmentId, "Shipment",
            "Shipment dispatched", "Status moved to InTransit.", PoliceRecordVisibility.Internal);

        return new PoliceFlowResult
        {
            success = true,
            correlationId = correlationId,
            shipmentId = shipment.shipmentId,
            message = "Equipment shipment dispatched."
        };
    }

    public static PoliceFlowResult ArrestSuspect(
        string actorOfficerId,
        string stationId,
        string targetId,
        string caseId,
        LegalGroundType legalGround)
    {
        int day = Mathf.Max(1, GameSessionState.CurrentDay);
        string correlationId = "corr_" + Guid.NewGuid().ToString("N");
        PoliceRecordsSystem.AddRecord(
            PoliceRecordKind.ActionRequest, correlationId, day, actorOfficerId, targetId, "Subject",
            "Arrest requested", "Arrest flow started.", PoliceRecordVisibility.Internal);

        PoliceActionRequest req = new PoliceActionRequest
        {
            actionType = ActionType.Arrest,
            officerId = actorOfficerId,
            targetType = "Subject",
            targetId = targetId,
            locationId = stationId,
            legalGrounds = new[] { legalGround },
            startTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        PoliceActionResolution legal = PoliceLegalCodexRules.ResolvePoliceActionLegality(req);
        PoliceRecordsSystem.AddRecord(
            PoliceRecordKind.Approval, correlationId, day, actorOfficerId, targetId, "Subject",
            legal.isActionAllowed ? "Arrest approved" : "Arrest denied",
            legal.legalitySummary, PoliceRecordVisibility.Internal);

        if (!legal.isActionAllowed)
        {
            PoliceRecordsSystem.AddRecord(
                PoliceRecordKind.Outcome, correlationId, day, actorOfficerId, targetId, "Subject",
                "Arrest flow failed", "Legality gate denied arrest.", PoliceRecordVisibility.Internal);
            return new PoliceFlowResult { success = false, correlationId = correlationId, message = legal.legalitySummary };
        }

        OfficerProfile officer = PoliceWorldState.GetOfficer(actorOfficerId);
        PoliceOutcomeBundle outcome = PoliceActionResolver.Resolve(
            officer,
            target: null,
            new PoliceActionContext(),
            legalGround: LegalGroundState.Established,
            authorizationLevel: PoliceAuthorizationLevel.Supervisor,
            documentationQuality: PoliceDocumentationQuality.Complete,
            actionType: PoliceActionType.Arrest);

        string outcomeLabel = ResolveOutcomeLabel(outcome.OperationalOutcomeScore, outcome.ExposureOutcomeScore);
        PoliceRecordsSystem.AddRecord(
            PoliceRecordKind.Execution, correlationId, day, actorOfficerId, targetId, "Subject",
            "Arrest executed", "Execution outcome=" + outcomeLabel, PoliceRecordVisibility.Internal);

        CaseFile file = FindCase(caseId);
        if (file == null)
        {
            file = CaseOpenResolver.OpenCase(
                PoliceCaseType.Subject,
                "Arrest case: " + targetId,
                actorOfficerId,
                stationId,
                actorOfficerId,
                actorOfficerId,
                legalGround,
                "Opened by arrest flow.");
            file.status = PoliceCaseStatus.Active;
            PoliceWorldState.CaseFiles.Add(file);
        }

        EvidenceItem ev = EvidenceCreationResolver.Create(
            EvidenceType.Observational,
            "arrest_report",
            actorOfficerId,
            stationId,
            "act_" + correlationId,
            "OfficerReport",
            actorOfficerId,
            "Arrest report for target " + targetId);
        EvidenceLegalityResolver.ApplyLegality(ev, legal);
        EvidenceStrengthResolver.Recompute(ev);
        PoliceWorldState.EvidenceItems.Add(ev);
        PoliceCaseEvidenceResolver.AttachEvidenceToCase(file, ev, contradictionWithExistingEvidence: false);

        PoliceRecordsSystem.AddRecord(
            PoliceRecordKind.CaseUpdate, correlationId, day, actorOfficerId, file.caseId, "Case",
            "Case updated from arrest", "Evidence attached to case under case-managed flow.", PoliceRecordVisibility.Internal);
        PoliceRecordsSystem.AddRecord(
            PoliceRecordKind.EvidenceAccess, correlationId, day, actorOfficerId, ev.evidenceId, "Evidence",
            "Evidence recorded", "Evidence linked to case " + file.caseId, PoliceRecordVisibility.Internal);
        PoliceRecordsSystem.AddRecord(
            PoliceRecordKind.Outcome, correlationId, day, actorOfficerId, targetId, "Subject",
            "Arrest flow completed", "Outcome class=" + outcomeLabel, PoliceRecordVisibility.Internal);

        return new PoliceFlowResult
        {
            success = true,
            correlationId = correlationId,
            caseId = file.caseId,
            evidenceId = ev.evidenceId,
            message = "Arrest processed."
        };
    }

    private static CaseFile FindCase(string caseId)
    {
        if (string.IsNullOrWhiteSpace(caseId))
            return null;
        for (int i = 0; i < PoliceWorldState.CaseFiles.Count; i++)
        {
            CaseFile c = PoliceWorldState.CaseFiles[i];
            if (c != null && string.Equals(c.caseId, caseId, StringComparison.OrdinalIgnoreCase))
                return c;
        }
        return null;
    }

    private static string ResolveOutcomeLabel(int operationalScore, int exposureScore)
    {
        if (operationalScore >= 70 && exposureScore <= 35)
            return "Success";
        if (operationalScore >= 50)
            return "PartialSuccess";
        if (exposureScore >= 60)
            return "LoudFailure";
        return "SilentFailure";
    }
}

public static class PoliceCaseEvidenceResolver
{
    public static void AttachEvidenceToCase(CaseFile caseFile, EvidenceItem item, bool contradictionWithExistingEvidence)
    {
        if (caseFile == null || item == null)
            return;
        EvidenceLinkResolver.LinkToCase(item, caseFile.caseId);
        EvidenceToCaseResolver.ApplyEvidenceToCase(caseFile, item, contradictionWithExistingEvidence);
    }

    public static List<EvidenceItem> GetEvidenceForCase(string caseId)
    {
        List<EvidenceItem> outList = new List<EvidenceItem>();
        if (string.IsNullOrWhiteSpace(caseId))
            return outList;
        for (int i = 0; i < PoliceWorldState.EvidenceItems.Count; i++)
        {
            EvidenceItem ev = PoliceWorldState.EvidenceItems[i];
            if (ev == null || ev.linkedCaseIds == null)
                continue;
            for (int j = 0; j < ev.linkedCaseIds.Count; j++)
            {
                if (string.Equals(ev.linkedCaseIds[j], caseId, StringComparison.OrdinalIgnoreCase))
                {
                    outList.Add(ev);
                    break;
                }
            }
        }
        return outList;
    }
}
