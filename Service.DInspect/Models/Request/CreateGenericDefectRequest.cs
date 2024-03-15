using Service.DInspect.Models.Entity;

namespace Service.DInspect.Models.Request
{
    public class CreateGenericDefectRequest
    {
        public EmployeeModel employee { get; set; }
        public DefectHeaderWithIdModel defectHeader { get; set; }
        public dynamic defectDetail { get; set; }
    }
}
