namespace Service.DInspect.Models.Helper
{
    public class TaskTandemHelperModel
    {
        public string id { get; set; }
        public string modelId { get; set; }
        public string psTypeId { get; set; }
        public string key { get; set; }
        public string description { get; set; }
        public dynamic items { get; set; }
    }
}
