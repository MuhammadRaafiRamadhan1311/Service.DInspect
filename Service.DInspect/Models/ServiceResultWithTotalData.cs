using System.Security.Cryptography.X509Certificates;

namespace Service.DInspect.Models
{
    public class ServiceResultWithTotalData : ServiceResult
    {
        public string Total { get; set; }
    }
}
