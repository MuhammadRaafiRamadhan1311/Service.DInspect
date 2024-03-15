using Service.DInspect.Models.Entity;
using System.Collections.Generic;

namespace Service.DInspect.Models.Response
{
    public class LogResponse
    {
        public string id { get; set; }
        public EmployeeModel employee { get; set; }
        public string timeLoggedIn { get; set; }
        public string shift { get; set; }
        public bool isIHaveReadChecked { get; set; }
        public List<object> riskPhotos { get; set; }
    }
}
