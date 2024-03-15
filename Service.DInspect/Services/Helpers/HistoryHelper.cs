using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service.DInspect.Helpers;
using Service.DInspect.Interfaces;
using Service.DInspect.Models;
using Service.DInspect.Models.EHMS;
using Service.DInspect.Models.Entity;
using Service.DInspect.Models.Enum;
using Service.DInspect.Models.Helper;
using Service.DInspect.Models.Response;
using Service.DInspect.Repositories;
using Service.DInspect.Services;
using Service.DInspect.Repositories;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

public class HistoryHelper
{
    private MySetting _appSetting;
    private string _accessToken;
    private IConnectionFactory _connectionFactory;
    private IRepositoryBase _serviceSheetRepository;
    private IRepositoryBase _interventionRepository;
    private IRepositoryBase _interimEngineHeaderRepository;
    private IRepositoryBase _settingRepository;

    public HistoryHelper(MySetting appSetting, IConnectionFactory connectionFactory, string accessToken)
    {
        _appSetting = appSetting;
        _accessToken = accessToken;
        _connectionFactory= connectionFactory;
        _serviceSheetRepository = new InterventionRepository(connectionFactory, EnumContainer.ServiceSheetHeader);
        _interventionRepository = new InterventionRepository(connectionFactory, EnumContainer.Intervention);
        _interimEngineHeaderRepository = new InterimEngineDefectHeaderRepository(connectionFactory, EnumContainer.InterimEngineHeader);
        _settingRepository = new InterventionRepository(connectionFactory, EnumContainer.MasterSetting);
    }

