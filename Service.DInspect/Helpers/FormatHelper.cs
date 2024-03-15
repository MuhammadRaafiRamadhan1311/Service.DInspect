using Service.DInspect.Models.Enum;
using System;
using System.Globalization;

namespace Service.DInspect.Helpers
{
    public static class FormatHelper
    {
        public static DateTime ConvertToDateTime24(string formDateTime)
        {
            DateTime result = DateTime.ParseExact(formDateTime.Replace($" ({EnumFormatting.appTimeZoneDesc})", string.Empty), EnumFormatting.DateToFullString24, null);
            return result;
        }
        public static DateTime ConvertToDateTime12(string formDateTime)
        {
            DateTime result = DateTime.ParseExact(formDateTime, EnumFormatting.DateToFullString12, null);
            return result;
        }

        public static DateTime ConvertToDate(string formDateTime)
        {
            DateTime result = DateTime.ParseExact(formDateTime, EnumFormatting.DateToString, null);
            return result;
        }
    }
}