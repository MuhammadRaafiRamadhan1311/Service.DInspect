using Service.DInspect.Repositories;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Logging;
using System.Linq;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Service.DInspect.Services.Helpers;
using Service.DInspect.Helpers;
using Service.DInspect.Models.Enum;
using Service.DInspect.Models;
using Service.DInspect.Models.Response;
using Service.DInspect.Models.Request;
using Service.DInspect.Interfaces;

namespace Service.DInspect.Services
{
    public class SuckAndBlowDetailService : ServiceBase
    {
        protected string _container;
        protected IConnectionFactory _connectionFactory;
        protected IRepositoryBase _serviceHeaderRepository;

        public SuckAndBlowDetailService(MySetting appSetting, IConnectionFactory connectionFactory, string container, string accessToken, ILoggerFactory logger) : base(appSetting, connectionFactory, container, accessToken)
        {
            _container = container;
            _connectionFactory = connectionFactory;
            _repository = new SuckAndBlowDetailRepository(connectionFactory, container);
            _serviceHeaderRepository = new SuckAndBlowHeaderRepository(connectionFactory, EnumContainer.InterimEngineHeader);
            //_logger = logger.CreateLogger<ServiceSheetDetailService>();
        }

        public override async Task<ServiceResult> Put(UpdateRequest updateRequest)
        {
            throw new Exception("Function is not available");
        }

        public async Task<ServiceResult> UpdateTask(UpdateTaskRequest updateTaskRequest)
        {
            UpdateTaskServiceHelper service = new UpdateTaskServiceHelper(_appSetting, _connectionFactory, _container, _accessToken);
            ServiceResult result = await service.UpdateTask(updateTaskRequest);

            if (!result.IsError)
            {
                SOSService SOSService = new SOSService(_appSetting, _connectionFactory, EnumContainer.SOSHistory, _accessToken);
                await SOSService.UpdateTask(updateTaskRequest, EnumEformType.SuckAndBlow);
            }
            return result;
        }

        public async Task<ServiceResult> UpdateTaskWithDefect(UpdateTaskDefectRequest updateTaskDefectRequest)
        {
            UpdateTaskServiceHelper service = new UpdateTaskServiceHelper(_appSetting, _connectionFactory, _container, _accessToken);
            ServiceResult result = await service.UpdateTaskWithDefect(updateTaskDefectRequest);

            if (!result.IsError)
            {
                SOSService SOSService = new SOSService(_appSetting, _connectionFactory, EnumContainer.SOSHistory, _accessToken);
                UpdateTaskRequest updateTaskRequest = new UpdateTaskRequest()
                {
                    workorder = updateTaskDefectRequest.workorder,
                    headerId = updateTaskDefectRequest.headerId,
                    updateParams = updateTaskDefectRequest.updateParams,
                    employee = updateTaskDefectRequest.employee

                };
                await SOSService.UpdateTask(updateTaskRequest, EnumEformType.SuckAndBlow);
            }
            return result;
        }

