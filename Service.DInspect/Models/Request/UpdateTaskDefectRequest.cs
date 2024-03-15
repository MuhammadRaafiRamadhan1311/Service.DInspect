using Service.DInspect.Models.Entity;

namespace Service.DInspect.Models.Request
{
    public class UpdateTaskDefectRequest : UpdateRequest
    {
        public string headerId { get; set; }
        public string workorder { get; set; }
        public string localInterventionStatus { get; set; }
        public DefectHeaderWithIdModel defectHeader { get; set; }
        public dynamic defectDetail { get; set; }
    }
}
