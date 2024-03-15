using Service.DInspect.Models.Entity;
using System.Collections.Generic;

namespace Service.DInspect.Models.Request
{
    public class DeleteFullCycleRequest
    {
        public List<string> workorders { get; set; }
        public EmployeeModel employee { get; set; }
    }
}
