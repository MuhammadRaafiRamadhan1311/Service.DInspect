using Service.DInspect.Models.Entity;
using System;
using System.Collections.Generic;

namespace Service.DInspect.Models.EHMS
{
    public class InterventionRecomModel
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
        public DateTime sampleDate { get; set; }
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
        public string defectStatusId { get; set; }
        public string additionalInformation { get; set; }
        public DateTime? cautionRatingDate { get; set; }
        public long? followUpPriority { get; set; }
        public long? followUpPriorityUomId { get; set; }
        public string followUpPriorityUom { get; set; }
        public string keyPbi { get; set; }
        public long? hmOffset { get; set; }
        public string serviceStart { get; set; }
        public string serviceEnd { get; set; }
        //public DateTime? estCompletionDate { get; set; }
        public DateTime? estimationCompletionDate { get; set; }
        //public string createOn { get; set; }
        //public string createdBy { get; set; }
        //public string changedOn { get; set; }
        //public string changedBy { get; set; }
        public List<InterventionRecomDetailModel> listInterventionDetail { get; set; }
        public List<InterventionComponentModel> components { get; set; }
    }

    public class InterventionRecomDetailModel
    {
        public long? trInterventionDetailId { get; set; }
        public long? interventionHeaderId { get; set; }
        public long? recomendedActionId { get; set; }
        public string recomendedAction { get; set; }
        public long? sequence { get; set; }
        public long? executed { get; set; }
        public DateTime? actualInterventiondate { get; set; }
        public string interventionStatus { get; set; }
        public string value { get; set; }
        public long? uomId { get; set; }
        public string uom { get; set; }
        public long? ratingId { get; set; }
        public string rating { get; set; }
        public bool? isAdditionalTask { get; set; }
        public string condition { get; set; }
        public string actualInterVentionDateBy { get; set; }
        public string typeTask { get; set; }
        public string typeTaskId { get; set; }
        public string modelUnitId { get; set; }
        public int? psType { get; set; }
        public string taskGroupKey { get; set; }
        public string taskKey { get; set; }
        public int? interventionSequence { get; set; }
        public string subTask { get; set; }
        public long? refDocId { get; set; }
        //public string createdOn { get; set; }
        //public string createdBy { get; set; }
        //public string changedOn { get; set; }
        //public string changedBy { get; set; }
    }
}
