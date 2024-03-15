using System.Collections.Generic;

namespace Service.DInspect.Models.Request
{
    public class UpdateParam
    {
        public string keyValue { get; set; }
        public List<PropertyParam> propertyParams { get; set; }
    }
}
