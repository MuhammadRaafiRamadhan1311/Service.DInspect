using Service.DInspect.Models.Entity;

namespace Service.DInspect.Models.Request
{
    public class CreateRequest
    {
        public EmployeeModel employee { get; set; }
        public dynamic entity { get; set; }
    }
}
