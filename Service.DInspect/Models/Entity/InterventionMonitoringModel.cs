namespace Service.DInspect.Models.Entity
{
    public class InterventionMonitoringModel
    {
        public string id { get; set; }
        public string key { get; set; }
        public string siteId { get; set; }
        public string sitedesc { get; set; }
        public string trInterventionHeaderId { get; set; }
        public string equipmentId { get; set; }
        public string equipment { get; set; }
        public string equipmentDesc { get; set; }
        public string equipmentModel { get; set; }
        public string equipmentBrand { get; set; }
        public string equipmentGroup { get; set; }
        public string componentId { get; set; }
        public string componentCode { get; set; }
        public string componentDescription { get; set; }
        public string sampleType { get; set; }
        public string interventionCode { get; set; }
        public string interventionReason { get; set; }
        public string sampleDate { get; set; }
        public string sampleStatusId { get; set; }
        public string sampleStatus { get; set; }
        public string smu { get; set; }
        public string smuDue { get; set; }
        public string componentHm { get; set; }
        public string mdInterventionStatusId { get; set; }
        public string interventionStatus { get; set; }
        public string interventionStatusDesc { get; set; }
        public string interventionDiagnosis { get; set; }
        public string sapWorkOrder { get; set; }
        public string statusDatetime { get; set; }
        public string interventionExecutionId { get; set; }
        public string interventionExecution { get; set; }
        public string interventionExecutionBy { get; set; }
        public string cautionRatingDate { get; set; }
        public string followUpPriority { get; set; }
        public string followUpPriorityUomId { get; set; }
        public string followUpPriorityUom { get; set; }
        public string keyPbi { get; set; }
        public string estimationCompletionDate { get; set; }
    }
}
