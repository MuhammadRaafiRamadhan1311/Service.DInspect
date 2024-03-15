using System.Collections.Generic;

namespace Service.DInspect.Models.EHMS
{
    public class TaskTypeConditionModel
    {
        public long typeTaskId { get; set; }
        public string typeTask { get; set; }
        public List<TaskConditionModel> listTypeCondition { get; set; }
    }

    public class TaskConditionModel
    {
        public long typeConditionId { get; set; }
        public string typeCondition { get; set; }
    }
}
