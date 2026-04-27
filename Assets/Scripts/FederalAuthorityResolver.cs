using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resolves whether an action is allowed for the <em>officer</em> passed in (rank, portfolio, doctrine). For now, <see cref="FederalActionRequest.requestingAgentId"/> is only the requesting agent id on the request envelope — there is <strong>no</strong> full split yet between requester, approver, and executors. The next step is <c>FederalWorkflowOrchestrator</c> (request / approve / execute / accountability on violations).
/// </summary>

public enum FederalActionType
{
    OpenFederalCase = 0,
    OpenStrategicCase = 1,
    TakeOverPoliceCase = 2,
    AccessPoliceCase = 3,
    AccessPoliceEvidence = 4,
    ShortSurveillance = 5,
    ExtendedSurveillance = 6,
    RecruitSource = 7,
    HandleSource = 8,
    DeepCoverInsertion = 9,
    UseSafeHouse = 10,
    CreateRegisteredFacility = 11,
    UseClassifiedFund = 12,
    UseFederalBlackCash = 13,
    FederalSearch = 14,
    FederalArrest = 15,
    FederalRaid = 16,
    Wiretap = 17,
    LethalForce = 18,
    PoliticalSensitiveAction = 19,
    SecurityThreatRemoval = 20,
    ExtractOperatedSourceFromCustody = 21
}

public enum FederalBureauDocLevel
{
    None = 0,
    Basic = 1,
    Full = 2,
    Classified = 3
}

[Serializable]
public struct FederalActionRequest
{
    public FederalActionType actionType;
    /// <summary>Who is asking (envelope). Not yet separate from approver/executor — see <see cref="FederalAuthorityResolver"/> summary.</summary>
    public string requestingAgentId;
    public int targetType;
    public string targetId;
    public string caseId;
    public string facilityId;
    public int requestedBudgetMinor;
    public bool requiresSecrecy;
    public bool emergencyClaimed;
    public bool hasWarrant;
    public bool hasPoliceAccessLog;
    public bool hasTakeoverLog;
    public bool isLargeScaleOp;
    public bool targetIsPublicFigure;
}

[Serializable]
public struct FederalAuthorityResolution
{
    public bool isAllowed;
    public FederalBureauRank requiredRankMin;
    public FederalDeputyPortfolio requiredPortfolio;
    public bool requiresWarrant;
    public bool requiresDeputyApproval;
    public bool requiresDirectorApproval;
    public List<string> violationFlags;
    public FederalBureauDocLevel documentationLevel;
    public string summary;
}

public static class FederalAuthorityResolver
{
    const int SignificantBudgetMinor = 25_000;

