namespace Service.DInspect.Models.Enum
{
    public static class EnumErrorMessage
    {
        public static string ErrMsg400 { get { return "Please contact your administrator!"; } }
        public static string ErrMsgTaskType { get { return "Task type undefined!"; } }
        public static string ErrMsgTaskValue { get { return "Task value undefined!"; } }
        public static string ErrMsgDefectWorOrder { get { return "WO number has been inputted by <<userName>>"; } }
        public static string ErrMsgDefectReview { get { return "Defect Information has been <<status>>d by Supervisor"; } }
        public static string ErrMsgDefectApproval { get { return "Defect has been <<status>>d by <<userName>>"; } }
        public static string ErrMsgTaskCrackApproval { get { return "Task crack has been <<status>>d by <<userName>>"; } }
        public static string ErrMsgTaskNAApproval { get { return "Not applicable task has been <<status>>d by <<userName>>"; } }
        public static string ErrMsgMachineSMUApproval { get { return "Machine SMU has been <<status>>d by <<userName>>"; } }
        public static string ErrMsgCompartmentOilSample { get { return "No sampling was carried out"; } }
        public static string ErrMsgCompartmentOilSampleIsnull { get { return "No data sample for this compartment"; } }
        public static string ErrMsgLubricantMapping { get { return "Lube Recommendation for <<modelUnitId>> and service size <<psType>> is not configured yet, contact your system administrator!"; } }
        public static string ErrMsgLubricantMappingSuckAndBlow { get { return "Lube Recommendation for <<modelUnitId>> is not configured yet, contact your system administrator!"; } }
        public static string ErrMsgServiceSheetDefectHeaderApproval { get { return "You cannot approve this digital service sheet because <<approvedBy>> already approved"; } }
        public static string ErrMsgInterimDefectHeaderApproval { get { return "You cannot approve this interim service sheet because <<approvedBy>> already approved"; } }
        public static string ErrMsgInterventionDefectHeaderApproval { get { return "You cannot approve this intervention service sheet because <<approvedBy>> already approved"; } }
    }
}
