namespace Service.DInspect.Models.Enum
{
    public class EnumPreviousTaskType
    {
        public static string Tandem { get { return "Tandem"; } }
        public static string Replacement { get { return "Replacement"; } }
        public static string ReplacementGap { get { return "ReplacementGap"; } }

        public static string TandemRating { get { return "AUTOMATIC_PREVIOUS"; } }
        public static string ReplacementRating { get { return "AUTOMATIC_REPLACEMENT"; } }
        public static string ReplacementGapRating { get { return "AUTOMATIC_REPLACEMENT_GAP"; } }
    }
}
