using Service.DInspect.Models;

namespace Service.DInspect.Models.Enum
{
    public static class EnumFormatting
    {
        public static MySetting appSetting { get; set; }
        public static string appTimeZoneDesc { get; set; }

        public static string DateToString { get { return "dd/MM/yy"; } }
        public static string DateToFullString24 { get { return "dd/MM/yy HH:mm:ss"; } }
        public static string DateToFullString12 { get { return "dd/MM/yy hh:mm:ss tt"; } }
        public static string Time12 { get { return "hh:mm:ss tt"; } }

        public static string DateTimeToString
        {
            get
            {
                string timeZoneDesc = string.IsNullOrEmpty(appTimeZoneDesc) ? string.Empty : $" ({appTimeZoneDesc})";
                return $"dd/MM/yy HH:mm:ss{timeZoneDesc}";
            }
        }


        public static string DefaultDateTimeToString { get { return "yyyy-MM-dd HH:mm:ss"; } }
        public static string AestDateTimeFormat { get { return "dd/MM/yy HH:mm:ss (AEST)"; } }
    }
}
