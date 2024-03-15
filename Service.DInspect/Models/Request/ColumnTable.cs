namespace Service.DInspect.Models.Request
{
    public class ColumnTable
    {
        public string key { get; set; }
        public string seqId { get; set; }
        public string itemType { get; set; }
        public string value { get; set; }
        public StyleTable style { get; set; }
    }

    public class StyleTable
    {
        public int width { get; set; } = 100;
        public string borderColor { get; set; } = "none";
        public string bgColor { get; set; } = "none";
        public string fontColor { get; set; } = "#000000";
        public string textAlign { get; set; } = "center";
    }
}
