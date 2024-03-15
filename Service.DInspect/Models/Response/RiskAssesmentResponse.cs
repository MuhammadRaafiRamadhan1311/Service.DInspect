using Service.DInspect.Models.Entity;
using System.Collections.Generic;

namespace Service.DInspect.Models.Response
{
    public class RiskAssesmentResponse
    {
        public string key { get; set; }
        public string taskType { get; set; }
        public string caption { get; set; }
        public string itemValue { get; set; }
        public List<RiskAssesmentValue> value { get; set; }
    }

    public class RiskAssesmentValue
    {
        public object image { get; set; }
        public string imageType { get; set; }
        public string updatedDate { get; set; }
        public EmployeeModel updatedBy { get; set; }
    }
}
