namespace Service.DInspect.Models.Request
{
    public class UpdateInterventionByEhmsRequest
    {
        public string trInterventionHeaderId { get; set; }
        public string interventionStatusId { get; set; }
        public string interventionStatus { get; set; }
        public string interventionDiagnosis { get; set; }
        public string followUpPriority { get; set; }
        public string estimationCompletionDate { get; set; }
        public string sapWorkOrder { get; set; }
        public string employeeId { get; set; }
        public string employeeName { get; set; }
    }
}
