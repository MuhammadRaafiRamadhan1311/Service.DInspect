namespace Service.DInspect.Models.Entity
{
    public class StatusHistoryModel
    {
        public string status { get; set; }
        public EmployeeModel updatedBy { get; set; }
        public string updatedDate { get; set; }
        public string tsUpdatedDate { get; set; }
    }
}
