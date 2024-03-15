namespace Service.DInspect.Models.Request
{
    public class TaskCBMDefaultValueRequest
    {
        public string id { get; set; }
        public string modelId { get; set; }
        public string psTypeId { get; set; }
        public string taskId { get; set; }
        public string defaultValue { get; set; }
    }
}
