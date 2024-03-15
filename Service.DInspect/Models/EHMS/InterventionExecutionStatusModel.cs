using System;

namespace Service.DInspect.Models.EHMS
{
    public class InterventionExecutionStatusModel
    {
        public long md_intervention_execution_id { get; set; }
        public string intervention_execution { get; set; }
        public DateTime? start_date { get; set; }
        public DateTime? end_date { get; set; }
        public bool is_deleted { get; set; }
        public DateTime? created_on { get; set; }
        public string created_by { get; set; }
        public DateTime? changed_on { get; set; }
        public string changed_by { get; set; }
    }
}