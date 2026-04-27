using System;
using System.Collections.Generic;
using UnityEngine;

public static class FederalPolicyResolver
{
    public const string DefaultDirectorId = "fbu_dir_01";

    public static void EnsureSeeded(int cityMapSeed = 0)
    {
        if (BureauWorldState.activeFederalPolicies == null) return;
        if (BureauWorldState.activeFederalPolicies.Count == 0)
            SeedDefaultDirectorPolicies(cityMapSeed);
        FederalLeadershipAuthorityResolver.EnsureAuthorityProfiles();
        // Keep validation cheap in editor/development
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        FederalPolicyValidation.LintToConsole();
#endif
    }

    static void SeedDefaultDirectorPolicies(int cityMapSeed)
    {
        if (!BureauWorldState.IsBootstrapped || string.IsNullOrEmpty(DefaultDirectorId))
            return;
        int idSalt = (cityMapSeed + 0x1FED) & 0x7FFFFFFF;
        void add(string idSuffix, int domain, int stance, int strictness, int pr, bool declared = true)
        {
            BureauWorldState.activeFederalPolicies.Add(new FederalPolicyProfile
            {
                policyId = "fp_bureau_" + idSuffix + "_" + (idSalt % 10000).ToString("D4"),
                ownerAgentId = DefaultDirectorId,
                ownerRank = (int)FederalBureauRank.DirectorOfCentralUnit,
                ownerScopeInt = (int)FederalPolicyOwnerScope.BureauWide,
                domainInt = domain,
                stanceInt = stance,
                strictnessInt = strictness,
                priority = pr,
                active = true,
                publiclyDeclared = declared,
                visibilityInt = (int)FederalPolicyVisibility.Rumored,
                deputyPortfolioInt = 0,
                divisionId = string.Empty
            });
        }
        // Reasonable "central unit" default line: OC priority, force restraint, public caution.
        add("oc", (int)FederalPolicyDomain.OrganizedCrime, (int)FederalPolicyStance.Prioritize, (int)FederalPolicyStrictness.Preferred, 90, true);
        add("uof", (int)FederalPolicyDomain.UseOfForce, (int)FederalPolicyStance.Restrict, (int)FederalPolicyStrictness.Mandatory, 80, true);
        add("leth", (int)FederalPolicyDomain.LethalForce, (int)FederalPolicyStance.Avoid, (int)FederalPolicyStrictness.Mandatory, 100, true);
        add("pub", (int)FederalPolicyDomain.PublicExposure, (int)FederalPolicyStance.Avoid, (int)FederalPolicyStrictness.Mandatory, 70, true);
        add("pol", (int)FederalPolicyDomain.PoliceRelations, (int)FederalPolicyStance.Neutral, (int)FederalPolicyStrictness.Advisory, 40, false);
        add("und", (int)FederalPolicyDomain.UndercoverOps, (int)FederalPolicyStance.Prioritize, (int)FederalPolicyStrictness.Preferred, 75, false);
        add("raid", (int)FederalPolicyDomain.Raids, (int)FederalPolicyStance.Restrict, (int)FederalPolicyStrictness.Preferred, 55, false);
        add("tco", (int)FederalPolicyDomain.CaseTakeover, (int)FederalPolicyStance.Prefer, (int)FederalPolicyStrictness.Advisory, 50, true);
        add("blk", (int)FederalPolicyDomain.BlackCashTolerance, (int)FederalPolicyStance.Avoid, (int)FederalPolicyStrictness.Advisory, 30, false);
    }

    public static List<FederalPolicyProfile> GetActiveForDomain(int domainInt)
    {
        var outList = new List<FederalPolicyProfile>();
        for (int i = 0; i < BureauWorldState.activeFederalPolicies.Count; i++)
        {
            var p = BureauWorldState.activeFederalPolicies[i];
            if (p == null || !p.active) continue;
            if (p.domainInt == domainInt) outList.Add(p);
        }
        return outList;
    }
}

