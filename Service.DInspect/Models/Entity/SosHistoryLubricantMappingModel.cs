using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.DInspect.Models.Entity
{
    public class SosHistoryLubricantMappingModel
    {
        public string key { get; set; }
        public string compartmentLubricant { get; set; }
        public string recommendedLubricants { get; set; }
        public string volume { get; set; }
        public string uoM { get; set; }
        public string lubricantType { get; set; }
        public string isSOS { get; set; }
        //public string taskChange { get; set; }
        //public string taskAdded { get; set; }
        public List<TaskMapping> task { get; set; }
    }

    public class TaskMapping
    {
        public string name { get; set; }
        public string key { get; set; }
        public string taskValue { get; set; }
        public string value { get; set; }
        public string updatedDate { get; set; }
    }
}
