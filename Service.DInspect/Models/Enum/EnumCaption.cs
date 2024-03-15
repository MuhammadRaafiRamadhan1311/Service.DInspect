using Microsoft.AspNetCore.Mvc.ViewEngines;

namespace Service.DInspect.Models.Enum
{
    public static class EnumCaption
    {
        //public static string RecomendationActions => "Recomendation Actions";
        //public static string AdditionalName { get { return "Additional Tasks"; } }
        public static string InterventionChecks { get { return "Intervention Checks"; } }
        public static string AdditionalTasks { get { return "Additional Tasks"; } }
        public static string DefectIdentify { get { return "Defect Identify"; } }
        public static string System { get { return "sys"; } }
        public static string EForm { get { return "Digital Service Forms"; } }
        public static string DefectReview { get { return "Defect Review"; } }
        public static string DefectReviewAndServiceApproval { get { return "Defect Review & Service Approval"; } }
        public static string Approval { get { return "Approval"; } }
        public static string DefectReviewPlanner { get { return "Defect Review Planner"; } }
        public static string ComponentInterventionForms { get { return "Component Intervention Forms"; } }
        public static string ComponentInterimForms { get { return "Interim Engine Service Forms"; } }
        public static string Timezone { get { return "AEST"; } }

        public static string ServiceSheet { get { return "Service Sheet"; } }
        public static string Interim { get { return "Interim"; } }
        public static string Intervention { get { return "Intervention"; } }
        public static string Required { get { return "Required"; } }
        public static string NoneValue { get { return "-"; } }
        public static string Adjustment { get { return "Adjustment"; } }
        public static string Replacement { get { return "Replacement"; } }
        public static string DefectHeaders { get { return "DefectHeaders"; } }
        public static string Comments { get { return "Comments"; } }
        public static string FormVersion { get { return "Form Version"; } }
        public static string LinkDocumentation { get { return "https://bukittechnology.atlassian.net/wiki/spaces/BAA/pages/183566350/Generate+Service+Sheet"; } }
        public static string FormulaGuid { get { return "=LOWER(CONCATENATE(DEC2HEX(RANDBETWEEN(0;POWER(16;8));8);\"-\";DEC2HEX(RANDBETWEEN(0;POWER(16;4));4);\"-\";\"4\";DEC2HEX(RANDBETWEEN(0;POWER(16;3));3);\"-\";DEC2HEX(RANDBETWEEN(8;11));DEC2HEX(RANDBETWEEN(0;POWER(16;3));3);\"-\";DEC2HEX(RANDBETWEEN(0;POWER(16;8));8);DEC2HEX(RANDBETWEEN(0;POWER(16;4));4)))"; } }
    }
}