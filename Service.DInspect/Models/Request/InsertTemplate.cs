using DocumentFormat.OpenXml.Office2010.ExcelAc;
using System.Collections.Generic;

namespace Service.DInspect.Models.Request
{
    public class InsertTemplate
    {
        public string PsTypeId { get; set; }
        public List<dynamic> Details { get; set; }
    }
}
