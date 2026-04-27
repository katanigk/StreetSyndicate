using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PoliceActionRequest
{
    public ActionType actionType;
    public string officerId;
    public string targetType;
    public string targetId;
    public string locationId;
    public LegalGroundType[] legalGrounds = Array.Empty<LegalGroundType>();
    public string warrantId;
    public string authorizerId;
    public bool emergencyClaimed;
    public long startTime;
    public string[] contextFlags = Array.Empty<string>();

    // Runtime/action detail payload for legality checks.
    public bool forceUsed;
    public bool lethalForceUsed;
    public bool severeInjuryCaused;
    public bool prolongedDetention;
    public bool specificIndicatorForSearch;
    public bool lawfulContextForSeizure;
    public bool activeResistance;
    public bool imminentLethalThreat;
    public bool authorizationExpired;
    public bool reportMissing;
    public bool reportContradictionsFound;
    public bool evidenceChainBroken;
    public bool civilianComplaintLodged;
    public bool officerDisputeLodged;
    public bool emergencyContextEvidencePresent;
    public bool specialOperationAuthorizationPresent;

    // Scope controls.
    public bool exceededTargetScope;
    public bool exceededLocationScope;
    public bool exceededTimeScope;
    public bool exceededEvidenceScope;
}

[Serializable]
public class PoliceActionResolution
{
    public bool isActionAllowed;
    public LegalityLevel legalityLevel;
    public ApprovalLevel requiredApprovalLevel;
    public DocumentationLevel requiredDocumentationLevel;
    public float riskOfViolation;
    public ViolationFlag[] generatedFlags = Array.Empty<ViolationFlag>();
    public string[] generatedReports = Array.Empty<string>();
    public string[] generatedEvidenceLinks = Array.Empty<string>();
    public string[] generatedCaseLinks = Array.Empty<string>();
    public MisconductSeverity misconductSeverity;
    public string legalitySummary;
}

public enum ActionType
{
    Approach,
    RequestIdentification,
    Question,
    Detain,
    FriskSearch,
    VehicleSearch,
    PropertySearch,
    SeizeEvidence,
    SeizeProperty,
    Arrest,
    ShortSurveillance,
    ExtendedSurveillance,
    Wiretap,
    IntelligenceInfiltration,
    UseSoftControl,
    UseHardControl,
    UseLethalForce,
    TransportDetainee,
    OpenCase,
    Interrogate,
    Raid,
    CheckpointStop,
    PerimeterContainment
}

public enum LegalGroundType
{
    ReasonableSuspicion,
    IntelligenceLead,
    CaughtInTheAct,
    Complaint,
    Warrant,
    Emergency
}

public enum LegalityLevel
{
    Lawful,
    Borderline,
    Unlawful
}

public enum ApprovalLevel
{
    None,
    Sergeant,
    Lieutenant,
    Captain,
    WarrantOnly
}

public enum DocumentationLevel
{
    None,
    Basic,
    Full,
    FullWithReview
}

public enum ViolationFlag
{
    NoLegalGround,
    WeakGroundForAction,
    MissingApproval,
    MissingWarrant,
    EmergencyNotJustified,
    ExcessiveScope,
    ExcessiveForce,
    FalseReportRisk,
    ImproperDetentionLength,
    ImproperSearch,
    ImproperSeizure,
    ImproperArrest,
    ImproperSurveillance,
    ImproperWiretap,
    EvidenceChainRisk
}

public enum MisconductSeverity
{
    None,
    Minor,
    Moderate,
    Severe
}

