using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.DInspect.Models.Response
{
    public class PrintSosHistoryResponse
    {
        public string id { get; set; }
        public string key { get; set; }
        public string headerId { get; set; }
        public string workOrder { get; set; }
        public string eformType { get; set; }
        public string modelId { get; set; }
        public string equipment { get; set; }
        public string psTypeId { get; set; }
        public string equipmentSerialNumber { get; set; }
        public string equipmentModel { get; set; }
        public string customerName { get; set; }
        public string jobSite { get; set; }
        public string brandDescription { get; set; }
        public string meterHrs { get; set; }
        public string brand { get; set; }
        public string fuelType { get; set; }
        public string createdDate { get; set; }
        public dynamic details { get; set; }
    }

    public class TaskSOSResponse
    {
        public string key { get; set; }
        public string compartmentLubricant { get; set; }
        public string recommendedLubricants { get; set; }
        public string sampleDate { get; set; }
        public string volume { get; set; }
        public string uoM { get; set; }
        public string lubricantType { get; set; }
        public string taskChange { get; set; }
        public string taskAdded { get; set; }
        public string hrsOnOil { get; set; }
        public string lastMeterHrs { get; set; }
        public string qrString { get; set; }

    }
}
