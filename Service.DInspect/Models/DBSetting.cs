using Service.DInspect.Models.Entity;

namespace Service.DInspect.Models
{
    public class DBSetting
    {
        public string id { get; set; }
        public string key { get; set; }
        public string value { get; set; }
        public string desc { get; set; }
        public string group { get; set; }
        public string isActive { get; set; }
        public string isDeleted { get; set; }
        public EmployeeModel createdBy { get; set; }
        public string createdDate { get; set; }
        public EmployeeModel updatedBy { get; set; }
        public string updatedDate { get; set; }
    }
}