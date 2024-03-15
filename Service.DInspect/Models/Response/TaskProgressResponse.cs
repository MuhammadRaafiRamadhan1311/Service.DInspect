namespace Service.DInspect.Models.Response
{
    public class TaskProgressResponse
    {
        public string workorder { get; set; }
        public string group { get; set; }
        public int totalTask { get; set; }
        public int doneTask { get; set; }

    }

    public class TaskProgressResponseWithIdentifiedDefect : TaskProgressResponse
    {
        public int identifiedDefectCount { get; set; }
    }
}
