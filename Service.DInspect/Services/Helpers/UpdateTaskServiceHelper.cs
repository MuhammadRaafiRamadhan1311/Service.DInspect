using Azure.Core;
using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Math;
using DocumentFormat.OpenXml.Office2019.Drawing.Model3D;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service.DInspect.Helpers;
using Service.DInspect.Interfaces;
using Service.DInspect.Models;
using Service.DInspect.Models.EHMS;
using Service.DInspect.Models.Entity;
using Service.DInspect.Models.Enum;
using Service.DInspect.Models.Request;
using Service.DInspect.Models.Response;
using Service.DInspect.Repositories;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Transactions;

namespace Service.DInspect.Services.Helpers
{
    public class UpdateTaskServiceHelper
    {
        private string _container;
        private IRepositoryBase _repository;
        private IRepositoryBase _servicesheetHeaderRepository;
        protected IRepositoryBase _defectHeaderRepository;
        protected IRepositoryBase _defectDetailRepository;
        protected IRepositoryBase _cbmHistoryRepository;
        protected IConnectionFactory _connectionFactory;
        protected MySetting _appSetting;
        protected string _accessToken;
        protected readonly TelemetryClient _telemetryClient;
        //private readonly ILogger<TEntity> _logger;

        public UpdateTaskServiceHelper(MySetting appSetting, IConnectionFactory connectionFactory, string container, string accessToken, TelemetryClient telemetryClient = null)
        {
            _container = container;
            _appSetting = appSetting;
            _accessToken = accessToken;
            _connectionFactory = connectionFactory;
            _telemetryClient = telemetryClient;

            if (container == EnumContainer.ServiceSheetDetail)
            {
                _repository = new ServiceSheetDetailRepository(connectionFactory, container);
                _servicesheetHeaderRepository = new ServiceSheetHeaderRepository(connectionFactory, EnumContainer.ServiceSheetHeader);
                _defectHeaderRepository = new DefectHeaderRepository(connectionFactory, EnumContainer.DefectHeader);
                _defectDetailRepository = new DefectDetailRepository(connectionFactory, EnumContainer.DefectDetail);
                _cbmHistoryRepository = new CbmHitoryRepository(connectionFactory, EnumContainer.CbmHistory);
            }
            else if (container == EnumContainer.Intervention)
            {
                _repository = new InterventionRepository(connectionFactory, container);
                _servicesheetHeaderRepository = new InterventionRepository(connectionFactory, container);
                _defectHeaderRepository = new InterventionDefectHeaderRepository(connectionFactory, EnumContainer.InterventionDefectHeader);
                _defectDetailRepository = new InterventionDefectDetailRepository(connectionFactory, EnumContainer.InterventionDefectDetail);
                _cbmHistoryRepository = new CbmHitoryRepository(connectionFactory, EnumContainer.CbmHistory);
            }
            else if (container == EnumContainer.CalibrationDetail)
            {
                _repository = new CalibrationDetailRepository(connectionFactory, container);
                _servicesheetHeaderRepository = new CalibrationHeaderRepository(connectionFactory, EnumContainer.CalibrationHeader);
            }
            else if (container == EnumContainer.ServiceSheetHeader)
            {
                _repository = new ServiceSheetHeaderRepository(connectionFactory, container);
                _servicesheetHeaderRepository = new ServiceSheetHeaderRepository(connectionFactory, EnumContainer.ServiceSheetHeader);
            }
            else if (container == EnumContainer.InterimEngineDetail)
            {
                _repository = new SuckAndBlowDetailRepository(connectionFactory, container);
                _servicesheetHeaderRepository = new SuckAndBlowHeaderRepository(connectionFactory, EnumContainer.InterimEngineHeader);
                _defectHeaderRepository = new InterimEngineDefectHeaderRepository(connectionFactory, EnumContainer.InterimEngineDefectHeader);
                _defectDetailRepository = new InterimEngineDefectDetailRepository(connectionFactory, EnumContainer.InterimEngineDefectDetail);
                _cbmHistoryRepository = new CbmHitoryRepository(connectionFactory, EnumContainer.CbmHistory);
            }
            else if (container == EnumContainer.InterimEngineHeader)
            {
                _repository = new SuckAndBlowHeaderRepository(connectionFactory, container);
                _servicesheetHeaderRepository = new SuckAndBlowDetailRepository(connectionFactory, EnumContainer.InterimEngineHeader);
                _cbmHistoryRepository = new CbmHitoryRepository(connectionFactory, EnumContainer.CbmHistory);
            }

            //_logger = logger.CreateLogger<TEntity>();
        }

        public async Task<ServiceResult> UpdateTask(UpdateTaskRequest updateTaskRequest)
        {
            try
            {
                #region Validation
                //_logger.LogWarning("Validation");

                #region Service Sheet Status Validation

                var header = await _servicesheetHeaderRepository.Get(updateTaskRequest.headerId);
                var status = string.Empty;

                if (_container == EnumContainer.ServiceSheetDetail)
                    status = header?.status;
                else if (_container == EnumContainer.Intervention)
                    status = header?.interventionExecution;
                else if (_container == EnumContainer.CalibrationDetail)
                    status = header?.statusCalibration;
                else if (_container == EnumContainer.InterimEngineDetail)
                    status = header?.status;

                if (status != EnumStatus.EFormOnProgress)
                    throw new Exception($"Failed to update data, service sheet status is {status}");

                #endregion

                var rsc = await _repository.Get(updateTaskRequest.id);
                string taskId = string.Empty;
                string workOrder = string.Empty;

                foreach (var updateParam in updateTaskRequest.updateParams)
                {
                    //var listProperty = updateParam.propertyParams.Select(x => x.propertyName).ToList();
                    //GetFieldValueListRequest customValidationRequest = new GetFieldValueListRequest()
                    //{
                    //    id = updateTaskRequest.id,
                    //    keyValue = updateParam.keyValue,
                    //    propertyName = listProperty
                    //};

                    //Dictionary<string, object> validation = await _repository.GetFieldValueList(customValidationRequest, true);

                    foreach (var propertyParam in updateParam.propertyParams)
                    {
                        string errMsg = await Validation(new ValidationRequest()
                        {
                            rsc = rsc,
                            //id = updateTaskRequest.id,
                            keyValue = updateParam.keyValue,
                            propertyName = propertyParam.propertyName,
                            propertyValue = propertyParam.propertyValue,
                            //employeeId = updateTaskRequest.employee.id,
                            isDefect = false,
                            headerId = updateTaskRequest.headerId,
                            workorder = updateTaskRequest.workorder,
                            //SSdetailId = updateTaskRequest.id,
                            employee = updateTaskRequest.employee,
                            propertyParams = updateParam.propertyParams,
                            taskKey = updateTaskRequest.taskKey
                        });

                        if (!string.IsNullOrEmpty(errMsg))
                        {
                            return new ServiceResult
                            {
                                Message = errMsg,
                                IsError = true
                            };
                        }
                    }
                }

                #endregion

                UpdateRequest updateRequest = new UpdateRequest()
                {
                    id = updateTaskRequest.id,
                    workOrder = updateTaskRequest.workorder,
                    updateParams = updateTaskRequest.updateParams,
                    employee = updateTaskRequest.employee
                };

                //_logger.LogWarning("Update Service Sheet Detail");

                var result = await _repository.Update(updateRequest, rsc);

                #region Delete Ext Defect

                //await DeleteExtDefect(updateParam.keyValue, updateTaskDefectRequest.workorder, updateTaskDefectRequest.employee);

                #endregion

                return new ServiceResult
                {
                    Message = "Data updated successfully",
                    IsError = false,
                    Content = null
                };

                //ServiceResult updateResult = await GetUpdateResult(updateTaskRequest.id);

                //return updateResult;
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

        public async Task<ServiceResult> UpdateTaskRevise(UpdateTaskReviseRequest updateTaskRequest)
        {
            try
            {
                #region Validation
                //_logger.LogWarning("Validation");

                #region Service Sheet Status Validation

                var header = await _servicesheetHeaderRepository.Get(updateTaskRequest.headerId);
                var status = string.Empty;

                //if (_container == EnumContainer.ServiceSheetDetail)
                //    status = header?.status;
                //else if (_container == EnumContainer.Intervention)
                //    status = header?.interventionExecution;
                //else if (_container == EnumContainer.CalibrationDetail)
                //    status = header?.statusCalibration;
                //else if (_container == EnumContainer.InterimEngineDetail)
                //    status = header?.status;

                //if (status != EnumStatus.EFormOnProgress)
                //    throw new Exception($"Failed to update data, service sheet status is {status}");

                #endregion

                var rsc = await _repository.Get(updateTaskRequest.id);
                string taskId = string.Empty;
                string workOrder = string.Empty;
                string dataTaskKey = string.Empty;
                string reasonData = string.Empty;
                string currenValue = string.Empty;
                string currenRating = string.Empty;
                string dataTaskKeyMain = updateTaskRequest.taskKey;
                bool hasTaskValue = false;

                foreach (var updateParam in updateTaskRequest.updateParams)
                {
                    //var listProperty = updateParam.propertyParams.Select(x => x.propertyName).ToList();
                    //GetFieldValueListRequest customValidationRequest = new GetFieldValueListRequest()
                    //{
                    //    id = updateTaskRequest.id,
                    //    keyValue = updateParam.keyValue,
                    //    propertyName = listProperty
                    //};

                    //Dictionary<string, object> validation = await _repository.GetFieldValueList(customValidationRequest, true);

                    foreach (var propertyParam in updateParam.propertyParams)
                    {
                        if (propertyParam.propertyName.ToLower() == EnumQuery.TaskValue.ToLower()) { hasTaskValue = true; }
                        if (propertyParam.propertyName.ToLower() == EnumQuery.Reason.ToLower()) { reasonData = propertyParam.propertyValue; }
                        if (propertyParam.propertyName.ToLower() == EnumQuery.Value.ToLower() &&
                            !propertyParam.propertyValue.Contains("filename") &&
                            propertyParam.propertyValue != EnumTaskValue.CbmA &&
                            propertyParam.propertyValue != EnumTaskValue.CbmB &&
                            propertyParam.propertyValue != EnumTaskValue.CbmC &&
                            propertyParam.propertyValue != EnumTaskValue.CbmX) { currenValue = propertyParam.propertyValue; }

                        if (propertyParam.propertyName.ToLower() == EnumQuery.Value.ToLower() &&
                            !propertyParam.propertyValue.Contains("filename") &&
                            (propertyParam.propertyValue == EnumTaskValue.CbmA ||
                            propertyParam.propertyValue == EnumTaskValue.CbmB ||
                            propertyParam.propertyValue == EnumTaskValue.CbmC ||
                            propertyParam.propertyValue == EnumTaskValue.CbmX)) { currenRating = propertyParam.propertyValue; }

                        dataTaskKey = updateParam.keyValue;
                        string errMsg = await Validation(new ValidationRequest()
                        {
                            rsc = rsc,
                            //id = updateTaskRequest.id,
                            keyValue = updateParam.keyValue,
                            propertyName = propertyParam.propertyName,
                            propertyValue = propertyParam.propertyValue,
                            //employeeId = updateTaskRequest.employee.id,
                            isDefect = false,
                            headerId = updateTaskRequest.headerId,
                            workorder = updateTaskRequest.workorder,
                            employee = updateTaskRequest.employee,
                            propertyParams = updateParam.propertyParams,
                            reason = reasonData
                        });

                        if (!string.IsNullOrEmpty(errMsg))
                        {
                            return new ServiceResult
                            {
                                Message = errMsg,
                                IsError = true
                            };
                        }
                    }
                }

                #endregion

                UpdateRequest updateRequest = new UpdateRequest()
                {
                    id = updateTaskRequest.id,
                    workOrder = updateTaskRequest.workorder,
                    updateParams = updateTaskRequest.updateParams,
                    employee = updateTaskRequest.employee
                };

                //_logger.LogWarning("Update Service Sheet Detail");

                //check data if cbm adjustment/replacement
                DetailServiceSheet _model = new DetailServiceSheet();
                _model.workOrder = updateRequest.workOrder;
                _model.taskKey = dataTaskKeyMain;

                var _repoDetailData = new ServiceSheetDetailRepository(_connectionFactory, EnumContainer.ServiceSheetDetail);
                var _currentData = await _repoDetailData.GetDataServiceSheetDetailByKey(_model);
                bool isCbmAdjustmentOrReplacement = _currentData[0].cbmAdjustmentReplacement;
                string updatedDate = ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerDateTime);

                #region History Revise if not adjustment/replacement
                if (isCbmAdjustmentOrReplacement == false)
                {
                    #region History Revise
                    if (hasTaskValue)
                    {
                        Dictionary<string, object> paramHistory = new Dictionary<string, object>();
                        paramHistory.Add("workOrder", updateRequest.workOrder);
                        paramHistory.Add("taskKey", dataTaskKeyMain);

                        var dataHistory = await _cbmHistoryRepository.GetDataByParam(paramHistory);
                        if (dataHistory == null)
                        {
                            DetailServiceSheet model = new DetailServiceSheet();
                            model.workOrder = updateRequest.workOrder;
                            model.taskKey = dataTaskKeyMain;

                            var _repoDetail = new ServiceSheetDetailRepository(_connectionFactory, EnumContainer.ServiceSheetDetail);
                            var currentData = await _repoDetail.GetDataServiceSheetDetailByKey(model);

                            Dictionary<string, object> paramHeader = new Dictionary<string, object>();
                            paramHeader.Add("workOrder", model.workOrder);

                            var curentDataHeaderUpdate = await _servicesheetHeaderRepository.GetDataByParam(paramHeader);

                            if (curentDataHeaderUpdate == null)
                                throw new Exception($"Current data header work order {model.workOrder} not found");

                            CbmHistoryParam headerHisUpdate = new CbmHistoryParam();
                            headerHisUpdate.key = EnumGroup.General;
                            headerHisUpdate.workOrder = model.workOrder;
                            //headerHisUpdate.workOrder = model.taskKey;
                            headerHisUpdate.equipment = curentDataHeaderUpdate.equipment;
                            headerHisUpdate.taskKey = model.taskKey;
                            headerHisUpdate.siteId = curentDataHeaderUpdate.siteId;
                            headerHisUpdate.serviceSheetDetailId = currentData[0].id;
                            headerHisUpdate.defectHeaderId = "";
                            headerHisUpdate.detail = new DetailCBMHistory();

                            //add 2024-02-19
                            headerHisUpdate.modelId = rsc[EnumQuery.ModelId];
                            headerHisUpdate.psTypeId = rsc[EnumQuery.PsTypeId];
                            headerHisUpdate.taskDescription = currentData[0][EnumQuery.Description];
                            headerHisUpdate.category = currentData[0].category + " " + currentData[0].rating;
                            headerHisUpdate.currentValue = currentData[0][EnumQuery.MeasurementValue];
                            headerHisUpdate.currentRating = currentData[0][EnumQuery.TaskValue];
                            headerHisUpdate.replacementValue = currentData[0][EnumQuery.NonCbmAdjustmentReplacementMeasurementValue];
                            headerHisUpdate.replacementRating = currentData[0][EnumQuery.NonCbmAdjustmentReplacementRating];
                            headerHisUpdate.closedDate = updatedDate;
                            headerHisUpdate.closedBy = updateRequest.employee.id;
                            headerHisUpdate.source = "ip_overwrite";

                            foreach (var item in currentData)
                            {
                                item.Remove("id");
                                item.Remove("uom");
                                item.Remove("taskNo");
                                item.Remove("measurementValue");
                                item.Remove("nonCbmAdjustmentReplacementMeasurementValue");
                                item.Remove("nonCbmAdjustmentReplacementRating");
                                //item.Remove("cbmAdjustmentReplacementValue");
                            }

                            // Last Value
                            DetailServiceSheet modelUpdate = new DetailServiceSheet();
                            modelUpdate.workOrder = updateRequest.workOrder;
                            modelUpdate.taskKey = dataTaskKeyMain;

                            var _repoDetailUpdate = new ServiceSheetDetailRepository(_connectionFactory, EnumContainer.ServiceSheetDetail);
                            var currentDataUpdate = await _repoDetailUpdate.GetDataServiceSheetDetailByKey(modelUpdate);

                            foreach (var item in currentDataUpdate)
                            {
                                item.Remove("id");
                                item.Remove("uom");
                                item.Remove("taskNo");
                                item.Remove("measurementValue");
                                item.Remove("nonCbmAdjustmentReplacementMeasurementValue");
                                item.Remove("nonCbmAdjustmentReplacementRating");
                                //item.Remove("cbmAdjustmentReplacementValue");

                                foreach (var itemDetail in item.items)
                                {
                                    if (updateTaskRequest.updateParams.Where(x => x.keyValue.ToString() == itemDetail.key.ToString()).Count() != 0)
                                    {
                                        var dataUpdate = updateTaskRequest.updateParams.Where(x => x.keyValue.ToString() == itemDetail.key.ToString()).FirstOrDefault();
                                        var dataDetailUpdate = dataUpdate.propertyParams.Where(x => x.propertyName.ToString() == "value").FirstOrDefault();
                                        var dataDetailUpdateBy = dataUpdate.propertyParams.Where(x => x.propertyName.ToString() == "updatedBy").FirstOrDefault();
                                        var dataDetailUpdateDate = dataUpdate.propertyParams.Where(x => x.propertyName.ToString() == "updatedDate").FirstOrDefault();

                                        if (dataDetailUpdate.propertyValue.Contains("filename"))
                                        {
                                            itemDetail.value = JsonConvert.DeserializeObject<object>(dataDetailUpdate.propertyValue);
                                        }
                                        else
                                        {
                                            itemDetail.value = dataDetailUpdate.propertyValue;
                                        }

                                        if (dataDetailUpdate.propertyValue == EnumTaskValue.CbmA || dataDetailUpdate.propertyValue == EnumTaskValue.CbmB || dataDetailUpdate.propertyValue == EnumTaskValue.CbmC || dataDetailUpdate.propertyValue == EnumTaskValue.CbmX)
                                        {
                                            item.taskValue = dataDetailUpdate.propertyValue;
                                            item.updatedBy = JsonConvert.DeserializeObject<object>(dataDetailUpdateBy.propertyValue);
                                            item.updatedDate = ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerDateTime);
                                        }

                                    }
                                }
                            }

                            currentData.Add(currentDataUpdate[0]);

                            DetailCBMHistory detailHisUpdate = new DetailCBMHistory();
                            detailHisUpdate.key = "HISTORY";
                            detailHisUpdate.category = currentData[0].category;
                            detailHisUpdate.rating = currentData[0].rating;
                            detailHisUpdate.history = currentData;

                            headerHisUpdate.detail = detailHisUpdate;

                            var modelHeader = new CreateRequest();
                            modelHeader.employee = new EmployeeModel();

                            modelHeader.employee.id = updateRequest.employee.id;
                            modelHeader.employee.name = updateRequest.employee.name;
                            modelHeader.entity = headerHisUpdate;

                            var resultAddHeader = await _cbmHistoryRepository.Create(modelHeader);
                        }
                        else
                        {
                            var detailHistory = StaticHelper.GetPropValue(dataHistory.detail, "history");

                            List<dynamic> dataCount = JsonConvert.DeserializeObject<List<dynamic>>(JsonConvert.SerializeObject(detailHistory));

                            if (dataCount.Count() > 3)
                            {
                                return new ServiceResult
                                {
                                    Message = "Cannot revise the data, you have limit for revising 3 times.",
                                    IsError = true
                                };
                            }

                            // Last Value
                            DetailServiceSheet modelUpdate = new DetailServiceSheet();
                            modelUpdate.workOrder = updateRequest.workOrder;
                            modelUpdate.taskKey = dataTaskKeyMain;

                            var _repoDetailUpdate = new ServiceSheetDetailRepository(_connectionFactory, EnumContainer.ServiceSheetDetail);
                            var currentDataUpdate = await _repoDetailUpdate.GetDataServiceSheetDetailByKey(modelUpdate);

                            foreach (var item in currentDataUpdate)
                            {
                                item.Remove("id");
                                item.Remove("uom");
                                item.Remove("taskNo");
                                item.Remove("measurementValue");
                                item.Remove("nonCbmAdjustmentReplacementMeasurementValue");
                                item.Remove("nonCbmAdjustmentReplacementRating");
                                //item.Remove("cbmAdjustmentReplacementValue");

                                foreach (var itemDetail in item.items)
                                {
                                    if (updateTaskRequest.updateParams.Where(x => x.keyValue.ToString() == itemDetail.key.ToString()).Count() != 0)
                                    {
                                        var dataUpdate = updateTaskRequest.updateParams.Where(x => x.keyValue.ToString() == itemDetail.key.ToString()).FirstOrDefault();
                                        var dataDetailUpdate = dataUpdate.propertyParams.Where(x => x.propertyName.ToString() == "value").FirstOrDefault();
                                        var dataDetailUpdateBy = dataUpdate.propertyParams.Where(x => x.propertyName.ToString() == "updatedBy").FirstOrDefault();
                                        var dataDetailUpdateDate = dataUpdate.propertyParams.Where(x => x.propertyName.ToString() == "updatedDate").FirstOrDefault();

                                        if (dataDetailUpdate.propertyValue.Contains("filename"))
                                        {
                                            itemDetail.value = JsonConvert.DeserializeObject<object>(dataDetailUpdate.propertyValue);
                                        }
                                        else
                                        {
                                            itemDetail.value = dataDetailUpdate.propertyValue;
                                        }

                                        if (dataDetailUpdate.propertyValue == EnumTaskValue.CbmA || dataDetailUpdate.propertyValue == EnumTaskValue.CbmB || dataDetailUpdate.propertyValue == EnumTaskValue.CbmC || dataDetailUpdate.propertyValue == EnumTaskValue.CbmX)
                                        {
                                            item.taskValue = dataDetailUpdate.propertyValue;
                                            item.updatedBy = JsonConvert.DeserializeObject<object>(dataDetailUpdateBy.propertyValue);
                                            item.updatedDate = ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerDateTime);
                                        }
                                    }
                                }
                            }

                            detailHistory.Add(currentDataUpdate[0]);

                            List<PropertyParam> propertyParams = new List<PropertyParam>() {
                        new PropertyParam()
                        {
                            propertyName = EnumQuery.CbmHistory,
                            propertyValue = JsonConvert.SerializeObject(detailHistory)
                        }
                    };

                            UpdateRequest updateDataParams = new UpdateRequest();
                            updateDataParams.id = dataHistory.id;
                            updateDataParams.workOrder = dataHistory.workOrder;
                            updateDataParams.updateParams = new List<UpdateParam>();
                            updateDataParams.employee = updateTaskRequest.employee;

                            updateDataParams.updateParams.Add(new UpdateParam()
                            {
                                keyValue = "HISTORY",
                                propertyParams = propertyParams
                            });

                            var resultUpdateHeader = await _cbmHistoryRepository.Update(updateDataParams, dataHistory);
                        }
                    }
                    else
                    {
                        Dictionary<string, object> paramHistory = new Dictionary<string, object>();
                        paramHistory.Add("workOrder", updateRequest.workOrder);
                        paramHistory.Add("taskKey", dataTaskKeyMain);

                        var dataHistory = await _cbmHistoryRepository.GetDataByParam(paramHistory);
                        if (dataHistory != null)
                        {
                            DetailServiceSheet model = new DetailServiceSheet();
                            model.workOrder = updateRequest.workOrder;
                            model.taskKey = dataTaskKeyMain;

                            var _repoDetail = new ServiceSheetDetailRepository(_connectionFactory, EnumContainer.ServiceSheetDetail);
                            var currentData = await _repoDetail.GetDataServiceSheetDetailByKey(model);
                            //bool isCbmAdjustmentOrReplacement = currentData[0].cbmAdjustmentReplacement;

                            var detailHistory = StaticHelper.GetPropValue(dataHistory.detail, "history");

                            List<dynamic> dataCount = JsonConvert.DeserializeObject<List<dynamic>>(JsonConvert.SerializeObject(detailHistory));

                            if (dataCount.Count() > 3)
                            {
                                return new ServiceResult
                                {
                                    Message = "Cannot revise the data, you have limit for revising 3 times.",
                                    IsError = true
                                };
                            }

                            // Last Value
                            DetailServiceSheet modelUpdate = new DetailServiceSheet();
                            modelUpdate.workOrder = updateRequest.workOrder;
                            modelUpdate.taskKey = dataTaskKeyMain;

                            var _repoDetailUpdate = new ServiceSheetDetailRepository(_connectionFactory, EnumContainer.ServiceSheetDetail);
                            var currentDataUpdate = await _repoDetailUpdate.GetDataServiceSheetDetailByKey(modelUpdate);

                            foreach (var item in currentDataUpdate)
                            {
                                item.Remove("id");
                                item.Remove("uom");
                                item.Remove("taskNo");
                                item.Remove("measurementValue");
                                item.Remove("nonCbmAdjustmentReplacementMeasurementValue");
                                item.Remove("nonCbmAdjustmentReplacementRating");
                                //item.Remove("cbmAdjustmentReplacementValue");

                                foreach (var itemDetail in item.items)
                                {
                                    if (updateTaskRequest.updateParams.Where(x => x.keyValue.ToString() == itemDetail.key.ToString()).Count() != 0)
                                    {
                                        var dataUpdate = updateTaskRequest.updateParams.Where(x => x.keyValue.ToString() == itemDetail.key.ToString()).FirstOrDefault();
                                        var dataDetailUpdate = dataUpdate.propertyParams.Where(x => x.propertyName.ToString() == "value").FirstOrDefault();
                                        var dataDetailUpdateBy = dataUpdate.propertyParams.Where(x => x.propertyName.ToString() == "updatedBy").FirstOrDefault();
                                        var dataDetailUpdateDate = dataUpdate.propertyParams.Where(x => x.propertyName.ToString() == "updatedDate").FirstOrDefault();

                                        if (dataDetailUpdate.propertyValue.Contains("filename"))
                                        {
                                            itemDetail.value = JsonConvert.DeserializeObject<object>(dataDetailUpdate.propertyValue);
                                        }
                                        else
                                        {
                                            itemDetail.value = dataDetailUpdate.propertyValue;
                                        }

                                        if (dataDetailUpdate.propertyValue == EnumTaskValue.CbmA || dataDetailUpdate.propertyValue == EnumTaskValue.CbmB || dataDetailUpdate.propertyValue == EnumTaskValue.CbmC || dataDetailUpdate.propertyValue == EnumTaskValue.CbmX)
                                        {
                                            item.taskValue = dataDetailUpdate.propertyValue;
                                            item.updatedBy = JsonConvert.DeserializeObject<object>(dataDetailUpdateBy.propertyValue);
                                            item.updatedDate = ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerDateTime);
                                        }

                                    }
                                }
                            }

                            detailHistory.Add(currentDataUpdate[0]);

                            //if (isCbmAdjustmentOrReplacement == true)
                            //{
                            //    // Last Value
                            //    DetailServiceSheet modelUpdate = new DetailServiceSheet();
                            //    modelUpdate.workOrder = updateRequest.workOrder;
                            //    modelUpdate.taskKey = dataTaskKeyMain;

                            //    var _repoDetailUpdate = new ServiceSheetDetailRepository(_connectionFactory, EnumContainer.ServiceSheetDetail);
                            //    var currentDataUpdate = await _repoDetailUpdate.GetDataServiceSheetDetailByKey(modelUpdate);

                            //    foreach (var item in currentDataUpdate)
                            //    {
                            //        item.Remove("id");
                            //        item.Remove("uom");
                            //        item.Remove("taskNo");
                            //        item.Remove("measurementValue");

                            //        foreach (var itemDetail in item.items)
                            //        {
                            //            if (updateTaskRequest.updateParams.Where(x => x.keyValue.ToString() == itemDetail.key.ToString()).Count() != 0)
                            //            {
                            //                var dataUpdate = updateTaskRequest.updateParams.Where(x => x.keyValue.ToString() == itemDetail.key.ToString()).FirstOrDefault();
                            //                var dataDetailUpdate = dataUpdate.propertyParams.Where(x => x.propertyName.ToString() == "value").FirstOrDefault();
                            //                var dataDetailUpdateBy = dataUpdate.propertyParams.Where(x => x.propertyName.ToString() == "updatedBy").FirstOrDefault();
                            //                var dataDetailUpdateDate = dataUpdate.propertyParams.Where(x => x.propertyName.ToString() == "updatedDate").FirstOrDefault();

                            //                if (dataDetailUpdate.propertyValue.Contains("filename"))
                            //                {
                            //                    itemDetail.value = JsonConvert.DeserializeObject<object>(dataDetailUpdate.propertyValue);
                            //                }
                            //                else
                            //                {
                            //                    itemDetail.value = dataDetailUpdate.propertyValue;
                            //                }

                            //                if (dataDetailUpdate.propertyValue == EnumTaskValue.CbmA || dataDetailUpdate.propertyValue == EnumTaskValue.CbmB || dataDetailUpdate.propertyValue == EnumTaskValue.CbmC || dataDetailUpdate.propertyValue == EnumTaskValue.CbmX)
                            //                {
                            //                    item.taskValue = dataDetailUpdate.propertyValue;
                            //                    item.updatedBy = JsonConvert.DeserializeObject<object>(dataDetailUpdateBy.propertyValue);
                            //                    item.updatedDate = ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerDateTime);
                            //                }

                            //            }
                            //        }
                            //    }

                            //    detailHistory.Add(currentDataUpdate[0]);
                            //}
                            //else
                            //{
                            //    int countData = 1;
                            //    foreach (var itemData in dataCount)
                            //    {
                            //        foreach (var item in itemData.items)
                            //        {
                            //            if (dataCount.Count() == countData && updateTaskRequest.updateParams.Where(x => x.keyValue.ToString() == item.key.ToString()).Count() != 0)
                            //            {
                            //                var dataUpdate = updateTaskRequest.updateParams.Where(x => x.keyValue.ToString() == item.key.ToString()).FirstOrDefault();
                            //                var dataDetailUpdate = dataUpdate.propertyParams.Where(x => x.propertyName.ToString() == "value").FirstOrDefault();

                            //                if (dataDetailUpdate.propertyValue.Contains("filename"))
                            //                {
                            //                    item.value = JsonConvert.DeserializeObject<object>(dataDetailUpdate.propertyValue);
                            //                }
                            //                else
                            //                {
                            //                    item.value = dataDetailUpdate.propertyValue;
                            //                }

                            //            }
                            //        }
                            //        countData++;
                            //    }

                            //    detailHistory = dataCount;
                            //}

                            List<PropertyParam> propertyParams = new List<PropertyParam>() {
                            new PropertyParam()
                                {
                                    propertyName = EnumQuery.CbmHistory,
                                    propertyValue = JsonConvert.SerializeObject(detailHistory)
                                }
                            };

                            List<PropertyParam> propertyParamsHeaderData = new List<PropertyParam>() {
                                new PropertyParam()
                                {
                                    propertyName = EnumQuery.CurrentValue,
                                    propertyValue = currenValue
                                },
                                new PropertyParam()
                                {
                                    propertyName = EnumQuery.CurrentRating,
                                    propertyValue = currenRating
                                }
                            };

                            UpdateRequest updateDataParams = new UpdateRequest();
                            updateDataParams.id = dataHistory.id;
                            updateDataParams.workOrder = dataHistory.workOrder;
                            updateDataParams.updateParams = new List<UpdateParam>();
                            updateDataParams.employee = updateTaskRequest.employee;

                            updateDataParams.updateParams.Add(new UpdateParam()
                            {
                                keyValue = "HISTORY",
                                propertyParams = propertyParams
                            });

                            if (_currentData[0].taskReplacement != null && (bool)_currentData[0].taskReplacement)
                            {
                                updateDataParams.updateParams.Add(new UpdateParam()
                                {
                                    keyValue = "GENERAL",
                                    propertyParams = propertyParamsHeaderData
                                });
                            }


                            var resultUpdateHeader = await _cbmHistoryRepository.Update(updateDataParams, dataHistory);
                            //var currentDataUpdate = await _repoDetailUpdate.GetDataItemsDetailValue(modelUpdate);
                        }
                        else
                        {
                            DetailServiceSheet model = new DetailServiceSheet();
                            model.workOrder = updateRequest.workOrder;
                            model.taskKey = dataTaskKeyMain;

                            var _repoDetail = new ServiceSheetDetailRepository(_connectionFactory, EnumContainer.ServiceSheetDetail);
                            var currentData = await _repoDetail.GetDataServiceSheetDetailByKey(model);

                            Dictionary<string, object> paramHeader = new Dictionary<string, object>();
                            paramHeader.Add("workOrder", model.workOrder);

                            var curentDataHeaderUpdate = await _servicesheetHeaderRepository.GetDataByParam(paramHeader);

                            if (curentDataHeaderUpdate == null)
                                throw new Exception($"Current data header work order {model.workOrder} not found");

                            CbmHistoryParam headerHisUpdate = new CbmHistoryParam();
                            headerHisUpdate.key = EnumGroup.General;
                            headerHisUpdate.workOrder = model.workOrder;
                            //headerHisUpdate.workOrder = model.taskKey;
                            headerHisUpdate.equipment = curentDataHeaderUpdate.equipment;
                            headerHisUpdate.taskKey = model.taskKey;
                            headerHisUpdate.siteId = curentDataHeaderUpdate.siteId;
                            headerHisUpdate.serviceSheetDetailId = currentData[0].id;
                            headerHisUpdate.defectHeaderId = "";
                            headerHisUpdate.detail = new DetailCBMHistory();

                            //add 2024-02-19
                            headerHisUpdate.modelId = rsc[EnumQuery.ModelId];
                            headerHisUpdate.psTypeId = rsc[EnumQuery.PsTypeId];
                            headerHisUpdate.taskDescription = currentData[0][EnumQuery.Description];
                            headerHisUpdate.category = currentData[0].category + " " + currentData[0].rating;
                            headerHisUpdate.currentValue = currentData[0][EnumQuery.MeasurementValue];
                            headerHisUpdate.currentRating = currentData[0][EnumQuery.TaskValue];
                            headerHisUpdate.replacementValue = currentData[0][EnumQuery.NonCbmAdjustmentReplacementMeasurementValue];
                            headerHisUpdate.replacementRating = currentData[0][EnumQuery.NonCbmAdjustmentReplacementRating];
                            headerHisUpdate.closedDate = updatedDate;
                            headerHisUpdate.closedBy = updateRequest.employee.id;
                            headerHisUpdate.source = "ip_overwrite";

                            foreach (var item in currentData)
                            {
                                item.Remove("id");
                                item.Remove("uom");
                                item.Remove("taskNo");
                                item.Remove("measurementValue");
                                item.Remove("nonCbmAdjustmentReplacementMeasurementValue");
                                item.Remove("nonCbmAdjustmentReplacementRating");
                                //item.Remove("cbmAdjustmentReplacementValue");
                            }

                            // Last Value
                            DetailServiceSheet modelUpdate = new DetailServiceSheet();
                            modelUpdate.workOrder = updateRequest.workOrder;
                            modelUpdate.taskKey = dataTaskKeyMain;

                            var _repoDetailUpdate = new ServiceSheetDetailRepository(_connectionFactory, EnumContainer.ServiceSheetDetail);
                            var currentDataUpdate = await _repoDetailUpdate.GetDataServiceSheetDetailByKey(modelUpdate);
                            //bool isCbmAdjustmentOrReplacement = currentData[0].cbmAdjustmentReplacement;

                            foreach (var item in currentDataUpdate)
                            {
                                item.Remove("id");
                                item.Remove("uom");
                                item.Remove("taskNo");
                                item.Remove("measurementValue");
                                item.Remove("nonCbmAdjustmentReplacementMeasurementValue");
                                item.Remove("nonCbmAdjustmentReplacementRating");
                                //item.Remove("cbmAdjustmentReplacementValue");

                                foreach (var itemDetail in item.items)
                                {
                                    if (updateTaskRequest.updateParams.Where(x => x.keyValue.ToString() == itemDetail.key.ToString()).Count() != 0)
                                    {
                                        var dataUpdate = updateTaskRequest.updateParams.Where(x => x.keyValue.ToString() == itemDetail.key.ToString()).FirstOrDefault();
                                        var dataDetailUpdate = dataUpdate.propertyParams.Where(x => x.propertyName.ToString() == "value").FirstOrDefault();
                                        var dataDetailUpdateBy = dataUpdate.propertyParams.Where(x => x.propertyName.ToString() == "updatedBy").FirstOrDefault();
                                        var dataDetailUpdateDate = dataUpdate.propertyParams.Where(x => x.propertyName.ToString() == "updatedDate").FirstOrDefault();

                                        if (dataDetailUpdate.propertyValue.Contains("filename"))
                                        {
                                            itemDetail.value = JsonConvert.DeserializeObject<object>(dataDetailUpdate.propertyValue);
                                        }
                                        else
                                        {
                                            itemDetail.value = dataDetailUpdate.propertyValue;
                                        }

                                        if (dataDetailUpdate.propertyValue == EnumTaskValue.CbmA || dataDetailUpdate.propertyValue == EnumTaskValue.CbmB || dataDetailUpdate.propertyValue == EnumTaskValue.CbmC || dataDetailUpdate.propertyValue == EnumTaskValue.CbmX)
                                        {
                                            item.taskValue = dataDetailUpdate.propertyValue;
                                            item.updatedBy = JsonConvert.DeserializeObject<object>(dataDetailUpdateBy.propertyValue);
                                            item.updatedDate = ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerDateTime);
                                        }

                                    }
                                }
                            }

                            //if (isCbmAdjustmentOrReplacement == false)
                            currentData.Add(currentDataUpdate[0]);

                            DetailCBMHistory detailHisUpdate = new DetailCBMHistory();
                            detailHisUpdate.key = "HISTORY";
                            detailHisUpdate.category = currentData[0].category;
                            detailHisUpdate.rating = currentData[0].rating;
                            detailHisUpdate.history = currentData;

                            headerHisUpdate.detail = detailHisUpdate;

                            var modelHeader = new CreateRequest();
                            modelHeader.employee = new EmployeeModel();

                            modelHeader.employee.id = updateRequest.employee.id;
                            modelHeader.employee.name = updateRequest.employee.name;
                            modelHeader.entity = headerHisUpdate;

                            var resultAddHeader = await _cbmHistoryRepository.Create(modelHeader);
                        }

                    }
                    #endregion
                }
                #endregion

