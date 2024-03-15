namespace Service.DInspect.Models.Helper
{
    public class CommentHelperModel
    {
        public string taskKey { get; set; }
        public string taskDesc { get; set; }
        public string taskComment { get; set; }
        public dynamic createdBy { get; set; }
        public dynamic createdDate { get; set; }
        public dynamic updatedBy { get; set; }
        public dynamic updatedDate { get; set; }
    }
}
