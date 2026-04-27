using System;
using UnityEngine;

public enum PoliceRank
{
    Constable,
    SeniorConstable,
    Sergeant,
    Lieutenant,
    Captain,
    Commander,
    ChiefCommissioner
}

public enum PoliceCoreRole
{
    PatrolOfficer,
    Detective,
    IntelligenceOfficer,
    EnforcementOfficer,
    EvidenceOfficer,
    InternalOversightOfficer,
    StationCommander,
    CityCommand
}

[Flags]
public enum AuthorizationFlags
{
    None = 0,
    CanPerform = 1 << 0,
    CanAuthorize = 1 << 1,
    RequiresSupervisorApproval = 1 << 2,
    RequiresCaptainApproval = 1 << 3,
    RequiresCommanderApproval = 1 << 4,
    RequiresWarrant = 1 << 5,
    EmergencyBypassUsed = 1 << 6,
    ApprovalMissing = 1 << 7,
    WarrantMissing = 1 << 8
}

[Serializable]
public struct PoliceAuthorityRequest
{
    public ActionType Action;
    public PoliceRank PerformerRank;
    public PoliceCoreRole PerformerRole;
    public PoliceRank? AuthorizerRank;
    public bool HasWarrant;
    public bool EmergencyClaimed;
    public bool EmergencyJustificationPresent;
}

[Serializable]
public struct PoliceAuthorityResolution
{
    public bool CanPerform;
    public bool CanAuthorize;
    public ApprovalLevel RequiredApproval;
    public bool RequiresWarrant;
    public bool EffectiveAuthorized;
    public AuthorizationFlags Flags;
    public string Summary;
}

public static class PoliceAuthorityMatrix
{
    public static PoliceAuthorityResolution Resolve(PoliceAuthorityRequest req)
    {
        ApprovalLevel required = ResolveRequiredApproval(req.Action);
        bool requiresWarrant = ActionRequiresWarrant(req.Action);

        bool performAllowed = CanRolePerform(req.Action, req.PerformerRole) && MeetsMinRankForPerform(req.Action, req.PerformerRank);
        bool canAuthorizeByRank = req.AuthorizerRank.HasValue && MeetsRequiredApprovalRank(required, req.AuthorizerRank.Value);
        bool canAuthorizeByRole = req.AuthorizerRank.HasValue && CanRoleAuthorize(req.Action, req.AuthorizerRank.Value);
        bool canAuthorize = canAuthorizeByRank && canAuthorizeByRole;

        AuthorizationFlags flags = AuthorizationFlags.None;
        if (performAllowed) flags |= AuthorizationFlags.CanPerform;
        if (canAuthorize || required == ApprovalLevel.None) flags |= AuthorizationFlags.CanAuthorize;

        switch (required)
        {
            case ApprovalLevel.Sergeant: flags |= AuthorizationFlags.RequiresSupervisorApproval; break;
            case ApprovalLevel.Lieutenant: flags |= AuthorizationFlags.RequiresSupervisorApproval; break;
            case ApprovalLevel.Captain: flags |= AuthorizationFlags.RequiresCaptainApproval; break;
        }
        if (req.Action == ActionType.Raid || req.Action == ActionType.PerimeterContainment || req.Action == ActionType.CheckpointStop)
            flags |= AuthorizationFlags.RequiresCommanderApproval;
        if (requiresWarrant)
            flags |= AuthorizationFlags.RequiresWarrant;

        bool warrantSatisfied = !requiresWarrant || req.HasWarrant;
        bool approvalSatisfied = required == ApprovalLevel.None || canAuthorize;

        // Emergency bypass can relax some approval checks, never warrant-only endpoints.
        bool emergencyBypassUsed = false;
        if (!approvalSatisfied && req.EmergencyClaimed && req.EmergencyJustificationPresent && !requiresWarrant)
        {
            approvalSatisfied = true;
            emergencyBypassUsed = true;
            flags |= AuthorizationFlags.EmergencyBypassUsed;
        }

        if (!approvalSatisfied)
            flags |= AuthorizationFlags.ApprovalMissing;
        if (!warrantSatisfied)
            flags |= AuthorizationFlags.WarrantMissing;

        bool effectiveAuthorized = performAllowed && approvalSatisfied && warrantSatisfied;

        return new PoliceAuthorityResolution
        {
            CanPerform = performAllowed,
            CanAuthorize = canAuthorize || required == ApprovalLevel.None,
            RequiredApproval = required,
            RequiresWarrant = requiresWarrant,
            EffectiveAuthorized = effectiveAuthorized,
            Flags = flags,
            Summary = BuildSummary(req, performAllowed, required, requiresWarrant, approvalSatisfied, warrantSatisfied, emergencyBypassUsed)
        };
    }