    public async Task<List<ServiceSheetHistoryResponse>> GetHistory(string siteId = null, string startdate = null,string enddate = null)
    {
        CallAPIService callAPI = new CallAPIService(_appSetting, _accessToken);

        List<ServiceSheetHistoryResponse> result = new List<ServiceSheetHistoryResponse>() {
            new ServiceSheetHistoryResponse(){
                status = EnumStatus.EFormOpen,
                dataCount = 0,
                data = new TypeMonitoringStatus()
                {
                    Intervention = new List<dynamic>(),
                    Servicesheet = new List<DailyScheduleModel>(),
                    InterimEngine = new List<dynamic>()
                }
            },
            new ServiceSheetHistoryResponse(){
                status = EnumStatus.EFormOnProgress,
                dataCount = 0,
                data = new TypeMonitoringStatus()
                {
                    Intervention = new List<dynamic>(),
                    Servicesheet = new List<DailyScheduleModel>(),
                    InterimEngine = new List<dynamic>()
                }
            },
            new ServiceSheetHistoryResponse(){
                status = EnumStatus.EFormSubmited,
                dataCount = 0,
                data = new TypeMonitoringStatus()
                {
                    Intervention = new List<dynamic>(),
                    Servicesheet = new List<DailyScheduleModel>(),
                    InterimEngine = new List<dynamic>()
                }
            },
            //new ServiceSheetHistoryResponse(){
            //    status = EnumStatus.EFormApprovedSPV,
            //    dataCount = 0,
            //    data = new TypeMonitoringStatus()
            //    {
            //        Intervention = new List<dynamic>(),
            //        Servicesheet = new List<DailyScheduleModel>(),
            //        InterimEngine = new List<dynamic>()
            //    }
            //},
            new ServiceSheetHistoryResponse(){
                status = EnumStatus.EFormFinalReview,
                dataCount = 0,
                data = new TypeMonitoringStatus()
                {
                    Intervention = new List<dynamic>(),
                    Servicesheet = new List<DailyScheduleModel>(),
                    InterimEngine = new List<dynamic>()
                }
            },
            new ServiceSheetHistoryResponse(){
                status = EnumStatus.EFormClosed,
                dataCount = 0,
                data = new TypeMonitoringStatus()
                {
                    Intervention = new List<dynamic>(),
                    Servicesheet = new List<DailyScheduleModel>(),
                    InterimEngine = new List<dynamic>()
                }
            }
        };

        MasterSettingRepository _settingRepo = new MasterSettingRepository(_connectionFactory, EnumContainer.MasterSetting);
        var dbSettings = _settingRepo.GetAllData().Result;
        List<DBSetting> dBSettings = JsonConvert.DeserializeObject<List<DBSetting>>(JsonConvert.SerializeObject(dbSettings));
        EnumFormatting.appTimeZoneDesc = dBSettings.Where(x => x.key == EnumQuery.TimeZoneDesc).FirstOrDefault()?.value;

        #region Get status monitoring intervention

        Dictionary<string, object> settingParam = new Dictionary<string, object>();
        settingParam.Add(EnumQuery.Key, EnumQuery.InterventionMaxEstDate);

        var setting = await _settingRepository.GetDataByParam(settingParam);
        var maxDay = setting[EnumQuery.Value];
        DateTime curentDate = EnumCommonProperty.CurrentDateTime.AddDays(Convert.ToInt32(maxDay));

        Dictionary<string, object> paramIntervention = new Dictionary<string, object>();
        paramIntervention.Add(EnumQuery.InterventionStatus, EnumStatus.InterventionAccepted);
        paramIntervention.Add(EnumQuery.IsActive, true.ToString().ToLower());
        paramIntervention.Add(EnumQuery.IsDeleted, false.ToString().ToLower());

        // if siteId == HO Site then skip filter by site
        string groupFilter = EnumGeneralFilterGroup.Site;
        CallAPIHelper callAPIHelperFilter = new CallAPIHelper(_accessToken);
        var filterRes = await callAPIHelperFilter.Get(EnumUrl.GetGeneralFilter + $"?group={groupFilter}&ver=v1");
        IList<GeneralFilterHelperModel> filters = JsonConvert.DeserializeObject<List<GeneralFilterHelperModel>>(JsonConvert.SerializeObject(filterRes.Result.Content));

        if (filters.Any(x => x.Value == siteId))
            siteId = null;

        if (siteId != null)
            paramIntervention.Add(EnumQuery.siteId, siteId);

        var interventions = await _interventionRepository.GetDataListByParamJArray(paramIntervention);

        var interventionComponentSystemResult = await callAPI.GetInterventionComponentSystem();
        List<InterventionListModel> interventionComponentSystem = JsonConvert.DeserializeObject<List<InterventionListModel>>(JsonConvert.SerializeObject(interventionComponentSystemResult));

        foreach (var intervention in interventions)
        {
            JToken jIntervention = JToken.FromObject(intervention);
            string isDownload = jIntervention[EnumQuery.IsDownload] != null ? jIntervention[EnumQuery.IsDownload].ToString() : "false";
            intervention[EnumQuery.IsDownload] = isDownload;
            intervention[EnumQuery.ComponentSystem] = interventionComponentSystem.Where(x => x.KeyPbi == intervention[EnumQuery.KeyPbi]?.ToString()).FirstOrDefault()?.ComponentGroup;
        }

        InterventionServiceHelper interventionServiceHelper = new InterventionServiceHelper(_appSetting, _connectionFactory, _accessToken);
        List<InterventionHeaderListModel> openData = await interventionServiceHelper.GetInterventionList(siteId);
        openData = openData.Where(x => x.interventionExecution == EnumStatus.EFormOpen || x.interventionExecution == EnumStatus.EFormRevise).OrderBy(x => x.estimationCompletionDate).ToList();
        // Add feature filtering monitoring status
        openData = openData.Where(x => x.estimationCompletionDate >= DateTime.Parse(startdate) && x.estimationCompletionDate <= DateTime.Parse(enddate)).OrderBy(x => x.estimationCompletionDate).ToList();
        result.Where(x => x.status == EnumStatus.EFormOpen).FirstOrDefault().data.Intervention.AddRange(openData);

        //result.Where(x => x.status == EnumStatus.EFormOpen).FirstOrDefault().data.Intervention.AddRange(interventions.FilterLessEqualsThan(EnumQuery.EstimationCompletionDate, curentDate.ToString(EnumFormatting.DefaultDateTimeToString)).FilterEqual(EnumQuery.InterventionExecution, EnumStatus.EFormOpen).OrderBy(x => FormatHelper.ConvertToDateTime24(x[EnumQuery.EstimationCompletionDate].ToString())).ToList
        var _startDate = DateTime.Parse(startdate).ToString(EnumFormatting.DateToString); 
        var _endaDate = DateTime.Parse(enddate).ToString(EnumFormatting.DateToString);

        interventions = interventions.FilterMoreEqualsThan(EnumQuery.ServiceStart,_startDate).FilterLessEqualsThan(EnumQuery.ServiceEnd,_endaDate);
        //var _interventions = interventions.Where(x => x[EnumQuery.ServiceStart].ToString() != "" && (FormatHelper.ConvertToDate(FormatHelper.ConvertToDateTime24(x[EnumQuery.ServiceStart].ToString()).ToString(EnumFormatting.DateToString)) >= FormatHelper.ConvertToDate(_startDate)));


        result.Where(x => x.status == EnumStatus.EFormOnProgress).FirstOrDefault().data.Intervention.AddRange(interventions.FilterEqual(EnumQuery.InterventionExecution, EnumStatus.EFormOnProgress).OrderByDescending(x => FormatHelper.ConvertToDateTime24(x[EnumQuery.UpdatedDate].ToString())).ToList());
        result.Where(x => x.status == EnumStatus.EFormSubmited).FirstOrDefault().data.Intervention.AddRange(interventions.FilterEqual(EnumQuery.InterventionExecution, EnumStatus.EFormSubmited).OrderByDescending(x => FormatHelper.ConvertToDateTime24(x[EnumQuery.ServiceEnd].ToString())).ToList());
        //result.Where(x => x.status == EnumStatus.EFormApprovedSPV).FirstOrDefault().data.Intervention.AddRange(interventions.FilterEqual(EnumQuery.InterventionExecution, EnumStatus.EFormApprovedSPV)).OrderByDescending(x => FormatHelper.ConvertToDateTime24(x[EnumQuery.ServiceEnd].ToString())).ToList());
        result.Where(x => x.status == EnumStatus.EFormFinalReview).FirstOrDefault().data.Intervention.AddRange(interventions.FilterEqual(EnumQuery.InterventionExecution, EnumStatus.EFormClosed).FilterEqual(EnumQuery.DefectStatus, EnumStatus.DefectApprovedSPV).OrderByDescending(x => FormatHelper.ConvertToDateTime24(x[EnumQuery.ServiceEnd].ToString())).ToList());
        result.Where(x => x.status == EnumStatus.EFormClosed).FirstOrDefault().data.Intervention.AddRange(interventions.FilterEqual(EnumQuery.InterventionExecution, EnumStatus.EFormClosed).FilterNotEqual(EnumQuery.DefectStatus, EnumStatus.DefectApprovedSPV).OrderByDescending(x => FormatHelper.ConvertToDateTime24(x[EnumQuery.ServiceEnd].ToString())).ToList());

        #endregion

        #region Get status montoring servicesheet
        CallAPIHelper callAPIHelper = new CallAPIHelper(_accessToken);
        var dailySchedule = await callAPIHelper.Get(EnumUrl.GetAdmHistoryUrl + $"&siteId={siteId}");
        List<DailyScheduleModel> dailySchedules = JsonConvert.DeserializeObject<List<DailyScheduleModel>>(JsonConvert.SerializeObject(dailySchedule.Result.Content));
        List<DailyScheduleModel> dailySchedulesInterim = JsonConvert.DeserializeObject<List<DailyScheduleModel>>(JsonConvert.SerializeObject(dailySchedule.Result.Content));

        Dictionary<string, object> paramServiceSheet = new Dictionary<string, object>();
        paramServiceSheet.Add(EnumQuery.IsActive, "true");
        paramServiceSheet.Add(EnumQuery.IsDeleted, "false");
        if(siteId != null) paramServiceSheet.Add(EnumQuery.siteId, siteId);
        var serviceSheets = await _serviceSheetRepository.GetDataListByParamJArray(paramServiceSheet);

        foreach (var serviceSheet in dailySchedules)
        {
            var header = serviceSheets.FilterEqual(EnumQuery.SSWorkorder, serviceSheet.workOrder).FirstOrDefault();

            if (header != null)
            {
                JToken jHeader = JToken.FromObject(header);
                string status = jHeader[EnumQuery.Status].ToString();
                string defectStatus = jHeader[EnumQuery.DefectStatus].ToString();
                string isDownload = jHeader[EnumQuery.IsDownload] != null ? jHeader[EnumQuery.IsDownload].ToString() : "false";
                serviceSheet.status = status;
                serviceSheet.defectStatus = defectStatus;
                serviceSheet.isDownload = isDownload;

                //if (serviceSheet.status == EnumStatus.EFormOnProgress)
                //{
                    DateTime serviceStart;
                    DateTime serviceEnd;
                    if (DateTime.TryParseExact(jHeader[EnumQuery.ServiceStart].ToString(), "dd/MM/yy HH:mm:ss (AEST)", CultureInfo.InvariantCulture, DateTimeStyles.None, out serviceStart))
                    {
                        // Parsing was successful
                        serviceSheet.serviceStart = serviceStart.ToString("yyyy-MM-dd'T'HH:mm:ss");
                    }
                    else
                    {
                        // Parsing failed
                        serviceSheet.serviceStart = "";
                    }

                    if (DateTime.TryParseExact(jHeader[EnumQuery.ServiceEnd].ToString(), "dd/MM/yy HH:mm:ss (AEST)", CultureInfo.InvariantCulture, DateTimeStyles.None, out serviceEnd))
                    {
                        // Parsing was successful
                        serviceSheet.serviceEnd = serviceEnd.ToString("yyyy-MM-dd'T'HH:mm:ss");
                    }
                    else
                    {
                        // Parsing failed
                        serviceSheet.serviceEnd = serviceSheet.changedOn;
                    }
                //}
                if (!string.IsNullOrEmpty(serviceSheet.serviceStart) && !string.IsNullOrEmpty(serviceSheet.serviceEnd))
                {
                    string _serviceStart = DateTime.Parse(serviceSheet.serviceStart).ToString(EnumFormatting.DateToString);
                    string _serviceEnd = DateTime.Parse(serviceSheet.serviceEnd).ToString(EnumFormatting.DateToString);
                    _startDate = DateTime.Parse(startdate).ToString(EnumFormatting.DateToString);
                    _endaDate = DateTime.Parse(enddate).ToString(EnumFormatting.DateToString);
                    if (FormatHelper.ConvertToDate(_serviceStart) >= FormatHelper.ConvertToDate(_startDate) && FormatHelper.ConvertToDate(_serviceEnd) <= FormatHelper.ConvertToDate(_endaDate))
                    {
                        if (status == EnumStatus.EFormOpen)
                            result.Where(x => x.status == EnumStatus.EFormOpen).FirstOrDefault().data.Servicesheet.Add(serviceSheet);
                        else if (status == EnumStatus.EFormOnProgress)
                            result.Where(x => x.status == EnumStatus.EFormOnProgress).FirstOrDefault().data.Servicesheet.Add(serviceSheet);
                        else if (status == EnumStatus.EFormSubmited)
                            result.Where(x => x.status == EnumStatus.EFormSubmited).FirstOrDefault().data.Servicesheet.Add(serviceSheet);
                        //else if (status == EnumStatus.EFormApprovedSPV)
                        //    result.Where(x => x.status == EnumStatus.EFormApprovedSPV).FirstOrDefault().data.Servicesheet.Add(serviceSheet);
                        else if (status == EnumStatus.EFormClosed && defectStatus == EnumStatus.DefectApprovedSPV)
                            result.Where(x => x.status == EnumStatus.EFormFinalReview).FirstOrDefault().data.Servicesheet.Add(serviceSheet);
                        else if (status == EnumStatus.EFormClosed && defectStatus != EnumStatus.DefectApprovedSPV)
                            result.Where(x => x.status == EnumStatus.EFormClosed).FirstOrDefault().data.Servicesheet.Add(serviceSheet);
                    }
                }
            }
            else
            {
                var dateServicesToString = serviceSheet.dateService.ToString();
                DateTime dateServices = DateTime.Parse(dateServicesToString);
                DateTime startDates = DateTime.Parse(startdate);
                DateTime endDate = DateTime.Parse(enddate);

                if(dateServices >= DateTime.Parse(startdate) && dateServices <= DateTime.Parse(enddate))
                    result.Where(x => x.status == EnumStatus.EFormOpen).FirstOrDefault().data.Servicesheet.Add(serviceSheet);
            }
        }

        // sorting yet to start by serviceDate
        result.Where(x => x.status == EnumStatus.EFormOpen).FirstOrDefault().data.Servicesheet = result.Where(x => x.status == EnumStatus.EFormOpen).FirstOrDefault().data.Servicesheet.OrderByDescending(x => x.dateService).ToList();
        // sorting in progress by serviceStart
        result.Where(x => x.status == EnumStatus.EFormOnProgress).FirstOrDefault().data.Servicesheet = result.Where(x => x.status == EnumStatus.EFormOnProgress).FirstOrDefault().data.Servicesheet.OrderByDescending(x => x.serviceStart).ToList();
        // sorting under review by service actual finish
        result.Where(x => x.status == EnumStatus.EFormSubmited).FirstOrDefault().data.Servicesheet = result.Where(x => x.status == EnumStatus.EFormSubmited).FirstOrDefault().data.Servicesheet.OrderByDescending(x => x.serviceEnd).ToList();
        // sorting final review by service actual finish
        result.Where(x => x.status == EnumStatus.EFormFinalReview).FirstOrDefault().data.Servicesheet = result.Where(x => x.status == EnumStatus.EFormFinalReview).FirstOrDefault().data.Servicesheet.OrderByDescending(x => x.serviceEnd).ToList();

        result.Where(x => x.status == EnumStatus.EFormClosed).FirstOrDefault().data.Servicesheet = result.Where(x => x.status == EnumStatus.EFormClosed).FirstOrDefault().data.Servicesheet.OrderByDescending(x => x.serviceEnd).ToList();
        #endregion

        #region Get monitoring status interim engine service
        Dictionary<string, object> paramInterim = new Dictionary<string, object>();
        paramInterim.Add(EnumQuery.IsActive, "true");
        paramInterim.Add(EnumQuery.IsDeleted, "false");
        if (siteId != null) paramInterim.Add(EnumQuery.siteId, siteId);
        var interimEngine = await _interimEngineHeaderRepository.GetDataListByParamJArray(paramInterim);

        foreach (var interim in dailySchedulesInterim)
        {
            var header = interimEngine.FilterEqual(EnumQuery.SSWorkorder, interim.workOrder).FirstOrDefault();

            if (header != null)
            {
                JToken jHeader = JToken.FromObject(header);
                string status = jHeader[EnumQuery.Status].ToString();
                string defectStatus = jHeader[EnumQuery.DefectStatus].ToString();
                string isDownload = jHeader[EnumQuery.IsDownload] != null ? jHeader[EnumQuery.IsDownload].ToString() : "false";
                interim.status = status;
                interim.defectStatus = defectStatus;
                interim.isDownload = isDownload;

                //if (interim.status == EnumStatus.EFormOnProgress)
                //{
                    DateTime serviceStart;
                    DateTime serviceEnd;
                    if (DateTime.TryParseExact(jHeader[EnumQuery.ServiceStart].ToString(), "dd/MM/yy HH:mm:ss (AEST)", CultureInfo.InvariantCulture, DateTimeStyles.None, out serviceStart))
                    {
                        // Parsing was successful
                        interim.serviceStart = serviceStart.ToString("yyyy-MM-dd'T'HH:mm:ss");
                    }
                    else
                    {
                        // Parsing failed
                        interim.serviceStart = "";
                    }

                    if (DateTime.TryParseExact(jHeader[EnumQuery.ServiceEnd].ToString(), "dd/MM/yy HH:mm:ss (AEST)", CultureInfo.InvariantCulture, DateTimeStyles.None, out serviceEnd))
                    {
                        // Parsing was successful
                        interim.serviceEnd = serviceEnd.ToString("yyyy-MM-dd'T'HH:mm:ss");
                    }
                    else
                    {
                        // Parsing failed
                        interim.serviceEnd = interim.changedOn;
                }
                //}
                if (!string.IsNullOrEmpty(interim.serviceStart) && !string.IsNullOrEmpty(interim.serviceEnd))
                {
                    string _serviceStart = DateTime.Parse(interim.serviceStart).ToString(EnumFormatting.DateToString);
                    string _serviceEnd = DateTime.Parse(interim.serviceEnd).ToString(EnumFormatting.DateToString);
                    _startDate = DateTime.Parse(startdate).ToString(EnumFormatting.DateToString);
                    _endaDate = DateTime.Parse(enddate).ToString(EnumFormatting.DateToString);
                    if (FormatHelper.ConvertToDate(_serviceStart) >= FormatHelper.ConvertToDate(_startDate) && FormatHelper.ConvertToDate(_serviceEnd) <= FormatHelper.ConvertToDate(_endaDate))
                    {
                        //if (status == EnumStatus.EFormOpen)
                        //result.Where(x => x.status == EnumStatus.IEngineOpen).FirstOrDefault().data.InterimEngine.Add(interim);
                        if (status == EnumStatus.IEngineOnProgress)
                            result.Where(x => x.status == EnumStatus.IEngineOnProgress).FirstOrDefault().data.InterimEngine.Add(interim);
                        else if (status == EnumStatus.IEngineSubmited)
                            result.Where(x => x.status == EnumStatus.IEngineSubmited).FirstOrDefault().data.InterimEngine.Add(interim);
                        //else if (status == EnumStatus.EFormApprovedSPV)
                        //    result.Where(x => x.status == EnumStatus.EFormApprovedSPV).FirstOrDefault().data.Servicesheet.Add(serviceSheet);
                        else if (status == EnumStatus.IEngineClosed && defectStatus == EnumStatus.DefectApprovedSPV)
                            result.Where(x => x.status == EnumStatus.EFormFinalReview).FirstOrDefault().data.InterimEngine.Add(interim);
                        else if (status == EnumStatus.IEngineClosed && defectStatus != EnumStatus.DefectApprovedSPV)
                            result.Where(x => x.status == EnumStatus.EFormClosed).FirstOrDefault().data.InterimEngine.Add(interim);
                    }
                }
            }
            else
            {
                //result.Where(x => x.status == EnumStatus.EFormOpen).FirstOrDefault().data.InterimEngine.Add(interim);
            }
        }

        // sorting yet to start by serviceDate
        //result.Where(x => x.status == EnumStatus.IEngineOpen).FirstOrDefault().data.Servicesheet = result.Where(x => x.status == EnumStatus.EFormOpen).FirstOrDefault().data.Servicesheet.OrderBy(x => x.dateService).ToList();
        // sorting in progress by serviceStart
        result.Where(x => x.status == EnumStatus.IEngineOnProgress).FirstOrDefault().data.InterimEngine = result.Where(x => x.status == EnumStatus.IEngineOnProgress).FirstOrDefault().data.InterimEngine.OrderByDescending(x => x.serviceStart).ToList();
        // sorting under review by service actual finish
        result.Where(x => x.status == EnumStatus.IEngineSubmited).FirstOrDefault().data.InterimEngine = result.Where(x => x.status == EnumStatus.IEngineSubmited).FirstOrDefault().data.InterimEngine.OrderByDescending(x => x.serviceEnd).ToList();
        // sorting final review by service actual finish
        result.Where(x => x.status == EnumStatus.EFormFinalReview).FirstOrDefault().data.InterimEngine = result.Where(x => x.status == EnumStatus.EFormFinalReview).FirstOrDefault().data.InterimEngine.OrderByDescending(x => x.serviceEnd).ToList();

        result.Where(x => x.status == EnumStatus.EFormClosed).FirstOrDefault().data.InterimEngine = result.Where(x => x.status == EnumStatus.EFormClosed).FirstOrDefault().data.InterimEngine.OrderByDescending(x => x.serviceEnd).ToList();
        #endregion

        foreach (var item in result)
            item.dataCount = item.data.Intervention.Count + item.data.Servicesheet.Count;

        return result;
    }

