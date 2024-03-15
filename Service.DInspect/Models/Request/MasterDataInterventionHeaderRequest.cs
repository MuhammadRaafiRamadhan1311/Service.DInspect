using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.DInspect.Models.Request
{
    public class MasterDataInterventionHeaderRequest
    {
        public long? InterventionHeaderId { get; set; }
        public long? InterventionHeaderSourceId { get; set; }
        public string Site { get; set; }
        public long? EquipmentModelId { get; set; }
        public string EquipmentModel { get; set; }
        public long? EquipmentNumberId { get; set; }
        public string EquipmentNumber { get; set; }
        public long? ComponentId { get; set; }
        public string Component { get; set; }
        public string ComponentDescription { get; set; }
        public decimal? ComponentHours { get; set; }
        public string InterventionReason { get; set; }
        public decimal? ConditionRatingSmu { get; set; }
        public DateTime ConditionRatingDate { get; set; }
        public string InterventionStatus { get; set; }
        public string InterventionDiagnostic { get; set; }
        public string FollowUpPriority { get; set; }
        public long? UomId { get; set; }
        public DateTime EstimationCompletionDate { get; set; }
        public string WorkOrder { get; set; }
        public long? InterventionExecutionStatusId { get; set; }
        public string InterventionExecutionBy { get; set; }
        public long? DefectStatusId { get; set; }
        public string KeyPbi { get; set; }
        public string EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public bool IsRevise { get; set; }
        public string Fitur { get; set; }
        public string AdditionalInformation { get; set; }
        public bool IsDma { get; set; }
        public DateTime? ServiceStart { get; set; }
        public DateTime? ServiceEnd { get; set; }
        public List<dynamic> AdditionalTask { get; set; }
    }
}
