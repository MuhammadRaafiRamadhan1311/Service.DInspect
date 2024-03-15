namespace Service.DInspect.Models.Request
{
    public class UpdateDefectDetailByTaskKeyRequest : UpdateRequest
    {
        public string taskKey { get; set; }
        public string serviceSheetDetailId { get; set; }
    }
}
