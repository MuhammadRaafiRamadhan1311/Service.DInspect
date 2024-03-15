using Service.DInspect.Models.Request;
using System.Collections.Generic;

namespace Service.DInspect.Models.Response
{
    public class UpdateHeaderResponse : UpdateRequest
    {
        public string updatedDate { get; set; }
        public List<UpdateParam> checkBeforeTruck { get; set; }
    }
}
