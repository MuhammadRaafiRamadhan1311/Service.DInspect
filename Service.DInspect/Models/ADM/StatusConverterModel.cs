using System;

namespace Service.DInspect.Models.ADM
{
    public class StatusConverterModel
    {
        public long? StatusConverterId { get; set; }
        public string StatusConverter { get; set; }
        public string StatusConverterDescription { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string ChangedBy { get; set; }
        public DateTime ChangedOn { get; set; }
    }
}