                var result = await _repository.Update(updateRequest, rsc);

                #region History Revise if adjustment/replacement
                if (isCbmAdjustmentOrReplacement == true)
                {
                    #region History Revise
                    if (hasTaskValue)
                    {
                        Dictionary<string, object> paramHistory = new Dictionary<string, object>();
                        paramHistory.Add("workOrder", updateRequest.workOrder);
                        paramHistory.Add("taskKey", dataTaskKeyMain);

                        var dataHistory = await _cbmHistoryRepository.GetDataByParam(paramHistory);
                        if (dataHistory == null)
                        {

                            Dictionary<string, object> paramHeader = new Dictionary<string, object>();
                            paramHeader.Add("workOrder", _model.workOrder);

                            var curentDataHeaderUpdate = await _servicesheetHeaderRepository.GetDataByParam(paramHeader);

                            if (curentDataHeaderUpdate == null)
                                throw new Exception($"Current data header work order {_model.workOrder} not found");

                            CbmHistoryParam headerHisUpdate = new CbmHistoryParam();
                            headerHisUpdate.key = EnumGroup.General;
                            headerHisUpdate.workOrder = _model.workOrder;
                            //headerHisUpdate.workOrder = model.taskKey;
                            headerHisUpdate.equipment = curentDataHeaderUpdate.equipment;
                            headerHisUpdate.taskKey = _model.taskKey;
                            headerHisUpdate.siteId = curentDataHeaderUpdate.siteId;
                            headerHisUpdate.serviceSheetDetailId = _currentData[0].id;
                            headerHisUpdate.defectHeaderId = "";
                            headerHisUpdate.detail = new DetailCBMHistory();

                            //add 2024-02-19
                            headerHisUpdate.modelId = rsc[EnumQuery.ModelId];
                            headerHisUpdate.psTypeId = rsc[EnumQuery.PsTypeId];
                            headerHisUpdate.taskDescription = _currentData[0][EnumQuery.Description];
                            headerHisUpdate.category = _currentData[0].category + " " + _currentData[0].rating;
                            headerHisUpdate.currentValue = (bool)_currentData[EnumQuery.CbmAdjustmentReplacement] ? _currentData[EnumQuery.CurrentValue] : _currentData[0][EnumQuery.MeasurementValue];
                            headerHisUpdate.currentRating = (bool)_currentData[EnumQuery.CbmAdjustmentReplacement] ? _currentData[EnumQuery.CurrentRating] : _currentData[0][EnumQuery.TaskValue];
                            headerHisUpdate.replacementValue = (bool)_currentData[EnumQuery.CbmAdjustmentReplacement] ? _currentData[EnumQuery.ReplacementValue] : _currentData[0][EnumQuery.NonCbmAdjustmentReplacementMeasurementValue];
                            headerHisUpdate.replacementRating = (bool)_currentData[EnumQuery.CbmAdjustmentReplacement] ? _currentData[EnumQuery.ReplacementRating] : _currentData[0][EnumQuery.NonCbmAdjustmentReplacementRating];
                            headerHisUpdate.closedDate = updatedDate;
                            headerHisUpdate.closedBy = updateRequest.employee.id;
                            headerHisUpdate.source = "ip_overwrite";

                            foreach (var item in _currentData)
                            {
                                item.Remove("id");
                                item.Remove("uom");
                                item.Remove("taskNo");
                                item.Remove("measurementValue");
                                item.Remove("nonCbmAdjustmentReplacementMeasurementValue");
                                item.Remove("nonCbmAdjustmentReplacementRating");
                                //item.Remove("cbmAdjustmentReplacementValue");
                            }

                            // Last Value
                            DetailServiceSheet modelUpdate = new DetailServiceSheet();
                            modelUpdate.workOrder = updateRequest.workOrder;
                            modelUpdate.taskKey = dataTaskKeyMain;

                            var _repoDetailUpdate = new ServiceSheetDetailRepository(_connectionFactory, EnumContainer.ServiceSheetDetail);
                            var currentDataUpdate = await _repoDetailUpdate.GetDataServiceSheetDetailByKey(modelUpdate);

                            foreach (var item in currentDataUpdate)
                            {
                                item.Remove("id");
                                item.Remove("uom");
                                item.Remove("taskNo");
                                item.Remove("measurementValue");
                                item.Remove("nonCbmAdjustmentReplacementMeasurementValue");
                                item.Remove("nonCbmAdjustmentReplacementRating");
                                //item.Remove("cbmAdjustmentReplacementValue");
                            }

                            _currentData.Add(currentDataUpdate[0]);

                            DetailCBMHistory detailHisUpdate = new DetailCBMHistory();
                            detailHisUpdate.key = "HISTORY";
                            detailHisUpdate.category = _currentData[0].category;
                            detailHisUpdate.rating = _currentData[0].rating;
                            detailHisUpdate.history = _currentData;

                            headerHisUpdate.detail = detailHisUpdate;

                            var modelHeader = new CreateRequest();
                            modelHeader.employee = new EmployeeModel();

                            modelHeader.employee.id = updateRequest.employee.id;
                            modelHeader.employee.name = updateRequest.employee.name;
                            modelHeader.entity = headerHisUpdate;

                            var resultAddHeader = await _cbmHistoryRepository.Create(modelHeader);
                        }
                        else
                        {
                            var detailHistory = StaticHelper.GetPropValue(dataHistory.detail, "history");

                            List<dynamic> dataCount = JsonConvert.DeserializeObject<List<dynamic>>(JsonConvert.SerializeObject(detailHistory));

                            if (dataCount.Count() > 3)
                            {
                                return new ServiceResult
                                {
                                    Message = "Cannot revise the data, you have limit for revising 3 times.",
                                    IsError = true
                                };
                            }

                            // Last Value
                            DetailServiceSheet modelUpdate = new DetailServiceSheet();
                            modelUpdate.workOrder = updateRequest.workOrder;
                            modelUpdate.taskKey = dataTaskKeyMain;

                            var _repoDetailUpdate = new ServiceSheetDetailRepository(_connectionFactory, EnumContainer.ServiceSheetDetail);
                            var currentDataUpdate = await _repoDetailUpdate.GetDataServiceSheetDetailByKey(modelUpdate);

                            foreach (var item in currentDataUpdate)
                            {
                                item.Remove("id");
                                item.Remove("uom");
                                item.Remove("taskNo");
                                item.Remove("measurementValue");
                                item.Remove("nonCbmAdjustmentReplacementMeasurementValue");
                                item.Remove("nonCbmAdjustmentReplacementRating");
                                //item.Remove("cbmAdjustmentReplacementValue");
                            }

                            detailHistory.Add(currentDataUpdate[0]);

                            List<PropertyParam> propertyParams = new List<PropertyParam>() {
                            new PropertyParam()
                            {
                                propertyName = EnumQuery.CbmHistory,
                                propertyValue = JsonConvert.SerializeObject(detailHistory)
                            }
                    };

                            UpdateRequest updateDataParams = new UpdateRequest();
                            updateDataParams.id = dataHistory.id;
                            updateDataParams.workOrder = dataHistory.workOrder;
                            updateDataParams.updateParams = new List<UpdateParam>();
                            updateDataParams.employee = updateTaskRequest.employee;

                            updateDataParams.updateParams.Add(new UpdateParam()
                            {
                                keyValue = "HISTORY",
                                propertyParams = propertyParams
                            });

                            var resultUpdateHeader = await _cbmHistoryRepository.Update(updateDataParams, dataHistory);
                        }
                    }
                    else
                    {
                        Dictionary<string, object> paramHistory = new Dictionary<string, object>();
                        paramHistory.Add("workOrder", updateRequest.workOrder);
                        paramHistory.Add("taskKey", dataTaskKeyMain);

                        var dataHistory = await _cbmHistoryRepository.GetDataByParam(paramHistory);
                        if (dataHistory != null)
                        {

                            var detailHistory = StaticHelper.GetPropValue(dataHistory.detail, "history");

                            List<dynamic> dataCount = JsonConvert.DeserializeObject<List<dynamic>>(JsonConvert.SerializeObject(detailHistory));

                            if (dataCount.Count() > 3)
                            {
                                return new ServiceResult
                                {
                                    Message = "Cannot revise the data, you have limit for revising 3 times.",
                                    IsError = true
                                };
                            }

                            // Last Value
                            DetailServiceSheet modelUpdate = new DetailServiceSheet();
                            modelUpdate.workOrder = updateRequest.workOrder;
                            modelUpdate.taskKey = dataTaskKeyMain;

                            var _repoDetailUpdate = new ServiceSheetDetailRepository(_connectionFactory, EnumContainer.ServiceSheetDetail);
                            var currentDataUpdate = await _repoDetailUpdate.GetDataServiceSheetDetailByKey(modelUpdate);

                            foreach (var item in currentDataUpdate)
                            {
                                item.Remove("id");
                                item.Remove("uom");
                                item.Remove("taskNo");
                                item.Remove("measurementValue");
                                item.Remove("nonCbmAdjustmentReplacementMeasurementValue");
                                item.Remove("nonCbmAdjustmentReplacementRating");
                                //item.Remove("cbmAdjustmentReplacementValue");
                            }

                            detailHistory.Add(currentDataUpdate[0]);

                            List<PropertyParam> propertyParams = new List<PropertyParam>() {
                            new PropertyParam()
                                {
                                    propertyName = EnumQuery.CbmHistory,
                                    propertyValue = JsonConvert.SerializeObject(detailHistory)
                                }
                        };

                            UpdateRequest updateDataParams = new UpdateRequest();
                            updateDataParams.id = dataHistory.id;
                            updateDataParams.workOrder = dataHistory.workOrder;
                            updateDataParams.updateParams = new List<UpdateParam>();
                            updateDataParams.employee = updateTaskRequest.employee;

                            updateDataParams.updateParams.Add(new UpdateParam()
                            {
                                keyValue = "HISTORY",
                                propertyParams = propertyParams
                            });

                            var resultUpdateHeader = await _cbmHistoryRepository.Update(updateDataParams, dataHistory);
                            //var currentDataUpdate = await _repoDetailUpdate.GetDataItemsDetailValue(modelUpdate);
                        }
                        else
                        {

                            Dictionary<string, object> paramHeader = new Dictionary<string, object>();
                            paramHeader.Add("workOrder", _model.workOrder);

                            var curentDataHeaderUpdate = await _servicesheetHeaderRepository.GetDataByParam(paramHeader);

                            if (curentDataHeaderUpdate == null)
                                throw new Exception($"Current data header work order {_model.workOrder} not found");

                            CbmHistoryParam headerHisUpdate = new CbmHistoryParam();
                            headerHisUpdate.key = EnumGroup.General;
                            headerHisUpdate.workOrder = _model.workOrder;
                            //headerHisUpdate.workOrder = model.taskKey;
                            headerHisUpdate.equipment = curentDataHeaderUpdate.equipment;
                            headerHisUpdate.taskKey = _model.taskKey;
                            headerHisUpdate.siteId = curentDataHeaderUpdate.siteId;
                            headerHisUpdate.serviceSheetDetailId = _currentData[0].id;
                            headerHisUpdate.defectHeaderId = "";
                            headerHisUpdate.detail = new DetailCBMHistory();

                            //add 2024-02-19
                            headerHisUpdate.modelId = rsc[EnumQuery.ModelId];
                            headerHisUpdate.psTypeId = rsc[EnumQuery.PsTypeId];
                            headerHisUpdate.taskDescription = _currentData[0][EnumQuery.Description];
                            headerHisUpdate.category = _currentData[0].category + " " + _currentData[0].rating;
                            headerHisUpdate.currentValue = _currentData[0][EnumQuery.MeasurementValue];
                            headerHisUpdate.currentRating = _currentData[0][EnumQuery.TaskValue];
                            headerHisUpdate.replacementValue = _currentData[0][EnumQuery.NonCbmAdjustmentReplacementMeasurementValue];
                            headerHisUpdate.replacementRating = _currentData[0][EnumQuery.NonCbmAdjustmentReplacementRating];
                            headerHisUpdate.closedDate = updatedDate;
                            headerHisUpdate.closedBy = updateRequest.employee.id;
                            headerHisUpdate.source = "ip_overwrite";

                            foreach (var item in _currentData)
                            {
                                item.Remove("id");
                                item.Remove("uom");
                                item.Remove("taskNo");
                                item.Remove("measurementValue");
                                item.Remove("nonCbmAdjustmentReplacementMeasurementValue");
                                item.Remove("nonCbmAdjustmentReplacementRating");
                                //item.Remove("cbmAdjustmentReplacementValue");
                            }

                            // Last Value
                            DetailServiceSheet modelUpdate = new DetailServiceSheet();
                            modelUpdate.workOrder = updateRequest.workOrder;
                            modelUpdate.taskKey = dataTaskKeyMain;

                            var _repoDetailUpdate = new ServiceSheetDetailRepository(_connectionFactory, EnumContainer.ServiceSheetDetail);
                            var currentDataUpdate = await _repoDetailUpdate.GetDataServiceSheetDetailByKey(modelUpdate);

                            foreach (var item in currentDataUpdate)
                            {
                                item.Remove("id");
                                item.Remove("uom");
                                item.Remove("taskNo");
                                item.Remove("measurementValue");
                                item.Remove("nonCbmAdjustmentReplacementMeasurementValue");
                                item.Remove("nonCbmAdjustmentReplacementRating");
                                //item.Remove("cbmAdjustmentReplacementValue");
                            }

                            _currentData.Add(currentDataUpdate[0]);

                            DetailCBMHistory detailHisUpdate = new DetailCBMHistory();
                            detailHisUpdate.key = "HISTORY";
                            detailHisUpdate.category = _currentData[0].category;
                            detailHisUpdate.rating = _currentData[0].rating;
                            detailHisUpdate.history = _currentData;

                            headerHisUpdate.detail = detailHisUpdate;

                            var modelHeader = new CreateRequest();
                            modelHeader.employee = new EmployeeModel();

                            modelHeader.employee.id = updateRequest.employee.id;
                            modelHeader.employee.name = updateRequest.employee.name;
                            modelHeader.entity = headerHisUpdate;

                            var resultAddHeader = await _cbmHistoryRepository.Create(modelHeader);
                        }
                    }
                    #endregion
                }
                #endregion

                #region Update Header

                //var rscHeader = await _servicesheetHeaderRepository.Get(rsc.headerId.ToString());

                //string updatedDate = ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerDateTime);

                List<PropertyParam> propertyParamsHeader = new List<PropertyParam>() {
                new PropertyParam()
                    {
                        propertyName = EnumQuery.UpdatedDate,
                        propertyValue = updatedDate
                    }
                };

                UpdateRequest updateDataParamsHeader = new UpdateRequest();
                updateDataParamsHeader.id = rsc.headerId;
                updateDataParamsHeader.workOrder = rsc.workOrder;
                updateDataParamsHeader.updateParams = new List<UpdateParam>();
                updateDataParamsHeader.employee = updateTaskRequest.employee;

                updateDataParamsHeader.updateParams.Add(new UpdateParam()
                {
                    keyValue = "GENERAL",
                    propertyParams = propertyParamsHeader
                });

                var resultHeader = await _servicesheetHeaderRepository.Update(updateDataParamsHeader, header);
                #endregion

                #region Delete Ext Defect

                //await DeleteExtDefect(updateParam.keyValue, updateTaskDefectRequest.workorder, updateTaskDefectRequest.employee);

                #endregion

                return new ServiceResult
                {
                    Message = "Data updated successfully",
                    IsError = false,
                    Content = null
                };

                //ServiceResult updateResult = await GetUpdateResult(updateTaskRequest.id);

                //return updateResult;
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

        public async Task<ServiceResult> UpdateTaskInterventionRevise(UpdateTaskRequest updateTaskRequest)
        {

            var createCBMHistoryResult = await CreateCBMHistory(updateTaskRequest);

            if (!createCBMHistoryResult.IsError)
            {
                var updateResult = await UpdateTask(updateTaskRequest);
                return updateResult;
            }
            else
            {
                return createCBMHistoryResult;
            }
        }

