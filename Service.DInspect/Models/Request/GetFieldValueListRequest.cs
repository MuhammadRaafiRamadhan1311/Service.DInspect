using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.DInspect.Models.Request
{
    public class GetFieldValueListRequest
    {
        public string id { get; set; }
        public string keyValue { get; set; }
        public List<string> propertyName { get; set; }
    }
}
