namespace Service.DInspect.Models.Enum
{
    public static class EnumFilterOperator
    {
        public static string Equal { get { return "="; } }
        public static string NotEqual { get { return "<>"; } }
        public static string MoreThan { get { return ">"; } }
        public static string MoreThanEqual { get { return ">="; } }
        public static string LessThan { get { return "<"; } }
        public static string LessThanEqual { get { return "<="; } }
    }
}