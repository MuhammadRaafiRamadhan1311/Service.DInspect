namespace Service.DInspect.Models.Request
{
    public class GetFieldValueRequest
    {
        public string id { get; set; }
        public string keyValue { get; set; }
        public string propertyName { get; set; }
    }
}
