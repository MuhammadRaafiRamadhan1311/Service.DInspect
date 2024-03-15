namespace Service.DInspect.Models.Request
{
    public class GetTaskCollectionRequest
    {
        public string modelId { get; set; }
        public string psTypeId { get; set; }
        public string version { get; set; }
        public string category { get; set; }
        public string description { get; set; }
        public string subTask { get; set; }
        public string status { get; set; }
        public string releaseDate { get; set; }
        public int page { get; set; }
        public int pageSize { get; set; }
        public string orderBy { get; set; }
    }
}
