using System;
using System.Collections.Generic;

public enum ActionDomain
{
    Person,
    Place,
    Property,
    Information,
    Organization,
    Internal
}

public enum InvasivenessLevel
{
    Low,
    Medium,
    High,
    Severe
}

public enum LegalitySensitivity
{
    Low,
    Medium,
    High,
    Critical
}

public enum EscalationPotential
{
    None,
    Limited,
    High,
    Critical
}

public enum PoliceCatalogAction
{
    // Person
    ApproachPerson,
    RequestIdentification,
    BriefQuestioning,
    TemporaryDetention,
    FriskPerson,
    HandcuffPerson,
    TransportPerson,
    ArrestPerson,
    InterrogateSuspect,
    TakeWitnessStatement,
    SummonForQuestioning,
    LineupIdentification,
    ShortPersonalSurveillance,
    ExtendedPersonalSurveillance,
    HandleHumanSource,
    RecruitInformant,
    CoerciveLeverage, // systemic illegal/corrupt path
    RecruitStateWitness,
    AssignProtectiveDetail,
    NeutralizeImmediateThreat,

    // Place
    ObservePlace,
    PerimeterSurveillancePlace,
    VisiblePoliceVisit,
    EnterPublicPlace,
    LicensingAndRecordsInspection,
    SearchBuilding,
    RaidPlace,
    PerimeterContainmentPlace,
    TemporarilyClosePlace,
    PlaceInsideSource,
    WiretapPlace,
    CovertEntryPlace,

    // Property
    IdentifySuspiciousProperty,
    SeizeEvidenceItem,
    ConfiscateProperty,
    SearchProperty,
    ExamineDocuments,
    ProcessEvidenceChain,
    ForensicExamination,
    DestroyHazardousProperty,

    // Information
    CollectRumor,
    RegisterIntel,
    CrossCorrelateInformation,
    ClassifySource,
    RequestWarrantFromIntel,
    ConcealInformation,
    LeakInformation,
    ForgeInformation,

    // Organization
    OpenOrganizationCase,
    MapOrganizationStructure,
    IdentifyKeyTargets,
    InfiltrateOrganization,
    BuildStrategicCase,
    RaidOrganization,
    SeizeOrganizationInfrastructure,
    RecruitOrgMemberAsSource,
    StrategicCapabilitySuppression,

    // Internal
    OpenInternalReview,
    AuditReportInternal,
    AuditForceInternal,
    AuditCorruptionInternal,
    AuditLeakInternal,
    AuditEvidenceInternal,
    AuditComplaintInternal,
    CoverUpInternal,
    ScapegoatJuniorOfficer,
    ReassignOfficerStation,
    FreezePromotion,
    SuspendOfficer
}

[Serializable]
public sealed class PoliceActionCatalogEntry
{
    public PoliceCatalogAction Action;
    public string DisplayNameEn;
    public ActionDomain Domain;
    public InvasivenessLevel Invasiveness;
    public LegalitySensitivity Sensitivity;
    public DocumentationLevel DocumentationNeed;
    public EscalationPotential Escalation;
}

public static class PoliceActionCatalog
{
    private static readonly Dictionary<PoliceCatalogAction, PoliceActionCatalogEntry> _entries =
        BuildEntries();

    public static PoliceActionCatalogEntry Get(PoliceCatalogAction action)
    {
        return _entries.TryGetValue(action, out PoliceActionCatalogEntry e) ? e : null;
    }

    public static IReadOnlyDictionary<PoliceCatalogAction, PoliceActionCatalogEntry> GetAll()
    {
        return _entries;
    }

