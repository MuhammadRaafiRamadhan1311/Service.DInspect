using System.Collections.Generic;

namespace Service.DInspect.Models.Request
{
    public class ResetUpdatedDateRequest : CreateRequest
    {
        public List<string> modelIds { get; set; }

    }
}
