using System.Collections.Generic;

namespace Service.DInspect.Models.Response
{
    public class GenerateJsonModel
    {
        public string modelId { get; set; }
        public string psTypeId { get; set; }
        public string workOrder { get; set; }
        public string groupName { get; set; }
        public int groupSeq { get; set; }
        public string key { get; set; }
        public string version { get; set; }
        public List<SubGroup> subGroup { get; set; }
    }

    public class SubGroup
    {
        public string name { get; set; }
        public string key { get; set; }
        public string desc { get; set; }
        public List<TaskGroup> taskGroup { get; set; }
    }

    public class TaskGroup
    {
        public string name { get; set; }
        public string key { get; set; }
        public dynamic task { get; set; }
    }
}
