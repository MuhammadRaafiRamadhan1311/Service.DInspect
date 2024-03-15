using System;

namespace Service.DInspect.Models.EHMS
{
    public class MasterRatingModel
    {
        public long md_rating_id { get; set; }
        public string rating { get; set; }
        public string rating_desc { get; set; }
        public DateTime start_date { get; set; }
        public DateTime end_date { get; set; }
        public bool is_deleted { get; set; }
        public string created_by { get; set; }
        public DateTime created_on { get; set; }
        public string changed_by { get; set; }
        public DateTime? changed_on { get; set; }
    }
}
