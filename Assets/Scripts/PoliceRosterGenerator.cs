using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Builds a deterministic city police roster and wires the pilot <see cref="PoliceHeadquartersProfile"/> to real officer ids.</summary>
public static class PoliceRosterGenerator
{
    static readonly string[] FirstNames =
    {
        "James", "Frank", "Michael", "Thomas", "Richard", "Daniel", "George", "Charles",
        "Eleanor", "Margaret", "Helen", "Ruth", "Edith", "Catherine", "Clara", "Annie"
    };

    static readonly string[] LastNames =
    {
        "O'Rourke", "Harlan", "Marconi", "Wexler", "Kowalski", "Graves", "Sullivan", "Brennan",
        "Carmody", "Doyle", "Fitzgerald", "Hale", "Ingram", "Kerr", "Lombardi", "Muldoon"
    };

    public static void Build(
        int cityMapSeed,
        out PoliceHeadquartersProfile hq,
        out List<OfficerProfile> officers,
        out List<OfficerCareerProfile> careers,
        out List<InternalRiskProfile> internalRisk)
    {
        int s = cityMapSeed != 0 ? cityMapSeed : 1;
        var rng = new System.Random(unchecked(s ^ 0x2E8B0E33));
        hq = PoliceOrganizationPilotFactory.BuildSingleStationPilot();
        officers = new List<OfficerProfile>();
        careers = new List<OfficerCareerProfile>();
        internalRisk = new List<InternalRiskProfile>();
        if (hq == null || hq.Stations == null || hq.Stations.Count == 0)
            return;

        PoliceStationProfile station = hq.Stations[0];
        int manpower = Mathf.Clamp(station.Manpower, 1, 100);
        int count = Mathf.Clamp(12 + (manpower * 36 / 100), 12, 56);
        int deptCount = station.Departments != null ? station.Departments.Count : 1;

        for (int i = 0; i < count; i++)
        {
            int departmentIndex = i % Mathf.Max(1, deptCount);
            PoliceDepartmentProfile dept = station.Departments[departmentIndex];
            string deptId = dept != null ? dept.DepartmentId : "dept_patrol";
            PoliceDutyRole duty = DepartmentKindToDutyRole(dept != null ? dept.Kind : PoliceDepartmentKind.Patrol);
            PoliceRank rank = PickRank(rng);
            string first = FirstNames[rng.Next(0, FirstNames.Length)];
            string last = LastNames[rng.Next(0, LastNames.Length)];
            string name = first + " " + last;
            string id = "off_" + i.ToString("D4");

            var o = new OfficerProfile
            {
                OfficerId = id,
                DisplayName = name,
                Role = duty.ToString(),
                Rank = PoliceRankToDisplay(rank),
                DutyRole = duty,
                StationId = station.StationId,
                DepartmentId = deptId,
                TeamId = "team_" + deptId + "_1",
                SkillLevels = new int[DerivedSkillProgression.SkillCount]
            };
            RollTraits(rng, o);
            RollSkillsForDuty(rng, o, duty);

            o.CorruptionVulnerability = rng.Next(5, 45);
            o.CorruptionPressure = rng.Next(5, 50);
            o.SystemLoyalty = rng.Next(40, 95);
            o.Personality = (OfficerPersonalityFlags)rng.Next(0, 32);

            var c = new OfficerCareerProfile
            {
                officerId = id,
                serviceYears = 1f + (float)rng.NextDouble() * 18f,
                currentRank = rank,
                currentAssignment = MapDutyToCore(duty),
                careerStatus = OfficerCareerStatus.Active
            };
            c.promotionScore = rng.Next(20, 75);
            c.internalReputation = rng.Next(25, 80);
            c.streetReputation = rng.Next(15, 70);
            c.publicReputation = rng.Next(20, 75);
            c.traumaLevel = rng.Next(0, 25);
            c.burnoutLevel = rng.Next(0, 20);

            var ir = new InternalRiskProfile
            {
                officerId = id,
                misconductRisk = rng.Next(0, 25),
                corruptionRisk = rng.Next(0, 30),
                reportIntegrity = rng.Next(50, 95),
                forceDiscipline = rng.Next(45, 90),
                evidenceIntegrity = rng.Next(50, 95),
                leakRisk = rng.Next(0, 20),
                politicalProtection = rng.Next(0, 20),
                blackmailExposure = rng.Next(0, 15)
            };

            officers.Add(o);
            careers.Add(c);
            internalRisk.Add(ir);
        }

        string stationId = station.StationId;
        OfficerProfile chiefPick = null;
        for (int i = 0; i < officers.Count; i++)
        {
            if (officers[i] == null)
                continue;
            if (chiefPick == null || GetRankOrder(careers[i].currentRank) > GetRankOrder(FindCareerById(careers, chiefPick.OfficerId).currentRank))
                chiefPick = officers[i];
        }
        if (chiefPick != null)
        {
            var cc = FindCareerById(careers, chiefPick.OfficerId);
            if (cc != null)
            {
                cc.currentRank = PoliceRank.ChiefCommissioner;
                cc.currentAssignment = PoliceCoreRole.CityCommand;
            }
            chiefPick.Rank = PoliceRankToDisplay(PoliceRank.ChiefCommissioner);
            chiefPick.DutyRole = PoliceDutyRole.AdministrativeOfficer;
            hq.ChiefOfficerId = chiefPick.OfficerId;
        }
        OfficerProfile stationCommander = null;
        for (int i = 0; i < officers.Count; i++)
        {
            if (officers[i] == null)
                continue;
            if (!string.Equals(officers[i].StationId, stationId, StringComparison.Ordinal))
                continue;
            if (chiefPick != null && string.Equals(officers[i].OfficerId, chiefPick.OfficerId, StringComparison.Ordinal))
                continue;
            if (stationCommander == null || GetRankOrder(careers[i].currentRank) > GetRankOrder(FindCareerById(careers, stationCommander.OfficerId).currentRank))
                stationCommander = officers[i];
        }
        if (stationCommander == null)
        {
            for (int i = 0; i < officers.Count; i++)
            {
                if (officers[i] == null)
                    continue;
                if (string.Equals(officers[i].StationId, stationId, StringComparison.Ordinal) &&
                    (stationCommander == null || GetRankOrder(careers[i].currentRank) > GetRankOrder(FindCareerById(careers, stationCommander.OfficerId).currentRank)))
                    stationCommander = officers[i];
            }
        }
        if (stationCommander != null)
        {
            OfficerCareerProfile sc = FindCareerById(careers, stationCommander.OfficerId);
            if (sc != null)
            {
                if (sc.currentRank < PoliceRank.Commander)
                {
                    sc.currentRank = PoliceRank.Commander;
                    stationCommander.Rank = PoliceRankToDisplay(PoliceRank.Commander);
                }
                sc.currentAssignment = PoliceCoreRole.StationCommander;
            }
            station.CommanderOfficerId = stationCommander.OfficerId;
        }
        WireDepartmentsAndTeams(hq, officers, careers, rng);
    }

