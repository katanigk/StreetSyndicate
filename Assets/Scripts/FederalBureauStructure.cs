/// <summary>
/// Federal “Bureau” (Central Serious Crime Unit) ranks and deputy portfolios.
/// Distinct from <see cref="PoliceRank"/> / local police; use this for national-security law and future federal resolvers.
/// Terminology: do not use local-police “detective” for this org — use Field / Senior / Special / Supervising Special Agent, Unit Chief, Deputy Director, Director.
/// </summary>
public enum FederalBureauRank
{
    FieldAgent = 1,
    SeniorFieldAgent = 2,
    SpecialAgent = 3,
    SupervisingSpecialAgent = 4,
    UnitChief = 5,
    DeputyDirector = 6,
    DirectorOfCentralUnit = 7
}

/// <summary>Which Deputy Director line a rank-6 officer holds (rank 7 = Director has no portfolio).</summary>
public enum FederalDeputyPortfolio
{
    None = 0,
    Operations = 1,
    Intelligence = 2,
    BudgetFacilitiesLogistics = 3,
    PoliticalLegal = 4
}

/// <summary>Rules from the National Security Act — authorized vs senior command, and portfolio hooks.</summary>
public static class FederalBureauStructure
{
    /// <summary>Default: Supervising Special Agent or higher, unless a written, time-limited mission order says otherwise.</summary>
    public static bool IsAuthorizedOfficer(FederalBureauRank rank, bool hasWrittenMissionAuthorization)
    {
        if (rank >= FederalBureauRank.SupervisingSpecialAgent)
            return true;
        return hasWrittenMissionAuthorization;
    }

    public static bool IsSeniorCommandOfficer(FederalBureauRank rank)
    {
        return rank >= FederalBureauRank.UnitChief;
    }

    public static bool IsDeputyDirector(FederalBureauRank rank, FederalDeputyPortfolio portfolio)
    {
        return rank == FederalBureauRank.DeputyDirector && portfolio != FederalDeputyPortfolio.None;
    }

    /// <summary>Major budget / new facility / classified fund line — only the Budget deputy (or Director override in policy layer).</summary>
    public static bool IsBudgetSystemGate(FederalBureauRank rank, FederalDeputyPortfolio portfolio)
    {
        if (rank == FederalBureauRank.DirectorOfCentralUnit)
            return true;
        return rank == FederalBureauRank.DeputyDirector && portfolio == FederalDeputyPortfolio.BudgetFacilitiesLogistics;
    }

    public static string ToDisplayName(FederalBureauRank r)
    {
        return r switch
        {
            FederalBureauRank.FieldAgent => "Field Agent",
            FederalBureauRank.SeniorFieldAgent => "Senior Field Agent",
            FederalBureauRank.SpecialAgent => "Special Agent",
            FederalBureauRank.SupervisingSpecialAgent => "Supervising Special Agent",
            FederalBureauRank.UnitChief => "Unit Chief",
            FederalBureauRank.DeputyDirector => "Deputy Director",
            FederalBureauRank.DirectorOfCentralUnit => "Director of the Central Unit",
            _ => "Field Agent"
        };
    }

    public static string PortfolioDisplayName(FederalDeputyPortfolio p)
    {
        return p switch
        {
            FederalDeputyPortfolio.Operations => "Deputy Director for Operations",
            FederalDeputyPortfolio.Intelligence => "Deputy Director for Intelligence",
            FederalDeputyPortfolio.BudgetFacilitiesLogistics => "Deputy Director for Budget, Facilities and Logistics",
            FederalDeputyPortfolio.PoliticalLegal => "Deputy Director for Political and Legal Affairs",
            _ => string.Empty
        };
    }

    /// <summary>Canonical division keys for the eight Bureau line units. Director and Deputy Directors are not placed in any division (their <c>divisionId</c> is null or empty).</summary>
    public static class FederalBureauDivisionIds
    {
        public const string OrganizedCrime = "div_organized_crime";
        public const string ProhibitionSubstances = "div_prohibition_substances";
        public const string Intelligence = "div_intelligence";
        public const string Operations = "div_operations";
        public const string FacilitiesLogistics = "div_facilities_logistics";
        public const string PoliticalLegal = "div_political_legal";
        public const string InternalControl = "div_internal_control";
        public const string StrategicCases = "div_strategic_cases";
    }

    /// <summary>In-world org tier: crime family threshold (placeholder until org tier table is provided).</summary>
    public const int CrimeFamilyMinimumOrgTier = 4;

    public static bool IsCrimeFamilyTier(int orgTier)
    {
        return orgTier >= CrimeFamilyMinimumOrgTier;
    }
}
