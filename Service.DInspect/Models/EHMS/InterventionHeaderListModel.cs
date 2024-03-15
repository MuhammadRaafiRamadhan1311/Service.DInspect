using System;

namespace Service.DInspect.Models.EHMS
{
    public class InterventionHeaderListModel
    {
        public string siteId { get; set; }
        public string sitedesc { get; set; }
        public long? trInterventionHeaderId { get; set; }
        public long? equipmentId { get; set; }
        public string equipment { get; set; }
        public string equipmentDesc { get; set; }
        public string equipmentModel { get; set; }
        public string equipmentBrand { get; set; }
        public string equipmentGroup { get; set; }
        public long? componentId { get; set; }
        public string componentCode { get; set; }
        public string componentDescription { get; set; }
        public string sampleType { get; set; }
        public string interventionCode { get; set; }
        public string interventionReason { get; set; }
        public string sampleDate { get; set; }
        public long? sampleStatusId { get; set; }
        public string sampleStatus { get; set; }
        public string smu { get; set; }
        public string smuDue { get; set; }
        public string componentHm { get; set; }
        public long? mdInterventionStatusId { get; set; }
        public string interventionStatus { get; set; }
        public string interventionStatusDesc { get; set; }
        public string interventionDiagnosis { get; set; }
        public long? sapWorkOrder { get; set; }
        public DateTime? statusDatetime { get; set; }
        public long? interventionExecutionId { get; set; }
        public string interventionExecution { get; set; }
        public string interventionExecutionBy { get; set; }
        public DateTime? cautionRatingDate { get; set; }
        public long? followUpPriority { get; set; }
        public long? followUpPriorityUomId { get; set; }
        public string followUpPriorityUom { get; set; }
        public string keyPbi { get; set; }
        //public DateTime? estCompletionDate { get; set; }
        public DateTime estimationCompletionDate { get; set; }
        public DateTime HeaderChangedOn { get; set; }
        public string ComponentSystem { get; set; }
    }
}
