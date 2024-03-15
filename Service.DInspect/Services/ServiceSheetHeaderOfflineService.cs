//using Microsoft.AspNetCore.Connections;
using Service.DInspect.Models;
using Service.DInspect.Models.Entity;
using Service.DInspect.Models.Enum;
using Service.DInspect.Models.Request;
using Service.DInspect.Models.Response;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service.DInspect.Helpers;
using Service.DInspect.Interfaces;
using Service.DInspect.Models.ADM;
using Service.DInspect.Models.EHMS;
using Service.DInspect.Models.Helper;
using Service.DInspect.Models.Response;
using Service.DInspect.Repositories;
using Service.DInspect.Services.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Service.DInspect.Models;
using Service.DInspect.Repositories;
using Service.DInspect.Services;

namespace NetCosmosDB.Services
{
    public class ServiceSheetHeaderOfflineService : ServiceBase
    {
        //protected string _container;
        //protected IRepositoryBase _defectHeaderRepository;
        //protected IRepositoryBase _masterServiceSheetRepository;
        //protected IRepositoryBase _serviceHeaderRepository;
        //protected IRepositoryBase _serviceDetailRepository;
        //protected IRepositoryBase _interventioRepository;
        private IConnectionFactory _connectionFactory;

        public ServiceSheetHeaderOfflineService(MySetting appSetting, IConnectionFactory connectionFactory, string container, string accessToken) : base(appSetting, connectionFactory, container, accessToken)
        {
            //_container = container;
            _connectionFactory = connectionFactory;
            _repository = new ServiceSheetHeaderRepository(connectionFactory, container);
            //_defectHeaderRepository = new DefectHeaderRepository(connectionFactory, EnumContainer.DefectHeader);
            //_masterServiceSheetRepository = new MasterServiceSheetRepository(connectionFactory, EnumContainer.MasterServiceSheet);
            //_serviceDetailRepository = new ServiceSheetDetailRepository(connectionFactory, EnumContainer.ServiceSheetDetail);
            //_serviceHeaderRepository = new ServiceSheetHeaderRepository(connectionFactory, EnumContainer.ServiceSheetHeader);
            //_interventioRepository = new InterventionRepository(connectionFactory, EnumContainer.Intervention);
        }

        //public override async Task<ServiceResult> Put(UpdateRequest updateRequest)
        //{
        //    try
        //    {
        //        string updatedDate = ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerDateTime);
        //        string tsUpdatedDate = ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerTimeStamp);

        //        string jsonUpdateRequest = JsonConvert.SerializeObject(updateRequest);
        //        jsonUpdateRequest = jsonUpdateRequest.Replace(EnumCommonProperty.ServerDateTime, updatedDate).Replace(EnumCommonProperty.ServerTimeStamp, tsUpdatedDate);

        //        updateRequest = JsonConvert.DeserializeObject<UpdateRequest>(jsonUpdateRequest);

        //        UpdateParam updatedStatusParam = updateRequest.updateParams.Where(s => s.propertyParams.Any(x => x.propertyName == EnumQuery.Status)).FirstOrDefault();
        //        UpdateParam updatedDefectStatusParam = updateRequest.updateParams.Where(s => s.propertyParams.Any(x => x.propertyName == EnumQuery.DefectStatus)).FirstOrDefault();

        //        bool updatePersonnel = updateRequest.updateParams.Any(s => s.propertyParams.Any(x => x.propertyName == EnumQuery.Status || x.propertyName == EnumQuery.ServicePersonnels));
        //        bool updateDownloadHistory = updateRequest.updateParams.Any(s => s.propertyParams.Any(x => x.propertyName == EnumQuery.DownloadHistory));

        //        var rsc = await _repository.Get(updateRequest.id);

        //        #region Check Task Progress

        //        string headerIdProgress = StaticHelper.GetPropValue(rsc, EnumQuery.ID)?.ToString();
        //        string modelIdIdProgress = StaticHelper.GetPropValue(rsc, EnumQuery.ModelId)?.ToString();
        //        string psTypeIdProgress = StaticHelper.GetPropValue(rsc, EnumQuery.PsTypeId)?.ToString();
        //        string workOrderProgress = StaticHelper.GetPropValue(rsc, EnumQuery.SSWorkorder)?.ToString();

