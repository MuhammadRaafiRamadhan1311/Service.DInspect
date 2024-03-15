using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.DInspect.Models.Request
{
    public class ServiceSheetDataRequest : CreateRequest
    {
        public string modelId { get; set; }
        public string psTypeId { get; set; }
        public string workOrder { get; set; }
        public string unitNumber { get; set; }
        public string siteId { get; set; }
    }
}
