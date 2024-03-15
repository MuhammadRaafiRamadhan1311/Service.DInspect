using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Service.DInspect.Interfaces;
using Service.DInspect.Models;
using Service.DInspect.Models.Enum;

namespace Service.DInspect.Services
{
    public class ServiceWrapper : IServiceWrapper
    {
        private IConnectionFactory _connectionFactory;
        private MySetting _appSetting;
        //private readonly IBlobStorageRepository _blobStorageRepository;
        private ILoggerFactory _logger;
        private string _accessToken;
        //private readonly TelemetryClient _telemetryClient;

        public ServiceWrapper(IOptions<MySetting> appSettings, IConnectionFactory connectionFactory, ILoggerFactory logger)
        {
            _appSetting = appSettings.Value;
            _connectionFactory = connectionFactory;
            //_blobStorageRepository = blobStorageRepository;
            _logger = logger;
            //_telemetryClient = telemetryClient;
        }

        public IConnectionFactory ConnectionFactory
        {
            get
            {
                return _connectionFactory;
            }
        }

        public MySetting AppSetting
        {
            get
            {
                return _appSetting;
            }
        }



        public ILoggerFactory LoggerFactory
        {
            get
            {
                return _logger;
            }
        }

        public string AccessToken
        {
            get
            {
                return _accessToken;
            }

            set
            {
                _accessToken = value;
            }
        }

 
        public DefectHeaderService DefectHeader => new DefectHeaderService(_appSetting, _connectionFactory, EnumContainer.DefectHeader, _accessToken);
        public DefectDetailService DefectDetail => new DefectDetailService(_appSetting, _connectionFactory, EnumContainer.DefectDetail, _accessToken);
 
    }
}
