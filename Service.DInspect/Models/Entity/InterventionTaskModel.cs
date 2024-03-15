using System.Collections.Generic;

namespace Service.DInspect.Models.Entity
{
    public class InterventionTaskModel
    {
        public string key { get; set; }
        public string name { get; set; }
        public List<dynamic> tasks { get; set; }
    }
}