        //        Dictionary<string, object> paramProgress = new Dictionary<string, object>();
        //        paramProgress.Add(EnumQuery.HeaderId, headerIdProgress);
        //        paramProgress.Add(EnumQuery.ModelId, modelIdIdProgress);
        //        paramProgress.Add(EnumQuery.PsTypeId, psTypeIdProgress);
        //        paramProgress.Add(EnumQuery.SSWorkorder, workOrderProgress);

        //        ServiceSheetDetailService updateTaskService = new ServiceSheetDetailService(_appSetting, _connectionFactory, EnumContainer.ServiceSheetDetail, _accessToken, null);
        //        ServiceResult resultsProgress = await updateTaskService.GetTaskProgress(paramProgress);

        //        List<TaskProgressResponse> objTaskProgress = JsonConvert.DeserializeObject<List<TaskProgressResponse>>(JsonConvert.SerializeObject(resultsProgress.Content));

        //        if (updatedStatusParam != null)
        //        {
        //            var statusForm = updatedStatusParam.propertyParams.Where(x => x.propertyName == EnumQuery.Status).FirstOrDefault();

        //            if (statusForm != null && statusForm.propertyValue == EnumStatus.EFormSubmited &&
        //                objTaskProgress.Where(x => x.totalTask != x.doneTask && x.group != EnumGroup.DefectIdentifiedService).Any())
        //            {
        //                return new ServiceResult
        //                {
        //                    Message = "Please full fill your service sheet.",
        //                    IsError = true,
        //                    Content = objTaskProgress.Where(x => x.totalTask != x.doneTask && x.group != EnumGroup.DefectIdentifiedService).ToList()
        //                };
        //            }
        //        }
        //        #endregion

        //        #region get statusHistories
        //        if (updatedStatusParam != null || updatedDefectStatusParam != null)
        //        {
        //            //var status = await _repository.GetFieldValue(new GetFieldValueRequest()
        //            //{
        //            //    id = updateRequest.id,
        //            //    keyValue = updateParam.keyValue,
        //            //    propertyName = EnumQuery.StatusHistory
        //            //});

        //            #region validate status
        //            Dictionary<string, int> statusMap = new Dictionary<string, int>
        //            {
        //                {"Open", 1},
        //                {"On Progress", 2},
        //                {"Submited", 3},
        //                {"Close", 4}
        //            };

        //            string localStatus = updateRequest.localStatus?.ToString();
        //            string actualStatus = StaticHelper.GetPropValue(rsc, EnumQuery.Status)?.ToString();

        //            if (localStatus != actualStatus || localStatus == EnumStatus.EFormClosed)
        //            {
        //                if ((localStatus == EnumStatus.EFormOpen && statusMap[actualStatus] > statusMap[EnumStatus.EFormOnProgress]) || (localStatus == EnumStatus.EFormOnProgress && statusMap[actualStatus] >= statusMap[EnumStatus.EFormSubmited]))
        //                {
        //                    return new ServiceResult
        //                    {
        //                        Message = "", //no pop up error, go to next tab
        //                        IsError = true,
        //                        Content = null
        //                    };
        //                }
        //                else if (localStatus == EnumStatus.EFormClosed || localStatus == EnumStatus.EFormSubmited)
        //                {
        //                    Dictionary<string, int> statusDefectMap = new Dictionary<string, int>
        //                    {get
        //                        {"Approved (SPV)", 1},
        //                        {"Completed", 2}
        //                    };
        //                    string defectStatus = updatedDefectStatusParam.propertyParams.Where(x => x.propertyName == EnumQuery.DefectStatus).FirstOrDefault()?.propertyValue;
        //                    string actualDefectStatus = StaticHelper.GetPropValue(rsc, EnumQuery.DefectStatus)?.ToString();

