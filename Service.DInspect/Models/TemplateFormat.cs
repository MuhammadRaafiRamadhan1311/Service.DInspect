using System.Collections.Generic;

namespace Service.DInspect.Models
{
    public class TemplateFormat
    {
        public string workOrder { get; set; }
        public string modelId { get; set; }
        public string psTypeId { get; set; }
        public string groupName { get; set; }
        public string key { get; set; }
        public List<SubGroupTemplate> subGroup { get; set; }
    }

    public class SubGroupTemplate
    {
        public string name { get; set; }
        public string key { get; set; }
        public List<TaskGroupTemplate> taskGroup { get; set; }
    }

    public class TaskGroupTemplate
    {
        public string name { get; set; }
        public string key { get; set; }
        public object task { get; set; }
    }
}
