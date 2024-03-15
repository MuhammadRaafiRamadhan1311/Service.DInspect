using Service.DInspect.Models.Entity;
using System.Collections.Generic;

namespace Service.DInspect.Models.Request
{
    public class UpdateTaskRequest
    {
        public string headerId { get; set; }
        public string workorder { get; set; }
        public string id { get; set; }
        public string localInterventionStatus { get; set; }
        public string taskKey { get; set; }
        public List<UpdateParam> updateParams { get; set; }
        public EmployeeModel employee { get; set; }
    }
}