public static class FederalLeadershipAuthorityResolver
{
    public static void EnsureAuthorityProfiles()
    {
        if (BureauWorldState.leadershipAuthorityProfiles == null) return;
        for (int i = 0; i < BureauWorldState.Roster.Count; i++)
        {
            var a = BureauWorldState.Roster[i];
            if (a == null || string.IsNullOrEmpty(a.agentId)) continue;
            if (FindByAgent(a.agentId) != null) continue;
            int h = unchecked(a.agentId.GetHashCode() + BureauWorldState.BootstrapSeed * 0x1B873593);
            var la = new FederalLeadershipAuthorityProfile
            {
                agentId = a.agentId,
                respectFromSubordinates = 40 + (h & 15),
                fearFromSubordinates = 20 + ((h >> 3) & 20),
                legitimacy = 50 + ((h >> 5) & 20),
                policyClarity = 40 + ((h >> 7) & 20),
                enforcementConsistency = 35 + ((h >> 9) & 25),
                punishmentCredibility = 30 + ((h >> 11) & 25),
                politicalProtection = (a.rank >= FederalBureauRank.DeputyDirector) ? 60 + (h & 20) : 20 + (h & 20)
            };
            BureauWorldState.leadershipAuthorityProfiles.Add(la);
        }
        if (BureauWorldState.leadershipAuthorityProfiles.Count == 0 && !string.IsNullOrEmpty(FederalPolicyResolver.DefaultDirectorId))
        {
            BureauWorldState.leadershipAuthorityProfiles.Add(new FederalLeadershipAuthorityProfile
            {
                agentId = FederalPolicyResolver.DefaultDirectorId,
                respectFromSubordinates = 55,
                fearFromSubordinates = 45,
                legitimacy = 50,
                policyClarity = 50,
                enforcementConsistency = 50,
                punishmentCredibility = 55,
                politicalProtection = 70
            });
        }
        while (BureauWorldState.leadershipAuthorityProfiles.Count > 200)
            BureauWorldState.leadershipAuthorityProfiles.RemoveAt(0);
    }

    static FederalLeadershipAuthorityProfile FindByAgent(string id)
    {
        for (int i = 0; i < BureauWorldState.leadershipAuthorityProfiles.Count; i++)
        {
            if (BureauWorldState.leadershipAuthorityProfiles[i] != null
                && string.Equals(BureauWorldState.leadershipAuthorityProfiles[i].agentId, id, StringComparison.Ordinal))
                return BureauWorldState.leadershipAuthorityProfiles[i];
        }
        return null;
    }
}

public static class FederalPolicyInfluenceResolver
{
    static int StrictnessToWeight(int s)
    {
        if (s == (int)FederalPolicyStrictness.Advisory) return 15;
        if (s == (int)FederalPolicyStrictness.Preferred) return 35;
        if (s == (int)FederalPolicyStrictness.Mandatory) return 55;
        if (s == (int)FederalPolicyStrictness.Forbidden) return 70;
        return 25;
    }

    public static int ComputeAggregate(int day, int citySeed, out int fieldResistance0to100)
    {
        int sum = 0, count = 0, resist = 0;
        for (int i = 0; i < BureauWorldState.activeFederalPolicies.Count; i++)
        {
            var p = BureauWorldState.activeFederalPolicies[i];
            if (p == null || !p.active) continue;
            if (string.IsNullOrEmpty(p.ownerAgentId)) continue;
            var la = GetAuthority(p.ownerAgentId);
            int ownerAuth = la != null
                ? (la.respectFromSubordinates + la.fearFromSubordinates + la.legitimacy + la.policyClarity) / 4
                : 40;
            int sW = StrictnessToWeight(p.strictnessInt);
            int cW = la != null ? (la.enforcementConsistency + la.punishmentCredibility) / 2 : 35;
            int line = ownerAuth * 2 / 3 + sW / 2 + cW / 3;
            if (BureauWorldState.currentHeat > 60) line -= 4;
            if (BureauWorldState.politicalPressure > 70) line -= 3;
            if (BureauWorldState.Budget != null && BureauWorldState.Budget.federalBlackCashMinor > 50_000) resist += 6;
            line = Mathf.Clamp(line, 0, 100);
            sum += line;
            count++;
        }
        if (count == 0) { fieldResistance0to100 = 0; return 0; }
        int avg = sum / count;
        // Culture resistance: low legitimacy across leaders lowers pull-through
        resist += Mathf.Min(30, (100 - (avg / 2)) / 3);
        fieldResistance0to100 = Mathf.Clamp(resist, 0, 100);
        return Mathf.Clamp(avg - fieldResistance0to100 / 4, 0, 100);
    }

