namespace Service.DInspect.Models
{
    public class MySetting
    {
        public ConnectionStringModel ConnectionStrings { get; set; }
        public string UtilityBaseUrl { get; set; }
        public string ADMBaseUrl { get; set; }
        public string EHMSBaseUrl { get; set; }
        public string BlobUrl { get; set; }
        //public string TimeZone { get; set; }
        //public string TimeZoneDesc { get; set; }
    }

    public class ConnectionStringModel
    {
        public string CosmosConnection { get; set; }
        public string EndpointUri { get; set; }
        public string PrimaryKey { get; set; }
        public string DatabaseName { get; set; }

    }
}
