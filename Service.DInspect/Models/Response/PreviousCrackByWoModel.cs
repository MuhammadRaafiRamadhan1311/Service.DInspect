using Service.DInspect.Models.Entity;
using System.Collections.Generic;

namespace Service.DInspect.Models.Response
{
    public class PreviousCrackByWoModel
    {
        public string taskId { get; set; }
        public List<PreviousCrackModel> previousCrack { get; set; }
    }
}
