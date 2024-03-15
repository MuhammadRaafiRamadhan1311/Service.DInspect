namespace Service.DInspect.Models.Entity
{
    public class DailyScheduleModel
    {
        public string dailyScheduleId { get; set; }
        public string unitNumber { get; set; }
        public string equipmentModel { get; set; }
        public string brand { get; set; }
        public string smuDue { get; set; }
        public string workOrder { get; set; }
        public string psType { get; set; }
        public string dateService { get; set; }
        public string shift { get; set; }
        public string isActive { get; set; }
        public string startDate { get; set; }
        public string endDate { get; set; }
        public string createdOn { get; set; }
        public string createdBy { get; set; }
        public string changedOn { get; set; }
        public string changedBy { get; set; }
        public string eFormId { get; set; }
        public string eFormKey { get; set; }
        public string status { get; set; }
        public string defectStatus { get; set; }
        public string isDownload { get; set; }
        public string eFormStatus { get; set; }
        public string form { get; set; }
        public string serviceStart { get; set; }
        public string serviceEnd { get; set; }
    }
}