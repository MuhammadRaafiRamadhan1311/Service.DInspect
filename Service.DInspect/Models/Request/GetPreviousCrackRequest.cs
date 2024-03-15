namespace Service.DInspect.Models.Request
{
    public class GetPreviousCrackRequest
    {
        public string modelId { get; set; }
        public string psTypeId { get; set; }
        public string taskId { get; set; }
    }
}