public static class PoliceLegalCodexRules
{
    public static PoliceActionResolution ResolvePoliceActionLegality(PoliceActionRequest request)
    {
        if (request == null)
            return new PoliceActionResolution { isActionAllowed = false, legalityLevel = LegalityLevel.Unlawful, legalitySummary = "Missing request." };

        HashSet<ViolationFlag> flags = new HashSet<ViolationFlag>();
        int groundStrength = ComputeGroundStrength(request);
        int minStrength = DetermineMinimumGroundStrength(request);
        ApprovalLevel requiredApproval = DetermineRequiredApproval(request);
        DocumentationLevel requiredDoc = DetermineRequiredDocumentation(request);

        ValidateGrounds(request, groundStrength, minStrength, flags);
        CheckApproval(request, requiredApproval, flags);
        CheckScopeLimits(request, flags);
        CheckForceLegality(request, flags);
        CheckActionSpecificViolations(request, flags);
        EmitInternalReviewTriggers(request, flags);

        ViolationFlag[] generatedFlags = ToSortedArray(flags);
        float risk = ComputeViolationRisk(generatedFlags);
        LegalityLevel legalityLevel = ResolveLegalityLevel(generatedFlags);
        bool allowed = legalityLevel != LegalityLevel.Unlawful;

        List<string> reports = BuildGeneratedReports(request, requiredDoc, generatedFlags);
        List<string> evidenceLinks = BuildGeneratedEvidenceLinks(request, generatedFlags);
        List<string> caseLinks = BuildGeneratedCaseLinks(request, generatedFlags);

        return new PoliceActionResolution
        {
            isActionAllowed = allowed,
            legalityLevel = legalityLevel,
            requiredApprovalLevel = requiredApproval,
            requiredDocumentationLevel = requiredDoc,
            riskOfViolation = risk,
            generatedFlags = generatedFlags,
            generatedReports = reports.ToArray(),
            generatedEvidenceLinks = evidenceLinks.ToArray(),
            generatedCaseLinks = caseLinks.ToArray(),
            misconductSeverity = ResolveMisconductSeverity(generatedFlags),
            legalitySummary = BuildLegalitySummary(allowed, legalityLevel, risk, generatedFlags)
        };
    }

    private static void ValidateGrounds(PoliceActionRequest request, int groundStrength, int minStrength, HashSet<ViolationFlag> flags)
    {
        if (request.legalGrounds == null || request.legalGrounds.Length == 0)
            flags.Add(ViolationFlag.NoLegalGround);

        if (groundStrength < minStrength)
            flags.Add(ViolationFlag.WeakGroundForAction);

        bool requiresWarrant = request.actionType == ActionType.PropertySearch ||
                               request.actionType == ActionType.Wiretap ||
                               (request.actionType == ActionType.Raid && !request.emergencyClaimed);

        bool hasWarrantGround = HasGround(request, LegalGroundType.Warrant) || !string.IsNullOrWhiteSpace(request.warrantId);
        if (requiresWarrant && !hasWarrantGround)
            flags.Add(ViolationFlag.MissingWarrant);

        if (request.emergencyClaimed && !request.emergencyContextEvidencePresent)
            flags.Add(ViolationFlag.EmergencyNotJustified);
    }

    private static int ComputeGroundStrength(PoliceActionRequest request)
    {
        if (request == null || request.legalGrounds == null || request.legalGrounds.Length == 0)
            return 0;

        int best = 0;
        for (int i = 0; i < request.legalGrounds.Length; i++)
        {
            LegalGroundType g = request.legalGrounds[i];
            int v = g switch
            {
                LegalGroundType.Complaint => 1,
                LegalGroundType.ReasonableSuspicion => 2,
                LegalGroundType.IntelligenceLead => 3,
                LegalGroundType.CaughtInTheAct => 4,
                LegalGroundType.Warrant => 5,
                LegalGroundType.Emergency => 5,
                _ => 0
            };
            if (v > best)
                best = v;
        }
        return best;
    }

