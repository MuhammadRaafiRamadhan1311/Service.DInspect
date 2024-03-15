namespace Service.DInspect.Models.ADM
{
    public class EquipmentNumberModel
    {
        public long EquipmentNumberId { get; set; }
        public string EquipmentNumber { get; set; }
        public string EquipmentNumberDescription { get; set; }
        public string SerialNumber { get; set; }
        public string Level { get; set; }
        public bool IsActive { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
    }
}