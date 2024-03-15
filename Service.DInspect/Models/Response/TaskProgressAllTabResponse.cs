namespace Service.DInspect.Models.Response
{
    public class TaskProgressAllTabResponse
    {
        public string keyGroup { get; set; }
        public string key { get; set; }
        public string modelId { get; set; }
        public string psTypeId { get; set; }
        public string category { get; set; }
        public string rating { get; set; }
        public string groupTaskId { get; set; }
        public string taskValue { get; set; }
        public string parentGroupTaskId { get; set; }
        public string childGroupTaskId { get; set; }
        public string disabledByItemKey { get; set; }
        public string showParameter { get; set; }
        public string updatedDate { get; set; }
    }
}
