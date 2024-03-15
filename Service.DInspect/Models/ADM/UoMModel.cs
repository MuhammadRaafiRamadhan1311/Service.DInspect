namespace Service.DInspect.Models.ADM
{
    public class UoMModel
    {
        public int mdUomId { get; set; }
        public string uom { get; set; }
        public string startDate { get; set; }
        public string endDate { get; set; }
        public bool isActive { get; set; }
    }
}