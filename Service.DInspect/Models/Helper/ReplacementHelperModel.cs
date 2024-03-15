namespace Service.DInspect.Models.Helper
{
    public class ReplacementHelperModel
    {
        public string key { get; set; }
        public string description { get; set; }
        public string measurement { get; set; }
        public string rating { get; set; }
        public string commentValue { get; set; }
        public string commentLabel { get; set; }
        public string commentPlaceHolder { get; set; }
        public string uom { get; set; }
        public dynamic createdBy { get; set; }
        public string createdDate { get; set; }
        public dynamic updatedBy { get; set; }
        public string updatedDate { get; set; }
    }
}
