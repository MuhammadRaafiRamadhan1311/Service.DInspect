namespace Service.DInspect.Models.Response
{
    public class DefectIdentifyResponse
    {
        public string version { get; set; }
        public dynamic defectHeader { get; set; }
        public dynamic defectDetail { get; set; }
        public dynamic comment { get; set; }
        public dynamic approveBy { get; set; }
        public dynamic approveDate { get; set; }
    }
}
