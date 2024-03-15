namespace Service.DInspect.Models.Request
{
    public class TaskTandemRequest
    {
        public string id { get; set; }
        public string modelId { get; set; }
        public string psTypeId { get; set; }
        public string taskId { get; set; }
        public string taskCrackCode { get; set; }
        public string locationDesc { get; set; }
        public string uom { get; set; }
    }
}