        //                    if (statusDefectMap[actualDefectStatus] >= statusDefectMap[defectStatus])
        //                    {
        //                        return new ServiceResult
        //                        {
        //                            Message = "You cannot approve this digital service sheet because another user already approved.",
        //                            IsError = true,
        //                            Content = null
        //                        };
        //                    }
        //                }

        //            }
        //            #endregion

        //            if (updatedStatusParam != null)
        //            {
        //                var status = StaticHelper.GetPropValue(rsc, updatedStatusParam.keyValue, EnumQuery.StatusHistory);

        //                List<StatusHistoryModel> statusHistories = JsonConvert.DeserializeObject<List<StatusHistoryModel>>(JsonConvert.SerializeObject(status));
        //                if (statusHistories == null)
        //                    statusHistories = new List<StatusHistoryModel>();

        //                statusHistories.Add(new StatusHistoryModel()
        //                {
        //                    status = updatedStatusParam.propertyParams.Where(x => x.propertyName == EnumQuery.Status).FirstOrDefault()?.propertyValue,
        //                    updatedBy = updateRequest.employee,
        //                    updatedDate = updatedDate,
        //                    tsUpdatedDate = tsUpdatedDate
        //                });

        //                List<PropertyParam> propertyParams = new List<PropertyParam>() {
        //                    new PropertyParam()
        //                    {
        //                        propertyName = EnumQuery.StatusHistory,
        //                        propertyValue = JsonConvert.SerializeObject(statusHistories)
        //                    }
        //                };

        //                updateRequest.updateParams.Add(new UpdateParam()
        //                {
        //                    keyValue = updatedStatusParam.keyValue,
        //                    propertyParams = propertyParams
        //                });
        //            }
        //        }
        //        #endregion
        //        #region get personnel history
        //        if (updatePersonnel)
        //        {
        //            #region check if submit form
        //            for (int i = 0; i < updateRequest.updateParams.Count; i++)
        //            {
        //                for (int j = 0; j < updateRequest.updateParams[i].propertyParams.Count; j++)
        //                {
        //                    if (updateRequest.updateParams[i].keyValue == EnumGroup.General && updateRequest.updateParams[i].propertyParams[j].propertyName == EnumQuery.Status)
        //                    {
        //                        if (updateRequest.updateParams[i].propertyParams[j].propertyValue == EnumStatus.EFormSubmited)
        //                        {
        //                            List<PropertyParam> tempData = new List<PropertyParam>();
        //                            tempData.Add(new PropertyParam()
        //                            {
        //                                propertyName = EnumQuery.ServicePersonnels,
        //                                propertyValue = ""
        //                            });
        //                            updateRequest.updateParams.Add(new UpdateParam
        //                            {
        //                                keyValue = EnumGroup.General,
        //                                propertyParams = tempData
        //                            });
        //                        }
        //                    }
        //                }
        //            }
        //            #endregion
        //        }
        //        #endregion
        //        #region get download history
        //        if (updateDownloadHistory)
        //        {
        //            var history = StaticHelper.GetPropValue(rsc, EnumGroup.General, EnumQuery.DownloadHistory);

        //            List<DownloadHistoryModel> statusHistories = JsonConvert.DeserializeObject<List<DownloadHistoryModel>>(JsonConvert.SerializeObject(history));
        //            if (statusHistories == null)
        //                statusHistories = new List<DownloadHistoryModel>();

        //            statusHistories.Add(new DownloadHistoryModel()
        //            {
        //                downloadBy = updateRequest.employee,
        //                downloadDate = updatedDate,
        //            });

        //            List<PropertyParam> propertyParams = new List<PropertyParam>() {
        //                new PropertyParam()
        //                {
        //                    propertyName = EnumQuery.DownloadHistory,
        //                    propertyValue = JsonConvert.SerializeObject(statusHistories)
        //                }
        //            };

        //            updateRequest.updateParams.Add(new UpdateParam()
        //            {
        //                keyValue = EnumGroup.General,
        //                propertyParams = propertyParams
        //            });
        //        }
        //        #endregion

