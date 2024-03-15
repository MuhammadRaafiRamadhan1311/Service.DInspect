using System.Collections.Generic;

namespace Service.DInspect.Models.Response
{
    public class ModelSiteMappingResponse
    {
        public string EquipmentModelId { get; set; }
        public string MenuName { get; set; }
        public List<ModelSiteMappingSubResponse> PsType { get; set; }
    }
}