        public async Task<ServiceResult> GetTaskProgress(Dictionary<string, object> param)
        {
            try
            {
                List<TaskProgressResponse> result = new List<TaskProgressResponse>();

                param.Add("isDeleted", "false");
                var dataGroups = await _repository.GetDataListByParam(param);

                foreach (var dataGroup in dataGroups)
                {
                    UpdateTaskServiceHelper updateTaskService = new UpdateTaskServiceHelper(_appSetting, _connectionFactory, _container, _accessToken);
                    TaskProgressResponse data = await updateTaskService.GetTaskProgress(dataGroup);

                    result.Add(data);
                }

                Version103Service version103Service = new Version103Service(_appSetting, _connectionFactory, _accessToken);
                string workOrder = param.Where(x => x.Key == EnumQuery.SSWorkorder).SingleOrDefault().Value.ToString();


                if (workOrder != null)
                {
                    var res = await version103Service.GetInterimDefectHeader(workOrder);

                    string json = JsonConvert.SerializeObject(res);
                    JObject jObj = JObject.Parse(json);

                    var defectHeaders = jObj.SelectToken(EnumQuery.DefectHeader);

                    var defectHeaderCount = defectHeaders
                        .Where(x => x.SelectToken(EnumQuery.Category).ToString() == EnumCategoryServiceSheet.CBM || x.SelectToken(EnumQuery.Category).ToString() == EnumCategoryServiceSheet.CBM_NORMAL)
                        .Count(); // count condition data

                    //var defectCount = jObj.SelectToken(EnumQuery.DefectDetail).Count();

                    int idCount = defectHeaderCount;
                    foreach (JToken tk in defectHeaders)
                    {
                        var id = tk.SelectToken(EnumQuery.ID).ToString();
                        idCount += jObj.SelectToken(EnumQuery.DefectDetail).Where(x => x.SelectToken(EnumQuery.DefectHeaderId).ToString() == id).Count();
                    }
                    result.Add(new TaskProgressResponseWithIdentifiedDefect
                    {
                        workorder = workOrder,
                        group = EnumGroup.DefectIdentifiedService,
                        doneTask = 0,
                        totalTask = 0,
                        identifiedDefectCount = idCount,
                    });

                }

                return new ServiceResult
                {
                    Message = "Get task progress successfully",
                    IsError = false,
                    Content = result
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<ServiceResult> GetUpdateResult(string id)
        {
            var updatedData = await Get(id);

            string modelId = StaticHelper.GetPropValue(updatedData.Content, EnumQuery.ModelId)?.Value;
            string psTypeId = StaticHelper.GetPropValue(updatedData.Content, EnumQuery.PsTypeId)?.Value;
            string workOrder = StaticHelper.GetPropValue(updatedData.Content, EnumQuery.SSWorkorder)?.Value;

            Dictionary<string, object> taskProgressParam = new Dictionary<string, object>();
            taskProgressParam.Add(EnumQuery.ModelId, modelId);
            taskProgressParam.Add(EnumQuery.PsTypeId, psTypeId);
            taskProgressParam.Add(EnumQuery.SSWorkorder, workOrder);

            var taskProgress = await GetTaskProgress(taskProgressParam);

            dynamic result = new ExpandoObject();
            result.TaskProgress = taskProgress?.Content;
            result.UpdatedData = updatedData?.Content;

            return new ServiceResult
            {
                Message = "Data updated successfully",
                IsError = false,
                Content = result
            };
        }

        public async Task<ServiceResult> UpdateTaskReviseInterim(UpdateTaskReviseRequest updateTaskRequest)
        {
            UpdateTaskServiceHelper service = new UpdateTaskServiceHelper(_appSetting, _connectionFactory, _container, _accessToken);
            ServiceResult result = await service.UpdateTaskReviseInterim(updateTaskRequest);

            return result;
        }

        public async Task<ServiceResult> UpdateTaskWithDefectReviseInterim(UpdateTaskDefectReviseRequest updateTaskDefectRequest)
        {
            UpdateTaskServiceHelper service = new UpdateTaskServiceHelper(_appSetting, _connectionFactory, _container, _accessToken);
            ServiceResult result = await service.UpdateTaskWithDefectReviseInterim(updateTaskDefectRequest);

            return result;
        }

        public async Task<ServiceResult> GetDataDetailCbm(DetailServiceSheet model)
        {
            var _repoDetail = new SuckAndBlowDetailRepository(_connectionFactory, EnumContainer.InterimEngineDetail);
            dynamic currentData = await _repoDetail.GetDataServiceSheetDetailByKey(model);

            if (currentData == null)
                throw new Exception($"Current data work order {model.workOrder} not found");

            Dictionary<string, object> paramHeader = new Dictionary<string, object>();
            paramHeader.Add(EnumQuery.SSWorkorder, model.workOrder);

            var curentDataHeader = await _serviceHeaderRepository.GetDataByParam(paramHeader);

            if (curentDataHeader == null)
                throw new Exception($"Current data header work order {model.workOrder} not found");

            var _repoCbmHistory = new CbmHitoryRepository(_connectionFactory, EnumContainer.CbmHistory);
            Dictionary<string, object> paramHistory = new Dictionary<string, object>();
            paramHistory.Add(EnumQuery.SSWorkorder, model.workOrder);
            paramHistory.Add(EnumQuery.SSTaskKey, model.taskKey);

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
            result.Add(EnumQuery.SSWorkorder, model.workOrder);
            result.Add(EnumQuery.Equipment, curentDataHeader.equipment);
            result.Add(EnumQuery.siteId, curentDataHeader.siteId);
            result.Add(EnumQuery.SSTaskKey, model.taskKey);
            result.Add(EnumQuery.HeaderId, curentDataHeader.id);
            result.Add(EnumQuery.Form, curentDataHeader.form);
            result.Add(EnumQuery.ServiceSheetDetailId, currentData[0].id);


            Dictionary<string, object> curentDetail = new Dictionary<string, object>();

            var splitDesc = currentData[0].description.ToString().Split(";");

            curentDetail.Add(EnumQuery.SSTaskKey, currentData[0].key);
            curentDetail.Add(EnumQuery.Category, currentData[0].category);
            curentDetail.Add(EnumQuery.Rating, currentData[0].taskValue);
            curentDetail.Add(EnumQuery.UpdatedBy, currentData[0].updatedBy);
            curentDetail.Add(EnumQuery.UpdatedDate, currentData[0].updatedDate);
            curentDetail.Add(EnumQuery.TaskNo, currentData[0].taskNo);
            curentDetail.Add(EnumQuery.MeasurementLocation, splitDesc[2]);
            curentDetail.Add(EnumQuery.MeasurementValue, currentData[0].measurementValue);
            curentDetail.Add(EnumQuery.SSUom, currentData[0].uom);
            curentDetail.Add(EnumQuery.Section, currentData[0].SectionData);

            var adjustmentCalibration = new List<dynamic>();
            var historyModifiedDefault = new List<dynamic>();
            string getTaskValue = string.Empty;
            if (currentData[0].rating == EnumRatingServiceSheet.CALIBRATION)
            {
                var newParams = new Dictionary<string, object>();
                newParams.Add(EnumQuery.SSWorkorder, model.workOrder.ToString());
                newParams.Add(EnumQuery.ID, currentData[0].id);
                var dataDetail = await _repoDetail.GetDataByParam(newParams);


                var getCalibration = StaticHelper.GetData(dataDetail, EnumQuery.Key, model.taskKey);


                getTaskValue = StaticHelper.GetData(dataDetail, "showParameter", "suspensionCylinder")[0][EnumQuery.TaskValue];

                foreach (var item in getCalibration)
                {
                    string key = item[EnumQuery.Key].ToString();

                    curentDetail = new Dictionary<string, object>();
                    curentDetail.Add(EnumQuery.SSTaskKey, "");
                    curentDetail.Add(EnumQuery.Category, "");
                    curentDetail.Add(EnumQuery.Rating, "");
                    curentDetail.Add(EnumQuery.UpdatedBy, "");
                    curentDetail.Add(EnumQuery.UpdatedDate, "");
                    curentDetail.Add(EnumQuery.TaskNo, "");
                    curentDetail.Add(EnumQuery.MeasurementLocation, "");
                    curentDetail.Add(EnumQuery.MeasurementValue, "");
                    curentDetail.Add(EnumQuery.SSUom, "");

                    if (getTaskValue == EnumTaskValue.CalibrationYes)
                    {
                        var getCalibrationAdjustment = StaticHelper.GetParentData(dataDetail, EnumQuery.MappingKeyId, key);
                        var mappingKey = getCalibrationAdjustment != null ? getCalibrationAdjustment.key.ToString() : string.Empty;
                        if (getCalibrationAdjustment != null)
                        {

                            curentDetail[EnumQuery.SSTaskKey] = getCalibrationAdjustment.key;
                            //curentDetail[EnumQuery.Category] = getCalibrationAdjustment.category;
                            curentDetail[EnumQuery.Category] = EnumCategoryServiceSheet.CBM;
                            curentDetail[EnumQuery.Rating] = getCalibrationAdjustment.items[3].valueItemType == null ? getCalibrationAdjustment.items[5].value : getCalibrationAdjustment.items[6].value;
                            curentDetail[EnumQuery.UpdatedBy] = getCalibrationAdjustment.updatedBy;
                            curentDetail[EnumQuery.UpdatedDate] = getCalibrationAdjustment.updatedDate;
                            curentDetail[EnumQuery.TaskNo] = getCalibrationAdjustment.items[0].value;
                            curentDetail[EnumQuery.MeasurementLocation] = getCalibrationAdjustment.items[2].value;
                            curentDetail[EnumQuery.MeasurementValue] = getCalibrationAdjustment.items[3].valueItemType == null ? getCalibrationAdjustment.items[3].value : getCalibrationAdjustment.items[4].value;
                            curentDetail[EnumQuery.SSUom] = getCalibrationAdjustment.items[3].valueItemType == null ? getCalibrationAdjustment.items[4].value : getCalibrationAdjustment.items[5].value;

                        }
                    }
                    else
                    {
                        var getCalibrationAdjustment = StaticHelper.GetData(dataDetail, EnumQuery.Key, key);
                        var KeysuspensionCylinder = StaticHelper.GetData(dataDetail, EnumQuery.ShowParameter, EnumQuery.SuspensionCylinder)[0][EnumQuery.Key].ToString();
                        var keyCalibration = getCalibrationAdjustment[0][EnumQuery.Key].ToString();
                        if (getCalibrationAdjustment != null)
                        {
                            curentDetail[EnumQuery.SSTaskKey] = getCalibrationAdjustment[0][EnumQuery.Key];
                            curentDetail[EnumQuery.Category] = getCalibrationAdjustment[0][EnumQuery.Category];
                            curentDetail[EnumQuery.Rating] = getCalibrationAdjustment[0][EnumQuery.Items][3].valueItemType == null ? getCalibrationAdjustment[0][EnumQuery.Items][5].value : getCalibrationAdjustment[0][EnumQuery.Items][6].value;
                            curentDetail[EnumQuery.UpdatedBy] = getCalibrationAdjustment[0][EnumQuery.UpdatedBy];
                            curentDetail[EnumQuery.UpdatedDate] = getCalibrationAdjustment[0][EnumQuery.UpdatedDate];
                            curentDetail[EnumQuery.TaskNo] = getCalibrationAdjustment[0][EnumQuery.Items][0].value;
                            curentDetail[EnumQuery.MeasurementLocation] = getCalibrationAdjustment[0][EnumQuery.Items][2].value;
                            curentDetail[EnumQuery.MeasurementValue] = getCalibrationAdjustment[0][EnumQuery.Items][3].valueItemType == null ? getCalibrationAdjustment[0][EnumQuery.Items][3].value : getCalibrationAdjustment[0][EnumQuery.Items][4].value;
                            curentDetail[EnumQuery.SSUom] = getCalibrationAdjustment[0][EnumQuery.Items][3].valueItemType == null ? getCalibrationAdjustment[0][EnumQuery.Items][4].value : getCalibrationAdjustment[0][EnumQuery.Items][5].value;

                        }
                    }
                }
            }


            List<dynamic> listItems = JsonConvert.DeserializeObject<List<dynamic>>(JsonConvert.SerializeObject(currentData[0].items));
            List<dynamic> listPictures = JsonConvert.DeserializeObject<List<dynamic>>(JsonConvert.SerializeObject(currentData[0].pictures));
            bool isCbmAdjustmentOrReplacement = currentData[0].cbmAdjustmentReplacement;

            string modelCbm = curentDataHeader.modelId.ToString();
            modelCbm = modelCbm.Replace("KOM ", "");
            modelCbm = modelCbm.Replace("CAT ", "");
            modelCbm = modelCbm.Replace("HIT ", "");
            modelCbm = modelCbm.Replace("LIE ", "");

            CallAPIHelper callAPI = new CallAPIHelper(_accessToken);
            ApiResponse response = await callAPI.Get($"{EnumUrl.GetParameterRatingOverwrite}&model={modelCbm}&psType={curentDataHeader.psTypeId.ToString()}");

            result.Add("currentCondition", curentDetail);

            List<dynamic> paramAdm = JsonConvert.DeserializeObject<List<dynamic>>(JsonConvert.SerializeObject(response.Result.Content));

            if (paramAdm.Any())
            {
                List<ParameterRating> detailCbm = JsonConvert.DeserializeObject<List<ParameterRating>>(JsonConvert.SerializeObject(response.Result.Content[0].detail));

                //initiate when component null as empty string to generalize before check
                var _component = model.component ?? "";

                //adjustment query list cbm value when params component are exists or not
                if (_component == "")
                {
                    detailCbm = detailCbm.Where(x => x.taskKey == model.taskKey).OrderBy(x => x.cbmRating).ToList();
                }
                else
                {
                    detailCbm = detailCbm.Where(x => x.taskKey == model.taskKey && x.component == model.component).OrderBy(x => x.cbmRating).ToList();
                }


                result.Add("component", detailCbm.FirstOrDefault() != null ? detailCbm.FirstOrDefault().component : string.Empty);

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
                result.Add("component", string.Empty);
            }

            result.Add("detailedPicture", isCbmAdjustmentOrReplacement ? listPictures : listItems.Where(x => x[EnumQuery.ItemType] == EnumQuery.SmallCamera).Select(o => o[EnumQuery.Value]).FirstOrDefault());
            result.Add("historyModified", resultHistory);

            var currentDataMaster = await _repoDetail.GetDataMasterServiceByKey(curentDataHeader.modelId.ToString(), model.taskKey, curentDataHeader.psTypeId.ToString(), model.workOrder, getTaskValue);
            List<dynamic> objDataMaster = JsonConvert.DeserializeObject<List<dynamic>>(JsonConvert.SerializeObject(currentDataMaster));

            List<dynamic> resultListMasterTemplate = new List<dynamic>();
            if (objDataMaster.Any())
            {
                string resultParam = string.Empty;
                if (currentData[0].rating == EnumRatingServiceSheet.CALIBRATION)
                {
                    objDataMaster[0][EnumQuery.Category] = EnumCategoryServiceSheet.CBM;
                    objDataMaster[0][EnumQuery.Rating] = EnumRatingServiceSheet.CALIBRATION;
                }

                List<dynamic> objItemList = new List<dynamic>();
                foreach (var item in objDataMaster.FirstOrDefault().items)
                {
                    if (item.itemType?.ToString() == EnumQuery.DropDown || item.itemType?.ToString() == EnumQuery.SmallCamera || item.itemType?.ToString() == "input" || item.itemType?.ToString() == "statusInfo")
                    {
                        item.value = "";
                        item.updatedBy = "";
                        item.updatedDate = "";
                    }

                    if (item.categoryItemType?.ToString() != EnumQuery.DropdownTool &&
                        item.categoryItemType?.ToString() != EnumQuery.BrakeTypeDropdown &&
                        item.categoryItemType?.ToString() != EnumQuery.DropdownToolDisc)
                    {
                        if (item.categoryItemType?.ToString() == EnumQuery.ResultParamRating)
                        {
                            resultParam = item.key.ToString();
                        }

                        objItemList.Add(item);
                    }
                }

                objDataMaster.FirstOrDefault().items = JsonConvert.DeserializeObject<object>(JsonConvert.SerializeObject(objItemList));

                if (!string.IsNullOrEmpty(resultParam))
                {
                    var calculateResult = await _repoDetail.GetDataMasterServiceByCalculateKey(model.workOrder, resultParam);
                    List<dynamic> objDataCalculate = JsonConvert.DeserializeObject<List<dynamic>>(JsonConvert.SerializeObject(calculateResult));

                    foreach (var dataCalculate in objDataCalculate)
                    {
                        dataCalculate[EnumQuery.Items][3][EnumQuery.Value] = string.Empty;
                        dataCalculate[EnumQuery.Items][5][EnumQuery.Value] = JToken.FromObject(new string[] { });
                        dataCalculate[EnumQuery.TaskValue] = string.Empty;
                    }

                    resultListMasterTemplate.AddRange(objDataCalculate);
                }
            }

            resultListMasterTemplate.AddRange(objDataMaster);

            result.Add("historyModifiedDefault", resultListMasterTemplate);

            #region add property for cbm replacement/replacement gap
            string _rating = currentData[0].rating;

            if (_rating == "AUTOMATIC_REPLACEMENT" || _rating == "AUTOMATIC_REPLACEMENT_GAP")
            {
                string _groupTaskId = currentData[0].groupTaskId;
                var _dataPhoto = await _repoDetail.GetDataReplacementPhotosByKey(model.workOrder, _groupTaskId);

                result.Add("replacementPhoto", _dataPhoto);
                result.Add("isReplacementAdjustment", isCbmAdjustmentOrReplacement);
                result.Add("beforeReplacementItems", listItems);
            }
            #endregion

            return new ServiceResult
            {
                IsError = false,
                Message = "Get previous crack successfully",
                Content = result
            };
        }
    }
}
