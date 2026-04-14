namespace FamilyBusiness.CityGen.Government.Windows
{
    /// <summary>Batch 13: centralized empty / reserved copy for window models (UI binds to these strings).</summary>
    public static class GovernmentWindowPlaceholderCopy
    {
        public const string PersonnelNotYet = "No known personnel yet.";
        public const string PersonnelBody = "Personnel records are not generated in this build.";

        public const string PoliceCasesNotYet = "No known active police cases yet.";
        public const string PoliceCasesBody = "Case tracking is not simulated in this build.";

        public const string FederalCasesNotYet = "No known active federal cases yet.";
        public const string FederalCasesBody = "Federal case tracking is not simulated in this build.";

        public const string PolicePressureTitle = "Pressure overview";
        public const string PolicePressureNotSimulated = "Live pressure simulation is not available yet.";
        public const string FederalInterestTitle = "Federal interest";
        public const string FederalInterestNotSimulated = "Federal interest tracking is not available yet.";

        public const string CourtProceedingsNotYet = "No known active proceedings.";
        public const string CourtProceedingsBody = "Court proceedings are not generated in this build.";

        public const string CourtReservedTitle = "Reserved";
        public const string CourtReservedBody = "This mode is not yet available.";

        public const string ActionNotImplemented = "Not available in this build.";
        public const string SubDepartmentsLater = "Internal departments — not yet populated.";

        public const string NoGovernmentDataTitle = "Government data missing";
        public const string NoGovernmentDataBody = "Refresh extraction (GovernmentDataExtractor.Refresh) after generating the city.";

        public const string NoPoliceFacilitiesTitle = "No entries";
        public const string NoPoliceFacilitiesBody = "No police facilities appear in your crew’s government window yet (intel below rumor).";

        public const string NoFederalFacilitiesTitle = "No entries";
        public const string NoFederalFacilitiesBody = "No federal facilities appear in your crew’s government window yet (intel below rumor).";
    }
}