        //        foreach (var updateParam in updateRequest.updateParams)
        //        {
        //            foreach (var propertyParam in updateParam.propertyParams)
        //            {
        //                if (propertyParam.propertyName == EnumQuery.ServicePersonnels)
        //                {
        //                    var paramServicePersonels = new Dictionary<string, object>();
        //                    List<PropertyParam> propertyParamSubmit = new List<PropertyParam>();
        //                    paramServicePersonels.Add("id", updateRequest.id);

        //                    var oldDataPersonels = await GetDataByParam(paramServicePersonels);

        //                    List<ServicePersonnelsResponse> oldJsonPersonels = JsonConvert.DeserializeObject<List<ServicePersonnelsResponse>>(JsonConvert.SerializeObject(oldDataPersonels.Content.servicePersonnels));

        //                    ServicePersonnelsResponse newJsonPersonel = JsonConvert.DeserializeObject<ServicePersonnelsResponse>(propertyParam.propertyValue);
        //                    List<ServicePersonnelsResponse> newJsonPersonels = new List<ServicePersonnelsResponse>();
        //                    newJsonPersonels.Add(newJsonPersonel);

        //                    #region get shift data
        //                    CallAPIHelper callAPI = new CallAPIHelper(_accessToken);
        //                    ApiResponse response = await callAPI.Get($"{EnumUrl.GetShift}");
        //                    List<ShiftModel> shiftModel = JsonConvert.DeserializeObject<List<ShiftModel>>(JsonConvert.SerializeObject(response.Result.Content));
        //                    #endregion
        //                    string idPersonnel = "";
        //                    string serviceEnd = "";
        //                    UpdatedByResponse personnel = new UpdatedByResponse();

        //                    if (propertyParam.propertyValue == "") //submited form
        //                    {
        //                        foreach (var updateParamStatus in updateRequest.updateParams)
        //                        {
        //                            foreach (var propertyIdParamStatus in updateParamStatus.propertyParams)
        //                            {
        //                                if (propertyIdParamStatus.propertyName == EnumQuery.UpdatedBy)
        //                                {
        //                                    personnel = JsonConvert.DeserializeObject<UpdatedByResponse>(propertyIdParamStatus.propertyValue);
        //                                    idPersonnel = personnel.id;
        //                                }
        //                                if (propertyIdParamStatus.propertyName == EnumQuery.UpdatedDate)
        //                                {
        //                                    serviceEnd = propertyIdParamStatus.propertyValue;
        //                                    break;
        //                                }
        //                            }
        //                            propertyParamSubmit.Add(new PropertyParam { propertyName = EnumQuery.UpdatedBy, propertyValue = JsonConvert.SerializeObject(personnel) });
        //                            propertyParamSubmit.Add(new PropertyParam { propertyName = EnumQuery.UpdatedDate, propertyValue = serviceEnd });
        //                        }
        //                    }
        //                    CalculateServicePersonnelHelper serviceHelper = new CalculateServicePersonnelHelper();
        //                    var newJsonPersonnels = serviceHelper.CalculateServicePersonnel(oldJsonPersonels, newJsonPersonels, shiftModel, updateParam.propertyParams, idPersonnel, propertyParamSubmit);
        //                    propertyParam.propertyValue = JsonConvert.SerializeObject(newJsonPersonnels);
        //                }
        //                else if (updateParam.keyValue == EnumQuery.KeyValueRiskAssesment)
        //                {
        //                    var paramRiskAssesment = new Dictionary<string, object>();
        //                    paramRiskAssesment.Add("id", updateRequest.id);

        //                    var oldDataRiskAssesment = await GetDataByParam(paramRiskAssesment);

        //                    List<RiskAssesmentValue> oldJsonRiskAssesment = JsonConvert.DeserializeObject<List<RiskAssesmentValue>>(JsonConvert.SerializeObject(oldDataRiskAssesment.Content.riskAssesment[0].value));

        //                    List<RiskAssesmentValue> newJsonRiskAssesment = JsonConvert.DeserializeObject<List<RiskAssesmentValue>>(propertyParam.propertyValue);