    public static FederalAuthorityResolution Resolve(FederalActionRequest req, FederalAgentProfile agent)
    {
        var v = new List<string>();
        var res = new FederalAuthorityResolution
        {
            isAllowed = true,
            requiredRankMin = FederalBureauRank.FieldAgent,
            requiredPortfolio = FederalDeputyPortfolio.None,
            requiresWarrant = false,
            requiresDeputyApproval = false,
            requiresDirectorApproval = false,
            violationFlags = v,
            documentationLevel = FederalBureauDocLevel.Basic,
            summary = "Allowed."
        };

        if (agent == null)
        {
            return Deny("No agent profile.", v, FederalBureauRank.FieldAgent);
        }

        FederalBureauRank r = agent.rank;
        FederalDeputyPortfolio p = agent.deputyPortfolio;

        bool isDepOp = p == FederalDeputyPortfolio.Operations;
        bool isDepIntel = p == FederalDeputyPortfolio.Intelligence;
        bool isDepBudget = p == FederalDeputyPortfolio.BudgetFacilitiesLogistics;
        bool isDepPol = p == FederalDeputyPortfolio.PoliticalLegal;
        bool isDirector = r == FederalBureauRank.DirectorOfCentralUnit;
        bool isDeputy = r == FederalBureauRank.DeputyDirector;
        bool isUnitChief = r == FederalBureauRank.UnitChief;
        bool isSSA = r == FederalBureauRank.SupervisingSpecialAgent;
        bool isSpec = r >= FederalBureauRank.SpecialAgent;
        bool isCoreCommand = isUnitChief || (isDeputy && p != FederalDeputyPortfolio.None) || isDirector;

        bool Meets(FederalBureauRank min) => (int)r >= (int)min;

        switch (req.actionType)
        {
            case FederalActionType.UseFederalBlackCash:
                v.Add("BlackCashProhibited");
                return Deny("Use of black cash is never statutorily authorized; log as violation if it occurs in play.", v, FederalBureauRank.FieldAgent, doc: FederalBureauDocLevel.Classified);

            case FederalActionType.Wiretap:
                res.requiresWarrant = true;
                if (!req.hasWarrant)
                {
                    v.Add("WiretapWithoutWarrant");
                    return Deny("Wiretap requires a warrant (or national equivalent).", v, FederalBureauRank.SupervisingSpecialAgent, warrant: true);
                }
                if (!Meets(FederalBureauRank.SupervisingSpecialAgent))
                    return Deny("Wiretap needs at least Supervising Special Agent to execute as approved tasking.", v, FederalBureauRank.SupervisingSpecialAgent, warrant: true);
                res.documentationLevel = FederalBureauDocLevel.Classified;
                res.summary = "Wiretap with warrant: documentation classified.";
                return res;

            case FederalActionType.CreateRegisteredFacility:
                if (!(isDirector || (isDeputy && isDepBudget)))
                {
                    v.Add("CreateFacilityRequiresBudgetDeputyOrDirector");
                    return Deny("Creating a registered facility requires Deputy Director (Budget) or Director.", v, FederalBureauRank.DeputyDirector);
                }
                res.documentationLevel = FederalBureauDocLevel.Full;
                return res;

            case FederalActionType.UseClassifiedFund:
                if (req.requestedBudgetMinor >= SignificantBudgetMinor && !(isDirector || (isDeputy && isDepBudget)))
                {
                    v.Add("ClassifiedFundMajorRequiresBudgetDeputy");
                    return Deny("Significant spend from the classified fund requires Budget deputy or Director.", v, FederalBureauRank.DeputyDirector);
                }
                if (Meets(FederalBureauRank.SupervisingSpecialAgent) && req.requestedBudgetMinor < SignificantBudgetMinor)
                {
                    res.summary = "Limited use from existing allocated classified line.";
                    res.documentationLevel = FederalBureauDocLevel.Classified;
                    return res;
                }
                if (!Meets(FederalBureauRank.SupervisingSpecialAgent))
                    return Deny("Insufficient rank for fund access even at low amounts.", v, FederalBureauRank.SupervisingSpecialAgent);
                return res;

            case FederalActionType.AccessPoliceCase:
            case FederalActionType.AccessPoliceEvidence:
                if (!req.hasPoliceAccessLog)
                {
                    v.Add("MissingPoliceAccessLog");
                    return Deny("Access to local police file/evidence must be logged (station access register).", v, FederalBureauRank.SpecialAgent);
                }
                if (!isSpec)
                    return Deny("Field agents cannot solo-access police case materials.", v, FederalBureauRank.SpecialAgent);
                res.documentationLevel = req.actionType == FederalActionType.AccessPoliceEvidence ? FederalBureauDocLevel.Full : FederalBureauDocLevel.Basic;
                return res;

            case FederalActionType.TakeOverPoliceCase:
                if (!req.hasTakeoverLog)
                {
                    v.Add("MissingTakeoverLog");
                    return Deny("Takeover requires a logged takeover event.", v, FederalBureauRank.UnitChief);
                }
                if (!Meets(FederalBureauRank.UnitChief))
                {
                    v.Add("TakeoverRankInsufficient");
                    return Deny("Takeover requires at least Unit Chief; sensitive takeovers need Operations deputy.", v, FederalBureauRank.UnitChief);
                }
                if (req.isLargeScaleOp && !(isDepOp || isDirector))
                {
                    v.Add("SensitiveTakeoverRequiresOpsDeputy");
                    return Deny("Politically or tactically large takeover may require Operations deputy.", v, FederalBureauRank.DeputyDirector);
                }
                return res;

            case FederalActionType.DeepCoverInsertion:
                if (!Meets(FederalBureauRank.UnitChief) && !((isDeputy && isDepIntel) || isDirector))
                {
                    v.Add("DeepCoverRequiresUnitChief");
                    return Deny("Deep cover must be approved at least at Unit Chief; sensitive runs need Intelligence chain.", v, FederalBureauRank.UnitChief);
                }
                res.documentationLevel = FederalBureauDocLevel.Classified;
                return res;

            case FederalActionType.PoliticalSensitiveAction:
                if (!(isDirector || (isDeputy && isDepPol)))
                {
                    v.Add("PoliticalSensitivityRequiresDeputy");
                    if (req.targetIsPublicFigure)
                        v.Add("PublicFigure");
                    return Deny("Press- or politics-sensitive action requires the Political & Legal deputy or the Director.", v, FederalBureauRank.DeputyDirector);
                }
                if (req.targetIsPublicFigure && !isDirector && !(isDeputy && isDepPol))
                    return Deny("A public-figure line requires the Political & Legal channel at minimum, often the Director for cross-branch exposure.", v, FederalBureauRank.DeputyDirector);
                return res;

            case FederalActionType.FederalRaid:
                if (req.isLargeScaleOp && !(isDepOp || isDirector))
                {
                    v.Add("LargeRaidRequiresOpsDeputy");
                    return Deny("Wide/large raid requires Operations deputy or Director.", v, FederalBureauRank.DeputyDirector);
                }
                if (!isUnitChief && !isCoreCommand)
                    return Deny("Base raid / strike lead requires at least Unit Chief (or above).", v, FederalBureauRank.UnitChief);
                return res;

            case FederalActionType.OpenStrategicCase:
                if (!isDirector)
                {
                    v.Add("StrategicCaseRequiresDirector");
                    return Deny("Strategic / system-wide case opening requires the Director (or an explicit future delegate rule).", v, FederalBureauRank.DirectorOfCentralUnit, director: true);
                }
                return res;

            case FederalActionType.LethalForce:
                if (!isCoreCommand)
                    return Deny("Lethal authorization chain requires command-grade review.", v, FederalBureauRank.SupervisingSpecialAgent);
                res.documentationLevel = FederalBureauDocLevel.Classified;
                res.summary = "Lethal: mandatory classified AAR.";
                return res;

            case FederalActionType.SecurityThreatRemoval:
                // Distinct from field self-defense lethal force:
                // requires Director line + judicial order (represented as warrant in v1 workflow flags).
                res.requiresDirectorApproval = true;
                res.requiresWarrant = true;
                res.documentationLevel = FederalBureauDocLevel.Classified;
                if (!isDirector)
                    return Deny("Security threat removal requires Director written approval.", v, FederalBureauRank.DirectorOfCentralUnit, warrant: true, director: true, doc: FederalBureauDocLevel.Classified);
                if (!req.hasWarrant)
                {
                    v.Add("SecurityThreatRemovalWithoutJudicialOrder");
                    return Deny("Security threat removal requires final judicial order.", v, FederalBureauRank.DirectorOfCentralUnit, warrant: true, director: true, doc: FederalBureauDocLevel.Classified);
                }
                res.summary = "Security threat removal: director + judicial order + classified packet.";
                return res;

            case FederalActionType.ExtractOperatedSourceFromCustody:
                // Authority to pull an operated source from other custody: command-level and sealed documentation.
                if (!Meets(FederalBureauRank.UnitChief) && !isDirector && !(isDeputy && isDepPol))
                    return Deny("Extracting an operated source from custody requires Unit Chief+, preferably Political/Legal chain.", v, FederalBureauRank.UnitChief, doc: FederalBureauDocLevel.Classified);
                res.documentationLevel = FederalBureauDocLevel.Classified;
                res.summary = "Operated source extraction from external custody.";
                return res;

            case FederalActionType.ExtendedSurveillance:
                if (!isSSA && !isUnitChief && !isCoreCommand)
                    return Deny("Long surveillance must be green-lit by a Supervising+ authorized officer.", v, FederalBureauRank.SupervisingSpecialAgent);
                return res;

            case FederalActionType.ShortSurveillance:
            case FederalActionType.RecruitSource:
            case FederalActionType.HandleSource:
                if (!isSpec)
                    return Deny("Field-only agents can observe; execution/lead of source ops is Special Agent+.", v, FederalBureauRank.SpecialAgent);
                return res;

            case FederalActionType.FederalArrest:
            case FederalActionType.FederalSearch:
                if (!isSpec)
                    return Deny("Search/arrest in federal name requires at least Special Agent, with SSA for extended search packages.", v, FederalBureauRank.SpecialAgent);
                if (req.actionType == FederalActionType.FederalSearch && req.isLargeScaleOp && !isSSA)
                    return Deny("Extended/wide search package needs Supervising+ or Unit Chief per doctrine.", v, FederalBureauRank.SupervisingSpecialAgent);
                return res;

            case FederalActionType.UseSafeHouse:
                if (!isSSA && !isUnitChief && !isCoreCommand)
                    return Deny("Use of a registered safe house must be on an authorized officer’s docket (SSA+ in typical doctrine).", v, FederalBureauRank.SupervisingSpecialAgent);
                return res;

            case FederalActionType.OpenFederalCase:
                if (!isUnitChief && !isCoreCommand)
                    return Deny("Opening a federal case file of substance requires at least a Unit Chief or a deputy’s portfolio line.", v, FederalBureauRank.UnitChief);
                return res;

            default:
                if (!isSpec)
                    return Deny("Unspecified action: minimum Special Agent to lead.", v, FederalBureauRank.SpecialAgent);
                return res;
        }
    }

    static FederalAuthorityResolution Deny(
        string summary,
        List<string> flags,
        FederalBureauRank min,
        bool warrant = false,
        bool director = false,
        FederalBureauDocLevel doc = FederalBureauDocLevel.Basic)
    {
        return new FederalAuthorityResolution
        {
            isAllowed = false,
            requiredRankMin = min,
            requiredPortfolio = FederalDeputyPortfolio.None,
            requiresWarrant = warrant,
            requiresDeputyApproval = (int)min >= (int)FederalBureauRank.DeputyDirector,
            requiresDirectorApproval = director,
            violationFlags = flags,
            documentationLevel = doc,
            summary = summary
        };
    }
}
