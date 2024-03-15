using System;

namespace Service.DInspect.Models.Request
{
    public class UpdateInterventionRequest
    {
        public long tr_intervention_header_id { get; set; }
        public long? equipment_id { get; set; }
        public long? component_id { get; set; }
        public string sample_type { get; set; }
        public string intervention_code_id { get; set; }
        public string intervention_reason { get; set; }
        public DateTime sample_date { get; set; }
        public long sample_status_id { get; set; }
        public decimal smu { get; set; }
        public decimal component_hm { get; set; }
        public long md_intervention_status_id { get; set; }
        public string intervention_diagnostic { get; set; }
        public long sap_work_order { get; set; }
        public DateTime? status_date_time { get; set; }
        public long intervention_execution_id { get; set; }
        public string intervention_execution_by { get; set; }
        public DateTime? estimation_completion_date { get; set; }
        public string time_zone { get; set; }
        public string key_pbi { get; set; }
        public bool is_active { get; set; }
        public bool is_deleted { get; set; }
        public DateTime created_on { get; set; }
        public string created_by { get; set; }
        public DateTime changed_on { get; set; }
        public string changed_by { get; set; }
    }
}