        //                    //var newRiskAssesment = newJsonRiskAssesment.Where(x => !oldJsonRiskAssesment.Select(p => p.image).Contains(x.image)).ToList();
        //                    //if (newRiskAssesment.Count > 0)
        //                    //{
        //                    //    oldJsonRiskAssesment.AddRange(newRiskAssesment);
        //                    //}
        //                    //oldJsonRiskAssesment = oldJsonRiskAssesment.Where(x => newJsonRiskAssesment.Select(p => p.image).Contains(x.image)).ToList();


        //                    #region update new

        //                    string employeeId = updateRequest.employee.id;
        //                    oldJsonRiskAssesment.RemoveAll(oldJsonRiskAssesment => oldJsonRiskAssesment.updatedBy.id == employeeId);
        //                    oldJsonRiskAssesment.AddRange(newJsonRiskAssesment.Where(x => x.updatedBy.id == employeeId));

        //                    #endregion

        //                    propertyParam.propertyValue = JsonConvert.SerializeObject(oldJsonRiskAssesment);

        //                }
        //                else if (propertyParam.propertyName == EnumQuery.Log)
        //                {
        //                    var paramLog = new Dictionary<string, object>();
        //                    paramLog.Add("id", updateRequest.id);

        //                    var oldDataLog = await GetDataByParam(paramLog);

        //                    List<LogResponse> oldJsonLog = JsonConvert.DeserializeObject<List<LogResponse>>(JsonConvert.SerializeObject(oldDataLog.Content.log));

        //                    LogResponse newJsonLog = JsonConvert.DeserializeObject<LogResponse>(propertyParam.propertyValue);

        //                    oldJsonLog.Add(newJsonLog);

        //                    propertyParam.propertyValue = JsonConvert.SerializeObject(oldJsonLog);
        //                }
        //                else if (updateParam.keyValue == EnumGroup.General && propertyParam.propertyName == EnumQuery.Status)
        //                {
        //                    if (propertyParam.propertyValue == EnumStatus.EFormApprovedSPV)
        //                    {
        //                        propertyParam.propertyValue = EnumStatus.EFormClosed;
        //                    }
        //                }
        //            }
        //        }

        //        var result = await _repository.Update(updateRequest, rsc);

        //        if (updatedStatusParam != null)
        //        {
        //            //List<UpdateParam> resultModel = JsonConvert.DeserializeObject<List<UpdateParam>>(JsonConvert.SerializeObject(result.updateParams));
        //            //var sectionGeneral = resultModel.Where(x => x.keyValue == EnumGroup.General).FirstOrDefault();
        //            //string statusDataUpdated = sectionGeneral.propertyParams.Where(x => x.propertyName == EnumQuery.Status).FirstOrDefault()?.propertyValue;

        //            //var rsc = await _repository.Get(updateRequest.id);

        //            string statusEform = updatedStatusParam.propertyParams.Where(x => x.propertyName == EnumQuery.Status).FirstOrDefault()?.propertyValue;
        //            string paramStatus = statusEform == EnumStatus.EFormApprovedSPV ? EnumStatus.EFormClosed : statusEform;

        //            JObject content = new JObject();
        //            content.Add(EnumQuery.SSWorkorder, rsc[EnumQuery.SSWorkorder]);
        //            content.Add(EnumQuery.Status, paramStatus);
        //            //content.Add(EnumQuery.Status, statusDataUpdated);

        //            CallAPIHelper callAPI = new CallAPIHelper(_accessToken);
        //            ApiResponse response = await callAPI.Post(EnumUrl.UpdateStatusDailyScheduleUrl, content);

        //            if (response.StatusCode != 200 && response.StatusCode != 201)
        //                throw new Exception("Failed to update status in Daily Schedule");
        //        }

        //        return new ServiceResult
        //        {
        //            Message = "Data updated successfully",
        //            IsError = false,
        //            Content = result
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        return new ServiceResult
        //        {
        //            Message = ex.Message,
        //            IsError = true
        //        };
        //    }
        //}
    }
}
