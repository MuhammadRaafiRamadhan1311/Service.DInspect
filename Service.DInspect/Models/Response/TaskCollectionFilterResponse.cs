using DocumentFormat.OpenXml.Office2010.ExcelAc;
using System.Collections.Generic;

namespace Service.DInspect.Models.Response
{
    public class TaskCollectionFilterResponse
    {
        public List<string> ListModelId { get; set; }
        public List<string> ListPsTypeid { get; set; }
        public List<string> ListVersion { get; set; }
        public List<string> listCategroy { get; set; }
        public List<string> listSubtask { get; set; }
        public List<string> listStatus { get; set; }
        public List<string> listReleaseDate { get; set; }
    }
}
