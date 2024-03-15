using System.Collections.Generic;

namespace Service.DInspect.Models.Request
{
    public class MappingRecommendedLubricant
    {
        public string key { get; set; } = "MAPPING";
        public string site { get; set; }
        public string modelId { get; set; }
        public string psTypeId { get; set; }
        public List<DetailMappingLubricant> detail { get; set; }
    }

    public class MappingRecommendedInterimLubricant
    {
        public string key { get; set; } = "MAPPING";
        public string site { get; set; }
        public string modelId { get; set; }
        public string eformType { get; set; }
        public List<DetailMappingLubricant> detail { get; set; }
    }

    public class DetailMappingLubricant
    {
        public string key { get; set; }
        public string taskKeyOilSample { get; set; }
        public string taskKeyOilChange { get; set; }
        public string taskKeyOilLevelCheck { get; set; }
        public string taskTopUpLevelCheck { get; set; }
        public string compartmentLubricant { get; set; }
        public string compartmentCode { get; set; }
        public string recommendedLubricant { get; set; }
        public string volume { get; set; }
        public string uoM { get; set; }
        public string lubricantType { get; set; }
        public string isSOS { get; set; }
    }
}