        public async Task<ServiceResult> UpdateTaskOffline(UpdateTaskRequest updateTaskRequest)
        {
            try
            {
                #region Validation
                //_logger.LogWarning("Validation");

                #region Service Sheet Status Validation

                var header = await _servicesheetHeaderRepository.Get(updateTaskRequest.headerId);
                var status = string.Empty;

                if (_container == EnumContainer.ServiceSheetDetail)
                    status = header?.status;
                else if (_container == EnumContainer.Intervention)
                    status = header?.interventionExecution;
                else if (_container == EnumContainer.CalibrationDetail)
                    status = header?.statusCalibration;
                else if (_container == EnumContainer.InterimEngineDetail)
                    status = header?.status;

                if (status != EnumStatus.EFormOnProgress)
                    throw new Exception($"Failed to update data, service sheet status is {status}");

                #endregion

                var rsc = await _repository.Get(updateTaskRequest.id);
                string taskId = string.Empty;
                string workOrder = string.Empty;

                foreach (var updateParam in updateTaskRequest.updateParams)
                {
                    //var listProperty = updateParam.propertyParams.Select(x => x.propertyName).ToList();
                    //GetFieldValueListRequest customValidationRequest = new GetFieldValueListRequest()
                    //{
                    //    id = updateTaskRequest.id,
                    //    keyValue = updateParam.keyValue,
                    //    propertyName = listProperty
                    //};

                    //Dictionary<string, object> validation = await _repository.GetFieldValueList(customValidationRequest, true);

                    foreach (var propertyParam in updateParam.propertyParams)
                    {
                        string errMsg = await ValidationOffline(new ValidationRequest()
                        {
                            rsc = rsc,
                            //id = updateTaskRequest.id,
                            keyValue = updateParam.keyValue,
                            propertyName = propertyParam.propertyName,
                            propertyValue = propertyParam.propertyValue,
                            //employeeId = updateTaskRequest.employee.id,
                            isDefect = false,
                            headerId = updateTaskRequest.headerId,
                            workorder = updateTaskRequest.workorder,
                            employee = updateTaskRequest.employee,
                            propertyParams = updateParam.propertyParams,
                            taskKey = updateTaskRequest.taskKey
                        });

                        if (!string.IsNullOrEmpty(errMsg))
                        {
                            return new ServiceResult
                            {
                                Message = errMsg,
                                IsError = true
                            };
                        }
                    }
                }

                #endregion

                UpdateRequest updateRequest = new UpdateRequest()
                {
                    id = updateTaskRequest.id,
                    workOrder = updateTaskRequest.workorder,
                    updateParams = updateTaskRequest.updateParams,
                    employee = updateTaskRequest.employee
                };

                //_logger.LogWarning("Update Service Sheet Detail");

                #region Updated Date Validation
                var taskValueLatest = string.Empty;
                var _repoDetail = new ServiceSheetDetailRepository(_connectionFactory, EnumContainer.ServiceSheetDetail);

                foreach (var updateParam in updateTaskRequest.updateParams)
                {
                    foreach (var propertyParam in updateParam.propertyParams)
                    {
                        if (propertyParam.propertyName == EnumQuery.TaskValue || propertyParam.propertyName == EnumQuery.Value)
                        {
                            taskValueLatest = propertyParam.propertyValue;
                        }

                        if (propertyParam.propertyName == EnumCommonProperty.UpdatedDate)
                        {
                            string keyValue = updateParam.keyValue;
                            if (!string.IsNullOrEmpty(propertyParam.propertyValue) && propertyParam.propertyValue != EnumCommonProperty.ServerDateTime)
                            {
                                DateTime dateTaskValue = FormatHelper.ConvertToDateTime24(propertyParam.propertyValue);

                                DetailServiceSheet modelData = new DetailServiceSheet();
                                modelData.workOrder = updateTaskRequest.workorder;
                                modelData.taskKey = keyValue;

                                var currentData = await _repoDetail.GetDataServiceSheetDetailCurrent(modelData);

                                List<dynamic> dataJson = JsonConvert.DeserializeObject<List<dynamic>>(JsonConvert.SerializeObject(currentData));

                                bool isTaskCbm = false;

                                if (dataJson.Count > 0)
                                {
                                    var category = StaticHelper.GetPropValue(dataJson.FirstOrDefault(), EnumQuery.Category);
                                    isTaskCbm = category != null && category.ToString() == EnumCategoryServiceSheet.CBM ? true : false;
                                }

                                Dictionary<string, string> jsonInfo = new Dictionary<string, string>();
                                jsonInfo.Add("data", dataJson.ToString());
                                jsonInfo.Add("workOrder", updateTaskRequest.workorder);
                                jsonInfo.Add("dateTaskValue", dateTaskValue.ToString());


                                if (dataJson.Any())
                                {
                                    if (!string.IsNullOrEmpty(dataJson.FirstOrDefault().updatedDate.ToString()))
                                    {
                                        DateTime dateTaskValueCurrent = FormatHelper.ConvertToDateTime24(dataJson.FirstOrDefault().updatedDate.ToString());
                                        string taskKeyId = dataJson.FirstOrDefault().key.ToString();
                                        jsonInfo.Add("dateTaskValueCurrent", dateTaskValueCurrent.ToString());
                                        jsonInfo.Add("taskId", taskKeyId);
                                        _telemetryClient.TrackEvent("Data {0}", jsonInfo);

                                        if (dateTaskValueCurrent > dateTaskValue)
                                        {
                                            #region Check Defect
                                            Dictionary<string, object> paramDefectCurrent = new Dictionary<string, object>();
                                            paramDefectCurrent.Add(EnumQuery.Workorder, modelData.workOrder);
                                            paramDefectCurrent.Add(EnumQuery.TaskId, taskKeyId);

                                            var defectCurrent = await _defectHeaderRepository.GetDataByParam(paramDefectCurrent);

                                            if (defectCurrent != null && defectCurrent.taskValue.ToString() != dataJson.FirstOrDefault().taskValue.ToString())
                                            {
                                                await DeleteExtDefect(taskKeyId, modelData.workOrder, updateTaskRequest.employee);
                                                return new ServiceResult
                                                {
                                                    Message = $"Data has been updated.",
                                                    IsError = false
                                                };
                                            }
                                            #endregion

                                            return new ServiceResult
                                            {
                                                Message = $"Data already updated {dateTaskValueCurrent}.",
                                                IsError = true
                                            };
                                        }

                                        if (taskValueLatest == EnumTaskValue.NormalOK && !isTaskCbm || taskValueLatest == EnumTaskValue.CbmA || taskValueLatest == EnumTaskValue.CbmB)
                                        {
                                            Dictionary<string, object> paramDefectCurrent = new Dictionary<string, object>();
                                            paramDefectCurrent.Add(EnumQuery.Workorder, modelData.workOrder);
                                            paramDefectCurrent.Add(EnumQuery.TaskId, taskKeyId);

                                            var defectCurrent = await _defectHeaderRepository.GetDataByParam(paramDefectCurrent);

                                            if (defectCurrent != null)
                                            {
                                                await DeleteExtDefect(taskKeyId, modelData.workOrder, updateTaskRequest.employee);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                #endregion

                var result = await _repository.Update(updateRequest, rsc);

                #region Delete Ext Defect

                //await DeleteExtDefect(updateParam.keyValue, updateTaskDefectRequest.workorder, updateTaskDefectRequest.employee);

                #endregion

                return new ServiceResult
                {
                    Message = "Data updated successfully",
                    IsError = false,
                    Content = null
                };

                //ServiceResult updateResult = await GetUpdateResult(updateTaskRequest.id);

                //return updateResult;
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

        public async Task<ServiceResult> UpdateTaskWithDefectValidation(UpdateTaskDefectRequest updateTaskDefectRequest)
        {
            try
            {
                var header = await _servicesheetHeaderRepository.Get(updateTaskDefectRequest.headerId);
                var status = string.Empty;
                dynamic rsc;
                UpdateParam defectParam = new UpdateParam();

                if (_container == EnumContainer.ServiceSheetDetail)
                    status = header?.status;
                else if (_container == EnumContainer.Intervention)
                    status = header?.interventionExecution;
                else if (_container == EnumContainer.InterimEngineDetail)
                    status = header?.status;

                var Keygeneral = updateTaskDefectRequest.updateParams.Where(x => x.keyValue == EnumGroup.General).Select(x => x.keyValue).SingleOrDefault();

                if (status != EnumStatus.EFormOnProgress && Keygeneral != EnumGroup.General)
                    throw new Exception($"Failed to update data, service sheet status is {status}");

                if (status != EnumStatus.EFormOpen && Keygeneral == EnumGroup.General)
                {
                    var smu = header?.smu;
                    var hmOffset = header?.hmOffset;
                    throw new Exception(string.Format("Failed to update data, service sheet status is {0}, smu is {1}, hm offset is {2}", status, smu, hmOffset));
                }

                return new ServiceResult() { IsError = false, Message = "Data Valid" };
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

        public async Task<ServiceResult> UpdateTaskWithDefect(UpdateTaskDefectRequest updateTaskDefectRequest)
        {
            try
            {
                #region Validation
                //_logger.LogWarning("Validation");

                var header = await _servicesheetHeaderRepository.Get(updateTaskDefectRequest.headerId);
                var status = string.Empty;
                dynamic rsc;
                UpdateParam defectParam = new UpdateParam();

                if (_container == EnumContainer.ServiceSheetDetail)
                    status = header?.status;
                else if (_container == EnumContainer.Intervention)
                    status = header?.interventionExecution;
                else if (_container == EnumContainer.InterimEngineDetail)
                    status = header?.status;

                var Keygeneral = updateTaskDefectRequest.updateParams.Where(x => x.keyValue == EnumGroup.General).Select(x => x.keyValue).SingleOrDefault();

                if (status != EnumStatus.EFormOnProgress && Keygeneral != EnumGroup.General)
                    throw new Exception($"Failed to update data, service sheet status is {status}");

                if (status != EnumStatus.EFormOpen && Keygeneral == EnumGroup.General)
                {
                    var smu = header?.smu;
                    var hmOffset = header?.hmOffset;
                    throw new Exception(string.Format("Failed to update data, service sheet status is {0}, smu is {1}, hm offset is {2}", status, smu, hmOffset));
                }

                #region Updated Date Validation

                if (Keygeneral == EnumGroup.General)
                    rsc = header;
                else
                    rsc = await _repository.Get(updateTaskDefectRequest.id);


                foreach (var updateParam in updateTaskDefectRequest.updateParams)
                {
                    foreach (var propertyParam in updateParam.propertyParams)
                    {
                        string errMsg = await Validation(new ValidationRequest()
                        {
                            rsc = rsc,
                            //id = updateTaskDefectRequest.id,
                            keyValue = updateParam.keyValue,
                            propertyName = propertyParam.propertyName,
                            propertyValue = propertyParam.propertyValue,
                            //employeeId = updateTaskDefectRequest.employee.id,
                            defectHeader = updateTaskDefectRequest.defectHeader,
                            defectDetail = updateTaskDefectRequest.defectDetail,
                            isDefect = true,
                            headerId = updateTaskDefectRequest.headerId,
                            //SSdetailId = updateTaskDefectRequest.id,
                            workorder = updateTaskDefectRequest.workorder == null ? updateTaskDefectRequest.workOrder : updateTaskDefectRequest.workorder,
                            employee = updateTaskDefectRequest.employee,
                            propertyParams = updateParam.propertyParams
                        });

                        if (!string.IsNullOrEmpty(errMsg))
                        {
                            return new ServiceResult
                            {
                                Message = errMsg,
                                IsError = true
                            };
                        }

                        if (propertyParam.propertyName.ToLower() == EnumQuery.TaskValue.ToLower() ||
                            propertyParam.propertyName.ToLower() == EnumQuery.TaskValueLeak.ToLower() ||
                            propertyParam.propertyName.ToLower() == EnumQuery.TaskValueMounting.ToLower())
                            defectParam = updateParam;
                    }
                }

                var _repoDetail = new ServiceSheetDetailRepository(_connectionFactory, EnumContainer.ServiceSheetDetail);

                foreach (var updateParam in updateTaskDefectRequest.updateParams)
                {
                    foreach (var propertyParam in updateParam.propertyParams)
                    {
                        if (propertyParam.propertyName == EnumCommonProperty.UpdatedDate)
                        {
                            string keyValue = updateParam.keyValue;
                            if (!string.IsNullOrEmpty(propertyParam.propertyValue) && propertyParam.propertyValue != EnumCommonProperty.ServerDateTime)
                            {
                                DateTime dateTaskValue = FormatHelper.ConvertToDateTime24(propertyParam.propertyValue);

                                DetailServiceSheet modelData = new DetailServiceSheet();
                                modelData.workOrder = updateTaskDefectRequest.workorder;
                                modelData.taskKey = keyValue;

                                var currentData = await _repoDetail.GetDataServiceSheetDetailByKey(modelData);

                                List<dynamic> dataJson = JsonConvert.DeserializeObject<List<dynamic>>(JsonConvert.SerializeObject(currentData));

                                if (dataJson.Any())
                                {
                                    if (!string.IsNullOrEmpty(dataJson.FirstOrDefault().updatedDate.ToString()))
                                    {
                                        DateTime dateTaskValueCurrent = FormatHelper.ConvertToDateTime24(dataJson.FirstOrDefault().updatedDate.ToString());

                                        if (dateTaskValueCurrent > dateTaskValue)
                                        {
                                            return new ServiceResult
                                            {
                                                Message = $"Data already updated {dateTaskValueCurrent}.",
                                                IsError = true
                                            };
                                        }

                                        await DeleteExtDefect(updateParam.keyValue, updateTaskDefectRequest.workorder, updateTaskDefectRequest.employee);
                                    }
                                }
                            }
                        }
                    }
                }

                #endregion

                #endregion

                #region Defect

                if (defectParam != null)
                {
                    #region Delete Ext Defect

                    //await DeleteExtDefect(updateParam.keyValue, updateTaskDefectRequest.workorder, updateTaskDefectRequest.employee);

                    #endregion

                    #region Create Defect Header
                    //_logger.LogWarning("Update Defect Header");

                    updateTaskDefectRequest.defectHeader.taskId = Keygeneral == EnumGroup.General ? updateTaskDefectRequest.headerId : defectParam.keyValue;
                    updateTaskDefectRequest.defectHeader.statusHistory = new List<StatusHistoryModel>() {
                        new StatusHistoryModel(){
                            status = EnumStatus.DefectSubmit,
                            updatedBy = updateTaskDefectRequest.employee,
                            tsUpdatedDate = ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerDateTime),
                            updatedDate = ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerTimeStamp)
                        }
                    };
                    if (Keygeneral == EnumGroup.General)
                        updateTaskDefectRequest.defectHeader.defectType = EnumTaskType.MachineSMU;

                    CreateRequest createHeaderRequest = new CreateRequest()
                    {
                        employee = updateTaskDefectRequest.employee,
                        entity = updateTaskDefectRequest.defectHeader
                    };

                    Dictionary<string, string> adjustHeaderFields = new Dictionary<string, string>();
                    adjustHeaderFields.Add(EnumQuery.Key, Guid.NewGuid().ToString());

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

                    #region Create Defect Intervention EHMS

                    if (_container == EnumContainer.Intervention)
                    {
                        await CreateInterventionDefectEHMS(updateTaskDefectRequest.defectHeader.interventionHeaderId, updateTaskDefectRequest.defectHeader.taskId, updateTaskDefectRequest.employee.id);
                    }

                    #endregion

                    #endregion

                    #region Create Defect Detail
                    //_logger.LogWarning("Update Defect Detail");

                    if (updateTaskDefectRequest.defectDetail != null)
                    {

                        string defectDetailString = JsonConvert.SerializeObject(updateTaskDefectRequest.defectDetail);
                        defectDetailString = defectDetailString.Replace(EnumCommonProperty.ServerDateTime, ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerDateTime));
                        defectDetailString = defectDetailString.Replace(EnumCommonProperty.ServerTimeStamp, ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerTimeStamp));
                        JObject defectDetail = JObject.Parse(defectDetailString);
                        defectDetail.Add(EnumQuery.Key, Guid.NewGuid().ToString());
                        if (Keygeneral == EnumGroup.General)
                        {
                            defectDetail.Add(EnumQuery.Type, EnumTaskType.MachineSMU);
                            defectDetail.Add(EnumQuery.Reason, string.Empty);
                        }

                        JObject detailObject = new JObject();
                        detailObject.Add(EnumQuery.Detail, defectDetail);

                        CreateRequest createDetailRequest = new CreateRequest()
                        {
                            employee = updateTaskDefectRequest.employee,
                            entity = detailObject
                        };

                        Dictionary<string, string> adjustDetailFields = new Dictionary<string, string>();
                        adjustDetailFields.Add(EnumQuery.Key, Guid.NewGuid().ToString());
                        adjustDetailFields.Add(EnumQuery.Workorder, updateTaskDefectRequest.defectHeader.workorder);
                        adjustDetailFields.Add(EnumQuery.DefectHeaderId, defectHeaderId);
                        adjustDetailFields.Add(EnumQuery.ServicesheetDetailId, updateTaskDefectRequest.defectHeader.serviceSheetDetailId);
                        adjustDetailFields.Add(EnumQuery.InterventionId, updateTaskDefectRequest.defectHeader.interventionId);
                        adjustDetailFields.Add(EnumQuery.InterventionHeaderId, updateTaskDefectRequest.defectHeader.interventionHeaderId);
                        adjustDetailFields.Add(EnumQuery.TaskId, updateTaskDefectRequest.defectHeader.taskId);

                        var defectDetailResult = await _defectDetailRepository.Create(createDetailRequest, adjustDetailFields);
                        updateTaskDefectRequest.defectDetail = defectDetail;
                    }

                    #endregion
                }

                #endregion
                if (Keygeneral != EnumGroup.General)
                {
                    UpdateRequest updateRequest = new UpdateRequest()
                    {
                        id = updateTaskDefectRequest.id,
                        workOrder = updateTaskDefectRequest.workorder == null ? updateTaskDefectRequest.workOrder : updateTaskDefectRequest.workorder,
                        updateParams = updateTaskDefectRequest.updateParams,
                        employee = updateTaskDefectRequest.employee
                    };

                    //_logger.LogWarning("Update Service Sheet Detail");

                    var result = await _repository.Update(updateRequest, rsc);
                }

                return new ServiceResult
                {
                    Message = "Data updated successfully",
                    IsError = false,
                    Content = updateTaskDefectRequest
                };

                //ServiceResult updateResult = await GetUpdateResult(updateTaskDefectRequest.id);

                //return updateResult;
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

        public async Task<ServiceResult> UpdateTaskWithDefectMultiple(UpdateTaskDefectRequest updateTaskDefectRequest)
        {
            try
            {
                #region Validation
                //_logger.LogWarning("Validation");

                var header = await _servicesheetHeaderRepository.Get(updateTaskDefectRequest.headerId);
                var status = string.Empty;

                if (_container == EnumContainer.ServiceSheetDetail)
                    status = header?.status;
                else if (_container == EnumContainer.Intervention)
                    status = header?.interventionExecution;
                else if (_container == EnumContainer.InterimEngineDetail)
                    status = header?.status;

                if (status != EnumStatus.EFormOnProgress)
                    throw new Exception($"Failed to update data, service sheet status is {status}");

                UpdateParam defectParam = new UpdateParam();
                var rsc = await _repository.Get(updateTaskDefectRequest.id);

                foreach (var updateParam in updateTaskDefectRequest.updateParams)
                {
                    foreach (var propertyParam in updateParam.propertyParams)
                    {
                        string errMsg = await ValidationMultiple(new ValidationRequest()
                        {
                            rsc = rsc,
                            //id = updateTaskDefectRequest.id,
                            keyValue = updateParam.keyValue,
                            propertyName = propertyParam.propertyName,
                            propertyValue = propertyParam.propertyValue,
                            //employeeId = updateTaskDefectRequest.employee.id,
                            defectHeader = updateTaskDefectRequest.defectHeader,
                            defectDetail = updateTaskDefectRequest.defectDetail,
                            isDefect = true,
                            headerId = updateTaskDefectRequest.headerId,
                            workorder = updateTaskDefectRequest.workorder == null ? updateTaskDefectRequest.workOrder : updateTaskDefectRequest.workorder,
                            //SSdetailId = updateTaskDefectRequest.id,
                            employee = updateTaskDefectRequest.employee,
                            propertyParams = updateParam.propertyParams
                        });

                        if (!string.IsNullOrEmpty(errMsg))
                        {
                            return new ServiceResult
                            {
                                Message = errMsg,
                                IsError = true
                            };
                        }

                        if (propertyParam.propertyName.ToLower() == EnumQuery.TaskValue.ToLower() ||
                            propertyParam.propertyName.ToLower() == EnumQuery.TaskValueLeak.ToLower() ||
                            propertyParam.propertyName.ToLower() == EnumQuery.TaskValueMounting.ToLower())
                            defectParam = updateParam;
                    }
                }

                #endregion

                #region Updated Date Validation
                var _repoDetail = new ServiceSheetDetailRepository(_connectionFactory, EnumContainer.ServiceSheetDetail);

                foreach (var updateParam in updateTaskDefectRequest.updateParams)
                {
                    string keyValue = updateParam.keyValue;

                    var propertyParams = updateParam.propertyParams.ToDictionary(param => param.propertyName, param => param.propertyValue);

                    var updatedDate = propertyParams[EnumCommonProperty.UpdatedDate];

                    if (!string.IsNullOrEmpty(updatedDate) && updatedDate != EnumCommonProperty.ServerDateTime)
                    {
                        DateTime dateTaskValue = FormatHelper.ConvertToDateTime24(updatedDate);
                        DetailServiceSheet modelData = new DetailServiceSheet();
                        modelData.workOrder = updateTaskDefectRequest.workorder;
                        modelData.taskKey = keyValue;

                        var currentData = await _repoDetail.GetDataServiceSheetDetailByKey(modelData);

                        List<dynamic> dataJson = JsonConvert.DeserializeObject<List<dynamic>>(JsonConvert.SerializeObject(currentData));

                        if (dataJson.Any())
                        {
                            if (!string.IsNullOrEmpty(dataJson.FirstOrDefault().updatedDate.ToString()))
                            {
                                DateTime dateTaskValueCurrent = FormatHelper.ConvertToDateTime24(dataJson.FirstOrDefault().updatedDate.ToString());

                                if (dateTaskValueCurrent > dateTaskValue)
                                {
                                    return new ServiceResult
                                    {
                                        Message = $"Data already updated {dateTaskValueCurrent}.",
                                        IsError = true
                                    };
                                }

                                Dictionary<string, object> paramDefectCurrent = new Dictionary<string, object>();
                                paramDefectCurrent.Add(EnumQuery.Workorder, modelData.workOrder);
                                paramDefectCurrent.Add(EnumQuery.TaskId, keyValue);

                                var defectCurrent = await _defectHeaderRepository.GetDataByParam(paramDefectCurrent);

                                var newValue = propertyParams[EnumQuery.TaskValue].ToString();
                                if (defectCurrent != null && !string.IsNullOrEmpty(newValue) && defectCurrent.taskValue.ToString() != newValue)
                                {
                                    await DeleteExtDefect(updateParam.keyValue, updateTaskDefectRequest.workorder, updateTaskDefectRequest.employee);
                                }
                            }
                        }
                    }
                }

                #endregion

                #region Defect

                if (defectParam != null)
                {
                    #region Delete Ext Defect

                    //await DeleteExtDefect(updateParam.keyValue, updateTaskDefectRequest.workorder, updateTaskDefectRequest.employee);

                    #endregion

                    #region Create Defect Header
                    //_logger.LogWarning("Update Defect Header");

                    updateTaskDefectRequest.defectHeader.taskId = defectParam.keyValue;
                    if (updateTaskDefectRequest.defectHeader.defectHeaderId != null && updateTaskDefectRequest.defectHeader.defectDetailId != null)
                    {
                        updateTaskDefectRequest.defectHeader.id = updateTaskDefectRequest.defectHeader.defectHeaderId;
                    }
                    updateTaskDefectRequest.defectHeader.statusHistory = new List<StatusHistoryModel>() {
                        new StatusHistoryModel(){
                            status = EnumStatus.DefectSubmit,
                            updatedBy = updateTaskDefectRequest.employee,
                            tsUpdatedDate = ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerDateTime),
                            updatedDate = ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerTimeStamp)
                        }
                    };

                    CreateRequest createHeaderRequest = new CreateRequest()
                    {
                        employee = updateTaskDefectRequest.employee,
                        entity = updateTaskDefectRequest.defectHeader
                    };

                    Dictionary<string, string> adjustHeaderFields = new Dictionary<string, string>();
                    adjustHeaderFields.Add(EnumQuery.Key, Guid.NewGuid().ToString());

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

                    #region Create Defect Intervention EHMS

                    if (_container == EnumContainer.Intervention)
                    {
                        await CreateInterventionDefectEHMS(updateTaskDefectRequest.defectHeader.interventionHeaderId, updateTaskDefectRequest.defectHeader.taskId, updateTaskDefectRequest.employee.id);
                    }

                    #endregion

                    #endregion

                    #region Create Defect Detail
                    //_logger.LogWarning("Update Defect Detail");

                    if (updateTaskDefectRequest.defectDetail != null)
                    {
                        string defectDetailString = JsonConvert.SerializeObject(updateTaskDefectRequest.defectDetail);
                        defectDetailString = defectDetailString.Replace(EnumCommonProperty.ServerDateTime, ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerDateTime));
                        defectDetailString = defectDetailString.Replace(EnumCommonProperty.ServerTimeStamp, ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerTimeStamp));
                        JObject defectDetail = JObject.Parse(defectDetailString);
                        defectDetail.Add(EnumQuery.Key, Guid.NewGuid().ToString());

                        JObject detailObject = new JObject();
                        detailObject.Add(EnumQuery.Detail, defectDetail);

                        CreateRequest createDetailRequest = new CreateRequest()
                        {
                            employee = updateTaskDefectRequest.employee,
                            entity = detailObject
                        };

                        Dictionary<string, string> adjustDetailFields = new Dictionary<string, string>();
                        adjustDetailFields.Add(EnumQuery.Key, Guid.NewGuid().ToString());
                        adjustDetailFields.Add(EnumQuery.Workorder, updateTaskDefectRequest.defectHeader.workorder);
                        adjustDetailFields.Add(EnumQuery.DefectHeaderId, defectHeaderId);
                        adjustDetailFields.Add(EnumQuery.ServicesheetDetailId, updateTaskDefectRequest.defectHeader.serviceSheetDetailId);
                        adjustDetailFields.Add(EnumQuery.InterventionId, updateTaskDefectRequest.defectHeader.interventionId);
                        adjustDetailFields.Add(EnumQuery.InterventionHeaderId, updateTaskDefectRequest.defectHeader.interventionHeaderId);
                        adjustDetailFields.Add(EnumQuery.TaskId, updateTaskDefectRequest.defectHeader.taskId);
                        if (updateTaskDefectRequest.defectHeader.defectHeaderId != null && updateTaskDefectRequest.defectHeader.defectDetailId != null)
                        {
                            adjustDetailFields.Add(EnumQuery.ID, updateTaskDefectRequest.defectHeader.defectDetailId.ToString());
                        }

                        var defectDetailResult = await _defectDetailRepository.Create(createDetailRequest, adjustDetailFields);
                        updateTaskDefectRequest.defectDetail = defectDetail;
                    }

                    #endregion
                }

                #endregion

                UpdateRequest updateRequest = new UpdateRequest()
                {
                    id = updateTaskDefectRequest.id,
                    workOrder = updateTaskDefectRequest.workorder == null ? updateTaskDefectRequest.workOrder : updateTaskDefectRequest.workorder,
                    updateParams = updateTaskDefectRequest.updateParams,
                    employee = updateTaskDefectRequest.employee
                };

                //_logger.LogWarning("Update Service Sheet Detail");

                var result = await _repository.Update(updateRequest, rsc);

                return new ServiceResult
                {
                    Message = "Data updated successfully",
                    IsError = false,
                    Content = updateTaskDefectRequest
                };

                //ServiceResult updateResult = await GetUpdateResult(updateTaskDefectRequest.id);

                //return updateResult;
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

        public async Task<ServiceResult> UpdateTaskWithDefectRevise(UpdateTaskDefectReviseRequest updateTaskDefectRequest)
        {
            try
            {
                #region Validation
                //_logger.LogWarning("Validation");

                var header = await _servicesheetHeaderRepository.Get(updateTaskDefectRequest.headerId);
                var status = string.Empty;
                string dataTaskKeyMain = updateTaskDefectRequest.taskKey == null ? updateTaskDefectRequest.defectHeader.taskId : updateTaskDefectRequest.taskKey;
                bool hasTaskValue = false;
                string taskValueData = string.Empty;

                //if (_container == EnumContainer.ServiceSheetDetail)
                //    status = header?.status;
                //else if (_container == EnumContainer.Intervention)
                //    status = header?.interventionExecution;
                //else if (_container == EnumContainer.InterimEngineDetail)
                //    status = header?.status;

                //if (status != EnumStatus.EFormOnProgress)
                //    throw new Exception($"Failed to update data, service sheet status is {status}");

                UpdateParam defectParam = new UpdateParam();
                var rsc = await _repository.Get(updateTaskDefectRequest.id);

                foreach (var updateParam in updateTaskDefectRequest.updateParams)
                {
                    foreach (var propertyParam in updateParam.propertyParams)
                    {
                        if (propertyParam.propertyName.ToLower() == EnumQuery.TaskValue.ToLower())
                        {
                            taskValueData = propertyParam.propertyValue;
                            hasTaskValue = true;
                        }

                        string errMsg = await Validation(new ValidationRequest()
                        {
                            rsc = rsc,
                            //id = updateTaskDefectRequest.id,
                            keyValue = updateParam.keyValue,
                            propertyName = propertyParam.propertyName,
                            propertyValue = propertyParam.propertyValue,
                            //employeeId = updateTaskDefectRequest.employee.id,
                            defectHeader = updateTaskDefectRequest.defectHeader,
                            defectDetail = updateTaskDefectRequest.defectDetail,
                            isDefect = true,
                            headerId = updateTaskDefectRequest.headerId,
                            workorder = updateTaskDefectRequest.workorder == null ? updateTaskDefectRequest.workOrder : updateTaskDefectRequest.workorder,
                            employee = updateTaskDefectRequest.employee
                        });

                        if (!string.IsNullOrEmpty(errMsg))
                        {
                            return new ServiceResult
                            {
                                Message = errMsg,
                                IsError = true
                            };
                        }

                        if (propertyParam.propertyName.ToLower() == EnumQuery.TaskValue.ToLower())
                            defectParam = updateParam;
                    }
                }

                #endregion

                #region Defect

                if (defectParam != null)
                {
                    #region Delete Ext Defect

                    await DeleteExtDefect(defectParam.keyValue, updateTaskDefectRequest.workorder, updateTaskDefectRequest.employee);

                    #endregion

                    #region Create Defect Header
                    //_logger.LogWarning("Update Defect Header");

                    updateTaskDefectRequest.defectHeader.taskId = defectParam.keyValue;
                    updateTaskDefectRequest.defectHeader.statusHistory = new List<StatusHistoryModel>() {
                        new StatusHistoryModel(){
                            status = EnumStatus.DefectSubmit,
                            updatedBy = updateTaskDefectRequest.employee,
                            tsUpdatedDate = ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerDateTime),
                            updatedDate = ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerTimeStamp)
                        }
                    };

                    CreateRequest createHeaderRequest = new CreateRequest()
                    {
                        employee = updateTaskDefectRequest.employee,
                        entity = updateTaskDefectRequest.defectHeader
                    };

                    Dictionary<string, string> adjustHeaderFields = new Dictionary<string, string>();
                    adjustHeaderFields.Add(EnumQuery.Key, Guid.NewGuid().ToString());

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

                    #region Create Defect Intervention EHMS

                    //if (_container == EnumContainer.Intervention)
                    //{
                    //    await CreateInterventionDefectEHMS(updateTaskDefectRequest.defectHeader.interventionHeaderId, updateTaskDefectRequest.defectHeader.taskId, updateTaskDefectRequest.employee.id);
                    //}

                    #endregion

                    #endregion

                    #region Create Defect Detail
                    //_logger.LogWarning("Update Defect Detail");

                    if (updateTaskDefectRequest.defectDetail != null)
                    {
                        string defectDetailString = JsonConvert.SerializeObject(updateTaskDefectRequest.defectDetail);
                        defectDetailString = defectDetailString.Replace(EnumCommonProperty.ServerDateTime, ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerDateTime));
                        defectDetailString = defectDetailString.Replace(EnumCommonProperty.ServerTimeStamp, ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerTimeStamp));
                        JObject defectDetail = JObject.Parse(defectDetailString);
                        defectDetail.Add(EnumQuery.Key, Guid.NewGuid().ToString());

                        JObject detailObject = new JObject();
                        detailObject.Add(EnumQuery.Detail, defectDetail);

                        CreateRequest createDetailRequest = new CreateRequest()
                        {
                            employee = updateTaskDefectRequest.employee,
                            entity = detailObject
                        };

                        Dictionary<string, string> adjustDetailFields = new Dictionary<string, string>();
                        adjustDetailFields.Add(EnumQuery.Key, Guid.NewGuid().ToString());
                        adjustDetailFields.Add(EnumQuery.Workorder, updateTaskDefectRequest.defectHeader.workorder);
                        adjustDetailFields.Add(EnumQuery.DefectHeaderId, defectHeaderId);
                        adjustDetailFields.Add(EnumQuery.ServicesheetDetailId, updateTaskDefectRequest.defectHeader.serviceSheetDetailId);
                        adjustDetailFields.Add(EnumQuery.InterventionId, updateTaskDefectRequest.defectHeader.interventionId);
                        adjustDetailFields.Add(EnumQuery.InterventionHeaderId, updateTaskDefectRequest.defectHeader.interventionHeaderId);
                        adjustDetailFields.Add(EnumQuery.TaskId, updateTaskDefectRequest.defectHeader.taskId);

                        var defectDetailResult = await _defectDetailRepository.Create(createDetailRequest, adjustDetailFields);
                        updateTaskDefectRequest.defectDetail = defectDetail;
                    }

                    #endregion
                }

                #endregion

                UpdateRequest updateRequest = new UpdateRequest()
                {
                    id = updateTaskDefectRequest.id,
                    workOrder = updateTaskDefectRequest.workorder == null ? updateTaskDefectRequest.workOrder : updateTaskDefectRequest.workorder,
                    updateParams = updateTaskDefectRequest.updateParams,
                    employee = updateTaskDefectRequest.employee
                };

                #region History Revise
                //if (hasTaskValue)
                //{
                //    Dictionary<string, object> paramHistory = new Dictionary<string, object>();
                //    paramHistory.Add("workOrder", updateRequest.workOrder);
                //    paramHistory.Add("taskKey", dataTaskKeyMain);

                //    var dataHistory = await _cbmHistoryRepository.GetDataByParam(paramHistory);
                //    if (dataHistory == null)
                //    {
                //        DetailServiceSheet model = new DetailServiceSheet();
                //        model.workOrder = updateRequest.workOrder;
                //        model.taskKey = dataTaskKeyMain;

                //        var _repoDetail = new ServiceSheetDetailRepository(_connectionFactory, EnumContainer.ServiceSheetDetail);
                //        var currentData = await _repoDetail.GetDataServiceSheetDetailByKey(model);

                //        Dictionary<string, object> paramHeader = new Dictionary<string, object>();
                //        paramHeader.Add("workOrder", model.workOrder);

                //        var curentDataHeaderUpdate = await _servicesheetHeaderRepository.GetDataByParam(paramHeader);

                //        if (curentDataHeaderUpdate == null)
                //            throw new Exception($"Current data header work order {model.workOrder} not found");

                //        CbmHistoryParam headerHisUpdate = new CbmHistoryParam();
                //        headerHisUpdate.workOrder = model.workOrder;
                //        headerHisUpdate.equipment = curentDataHeaderUpdate.equipment;
                //        headerHisUpdate.taskKey = model.taskKey;
                //        headerHisUpdate.siteId = curentDataHeaderUpdate.siteId;
                //        headerHisUpdate.serviceSheetDetailId = currentData[0].id;
                //        headerHisUpdate.defectHeaderId = "";
                //        headerHisUpdate.detail = new DetailCBMHistory();

                //        foreach (var item in currentData)
                //        {
                //            item.Remove("id");
                //            item.Remove("uom");
                //            item.Remove("taskNo");
                //            item.Remove("measurementValue");
                //        }

                //        // Last Value
                //        //DetailServiceSheet modelUpdate = new DetailServiceSheet();
                //        //modelUpdate.workOrder = updateRequest.workOrder;
                //        //modelUpdate.taskKey = dataTaskKeyMain;

                //        //var _repoDetailUpdate = new ServiceSheetDetailRepository(_connectionFactory, EnumContainer.ServiceSheetDetail);
                //        //var currentDataUpdate = await _repoDetailUpdate.GetDataServiceSheetDetailByKey(modelUpdate);

                //        foreach (var item in currentData)
                //        {
                //            item.Remove("id");
                //            item.Remove("uom");
                //            item.Remove("taskNo");
                //            item.Remove("measurementValue");

                //            foreach (var itemDetail in item.items)
                //            {
                //                //if (updateTaskDefectRequest.updateParams.Where(x => x.keyValue.ToString() == itemDetail.key.ToString()).Count() != 0)
                //                if (itemDetail.isTaskValue != null && itemDetail.isTaskValue == true)
                //                {
                //                    //var dataUpdate = updateTaskDefectRequest.updateParams.Where(x => x.keyValue.ToString() == itemDetail.key.ToString()).FirstOrDefault();
                //                    //var dataDetailUpdate = dataUpdate.propertyParams.Where(x => x.propertyName.ToString() == "value").FirstOrDefault();

                //                    //var dataDetailUpdateBy = dataUpdate.propertyParams.Where(x => x.propertyName.ToString() == "updatedBy").FirstOrDefault();
                //                    //var dataDetailUpdateDate = dataUpdate.propertyParams.Where(x => x.propertyName.ToString() == "updatedDate").FirstOrDefault();

                //                    itemDetail.value = taskValueData;

                //                    if (taskValueData == "A" || taskValueData == "B" || taskValueData == "C" || taskValueData == "X")
                //                    {
                //                        item.taskValue = taskValueData;
                //                        item.updatedBy = JsonConvert.DeserializeObject<object>(JsonConvert.SerializeObject(updateTaskDefectRequest.employee));
                //                        item.updatedDate = ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerDateTime);
                //                    }

                //                }
                //            }
                //        }

                //        //currentData.Add(currentDataUpdate[0]);

                //        DetailCBMHistory detailHisUpdate = new DetailCBMHistory();
                //        detailHisUpdate.key = "HISTORY";
                //        detailHisUpdate.category = currentData[0].category;
                //        detailHisUpdate.rating = currentData[0].rating;
                //        detailHisUpdate.history = currentData;

                //        headerHisUpdate.detail = detailHisUpdate;

                //        var modelHeader = new CreateRequest();
                //        modelHeader.employee = new EmployeeModel();

                //        modelHeader.employee.id = updateRequest.employee.id;
                //        modelHeader.employee.name = updateRequest.employee.name;
                //        modelHeader.entity = headerHisUpdate;

                //        var resultAddHeader = await _cbmHistoryRepository.Create(modelHeader);
                //    }
                //    else
                //    {
                //        var detailHistory = StaticHelper.GetPropValue(dataHistory.detail, "history");

                //        // Last Value
                //        DetailServiceSheet modelUpdate = new DetailServiceSheet();
                //        modelUpdate.workOrder = updateRequest.workOrder;
                //        modelUpdate.taskKey = dataTaskKeyMain;

                //        var _repoDetailUpdate = new ServiceSheetDetailRepository(_connectionFactory, EnumContainer.ServiceSheetDetail);
                //        var currentDataUpdate = await _repoDetailUpdate.GetDataServiceSheetDetailByKey(modelUpdate);

                //        foreach (var item in currentDataUpdate)
                //        {
                //            item.Remove("id");
                //            item.Remove("uom");
                //            item.Remove("taskNo");
                //            item.Remove("measurementValue");

                //            foreach (var itemDetail in item.items)
                //            {
                //                //if (updateTaskDefectRequest.updateParams.Where(x => x.keyValue.ToString() == itemDetail.key.ToString()).Count() != 0)
                //                if (itemDetail.isTaskValue != null && itemDetail.isTaskValue == true)
                //                {
                //                    //var dataUpdate = updateTaskDefectRequest.updateParams.Where(x => x.keyValue.ToString() == itemDetail.key.ToString()).FirstOrDefault();
                //                    //var dataDetailUpdate = dataUpdate.propertyParams.Where(x => x.propertyName.ToString() == "value").FirstOrDefault();

                //                    itemDetail.value = taskValueData;

                //                    if (taskValueData == "A" || taskValueData == "B" || taskValueData == "C" || taskValueData == "X")
                //                    {
                //                        item.taskValue = taskValueData;
                //                        item.updatedBy = JsonConvert.DeserializeObject<object>(JsonConvert.SerializeObject(updateTaskDefectRequest.employee));
                //                        item.updatedDate = ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerDateTime);
                //                    }

                //                }
                //            }
                //        }

                //        detailHistory.Add(currentDataUpdate[0]);

                //        List<PropertyParam> propertyParams = new List<PropertyParam>() {
                //        new PropertyParam()
                //        {
                //            propertyName = EnumQuery.CbmHistory,
                //            propertyValue = JsonConvert.SerializeObject(detailHistory)
                //        }
                //    };

                //        UpdateRequest updateDataParams = new UpdateRequest();
                //        updateDataParams.id = dataHistory.id;
                //        updateDataParams.workOrder = dataHistory.workOrder;
                //        updateDataParams.updateParams = new List<UpdateParam>();
                //        updateDataParams.employee = updateTaskDefectRequest.employee;

                //        updateDataParams.updateParams.Add(new UpdateParam()
                //        {
                //            keyValue = "HISTORY",
                //            propertyParams = propertyParams
                //        });

                //        var resultUpdateHeader = await _cbmHistoryRepository.Update(updateDataParams, dataHistory);
                //    }
                //}
                #endregion

                //_logger.LogWarning("Update Service Sheet Detail");

                var result = await _repository.Update(updateRequest, rsc);

                return new ServiceResult
                {
                    Message = "Data updated successfully",
                    IsError = false,
                    Content = updateTaskDefectRequest
                };

                //ServiceResult updateResult = await GetUpdateResult(updateTaskDefectRequest.id);

                //return updateResult;
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

        public async Task<ServiceResult> UpdateTaskWithDefectInterventionRevise(UpdateTaskDefectRequest updateTaskDefectRequest)
        {
            var createCBMHistoryResult = await CreateCBMHistory(updateTaskDefectRequest);

            if (!createCBMHistoryResult.IsError)
            {
                var updateResult = await UpdateTaskWithDefect(updateTaskDefectRequest);
                return updateResult;
            }
            else
            {
                return createCBMHistoryResult;
            }
        }

        public async Task<ServiceResult> UpdateTaskGeneralForm(UpdateTaskRequest updateTaskRequest)
        {
            try
            {
                #region Validation
                //_logger.LogWarning("Validation");

                #region Service Sheet Status Validation

                var header = await _servicesheetHeaderRepository.Get(updateTaskRequest.id);
                var status = header?.status;

                //if (status != EnumStatus.EFormOnProgress)
                //    throw new Exception($"Failed to update data, service sheet status is {status}");

                #endregion

                var rsc = await _repository.Get(updateTaskRequest.id);
                string taskId = string.Empty;
                string workOrder = string.Empty;

                foreach (var updateParam in updateTaskRequest.updateParams)
                {
                    //var listProperty = updateParam.propertyParams.Select(x => x.propertyName).ToList();
                    //GetFieldValueListRequest customValidationRequest = new GetFieldValueListRequest()
                    //{
                    //    id = updateTaskRequest.id,
                    //    keyValue = updateParam.keyValue,
                    //    propertyName = listProperty
                    //};

                    //Dictionary<string, object> validation = await _repository.GetFieldValueList(customValidationRequest, true);

                    foreach (var propertyParam in updateParam.propertyParams)
                    {
                        string errMsg = await ValidationGeneralForm(new ValidationRequest()
                        {
                            rsc = rsc,
                            //id = updateTaskRequest.id,
                            keyValue = updateParam.keyValue,
                            propertyName = propertyParam.propertyName,
                            propertyValue = propertyParam.propertyValue,
                            //employeeId = updateTaskRequest.employee.id,
                            isDefect = false,
                            headerId = updateTaskRequest.headerId,
                            workorder = updateTaskRequest.workorder,
                            employee = updateTaskRequest.employee
                        });

                        if (!string.IsNullOrEmpty(errMsg))
                        {
                            List<UpdateParam> resultCheckBeforTruck = new List<UpdateParam>();

                            List<dynamic> oldCheckBeforeTruck = JsonConvert.DeserializeObject<List<dynamic>>(JsonConvert.SerializeObject(header.checkBeforeTruck.items));
                            foreach (var itemBeforTruck in oldCheckBeforeTruck)
                            {

                                var resulTruck = new Dictionary<string, object>();
                                resulTruck.Add("keyValue", itemBeforTruck.key);

                                var resultItemTruck = new List<Dictionary<string, object>>();

                                var valueResult = new Dictionary<string, object>();
                                valueResult.Add("propertyName", "value");
                                valueResult.Add("propertyValue", itemBeforTruck.value);

                                var updateDateResult = new Dictionary<string, object>();
                                updateDateResult.Add("propertyName", "updatedDate");
                                updateDateResult.Add("propertyValue", itemBeforTruck.updatedDate);

                                var updatedByResult = new Dictionary<string, object>();
                                updatedByResult.Add("propertyName", "updatedBy");
                                updatedByResult.Add("propertyValue", JsonConvert.SerializeObject(itemBeforTruck.updatedBy));

                                resultItemTruck.Add(valueResult);
                                resultItemTruck.Add(updateDateResult);
                                resultItemTruck.Add(updatedByResult);

                                resulTruck.Add("propertyParams", resultItemTruck);

                                UpdateParam resultParam = JsonConvert.DeserializeObject<UpdateParam>(JsonConvert.SerializeObject(resulTruck));

                                resultCheckBeforTruck.Add(resultParam);
                            }

                            GetFieldValueRequest updatedByRequest = new GetFieldValueRequest()
                            {
                                id = updateTaskRequest.id,
                                keyValue = updateTaskRequest.updateParams[0].keyValue,
                                propertyName = $"{EnumQuery.UpdatedBy}"
                            };

                            var updatedBy = await _repository.GetFieldValue(updatedByRequest, true);

                            GetFieldValueRequest updatedByRequestDate = new GetFieldValueRequest()
                            {
                                id = updateTaskRequest.id,
                                keyValue = updateTaskRequest.updateParams[0].keyValue,
                                propertyName = $"{EnumQuery.UpdatedDate}"
                            };

                            var updatedDateData = await _repository.GetFieldValue(updatedByRequestDate, true);

                            foreach (var item in updateTaskRequest.updateParams[0].propertyParams)
                            {
                                if (item.propertyName == EnumQuery.UpdatedBy)
                                {
                                    item.propertyValue = JsonConvert.SerializeObject(updatedBy);
                                }
                                else if (item.propertyName == EnumQuery.UpdatedDate)
                                {
                                    item.propertyValue = updatedDateData;
                                }
                            }

                            UpdateGeneralFormRequest updateRequestContentError = new UpdateGeneralFormRequest()
                            {
                                id = updateTaskRequest.id,
                                workOrder = updateTaskRequest.workorder,
                                updateParams = updateTaskRequest.updateParams,
                                employee = updateTaskRequest.employee,
                                updatedDate = updatedDateData,
                                checkBeforeTruck = resultCheckBeforTruck
                            };

                            return new ServiceResult
                            {
                                Message = errMsg,
                                IsError = true,
                                Content = updateRequestContentError
                            };
                        }
                    }
                }

                #endregion

                UpdateRequest updateRequest = new UpdateRequest()
                {
                    id = updateTaskRequest.id,
                    workOrder = updateTaskRequest.workorder,
                    updateParams = updateTaskRequest.updateParams,
                    employee = updateTaskRequest.employee
                };


                var result = await _repository.Update(updateRequest, rsc);

                UpdateGeneralFormRequest resultData = new UpdateGeneralFormRequest()
                {
                    id = updateTaskRequest.id,
                    workOrder = updateTaskRequest.workorder,
                    updateParams = result.updateParams,
                    employee = updateTaskRequest.employee,
                    updatedDate = result.updatedDate,
                    checkBeforeTruck = result.checkBeforeTruck
                };

                return new ServiceResult
                {
                    Message = "Data updated successfully",
                    IsError = false,
                    Content = resultData
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

        public async Task DeleteExtDefect(string taskId, string workorder, EmployeeModel employee, string taskType = null)
        {
            var tranOpts = new TransactionOptions()
            {
                IsolationLevel = IsolationLevel.ReadCommitted,
                Timeout = TransactionManager.MaximumTimeout
            };
            using (var tran = new TransactionScope(TransactionScopeOption.Required, tranOpts, TransactionScopeAsyncFlowOption.Enabled))
            {
                Dictionary<string, object> param = new Dictionary<string, object>();
                param.Add(EnumQuery.Workorder, workorder);
                param.Add(EnumQuery.TaskId, taskId);
                //param.Add(EnumQuery.IsActive, "true");
                //if (_container == EnumContainer.ServiceSheetDetail)
                //{
                //    param.Add(EnumQuery.ServicesheetDetailId, SSdetailId);
                //}

                var defectHeaders = await _defectHeaderRepository.GetDataListByParam(param, int.MaxValue, EnumQuery.Ts, EnumQuery.DESC);

                if (defectHeaders.Count > 0)
                {
                    foreach (var defectHeader in defectHeaders)
                    {
                        if (taskType != null)
                        {
                            //var defectHeaderString = JsonConvert.DeserializeObject(defectHeader);
                            if (taskType.ToLower() == EnumQuery.TaskValueMounting.ToLower())
                            {
                                if (!defectHeader["taskDesc"].ToString().Contains(EnumQuery.TaskDescMounting))
                                {
                                    continue;
                                }
                            }
                            else if (taskType.ToLower() == EnumQuery.TaskValueLeak.ToLower())
                            {
                                if (!defectHeader["taskDesc"].ToString().Contains(EnumQuery.TaskDescLeak))
                                {
                                    continue;
                                }
                            }
                        }
                        string id = StaticHelper.GetPropValue(defectHeader, EnumQuery.ID)?.Value;

                        if (!string.IsNullOrEmpty(id))
                        {
                            var result = await _defectHeaderRepository.Delete(new DeleteRequest()
                            {
                                id = id,
                                employee = employee
                            });
                        }

                        #region Delete Defect Intervention EHMS

                        if (_container == EnumContainer.Intervention)
                        {
                            DefectHeaderModel defectHeaderModel = JsonConvert.DeserializeObject<DefectHeaderModel>(JsonConvert.SerializeObject(defectHeader));
                            await DeleteInterventionDefectEHMS(defectHeaderModel.interventionHeaderId, defectHeaderModel.taskId, employee.id);

                        }

                        #endregion
                    }
                }

                tran.Complete();
            }

        }

        public async Task<TaskProgressResponse> GetTaskProgress(dynamic rsc)
        {
            int doneTask = 0;
            int taskGroupData = 0;
            int groupNoTask = 0;

            var resultTask = new List<string>();

            string json = JsonConvert.SerializeObject(rsc);
            JObject jObj = JObject.Parse(json);
            JToken token = JToken.Parse(json);

            var grupTask = token.SelectTokens($"$..[?(@.{EnumQuery.GroupTaskId} != '')]['{EnumQuery.GroupTaskId}']")
           .Select(x => jObj.SelectToken(x.Path).Value<string>()).Distinct()
           .ToList();

            var groupName = jObj["key"]?.ToString();

            foreach (var item in grupTask)
            {
                var excludeGroup = token.SelectTokens($"$..[?(@.{EnumQuery.GroupTaskId} == '{item}')]['{EnumQuery.Key}', '{EnumQuery.TaskValue}', '{EnumQuery.Category}', '{EnumQuery.Rating}', '{EnumQuery.ParentGroupTaskId}', '{EnumQuery.ChildGroupTaskId}']")
                                   .Select(v => (JProperty)v.Parent) // Get parent JProperty (which encapsulates name as well as value)
                                    .GroupBy(p => p.Parent)           // Group by parent JObject
                                    .Select(g => new JObject(g))
                                    .ToList();     // Create a new object with the filtered properties

                var getDisabledKey = token.SelectTokens($"$..[?(@.items[?(@.{EnumQuery.DisabledByItemKey} != '')] && @.{EnumQuery.GroupTaskId} == '{item}')]['{EnumQuery.Key}', '{EnumQuery.TaskValue}', '{EnumQuery.ParentGroupTaskId}', '{EnumQuery.ChildGroupTaskId}']")
                                  .Select(v => (JProperty)v.Parent) // Get parent JProperty (which encapsulates name as well as value)
                                   .GroupBy(p => p.Parent)           // Group by parent JObject
                                   .Select(g => new JObject(g))
                                   .ToList();     // Create a new object with the filtered properties

                if (excludeGroup.Count > 0)
                {
                    var disabledKey = JsonConvert.DeserializeObject<List<GroupTaskIdResponse>>(JsonConvert.SerializeObject(getDisabledKey));
                    try
                    {
                        var objGroupTaskId = JsonConvert.DeserializeObject<List<GroupTaskIdResponse>>(JsonConvert.SerializeObject(excludeGroup));
                        var parentGroupTaskId = objGroupTaskId.Where(x => x.parentGroupTaskId != null || x.childGroupTaskId != null).ToList();

                        objGroupTaskId = objGroupTaskId.Where(x => x.taskValue != null).ToList();

                        if (objGroupTaskId.Count > 0 && objGroupTaskId.Where(x => string.IsNullOrEmpty(x.taskValue)).Count() == 0)
                        {
                            doneTask += 1;
                        }
                        //else if (objGroupTaskId.Count > 0 && objGroupTaskId.Where(x => x.taskValue == EnumTaskValue.NormalNA && groupName != EnumGroup.ChassisCrackService && disabledKey.Count() > 0).Count() > 0 && objGroupTaskId.Where(x => string.IsNullOrEmpty(x.taskValue) && (x.category == EnumRatingServiceSheet.NORMAL && x.rating == EnumRatingServiceSheet.NO)).Count() == 0)
                        //{
                        //    doneTask += 1;
                        //}
                        else if (objGroupTaskId.Count > 0 && objGroupTaskId.Where(x => x.taskValue == EnumTaskValue.NormalNA && groupName != EnumGroup.ChassisCrackService && disabledKey.Count() > 0).Count() > 0 && objGroupTaskId.Where(x => x.category != EnumRatingServiceSheet.NORMAL && x.rating != EnumRatingServiceSheet.NO).Count() != 0)
                        {
                            doneTask += 1;
                        }
                        else if (objGroupTaskId.Count > 0 && objGroupTaskId.Where(x => x.taskValue == EnumTaskValue.CrackNA && groupName == EnumGroup.ChassisCrackService && disabledKey.Count() > 0).Count() > 0)
                        {
                            doneTask += 1;
                        }

                        if (parentGroupTaskId.Count > 0)
                        {
                            int totalSubGroupId = 0;
                            var groupByTaskId = parentGroupTaskId.Where(p => !string.IsNullOrEmpty(p.parentGroupTaskId)).Select(x => x.parentGroupTaskId).ToList();
                            foreach (var itemSub in groupByTaskId)
                            {
                                var getDataParentGroup = parentGroupTaskId.Where(x => x.parentGroupTaskId == itemSub && !string.IsNullOrEmpty(x.taskValue)).Count();
                                if (getDataParentGroup > 0)
                                {
                                    totalSubGroupId += 1;
                                }
                                else
                                {
                                    var getDataChildGroup = parentGroupTaskId.Where(x => x.childGroupTaskId == itemSub && !string.IsNullOrEmpty(x.taskValue)).Count();
                                    if (getDataChildGroup > 0)
                                    {
                                        totalSubGroupId += 1;
                                    }
                                }
                            }

                            if (parentGroupTaskId.Where(x => !string.IsNullOrEmpty(x.parentGroupTaskId)).Count() == totalSubGroupId)
                            {
                                doneTask += 1;
                            }
                        }

                        if (objGroupTaskId.Count == 0)
                            groupNoTask++;

                        taskGroupData += objGroupTaskId.Count;
                    }
                    catch (Exception ex)
                    {
                        var objGroupTaskId = JsonConvert.DeserializeObject<List<object>>(JsonConvert.SerializeObject(excludeGroup));
                        JObject taskImage = JsonConvert.DeserializeObject<JObject>(objGroupTaskId[0]?.ToString());

                        if (taskImage != null && taskImage.TryGetValue(EnumQuery.TaskValue, out var taskValueToken))
                        {
                            string taskValue = taskValueToken.ToString();
                            if (!string.IsNullOrWhiteSpace(taskValue))
                            {
                                doneTask++;
                                groupNoTask++;
                            }
                        }
                    }
                }
            }

            var taskValues = token.SelectTokens($"$..{EnumQuery.TaskValue}")
            .Select(x => jObj.SelectToken(x.Path).Value<object>())
            .ToList();

            var taskValuesWithoutGroup = token.SelectTokens($"$..[?(@.{EnumQuery.GroupTaskId} == '' && @.{EnumQuery.Rating} != '{EnumTaskType.RatingCalibration}')]['{EnumQuery.Key}', '{EnumQuery.TaskValue}']")
                                 .Select(v => (JProperty)v.Parent) // Get parent JProperty (which encapsulates name as well as value)
                                  .GroupBy(p => p.Parent)           // Group by parent JObject
                                  .Select(g => new JObject(g))
                                  .ToList();     // Create a new object with the filtered properties


            var taskValuesWithoutGroupSuspension = token.SelectTokens($"$..[?(@.{EnumQuery.GroupTaskId} == '' && @.{EnumQuery.Rating} == '{EnumTaskType.RatingCalibration}')]['{EnumQuery.Key}', '{EnumQuery.TaskValue}']")
                                  .Select(v => (JProperty)v.Parent) // Get parent JProperty (which encapsulates name as well as value)
                                   .GroupBy(p => p.Parent)           // Group by parent JObject
                                   .Select(g => new JObject(g))
                                   .ToList();

            if (taskValuesWithoutGroupSuspension.Count > 0)
            {
                var objWithoutGroupSuspenstionTaskId = JsonConvert.DeserializeObject<List<GroupTaskIdResponse>>(JsonConvert.SerializeObject(taskValuesWithoutGroupSuspension));
                objWithoutGroupSuspenstionTaskId = objWithoutGroupSuspenstionTaskId.Where(x => x.taskValue.Equals(EnumTaskValue.CalibrationNo)).ToList();
                if (objWithoutGroupSuspenstionTaskId.Count > 0)
                {
                    doneTask += objWithoutGroupSuspenstionTaskId.Count();
                }
                else
                {
                    var groupAdjustmentSuspension = token.SelectTokens($"$..[?(@.{EnumQuery.GroupTaskId} != '' && @.{EnumQuery.ShowParameter} == '{EnumQuery.ValueShowParameter}')]['{EnumQuery.Key}', '{EnumQuery.UpdatedDate}']")
                                  .Select(v => (JProperty)v.Parent) // Get parent JProperty (which encapsulates name as well as value)
                                   .GroupBy(p => p.Parent)           // Group by parent JObject
                                   .Select(g => new JObject(g))
                                   .ToList();
                    var items = JsonConvert.DeserializeObject<List<GroupTaskIdResponse>>(JsonConvert.SerializeObject(groupAdjustmentSuspension));
                    var notSuspensionCylinderTask = items.Where(x => string.IsNullOrEmpty(x.updatedDate)).ToList();
                    if (notSuspensionCylinderTask.Count == 0) doneTask += 1;
                }

            }

            if (taskValuesWithoutGroup.Count > 0)
            {
                var objWithoutGroupTaskId = JsonConvert.DeserializeObject<List<GroupTaskIdResponse>>(JsonConvert.SerializeObject(taskValuesWithoutGroup));
                objWithoutGroupTaskId = objWithoutGroupTaskId.Where(x => !string.IsNullOrEmpty(x.taskValue)).ToList();

                doneTask += objWithoutGroupTaskId.Count();
            }


            string workorder = string.Empty;

            if (_container == EnumContainer.ServiceSheetDetail)
                workorder = jObj["workOrder"]?.ToString();
            else if (_container == EnumContainer.Intervention)
                workorder = jObj["sapWorkOrder"]?.ToString();

            TaskProgressResponse result = new TaskProgressResponse()
            {
                workorder = workorder,
                group = jObj["key"]?.ToString(),
                totalTask = taskValues.Count - taskGroupData + grupTask.Count - groupNoTask,
                doneTask = doneTask
            };

            return result;
        }

        public async Task<TaskProgressResponse> GetTaskProgressV2(dynamic rsc)
        {
            int doneTask = 0;
            int taskGroupData = 0;
            int groupNoTask = 0;

            var resultTask = new List<string>();

            string json = JsonConvert.SerializeObject(rsc);
            JObject jObj = JObject.Parse(json);
            JToken token = JToken.Parse(json);

            var grupTask = token.SelectTokens($"$..[?(@.{EnumQuery.GroupTaskId} != '')]['{EnumQuery.GroupTaskId}']")
           .Select(x => jObj.SelectToken(x.Path).Value<string>()).Distinct()
           .ToList();

            var groupName = jObj["key"]?.ToString();

            foreach (var item in grupTask)
            {
                var excludeGroup = token.SelectTokens($"$..[?(@.{EnumQuery.GroupTaskId} == '{item}')]['{EnumQuery.Key}', '{EnumQuery.TaskValue}', '{EnumQuery.Category}', '{EnumQuery.Rating}', '{EnumQuery.ParentGroupTaskId}', '{EnumQuery.ChildGroupTaskId}', '{EnumQuery.GroupTaskId}']")
                                   .Select(v => (JProperty)v.Parent) // Get parent JProperty (which encapsulates name as well as value)
                                    .GroupBy(p => p.Parent)           // Group by parent JObject
                                    .Select(g => new JObject(g))
                                    .ToList();     // Create a new object with the filtered properties

                var getDisabledKey = token.SelectTokens($"$..[?(@.[?(@.{EnumQuery.DisabledByItemKey} != '')] && @.{EnumQuery.GroupTaskId} == '{item}')]['{EnumQuery.Key}', '{EnumQuery.TaskValue}', '{EnumQuery.ParentGroupTaskId}', '{EnumQuery.ChildGroupTaskId}']")
                                  .Select(v => (JProperty)v.Parent) // Get parent JProperty (which encapsulates name as well as value)
                                   .GroupBy(p => p.Parent)           // Group by parent JObject
                                   .Select(g => new JObject(g))
                                   .ToList();     // Create a new object with the filtered properties

                if (excludeGroup.Count > 0)
                {
                    var disabledKey = JsonConvert.DeserializeObject<List<GroupTaskIdResponse>>(JsonConvert.SerializeObject(getDisabledKey));
                    try
                    {
                        var objGroupTaskId = JsonConvert.DeserializeObject<List<GroupTaskIdResponse>>(JsonConvert.SerializeObject(excludeGroup));
                        //var parentGroupTaskId = objGroupTaskId.Where(x => x.parentGroupTaskId != null || x.childGroupTaskId != null).ToList();

                        objGroupTaskId = objGroupTaskId.Where(x => x.taskValue != null).ToList();

                        if (objGroupTaskId.Count > 0 && objGroupTaskId.Where(x => string.IsNullOrEmpty(x.taskValue)).Count() == 0)
                        {
                            doneTask += 1;
                        }
                        //else if (objGroupTaskId.Count > 0 && objGroupTaskId.Where(x => x.taskValue == EnumTaskValue.NormalNA && groupName != EnumGroup.ChassisCrackService && disabledKey.Count() > 0).Count() > 0 && objGroupTaskId.Where(x => string.IsNullOrEmpty(x.taskValue) && (x.category == EnumRatingServiceSheet.NORMAL && x.rating == EnumRatingServiceSheet.NO)).Count() == 0)
                        //{
                        //    doneTask += 1;
                        //}
                        else if (objGroupTaskId.Count > 0 && objGroupTaskId.Where(x => x.taskValue == EnumTaskValue.NormalNA && groupName != EnumGroup.ChassisCrackService && disabledKey.Count() > 0).Count() > 0 && objGroupTaskId.Where(x => x.category != EnumRatingServiceSheet.NORMAL && x.rating != EnumRatingServiceSheet.NO).Count() != 0)
                        {
                            doneTask += 1;
                        }
                        else if (objGroupTaskId.Count > 0 && objGroupTaskId.Where(x => x.taskValue == EnumTaskValue.CrackNA && groupName == EnumGroup.ChassisCrackService && disabledKey.Count() > 0).Count() > 0)
                        {
                            doneTask += 1;
                        }

                        #region Birana

                        var biranaGroupTaskIds = objGroupTaskId.Where(x => x.parentGroupTaskId != null || x.childGroupTaskId != null).Select(s => s.groupTaskId).Distinct().ToList();

                        foreach (var biranaGroupTaskId in biranaGroupTaskIds)
                        {
                            int totalSubTask = 0;
                            int doneSubTask = 0;

                            var tasks = objGroupTaskId.Where(x => x.groupTaskId == biranaGroupTaskId).ToList();
                            var headerSubTask = tasks.Where(x => !string.IsNullOrEmpty(x.parentGroupTaskId)).ToList();

                            totalSubTask += headerSubTask.Count();

                            foreach (var header in headerSubTask)
                            {
                                if (!string.IsNullOrEmpty(header.taskValue))
                                {
                                    doneSubTask++;
                                }
                                else
                                {
                                    var childSubTask = tasks.Where(x => x.childGroupTaskId == header.parentGroupTaskId).ToList();
                                    int doneChildSubTask = childSubTask.Where(x => !string.IsNullOrEmpty(x.taskValue)).Count();

                                    if (doneChildSubTask == childSubTask.Count)
                                        doneSubTask++;
                                }
                            }

                            totalSubTask += tasks.Where(x => string.IsNullOrEmpty(x.parentGroupTaskId) && string.IsNullOrEmpty(x.childGroupTaskId)).Count();
                            doneSubTask += tasks.Where(x => string.IsNullOrEmpty(x.parentGroupTaskId) && string.IsNullOrEmpty(x.childGroupTaskId) && !string.IsNullOrEmpty(x.taskValue)).Count();

                            if (totalSubTask == doneSubTask)
                                doneTask += 1;
                        }

                        #endregion

                        if (objGroupTaskId.Count == 0)
                            groupNoTask++;

                        taskGroupData += objGroupTaskId.Count;
                    }
                    catch (Exception ex)
                    {
                        var objGroupTaskId = JsonConvert.DeserializeObject<List<object>>(JsonConvert.SerializeObject(excludeGroup));
                        JObject taskImage = JsonConvert.DeserializeObject<JObject>(objGroupTaskId[0]?.ToString());

                        if (taskImage != null && taskImage.TryGetValue(EnumQuery.TaskValue, out var taskValueToken))
                        {
                            string taskValue = taskValueToken.ToString();
                            if (!string.IsNullOrWhiteSpace(taskValue))
                            {
                                doneTask++;
                                groupNoTask++;
                            }
                        }
                    }
                }
            }

            //var taskValues = token.SelectTokens($"$..{EnumQuery.TaskValue}")
            //.Select(x => jObj.SelectToken(x.Path).Value<object>())
            //.ToList();

            var taskValues = token.SelectTokens($"$..[?(@.{EnumQuery.TaskValue} != null)]['{EnumQuery.TaskValue}']")
                                 .Select(v => (JProperty)v.Parent) // Get parent JProperty (which encapsulates name as well as value)
                                  .GroupBy(p => p.Parent)           // Group by parent JObject
                                  .Select(g => new JObject(g))
                                  .ToList();     // Create a new object with the filtered properties

            var taskValuesWithoutGroup = token.SelectTokens($"$..[?(@.{EnumQuery.GroupTaskId} == '' && @.{EnumQuery.Rating} != '{EnumTaskType.RatingCalibration}')]['{EnumQuery.Key}', '{EnumQuery.TaskValue}']")
                                 .Select(v => (JProperty)v.Parent) // Get parent JProperty (which encapsulates name as well as value)
                                  .GroupBy(p => p.Parent)           // Group by parent JObject
                                  .Select(g => new JObject(g))
                                  .ToList();     // Create a new object with the filtered properties


            var taskValuesWithoutGroupSuspension = token.SelectTokens($"$..[?(@.{EnumQuery.GroupTaskId} == '' && @.{EnumQuery.Rating} == '{EnumTaskType.RatingCalibration}')]['{EnumQuery.Key}', '{EnumQuery.TaskValue}']")
                                  .Select(v => (JProperty)v.Parent) // Get parent JProperty (which encapsulates name as well as value)
                                   .GroupBy(p => p.Parent)           // Group by parent JObject
                                   .Select(g => new JObject(g))
                                   .ToList();

            if (taskValuesWithoutGroupSuspension.Count > 0)
            {
                var objWithoutGroupSuspenstionTaskId = JsonConvert.DeserializeObject<List<GroupTaskIdResponse>>(JsonConvert.SerializeObject(taskValuesWithoutGroupSuspension));
                objWithoutGroupSuspenstionTaskId = objWithoutGroupSuspenstionTaskId.Where(x => x.taskValue.Equals(EnumTaskValue.CalibrationNo)).ToList();
                if (objWithoutGroupSuspenstionTaskId.Count > 0)
                {
                    doneTask += objWithoutGroupSuspenstionTaskId.Count();
                }
                else
                {
                    var groupAdjustmentSuspension = token.SelectTokens($"$..[?(@.{EnumQuery.GroupTaskId} != '' && @.{EnumQuery.ShowParameter} == '{EnumQuery.ValueShowParameter}')]['{EnumQuery.Key}', '{EnumQuery.UpdatedDate}']")
                                  .Select(v => (JProperty)v.Parent) // Get parent JProperty (which encapsulates name as well as value)
                                   .GroupBy(p => p.Parent)           // Group by parent JObject
                                   .Select(g => new JObject(g))
                                   .ToList();
                    var items = JsonConvert.DeserializeObject<List<GroupTaskIdResponse>>(JsonConvert.SerializeObject(groupAdjustmentSuspension));
                    var notSuspensionCylinderTask = items.Where(x => string.IsNullOrEmpty(x.updatedDate)).ToList();
                    if (notSuspensionCylinderTask.Count == 0) doneTask += 1;
                }

            }

            if (taskValuesWithoutGroup.Count > 0)
            {
                var objWithoutGroupTaskId = JsonConvert.DeserializeObject<List<GroupTaskIdResponse>>(JsonConvert.SerializeObject(taskValuesWithoutGroup));
                objWithoutGroupTaskId = objWithoutGroupTaskId.Where(x => !string.IsNullOrEmpty(x.taskValue)).ToList();

                doneTask += objWithoutGroupTaskId.Count();
            }


            string workorder = string.Empty;

            if (_container == EnumContainer.ServiceSheetDetail)
                workorder = jObj["workOrder"]?.ToString();
            else if (_container == EnumContainer.Intervention)
                workorder = jObj["sapWorkOrder"]?.ToString();

            TaskProgressResponse result = new TaskProgressResponse()
            {
                workorder = workorder,
                group = jObj["key"]?.ToString(),
                totalTask = taskValues.Count - taskGroupData + grupTask.Count - groupNoTask,
                doneTask = doneTask
            };

            return result;
        }

        private async Task CreateInterventionDefectEHMS(string intHeaderId, string taskId, string empId)
        {
            CallAPIService callAPI = new CallAPIService(_appSetting, _accessToken);
            InterventionDetailModel interventionDetail = new InterventionDetailModel();

            Dictionary<string, object> intDetailParam = new Dictionary<string, object>();
            intDetailParam.Add(StaticHelper.GetPropertyName(() => interventionDetail.intervention_header_id), Convert.ToInt64(intHeaderId));
            intDetailParam.Add(StaticHelper.GetPropertyName(() => interventionDetail.task_key), taskId);
            intDetailParam.Add(StaticHelper.GetPropertyName(() => interventionDetail.is_active), true);
            intDetailParam.Add(StaticHelper.GetPropertyName(() => interventionDetail.is_deleted), false);

            dynamic intDetailResult = await callAPI.EHMSGetByParam(EnumController.InterventionDetail, intDetailParam);

            if (intDetailResult != null)
            {
                interventionDetail = JsonConvert.DeserializeObject<InterventionDetailModel>(JsonConvert.SerializeObject(intDetailResult));

                DateTime currentDate = EnumCommonProperty.CurrentDateTime;

                InterventionDefectModel interventionDefect = new InterventionDefectModel()
                {
                    intervention_detail_id = interventionDetail.tr_intervention_detail_id,
                    is_active = true,
                    is_deleted = false,
                    created_on = currentDate,
                    created_by = empId,
                    changed_on = currentDate,
                    changed_by = empId
                };

                await callAPI.EHMSPost(EnumController.InterventionDefect, interventionDefect);
            }
        }

        private async Task DeleteInterventionDefectEHMS(string intHeaderId, string taskId, string empId)
        {
            CallAPIService callAPI = new CallAPIService(_appSetting, _accessToken);
            InterventionDetailModel interventionDetail = new InterventionDetailModel();
            InterventionDefectModel interventionDefect = new InterventionDefectModel();

            Dictionary<string, object> intDetailParam = new Dictionary<string, object>();
            intDetailParam.Add(StaticHelper.GetPropertyName(() => interventionDetail.intervention_header_id), Convert.ToInt64(intHeaderId));
            intDetailParam.Add(StaticHelper.GetPropertyName(() => interventionDetail.task_key), taskId);
            intDetailParam.Add(StaticHelper.GetPropertyName(() => interventionDetail.is_active), true);
            intDetailParam.Add(StaticHelper.GetPropertyName(() => interventionDetail.is_deleted), false);

            dynamic intDetailResult = await callAPI.EHMSGetByParam(EnumController.InterventionDetail, intDetailParam);

            if (intDetailResult != null)
            {
                interventionDetail = JsonConvert.DeserializeObject<InterventionDetailModel>(JsonConvert.SerializeObject(intDetailResult));

                Dictionary<string, object> DeleteInterventionDefectParam = new Dictionary<string, object>();
                DeleteInterventionDefectParam.Add(StaticHelper.GetPropertyName(() => interventionDefect.intervention_detail_id), interventionDetail.tr_intervention_detail_id);
                DeleteInterventionDefectParam.Add(StaticHelper.GetPropertyName(() => interventionDefect.changed_by), empId);
                DeleteInterventionDefectParam.Add(StaticHelper.GetPropertyName(() => interventionDefect.changed_on), EnumCommonProperty.CurrentDateTime);

                dynamic defectResult = await callAPI.EHMSDeleteInterventionDefect(DeleteInterventionDefectParam);

                /*dynamic defectResult = await callAPI.EHMSGetByParam(EnumController.InterventionDefect, interventionDefectParam);

                if (defectResult != null)
                {
                    interventionDefect = JsonConvert.DeserializeObject<InterventionDefectModel>(JsonConvert.SerializeObject(defectResult));

                    if (defectResult != null)
                    {
                        interventionDefect = JsonConvert.DeserializeObject<InterventionDefectModel>(JsonConvert.SerializeObject(defectResult));
                        interventionDefect.is_deleted = true;
                        interventionDefect.changed_on = EnumCommonProperty.CurrentDateTime;
                        interventionDefect.changed_by = empId;

                        await callAPI.EHMSPut(EnumController.InterventionDefect, interventionDefect);
                    }
                }*/
            }
        }

        #region Interim Engine
        public async Task<ServiceResult> UpdateTaskReviseInterim(UpdateTaskReviseRequest updateTaskRequest)
        {
            try
            {
                #region Validation
                //_logger.LogWarning("Validation");

                #region Service Sheet Status Validation

                var header = await _servicesheetHeaderRepository.Get(updateTaskRequest.headerId);
                var status = string.Empty;

                #endregion

                var rsc = await _repository.Get(updateTaskRequest.id);
                string taskId = string.Empty;
                string workOrder = string.Empty;
                string dataTaskKey = string.Empty;
                string reasonData = string.Empty;
                string dataTaskKeyMain = updateTaskRequest.taskKey;
                bool hasTaskValue = false;

                foreach (var updateParam in updateTaskRequest.updateParams)
                {

                    foreach (var propertyParam in updateParam.propertyParams)
                    {
                        if (propertyParam.propertyName.ToLower() == EnumQuery.TaskValue.ToLower()) { hasTaskValue = true; }
                        if (propertyParam.propertyName.ToLower() == EnumQuery.Reason.ToLower()) { reasonData = propertyParam.propertyValue; }

                        dataTaskKey = updateParam.keyValue;
                        string errMsg = await ValidationInterim(new ValidationRequest()
                        {
                            rsc = rsc,
                            //id = updateTaskRequest.id,
                            keyValue = updateParam.keyValue,
                            propertyName = propertyParam.propertyName,
                            propertyValue = propertyParam.propertyValue,
                            //employeeId = updateTaskRequest.employee.id,
                            isDefect = false,
                            headerId = updateTaskRequest.headerId,
                            workorder = updateTaskRequest.workorder,
                            employee = updateTaskRequest.employee,
                            propertyParams = updateParam.propertyParams,
                            reason = reasonData
                        });

                        if (!string.IsNullOrEmpty(errMsg))
                        {
                            return new ServiceResult
                            {
                                Message = errMsg,
                                IsError = true
                            };
                        }
                    }
                }

                #endregion

                UpdateRequest updateRequest = new UpdateRequest()
                {
                    id = updateTaskRequest.id,
                    workOrder = updateTaskRequest.workorder,
                    updateParams = updateTaskRequest.updateParams,
                    employee = updateTaskRequest.employee
                };

                //_logger.LogWarning("Update Service Sheet Detail");

                //check data if cbm adjustment/replacement
                DetailServiceSheet _model = new DetailServiceSheet();
                _model.workOrder = updateRequest.workOrder;
                _model.taskKey = dataTaskKeyMain;

                var _repoDetailData = new SuckAndBlowDetailRepository(_connectionFactory, EnumContainer.InterimEngineDetail);
                var _currentData = await _repoDetailData.GetDataServiceSheetDetailByKey(_model);
                bool isCbmAdjustmentOrReplacement = _currentData[0].cbmAdjustmentReplacement;
                string updatedDate = ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerDateTime);

                #region History Revise if not adjustment/replacement
                if (isCbmAdjustmentOrReplacement == false)
                {
                    #region History Revise
                    if (hasTaskValue)
                    {
                        Dictionary<string, object> paramHistory = new Dictionary<string, object>();
                        paramHistory.Add("workOrder", updateRequest.workOrder);
                        paramHistory.Add("taskKey", dataTaskKeyMain);

                        var dataHistory = await _cbmHistoryRepository.GetDataByParam(paramHistory);
                        if (dataHistory == null)
                        {
                            DetailServiceSheet model = new DetailServiceSheet();
                            model.workOrder = updateRequest.workOrder;
                            model.taskKey = dataTaskKeyMain;

                            var _repoDetail = new SuckAndBlowDetailRepository(_connectionFactory, EnumContainer.InterimEngineDetail);
                            var currentData = await _repoDetail.GetDataServiceSheetDetailByKey(model);

                            Dictionary<string, object> paramHeader = new Dictionary<string, object>();
                            paramHeader.Add("workOrder", model.workOrder);

                            var curentDataHeaderUpdate = await _servicesheetHeaderRepository.GetDataByParam(paramHeader);

                            if (curentDataHeaderUpdate == null)
                                throw new Exception($"Current data header work order {model.workOrder} not found");

                            CbmHistoryParam headerHisUpdate = new CbmHistoryParam();
                            headerHisUpdate.key = EnumGroup.General;
                            headerHisUpdate.workOrder = model.workOrder;
                            //headerHisUpdate.workOrder = model.taskKey;
                            headerHisUpdate.equipment = curentDataHeaderUpdate.equipment;
                            headerHisUpdate.taskKey = model.taskKey;
                            headerHisUpdate.siteId = curentDataHeaderUpdate.siteId;
                            headerHisUpdate.serviceSheetDetailId = currentData[0].id;
                            headerHisUpdate.defectHeaderId = "";
                            headerHisUpdate.detail = new DetailCBMHistory();

                            //add 2024-02-19
                            headerHisUpdate.modelId = rsc[EnumQuery.ModelId];
                            headerHisUpdate.psTypeId = rsc[EnumQuery.PsTypeId];
                            headerHisUpdate.taskDescription = currentData[0][EnumQuery.Description];
                            headerHisUpdate.category = currentData[0].category + " " + currentData[0].rating;
                            headerHisUpdate.currentValue = currentData[0][EnumQuery.MeasurementValue];
                            headerHisUpdate.currentRating = currentData[0][EnumQuery.TaskValue];
                            headerHisUpdate.replacementValue = currentData[0][EnumQuery.NonCbmAdjustmentReplacementMeasurementValue];
                            headerHisUpdate.replacementRating = currentData[0][EnumQuery.NonCbmAdjustmentReplacementRating];
                            headerHisUpdate.closedDate = updatedDate;
                            headerHisUpdate.closedBy = updateRequest.employee.id;
                            headerHisUpdate.source = "ip_overwrite";

                            foreach (var item in currentData)
                            {
                                item.Remove("id");
                                item.Remove("uom");
                                item.Remove("taskNo");
                                item.Remove("measurementValue");
                                item.Remove("nonCbmAdjustmentReplacementMeasurementValue");
                                item.Remove("nonCbmAdjustmentReplacementRating");
                                //item.Remove("cbmAdjustmentReplacementValue");
                            }

                            // Last Value
                            DetailServiceSheet modelUpdate = new DetailServiceSheet();
                            modelUpdate.workOrder = updateRequest.workOrder;
                            modelUpdate.taskKey = dataTaskKeyMain;

                            var _repoDetailUpdate = new SuckAndBlowDetailRepository(_connectionFactory, EnumContainer.InterimEngineDetail);
                            var currentDataUpdate = await _repoDetailUpdate.GetDataServiceSheetDetailByKey(modelUpdate);

                            foreach (var item in currentDataUpdate)
                            {
                                item.Remove("id");
                                item.Remove("uom");
                                item.Remove("taskNo");
                                item.Remove("measurementValue");
                                item.Remove("nonCbmAdjustmentReplacementMeasurementValue");
                                item.Remove("nonCbmAdjustmentReplacementRating");
                                //item.Remove("cbmAdjustmentReplacementValue");

                                foreach (var itemDetail in item.items)
                                {
                                    if (updateTaskRequest.updateParams.Where(x => x.keyValue.ToString() == itemDetail.key.ToString()).Count() != 0)
                                    {
                                        var dataUpdate = updateTaskRequest.updateParams.Where(x => x.keyValue.ToString() == itemDetail.key.ToString()).FirstOrDefault();
                                        var dataDetailUpdate = dataUpdate.propertyParams.Where(x => x.propertyName.ToString() == "value").FirstOrDefault();
                                        var dataDetailUpdateBy = dataUpdate.propertyParams.Where(x => x.propertyName.ToString() == "updatedBy").FirstOrDefault();
                                        var dataDetailUpdateDate = dataUpdate.propertyParams.Where(x => x.propertyName.ToString() == "updatedDate").FirstOrDefault();

                                        if (dataDetailUpdate.propertyValue.Contains("filename"))
                                        {
                                            itemDetail.value = JsonConvert.DeserializeObject<object>(dataDetailUpdate.propertyValue);
                                        }
                                        else
                                        {
                                            itemDetail.value = dataDetailUpdate.propertyValue;
                                        }

                                        if (dataDetailUpdate.propertyValue == EnumTaskValue.CbmA || dataDetailUpdate.propertyValue == EnumTaskValue.CbmB || dataDetailUpdate.propertyValue == EnumTaskValue.CbmC || dataDetailUpdate.propertyValue == EnumTaskValue.CbmX)
                                        {
                                            item.taskValue = dataDetailUpdate.propertyValue;
                                            item.updatedBy = JsonConvert.DeserializeObject<object>(dataDetailUpdateBy.propertyValue);
                                            item.updatedDate = ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerDateTime);
                                        }

                                    }
                                }
                            }

                            currentData.Add(currentDataUpdate[0]);

                            DetailCBMHistory detailHisUpdate = new DetailCBMHistory();
                            detailHisUpdate.key = "HISTORY";
                            detailHisUpdate.category = currentData[0].category;
                            detailHisUpdate.rating = currentData[0].rating;
                            detailHisUpdate.history = currentData;

                            headerHisUpdate.detail = detailHisUpdate;

                            var modelHeader = new CreateRequest();
                            modelHeader.employee = new EmployeeModel();

                            modelHeader.employee.id = updateRequest.employee.id;
                            modelHeader.employee.name = updateRequest.employee.name;
                            modelHeader.entity = headerHisUpdate;

                            var resultAddHeader = await _cbmHistoryRepository.Create(modelHeader);
                        }
                        else
                        {
                            var detailHistory = StaticHelper.GetPropValue(dataHistory.detail, "history");

                            List<dynamic> dataCount = JsonConvert.DeserializeObject<List<dynamic>>(JsonConvert.SerializeObject(detailHistory));

                            if (dataCount.Count() > 3)
                            {
                                return new ServiceResult
                                {
                                    Message = "Cannot revise the data, you have limit for revising 3 times.",
                                    IsError = true
                                };
                            }

                            // Last Value
                            DetailServiceSheet modelUpdate = new DetailServiceSheet();
                            modelUpdate.workOrder = updateRequest.workOrder;
                            modelUpdate.taskKey = dataTaskKeyMain;

                            var _repoDetailUpdate = new SuckAndBlowDetailRepository(_connectionFactory, EnumContainer.InterimEngineDetail);
                            var currentDataUpdate = await _repoDetailUpdate.GetDataServiceSheetDetailByKey(modelUpdate);

                            foreach (var item in currentDataUpdate)
                            {
                                item.Remove("id");
                                item.Remove("uom");
                                item.Remove("taskNo");
                                item.Remove("measurementValue");
                                item.Remove("nonCbmAdjustmentReplacementMeasurementValue");
                                item.Remove("nonCbmAdjustmentReplacementRating");
                                //item.Remove("cbmAdjustmentReplacementValue");

                                foreach (var itemDetail in item.items)
                                {
                                    if (updateTaskRequest.updateParams.Where(x => x.keyValue.ToString() == itemDetail.key.ToString()).Count() != 0)
                                    {
                                        var dataUpdate = updateTaskRequest.updateParams.Where(x => x.keyValue.ToString() == itemDetail.key.ToString()).FirstOrDefault();
                                        var dataDetailUpdate = dataUpdate.propertyParams.Where(x => x.propertyName.ToString() == "value").FirstOrDefault();
                                        var dataDetailUpdateBy = dataUpdate.propertyParams.Where(x => x.propertyName.ToString() == "updatedBy").FirstOrDefault();
                                        var dataDetailUpdateDate = dataUpdate.propertyParams.Where(x => x.propertyName.ToString() == "updatedDate").FirstOrDefault();

                                        if (dataDetailUpdate.propertyValue.Contains("filename"))
                                        {
                                            itemDetail.value = JsonConvert.DeserializeObject<object>(dataDetailUpdate.propertyValue);
                                        }
                                        else
                                        {
                                            itemDetail.value = dataDetailUpdate.propertyValue;
                                        }

                                        if (dataDetailUpdate.propertyValue == EnumTaskValue.CbmA || dataDetailUpdate.propertyValue == EnumTaskValue.CbmB || dataDetailUpdate.propertyValue == EnumTaskValue.CbmC || dataDetailUpdate.propertyValue == EnumTaskValue.CbmX)
                                        {
                                            item.taskValue = dataDetailUpdate.propertyValue;
                                            item.updatedBy = JsonConvert.DeserializeObject<object>(dataDetailUpdateBy.propertyValue);
                                            item.updatedDate = ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerDateTime);
                                        }
                                    }
                                }
                            }

                            detailHistory.Add(currentDataUpdate[0]);

                            List<PropertyParam> propertyParams = new List<PropertyParam>() {
                        new PropertyParam()
                        {
                            propertyName = EnumQuery.CbmHistory,
                            propertyValue = JsonConvert.SerializeObject(detailHistory)
                        }
                    };

                            UpdateRequest updateDataParams = new UpdateRequest();
                            updateDataParams.id = dataHistory.id;
                            updateDataParams.workOrder = dataHistory.workOrder;
                            updateDataParams.updateParams = new List<UpdateParam>();
                            updateDataParams.employee = updateTaskRequest.employee;

                            updateDataParams.updateParams.Add(new UpdateParam()
                            {
                                keyValue = "HISTORY",
                                propertyParams = propertyParams
                            });

                            var resultUpdateHeader = await _cbmHistoryRepository.Update(updateDataParams, dataHistory);
                        }
                    }
                    else
                    {
                        Dictionary<string, object> paramHistory = new Dictionary<string, object>();
                        paramHistory.Add("workOrder", updateRequest.workOrder);
                        paramHistory.Add("taskKey", dataTaskKeyMain);

                        var dataHistory = await _cbmHistoryRepository.GetDataByParam(paramHistory);
                        if (dataHistory != null)
                        {
                            DetailServiceSheet model = new DetailServiceSheet();
                            model.workOrder = updateRequest.workOrder;
                            model.taskKey = dataTaskKeyMain;

                            var _repoDetail = new SuckAndBlowDetailRepository(_connectionFactory, EnumContainer.InterimEngineDetail);
                            var currentData = await _repoDetail.GetDataServiceSheetDetailByKey(model);
                            //bool isCbmAdjustmentOrReplacement = currentData[0].cbmAdjustmentReplacement;

                            var detailHistory = StaticHelper.GetPropValue(dataHistory.detail, "history");

                            List<dynamic> dataCount = JsonConvert.DeserializeObject<List<dynamic>>(JsonConvert.SerializeObject(detailHistory));

                            if (dataCount.Count() > 3)
                            {
                                return new ServiceResult
                                {
                                    Message = "Cannot revise the data, you have limit for revising 3 times.",
                                    IsError = true
                                };
                            }

                            // Last Value
                            DetailServiceSheet modelUpdate = new DetailServiceSheet();
                            modelUpdate.workOrder = updateRequest.workOrder;
                            modelUpdate.taskKey = dataTaskKeyMain;

                            var _repoDetailUpdate = new SuckAndBlowDetailRepository(_connectionFactory, EnumContainer.InterimEngineDetail);
                            var currentDataUpdate = await _repoDetailUpdate.GetDataServiceSheetDetailByKey(modelUpdate);

                            foreach (var item in currentDataUpdate)
                            {
                                item.Remove("id");
                                item.Remove("uom");
                                item.Remove("taskNo");
                                item.Remove("measurementValue");
                                item.Remove("nonCbmAdjustmentReplacementMeasurementValue");
                                item.Remove("nonCbmAdjustmentReplacementRating");
                                //item.Remove("cbmAdjustmentReplacementValue");

                                foreach (var itemDetail in item.items)
                                {
                                    if (updateTaskRequest.updateParams.Where(x => x.keyValue.ToString() == itemDetail.key.ToString()).Count() != 0)
                                    {
                                        var dataUpdate = updateTaskRequest.updateParams.Where(x => x.keyValue.ToString() == itemDetail.key.ToString()).FirstOrDefault();
                                        var dataDetailUpdate = dataUpdate.propertyParams.Where(x => x.propertyName.ToString() == "value").FirstOrDefault();
                                        var dataDetailUpdateBy = dataUpdate.propertyParams.Where(x => x.propertyName.ToString() == "updatedBy").FirstOrDefault();
                                        var dataDetailUpdateDate = dataUpdate.propertyParams.Where(x => x.propertyName.ToString() == "updatedDate").FirstOrDefault();

                                        if (dataDetailUpdate.propertyValue.Contains("filename"))
                                        {
                                            itemDetail.value = JsonConvert.DeserializeObject<object>(dataDetailUpdate.propertyValue);
                                        }
                                        else
                                        {
                                            itemDetail.value = dataDetailUpdate.propertyValue;
                                        }

                                        if (dataDetailUpdate.propertyValue == EnumTaskValue.CbmA || dataDetailUpdate.propertyValue == EnumTaskValue.CbmB || dataDetailUpdate.propertyValue == EnumTaskValue.CbmC || dataDetailUpdate.propertyValue == EnumTaskValue.CbmX)
                                        {
                                            item.taskValue = dataDetailUpdate.propertyValue;
                                            item.updatedBy = JsonConvert.DeserializeObject<object>(dataDetailUpdateBy.propertyValue);
                                            item.updatedDate = ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerDateTime);
                                        }

                                    }
                                }
                            }

                            detailHistory.Add(currentDataUpdate[0]);


                            List<PropertyParam> propertyParams = new List<PropertyParam>() {
                        new PropertyParam()
                            {
                                propertyName = EnumQuery.CbmHistory,
                                propertyValue = JsonConvert.SerializeObject(detailHistory)
                            }
                        };

                            UpdateRequest updateDataParams = new UpdateRequest();
                            updateDataParams.id = dataHistory.id;
                            updateDataParams.workOrder = dataHistory.workOrder;
                            updateDataParams.updateParams = new List<UpdateParam>();
                            updateDataParams.employee = updateTaskRequest.employee;

                            updateDataParams.updateParams.Add(new UpdateParam()
                            {
                                keyValue = "HISTORY",
                                propertyParams = propertyParams
                            });

                            var resultUpdateHeader = await _cbmHistoryRepository.Update(updateDataParams, dataHistory);
                            //var currentDataUpdate = await _repoDetailUpdate.GetDataItemsDetailValue(modelUpdate);
                        }
                        else
                        {
                            DetailServiceSheet model = new DetailServiceSheet();
                            model.workOrder = updateRequest.workOrder;
                            model.taskKey = dataTaskKeyMain;

                            var _repoDetail = new SuckAndBlowDetailRepository(_connectionFactory, EnumContainer.InterimEngineDetail);
                            var currentData = await _repoDetail.GetDataServiceSheetDetailByKey(model);

                            Dictionary<string, object> paramHeader = new Dictionary<string, object>();
                            paramHeader.Add("workOrder", model.workOrder);

                            var curentDataHeaderUpdate = await _servicesheetHeaderRepository.GetDataByParam(paramHeader);

                            if (curentDataHeaderUpdate == null)
                                throw new Exception($"Current data header work order {model.workOrder} not found");

                            CbmHistoryParam headerHisUpdate = new CbmHistoryParam();
                            headerHisUpdate.key = EnumGroup.General;
                            headerHisUpdate.workOrder = model.workOrder;
                            //headerHisUpdate.workOrder = model.taskKey;
                            headerHisUpdate.equipment = curentDataHeaderUpdate.equipment;
                            headerHisUpdate.taskKey = model.taskKey;
                            headerHisUpdate.siteId = curentDataHeaderUpdate.siteId;
                            headerHisUpdate.serviceSheetDetailId = currentData[0].id;
                            headerHisUpdate.defectHeaderId = "";
                            headerHisUpdate.detail = new DetailCBMHistory();

                            //add 2024-02-19
                            headerHisUpdate.modelId = rsc[EnumQuery.ModelId];
                            headerHisUpdate.psTypeId = rsc[EnumQuery.PsTypeId];
                            headerHisUpdate.taskDescription = currentData[0][EnumQuery.Description];
                            headerHisUpdate.category = currentData[0].category + " " + currentData[0].rating;
                            headerHisUpdate.currentValue = currentData[0][EnumQuery.MeasurementValue];
                            headerHisUpdate.currentRating = currentData[0][EnumQuery.TaskValue];
                            headerHisUpdate.replacementValue = currentData[0][EnumQuery.NonCbmAdjustmentReplacementMeasurementValue];
                            headerHisUpdate.replacementRating = currentData[0][EnumQuery.NonCbmAdjustmentReplacementRating];
                            headerHisUpdate.closedDate = updatedDate;
                            headerHisUpdate.closedBy = updateRequest.employee.id;
                            headerHisUpdate.source = "ip_overwrite";

                            foreach (var item in currentData)
                            {
                                item.Remove("id");
                                item.Remove("uom");
                                item.Remove("taskNo");
                                item.Remove("measurementValue");
                                item.Remove("nonCbmAdjustmentReplacementMeasurementValue");
                                item.Remove("nonCbmAdjustmentReplacementRating");
                                //item.Remove("cbmAdjustmentReplacementValue");
                            }

                            // Last Value
                            DetailServiceSheet modelUpdate = new DetailServiceSheet();
                            modelUpdate.workOrder = updateRequest.workOrder;
                            modelUpdate.taskKey = dataTaskKeyMain;

                            var _repoDetailUpdate = new SuckAndBlowDetailRepository(_connectionFactory, EnumContainer.InterimEngineDetail);
                            var currentDataUpdate = await _repoDetailUpdate.GetDataServiceSheetDetailByKey(modelUpdate);
                            //bool isCbmAdjustmentOrReplacement = currentData[0].cbmAdjustmentReplacement;

                            foreach (var item in currentDataUpdate)
                            {
                                item.Remove("id");
                                item.Remove("uom");
                                item.Remove("taskNo");
                                item.Remove("measurementValue");
                                item.Remove("nonCbmAdjustmentReplacementMeasurementValue");
                                item.Remove("nonCbmAdjustmentReplacementRating");
                                //item.Remove("cbmAdjustmentReplacementValue");

                                foreach (var itemDetail in item.items)
                                {
                                    if (updateTaskRequest.updateParams.Where(x => x.keyValue.ToString() == itemDetail.key.ToString()).Count() != 0)
                                    {
                                        var dataUpdate = updateTaskRequest.updateParams.Where(x => x.keyValue.ToString() == itemDetail.key.ToString()).FirstOrDefault();
                                        var dataDetailUpdate = dataUpdate.propertyParams.Where(x => x.propertyName.ToString() == "value").FirstOrDefault();
                                        var dataDetailUpdateBy = dataUpdate.propertyParams.Where(x => x.propertyName.ToString() == "updatedBy").FirstOrDefault();
                                        var dataDetailUpdateDate = dataUpdate.propertyParams.Where(x => x.propertyName.ToString() == "updatedDate").FirstOrDefault();

                                        if (dataDetailUpdate.propertyValue.Contains("filename"))
                                        {
                                            itemDetail.value = JsonConvert.DeserializeObject<object>(dataDetailUpdate.propertyValue);
                                        }
                                        else
                                        {
                                            itemDetail.value = dataDetailUpdate.propertyValue;
                                        }

                                        if (dataDetailUpdate.propertyValue == EnumTaskValue.CbmA || dataDetailUpdate.propertyValue == EnumTaskValue.CbmB || dataDetailUpdate.propertyValue == EnumTaskValue.CbmC || dataDetailUpdate.propertyValue == EnumTaskValue.CbmX)
                                        {
                                            item.taskValue = dataDetailUpdate.propertyValue;
                                            item.updatedBy = JsonConvert.DeserializeObject<object>(dataDetailUpdateBy.propertyValue);
                                            item.updatedDate = ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerDateTime);
                                        }

                                    }
                                }
                            }

                            //if (isCbmAdjustmentOrReplacement == false)
                            currentData.Add(currentDataUpdate[0]);

                            DetailCBMHistory detailHisUpdate = new DetailCBMHistory();
                            detailHisUpdate.key = "HISTORY";
                            detailHisUpdate.category = currentData[0].category;
                            detailHisUpdate.rating = currentData[0].rating;
                            detailHisUpdate.history = currentData;

                            headerHisUpdate.detail = detailHisUpdate;

                            var modelHeader = new CreateRequest();
                            modelHeader.employee = new EmployeeModel();

                            modelHeader.employee.id = updateRequest.employee.id;
                            modelHeader.employee.name = updateRequest.employee.name;
                            modelHeader.entity = headerHisUpdate;

                            var resultAddHeader = await _cbmHistoryRepository.Create(modelHeader);
                        }

                    }
                    #endregion
                }
                #endregion

                var result = await _repository.Update(updateRequest, rsc);

                #region History Revise if adjustment/replacement
                if (isCbmAdjustmentOrReplacement == true)
                {
                    #region History Revise
                    if (hasTaskValue)
                    {
                        Dictionary<string, object> paramHistory = new Dictionary<string, object>();
                        paramHistory.Add("workOrder", updateRequest.workOrder);
                        paramHistory.Add("taskKey", dataTaskKeyMain);

                        var dataHistory = await _cbmHistoryRepository.GetDataByParam(paramHistory);
                        if (dataHistory == null)
                        {

                            Dictionary<string, object> paramHeader = new Dictionary<string, object>();
                            paramHeader.Add("workOrder", _model.workOrder);

                            var curentDataHeaderUpdate = await _servicesheetHeaderRepository.GetDataByParam(paramHeader);

                            if (curentDataHeaderUpdate == null)
                                throw new Exception($"Current data header work order {_model.workOrder} not found");

                            CbmHistoryParam headerHisUpdate = new CbmHistoryParam();
                            headerHisUpdate.key = EnumGroup.General;
                            headerHisUpdate.workOrder = _model.workOrder;
                            //headerHisUpdate.workOrder = model.taskKey;
                            headerHisUpdate.equipment = curentDataHeaderUpdate.equipment;
                            headerHisUpdate.taskKey = _model.taskKey;
                            headerHisUpdate.siteId = curentDataHeaderUpdate.siteId;
                            headerHisUpdate.serviceSheetDetailId = _currentData[0].id;
                            headerHisUpdate.defectHeaderId = "";
                            headerHisUpdate.detail = new DetailCBMHistory();

                            //add 2024-02-19
                            headerHisUpdate.modelId = rsc[EnumQuery.ModelId];
                            headerHisUpdate.psTypeId = rsc[EnumQuery.PsTypeId];
                            headerHisUpdate.taskDescription = _currentData[0][EnumQuery.Description];
                            headerHisUpdate.category = _currentData[0].category + " " + _currentData[0].rating;
                            headerHisUpdate.currentValue = _currentData[0][EnumQuery.MeasurementValue];
                            headerHisUpdate.currentRating = _currentData[0][EnumQuery.TaskValue];
                            headerHisUpdate.replacementValue = _currentData[0][EnumQuery.NonCbmAdjustmentReplacementMeasurementValue];
                            headerHisUpdate.replacementRating = _currentData[0][EnumQuery.NonCbmAdjustmentReplacementRating];
                            headerHisUpdate.closedDate = updatedDate;
                            headerHisUpdate.closedBy = updateRequest.employee.id;
                            headerHisUpdate.source = "ip_overwrite";

                            foreach (var item in _currentData)
                            {
                                item.Remove("id");
                                item.Remove("uom");
                                item.Remove("taskNo");
                                item.Remove("measurementValue");
                                item.Remove("nonCbmAdjustmentReplacementMeasurementValue");
                                item.Remove("nonCbmAdjustmentReplacementRating");
                                //item.Remove("cbmAdjustmentReplacementValue");
                            }

                            // Last Value
                            DetailServiceSheet modelUpdate = new DetailServiceSheet();
                            modelUpdate.workOrder = updateRequest.workOrder;
                            modelUpdate.taskKey = dataTaskKeyMain;

                            var _repoDetailUpdate = new SuckAndBlowDetailRepository(_connectionFactory, EnumContainer.InterimEngineDetail);
                            var currentDataUpdate = await _repoDetailUpdate.GetDataServiceSheetDetailByKey(modelUpdate);

                            foreach (var item in currentDataUpdate)
                            {
                                item.Remove("id");
                                item.Remove("uom");
                                item.Remove("taskNo");
                                item.Remove("measurementValue");
                                item.Remove("nonCbmAdjustmentReplacementMeasurementValue");
                                item.Remove("nonCbmAdjustmentReplacementRating");
                                //item.Remove("cbmAdjustmentReplacementValue");
                            }

                            _currentData.Add(currentDataUpdate[0]);

                            DetailCBMHistory detailHisUpdate = new DetailCBMHistory();
                            detailHisUpdate.key = "HISTORY";
                            detailHisUpdate.category = _currentData[0].category;
                            detailHisUpdate.rating = _currentData[0].rating;
                            detailHisUpdate.history = _currentData;

                            headerHisUpdate.detail = detailHisUpdate;

                            var modelHeader = new CreateRequest();
                            modelHeader.employee = new EmployeeModel();

                            modelHeader.employee.id = updateRequest.employee.id;
                            modelHeader.employee.name = updateRequest.employee.name;
                            modelHeader.entity = headerHisUpdate;

                            var resultAddHeader = await _cbmHistoryRepository.Create(modelHeader);
                        }
                        else
                        {
                            var detailHistory = StaticHelper.GetPropValue(dataHistory.detail, "history");

                            List<dynamic> dataCount = JsonConvert.DeserializeObject<List<dynamic>>(JsonConvert.SerializeObject(detailHistory));

                            if (dataCount.Count() > 3)
                            {
                                return new ServiceResult
                                {
                                    Message = "Cannot revise the data, you have limit for revising 3 times.",
                                    IsError = true
                                };
                            }

                            // Last Value
                            DetailServiceSheet modelUpdate = new DetailServiceSheet();
                            modelUpdate.workOrder = updateRequest.workOrder;
                            modelUpdate.taskKey = dataTaskKeyMain;

                            var _repoDetailUpdate = new SuckAndBlowDetailRepository(_connectionFactory, EnumContainer.InterimEngineDetail);
                            var currentDataUpdate = await _repoDetailUpdate.GetDataServiceSheetDetailByKey(modelUpdate);

                            foreach (var item in currentDataUpdate)
                            {
                                item.Remove("id");
                                item.Remove("uom");
                                item.Remove("taskNo");
                                item.Remove("measurementValue");
                                item.Remove("nonCbmAdjustmentReplacementMeasurementValue");
                                item.Remove("nonCbmAdjustmentReplacementRating");
                                //item.Remove("cbmAdjustmentReplacementValue");
                            }

                            detailHistory.Add(currentDataUpdate[0]);

                            List<PropertyParam> propertyParams = new List<PropertyParam>() {
                            new PropertyParam()
                            {
                                propertyName = EnumQuery.CbmHistory,
                                propertyValue = JsonConvert.SerializeObject(detailHistory)
                            }
                    };

                            UpdateRequest updateDataParams = new UpdateRequest();
                            updateDataParams.id = dataHistory.id;
                            updateDataParams.workOrder = dataHistory.workOrder;
                            updateDataParams.updateParams = new List<UpdateParam>();
                            updateDataParams.employee = updateTaskRequest.employee;

                            updateDataParams.updateParams.Add(new UpdateParam()
                            {
                                keyValue = "HISTORY",
                                propertyParams = propertyParams
                            });

                            var resultUpdateHeader = await _cbmHistoryRepository.Update(updateDataParams, dataHistory);
                        }
                    }
                    else
                    {
                        Dictionary<string, object> paramHistory = new Dictionary<string, object>();
                        paramHistory.Add("workOrder", updateRequest.workOrder);
                        paramHistory.Add("taskKey", dataTaskKeyMain);

                        var dataHistory = await _cbmHistoryRepository.GetDataByParam(paramHistory);
                        if (dataHistory != null)
                        {

                            var detailHistory = StaticHelper.GetPropValue(dataHistory.detail, "history");

                            List<dynamic> dataCount = JsonConvert.DeserializeObject<List<dynamic>>(JsonConvert.SerializeObject(detailHistory));

                            if (dataCount.Count() > 3)
                            {
                                return new ServiceResult
                                {
                                    Message = "Cannot revise the data, you have limit for revising 3 times.",
                                    IsError = true
                                };
                            }

                            // Last Value
                            DetailServiceSheet modelUpdate = new DetailServiceSheet();
                            modelUpdate.workOrder = updateRequest.workOrder;
                            modelUpdate.taskKey = dataTaskKeyMain;

                            var _repoDetailUpdate = new SuckAndBlowDetailRepository(_connectionFactory, EnumContainer.InterimEngineDetail);
                            var currentDataUpdate = await _repoDetailUpdate.GetDataServiceSheetDetailByKey(modelUpdate);

                            foreach (var item in currentDataUpdate)
                            {
                                item.Remove("id");
                                item.Remove("uom");
                                item.Remove("taskNo");
                                item.Remove("measurementValue");
                                item.Remove("nonCbmAdjustmentReplacementMeasurementValue");
                                item.Remove("nonCbmAdjustmentReplacementRating");
                                //item.Remove("cbmAdjustmentReplacementValue");
                            }

                            detailHistory.Add(currentDataUpdate[0]);

                            List<PropertyParam> propertyParams = new List<PropertyParam>() {
                            new PropertyParam()
                                {
                                    propertyName = EnumQuery.CbmHistory,
                                    propertyValue = JsonConvert.SerializeObject(detailHistory)
                                }
                        };

                            UpdateRequest updateDataParams = new UpdateRequest();
                            updateDataParams.id = dataHistory.id;
                            updateDataParams.workOrder = dataHistory.workOrder;
                            updateDataParams.updateParams = new List<UpdateParam>();
                            updateDataParams.employee = updateTaskRequest.employee;

                            updateDataParams.updateParams.Add(new UpdateParam()
                            {
                                keyValue = "HISTORY",
                                propertyParams = propertyParams
                            });

                            var resultUpdateHeader = await _cbmHistoryRepository.Update(updateDataParams, dataHistory);
                            //var currentDataUpdate = await _repoDetailUpdate.GetDataItemsDetailValue(modelUpdate);
                        }
                        else
                        {

                            Dictionary<string, object> paramHeader = new Dictionary<string, object>();
                            paramHeader.Add("workOrder", _model.workOrder);

                            var curentDataHeaderUpdate = await _servicesheetHeaderRepository.GetDataByParam(paramHeader);

                            if (curentDataHeaderUpdate == null)
                                throw new Exception($"Current data header work order {_model.workOrder} not found");

                            CbmHistoryParam headerHisUpdate = new CbmHistoryParam();
                            headerHisUpdate.key = EnumGroup.General;
                            headerHisUpdate.workOrder = _model.workOrder;
                            //headerHisUpdate.workOrder = model.taskKey;
                            headerHisUpdate.equipment = curentDataHeaderUpdate.equipment;
                            headerHisUpdate.taskKey = _model.taskKey;
                            headerHisUpdate.siteId = curentDataHeaderUpdate.siteId;
                            headerHisUpdate.serviceSheetDetailId = _currentData[0].id;
                            headerHisUpdate.defectHeaderId = "";
                            headerHisUpdate.detail = new DetailCBMHistory();

                            //add 2024-02-19
                            headerHisUpdate.modelId = rsc[EnumQuery.ModelId];
                            headerHisUpdate.psTypeId = rsc[EnumQuery.PsTypeId];
                            headerHisUpdate.taskDescription = _currentData[0][EnumQuery.Description];
                            headerHisUpdate.category = _currentData[0].category + " " + _currentData[0].rating;
                            headerHisUpdate.currentValue = _currentData[0][EnumQuery.MeasurementValue];
                            headerHisUpdate.currentRating = _currentData[0][EnumQuery.TaskValue];
                            headerHisUpdate.replacementValue = _currentData[0][EnumQuery.NonCbmAdjustmentReplacementMeasurementValue];
                            headerHisUpdate.replacementRating = _currentData[0][EnumQuery.NonCbmAdjustmentReplacementRating];
                            headerHisUpdate.closedDate = updatedDate;
                            headerHisUpdate.closedBy = updateRequest.employee.id;
                            headerHisUpdate.source = "ip_overwrite";

                            foreach (var item in _currentData)
                            {
                                item.Remove("id");
                                item.Remove("uom");
                                item.Remove("taskNo");
                                item.Remove("measurementValue");
                                item.Remove("nonCbmAdjustmentReplacementMeasurementValue");
                                item.Remove("nonCbmAdjustmentReplacementRating");
                                //item.Remove("cbmAdjustmentReplacementValue");
                            }

                            // Last Value
                            DetailServiceSheet modelUpdate = new DetailServiceSheet();
                            modelUpdate.workOrder = updateRequest.workOrder;
                            modelUpdate.taskKey = dataTaskKeyMain;

                            var _repoDetailUpdate = new SuckAndBlowDetailRepository(_connectionFactory, EnumContainer.InterimEngineDetail);
                            var currentDataUpdate = await _repoDetailUpdate.GetDataServiceSheetDetailByKey(modelUpdate);

                            foreach (var item in currentDataUpdate)
                            {
                                item.Remove("id");
                                item.Remove("uom");
                                item.Remove("taskNo");
                                item.Remove("measurementValue");
                                item.Remove("nonCbmAdjustmentReplacementMeasurementValue");
                                item.Remove("nonCbmAdjustmentReplacementRating");
                                //item.Remove("cbmAdjustmentReplacementValue");
                            }

                            _currentData.Add(currentDataUpdate[0]);

                            DetailCBMHistory detailHisUpdate = new DetailCBMHistory();
                            detailHisUpdate.key = "HISTORY";
                            detailHisUpdate.category = _currentData[0].category;
                            detailHisUpdate.rating = _currentData[0].rating;
                            detailHisUpdate.history = _currentData;

                            headerHisUpdate.detail = detailHisUpdate;

                            var modelHeader = new CreateRequest();
                            modelHeader.employee = new EmployeeModel();

                            modelHeader.employee.id = updateRequest.employee.id;
                            modelHeader.employee.name = updateRequest.employee.name;
                            modelHeader.entity = headerHisUpdate;

                            var resultAddHeader = await _cbmHistoryRepository.Create(modelHeader);
                        }
                    }
                    #endregion
                }
                #endregion

                #region Update Header


                //string updatedDate = ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerDateTime);

                List<PropertyParam> propertyParamsHeader = new List<PropertyParam>() {
                new PropertyParam()
                    {
                        propertyName = EnumQuery.UpdatedDate,
                        propertyValue = updatedDate
                    }
                };

                UpdateRequest updateDataParamsHeader = new UpdateRequest();
                updateDataParamsHeader.id = rsc.headerId;
                updateDataParamsHeader.workOrder = rsc.workOrder;
                updateDataParamsHeader.updateParams = new List<UpdateParam>();
                updateDataParamsHeader.employee = updateTaskRequest.employee;

                updateDataParamsHeader.updateParams.Add(new UpdateParam()
                {
                    keyValue = "GENERAL",
                    propertyParams = propertyParamsHeader
                });

                var resultHeader = await _servicesheetHeaderRepository.Update(updateDataParamsHeader, header);
                #endregion

                #region Delete Ext Defect

                //await DeleteExtDefect(updateParam.keyValue, updateTaskDefectRequest.workorder, updateTaskDefectRequest.employee);

                #endregion

                return new ServiceResult
                {
                    Message = "Data updated successfully",
                    IsError = false,
                    Content = null
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

        public async Task<ServiceResult> UpdateTaskWithDefectReviseInterim(UpdateTaskDefectReviseRequest updateTaskDefectRequest)
        {
            try
            {
                #region Validation
                //_logger.LogWarning("Validation");

                var header = await _servicesheetHeaderRepository.Get(updateTaskDefectRequest.headerId);
                var status = string.Empty;
                string dataTaskKeyMain = updateTaskDefectRequest.taskKey == null ? updateTaskDefectRequest.defectHeader.taskId : updateTaskDefectRequest.taskKey;
                bool hasTaskValue = false;
                string taskValueData = string.Empty;

                UpdateParam defectParam = new UpdateParam();
                var rsc = await _repository.Get(updateTaskDefectRequest.id);

                foreach (var updateParam in updateTaskDefectRequest.updateParams)
                {
                    foreach (var propertyParam in updateParam.propertyParams)
                    {
                        if (propertyParam.propertyName.ToLower() == EnumQuery.TaskValue.ToLower())
                        {
                            taskValueData = propertyParam.propertyValue;
                            hasTaskValue = true;
                        }

                        string errMsg = await ValidationInterim(new ValidationRequest()
                        {
                            rsc = rsc,
                            //id = updateTaskDefectRequest.id,
                            keyValue = updateParam.keyValue,
                            propertyName = propertyParam.propertyName,
                            propertyValue = propertyParam.propertyValue,
                            //employeeId = updateTaskDefectRequest.employee.id,
                            defectHeader = updateTaskDefectRequest.defectHeader,
                            defectDetail = updateTaskDefectRequest.defectDetail,
                            isDefect = true,
                            headerId = updateTaskDefectRequest.headerId,
                            workorder = updateTaskDefectRequest.workorder == null ? updateTaskDefectRequest.workOrder : updateTaskDefectRequest.workorder,
                            employee = updateTaskDefectRequest.employee
                        });

                        if (!string.IsNullOrEmpty(errMsg))
                        {
                            return new ServiceResult
                            {
                                Message = errMsg,
                                IsError = true
                            };
                        }

                        if (propertyParam.propertyName.ToLower() == EnumQuery.TaskValue.ToLower())
                            defectParam = updateParam;
                    }
                }

                #endregion

                #region Defect

                if (defectParam != null)
                {
                    #region Delete Ext Defect

                    await DeleteExtDefect(defectParam.keyValue, updateTaskDefectRequest.workorder, updateTaskDefectRequest.employee);

                    #endregion

                    #region Create Defect Header
                    //_logger.LogWarning("Update Defect Header");

                    updateTaskDefectRequest.defectHeader.taskId = defectParam.keyValue;
                    updateTaskDefectRequest.defectHeader.statusHistory = new List<StatusHistoryModel>() {
                        new StatusHistoryModel(){
                            status = EnumStatus.DefectSubmit,
                            updatedBy = updateTaskDefectRequest.employee,
                            tsUpdatedDate = ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerDateTime),
                            updatedDate = ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerTimeStamp)
                        }
                    };

                    CreateRequest createHeaderRequest = new CreateRequest()
                    {
                        employee = updateTaskDefectRequest.employee,
                        entity = updateTaskDefectRequest.defectHeader
                    };

                    Dictionary<string, string> adjustHeaderFields = new Dictionary<string, string>();
                    adjustHeaderFields.Add(EnumQuery.Key, Guid.NewGuid().ToString());

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
                    //_logger.LogWarning("Update Defect Detail");

                    if (updateTaskDefectRequest.defectDetail != null)
                    {
                        string defectDetailString = JsonConvert.SerializeObject(updateTaskDefectRequest.defectDetail);
                        defectDetailString = defectDetailString.Replace(EnumCommonProperty.ServerDateTime, ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerDateTime));
                        defectDetailString = defectDetailString.Replace(EnumCommonProperty.ServerTimeStamp, ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerTimeStamp));
                        JObject defectDetail = JObject.Parse(defectDetailString);
                        defectDetail.Add(EnumQuery.Key, Guid.NewGuid().ToString());

                        JObject detailObject = new JObject();
                        detailObject.Add(EnumQuery.Detail, defectDetail);

                        CreateRequest createDetailRequest = new CreateRequest()
                        {
                            employee = updateTaskDefectRequest.employee,
                            entity = detailObject
                        };

                        Dictionary<string, string> adjustDetailFields = new Dictionary<string, string>();
                        adjustDetailFields.Add(EnumQuery.Key, Guid.NewGuid().ToString());
                        adjustDetailFields.Add(EnumQuery.Workorder, updateTaskDefectRequest.defectHeader.workorder);
                        adjustDetailFields.Add(EnumQuery.DefectHeaderId, defectHeaderId);
                        adjustDetailFields.Add(EnumQuery.ServicesheetDetailId, updateTaskDefectRequest.defectHeader.serviceSheetDetailId);
                        adjustDetailFields.Add(EnumQuery.InterventionId, updateTaskDefectRequest.defectHeader.interventionId);
                        adjustDetailFields.Add(EnumQuery.InterventionHeaderId, updateTaskDefectRequest.defectHeader.interventionHeaderId);
                        adjustDetailFields.Add(EnumQuery.TaskId, updateTaskDefectRequest.defectHeader.taskId);

                        var defectDetailResult = await _defectDetailRepository.Create(createDetailRequest, adjustDetailFields);
                        updateTaskDefectRequest.defectDetail = defectDetail;
                    }

                    #endregion
                }

                #endregion

                UpdateRequest updateRequest = new UpdateRequest()
                {
                    id = updateTaskDefectRequest.id,
                    workOrder = updateTaskDefectRequest.workorder == null ? updateTaskDefectRequest.workOrder : updateTaskDefectRequest.workorder,
                    updateParams = updateTaskDefectRequest.updateParams,
                    employee = updateTaskDefectRequest.employee
                };

                //_logger.LogWarning("Update Service Sheet Detail");

                var result = await _repository.Update(updateRequest, rsc);

                return new ServiceResult
                {
                    Message = "Data updated successfully",
                    IsError = false,
                    Content = updateTaskDefectRequest
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
        #endregion

        #region Validation

        public async Task<string> Validation(ValidationRequest validationRequest)
        {
            string groupName = validationRequest.rsc.groupName;
            string result = string.Empty;
            bool isTaskValue = true;
            validationRequest.propertyValue = ((RepositoryBase)_repository).GetSettingValue(validationRequest.propertyValue);

            var dbTaskValue = StaticHelper.GetPropValue(validationRequest.rsc, validationRequest.keyValue, EnumQuery.TaskValue);
            if (dbTaskValue == null)
            {
                isTaskValue = false;
                dbTaskValue = StaticHelper.GetPropValue(validationRequest.rsc, validationRequest.keyValue, EnumQuery.Value);
            }
            if (dbTaskValue is JArray)
            {
                dbTaskValue = dbTaskValue.ToString();
            }
            //string dbRating = StaticHelper.GetPropValue(validationRequest.rsc, validationRequest.keyValue, EnumQuery.Rating);

            try
            {
                #region Validation Value to TaskValue
                if (validationRequest.propertyName.ToLower() == EnumQuery.Value.ToLower() && !string.IsNullOrEmpty(validationRequest.propertyValue))
                {
                    var _repoDetail = new ServiceSheetDetailRepository(_connectionFactory, EnumContainer.ServiceSheetDetail);
                    var resultData = await _repoDetail.GetDataItemsDetailValue(validationRequest.rsc.id.ToString(), validationRequest.keyValue);

                    List<dynamic> dataValue = JsonConvert.DeserializeObject<List<dynamic>>(JsonConvert.SerializeObject(resultData));
                    if (dataValue.Any())
                    {
                        if (dataValue.FirstOrDefault().isTaskValue != null && (bool)dataValue.FirstOrDefault().isTaskValue)
                        {
                            var dataDetail = dataValue.FirstOrDefault();
                            //isTaskValue = dataValue.FirstOrDefault().isTaskValue;

                            string updatedById = dataValue.FirstOrDefault().updatedBy.ToString() == "" ? "" : dataValue.FirstOrDefault().updatedBy.id;

                            bool isMovement = false;
                            if (dataDetail.taskValue == EnumTaskValue.NormalNotOK && dataDetail.value == EnumTaskValue.NormalOK)
                            {
                                isMovement = true;
                            }

                            if (!string.IsNullOrEmpty(updatedById) && validationRequest.employee.id != updatedById)
                            {
                                string taskCategory = dataValue.FirstOrDefault().category;

                                if (!isMovement && string.IsNullOrEmpty(validationRequest.reason))
                                {
                                    if (!(_container == EnumContainer.Intervention && taskCategory == EnumCategoryServiceSheet.NORMAL && dbTaskValue == EnumTaskValue.IntNormalNA) //NA intervention
                                    && !(_container != EnumContainer.Intervention && taskCategory == EnumCategoryServiceSheet.NORMAL && dbTaskValue == EnumTaskValue.NormalNA) //NA serviceSheet
                                    && !(taskCategory == EnumCategoryServiceSheet.CBM) // cbm
                                    && !(taskCategory == EnumCategoryServiceSheet.CRACK && dbTaskValue == EnumTaskValue.CrackOK && (validationRequest.propertyValue == EnumTaskValue.CrackNotOKYes || validationRequest.propertyValue == EnumTaskValue.CrackNotOKNo)) // no crack to crack monitor && // no crack to crack repair
                                    && !(taskCategory == EnumCategoryServiceSheet.CRACK && dbTaskValue == EnumTaskValue.CrackNA)) // N/A to crack (1,2,3)
                                        throw new Exception($"Task already updated by other mechanic!");
                                }


                                // check have reason
                                //if (isTaskValue && ((_container == EnumContainer.Intervention && taskCategory == EnumCategoryServiceSheet.NORMAL && dbTaskValue == EnumTaskValue.IntNormalNA) //NA intervention
                                //    || (_container != EnumContainer.Intervention && taskCategory == EnumCategoryServiceSheet.NORMAL && dbTaskValue == EnumTaskValue.NormalNA) //NA servicesheet
                                //    || (taskCategory == EnumCategoryServiceSheet.CRACK && dbTaskValue == EnumTaskValue.CrackNA))) //NA crack
                                //{
                                //    bool isHaveReason = false;
                                //    foreach (var propertyParam in validationRequest.propertyParams)
                                //    {
                                //        if (propertyParam.propertyName == EnumQuery.Reason && propertyParam.propertyValue != String.Empty)
                                //        {
                                //            isHaveReason = true;
                                //        }
                                //    }
                                //    if (!isHaveReason)
                                //    {
                                //        throw new Exception($"You cannot change Not Applicable without a reason, please retry!");
                                //    }
                                //}
                            }
                        }
                        else
                        {
                            string taskCategory = dataValue.FirstOrDefault().category;

                            if (dataValue.FirstOrDefault().style.placeholder != null && dataValue.FirstOrDefault().style.placeholder.ToString().Contains("Additional Information (if required)"))
                            {
                                string updatedById = dataValue.FirstOrDefault().updatedBy.ToString() == "" ? "" : dataValue.FirstOrDefault().updatedBy.id;

                                if (!string.IsNullOrEmpty(updatedById) && validationRequest.employee.id != updatedById)
                                    throw new Exception($"Task already updated by other mechanic!");
                            }
                            #region validation update task defect

                            var parentTaskValue = StaticHelper.GetPropValue(validationRequest.rsc, validationRequest.taskKey, EnumQuery.TaskValue);
                            if (_container == EnumContainer.Intervention && parentTaskValue == EnumTaskValue.IntNormalNotOK // Defect to anything
                                || _container != EnumContainer.Intervention && parentTaskValue == EnumTaskValue.NormalNotOK // Defect to anything 
                            || taskCategory == EnumCategoryServiceSheet.CRACK && dbTaskValue == EnumTaskValue.CrackNotOKNo) // Defect to anything

                            {
                                Dictionary<string, object> param = new Dictionary<string, object>();
                                param.Add(EnumQuery.TaskId, validationRequest.taskKey);
                                param.Add(EnumQuery.Workorder, validationRequest.workorder);
                                param.Add(EnumQuery.IsActive, "true");
                                param.Add(EnumQuery.IsDeleted, "false");

                                var defects = await _defectHeaderRepository.GetDataListByParam(param);

                                if (defects != null)
                                {
                                    foreach (var defect in defects)
                                    {
                                        var status = StaticHelper.GetPropValue(defect, EnumQuery.Status);
                                        if (status != EnumStatus.DefectSubmit && status != EnumStatus.DefectSubmited)
                                        {
                                            throw new Exception($"You cannot modify the defect once already approved or declined by Supervisor");
                                        }
                                    }
                                }
                            }
                            #endregion
                        }
                    }
                }
                #endregion

                #region Validation Task Value

                if (validationRequest.propertyName.ToLower() == EnumQuery.TaskValue.ToLower() && !string.IsNullOrEmpty(validationRequest.propertyValue))
                {
                    var updatedBy = StaticHelper.GetPropValue(validationRequest.rsc, validationRequest.keyValue, EnumQuery.UpdatedBy);
                    string updatedById = StaticHelper.GetPropValue(updatedBy, EnumQuery.ID);
                    string taskCategory = StaticHelper.GetPropValue(validationRequest.rsc, validationRequest.keyValue, EnumQuery.Category);

                    if (!string.IsNullOrEmpty(updatedById) && validationRequest.employee.id != updatedById)
                    {
                        if (!(_container == EnumContainer.Intervention && taskCategory == EnumCategoryServiceSheet.NORMAL && dbTaskValue == EnumTaskValue.IntNormalNA) //NA intervention
                            && !(_container != EnumContainer.Intervention && taskCategory == EnumCategoryServiceSheet.NORMAL && dbTaskValue == EnumTaskValue.NormalNA) //NA serviceSheet
                            && !(taskCategory == EnumCategoryServiceSheet.CBM) // cbm
                            && !(taskCategory == EnumCategoryServiceSheet.CRACK && dbTaskValue == EnumTaskValue.CrackOK && (validationRequest.propertyValue == EnumTaskValue.CrackNotOKYes || validationRequest.propertyValue == EnumTaskValue.CrackNotOKNo)) // no crack to crack monitor && // no crack to crack repair
                            && !(taskCategory == EnumCategoryServiceSheet.CRACK && dbTaskValue == EnumTaskValue.CrackNA)) // N/A to crack (1,2,3)
                            throw new Exception($"Task already updated by other mechanic!");

                        // check have reason
                        if (_container == EnumContainer.Intervention && taskCategory == EnumCategoryServiceSheet.NORMAL && dbTaskValue == EnumTaskValue.IntNormalNA //NA intervention
                            || _container != EnumContainer.Intervention && taskCategory == EnumCategoryServiceSheet.NORMAL && dbTaskValue == EnumTaskValue.NormalNA //NA servicesheet
                            || taskCategory == EnumCategoryServiceSheet.CRACK && dbTaskValue == EnumTaskValue.CrackNA) //NA crack
                        {
                            bool isHaveReason = false;
                            foreach (var propertyParam in validationRequest.propertyParams)
                            {
                                if (propertyParam.propertyName == EnumQuery.Reason && propertyParam.propertyValue != string.Empty)
                                {
                                    isHaveReason = true;
                                }
                            }
                            if (!isHaveReason)
                            {
                                throw new Exception($"You cannot change Not Applicable without a reason, please retry!");
                            }
                        }
                    }
                    #region validation update task defect
                    if (_container == EnumContainer.Intervention && taskCategory == EnumCategoryServiceSheet.NORMAL && dbTaskValue == EnumTaskValue.IntNormalNotOK // Defect to anything
                        || _container != EnumContainer.Intervention && taskCategory == EnumCategoryServiceSheet.NORMAL && dbTaskValue == EnumTaskValue.NormalNotOK // Defect to anything
                        || taskCategory == EnumCategoryServiceSheet.CRACK && dbTaskValue == EnumTaskValue.CrackNotOKNo) // Defect to anything

                    {
                        Dictionary<string, object> param = new Dictionary<string, object>();
                        param.Add(EnumQuery.TaskId, validationRequest.keyValue);
                        param.Add(EnumQuery.Workorder, validationRequest.workorder);
                        param.Add(EnumQuery.IsActive, "true");
                        param.Add(EnumQuery.IsDeleted, "false");

                        var defects = await _defectHeaderRepository.GetDataListByParam(param);

                        if (defects != null)
                        {
                            foreach (var defect in defects)
                            {
                                var status = StaticHelper.GetPropValue(defect, EnumQuery.Status);
                                if (status != EnumStatus.DefectSubmit && status != EnumStatus.DefectSubmited)
                                {
                                    throw new Exception($"You cannot modify the defect once already approved or declined by Supervisor");
                                }
                            }
                        }
                    }
                    #endregion

                    //if (!string.IsNullOrEmpty(updatedById) && validationRequest.employee.id != updatedById)
                    //{
                    //    throw new Exception($"Task already updated by other mechanic!");
                    //}
                }

                #region validation update reset defect
                if (validationRequest.propertyName == EnumQuery.TaskValue && string.IsNullOrEmpty(validationRequest.propertyValue)) //reset dropdown
                {
                    Dictionary<string, object> param = new Dictionary<string, object>();
                    param.Add(EnumQuery.TaskId, validationRequest.keyValue);
                    param.Add(EnumQuery.Workorder, validationRequest.workorder);
                    param.Add(EnumQuery.IsActive, "true");
                    param.Add(EnumQuery.IsDeleted, "false");

                    var defects = await _defectHeaderRepository.GetDataListByParam(param);

                    if (defects != null)
                    {
                        foreach (var defect in defects)
                        {
                            var status = StaticHelper.GetPropValue(defect, EnumQuery.Status);
                            if (status != EnumStatus.DefectSubmit && status != EnumStatus.DefectSubmited)
                            {
                                throw new Exception($"You cannot modify the defect once already approved or declined by Supervisor");
                            }
                        }
                    }
                }
                #endregion

                #endregion

                #region Task Value Validation

                if (validationRequest.propertyName.ToLower() == EnumQuery.TaskValue.ToLower())
                {
                    if (_container == EnumContainer.Intervention)
                    {
                        if (validationRequest.propertyValue != EnumTaskValue.InSpec && validationRequest.propertyValue != EnumTaskValue.OutOfSpec)
                        {
                            if (validationRequest.isDefect)
                            {
                                if (validationRequest.propertyValue == EnumTaskValue.IntNormalOK ||
                                    validationRequest.propertyValue == EnumTaskValue.IntNormalCompleted ||
                                    validationRequest.propertyValue == EnumTaskValue.CbmA ||
                                    validationRequest.propertyValue == EnumTaskValue.CbmB ||
                                    validationRequest.propertyValue == EnumTaskValue.CrackOK)
                                {
                                    throw new Exception($"Task value with {EnumQuery.Key} {validationRequest.keyValue} is not defect!");
                                }
                            }
                            else
                            {
                                if (validationRequest.propertyValue != EnumTaskValue.IntNormalOK &&
                                    validationRequest.propertyValue != EnumTaskValue.IntNormalCompleted &&
                                    validationRequest.propertyValue != EnumTaskValue.CbmA &&
                                    validationRequest.propertyValue != EnumTaskValue.CbmB &&
                                    validationRequest.propertyValue != EnumTaskValue.CrackOK &&
                                    validationRequest.propertyValue != EnumTaskValue.CalibrationComplete &&
                                    validationRequest.propertyValue != EnumTaskValue.CalibrationYes &&
                                    !string.IsNullOrEmpty(validationRequest.propertyValue))
                                {
                                    throw new Exception($"Task value with {EnumQuery.Key} {validationRequest.keyValue} is defect!");
                                }
                            }
                        }
                    }
                    else
                    {
                        if (validationRequest.isDefect)
                        {
                            if (validationRequest.propertyValue == EnumTaskValue.NormalOK ||
                                validationRequest.propertyValue == EnumTaskValue.CbmA ||
                                validationRequest.propertyValue == EnumTaskValue.CbmB ||
                                validationRequest.propertyValue == EnumTaskValue.CrackOK)
                            {
                                throw new Exception($"Task value with {EnumQuery.Key} {validationRequest.keyValue} is not defect!");
                            }
                        }
                        else
                        {
                            if (validationRequest.propertyValue != EnumTaskValue.NormalOK &&
                                validationRequest.propertyValue != EnumTaskValue.CbmA &&
                                validationRequest.propertyValue != EnumTaskValue.CbmB &&
                                validationRequest.propertyValue != EnumTaskValue.CrackOK &&
                                validationRequest.propertyValue != EnumTaskValue.CalibrationComplete &&
                                validationRequest.propertyValue != EnumTaskValue.CalibrationYes &&
                                !string.IsNullOrEmpty(validationRequest.propertyValue) &&
                                !validationRequest.propertyValue.Contains(EnumQuery.Filename)) //for image new Type double cbm
                            {
                                throw new Exception($"Task value with {EnumQuery.Key} {validationRequest.keyValue} is defect!");
                            }
                        }
                    }

                    //if (_container == EnumContainer.Intervention)
                    //{
                    //    if (validationRequest.propertyValue != EnumTaskValue.InSpec && validationRequest.propertyValue != EnumTaskValue.OutOfSpec)
                    //    {
                    //        if (!string.IsNullOrEmpty(dbTaskValue) && dbRating != EnumTaskType.RatingNormal &&
                    //            dbTaskValue.Replace("'", string.Empty) == EnumTaskValue.IntNormalNotOK)
                    //        //dbTaskValue.Replace("'", string.Empty) == EnumTaskValue.IntNormalNA)
                    //        {
                    //            throw new Exception($"Task value with {EnumQuery.Key} {validationRequest.keyValue} is already defect!");
                    //        }
                    //    }
                    //}
                    //else
                    //{
                    //    if (!string.IsNullOrEmpty(dbTaskValue) && dbRating != EnumTaskType.RatingNormal &&
                    //        (dbTaskValue.Replace("'", string.Empty) == EnumTaskValue.NormalNotOK ||
                    //        //dbTaskValue.Replace("'", string.Empty) == EnumTaskValue.NormalNA || -- Can change N/A to OK, System Working
                    //        dbTaskValue.Replace("'", string.Empty) == EnumTaskValue.CrackNotOKYes ||
                    //        (dbTaskValue.Replace("'", string.Empty) == EnumTaskValue.CrackNotOKNo && groupName == EnumGroup.ChassisCrackService)) /* Only Chassis Crack */
                    //        //dbTaskValue.Replace("'", string.Empty) == EnumTaskValue.CrackNA) -- Can change N/A to OK, System Working
                    //        )
                    //    {
                    //        throw new Exception($"Task value with {EnumQuery.Key} {validationRequest.keyValue} is already defect!");
                    //    }
                    //}
                }

                #endregion

                #region Defect

                if (validationRequest.propertyName.ToLower() == EnumQuery.TaskValue.ToLower() && _container != EnumContainer.CalibrationDetail ||
                    validationRequest.propertyName.ToLower() == EnumQuery.TaskValueLeak.ToLower() ||
                    validationRequest.propertyName.ToLower() == EnumQuery.TaskValueMounting.ToLower())
                {
                    await DeleteExtDefect(validationRequest.keyValue, validationRequest.workorder, validationRequest.employee, validationRequest.propertyName);
                }

                #endregion

                #region Data Type Validation

                //GetFieldValueRequest valueTypeRequest = new GetFieldValueRequest()
                //{
                //    id = validationRequest.id,
                //    keyValue = validationRequest.keyValue,
                //    propertyName = EnumQuery.ValueType
                //};

                //string valueType = await _repository.GetFieldValue(valueTypeRequest, true);

                //if (!string.IsNullOrEmpty(valueType))
                //{
                //    bool isValidValueType = false;

                //    if (valueType.ToLower() == EnumDataType.String.ToLower())
                //    {
                //        isValidValueType = validationRequest.propertyValue is string;
                //    }
                //    else if (valueType.ToLower() == EnumDataType.AlphaNumeric.ToLower())
                //    {
                //        Regex regex = new Regex("^[a-zA-Z0-9]*$");
                //        isValidValueType = regex.IsMatch(validationRequest.propertyValue);
                //    }
                //    else if (valueType.ToLower() == EnumDataType.Integer.ToLower())
                //    {
                //        isValidValueType = int.TryParse(validationRequest.propertyValue, out int value);
                //    }
                //    else if (valueType.ToLower() == EnumDataType.Float.ToLower())
                //    {
                //        isValidValueType = float.TryParse(validationRequest.propertyValue, out float value);
                //    }
                //    else if (valueType.ToLower() == EnumDataType.Boolean.ToLower())
                //    {
                //        isValidValueType = bool.TryParse(validationRequest.propertyValue, out bool value);
                //    }
                //    else if (valueType.ToLower() == EnumDataType.DateTime.ToLower())
                //    {
                //        string datetimeFormat = EnumFormatting.DateTimeToString;
                //        isValidValueType = DateTime.TryParseExact(validationRequest.propertyValue, datetimeFormat, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime value);
                //    }
                //    else if (valueType.ToLower() == EnumDataType.Date.ToLower())
                //    {
                //        string dateFormat = EnumFormatting.DateToString;
                //        isValidValueType = DateTime.TryParseExact(validationRequest.propertyValue, dateFormat, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime value);
                //    }
                //    else if (valueType.ToLower() == EnumDataType.Time.ToLower())
                //    {
                //        isValidValueType = TimeSpan.TryParse(validationRequest.propertyValue, out TimeSpan value);
                //    }
                //    else if (valueType.ToLower() == EnumDataType.Object.ToLower())
                //    {
                //        var token = JToken.Parse(validationRequest.propertyValue);
                //        isValidValueType = token is JObject;
                //    }
                //    else if (valueType.ToLower() == EnumDataType.Array.ToLower())
                //    {
                //        var token = JToken.Parse(validationRequest.propertyValue);
                //        isValidValueType = token is JArray;
                //    }
                //    else
                //    {
                //        throw new Exception($"Data type { valueType } undefined!");
                //    }

                //    if (!isValidValueType)
                //        throw new Exception($"Data type of { validationRequest.propertyName } property with key { validationRequest.keyValue } must be { valueType }!");
                //}

                #endregion

                #region Defect Labours Validation

                var labourData = validationRequest.defectDetail?.labours?.Value;

                if (labourData != null)
                {
                    List<LabourModel> labours = JsonConvert.DeserializeObject<List<LabourModel>>(labourData);

                    foreach (var labour in labours)
                    {
                        Regex regex = new Regex(@"^(?:\d{0,5}\.\d{1,2})$|^\d{0,5}$");
                        bool validQty = regex.IsMatch(labour.qty);
                        bool validHireEach = regex.IsMatch(labour.hireEach);
                        bool validTotalHours = regex.IsMatch(labour.totalHours);

                        if (!validQty || !validHireEach)
                            throw new Exception("Data format of Qty, Hire Each, Total Hours must be Decimal(5,2)");
                    }
                }

                #endregion

                #region Custom Validation

                if (validationRequest.propertyName == EnumQuery.Value)
                {
                    string validation = StaticHelper.GetPropValue(validationRequest.rsc, validationRequest.keyValue, EnumQuery.CustomValidation);

                    if (!string.IsNullOrEmpty(validation))
                    {
                        List<string> customValidations = validation.Split(" | ").ToList();

                        foreach (string customValidation in customValidations)
                        {
                            List<string> param = new List<string>() {
                                validationRequest.keyValue,
                                validationRequest.propertyName,
                                validationRequest.propertyValue
                            };

                            string[] arrCustomValidation = customValidation.Split(" : ");
                            string funcName = arrCustomValidation[0];

                            if (arrCustomValidation.Length > 1)
                                param.AddRange(arrCustomValidation[1]?.Split(", ").ToList());

                            GetType().GetMethod(funcName)?.Invoke(this, param.ToArray());
                        }
                    }
                }

                #endregion

            }
            catch (Exception ex)
            {
                result = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            }

            return result;
        }

        public async Task<string> ValidationForm(ValidationRequest validationRequest)
        {
            string groupName = validationRequest.rsc.groupName;
            string result = string.Empty;
            validationRequest.propertyValue = ((RepositoryBase)_repository).GetSettingValue(validationRequest.propertyValue);

            try
            {
                #region Updated By Validation

                //if (validationRequest.propertyName.ToLower() == EnumQuery.TaskValue.ToLower())
                //{
                //    GetFieldValueRequest updatedByRequest = new GetFieldValueRequest()
                //    {
                //        id = validationRequest.id,
                //        keyValue = validationRequest.keyValue,
                //        propertyName = $"{EnumQuery.UpdatedBy}"
                //    };

                //    var updatedBy = await _repository.GetFieldValue(updatedByRequest, true);
                //    string updatedById = StaticHelper.GetPropValue(updatedBy, EnumQuery.ID);

                //    if (!string.IsNullOrEmpty(updatedById) && validationRequest.employeeId != updatedById)
                //        throw new Exception($"Task already updated by other mechanic!");
                //}

                #endregion

                #region Task Value Validation

                if (validationRequest.propertyName.ToLower() == EnumQuery.TaskValue.ToLower())
                {
                    if (_container == EnumContainer.Intervention)
                    {
                        if (validationRequest.isDefect)
                        {
                            if (validationRequest.propertyValue == EnumTaskValue.IntNormalOK ||
                                validationRequest.propertyValue == EnumTaskValue.IntNormalCompleted ||
                                validationRequest.propertyValue == EnumTaskValue.CbmA ||
                                validationRequest.propertyValue == EnumTaskValue.CbmB ||
                                validationRequest.propertyValue == EnumTaskValue.CrackOK)
                            {
                                throw new Exception($"Task value with {EnumQuery.Key} {validationRequest.keyValue} is not defect!");
                            }
                        }
                        else
                        {
                            if (validationRequest.propertyValue != EnumTaskValue.IntNormalOK &&
                                validationRequest.propertyValue != EnumTaskValue.IntNormalCompleted &&
                                validationRequest.propertyValue != EnumTaskValue.CbmA &&
                                validationRequest.propertyValue != EnumTaskValue.CbmB &&
                                validationRequest.propertyValue != EnumTaskValue.CrackOK &&
                                validationRequest.propertyValue != EnumTaskValue.CalibrationComplete &&
                                validationRequest.propertyValue != EnumTaskValue.CalibrationYes &&
                                !string.IsNullOrEmpty(validationRequest.propertyValue))
                            {
                                throw new Exception($"Task value with {EnumQuery.Key} {validationRequest.keyValue} is defect!");
                            }
                        }
                    }
                    else
                    {
                        if (validationRequest.isDefect)
                        {
                            if (validationRequest.propertyValue == EnumTaskValue.NormalOK ||
                                validationRequest.propertyValue == EnumTaskValue.CbmA ||
                                validationRequest.propertyValue == EnumTaskValue.CbmB ||
                                validationRequest.propertyValue == EnumTaskValue.CrackOK)
                            {
                                throw new Exception($"Task value with {EnumQuery.Key} {validationRequest.keyValue} is not defect!");
                            }
                        }
                        else
                        {
                            if (validationRequest.propertyValue != EnumTaskValue.NormalOK &&
                                validationRequest.propertyValue != EnumTaskValue.CbmA &&
                                validationRequest.propertyValue != EnumTaskValue.CbmB &&
                                validationRequest.propertyValue != EnumTaskValue.CrackOK &&
                                validationRequest.propertyValue != EnumTaskValue.CalibrationComplete &&
                                validationRequest.propertyValue != EnumTaskValue.CalibrationYes &&
                                !string.IsNullOrEmpty(validationRequest.propertyValue))
                            {
                                throw new Exception($"Task value with {EnumQuery.Key} {validationRequest.keyValue} is defect!");
                            }
                        }
                    }

                    string dbTaskValue = StaticHelper.GetPropValue(validationRequest.rsc, validationRequest.keyValue, EnumQuery.TaskValue);
                    string dbRating = StaticHelper.GetPropValue(validationRequest.rsc, validationRequest.keyValue, EnumQuery.Rating);

                    if (_container == EnumContainer.Intervention)
                    {
                        if (!string.IsNullOrEmpty(dbTaskValue) && dbRating != EnumTaskType.RatingNormal &&
                                dbTaskValue.Replace("'", string.Empty) == EnumTaskValue.IntNormalNotOK)
                        //dbTaskValue.Replace("'", string.Empty) == EnumTaskValue.IntNormalNA)
                        {
                            throw new Exception($"Task value with {EnumQuery.Key} {validationRequest.keyValue} is already defect!");
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(dbTaskValue) && dbRating != EnumTaskType.RatingNormal &&
                            (dbTaskValue.Replace("'", string.Empty) == EnumTaskValue.NormalNotOK ||
                            //dbTaskValue.Replace("'", string.Empty) == EnumTaskValue.NormalNA || -- Can change N/A to OK, System Working
                            dbTaskValue.Replace("'", string.Empty) == EnumTaskValue.CrackNotOKYes ||
                            dbTaskValue.Replace("'", string.Empty) == EnumTaskValue.CrackNotOKNo && groupName == EnumGroup.ChassisCrackService) /* Only Chassis Crack */
                            //dbTaskValue.Replace("'", string.Empty) == EnumTaskValue.CrackNA) -- Can change N/A to OK, System Working
                            )
                        {
                            throw new Exception($"Task value with {EnumQuery.Key} {validationRequest.keyValue} is already defect!");
                        }
                    }
                }

                #endregion 

                #region Data Type Validation

                //GetFieldValueRequest valueTypeRequest = new GetFieldValueRequest()
                //{
                //    id = validationRequest.id,
                //    keyValue = validationRequest.keyValue,
                //    propertyName = EnumQuery.ValueType
                //};

                //string valueType = await _repository.GetFieldValue(valueTypeRequest, true);

                //if (!string.IsNullOrEmpty(valueType))
                //{
                //    bool isValidValueType = false;

                //    if (valueType.ToLower() == EnumDataType.String.ToLower())
                //    {
                //        isValidValueType = validationRequest.propertyValue is string;
                //    }
                //    else if (valueType.ToLower() == EnumDataType.AlphaNumeric.ToLower())
                //    {
                //        Regex regex = new Regex("^[a-zA-Z0-9]*$");
                //        isValidValueType = regex.IsMatch(validationRequest.propertyValue);
                //    }
                //    else if (valueType.ToLower() == EnumDataType.Integer.ToLower())
                //    {
                //        isValidValueType = int.TryParse(validationRequest.propertyValue, out int value);
                //    }
                //    else if (valueType.ToLower() == EnumDataType.Float.ToLower())
                //    {
                //        isValidValueType = float.TryParse(validationRequest.propertyValue, out float value);
                //    }
                //    else if (valueType.ToLower() == EnumDataType.Boolean.ToLower())
                //    {
                //        isValidValueType = bool.TryParse(validationRequest.propertyValue, out bool value);
                //    }
                //    else if (valueType.ToLower() == EnumDataType.DateTime.ToLower())
                //    {
                //        string datetimeFormat = EnumFormatting.DateTimeToString;
                //        isValidValueType = DateTime.TryParseExact(validationRequest.propertyValue, datetimeFormat, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime value);
                //    }
                //    else if (valueType.ToLower() == EnumDataType.Date.ToLower())
                //    {
                //        string dateFormat = EnumFormatting.DateToString;
                //        isValidValueType = DateTime.TryParseExact(validationRequest.propertyValue, dateFormat, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime value);
                //    }
                //    else if (valueType.ToLower() == EnumDataType.Time.ToLower())
                //    {
                //        isValidValueType = TimeSpan.TryParse(validationRequest.propertyValue, out TimeSpan value);
                //    }
                //    else if (valueType.ToLower() == EnumDataType.Object.ToLower())
                //    {
                //        var token = JToken.Parse(validationRequest.propertyValue);
                //        isValidValueType = token is JObject;
                //    }
                //    else if (valueType.ToLower() == EnumDataType.Array.ToLower())
                //    {
                //        var token = JToken.Parse(validationRequest.propertyValue);
                //        isValidValueType = token is JArray;
                //    }
                //    else
                //    {
                //        throw new Exception($"Data type { valueType } undefined!");
                //    }

                //    if (!isValidValueType)
                //        throw new Exception($"Data type of { validationRequest.propertyName } property with key { validationRequest.keyValue } must be { valueType }!");
                //}

                #endregion

                #region Defect Labours Validation

                var labourData = validationRequest.defectDetail?.labours?.Value;

                if (labourData != null)
                {
                    List<LabourModel> labours = JsonConvert.DeserializeObject<List<LabourModel>>(labourData);

                    foreach (var labour in labours)
                    {
                        Regex regex = new Regex(@"^(?:\d{0,5}\.\d{1,2})$|^\d{0,5}$");
                        bool validQty = regex.IsMatch(labour.qty);
                        bool validHireEach = regex.IsMatch(labour.hireEach);
                        bool validTotalHours = regex.IsMatch(labour.totalHours);

                        if (!validQty || !validHireEach)
                            throw new Exception("Data format of Qty, Hire Each, Total Hours must be Decimal(5,2)");
                    }
                }

                #endregion

                #region Custom Validation

                if (validationRequest.propertyName == EnumQuery.Value)
                {
                    GetFieldValueRequest customValidationRequest = new GetFieldValueRequest()
                    {
                        id = validationRequest.id,
                        keyValue = validationRequest.keyValue,
                        propertyName = EnumQuery.CustomValidation
                    };

                    string validation = await _repository.GetFieldValue(customValidationRequest, true);

                    if (!string.IsNullOrEmpty(validation))
                    {
                        List<string> customValidations = validation.Split(" | ").ToList();

                        foreach (string customValidation in customValidations)
                        {
                            List<string> param = new List<string>() {
                                validationRequest.keyValue,
                                validationRequest.propertyName,
                                validationRequest.propertyValue
                            };

                            string[] arrCustomValidation = customValidation.Split(" : ");
                            string funcName = arrCustomValidation[0];

                            if (arrCustomValidation.Length > 1)
                                param.AddRange(arrCustomValidation[1]?.Split(", ").ToList());

                            GetType().GetMethod(funcName)?.Invoke(this, param.ToArray());
                        }
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                result = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            }

            return result;
        }

        public async Task<string> ValidationGeneralForm(ValidationRequest validationRequest)
        {
            string result = string.Empty;
            validationRequest.propertyValue = ((RepositoryBase)_repository).GetSettingValue(validationRequest.propertyValue);

            try
            {
                #region Updated By Validation

                if (validationRequest.propertyName.ToLower() == EnumQuery.Value.ToLower())
                {
                    var updatedBy = StaticHelper.GetPropValue(validationRequest.rsc, validationRequest.keyValue, EnumQuery.UpdatedBy);
                    string updatedById = StaticHelper.GetPropValue(updatedBy, EnumQuery.ID);

                    if (!string.IsNullOrEmpty(updatedById) && validationRequest.employee.id != updatedById)
                        throw new Exception($"Task already updated by other mechanic!");
                }

                #endregion
            }
            catch (Exception ex)
            {
                result = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            }

            return result;
        }

        public async Task<string> ValidationMultiple(ValidationRequest validationRequest)
        {
            string groupName = validationRequest.rsc.groupName;
            string result = string.Empty;
            validationRequest.propertyValue = ((RepositoryBase)_repository).GetSettingValue(validationRequest.propertyValue);

            try
            {
                #region Updated By Validation

                //if (validationRequest.propertyName.ToLower() == EnumQuery.TaskValue.ToLower())
                //{
                //    var updatedBy = StaticHelper.GetPropValue(validationRequest.rsc, validationRequest.keyValue, EnumQuery.UpdatedBy);
                //    string updatedById = StaticHelper.GetPropValue(updatedBy, EnumQuery.ID);

                //    if (!string.IsNullOrEmpty(updatedById) && validationRequest.employee.id != updatedById)
                //        throw new Exception($"Task already updated by other mechanic!");
                //}

                #endregion

                #region Task Value Validation

                if (validationRequest.propertyName.ToLower() == EnumQuery.TaskValue.ToLower())
                {
                    if (_container == EnumContainer.Intervention)
                    {
                        if (validationRequest.isDefect)
                        {
                            if (validationRequest.propertyValue == EnumTaskValue.IntNormalOK ||
                                validationRequest.propertyValue == EnumTaskValue.IntNormalCompleted ||
                                validationRequest.propertyValue == EnumTaskValue.CbmA ||
                                validationRequest.propertyValue == EnumTaskValue.CbmB ||
                                validationRequest.propertyValue == EnumTaskValue.CrackOK)
                            {
                                throw new Exception($"Task value with {EnumQuery.Key} {validationRequest.keyValue} is not defect!");
                            }
                        }
                        else
                        {
                            if (validationRequest.propertyValue != EnumTaskValue.IntNormalOK &&
                                validationRequest.propertyValue != EnumTaskValue.IntNormalCompleted &&
                                validationRequest.propertyValue != EnumTaskValue.CbmA &&
                                validationRequest.propertyValue != EnumTaskValue.CbmB &&
                                validationRequest.propertyValue != EnumTaskValue.CrackOK &&
                                validationRequest.propertyValue != EnumTaskValue.CalibrationComplete &&
                                validationRequest.propertyValue != EnumTaskValue.CalibrationYes &&
                                !string.IsNullOrEmpty(validationRequest.propertyValue))
                            {
                                throw new Exception($"Task value with {EnumQuery.Key} {validationRequest.keyValue} is defect!");
                            }
                        }
                    }
                    else
                    {
                        if (validationRequest.isDefect)
                        {
                            if (validationRequest.propertyValue == EnumTaskValue.NormalOK ||
                                validationRequest.propertyValue == EnumTaskValue.CbmA ||
                                validationRequest.propertyValue == EnumTaskValue.CbmB ||
                                validationRequest.propertyValue == EnumTaskValue.CrackOK)
                            {
                                throw new Exception($"Task value with {EnumQuery.Key} {validationRequest.keyValue} is not defect!");
                            }
                        }
                        else
                        {
                            if (validationRequest.propertyValue != EnumTaskValue.NormalOK &&
                                validationRequest.propertyValue != EnumTaskValue.CbmA &&
                                validationRequest.propertyValue != EnumTaskValue.CbmB &&
                                validationRequest.propertyValue != EnumTaskValue.CrackOK &&
                                validationRequest.propertyValue != EnumTaskValue.CalibrationComplete &&
                                validationRequest.propertyValue != EnumTaskValue.CalibrationYes &&
                                !string.IsNullOrEmpty(validationRequest.propertyValue))
                            {
                                throw new Exception($"Task value with {EnumQuery.Key} {validationRequest.keyValue} is defect!");
                            }
                        }
                    }


                    //string dbTaskValue = StaticHelper.GetPropValue(validationRequest.rsc, validationRequest.keyValue, EnumQuery.TaskValue);
                    //string dbRating = StaticHelper.GetPropValue(validationRequest.rsc, validationRequest.keyValue, EnumQuery.Rating);

                    //if (_container == EnumContainer.Intervention)
                    //{
                    //    if (!string.IsNullOrEmpty(dbTaskValue) && dbRating != EnumTaskType.RatingNormal &&
                    //            dbTaskValue.Replace("'", string.Empty) == EnumTaskValue.IntNormalNotOK)
                    //    //dbTaskValue.Replace("'", string.Empty) == EnumTaskValue.IntNormalNA)
                    //    {
                    //        throw new Exception($"Task value with {EnumQuery.Key} {validationRequest.keyValue} is already defect!");
                    //    }
                    //}
                    //else
                    //{
                    //    if (!string.IsNullOrEmpty(dbTaskValue) && dbRating != EnumTaskType.RatingNormal &&
                    //        (dbTaskValue.Replace("'", string.Empty) == EnumTaskValue.NormalNotOK ||
                    //        //dbTaskValue.Replace("'", string.Empty) == EnumTaskValue.NormalNA || -- Can change N/A to OK, System Working
                    //        dbTaskValue.Replace("'", string.Empty) == EnumTaskValue.CrackNotOKYes ||
                    //        (dbTaskValue.Replace("'", string.Empty) == EnumTaskValue.CrackNotOKNo && groupName == EnumGroup.ChassisCrackService)) /* Only Chassis Crack */
                    //        //dbTaskValue.Replace("'", string.Empty) == EnumTaskValue.CrackNA) -- Can change N/A to OK, System Working
                    //        )
                    //    {
                    //        throw new Exception($"Task value with {EnumQuery.Key} {validationRequest.keyValue} is already defect!");
                    //    }
                    //}
                }

                #endregion

                #region check reason for change NA value
                string taskCategory = StaticHelper.GetPropValue(validationRequest.rsc, validationRequest.keyValue, EnumQuery.Category);
                string dbTaskValue = StaticHelper.GetPropValue(validationRequest.rsc, validationRequest.keyValue, EnumQuery.TaskValue);
                var updatedBy = StaticHelper.GetPropValue(validationRequest.rsc, validationRequest.keyValue, EnumQuery.UpdatedBy);
                string updatedById = StaticHelper.GetPropValue(updatedBy, EnumQuery.ID);
                if (!string.IsNullOrEmpty(updatedById) && validationRequest.employee.id != updatedById)
                {
                    if (_container == EnumContainer.Intervention && taskCategory == EnumCategoryServiceSheet.NORMAL && dbTaskValue == EnumTaskValue.IntNormalNA //NA intervention
                            || _container != EnumContainer.Intervention && taskCategory == EnumCategoryServiceSheet.NORMAL && dbTaskValue == EnumTaskValue.NormalNA //NA servicesheet
                            || taskCategory == EnumCategoryServiceSheet.CRACK && dbTaskValue == EnumTaskValue.CrackNA) //NA crack

                    {
                        bool isHaveReason = false;
                        foreach (var propertyParam in validationRequest.propertyParams)
                        {
                            if (propertyParam.propertyName == EnumQuery.Reason && propertyParam.propertyValue != string.Empty)
                            {
                                isHaveReason = true;
                            }
                        }
                        if (!isHaveReason)
                        {
                            throw new Exception($"You cannot change Not Applicable without a reason, please retry!");
                        }
                    }
                }
                #endregion

                #region Defect

                //if (validationRequest.propertyName.ToLower() == EnumQuery.TaskValue.ToLower() && _container != EnumContainer.CalibrationDetail)
                //{
                //    await DeleteExtDefect(validationRequest.keyValue, validationRequest.workorder, validationRequest.employee);
                //}

                #endregion

                #region Data Type Validation

                //GetFieldValueRequest valueTypeRequest = new GetFieldValueRequest()
                //{
                //    id = validationRequest.id,
                //    keyValue = validationRequest.keyValue,
                //    propertyName = EnumQuery.ValueType
                //};

                //string valueType = await _repository.GetFieldValue(valueTypeRequest, true);

                //if (!string.IsNullOrEmpty(valueType))
                //{
                //    bool isValidValueType = false;

                //    if (valueType.ToLower() == EnumDataType.String.ToLower())
                //    {
                //        isValidValueType = validationRequest.propertyValue is string;
                //    }
                //    else if (valueType.ToLower() == EnumDataType.AlphaNumeric.ToLower())
                //    {
                //        Regex regex = new Regex("^[a-zA-Z0-9]*$");
                //        isValidValueType = regex.IsMatch(validationRequest.propertyValue);
                //    }
                //    else if (valueType.ToLower() == EnumDataType.Integer.ToLower())
                //    {
                //        isValidValueType = int.TryParse(validationRequest.propertyValue, out int value);
                //    }
                //    else if (valueType.ToLower() == EnumDataType.Float.ToLower())
                //    {
                //        isValidValueType = float.TryParse(validationRequest.propertyValue, out float value);
                //    }
                //    else if (valueType.ToLower() == EnumDataType.Boolean.ToLower())
                //    {
                //        isValidValueType = bool.TryParse(validationRequest.propertyValue, out bool value);
                //    }
                //    else if (valueType.ToLower() == EnumDataType.DateTime.ToLower())
                //    {
                //        string datetimeFormat = EnumFormatting.DateTimeToString;
                //        isValidValueType = DateTime.TryParseExact(validationRequest.propertyValue, datetimeFormat, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime value);
                //    }
                //    else if (valueType.ToLower() == EnumDataType.Date.ToLower())
                //    {
                //        string dateFormat = EnumFormatting.DateToString;
                //        isValidValueType = DateTime.TryParseExact(validationRequest.propertyValue, dateFormat, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime value);
                //    }
                //    else if (valueType.ToLower() == EnumDataType.Time.ToLower())
                //    {
                //        isValidValueType = TimeSpan.TryParse(validationRequest.propertyValue, out TimeSpan value);
                //    }
                //    else if (valueType.ToLower() == EnumDataType.Object.ToLower())
                //    {
                //        var token = JToken.Parse(validationRequest.propertyValue);
                //        isValidValueType = token is JObject;
                //    }
                //    else if (valueType.ToLower() == EnumDataType.Array.ToLower())
                //    {
                //        var token = JToken.Parse(validationRequest.propertyValue);
                //        isValidValueType = token is JArray;
                //    }
                //    else
                //    {
                //        throw new Exception($"Data type { valueType } undefined!");
                //    }

                //    if (!isValidValueType)
                //        throw new Exception($"Data type of { validationRequest.propertyName } property with key { validationRequest.keyValue } must be { valueType }!");
                //}

                #endregion

                #region Defect Labours Validation

                var labourData = validationRequest.defectDetail?.labours?.Value;

                if (labourData != null)
                {
                    List<LabourModel> labours = JsonConvert.DeserializeObject<List<LabourModel>>(labourData);

                    foreach (var labour in labours)
                    {
                        Regex regex = new Regex(@"^(?:\d{0,5}\.\d{1,2})$|^\d{0,5}$");
                        bool validQty = regex.IsMatch(labour.qty);
                        bool validHireEach = regex.IsMatch(labour.hireEach);
                        bool validTotalHours = regex.IsMatch(labour.totalHours);

                        if (!validQty || !validHireEach)
                            throw new Exception("Data format of Qty, Hire Each, Total Hours must be Decimal(5,2)");
                    }
                }

                #endregion

                #region Custom Validation

                if (validationRequest.propertyName == EnumQuery.Value)
                {
                    string validation = StaticHelper.GetPropValue(validationRequest.rsc, validationRequest.keyValue, EnumQuery.CustomValidation);

                    if (!string.IsNullOrEmpty(validation))
                    {
                        List<string> customValidations = validation.Split(" | ").ToList();

                        foreach (string customValidation in customValidations)
                        {
                            List<string> param = new List<string>() {
                                validationRequest.keyValue,
                                validationRequest.propertyName,
                                validationRequest.propertyValue
                            };

                            string[] arrCustomValidation = customValidation.Split(" : ");
                            string funcName = arrCustomValidation[0];

                            if (arrCustomValidation.Length > 1)
                                param.AddRange(arrCustomValidation[1]?.Split(", ").ToList());

                            GetType().GetMethod(funcName)?.Invoke(this, param.ToArray());
                        }
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                result = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            }

            return result;
        }

        public async Task<string> ValidationOffline(ValidationRequest validationRequest)
        {
            string groupName = validationRequest.rsc.groupName;
            string result = string.Empty;
            bool isTaskValue = true;
            validationRequest.propertyValue = ((RepositoryBase)_repository).GetSettingValue(validationRequest.propertyValue);

            var dbTaskValue = StaticHelper.GetPropValue(validationRequest.rsc, validationRequest.keyValue, EnumQuery.TaskValue);
            if (dbTaskValue == null)
            {
                isTaskValue = false;
                dbTaskValue = StaticHelper.GetPropValue(validationRequest.rsc, validationRequest.keyValue, EnumQuery.Value);
            }
            if (dbTaskValue is JArray)
            {
                dbTaskValue = dbTaskValue.ToString();
            }
            //string dbRating = StaticHelper.GetPropValue(validationRequest.rsc, validationRequest.keyValue, EnumQuery.Rating);

            try
            {
                #region Validation Value to TaskValue
                if (validationRequest.propertyName.ToLower() == EnumQuery.Value.ToLower() && !string.IsNullOrEmpty(validationRequest.propertyValue))
                {
                    var _repoDetail = new ServiceSheetDetailRepository(_connectionFactory, EnumContainer.ServiceSheetDetail);
                    var resultData = await _repoDetail.GetDataItemsDetailValue(validationRequest.rsc.id.ToString(), validationRequest.keyValue);

                    List<dynamic> dataValue = JsonConvert.DeserializeObject<List<dynamic>>(JsonConvert.SerializeObject(resultData));
                    if (dataValue.Any())
                    {
                        if (dataValue.FirstOrDefault().isTaskValue != null && (bool)dataValue.FirstOrDefault().isTaskValue)
                        {
                            var dataDetail = dataValue.FirstOrDefault();
                            //isTaskValue = dataValue.FirstOrDefault().isTaskValue;

                            string updatedById = dataValue.FirstOrDefault().updatedBy.ToString() == "" ? "" : dataValue.FirstOrDefault().updatedBy.id;

                            bool isMovement = false;
                            if (dataDetail.taskValue == EnumTaskValue.NormalNotOK && dataDetail.value == EnumTaskValue.NormalOK)
                            {
                                isMovement = true;
                            }

                            if (!string.IsNullOrEmpty(updatedById) && validationRequest.employee.id != updatedById)
                            {
                                string taskCategory = dataValue.FirstOrDefault().category;

                                if (!isMovement && string.IsNullOrEmpty(validationRequest.reason))
                                {
                                    if (!(_container == EnumContainer.Intervention && taskCategory == EnumCategoryServiceSheet.NORMAL && dbTaskValue == EnumTaskValue.IntNormalNA) //NA intervention
                                    && !(_container != EnumContainer.Intervention && taskCategory == EnumCategoryServiceSheet.NORMAL && dbTaskValue == EnumTaskValue.NormalNA) //NA serviceSheet
                                    && !(taskCategory == EnumCategoryServiceSheet.CBM) // cbm
                                    && !(taskCategory == EnumCategoryServiceSheet.CRACK && dbTaskValue == EnumTaskValue.CrackOK && (validationRequest.propertyValue == EnumTaskValue.CrackNotOKYes || validationRequest.propertyValue == EnumTaskValue.CrackNotOKNo)) // no crack to crack monitor && // no crack to crack repair
                                    && !(taskCategory == EnumCategoryServiceSheet.CRACK && dbTaskValue == EnumTaskValue.CrackNA)) // N/A to crack (1,2,3)
                                        throw new Exception($"Task already updated by other mechanic!");
                                }


                                // check have reason
                                //if (isTaskValue && ((_container == EnumContainer.Intervention && taskCategory == EnumCategoryServiceSheet.NORMAL && dbTaskValue == EnumTaskValue.IntNormalNA) //NA intervention
                                //    || (_container != EnumContainer.Intervention && taskCategory == EnumCategoryServiceSheet.NORMAL && dbTaskValue == EnumTaskValue.NormalNA) //NA servicesheet
                                //    || (taskCategory == EnumCategoryServiceSheet.CRACK && dbTaskValue == EnumTaskValue.CrackNA))) //NA crack
                                //{
                                //    bool isHaveReason = false;
                                //    foreach (var propertyParam in validationRequest.propertyParams)
                                //    {
                                //        if (propertyParam.propertyName == EnumQuery.Reason && propertyParam.propertyValue != String.Empty)
                                //        {
                                //            isHaveReason = true;
                                //        }
                                //    }
                                //    if (!isHaveReason)
                                //    {
                                //        throw new Exception($"You cannot change Not Applicable without a reason, please retry!");
                                //    }
                                //}
                            }
                        }
                        else
                        {
                            string taskCategory = dataValue.FirstOrDefault().category;

                            if (dataValue.FirstOrDefault().style.placeholder != null && dataValue.FirstOrDefault().style.placeholder.ToString().Contains("Additional Information (if required)"))
                            {
                                string updatedById = dataValue.FirstOrDefault().updatedBy.ToString() == "" ? "" : dataValue.FirstOrDefault().updatedBy.id;

                                if (!string.IsNullOrEmpty(updatedById) && validationRequest.employee.id != updatedById)
                                    throw new Exception($"Task already updated by other mechanic!");
                            }
                            #region validation update task defect

                            var parentTaskValue = StaticHelper.GetPropValue(validationRequest.rsc, validationRequest.taskKey, EnumQuery.TaskValue);
                            if (_container == EnumContainer.Intervention && parentTaskValue == EnumTaskValue.IntNormalNotOK // Defect to anything
                                || _container != EnumContainer.Intervention && parentTaskValue == EnumTaskValue.NormalNotOK // Defect to anything 
                            || taskCategory == EnumCategoryServiceSheet.CRACK && dbTaskValue == EnumTaskValue.CrackNotOKNo) // Defect to anything

                            {
                                Dictionary<string, object> param = new Dictionary<string, object>();
                                param.Add(EnumQuery.TaskId, validationRequest.taskKey);
                                param.Add(EnumQuery.Workorder, validationRequest.workorder);
                                param.Add(EnumQuery.IsActive, "true");
                                param.Add(EnumQuery.IsDeleted, "false");

                                var defects = await _defectHeaderRepository.GetDataListByParam(param);

                                if (defects != null)
                                {
                                    foreach (var defect in defects)
                                    {
                                        var status = StaticHelper.GetPropValue(defect, EnumQuery.Status);
                                        if (status != EnumStatus.DefectSubmit && status != EnumStatus.DefectSubmited)
                                        {
                                            throw new Exception($"You cannot modify the defect once already approved or declined by Supervisor");
                                        }
                                    }
                                }
                            }
                            #endregion
                        }
                    }
                }
                #endregion

                #region Validation Task Value

                if (validationRequest.propertyName.ToLower() == EnumQuery.TaskValue.ToLower() && !string.IsNullOrEmpty(validationRequest.propertyValue))
                {
                    var updatedBy = StaticHelper.GetPropValue(validationRequest.rsc, validationRequest.keyValue, EnumQuery.UpdatedBy);
                    string updatedById = StaticHelper.GetPropValue(updatedBy, EnumQuery.ID);
                    string taskCategory = StaticHelper.GetPropValue(validationRequest.rsc, validationRequest.keyValue, EnumQuery.Category);

                    if (!string.IsNullOrEmpty(updatedById) && validationRequest.employee.id != updatedById)
                    {
                        if (!(_container == EnumContainer.Intervention && taskCategory == EnumCategoryServiceSheet.NORMAL && dbTaskValue == EnumTaskValue.IntNormalNA) //NA intervention
                            && !(_container != EnumContainer.Intervention && taskCategory == EnumCategoryServiceSheet.NORMAL && dbTaskValue == EnumTaskValue.NormalNA) //NA serviceSheet
                            && !(taskCategory == EnumCategoryServiceSheet.CBM) // cbm
                            && !(taskCategory == EnumCategoryServiceSheet.CRACK && dbTaskValue == EnumTaskValue.CrackOK && (validationRequest.propertyValue == EnumTaskValue.CrackNotOKYes || validationRequest.propertyValue == EnumTaskValue.CrackNotOKNo)) // no crack to crack monitor && // no crack to crack repair
                            && !(taskCategory == EnumCategoryServiceSheet.CRACK && dbTaskValue == EnumTaskValue.CrackNA)) // N/A to crack (1,2,3)
                            throw new Exception($"Task already updated by other mechanic!");

                        // check have reason
                        if (_container == EnumContainer.Intervention && taskCategory == EnumCategoryServiceSheet.NORMAL && dbTaskValue == EnumTaskValue.IntNormalNA //NA intervention
                            || _container != EnumContainer.Intervention && taskCategory == EnumCategoryServiceSheet.NORMAL && dbTaskValue == EnumTaskValue.NormalNA //NA servicesheet
                            || taskCategory == EnumCategoryServiceSheet.CRACK && dbTaskValue == EnumTaskValue.CrackNA) //NA crack
                        {
                            bool isHaveReason = false;
                            foreach (var propertyParam in validationRequest.propertyParams)
                            {
                                if (propertyParam.propertyName == EnumQuery.Reason && propertyParam.propertyValue != string.Empty)
                                {
                                    isHaveReason = true;
                                }
                            }
                            if (!isHaveReason)
                            {
                                throw new Exception($"You cannot change Not Applicable without a reason, please retry!");
                            }
                        }
                    }
                    #region validation update task defect
                    if (_container == EnumContainer.Intervention && taskCategory == EnumCategoryServiceSheet.NORMAL && dbTaskValue == EnumTaskValue.IntNormalNotOK // Defect to anything
                        || _container != EnumContainer.Intervention && taskCategory == EnumCategoryServiceSheet.NORMAL && dbTaskValue == EnumTaskValue.NormalNotOK // Defect to anything
                        || taskCategory == EnumCategoryServiceSheet.CRACK && dbTaskValue == EnumTaskValue.CrackNotOKNo) // Defect to anything

                    {
                        Dictionary<string, object> param = new Dictionary<string, object>();
                        param.Add(EnumQuery.TaskId, validationRequest.keyValue);
                        param.Add(EnumQuery.Workorder, validationRequest.workorder);
                        param.Add(EnumQuery.IsActive, "true");
                        param.Add(EnumQuery.IsDeleted, "false");

                        var defects = await _defectHeaderRepository.GetDataListByParam(param);

                        if (defects != null)
                        {
                            foreach (var defect in defects)
                            {
                                var status = StaticHelper.GetPropValue(defect, EnumQuery.Status);
                                if (status != EnumStatus.DefectSubmit && status != EnumStatus.DefectSubmited)
                                {
                                    throw new Exception($"You cannot modify the defect once already approved or declined by Supervisor");
                                }
                            }
                        }
                    }
                    #endregion

                    //if (!string.IsNullOrEmpty(updatedById) && validationRequest.employee.id != updatedById)
                    //{
                    //    throw new Exception($"Task already updated by other mechanic!");
                    //}
                }

                #region validation update reset defect
                if (validationRequest.propertyName == EnumQuery.TaskValue && string.IsNullOrEmpty(validationRequest.propertyValue)) //reset dropdown
                {
                    Dictionary<string, object> param = new Dictionary<string, object>();
                    param.Add(EnumQuery.TaskId, validationRequest.keyValue);
                    param.Add(EnumQuery.Workorder, validationRequest.workorder);
                    param.Add(EnumQuery.IsActive, "true");
                    param.Add(EnumQuery.IsDeleted, "false");

                    var defects = await _defectHeaderRepository.GetDataListByParam(param);

                    if (defects != null)
                    {
                        foreach (var defect in defects)
                        {
                            var status = StaticHelper.GetPropValue(defect, EnumQuery.Status);
                            if (status != EnumStatus.DefectSubmit && status != EnumStatus.DefectSubmited)
                            {
                                throw new Exception($"You cannot modify the defect once already approved or declined by Supervisor");
                            }
                        }
                    }
                }
                #endregion

                #endregion

                #region Task Value Validation

                if (validationRequest.propertyName.ToLower() == EnumQuery.TaskValue.ToLower())
                {
                    if (_container == EnumContainer.Intervention)
                    {
                        if (validationRequest.propertyValue != EnumTaskValue.InSpec && validationRequest.propertyValue != EnumTaskValue.OutOfSpec)
                        {
                            if (validationRequest.isDefect)
                            {
                                if (validationRequest.propertyValue == EnumTaskValue.IntNormalOK ||
                                    validationRequest.propertyValue == EnumTaskValue.IntNormalCompleted ||
                                    validationRequest.propertyValue == EnumTaskValue.CbmA ||
                                    validationRequest.propertyValue == EnumTaskValue.CbmB ||
                                    validationRequest.propertyValue == EnumTaskValue.CrackOK)
                                {
                                    throw new Exception($"Task value with {EnumQuery.Key} {validationRequest.keyValue} is not defect!");
                                }
                            }
                            else
                            {
                                if (validationRequest.propertyValue != EnumTaskValue.IntNormalOK &&
                                    validationRequest.propertyValue != EnumTaskValue.IntNormalCompleted &&
                                    validationRequest.propertyValue != EnumTaskValue.CbmA &&
                                    validationRequest.propertyValue != EnumTaskValue.CbmB &&
                                    validationRequest.propertyValue != EnumTaskValue.CrackOK &&
                                    validationRequest.propertyValue != EnumTaskValue.CalibrationComplete &&
                                    validationRequest.propertyValue != EnumTaskValue.CalibrationYes &&
                                    !string.IsNullOrEmpty(validationRequest.propertyValue))
                                {
                                    throw new Exception($"Task value with {EnumQuery.Key} {validationRequest.keyValue} is defect!");
                                }
                            }
                        }
                    }
                    else
                    {
                        if (validationRequest.isDefect)
                        {
                            if (validationRequest.propertyValue == EnumTaskValue.NormalOK ||
                                validationRequest.propertyValue == EnumTaskValue.CbmA ||
                                validationRequest.propertyValue == EnumTaskValue.CbmB ||
                                validationRequest.propertyValue == EnumTaskValue.CrackOK)
                            {
                                throw new Exception($"Task value with {EnumQuery.Key} {validationRequest.keyValue} is not defect!");
                            }
                        }
                        else
                        {
                            if (validationRequest.propertyValue != EnumTaskValue.NormalOK &&
                                validationRequest.propertyValue != EnumTaskValue.CbmA &&
                                validationRequest.propertyValue != EnumTaskValue.CbmB &&
                                validationRequest.propertyValue != EnumTaskValue.CrackOK &&
                                validationRequest.propertyValue != EnumTaskValue.CalibrationComplete &&
                                validationRequest.propertyValue != EnumTaskValue.CalibrationYes &&
                                !string.IsNullOrEmpty(validationRequest.propertyValue) &&
                                !validationRequest.propertyValue.Contains(EnumQuery.Filename)) //for image new Type double cbm
                            {
                                throw new Exception($"Task value with {EnumQuery.Key} {validationRequest.keyValue} is defect!");
                            }
                        }
                    }

                    //if (_container == EnumContainer.Intervention)
                    //{
                    //    if (validationRequest.propertyValue != EnumTaskValue.InSpec && validationRequest.propertyValue != EnumTaskValue.OutOfSpec)
                    //    {
                    //        if (!string.IsNullOrEmpty(dbTaskValue) && dbRating != EnumTaskType.RatingNormal &&
                    //            dbTaskValue.Replace("'", string.Empty) == EnumTaskValue.IntNormalNotOK)
                    //        //dbTaskValue.Replace("'", string.Empty) == EnumTaskValue.IntNormalNA)
                    //        {
                    //            throw new Exception($"Task value with {EnumQuery.Key} {validationRequest.keyValue} is already defect!");
                    //        }
                    //    }
                    //}
                    //else
                    //{
                    //    if (!string.IsNullOrEmpty(dbTaskValue) && dbRating != EnumTaskType.RatingNormal &&
                    //        (dbTaskValue.Replace("'", string.Empty) == EnumTaskValue.NormalNotOK ||
                    //        //dbTaskValue.Replace("'", string.Empty) == EnumTaskValue.NormalNA || -- Can change N/A to OK, System Working
                    //        dbTaskValue.Replace("'", string.Empty) == EnumTaskValue.CrackNotOKYes ||
                    //        (dbTaskValue.Replace("'", string.Empty) == EnumTaskValue.CrackNotOKNo && groupName == EnumGroup.ChassisCrackService)) /* Only Chassis Crack */
                    //        //dbTaskValue.Replace("'", string.Empty) == EnumTaskValue.CrackNA) -- Can change N/A to OK, System Working
                    //        )
                    //    {
                    //        throw new Exception($"Task value with {EnumQuery.Key} {validationRequest.keyValue} is already defect!");
                    //    }
                    //}
                }

                #endregion

                #region Defect

                //if ((validationRequest.propertyName.ToLower() == EnumQuery.TaskValue.ToLower() && _container != EnumContainer.CalibrationDetail) ||
                //    validationRequest.propertyName.ToLower() == EnumQuery.TaskValueLeak.ToLower() ||
                //    validationRequest.propertyName.ToLower() == EnumQuery.TaskValueMounting.ToLower())
                //{
                //    await DeleteExtDefect(validationRequest.keyValue, validationRequest.workorder, validationRequest.employee, validationRequest.propertyName);
                //}

                #endregion

                #region Data Type Validation

                //GetFieldValueRequest valueTypeRequest = new GetFieldValueRequest()
                //{
                //    id = validationRequest.id,
                //    keyValue = validationRequest.keyValue,
                //    propertyName = EnumQuery.ValueType
                //};

                //string valueType = await _repository.GetFieldValue(valueTypeRequest, true);

                //if (!string.IsNullOrEmpty(valueType))
                //{
                //    bool isValidValueType = false;

                //    if (valueType.ToLower() == EnumDataType.String.ToLower())
                //    {
                //        isValidValueType = validationRequest.propertyValue is string;
                //    }
                //    else if (valueType.ToLower() == EnumDataType.AlphaNumeric.ToLower())
                //    {
                //        Regex regex = new Regex("^[a-zA-Z0-9]*$");
                //        isValidValueType = regex.IsMatch(validationRequest.propertyValue);
                //    }
                //    else if (valueType.ToLower() == EnumDataType.Integer.ToLower())
                //    {
                //        isValidValueType = int.TryParse(validationRequest.propertyValue, out int value);
                //    }
                //    else if (valueType.ToLower() == EnumDataType.Float.ToLower())
                //    {
                //        isValidValueType = float.TryParse(validationRequest.propertyValue, out float value);
                //    }
                //    else if (valueType.ToLower() == EnumDataType.Boolean.ToLower())
                //    {
                //        isValidValueType = bool.TryParse(validationRequest.propertyValue, out bool value);
                //    }
                //    else if (valueType.ToLower() == EnumDataType.DateTime.ToLower())
                //    {
                //        string datetimeFormat = EnumFormatting.DateTimeToString;
                //        isValidValueType = DateTime.TryParseExact(validationRequest.propertyValue, datetimeFormat, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime value);
                //    }
                //    else if (valueType.ToLower() == EnumDataType.Date.ToLower())
                //    {
                //        string dateFormat = EnumFormatting.DateToString;
                //        isValidValueType = DateTime.TryParseExact(validationRequest.propertyValue, dateFormat, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime value);
                //    }
                //    else if (valueType.ToLower() == EnumDataType.Time.ToLower())
                //    {
                //        isValidValueType = TimeSpan.TryParse(validationRequest.propertyValue, out TimeSpan value);
                //    }
                //    else if (valueType.ToLower() == EnumDataType.Object.ToLower())
                //    {
                //        var token = JToken.Parse(validationRequest.propertyValue);
                //        isValidValueType = token is JObject;
                //    }
                //    else if (valueType.ToLower() == EnumDataType.Array.ToLower())
                //    {
                //        var token = JToken.Parse(validationRequest.propertyValue);
                //        isValidValueType = token is JArray;
                //    }
                //    else
                //    {
                //        throw new Exception($"Data type { valueType } undefined!");
                //    }

                //    if (!isValidValueType)
                //        throw new Exception($"Data type of { validationRequest.propertyName } property with key { validationRequest.keyValue } must be { valueType }!");
                //}

                #endregion

                #region Defect Labours Validation

                var labourData = validationRequest.defectDetail?.labours?.Value;

                if (labourData != null)
                {
                    List<LabourModel> labours = JsonConvert.DeserializeObject<List<LabourModel>>(labourData);

                    foreach (var labour in labours)
                    {
                        Regex regex = new Regex(@"^(?:\d{0,5}\.\d{1,2})$|^\d{0,5}$");
                        bool validQty = regex.IsMatch(labour.qty);
                        bool validHireEach = regex.IsMatch(labour.hireEach);
                        bool validTotalHours = regex.IsMatch(labour.totalHours);

                        if (!validQty || !validHireEach)
                            throw new Exception("Data format of Qty, Hire Each, Total Hours must be Decimal(5,2)");
                    }
                }

                #endregion

                #region Custom Validation

                if (validationRequest.propertyName == EnumQuery.Value)
                {
                    string validation = StaticHelper.GetPropValue(validationRequest.rsc, validationRequest.keyValue, EnumQuery.CustomValidation);

                    if (!string.IsNullOrEmpty(validation))
                    {
                        List<string> customValidations = validation.Split(" | ").ToList();

                        foreach (string customValidation in customValidations)
                        {
                            List<string> param = new List<string>() {
                                validationRequest.keyValue,
                                validationRequest.propertyName,
                                validationRequest.propertyValue
                            };

                            string[] arrCustomValidation = customValidation.Split(" : ");
                            string funcName = arrCustomValidation[0];

                            if (arrCustomValidation.Length > 1)
                                param.AddRange(arrCustomValidation[1]?.Split(", ").ToList());

                            GetType().GetMethod(funcName)?.Invoke(this, param.ToArray());
                        }
                    }
                }

                #endregion

            }
            catch (Exception ex)
            {
                result = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            }

            return result;
        }

        public async Task<string> ValidationInterim(ValidationRequest validationRequest)
        {
            string groupName = validationRequest.rsc.groupName;
            string result = string.Empty;
            bool isTaskValue = true;
            validationRequest.propertyValue = ((RepositoryBase)_repository).GetSettingValue(validationRequest.propertyValue);

            var dbTaskValue = StaticHelper.GetPropValue(validationRequest.rsc, validationRequest.keyValue, EnumQuery.TaskValue);
            if (dbTaskValue == null)
            {
                isTaskValue = false;
                dbTaskValue = StaticHelper.GetPropValue(validationRequest.rsc, validationRequest.keyValue, EnumQuery.Value);
            }
            if (dbTaskValue is JArray)
            {
                dbTaskValue = dbTaskValue.ToString();
            }
            //string dbRating = StaticHelper.GetPropValue(validationRequest.rsc, validationRequest.keyValue, EnumQuery.Rating);

            try
            {
                #region Validation Value to TaskValue
                if (validationRequest.propertyName.ToLower() == EnumQuery.Value.ToLower() && !string.IsNullOrEmpty(validationRequest.propertyValue))
                {
                    var _repoDetail = new SuckAndBlowDetailRepository(_connectionFactory, EnumContainer.InterimEngineDetail);
                    var resultData = await _repoDetail.GetDataItemsDetailValue(validationRequest.rsc.id.ToString(), validationRequest.keyValue);

                    List<dynamic> dataValue = JsonConvert.DeserializeObject<List<dynamic>>(JsonConvert.SerializeObject(resultData));
                    if (dataValue.Any())
                    {
                        if (dataValue.FirstOrDefault().isTaskValue != null && (bool)dataValue.FirstOrDefault().isTaskValue)
                        {
                            var dataDetail = dataValue.FirstOrDefault();
                            //isTaskValue = dataValue.FirstOrDefault().isTaskValue;

                            string updatedById = dataValue.FirstOrDefault().updatedBy.ToString() == "" ? "" : dataValue.FirstOrDefault().updatedBy.id;

                            bool isMovement = false;
                            if (dataDetail.taskValue == EnumTaskValue.NormalNotOK && dataDetail.value == EnumTaskValue.NormalOK)
                            {
                                isMovement = true;
                            }

                            if (!string.IsNullOrEmpty(updatedById) && validationRequest.employee.id != updatedById)
                            {
                                string taskCategory = dataValue.FirstOrDefault().category;

                                if (!isMovement && string.IsNullOrEmpty(validationRequest.reason))
                                {
                                    if (!(_container == EnumContainer.Intervention && taskCategory == EnumCategoryServiceSheet.NORMAL && dbTaskValue == EnumTaskValue.IntNormalNA) //NA intervention
                                    && !(_container != EnumContainer.Intervention && taskCategory == EnumCategoryServiceSheet.NORMAL && dbTaskValue == EnumTaskValue.NormalNA) //NA serviceSheet
                                    && !(taskCategory == EnumCategoryServiceSheet.CBM) // cbm
                                    && !(taskCategory == EnumCategoryServiceSheet.CRACK && dbTaskValue == EnumTaskValue.CrackOK && (validationRequest.propertyValue == EnumTaskValue.CrackNotOKYes || validationRequest.propertyValue == EnumTaskValue.CrackNotOKNo)) // no crack to crack monitor && // no crack to crack repair
                                    && !(taskCategory == EnumCategoryServiceSheet.CRACK && dbTaskValue == EnumTaskValue.CrackNA)) // N/A to crack (1,2,3)
                                        throw new Exception($"Task already updated by other mechanic!");
                                }
                            }
                        }
                        else
                        {
                            string taskCategory = dataValue.FirstOrDefault().category;

                            if (dataValue.FirstOrDefault().style.placeholder != null && dataValue.FirstOrDefault().style.placeholder.ToString().Contains("Additional Information (if required)"))
                            {
                                string updatedById = dataValue.FirstOrDefault().updatedBy.ToString() == "" ? "" : dataValue.FirstOrDefault().updatedBy.id;

                                if (!string.IsNullOrEmpty(updatedById) && validationRequest.employee.id != updatedById)
                                    throw new Exception($"Task already updated by other mechanic!");
                            }
                            #region validation update task defect

                            var parentTaskValue = StaticHelper.GetPropValue(validationRequest.rsc, validationRequest.taskKey, EnumQuery.TaskValue);
                            if (_container == EnumContainer.Intervention && parentTaskValue == EnumTaskValue.IntNormalNotOK // Defect to anything
                                || _container != EnumContainer.Intervention && parentTaskValue == EnumTaskValue.NormalNotOK // Defect to anything 
                            || taskCategory == EnumCategoryServiceSheet.CRACK && dbTaskValue == EnumTaskValue.CrackNotOKNo) // Defect to anything

                            {
                                Dictionary<string, object> param = new Dictionary<string, object>();
                                param.Add(EnumQuery.TaskId, validationRequest.taskKey);
                                param.Add(EnumQuery.Workorder, validationRequest.workorder);
                                param.Add(EnumQuery.IsActive, "true");
                                param.Add(EnumQuery.IsDeleted, "false");

                                var defects = await _defectHeaderRepository.GetDataListByParam(param);

                                if (defects != null)
                                {
                                    foreach (var defect in defects)
                                    {
                                        var status = StaticHelper.GetPropValue(defect, EnumQuery.Status);
                                        if (status != EnumStatus.DefectSubmit && status != EnumStatus.DefectSubmited)
                                        {
                                            throw new Exception($"You cannot modify the defect once already approved or declined by Supervisor");
                                        }
                                    }
                                }
                            }
                            #endregion
                        }
                    }
                }
                #endregion

                #region Validation Task Value

                if (validationRequest.propertyName.ToLower() == EnumQuery.TaskValue.ToLower() && !string.IsNullOrEmpty(validationRequest.propertyValue))
                {
                    var updatedBy = StaticHelper.GetPropValue(validationRequest.rsc, validationRequest.keyValue, EnumQuery.UpdatedBy);
                    string updatedById = StaticHelper.GetPropValue(updatedBy, EnumQuery.ID);
                    string taskCategory = StaticHelper.GetPropValue(validationRequest.rsc, validationRequest.keyValue, EnumQuery.Category);

                    if (!string.IsNullOrEmpty(updatedById) && validationRequest.employee.id != updatedById)
                    {
                        if (!(_container == EnumContainer.Intervention && taskCategory == EnumCategoryServiceSheet.NORMAL && dbTaskValue == EnumTaskValue.IntNormalNA) //NA intervention
                            && !(_container != EnumContainer.Intervention && taskCategory == EnumCategoryServiceSheet.NORMAL && dbTaskValue == EnumTaskValue.NormalNA) //NA serviceSheet
                            && !(taskCategory == EnumCategoryServiceSheet.CBM) // cbm
                            && !(taskCategory == EnumCategoryServiceSheet.CRACK && dbTaskValue == EnumTaskValue.CrackOK && (validationRequest.propertyValue == EnumTaskValue.CrackNotOKYes || validationRequest.propertyValue == EnumTaskValue.CrackNotOKNo)) // no crack to crack monitor && // no crack to crack repair
                            && !(taskCategory == EnumCategoryServiceSheet.CRACK && dbTaskValue == EnumTaskValue.CrackNA)) // N/A to crack (1,2,3)
                            throw new Exception($"Task already updated by other mechanic!");

                        // check have reason
                        if (_container == EnumContainer.Intervention && taskCategory == EnumCategoryServiceSheet.NORMAL && dbTaskValue == EnumTaskValue.IntNormalNA //NA intervention
                            || _container != EnumContainer.Intervention && taskCategory == EnumCategoryServiceSheet.NORMAL && dbTaskValue == EnumTaskValue.NormalNA //NA servicesheet
                            || taskCategory == EnumCategoryServiceSheet.CRACK && dbTaskValue == EnumTaskValue.CrackNA) //NA crack
                        {
                            bool isHaveReason = false;
                            foreach (var propertyParam in validationRequest.propertyParams)
                            {
                                if (propertyParam.propertyName == EnumQuery.Reason && propertyParam.propertyValue != string.Empty)
                                {
                                    isHaveReason = true;
                                }
                            }
                            if (!isHaveReason)
                            {
                                throw new Exception($"You cannot change Not Applicable without a reason, please retry!");
                            }
                        }
                    }
                    #region validation update task defect
                    if (_container == EnumContainer.Intervention && taskCategory == EnumCategoryServiceSheet.NORMAL && dbTaskValue == EnumTaskValue.IntNormalNotOK // Defect to anything
                        || _container != EnumContainer.Intervention && taskCategory == EnumCategoryServiceSheet.NORMAL && dbTaskValue == EnumTaskValue.NormalNotOK // Defect to anything
                        || taskCategory == EnumCategoryServiceSheet.CRACK && dbTaskValue == EnumTaskValue.CrackNotOKNo) // Defect to anything

                    {
                        Dictionary<string, object> param = new Dictionary<string, object>();
                        param.Add(EnumQuery.TaskId, validationRequest.keyValue);
                        param.Add(EnumQuery.Workorder, validationRequest.workorder);
                        param.Add(EnumQuery.IsActive, "true");
                        param.Add(EnumQuery.IsDeleted, "false");

                        var defects = await _defectHeaderRepository.GetDataListByParam(param);

                        if (defects != null)
                        {
                            foreach (var defect in defects)
                            {
                                var status = StaticHelper.GetPropValue(defect, EnumQuery.Status);
                                if (status != EnumStatus.DefectSubmit && status != EnumStatus.DefectSubmited)
                                {
                                    throw new Exception($"You cannot modify the defect once already approved or declined by Supervisor");
                                }
                            }
                        }
                    }
                    #endregion

                    //if (!string.IsNullOrEmpty(updatedById) && validationRequest.employee.id != updatedById)
                    //{
                    //    throw new Exception($"Task already updated by other mechanic!");
                    //}
                }

                #region validation update reset defect
                if (validationRequest.propertyName == EnumQuery.TaskValue && string.IsNullOrEmpty(validationRequest.propertyValue)) //reset dropdown
                {
                    Dictionary<string, object> param = new Dictionary<string, object>();
                    param.Add(EnumQuery.TaskId, validationRequest.keyValue);
                    param.Add(EnumQuery.Workorder, validationRequest.workorder);
                    param.Add(EnumQuery.IsActive, "true");
                    param.Add(EnumQuery.IsDeleted, "false");

                    var defects = await _defectHeaderRepository.GetDataListByParam(param);

                    if (defects != null)
                    {
                        foreach (var defect in defects)
                        {
                            var status = StaticHelper.GetPropValue(defect, EnumQuery.Status);
                            if (status != EnumStatus.DefectSubmit && status != EnumStatus.DefectSubmited)
                            {
                                throw new Exception($"You cannot modify the defect once already approved or declined by Supervisor");
                            }
                        }
                    }
                }
                #endregion

                #endregion

                #region Task Value Validation

                if (validationRequest.propertyName.ToLower() == EnumQuery.TaskValue.ToLower())
                {
                    if (_container == EnumContainer.Intervention)
                    {
                        if (validationRequest.propertyValue != EnumTaskValue.InSpec && validationRequest.propertyValue != EnumTaskValue.OutOfSpec)
                        {
                            if (validationRequest.isDefect)
                            {
                                if (validationRequest.propertyValue == EnumTaskValue.IntNormalOK ||
                                    validationRequest.propertyValue == EnumTaskValue.IntNormalCompleted ||
                                    validationRequest.propertyValue == EnumTaskValue.CbmA ||
                                    validationRequest.propertyValue == EnumTaskValue.CbmB ||
                                    validationRequest.propertyValue == EnumTaskValue.CrackOK)
                                {
                                    throw new Exception($"Task value with {EnumQuery.Key} {validationRequest.keyValue} is not defect!");
                                }
                            }
                            else
                            {
                                if (validationRequest.propertyValue != EnumTaskValue.IntNormalOK &&
                                    validationRequest.propertyValue != EnumTaskValue.IntNormalCompleted &&
                                    validationRequest.propertyValue != EnumTaskValue.CbmA &&
                                    validationRequest.propertyValue != EnumTaskValue.CbmB &&
                                    validationRequest.propertyValue != EnumTaskValue.CrackOK &&
                                    validationRequest.propertyValue != EnumTaskValue.CalibrationComplete &&
                                    validationRequest.propertyValue != EnumTaskValue.CalibrationYes &&
                                    !string.IsNullOrEmpty(validationRequest.propertyValue))
                                {
                                    throw new Exception($"Task value with {EnumQuery.Key} {validationRequest.keyValue} is defect!");
                                }
                            }
                        }
                    }
                    else
                    {
                        if (validationRequest.isDefect)
                        {
                            if (validationRequest.propertyValue == EnumTaskValue.NormalOK ||
                                validationRequest.propertyValue == EnumTaskValue.CbmA ||
                                validationRequest.propertyValue == EnumTaskValue.CbmB ||
                                validationRequest.propertyValue == EnumTaskValue.CrackOK)
                            {
                                throw new Exception($"Task value with {EnumQuery.Key} {validationRequest.keyValue} is not defect!");
                            }
                        }
                        else
                        {
                            if (validationRequest.propertyValue != EnumTaskValue.NormalOK &&
                                validationRequest.propertyValue != EnumTaskValue.CbmA &&
                                validationRequest.propertyValue != EnumTaskValue.CbmB &&
                                validationRequest.propertyValue != EnumTaskValue.CrackOK &&
                                validationRequest.propertyValue != EnumTaskValue.CalibrationComplete &&
                                validationRequest.propertyValue != EnumTaskValue.CalibrationYes &&
                                !string.IsNullOrEmpty(validationRequest.propertyValue) &&
                                !validationRequest.propertyValue.Contains(EnumQuery.Filename)) //for image new Type double cbm
                            {
                                throw new Exception($"Task value with {EnumQuery.Key} {validationRequest.keyValue} is defect!");
                            }
                        }
                    }
                }

                #endregion

                #region Defect

                if (validationRequest.propertyName.ToLower() == EnumQuery.TaskValue.ToLower() && _container != EnumContainer.CalibrationDetail ||
                    validationRequest.propertyName.ToLower() == EnumQuery.TaskValueLeak.ToLower() ||
                    validationRequest.propertyName.ToLower() == EnumQuery.TaskValueMounting.ToLower())
                {
                    await DeleteExtDefect(validationRequest.keyValue, validationRequest.workorder, validationRequest.employee, validationRequest.propertyName);
                }

                #endregion

                #region Defect Labours Validation

                var labourData = validationRequest.defectDetail?.labours?.Value;

                if (labourData != null)
                {
                    List<LabourModel> labours = JsonConvert.DeserializeObject<List<LabourModel>>(labourData);

                    foreach (var labour in labours)
                    {
                        Regex regex = new Regex(@"^(?:\d{0,5}\.\d{1,2})$|^\d{0,5}$");
                        bool validQty = regex.IsMatch(labour.qty);
                        bool validHireEach = regex.IsMatch(labour.hireEach);
                        bool validTotalHours = regex.IsMatch(labour.totalHours);

                        if (!validQty || !validHireEach)
                            throw new Exception("Data format of Qty, Hire Each, Total Hours must be Decimal(5,2)");
                    }
                }

                #endregion

                #region Custom Validation

                if (validationRequest.propertyName == EnumQuery.Value)
                {
                    string validation = StaticHelper.GetPropValue(validationRequest.rsc, validationRequest.keyValue, EnumQuery.CustomValidation);

                    if (!string.IsNullOrEmpty(validation))
                    {
                        List<string> customValidations = validation.Split(" | ").ToList();

                        foreach (string customValidation in customValidations)
                        {
                            List<string> param = new List<string>() {
                                validationRequest.keyValue,
                                validationRequest.propertyName,
                                validationRequest.propertyValue
                            };

                            string[] arrCustomValidation = customValidation.Split(" : ");
                            string funcName = arrCustomValidation[0];

                            if (arrCustomValidation.Length > 1)
                                param.AddRange(arrCustomValidation[1]?.Split(", ").ToList());

                            GetType().GetMethod(funcName)?.Invoke(this, param.ToArray());
                        }
                    }
                }

                #endregion

            }
            catch (Exception ex)
            {
                result = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            }

            return result;
        }

        public void Required(string keyValue, string propertyName, string propertyValue)
        {
            if (string.IsNullOrEmpty(propertyValue) || propertyValue == "0")
                throw new Exception($"Field {propertyName} with key {keyValue} is required!");
        }

        //public void Percentage(string keyValue, string propertyName, string propertyValue)
        //{
        //    Regex regex = new Regex(@"^(\d{0,2}(\.\d{1,2})?|100(\.00?)?)$");
        //    if (!regex.IsMatch(propertyValue))
        //        throw new Exception($"Field { propertyName } with key { keyValue } must be 0 - 100!");
        //}

        //public string DefectValidation(UpdateTaskDefectRequest updateTaskDefectRequest)
        //{
        //    string result = string.Empty;

        //    if (updateTaskDefectRequest.defectHeader.category == EnumTaskType.Normal)
        //    {
        //        if (updateTaskDefectRequest.defectHeader.taskValue == EnumTaskValue.NormalNotOK)
        //        {

        //        }
        //        else if (updateTaskDefectRequest.defectHeader.taskValue == EnumTaskValue.NormalNA)
        //        {

        //        }
        //        else
        //        {

        //        }
        //    }
        //    else if (updateTaskDefectRequest.defectHeader.category == EnumTaskType.CBM)
        //    {
        //        if (updateTaskDefectRequest.defectHeader.taskValue == EnumTaskValue.CbmB)
        //        {

        //        }
        //        else if (updateTaskDefectRequest.defectHeader.taskValue == EnumTaskValue.CbmC)
        //        {

        //        }
        //        else if (updateTaskDefectRequest.defectHeader.taskValue == EnumTaskValue.CbmX)
        //        {

        //        }
        //        else
        //        {
        //            result = EnumErrorMessage.ErrMsgTaskValue;
        //        }
        //    }
        //    else if (updateTaskDefectRequest.defectHeader.category == EnumTaskType.Crack)
        //    {
        //        if (updateTaskDefectRequest.defectHeader.taskValue == EnumTaskValue.CrackNotOKYes)
        //        {

        //        }
        //        else if (updateTaskDefectRequest.defectHeader.taskValue == EnumTaskValue.CrackNotOKNo)
        //        {

        //        }
        //        else if (updateTaskDefectRequest.defectHeader.taskValue == EnumTaskValue.CrackNA)
        //        {

        //        }
        //        else
        //        {
        //            result = EnumErrorMessage.ErrMsgTaskValue;
        //        }
        //    }
        //    else
        //    {
        //        result = EnumErrorMessage.ErrMsgTaskType;
        //    }

        //    return result;
        //}

        #endregion

        #region Private Function

        private async Task<ServiceResult> CreateCBMHistory(dynamic request)
        {
            UpdateTaskReviseRequest updateTaskRequest = JsonConvert.DeserializeObject<UpdateTaskReviseRequest>(JsonConvert.SerializeObject(request));

            UpdateRequest updateRequest = new UpdateRequest()
            {
                id = updateTaskRequest.id,
                workOrder = updateTaskRequest.workorder,
                updateParams = updateTaskRequest.updateParams,
                employee = updateTaskRequest.employee
            };

            foreach (var updateParam in updateTaskRequest.updateParams)
            {
                string dataTaskKeyMain = updateParam.keyValue;

                foreach (var propertyParam in updateParam.propertyParams)
                {
                    if (propertyParam.propertyName.ToLower() == EnumQuery.TaskValue.ToLower())
                    {
                        string taskValueData = propertyParam.propertyValue;
                        var interventionResult = await _repository.Get(updateRequest.id);

                        if (interventionResult == null)
                        {
                            return new ServiceResult()
                            {
                                Message = "Intervention not found!",
                                IsError = true,
                                Content = null
                            };
                        }

                        InterventionModel intervention = JsonConvert.DeserializeObject<InterventionModel>(JsonConvert.SerializeObject(interventionResult));

                        Dictionary<string, object> paramHistory = new Dictionary<string, object>();
                        paramHistory.Add(EnumQuery.KeyPbi, intervention.keyPbi);
                        paramHistory.Add(EnumQuery.SSTaskKey, dataTaskKeyMain);

                        var dataHistory = await _cbmHistoryRepository.GetDataByParam(paramHistory);

                        if (dataHistory == null)
                        {
                            var _repo = new InterventionRepository(_connectionFactory, EnumContainer.Intervention);
                            var currentData = StaticHelper.GetData(interventionResult, EnumQuery.Key, dataTaskKeyMain);
                            var currentDataUpdate = StaticHelper.GetData(interventionResult, EnumQuery.Key, dataTaskKeyMain);

                            CbmHistoryParam headerHisUpdate = new CbmHistoryParam();
                            headerHisUpdate.key = EnumGroup.General;
                            headerHisUpdate.workOrder = intervention.sapWorkOrder;
                            headerHisUpdate.keyPbi = intervention.keyPbi;
                            headerHisUpdate.equipment = intervention.equipment;
                            headerHisUpdate.taskKey = dataTaskKeyMain;
                            headerHisUpdate.siteId = intervention.siteId;
                            headerHisUpdate.serviceSheetDetailId = "";
                            headerHisUpdate.defectHeaderId = "";
                            headerHisUpdate.detail = new DetailCBMHistory();

                            foreach (var item in currentData)
                            {
                                item.Remove("id");
                                item.Remove("uom");
                                item.Remove("taskNo");
                                item.Remove("measurementValue");
                            }

                            // Last Value
                            foreach (var item in currentDataUpdate)
                            {
                                item.Remove("id");
                                item.Remove("uom");
                                item.Remove("taskNo");
                                item.Remove("measurementValue");

                                foreach (var itemDetail in item.items)
                                {
                                    if (itemDetail.isTaskValue != null && itemDetail.isTaskValue == true)
                                    {
                                        itemDetail.value = taskValueData;

                                        if (taskValueData == "A" || taskValueData == "B" || taskValueData == "C" || taskValueData == "X")
                                        {
                                            item.taskValue = taskValueData;
                                            item.updatedBy = JsonConvert.DeserializeObject<object>(JsonConvert.SerializeObject(updateTaskRequest.employee));
                                            item.updatedDate = ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerDateTime);
                                        }

                                    }
                                }
                            }

                            currentData.Add(currentDataUpdate[0]);

                            DetailCBMHistory detailHisUpdate = new DetailCBMHistory();
                            detailHisUpdate.key = "HISTORY";
                            detailHisUpdate.category = currentData[0].category;
                            detailHisUpdate.rating = currentData[0].rating;
                            detailHisUpdate.history = currentData;

                            headerHisUpdate.detail = detailHisUpdate;

                            var modelHeader = new CreateRequest();
                            modelHeader.employee = new EmployeeModel();

                            modelHeader.employee.id = updateRequest.employee.id;
                            modelHeader.employee.name = updateRequest.employee.name;
                            modelHeader.entity = headerHisUpdate;

                            var resultAddHeader = await _cbmHistoryRepository.Create(modelHeader);
                        }
                        else
                        {
                            var detailHistory = StaticHelper.GetPropValue(dataHistory.detail, "history");

                            // Last Value
                            var currentDataUpdate = StaticHelper.GetData(interventionResult, EnumQuery.Key, dataTaskKeyMain);

                            foreach (var item in currentDataUpdate)
                            {
                                item.Remove("id");
                                item.Remove("uom");
                                item.Remove("taskNo");
                                item.Remove("measurementValue");

                                foreach (var itemDetail in item.items)
                                {
                                    if (itemDetail.isTaskValue != null && itemDetail.isTaskValue == true)
                                    {
                                        itemDetail.value = taskValueData;

                                        if (taskValueData == "A" || taskValueData == "B" || taskValueData == "C" || taskValueData == "X")
                                        {
                                            item.taskValue = taskValueData;
                                            item.updatedBy = JsonConvert.DeserializeObject<object>(JsonConvert.SerializeObject(updateTaskRequest.employee));
                                            item.updatedDate = ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerDateTime);
                                        }

                                    }
                                }
                            }

                            detailHistory.Add(currentDataUpdate[0]);

                            List<PropertyParam> propertyParams = new List<PropertyParam>() {
                                new PropertyParam()
                                {
                                    propertyName = EnumQuery.CbmHistory,
                                    propertyValue = JsonConvert.SerializeObject(detailHistory)
                                }
                            };

                            UpdateRequest updateDataParams = new UpdateRequest();
                            updateDataParams.id = dataHistory.id;
                            updateDataParams.workOrder = dataHistory.workOrder;
                            updateDataParams.updateParams = new List<UpdateParam>();
                            updateDataParams.employee = updateTaskRequest.employee;

                            updateDataParams.updateParams.Add(new UpdateParam()
                            {
                                keyValue = "HISTORY",
                                propertyParams = propertyParams
                            });

                            var resultUpdateHeader = await _cbmHistoryRepository.Update(updateDataParams, dataHistory);
                        }
                    }
                }
            }

            return new ServiceResult()
            {
                Message = "Data updated successfully",
                IsError = false,
                Content = null
            };
        }

        #endregion
    }
}