    private static Dictionary<PoliceCatalogAction, PoliceActionCatalogEntry> BuildEntries()
    {
        Dictionary<PoliceCatalogAction, PoliceActionCatalogEntry> d = new Dictionary<PoliceCatalogAction, PoliceActionCatalogEntry>(128);

        // Person
        Add(d, PoliceCatalogAction.ApproachPerson, "Approach", ActionDomain.Person, InvasivenessLevel.Low, LegalitySensitivity.Low, DocumentationLevel.Basic, EscalationPotential.Limited);
        Add(d, PoliceCatalogAction.RequestIdentification, "Request Identification", ActionDomain.Person, InvasivenessLevel.Low, LegalitySensitivity.Medium, DocumentationLevel.Basic, EscalationPotential.Limited);
        Add(d, PoliceCatalogAction.BriefQuestioning, "Short Questioning", ActionDomain.Person, InvasivenessLevel.Low, LegalitySensitivity.Medium, DocumentationLevel.Basic, EscalationPotential.Limited);
        Add(d, PoliceCatalogAction.TemporaryDetention, "Detain", ActionDomain.Person, InvasivenessLevel.Medium, LegalitySensitivity.High, DocumentationLevel.Full, EscalationPotential.High);
        Add(d, PoliceCatalogAction.FriskPerson, "Frisk Search", ActionDomain.Person, InvasivenessLevel.Medium, LegalitySensitivity.High, DocumentationLevel.Full, EscalationPotential.High);
        Add(d, PoliceCatalogAction.HandcuffPerson, "Handcuff", ActionDomain.Person, InvasivenessLevel.Medium, LegalitySensitivity.High, DocumentationLevel.Full, EscalationPotential.High);
        Add(d, PoliceCatalogAction.TransportPerson, "Transport Detainee", ActionDomain.Person, InvasivenessLevel.Medium, LegalitySensitivity.High, DocumentationLevel.Full, EscalationPotential.High);
        Add(d, PoliceCatalogAction.ArrestPerson, "Arrest", ActionDomain.Person, InvasivenessLevel.High, LegalitySensitivity.Critical, DocumentationLevel.Full, EscalationPotential.Critical);
        Add(d, PoliceCatalogAction.InterrogateSuspect, "Interrogate Suspect", ActionDomain.Person, InvasivenessLevel.Medium, LegalitySensitivity.High, DocumentationLevel.Full, EscalationPotential.High);
        Add(d, PoliceCatalogAction.TakeWitnessStatement, "Take Witness Statement", ActionDomain.Person, InvasivenessLevel.Low, LegalitySensitivity.Medium, DocumentationLevel.Full, EscalationPotential.Limited);
        Add(d, PoliceCatalogAction.SummonForQuestioning, "Summon for Questioning", ActionDomain.Person, InvasivenessLevel.Low, LegalitySensitivity.Medium, DocumentationLevel.Full, EscalationPotential.Limited);
        Add(d, PoliceCatalogAction.LineupIdentification, "Lineup Identification", ActionDomain.Person, InvasivenessLevel.Medium, LegalitySensitivity.High, DocumentationLevel.Full, EscalationPotential.High);
        Add(d, PoliceCatalogAction.ShortPersonalSurveillance, "Short Personal Surveillance", ActionDomain.Person, InvasivenessLevel.Low, LegalitySensitivity.Medium, DocumentationLevel.Basic, EscalationPotential.Limited);
        Add(d, PoliceCatalogAction.ExtendedPersonalSurveillance, "Extended Personal Surveillance", ActionDomain.Person, InvasivenessLevel.Medium, LegalitySensitivity.High, DocumentationLevel.Full, EscalationPotential.High);
        Add(d, PoliceCatalogAction.HandleHumanSource, "Handle Human Source", ActionDomain.Person, InvasivenessLevel.Medium, LegalitySensitivity.High, DocumentationLevel.FullWithReview, EscalationPotential.High);
        Add(d, PoliceCatalogAction.RecruitInformant, "Recruit Informant", ActionDomain.Person, InvasivenessLevel.Medium, LegalitySensitivity.High, DocumentationLevel.FullWithReview, EscalationPotential.High);
        Add(d, PoliceCatalogAction.CoerciveLeverage, "Coercive Leverage", ActionDomain.Person, InvasivenessLevel.High, LegalitySensitivity.Critical, DocumentationLevel.FullWithReview, EscalationPotential.Critical);
        Add(d, PoliceCatalogAction.RecruitStateWitness, "Recruit Cooperative Witness", ActionDomain.Person, InvasivenessLevel.High, LegalitySensitivity.Critical, DocumentationLevel.FullWithReview, EscalationPotential.Critical);
        Add(d, PoliceCatalogAction.AssignProtectiveDetail, "Assign Protective Detail", ActionDomain.Person, InvasivenessLevel.Medium, LegalitySensitivity.High, DocumentationLevel.Full, EscalationPotential.Limited);
        Add(d, PoliceCatalogAction.NeutralizeImmediateThreat, "Neutralize Immediate Threat", ActionDomain.Person, InvasivenessLevel.Severe, LegalitySensitivity.Critical, DocumentationLevel.FullWithReview, EscalationPotential.Critical);

        // Place
        Add(d, PoliceCatalogAction.ObservePlace, "Observe Place", ActionDomain.Place, InvasivenessLevel.Low, LegalitySensitivity.Medium, DocumentationLevel.Basic, EscalationPotential.Limited);
        Add(d, PoliceCatalogAction.PerimeterSurveillancePlace, "Perimeter Surveillance", ActionDomain.Place, InvasivenessLevel.Medium, LegalitySensitivity.High, DocumentationLevel.Full, EscalationPotential.High);
        Add(d, PoliceCatalogAction.VisiblePoliceVisit, "Visible Police Visit", ActionDomain.Place, InvasivenessLevel.Low, LegalitySensitivity.Low, DocumentationLevel.Basic, EscalationPotential.Limited);
        Add(d, PoliceCatalogAction.EnterPublicPlace, "Enter Public Place", ActionDomain.Place, InvasivenessLevel.Low, LegalitySensitivity.Low, DocumentationLevel.Basic, EscalationPotential.Limited);
        Add(d, PoliceCatalogAction.LicensingAndRecordsInspection, "Licensing / Records Inspection", ActionDomain.Place, InvasivenessLevel.Medium, LegalitySensitivity.High, DocumentationLevel.Full, EscalationPotential.High);
        Add(d, PoliceCatalogAction.SearchBuilding, "Search Building", ActionDomain.Place, InvasivenessLevel.High, LegalitySensitivity.Critical, DocumentationLevel.FullWithReview, EscalationPotential.Critical);
        Add(d, PoliceCatalogAction.RaidPlace, "Raid Place", ActionDomain.Place, InvasivenessLevel.Severe, LegalitySensitivity.Critical, DocumentationLevel.FullWithReview, EscalationPotential.Critical);
        Add(d, PoliceCatalogAction.PerimeterContainmentPlace, "Perimeter Containment", ActionDomain.Place, InvasivenessLevel.High, LegalitySensitivity.High, DocumentationLevel.Full, EscalationPotential.High);
        Add(d, PoliceCatalogAction.TemporarilyClosePlace, "Temporarily Close Place", ActionDomain.Place, InvasivenessLevel.High, LegalitySensitivity.High, DocumentationLevel.Full, EscalationPotential.High);
        Add(d, PoliceCatalogAction.PlaceInsideSource, "Place Inside Source", ActionDomain.Place, InvasivenessLevel.High, LegalitySensitivity.Critical, DocumentationLevel.FullWithReview, EscalationPotential.Critical);
        Add(d, PoliceCatalogAction.WiretapPlace, "Wiretap Place", ActionDomain.Place, InvasivenessLevel.Severe, LegalitySensitivity.Critical, DocumentationLevel.FullWithReview, EscalationPotential.Critical);
        Add(d, PoliceCatalogAction.CovertEntryPlace, "Covert Entry", ActionDomain.Place, InvasivenessLevel.Severe, LegalitySensitivity.Critical, DocumentationLevel.FullWithReview, EscalationPotential.Critical);

        // Property
        Add(d, PoliceCatalogAction.IdentifySuspiciousProperty, "Identify Suspicious Property", ActionDomain.Property, InvasivenessLevel.Low, LegalitySensitivity.Medium, DocumentationLevel.Basic, EscalationPotential.Limited);
        Add(d, PoliceCatalogAction.SeizeEvidenceItem, "Seize Evidence", ActionDomain.Property, InvasivenessLevel.Medium, LegalitySensitivity.High, DocumentationLevel.Full, EscalationPotential.High);
        Add(d, PoliceCatalogAction.ConfiscateProperty, "Confiscate Property", ActionDomain.Property, InvasivenessLevel.High, LegalitySensitivity.High, DocumentationLevel.Full, EscalationPotential.High);
        Add(d, PoliceCatalogAction.SearchProperty, "Search Property", ActionDomain.Property, InvasivenessLevel.Medium, LegalitySensitivity.High, DocumentationLevel.Full, EscalationPotential.High);
        Add(d, PoliceCatalogAction.ExamineDocuments, "Examine Documents", ActionDomain.Property, InvasivenessLevel.Medium, LegalitySensitivity.High, DocumentationLevel.Full, EscalationPotential.High);
        Add(d, PoliceCatalogAction.ProcessEvidenceChain, "Process Evidence Chain", ActionDomain.Property, InvasivenessLevel.Medium, LegalitySensitivity.Critical, DocumentationLevel.Full, EscalationPotential.Limited);
        Add(d, PoliceCatalogAction.ForensicExamination, "Forensic Examination", ActionDomain.Property, InvasivenessLevel.Medium, LegalitySensitivity.High, DocumentationLevel.Full, EscalationPotential.High);
        Add(d, PoliceCatalogAction.DestroyHazardousProperty, "Destroy Hazardous Property", ActionDomain.Property, InvasivenessLevel.High, LegalitySensitivity.Critical, DocumentationLevel.FullWithReview, EscalationPotential.High);

        // Information
        Add(d, PoliceCatalogAction.CollectRumor, "Collect Rumor", ActionDomain.Information, InvasivenessLevel.Low, LegalitySensitivity.Low, DocumentationLevel.Basic, EscalationPotential.Limited);
        Add(d, PoliceCatalogAction.RegisterIntel, "Register Intel", ActionDomain.Information, InvasivenessLevel.Low, LegalitySensitivity.Medium, DocumentationLevel.Full, EscalationPotential.Limited);
        Add(d, PoliceCatalogAction.CrossCorrelateInformation, "Cross-Correlate Information", ActionDomain.Information, InvasivenessLevel.Low, LegalitySensitivity.Medium, DocumentationLevel.Full, EscalationPotential.Limited);
        Add(d, PoliceCatalogAction.ClassifySource, "Classify Source", ActionDomain.Information, InvasivenessLevel.Medium, LegalitySensitivity.High, DocumentationLevel.FullWithReview, EscalationPotential.Limited);
        Add(d, PoliceCatalogAction.RequestWarrantFromIntel, "Request Warrant from Intel", ActionDomain.Information, InvasivenessLevel.Medium, LegalitySensitivity.Critical, DocumentationLevel.Full, EscalationPotential.High);
        Add(d, PoliceCatalogAction.ConcealInformation, "Conceal Information", ActionDomain.Information, InvasivenessLevel.High, LegalitySensitivity.Critical, DocumentationLevel.FullWithReview, EscalationPotential.Critical);
        Add(d, PoliceCatalogAction.LeakInformation, "Leak Information", ActionDomain.Information, InvasivenessLevel.High, LegalitySensitivity.Critical, DocumentationLevel.FullWithReview, EscalationPotential.Critical);
        Add(d, PoliceCatalogAction.ForgeInformation, "Forge Information", ActionDomain.Information, InvasivenessLevel.Severe, LegalitySensitivity.Critical, DocumentationLevel.FullWithReview, EscalationPotential.Critical);

        // Organization
        Add(d, PoliceCatalogAction.OpenOrganizationCase, "Open Organization Case", ActionDomain.Organization, InvasivenessLevel.Medium, LegalitySensitivity.High, DocumentationLevel.Full, EscalationPotential.High);
        Add(d, PoliceCatalogAction.MapOrganizationStructure, "Map Organization Structure", ActionDomain.Organization, InvasivenessLevel.Medium, LegalitySensitivity.High, DocumentationLevel.FullWithReview, EscalationPotential.High);
        Add(d, PoliceCatalogAction.IdentifyKeyTargets, "Identify Key Targets", ActionDomain.Organization, InvasivenessLevel.Medium, LegalitySensitivity.High, DocumentationLevel.FullWithReview, EscalationPotential.High);
        Add(d, PoliceCatalogAction.InfiltrateOrganization, "Infiltrate Organization", ActionDomain.Organization, InvasivenessLevel.Severe, LegalitySensitivity.Critical, DocumentationLevel.FullWithReview, EscalationPotential.Critical);
        Add(d, PoliceCatalogAction.BuildStrategicCase, "Build Strategic Case", ActionDomain.Organization, InvasivenessLevel.Medium, LegalitySensitivity.High, DocumentationLevel.Full, EscalationPotential.High);
        Add(d, PoliceCatalogAction.RaidOrganization, "Raid Organization", ActionDomain.Organization, InvasivenessLevel.Severe, LegalitySensitivity.Critical, DocumentationLevel.FullWithReview, EscalationPotential.Critical);
        Add(d, PoliceCatalogAction.SeizeOrganizationInfrastructure, "Seize Organization Infrastructure", ActionDomain.Organization, InvasivenessLevel.High, LegalitySensitivity.Critical, DocumentationLevel.FullWithReview, EscalationPotential.Critical);
        Add(d, PoliceCatalogAction.RecruitOrgMemberAsSource, "Recruit Organization Member as Source", ActionDomain.Organization, InvasivenessLevel.High, LegalitySensitivity.Critical, DocumentationLevel.FullWithReview, EscalationPotential.Critical);
        Add(d, PoliceCatalogAction.StrategicCapabilitySuppression, "Strategic Capability Suppression", ActionDomain.Organization, InvasivenessLevel.Severe, LegalitySensitivity.Critical, DocumentationLevel.FullWithReview, EscalationPotential.Critical);

        // Internal
        Add(d, PoliceCatalogAction.OpenInternalReview, "Open Internal Review", ActionDomain.Internal, InvasivenessLevel.Medium, LegalitySensitivity.High, DocumentationLevel.Full, EscalationPotential.High);
        Add(d, PoliceCatalogAction.AuditReportInternal, "Audit Report (Internal)", ActionDomain.Internal, InvasivenessLevel.Medium, LegalitySensitivity.High, DocumentationLevel.Full, EscalationPotential.High);
        Add(d, PoliceCatalogAction.AuditForceInternal, "Audit Force Usage (Internal)", ActionDomain.Internal, InvasivenessLevel.High, LegalitySensitivity.Critical, DocumentationLevel.FullWithReview, EscalationPotential.Critical);
        Add(d, PoliceCatalogAction.AuditCorruptionInternal, "Audit Corruption (Internal)", ActionDomain.Internal, InvasivenessLevel.High, LegalitySensitivity.Critical, DocumentationLevel.FullWithReview, EscalationPotential.Critical);
        Add(d, PoliceCatalogAction.AuditLeakInternal, "Audit Leak (Internal)", ActionDomain.Internal, InvasivenessLevel.High, LegalitySensitivity.Critical, DocumentationLevel.FullWithReview, EscalationPotential.Critical);
        Add(d, PoliceCatalogAction.AuditEvidenceInternal, "Audit Evidence (Internal)", ActionDomain.Internal, InvasivenessLevel.High, LegalitySensitivity.Critical, DocumentationLevel.FullWithReview, EscalationPotential.Critical);
        Add(d, PoliceCatalogAction.AuditComplaintInternal, "Audit Complaint (Internal)", ActionDomain.Internal, InvasivenessLevel.Medium, LegalitySensitivity.High, DocumentationLevel.Full, EscalationPotential.High);
        Add(d, PoliceCatalogAction.CoverUpInternal, "Cover-up (Internal)", ActionDomain.Internal, InvasivenessLevel.Severe, LegalitySensitivity.Critical, DocumentationLevel.FullWithReview, EscalationPotential.Critical);
        Add(d, PoliceCatalogAction.ScapegoatJuniorOfficer, "Scapegoat Junior Officer", ActionDomain.Internal, InvasivenessLevel.High, LegalitySensitivity.Critical, DocumentationLevel.FullWithReview, EscalationPotential.Critical);
        Add(d, PoliceCatalogAction.ReassignOfficerStation, "Reassign Officer Station", ActionDomain.Internal, InvasivenessLevel.Medium, LegalitySensitivity.High, DocumentationLevel.Full, EscalationPotential.High);
        Add(d, PoliceCatalogAction.FreezePromotion, "Freeze Promotion", ActionDomain.Internal, InvasivenessLevel.Medium, LegalitySensitivity.High, DocumentationLevel.Full, EscalationPotential.Limited);
        Add(d, PoliceCatalogAction.SuspendOfficer, "Suspend Officer", ActionDomain.Internal, InvasivenessLevel.High, LegalitySensitivity.Critical, DocumentationLevel.FullWithReview, EscalationPotential.High);

        return d;
    }

    private static void Add(
        Dictionary<PoliceCatalogAction, PoliceActionCatalogEntry> d,
        PoliceCatalogAction action,
        string displayNameEn,
        ActionDomain domain,
        InvasivenessLevel invasiveness,
        LegalitySensitivity sensitivity,
        DocumentationLevel docNeed,
        EscalationPotential escalation)
    {
        d[action] = new PoliceActionCatalogEntry
        {
            Action = action,
            DisplayNameEn = displayNameEn,
            Domain = domain,
            Invasiveness = invasiveness,
            Sensitivity = sensitivity,
            DocumentationNeed = docNeed,
            Escalation = escalation
        };
    }
}
