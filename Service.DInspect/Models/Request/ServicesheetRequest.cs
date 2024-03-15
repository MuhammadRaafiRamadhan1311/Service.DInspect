using System.Collections.Generic;

namespace Service.DInspect.Models.Request
{
    public class ServicesheetRequest
    {
        public List<SelectRequest> selectedFields { get; set; }
        public List<ParameterRequest> parameters { get; set; }
    }
}
