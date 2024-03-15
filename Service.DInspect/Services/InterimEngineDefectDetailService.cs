using Microsoft.Extensions.Logging;
using Service.DInspect.Repositories;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;
using Service.DInspect.Helpers;
using Service.DInspect.Models.Enum;
using Service.DInspect.Models.Request;
using Service.DInspect.Models;
using Service.DInspect.Models.Response;
using Service.DInspect.Models.Entity;
using Service.DInspect.Interfaces;

namespace Service.DInspect.Services
{
    public class InterimEngineDefectDetailService : ServiceBase
    {
        protected IRepositoryBase _serviceSheetHeaderRepository;
        protected IRepositoryBase _defectHeaderRepository;
        protected IRepositoryBase _psTypeSettingRepository;
        protected IRepositoryBase _taskCrackRepository;

        public InterimEngineDefectDetailService(MySetting appSetting, IConnectionFactory connectionFactory, string container, string accessToken) : base(appSetting, connectionFactory, container, accessToken)
        {
            _repository = new DefectDetailRepository(connectionFactory, container);
            _serviceSheetHeaderRepository = new InterimEngineDefectHeaderRepository(connectionFactory, EnumContainer.InterimEngineHeader);
            _defectHeaderRepository = new InterimEngineDefectHeaderRepository(connectionFactory, EnumContainer.InterimEngineDefectHeader);
            _psTypeSettingRepository = new PsTypeSettingRepository(connectionFactory, EnumContainer.PsTypeSetting);
            _taskCrackRepository = new TaskCrackRepository(connectionFactory, EnumContainer.TaskCrack);
        }

