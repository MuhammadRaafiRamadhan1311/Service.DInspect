namespace Service.DInspect.Models.Request
{
    public class GetInterventionReviseRequest
    {
        public string workOrder { get; set; }
        public string taskKey { get; set; }
        public string component { get; set; }
    }
}
