namespace Service.DInspect.Models.Response
{
    public class GenerateColumnJson
    {
        public int Row { get; set; }
        public bool FirstRow { get; set; }
        public bool LastRow { get; set; }
        public string GroupTaskId { get; set; }
        public string Model { get; set; }
        public string GroupName { get; set; }
        public string Section { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string Number250 { get; set; }
        public string Number500 { get; set; }
        public string Number1000 { get; set; }
        public string Number2000 { get; set; }
        public string Number4000 { get; set; }
        public string GuidTable { get; set; }
        public string ImageData { get; set; }
        public string Table { get; set; }
        public string ServiceMappingValue { get; set; }
        public string SOS { get; set; }
        public string SectionColumn { get; set; }
        public string TaskKey { get; set; }
        public string IsCbm { get; set; }
        public string IsCrack { get; set; }
        public string IsDefectGroup { get; set; }
        public string HeaderName { get; set; }
    }
}