    private static int DetermineMinimumGroundStrength(PoliceActionRequest request)
    {
        return request.actionType switch
        {
            ActionType.Approach => 1,
            ActionType.RequestIdentification => 2,
            ActionType.Question => 1,
            ActionType.Detain => 3,
            ActionType.FriskSearch => request.specificIndicatorForSearch ? 3 : 4,
            ActionType.VehicleSearch => 3,
            ActionType.PropertySearch => 5,
            ActionType.SeizeEvidence => 2,
            ActionType.SeizeProperty => 4,
            ActionType.Arrest => 4,
            ActionType.ShortSurveillance => 2,
            ActionType.ExtendedSurveillance => 3,
            ActionType.Wiretap => 5,
            ActionType.IntelligenceInfiltration => 3,
            ActionType.UseSoftControl => 2,
            ActionType.UseHardControl => 3,
            ActionType.UseLethalForce => 5,
            ActionType.TransportDetainee => 3,
            ActionType.OpenCase => 1,
            ActionType.Interrogate => 3,
            ActionType.Raid => 5,
            ActionType.CheckpointStop => 3,
            ActionType.PerimeterContainment => 3,
            _ => 1
        };
    }

    private static ApprovalLevel DetermineRequiredApproval(PoliceActionRequest request)
    {
        return request.actionType switch
        {
            ActionType.ExtendedSurveillance => ApprovalLevel.Lieutenant,
            ActionType.Wiretap => ApprovalLevel.WarrantOnly,
            ActionType.IntelligenceInfiltration => ApprovalLevel.Captain,
            ActionType.Raid => request.emergencyClaimed ? ApprovalLevel.Captain : ApprovalLevel.WarrantOnly,
            ActionType.CheckpointStop => ApprovalLevel.Captain,
            ActionType.PropertySearch => request.emergencyClaimed ? ApprovalLevel.Lieutenant : ApprovalLevel.WarrantOnly,
            ActionType.SeizeProperty => ApprovalLevel.Lieutenant,
            ActionType.PerimeterContainment => ApprovalLevel.Sergeant,
            ActionType.Detain => request.prolongedDetention ? ApprovalLevel.Sergeant : ApprovalLevel.None,
            _ => ApprovalLevel.None
        };
    }

    private static DocumentationLevel DetermineRequiredDocumentation(PoliceActionRequest request)
    {
        switch (request.actionType)
        {
            case ActionType.Approach:
                return request.forceUsed ? DocumentationLevel.Basic : DocumentationLevel.None;
            case ActionType.RequestIdentification:
            case ActionType.Question:
            case ActionType.ShortSurveillance:
                return DocumentationLevel.Basic;
            case ActionType.Wiretap:
            case ActionType.IntelligenceInfiltration:
            case ActionType.Raid:
            case ActionType.UseHardControl:
            case ActionType.UseLethalForce:
            case ActionType.PropertySearch:
                return DocumentationLevel.FullWithReview;
            default:
                return DocumentationLevel.Full;
        }
    }

    private static void CheckApproval(PoliceActionRequest request, ApprovalLevel requiredApproval, HashSet<ViolationFlag> flags)
    {
        if (requiredApproval == ApprovalLevel.None)
            return;

        if (requiredApproval == ApprovalLevel.WarrantOnly)
        {
            if (string.IsNullOrWhiteSpace(request.warrantId))
                flags.Add(ViolationFlag.MissingWarrant);
            return;
        }

        if (string.IsNullOrWhiteSpace(request.authorizerId))
            flags.Add(ViolationFlag.MissingApproval);
    }

    private static void CheckScopeLimits(PoliceActionRequest request, HashSet<ViolationFlag> flags)
    {
        if (request.exceededTargetScope || request.exceededLocationScope || request.exceededTimeScope || request.exceededEvidenceScope)
            flags.Add(ViolationFlag.ExcessiveScope);

        if (request.exceededEvidenceScope && (request.actionType == ActionType.SeizeEvidence || request.actionType == ActionType.SeizeProperty))
            flags.Add(ViolationFlag.ImproperSeizure);

        if (request.authorizationExpired && (request.actionType == ActionType.ShortSurveillance || request.actionType == ActionType.ExtendedSurveillance))
            flags.Add(ViolationFlag.ImproperSurveillance);
    }

