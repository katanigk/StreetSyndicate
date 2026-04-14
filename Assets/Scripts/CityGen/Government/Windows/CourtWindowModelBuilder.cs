using FamilyBusiness.CityGen.Data;

namespace FamilyBusiness.CityGen.Government.Windows
{
    /// <summary>Batch 13: court window placeholders until proceedings/personnel pipelines exist.</summary>
    public static class CourtWindowModelBuilder
    {
        public static CourtWindowStateModel Build(CityData city, CourtWindowMode mode, string selectedItemId = null)
        {
            _ = selectedItemId;

            if (city == null || city.GovernmentData == null)
            {
                return new CourtWindowStateModel
                {
                    ActiveMode = mode,
                    UsesCenterPlaceholder = true,
                    CenterFallbackTitle = GovernmentWindowPlaceholderCopy.NoGovernmentDataTitle,
                    CenterFallbackBody = GovernmentWindowPlaceholderCopy.NoGovernmentDataBody
                };
            }

            var model = new CourtWindowStateModel
            {
                ActiveMode = mode,
                UsesCenterPlaceholder = true,
                IsReservedSlotMode = mode is CourtWindowMode.Reserved1 or CourtWindowMode.Reserved2
            };

            switch (mode)
            {
                case CourtWindowMode.Proceedings:
                    model.CenterFallbackTitle = GovernmentWindowPlaceholderCopy.CourtProceedingsNotYet;
                    model.CenterFallbackBody = GovernmentWindowPlaceholderCopy.CourtProceedingsBody;
                    break;
                case CourtWindowMode.Personnel:
                    model.CenterFallbackTitle = GovernmentWindowPlaceholderCopy.PersonnelNotYet;
                    model.CenterFallbackBody = GovernmentWindowPlaceholderCopy.PersonnelBody;
                    break;
                default:
                    model.CenterFallbackTitle = GovernmentWindowPlaceholderCopy.CourtReservedTitle;
                    model.CenterFallbackBody = GovernmentWindowPlaceholderCopy.CourtReservedBody;
                    break;
            }

            return model;
        }
    }
}
