using Service.DInspect.Models.Entity;
using System.Collections.Generic;

namespace Service.DInspect.Models.Request
{
    public class ValidationRequest
    {
        public dynamic rsc { get; set; }
        public string id { get; set; }
        public string keyValue { get; set; }
        public string propertyName { get; set; }
        public string propertyValue { get; set; }
        public string employeeId { get; set; }
        public DefectHeaderModel defectHeader { get; set; }
        public dynamic defectDetail { get; set; }
        public bool isDefect { get; set; }
        public string headerId { get; set; }
        public string workorder { get; set; }
        public string reason { get; set; }
        public EmployeeModel employee { get; set; }
        public List<PropertyParam> propertyParams { get; set; }
        public string taskKey { get; set; }
    }
}
