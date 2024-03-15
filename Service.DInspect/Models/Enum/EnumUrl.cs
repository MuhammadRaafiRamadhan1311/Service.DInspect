using Service.DInspect.Models;

namespace Service.DInspect.Models.Enum
{
    public static class EnumUrl
    {
        public static MySetting appSetting { get; set; }

        #region ADM

        public static string GetPreviousCrackUrl { get { return $"{appSetting.ADMBaseUrl}/api/master_data/daily_schedule/get_history?ver=v1"; } }
        public static string GetAdmServicesheet { get { return $"{appSetting.ADMBaseUrl}/api/master_data/daily_schedule/get_config_service_sheet?ver=v1"; } }
        public static string GetAdmHistoryUrl { get { return $"{appSetting.ADMBaseUrl}/api/master_data/daily_schedule/get_history?ver=v1"; } }
        public static string UpdateStatusDailyScheduleUrl { get { return $"{appSetting.ADMBaseUrl}/api/master_data/daily_schedule/update_status?ver=v1"; } }
        public static string UpdateStatusDailyScheduleInterimUrl { get { return $"{appSetting.ADMBaseUrl}/api/master_data/daily_schedule/update_status_interim?ver=v1"; } }
        public static string GetUoM { get { return $"{appSetting.ADMBaseUrl}/api/master_data/uom?Page=1&PageSize=1000&ver=v1"; } }
        public static string GetEquipmentNumber { get { return $"{appSetting.ADMBaseUrl}/api/master_data/equipment_number"; } }
        public static string GetShift { get { return $"{appSetting.ADMBaseUrl}/api/master_data/shift?Page=1&PageSize=1000&ver=v1"; } }
        public static string GetStatusConverter { get { return $"{appSetting.ADMBaseUrl}/api/master_data/status_converter?Page=1&PageSize=1000&ver=v1"; } }
        public static string GetMasterSos { get { return $"{appSetting.ADMBaseUrl}/api/master_data/sos?Equipment=<<equipment>>&ver=v1"; } }
        public static string GetMasterEquipmentAssignment { get { return $"{appSetting.ADMBaseUrl}/api/master_data/equipment_assignment?Page=1&PageSize=1000&ver=v1"; } }
        public static string GetParameterRating { get { return $"{appSetting.ADMBaseUrl}/api/master_data/parameter_ehms/rating/?ver=v1"; } }
        public static string GetParameterRatingOverwrite { get { return $"{appSetting.ADMBaseUrl}/api/master_data/parameter_ehms/rating/overwrite/?ver=v1"; } }
        public static string GetSiteMapping { get { return $"{appSetting.ADMBaseUrl}/api/master_data/model_site_mapping/?ver=v1&Page=1&PageSize=1000"; } }
        public static string SiteValidation { get { return $"{appSetting.ADMBaseUrl}/api/master_data/model_site_mapping/site_validation?ver=v1"; } }
        #endregion

        #region EHMS

        public static string EHMSBaseUrl { get { return $"{appSetting.EHMSBaseUrl}/api/<<controller>>?ver=v1"; } }
        public static string EHMSGetByIdUrl { get { return $"{appSetting.EHMSBaseUrl}/api/<<controller>>/<<id>>?ver=v1"; } }
        public static string EHMSGetAllByParamUrl { get { return $"{appSetting.EHMSBaseUrl}/api/<<controller>>/get_all_by_param?ver=v1"; } }
        public static string EHMSGetByParamUrl { get { return $"{appSetting.EHMSBaseUrl}/api/<<controller>>/get_by_param?ver=v1"; } }

        public static string EhmsGetInterventionForm { get { return $"{appSetting.EHMSBaseUrl}/api/intervention_header/intervention_forms?KeyPbi=<<keyPbi>>&ver=v1"; } }
        public static string GetSyncInterventionUrl { get { return $"{appSetting.EHMSBaseUrl}/api/intervention/getAllData?Page=1&PageSize=1000&KeyPbi=<<keyPbi>>&ver=v1"; } }
        public static string UpdateEHMSInterventionHeaderUrl { get { return $"{appSetting.EHMSBaseUrl}/api/intervention_header/intervention_forms?userAccount=<<userAccount>>&ver=v1"; } }
        //public static string UpdateEHMSInterventionDetail { get { return $"{appSetting.EHMSBaseUrl}/api/intervention/Intervention/detail?userAccount={EnumCommonProperty.UserAccount}&ver=v1"; } }

        public static string GetInterventionList { get { return $"{appSetting.EHMSBaseUrl}/api/intervention/get_intervention?ver=v1"; } }
        public static string GetTaskTypeCondition { get { return $"{appSetting.EHMSBaseUrl}/api/master_type_condition_assignment/type_condition_assignment?typeTaskId=<<typeTaskId>>&typeTask=<<typeTask>>&ver=v1"; } }
        public static string GetInterventionComponentSystem { get { return $"{appSetting.EHMSBaseUrl}/api/intervention/get_intervention_component_system?ver=v1"; } }
        public static string DeleteInterventionDefect { get { return $"{appSetting.EHMSBaseUrl}/api/intervention_defect/delete_intervention_defect?ver=v1"; } }

        #endregion

        #region Utility
        public static string GetDataEmployeeProfile { get { return $"{appSetting.UtilityBaseUrl}/api/master_employee/get_by_param?ver=v1"; } }
        public static string GetDataEmployeeProfileById { get { return $"{appSetting.UtilityBaseUrl}/api/master_employee/getById"; } }
        public static string GetGeneralFilter { get { return $"{appSetting.UtilityBaseUrl}/api/general_filter/getByGroup"; } }
        public static string GetUserMenu { get { return $"{appSetting.UtilityBaseUrl}/api/master_menu/user_menu_mobile?ver=v1"; } }
        public static string GetFileUrl { get { return $"{appSetting.UtilityBaseUrl}/api/master_attachment/get_url?ver=v1"; } }
        #endregion
    }
}
