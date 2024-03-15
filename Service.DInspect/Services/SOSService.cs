using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPoco;
using Service.DInspect.Helpers;
using Service.DInspect.Interfaces;
using Service.DInspect.Models;
using Service.DInspect.Models.Entity;
using Service.DInspect.Models.Enum;
using Service.DInspect.Models.Request;
using Service.DInspect.Models.Response;
using Service.DInspect.Repositories;
using Service.DInspect.Repositories;
using Service.DInspect.Services.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Service.DInspect.Services
{
    public class SOSService : ServiceBase
    {
        protected string _container;
        protected IConnectionFactory _connectionFactory;
        protected IRepositoryBase _sosHistory,
            _serviceSheetRepository,
            _serviceSheetDetailRepository,
            _lubricantMappingRepository,
            _settingRepository,
            _serviceSuckAndBlowRepository,
            _serviceSuckAndBlowDetailRepository,
            _serviceInterventionRepository;
        protected QRCodeHelper _qrCodeHelper;

        public SOSService(MySetting appSetting, IConnectionFactory connectionFactory, string container, string accessToken) : base(appSetting, connectionFactory, container, accessToken)
        {
            _container = container;
            _connectionFactory = connectionFactory;
            _repository = new SOSRepository(connectionFactory, container);
            _serviceSheetRepository = new ServiceSheetHeaderRepository(connectionFactory, EnumContainer.ServiceSheetHeader);
            _serviceSheetDetailRepository = new ServiceSheetDetailRepository(connectionFactory, EnumContainer.ServiceSheetDetail);
            _lubricantMappingRepository = new LubricantMappingRepository(connectionFactory, EnumContainer.LubricantMapping);
            _settingRepository = new MasterSettingRepository(connectionFactory, EnumContainer.MasterSetting);
            _serviceSuckAndBlowRepository = new SuckAndBlowHeaderRepository(connectionFactory, EnumContainer.InterimEngineHeader);
            _serviceSuckAndBlowDetailRepository = new SuckAndBlowHeaderRepository(connectionFactory, EnumContainer.InterimEngineDetail);
            _serviceInterventionRepository = new InterventionRepository(connectionFactory, EnumContainer.Intervention);
            _qrCodeHelper = new QRCodeHelper();
        }

        public async Task<ServiceResult> getSOSHistoryAsync(string siteId = null)
        {
            try
            {

                Dictionary<string, object> settingParam = new Dictionary<string, object>();
                settingParam.Add(EnumQuery.Key, EnumQuery.SosPrintLabelLessDay);
                var setting = await _settingRepository.GetDataByParam(settingParam);
                var maxDay = setting[EnumQuery.Value];
                DateTime curentDate = EnumCommonProperty.CurrentDateTime.AddDays(Convert.ToInt32(maxDay));
                #region Get status montoring servicesheet
                //CallAPIHelper callAPIHelper = new CallAPIHelper(_accessToken);
                //var dailySchedule = await callAPIHelper.Get(EnumUrl.GetAdmHistoryUrl + $"&siteId={siteId}");
                //List<DailyScheduleModel> dailySchedules = JsonConvert.DeserializeObject<List<DailyScheduleModel>>(JsonConvert.SerializeObject(dailySchedule.Result.Content));

                //var serviceSheets = await _serviceSheetRepository.GetActiveDataJArray();

                //#region get day setting print sos history
                //Dictionary<string, object> settingParam = new Dictionary<string, object>();
                //settingParam.Add(EnumQuery.Key, EnumQuery.SosPrintLabelLessDay);

                //var setting = await _settingRepository.GetDataByParam(settingParam);
                //var maxDay = setting[EnumQuery.Value];
                //DateTime curentDate = EnumCommonProperty.CurrentDateTime.AddDays(Convert.ToInt32(maxDay));
                //#endregion
                //DateTime serviceStart;
                //foreach (var serviceSheet in dailySchedules)
                //{
                //    var header = serviceSheets.FilterEqual(EnumQuery.SSWorkorder, serviceSheet.workOrder).FirstOrDefault();

                //    if (header != null)
                //    {
                //        JToken jHeader = JToken.FromObject(header);
                //        serviceSheet.status = jHeader[EnumQuery.Status].ToString();
                //        serviceSheet.eFormId = jHeader[EnumQuery.ID].ToString();
                //        serviceSheet.defectStatus = jHeader[EnumQuery.DefectStatus].ToString();
                //        serviceSheet.isDownload = jHeader[EnumQuery.IsDownload] != null ? jHeader[EnumQuery.IsDownload].ToString() : "false";
                //        if (DateTime.TryParseExact(jHeader[EnumQuery.ServiceStart].ToString(), "dd/MM/yy HH:mm:ss (AEST)", CultureInfo.InvariantCulture, DateTimeStyles.None, out serviceStart))
                //        {
                //            // Parsing was successful
                //            serviceSheet.serviceStart = serviceStart.ToString("yyyy-MM-dd'T'HH:mm:ss");
                //        }
                //        serviceSheet.serviceEnd = jHeader[EnumQuery.ServiceEnd].ToString();


                //        if (serviceSheet.status != EnumStatus.EFormOpen && (serviceStart >= curentDate))
                //            result.Add(serviceSheet);
                //    }

                //}
                #endregion

                var param = new Dictionary<string, object>()
                {
                    { EnumQuery.siteId, siteId}
                };
                var rsc = await _repository.GetDataListByParam(param);
                IList<PrintSosHistoryResponse> SoshistoryResponse = JsonConvert.DeserializeObject<List<PrintSosHistoryResponse>>(JsonConvert.SerializeObject(rsc));

                SoshistoryResponse = SoshistoryResponse.Where(x => DateTime.ParseExact(x.createdDate.ToString(), EnumFormatting.AestDateTimeFormat, CultureInfo.InvariantCulture) > curentDate).ToList();

                return new ServiceResult
                {
                    Message = string.Format("Get Service Sheet Close Successfully {0} rows", SoshistoryResponse.Count),
                    IsError = false,
                    Content = SoshistoryResponse
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

        public async Task<ServiceResult> UpdateTask(UpdateTaskRequest updateTaskRequest, string eform)
        {
            #region get id soshitory
            Dictionary<string, object> param = new Dictionary<string, object>
            {
                { EnumQuery.EformType, eform}

            };

            if (eform == EnumEformType.EformIntervention)
                param.Add(EnumQuery.HeaderId, updateTaskRequest.id);
            else
                param.Add(EnumQuery.SSWorkorder, updateTaskRequest.workorder);


            var rsc = await _repository.GetDataByParam(param);
            if (rsc != null)
            {

                var id = StaticHelper.GetPropValue(rsc, EnumQuery.ID);
                var headerId = StaticHelper.GetPropValue(rsc, EnumQuery.HeaderId);

                updateTaskRequest.id = id.ToString();
                updateTaskRequest.headerId = headerId.ToString();

                #endregion
                UpdateRequest updateRequest = new UpdateRequest()
                {
                    id = updateTaskRequest.id,
                    workOrder = updateTaskRequest.workorder,
                    updateParams = updateTaskRequest.updateParams,
                    employee = updateTaskRequest.employee
                };
                var result = await _repository.Update(updateRequest, rsc);
            }
            return new ServiceResult
            {
                Message = "Data updated successfully",
                IsError = false,
                Content = null
            };
        }

        public async Task<ServiceResult> GetPrintSosHistory(PrintSosHistoryRequest param)
        {

            try
            {
                var filterParam = new Dictionary<string, object>()
                {
                    { EnumQuery.SSWorkorder, param.workOrder},
                    { EnumQuery.EformType, param.eformType}
                };
                dynamic rsc = await _repository.GetDataByParam(filterParam);


                JObject JobjSosHistory = JObject.Parse(JsonConvert.SerializeObject(rsc));
                JToken jToken = JToken.Parse(JsonConvert.SerializeObject(rsc));

                #region get data latest sos hitory meter hrs
                var paramSosLates = new Dictionary<string, object>()
                {
                    { EnumQuery.Equipment,JobjSosHistory[EnumQuery.Equipment] },
                    { EnumQuery.HeaderId,JobjSosHistory[EnumQuery.HeaderId] }
                };
                var latesSos = await ((SOSRepository)_repository).GetDataSosLatest(paramSosLates);
                #endregion
                #region fetch data
                List<dynamic> ListDetail = new List<dynamic>();
                foreach (var item in param.oilSampleKey)
                {
                    dynamic responseSubgroup = jToken.SelectTokens($"$...[?(@.{EnumQuery.Key} == '{item}')]")?
                    .Select(x => JobjSosHistory.SelectToken(x.Parent.Parent.Parent.Path).Value<dynamic>())
                    .FirstOrDefault();

                    dynamic responseTask = jToken.SelectTokens($"$...[?(@.{EnumQuery.Key} == '{item}')]")?
                    .Select(x => JobjSosHistory.SelectToken(x.Parent.Parent.Path).Value<dynamic>())
                    .FirstOrDefault();

                    string keyCompartment = StaticHelper.GetPropValue(responseSubgroup, EnumQuery.Key);
                    JArray topLatestByKey = latesSos.FilterEqual("keyCompartment", keyCompartment);

                    var levelChecktaskValue = string.Empty;
                    var TopUpvalue = string.Empty;
                    var sampleDate = string.Empty;
                    bool isChanged = false;
                    bool isAdded = false;
                    long LatestMeterHrs = 0;
                    long hrsOilresult = 0;
                    var qrString = _qrCodeHelper.GenerateStringQRCode(Convert.ToString(responseSubgroup[EnumQuery.CompartmentLubricant]));
                    foreach (var items in responseTask)
                    {
                        if (items[EnumQuery.Name] == EnumQuery.LubeServiceChange)
                        {
                            sampleDate = items[EnumQuery.UpdatedDate];
                            isChanged = items[EnumQuery.TaskValue] == EnumTaskValue.NormalOK ? true : false;
                            string cekUpdateDate = items[EnumQuery.UpdatedDate];
                            if (latesSos.Count > 0)
                            {
                                DateTime ParsesampleDate;
                                var sampledateToString = string.Empty;
                                JArray FilterDate = new JArray();
                                if (DateTime.TryParseExact(items[EnumQuery.UpdatedDate].ToString(), "dd/MM/yy HH:mm:ss (AEST)", CultureInfo.InvariantCulture, DateTimeStyles.None, out ParsesampleDate))
                                {
                                    sampledateToString = ParsesampleDate.ToString("yyyy-MM-dd'T'HH:mm:ss");
                                    FilterDate = topLatestByKey.FilterLessThan(EnumQuery.UpdatedDate, sampledateToString);
                                }
                                else
                                    sampledateToString = "";



                                if (FilterDate.Count > 0)
                                {
                                    var topLatestRes = FilterDate.FirstOrDefault();
                                    LatestMeterHrs = long.Parse(StaticHelper.GetPropValue(topLatestRes, EnumQuery.MeterHrs).ToString());
                                }
                            }
                            hrsOilresult = long.Parse(JobjSosHistory[EnumQuery.MeterHrs].ToString()) - LatestMeterHrs;
                        }
                        if (items[EnumQuery.Name] == EnumQuery.LubeServiceLevelCheck)
                            levelChecktaskValue = items[EnumQuery.TaskValue];

                        if (items[EnumQuery.Name] == EnumQuery.LubeServiceLevelTopUp)
                            TopUpvalue = items[EnumQuery.Value];
                    }
                    isAdded = !string.IsNullOrEmpty(TopUpvalue) && long.Parse(TopUpvalue) > 0 ? true : false;
                    var detail = new TaskSOSResponse()
                    {
                        key = item,
                        compartmentLubricant = responseSubgroup[EnumQuery.CompartmentLubricant],
                        recommendedLubricants = responseSubgroup[EnumQuery.RecomendedLubricant],
                        sampleDate = sampleDate,
                        volume = responseSubgroup[EnumQuery.Volume],
                        uoM = responseSubgroup[EnumQuery.Uom],
                        lubricantType = responseSubgroup[EnumQuery.LubricantType],
                        taskChange = isChanged.ToString(),
                        taskAdded = isAdded.ToString(),
                        hrsOnOil = hrsOilresult.ToString(),
                        lastMeterHrs = LatestMeterHrs.ToString(),
                        qrString = qrString
                    };
                    ListDetail.Add(detail);
                }

                dynamic result = new PrintSosHistoryResponse()
                {
                    id = JobjSosHistory[EnumQuery.ID].ToString(),
                    key = JobjSosHistory[EnumQuery.Key].ToString(),
                    headerId = JobjSosHistory[EnumQuery.HeaderId].ToString(),
                    workOrder = JobjSosHistory[EnumQuery.SSWorkorder].ToString(),
                    eformType = JobjSosHistory[EnumQuery.EformType].ToString(),
                    modelId = (string)JobjSosHistory[EnumQuery.ModelId],
                    equipment = (string)JobjSosHistory[EnumQuery.Equipment],
                    psTypeId = (string)JobjSosHistory[EnumQuery.PsTypeId],
                    brand = JobjSosHistory[EnumQuery.Brand].ToString(),
                    meterHrs = (string)JobjSosHistory[EnumQuery.MeterHrs],
                    fuelType = (string)JobjSosHistory[EnumQuery.FuelType],
                    equipmentSerialNumber = (string)JobjSosHistory[EnumQuery.EquipmentSerialNumber],
                    equipmentModel = (string)JobjSosHistory[EnumQuery.EquipmentModel],
                    customerName = (string)JobjSosHistory[EnumQuery.CustomerName],
                    jobSite = (string)JobjSosHistory[EnumQuery.JobSite],
                    brandDescription = (string)JobjSosHistory[EnumQuery.BrandDescription],
                    createdDate = (string)JobjSosHistory[EnumQuery.CreatedDate],
                    details = ListDetail
                };
                #endregion

                return new ServiceResult()
                {
                    IsError = false,
                    Content = result
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult()
                {
                    IsError = true,
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceResult> getSOSCompartmentAsync(Dictionary<string, object> param)
        {
            try
            {

                #region get compartment for servicesheet
                //param.Add( EnumQuery.Key, EnumGroup.LubeService );
                //dynamic rsc = await _serviceSheetDetailRepository.GetDataByParam(param);

                //string json = JsonConvert.SerializeObject(rsc);
                //JObject jObj = JObject.Parse(json);
                //JToken token = JToken.Parse(json);

                //var servicesheetDetailid = jObj[$"{EnumQuery.ID}"];
                //var servicesheetModelId = jObj[$"{EnumQuery.ModelId}"].ToString();
                //var servicesheetPsTypeId =   jObj[$"{EnumQuery.PsTypeId}"].ToString();

                //var compartment = token.SelectToken($"$...[?(@.{EnumQuery.Key} == '{EnumQuery.LubeServiceSample}')]['{EnumQuery.Task}']").ToList();

                //// get lubricant mappings
                //var lubricantParams = new Dictionary<string, object>
                //{
                //    {EnumQuery.ModelId, servicesheetModelId },
                //    {EnumQuery.PsTypeId, servicesheetPsTypeId }
                //};

                //var lubricantData = await _lubricantMappingRepository.GetDataByParam(lubricantParams);

                //JObject lubricants = null;
                //if (lubricantData != null)
                //    lubricants = JObject.Parse(JsonConvert.SerializeObject(lubricantData));

                //List <dynamic> result = new List<dynamic>();

                //foreach (var item in compartment)
                //{
                //    string description = item[EnumQuery.Description].ToString();
                //    if (description != "")
                //    {
                //        string errorMsg = "";

                //        if (lubricants != null)
                //        {
                //            JToken itemLubricant = ((JArray)lubricants[$"{EnumQuery.Detail}"]).FilterEqual("taskKeyOilSample", item[EnumQuery.Key].ToString()).FirstOrDefault();
                //            if (itemLubricant["isSOS"].ToString() == "false")
                //                errorMsg = "No data sample for this compartment";
                //            else
                //                if (item[EnumQuery.TaskValue].ToString() != "1")
                //                errorMsg = "No sampling was carried out";
                //        }
                //        else
                //        {
                //            errorMsg = "No data sample for this compartment";
                //        }
                //        var items = new Dictionary<string, dynamic>()
                //        {
                //            {EnumQuery.Key, item[EnumQuery.Key]},
                //            {EnumQuery.Description, item[EnumQuery.Description]},
                //            {EnumCommonProperty.ErrorMsg, errorMsg},
                //        };
                //        result.Add(items);
                //    }
                //}

                #endregion

                var rsc = await _repository.GetDataByParam(param);
                var rscTask = StaticHelper.GetData(rsc, EnumQuery.Task);

                List<dynamic> result = new List<dynamic>();
                foreach (var item in rscTask)
                {
                    var fetchData = new Dictionary<string, dynamic>();
                    JArray JsonArrayOilSmple = item;
                    var Jproperty = JsonArrayOilSmple.FirstOrDefault(x => x.Value<string>(EnumQuery.Key) != "");
                    if (Jproperty != null)
                    {
                        var key = StaticHelper.GetPropValue(Jproperty, EnumQuery.Key);
                        var oilSampleParent = StaticHelper.GetParentAdjIndexData(rsc, EnumQuery.Key, key.ToString(), 3);

                        var keyLubricant = StaticHelper.GetPropValue(oilSampleParent, EnumQuery.Key);
                        var compartmentLubricant = StaticHelper.GetPropValue(oilSampleParent, EnumQuery.CompartmentLubricant);
                        var keyOilSample = StaticHelper.GetPropValue(Jproperty, EnumQuery.Key);
                        var keyValue = StaticHelper.GetPropValue(Jproperty, EnumQuery.TaskValue);
                        var errMsg = string.Empty;

                        if ((keyValue.ToString() != EnumTaskValue.NormalOK || keyValue.ToString() != EnumTaskValue.IntNormalCompleted && param[EnumQuery.EformType].ToString() == EnumEformType.EformIntervention) && !string.IsNullOrEmpty(keyValue.ToString()))
                            errMsg = EnumErrorMessage.ErrMsgCompartmentOilSample;
                        else if (string.IsNullOrEmpty(keyValue.ToString()))
                            errMsg = EnumErrorMessage.ErrMsgCompartmentOilSampleIsnull;

                        var isError = !string.IsNullOrEmpty(errMsg) ? true.ToString() : false.ToString();

                        fetchData.Add(EnumQuery.Key, keyLubricant);
                        fetchData.Add(EnumQuery.CompartmentLubricant, compartmentLubricant);
                        fetchData.Add(EnumQuery.TaskKeyOilSample, keyOilSample);
                        fetchData.Add(EnumQuery.TaskValue, keyValue);
                        fetchData.Add(EnumQuery.IsError, isError);
                        fetchData.Add(EnumCommonProperty.ErrorMsg, errMsg);

                        result.Add(fetchData);

                    }
                }

                return new ServiceResult
                {
                    Message = "Get data successfully",
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

        public async Task<ServiceResult> GenerateSosHistory(Dictionary<string, object> param)
        {
            try
            {
                var filterParam = new Dictionary<string, object>();

                if (param[EnumQuery.EformType].ToString() == EnumEformType.EformIntervention)
                {
                    filterParam = new Dictionary<string, object>()
                    {
                        { EnumQuery.KeyPbi, param[EnumQuery.KeyPbi]},
                        { EnumQuery.EformType, param[EnumQuery.EformType]}
                    };
                }
                else if (param[EnumQuery.EformType].ToString() == EnumEformType.SuckAndBlow || param[EnumQuery.EformType].ToString() == EnumEformType.EformServiceSheet)
                {

                    filterParam = new Dictionary<string, object>()
                    {
                        { EnumQuery.SSWorkorder, param[EnumQuery.SSWorkorder]},
                        { EnumQuery.EformType, param[EnumQuery.EformType]}
                    };
                }

                dynamic data = await _repository.GetDataByParam(filterParam);

                if (data == null && filterParam.Count > 0)
                {
                    #region get master sos ADM 
                    CallAPIService callAPI = new CallAPIService(_appSetting, _accessToken);
                    dynamic masterDataSos = await callAPI.AdmGetMasterSos(param[EnumQuery.Equipment].ToString());
                    #endregion

                    SosHistoryModel sosHistoryModels = new SosHistoryModel();

                    if (param[EnumQuery.EformType].ToString() == EnumEformType.EformServiceSheet)
                        sosHistoryModels = await GenerateFromServiceSheet(filterParam, masterDataSos);
                    else if (param[EnumQuery.EformType].ToString() == EnumEformType.SuckAndBlow)
                        sosHistoryModels = await GenerateFromIntherimEngine(filterParam, masterDataSos);
                    else if (param[EnumQuery.EformType].ToString() == EnumEformType.EformIntervention)
                        sosHistoryModels = await GenerateFromInterventionForm(param, masterDataSos);
                    else sosHistoryModels = null;


                    //if (sosHistoryModels != null || !string.IsNullOrEmpty(sosHistoryModels.key))
                    //{
                    //    //var result = await _repository.Create(new CreateRequest()
                    //    //{
                    //    //    employee = new EmployeeModel() { id = EnumCaption.System, name = EnumCaption.System },
                    //    //    entity = sosHistoryModels
                    //    //});
                    //}
                }

                return new ServiceResult()
                {
                    Message = string.Format("Create data is successfully"),
                    IsError = false
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult()
                {
                    IsError = true,
                    Message = ex.Message
                };
            }
        }

        private async Task<SosHistoryModel> GenerateFromInterventionForm(Dictionary<string, object> param, dynamic masterDataSos)
        {
            SosHistoryModel sosHistoryModels = new SosHistoryModel();

            #region get data Intervention header
            Dictionary<string, object> SSparam = new Dictionary<string, object>();
            SSparam.Add(EnumQuery.IsDeleted, false.ToString().ToLower());
            SSparam.Add(EnumQuery.KeyPbi, param[EnumQuery.KeyPbi].ToString());
            //SSparam.Add(EnumQuery.SSWorkorder, param[EnumQuery.SSWorkorder].ToString());

            var InterventionData = await _serviceInterventionRepository.GetDataListByParamJArray(SSparam);

            JObject JInterventionData = JObject.Parse(JsonConvert.SerializeObject(InterventionData.FirstOrDefault()));

            #endregion

            #region get param lubricant mapping

            Dictionary<string, object> paramLubricant = new Dictionary<string, object>();
            paramLubricant.Add(EnumQuery.ModelId, JInterventionData[EnumQuery.EquipmentDesc]);
            paramLubricant.Add(EnumQuery.EformType, param[EnumQuery.EformType]);

            var rscLubricantMapping = await _lubricantMappingRepository.GetDataByParam(paramLubricant);
            #endregion

            if (rscLubricantMapping != null)
            {

                var json = JsonConvert.SerializeObject(rscLubricantMapping);
                JObject jObj = JObject.Parse(json);
                JToken token = JToken.Parse(json);

                var resLubricantMapping = token.SelectToken($"$..['{EnumQuery.Detail}']").ToList();

                #region get suck and blow detail
                Dictionary<string, object> paramSuckAndBlowDetail = new Dictionary<string, object>();
                paramSuckAndBlowDetail.Add(EnumQuery.SSWorkorder, param[EnumQuery.SSWorkorder]);
                paramSuckAndBlowDetail.Add(EnumQuery.GroupName, EnumGroup.SuckAndBlow);

                //var servicesheetDetail = await _serviceSuckAndBlowDetailRepository.GetDataByParam(paramSuckAndBlowDetail);

                string jsonInterventionData = JsonConvert.SerializeObject(InterventionData.FirstOrDefault());
                JObject JobjInterventionData = JObject.Parse(jsonInterventionData);
                JToken tokenInterventionData = JToken.Parse(jsonInterventionData);

                #endregion

                #region fetch data

                List<SosHistoryLubricantMappingModel> resSubgroup = new List<SosHistoryLubricantMappingModel>();
                foreach (var item in resLubricantMapping)
                {
                    var taskValue = string.Empty;
                    var sampleDate = string.Empty;
                    var value = string.Empty;
                    bool isTopup = false;

                    #region get value of compartment
                    var taskValueOilSample = tokenInterventionData.SelectTokens($"$.....[?(@.{EnumQuery.Key}=='{item[EnumQuery.TaskKeyOilSample]}')]['{EnumQuery.TaskValue}','{EnumQuery.UpdatedDate}']")
                    .Select(v => (JProperty)v.Parent) // Get parent JProperty (which encapsulates name as well as value)
                                .GroupBy(p => p.Parent)           // Group by parent JObject
                                .Select(g => new JObject(g)).ToList();

                    var taskValueOilChange = tokenInterventionData.SelectTokens($"$.....[?(@.{EnumQuery.Key}=='{item[EnumQuery.TaskKeyOilChange]}')]['{EnumQuery.TaskValue}','{EnumQuery.UpdatedDate}']")
                    .Select(v => (JProperty)v.Parent) // Get parent JProperty (which encapsulates name as well as value)
                                .GroupBy(p => p.Parent)           // Group by parent JObject
                                .Select(g => new JObject(g)).ToList();

                    var taskValueOilLevelCheck = tokenInterventionData.SelectTokens($"$.....[?(@.{EnumQuery.Key}=='{item[EnumQuery.TaskKeyOilLevelCheck]}')]['{EnumQuery.TaskValue}','{EnumQuery.UpdatedDate}']")
                        .Select(v => (JProperty)v.Parent) // Get parent JProperty (which encapsulates name as well as value)
                                    .GroupBy(p => p.Parent)           // Group by parent JObject
                                    .Select(g => new JObject(g)).ToList();
                    #endregion

                    if (taskValueOilSample.Count >= 1)
                    {
                        dynamic jsontaskValueOilSample = JsonConvert.SerializeObject(taskValueOilSample.FirstOrDefault());
                        JObject JtaskValueOilSample = JObject.Parse(jsontaskValueOilSample);
                        taskValue = JtaskValueOilSample[EnumQuery.TaskValue].ToString();
                        sampleDate = JtaskValueOilSample[EnumQuery.UpdatedDate].ToString();
                    }
                    var oilSample = new TaskMapping()
                    {
                        name = EnumQuery.LubeServiceSample,
                        key = item[EnumQuery.TaskKeyOilSample]?.ToString(),
                        taskValue = taskValue,
                        updatedDate = sampleDate,
                        value = ""
                    };

                    taskValue = string.Empty; sampleDate = string.Empty;
                    if (taskValueOilChange.Count >= 1)
                    {
                        dynamic jsontaskValue = JsonConvert.SerializeObject(taskValueOilChange.FirstOrDefault());
                        JObject JtaskValue = JObject.Parse(jsontaskValue);
                        taskValue = JtaskValue[EnumQuery.TaskValue].ToString();
                        sampleDate = JtaskValue[EnumQuery.UpdatedDate].ToString();
                    }

                    var oilChange = new TaskMapping()
                    {
                        name = EnumQuery.LubeServiceChange,
                        key = item[EnumQuery.TaskKeyOilChange]?.ToString(),
                        taskValue = taskValue,
                        updatedDate = sampleDate,
                        value = ""
                    };

                    taskValue = string.Empty; sampleDate = string.Empty;
                    if (taskValueOilLevelCheck.Count >= 1)
                    {
                        dynamic jsontaskValue = JsonConvert.SerializeObject(taskValueOilLevelCheck.FirstOrDefault());
                        JObject JtaskValue = JObject.Parse(jsontaskValue);
                        taskValue = JtaskValue[EnumQuery.TaskValue].ToString();
                        sampleDate = JtaskValue[EnumQuery.UpdatedDate].ToString();
                    }
                    var oilLevelCheck = new TaskMapping()
                    {
                        name = EnumQuery.LubeServiceLevelCheck,
                        key = item[EnumQuery.TaskKeyOilLevelCheck]?.ToString(),
                        taskValue = taskValue,
                        updatedDate = sampleDate,
                        value = ""
                    };

                    var listtask = new List<TaskMapping>();
                    listtask.Add(oilSample);
                    listtask.Add(oilChange);
                    listtask.Add(oilLevelCheck);
                    //listtask.Add(topUpLevelCheck);

                    var ts = new SosHistoryLubricantMappingModel()
                    {
                        key = item[EnumQuery.Key].ToString(),
                        compartmentLubricant = item[EnumQuery.CompartmentLubricant]?.ToString(),
                        recommendedLubricants = item[EnumQuery.RecomendedLubricant]?.ToString(),
                        volume = item[EnumQuery.Volume]?.ToString(),
                        uoM = item[EnumQuery.Uom]?.ToString(),
                        lubricantType = item[EnumQuery.LubricantType]?.ToString(),
                        isSOS = item[EnumQuery.IsSOS]?.ToString(),
                        task = listtask.Where(x => !string.IsNullOrEmpty(x.key)).ToList()
                    };

                    resSubgroup.Add(ts);
                }
                #endregion

                long EHM = long.Parse(JInterventionData[EnumQuery.Smu].ToString());
                long EhmOffset = StaticHelper.GetPropValue(masterDataSos, EnumQuery.EhmOffset);
                sosHistoryModels = new SosIntervention()
                {
                    keyPbi = JInterventionData[EnumQuery.KeyPbi].ToString(),
                    key = JInterventionData[EnumQuery.Key].ToString(),
                    headerId = JInterventionData[EnumQuery.ID].ToString(),
                    siteId = (string)StaticHelper.GetPropValue(masterDataSos, EnumQuery.siteId),
                    workOrder = JInterventionData[EnumQuery.SapWorkOrder].ToString(),
                    modelId = JInterventionData[EnumQuery.EquipmentModel].ToString(),
                    //psTypeId = JInterventionData[EnumQuery.PsTypeId]?.ToString(),
                    equipment = JInterventionData[EnumQuery.Equipment].ToString(),
                    meterHrs = (EHM + EhmOffset).ToString(),
                    brand = (string)StaticHelper.GetPropValue(masterDataSos, EnumQuery.Brand),
                    equipmentModel = (string)StaticHelper.GetPropValue(masterDataSos, EnumQuery.EquipmentModel),
                    equipmentSerialNumber = (string)StaticHelper.GetPropValue(masterDataSos, EnumQuery.SerialNumber),
                    customerName = (string)StaticHelper.GetPropValue(masterDataSos, EnumQuery.Ownership),
                    jobSite = (string)StaticHelper.GetPropValue(masterDataSos, EnumQuery.SiteDescription),
                    brandDescription = (string)StaticHelper.GetPropValue(masterDataSos, EnumQuery.BrandDescription),
                    fuelType = EnumTaskValue.FeulType,
                    eformType = param[EnumQuery.EformType].ToString()

                };

                sosHistoryModels.subGroup = resSubgroup;

                await _repository.Create(new CreateRequest()
                {
                    employee = new EmployeeModel() { id = EnumCaption.System, name = EnumCaption.System },
                    entity = sosHistoryModels
                });
            }

            return sosHistoryModels;
        }

        private async Task<SosHistoryModel> GenerateFromIntherimEngine(Dictionary<string, object> param, dynamic masterDataSos)
        {
            SosHistoryModel sosHistoryModels = new SosHistoryModel();
            #region get data Interim Engine header
            Dictionary<string, object> SSparam = new Dictionary<string, object>();
            SSparam.Add(EnumQuery.IsDeleted, false.ToString().ToLower());
            SSparam.Add(EnumQuery.GroupName, EnumGroup.General);
            SSparam.Add(EnumQuery.SSWorkorder, param[EnumQuery.SSWorkorder].ToString());

            var SuckAndBlowHeader = await _serviceSuckAndBlowRepository.GetDataListByParamJArray(SSparam);

            JObject JSuckAndBlowHeader = JObject.Parse(JsonConvert.SerializeObject(SuckAndBlowHeader.FirstOrDefault()));

            #endregion

            #region get param lubricant mapping

            Dictionary<string, object> paramLubricant = new Dictionary<string, object>();
            paramLubricant.Add(EnumQuery.ModelId, JSuckAndBlowHeader[EnumQuery.ModelId]);
            paramLubricant.Add(EnumQuery.EformType, param[EnumQuery.EformType]);

            var rscLubricantMapping = await _lubricantMappingRepository.GetDataByParam(paramLubricant);
            #endregion

            if (rscLubricantMapping != null)
            {


                var json = JsonConvert.SerializeObject(rscLubricantMapping);
                JObject jObj = JObject.Parse(json);
                JToken token = JToken.Parse(json);

                var resLubricantMapping = token.SelectToken($"$..['{EnumQuery.Detail}']").ToList();

                #region get suck and blow detail
                Dictionary<string, object> paramSuckAndBlowDetail = new Dictionary<string, object>();
                paramSuckAndBlowDetail.Add(EnumQuery.SSWorkorder, param[EnumQuery.SSWorkorder]);
                paramSuckAndBlowDetail.Add(EnumQuery.GroupName, EnumGroup.SuckAndBlow);

                var servicesheetDetail = await _serviceSuckAndBlowDetailRepository.GetDataByParam(paramSuckAndBlowDetail);

                string jsonSuckAndBlowDetail = JsonConvert.SerializeObject(servicesheetDetail);
                JObject jSuckAndBlowDetail = JObject.Parse(jsonSuckAndBlowDetail);
                JToken tokenSuckAndBlowDetail = JToken.Parse(jsonSuckAndBlowDetail);

                #endregion

                #region fetch data
                List<SosHistoryLubricantMappingModel> resSubgroup = new List<SosHistoryLubricantMappingModel>();
                foreach (var item in resLubricantMapping)
                {
                    var taskValue = string.Empty;
                    var sampleDate = string.Empty;
                    var value = string.Empty;
                    bool isTopup = false;

                    #region get value of compartment
                    var taskValueOilSample = tokenSuckAndBlowDetail.SelectTokens($"$...[?(@.{EnumQuery.Key}=='{item[EnumQuery.TaskKeyOilSample]}')]['{EnumQuery.TaskValue}','{EnumQuery.UpdatedDate}']")
                    .Select(v => (JProperty)v.Parent) // Get parent JProperty (which encapsulates name as well as value)
                                .GroupBy(p => p.Parent)           // Group by parent JObject
                                .Select(g => new JObject(g)).ToList();

                    var taskValueOilChange = tokenSuckAndBlowDetail.SelectTokens($"$...[?(@.{EnumQuery.Key}=='{item[EnumQuery.TaskKeyOilChange]}')]['{EnumQuery.TaskValue}','{EnumQuery.UpdatedDate}']")
                    .Select(v => (JProperty)v.Parent) // Get parent JProperty (which encapsulates name as well as value)
                                .GroupBy(p => p.Parent)           // Group by parent JObject
                                .Select(g => new JObject(g)).ToList();

                    var taskValueOilLevelCheck = tokenSuckAndBlowDetail.SelectTokens($"$...[?(@.{EnumQuery.Key}=='{item[EnumQuery.TaskKeyOilLevelCheck]}')]['{EnumQuery.TaskValue}','{EnumQuery.UpdatedDate}']")
                        .Select(v => (JProperty)v.Parent) // Get parent JProperty (which encapsulates name as well as value)
                                    .GroupBy(p => p.Parent)           // Group by parent JObject
                                    .Select(g => new JObject(g)).ToList();
                    #endregion

                    if (taskValueOilSample.Count >= 1)
                    {
                        dynamic jsontaskValueOilSample = JsonConvert.SerializeObject(taskValueOilSample.FirstOrDefault());
                        JObject JtaskValueOilSample = JObject.Parse(jsontaskValueOilSample);
                        taskValue = JtaskValueOilSample[EnumQuery.TaskValue].ToString();
                        sampleDate = JtaskValueOilSample[EnumQuery.UpdatedDate].ToString();
                    }
                    var oilSample = new TaskMapping()
                    {
                        name = EnumQuery.LubeServiceSample,
                        key = item[EnumQuery.TaskKeyOilSample]?.ToString(),
                        taskValue = taskValue,
                        updatedDate = sampleDate,
                        value = ""
                    };

                    if (taskValueOilChange.Count >= 1)
                    {
                        taskValue = string.Empty;
                        sampleDate = string.Empty;
                        dynamic jsontaskValue = JsonConvert.SerializeObject(taskValueOilChange.FirstOrDefault());
                        JObject JtaskValue = JObject.Parse(jsontaskValue);
                        taskValue = JtaskValue[EnumQuery.TaskValue].ToString();
                        sampleDate = JtaskValue[EnumQuery.UpdatedDate].ToString();
                    }

                    var oilChange = new TaskMapping()
                    {
                        name = EnumQuery.LubeServiceChange,
                        key = item[EnumQuery.TaskKeyOilChange]?.ToString(),
                        taskValue = taskValue,
                        updatedDate = sampleDate,
                        value = ""
                    };

                    if (taskValueOilLevelCheck.Count >= 1)
                    {
                        taskValue = string.Empty;
                        sampleDate = string.Empty;
                        dynamic jsontaskValue = JsonConvert.SerializeObject(taskValueOilLevelCheck.FirstOrDefault());
                        JObject JtaskValue = JObject.Parse(jsontaskValue);
                        taskValue = JtaskValue[EnumQuery.TaskValue].ToString();
                        sampleDate = JtaskValue[EnumQuery.UpdatedDate].ToString();
                    }
                    var oilLevelCheck = new TaskMapping()
                    {
                        name = EnumQuery.LubeServiceLevelCheck,
                        key = item[EnumQuery.TaskKeyOilLevelCheck]?.ToString(),
                        taskValue = taskValue,
                        updatedDate = sampleDate,
                        value = ""
                    };

                    var listtask = new List<TaskMapping>();
                    listtask.Add(oilSample);
                    listtask.Add(oilChange);
                    listtask.Add(oilLevelCheck);
                    //listtask.Add(topUpLevelCheck);

                    var ts = new SosHistoryLubricantMappingModel()
                    {
                        key = item[EnumQuery.Key].ToString(),
                        compartmentLubricant = item[EnumQuery.CompartmentLubricant]?.ToString(),
                        recommendedLubricants = item[EnumQuery.RecomendedLubricant]?.ToString(),
                        volume = item[EnumQuery.Volume]?.ToString(),
                        uoM = item[EnumQuery.Uom]?.ToString(),
                        lubricantType = item[EnumQuery.LubricantType]?.ToString(),
                        isSOS = item[EnumQuery.IsSOS]?.ToString(),
                        task = listtask.Where(x => !string.IsNullOrEmpty(x.key)).ToList()
                    };

                    resSubgroup.Add(ts);
                }
                #endregion

                long EHM = long.Parse(JSuckAndBlowHeader[EnumQuery.Smu].ToString());
                long EhmOffset = StaticHelper.GetPropValue(masterDataSos, EnumQuery.EhmOffset);
                sosHistoryModels = new SosHistoryModel()
                {
                    key = Guid.NewGuid().ToString(),
                    headerId = JSuckAndBlowHeader[EnumQuery.ID].ToString(),
                    siteId = (string)StaticHelper.GetPropValue(masterDataSos, EnumQuery.siteId),
                    workOrder = JSuckAndBlowHeader[EnumQuery.SSWorkorder].ToString(),
                    modelId = JSuckAndBlowHeader[EnumQuery.ModelId].ToString(),
                    psTypeId = JSuckAndBlowHeader[EnumQuery.PsTypeId].ToString(),
                    equipment = JSuckAndBlowHeader[EnumQuery.Equipment].ToString(),
                    meterHrs = (EHM + EhmOffset).ToString(),
                    brand = (string)StaticHelper.GetPropValue(masterDataSos, EnumQuery.Brand),
                    equipmentModel = (string)StaticHelper.GetPropValue(masterDataSos, EnumQuery.EquipmentModel),
                    equipmentSerialNumber = (string)StaticHelper.GetPropValue(masterDataSos, EnumQuery.SerialNumber),
                    customerName = (string)StaticHelper.GetPropValue(masterDataSos, EnumQuery.Ownership),
                    jobSite = (string)StaticHelper.GetPropValue(masterDataSos, EnumQuery.SiteDescription),
                    brandDescription = (string)StaticHelper.GetPropValue(masterDataSos, EnumQuery.BrandDescription),
                    fuelType = EnumTaskValue.FeulType,
                    eformType = param[EnumQuery.EformType].ToString()

                };

                sosHistoryModels.subGroup = resSubgroup;

                await _repository.Create(new CreateRequest()
                {
                    employee = new EmployeeModel() { id = EnumCaption.System, name = EnumCaption.System },
                    entity = sosHistoryModels
                });
            }

            return sosHistoryModels;
        }

        protected async Task<SosHistoryModel> GenerateFromServiceSheet(Dictionary<string, object> param, dynamic masterDataSos)
        {
            SosHistoryModel sosHistoryModels = new SosHistoryModel();

            #region get data servicesheet header
            Dictionary<string, object> SSparam = new Dictionary<string, object>();
            SSparam.Add(EnumQuery.IsDeleted, false.ToString().ToLower());
            SSparam.Add(EnumQuery.GroupName, EnumGroup.General);
            SSparam.Add(EnumQuery.SSWorkorder, param[EnumQuery.SSWorkorder].ToString());
            //SSparam.Add(EnumQuery.ModelId, param[EnumQuery.ModelId]);
            //SSparam.Add(EnumQuery.Equipment, param[EnumQuery.Equipment]);

            var ListServiceSheetHeader = await _serviceSheetRepository.GetDataListByParamJArray(SSparam);
            //var resList = StaticHelper.GetData(ListServiceSheetHeader, EnumQuery.SSWorkorder, param[EnumQuery.SSWorkorder].ToString());

            JObject JserviceSheetHeader = JObject.Parse(JsonConvert.SerializeObject(ListServiceSheetHeader.FirstOrDefault()));

            #endregion

            #region get param lubricant mapping

            Dictionary<string, object> paramLubricant = new Dictionary<string, object>();
            paramLubricant.Add(EnumQuery.ModelId, JserviceSheetHeader[EnumQuery.ModelId]);
            paramLubricant.Add(EnumQuery.PsTypeId, JserviceSheetHeader[EnumQuery.PsTypeId]);

            var rsc = await _lubricantMappingRepository.GetDataByParam(paramLubricant);
            #endregion

            if (rsc != null)
            {

                var json = JsonConvert.SerializeObject(rsc);
                JObject jObj = JObject.Parse(json);
                JToken token = JToken.Parse(json);

                var resLubricantMapping = token.SelectToken($"$..['{EnumQuery.Detail}']").ToList();

                #region get servicesheetdetail
                Dictionary<string, object> paramServiceSheetDetail = new Dictionary<string, object>();
                paramServiceSheetDetail.Add(EnumQuery.SSWorkorder, param[EnumQuery.SSWorkorder]);
                paramServiceSheetDetail.Add(EnumQuery.GroupName, EnumGroup.LubeService);

                var servicesheetDetail = await _serviceSheetDetailRepository.GetDataByParam(paramServiceSheetDetail);

                string jsonservicesheetDetail = JsonConvert.SerializeObject(servicesheetDetail);
                JObject jservicesheetDetail = JObject.Parse(jsonservicesheetDetail);
                JToken tokenservicesheetDetail = JToken.Parse(jsonservicesheetDetail);

                #endregion

                #region fetch data

                List<SosHistoryLubricantMappingModel> resSubgroup = new List<SosHistoryLubricantMappingModel>();

                foreach (var item in resLubricantMapping)
                {
                    var taskValue = string.Empty;
                    var sampleDate = string.Empty;
                    var value = string.Empty;
                    bool isTopup = false;

                    #region get task value of compartment

                    var taskValueOilSample = tokenservicesheetDetail.SelectTokens($"$....[?(@.{EnumQuery.Key}=='{item[EnumQuery.TaskKeyOilSample]}')]['{EnumQuery.TaskValue}','{EnumQuery.UpdatedDate}']")
                        .Select(v => (JProperty)v.Parent) // Get parent JProperty (which encapsulates name as well as value)
                                    .GroupBy(p => p.Parent)           // Group by parent JObject
                                    .Select(g => new JObject(g)).ToList();

                    var taskValueOilChange = tokenservicesheetDetail.SelectTokens($"$....[?(@.{EnumQuery.Key}=='{item[EnumQuery.TaskKeyOilChange]}')]['{EnumQuery.TaskValue}','{EnumQuery.UpdatedDate}']")
                        .Select(v => (JProperty)v.Parent) // Get parent JProperty (which encapsulates name as well as value)
                                    .GroupBy(p => p.Parent)           // Group by parent JObject
                                    .Select(g => new JObject(g)).ToList();

                    var taskValueOilLevelCheck = tokenservicesheetDetail.SelectTokens($"$....[?(@.{EnumQuery.Key}=='{item[EnumQuery.TaskKeyOilLevelCheck]}')]['{EnumQuery.TaskValue}','{EnumQuery.UpdatedDate}']")
                        .Select(v => (JProperty)v.Parent) // Get parent JProperty (which encapsulates name as well as value)
                                    .GroupBy(p => p.Parent)           // Group by parent JObject
                                    .Select(g => new JObject(g)).ToList();

                    var taskValueTopUp = tokenservicesheetDetail.SelectTokens($"$.....[?(@.{EnumQuery.Key}=='{item[EnumQuery.TaskTopUpLevelCheck]}')]['{EnumQuery.Value}']")
                        .Select(v => (JProperty)v.Parent) // Get parent JProperty (which encapsulates name as well as value)
                                    .GroupBy(p => p.Parent)           // Group by parent JObject
                                    .Select(g => new JObject(g)).ToList();
                    #endregion


                    if (taskValueOilSample.Count >= 1)
                    {
                        dynamic jsontaskValueOilSample = JsonConvert.SerializeObject(taskValueOilSample.FirstOrDefault());
                        JObject JtaskValueOilSample = JObject.Parse(jsontaskValueOilSample);
                        taskValue = JtaskValueOilSample[EnumQuery.TaskValue].ToString();
                        sampleDate = JtaskValueOilSample[EnumQuery.UpdatedDate].ToString();
                    }
                    var oilSample = new TaskMapping()
                    {
                        name = EnumQuery.LubeServiceSample,
                        key = item[EnumQuery.TaskKeyOilSample]?.ToString(),
                        taskValue = taskValue,
                        updatedDate = sampleDate,
                        value = ""
                    };

                    if (taskValueOilChange.Count >= 1)
                    {
                        taskValue = string.Empty;
                        sampleDate = string.Empty;
                        dynamic jsontaskValue = JsonConvert.SerializeObject(taskValueOilChange.FirstOrDefault());
                        JObject JtaskValue = JObject.Parse(jsontaskValue);
                        taskValue = JtaskValue[EnumQuery.TaskValue].ToString();
                        sampleDate = JtaskValue[EnumQuery.UpdatedDate].ToString();
                    }

                    var oilChange = new TaskMapping()
                    {
                        name = EnumQuery.LubeServiceChange,
                        key = item[EnumQuery.TaskKeyOilChange]?.ToString(),
                        taskValue = taskValue,
                        updatedDate = sampleDate,
                        value = ""
                    };

                    if (taskValueOilLevelCheck.Count >= 1)
                    {
                        taskValue = string.Empty;
                        sampleDate = string.Empty;
                        dynamic jsontaskValue = JsonConvert.SerializeObject(taskValueOilLevelCheck.FirstOrDefault());
                        JObject JtaskValue = JObject.Parse(jsontaskValue);
                        taskValue = JtaskValue[EnumQuery.TaskValue].ToString();
                        sampleDate = JtaskValue[EnumQuery.UpdatedDate].ToString();
                    }
                    var oilLevelCheck = new TaskMapping()
                    {
                        name = EnumQuery.LubeServiceLevelCheck,
                        key = item[EnumQuery.TaskKeyOilLevelCheck]?.ToString(),
                        taskValue = taskValue,
                        updatedDate = sampleDate,
                        value = ""
                    };

                    if (taskValueTopUp.Count >= 1)
                    {
                        taskValue = string.Empty;
                        sampleDate = string.Empty;
                        dynamic jsonValueTopUp = JsonConvert.SerializeObject(taskValueTopUp.FirstOrDefault());
                        JObject JValueTopUp = JObject.Parse(jsonValueTopUp);
                        value = JValueTopUp[EnumQuery.Value].ToString();
                    }

                    var topUpLevelCheck = new TaskMapping()
                    {
                        name = EnumQuery.LubeServiceLevelTopUp,
                        key = item[EnumQuery.TaskTopUpLevelCheck]?.ToString(),
                        taskValue = taskValue,
                        updatedDate = sampleDate,
                        value = value
                    };

                    var listtask = new List<TaskMapping>();
                    listtask.Add(oilSample);
                    listtask.Add(oilChange);
                    listtask.Add(oilLevelCheck);
                    listtask.Add(topUpLevelCheck);

                    var ts = new SosHistoryLubricantMappingModel()
                    {
                        key = item[EnumQuery.Key].ToString(),
                        compartmentLubricant = item[EnumQuery.CompartmentLubricant]?.ToString(),
                        recommendedLubricants = item[EnumQuery.RecomendedLubricant]?.ToString(),
                        volume = item[EnumQuery.Volume]?.ToString(),
                        uoM = item[EnumQuery.Uom]?.ToString(),
                        lubricantType = item[EnumQuery.LubricantType]?.ToString(),
                        isSOS = item[EnumQuery.IsSOS]?.ToString(),
                        task = listtask.Where(x => !string.IsNullOrEmpty(x.key)).ToList()
                    };

                    resSubgroup.Add(ts);
                }

                #endregion

                long EHM = long.Parse(JserviceSheetHeader[EnumQuery.Smu].ToString());
                long EhmOffset = StaticHelper.GetPropValue(masterDataSos, EnumQuery.EhmOffset);
                var serialNumber = StaticHelper.GetPropValue(masterDataSos, EnumQuery.SerialNumber);
                sosHistoryModels = new SosHistoryModel()
                {
                    key = Guid.NewGuid().ToString(),
                    headerId = JserviceSheetHeader[EnumQuery.ID].ToString(),
                    siteId = (string)StaticHelper.GetPropValue(masterDataSos, EnumQuery.siteId),
                    workOrder = JserviceSheetHeader[EnumQuery.SSWorkorder].ToString(),
                    modelId = JserviceSheetHeader[EnumQuery.ModelId].ToString(),
                    psTypeId = JserviceSheetHeader[EnumQuery.PsTypeId].ToString(),
                    equipment = JserviceSheetHeader[EnumQuery.Equipment].ToString(),
                    meterHrs = (EHM + EhmOffset).ToString(),
                    brand = (string)StaticHelper.GetPropValue(masterDataSos, EnumQuery.Brand),
                    equipmentModel = (string)StaticHelper.GetPropValue(masterDataSos, EnumQuery.EquipmentModel),
                    equipmentSerialNumber = serialNumber.ToString(),
                    customerName = (string)StaticHelper.GetPropValue(masterDataSos, EnumQuery.Ownership),
                    jobSite = (string)StaticHelper.GetPropValue(masterDataSos, EnumQuery.SiteDescription),
                    brandDescription = (string)StaticHelper.GetPropValue(masterDataSos, EnumQuery.BrandDescription),
                    fuelType = EnumTaskValue.FeulType,
                    eformType = param[EnumQuery.EformType].ToString()

                };

                sosHistoryModels.subGroup = resSubgroup;

                await _repository.Create(new CreateRequest()
                {
                    employee = new EmployeeModel() { id = EnumCaption.System, name = EnumCaption.System },
                    entity = sosHistoryModels
                });
            }
            return sosHistoryModels;
        }
    }
}
