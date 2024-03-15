using Service.DInspect.Models.Entity;
using System.Collections.Generic;

namespace Service.DInspect.Models.Request
{
    public class CreateRequestCalibrationHeader
    {
        public string modelId { get; set; }
        public string psTypeId { get; set; }
        public string workOrder { get; set; }
        public string equipment { get; set; }
        public string serialNumber { get; set; }
        public string statusCalibration { get; set; }
        public string smu { get; set; }
        public List<StatusHistoryModel> statusHistory { get; set; }
    }
}
