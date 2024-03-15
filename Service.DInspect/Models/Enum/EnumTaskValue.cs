namespace Service.DInspect.Models.Enum
{
    public static class EnumTaskValue
    {
        public static string NormalOK { get { return "1"; } }
        public static string NormalNotOK { get { return "2"; } }
        public static string NormalNA { get { return "3"; } }

        public static string IntNormalOK { get { return "1"; } }
        public static string IntNormalCompleted { get { return "2"; } }
        public static string IntNormalNotOK { get { return "3"; } }
        public static string IntNormalNA { get { return "4"; } }

        public static string CbmA { get { return "A"; } }
        public static string CbmB { get { return "B"; } }
        public static string CbmC { get { return "C"; } }
        public static string CbmX { get { return "X"; } }

        public static string CrackOK { get { return "1"; } }
        public static string CrackNotOKYes { get { return "2"; } }
        public static string CrackNotOKNo { get { return "3"; } }
        public static string CrackNA { get { return "4"; } }

        public static string CalibrationComplete { get { return "true"; } }

        public static string CalibrationYes { get { return "2"; } }
        public static string CalibrationNo { get { return "1"; } }

        public static string InSpec { get { return "In spec"; } }
        public static string OutOfSpec { get { return "Out of spec"; } }

        public static string FeulType { get { return "Diesel"; } }
    }
}
