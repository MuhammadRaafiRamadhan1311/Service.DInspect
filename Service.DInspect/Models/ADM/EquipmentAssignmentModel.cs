using System;

namespace Service.DInspect.Models.ADM
{
    public class EquipmentAssignmentModel
    {
        public string EquipmentAssignmentId { get; set; }
        public long? EquipmentNumberId { get; set; }
        public string Equipment { get; set; }
        public string ObjectType { get; set; }
        public string PlannerGroup { get; set; }
        public string MaintenanceWorkCenter { get; set; }
        public string CostCenter { get; set; }
        public string WbsElement { get; set; }
        public string Level { get; set; }
        public string EquipmentType { get; set; }
        public string EquipmentGroup { get; set; }
        public long EquipmentGroupId { get; set; }
        public string EquipmentModel { get; set; }
        public long EquipmentModelId { get; set; }
        public string EquipmentStatus { get; set; }
        public string Site { get; set; }
        public string PlanningPlant { get; set; }
        public string MaintenancePlant { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public DateTime ChangedOn { get; set; }
        public string ChangedBy { get; set; }
    }
}
