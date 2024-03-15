using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Service.DInspect.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Service.DInspect.Models.Request;
using Service.DInspect.Models.Enum;
using Service.DInspect.Models;
using Service.DInspect.Interfaces;

namespace Service.DInspect.Services.Helpers
{
    public class GenericDefectServiceHelper
    {
        private string _container;
        private IRepositoryBase _repository;
        private IRepositoryBase _headerRepository;
        protected IRepositoryBase _defectHeaderRepository;
        protected IRepositoryBase _defectDetailRepository;
        protected MySetting _appSetting;
        protected string _accessToken;

        public GenericDefectServiceHelper(MySetting appSetting, IConnectionFactory connectionFactory, string container, string accessToken)
        {
            _container = container;
            _appSetting = appSetting;
            _accessToken = accessToken;
            if (container == EnumContainer.ServiceSheetDetail)
            {
                _repository = new ServiceSheetDetailRepository(connectionFactory, container);
                _headerRepository = new ServiceSheetHeaderRepository(connectionFactory, EnumContainer.ServiceSheetHeader);
                _defectHeaderRepository = new DefectHeaderRepository(connectionFactory, EnumContainer.DefectHeader);
                _defectDetailRepository = new DefectDetailRepository(connectionFactory, EnumContainer.DefectDetail);
            }
            else if (container == EnumContainer.Intervention)
            {
                _repository = new InterventionRepository(connectionFactory, container);
                _headerRepository = new InterventionRepository(connectionFactory, container);
                _defectHeaderRepository = new InterventionDefectHeaderRepository(connectionFactory, EnumContainer.InterventionDefectHeader);
                _defectDetailRepository = new InterventionDefectDetailRepository(connectionFactory, EnumContainer.InterventionDefectDetail);
            }
        }

        public async Task<ServiceResult> CreateGenericDefect(CreateGenericDefectRequest request)
        {
            try
            {
                #region Create Defect Header
                CreateRequest createHeaderRequest = new CreateRequest()
                {
                    employee = request.employee,
                    entity = request.defectHeader,
                };
                Dictionary<string, string> adjustHeaderFields = new Dictionary<string, string>
                {
                    { EnumQuery.Key, Guid.NewGuid().ToString() }
                };

                var defectHeaderResult = await _defectHeaderRepository.Create(createHeaderRequest, adjustHeaderFields);
                string defectHeaderId = defectHeaderResult[EnumCommonProperty.ID];

                if (string.IsNullOrEmpty(defectHeaderId))
                {
                    return new ServiceResult
                    {
                        Message = "Failed to get defect header!",
                        IsError = true
                    };
                }

                #endregion

                #region Create Defect Detail
                if (request.defectDetail != null)
                {
                    string defectDetailString = JsonConvert.SerializeObject(request.defectDetail);
                    defectDetailString = defectDetailString.Replace(EnumCommonProperty.ServerDateTime, ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerDateTime));
                    defectDetailString = defectDetailString.Replace(EnumCommonProperty.ServerTimeStamp, ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerTimeStamp));
                    JObject defectDetail = JObject.Parse(defectDetailString);
                    defectDetail.Add(EnumQuery.Key, Guid.NewGuid().ToString());

                    JObject detailObject = new JObject();
                    detailObject.Add(EnumQuery.Detail, defectDetail);

                    CreateRequest createDetailRequest = new CreateRequest()
                    {
                        employee = request.employee,
                        entity = detailObject
                    };
                    Dictionary<string, string> adjustDetailFields = new Dictionary<string, string>
                    {
                        { EnumQuery.Key, Guid.NewGuid().ToString() },
                        { EnumQuery.Workorder, request.defectHeader.workorder },
                        { EnumQuery.DefectHeaderId, defectHeaderId },
                        { EnumQuery.ServicesheetDetailId, request.defectHeader.serviceSheetDetailId },
                        { EnumQuery.InterventionId, request.defectHeader.interventionId },
                        { EnumQuery.InterventionHeaderId, request.defectHeader.interventionHeaderId },
                        { EnumQuery.TaskId, request.defectHeader.taskId }
                    };

                    var defectDetailResult = await _defectDetailRepository.Create(createDetailRequest, adjustDetailFields);
                    request.defectDetail = defectDetail;
                }

                return new ServiceResult
                {
                    Message = "Data updated successfully",
                    IsError = false,
                    Content = request
                };

                #endregion
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