        public async Task<ServiceResult> GetPreviousCrack(PreviousCrackRequest previousCrackRequest)
        {
            try
            {
                List<PreviousCrackModel> result = new List<PreviousCrackModel>();

                Dictionary<string, object> psTypeSeetingParam = new Dictionary<string, object>();
                psTypeSeetingParam.Add(EnumQuery.ModelId, previousCrackRequest.modelId);
                psTypeSeetingParam.Add(EnumQuery.PsTypeId, previousCrackRequest.psTypeId);

                var psTypeSetting = await _psTypeSettingRepository.GetDataByParam(psTypeSeetingParam);

                if (psTypeSetting == null)
                    return new ServiceResult
                    {
                        Message = $"PS Type setting not found",
                        IsError = true
                    };

                string prevPsTypeId = psTypeSetting?.prevPsTypeId;

                Dictionary<string, object> currTaskCrackParam = new Dictionary<string, object>();
                currTaskCrackParam.Add(EnumQuery.ModelId, previousCrackRequest.modelId);
                currTaskCrackParam.Add(EnumQuery.PsTypeId, previousCrackRequest.psTypeId);
                currTaskCrackParam.Add(EnumQuery.TaskId, previousCrackRequest.taskId);

                var currTaskCracks = await _taskCrackRepository.GetDataListByParam(currTaskCrackParam);

                if (currTaskCracks == null || currTaskCracks.Count == 0)
                    return new ServiceResult
                    {
                        Message = $"Task crack setting with PS Type: {previousCrackRequest.psTypeId} and Task Id: {previousCrackRequest.taskId} not found",
                        IsError = true
                    };

                List<PreviousCrackResponse> serviceCracks = new List<PreviousCrackResponse>();

                foreach (var currTaskCrack in currTaskCracks)
                {
                    serviceCracks.Add(new PreviousCrackResponse()
                    {
                        workorder = previousCrackRequest.workorder,
                        taskId = currTaskCrack.taskId,
                        locationId = currTaskCrack.taskCrackCode,
                        locationDesc = currTaskCrack.locationDesc
                    });
                }

                Dictionary<string, object> prevServiceSheetParam = new Dictionary<string, object>();
                prevServiceSheetParam.Add(EnumQuery.ModelId, previousCrackRequest.modelId);
                prevServiceSheetParam.Add(EnumQuery.PsTypeId, prevPsTypeId);
                prevServiceSheetParam.Add(EnumQuery.Status, EnumStatus.EFormClosed);

                var prevServiceSheet = await _serviceSheetHeaderRepository.GetDataListByParam(prevServiceSheetParam, 1, EnumQuery.TsServiceEnd, EnumQuery.DESC);

                if (prevServiceSheet.Count > 0)
                {
                    var prevWorkorder = StaticHelper.GetPropValue(prevServiceSheet[0], EnumQuery.SSWorkorder)?.Value;

                    foreach (var serviceCrack in serviceCracks)
                    {
                        Dictionary<string, object> defectHeaderParam = new Dictionary<string, object>();
                        defectHeaderParam.Add(EnumQuery.Workorder, prevWorkorder);
                        defectHeaderParam.Add(EnumQuery.TaskId, previousCrackRequest.taskId);
                        defectHeaderParam.Add(EnumQuery.IsActive, "true");

                        var prevDefectHeader = await _defectHeaderRepository.GetDataListByParam(defectHeaderParam);

                        if (prevDefectHeader.Count > 0)
                        {
                            var prevDefectHeaderId = StaticHelper.GetPropValue(prevDefectHeader[0], EnumQuery.ID)?.Value;

                            Dictionary<string, object> defectDetailParam = new Dictionary<string, object>();
                            defectDetailParam.Add(EnumQuery.DefectHeaderId, prevDefectHeaderId);

                            var defectDetail = await _repository.GetDataByParam(defectDetailParam);
                            string jsonPreviousCrack = defectDetail?.detail?.previousCracks;

                            if (!string.IsNullOrEmpty(jsonPreviousCrack))
                            {
                                List<PreviousCrackModel> previousCracks = JsonConvert.DeserializeObject<List<PreviousCrackModel>>(jsonPreviousCrack);
                                PreviousCrackModel previousCrack = previousCracks.Where(x => x.locationId == serviceCrack.locationId).FirstOrDefault();

                                if (previousCrack == null)
                                {
                                    result.Add(new PreviousCrackModel()
                                    {
                                        locationId = serviceCrack.locationId,
                                        locationDesc = serviceCrack.locationDesc,
                                        previousCrack = "-",
                                        currentCrack = "0"
                                    });
                                }
                                else
                                {
                                    result.Add(new PreviousCrackModel()
                                    {
                                        locationId = serviceCrack.locationId,
                                        locationDesc = serviceCrack.locationDesc,
                                        previousCrack = previousCrack.currentCrack,
                                        currentCrack = "0"
                                    });
                                }
                            }
                            else
                            {
                                result.Add(new PreviousCrackModel()
                                {
                                    locationId = serviceCrack.locationId,
                                    locationDesc = serviceCrack.locationDesc,
                                    previousCrack = "-",
                                    currentCrack = "0"
                                });
                            }
                        }
                        else
                        {
                            result.Add(new PreviousCrackModel()
                            {
                                locationId = serviceCrack.locationId,
                                locationDesc = serviceCrack.locationDesc,
                                previousCrack = "-",
                                currentCrack = "0"
                            });
                        }
                    }
                }
                else
                {
                    foreach (var serviceCrack in serviceCracks)
                    {
                        result.Add(new PreviousCrackModel()
                        {
                            locationId = serviceCrack.locationId,
                            locationDesc = serviceCrack.locationDesc,
                            previousCrack = "-",
                            currentCrack = "0"
                        });
                    }
                }

                return new ServiceResult
                {
                    IsError = false,
                    Message = "Get previous crack successfully",
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

        public async Task<ServiceResult> GetServiceSheetCrack(string workOrder)
        {
            try
            {
                List<dynamic> result = new List<dynamic>();

                Dictionary<string, object> defectParam = new Dictionary<string, object>();
                defectParam.Add(EnumQuery.Workorder, workOrder);

                var defects = await _repository.GetDataListByParam(defectParam);

                if (defects != null && defects.Count > 0)
                {
                    foreach (var defect in defects)
                    {
                        JObject jDefect = JObject.FromObject(defect);
                        var previousCracks = jDefect[EnumQuery.Detail][EnumQuery.PreviousCracks];

                        if (previousCracks != null && !string.IsNullOrEmpty(previousCracks.ToString()))
                            result.Add(jDefect);
                    }
                }

                return new ServiceResult
                {
                    Message = "Get  service sheet crack successfully",
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
