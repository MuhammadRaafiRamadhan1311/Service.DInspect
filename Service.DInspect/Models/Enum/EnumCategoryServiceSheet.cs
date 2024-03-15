namespace Service.DInspect.Models.Enum
{
    public static class EnumCategoryServiceSheet
    {
        #region CBM
        public static string CBM { get { return "CBM"; } }
        public static string CBM_MANUAL { get { return "CBM_MANUAL"; } }
        public static string CBM_ADJUSTMENT { get { return "CBM_ADJUSTMENT"; } }
        public static string CBM_ADJUSTMENT_CARRY_ROLLER { get { return "CBM_ADJUSTMENT_CARRY_ROLLER"; } }
        public static string CBM_NORMAL { get { return "CBM_NORMAL"; } }
        public static string CBM_CALCULATE { get { return "CBM_CALCULATE"; } }
        public static string CBM_CALCULATE_RESULT { get { return "CBM_CALCULATE_RESULT"; } }
        public static string CBM_MANUAL_DOUBLE { get { return "CBM_MANUAL_DOUBLE"; } }
        public static string CBM_DESC { get { return "CBM_DESC"; } }
        public static string CBM_CALIBRATION { get { return "CBM_CALIBRATION"; } }
        public static string CBM_PREVIOUS { get { return "CBM_PREVIOUS"; } }
        #endregion

        #region Defect
        public static string NORMAL_DESC { get { return "NORMAL_DESC"; } }
        public static string Defect { get { return "Defect"; } }
        public static string DEFECT_MEASUREMENT { get { return "DEFECT_MEASUREMENT"; } }
        public static string Defect_Group { get { return "Defect_Group"; } }
        #endregion

        #region Service (Lube Service)
        public static string Service { get { return "Service"; } }
        public static string Service_With_Header { get { return "Service_With_Header"; } }
        public static string Service_Mapping { get { return "Service_Mapping"; } }
        public static string Service_Input { get { return "Service_Input"; } }
        #endregion

        #region Crack
        public static string Crack { get { return "Crack"; } }
        public static string CRACK_SUBTASK { get { return "CRACK_SUBTASK"; } }
        public static string CRACK_NON_GROUP { get { return "CRACK_NON_GROUP"; } }
        public static string INFO_SECTION { get { return "INFO_SECTION"; } }
        #endregion

        public static string Note { get { return "Note"; } }
        public static string SKIP_PRESERVICE { get { return "SKIP_PRESERVICE"; } }
        public static string Section { get { return "Section"; } }

        #region Master Table Category
        public static string NORMAL { get { return "NORMAL"; } }
        public static string NORMAL_WITH_MAPPING { get { return "NORMAL_WITH_MAPPING"; } }
        public static string NORMAL_WITH_COMMENT { get { return "NORMAL_WITH_COMMENT"; } }
        public static string NORMAL_WITH_INPUT { get { return "NORMAL_WITH_INPUT"; } }
        public static string NORMAL_WITH_HEADER { get { return "NORMAL_WITH_HEADER"; } }
        public static string CRACK { get { return "CRACK"; } }
        public static string NOTE { get { return "NOTE"; } }
        public static string TABLE { get { return "TABLE"; } }
        public static string GUIDE { get { return "GUIDE"; } }

        public static string NORMAL_DESC_BORDER { get { return "NORMAL_DESC_BORDER"; } }

        public static string CBM_BRAKE { get { return "CBM_BRAKE"; } }
        #endregion

    }
}
