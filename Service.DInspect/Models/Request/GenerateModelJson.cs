using Microsoft.AspNetCore.Http;

namespace Service.DInspect.Models.Request
{
    public class GenerateModelJson
    {
        public string workSheet { get; set; }
        public string startRow { get; set; }
        public string endRow { get; set; }
        public string groupName { get; set; }
        public IFormFile file { get; set; }
    }
}