    public static int ComputeAggregateForDay(int day, int citySeed) => ComputeAggregate(day, citySeed, out _);

    static FederalLeadershipAuthorityProfile GetAuthority(string id)
    {
        for (int i = 0; i < BureauWorldState.leadershipAuthorityProfiles.Count; i++)
        {
            if (BureauWorldState.leadershipAuthorityProfiles[i] != null
                && string.Equals(BureauWorldState.leadershipAuthorityProfiles[i].agentId, id, StringComparison.Ordinal))
                return BureauWorldState.leadershipAuthorityProfiles[i];
        }
        return null;
    }
}

public static class FederalStrategyBiasResolver
{
    static int HashMix(int a, int b) { unchecked { return a * 0x45D9F3B ^ b; } }

    public static FederalDailyStrategy BiasStrategy(
        FederalDailyStrategy current,
        FederalDirectorPolicyMode mode,
        int influence0to100,
        int day,
        int citySeed,
        int interest0to100,
        int publicExposure0to100)
    {
        var s = current;
        if (influence0to100 < 15) return s;
        int h = Mathf.Abs(HashMix(day, citySeed + (int)mode * 0x1D) + interest0to100 * 7) % 1000;

        switch (mode)
        {
            case FederalDirectorPolicyMode.AggressiveEnforcement:
                if (h % 2 == 0) { if (s == FederalDailyStrategy.Observe) s = FederalDailyStrategy.Pressure; }
                if (h % 3 == 0 && interest0to100 > 50) s = FederalDailyStrategy.Strike;
                break;
            case FederalDirectorPolicyMode.SilentInfiltration:
                if (h % 2 == 0 && interest0to100 > 40) s = FederalDailyStrategy.Infiltrate;
                if (s == FederalDailyStrategy.Strike) s = FederalDailyStrategy.BuildCase;
                break;
            case FederalDirectorPolicyMode.PublicLegitimacy:
                if (h % 2 == 0) s = FederalDailyStrategy.BuildCase;
                if (s == FederalDailyStrategy.Strike) s = FederalDailyStrategy.Pressure;
                if (publicExposure0to100 > 55) s = FederalDailyStrategy.Observe;
                break;
            case FederalDirectorPolicyMode.DamageControl:
                s = (h & 1) == 0 ? FederalDailyStrategy.LayLow : FederalDailyStrategy.CoverUp;
                break;
            case FederalDirectorPolicyMode.PoliticalSurvival:
                if (h % 2 == 0) s = FederalDailyStrategy.BuildCase;
                if (interest0to100 > 75) s = FederalDailyStrategy.Pressure;
                break;
            case FederalDirectorPolicyMode.StrategicDecapitation:
                if (interest0to100 > 55) s = h % 2 == 0 ? FederalDailyStrategy.Strike : FederalDailyStrategy.Infiltrate;
                break;
            case FederalDirectorPolicyMode.PoliceOverride:
                if (interest0to100 > 45) s = FederalDailyStrategy.TakeOverPoliceCase;
                break;
        }
        if (influence0to100 < 35) return s;
        if (h % 5 == 0 && s == FederalDailyStrategy.Observe && interest0to100 > 35) s = FederalDailyStrategy.Pressure;
        return s;
    }
}

