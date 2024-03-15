using Service.DInspect.Models.Entity;

namespace Service.DInspect.Models.Request
{
    public class DeleteRequest
    {
        public EmployeeModel employee { get; set; }
        public string id { get; set; }
        public string partitionKey { get; set; }
    }
}
