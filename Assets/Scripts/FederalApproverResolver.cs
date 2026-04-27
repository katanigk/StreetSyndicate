using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Picks a real approver from <see cref="BureauWorldState.Roster"/>; otherwise <see cref="FederalApproverLookupStatus.MissingApprover"/>.</summary>
public static class FederalApproverResolver
{
    public static bool TryFindApprover(
        string requestedByAgentId,
        FederalAuthorityResolution res,
        FederalActionType action,
        out string approverId,
        out FederalApproverLookupStatus status)
    {
        approverId = null;
        status = FederalApproverLookupStatus.MissingApprover;

        IReadOnlyList<FederalAgentProfile> roster = BureauWorldState.Roster;
        if (roster == null || roster.Count == 0)
            return false;

        var requester = BureauWorldState.GetAgent(requestedByAgentId);
        int requesterR = requester != null ? (int)requester.rank : 0;

        if (requester != null && requester.rank == FederalBureauRank.DirectorOfCentralUnit
            && (res.requiresDirectorApproval || action == FederalActionType.OpenStrategicCase))
        {
            approverId = requester.agentId;
            status = FederalApproverLookupStatus.Found;
            return true;
        }

        if (res.requiresDirectorApproval || action == FederalActionType.OpenStrategicCase)
        {
            if (TryPickSingleRank(FederalBureauRank.DirectorOfCentralUnit, roster, requesterR, out approverId, requestedByAgentId))
            {
                status = FederalApproverLookupStatus.Found;
                return true;
            }
            return false;
        }

        if (res.requiredPortfolio != FederalDeputyPortfolio.None)
        {
            FederalAgentProfile best = null;
            for (int i = 0; i < roster.Count; i++)
            {
                var a = roster[i];
                if (a == null) continue;
                if (a.rank != FederalBureauRank.DeputyDirector) continue;
                if (a.deputyPortfolio != res.requiredPortfolio) continue;
                if (!a.availableForField) continue;
                if (!OutranksOrPolicy(requester, a, requestedByAgentId))
                    continue;
                if (best == null || (int)a.rank > (int)best.rank)
                    best = a;
            }
            if (best != null)
            {
                approverId = best.agentId;
                status = FederalApproverLookupStatus.Found;
                return true;
            }
            return false;
        }

        // Rank floor from resolver
        int minR = (int)res.requiredRankMin;
        {
            FederalAgentProfile best = null;
            for (int i = 0; i < roster.Count; i++)
            {
                var a = roster[i];
                if (a == null) continue;
                if (!a.availableForField) continue;
                if ((int)a.rank < minR) continue;
                if (!OutranksOrPolicy(requester, a, requestedByAgentId)) continue;
                if (best == null || (int)a.rank < (int)best.rank) // pick lowest rank that still qualifies = direct supervisor line
                    best = a;
            }
            if (best != null)
            {
                approverId = best.agentId;
                status = FederalApproverLookupStatus.Found;
                return true;
            }
        }
        if (res.requiresDeputyApproval && res.requiredPortfolio == FederalDeputyPortfolio.None)
        {
            // Any deputy with relevant portfolio is unknown — do not auto-pick; missing approver
            return false;
        }
        return false;
    }

    static bool OutranksOrPolicy(FederalAgentProfile requester, FederalAgentProfile candidate, string requesterId)
    {
        if (candidate == null) return false;
        if (string.IsNullOrEmpty(requesterId)) return true;
        if (string.Equals(candidate.agentId, requesterId, StringComparison.OrdinalIgnoreCase))
            return false;
        if (requester == null) return true;
        if ((int)candidate.rank > (int)requester.rank) return true;
        if (requester.rank == FederalBureauRank.UnitChief && candidate.rank == FederalBureauRank.UnitChief) return !string.Equals(candidate.agentId, requesterId, StringComparison.Ordinal);
        // Same rank, different person: allow only if not same as requester
        if ((int)candidate.rank == (int)requester.rank)
            return !string.Equals(candidate.agentId, requester.agentId, StringComparison.OrdinalIgnoreCase);
        return (int)candidate.rank > (int)requester.rank;
    }

    static bool TryPickSingleRank(
        FederalBureauRank r,
        IReadOnlyList<FederalAgentProfile> roster,
        int requesterR,
        out string id,
        string requesterId)
    {
        id = null;
        for (int i = 0; i < roster.Count; i++)
        {
            var a = roster[i];
            if (a == null) continue;
            if (a.rank != r) continue;
            if (!a.availableForField) continue;
            if (string.Equals(a.agentId, requesterId, StringComparison.OrdinalIgnoreCase)) continue;
            id = a.agentId;
            return true;
        }
        if (r == FederalBureauRank.DirectorOfCentralUnit)
        {
            for (int i = 0; i < roster.Count; i++)
            {
                var a = roster[i];
                if (a == null) continue;
                if (a.rank != r) continue;
                id = a.agentId;
                return id != null;
            }
        }
        return false;
    }
}