public static class FederalPolicyConflictResolver
{
    public static void RunDaily(int day, int citySeed)
    {
        if (BureauWorldState.activeFederalPolicies.Count < 2) return;
        int cap = 4;
        for (int a = 0; a < BureauWorldState.activeFederalPolicies.Count && cap > 0; a++)
        {
            var pa = BureauWorldState.activeFederalPolicies[a];
            if (pa == null || !pa.active) continue;
            for (int b = a + 1; b < BureauWorldState.activeFederalPolicies.Count && cap > 0; b++)
            {
                var pb = BureauWorldState.activeFederalPolicies[b];
                if (pb == null || !pb.active) continue;
                if (pa.domainInt != pb.domainInt) continue;
                if (pa.stanceInt == pb.stanceInt) continue;
                if (string.Equals(pa.ownerAgentId, pb.ownerAgentId, StringComparison.Ordinal)
                    && Mathf.Abs(pa.stanceInt - pb.stanceInt) < 2) continue;
                if (RecordExistsForPair(pa.policyId, pb.policyId)) continue;
                int sev = Mathf.Min(5, 1 + Mathf.Abs(pa.stanceInt - pb.stanceInt) / 2);
                string high = (int)pa.ownerRank >= (int)pb.ownerRank ? pa.policyId : pb.policyId;
                var r = new FederalPolicyConflictRecord
                {
                    conflictId = "fp_cnf_" + day + "_" + a + "_" + b,
                    involvedPolicyIds = new List<string> { pa.policyId, pb.policyId }
                };
                r.highestAuthorityPolicyId = high;
                r.fieldDominantPolicyId = (pa.strictnessInt >= pb.strictnessInt) ? pa.policyId : pb.policyId;
                r.conflictSeverity = sev;
                r.resolvedBy = "DefaultRank+Strictness";
                r.dayIndex = day;
                r.createdTicks = System.DateTime.UtcNow.Ticks;
                BureauWorldState.policyConflictRecords.Add(r);
                cap--;
            }
        }
        while (BureauWorldState.policyConflictRecords.Count > 200)
            BureauWorldState.policyConflictRecords.RemoveAt(0);
    }

    static bool RecordExistsForPair(string a, string b)
    {
        for (int i = 0; i < BureauWorldState.policyConflictRecords.Count; i++)
        {
            var c = BureauWorldState.policyConflictRecords[i];
            if (c == null || c.involvedPolicyIds == null) continue;
            if (c.involvedPolicyIds.Count < 2) continue;
            if ((c.involvedPolicyIds.Contains(a) && c.involvedPolicyIds.Contains(b)) || (c.involvedPolicyIds.Contains(b) && c.involvedPolicyIds.Contains(a)))
                return true;
        }
        return false;
    }
}

public static class FederalPolicyValidation
{
    public static void LintToConsole()
    {
        for (int i = 0; i < BureauWorldState.activeFederalPolicies.Count; i++)
        {
            var p = BureauWorldState.activeFederalPolicies[i];
            if (p == null) { Debug.LogWarning("[FederalPolicy] null entry"); continue; }
            if (string.IsNullOrEmpty(p.ownerAgentId))
                Debug.LogWarning("[FederalPolicy] " + p.policyId + " missing ownerAgentId");
            if (p.domainInt < 0) Debug.LogWarning("[FederalPolicy] " + p.policyId + " missing/invalid domain");
            if (p.strictnessInt == (int)FederalPolicyStrictness.Forbidden
                && p.stanceInt != (int)FederalPolicyStance.Avoid
                && p.stanceInt != (int)FederalPolicyStance.Restrict)
                Debug.LogWarning("[FederalPolicy] " + p.policyId + " Forbidden strictness should pair with Avoid/Restrict stance.");
        }

        for (int i = 0; i < BureauWorldState.policyConflictRecords.Count; i++)
        {
            var c = BureauWorldState.policyConflictRecords[i];
            if (c == null) continue;
            if (c.involvedPolicyIds == null || c.involvedPolicyIds.Count < 2)
                Debug.LogWarning("[FederalPolicy] conflict record missing 2+ policies: " + c.conflictId);
        }

        for (int i = 0; i < BureauWorldState.FederalDeviationRecords.Count; i++)
        {
            var d = BureauWorldState.FederalDeviationRecords[i];
            if (d == null) continue;
            bool policyKnown = !string.IsNullOrEmpty(d.policyId);
            bool taggedAsPolicyDeviation = d.deviationTypeInts != null && d.deviationTypeInts.Contains((int)FederalDeviationType.PolicyDeviation);
            if (taggedAsPolicyDeviation && !policyKnown)
                Debug.LogWarning("[FederalPolicy] policy deviation missing policyId: " + d.deviationId);
        }
    }
}
