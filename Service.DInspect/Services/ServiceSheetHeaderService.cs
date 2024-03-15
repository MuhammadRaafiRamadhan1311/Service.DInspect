using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.EMMA;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service.DInspect.Helpers;
using Service.DInspect.Interfaces;
using Service.DInspect.Models;
using Service.DInspect.Models.ADM;
using Service.DInspect.Models.Entity;
using Service.DInspect.Models.Enum;
using Service.DInspect.Models.Helper;
using Service.DInspect.Models.Request;
using Service.DInspect.Models.Response;
using Service.DInspect.Services.Helpers;
using Service.DInspect.Repositories;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Service.DInspect.Services
{
    public class ServiceSheetHeaderService : ServiceBase
    {
        protected string _container;
        protected IRepositoryBase _defectHeaderRepository;
        protected IRepositoryBase _defectDetailRepository;
        protected IRepositoryBase _masterServiceSheetRepository;
        protected IRepositoryBase _serviceHeaderRepository;
        protected IRepositoryBase _serviceDetailRepository;
        protected IRepositoryBase _interventionRepository;
        protected IRepositoryBase _mappingLubricantRepository;
        protected IRepositoryBase _cbmHistoryRepository;
        protected IRepositoryBase _interventionDefectHeaderRepository;
        protected IRepositoryBase _interventionDefectDetailRepository;
        protected IConnectionFactory _connectionFactory;

        public ServiceSheetHeaderService(MySetting appSetting, IConnectionFactory connectionFactory, string container, string accessToken) : base(appSetting, connectionFactory, container, accessToken)
        {
            _container = container;
            _connectionFactory = connectionFactory;
            _repository = new ServiceSheetHeaderRepository(connectionFactory, container);
            _defectHeaderRepository = new DefectHeaderRepository(connectionFactory, EnumContainer.DefectHeader);
            _defectDetailRepository = new DefectDetailRepository(connectionFactory, EnumContainer.DefectDetail);
            _masterServiceSheetRepository = new MasterServiceSheetRepository(connectionFactory, EnumContainer.MasterServiceSheet);
            _serviceDetailRepository = new ServiceSheetDetailRepository(connectionFactory, EnumContainer.ServiceSheetDetail);
            _serviceHeaderRepository = new ServiceSheetHeaderRepository(connectionFactory, EnumContainer.ServiceSheetHeader);
            _interventionRepository = new InterventionRepository(connectionFactory, EnumContainer.Intervention);
            _mappingLubricantRepository = new InterventionRepository(connectionFactory, EnumContainer.LubricantMapping);
            _cbmHistoryRepository = new CbmHitoryRepository(connectionFactory, EnumContainer.CbmHistory);
            _interventionDefectHeaderRepository = new InterventionDefectHeaderRepository(connectionFactory, EnumContainer.InterventionDefectHeader);
            _interventionDefectDetailRepository = new InterventionDefectDetailRepository(connectionFactory, EnumContainer.InterventionDefectDetail);
        }

        public override async Task<ServiceResult> Put(UpdateRequest updateRequest)
        {
            try
            {
                string updatedDate = ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerDateTime);
                string tsUpdatedDate = ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerTimeStamp);

                string jsonUpdateRequest = JsonConvert.SerializeObject(updateRequest);
                jsonUpdateRequest = jsonUpdateRequest.Replace(EnumCommonProperty.ServerDateTime, updatedDate).Replace(EnumCommonProperty.ServerTimeStamp, tsUpdatedDate);

                updateRequest = JsonConvert.DeserializeObject<UpdateRequest>(jsonUpdateRequest);

                UpdateParam updatedStatusParam = updateRequest.updateParams.Where(s => s.propertyParams.Any(x => x.propertyName == EnumQuery.Status)).FirstOrDefault();
                UpdateParam updatedDefectStatusParam = updateRequest.updateParams.Where(s => s.propertyParams.Any(x => x.propertyName == EnumQuery.DefectStatus)).FirstOrDefault();

                bool updateDefectStatus = updateRequest.updateParams.Any(s => s.propertyParams.Any(x => x.propertyName == EnumQuery.DefectStatus));
                bool updatePersonnel = updateRequest.updateParams.Any(s => s.propertyParams.Any(x => x.propertyName == EnumQuery.Status || x.propertyName == EnumQuery.ServicePersonnels));
                bool updateDownloadHistory = updateRequest.updateParams.Any(s => s.propertyParams.Any(x => x.propertyName == EnumQuery.DownloadHistory));

                var rsc = await _repository.Get(updateRequest.id);

                #region get statusHistories
                if (updatedStatusParam != null || updatedDefectStatusParam != null)
                {
                    #region validate status
                    Dictionary<string, int> statusMap = new Dictionary<string, int>
                    {
                        {"Open", 1},
                        {"On Progress", 2},
                        {"Submited", 3},
                        {"Close", 4}
                    };

                    string localStatus = updateRequest.localStatus?.ToString();
                    string actualStatus = StaticHelper.GetPropValue(rsc, EnumQuery.Status)?.ToString();

                    if (localStatus != actualStatus || localStatus == EnumStatus.EFormClosed)
                    {
                        if (localStatus == EnumStatus.EFormOpen && statusMap[actualStatus] > statusMap[EnumStatus.EFormOnProgress] || localStatus == EnumStatus.EFormOnProgress && statusMap[actualStatus] >= statusMap[EnumStatus.EFormSubmited])
                        {
                            return new ServiceResult
                            {
                                Message = "", //no pop up error, go to next tab
                                IsError = true,
                                Content = null
                            };
                        }
                        else if (localStatus == EnumStatus.EFormClosed || localStatus == EnumStatus.EFormSubmited)
                        {
                            Dictionary<string, int> statusDefectMap = new Dictionary<string, int>
                            {
                                {"Approved (SPV)", 1},
                                {"Completed", 2}
                            };
                            string defectStatus = updatedDefectStatusParam.propertyParams.Where(x => x.propertyName == EnumQuery.DefectStatus).FirstOrDefault()?.propertyValue;
                            string actualDefectStatus = StaticHelper.GetPropValue(rsc, EnumQuery.DefectStatus)?.ToString();

                            if (statusDefectMap[actualDefectStatus] >= statusDefectMap[defectStatus])
                            {
                                var _updatedBy = rsc[EnumQuery.UpdatedBy];
                                string _approvedBy = _updatedBy[EnumQuery.Name];

                                string _message = EnumErrorMessage.ErrMsgServiceSheetDefectHeaderApproval.Replace(EnumCommonProperty.ApprovedBy, _approvedBy);

                                return new ServiceResult
                                {
                                    Message = _message,
                                    IsError = true,
                                    Content = null
                                };
                            }
                        }

                    }
                    #endregion

                    if (updatedStatusParam != null)
                    {
                        var status = StaticHelper.GetPropValue(rsc, updatedStatusParam.keyValue, EnumQuery.StatusHistory);

                        List<StatusHistoryModel> statusHistories = JsonConvert.DeserializeObject<List<StatusHistoryModel>>(JsonConvert.SerializeObject(status));
                        if (statusHistories == null)
                            statusHistories = new List<StatusHistoryModel>();

                        statusHistories.Add(new StatusHistoryModel()
                        {
                            status = updatedStatusParam.propertyParams.Where(x => x.propertyName == EnumQuery.Status).FirstOrDefault()?.propertyValue,
                            updatedBy = updateRequest.employee,
                            updatedDate = updatedDate,
                            tsUpdatedDate = tsUpdatedDate
                        });

                        List<PropertyParam> propertyParams = new List<PropertyParam>() {
                        new PropertyParam()
                        {
                            propertyName = EnumQuery.StatusHistory,
                            propertyValue = JsonConvert.SerializeObject(statusHistories)
                        }
                    };

                        updateRequest.updateParams.Add(new UpdateParam()
                        {
                            keyValue = updatedStatusParam.keyValue,
                            propertyParams = propertyParams
                        });
                    }
                }
                #endregion
                #region get personnel history
                if (updatePersonnel)
                {
                    #region check if submit form
                    for (int i = 0; i < updateRequest.updateParams.Count; i++)
                    {
                        for (int j = 0; j < updateRequest.updateParams[i].propertyParams.Count; j++)
                        {
                            if (updateRequest.updateParams[i].keyValue == EnumGroup.General && updateRequest.updateParams[i].propertyParams[j].propertyName == EnumQuery.Status)
                            {
                                if (updateRequest.updateParams[i].propertyParams[j].propertyValue == EnumStatus.EFormSubmited)
                                {
                                    List<PropertyParam> tempData = new List<PropertyParam>();
                                    tempData.Add(new PropertyParam()
                                    {
                                        propertyName = EnumQuery.ServicePersonnels,
                                        propertyValue = ""
                                    });
                                    updateRequest.updateParams.Add(new UpdateParam
                                    {
                                        keyValue = EnumGroup.General,
                                        propertyParams = tempData
                                    });
                                }
                            }
                        }
                    }
                    #endregion
                }
                #endregion
                #region get download history
                if (updateDownloadHistory)
                {
                    var history = StaticHelper.GetPropValue(rsc, EnumGroup.General, EnumQuery.DownloadHistory);

                    List<DownloadHistoryModel> statusHistories = JsonConvert.DeserializeObject<List<DownloadHistoryModel>>(JsonConvert.SerializeObject(history));
                    if (statusHistories == null)
                        statusHistories = new List<DownloadHistoryModel>();

                    statusHistories.Add(new DownloadHistoryModel()
                    {
                        downloadBy = updateRequest.employee,
                        downloadDate = updatedDate,
                    });

                    List<PropertyParam> propertyParams = new List<PropertyParam>() {
                        new PropertyParam()
                        {
                            propertyName = EnumQuery.DownloadHistory,
                            propertyValue = JsonConvert.SerializeObject(statusHistories)
                        }
                    };

                    updateRequest.updateParams.Add(new UpdateParam()
                    {
                        keyValue = EnumGroup.General,
                        propertyParams = propertyParams
                    });
                }
                #endregion

                foreach (var updateParam in updateRequest.updateParams)
                {
                    foreach (var propertyParam in updateParam.propertyParams)
                    {
                        if (propertyParam.propertyName == EnumQuery.ServicePersonnels)
                        {
                            var paramServicePersonels = new Dictionary<string, object>();
                            List<PropertyParam> propertyParamSubmit = new List<PropertyParam>();
                            paramServicePersonels.Add("id", updateRequest.id);

                            var oldDataPersonels = await GetDataByParam(paramServicePersonels);

                            List<ServicePersonnelsResponse> oldJsonPersonels = JsonConvert.DeserializeObject<List<ServicePersonnelsResponse>>(JsonConvert.SerializeObject(oldDataPersonels.Content.servicePersonnels));

                            ServicePersonnelsResponse newJsonPersonel = JsonConvert.DeserializeObject<ServicePersonnelsResponse>(propertyParam.propertyValue);
                            List<ServicePersonnelsResponse> newJsonPersonels = new List<ServicePersonnelsResponse>();
                            newJsonPersonels.Add(newJsonPersonel);

                            #region get shift data
                            CallAPIHelper callAPI = new CallAPIHelper(_accessToken);
                            ApiResponse response = await callAPI.Get($"{EnumUrl.GetShift}");
                            List<ShiftModel> shiftModel = JsonConvert.DeserializeObject<List<ShiftModel>>(JsonConvert.SerializeObject(response.Result.Content));
                            #endregion
                            string idPersonnel = "";
                            //string PersonnelName = "";
                            string serviceEnd = "";
                            UpdatedByResponse personnel = new UpdatedByResponse();
                            if (propertyParam.propertyValue == "") //submited form
                            {
                                foreach (var updateParamStatus in updateRequest.updateParams)
                                {
                                    foreach (var propertyIdParamStatus in updateParamStatus.propertyParams)
                                    {
                                        if (propertyIdParamStatus.propertyName == EnumQuery.UpdatedBy)
                                        {
                                            personnel = JsonConvert.DeserializeObject<UpdatedByResponse>(propertyIdParamStatus.propertyValue);
                                            idPersonnel = personnel.id;
                                        }
                                        if (propertyIdParamStatus.propertyName == EnumQuery.UpdatedDate)
                                        {
                                            serviceEnd = propertyIdParamStatus.propertyValue;
                                            break;
                                        }
                                    }
                                }
                                propertyParamSubmit.Add(new PropertyParam { propertyName = EnumQuery.UpdatedBy, propertyValue = JsonConvert.SerializeObject(personnel) });
                                propertyParamSubmit.Add(new PropertyParam { propertyName = EnumQuery.UpdatedDate, propertyValue = serviceEnd });
                            }
                            CalculateServicePersonnelHelper serviceHelper = new CalculateServicePersonnelHelper();

                            var newJsonPersonnels = serviceHelper.CalculateServicePersonnel(oldJsonPersonels, newJsonPersonels, shiftModel, updateParam.propertyParams, idPersonnel, propertyParamSubmit);
                            propertyParam.propertyValue = JsonConvert.SerializeObject(newJsonPersonnels);
                        }
                        else if (updateParam.keyValue == EnumQuery.KeyValueRiskAssesment)
                        {
                            var paramRiskAssesment = new Dictionary<string, object>();
                            paramRiskAssesment.Add("id", updateRequest.id);

                            var oldDataRiskAssesment = await GetDataByParam(paramRiskAssesment);

                            List<RiskAssesmentValue> oldJsonRiskAssesment = JsonConvert.DeserializeObject<List<RiskAssesmentValue>>(JsonConvert.SerializeObject(oldDataRiskAssesment.Content.riskAssesment[0].value));

                            List<RiskAssesmentValue> newJsonRiskAssesment = JsonConvert.DeserializeObject<List<RiskAssesmentValue>>(propertyParam.propertyValue);


                            //var newRiskAssesment = newJsonRiskAssesment.Where(x => !oldJsonRiskAssesment.Select(p => p.image).Contains(x.image)).ToList();
                            //if (newRiskAssesment.Count > 0)
                            //{
                            //    oldJsonRiskAssesment.AddRange(newRiskAssesment);
                            //}
                            //oldJsonRiskAssesment = oldJsonRiskAssesment.Where(x => newJsonRiskAssesment.Select(p => p.image).Contains(x.image)).ToList();


                            #region update new

                            string employeeId = updateRequest.employee.id;
                            oldJsonRiskAssesment.RemoveAll(oldJsonRiskAssesment => oldJsonRiskAssesment.updatedBy.id == employeeId);
                            oldJsonRiskAssesment.AddRange(newJsonRiskAssesment.Where(x => x.updatedBy.id == employeeId));

                            #endregion

                            propertyParam.propertyValue = JsonConvert.SerializeObject(oldJsonRiskAssesment);

                        }
                        //else if (propertyParam.propertyName == EnumQuery.Log)
                        //{
                        //    var paramLog = new Dictionary<string, object>();
                        //    paramLog.Add("id", updateRequest.id);

                        //    var oldDataLog = await GetDataByParam(paramLog);

                        //    List<LogResponse> oldJsonLog = JsonConvert.DeserializeObject<List<LogResponse>>(JsonConvert.SerializeObject(oldDataLog.Content.log));

                        //    List<LogResponse> newJsonLog = JsonConvert.DeserializeObject<List<LogResponse>>(propertyParam.propertyValue);

                        //    var newLog = newJsonLog.Where(x => !oldJsonLog.Select(p => p.id).Contains(x.id)).ToList();
                        //    if (newLog.Count > 0)
                        //    {
                        //        oldJsonLog.AddRange(newLog);
                        //    }

                        //    propertyParam.propertyValue = JsonConvert.SerializeObject(oldJsonLog);
                        //}
                        else if (updateParam.keyValue == EnumGroup.General && propertyParam.propertyName == EnumQuery.Status)
                        {
                            if (propertyParam.propertyValue == EnumStatus.EFormApprovedSPV)
                            {
                                propertyParam.propertyValue = EnumStatus.EFormClosed;
                            }
                        }
                    }
                }

                var result = await _repository.Update(updateRequest, rsc);

                if (updatedStatusParam != null)
                {
                    //List<UpdateParam> resultModel = JsonConvert.DeserializeObject<List<UpdateParam>>(JsonConvert.SerializeObject(result.updateParams));
                    //var sectionGeneral = resultModel.Where(x => x.keyValue == EnumGroup.General).FirstOrDefault();
                    //string statusDataUpdated = sectionGeneral.propertyParams.Where(x => x.propertyName == EnumQuery.Status).FirstOrDefault()?.propertyValue;

                    //var rsc = await _repository.Get(updateRequest.id);

                    string statusEform = updatedStatusParam.propertyParams.Where(x => x.propertyName == EnumQuery.Status).FirstOrDefault()?.propertyValue;
                    string paramStatus = statusEform == EnumStatus.EFormApprovedSPV ? EnumStatus.EFormClosed : statusEform;

                    JObject content = new JObject();
                    content.Add(EnumQuery.SSWorkorder, rsc[EnumQuery.SSWorkorder]);
                    content.Add(EnumQuery.Status, paramStatus);
                    content.Add(EnumQuery.DefectStatus, rsc[EnumQuery.DefectStatus] == EnumStatus.DefectCompleted ? true : false);
                    //content.Add(EnumQuery.Status, statusDataUpdated);

                    CallAPIHelper callAPI = new CallAPIHelper(_accessToken);
                    ApiResponse response = await callAPI.Post(EnumUrl.UpdateStatusDailyScheduleUrl, content);

                    if (response.StatusCode != 200 && response.StatusCode != 201)
                        throw new Exception("Failed to update status in Daily Schedule");

                    if (statusEform == EnumStatus.EFormOnProgress)
                    {
                        #region generate sos history
                        Dictionary<string, object> paramSosHis = new Dictionary<string, object>()
                                {
                                    { EnumQuery.SSWorkorder, rsc[EnumQuery.SSWorkorder]},
                                    { EnumQuery.Equipment, rsc[EnumQuery.Equipment]},
                                    { EnumQuery.EformType, EnumEformType.EformServiceSheet}
                                };

                        SOSService SOSService = new SOSService(_appSetting, _connectionFactory, EnumContainer.SOSHistory, _accessToken);
                        var resultsos = await SOSService.GenerateSosHistory(paramSosHis);
                        #endregion
                    }
                }

                #region input historical if cbm replacement & defect status = completed 
                if (updateDefectStatus)
                {
                    string _status = updatedDefectStatusParam.propertyParams[0].propertyValue.ToString();
                    if (_status == EnumStatus.DefectCompleted)
                    {
                        string _workorder = rsc[EnumQuery.SSWorkorder];
                        string _modelId = rsc[EnumQuery.ModelId];
                        string _psTypeId = rsc[EnumQuery.PsTypeId];
                        string _equipment = rsc[EnumQuery.Equipment];
                        string _siteId = rsc[EnumQuery.siteId];

                        DetailServiceSheet modelData = new DetailServiceSheet();
                        modelData.workOrder = _workorder;
                        modelData.taskKey = null;

                        var _repoDetail = new ServiceSheetDetailRepository(_connectionFactory, EnumContainer.ServiceSheetDetail);

                        var currentData = await _repoDetail.GetDataServiceSheetDetailByKey(modelData);

                        foreach (var item in currentData)
                        {
                            //string _serviceSheetDetailId = item[EnumQuery.ID];
                            //string _valueCurrent = item["nonCbmAdjustmentReplacementMeasurementValue"];
                            //string _ratingCurrent = item["nonCbmAdjustmentReplacementMeasurementRating"];
                            //string _valueReplacement = item["taskValue"];
                            //string _ratingReplacement = item["measurementValue"];

                            //store detail history into array
                            var list = new List<dynamic>();
                            list.Add(item);

                            CbmHistoryParam headerHisUpdate = new CbmHistoryParam();
                            headerHisUpdate.key = EnumGroup.General;
                            headerHisUpdate.workOrder = _workorder;
                            headerHisUpdate.equipment = _equipment;
                            headerHisUpdate.modelId = _modelId;
                            headerHisUpdate.psTypeId = _psTypeId;
                            headerHisUpdate.taskKey = item[EnumQuery.Key];
                            headerHisUpdate.taskDescription = item[EnumQuery.Description];
                            headerHisUpdate.source = "digital_service";
                            //headerHisUpdate.category = currentData[0].rating;
                            headerHisUpdate.category = item[EnumQuery.Category] + " " + item[EnumQuery.Rating];
                            headerHisUpdate.currentValue = (bool)item[EnumQuery.CbmAdjustmentReplacement] ? item[EnumQuery.CurrentValue] : item[EnumQuery.MeasurementValue];
                            headerHisUpdate.currentRating = (bool)item[EnumQuery.CbmAdjustmentReplacement] ? item[EnumQuery.CurrentRating] : item[EnumQuery.TaskValue];
                            headerHisUpdate.replacementValue = (bool)item[EnumQuery.CbmAdjustmentReplacement] ? item[EnumQuery.ReplacementValue] : item[EnumQuery.NonCbmAdjustmentReplacementMeasurementValue];
                            headerHisUpdate.replacementRating = (bool)item[EnumQuery.CbmAdjustmentReplacement] ? item[EnumQuery.ReplacementRating] : item[EnumQuery.NonCbmAdjustmentReplacementRating];
                            headerHisUpdate.siteId = _siteId;
                            headerHisUpdate.closedDate = updatedDate;
                            headerHisUpdate.closedBy = updateRequest.employee.id;
                            headerHisUpdate.serviceSheetDetailId = item[EnumQuery.ID];
                            headerHisUpdate.defectHeaderId = "";
                            headerHisUpdate.detail = new DetailCBMHistory();

                            DetailCBMHistory detailHisUpdate = new DetailCBMHistory();
                            detailHisUpdate.key = "HISTORY";
                            detailHisUpdate.category = item[EnumQuery.Category];
                            detailHisUpdate.rating = item[EnumQuery.Rating];
                            detailHisUpdate.history = list;

                            headerHisUpdate.detail = detailHisUpdate;

                            var modelHeader = new CreateRequest();
                            modelHeader.employee = new EmployeeModel();

                            modelHeader.employee.id = updateRequest.employee.id;
                            modelHeader.employee.name = updateRequest.employee.name;
                            modelHeader.entity = headerHisUpdate;

                            item.Remove("id");
                            item.Remove("uom");
                            item.Remove("taskNo");
                            item.Remove("measurementValue");
                            item.Remove("nonCbmAdjustmentReplacementMeasurementValue");
                            item.Remove("nonCbmAdjustmentReplacementRating");

                            var resultAddHeader = await _cbmHistoryRepository.Create(modelHeader);
                        }
                    }
                }
                #endregion

                return new ServiceResult
                {
                    Message = "Data updated successfully",
                    IsError = false,
                    Content = result
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<ServiceResult> GetDefectServiceSheet(string supervisor, string userGroup)
        {
            try
            {
                List<dynamic> result = new List<dynamic>();

                // get user profile
                CallAPIHelper callAPIHelperEmp = new CallAPIHelper(_accessToken);
                var empRes = await callAPIHelperEmp.Get(EnumUrl.GetDataEmployeeProfileById + $"/{supervisor}?ver=v1");

                IList<EmployeeHelperModel> empProfiles = JsonConvert.DeserializeObject<List<EmployeeHelperModel>>(JsonConvert.SerializeObject(empRes.Result.Content));
                IList<string> empUserGroups = empProfiles.Select(x => x.GroupName.ToLower()).ToList();
                userGroup = userGroup.ToLower();
                var siteId = empProfiles.FirstOrDefault().SiteId;

                if (!empUserGroups.Contains(userGroup))
                {
                    throw new Exception($"User is Not a Member of the {userGroup} Group");
                }

                // if siteId == HO Site then skip filter by site
                string groupFilter = EnumGeneralFilterGroup.Site;
                CallAPIHelper callAPIHelperFilter = new CallAPIHelper(_accessToken);
                var filterRes = await callAPIHelperFilter.Get(EnumUrl.GetGeneralFilter + $"?group={groupFilter}&ver=v1");
                IList<GeneralFilterHelperModel> filters = JsonConvert.DeserializeObject<List<GeneralFilterHelperModel>>(JsonConvert.SerializeObject(filterRes.Result.Content));

                if (filters.Any(x => x.Value == siteId))
                    siteId = null;

                // get daily schedules
                CallAPIHelper callAPIHelper = new CallAPIHelper(_accessToken);
                var dailySchedule = await callAPIHelper.Get(EnumUrl.GetAdmHistoryUrl + $"&siteId={siteId}");
                List<DailyScheduleModel> dailySchedules = JsonConvert.DeserializeObject<List<DailyScheduleModel>>(JsonConvert.SerializeObject(dailySchedule.Result.Content));

                Dictionary<string, object> paramServiceSheet = new Dictionary<string, object>();
                paramServiceSheet.Add(EnumQuery.IsActive, "true");
                paramServiceSheet.Add(EnumQuery.IsDeleted, "false");
                if (siteId != null) paramServiceSheet.Add(EnumQuery.siteId, siteId);
                var serviceSheetHeaders = await _repository.GetDataListByParamJArray(paramServiceSheet);

                foreach (var serviceSheet in dailySchedules)
                {
                    var header = serviceSheetHeaders.FilterEqual(EnumQuery.SSWorkorder, serviceSheet.workOrder).FirstOrDefault();

                    if (header != null)
                    {
                        JToken jHeader = JToken.FromObject(header);

                        if (header != null && userGroup == EnumPosition.Supervisor.ToLower() && jHeader[EnumQuery.Status].ToString() == EnumStatus.EFormSubmited || header != null && userGroup == EnumPosition.Planner.ToLower() && jHeader[EnumQuery.Status].ToString() == EnumStatus.EFormClosed && jHeader[EnumQuery.DefectStatus].ToString() == EnumStatus.DefectApprovedSPV ||
                           header != null && userGroup == EnumPosition.Supervisor.ToLower() && jHeader[EnumQuery.Status].ToString() == EnumStatus.EFormOnProgress
                            )
                        {
                            Dictionary<string, object> paramDefect = new Dictionary<string, object>();
                            paramDefect.Add(EnumQuery.SSWorkorder, serviceSheet.workOrder);
                            //var defectHeaders = await _defectHeaderRepository.GetActiveDataJArray();
                            var defects = await _defectHeaderRepository.GetDataListByParamJArray(paramDefect);

                            //var defects = defectHeaders.FilterEqual(EnumQuery.Workorder, serviceSheet.workOrder);

                            //if (defects.Count > 0)
                            //{
                            string jsonDefects = JsonConvert.SerializeObject(defects);
                            List<DefectHelperModel> defectHelpers = JsonConvert.DeserializeObject<List<DefectHelperModel>>(jsonDefects);

                            if (defectHelpers.Any(x => x.status != EnumStatus.DefectApprovedSPV && x.status != EnumStatus.DefectCompleted))
                            {
                                serviceSheet.eFormId = jHeader[EnumQuery.ID].ToString();
                                serviceSheet.eFormKey = jHeader[EnumQuery.Key].ToString();
                                serviceSheet.defectStatus = jHeader[EnumQuery.DefectStatus].ToString();
                                serviceSheet.eFormStatus = jHeader[EnumQuery.Status].ToString();
                                serviceSheet.form = jHeader[EnumQuery.Form].ToString() + " " + jHeader[EnumQuery.Equipment].ToString() + " " + jHeader[EnumQuery.SSWorkorder].ToString();

                                int countNeedAct = defectHelpers.Where(x => x.category == EnumTaskType.Normal && x.taskValue == EnumTaskValue.NormalNotOK && x.defectType == EnumDefectType.Yes
                                || x.category == EnumTaskType.Crack && x.taskValue == EnumTaskValue.CrackNotOKYes).Count();

                                int countOnpAct = defectHelpers.Where(x => x.category == EnumTaskType.Normal && x.taskValue == EnumTaskValue.NormalNotOK && x.defectType == EnumDefectType.Yes && x.status == EnumStatus.DefectSubmit
                                || x.category == EnumTaskType.Crack && x.taskValue == EnumTaskValue.CrackNotOKYes && x.status == EnumStatus.DefectSubmit).Count();

                                if (countNeedAct == countOnpAct)
                                    serviceSheet.status = EnumStatus.DefectNotAcknowledge;
                                else
                                    serviceSheet.status = EnumStatus.DefectNotApproved;

                                result.Add(serviceSheet);
                            }
                            else
                            {
                                serviceSheet.eFormId = jHeader[EnumQuery.ID].ToString();
                                serviceSheet.eFormKey = jHeader[EnumQuery.Key].ToString();
                                serviceSheet.defectStatus = jHeader[EnumQuery.DefectStatus].ToString();
                                serviceSheet.eFormStatus = jHeader[EnumQuery.Status].ToString();
                                serviceSheet.form = jHeader[EnumQuery.Form].ToString() + " " + jHeader[EnumQuery.Equipment].ToString() + " " + jHeader[EnumQuery.SSWorkorder].ToString();

                                result.Add(serviceSheet);
                            }
                            //}
                        }
                    }
                }

                return new ServiceResult
                {
                    Message = "Get defect service sheet successfully",
                    IsError = false,
                    Content = result
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<ServiceResult> GetDefectServiceSheetV2(string supervisor, string userGroup)
        {
            try
            {
                List<dynamic> result = new List<dynamic>();

                // get user profile
                CallAPIHelper callAPIHelperEmp = new CallAPIHelper(_accessToken);
                var empRes = await callAPIHelperEmp.Get(EnumUrl.GetDataEmployeeProfileById + $"/{supervisor}?ver=v1");

                IList<EmployeeHelperModel> empProfiles = JsonConvert.DeserializeObject<List<EmployeeHelperModel>>(JsonConvert.SerializeObject(empRes.Result.Content));
                IList<string> empUserGroups = empProfiles.Select(x => x.GroupName.ToLower()).ToList();
                userGroup = userGroup.ToLower();
                var siteId = empProfiles.FirstOrDefault().SiteId;

                if (!empUserGroups.Contains(userGroup))
                {
                    throw new Exception($"User is Not a Member of the {userGroup} Group");
                }

                // if siteId == HO Site then skip filter by site
                string groupFilter = EnumGeneralFilterGroup.Site;
                CallAPIHelper callAPIHelperFilter = new CallAPIHelper(_accessToken);
                var filterRes = await callAPIHelperFilter.Get(EnumUrl.GetGeneralFilter + $"?group={groupFilter}&ver=v1");
                IList<GeneralFilterHelperModel> filters = JsonConvert.DeserializeObject<List<GeneralFilterHelperModel>>(JsonConvert.SerializeObject(filterRes.Result.Content));

                if (filters.Any(x => x.Value == siteId))
                    siteId = null;

                // get daily schedules
                CallAPIHelper callAPIHelper = new CallAPIHelper(_accessToken);
                var dailySchedule = await callAPIHelper.Get(EnumUrl.GetAdmHistoryUrl + $"&siteId={siteId}");
                List<DailyScheduleModel> dailySchedules = JsonConvert.DeserializeObject<List<DailyScheduleModel>>(JsonConvert.SerializeObject(dailySchedule.Result.Content));

                List<string> fieldsParam = new List<string>() { EnumQuery.SSWorkorder, EnumQuery.ID, EnumQuery.Key, EnumQuery.DefectStatus, EnumQuery.Status, EnumQuery.Form, EnumQuery.Equipment };
                JArray dailyWorkOrderList = JArray.FromObject(dailySchedules.Select(x => x.workOrder));

                Dictionary<string, object> paramServiceSheet = new Dictionary<string, object>();
                paramServiceSheet.Add(EnumQuery.Fields, fieldsParam);
                paramServiceSheet.Add(EnumQuery.IsActive, "true");
                paramServiceSheet.Add(EnumQuery.IsDeleted, "false");
                paramServiceSheet.Add(EnumQuery.SSWorkorder, dailyWorkOrderList);
                if (siteId != null) paramServiceSheet.Add(EnumQuery.siteId, siteId);

                var serviceSheetHeaders = await _repository.GetDataListByParamJArray(paramServiceSheet);

                List<string> fieldsParamDefect = new List<string>() { EnumQuery.Workorder, EnumQuery.Status, EnumQuery.Category, EnumQuery.TaskValue, EnumQuery.DefectType };
                Dictionary<string, object> paramDefectList = new Dictionary<string, object>();
                paramDefectList.Add(EnumQuery.Fields, fieldsParamDefect);
                paramDefectList.Add(EnumQuery.Workorder, dailyWorkOrderList);
                paramDefectList.Add(EnumQuery.IsActive, "true");
                paramDefectList.Add(EnumQuery.IsDeleted, "false");

                var dataDefect = await _defectHeaderRepository.GetDataListByParamJArray(paramDefectList);

                IEnumerable<DefectHeaderResponse> dataDefectHeader = JsonConvert.DeserializeObject<List<DefectHeaderResponse>>(JsonConvert.SerializeObject(dataDefect));
                IEnumerable<ServiceHeaderResponse> dataServiceHeader = JsonConvert.DeserializeObject<List<ServiceHeaderResponse>>(JsonConvert.SerializeObject(serviceSheetHeaders));

                dataServiceHeader = dataServiceHeader.Where(x => x.status == EnumStatus.EFormSubmited && userGroup == EnumPosition.Supervisor.ToLower() ||
                                                                 x.status == EnumStatus.EFormOnProgress && userGroup == EnumPosition.Supervisor.ToLower() ||
                                                                 x.status == EnumStatus.EFormClosed && x.defectStatus == EnumStatus.DefectApprovedSPV && userGroup == EnumPosition.Planner.ToLower()
                                                                 );



                //var dataServiceApproval = dataServiceHeader.Where(x => x.status == EnumStatus.DefectApprovedSPV || x.status == EnumStatus.EFormOnProgress).ToList();
                var masterDailySchedule = dailySchedules.Where(x => dataServiceHeader.Select(o => o.workOrder).Contains(x.workOrder)).ToList();


                var query = (from serviceSheet in dataServiceHeader
                             join defectHeaderGroup in dataDefectHeader.GroupBy(x => x.workorder) on serviceSheet.workOrder equals defectHeaderGroup.Key into defectHeaderGroups
                             from defectHeader in defectHeaderGroups.DefaultIfEmpty()
                             join masterDaily in masterDailySchedule on serviceSheet.workOrder equals masterDaily.workOrder
                             select new MergeServiceSheetDefectHeader
                             {
                                 dailyScheduleId = masterDaily.dailyScheduleId,
                                 unitNumber = masterDaily.unitNumber,
                                 equipmentModel = masterDaily.equipmentModel,
                                 brand = masterDaily.brand,
                                 smuDue = masterDaily.smuDue,
                                 psType = masterDaily.psType,
                                 dateService = masterDaily.dateService,
                                 shift = masterDaily.shift,
                                 isActive = masterDaily.isActive,
                                 startDate = masterDaily.startDate,
                                 endDate = masterDaily.endDate,
                                 createdOn = masterDaily.createdOn,
                                 createdBy = masterDaily.createdBy,
                                 changedOn = masterDaily.changedOn,
                                 changedBy = masterDaily.changedBy,
                                 isDownload = masterDaily.isDownload,
                                 serviceStart = masterDaily.serviceStart,
                                 serviceEnd = masterDaily.serviceEnd,
                                 workOrder = serviceSheet.workOrder,
                                 eFormId = serviceSheet.id,
                                 eFormKey = serviceSheet.key,
                                 defectStatus = serviceSheet.defectStatus,
                                 eFormStatus = serviceSheet.status,
                                 form = serviceSheet.form + " " + serviceSheet.equipment + " " + serviceSheet.workOrder,
                                 status = defectHeader != null ? defectHeader.Select(grp => grp).Where(o => o.category == EnumTaskType.Normal && o.taskValue == EnumTaskValue.NormalNotOK && o.defectType == EnumDefectType.Yes ||
                                                                                     o.category == EnumTaskType.Crack && o.taskValue == EnumTaskValue.CrackNotOKYes).Count() ==
                                          defectHeader.Select(grp => grp).Where(p => p.category == EnumTaskType.Normal && p.taskValue == EnumTaskValue.NormalNotOK && p.defectType == EnumDefectType.Yes && p.status == EnumStatus.DefectSubmit ||
                                                                                     p.category == EnumTaskType.Crack && p.taskValue == EnumTaskValue.CrackNotOKYes && p.status == EnumStatus.DefectSubmit).Count() ?
                                          EnumStatus.DefectNotAcknowledge : EnumStatus.DefectNotApproved : ""
                             }).ToList();

                #region Data Config
                //foreach (var serviceSheet in dailySchedules)
                //{
                //    var header = serviceSheetHeaders.FilterEqual(EnumQuery.SSWorkorder, serviceSheet.workOrder).FirstOrDefault();

                //    if (header != null)
                //    {
                //        JToken jHeader = JToken.FromObject(header);

                //        if ((header != null && userGroup == EnumPosition.Supervisor.ToLower() && jHeader[EnumQuery.Status].ToString() == EnumStatus.EFormSubmited) || (header != null && userGroup == EnumPosition.Planner.ToLower() && jHeader[EnumQuery.Status].ToString() == EnumStatus.EFormClosed && jHeader[EnumQuery.DefectStatus].ToString() == EnumStatus.DefectApprovedSPV) ||
                //           (header != null && userGroup == EnumPosition.Supervisor.ToLower() && jHeader[EnumQuery.Status].ToString() == EnumStatus.EFormOnProgress)
                //            )
                //        {
                //            Dictionary<string, object> paramDefect = new Dictionary<string, object>();
                //            paramDefect.Add(EnumQuery.SSWorkorder, serviceSheet.workOrder);
                //            //var defectHeaders = await _defectHeaderRepository.GetActiveDataJArray();
                //            //var defects = await _defectHeaderRepository.GetDataListByParamJArray(paramDefect);

                //            //var defects = defectHeaders.FilterEqual(EnumQuery.Workorder, serviceSheet.workOrder);

                //            //if (defects.Count > 0)
                //            //{


                //            //string jsonDefects = JsonConvert.SerializeObject(defects);
                //            string jsonDefects = JsonConvert.SerializeObject(dataDefect);
                //            List<DefectHelperModel> defectHelpers = JsonConvert.DeserializeObject<List<DefectHelperModel>>(jsonDefects);

                //            if (defectHelpers.Any(x => x.status != EnumStatus.DefectApprovedSPV && x.status != EnumStatus.DefectCompleted))
                //            {
                //                serviceSheet.eFormId = jHeader[EnumQuery.ID].ToString();
                //                serviceSheet.eFormKey = jHeader[EnumQuery.Key].ToString();
                //                serviceSheet.defectStatus = jHeader[EnumQuery.DefectStatus].ToString();
                //                serviceSheet.eFormStatus = jHeader[EnumQuery.Status].ToString();
                //                serviceSheet.form = jHeader[EnumQuery.Form].ToString() + " " + jHeader[EnumQuery.Equipment].ToString() + " " + jHeader[EnumQuery.SSWorkorder].ToString();

                //                int countNeedAct = defectHelpers.Where(x => (x.category == EnumTaskType.Normal && x.taskValue == EnumTaskValue.NormalNotOK && x.defectType == EnumDefectType.Yes)
                //                || (x.category == EnumTaskType.Crack && x.taskValue == EnumTaskValue.CrackNotOKYes)).Count();

                //                int countOnpAct = defectHelpers.Where(x => (x.category == EnumTaskType.Normal && x.taskValue == EnumTaskValue.NormalNotOK && x.defectType == EnumDefectType.Yes && x.status == EnumStatus.DefectSubmit)
                //                || (x.category == EnumTaskType.Crack && x.taskValue == EnumTaskValue.CrackNotOKYes && x.status == EnumStatus.DefectSubmit)).Count();

                //                if (countNeedAct == countOnpAct)
                //                    serviceSheet.status = EnumStatus.DefectNotAcknowledge;
                //                else
                //                    serviceSheet.status = EnumStatus.DefectNotApproved;

                //                result.Add(serviceSheet);
                //            }
                //            else
                //            {
                //                serviceSheet.eFormId = jHeader[EnumQuery.ID].ToString();
                //                serviceSheet.eFormKey = jHeader[EnumQuery.Key].ToString();
                //                serviceSheet.defectStatus = jHeader[EnumQuery.DefectStatus].ToString();
                //                serviceSheet.eFormStatus = jHeader[EnumQuery.Status].ToString();
                //                serviceSheet.form = jHeader[EnumQuery.Form].ToString() + " " + jHeader[EnumQuery.Equipment].ToString() + " " + jHeader[EnumQuery.SSWorkorder].ToString();

                //                result.Add(serviceSheet);
                //            }
                //            //}
                //        }
                //    }
                //}
                #endregion

                return new ServiceResult
                {
                    Message = "Get defect service sheet successfully",
                    IsError = false,
                    Content = query
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<ServiceResult> GetDefectServiceSheetClose(string supervisor, string psTypeId, string equipmentModel, string unitNumber)
        {
            try
            {
                List<dynamic> result = new List<dynamic>();

                CallAPIHelper callAPIHelper = new CallAPIHelper(_accessToken);
                var dailySchedule = await callAPIHelper.Get(EnumUrl.GetAdmHistoryUrl);
                List<DailyScheduleModel> dailySchedules = JsonConvert.DeserializeObject<List<DailyScheduleModel>>(JsonConvert.SerializeObject(dailySchedule.Result.Content));

                List<string> fieldParams = new List<string>() { EnumQuery.SSWorkorder, EnumQuery.ID, EnumQuery.Key, EnumQuery.DefectStatus, EnumQuery.Status, EnumQuery.IsDownload, EnumQuery.Form, EnumQuery.ServiceStart, EnumQuery.ServiceEnd };
                JArray dailyWorkorderList = JArray.FromObject(dailySchedules.Select(x => x.workOrder));

                Dictionary<string, object> _params = new Dictionary<string, object>();

                _params.Add(EnumQuery.ModelId, equipmentModel);
                _params.Add(EnumQuery.PsTypeId, psTypeId);
                _params.Add(EnumQuery.Equipment, unitNumber);
                _params.Add(EnumQuery.SSWorkorder, dailyWorkorderList);
                _params.Add(EnumQuery.Fields, fieldParams);

                //var serviceSheetHeaders = await _repository.GetActiveDataJArray();
                var serviceSheetHeaders = await _repository.GetDataListByParamJArray(_params);

                foreach (var serviceSheet in dailySchedules)
                {
                    var header = serviceSheetHeaders.FilterEqual(EnumQuery.SSWorkorder, serviceSheet.workOrder).FirstOrDefault();

                    if (header != null)
                    {
                        JToken jHeader = JToken.FromObject(header);

                        serviceSheet.defectStatus = jHeader[EnumQuery.DefectStatus].ToString();

                        if (header != null && jHeader[EnumQuery.Status].ToString() == EnumStatus.EFormClosed)
                        {
                            Dictionary<string, object> defectParam = new Dictionary<string, object>();
                            defectParam.Add(EnumQuery.Workorder, serviceSheet.workOrder);
                            defectParam.Add(EnumQuery.IsActive, "true");

                            var defects = await _defectHeaderRepository.GetDataListByParam(defectParam);

                            if (defects.Count > 0)
                            {
                                serviceSheet.eFormId = jHeader[EnumQuery.ID].ToString();
                                serviceSheet.eFormKey = jHeader[EnumQuery.Key].ToString();
                                serviceSheet.status = jHeader[EnumQuery.Status].ToString();
                                serviceSheet.isDownload = jHeader[EnumQuery.IsDownload].ToString();
                                serviceSheet.eFormStatus = jHeader[EnumQuery.Status].ToString();
                                serviceSheet.form = jHeader[EnumQuery.Form].ToString();
                                serviceSheet.serviceStart = jHeader[EnumQuery.ServiceStart].ToString();
                                serviceSheet.serviceEnd = jHeader[EnumQuery.ServiceEnd].ToString();

                                result.Add(serviceSheet);
                            }
                        }
                    }
                }

                return new ServiceResult
                {
                    Message = "Get defect service sheet successfully",
                    IsError = false,
                    Content = result
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<ServiceResult> GetApprovalServiceSheet(string supervisor)
        {
            try
            {
                List<dynamic> result = new List<dynamic>();

                CallAPIHelper callAPIHelper = new CallAPIHelper(_accessToken);
                var dailySchedule = await callAPIHelper.Get(EnumUrl.GetAdmHistoryUrl);
                List<DailyScheduleModel> dailySchedules = JsonConvert.DeserializeObject<List<DailyScheduleModel>>(JsonConvert.SerializeObject(dailySchedule.Result.Content));

                var serviceSheetHeaders = await _repository.GetActiveDataJArray();
                var defectHeaders = await _defectHeaderRepository.GetActiveDataJArray();

                foreach (var serviceSheet in dailySchedules)
                {
                    var jHeader = serviceSheetHeaders.FilterEqual(EnumQuery.SSWorkorder, serviceSheet.workOrder).FirstOrDefault();

                    if (jHeader != null && jHeader[EnumQuery.Status].ToString() == EnumStatus.EFormApprovedSPV)
                    {
                        result.Add(serviceSheet);
                    }
                    else if (jHeader != null && jHeader[EnumQuery.Status].ToString() == EnumStatus.EFormSubmited)
                    {
                        var defects = defectHeaders.FilterEqual(EnumQuery.Workorder, serviceSheet.workOrder)
                            .FilterEqual(EnumQuery.IsActive, "true");

                        if (defects.Count == 0)
                            result.Add(serviceSheet);
                    }
                }

                return new ServiceResult
                {
                    Message = "Get approval service sheet successfully",
                    IsError = false,
                    Content = result
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<ServiceResult> GetDefectServiceSheetHistory(string supervisor, string psTypeId, string equipmentModel, string unitNumber)
        {
            try
            {
                List<DailyScheduleModel> result = new List<DailyScheduleModel>();
                var dataResult = await GetDefectServiceSheetClose(supervisor, psTypeId, equipmentModel, unitNumber);

                if (dataResult.Content.Count > 0)
                {
                    List<DailyScheduleModel> dataDefectList = JsonConvert.DeserializeObject<List<DailyScheduleModel>>(JsonConvert.SerializeObject(dataResult.Content));
                    //dataDefectList = dataDefectList.Where(x => Int32.Parse(x.psType) < Int32.Parse(psTypeId)).ToList();

                    foreach (var item in dataDefectList)
                    {
                        item.equipmentModel = $"{item.brand} {item.equipmentModel}";
                    }

                    result = (from a in dataDefectList
                              orderby a.changedOn descending
                              select a).Where(x => x.equipmentModel == equipmentModel && x.unitNumber == unitNumber).ToList();
                }

                return new ServiceResult
                {
                    Message = "Get defect service sheet successfully",
                    IsError = false,
                    Content = result
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<ServiceResult> ServiceSheet(ServiceSheetDataRequest model)
        {
            try
            {
                var resultJson = new Dictionary<string, object>();
                var resultData = new List<dynamic>();
                string headerID = string.Empty;

                var dataHeaderParam = new Dictionary<string, object>();
                dataHeaderParam.Add(EnumQuery.ModelId, model.modelId);
                dataHeaderParam.Add(EnumQuery.PsTypeId, model.psTypeId);
                dataHeaderParam.Add(EnumQuery.SSWorkorder, model.workOrder);
                dataHeaderParam.Add(EnumQuery.IsDeleted, "false");

                CallAPIHelper callAPIHelper = new CallAPIHelper(_accessToken);
                string paramSite = model.siteId == EnumSite.HeadOffice ? "" : model.siteId;
                var siteMappingResult = await callAPIHelper.Get(EnumUrl.GetSiteMapping + $"&equipmentModel={model.modelId}&psType={model.psTypeId}&site={paramSite}");
                List<ModelSiteMapping> listSiteMapping = JsonConvert.DeserializeObject<List<ModelSiteMapping>>(JsonConvert.SerializeObject(siteMappingResult.Result.Content));

                string releasedOn = null;
                if (listSiteMapping != null && listSiteMapping.Count > 0)
                {
                    var siteMapping = listSiteMapping.FirstOrDefault();
                    var DateReleasedOn = DateTime.Parse(siteMapping.ReleasedOn);
                    releasedOn = DateReleasedOn.ToString("dd MMM yyyy");
                }

                var oldDataHeader = await _serviceHeaderRepository.GetDataListByParam(dataHeaderParam);

                if (oldDataHeader.Count == 0)
                {
                    var dataParam = new Dictionary<string, object>();
                    dataParam.Add(EnumQuery.ModelId, model.modelId);
                    dataParam.Add(EnumQuery.PsTypeId, model.psTypeId);
                    dataParam.Add(EnumQuery.IsDeleted, "false");

                    var result = await _masterServiceSheetRepository.GetDataListByParam(dataParam);

                    foreach (var item in result)
                    {
                        if (item.groupName != EnumGroup.General)
                        {
                            item.headerId = headerID;

                            item.Remove(EnumCommonProperty.ID);
                            item.Remove("_rid");
                            item.Remove("_self");
                            item.Remove("_etag");
                            item.Remove("_attachments");
                            item.Remove("_ts");
                            item.Remove("isActive");
                            item.Remove("isDeleted");
                            item.Remove("updatedBy");
                            item.Remove("updatedDate");
                            item.Remove("createdBy");
                            item.Remove("createdDate");

                            item.workOrder = model.workOrder;

                            var modelDetail = new CreateRequest();
                            modelDetail.employee = new EmployeeModel();

                            modelDetail.employee.id = model.employee.id;
                            modelDetail.employee.name = model.employee.name;
                            modelDetail.entity = item;
                            string groupName = item.groupName;

                            if (groupName.ToLower() == EnumGroup.LubeService.ToLower())
                            {
                                CallAPIService callAPI = new CallAPIService(_appSetting, _accessToken);
                                var equipmentAssignmentResponse = await callAPI.GetEquipmentAssignment(model.unitNumber);
                                List<EquipmentAssignmentModel> equipmentAssignment = JsonConvert.DeserializeObject<List<EquipmentAssignmentModel>>(JsonConvert.SerializeObject(equipmentAssignmentResponse));

                                List<dynamic> resultTableLubricant = new List<dynamic>();

                                Dictionary<string, object> paramLubricant = new Dictionary<string, object>();
                                paramLubricant.Add("modelId", model.modelId);
                                paramLubricant.Add("psTypeId", model.psTypeId);
                                paramLubricant.Add("site", model.siteId == null ? EnumSite.BlackWater : model.siteId == EnumSite.HeadOffice ? equipmentAssignment.FirstOrDefault().Site : model.siteId);
                                paramLubricant.Add("isDeleted", "false");

                                var dataMappingLubricant = await _mappingLubricantRepository.GetDataByParam(paramLubricant);
                                if (dataMappingLubricant != null)
                                {
                                    foreach (var itemLubricant in dataMappingLubricant.detail)
                                    {
                                        var rowTableLubricant = new ColumnTableLubricant();

                                        var dataColumn1 = new ColumnTable
                                        {
                                            key = Guid.NewGuid().ToString(),
                                            seqId = "1",
                                            itemType = "label",
                                            value = itemLubricant.compartmentLubricant,
                                            style = new StyleTable()
                                        };

                                        var dataColumn2 = new ColumnTable
                                        {
                                            key = Guid.NewGuid().ToString(),
                                            seqId = "2",
                                            itemType = "label",
                                            value = itemLubricant.recommendedLubricant,
                                            style = new StyleTable()
                                        };

                                        var dataColumn3 = new ColumnTable
                                        {
                                            key = Guid.NewGuid().ToString(),
                                            seqId = "3",
                                            itemType = "label",
                                            value = $"{itemLubricant.volume} {itemLubricant.uoM}",
                                            style = new StyleTable()
                                        };

                                        rowTableLubricant.column1 = dataColumn1;
                                        rowTableLubricant.column2 = dataColumn2;
                                        rowTableLubricant.column3 = dataColumn3;

                                        resultTableLubricant.Add(rowTableLubricant);
                                    }

                                    foreach (var itemReplaceLubricant in modelDetail.entity.subGroup[0].taskGroup[1].task)
                                    {
                                        var dataType = itemReplaceLubricant.items.GetType().Name;

                                        if (dataType != "JArray" && itemReplaceLubricant.items == EnumCommonProperty.Lubricant)
                                        {
                                            itemReplaceLubricant.items = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(resultTableLubricant));
                                        }
                                    }
                                }
                                else
                                {
                                    var rowTableLubricant = new ColumnTableLubricant();

                                    var dataColumn1 = new ColumnTable
                                    {
                                        key = Guid.NewGuid().ToString(),
                                        seqId = "1",
                                        itemType = "label",
                                        value = "",
                                        style = new StyleTable()
                                    };

                                    var dataColumn2 = new ColumnTable
                                    {
                                        key = Guid.NewGuid().ToString(),
                                        seqId = "1",
                                        itemType = "label",
                                        value = EnumErrorMessage.ErrMsgLubricantMapping.Replace(EnumCommonProperty.ModelUnitId, model.modelId).Replace(EnumCommonProperty.PsType, model.psTypeId),
                                        style = new StyleTable()
                                        {
                                            width = 100,
                                            borderColor = "none",
                                            bgColor = "none",
                                            fontColor = "#FF0000",
                                            textAlign = "center"
                                        }
                                    };

                                    var dataColumn3 = new ColumnTable
                                    {
                                        key = Guid.NewGuid().ToString(),
                                        seqId = "3",
                                        itemType = "label",
                                        value = "",
                                        style = new StyleTable()
                                    };

                                    rowTableLubricant.column1 = dataColumn1;
                                    rowTableLubricant.column2 = dataColumn2;
                                    rowTableLubricant.column3 = dataColumn3;
                                    resultTableLubricant.Add(rowTableLubricant);
                                    foreach (var itemReplaceLubricant in modelDetail.entity.subGroup[0].taskGroup[1].task)
                                    {
                                        var dataType = itemReplaceLubricant.items.GetType().Name;

                                        if (dataType != "JArray" && itemReplaceLubricant.items == EnumCommonProperty.Lubricant)
                                        {
                                            itemReplaceLubricant.items = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(resultTableLubricant));
                                        }
                                    }
                                }
                            }

                            var resultAddDetail = await _serviceDetailRepository.Create(modelDetail);

                            if (resultAddDetail == null)
                            {
                                return new ServiceResult()
                                {
                                    IsError = true,
                                    Message = result.Message
                                };
                            }
                            else
                            {
                                resultAddDetail.Remove("_rid");
                                resultAddDetail.Remove("_self");
                                resultAddDetail.Remove("_etag");
                                resultAddDetail.Remove("_attachments");
                                resultAddDetail.Remove("_ts");

                                resultData.Add(resultAddDetail);
                            }
                        }
                        else
                        {
                            item.Remove(EnumCommonProperty.ID);
                            item.Remove("_rid");
                            item.Remove("_self");
                            item.Remove("_etag");
                            item.Remove("_attachments");
                            item.Remove("_ts");
                            item.Remove("isActive");
                            item.Remove("isDeleted");
                            item.Remove("updatedBy");
                            item.Remove("updatedDate");
                            item.Remove("createdBy");
                            item.Remove("createdDate");

                            string updatedDate = ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerDateTime);
                            string tsUpdatedDate = ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerTimeStamp);

                            List<StatusHistoryModel> statusHistories = new List<StatusHistoryModel>();

                            statusHistories.Add(new StatusHistoryModel()
                            {
                                status = EnumStatus.EFormOpen,
                                updatedBy = model.employee,
                                updatedDate = updatedDate,
                                tsUpdatedDate = tsUpdatedDate
                            });

                            JToken newObjectStatusHistories = JToken.FromObject(statusHistories);
                            CallAPIService callAPI = new CallAPIService(_appSetting, _accessToken);
                            var equipmentAssignmentResponse = await callAPI.GetEquipmentAssignment(model.unitNumber);
                            List<EquipmentAssignmentModel> equipmentAssignment = JsonConvert.DeserializeObject<List<EquipmentAssignmentModel>>(JsonConvert.SerializeObject(equipmentAssignmentResponse));

                            item.statusHistory = newObjectStatusHistories;
                            item.workOrder = model.workOrder;
                            item.equipment = model.unitNumber;
                            item.siteId = equipmentAssignment.FirstOrDefault().Site;
                            item.releasedDate = releasedOn;

                            JToken newObjectSupervisor = JToken.FromObject(model.employee);

                            item.supervisor = newObjectSupervisor;

                            var modelHeader = new CreateRequest();
                            modelHeader.employee = new EmployeeModel();

                            modelHeader.employee.id = model.employee.id;
                            modelHeader.employee.name = model.employee.name;
                            modelHeader.entity = item;

                            var resultAddHeader = await _serviceHeaderRepository.Create(modelHeader);
                            if (resultAddHeader == null)
                            {
                                return new ServiceResult()
                                {
                                    IsError = true,
                                    Message = result.Message
                                };
                            }

                            headerID = resultAddHeader.id;

                            resultAddHeader.Remove("_rid");
                            resultAddHeader.Remove("_self");
                            resultAddHeader.Remove("_etag");
                            resultAddHeader.Remove("_attachments");
                            resultAddHeader.Remove("_ts");

                            resultJson.Add("general", resultAddHeader);
                        }
                    }
                    resultJson.Add("serviceSheet", resultData);

                }
                else
                {
                    List<dynamic> dataObj = JsonConvert.DeserializeObject<List<dynamic>>(JsonConvert.SerializeObject(oldDataHeader));
                    var generalData = dataObj.Where(x => x.groupName == EnumGroup.General).FirstOrDefault();

                    //if (generalData.supervisor.id != model.employee.id)
                    //{
                    //    return new ServiceResult()
                    //    {
                    //        IsError = true,
                    //        Content = null,
                    //        Message = "Cannot Access With Different Supervisor"
                    //    };
                    //}

                    //&& !string.IsNullOrEmpty(generalData.updatedBy.ToString())

                    JToken newObjectSupervisor = JToken.FromObject(model.employee);

                    generalData.supervisor = newObjectSupervisor;
                    generalData.releasedDate = releasedOn;

                    #region delete duplicate data <= hours
                    List<ServicePersonnelsResponse> servicePersonnel = JsonConvert.DeserializeObject<List<ServicePersonnelsResponse>>(JsonConvert.SerializeObject(generalData.servicePersonnels));
                    string updatedDate = ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerDateTime); //get AEST datetime
                    int dataCount = servicePersonnel.Count;
                    for (int i = 0; i < dataCount - 1; i++)
                    {
                        for (int j = i + 1; j < dataCount; j++)
                        {
                            if (i > servicePersonnel.Count - 1 || j > servicePersonnel.Count - 1)
                            {
                                break;
                            }
                            if (servicePersonnel[i].mechanic.id == servicePersonnel[j].mechanic.id //same fitter
                                && servicePersonnel[i].shift == servicePersonnel[j].shift //same shift
                                && (DateTime.ParseExact(servicePersonnel[j].serviceStart, EnumFormatting.DateTimeToString, CultureInfo.InvariantCulture) - DateTime.ParseExact(servicePersonnel[i].serviceStart, EnumFormatting.DateTimeToString, CultureInfo.InvariantCulture)).TotalHours <= 3
                                )
                            {
                                servicePersonnel.RemoveAt(j);
                                j--;
                                continue;
                            }
                        }
                    }
                    generalData.servicePersonnels = null;
                    generalData.servicePersonnels = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(servicePersonnel));
                    #endregion


                    dataHeaderParam.Add(EnumQuery.HeaderId, generalData[$"{EnumQuery.ID}"]);

                    var oldDataDetail = await _serviceDetailRepository.GetDataListByParam(dataHeaderParam);

                    resultJson.Add("general", generalData);
                    resultJson.Add("serviceSheet", oldDataDetail);
                }

                if (!resultJson.Where(x => x.Key == "general").Any())
                {
                    return new ServiceResult()
                    {
                        IsError = true,
                        Message = "Tab General Not Found"
                    };
                }

                return new ServiceResult()
                {
                    IsError = false,
                    Content = resultJson
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult()
                {
                    IsError = true,
                    Content = null,
                    Message = ex.ToString()
                };
            }
        }

        public async Task<ServiceResult> GetServiceSheetHistory(string siteId = null, string startDate = null, string endDate = null)
        {
            // if siteId == HO Site then skip filter by site
            CallAPIHelper callAPIHelperFilter = new CallAPIHelper(_accessToken);
            string groupFilter = EnumGeneralFilterGroup.Site;
            var filterRes = await callAPIHelperFilter.Get(EnumUrl.GetGeneralFilter + $"?group={groupFilter}&ver=v1");
            IList<GeneralFilterHelperModel> filters = JsonConvert.DeserializeObject<List<GeneralFilterHelperModel>>(JsonConvert.SerializeObject(filterRes.Result.Content));

            if (filters.Any(x => x.Value == siteId))
                siteId = null;

            HistoryHelper historyHelper = new HistoryHelper(_appSetting, _connectionFactory, _accessToken);
            //var result = await historyHelper.GetHistory(siteId, startDate, endDate);
            var result = await historyHelper.GetHistoryV2(siteId, startDate, endDate);

            return new ServiceResult
            {
                Message = "Get  service sheet history successfully",
                IsError = false,
                Content = result
            };
        }

        public async Task<ServiceResult> GetServiceSheetHistoryV2(string siteId = null, string startDate = null, string endDate = null)
        {
            // if siteId == HO Site then skip filter by site
            CallAPIHelper callAPIHelperFilter = new CallAPIHelper(_accessToken);
            string groupFilter = EnumGeneralFilterGroup.Site;
            var filterRes = await callAPIHelperFilter.Get(EnumUrl.GetGeneralFilter + $"?group={groupFilter}&ver=v1");
            IList<GeneralFilterHelperModel> filters = JsonConvert.DeserializeObject<List<GeneralFilterHelperModel>>(JsonConvert.SerializeObject(filterRes.Result.Content));

            if (filters.Any(x => x.Value == siteId))
                siteId = null;

            HistoryHelper historyHelper = new HistoryHelper(_appSetting, _connectionFactory, _accessToken);
            //var result = await historyHelper.GetHistory(siteId, startDate, endDate);
            var result = await historyHelper.GetHistoryV2(siteId, startDate, endDate);

            return new ServiceResult
            {
                Message = "Get  service sheet history successfully",
                IsError = false,
                Content = result
            };
        }

        public async Task<ServiceResult> UpdateTask(UpdateTaskRequest updateTaskRequest)
        {
            UpdateTaskServiceHelper service = new UpdateTaskServiceHelper(_appSetting, _connectionFactory, _container, _accessToken);
            var result = await service.UpdateTaskGeneralForm(updateTaskRequest);

            return result;
        }

        public async Task<ServiceResult> UpdateSite(string defaultSite = "")
        {
            CallAPIHelper callAPIHelper = new CallAPIHelper(_accessToken);
            var dailySchedule = await callAPIHelper.Get(EnumUrl.GetAdmHistoryUrl);
            List<DailyScheduleModel> dailySchedules = JsonConvert.DeserializeObject<List<DailyScheduleModel>>(JsonConvert.SerializeObject(dailySchedule.Result.Content));
            var result = new UpdateHeaderResponse();
            foreach (var serviceSheet in dailySchedules)
            {
                Dictionary<string, object> param = new Dictionary<string, object>();
                param.Add(EnumQuery.IsActive, "true");
                param.Add(EnumQuery.IsDeleted, "false");
                param.Add(EnumQuery.SSWorkorder, serviceSheet.workOrder);
                //param.Add(EnumQuery.SSWorkorder, "111100010");


                var rsc = await _repository.GetDataByParam(param);
                if (rsc != null)
                {
                    string siteId = StaticHelper.GetPropValue(rsc, EnumQuery.siteId);
                    if (siteId == null || siteId == "")
                    {
                        CallAPIService callAPI = new CallAPIService(_appSetting, _accessToken);
                        var equipmentNumber = StaticHelper.GetPropValue(rsc, EnumQuery.Equipment)?.ToString();
                        var equipmentAssignmentResponse = await callAPI.GetEquipmentAssignment(equipmentNumber);
                        List<EquipmentAssignmentModel> equipmentAssignment = JsonConvert.DeserializeObject<List<EquipmentAssignmentModel>>(JsonConvert.SerializeObject(equipmentAssignmentResponse));

                        UpdateRequest updateRequest = new UpdateRequest()
                        {
                            id = StaticHelper.GetPropValue(rsc, EnumQuery.ID)?.ToString(),
                            employee = new EmployeeModel()
                            {
                                id = "1000000",
                                name = "System"
                            },
                            updateParams = new List<UpdateParam>
                        {
                            new UpdateParam
                            {
                                keyValue = EnumGroup.General,
                                propertyParams = new List<PropertyParam>
                                {
                                    new PropertyParam
                                    {
                                        propertyName = EnumQuery.siteId,
                                        propertyValue = equipmentAssignment.Count != 0 ? equipmentAssignment.FirstOrDefault().Site : defaultSite
                                    }
                                }
                            }
                        }
                        };

                        await _repository.Update(updateRequest, rsc);
                    }
                }
            }

            return new ServiceResult
            {
                Message = "Data updated successfully",
                IsError = false
            };
        }

        public async Task<ServiceResult> DeleteFullCycleData(List<string> wo)
        {
            JArray workorders = JArray.FromObject(wo);

            DeleteByParamRequest workOrderParam = new DeleteByParamRequest()
            {
                deleteParams = new Dictionary<string, object>() {
                    {EnumQuery.SSWorkorder, workorders }
                }
            };

            DeleteByParamRequest workorderParam = new DeleteByParamRequest()
            {
                deleteParams = new Dictionary<string, object>() {
                    {EnumQuery.Workorder, workorders }
                },
                employee = null
            };

            #region Servicesheet

            var taskServicesheetHeader = _serviceHeaderRepository.HardDeleteByParam(workOrderParam);
            var taskServicesheetDetail = _serviceDetailRepository.HardDeleteByParam(workOrderParam);
            var taskServicesheetDefectHeader = _defectHeaderRepository.HardDeleteByParam(workorderParam);
            var taskServicesheetDefectDetail = _defectDetailRepository.HardDeleteByParam(workorderParam);

            #endregion

            #region Interim

            IRepositoryBase interimHeaderRepository = new SuckAndBlowHeaderRepository(_connectionFactory, EnumContainer.InterimEngineHeader);
            IRepositoryBase interimDetailRepository = new SuckAndBlowDetailRepository(_connectionFactory, EnumContainer.InterimEngineDetail);
            IRepositoryBase interimDefectHeaderRepository = new InterimEngineDefectHeaderRepository(_connectionFactory, EnumContainer.InterimEngineDefectHeader);
            IRepositoryBase interimDefectDetailRepository = new InterimEngineDefectDetailRepository(_connectionFactory, EnumContainer.InterimEngineDefectDetail);

            var taskInterimHeader = interimHeaderRepository.HardDeleteByParam(workOrderParam);
            var taskInterimDetail = interimDetailRepository.HardDeleteByParam(workOrderParam);
            var taskInterimDefectHeader = interimDefectHeaderRepository.HardDeleteByParam(workorderParam);
            var taskInterimDefectDetail = interimDefectDetailRepository.HardDeleteByParam(workorderParam);

            #endregion

            #region Historical CBM

            var taskCbmHistory = _cbmHistoryRepository.HardDeleteByParam(workOrderParam);

            #endregion

            await Task.WhenAll(
                taskServicesheetHeader,
                taskServicesheetDetail,
                taskServicesheetDefectHeader,
                taskServicesheetDefectDetail,
                taskInterimHeader,
                taskInterimDetail,
                taskInterimDefectHeader,
                taskInterimDefectDetail,
                taskCbmHistory
                );

            return new ServiceResult
            {
                Message = "Data deleted successfully",
                IsError = false
            };
        }

        public async Task<ServiceResult> DeleteFullCycleDataIntervention(List<string> wo)
        {
            JArray workorders = JArray.FromObject(wo);

            DeleteByParamRequest _paramsIntervention = new DeleteByParamRequest()
            {
                deleteParams = new Dictionary<string, object>() {
                    {EnumQuery.SapWorkOrder, workorders }
                }
            };

            DeleteByParamRequest _paramsInterventionDefect = new DeleteByParamRequest()
            {
                deleteParams = new Dictionary<string, object>() {
                    {EnumQuery.Workorder, workorders }
                },
                employee = null
            };

            var taskIntervention = _interventionRepository.HardDeleteByParam(_paramsIntervention);
            var taskInterventionDefectHeader = _interventionDefectHeaderRepository.HardDeleteByParam(_paramsInterventionDefect);
            var taskInterventionDefectDetail = _interventionDefectDetailRepository.HardDeleteByParam(_paramsInterventionDefect);

            await Task.WhenAll(
                taskIntervention,
                taskInterventionDefectHeader,
                taskInterventionDefectDetail
                );

            return new ServiceResult
            {
                Message = "Data deleted successfully",
                IsError = false
            };
        }
    }
}
