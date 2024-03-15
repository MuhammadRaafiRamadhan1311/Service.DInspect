using Service.DInspect.Models.Request;

namespace Service.DInspect.Models.Response
{
    public class UpdateResponse : UpdateRequest
    {
        public string updatedDate { get; set; }
    }
}
