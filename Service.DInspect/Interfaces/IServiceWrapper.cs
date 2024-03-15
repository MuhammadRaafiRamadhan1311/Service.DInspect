using Microsoft.Extensions.Logging;
using Service.DInspect.Interfaces;
using Service.DInspect.Models;
using Service.DInspect.Services;
using Service.DInspect.Models;
using Service.DInspect.Services;

namespace Service.DInspect.Interfaces
{
    public interface IServiceWrapper
    {
        MySetting AppSetting { get; }
        IConnectionFactory ConnectionFactory { get; }
        //IBlobStorageRepository BlobStorage { get; }
        ILoggerFactory LoggerFactory { get; }
        string AccessToken { get; set; }

        DefectHeaderService DefectHeader { get; }
        DefectDetailService DefectDetail { get; }

    }
}
