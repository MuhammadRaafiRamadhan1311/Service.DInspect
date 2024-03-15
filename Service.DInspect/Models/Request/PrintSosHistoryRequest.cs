using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.DInspect.Models.Request
{
    public class PrintSosHistoryRequest
    {
        public string workOrder { get; set; }
        public string eformType { get; set; }
        public List<string> oilSampleKey { get; set; }
    }
}
