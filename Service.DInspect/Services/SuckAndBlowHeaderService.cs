using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;
using Service.DInspect.Repositories;
using Service.DInspect.Models.Response;
using System.Globalization;
using Service.DInspect.Models.Request;
using Service.DInspect.Services.Helpers;
using Service.DInspect.Models.Helper;
using Service.DInspect.Helpers;
using Service.DInspect.Models.Enum;
using Service.DInspect.Models.Response;
using Service.DInspect.Models.Entity;
using Service.DInspect.Models.ADM;
using Service.DInspect.Models;
using Service.DInspect.Interfaces;

namespace Service.DInspect.Services
{
    public class SuckAndBlowHeaderService : ServiceBase
    {
        protected string _container;
        protected IConnectionFactory _connectionFactory;
        protected IRepositoryBase _serviceDetailRepository;
        protected IRepositoryBase _masterServiceSheetRepository;
        protected IRepositoryBase _defectHeaderRepository;
        protected IRepositoryBase _mappingLubricantRepository;

        public SuckAndBlowHeaderService(MySetting appSetting, IConnectionFactory connectionFactory, string container, string accessToken, ILoggerFactory logger) : base(appSetting, connectionFactory, container, accessToken)
        {
            _container = container;
            _connectionFactory = connectionFactory;
            _repository = new SuckAndBlowHeaderRepository(connectionFactory, container);
            _serviceDetailRepository = new SuckAndBlowDetailRepository(connectionFactory, EnumContainer.InterimEngineDetail);
            _masterServiceSheetRepository = new MasterServiceSheetRepository(connectionFactory, EnumContainer.MasterServiceSheet);
            _defectHeaderRepository = new InterimEngineDefectHeaderRepository(connectionFactory, EnumContainer.InterimEngineDefectHeader);
            _mappingLubricantRepository = new InterventionRepository(connectionFactory, EnumContainer.LubricantMapping);
            //_logger = logger.CreateLogger<ServiceSheetDetailService>();
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

                var rsc = await _repository.Get(updateRequest.id);

                bool updatePersonnel = updateRequest.updateParams.Any(s => s.propertyParams.Any(x => x.propertyName == EnumQuery.Status || x.propertyName == EnumQuery.ServicePersonnels));
                bool updateDownloadHistory = updateRequest.updateParams.Any(s => s.propertyParams.Any(x => x.propertyName == EnumQuery.DownloadHistory));

                #region get statusHistories
                if (updatedStatusParam != null || updatedDefectStatusParam != null)
                {
                    //var status = await _repository.GetFieldValue(new GetFieldValueRequest()
                    //{
                    //    id = updateRequest.id,
                    //    keyValue = updateParam.keyValue,
                    //    propertyName = EnumQuery.StatusHistory
                    //});

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

                                string _message = EnumErrorMessage.ErrMsgInterimDefectHeaderApproval.Replace(EnumCommonProperty.ApprovedBy, _approvedBy);

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
                            status = updatedStatusParam.propertyParams.Where(x => x.propertyName == EnumQuery.Status).FirstOrDefault()?.propertyValue == EnumStatus.IEngineClosed ? updatedDefectStatusParam.propertyParams.Where(x => x.propertyName == EnumQuery.DefectStatus).FirstOrDefault()?.propertyValue : updatedStatusParam.propertyParams.Where(x => x.propertyName == EnumQuery.Status).FirstOrDefault()?.propertyValue,
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
                #region get shift data and personnel history
                if (updatePersonnel)
                {
                    //CallAPIHelper callAPI = new CallAPIHelper(_accessToken);
                    //ApiResponse response = await callAPI.Get($"{EnumUrl.GetShift}");
                    //List<ShiftModel> shift = JsonConvert.DeserializeObject<List<ShiftModel>>(JsonConvert.SerializeObject(response.Result.Content));
                    //List<PropertyParam> shiftPropertyParams = new List<PropertyParam>() {
                    //    new PropertyParam()
                    //    {
                    //        propertyName = EnumQuery.Shift,
                    //        propertyValue = JsonConvert.SerializeObject(shift)
                    //    }
                    //};
                    //updateRequest.updateParams.Add(new UpdateParam()
                    //{
                    //    keyValue = EnumGroup.General,
                    //    propertyParams = shiftPropertyParams
                    //});

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
                    }
                }

                var result = await _repository.Update(updateRequest, rsc);

                if (updatedStatusParam != null)
                {
                    //var rsc = await _repository.Get(updateRequest.id);

                    JObject content = new JObject();
                    content.Add(EnumQuery.SSWorkorder, rsc[EnumQuery.SSWorkorder]);
                    content.Add(EnumQuery.Status, updatedStatusParam.propertyParams.Where(x => x.propertyName == EnumQuery.Status).FirstOrDefault()?.propertyValue);

                    CallAPIHelper callAPI = new CallAPIHelper(_accessToken);
                    ApiResponse response = await callAPI.Post(EnumUrl.UpdateStatusDailyScheduleInterimUrl, content);

                    if (response.StatusCode != 200 && response.StatusCode != 201)
                        throw new Exception("Failed to update status in Daily Schedule");

                    string statusEform = updatedStatusParam.propertyParams.Where(x => x.propertyName == EnumQuery.Status).FirstOrDefault()?.propertyValue;
                    if (statusEform == EnumStatus.IEngineOnProgress)
                    {
                        #region generate sos history
                        Dictionary<string, object> paramSosHis = new Dictionary<string, object>()
                        {
                            { EnumQuery.SSWorkorder, rsc[EnumQuery.SSWorkorder]},
                            { EnumQuery.Equipment, rsc[EnumQuery.Equipment]},
                            { EnumQuery.EformType, EnumEformType.SuckAndBlow}
                        };

                        SOSService SOSService = new SOSService(_appSetting, _connectionFactory, EnumContainer.SOSHistory, _accessToken);
                        var resultsos = await SOSService.GenerateSosHistory(paramSosHis);

                        #endregion
                    }
                }

                return new ServiceResult
                {
                    Message = "Data updated successfully",
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

        public async Task<ServiceResult> CreateData(SuckAndBlowRequest model)
        {
            try
            {
                var resultJson = new Dictionary<string, object>();
                var resultData = new List<dynamic>();
                string headerID = string.Empty;

                var dataHeaderParam = new Dictionary<string, object>();
                dataHeaderParam.Add(EnumQuery.SSWorkorder, model.workOrder);
                dataHeaderParam.Add(EnumQuery.Equipment, model.unitNumber);
                dataHeaderParam.Add(EnumQuery.EformType, EnumEformType.SuckAndBlow);
                dataHeaderParam.Add(EnumQuery.PsTypeId, model.psTypeId);
                dataHeaderParam.Add(EnumQuery.IsDeleted, "false");

                var oldDataHeader = await _repository.GetDataListByParam(dataHeaderParam);

                if (oldDataHeader.Count == 0)
                {
                    var dataParam = new Dictionary<string, object>();
                    dataParam.Add(EnumQuery.EformType, EnumEformType.SuckAndBlow);
                    dataParam.Add(EnumQuery.ModelId, model.modelId);
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

                            string psTypeGeneral = item.psTypeId;

                            item.psTypeId = psTypeGeneral.Replace(EnumCommonProperty.PsType, model.psTypeId.ToString());

                            var modelDetail = new CreateRequest();
                            modelDetail.employee = new EmployeeModel();

                            modelDetail.employee.id = model.employee.id;
                            modelDetail.employee.name = model.employee.name;
                            modelDetail.entity = item;
                            string groupName = item.groupName;

                            if (groupName.ToLower() == EnumGroup.SuckAndBlow.ToLower())
                            {
                                CallAPIService callAPI = new CallAPIService(_appSetting, _accessToken);
                                var equipmentAssignmentResponse = await callAPI.GetEquipmentAssignment(model.unitNumber);
                                List<EquipmentAssignmentModel> equipmentAssignment = JsonConvert.DeserializeObject<List<EquipmentAssignmentModel>>(JsonConvert.SerializeObject(equipmentAssignmentResponse));

                                List<dynamic> resultTableLubricant = new List<dynamic>();

                                Dictionary<string, object> paramLubricant = new Dictionary<string, object>();
                                paramLubricant.Add("modelId", model.modelId);
                                //paramLubricant.Add("psTypeId", model.psTypeId);
                                paramLubricant.Add("eformType", "SuckAndBlow");
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

                                        if (dataType != "JArray" && itemReplaceLubricant.items == "<<LUBRICANT>>")
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
                                        value = EnumErrorMessage.ErrMsgLubricantMappingSuckAndBlow.Replace(EnumCommonProperty.ModelUnitId, model.modelId),
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

                                        if (dataType != "JArray" && itemReplaceLubricant.items == "<<LUBRICANT>>")
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

                            string formGeneral = item.form;
                            string psTypeGeneral = item.psTypeId;

                            item.form = formGeneral.Replace(EnumCommonProperty.PsType, model.psTypeId.ToString());
                            item.psTypeId = psTypeGeneral.Replace(EnumCommonProperty.PsType, model.psTypeId.ToString());
                            item.statusHistory = newObjectStatusHistories;
                            item.workOrder = model.workOrder;
                            item.equipment = model.unitNumber;
                            item.siteId = equipmentAssignment.FirstOrDefault().Site;

                            JToken newObjectSupervisor = JToken.FromObject(model.employee);

                            item.supervisor = newObjectSupervisor;

                            var modelHeader = new CreateRequest();
                            modelHeader.employee = new EmployeeModel();

                            modelHeader.employee.id = model.employee.id;
                            modelHeader.employee.name = model.employee.name;
                            modelHeader.entity = item;

                            var resultAddHeader = await _repository.Create(modelHeader);
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

                    resultJson.Add("suckBlowSheet", resultData);
                }
                else
                {
                    List<dynamic> dataObj = JsonConvert.DeserializeObject<List<dynamic>>(JsonConvert.SerializeObject(oldDataHeader));
                    var generalData = dataObj.Where(x => x.groupName == EnumGroup.General).FirstOrDefault();

                    JToken newObjectSupervisor = JToken.FromObject(model.employee);

                    generalData.supervisor = newObjectSupervisor;

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


                    var dataDetail = new Dictionary<string, object>();
                    dataDetail.Add(EnumQuery.HeaderId, generalData[$"{EnumQuery.ID}"]);
                    dataDetail.Add(EnumQuery.SSWorkorder, model.workOrder);

                    var oldDataDetail = await _serviceDetailRepository.GetDataListByParam(dataDetail);

                    resultJson.Add("general", generalData);
                    resultJson.Add("suckBlowSheet", oldDataDetail);
                }

                return new ServiceResult()
                {
                    IsError = false,
                    Content = resultJson
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

                if (siteId != null && filters.Any(x => x.Value == siteId))
                    siteId = null;

                // get daily schedules
                CallAPIHelper callAPIHelper = new CallAPIHelper(_accessToken);
                var dailySchedule = await callAPIHelper.Get(EnumUrl.GetAdmHistoryUrl + $"&siteId={siteId}");
                List<DailyScheduleModel> dailySchedules = JsonConvert.DeserializeObject<List<DailyScheduleModel>>(JsonConvert.SerializeObject(dailySchedule.Result.Content));

                Dictionary<string, object> paramInterim = new Dictionary<string, object>();
                paramInterim.Add(EnumQuery.IsActive, "true");
                paramInterim.Add(EnumQuery.IsDeleted, "false");
                if (siteId != null) paramInterim.Add(EnumQuery.siteId, siteId);
                var serviceSheetHeaders = await _repository.GetDataListByParamJArray(paramInterim);

                foreach (var serviceSheet in dailySchedules)
                {
                    var header = serviceSheetHeaders.FilterEqual(EnumQuery.SSWorkorder, serviceSheet.workOrder).FirstOrDefault();

                    if (header != null)
                    {
                        JToken jHeader = JToken.FromObject(header);

                        if (header != null && userGroup == EnumPosition.Supervisor.ToLower() && jHeader[EnumQuery.Status].ToString() == EnumStatus.EFormSubmited || header != null && userGroup == EnumPosition.Planner.ToLower() && jHeader[EnumQuery.Status].ToString() == EnumStatus.EFormClosed && jHeader[EnumQuery.DefectStatus].ToString() == EnumStatus.DefectApprovedSPV ||
                           header != null && userGroup == EnumPosition.Supervisor.ToLower() && jHeader[EnumQuery.Status].ToString() == EnumStatus.EFormOnProgress)
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
                return new ServiceResult
                {
                    Message = ex.Message,
                    IsError = true
                };
            }
        }

        public async Task<ServiceResult> GetDefectServiceSheetClose(string supervisor)
        {
            try
            {
                List<dynamic> result = new List<dynamic>();

                CallAPIHelper callAPIHelper = new CallAPIHelper(_accessToken);
                var dailySchedule = await callAPIHelper.Get(EnumUrl.GetAdmHistoryUrl);
                List<DailyScheduleModel> dailySchedules = JsonConvert.DeserializeObject<List<DailyScheduleModel>>(JsonConvert.SerializeObject(dailySchedule.Result.Content));

                var serviceSheetHeaders = await _repository.GetActiveDataJArray();

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
                return new ServiceResult
                {
                    Message = ex.Message,
                    IsError = true
                };
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
                var dataResult = await GetDefectServiceSheetClose(supervisor);

                if (dataResult.Content.Count > 0)
                {
                    List<DailyScheduleModel> dataDefectList = JsonConvert.DeserializeObject<List<DailyScheduleModel>>(JsonConvert.SerializeObject(dataResult.Content));
                    //dataDefectList = dataDefectList.Where(x => Int32.Parse(x.psType) < Int32.Parse(psTypeId)).ToList();

                    foreach (var item in dataDefectList)
                    {
                        item.equipmentModel = $"{item.brand} {item.equipmentModel}";
                    }

                    result = (from a in dataDefectList
                              orderby a.dateService descending
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

        public async Task<ServiceResult> GetServiceSheetHistory(string siteId = null)
        {
            try
            {
                // if siteId == HO Site then skip filter by site
                CallAPIHelper callAPIHelperFilter = new CallAPIHelper(_accessToken);
                string groupFilter = EnumGeneralFilterGroup.Site;
                var filterRes = await callAPIHelperFilter.Get(EnumUrl.GetGeneralFilter + $"?group={groupFilter}& ver=v1");
                IList<GeneralFilterHelperModel> filters = JsonConvert.DeserializeObject<List<GeneralFilterHelperModel>>(JsonConvert.SerializeObject(filterRes.Result.Content));

                if (filters.Any(x => x.Value == siteId))
                    siteId = null;

                HistoryHelper historyHelper = new HistoryHelper(_appSetting, _connectionFactory, _accessToken);
                var result = await historyHelper.GetHistory();

                return new ServiceResult
                {
                    Message = "Get  service sheet history successfully",
                    IsError = false,
                    Content = result
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
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
    }
}
