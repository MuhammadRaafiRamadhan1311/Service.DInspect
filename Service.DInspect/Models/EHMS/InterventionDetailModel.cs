using System;

namespace Service.DInspect.Models.EHMS
{
    public class InterventionDetailModel
    {
        public long tr_intervention_detail_id { get; set; }
        public long intervention_header_id { get; set; }
        public long? recomended_action_id { get; set; }
        public string recommended_action_desc { get; set; }
        public bool? executed { get; set; }
        public string time_zone { get; set; }
        public DateTime actual_intervention_date { get; set; }
        public string intervention_status { get; set; }
        public decimal? value { get; set; }
        public long? uom { get; set; }
        public long? rating_id { get; set; }
        public bool is_additional_task { get; set; }
        public string condition_code { get; set; }
        public string actual_intervention_date_by { get; set; }
        public long? model_unit_id { get; set; }
        public int? ps_type { get; set; }
        public string task_group_key { get; set; }
        public string task_key { get; set; }
        public int sequence { get; set; }
        public string sub_task { get; set; }
        public bool? is_mandatory { get; set; }
        public long? type_task_id { get; set; }
        public long? ref_doc_id { get; set; }
        public bool is_active { get; set; }
        public bool is_deleted { get; set; }
        public DateTime created_on { get; set; }
        public string created_by { get; set; }
        public DateTime changed_on { get; set; }
        public string changed_by { get; set; }
    }
}