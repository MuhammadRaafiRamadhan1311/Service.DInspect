using Service.DInspect.Models.Entity;
using System.Runtime.CompilerServices;

namespace Service.DInspect.Models.Response
{
    public class ServicePersonnelsResponse
    {
        public string key { get; set; }
        public EmployeeModel mechanic { get; set; }
        public string serviceStart { get; set; }
        public string serviceEnd { get; set; }
        public string shift { get; set; }
    }
}
