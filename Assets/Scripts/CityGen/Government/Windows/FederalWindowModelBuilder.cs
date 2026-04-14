using System.Collections.Generic;
using FamilyBusiness.CityGen.Data;

namespace FamilyBusiness.CityGen.Government.Windows
{
    /// <summary>Batch 13: shapes <see cref="CityData.GovernmentData"/> into federal window models.</summary>
    public static class FederalWindowModelBuilder
    {
        public static FederalWindowStateModel Build(CityData city, FederalWindowMode mode, string selectedItemId = null)
        {
            var model = new FederalWindowStateModel { ActiveMode = mode };

            if (city?.GovernmentData == null)
            {
                model.UsesCenterPlaceholder = true;
                model.CenterFallbackTitle = GovernmentWindowPlaceholderCopy.NoGovernmentDataTitle;
                model.CenterFallbackBody = GovernmentWindowPlaceholderCopy.NoGovernmentDataBody;
                return model;
            }

            switch (mode)
            {
                case FederalWindowMode.Deployment:
                    FillDeployment(city, model, selectedItemId);
                    break;
                case FederalWindowMode.Personnel:
                    FillPersonnelPlaceholder(model);
                    break;
                case FederalWindowMode.Cases:
                    FillCasesPlaceholder(model);
                    break;
                case FederalWindowMode.Interest:
                    FillInterestPlaceholder(city, model);
                    break;
            }

            return model;
        }

        static void FillDeployment(CityData city, FederalWindowStateModel model, string selectedItemId)
        {
            IReadOnlyList<GovernmentFacilityData> facilities =
                city.GovernmentData.GetIntelEligibleFacilities(GovernmentSystemKind.Federal);

            if (facilities.Count == 0)
            {
                model.UsesCenterPlaceholder = true;
                model.CenterFallbackTitle = GovernmentWindowPlaceholderCopy.NoFederalFacilitiesTitle;
                model.CenterFallbackBody = GovernmentWindowPlaceholderCopy.NoFederalFacilitiesBody;
                return;
            }

            string pick = selectedItemId;
            GovernmentFacilityData sel =
                city.GovernmentData.GetFacilityByInstitutionId(ParseInstOrNeg(pick ?? string.Empty));
            if (string.IsNullOrEmpty(pick) || sel == null || sel.GovernmentSystem != GovernmentSystemKind.Federal)
                pick = GovernmentWindowBuilderShared.StableInstitutionId(facilities[0].SourceInstitutionId);

            for (int i = 0; i < facilities.Count; i++)
            {
                GovernmentFacilityData f = facilities[i];
                string sid = GovernmentWindowBuilderShared.StableInstitutionId(f.SourceInstitutionId);
                model.LeftItems.Add(GovernmentWindowBuilderShared.ToFacilityListItem(city, f, GovernmentSystemKind.Federal,
                    sid == pick));
            }

            model.SelectedItemId = pick;
            int selId = ParseInstOrNeg(pick);
            GovernmentFacilityData selected = city.GovernmentData.GetFacilityByInstitutionId(selId);
            if (selected != null)
                model.DeploymentDetail = GovernmentWindowBuilderShared.BuildDeploymentDetail(city, selected);

            foreach (GovernmentWindowActionModel a in GovernmentWindowBuilderShared.StandardCrewActionPlaceholders())
                model.RightActions.Add(a);
        }

        static int ParseInstOrNeg(string stableId) =>
            GovernmentWindowBuilderShared.TryParseInstitutionStableId(stableId, out int id) ? id : -1;

        static void FillPersonnelPlaceholder(FederalWindowStateModel model)
        {
            model.UsesCenterPlaceholder = true;
            model.CenterFallbackTitle = GovernmentWindowPlaceholderCopy.PersonnelNotYet;
            model.CenterFallbackBody = GovernmentWindowPlaceholderCopy.PersonnelBody;
        }

        static void FillCasesPlaceholder(FederalWindowStateModel model)
        {
            model.UsesCenterPlaceholder = true;
            model.CenterFallbackTitle = GovernmentWindowPlaceholderCopy.FederalCasesNotYet;
            model.CenterFallbackBody = GovernmentWindowPlaceholderCopy.FederalCasesBody;
        }

        static void FillInterestPlaceholder(CityData city, FederalWindowStateModel model)
        {
            model.UsesCenterPlaceholder = true;
            model.CenterFallbackTitle = GovernmentWindowPlaceholderCopy.FederalInterestTitle;
            int known = city.GovernmentData.GetKnownFacilities(GovernmentSystemKind.Federal).Count;
            int intel = city.GovernmentData.GetIntelEligibleFacilities(GovernmentSystemKind.Federal).Count;
            model.CenterFallbackBody =
                $"{GovernmentWindowPlaceholderCopy.FederalInterestNotSimulated}\n\nOffices at Known intel or better: {known}.\nOffices at rumor+ (window-eligible): {intel}.";
        }
    }
}
