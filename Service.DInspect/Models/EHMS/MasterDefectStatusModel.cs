using System;

namespace Service.DInspect.Models.EHMS
{
    public class MasterDefectStatusModel
    {
        public long md_defect_status_id { get; set; }
        public string defect_status { get; set; }
        public bool is_active { get; set; }
        public bool is_deleted { get; set; }
        public DateTime created_on { get; set; }
        public string created_by { get; set; }
        public DateTime changed_on { get; set; }
        public string changed_by { get; set; }
    }
}
