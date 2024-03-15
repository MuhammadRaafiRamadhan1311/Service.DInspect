namespace Service.DInspect.Models.Request
{
    public class CbmHistoryParam
    {
        public string key { get; set; }
        public string workOrder { get; set; }
        public string keyPbi { get; set; }
        public string equipment { get; set; }
        public string siteId { get; set; }
        public string taskKey { get; set; }
        public string serviceSheetDetailId { get; set; }
        public string defectHeaderId { get; set; }
        public string modelId { get; set; }
        public string psTypeId { get; set; }
        public string taskDescription { get; set; }
        public string category { get; set; }
        public string currentValue { get; set; }
        public string currentRating { get; set; }
        public string replacementValue { get; set; }
        public string replacementRating { get; set; }
        public string closedDate { get; set; }
        public string closedBy { get; set; }
        public string source { get; set; }
        public DetailCBMHistory detail { get; set; }
    }

    public class DetailCBMHistory
    {
        public string key { get; set; }
        public string category { get; set; }
        public string rating { get; set; }
        public object history { get; set; }
    }
}
