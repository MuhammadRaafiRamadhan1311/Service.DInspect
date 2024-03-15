using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service.DInspect.Interfaces;
using Service.DInspect.Models;
using Service.DInspect.Models.ADM;
using Service.DInspect.Models.Entity;
using Service.DInspect.Models.Enum;
using Service.DInspect.Models.Request;
using Service.DInspect.Models.Response;
using Service.DInspect.Repositories;
using Service.DInspect.Services.Helpers;
using Service.DInspect.Helpers;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.DInspect.Services
{
    public class CalibrationHeaderService : ServiceBase
    {
        protected IRepositoryBase _taskCalibrationRepository;
        protected IRepositoryBase _calibrationHeaderRepository;
        protected IRepositoryBase _calibrationDetailRepository;
        protected IRepositoryBase _serviceSheetDetailRepository;
        protected IRepositoryBase _serviceSheetHeaderRepository;
        private readonly IConnectionFactory _connectionFactory;

        public CalibrationHeaderService(MySetting appSetting, IConnectionFactory connectionFactory, string container, string accessToken) : base(appSetting, connectionFactory, container, accessToken)
        {
            _connectionFactory = connectionFactory;
            _repository = new CalibrationHeaderRepository(connectionFactory, container);
            _calibrationHeaderRepository = new CalibrationHeaderRepository(connectionFactory, EnumContainer.CalibrationHeader);
            _calibrationDetailRepository = new CalibrationDetailRepository(connectionFactory, EnumContainer.CalibrationDetail);
            _serviceSheetDetailRepository = new ServiceSheetDetailRepository(connectionFactory, EnumContainer.ServiceSheetDetail);
            _serviceSheetHeaderRepository = new ServiceSheetHeaderRepository(connectionFactory, EnumContainer.ServiceSheetHeader);
            _taskCalibrationRepository = new TaskCalibrationRepository(connectionFactory, EnumContainer.TaskCalibration);
        }

        public async Task<ServiceResult> CalibrationPayload(CalibrationPayloadRequest model)
        {
            try
            {
                var resultJson = new Dictionary<string, object>();
                var resultData = new List<dynamic>();
                string headerID = string.Empty;

                #region Get SerialNumber

                CallAPIService callAPI = new CallAPIService(_appSetting, _accessToken);

                dynamic equipmentNumberResult = await callAPI.GetEquipmentNumber(model.unitNumber);
                List<EquipmentNumberModel> equipmentNumbers = JsonConvert.DeserializeObject<List<EquipmentNumberModel>>(JsonConvert.SerializeObject(equipmentNumberResult));

                if (equipmentNumbers.FirstOrDefault() == null)
                {
                    return new ServiceResult()
                    {
                        IsError = true,
                        Message = $"Data Unit Number {model.unitNumber} not found!"
                    };
                }

                string serialNumber = equipmentNumbers.FirstOrDefault().SerialNumber;

                #endregion


                #region Get SMU
                var ServiceHeaderParam = new Dictionary<string, object>();
                ServiceHeaderParam.Add(EnumQuery.SSWorkorder, model.workOrder);
                ServiceHeaderParam.Add(EnumQuery.IsDeleted, "false");

                var serviceSheetHeaderResult = await _serviceSheetHeaderRepository.GetDataListByParam(ServiceHeaderParam);
                List<dynamic> serviceSheetHeaderList = JsonConvert.DeserializeObject<List<dynamic>>(JsonConvert.SerializeObject(serviceSheetHeaderResult));

                if (serviceSheetHeaderList.FirstOrDefault() == null)
                {
                    return new ServiceResult()
                    {
                        IsError = true,
                        Message = $"Data SMU in work order {model.workOrder} not found!"
                    };
                }

                string smu = serviceSheetHeaderList.FirstOrDefault().smu;

                #endregion

                var dataHeaderParam = new Dictionary<string, object>();
                dataHeaderParam.Add(EnumQuery.ModelId, model.modelId);
                dataHeaderParam.Add(EnumQuery.PsTypeId, model.psTypeId);
                dataHeaderParam.Add(EnumQuery.SSWorkorder, model.workOrder);
                dataHeaderParam.Add(EnumQuery.IsDeleted, "false");

                var oldDataHeader = await _calibrationHeaderRepository.GetDataListByParam(dataHeaderParam);

                if (oldDataHeader.Count == 0)
                {
                    var dataParam = new Dictionary<string, object>();
                    dataParam.Add(EnumQuery.ModelId, model.modelId);
                    dataParam.Add(EnumQuery.PsTypeId, model.psTypeId);
                    dataParam.Add(EnumQuery.IsDeleted, "false");

                    var result = await _taskCalibrationRepository.GetDataListByParam(dataParam);

                    foreach (var item in result)
                    {
                        //remove
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

                        #region calibration header
                        string updatedDate = ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerDateTime);
                        string tsUpdatedDate = ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerTimeStamp);

                        List<StatusHistoryModel> statusHistories = new List<StatusHistoryModel>();

                        statusHistories.Add(new StatusHistoryModel()
                        {
                            status = EnumStatus.EFormOnProgress,
                            updatedBy = model.employee,
                            updatedDate = updatedDate,
                            tsUpdatedDate = tsUpdatedDate
                        });

                        var newItem = new CreateRequestCalibrationHeader();
                        newItem.modelId = model.modelId;
                        newItem.psTypeId = model.psTypeId;
                        newItem.workOrder = model.workOrder;
                        newItem.equipment = model.unitNumber;
                        newItem.serialNumber = serialNumber;
                        newItem.statusCalibration = EnumStatus.EFormOnProgress;
                        newItem.statusHistory = statusHistories;
                        newItem.smu = smu;

                        var modelHeader = new CreateRequest();
                        modelHeader.employee = new EmployeeModel();

                        modelHeader.employee.id = model.employee.id;
                        modelHeader.employee.name = model.employee.name;
                        modelHeader.entity = newItem;

                        var resultAddHeader = await _calibrationHeaderRepository.Create(modelHeader);

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

                        resultJson.Add("header", resultAddHeader);
                        #endregion

                        #region calibration detail
                        item.headerId = headerID;
                        item.workOrder = model.workOrder;

                        var modelDetail = new CreateRequest();
                        modelDetail.employee = new EmployeeModel();

                        modelDetail.employee.id = model.employee.id;
                        modelDetail.employee.name = model.employee.name;
                        modelDetail.entity = item;

                        var resultAddDetail = await _calibrationDetailRepository.Create(modelDetail);

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

                            var _repoDetail = new ServiceSheetDetailRepository(_connectionFactory, EnumContainer.ServiceSheetDetail);
                            var dataServiceSheetDetail = await _repoDetail.GetDataCalibration(model.workOrder);
                            resultAddDetail.transactionCalibration = dataServiceSheetDetail;

                            resultJson.Add("detail", resultAddDetail);
                        }
                        #endregion
                    }
                }
                else
                {
                    List<dynamic> dataObj = JsonConvert.DeserializeObject<List<dynamic>>(JsonConvert.SerializeObject(oldDataHeader));
                    var generalData = dataObj.FirstOrDefault();
                    dataHeaderParam.Add(EnumQuery.HeaderId, generalData[$"{EnumQuery.ID}"]);
                    generalData.smu = smu;

                    List<dynamic> oldDataDetail = await _calibrationDetailRepository.GetDataListByParam(dataHeaderParam);

                    var _repoDetail = new ServiceSheetDetailRepository(_connectionFactory, EnumContainer.ServiceSheetDetail);
                    var dataServiceSheetDetail = await _repoDetail.GetDataCalibration(model.workOrder);
                    oldDataDetail.FirstOrDefault().transactionCalibration = dataServiceSheetDetail;

                    resultJson.Add("header", generalData);
                    resultJson.Add("detail", oldDataDetail.FirstOrDefault());
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
    }
}