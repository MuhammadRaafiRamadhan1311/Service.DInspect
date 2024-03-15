using Service.DInspect.Models.Entity;
using System.Collections.Generic;

namespace Service.DInspect.Models.Request
{
    public class UpdateRequest
    {
        public string id { get; set; }
        public string workOrder { get; set; }
        public string localInterventionStatus { get; set; }
        public string localStatus { get; set; }
        public List<UpdateParam> updateParams { get; set; }
        public EmployeeModel employee { get; set; }
        public string userGroup { get; set; }
    }
}
