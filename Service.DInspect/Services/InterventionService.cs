using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.ApplicationInsights;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using Service.DInspect.Helpers;
using Service.DInspect.Interfaces;
using Service.DInspect.Models;
using Service.DInspect.Models.ADM;
using Service.DInspect.Models.EHMS;
using Service.DInspect.Models.Entity;
using Service.DInspect.Models.Enum;
using Service.DInspect.Models.Request;
using Service.DInspect.Models.Response;
using Service.DInspect.Repositories;
using Service.DInspect.Services.Helpers;
using Service.DInspect.Repositories;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Service.DInspect.Services
{
    public class InterventionService : ServiceBase
    {
        protected string _container;
        protected IConnectionFactory _connectionFactory;
        protected IRepositoryBase _taskTemplateRepository, _hisInterventionSync, _referenceRepository;
        protected IRepositoryBase _cbmHistoryRepository;
        private readonly TelemetryClient _telemetryClient;
        //protected MySetting _appSetting;
        //protected string _accessToken;

        public InterventionService(MySetting appSetting, IConnectionFactory connectionFactory, string container, string accessToken, TelemetryClient telemetryClient) : base(appSetting, connectionFactory, container, accessToken)
        {
            _telemetryClient = telemetryClient;
            _container = container;
            _connectionFactory = connectionFactory;
            _repository = new InterventionRepository(connectionFactory, container);
            _taskTemplateRepository = new TaskTemplateRepository(connectionFactory, EnumContainer.TaskTemplate);
            _hisInterventionSync = new HisInterventionSyncRepository(connectionFactory, EnumContainer.HisInterventionSync);
            _referenceRepository = new TaskTemplateRepository(connectionFactory, EnumContainer.MasterReference);
            _cbmHistoryRepository = new CbmHitoryRepository(connectionFactory, EnumContainer.CbmHistory);
            //_appSetting = appSetting;
            //_accessToken = accessToken;
        }

        public async Task<ServiceResult> SyncIntervention()
        {
            int syncCount = 0;

            try
            {
                CallAPIService callAPI = new CallAPIService(_appSetting, _accessToken);

                #region Get Intervention Data EHMS

                dynamic interventionResult = await callAPI.GetSyncIntervention(string.Empty);
                List<InterventionRecomModel> interventions = JsonConvert.DeserializeObject<List<InterventionRecomModel>>(JsonConvert.SerializeObject(interventionResult));

                #endregion

                if (interventions?.Count > 0)
                    syncCount = await GenerateIntervention(interventions);

                return new ServiceResult
                {
                    Message = "Sync Intervention successfully",
                    IsError = false,
                    Content = null
                };
            }
            catch (Exception ex)
            {
                #region his intervention sync

                Dictionary<string, dynamic> hisInterventionSyncParam = new Dictionary<string, dynamic>();
                hisInterventionSyncParam.Add(EnumQuery.Key, Guid.NewGuid().ToString());
                hisInterventionSyncParam.Add(EnumQuery.Detail, $"{syncCount} data intervention success to sync with error : {ex.Message}");
                hisInterventionSyncParam.Add(EnumQuery.IsSuccess, false.ToString().ToLower());

                var hisInterventionSync = await _hisInterventionSync.Create(new CreateRequest()
                {
                    employee = new EmployeeModel() { id = EnumCaption.System, name = EnumCaption.System },
                    entity = hisInterventionSyncParam
                });

                #endregion

                throw ex;
            }
        }

        public async Task<ServiceResult> GetIntervention(string keyPbi)
        {
            try
            {
                dynamic result = null;
                InterventionModel intervention = new InterventionModel();

                Dictionary<string, object> interventionParam = new Dictionary<string, object>();
                interventionParam.Add(StaticHelper.GetPropertyName(() => intervention.keyPbi), keyPbi);

                var interventionResult = await _repository.GetDataByParam(interventionParam);

                var customStep1 = new Dictionary<string, string>
                {
                    { "[STEP 1 - DInspect] Get Intervention in CosmosDB", "" }
                };

                _telemetryClient.TrackEvent($"{nameof(InterventionService)}-{nameof(GetIntervention)}", customStep1);

                if (interventionResult == null)
                {
                    CallAPIService callAPI = new CallAPIService(_appSetting, _accessToken);

                    #region Get Intervention Data EHMS

                    dynamic syncInterventionResult = await callAPI.GetSyncIntervention(keyPbi);
                    List<InterventionRecomModel> interventions = JsonConvert.DeserializeObject<List<InterventionRecomModel>>(JsonConvert.SerializeObject(syncInterventionResult));

                    var customStep2 = new Dictionary<string, string>
                    {
                        { "[STEP 2 - DInspect] Get Data EHMS", "" }
                    };

                    _telemetryClient.TrackEvent($"{nameof(InterventionService)}-{nameof(GetIntervention)}", customStep2);
                    #endregion

                    if (interventions?.Count > 0)
                    {
                        await GenerateIntervention(interventions);

                        InterventionRecomModel interventionRecom = interventions.FirstOrDefault();

                        Dictionary<string, object> paramInterventionDb = new Dictionary<string, object>();
                        paramInterventionDb.Add(StaticHelper.GetPropertyName(() => interventionRecom.keyPbi), interventionRecom.keyPbi);

                        result = await _repository.GetDataByParam(paramInterventionDb);
                    }
                }
                else
                {
                    result = interventionResult;
                }

                return new ServiceResult
                {
                    Message = "Get intervention successfully",
                    IsError = false,
                    Content = result
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async Task<InterventionTaskModel> GenerateDetails(List<InterventionRecomDetailModel> details, List<UoMModel> uoms, List<TaskTypeConditionModel> taskTypeConditions, List<MasterRatingModel> ratings)
        {
            List<dynamic> tasks = new List<dynamic>();
            string taskName = string.Empty;
            string uomText = $"['{string.Join("'; '", uoms.Select(x => x.uom))}']";
            string uomValue = $"['{string.Join("', '", uoms.Select(x => x.mdUomId))}']";
            string ratingText = $"['{string.Join("'; '", ratings.Select(x => x.rating))}']";
            string ratingValue = $"['{string.Join("', '", ratings.Select(x => x.rating))}']";

            details = details.OrderBy(x => x.interventionSequence).ToList();

            if (!Convert.ToBoolean(details.FirstOrDefault().isAdditionalTask))
                taskName = EnumCaption.InterventionChecks;
            else
                taskName = EnumCaption.AdditionalTasks;

            int count = 0;
            foreach (var itemDetail in details)
            {
                Dictionary<string, object> paramTemplate = new Dictionary<string, object>();
                paramTemplate = new Dictionary<string, object>();
                paramTemplate.Add(EnumQuery.Key, itemDetail.typeTaskId);
                var taskTemplate = await _taskTemplateRepository.GetDataByParam(paramTemplate);

                if (taskTemplate != null)
                {
                    var template = StaticHelper.GetPropValue(taskTemplate, EnumQuery.Template);
                    string strTaskTemplate = JsonConvert.SerializeObject(template);

                    strTaskTemplate = strTaskTemplate.Replace(EnumCommonProperty.TaskId, itemDetail.taskKey);
                    strTaskTemplate = strTaskTemplate.Replace(EnumCommonProperty.ModelUnitId, itemDetail.modelUnitId);
                    strTaskTemplate = strTaskTemplate.Replace(EnumCommonProperty.PsType, itemDetail.psType?.ToString());
                    strTaskTemplate = strTaskTemplate.Replace(EnumCommonProperty.TaskGroupKey, itemDetail.taskGroupKey);
                    strTaskTemplate = strTaskTemplate.Replace(EnumCommonProperty.InterventionSequence, itemDetail.interventionSequence?.ToString());
                    strTaskTemplate = strTaskTemplate.Replace(EnumCommonProperty.SubTask, itemDetail.subTask);
                    strTaskTemplate = Regex.Replace(strTaskTemplate, EnumCommonProperty.Guid, m => Guid.NewGuid().ToString());
                    strTaskTemplate = strTaskTemplate.Replace(EnumCommonProperty.ConditionGuid, Guid.NewGuid().ToString());
                    strTaskTemplate = strTaskTemplate.Replace(EnumCommonProperty.Sequence, itemDetail.interventionSequence?.ToString());
                    strTaskTemplate = strTaskTemplate.Replace(EnumCommonProperty.Description, itemDetail.recomendedAction);
                    strTaskTemplate = strTaskTemplate.Replace(EnumCommonProperty.Uom, itemDetail.uom);
                    strTaskTemplate = strTaskTemplate.Replace(EnumCommonProperty.RatingCaption, ratingText);
                    strTaskTemplate = strTaskTemplate.Replace(EnumCommonProperty.RatingValue, ratingValue);
                    strTaskTemplate = strTaskTemplate.Replace(EnumCommonProperty.IsAdditionalTask, Convert.ToInt32(itemDetail.isAdditionalTask).ToString());

                    //strTaskTemplate = strTaskTemplate.Replace(EnumCommonProperty.UomCaption, uomText);
                    //strTaskTemplate = strTaskTemplate.Replace(EnumCommonProperty.UomValue, uomValue);

                    if (long.TryParse(itemDetail.typeTaskId, out long idValue))
                    {
                        var conditions = taskTypeConditions.Where(x => x.typeTaskId == idValue).ToList();

                        string conditionCaption = $"['{string.Join("'; '", conditions.SelectMany(z => z.listTypeCondition).Select(s => s.typeCondition))}']";
                        string conditionValue = $"['{string.Join("', '", conditions.SelectMany(z => z.listTypeCondition).Select(s => s.typeConditionId))}']";

                        strTaskTemplate = strTaskTemplate.Replace(EnumCommonProperty.ConditionCaption, conditionCaption);
                        strTaskTemplate = strTaskTemplate.Replace(EnumCommonProperty.ConditionValue, conditionValue);
                    }

                    var customStep4 = new Dictionary<string, string>
                    {
                        { "[STEP 4 - DInspect] Generate JSON Intervention", "" }
                    };

                    _telemetryClient.TrackEvent($"{nameof(InterventionService)}-{nameof(GenerateDetails)}", customStep4);

                    dynamic newTaskTemplate = JsonConvert.DeserializeObject<dynamic>(strTaskTemplate);

                    #region Generate Reference

                    if (itemDetail.refDocId.HasValue)
                    {
                        dynamic reference = await GenerateReference(itemDetail.refDocId.ToString(), itemDetail.interventionSequence.ToString());
                        newTaskTemplate.reference = reference;

                        var customStep5 = new Dictionary<string, string>
                        {
                            { "[STEP 5 - DInspect] Generate JSON Intervention Reference", "" }
                        };

                        _telemetryClient.TrackEvent($"{nameof(InterventionService)}-{nameof(GenerateDetails)}", customStep5);
                    }

                    #endregion

                    tasks.Add(newTaskTemplate);

                    count++;
                }
            }

            InterventionTaskModel result = new InterventionTaskModel()
            {
                key = Guid.NewGuid().ToString(),
                name = taskName,
                tasks = tasks
            };

            return result;
        }

        private async Task<dynamic> GenerateReference(string referenceId, string sequence)
        {
            string strReferenceTemplate = "{}";

            Dictionary<string, object> paramTemplate = new Dictionary<string, object>();
            paramTemplate = new Dictionary<string, object>();
            paramTemplate.Add(EnumQuery.Key, referenceId);
            var referenceTemplate = await _referenceRepository.GetDataByParam(paramTemplate);

            if (referenceTemplate != null)
            {
                var template = StaticHelper.GetPropValue(referenceTemplate, EnumQuery.Template);
                strReferenceTemplate = JsonConvert.SerializeObject(template);

                strReferenceTemplate = strReferenceTemplate.Replace(EnumCommonProperty.Guid, Guid.NewGuid().ToString());
                strReferenceTemplate = strReferenceTemplate.Replace(EnumCommonProperty.Sequence, sequence);
            }

            dynamic result = JsonConvert.DeserializeObject<dynamic>(strReferenceTemplate);

            return result;
        }

        public override async Task<ServiceResult> Put(UpdateRequest updateRequest)
        {
            try
            {
                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                var rsc = await _repository.Get(updateRequest.id);
                JObject updateRequestObj = JObject.Parse(JsonConvert.SerializeObject(updateRequest));
                string status = StaticHelper.GetPropValue(updateRequestObj, EnumQuery.PropertyName, EnumQuery.InterventionExecution, EnumQuery.PropertyValue)?.ToString();
                string defectStatus = StaticHelper.GetPropValue(updateRequestObj, EnumQuery.PropertyName, EnumQuery.DefectStatus, EnumQuery.PropertyValue)?.ToString();
                string additionalInfo = StaticHelper.GetPropValue(updateRequestObj, EnumQuery.PropertyName, EnumQuery.AdditionalInformation, EnumQuery.PropertyValue)?.ToString();
                string serviceStartParam = StaticHelper.GetPropValue(updateRequestObj, EnumQuery.PropertyName, EnumQuery.ServiceStart, EnumQuery.PropertyValue)?.ToString();
                string serviceEndParam = StaticHelper.GetPropValue(updateRequestObj, EnumQuery.PropertyName, EnumQuery.ServiceEnd, EnumQuery.PropertyValue)?.ToString();
                string localInterventionStatus = StaticHelper.GetPropValue(updateRequestObj, EnumQuery.LocalInterventionStatus)?.ToString();
                bool updateDefectStatus = updateRequest.updateParams.Any(s => s.propertyParams.Any(x => x.propertyName == EnumQuery.DefectStatus));
                UpdateParam updatedDefectStatusParam = updateRequest.updateParams.Where(s => s.propertyParams.Any(x => x.propertyName == EnumQuery.DefectStatus)).FirstOrDefault();
                string updatedDate = ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerDateTime);

                InterventionRecomModel intervention = JsonConvert.DeserializeObject<InterventionRecomModel>(JsonConvert.SerializeObject(rsc));

                CallAPIService callAPI = new CallAPIService(_appSetting, _accessToken);

                dynamic interventionExecStatusResult = await callAPI.EHMSGetAll(EnumController.InterventionExecutionStatus);
                List<InterventionExecutionStatusModel> interventionExecStatus = JsonConvert.DeserializeObject<List<InterventionExecutionStatusModel>>(JsonConvert.SerializeObject(interventionExecStatusResult));

                int localExecutionId = (int)interventionExecStatus.Where(x => x.intervention_execution == localInterventionStatus).FirstOrDefault()?.md_intervention_execution_id;
                int cosmosExecutionId = (int)interventionExecStatus.Where(x => x.intervention_execution == intervention.interventionExecution).FirstOrDefault()?.md_intervention_execution_id;
                int onProgressId = (int)interventionExecStatus.Where(x => x.intervention_execution == EnumStatus.EFormOnProgress).FirstOrDefault()?.md_intervention_execution_id;

                bool updateDownloadHistory = updateRequest.updateParams.Any(s => s.propertyParams.Any(x => x.propertyName == EnumQuery.DownloadHistory));

                if (!(localExecutionId < cosmosExecutionId && cosmosExecutionId > onProgressId))
                {
                    if (rsc != null && status == EnumStatus.EFormSubmited)
                    {
                        #region Required Validation

                        var requiredValues = StaticHelper.GetPropValues(rsc, EnumQuery.CustomValidation, EnumCaption.Required, EnumQuery.Value);
                        List<string> values = JsonConvert.DeserializeObject<List<string>>(JsonConvert.SerializeObject(requiredValues));

                        if (values.Any(x => string.IsNullOrEmpty(x)))
                        {
                            return new ServiceResult
                            {
                                Message = "Please fill all required value.",
                                IsError = true
                            };
                        }

                        #endregion

                        #region Task Progress Validation

                        UpdateTaskServiceHelper updateTaskService = new UpdateTaskServiceHelper(_appSetting, _connectionFactory, _container, _accessToken);
                        TaskProgressResponse taskProgress = await updateTaskService.GetTaskProgress(rsc);

                        if (taskProgress.totalTask != taskProgress.doneTask)
                        {
                            return new ServiceResult
                            {
                                Message = "Please fill all task.",
                                IsError = true
                            };
                        }

                        #endregion
                    }

                    if (rsc != null && !string.IsNullOrEmpty(status))
                    {
                        string statusId = interventionExecStatus.Where(x => x.intervention_execution == status).FirstOrDefault()?.md_intervention_execution_id.ToString();

                        updateRequest.updateParams.Add(new UpdateParam()
                        {
                            keyValue = updateRequest.id,
                            propertyParams = new List<PropertyParam>() {
                            new PropertyParam(){ propertyName = EnumQuery.InterventionExecutionId, propertyValue = statusId }
                        }
                        });
                    }

                    var shiftResponse = await callAPI.GetShift();
                    List<ShiftModel> shifts = JsonConvert.DeserializeObject<List<ShiftModel>>(JsonConvert.SerializeObject(shiftResponse));

                    string keyValue = "";
                    foreach (var updateParams in updateRequest.updateParams)
                    {
                        foreach (var propertyParams in updateParams.propertyParams)
                        {
                            #region get key value
                            if (propertyParams.propertyName == EnumQuery.DownloadHistory && updateDownloadHistory)
                            {
                                keyValue = updateParams.keyValue;
                            }
                            #endregion
                            else if (propertyParams.propertyName == EnumQuery.InterventionSMU)
                            {
                                string interventionSmu = StaticHelper.GetPropValue(rsc, EnumQuery.InterventionSMU);

                                if (!string.IsNullOrEmpty(interventionSmu))
                                {
                                    propertyParams.propertyValue = interventionSmu;
                                }
                            }
                            else if (propertyParams.propertyName == EnumQuery.Log)
                            {
                                JArray logs = new JArray();

                                var dbLogs = StaticHelper.GetPropValue(rsc, EnumQuery.Log);
                                if (dbLogs != null && !string.IsNullOrEmpty(Convert.ToString(dbLogs)))
                                    logs = JArray.FromObject(dbLogs);

                                JObject propertyValue = JObject.Parse(propertyParams.propertyValue);
                                bool isExist = false;

                                for (int i = 0; i < logs.Count; i++)
                                {
                                    var log = logs[i];

                                    if (log[EnumQuery.Employee][EnumQuery.ID]?.ToString() == propertyValue[EnumQuery.Employee][EnumQuery.ID]?.ToString())
                                    {
                                        logs[i] = propertyValue;
                                        isExist = true;

                                        break;
                                    }
                                }

                                if (!isExist)
                                    logs.Add(propertyValue);

                                propertyParams.propertyValue = JsonConvert.SerializeObject(logs);
                            }
                            else if (propertyParams.propertyName == EnumQuery.ServicePersonnels)
                            {
                                JArray servicePersonnels = new JArray();

                                var dbServicePersonnels = StaticHelper.GetPropValue(rsc, EnumQuery.ServicePersonnels);
                                if (dbServicePersonnels != null && !string.IsNullOrEmpty(Convert.ToString(dbServicePersonnels)))
                                    servicePersonnels = JArray.FromObject(dbServicePersonnels);

                                MasterSettingRepository _settingRepo = new MasterSettingRepository(_connectionFactory, EnumContainer.MasterSetting);
                                var dbSettings = _settingRepo.GetAllData().Result;
                                List<DBSetting> dBSettings = JsonConvert.DeserializeObject<List<DBSetting>>(JsonConvert.SerializeObject(dbSettings));
                                EnumFormatting.appTimeZoneDesc = dBSettings.Where(x => x.key == EnumQuery.TimeZoneDesc).FirstOrDefault()?.value;

                                JObject propertyValue = JObject.Parse(propertyParams.propertyValue);
                                DateTime paramServiceStart = FormatHelper.ConvertToDateTime24(propertyValue[EnumQuery.ServiceStart].ToString());

                                var paramShift = shifts.Where(x => StaticHelper.TimeBetween(paramServiceStart.TimeOfDay, x.startHour24, x.endHour24)).FirstOrDefault().shift + $" {textInfo.ToTitleCase(EnumQuery.Shift)}";

                                if (string.IsNullOrEmpty(propertyValue[EnumQuery.ServiceEnd].ToString()))
                                {
                                    bool isNew = false;

                                    var dataExist = servicePersonnels.Where(servicePersonnel => servicePersonnel[EnumQuery.Mechanic][EnumQuery.ID]?.ToString() == propertyValue[EnumQuery.Mechanic][EnumQuery.ID]?.ToString() && string.IsNullOrEmpty(servicePersonnel[EnumQuery.ServiceEnd]?.ToString())).ToList();

                                    if (dataExist.Count > 0)
                                    {
                                        for (int i = 0; i < servicePersonnels.Count; i++)
                                        {
                                            var servicePersonnel = servicePersonnels[i];

                                            if (servicePersonnel[EnumQuery.Mechanic][EnumQuery.ID]?.ToString() == propertyValue[EnumQuery.Mechanic][EnumQuery.ID]?.ToString()
                                                && string.IsNullOrEmpty(servicePersonnel[EnumQuery.ServiceEnd].ToString()))
                                            {
                                                DateTime dbServiceStart = FormatHelper.ConvertToDateTime24(servicePersonnel[EnumQuery.ServiceStart].ToString());

                                                TimeSpan serviceDuration = paramServiceStart - dbServiceStart;

                                                string shift = servicePersonnel[EnumQuery.Shift].ToString();
                                                var shiftData = shifts.Where(x => x.shift.ToLower() == shift.ToLower().Replace($" {EnumQuery.Shift}", string.Empty)).FirstOrDefault();

                                                string serviceEnd = string.Empty;
                                                if (shiftData.startHourType.ToLower() == EnumQuery.PM && shiftData.endHourType.ToLower() == EnumQuery.AM)
                                                    serviceEnd = $"{dbServiceStart.AddDays(1).ToString(EnumFormatting.DateToString)} {shiftData.endHour} {shiftData.endHourType}";
                                                else
                                                    serviceEnd = $"{dbServiceStart.ToString(EnumFormatting.DateToString)} {shiftData.endHour} {shiftData.endHourType}";

                                                DateTime serviceEndDate = FormatHelper.ConvertToDateTime12(serviceEnd);

                                                if (!(serviceDuration <= new TimeSpan(3, 0, 0) && paramServiceStart <= serviceEndDate))
                                                {
                                                    if (paramServiceStart < serviceEndDate)
                                                        servicePersonnels[i][EnumQuery.ServiceEnd] = propertyValue[EnumQuery.ServiceStart];
                                                    else
                                                        servicePersonnels[i][EnumQuery.ServiceEnd] = serviceEndDate.ToString(EnumFormatting.DateTimeToString);

                                                    isNew = true;
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        isNew = true;
                                    }

                                    if (isNew)
                                    {
                                        propertyValue[EnumQuery.Shift] = paramShift;
                                        servicePersonnels.Add(propertyValue);
                                    }

                                    propertyParams.propertyValue = JsonConvert.SerializeObject(servicePersonnels);
                                }
                                else
                                {
                                    DateTime paramServiceEnd = FormatHelper.ConvertToDateTime24(propertyValue[EnumQuery.ServiceEnd].ToString());

                                    for (int i = 0; i < servicePersonnels.Count; i++)
                                    {
                                        var servicePersonnel = servicePersonnels[i];

                                        if (string.IsNullOrEmpty(servicePersonnel[EnumQuery.ServiceEnd].ToString()))
                                        {
                                            DateTime dbServiceStart = FormatHelper.ConvertToDateTime24(servicePersonnel[EnumQuery.ServiceStart].ToString());

                                            string shift = servicePersonnel[EnumQuery.Shift].ToString();
                                            var shiftData = shifts.Where(x => x.shift.ToLower() == shift.ToLower().Replace($" {EnumQuery.Shift}", string.Empty)).FirstOrDefault();

                                            string serviceEnd = string.Empty;
                                            if (shiftData.startHourType.ToLower() == EnumQuery.PM && shiftData.endHourType.ToLower() == EnumQuery.AM)
                                                serviceEnd = $"{dbServiceStart.AddDays(1).ToString(EnumFormatting.DateToString)} {shiftData.endHour} {shiftData.endHourType}";
                                            else
                                                serviceEnd = $"{dbServiceStart.ToString(EnumFormatting.DateToString)} {shiftData.endHour} {shiftData.endHourType}";

                                            DateTime serviceEndDate = FormatHelper.ConvertToDateTime12(serviceEnd);

                                            if (paramServiceEnd < serviceEndDate)
                                                servicePersonnels[i][EnumQuery.ServiceEnd] = propertyValue[EnumQuery.ServiceEnd];
                                            else
                                            {
                                                servicePersonnels[i][EnumQuery.ServiceEnd] = serviceEndDate.ToString(EnumFormatting.DateTimeToString);

                                                if (servicePersonnel[EnumQuery.Mechanic][EnumQuery.ID]?.ToString() == propertyValue[EnumQuery.Mechanic][EnumQuery.ID]?.ToString())
                                                {
                                                    string serviceStart = $"{serviceEndDate.ToString(EnumFormatting.DateToString)} {shiftData.startHour} {shiftData.endHourType}";
                                                    DateTime serviceStartDate = FormatHelper.ConvertToDateTime12(serviceStart);

                                                    propertyValue[EnumQuery.ServiceStart] = serviceStartDate.ToString(EnumFormatting.DateTimeToString);
                                                    propertyValue[EnumQuery.Shift] = paramShift;
                                                    servicePersonnels.Add(propertyValue);
                                                }
                                            }
                                        }
                                    }

                                    propertyParams.propertyValue = JsonConvert.SerializeObject(servicePersonnels);
                                }
                            }
                            else if (propertyParams.propertyName == EnumQuery.AdditionalInformation)
                            {
                                string additionalInformation = StaticHelper.GetPropValue(rsc, EnumQuery.AdditionalInformation);

                                if (!string.IsNullOrEmpty(additionalInformation))
                                {
                                    propertyParams.propertyValue = additionalInformation;
                                }
                            }
                        }
                    }
                    #region get all download history
                    if (!string.IsNullOrWhiteSpace(keyValue))// Get all download history
                    {
                        var history = StaticHelper.GetPropValue(rsc, keyValue, EnumQuery.DownloadHistory);

                        List<DownloadHistoryModel> statusHistories = JsonConvert.DeserializeObject<List<DownloadHistoryModel>>(JsonConvert.SerializeObject(history));
                        if (statusHistories == null)
                            statusHistories = new List<DownloadHistoryModel>();

                        statusHistories.Add(new DownloadHistoryModel()
                        {
                            downloadBy = updateRequest.employee,
                            downloadDate = EnumCommonProperty.CurrentDateTime.ToString(EnumFormatting.DateTimeToString),
                        });

                        List<PropertyParam> propertyParam = new List<PropertyParam>() {
                                    new PropertyParam()
                                    {
                                        propertyName = EnumQuery.DownloadHistory,
                                        propertyValue = JsonConvert.SerializeObject(statusHistories)
                                    }
                                };

                        updateRequest.updateParams.Add(new UpdateParam()
                        {
                            keyValue = keyValue,
                            propertyParams = propertyParam
                        });
                    }
                    #endregion
                    var result = await _repository.Update(updateRequest, rsc);

                    if (rsc != null && (!string.IsNullOrEmpty(status) || !string.IsNullOrEmpty(defectStatus)))
                    {
                        intervention.interventionExecution = status;
                        intervention.defectStatusId = defectStatus;
                        intervention.additionalInformation = additionalInfo;
                        intervention.serviceStart = serviceStartParam;
                        intervention.serviceEnd = serviceEndParam;
                        await UpdateInterventionHeaderEHMS(intervention, updateRequest.employee);

                        if (status == EnumStatus.EFormOnProgress)
                        {
                            #region generate sos history
                            Dictionary<string, object> paramSosHis = new Dictionary<string, object>()
                                {
                                    { EnumQuery.SSWorkorder, intervention.sapWorkOrder},
                                    { EnumQuery.KeyPbi, intervention.keyPbi},
                                    { EnumQuery.Equipment, intervention.equipment},
                                    { EnumQuery.EformType, EnumEformType.EformIntervention}
                                };

                            SOSService SOSService = new SOSService(_appSetting, _connectionFactory, EnumContainer.SOSHistory, _accessToken);
                            await SOSService.GenerateSosHistory(paramSosHis);

                            #endregion
                        }
                    }

                    #region input historical if cbm replacement & defect status = completed
                    if (updateDefectStatus)
                    {
                        string _status = updatedDefectStatusParam.propertyParams[0].propertyValue.ToString();
                        if (_status == EnumStatus.DefectCompleted)
                        {
                            string _workorder = rsc[EnumQuery.SapWorkOrder];
                            string _modelId = rsc[EnumQuery.EquipmentDesc];
                            string _psTypeId = "";
                            string _equipment = rsc[EnumQuery.Equipment];
                            string _siteId = rsc[EnumQuery.siteId];

                            DetailServiceSheet modelData = new DetailServiceSheet();
                            modelData.workOrder = _workorder;
                            modelData.taskKey = null;

                            var _repoDetail = new InterventionRepository(_connectionFactory, EnumContainer.Intervention);

                            var currentData = await _repoDetail.GetDataInterventionDetailByKey(modelData);

                            foreach (var item in currentData)
                            {

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
                                headerHisUpdate.source = "intervention";
                                //headerHisUpdate.category = currentData[0].rating;
                                headerHisUpdate.category = item[EnumQuery.Category] + " " + item[EnumQuery.Rating];
                                headerHisUpdate.currentValue = !(bool)item[EnumQuery.CbmAdjustmentReplacement] ? item[EnumQuery.CurrentValue] : item[EnumQuery.MeasurementValue];
                                headerHisUpdate.currentRating = !(bool)item[EnumQuery.CbmAdjustmentReplacement] ? item[EnumQuery.CurrentRating] : item[EnumQuery.TaskValue];
                                headerHisUpdate.replacementValue = !(bool)item[EnumQuery.CbmAdjustmentReplacement] ? item[EnumQuery.ReplacementValue] : item[EnumQuery.NonCbmAdjustmentReplacementMeasurementValue];
                                headerHisUpdate.replacementRating = !(bool)item[EnumQuery.CbmAdjustmentReplacement] ? item[EnumQuery.ReplacementRating] : item[EnumQuery.NonCbmAdjustmentReplacementRating];
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
                }
                else
                {
                    var _updatedBy = rsc[EnumQuery.UpdatedBy];
                    string _approvedBy = _updatedBy[EnumQuery.Name];

                    string _message = EnumErrorMessage.ErrMsgInterventionDefectHeaderApproval.Replace(EnumCommonProperty.ApprovedBy, _approvedBy);

                    return new ServiceResult
                    {
                        Message = _message,
                        IsError = true,
                        Content = null
                    };
                }

                return new ServiceResult
                {
                    Message = "Data updated successfully",
                    IsError = false,
                    Content = ""
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

        public async Task<ServiceResult> UpdateTask(UpdateTaskRequest updateTaskRequest)
        {
            ServiceResult result = new ServiceResult();

            if (await UpdateStatusValidation(updateTaskRequest, true))
            {
                UpdateTaskServiceHelper service = new UpdateTaskServiceHelper(_appSetting, _connectionFactory, _container, _accessToken);
                result = await service.UpdateTask(updateTaskRequest);

                if (!result.IsError)
                {
                    await UpdateInterventionDetailEHMS(updateTaskRequest);

                    SOSService SOSService = new SOSService(_appSetting, _connectionFactory, EnumContainer.SOSHistory, _accessToken);
                    await SOSService.UpdateTask(updateTaskRequest, EnumEformType.EformIntervention);

                }
            }
            else
            {
                UpdateTaskDefectRequest dataRequestValidation = new UpdateTaskDefectRequest();
                dataRequestValidation.headerId = updateTaskRequest.headerId;
                dataRequestValidation.updateParams = updateTaskRequest.updateParams;

                UpdateTaskServiceHelper service = new UpdateTaskServiceHelper(_appSetting, _connectionFactory, _container, _accessToken);
                var resultData = await service.UpdateTaskWithDefectValidation(dataRequestValidation);

                if (resultData.IsError)
                {
                    return resultData;
                }

                //result.Message = "Intervention status not On Progress";
            }

            return result;
        }

        public async Task<ServiceResult> UpdateTaskRevise(UpdateTaskRequest updateTaskRequest)
        {
            ServiceResult result = new ServiceResult();

            if (await UpdateStatusValidation(updateTaskRequest, false))
            {
                UpdateTaskServiceHelper service = new UpdateTaskServiceHelper(_appSetting, _connectionFactory, _container, _accessToken);
                result = await service.UpdateTaskInterventionRevise(updateTaskRequest);

                if (!result.IsError)
                {
                    //await UpdateInterventionDetailEHMS(updateTaskRequest);

                    SOSService SOSService = new SOSService(_appSetting, _connectionFactory, EnumContainer.SOSHistory, _accessToken);
                    await SOSService.UpdateTask(updateTaskRequest, EnumEformType.EformIntervention);

                }
            }
            else
            {
                result.Message = "Intervention status not On Progress";
            }

            return result;
        }

        public async Task<ServiceResult> UpdateTaskWithDefect(UpdateTaskDefectRequest updateTaskDefectRequest)
        {
            ServiceResult result = new ServiceResult();

            if (await UpdateStatusValidation(updateTaskDefectRequest, true))
            {
                UpdateTaskServiceHelper service = new UpdateTaskServiceHelper(_appSetting, _connectionFactory, _container, _accessToken);
                result = await service.UpdateTaskWithDefect(updateTaskDefectRequest);

                if (!result.IsError)
                {
                    await UpdateInterventionDetailEHMS(updateTaskDefectRequest);

                    SOSService SOSService = new SOSService(_appSetting, _connectionFactory, EnumContainer.SOSHistory, _accessToken);
                    UpdateTaskRequest updateTaskRequest = new UpdateTaskRequest()
                    {
                        workorder = updateTaskDefectRequest.workOrder,
                        headerId = updateTaskDefectRequest.headerId,
                        id = updateTaskDefectRequest.id,
                        updateParams = updateTaskDefectRequest.updateParams,
                        employee = updateTaskDefectRequest.employee

                    };
                    await SOSService.UpdateTask(updateTaskRequest, EnumEformType.EformIntervention);

                }
            }
            else
            {
                UpdateTaskServiceHelper service = new UpdateTaskServiceHelper(_appSetting, _connectionFactory, _container, _accessToken);
                var resultData = await service.UpdateTaskWithDefectValidation(updateTaskDefectRequest);

                if (resultData.IsError)
                {
                    return resultData;
                }
            }

            return result;
        }

        public async Task<ServiceResult> UpdateTaskWithDefectRevise(UpdateTaskDefectReviseRequest updateTaskDefectRequest)
        {
            ServiceResult result = new ServiceResult();

            if (await UpdateStatusValidation(updateTaskDefectRequest, false))
            {
                UpdateTaskServiceHelper service = new UpdateTaskServiceHelper(_appSetting, _connectionFactory, _container, _accessToken);
                result = await service.UpdateTaskWithDefectInterventionRevise(updateTaskDefectRequest);

                if (!result.IsError)
                {
                    //await UpdateInterventionDetailEHMS(updateTaskDefectRequest);

                    SOSService SOSService = new SOSService(_appSetting, _connectionFactory, EnumContainer.SOSHistory, _accessToken);
                    UpdateTaskRequest updateTaskRequest = new UpdateTaskRequest()
                    {
                        workorder = updateTaskDefectRequest.workOrder,
                        headerId = updateTaskDefectRequest.headerId,
                        id = updateTaskDefectRequest.id,
                        updateParams = updateTaskDefectRequest.updateParams,
                        employee = updateTaskDefectRequest.employee

                    };
                    await SOSService.UpdateTask(updateTaskRequest, EnumEformType.EformIntervention);

                }
            }

            return result;
        }

        public async Task<ServiceResult> CreateGenericDefect(CreateGenericDefectRequest createGenericDefectRequest)
        {
            GenericDefectServiceHelper serviceHelper = new GenericDefectServiceHelper(_appSetting, _connectionFactory, _container, _accessToken);
            var result = await serviceHelper.CreateGenericDefect(createGenericDefectRequest);

            return result;
        }

        private async Task<int> GenerateIntervention(List<InterventionRecomModel> interventions)
        {
            int syncCount = 0;

            try
            {
                CallAPIService callAPI = new CallAPIService(_appSetting, _accessToken);

                #region Get Intervention Data Iron Form

                Dictionary<string, object> param = new Dictionary<string, object>();
                List<string> fieldsParam = new List<string>() { EnumQuery.KeyPbi };
                param.Add(EnumQuery.Fields, fieldsParam);
                param.Add(EnumQuery.IsDeleted, false.ToString().ToLower());

                var interventionsDB = await _repository.GetDataListByParam(param);
                JArray jInterventionsDB = JArray.Parse(JsonConvert.SerializeObject(interventionsDB));
                List<string> keyPbis = jInterventionsDB?.Descendants().OfType<JProperty>().Select(p => p.Value.ToString()).ToList();

                #endregion

                #region Generate Template

                interventions = interventions.Where(x => !keyPbis.Any(y => y == x.keyPbi) && x.interventionStatus == EnumStatus.InterventionAccepted).ToList();

                if (interventions?.Count > 0)
                {
                    GeneralService generalService = new GeneralService(_appSetting, _connectionFactory, EnumContainer.MasterSetting, _accessToken);

                    #region Get Common Template

                    Dictionary<string, object> paramRiskAssesment = new Dictionary<string, object>();
                    paramRiskAssesment.Add(EnumQuery.Key, EnumQuery.RiskAssesment);
                    var riskAssesmentResult = await _taskTemplateRepository.GetDataByParam(paramRiskAssesment);
                    var riskAssesmentTemplate = StaticHelper.GetPropValue(riskAssesmentResult, EnumQuery.Template);
                    var riskAssesment = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(riskAssesmentTemplate));

                    Dictionary<string, object> paramsafetyPrecaution = new Dictionary<string, object>();
                    paramsafetyPrecaution.Add(EnumQuery.Key, EnumQuery.SafetyPrecaution);
                    var safetyPrecautionResult = await _taskTemplateRepository.GetDataByParam(paramsafetyPrecaution);
                    var safetyPrecautionTemplate = StaticHelper.GetPropValue(safetyPrecautionResult, EnumQuery.Template);
                    var safetyPrecaution = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(safetyPrecautionTemplate));

                    #endregion

                    #region Get Intervention Exec Status

                    dynamic interventionExecStatusResult = await callAPI.EHMSGetAll(EnumController.InterventionExecutionStatus);
                    List<InterventionExecutionStatusModel> interventionExecStatus = JsonConvert.DeserializeObject<List<InterventionExecutionStatusModel>>(JsonConvert.SerializeObject(interventionExecStatusResult));

                    #endregion

                    #region Get UoM

                    dynamic uomResult = await callAPI.GetUoM();
                    List<UoMModel> uoms = JsonConvert.DeserializeObject<List<UoMModel>>(JsonConvert.SerializeObject(uomResult));

                    #endregion

                    #region Get Rating

                    dynamic ratingResult = await callAPI.EHMSGetAll(EnumController.MasterRating);
                    List<MasterRatingModel> ratings = JsonConvert.DeserializeObject<List<MasterRatingModel>>(JsonConvert.SerializeObject(ratingResult));
                    ratings = ratings.Where(x => x.rating != EnumCaption.NoneValue).ToList();

                    #endregion

                    #region Get Task Type Condition

                    dynamic taskTypeConditionResult = await callAPI.GetTaskTypeCondition(null, null);
                    List<TaskTypeConditionModel> taskTypeConditions = JsonConvert.DeserializeObject<List<TaskTypeConditionModel>>(JsonConvert.SerializeObject(taskTypeConditionResult));

                    #endregion

                    #region get status history

                    var UpdateBy = new EmployeeModel()
                    {
                        id = EnumCaption.System,
                        name = EnumCaption.System
                    };


                    DateTime currentDate = EnumCommonProperty.CurrentDateTime;

                    var statusHistory = new List<StatusHistoryModel>()
                        {
                            new StatusHistoryModel
                            {
                                status = EnumStatus.EFormOpen,
                                updatedBy = UpdateBy,
                                updatedDate =  currentDate.ToString(EnumFormatting.DateTimeToString),
                                tsUpdatedDate = (currentDate - new DateTime(1970, 1, 1)).TotalSeconds.ToString()
                            }
                        };
                    #endregion

                    foreach (var item in interventions)
                    {
                        InterventionModel newIntervention = new InterventionModel()
                        {
                            key = Guid.NewGuid().ToString(),
                            siteId = item.siteId,
                            sitedesc = item.sitedesc,
                            trInterventionHeaderId = item.trInterventionHeaderId.ToString(),
                            equipmentId = item.equipmentId.ToString(),
                            equipment = item.equipment,
                            equipmentDesc = $"{item.equipmentBrand} {item.equipmentModel}",
                            equipmentModel = item.equipmentModel,
                            equipmentBrand = item.equipmentBrand,
                            equipmentGroup = item.equipmentGroup,
                            componentId = item.componentId?.ToString(),
                            componentCode = item.componentCode,
                            componentDescription = item.componentDescription,
                            components = item.components,
                            sampleType = item.sampleType,
                            interventionCode = item.interventionCode,
                            interventionReason = item.interventionReason,
                            sampleDate = item.sampleDate.ToString(EnumFormatting.DefaultDateTimeToString),
                            sampleStatus = item.sampleStatus,
                            sampleStatusId = item.sampleStatusId.ToString(),
                            smu = item.smu,
                            smuDue = item.smuDue,
                            componentHm = item.componentHm,
                            mdInterventionStatusId = item.mdInterventionStatusId.ToString(),
                            interventionStatus = item.interventionStatus,
                            interventionStatusDesc = item.interventionStatusDesc,
                            interventionDiagnosis = item.interventionDiagnosis,
                            version = generalService.GetLastVersion().Result.Content.ToString(),
                            sapWorkOrder = item.sapWorkOrder.ToString(),
                            statusDatetime = item.statusDatetime?.ToString(EnumFormatting.DefaultDateTimeToString),
                            interventionExecutionId = interventionExecStatus.Where(x => x.intervention_execution == EnumStatus.EFormOpen).FirstOrDefault()?.md_intervention_execution_id.ToString(),
                            interventionExecution = EnumStatus.EFormOpen,
                            interventionExecutionBy = item.interventionExecutionBy,
                            defectStatusId = item.defectStatusId,
                            cautionRatingDate = item.sampleDate.ToString(EnumFormatting.DefaultDateTimeToString),
                            followUpPriority = item.followUpPriority.ToString(),
                            followUpPriorityUomId = item.followUpPriorityUomId.ToString(),
                            followUpPriorityUom = item.followUpPriorityUom,
                            keyPbi = item.keyPbi,
                            estimationCompletionDate = item.estimationCompletionDate?.ToString(EnumFormatting.DefaultDateTimeToString),
                            supervisor = "",
                            additionalInformation = "",
                            statusHistory = statusHistory,
                            defectStatus = "",
                            dayShift = "",
                            riskAssesment = riskAssesment,
                            safetyPrecaution = safetyPrecaution,
                            imageEquipment = "",
                            serviceStart = "",
                            serviceEnd = "",
                            hmOffset = item.hmOffset?.ToString()
                        };

                        List<InterventionRecomDetailModel> newDetails = new List<InterventionRecomDetailModel>() {
                                new InterventionRecomDetailModel(){ typeTaskId = EnumQuery.HeaderTask, isAdditionalTask = false }
                            };

                        List<InterventionRecomDetailModel> newAdjDetails = new List<InterventionRecomDetailModel>() {
                                new InterventionRecomDetailModel(){ typeTaskId = EnumQuery.HeaderTask, isAdditionalTask = true }
                            };

                        newDetails.AddRange(item.listInterventionDetail.Where(x => !Convert.ToBoolean(x.isAdditionalTask)));
                        newAdjDetails.AddRange(item.listInterventionDetail.Where(x => Convert.ToBoolean(x.isAdditionalTask)));

                        var customStep3 = new Dictionary<string, string>
                        {
                            { "[STEP 3 - DInspect] Get Detail Intervention", "" }
                        };

                        _telemetryClient.TrackEvent($"{nameof(InterventionService)}-{nameof(GenerateIntervention)}", customStep3);

                        List<dynamic> details = new List<dynamic>();

                        Dictionary<string, object> interventionTasks = new Dictionary<string, object>();
                        interventionTasks.Add(EnumQuery.Key, Guid.NewGuid().ToString());
                        interventionTasks.Add(EnumQuery.Group, EnumCaption.InterventionChecks);

                        List<dynamic> recomActions = new List<dynamic>();
                        recomActions.Add(await GenerateDetails(newDetails, uoms, taskTypeConditions, ratings));
                        recomActions.Add(await GenerateDetails(newAdjDetails, uoms, taskTypeConditions, ratings));
                        interventionTasks.Add(EnumQuery.Tasks, recomActions);

                        details.Add(interventionTasks);

                        //Dictionary<string, object> defectIndetify = new Dictionary<string, object>();
                        //defectIndetify.Add(EnumQuery.Key, Guid.NewGuid().ToString());
                        //defectIndetify.Add(EnumQuery.Group, EnumCaption.defectIdentify);
                        //defectIndetify.Add(EnumQuery.Tasks, new List<dynamic>());

                        //details.Add(defectIndetify);

                        newIntervention.details = details;
                        EmployeeModel employee = new EmployeeModel()
                        {
                            id = EnumCaption.System,
                            name = EnumCaption.System
                        };

                        Dictionary<string, object> interventionParam = new Dictionary<string, object>();
                        interventionParam.Add(EnumQuery.KeyPbi, newIntervention.keyPbi);
                        interventionParam.Add(EnumQuery.IsActive, true.ToString().ToLower());
                        interventionParam.Add(EnumQuery.IsDeleted, false.ToString().ToLower());

                        var interventionDB = await _repository.GetDataByParam(interventionParam);

                        if (interventionDB == null)
                        {
                            var addResult = await _repository.Create(new CreateRequest()
                            {
                                employee = employee,
                                entity = newIntervention
                            });

                            var customStep6 = new Dictionary<string, string>
                            {
                                { "[STEP 6 - DInspect] Insert Intervention to CosmosDB tr_intervention", "" }
                            };

                            _telemetryClient.TrackEvent($"{nameof(InterventionService)}-{nameof(GenerateIntervention)}", customStep6);

                            item.interventionExecution = EnumStatus.EFormOpen;
                            await UpdateInterventionHeaderEHMS(item, employee);

                            var customStep7 = new Dictionary<string, string>
                            {
                                { "[STEP 7 - DInspect] Update Intervention Header Ehms", "" },
                                { "[STEP 7 - DInspect] Update Intervention Header Ehms (Employee)", "" }
                            };

                            _telemetryClient.TrackEvent($"{nameof(InterventionService)}-{nameof(GenerateIntervention)}", customStep7);

                            syncCount++;
                        }
                    }
                }

                #endregion

                #region his intervention sync

                Dictionary<string, dynamic> hisInterventionSyncParam = new Dictionary<string, dynamic>();
                hisInterventionSyncParam.Add(EnumQuery.Key, Guid.NewGuid().ToString());
                hisInterventionSyncParam.Add(EnumQuery.Detail, $"{syncCount} data intervention success to sync");
                hisInterventionSyncParam.Add(EnumQuery.IsSuccess, true.ToString().ToLower());

                var hisInterventionSync = await _hisInterventionSync.Create(new CreateRequest()
                {
                    employee = new EmployeeModel() { id = EnumCaption.System, name = EnumCaption.System },
                    entity = hisInterventionSyncParam
                });

                #endregion
            }
            catch (Exception ex)
            {
                var properties = new Dictionary<string, string> { ["Service"] = nameof(InterventionService) };
                var measurements = new Dictionary<string, double> { ["Function"] = 0 };     // Send the exception telemetry:

                _telemetryClient.TrackException(ex, properties, measurements);
            }

            return syncCount;
        }

        private async Task UpdateInterventionHeaderEHMS(InterventionRecomModel intervention, EmployeeModel employee)
        {
            CallAPIService callAPI = new CallAPIService(_appSetting, _accessToken);

            #region Get Intervention Exec Status

            dynamic interventionExecStatusResult = await callAPI.EHMSGetAll(EnumController.InterventionExecutionStatus);
            List<InterventionExecutionStatusModel> interventionExecStatus = JsonConvert.DeserializeObject<List<InterventionExecutionStatusModel>>(JsonConvert.SerializeObject(interventionExecStatusResult));

            #endregion

            #region Get Intervention Defect Status

            dynamic defectStatusResult = await callAPI.EHMSGetAll(EnumController.MasterDefectStatus);
            List<MasterDefectStatusModel> defectStatus = JsonConvert.DeserializeObject<List<MasterDefectStatusModel>>(JsonConvert.SerializeObject(defectStatusResult));

            #endregion

            var interventionHeaderResult = await callAPI.EhmsGetInterventionForm(intervention.keyPbi.ToString());
            List<MasterDataInterventionHeaderRequest> interventionHeader = JsonConvert.DeserializeObject<List<MasterDataInterventionHeaderRequest>>(JsonConvert.SerializeObject(interventionHeaderResult));

            MasterDataInterventionHeaderRequest objectRequest = interventionHeader.Select(x => x).FirstOrDefault();

            objectRequest.InterventionExecutionStatusId = interventionExecStatus.Where(x => x.intervention_execution == intervention.interventionExecution).FirstOrDefault()?.md_intervention_execution_id;
            objectRequest.DefectStatusId = defectStatus.Where(x => x.defect_status == intervention.defectStatusId).FirstOrDefault()?.md_defect_status_id;
            objectRequest.InterventionExecutionBy = employee.name;
            objectRequest.EmployeeId = employee.id;
            objectRequest.EmployeeName = employee.name;
            objectRequest.AdditionalInformation = intervention.additionalInformation;
            objectRequest.KeyPbi = intervention.keyPbi;
            objectRequest.ServiceStart = string.IsNullOrEmpty(intervention.serviceStart) ? null : FormatHelper.ConvertToDateTime24(intervention.serviceStart);
            objectRequest.ServiceEnd = string.IsNullOrEmpty(intervention.serviceEnd) ? null : FormatHelper.ConvertToDateTime24(intervention.serviceEnd);
            objectRequest.IsDma = true;
            objectRequest.AdditionalTask = new List<dynamic>();

            await callAPI.UpdateEHMSInterventionHeader(objectRequest, employee.name);
        }

        private async Task UpdateInterventionDetailEHMS(dynamic updateTask)
        {
            var data = await _repository.Get(updateTask.id);

            CallAPIService callAPI = new CallAPIService(_appSetting, _accessToken);

            #region Get UoM

            dynamic uomResult = await callAPI.GetUoM();
            List<UoMModel> uoms = JsonConvert.DeserializeObject<List<UoMModel>>(JsonConvert.SerializeObject(uomResult));

            dynamic ratingResult = await callAPI.EHMSGetAll(EnumController.MasterRating);
            List<MasterRatingModel> ratings = JsonConvert.DeserializeObject<List<MasterRatingModel>>(JsonConvert.SerializeObject(ratingResult));

            #endregion

            foreach (var updateParam in updateTask.updateParams)
            {
                var desc = StaticHelper.GetPropValue(data, EnumQuery.Key, updateParam.keyValue, EnumQuery.Description);
                dynamic updatedData = null;

                if (desc == EnumCaption.Adjustment)
                    updatedData = StaticHelper.GetParentAdjData(data, EnumQuery.Key, updateParam.keyValue);
                else
                    updatedData = StaticHelper.GetParentData(data, EnumQuery.Key, updateParam.keyValue);

                Dictionary<string, object> detailId = new Dictionary<string, object>();
                detailId.Add(EnumQuery.TaskKey, updatedData?.key);
                detailId.Add(EnumQuery.Intervention_Header_Id, data?.trInterventionHeaderId);

                var value = StaticHelper.GetPropValue(JObject.Parse(JsonConvert.SerializeObject(updateParam)), EnumQuery.PropertyName, EnumQuery.Value, EnumQuery.PropertyValue)?.ToString();
                var valuDetailId = detailId.Select(x => x.Value).FirstOrDefault();

                if (valuDetailId != null)
                {
                    string valueItemType = StaticHelper.GetPropValue(data, updateParam.keyValue, EnumQuery.ValueItemType);

                    if (!string.IsNullOrEmpty(valueItemType))
                    {
                        dynamic detailResult = await callAPI.EHMSGetAllByParam(EnumController.InterventionDetail, detailId);

                        if (detailResult != null && ((JContainer)detailResult).Count > 0)
                        {
                            List<InterventionDetailModel> updatedDetails = JsonConvert.DeserializeObject<List<InterventionDetailModel>>(JsonConvert.SerializeObject(detailResult));

                            InterventionDetailModel updatedDetail = updatedDetails.Select(x => x).FirstOrDefault();
                            if (valueItemType == EnumValueItemType.InputUom)
                            {
                                value = string.IsNullOrEmpty(value) ? "0" : value;
                                updatedDetail.value = Convert.ToDecimal(value);
                            }
                            else if (valueItemType == EnumValueItemType.Uom)
                            {
                                value = string.IsNullOrEmpty(value) ? "0" : value;
                                updatedDetail.uom = Convert.ToInt64(value);
                            }
                            else if (valueItemType == EnumValueItemType.Rating)
                            {
                                updatedDetail.rating_id = ratings.Where(x => x.rating == value).FirstOrDefault()?.md_rating_id;
                            }
                            else if (valueItemType == EnumValueItemType.Condition)
                            {
                                updatedDetail.condition_code = value;
                            }

                            DateTime currentDate = EnumCommonProperty.CurrentDateTime;

                            updatedDetail.actual_intervention_date_by = updateTask.employee.id;
                            updatedDetail.actual_intervention_date = currentDate;
                            updatedDetail.executed = true;
                            updatedDetail.changed_by = updateTask.employee.name;
                            updatedDetail.changed_on = currentDate;

                            await callAPI.EHMSPut(EnumController.InterventionDetail, updatedDetail);
                        }
                    }
                }

            }
        }

        public async Task<ServiceResult> GetDefectIdentifiedCount(string id)
        {
            try
            {
                Version103Service version103Service = new Version103Service(_appSetting, _connectionFactory, _accessToken);

                TaskProgressResponseWithIdentifiedDefect result;

                var res = await version103Service.GetInterventionDefectHeader(id);
                string json = JsonConvert.SerializeObject(res);
                JObject jObj = JObject.Parse(json);

                var defectHeaders = jObj.SelectToken(EnumQuery.DefectHeader);


                var defectHeaderCount = jObj.SelectToken(EnumQuery.DefectHeader)
                    .Where(x => x.SelectToken(EnumQuery.Category).ToString() == EnumCategoryServiceSheet.CBM || x.SelectToken(EnumQuery.Category).ToString() == EnumCategoryServiceSheet.CBM_NORMAL)
                    .Count();

                var idCount = defectHeaderCount;

                foreach (JToken tk in defectHeaders)
                {
                    var tkId = tk.SelectToken(EnumQuery.ID).ToString();
                    idCount += jObj.SelectToken(EnumQuery.DefectDetail).Where(x => x.SelectToken(EnumQuery.DefectHeaderId).ToString() == tkId).Count();
                }

                var defectDetailCount = jObj.SelectToken(EnumQuery.DefectDetail).Count();

                result = new TaskProgressResponseWithIdentifiedDefect
                {
                    workorder = "",
                    group = EnumGroup.DefectIdentifiedService,
                    doneTask = 0,
                    totalTask = 0,
                    identifiedDefectCount = idCount
                };

                return new ServiceResult
                {
                    Message = "Get defect identified count successfully",
                    IsError = false,
                    Content = result
                };

            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<ServiceResult> GetTaskProgress(string id)
        {
            try
            {
                var data = await _repository.Get(id);

                UpdateTaskServiceHelper updateTaskService = new UpdateTaskServiceHelper(_appSetting, _connectionFactory, _container, _accessToken);
                TaskProgressResponse result = await updateTaskService.GetTaskProgress(data);

                return new ServiceResult
                {
                    Message = "Get task progress successfully",
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

        public async Task<ServiceResult> UpdateInterventionByEhms(UpdateInterventionByEhmsRequest updateRequest)
        {
            try
            {
                dynamic result = null;

                if (!string.IsNullOrEmpty(updateRequest.trInterventionHeaderId))
                {

                    Dictionary<string, object> interventionParam = new Dictionary<string, object>();
                    interventionParam.Add("trInterventionHeaderId", updateRequest.trInterventionHeaderId);

                    var rsc = await _repository.GetDataByParam(interventionParam);

                    if (rsc != null)
                    {

                        var intervention = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(rsc));

                        List<PropertyParam> propertyParams = new List<PropertyParam>()
                        {
                            new PropertyParam { propertyName = EnumQuery.MdInterventionStatusId, propertyValue = updateRequest.interventionStatusId },
                            new PropertyParam { propertyName = EnumQuery.InterventionStatus, propertyValue = updateRequest.interventionStatus },
                            new PropertyParam { propertyName = EnumQuery.InterventionDiagnosis, propertyValue = updateRequest.interventionDiagnosis },
                            new PropertyParam { propertyName = EnumQuery.FollowUpPriority, propertyValue = updateRequest.followUpPriority },
                            new PropertyParam { propertyName = EnumQuery.EstimationCompletionDate, propertyValue = updateRequest.estimationCompletionDate },
                            new PropertyParam { propertyName = EnumQuery.SapWorkOrder, propertyValue = updateRequest.sapWorkOrder }
                            //new PropertyParam { propertyName = EnumQuery.Version, propertyValue = DateTime.UtcNow.ToString("yyMMddHHmmss")}
                        };

                        UpdateRequest updateRequests = new UpdateRequest()
                        {
                            id = intervention.id,
                            employee = new EmployeeModel()
                            {
                                id = updateRequest.employeeId,
                                name = updateRequest.employeeName
                            },
                            updateParams = new List<UpdateParam>()
                            {
                                new UpdateParam()
                                {
                                    keyValue = intervention.key,
                                    propertyParams = propertyParams
                                }
                            }
                        };

                        result = await _repository.Update(updateRequests, rsc);

                    }
                }

                return new ServiceResult()
                {
                    Message = "Update Intervention by iron portal successfuly",
                    IsError = false,
                    Content = result
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<ServiceResult> GetInterventionList(string siteId)
        {
            try
            {
                InterventionServiceHelper interventionServiceHelper = new InterventionServiceHelper(_appSetting, _connectionFactory, _accessToken);
                var result = await interventionServiceHelper.GetInterventionList(siteId);

                return new ServiceResult()
                {
                    Message = "Get Intervention by iron portal successfuly",
                    IsError = false,
                    Content = result
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<ServiceResult> GetOpenIntervention(string siteId, string startDate, string endDate)
        {
            try
            {
                InterventionServiceHelper interventionServiceHelper = new InterventionServiceHelper(_appSetting, _connectionFactory, _accessToken);
                List<InterventionHeaderListModel> result = await interventionServiceHelper.GetInterventionList(siteId);
                result = result.Where(x => x.interventionExecution == EnumStatus.EFormOpen || x.interventionExecution == EnumStatus.EFormRevise).OrderBy(x => x.estimationCompletionDate).ToList();

                result = result.Where(x => x.estimationCompletionDate >= DateTime.Parse(startDate) && x.estimationCompletionDate <= DateTime.Parse(endDate)).ToList();

                return new ServiceResult()
                {
                    Message = "Get Open Intervention successfuly",
                    IsError = false,
                    Content = result
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<ServiceResult> updateInterventionAsync(UpdateParamIntervention request)
        {
            try
            {
                dynamic result = null;
                if (!string.IsNullOrEmpty(request.interventionHeaderId))
                {

                    Dictionary<string, object> interventionParam = new Dictionary<string, object>();
                    interventionParam.Add("trInterventionHeaderId", request.interventionHeaderId);

                    var rsc = await _repository.GetDataByParam(interventionParam);

                    if (rsc != null)
                    {

                        var intervention = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(rsc));

                        UpdateRequest updateRequests = new UpdateRequest()
                        {
                            id = intervention.id,
                            employee = new EmployeeModel()
                            {
                                id = request.employeeId,
                                name = ""
                            },
                            updateParams = new List<UpdateParam>()
                            {
                                new UpdateParam()
                                {
                                    keyValue = intervention.key,
                                    propertyParams = request.propertyParams
                                }
                            }
                        };

                        result = await _repository.Update(updateRequests, rsc);

                    }
                }

                return new ServiceResult()
                {
                    Message = "Update Intervention successfuly",
                    IsError = false,
                    Content = result
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<ServiceResult> GetInterventionRevise(GetInterventionReviseRequest request)
        {
            try
            {
                var _repoDetail = new InterventionRepository(_connectionFactory, EnumContainer.Intervention);

                var currentData = await _repoDetail.GetDataInterventionReviselByKey(request);

                if (currentData == null)
                    throw new Exception($"Current data work order {request.workOrder} not found");

                var _repoCbmHistory = new CbmHitoryRepository(_connectionFactory, EnumContainer.CbmHistory);

                Dictionary<string, object> paramHistory = new Dictionary<string, object>();
                paramHistory.Add(EnumQuery.SSWorkorder, request.workOrder);
                paramHistory.Add(EnumQuery.SSTaskKey, request.taskKey);

                int seqIdCount = 0;
                var resultHistory = await _repoCbmHistory.GetDataByParam(paramHistory);

                if (resultHistory != null)
                {
                    foreach (var itemHistory in resultHistory.detail.history)
                    {
                        itemHistory.seqId = seqIdCount;

                        seqIdCount++;
                    }

                    List<dynamic> dataSortingHistory = JsonConvert.DeserializeObject<List<dynamic>>(JsonConvert.SerializeObject(resultHistory.detail.history));

                    resultHistory.detail.history = JsonConvert.DeserializeObject<object>(JsonConvert.SerializeObject(dataSortingHistory.OrderByDescending(x => x.seqId).ToList()));
                }

                Dictionary<string, object> result = new Dictionary<string, object>();
                result.Add(EnumQuery.SSWorkorder, request.workOrder);
                result.Add(EnumQuery.Equipment, currentData[0].equipment);
                result.Add(EnumQuery.siteId, currentData[0].siteId);
                result.Add(EnumQuery.SSTaskKey, request.taskKey);
                result.Add("component", currentData[0].componentDescription);

                var splitDesc = currentData[0].description.ToString().Split(";");

                var curentDetail = new Dictionary<string, object>();
                curentDetail.Add(EnumQuery.SSTaskKey, request.taskKey);
                curentDetail.Add(EnumQuery.Category, currentData[0].category);
                curentDetail.Add(EnumQuery.Rating, currentData[0].taskValue);
                curentDetail.Add(EnumQuery.UpdatedBy, currentData[0].updatedBy);
                curentDetail.Add(EnumQuery.UpdatedDate, currentData[0].updatedDate);
                curentDetail.Add(EnumQuery.TaskNo, currentData[0].taskNo);
                curentDetail.Add(EnumQuery.MeasurementLocation, splitDesc[2]);
                curentDetail.Add(EnumQuery.MeasurementValue, currentData[0].measurementValue);
                curentDetail.Add(EnumQuery.SSUom, currentData[0].uom);

                result.Add("currentCondition", curentDetail);

                if (currentData[0].category == EnumTaskType.CBM && (currentData[0].rating == EnumRatingServiceSheet.AUTOMATIC || currentData[0].rating == EnumRatingServiceSheet.AUTOMATIC_REPLACEMENT))
                {
                    string modelCbm = currentData[0].equipmentModel.ToString();
                    string psType = currentData[0].psType.ToString();

                    CallAPIHelper callAPI = new CallAPIHelper(_accessToken);
                    ApiResponse response = await callAPI.Get($"{EnumUrl.GetParameterRatingOverwrite}&model={modelCbm}&psType={psType}");

                    List<dynamic> paramAdm = JsonConvert.DeserializeObject<List<dynamic>>(JsonConvert.SerializeObject(response.Result.Content));

                    if (paramAdm.Any())
                    {
                        List<ParameterRating> detailCbm = JsonConvert.DeserializeObject<List<ParameterRating>>(JsonConvert.SerializeObject(response.Result.Content[0].detail));
                        detailCbm = detailCbm.Where(x => x.taskKey == request.taskKey && x.component == currentData[0].componentDescription.ToString()).OrderBy(x => x.cbmRating).ToList();

                        if (!detailCbm.Where(x => x.minValue == null).Any())
                        {
                            result.Add("detailSpec", detailCbm);
                        }
                        else
                        {
                            result.Add("detailSpec", new List<string>());
                        }
                    }
                    else
                    {
                        result.Add("detailSpec", paramAdm);
                    }
                }
                else
                {
                    result.Add("detailSpec", null);
                }

                result.Add("detailedPicture", currentData[0].picture);
                result.Add("historyModified", resultHistory);

                var currentDataMaster = await _repoDetail.GetDataInterventionHistoryDefaultByKey(request);
                List<dynamic> objDataMaster = JsonConvert.DeserializeObject<List<dynamic>>(JsonConvert.SerializeObject(currentDataMaster));

                List<dynamic> resultListMasterTemplate = new List<dynamic>();

                resultListMasterTemplate.AddRange(objDataMaster);
                result.Add("historyModifiedDefault", resultListMasterTemplate);


                return new ServiceResult
                {
                    IsError = false,
                    Message = "Get intervention revise successfully",
                    Content = result
                };

            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    IsError = true,
                    Message = ex.Message.ToString()
                };
            }
        }

        #region Private Function

        private async Task<bool> UpdateStatusValidation(dynamic updateTaskRequest, bool isComparedLocalStatus)
        {
            var rsc = await _repository.Get(updateTaskRequest.id);
            InterventionRecomModel intervention = JsonConvert.DeserializeObject<InterventionRecomModel>(JsonConvert.SerializeObject(rsc));

            JObject updateRequestObj = JObject.Parse(JsonConvert.SerializeObject(updateTaskRequest));
            string localInterventionStatus = StaticHelper.GetPropValue(updateRequestObj, EnumQuery.LocalInterventionStatus)?.ToString();

            CallAPIService callAPI = new CallAPIService(_appSetting, _accessToken);
            dynamic interventionExecStatusResult = await callAPI.EHMSGetAll(EnumController.InterventionExecutionStatus);
            List<InterventionExecutionStatusModel> interventionExecStatus = JsonConvert.DeserializeObject<List<InterventionExecutionStatusModel>>(JsonConvert.SerializeObject(interventionExecStatusResult));

            int localExecutionId = (int)interventionExecStatus.Where(x => x.intervention_execution == localInterventionStatus).FirstOrDefault()?.md_intervention_execution_id;
            int cosmosExecutionId = (int)interventionExecStatus.Where(x => x.intervention_execution == intervention.interventionExecution).FirstOrDefault()?.md_intervention_execution_id;
            int onProgressId = (int)interventionExecStatus.Where(x => x.intervention_execution == EnumStatus.EFormOnProgress).FirstOrDefault()?.md_intervention_execution_id;

            if (isComparedLocalStatus)
                return !(localExecutionId < cosmosExecutionId && cosmosExecutionId > onProgressId);
            else
                return !(cosmosExecutionId > onProgressId);
            //return false;
        }

        #endregion
    }
}