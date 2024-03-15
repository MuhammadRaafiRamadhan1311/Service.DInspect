using System.Collections.Generic;

namespace Service.DInspect.Models.Entity
{
    public class InterventionModel
    {
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
        public List<InterventionComponentModel> components { get; set; }
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
        public string defectStatusId { get; set; }
        public string cautionRatingDate { get; set; }
        public string followUpPriority { get; set; }
        public string followUpPriorityUomId { get; set; }
        public string followUpPriorityUom { get; set; }
        public string keyPbi { get; set; }
        public string estimationCompletionDate { get; set; }
        //public string createOn { get; set; }
        //public string createdBy { get; set; }
        //public string changedOn { get; set; }
        //public string changedBy { get; set; }
        public dynamic log { get; set; }
        public dynamic riskAssesment { get; set; }
        public dynamic safetyPrecaution { get; set; }
        public string imageEquipment { get; set; }
        public string serviceStart { get; set; }
        public string serviceEnd { get; set; }
        public string interventionSMU { get; set; }
        public string version { get; set; }
        public dynamic supervisor { get; set; }
        public string additionalInformation { get; set; }
        public List<StatusHistoryModel> statusHistory { get; set; }
        public string defectStatus { get; set; }
        public string dayShift { get; set; }
        public string hmOffset { get; set; }
        public List<dynamic> details { get; set; }
    }
}
