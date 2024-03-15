using Microsoft.Extensions.Logging;
using Service.DInspect.Repositories;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Service.DInspect.Services.Helpers;
using Service.DInspect.Models;
using Service.DInspect.Models.Enum;
using Service.DInspect.Models.Request;
using Service.DInspect.Interfaces;

namespace Service.DInspect.Services
{
    public class ServiceSheetDetailOfflineService : ServiceBase
    {
        protected string _container;
        protected IConnectionFactory _connectionFactory;
        protected IRepositoryBase _servicesheetHeaderRepository;
        protected IRepositoryBase _defectHeaderRepository;
        protected IRepositoryBase _defectDetailRepository;
        protected IRepositoryBase _psTypeSettingRepository;
        protected IRepositoryBase _taskTandemRepository;
        protected readonly TelemetryClient _telemetryClient;

        public ServiceSheetDetailOfflineService(MySetting appSetting, IConnectionFactory connectionFactory, string container, string accessToken, ILoggerFactory logger, TelemetryClient telemetryClient) : base(appSetting, connectionFactory, container, accessToken)
        {
            _container = container;
            _connectionFactory = connectionFactory;
            _repository = new ServiceSheetDetailRepository(connectionFactory, container);
            _servicesheetHeaderRepository = new ServiceSheetHeaderRepository(connectionFactory, EnumContainer.ServiceSheetHeader);
            _defectHeaderRepository = new DefectHeaderRepository(connectionFactory, EnumContainer.DefectHeader);
            _defectDetailRepository = new DefectDetailRepository(connectionFactory, EnumContainer.DefectDetail);
            _psTypeSettingRepository = new PsTypeSettingRepository(connectionFactory, EnumContainer.PsTypeSetting);
            _taskTandemRepository = new TaskTandemRepository(connectionFactory, EnumContainer.TaskTandem);
            _telemetryClient = telemetryClient;
        }

        public async Task<ServiceResult> UpdateTask(UpdateTaskRequest updateTaskRequest)
        {
            UpdateTaskServiceHelper service = new UpdateTaskServiceHelper(_appSetting, _connectionFactory, _container, _accessToken, _telemetryClient);
            var result = await service.UpdateTaskOffline(updateTaskRequest);

            return result;
        }
    }
}
