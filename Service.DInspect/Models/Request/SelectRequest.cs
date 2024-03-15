namespace Service.DInspect.Models.Request
{
    public class SelectRequest
    {
        /// <summary>Level type : tab, subGroup, taskGroup, task, items; Reference : EnumLevel</summary>
        public string level { get; set; }
        public string fieldName { get; set; }
    }
}
