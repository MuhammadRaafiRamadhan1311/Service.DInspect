using Service.DInspect.Models.Enum;
using Service.DInspect.Repositories;
using Microsoft.ApplicationInsights;
using Service.DInspect.Models;
using Service.DInspect.Interfaces;

namespace Service.DInspect.Services
{
    public class CbmHitoryService : ServiceBase
    {
        private string _container;
        private IConnectionFactory _connectionFactory;

        public CbmHitoryService(MySetting appSetting, IConnectionFactory connectionFactory, string container, string accessToken, TelemetryClient _telemetryClient) : base(appSetting, connectionFactory, container, accessToken)
        {
            _container = container;
            _connectionFactory = connectionFactory;
            _repository = new CbmHitoryRepository(connectionFactory, container);
        }
    }
}
