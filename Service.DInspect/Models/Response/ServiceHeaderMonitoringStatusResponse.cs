namespace Service.DInspect.Models.Response
{
    public class ServiceHeaderMonitoringStatusResponse
    {
        public string id { get; set; }
        public string workOrder { get; set; }
        public string key { get; set; }
        public string defectStatus { get; set; }
        public string status { get; set; }
        public string isDownload { get; set; }
        public string serviceStart { get; set; }
        public string serviceEnd { get; set; }
    }
}
