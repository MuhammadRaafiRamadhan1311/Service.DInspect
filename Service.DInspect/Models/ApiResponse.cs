namespace Service.DInspect.Models
{
    public class ApiResponse
    {
        public string Title { get; set; }
        public int StatusCode { get; set; }
        public ServiceResult Result { get; set; }
    }
}
