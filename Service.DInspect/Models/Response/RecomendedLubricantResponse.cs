using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.DInspect.Models.Response
{
    public class RecomendedLubricantResponse
    {
        public string taskGroupKey { get; set; }
        public string Key { get; set; }
        public string compartment { get; set; }
        public string recomendedLubricant { get; set; }
        public string oilStandart { get; set; }
    }
}
