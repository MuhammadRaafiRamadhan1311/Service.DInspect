//using Microsoft.AspNetCore.Connections;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Office2019.Drawing.Model3D;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;
using Microsoft.Azure.Cosmos.Serialization.HybridRow.Schemas;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using OfficeOpenXml.ConditionalFormatting;
using OfficeOpenXml.Style;
using Service.DInspect.Helpers;
using Service.DInspect.Interfaces;
using Service.DInspect.Models;
using Service.DInspect.Models.ADM;
using Service.DInspect.Models.Enum;
using Service.DInspect.Models.Helper;
using Service.DInspect.Models.Request;
using Service.DInspect.Models.Response;
using Service.DInspect.Models.EHMS;
using Service.DInspect.Models.Entity;
using Service.DInspect.Models.Request;
using Service.DInspect.Repositories;
using Service.DInspect.Services.Helpers;
using Service.DInspect.Helpers;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Service.DInspect.Services
{
    public class VersionHistoryService : ServiceBase
    {
        protected string _container;
        protected IRepositoryBase _defectHeaderRepository;
        protected IRepositoryBase _masterServiceSheetRepository;
        protected IRepositoryBase _serviceHeaderRepository;
        protected IRepositoryBase _serviceDetailRepository;
        protected IRepositoryBase _interventioRepository;
        protected IRepositoryBase _mappingLubricantRepository;
        protected IConnectionFactory _connectionFactory;

        public VersionHistoryService(MySetting appSetting, IConnectionFactory connectionFactory, string container, string accessToken, TelemetryClient telemetryClient) : base(appSetting, connectionFactory, container, accessToken)
        {
            _container = container;
            _connectionFactory = connectionFactory;
            _masterServiceSheetRepository = new MasterServiceSheetRepository(connectionFactory, EnumContainer.MasterServiceSheet);
            _mappingLubricantRepository = new InterventionRepository(connectionFactory, EnumContainer.LubricantMapping);
        }
        public async Task<ServiceResult> GetSideMenu(string siteId)
        {
            try
            {
                List<ModelSiteMappingResponse> result = new List<ModelSiteMappingResponse>();

                CallAPIHelper callAPIHelper = new CallAPIHelper(_accessToken);
                string paramSite = siteId == EnumSite.HeadOffice ? "" : siteId;
                var SideMenu = await callAPIHelper.Get(EnumUrl.GetSiteMapping + $"&order=equipmentModel_asc&site={paramSite}");
                List<ModelSiteMapping> listSideMenu = JsonConvert.DeserializeObject<List<ModelSiteMapping>>(JsonConvert.SerializeObject(SideMenu.Result.Content));
                listSideMenu = listSideMenu.OrderBy(x => x.EquipmentModelId).ToList();
                var listEquipmentModel = listSideMenu.Select(x => x.EquipmentModel).Distinct();

                foreach (var model in listEquipmentModel)
                {
                    ModelSiteMappingResponse mappingModel = new ModelSiteMappingResponse();
                    List<string> listPsType = new List<string>();
                    var modelData = listSideMenu.Where(x => x.EquipmentModel == model).ToList();
                    mappingModel.EquipmentModelId = model;
                    mappingModel.MenuName = modelData.Select(x => x.MenuName).FirstOrDefault();
                    List<ModelSiteMappingSubResponse> psType = new List<ModelSiteMappingSubResponse>();
                    foreach (var data in modelData)
                    {

                        if (listPsType.Contains(data.PsType)) continue;

                        string fileUrl = "";
                        if (!string.IsNullOrWhiteSpace(data.FileUrlId) && data.FileUrlId != "NULL")
                        {
                            var urlResponse = await callAPIHelper.Get(EnumUrl.GetFileUrl + $"&id={data.FileUrlId}");
                            fileUrl = JsonConvert.DeserializeObject<string>(JsonConvert.SerializeObject(urlResponse.Result.Content));
                        }
                        ModelSiteMappingSubResponse psTypeData = new ModelSiteMappingSubResponse();
                        psTypeData.PsType = data.PsType;
                        psTypeData.FileUrl = fileUrl;
                        listPsType.Add(data.PsType);
                        psType.Add(psTypeData);
                    }
                    mappingModel.PsType = psType;
                    result.Add(mappingModel);
                }

                return new ServiceResult
                {
                    Message = "Get side menu successfully",
                    IsError = false,
                    Content = result
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<ServiceResult> GetBlankServiceSheet(string psTypeId, string modelId, string siteId)
        {
            try
            {
                var resultJson = new Dictionary<string, object>();
                var resultData = new List<dynamic>();

                CallAPIHelper callAPIHelper = new CallAPIHelper(_accessToken);
                /*var response = await callAPIHelper.Get(EnumUrl.SiteValidation + $"&equipmentModel={modelId}&site={siteId}");
                var isValidSite = JsonConvert.DeserializeObject<bool>(JsonConvert.SerializeObject(response.Result.Content));
                if (!isValidSite)
                {
                    return new ServiceResult
                    {
                        Message = "Get blank service sheet form successfully",
                        IsError = false,
                        Content = resultJson
                    };
                }*/

                var siteMappingResult = await callAPIHelper.Get(EnumUrl.GetSiteMapping + $"&equipmentModel={modelId}&psType={psTypeId}&site={siteId}");
                List<ModelSiteMapping> listSiteMapping = JsonConvert.DeserializeObject<List<ModelSiteMapping>>(JsonConvert.SerializeObject(siteMappingResult.Result.Content));

                string releasedOn = null;
                if (listSiteMapping != null && listSiteMapping.Count > 0)
                {
                    var siteMapping = listSiteMapping.FirstOrDefault();
                    var DateReleasedOn = Convert.ToDateTime(siteMapping.ReleasedOn);
                    releasedOn = DateReleasedOn.ToString("dd MMMM yyyy");
                }
                else
                {
                    return new ServiceResult
                    {
                        Message = "Invalid Site and Model",
                        IsError = true,
                        Content = resultJson
                    };
                }

                var dataParam = new Dictionary<string, object>();
                dataParam.Add(EnumQuery.ModelId, modelId);
                dataParam.Add(EnumQuery.PsTypeId, psTypeId);
                //dataParam.Add(EnumQuery.SSWorkorder, model.workOrder);
                dataParam.Add(EnumQuery.IsDeleted, "false");

                var result = await _masterServiceSheetRepository.GetDataListByParam(dataParam);

                foreach (var item in result)
                {
                    if (item.groupName != EnumGroup.General)
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

                        if (item.groupName == EnumGroup.LubeService)
                        {
                            List<dynamic> resultTableLubricant = new List<dynamic>();

                            Dictionary<string, object> paramLubricant = new Dictionary<string, object>();
                            paramLubricant.Add(EnumQuery.ModelId, modelId);
                            paramLubricant.Add(EnumQuery.PsTypeId, psTypeId);
                            paramLubricant.Add(EnumQuery.site, siteId == EnumSite.HeadOffice ? EnumSite.BlackWater : siteId);
                            paramLubricant.Add(EnumQuery.IsDeleted, "false");

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

                                foreach (var itemReplaceLubricant in item.subGroup[0].taskGroup[1].task)
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
                                    value = EnumErrorMessage.ErrMsgLubricantMapping.Replace(EnumCommonProperty.ModelUnitId, modelId).Replace(EnumCommonProperty.PsType, psTypeId),
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
                                foreach (var itemReplaceLubricant in item.subGroup[0].taskGroup[1].task)
                                {
                                    var dataType = itemReplaceLubricant.items.GetType().Name;

                                    if (dataType != "JArray" && itemReplaceLubricant.items == EnumCommonProperty.Lubricant)
                                    {
                                        itemReplaceLubricant.items = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(resultTableLubricant));
                                    }
                                }
                            }
                        }

                        resultData.Add(item);
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
                        item.releasedDate = releasedOn;

                        resultJson.Add("general", item);
                    }
                }

                resultJson.Add("serviceSheet", resultData);
                resultJson.Add("releaseDate", releasedOn);
                return new ServiceResult
                {
                    Message = "Get blank service sheet form successfully",
                    IsError = false,
                    Content = resultJson
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<ServiceResultWithTotalData> GetTaskCollection(GetTaskCollectionRequest request)
        {
            try
            {
                List<TaskCollectionResponse> result = new List<TaskCollectionResponse>();
                string pattern = @"[a-zA-Z].*";
                var _repoMasterServiceSheet = new MasterServiceSheetRepository(_connectionFactory, EnumContainer.MasterServiceSheet);
                request.status = request.status?.ToLower() == EnumQuery.Active.ToLower() ? bool.TrueString.ToLower() : request.status?.ToLower() == EnumQuery.Inactive.ToLower() ? bool.FalseString.ToLower() : "";
                request.psTypeId = request.psTypeId?.ToLower() == EnumCaption.Interim.ToLower() ? EnumCommonProperty.PsType : request.psTypeId;
                if (string.IsNullOrWhiteSpace(request.version))
                {
                    if (string.IsNullOrWhiteSpace(request.status))
                    {
                        request.version = "";
                        request.status = bool.TrueString.ToLower();
                    }
                    else if (request.status?.ToLower() == bool.FalseString.ToLower())
                    {
                        return new ServiceResultWithTotalData
                        {
                            Message = "Get task collection successfully",
                            IsError = false,
                            Total = "0",
                            Content = new List<TaskCollectionResponse>()
                        };
                    }
                    else
                    {
                        request.version = "";
                    }
                }

                var resp = await _repoMasterServiceSheet.GetTaskCollection(request);
                var respDataCount = await _repoMasterServiceSheet.GetTotalData(request);

                //get release date from model site mapping
                string modelId = request.modelId == null || request.modelId == "" ? null : request.modelId.ToString();
                if (modelId != null)
                {
                    modelId = modelId.Replace("KOM ", "");
                    modelId = modelId.Replace("CAT ", "");
                    modelId = modelId.Replace("HIT ", "");
                    modelId = modelId.Replace("LIE ", "");
                }

                string _date = request.releaseDate == null || request.releaseDate == "" ? null : DateTime.Parse(request.releaseDate).ToString("yyyy-MM-dd");

                CallAPIHelper callAPIHelper = new CallAPIHelper(_accessToken);
                var siteMappingResult = await callAPIHelper.Get(EnumUrl.GetSiteMapping + $"&equipmentModel={request.modelId}&psType={request.psTypeId}&releasedOn={_date}&releasedOnTo={_date}");
                List<ModelSiteMapping> listSiteMapping = JsonConvert.DeserializeObject<List<ModelSiteMapping>>(JsonConvert.SerializeObject(siteMappingResult.Result.Content));

                result = JsonConvert.DeserializeObject<List<TaskCollectionResponse>>(JsonConvert.SerializeObject(resp));

                int count = 0;
                foreach (var item in result)
                {
                    if (listSiteMapping.Count > 0)
                    {
                        result[count].ReleaseDate = listSiteMapping.Where(x => x.EquipmentModel == item.ModelId && x.PsType == item.PsTypeId).Select(x => DateTime.Parse(x.ReleasedOn).ToString("dd MMM yyyy")).FirstOrDefault();
                        //DateTime.Parse(listSiteMapping[0].ReleasedOn).ToString("dd MMM yyyy");
                    }
                    else
                    {
                        result[count].ReleaseDate = "";
                    }
                    count++;
                }

                var finalResult = result.Select(x =>
                {
                    if (x.Status == bool.TrueString.ToLower()) x.Status = EnumQuery.Active;
                    else x.Status = EnumQuery.Inactive;

                    x.SubTask = Regex.Match(x.SubTask, pattern).Value;

                    if (x.PsTypeId == EnumCommonProperty.PsType) x.PsTypeId = EnumCaption.Interim;
                    return x;
                });
                if (!string.IsNullOrWhiteSpace(request.subTask))
                {
                    finalResult = finalResult.Where(x => x.SubTask == request.subTask).ToList();
                }
                #region logic orderBy and condition
                bool isNeedPagination = false;
                if (request.orderBy != null)
                {
                    if (request.orderBy.Contains(EnumQuery.Category))
                    {
                        var orderQuery = request.orderBy.Split('_').Last();
                        if (orderQuery == EnumQuery.ASC.ToLower())
                        {
                            finalResult = finalResult.OrderBy(x => x.Category).ToList();
                        }
                        else
                        {
                            finalResult = finalResult.OrderByDescending(x => x.Category).ToList();
                        }
                        isNeedPagination = true;
                    }
                    else if (request.orderBy.Contains(EnumQuery.Rating))
                    {
                        var orderQuery = request.orderBy.Split('_').Last();
                        if (orderQuery == EnumQuery.ASC.ToLower())
                        {
                            finalResult = finalResult.OrderBy(x => x.Rating).ToList();
                        }
                        else
                        {
                            finalResult = finalResult.OrderByDescending(x => x.Rating).ToList();
                        }
                        isNeedPagination = true;
                    }
                    else if (request.orderBy.Contains(EnumQuery.Description))
                    {
                        var orderQuery = request.orderBy.Split('_').Last();
                        if (orderQuery == EnumQuery.ASC.ToLower())
                        {
                            finalResult = finalResult.OrderBy(x => x.Description).ToList();
                        }
                        else
                        {
                            finalResult = finalResult.OrderByDescending(x => x.Description).ToList();
                        }
                        isNeedPagination = true;
                    }
                    else if (request.orderBy.Contains(EnumQuery.SubTaskUnderscore))
                    {
                        var orderQuery = request.orderBy.Split('_').Last();
                        if (orderQuery == EnumQuery.ASC.ToLower())
                        {
                            finalResult = finalResult.OrderBy(x => x.SubTask).ToList();
                        }
                        else
                        {
                            finalResult = finalResult.OrderByDescending(x => x.SubTask).ToList();
                        }
                        isNeedPagination = true;
                    }
                    else if (request.orderBy.Contains(EnumQuery.PsTypeUnderscore))
                    {
                        var orderQuery = request.orderBy.Split('_').Last();
                        if (orderQuery == EnumQuery.ASC.ToLower())
                        {
                            finalResult = finalResult.OrderBy(x =>
                            {
                                int.TryParse(x.PsTypeId, out int psTypeId);
                                return psTypeId;
                            }).ToList();
                        }
                        else
                        {
                            finalResult = finalResult.OrderByDescending(x =>
                            {
                                int.TryParse(x.PsTypeId, out int psTypeId);
                                return psTypeId;
                            }).ToList();
                        }
                        isNeedPagination = true;
                    }
                }
                if (!string.IsNullOrWhiteSpace(request.subTask) || isNeedPagination)
                {
                    finalResult = finalResult.Skip((request.page - 1) * request.pageSize).Take(request.pageSize).ToList();
                }
                #endregion

                List<TotalDataHelperModel> jsonCountData = JsonConvert.DeserializeObject<List<TotalDataHelperModel>>(JsonConvert.SerializeObject(respDataCount));

                return new ServiceResultWithTotalData
                {
                    Message = "Get task collection successfully",
                    IsError = false,
                    Total = jsonCountData.FirstOrDefault().TotalData,
                    Content = finalResult
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<byte[]> ExportTaskCollection(GetTaskCollectionRequest request)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var ep = new ExcelPackage())
            {
                #region Header
                ExcelWorksheet Sheet = ep.Workbook.Worksheets.Add("Task Collection");

                Sheet.Cells["A1"].Value = "Model";
                Sheet.Cells["B1"].Value = "Service Size";
                Sheet.Cells["C1"].Value = "Version";
                Sheet.Cells["D1"].Value = "Task Category";
                Sheet.Cells["E1"].Value = "Task Description";
                Sheet.Cells["F1"].Value = "Sub Task";
                Sheet.Cells["G1"].Value = "Task Rating";
                Sheet.Cells["H1"].Value = "Status";
                Sheet.Cells["I1"].Value = "Release Date";
                #endregion


                #region Content
                List<TaskCollectionResponse> result = new List<TaskCollectionResponse>();
                var _repoMasterServiceSheet = new MasterServiceSheetRepository(_connectionFactory, EnumContainer.MasterServiceSheet);
                request.status = request.status?.ToLower() == EnumQuery.Active.ToLower() ? bool.TrueString.ToLower() : request.status?.ToLower() == EnumQuery.Inactive.ToLower() ? bool.FalseString.ToLower() : "";
                request.psTypeId = request.psTypeId?.ToLower() == EnumCaption.Interim.ToLower() ? EnumCommonProperty.PsType : request.psTypeId;
                if (string.IsNullOrWhiteSpace(request.version))
                {
                    if (string.IsNullOrWhiteSpace(request.status))
                    {
                        request.version = "";
                        request.status = bool.TrueString.ToLower();
                    }
                    else if (request.status?.ToLower() == bool.FalseString.ToLower())
                    {
                        var nullStream = new MemoryStream(ep.GetAsByteArray());
                        return nullStream.ToArray();
                    }
                    else
                    {
                        request.version = "";
                    }
                }
                string pattern = @"[a-zA-Z].*";
                request.page = 1;
                request.pageSize = int.MaxValue - 1;
                var resp = await _repoMasterServiceSheet.GetTaskCollection(request);
                result = JsonConvert.DeserializeObject<List<TaskCollectionResponse>>(JsonConvert.SerializeObject(resp));

                //get release date from model site mapping
                string modelId = request.modelId == null || request.modelId == "" ? null : request.modelId.ToString();
                if (modelId != null)
                {
                    modelId = modelId.Replace("KOM ", "");
                    modelId = modelId.Replace("CAT ", "");
                    modelId = modelId.Replace("HIT ", "");
                    modelId = modelId.Replace("LIE ", "");
                }

                string _date = request.releaseDate == null || request.releaseDate == "" ? null : DateTime.Parse(request.releaseDate).ToString("yyyy-MM-dd");

                CallAPIHelper callAPIHelper = new CallAPIHelper(_accessToken);
                var siteMappingResult = await callAPIHelper.Get(EnumUrl.GetSiteMapping + $"&equipmentModel={request.modelId}&psType={request.psTypeId}&releasedOn={_date}&releasedOnTo={_date}");
                List<ModelSiteMapping> listSiteMapping = JsonConvert.DeserializeObject<List<ModelSiteMapping>>(JsonConvert.SerializeObject(siteMappingResult.Result.Content));

                int count = 0;
                foreach (var item in result)
                {
                    if (listSiteMapping.Count > 0)
                    {
                        result[count].ReleaseDate = listSiteMapping.Where(x => x.EquipmentModel == item.ModelId && x.PsType == item.PsTypeId).Select(x => DateTime.Parse(x.ReleasedOn).ToString("dd MMM yyyy")).FirstOrDefault();
                        //DateTime.Parse(listSiteMapping[0].ReleasedOn).ToString("dd MMM yyyy");
                    }
                    else
                    {
                        result[count].ReleaseDate = "";
                    }
                    count++;
                }

                var finalResult = result.Select(x =>
                {
                    if (x.Status == bool.TrueString.ToLower()) x.Status = EnumQuery.Active;
                    else x.Status = EnumQuery.Inactive;

                    x.SubTask = Regex.Match(x.SubTask, pattern).Value;

                    if (x.PsTypeId == EnumCommonProperty.PsType) x.PsTypeId = EnumCaption.Interim;
                    return x;
                });
                if (!string.IsNullOrWhiteSpace(request.subTask))
                {
                    finalResult = finalResult.Where(x => x.SubTask == request.subTask).ToList();
                }

                int row = 2;

                foreach (var data in finalResult)
                {
                    TaskCollectionResponse item = JsonConvert.DeserializeObject<TaskCollectionResponse>(JsonConvert.SerializeObject(data));

                    Sheet.Cells[string.Format("A{0}", row)].Value = item.ModelId?.Trim();
                    Sheet.Cells[string.Format("B{0}", row)].Value = item.PsTypeId?.Trim();
                    Sheet.Cells[string.Format("C{0}", row)].Value = item.Version?.Trim();
                    Sheet.Cells[string.Format("D{0}", row)].Value = item.Category?.Trim();
                    Sheet.Cells[string.Format("E{0}", row)].Value = item.Description?.Trim();
                    Sheet.Cells[string.Format("F{0}", row)].Value = item.SubTask?.Trim();
                    Sheet.Cells[string.Format("G{0}", row)].Value = item.Rating?.Trim();
                    Sheet.Cells[string.Format("H{0}", row)].Value = item.Status?.Trim();
                    Sheet.Cells[string.Format("I{0}", row)].Value = item.ReleaseDate?.Trim();
                    row++;
                }
                #endregion
                row--;
                //Sheet.Cells[string.Format("A1:I{0}", row)].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                //Sheet.Cells[string.Format("A1:I{0}", row)].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                //Sheet.Cells[string.Format("A1:I{0}", row)].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                //Sheet.Cells[string.Format("A1:I{0}", row)].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                //Sheet.Cells["A:I"].AutoFitColumns();

                var stream = new MemoryStream(ep.GetAsByteArray());
                return stream.ToArray();
            }
        }

        public async Task<ServiceResult> GetFilterTaskCollection(GetTaskCollectionRequest request)
        {
            try
            {
                #region declare variable
                var _repoMasterServiceSheet = new MasterServiceSheetRepository(_connectionFactory, EnumContainer.MasterServiceSheet);
                List<string> listModel = new List<string>();
                List<string> listPsType = new List<string>();
                List<string> listVersion = new List<string>();
                List<string> listCategroy = new List<string>();
                List<string> listSubtask = new List<string>();
                List<string> listStatus = new List<string>();
                List<string> listReleaseDate = new List<string>();
                string pattern = @"[a-zA-Z].*";
                #endregion

                request.status = request.status?.ToLower() == EnumQuery.Active.ToLower() ? bool.TrueString.ToLower() : request.status?.ToLower() == EnumQuery.Inactive.ToLower() ? bool.FalseString.ToLower() : "";
                request.psTypeId = request.psTypeId?.ToLower() == EnumCaption.Interim.ToLower() ? EnumCommonProperty.PsType : request.psTypeId;

                bool ignoreStatusForVersion = false;

                if (string.IsNullOrWhiteSpace(request.version))
                {
                    if (string.IsNullOrWhiteSpace(request.status))
                    {
                        request.version = "";
                        request.status = bool.TrueString.ToLower();
                        ignoreStatusForVersion = true;
                    }
                    else if (request.status?.ToLower() == bool.FalseString.ToLower())
                    {
                        return new ServiceResultWithTotalData
                        {
                            Message = "Get task collection successfully",
                            IsError = false,
                            Total = "0",
                            Content = new List<TaskCollectionResponse>()
                        };
                    }
                    else
                    {
                        request.version = "";
                    }
                }

                var respModel = await _repoMasterServiceSheet.GetModelTaskCollection(request);
                List<TaskCollectionResponse> model = JsonConvert.DeserializeObject<List<TaskCollectionResponse>>(JsonConvert.SerializeObject(respModel));
                listModel = model.Where(x => !string.IsNullOrWhiteSpace(x.ModelId)).Select(x => x.ModelId).ToList();

                var respPsType = await _repoMasterServiceSheet.GetPsTypeTaskCollection(request);
                model = JsonConvert.DeserializeObject<List<TaskCollectionResponse>>(JsonConvert.SerializeObject(respPsType));
                listPsType = model.Where(x => !string.IsNullOrWhiteSpace(x.PsTypeId)).Select(x =>
                {
                    if (x.PsTypeId == EnumCommonProperty.PsType) x.PsTypeId = EnumCaption.Interim;
                    return x.PsTypeId;
                }).OrderBy(x => long.TryParse(x, out long parsed) ? parsed : 0).ToList();

                var respVersion = await _repoMasterServiceSheet.GetVersionTaskCollection(request, ignoreStatusForVersion);
                model = JsonConvert.DeserializeObject<List<TaskCollectionResponse>>(JsonConvert.SerializeObject(respVersion));
                listVersion = model.Where(x => !string.IsNullOrWhiteSpace(x.Version)).Select(x => x.Version).OrderByDescending(x => x).ToList();

                var respCategory = await _repoMasterServiceSheet.GetCategoryTaskCollection(request);
                model = JsonConvert.DeserializeObject<List<TaskCollectionResponse>>(JsonConvert.SerializeObject(respCategory));
                listCategroy = model.Where(x => !string.IsNullOrWhiteSpace(x.Category)).Select(x => x.Category).ToList();

                var respSubTask = await _repoMasterServiceSheet.GetSubTaskTaskCollection(request);
                model = JsonConvert.DeserializeObject<List<TaskCollectionResponse>>(JsonConvert.SerializeObject(respSubTask));
                listSubtask = model.Where(x => !string.IsNullOrWhiteSpace(x.SubTask) && !x.SubTask.Contains(".")).Select(x =>
                {
                    return Regex.Match(x.SubTask, pattern).Value;
                }).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();

                var respStatus = await _repoMasterServiceSheet.GetStatusTaskCollection(request);
                model = JsonConvert.DeserializeObject<List<TaskCollectionResponse>>(JsonConvert.SerializeObject(respStatus));
                listStatus = model.Select(x =>
                {
                    if (x.Status == bool.TrueString.ToLower()) return EnumQuery.Active;
                    else return EnumQuery.Inactive;
                }).ToList();

                //get release date from model site mapping
                string modelId = request.modelId == null || request.modelId == "" ? null : request.modelId.ToString();
                if (modelId != null)
                {
                    modelId = modelId.Replace("KOM ", "");
                    modelId = modelId.Replace("CAT ", "");
                    modelId = modelId.Replace("HIT ", "");
                    modelId = modelId.Replace("LIE ", "");
                }

                string _date = request.releaseDate == null || request.releaseDate == "" ? null : DateTime.Parse(request.releaseDate).ToString("yyyy-MM-dd");

                CallAPIHelper callAPIHelper = new CallAPIHelper(_accessToken);
                var siteMappingResult = await callAPIHelper.Get(EnumUrl.GetSiteMapping + $"&equipmentModel={request.modelId}&psType={request.psTypeId}&releasedOn={_date}&releasedOnTo={_date}");
                List<ModelSiteMapping> listSiteMapping = JsonConvert.DeserializeObject<List<ModelSiteMapping>>(JsonConvert.SerializeObject(siteMappingResult.Result.Content));

                int count = 0;
                foreach (var item in listSiteMapping)
                {
                    listReleaseDate.Add(DateTime.Parse(item.ReleasedOn).ToString("dd MMM yyyy"));
                    count++;
                }

                listReleaseDate = listReleaseDate.Distinct().ToList();

                TaskCollectionFilterResponse result = new TaskCollectionFilterResponse
                {
                    ListModelId = listModel,
                    ListPsTypeid = listPsType,
                    ListVersion = listVersion,
                    listCategroy = listCategroy,
                    listSubtask = listSubtask,
                    listStatus = listStatus,
                    listReleaseDate = listReleaseDate
                };

                return new ServiceResult
                {
                    Message = "Get task collection successfully",
                    IsError = false,
                    Content = result
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