    private static ApprovalLevel ResolveRequiredApproval(ActionType action)
    {
        return action switch
        {
            ActionType.Approach => ApprovalLevel.None,
            ActionType.RequestIdentification => ApprovalLevel.None,
            ActionType.Question => ApprovalLevel.None,
            ActionType.Detain => ApprovalLevel.None,
            ActionType.FriskSearch => ApprovalLevel.None,
            ActionType.VehicleSearch => ApprovalLevel.Sergeant,
            ActionType.PropertySearch => ApprovalLevel.WarrantOnly,
            ActionType.SeizeEvidence => ApprovalLevel.None,
            ActionType.SeizeProperty => ApprovalLevel.Lieutenant,
            ActionType.Arrest => ApprovalLevel.Lieutenant,
            ActionType.ShortSurveillance => ApprovalLevel.None,
            ActionType.ExtendedSurveillance => ApprovalLevel.Lieutenant,
            ActionType.Wiretap => ApprovalLevel.WarrantOnly,
            ActionType.IntelligenceInfiltration => ApprovalLevel.Captain,
            ActionType.UseSoftControl => ApprovalLevel.None,
            ActionType.UseHardControl => ApprovalLevel.Sergeant,
            ActionType.UseLethalForce => ApprovalLevel.Sergeant,
            ActionType.TransportDetainee => ApprovalLevel.None,
            ActionType.OpenCase => ApprovalLevel.None,
            ActionType.Interrogate => ApprovalLevel.Lieutenant,
            ActionType.Raid => ApprovalLevel.Captain,
            ActionType.CheckpointStop => ApprovalLevel.Captain,
            ActionType.PerimeterContainment => ApprovalLevel.Sergeant,
            _ => ApprovalLevel.None
        };
    }

    private static bool ActionRequiresWarrant(ActionType action)
    {
        return action == ActionType.PropertySearch || action == ActionType.Wiretap;
    }

    private static bool MeetsMinRankForPerform(ActionType action, PoliceRank rank)
    {
        PoliceRank min = action switch
        {
            ActionType.Approach => PoliceRank.Constable,
            ActionType.RequestIdentification => PoliceRank.Constable,
            ActionType.Question => PoliceRank.Constable,
            ActionType.Detain => PoliceRank.Constable,
            ActionType.FriskSearch => PoliceRank.Constable,
            ActionType.VehicleSearch => PoliceRank.Constable,
            ActionType.PropertySearch => PoliceRank.Sergeant,
            ActionType.SeizeEvidence => PoliceRank.Constable,
            ActionType.SeizeProperty => PoliceRank.Sergeant,
            ActionType.Arrest => PoliceRank.Constable,
            ActionType.ShortSurveillance => PoliceRank.Constable,
            ActionType.ExtendedSurveillance => PoliceRank.Constable,
            ActionType.Wiretap => PoliceRank.Lieutenant,
            ActionType.IntelligenceInfiltration => PoliceRank.Captain,
            ActionType.UseSoftControl => PoliceRank.Constable,
            ActionType.UseHardControl => PoliceRank.Constable,
            ActionType.UseLethalForce => PoliceRank.Constable,
            ActionType.TransportDetainee => PoliceRank.Constable,
            ActionType.OpenCase => PoliceRank.Sergeant,
            ActionType.Interrogate => PoliceRank.Sergeant,
            ActionType.Raid => PoliceRank.Sergeant,
            ActionType.CheckpointStop => PoliceRank.Sergeant,
            ActionType.PerimeterContainment => PoliceRank.Sergeant,
            _ => PoliceRank.Constable
        };
        return rank >= min;
    }

    private static bool CanRolePerform(ActionType action, PoliceCoreRole role)
    {
        switch (action)
        {
            case ActionType.Interrogate:
                return role == PoliceCoreRole.Detective || role == PoliceCoreRole.IntelligenceOfficer || role == PoliceCoreRole.EnforcementOfficer;
            case ActionType.Wiretap:
                return role == PoliceCoreRole.IntelligenceOfficer || role == PoliceCoreRole.CityCommand;
            case ActionType.IntelligenceInfiltration:
                return role == PoliceCoreRole.IntelligenceOfficer || role == PoliceCoreRole.CityCommand;
            case ActionType.OpenCase:
                return role == PoliceCoreRole.Detective || role == PoliceCoreRole.InternalOversightOfficer || role == PoliceCoreRole.StationCommander || role == PoliceCoreRole.CityCommand;
            case ActionType.Raid:
                return role == PoliceCoreRole.EnforcementOfficer || role == PoliceCoreRole.StationCommander || role == PoliceCoreRole.CityCommand;
            default:
                return true;
        }
    }

    private static bool CanRoleAuthorize(ActionType action, PoliceRank authorizerRank)
    {
        if (authorizerRank >= PoliceRank.Commander)
            return true;
        ApprovalLevel required = ResolveRequiredApproval(action);
        return MeetsRequiredApprovalRank(required, authorizerRank);
    }

    private static bool MeetsRequiredApprovalRank(ApprovalLevel required, PoliceRank rank)
    {
        return required switch
        {
            ApprovalLevel.None => true,
            ApprovalLevel.Sergeant => rank >= PoliceRank.Sergeant,
            ApprovalLevel.Lieutenant => rank >= PoliceRank.Lieutenant,
            ApprovalLevel.Captain => rank >= PoliceRank.Captain,
            ApprovalLevel.WarrantOnly => false,
            _ => false
        };
    }

    private static string BuildSummary(
        PoliceAuthorityRequest req,
        bool performAllowed,
        ApprovalLevel required,
        bool requiresWarrant,
        bool approvalSatisfied,
        bool warrantSatisfied,
        bool emergencyBypassUsed)
    {
        return "Action=" + req.Action +
               " | PerformAllowed=" + performAllowed +
               " | RequiredApproval=" + required +
               " | RequiresWarrant=" + requiresWarrant +
               " | ApprovalSatisfied=" + approvalSatisfied +
               " | WarrantSatisfied=" + warrantSatisfied +
               " | EmergencyBypassUsed=" + emergencyBypassUsed;
    }
}
