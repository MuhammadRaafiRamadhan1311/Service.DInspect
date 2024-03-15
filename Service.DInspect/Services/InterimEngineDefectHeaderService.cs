using Service.DInspect.Repositories;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Service.DInspect.Services.Helpers;
using Service.DInspect.Models;
using Service.DInspect.Interfaces;
using Service.DInspect.Models.Request;

namespace Service.DInspect.Services
{
    public class InterimEngineDefectHeaderService : ServiceBase
    {
        private string _container;
        private IConnectionFactory _connectionFactory;

        public InterimEngineDefectHeaderService(MySetting appSetting, IConnectionFactory connectionFactory, string container, string accessToken) : base(appSetting, connectionFactory, container, accessToken)
        {
            _container = container;
            _connectionFactory = connectionFactory;
            _repository = new InterimEngineDefectHeaderRepository(connectionFactory, container);
        }

        public override async Task<ServiceResult> Put(UpdateRequest updateRequest)
        {
            try
            {
                DefectHeaderServiceHelper serviceHelper = new DefectHeaderServiceHelper(_connectionFactory, _container, _accessToken);
                dynamic result = await serviceHelper.Put(updateRequest);

                return new ServiceResult
                {
                    Message = "Data updated successfully",
                    IsError = false,
                    Content = result
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    Message = ex.Message,
                    IsError = true
                };
            }
        }

        public async Task<ServiceResult> GetInterimDefectHeader(string workOrder)
        {
            try
            {
                Version103Service version103Service = new Version103Service(_appSetting, _connectionFactory, _accessToken);
                var result = await version103Service.GetInterimDefectHeader(workOrder);

                return new ServiceResult()
                {
                    Message = "Get Interim Defect Header successfully",
                    IsError = false,
                    Content = result
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    Message = ex.Message,
                    IsError = true
                };
            }
        }
    }
}