    private static void CheckForceLegality(PoliceActionRequest request, HashSet<ViolationFlag> flags)
    {
        if (!request.forceUsed)
            return;

        if (request.actionType == ActionType.UseSoftControl && !request.activeResistance)
            flags.Add(ViolationFlag.ExcessiveForce);
        if (request.actionType == ActionType.UseHardControl && !request.activeResistance)
            flags.Add(ViolationFlag.ExcessiveForce);
        if (request.actionType == ActionType.UseLethalForce && !request.imminentLethalThreat)
            flags.Add(ViolationFlag.ExcessiveForce);
    }

    private static void CheckActionSpecificViolations(PoliceActionRequest request, HashSet<ViolationFlag> flags)
    {
        if ((request.actionType == ActionType.FriskSearch || request.actionType == ActionType.VehicleSearch) && !request.specificIndicatorForSearch)
            flags.Add(ViolationFlag.ImproperSearch);

        if (request.actionType == ActionType.Arrest && ComputeGroundStrength(request) < 4)
            flags.Add(ViolationFlag.ImproperArrest);

        if ((request.actionType == ActionType.ShortSurveillance || request.actionType == ActionType.ExtendedSurveillance) && request.authorizationExpired)
            flags.Add(ViolationFlag.ImproperSurveillance);

        if (request.actionType == ActionType.Wiretap && string.IsNullOrWhiteSpace(request.warrantId))
            flags.Add(ViolationFlag.ImproperWiretap);

        if (request.actionType == ActionType.Detain && request.prolongedDetention && string.IsNullOrWhiteSpace(request.authorizerId))
            flags.Add(ViolationFlag.ImproperDetentionLength);

        if (!request.lawfulContextForSeizure && (request.actionType == ActionType.SeizeEvidence || request.actionType == ActionType.SeizeProperty))
            flags.Add(ViolationFlag.ImproperSeizure);

        if (request.evidenceChainBroken)
            flags.Add(ViolationFlag.EvidenceChainRisk);
        if (request.reportContradictionsFound)
            flags.Add(ViolationFlag.FalseReportRisk);
    }

    private static void EmitInternalReviewTriggers(PoliceActionRequest request, HashSet<ViolationFlag> flags)
    {
        if (request.lethalForceUsed || request.severeInjuryCaused)
            flags.Add(ViolationFlag.ExcessiveForce);
        if (request.reportMissing)
            flags.Add(ViolationFlag.FalseReportRisk);
        if (request.civilianComplaintLodged || request.officerDisputeLodged)
            flags.Add(ViolationFlag.FalseReportRisk);
    }

    private static bool HasGround(PoliceActionRequest request, LegalGroundType ground)
    {
        if (request.legalGrounds == null)
            return false;
        for (int i = 0; i < request.legalGrounds.Length; i++)
        {
            if (request.legalGrounds[i] == ground)
                return true;
        }
        return false;
    }

    private static ViolationFlag[] ToSortedArray(HashSet<ViolationFlag> flags)
    {
        ViolationFlag[] arr = new ViolationFlag[flags.Count];
        flags.CopyTo(arr);
        Array.Sort(arr);
        return arr;
    }

    private static float ComputeViolationRisk(ViolationFlag[] flags)
    {
        if (flags == null || flags.Length == 0)
            return 0f;
        float score = 0f;
        for (int i = 0; i < flags.Length; i++)
        {
            score += flags[i] switch
            {
                ViolationFlag.NoLegalGround => 0.30f,
                ViolationFlag.MissingWarrant => 0.30f,
                ViolationFlag.ExcessiveForce => 0.32f,
                ViolationFlag.ImproperArrest => 0.26f,
                ViolationFlag.ImproperWiretap => 0.30f,
                ViolationFlag.FalseReportRisk => 0.22f,
                _ => 0.12f
            };
        }
        return Mathf.Clamp01(score);
    }

    private static LegalityLevel ResolveLegalityLevel(ViolationFlag[] flags)
    {
        if (flags == null || flags.Length == 0)
            return LegalityLevel.Lawful;

        bool severe = Contains(flags, ViolationFlag.NoLegalGround) ||
                      Contains(flags, ViolationFlag.MissingWarrant) ||
                      Contains(flags, ViolationFlag.ImproperArrest) ||
                      Contains(flags, ViolationFlag.ExcessiveForce) ||
                      Contains(flags, ViolationFlag.ImproperWiretap);
        if (severe)
            return LegalityLevel.Unlawful;
        return LegalityLevel.Borderline;
    }

