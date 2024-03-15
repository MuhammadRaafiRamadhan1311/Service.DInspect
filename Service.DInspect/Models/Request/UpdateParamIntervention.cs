using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.DInspect.Models.Request
{
    public class UpdateParamIntervention
    {
        public string interventionHeaderId { get; set; }
        public string employeeId { get; set; }
        public List<PropertyParam> propertyParams { get; set; }

    }
}
