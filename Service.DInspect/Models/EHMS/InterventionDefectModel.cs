using System;

namespace Service.DInspect.Models.EHMS
{
    public class InterventionDefectModel
    {
        public long tr_intervention_defect_id { get; set; }
        public long intervention_detail_id { get; set; }
        public string spv_status { get; set; }
        public string planner_status { get; set; }
        public string decline_reason { get; set; }
        public bool is_active { get; set; }
        public bool is_deleted { get; set; }
        public DateTime created_on { get; set; }
        public string created_by { get; set; }
        public DateTime changed_on { get; set; }
        public string changed_by { get; set; }
    }
}