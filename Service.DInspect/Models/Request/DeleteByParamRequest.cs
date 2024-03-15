using Service.DInspect.Models.Entity;
using System.Collections.Generic;

namespace Service.DInspect.Models.Request
{
    public class DeleteByParamRequest
    {
        public Dictionary<string, object> deleteParams { get; set; }
        public EmployeeModel employee { get; set; }
    }
}
