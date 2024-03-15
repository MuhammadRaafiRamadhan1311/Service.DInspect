namespace Service.DInspect.Models.Response
{
    public class ParameterRating
    {
        public string cbmRating { get; set; }
        public string cbmType { get; set; }
        public string detailNumber { get; set; }
        public decimal? maxValue { get; set; }
        public string maxValueComplete { get; set; }
        public decimal? minValue { get; set; }
        public string minValueComplete { get; set; }
        public string operatorMax { get; set; }
        public string operatorMin { get; set; }
        public string taskKey { get; set; }
        public string taskNumber { get; set; }
        public string uom { get; set; }
        public string component { get; set; }
    }
}
