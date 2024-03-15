using Service.DInspect.Models.Entity;
using System.Collections.Generic;

namespace Service.DInspect.Models.Request
{
    public class UpdateGeneralFormRequest
    {
        public string id { get; set; }
        public string workOrder { get; set; }
        public List<UpdateParam> updateParams { get; set; }
        public EmployeeModel employee { get; set; }
        public string updatedDate { get; set; }
        public List<UpdateParam> checkBeforeTruck { get; set; }
    }
}
