using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service.DInspect.Interfaces;
using Service.DInspect.Models;
using Service.DInspect.Models.Request;
using Service.DInspect.Services.Helpers;
using Service.DInspect.Models.Enum;
using Service.DInspect.Models.Response;
using Service.DInspect.Repositories;
using Service.DInspect.Helpers;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.DInspect.Services
{
    public class CalibrationDetailService : ServiceBase
    {
        protected string _container;
        protected IConnectionFactory _connectionFactory;

        public CalibrationDetailService(MySetting appSetting, IConnectionFactory connectionFactory, string container, string accessToken) : base(appSetting, connectionFactory, container, accessToken)
        {
            _container = container;
            _connectionFactory = connectionFactory;
            _repository = new CalibrationDetailRepository(connectionFactory, container);
        }

        public async Task<ServiceResult> UpdateTask(UpdateTaskRequest updateTaskRequest)
        {
            UpdateTaskServiceHelper service = new UpdateTaskServiceHelper(_appSetting, _connectionFactory, _container, _accessToken);
            var result = await service.UpdateTask(updateTaskRequest);

            return result;
        }
    }
}