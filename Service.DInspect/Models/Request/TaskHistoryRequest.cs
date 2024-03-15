namespace Service.DInspect.Models.Request
{
    public class TaskHistoryRequest : CreateRequest
    {
        public string workorder { get; set; }
        public string modelId { get; set; }
        public string psTypeId { get; set; }
        public string taskId { get; set; }
    }
}
