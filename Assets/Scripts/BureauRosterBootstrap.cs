using System;
using System.Collections.Generic;
using UnityEngine;

public static class BureauRosterBootstrap
{
    const string DirectorId = "fbu_dir_01";

    static readonly string[] First = { "Avery", "Blake", "Carter", "Dana", "Edgar", "Florence", "Graham", "Harriet" };
    static readonly string[] Last = { "Hayes", "Ivers", "Judd", "Keene", "Lomax", "Mercer", "Nolan", "Ortega" };

    public static void BuildIntoWorldState(int cityMapSeed)
    {
        int s = cityMapSeed != 0 ? cityMapSeed : 1;
        // 0xFED0BA11U does not fit signed int; XOR in uint32 space so seed stays int (Random ctor).
        int seed = unchecked(s ^ (int)0xFED0BA11U);
        var rng = new System.Random(seed);

        var depIds = new[] { "fbu_dep_ops", "fbu_dep_intel", "fbu_dep_bud", "fbu_dep_pol" };
        var depPort = new[] { FederalDeputyPortfolio.Operations, FederalDeputyPortfolio.Intelligence, FederalDeputyPortfolio.BudgetFacilitiesLogistics, FederalDeputyPortfolio.PoliticalLegal };

        var divs = new (string id, FederalDivisionType t, string chief, FederalAgentAssignment assign, string liasonDep)[]
        {
            (FederalBureauStructure.FederalBureauDivisionIds.OrganizedCrime, FederalDivisionType.OrganizedCrimeUnit, "fbu_chief_01", FederalAgentAssignment.OrganizedCrime, "fbu_dep_ops"),
            (FederalBureauStructure.FederalBureauDivisionIds.ProhibitionSubstances, FederalDivisionType.ProhibitionAndDangerousSubstancesUnit, "fbu_chief_02", FederalAgentAssignment.ProhibitionAndSubstances, "fbu_dep_bud"),
            (FederalBureauStructure.FederalBureauDivisionIds.Intelligence, FederalDivisionType.FederalIntelligenceUnit, "fbu_chief_03", FederalAgentAssignment.Intelligence, "fbu_dep_intel"),
            (FederalBureauStructure.FederalBureauDivisionIds.Operations, FederalDivisionType.FederalOperationsUnit, "fbu_chief_04", FederalAgentAssignment.Operations, "fbu_dep_ops"),
            (FederalBureauStructure.FederalBureauDivisionIds.FacilitiesLogistics, FederalDivisionType.FacilitiesAndLogisticsUnit, "fbu_chief_05", FederalAgentAssignment.FacilitiesAndLogistics, "fbu_dep_bud"),
            (FederalBureauStructure.FederalBureauDivisionIds.PoliticalLegal, FederalDivisionType.PoliticalAndLegalAffairsUnit, "fbu_chief_06", FederalAgentAssignment.PoliticalAndLegal, "fbu_dep_pol"),
            (FederalBureauStructure.FederalBureauDivisionIds.InternalControl, FederalDivisionType.InternalControlUnit, "fbu_chief_07", FederalAgentAssignment.InternalControl, "fbu_dep_pol"),
            (FederalBureauStructure.FederalBureauDivisionIds.StrategicCases, FederalDivisionType.StrategicCasesUnit, "fbu_chief_08", FederalAgentAssignment.OrganizedCrime, "fbu_dep_ops")
        };

        int nm = 0;
        string Name() => First[nm % First.Length] + " " + Last[(nm++ / 2) % Last.Length];

        void Push(FederalAgentProfile a)
        {
            BureauWorldState.Roster.Add(a);
        }

        // div: null or empty = not assigned to a line division (Director / Deputy only).
        FederalAgentProfile Make(string id, string n, FederalBureauRank r, FederalAgentAssignment a, string div, FederalDeputyPortfolio p = FederalDeputyPortfolio.None)
        {
            var o = new FederalAgentProfile
            {
                agentId = id,
                fullName = n,
                rank = r,
                deputyPortfolio = p,
                assignment = a,
                divisionId = string.IsNullOrEmpty(div) ? null : div,
                teamId = string.Empty,
                coverStatus = (r >= FederalBureauRank.UnitChief)
                    ? FederalCoverStatus.OpenIdentity
                    : RngCover(rng),
                availableForField = true,
                strength = 35 + rng.Next(0, 50),
                agility = 35 + rng.Next(0, 50),
                intelligence = 40 + rng.Next(0, 50),
                charisma = 35 + rng.Next(0, 50),
                mentalResilience = 40 + rng.Next(0, 50),
                determination = 40 + rng.Next(0, 50),
                career = new FederalAgentCareerStub
                {
                    serviceYears = 1f + (float)rng.NextDouble() * 18f,
                    promotionScore = rng.Next(25, 80)
                },
                secrecyRisk = rng.Next(0, 40),
                corruptionRisk = rng.Next(0, 30),
                blackmailRisk = rng.Next(0, 25),
                fieldReputation = rng.Next(20, 70),
                internalReputation = rng.Next(30, 85)
            };
            for (int i = 0; i < o.skillLevels.Length; i++)
                o.skillLevels[i] = rng.Next(0, 3);
            if (a == FederalAgentAssignment.Intelligence)
            {
                o.skillLevels[(int)DerivedSkill.Surveillance] = Mathf.Min(7, 3 + rng.Next(0, 4));
                o.skillLevels[(int)DerivedSkill.Analysis] = Mathf.Min(7, 2 + rng.Next(0, 3));
            }
            if (a == FederalAgentAssignment.Operations)
                o.skillLevels[(int)DerivedSkill.Leadership] = Mathf.Min(7, 2 + rng.Next(0, 3));
            o.personalityFlags = (int)(OfficerPersonalityFlags)rng.Next(0, 64);
            return o;
        }

        BureauWorldState.Roster.Clear();
        BureauWorldState.Divisions.Clear();
        BureauWorldState.FieldTeams.Clear();
        BureauWorldState.Facilities.Clear();

        // Director and Deputy Directors: not under any of the eight line divisions (divisionId null).
        Push(Make(DirectorId, Name(), FederalBureauRank.DirectorOfCentralUnit, FederalAgentAssignment.PoliticalAndLegal, null, FederalDeputyPortfolio.None));
        for (int i = 0; i < 4; i++)
            Push(Make(depIds[i], Name(), FederalBureauRank.DeputyDirector, AssignmentFor(depPort[i], rng), null, depPort[i]));

        for (int d = 0; d < divs.Length; d++)
        {
            Push(Make(divs[d].chief, Name(), FederalBureauRank.UnitChief, divs[d].assign, divs[d].id, FederalDeputyPortfolio.None));
            for (int k = 0; k < 4; k++)
            {
                var rr = (FederalBureauRank)rng.Next(1, 4);
                if (k == 0) rr = FederalBureauRank.SeniorFieldAgent;
                if (k == 1) rr = FederalBureauRank.SupervisingSpecialAgent;
                Push(Make("fbu_a_" + divs[d].id + "_" + k, Name(), rr, divs[d].assign, divs[d].id, FederalDeputyPortfolio.None));
            }
        }
        for (int d = 0; d < divs.Length; d++)
        {
            var D = new FederalDivision
            {
                divisionId = divs[d].id,
                divisionType = divs[d].t,
                chiefAgentId = divs[d].chief,
                deputyDirectorLiaisonId = divs[d].liasonDep
            };
            for (int i = 0; i < BureauWorldState.Roster.Count; i++)
            {
                if (BureauWorldState.Roster[i].divisionId == D.divisionId)
                    D.agentIds.Add(BureauWorldState.Roster[i].agentId);
            }
            BureauWorldState.Divisions.Add(D);
        }
        for (int t = 0; t < 2; t++)
        {
            var lead = FindByRankInDivision(rng, FederalBureauRank.SupervisingSpecialAgent, FederalBureauStructure.FederalBureauDivisionIds.Operations, DirectorId) ?? FindAny(rng, FederalBureauStructure.FederalBureauDivisionIds.Operations);
            if (string.IsNullOrEmpty(lead))
                lead = BureauWorldState.Roster[Mathf.Min(10, BureauWorldState.Roster.Count - 1)].agentId;
            var team = new FederalFieldTeam
            {
                teamId = "fbu_field_" + (t + 1),
                teamName = "Field team " + (t + 1),
                leadAgentId = lead,
                currentStatus = FederalFieldTeamStatus.Available,
                secrecyLevel = 2 + t,
                operationalRisk = 15 + 12 * t
            };
            string m0 = FindAny(rng, FederalBureauStructure.FederalBureauDivisionIds.Operations);
            if (!string.IsNullOrEmpty(m0) && m0 != lead) team.memberIds.Add(m0);
            team.memberIds.Add(lead);
            BureauWorldState.FieldTeams.Add(team);
        }
        BureauWorldState.Facilities.Add(new FederalFacility
        {
            facilityId = "fbu_hq_ash",
            facilityType = FederalFacilityTypeBureau.PublicHQ,
            isRegistered = true,
            budgetSource = FederalFacilityBudgetSource.OfficialBudget,
            currentUse = "federal_hq",
            controlledByDivisionId = FederalBureauStructure.FederalBureauDivisionIds.FacilitiesLogistics,
            secrecyLevel = 1,
            exposureRisk = 0
        });
        BureauWorldState.Facilities.Add(new FederalFacility
        {
            facilityId = "fbu_safe_01",
            facilityType = FederalFacilityTypeBureau.SafeHouse,
            isRegistered = true,
            budgetSource = FederalFacilityBudgetSource.ClassifiedFund,
            currentUse = "standby",
            controlledByDivisionId = FederalBureauStructure.FederalBureauDivisionIds.FacilitiesLogistics,
            secrecyLevel = 3,
            exposureRisk = 8
        });
    }

