using System.Collections.Generic;
using FamilyBusiness.CityGen.Data;

namespace FamilyBusiness.CityGen.Government.Windows
{
    /// <summary>Batch 13: shapes <see cref="CityData.GovernmentData"/> into police window models.</summary>
    public static class PoliceWindowModelBuilder
    {
        public static PoliceWindowStateModel Build(CityData city, PoliceWindowMode mode, string selectedItemId = null)
        {
            var model = new PoliceWindowStateModel { ActiveMode = mode };

            if (city?.GovernmentData == null)
            {
                model.UsesCenterPlaceholder = true;
                model.CenterFallbackTitle = GovernmentWindowPlaceholderCopy.NoGovernmentDataTitle;
                model.CenterFallbackBody = GovernmentWindowPlaceholderCopy.NoGovernmentDataBody;
                return model;
            }

            switch (mode)
            {
                case PoliceWindowMode.Deployment:
                    FillDeployment(city, model, selectedItemId);
                    break;
                case PoliceWindowMode.Personnel:
                    FillPersonnelPlaceholder(model);
                    break;
                case PoliceWindowMode.Cases:
                    FillCasesPlaceholder(model);
                    break;
                case PoliceWindowMode.Pressure:
                    FillPressurePlaceholder(city, model);
                    break;
            }

            return model;
        }

        static void FillDeployment(CityData city, PoliceWindowStateModel model, string selectedItemId)
        {
            IReadOnlyList<GovernmentFacilityData> facilities =
                city.GovernmentData.GetIntelEligibleFacilities(GovernmentSystemKind.Police);

            if (facilities.Count == 0)
            {
                model.UsesCenterPlaceholder = true;
                model.CenterFallbackTitle = GovernmentWindowPlaceholderCopy.NoPoliceFacilitiesTitle;
                model.CenterFallbackBody = GovernmentWindowPlaceholderCopy.NoPoliceFacilitiesBody;
                return;
            }

            string pick = selectedItemId;
            GovernmentFacilityData sel =
                city.GovernmentData.GetFacilityByInstitutionId(ParseInstOrNeg(pick ?? string.Empty));
            if (string.IsNullOrEmpty(pick) || sel == null || sel.GovernmentSystem != GovernmentSystemKind.Police)
                pick = GovernmentWindowBuilderShared.StableInstitutionId(facilities[0].SourceInstitutionId);

            for (int i = 0; i < facilities.Count; i++)
            {
                GovernmentFacilityData f = facilities[i];
                string sid = GovernmentWindowBuilderShared.StableInstitutionId(f.SourceInstitutionId);
                model.LeftItems.Add(GovernmentWindowBuilderShared.ToFacilityListItem(city, f, GovernmentSystemKind.Police,
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

        static void FillPersonnelPlaceholder(PoliceWindowStateModel model)
        {
            model.UsesCenterPlaceholder = true;
            model.CenterFallbackTitle = GovernmentWindowPlaceholderCopy.PersonnelNotYet;
            model.CenterFallbackBody = GovernmentWindowPlaceholderCopy.PersonnelBody;
        }

        static void FillCasesPlaceholder(PoliceWindowStateModel model)
        {
            model.UsesCenterPlaceholder = true;
            model.CenterFallbackTitle = GovernmentWindowPlaceholderCopy.PoliceCasesNotYet;
            model.CenterFallbackBody = GovernmentWindowPlaceholderCopy.PoliceCasesBody;
        }

        static void FillPressurePlaceholder(CityData city, PoliceWindowStateModel model)
        {
            model.UsesCenterPlaceholder = true;
            model.CenterFallbackTitle = GovernmentWindowPlaceholderCopy.PolicePressureTitle;
            int known = city.GovernmentData.GetKnownFacilities(GovernmentSystemKind.Police).Count;
            int intel = city.GovernmentData.GetIntelEligibleFacilities(GovernmentSystemKind.Police).Count;
            model.CenterFallbackBody =
                $"{GovernmentWindowPlaceholderCopy.PolicePressureNotSimulated}\n\nStations at Known intel or better: {known}.\nStations at rumor+ (window-eligible): {intel}.";
        }
    }
}
