using Service.DInspect.Models.Entity;
using System.Collections.Generic;

namespace Service.DInspect.Models.Request
{
    public class UpdateTaskServiceDetailRequest
    {
        public string serviceSheetDetailId { get; set; }
        public string workorder { get; set; }
        public string taskKey { get; set; }
        public string localInterventionStatus { get; set; }
        public List<UpdateParam> updateParams { get; set; }
        public EmployeeModel employee { get; set; }
    }
}