    static OfficerCareerProfile FindCareerById(List<OfficerCareerProfile> list, string id)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != null && string.Equals(list[i].officerId, id, StringComparison.OrdinalIgnoreCase))
                return list[i];
        }
        return null;
    }

    static void WireDepartmentsAndTeams(
        PoliceHeadquartersProfile hq,
        List<OfficerProfile> officers,
        List<OfficerCareerProfile> careers,
        System.Random rng)
    {
        if (hq == null)
            return;
        for (int s = 0; s < hq.Stations.Count; s++)
        {
            PoliceStationProfile st = hq.Stations[s];
            if (st == null || st.Departments == null)
                continue;
            for (int d = 0; d < st.Departments.Count; d++)
            {
                PoliceDepartmentProfile dept = st.Departments[d];
                if (dept == null)
                    continue;
                var inDept = new List<OfficerProfile>();
                for (int o = 0; o < officers.Count; o++)
                {
                    OfficerProfile op = officers[o];
                    if (op == null)
                        continue;
                    if (string.Equals(op.StationId, st.StationId) &&
                        string.Equals(op.DepartmentId, dept.DepartmentId, StringComparison.OrdinalIgnoreCase))
                        inDept.Add(op);
                }
                if (inDept.Count == 0)
                    continue;
                OfficerProfile head = inDept[0];
                for (int i = 1; i < inDept.Count; i++)
                {
                    if (GetRankOrder(FindCareerById(careers, inDept[i].OfficerId).currentRank) >
                        GetRankOrder(FindCareerById(careers, head.OfficerId).currentRank))
                        head = inDept[i];
                }
                dept.HeadOfficerId = head.OfficerId;
                dept.Teams.Clear();
                int teamSize = Mathf.Clamp(3 + rng.Next(0, 2), 2, 6);
                int teamIndex = 1;
                for (int i = 0; i < inDept.Count; i += teamSize)
                {
                    var team = new PoliceTeamProfile
                    {
                        TeamId = "team_" + dept.DepartmentId + "_" + teamIndex,
                        TeamName = dept.DepartmentName + " Team " + teamIndex
                    };
                    for (int j = i; j < inDept.Count && j < i + teamSize; j++)
                    {
                        team.MemberOfficerIds.Add(inDept[j].OfficerId);
                        inDept[j].TeamId = team.TeamId;
                    }
                    if (team.MemberOfficerIds.Count > 0)
                    {
                        team.LeadOfficerId = team.MemberOfficerIds[0];
                    }
                    dept.Teams.Add(team);
                    teamIndex++;
                }
            }
        }
    }

    static int GetRankOrder(PoliceRank r)
    {
        return (int)r;
    }

    static PoliceRank PickRank(System.Random rng)
    {
        int w = rng.Next(0, 100);
        if (w < 40) return PoliceRank.Constable;
        if (w < 70) return PoliceRank.SeniorConstable;
        if (w < 85) return PoliceRank.Sergeant;
        if (w < 93) return PoliceRank.Lieutenant;
        if (w < 98) return PoliceRank.Captain;
        if (w < 99) return PoliceRank.Commander;
        return PoliceRank.ChiefCommissioner;
    }

    static string PoliceRankToDisplay(PoliceRank r)
    {
        return r switch
        {
            PoliceRank.Constable => "Constable",
            PoliceRank.SeniorConstable => "Senior Constable",
            PoliceRank.Sergeant => "Sergeant",
            PoliceRank.Lieutenant => "Lieutenant",
            PoliceRank.Captain => "Captain",
            PoliceRank.Commander => "Commander",
            PoliceRank.ChiefCommissioner => "Chief Commissioner",
            _ => "Constable"
        };
    }

    static PoliceDutyRole DepartmentKindToDutyRole(PoliceDepartmentKind k)
    {
        return k switch
        {
            PoliceDepartmentKind.Patrol => PoliceDutyRole.PatrolOfficer,
            PoliceDepartmentKind.Investigations => PoliceDutyRole.Detective,
            PoliceDepartmentKind.Intelligence => PoliceDutyRole.IntelligenceOfficer,
            PoliceDepartmentKind.Enforcement => PoliceDutyRole.EnforcementOfficer,
            PoliceDepartmentKind.Evidence => PoliceDutyRole.EvidenceOfficer,
            PoliceDepartmentKind.Custody => PoliceDutyRole.CustodyOfficer,
            PoliceDepartmentKind.Records => PoliceDutyRole.RecordsOfficer,
            PoliceDepartmentKind.Administration => PoliceDutyRole.AdministrativeOfficer,
            _ => PoliceDutyRole.PatrolOfficer
        };
    }

    public static PoliceCoreRole MapDutyToCore(PoliceDutyRole d)
    {
        return d switch
        {
            PoliceDutyRole.PatrolOfficer => PoliceCoreRole.PatrolOfficer,
            PoliceDutyRole.Detective => PoliceCoreRole.Detective,
            PoliceDutyRole.IntelligenceOfficer => PoliceCoreRole.IntelligenceOfficer,
            PoliceDutyRole.EnforcementOfficer => PoliceCoreRole.EnforcementOfficer,
            PoliceDutyRole.EvidenceOfficer => PoliceCoreRole.EvidenceOfficer,
            PoliceDutyRole.OversightOfficer => PoliceCoreRole.InternalOversightOfficer,
            _ => PoliceCoreRole.PatrolOfficer
        };
    }

    static void RollTraits(System.Random rng, OfficerProfile o)
    {
        o.Strength = rng.Next(32, 78);
        o.Agility = rng.Next(32, 78);
        o.Intelligence = rng.Next(32, 78);
        o.Charisma = rng.Next(32, 78);
        o.MentalResilience = rng.Next(35, 88);
        o.Determination = rng.Next(35, 88);
    }

    static void RollSkillsForDuty(System.Random rng, OfficerProfile o, PoliceDutyRole duty)
    {
        for (int i = 0; i < o.SkillLevels.Length; i++)
            o.SkillLevels[i] = rng.Next(0, 3);
        void bump(DerivedSkill s, int max)
        {
            o.SkillLevels[(int)s] = Mathf.Min(max, o.SkillLevels[(int)s] + rng.Next(1, 4));
        }
        switch (duty)
        {
            case PoliceDutyRole.PatrolOfficer:
                bump(DerivedSkill.Driving, 6);
                bump(DerivedSkill.Intimidation, 6);
                break;
            case PoliceDutyRole.Detective:
                bump(DerivedSkill.Surveillance, 7);
                bump(DerivedSkill.Analysis, 7);
                bump(DerivedSkill.Legal, 6);
                break;
            case PoliceDutyRole.IntelligenceOfficer:
                bump(DerivedSkill.Surveillance, 7);
                bump(DerivedSkill.Deception, 5);
                bump(DerivedSkill.Analysis, 6);
                break;
            case PoliceDutyRole.EnforcementOfficer:
                bump(DerivedSkill.Firearms, 7);
                bump(DerivedSkill.Brawling, 6);
                break;
            case PoliceDutyRole.EvidenceOfficer:
                bump(DerivedSkill.Analysis, 7);
                bump(DerivedSkill.Legal, 5);
                break;
            case PoliceDutyRole.CustodyOfficer:
                bump(DerivedSkill.Intimidation, 5);
                bump(DerivedSkill.Leadership, 5);
                break;
            case PoliceDutyRole.RecordsOfficer:
            case PoliceDutyRole.AdministrativeOfficer:
                bump(DerivedSkill.Legal, 6);
                bump(DerivedSkill.Finance, 4);
                bump(DerivedSkill.Leadership, 5);
                break;
        }
    }
}
