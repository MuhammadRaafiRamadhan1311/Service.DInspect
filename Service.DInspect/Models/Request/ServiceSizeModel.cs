using System.Collections.Generic;

namespace Service.DInspect.Models.Request
{
    public class ServiceSizeModel
    {
        public List<dynamic> PsType250 { get; set; }
        public List<dynamic> PsType500 { get; set; }
        public List<dynamic> PsType1000 { get; set; }
        public List<dynamic> PsType2000 { get; set; }
        public List<dynamic> PsType4000 { get; set; }
    }
}
