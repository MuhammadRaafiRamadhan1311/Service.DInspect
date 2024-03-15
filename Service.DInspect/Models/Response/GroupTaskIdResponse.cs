using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.DInspect.Models.Response
{
    public class GroupTaskIdResponse
    {
        public string key { get; set; }
        public string taskValue { get; set; }
        public string parentGroupTaskId { get; set; }
        public string childGroupTaskId { get; set; }
        public string updatedDate { get; set; }
        public string category { get; set; }
        public string rating { get; set; }
        public string groupTaskId { get; set; }
    }
}