    static string FindByRankInDivision(System.Random rng, FederalBureauRank r, string div, string notId)
    {
        var pool = new List<string>();
        for (int i = 0; i < BureauWorldState.Roster.Count; i++)
        {
            var a = BureauWorldState.Roster[i];
            if (a.rank == r && a.divisionId == div && a.agentId != notId)
                pool.Add(a.agentId);
        }
        if (pool.Count == 0)
            return null;
        return pool[rng.Next(0, pool.Count)];
    }
    static string FindAny(System.Random rng, string div)
    {
        for (int i = 0; i < BureauWorldState.Roster.Count; i++)
        {
            if (BureauWorldState.Roster[i].divisionId == div)
                return BureauWorldState.Roster[i].agentId;
        }
        return null;
    }
    static FederalAgentAssignment AssignmentFor(FederalDeputyPortfolio p, System.Random rng) =>
        p switch
        {
            FederalDeputyPortfolio.Operations => FederalAgentAssignment.Operations,
            FederalDeputyPortfolio.Intelligence => FederalAgentAssignment.Intelligence,
            FederalDeputyPortfolio.BudgetFacilitiesLogistics => FederalAgentAssignment.FacilitiesAndLogistics,
            FederalDeputyPortfolio.PoliticalLegal => FederalAgentAssignment.PoliticalAndLegal,
            _ => FederalAgentAssignment.Field
        };
    static FederalCoverStatus RngCover(System.Random rng) => rng.Next(0, 100) < 70 ? FederalCoverStatus.ClassifiedIdentity : FederalCoverStatus.Undercover;
}