    private static bool Contains(ViolationFlag[] flags, ViolationFlag needle)
    {
        for (int i = 0; i < flags.Length; i++)
        {
            if (flags[i] == needle)
                return true;
        }
        return false;
    }

    private static MisconductSeverity ResolveMisconductSeverity(ViolationFlag[] flags)
    {
        if (flags == null || flags.Length == 0)
            return MisconductSeverity.None;

        if (Contains(flags, ViolationFlag.NoLegalGround) ||
            Contains(flags, ViolationFlag.MissingWarrant) ||
            Contains(flags, ViolationFlag.ImproperArrest) ||
            Contains(flags, ViolationFlag.ExcessiveForce) ||
            Contains(flags, ViolationFlag.ImproperWiretap) ||
            Contains(flags, ViolationFlag.FalseReportRisk))
            return MisconductSeverity.Severe;

        if (Contains(flags, ViolationFlag.ImproperSearch) ||
            Contains(flags, ViolationFlag.ImproperDetentionLength) ||
            Contains(flags, ViolationFlag.ImproperSurveillance))
            return MisconductSeverity.Moderate;

        return MisconductSeverity.Minor;
    }

    private static List<string> BuildGeneratedReports(PoliceActionRequest request, DocumentationLevel level, ViolationFlag[] flags)
    {
        List<string> reports = new List<string>();
        if (level == DocumentationLevel.None)
            return reports;

        reports.Add(level switch
        {
            DocumentationLevel.Basic => "BasicActionReport",
            DocumentationLevel.Full => "FullActionReport",
            DocumentationLevel.FullWithReview => "FullActionReportWithSupervisorReview",
            _ => "ActionReport"
        });

        if (request.forceUsed)
            reports.Add("ForceUsageReport");
        if (request.lethalForceUsed)
            reports.Add("LethalForceReviewTicket");
        if (request.evidenceChainBroken || Contains(flags, ViolationFlag.EvidenceChainRisk))
            reports.Add("EvidenceChainAnomalyReport");
        return reports;
    }

    private static List<string> BuildGeneratedEvidenceLinks(PoliceActionRequest request, ViolationFlag[] flags)
    {
        List<string> links = new List<string>();
        if (request.actionType == ActionType.SeizeEvidence || request.actionType == ActionType.SeizeProperty)
            links.Add("EvidenceRegistryLink");
        if (Contains(flags, ViolationFlag.EvidenceChainRisk))
            links.Add("EvidenceAdmissibilityRiskMarker");
        return links;
    }

    private static List<string> BuildGeneratedCaseLinks(PoliceActionRequest request, ViolationFlag[] flags)
    {
        List<string> links = new List<string>();
        if (request.actionType == ActionType.OpenCase || request.actionType == ActionType.Interrogate || request.actionType == ActionType.Raid)
            links.Add("CaseTimelineLink");

        if (request.civilianComplaintLodged || request.officerDisputeLodged || Contains(flags, ViolationFlag.FalseReportRisk))
            links.Add("InternalAffairsReviewTicket");

        if (Contains(flags, ViolationFlag.NoLegalGround) || Contains(flags, ViolationFlag.MissingWarrant) || Contains(flags, ViolationFlag.ExcessiveForce))
            links.Add("OfficerCredibilityDeltaNegative");
        return links;
    }

    private static string BuildLegalitySummary(bool allowed, LegalityLevel level, float risk, ViolationFlag[] flags)
    {
        string baseLine = allowed ? "Action passes legal gate" : "Action fails legal gate";
        return baseLine + " | Level: " + level + " | Risk: " + Mathf.RoundToInt(risk * 100f) + "% | Flags: " + (flags?.Length ?? 0);
    }
}
