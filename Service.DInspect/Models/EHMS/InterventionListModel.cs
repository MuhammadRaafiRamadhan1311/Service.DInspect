namespace Service.DInspect.Models.EHMS
{
    public class InterventionListModel
    {
        public long? EquipmentId { get; set; }
        public string EquipmentNumber { get; set; }
        public string InterventionCode { get; set; }
        public string WorkOrder { get; set; }
        public string Model { get; set; }
        public long? ModelId { get; set; }
        public string Brand { get; set; }
        public string Status { get; set; }
        public string ComponentGroup { get; set; }
        public string Intervention { get; set; }
        public string KeyPbi { get; set; }
    }
}