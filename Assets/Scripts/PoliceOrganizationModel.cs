using System;
using System.Collections.Generic;
using UnityEngine;

public enum PoliceDepartmentKind
{
    Patrol,
    Investigations,
    Intelligence,
    Enforcement,
    Evidence,
    Custody,
    Records,
    Administration
}

public enum PoliceDutyRole
{
    PatrolOfficer,
    Detective,
    IntelligenceOfficer,
    EnforcementOfficer,
    EvidenceOfficer,
    OversightOfficer,
    CustodyOfficer,
    RecordsOfficer,
    AdministrativeOfficer
}

[Serializable]
public class PoliceRankDefinition
{
    public string RankId;
    public string DisplayName;
    public int CommandLevel;
}

[Serializable]
public class PoliceOfficerAssignment
{
    public string OfficerId;
    public string DisplayName;
    public string RankId;
    public PoliceDutyRole DutyRole;
    public string DepartmentId;
    public string TeamId;
}

[Serializable]
public class PoliceTeamProfile
{
    public string TeamId;
    public string TeamName;
    public string LeadOfficerId;
    public List<string> MemberOfficerIds = new List<string>();
}

[Serializable]
public class PoliceDepartmentProfile
{
    public string DepartmentId;
    public string DepartmentName;
    public PoliceDepartmentKind Kind;
    public string HeadOfficerId;
    public List<PoliceTeamProfile> Teams = new List<PoliceTeamProfile>();
}

[Serializable]
public class PoliceStationProfile
{
    public string StationId;
    public string DisplayName;
    public string CommanderOfficerId;

    // Station depth points from your design.
    public int Professionalism; // 0..100
    public int Corruption;      // 0..100
    public int Workload;        // 0..100
    public int IntelligenceReadiness; // 0..100
    public int Manpower;        // 0..100
    public int EquipmentReadiness; // 0..100
    public int OperationalTempo; // 0..100

    public List<PoliceDepartmentProfile> Departments = new List<PoliceDepartmentProfile>();
}

[Serializable]
public class PoliceHeadquartersProfile
{
    public string HqId;
    public string DisplayName;
    public string ChiefOfficerId;
    public List<PoliceStationProfile> Stations = new List<PoliceStationProfile>();
}

public static class PoliceOrganizationPilotFactory
{
    /// <summary>
    /// Pilot setup for current scope: HQ exists in-world, one playable station is fully instantiated.
    /// </summary>
    public static PoliceHeadquartersProfile BuildSingleStationPilot()
    {
        PoliceHeadquartersProfile hq = new PoliceHeadquartersProfile
        {
            HqId = "hq_city_police",
            DisplayName = "Ashkelton City Police HQ",
            ChiefOfficerId = "officer_chief_placeholder"
        };

        PoliceStationProfile station = new PoliceStationProfile
        {
            StationId = "station_pilot_01",
            DisplayName = "Central Pilot Station",
            CommanderOfficerId = "officer_station_commander_placeholder",
            Professionalism = 58,
            Corruption = 37,
            Workload = 61,
            IntelligenceReadiness = 49,
            Manpower = 54,
            EquipmentReadiness = 52,
            OperationalTempo = 45
        };

        station.Departments.Add(NewDepartment(PoliceDepartmentKind.Patrol, "dept_patrol", "Patrol Division"));
        station.Departments.Add(NewDepartment(PoliceDepartmentKind.Investigations, "dept_investigations", "Investigations Division"));
        station.Departments.Add(NewDepartment(PoliceDepartmentKind.Intelligence, "dept_intelligence", "Intelligence Division"));
        station.Departments.Add(NewDepartment(PoliceDepartmentKind.Enforcement, "dept_enforcement", "Enforcement Division"));
        station.Departments.Add(NewDepartment(PoliceDepartmentKind.Evidence, "dept_evidence", "Evidence & Forensics"));
        station.Departments.Add(NewDepartment(PoliceDepartmentKind.Custody, "dept_custody", "Custody Unit"));
        station.Departments.Add(NewDepartment(PoliceDepartmentKind.Records, "dept_records", "Records Office"));
        station.Departments.Add(NewDepartment(PoliceDepartmentKind.Administration, "dept_admin", "Administration & Logistics"));

        hq.Stations.Add(station);
        return hq;
    }

    private static PoliceDepartmentProfile NewDepartment(PoliceDepartmentKind kind, string id, string name)
    {
        return new PoliceDepartmentProfile
        {
            DepartmentId = id,
            DepartmentName = name,
            Kind = kind,
            HeadOfficerId = "officer_head_" + id
        };
    }

    public static int GetRoleWeightForAction(PoliceDutyRole role, PoliceActionType actionType)
    {
        // Non-blocking role affinity. Traits/skills still decide the result.
        switch (role)
        {
            case PoliceDutyRole.PatrolOfficer:
                if (actionType == PoliceActionType.StreetStop || actionType == PoliceActionType.Detain || actionType == PoliceActionType.Chase)
                    return 12;
                break;
            case PoliceDutyRole.Detective:
                if (actionType == PoliceActionType.InterrogateSuspect || actionType == PoliceActionType.CaseManagement || actionType == PoliceActionType.CrossCheckIntel)
                    return 12;
                break;
            case PoliceDutyRole.IntelligenceOfficer:
                if (actionType == PoliceActionType.SurveillanceTail || actionType == PoliceActionType.HandleInformant || actionType == PoliceActionType.CrossCheckIntel)
                    return 12;
                break;
            case PoliceDutyRole.EnforcementOfficer:
                if (actionType == PoliceActionType.Arrest || actionType == PoliceActionType.Raid || actionType == PoliceActionType.Detain)
                    return 12;
                break;
            case PoliceDutyRole.EvidenceOfficer:
                if (actionType == PoliceActionType.EvidenceChainHandling || actionType == PoliceActionType.WriteReport)
                    return 12;
                break;
            case PoliceDutyRole.OversightOfficer:
                if (actionType == PoliceActionType.WriteReport || actionType == PoliceActionType.CrossCheckIntel)
                    return 8;
                break;
        }

        return 0;
    }
}