    public async Task<List<ServiceSheetHistoryResponse>> GetHistoryV2(string siteId = null, string startdate = null, string enddate = null)
    {
        CallAPIService callAPI = new CallAPIService(_appSetting, _accessToken);

        List<ServiceSheetHistoryResponse> result = new List<ServiceSheetHistoryResponse>() {
            new ServiceSheetHistoryResponse(){
                status = EnumStatus.EFormOpen,
                dataCount = 0,
                data = new TypeMonitoringStatus()
                {
                    Intervention = new List<dynamic>(),
                    Servicesheet = new List<DailyScheduleModel>(),
                    InterimEngine = new List<dynamic>()
                }
            },
            new ServiceSheetHistoryResponse(){
                status = EnumStatus.EFormOnProgress,
                dataCount = 0,
                data = new TypeMonitoringStatus()
                {
                    Intervention = new List<dynamic>(),
                    Servicesheet = new List<DailyScheduleModel>(),
                    InterimEngine = new List<dynamic>()
                }
            },
            new ServiceSheetHistoryResponse(){
                status = EnumStatus.EFormSubmited,
                dataCount = 0,
                data = new TypeMonitoringStatus()
                {
                    Intervention = new List<dynamic>(),
                    Servicesheet = new List<DailyScheduleModel>(),
                    InterimEngine = new List<dynamic>()
                }
            },
            //new ServiceSheetHistoryResponse(){
            //    status = EnumStatus.EFormApprovedSPV,
            //    dataCount = 0,
            //    data = new TypeMonitoringStatus()
            //    {
            //        Intervention = new List<dynamic>(),
            //        Servicesheet = new List<DailyScheduleModel>(),
            //        InterimEngine = new List<dynamic>()
            //    }
            //},
            new ServiceSheetHistoryResponse(){
                status = EnumStatus.EFormFinalReview,
                dataCount = 0,
                data = new TypeMonitoringStatus()
                {
                    Intervention = new List<dynamic>(),
                    Servicesheet = new List<DailyScheduleModel>(),
                    InterimEngine = new List<dynamic>()
                }
            },
            new ServiceSheetHistoryResponse(){
                status = EnumStatus.EFormClosed,
                dataCount = 0,
                data = new TypeMonitoringStatus()
                {
                    Intervention = new List<dynamic>(),
                    Servicesheet = new List<DailyScheduleModel>(),
                    InterimEngine = new List<dynamic>()
                }
            }
        };

        MasterSettingRepository _settingRepo = new MasterSettingRepository(_connectionFactory, EnumContainer.MasterSetting);
        var dbSettings = _settingRepo.GetAllData().Result;
        List<DBSetting> dBSettings = JsonConvert.DeserializeObject<List<DBSetting>>(JsonConvert.SerializeObject(dbSettings));
        EnumFormatting.appTimeZoneDesc = dBSettings.Where(x => x.key == EnumQuery.TimeZoneDesc).FirstOrDefault()?.value;

        var startDate = DateTime.Parse(startdate);
        var endDate = DateTime.Parse(enddate).Date.AddDays(1).AddTicks(-1);

        #region Get status monitoring intervention

        Dictionary<string, object> settingParam = new Dictionary<string, object>();
        settingParam.Add(EnumQuery.Key, EnumQuery.InterventionMaxEstDate);

        var setting = await _settingRepository.GetDataByParam(settingParam);
        var maxDay = setting[EnumQuery.Value];
        DateTime curentDate = EnumCommonProperty.CurrentDateTime.AddDays(Convert.ToInt32(maxDay));

        Dictionary<string, object> paramIntervention = new Dictionary<string, object>();
        paramIntervention.Add(EnumQuery.InterventionStatus, EnumStatus.InterventionAccepted);
        paramIntervention.Add(EnumQuery.IsActive, true.ToString().ToLower());
        paramIntervention.Add(EnumQuery.IsDeleted, false.ToString().ToLower());

        // if siteId == HO Site then skip filter by site
        string groupFilter = EnumGeneralFilterGroup.Site;
        CallAPIHelper callAPIHelperFilter = new CallAPIHelper(_accessToken);
        var filterRes = await callAPIHelperFilter.Get(EnumUrl.GetGeneralFilter + $"?group={groupFilter}&ver=v1");
        IList<GeneralFilterHelperModel> filters = JsonConvert.DeserializeObject<List<GeneralFilterHelperModel>>(JsonConvert.SerializeObject(filterRes.Result.Content));

        if (filters.Any(x => x.Value == siteId))
            siteId = null;

        if (siteId != null)
            paramIntervention.Add(EnumQuery.siteId, siteId);

        var interventions = await _interventionRepository.GetDataListByParamJArray(paramIntervention);

        var interventionComponentSystemResult = await callAPI.GetInterventionComponentSystem();
        List<InterventionListModel> interventionComponentSystem = JsonConvert.DeserializeObject<List<InterventionListModel>>(JsonConvert.SerializeObject(interventionComponentSystemResult));

        foreach (var intervention in interventions)
        {
            JToken jIntervention = JToken.FromObject(intervention);
            string isDownload = jIntervention[EnumQuery.IsDownload] != null ? jIntervention[EnumQuery.IsDownload].ToString() : "false";
            intervention[EnumQuery.IsDownload] = isDownload;
            intervention[EnumQuery.ComponentSystem] = interventionComponentSystem.Where(x => x.KeyPbi == intervention[EnumQuery.KeyPbi]?.ToString()).FirstOrDefault()?.ComponentGroup;
        }

        InterventionServiceHelper interventionServiceHelper = new InterventionServiceHelper(_appSetting, _connectionFactory, _accessToken);
        List<InterventionHeaderListModel> openData = await interventionServiceHelper.GetInterventionList(siteId);
        //openData = openData.Where(x => x.interventionExecution == EnumStatus.EFormOpen || x.interventionExecution == EnumStatus.EFormRevise).OrderBy(x => x.estimationCompletionDate).ToList();
        // Add feature filtering monitoring status
        openData = openData.Where(x => (x.interventionExecution == EnumStatus.EFormOpen || x.interventionExecution == EnumStatus.EFormRevise) && x.estimationCompletionDate >= startDate && x.estimationCompletionDate <= endDate).OrderBy(x => x.estimationCompletionDate).ToList();
        result.Where(x => x.status == EnumStatus.EFormOpen).FirstOrDefault().data.Intervention.AddRange(openData);

        //result.Where(x => x.status == EnumStatus.EFormOpen).FirstOrDefault().data.Intervention.AddRange(interventions.FilterLessEqualsThan(EnumQuery.EstimationCompletionDate, curentDate.ToString(EnumFormatting.DefaultDateTimeToString)).FilterEqual(EnumQuery.InterventionExecution, EnumStatus.EFormOpen).OrderBy(x => FormatHelper.ConvertToDateTime24(x[EnumQuery.EstimationCompletionDate].ToString())).ToList
        var _startDate = startDate.ToString(EnumFormatting.DateToString);
        var _endDate = endDate.ToString(EnumFormatting.DateToString);

        //interventions = interventions.FilterMoreEqualsThan(EnumQuery.ServiceStart, _startDate).FilterLessEqualsThan(EnumQuery.ServiceEnd, _endDate);
        //var _interventions = interventions.Where(x => x[EnumQuery.ServiceStart].ToString() != "" && (FormatHelper.ConvertToDate(FormatHelper.ConvertToDateTime24(x[EnumQuery.ServiceStart].ToString()).ToString(EnumFormatting.DateToString)) >= FormatHelper.ConvertToDate(_startDate)));

        /*result.Where(x => x.status == EnumStatus.EFormOnProgress).FirstOrDefault().data.Intervention.AddRange(interventions.FilterEqual(EnumQuery.InterventionExecution, EnumStatus.EFormOnProgress).OrderByDescending(x => FormatHelper.ConvertToDateTime24(x[EnumQuery.UpdatedDate].ToString())).ToList());
        result.Where(x => x.status == EnumStatus.EFormSubmited).FirstOrDefault().data.Intervention.AddRange(interventions.FilterEqual(EnumQuery.InterventionExecution, EnumStatus.EFormSubmited).OrderByDescending(x => FormatHelper.ConvertToDateTime24(x[EnumQuery.ServiceEnd].ToString())).ToList());
        //result.Where(x => x.status == EnumStatus.EFormApprovedSPV).FirstOrDefault().data.Intervention.AddRange(interventions.FilterEqual(EnumQuery.InterventionExecution, EnumStatus.EFormApprovedSPV)).OrderByDescending(x => FormatHelper.ConvertToDateTime24(x[EnumQuery.ServiceEnd].ToString())).ToList());
        result.Where(x => x.status == EnumStatus.EFormFinalReview).FirstOrDefault().data.Intervention.AddRange(interventions.FilterEqual(EnumQuery.InterventionExecution, EnumStatus.EFormClosed).FilterEqual(EnumQuery.DefectStatus, EnumStatus.DefectApprovedSPV).OrderByDescending(x => FormatHelper.ConvertToDateTime24(x[EnumQuery.ServiceEnd].ToString())).ToList());
        result.Where(x => x.status == EnumStatus.EFormClosed).FirstOrDefault().data.Intervention.AddRange(interventions.FilterEqual(EnumQuery.InterventionExecution, EnumStatus.EFormClosed).FilterNotEqual(EnumQuery.DefectStatus, EnumStatus.DefectApprovedSPV).OrderByDescending(x => FormatHelper.ConvertToDateTime24(x[EnumQuery.ServiceEnd].ToString())).ToList());*/

        result.Where(x => x.status == EnumStatus.EFormOnProgress).FirstOrDefault().data.Intervention.AddRange
            (interventions.Where(x => x[EnumQuery.InterventionExecution].ToString() == EnumStatus.EFormOnProgress && x[EnumQuery.UpdatedDate].ToString() != "" && FormatHelper.ConvertToDateTime24(x[EnumQuery.UpdatedDate].ToString()) >= startDate && FormatHelper.ConvertToDateTime24(x[EnumQuery.UpdatedDate].ToString()) <= endDate)
            .OrderByDescending(x => FormatHelper.ConvertToDateTime24(x[EnumQuery.UpdatedDate].ToString())).ToList());
        result.Where(x => x.status == EnumStatus.EFormSubmited).FirstOrDefault().data.Intervention.AddRange
            (interventions.Where(x => x[EnumQuery.InterventionExecution].ToString() == EnumStatus.EFormSubmited && x[EnumQuery.ServiceEnd].ToString() != "" && FormatHelper.ConvertToDateTime24(x[EnumQuery.ServiceEnd].ToString()) >= startDate && FormatHelper.ConvertToDateTime24(x[EnumQuery.ServiceEnd].ToString()) <= endDate)
            .OrderByDescending(x => FormatHelper.ConvertToDateTime24(x[EnumQuery.ServiceEnd].ToString())).ToList());
        //result.Where(x => x.status == EnumStatus.EFormApprovedSPV).FirstOrDefault().data.Intervention.AddRange(interventions.FilterEqual(EnumQuery.InterventionExecution, EnumStatus.EFormApprovedSPV)).OrderByDescending(x => FormatHelper.ConvertToDateTime24(x[EnumQuery.ServiceEnd].ToString())).ToList());
        result.Where(x => x.status == EnumStatus.EFormFinalReview).FirstOrDefault().data.Intervention.AddRange
            (interventions.Where(x => x[EnumQuery.InterventionExecution].ToString() == EnumStatus.EFormClosed && x[EnumQuery.DefectStatus].ToString() == EnumStatus.DefectApprovedSPV && x[EnumQuery.ServiceEnd].ToString() != "" && FormatHelper.ConvertToDateTime24(x[EnumQuery.ServiceEnd].ToString()) >= startDate && FormatHelper.ConvertToDateTime24(x[EnumQuery.ServiceEnd].ToString()) <= endDate)
            .OrderByDescending(x => FormatHelper.ConvertToDateTime24(x[EnumQuery.ServiceEnd].ToString())).ToList());
        result.Where(x => x.status == EnumStatus.EFormClosed).FirstOrDefault().data.Intervention.AddRange
            (interventions.Where(x => x[EnumQuery.InterventionExecution].ToString() == EnumStatus.EFormClosed && x[EnumQuery.DefectStatus].ToString() != EnumStatus.DefectApprovedSPV && x[EnumQuery.ServiceEnd].ToString() != "" && FormatHelper.ConvertToDateTime24(x[EnumQuery.ServiceEnd].ToString()) >= startDate && FormatHelper.ConvertToDateTime24(x[EnumQuery.ServiceEnd].ToString()) <= endDate)
            .OrderByDescending(x => FormatHelper.ConvertToDateTime24(x[EnumQuery.ServiceEnd].ToString())).ToList());

        #endregion

        #region Get status montoring servicesheet
        CallAPIHelper callAPIHelper = new CallAPIHelper(_accessToken);
        var dailySchedule = await callAPIHelper.Get(EnumUrl.GetAdmHistoryUrl + $"&siteId={siteId}");
        List<DailyScheduleModel> dailySchedules = JsonConvert.DeserializeObject<List<DailyScheduleModel>>(JsonConvert.SerializeObject(dailySchedule.Result.Content));
        List<DailyScheduleModel> dailySchedulesInterim = JsonConvert.DeserializeObject<List<DailyScheduleModel>>(JsonConvert.SerializeObject(dailySchedule.Result.Content));

        List<string> fieldsParam = new List<string>() { EnumQuery.SSWorkorder, EnumQuery.ID, EnumQuery.Key, EnumQuery.DefectStatus, EnumQuery.Status, EnumQuery.IsDownload, EnumQuery.ServiceStart, EnumQuery.ServiceEnd };
        JArray dailyWorkOrderList = JArray.FromObject(dailySchedules.Select(x => x.workOrder));

        Dictionary<string, object> paramServiceSheet = new Dictionary<string, object>();
        paramServiceSheet.Add(EnumQuery.IsActive, "true");
        paramServiceSheet.Add(EnumQuery.IsDeleted, "false");
        paramServiceSheet.Add(EnumQuery.SSWorkorder, dailyWorkOrderList);
        paramServiceSheet.Add(EnumQuery.Fields, fieldsParam);
        if (siteId != null) paramServiceSheet.Add(EnumQuery.siteId, siteId);
        var serviceSheets = await _serviceSheetRepository.GetDataListByParamJArray(paramServiceSheet);

        List<ServiceHeaderMonitoringStatusResponse> servicsSheetObj = JsonConvert.DeserializeObject<List<ServiceHeaderMonitoringStatusResponse>>(JsonConvert.SerializeObject(serviceSheets));

        DateTime serviceStart;
        DateTime serviceEnd;
        var objDaily = (from dailyData in dailySchedules
                        join serviceSheetData in servicsSheetObj on dailyData.workOrder equals serviceSheetData.workOrder
                        select new DailyScheduleModel
                        {
                            dailyScheduleId = dailyData.dailyScheduleId,
                            unitNumber = dailyData.unitNumber,
                            equipmentModel = dailyData.equipmentModel,
                            brand = dailyData.brand,
                            smuDue = dailyData.smuDue,
                            workOrder = dailyData.workOrder,
                            psType = dailyData.psType,
                            dateService = dailyData.dateService,
                            shift = dailyData.shift,
                            isActive = dailyData.isActive,
                            startDate = dailyData.startDate,
                            endDate = dailyData.endDate,
                            createdOn = dailyData.createdOn,
                            createdBy = dailyData.createdBy,
                            changedBy = dailyData.changedBy,
                            changedOn = dailyData.changedOn,
                            eFormId = dailyData.eFormId,
                            eFormKey = dailyData.eFormKey,
                            status = serviceSheetData.status, //
                            defectStatus = serviceSheetData.defectStatus, //
                            isDownload = serviceSheetData.isDownload, //
                            eFormStatus = dailyData.eFormStatus,
                            form = dailyData.form,
                            serviceStart = serviceSheetData.serviceStart.ToString() == "" ? "" : FormatHelper.ConvertToDateTime24(serviceSheetData.serviceStart.ToString()).ToString("yyyy-MM-dd'T'HH:mm:ss"),
                            serviceEnd = serviceSheetData.serviceEnd.ToString() == "" ? dailyData.changedOn : FormatHelper.ConvertToDateTime24(serviceSheetData.serviceEnd.ToString()).ToString("yyyy-MM-dd'T'HH:mm:ss")
                            //serviceStart = DateTime.TryParseExact(serviceSheetData.serviceStart.ToString(), "dd/MM/yy HH:mm:ss (AEST)", CultureInfo.InvariantCulture, DateTimeStyles.None, out serviceStart) == true ? serviceStart.ToString("yyyy-MM-dd'T'HH:mm:ss") : "",
                            //serviceEnd = DateTime.TryParseExact(serviceSheetData.serviceEnd.ToString(), "dd/MM/yy HH:mm:ss (AEST)", CultureInfo.InvariantCulture, DateTimeStyles.None, out serviceEnd) == true ? serviceEnd.ToString("yyyy-MM-dd'T'HH:mm:ss") : dailyData.changedOn
                        })
                        .Where(x =>
                        (x.status == EnumStatus.EFormOnProgress ?
                            (!string.IsNullOrEmpty(x.serviceStart) && DateTime.Parse(x.serviceStart) >= startDate && DateTime.Parse(x.serviceStart) <= endDate) :
                            (!string.IsNullOrEmpty(x.serviceEnd) && DateTime.Parse(x.serviceEnd) >= startDate && DateTime.Parse(x.serviceEnd) <= endDate)
                        )
                        ).ToList();


        /*result.Where(x => x.status == EnumStatus.EFormOpen).FirstOrDefault()
              .data.Servicesheet.AddRange(objDaily.Where(x => x.status == EnumStatus.EFormOpen).ToList());*/

        result.Where(x => x.status == EnumStatus.EFormOnProgress).FirstOrDefault()
              .data.Servicesheet.AddRange(objDaily.Where(x => x.status == EnumStatus.EFormOnProgress).ToList());
        result.Where(x => x.status == EnumStatus.EFormSubmited).FirstOrDefault()
              .data.Servicesheet.AddRange(objDaily.Where(x => x.status == EnumStatus.EFormSubmited).ToList());
        result.Where(x => x.status == EnumStatus.EFormFinalReview).FirstOrDefault()
              .data.Servicesheet.AddRange(objDaily.Where(x => x.status == EnumStatus.EFormClosed &&
                                                              x.defectStatus == EnumStatus.DefectApprovedSPV).ToList());
        result.Where(x => x.status == EnumStatus.EFormClosed).FirstOrDefault()
              .data.Servicesheet.AddRange(objDaily.Where(x => x.status == EnumStatus.EFormClosed &&
                                                              x.defectStatus != EnumStatus.DefectApprovedSPV).ToList());

        // Status Yet To Start
        var objDailyYeToStart = dailySchedules.Where(x => !servicsSheetObj.Select(o => o.workOrder).Contains(x.workOrder) &&
                                                          DateTime.Parse(x.dateService.ToString()) >= startDate &&
                                                          DateTime.Parse(x.dateService.ToString()) <= endDate).ToList();

        result.Where(x => x.status == EnumStatus.EFormOpen).FirstOrDefault().data.Servicesheet.AddRange(objDailyYeToStart);


        // sorting yet to start by serviceDate
        result.Where(x => x.status == EnumStatus.EFormOpen).FirstOrDefault().data.Servicesheet = result.Where(x => x.status == EnumStatus.EFormOpen).FirstOrDefault().data.Servicesheet.OrderByDescending(x => x.dateService).ToList();
        // sorting in progress by serviceStart
        result.Where(x => x.status == EnumStatus.EFormOnProgress).FirstOrDefault().data.Servicesheet = result.Where(x => x.status == EnumStatus.EFormOnProgress).FirstOrDefault().data.Servicesheet.OrderByDescending(x => x.serviceStart).ToList();
        // sorting under review by service actual finish
        result.Where(x => x.status == EnumStatus.EFormSubmited).FirstOrDefault().data.Servicesheet = result.Where(x => x.status == EnumStatus.EFormSubmited).FirstOrDefault().data.Servicesheet.OrderByDescending(x => x.serviceEnd).ToList();
        // sorting final review by service actual finish
        result.Where(x => x.status == EnumStatus.EFormFinalReview).FirstOrDefault().data.Servicesheet = result.Where(x => x.status == EnumStatus.EFormFinalReview).FirstOrDefault().data.Servicesheet.OrderByDescending(x => x.serviceEnd).ToList();

        result.Where(x => x.status == EnumStatus.EFormClosed).FirstOrDefault().data.Servicesheet = result.Where(x => x.status == EnumStatus.EFormClosed).FirstOrDefault().data.Servicesheet.OrderByDescending(x => x.serviceEnd).ToList();
        #endregion

        #region Get monitoring status interim engine service
        List<string> fieldsParamInterim = new List<string>() { EnumQuery.SSWorkorder, EnumQuery.ID, EnumQuery.Key, EnumQuery.DefectStatus, EnumQuery.Status, EnumQuery.IsDownload, EnumQuery.ServiceStart, EnumQuery.ServiceEnd };
        JArray dailyWorkOrderListInterim = JArray.FromObject(dailySchedules.Select(x => x.workOrder));

        Dictionary<string, object> paramInterim = new Dictionary<string, object>();
        paramInterim.Add(EnumQuery.IsActive, "true");
        paramInterim.Add(EnumQuery.IsDeleted, "false");
        paramInterim.Add(EnumQuery.SSWorkorder, dailyWorkOrderListInterim);
        paramInterim.Add(EnumQuery.Fields, fieldsParamInterim);
        if (siteId != null) paramInterim.Add(EnumQuery.siteId, siteId);
        var interimEngine = await _interimEngineHeaderRepository.GetDataListByParamJArray(paramInterim);

        List<ServiceHeaderMonitoringStatusResponse> interimObj = JsonConvert.DeserializeObject<List<ServiceHeaderMonitoringStatusResponse>>(JsonConvert.SerializeObject(interimEngine));

        DateTime serviceStartInterim;
        DateTime serviceEndInterim;
        var objDailyInterim = (from dailyData in dailySchedulesInterim
                               join interimData in interimObj on dailyData.workOrder equals interimData.workOrder
                               select new DailyScheduleModel
                                {
                                    dailyScheduleId = dailyData.dailyScheduleId,
                                    unitNumber = dailyData.unitNumber,
                                    equipmentModel = dailyData.equipmentModel,
                                    brand = dailyData.brand,
                                    smuDue = dailyData.smuDue,
                                    workOrder = dailyData.workOrder,
                                    psType = dailyData.psType,
                                    dateService = dailyData.dateService,
                                    shift = dailyData.shift,
                                    isActive = dailyData.isActive,
                                    startDate = dailyData.startDate,
                                    endDate = dailyData.endDate,
                                    createdOn = dailyData.createdOn,
                                    createdBy = dailyData.createdBy,
                                    changedBy = dailyData.changedBy,
                                    changedOn = dailyData.changedOn,
                                    eFormId = dailyData.eFormId,
                                    eFormKey = dailyData.eFormKey,
                                    status = interimData.status, //
                                    defectStatus = interimData.defectStatus, //
                                    isDownload = interimData.isDownload, //
                                    eFormStatus = dailyData.eFormStatus,
                                    form = dailyData.form,
                                    serviceStart = interimData.serviceStart.ToString() == "" ? "" : FormatHelper.ConvertToDateTime24(interimData.serviceStart.ToString()).ToString("yyyy-MM-dd'T'HH:mm:ss"),
                                    serviceEnd = interimData.serviceEnd.ToString() == "" ? dailyData.changedOn : FormatHelper.ConvertToDateTime24(interimData.serviceEnd.ToString()).ToString("yyyy-MM-dd'T'HH:mm:ss")
                                   //serviceStart = DateTime.TryParseExact(interimData.serviceStart.ToString(), "dd/MM/yy HH:mm:ss (AEST)", CultureInfo.InvariantCulture, DateTimeStyles.None, out serviceStartInterim) == true ? serviceStartInterim.ToString("yyyy-MM-dd'T'HH:mm:ss") : "",
                                   //serviceEnd = DateTime.TryParseExact(interimData.serviceEnd.ToString(), "dd/MM/yy HH:mm:ss (AEST)", CultureInfo.InvariantCulture, DateTimeStyles.None, out serviceEndInterim) == true ? serviceEndInterim.ToString("yyyy-MM-dd'T'HH:mm:ss") : dailyData.changedOn
                               })
                               .Where(x =>
                                 (x.status == EnumStatus.IEngineOpen ?
                                   (
                                       !servicsSheetObj.Select(o => o.workOrder).Contains(x.workOrder) &&
                                       DateTime.Parse(x.dateService.ToString()) >= startDate &&
                                       DateTime.Parse(x.dateService.ToString()) <= endDate
                                   ) :
                                   (x.status == EnumStatus.IEngineOnProgress ?
                                       (!string.IsNullOrEmpty(x.serviceStart) && DateTime.Parse(x.serviceStart) >= startDate && DateTime.Parse(x.serviceStart) <= endDate) :
                                       (!string.IsNullOrEmpty(x.serviceEnd) && DateTime.Parse(x.serviceEnd) >= startDate && DateTime.Parse(x.serviceEnd) <= endDate)
                                   )
                                 )
                                ).ToList();

        result.Where(x => x.status == EnumStatus.IEngineOnProgress).FirstOrDefault()
              .data.InterimEngine.AddRange(objDailyInterim.Where(x => x.status == EnumStatus.IEngineOnProgress).ToList());
        result.Where(x => x.status == EnumStatus.IEngineSubmited).FirstOrDefault()
              .data.InterimEngine.AddRange(objDailyInterim.Where(x => x.status == EnumStatus.IEngineSubmited).ToList());
        result.Where(x => x.status == EnumStatus.EFormFinalReview).FirstOrDefault()
              .data.InterimEngine.AddRange(objDailyInterim.Where(x => x.status == EnumStatus.EFormClosed &&
                                                                      x.defectStatus == EnumStatus.DefectApprovedSPV).ToList());
        result.Where(x => x.status == EnumStatus.EFormClosed).FirstOrDefault()
              .data.InterimEngine.AddRange(objDailyInterim.Where(x => x.status == EnumStatus.EFormClosed &&
                                                                      x.defectStatus != EnumStatus.DefectApprovedSPV).ToList());

        // Status Yet To Start
        var objInterimYeToStart = dailySchedulesInterim.Where(x => !interimObj.Select(o => o.workOrder).Contains(x.workOrder) &&
                                                          DateTime.Parse(x.dateService.ToString()) >= startDate &&
                                                          DateTime.Parse(x.dateService.ToString()) <= endDate).ToList();

        result.Where(x => x.status == EnumStatus.IEngineOpen).FirstOrDefault().data.InterimEngine.AddRange(objInterimYeToStart);

        // sorting yet to start by serviceDate
        //result.Where(x => x.status == EnumStatus.IEngineOpen).FirstOrDefault().data.Servicesheet = result.Where(x => x.status == EnumStatus.EFormOpen).FirstOrDefault().data.Servicesheet.OrderBy(x => x.dateService).ToList();
        // sorting in progress by serviceStart
        result.Where(x => x.status == EnumStatus.IEngineOnProgress).FirstOrDefault().data.InterimEngine = result.Where(x => x.status == EnumStatus.IEngineOnProgress).FirstOrDefault().data.InterimEngine.OrderByDescending(x => x.serviceStart).ToList();
        // sorting under review by service actual finish
        result.Where(x => x.status == EnumStatus.IEngineSubmited).FirstOrDefault().data.InterimEngine = result.Where(x => x.status == EnumStatus.IEngineSubmited).FirstOrDefault().data.InterimEngine.OrderByDescending(x => x.serviceEnd).ToList();
        // sorting final review by service actual finish
        result.Where(x => x.status == EnumStatus.EFormFinalReview).FirstOrDefault().data.InterimEngine = result.Where(x => x.status == EnumStatus.EFormFinalReview).FirstOrDefault().data.InterimEngine.OrderByDescending(x => x.serviceEnd).ToList();

        result.Where(x => x.status == EnumStatus.EFormClosed).FirstOrDefault().data.InterimEngine = result.Where(x => x.status == EnumStatus.EFormClosed).FirstOrDefault().data.InterimEngine.OrderByDescending(x => x.serviceEnd).ToList();
        #endregion

        foreach (var item in result)
            item.dataCount = item.data.Intervention.Count + item.data.Servicesheet.Count;

        return result;
    }
}