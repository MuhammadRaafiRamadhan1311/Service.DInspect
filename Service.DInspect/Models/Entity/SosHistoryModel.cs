using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.DInspect.Models.Entity
{
    public class SosHistoryModel
    {
        public string key { get; set; }
        public string headerId { get; set; }
        public string siteId { get; set; }
        public string workOrder { get; set; }
        public string eformType { get; set; }
        public string modelId { get; set; }
        public string equipmentModel { get; set; }
        public string equipment { get; set; }
        public string psTypeId { get; set; }
        public string meterHrs { get; set; }
        public string fuelType { get; set; }
        public string brand { get; set; }
        public string equipmentSerialNumber { get; set; }
        public string customerName { get; set; }
        public string jobSite { get; set; }
        public string brandDescription { get; set; }
        public dynamic subGroup { get; set; }
    }

    public class SosIntervention : SosHistoryModel
    {
        public string keyPbi { get; set; }
    }
}
