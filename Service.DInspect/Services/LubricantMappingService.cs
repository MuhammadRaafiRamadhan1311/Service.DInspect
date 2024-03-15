using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;
using Newtonsoft.Json;
using OfficeOpenXml;
using Service.DInspect.Interfaces;
using Service.DInspect.Models;
using Service.DInspect.Models.Enum;
using Service.DInspect.Models.Request;
using Service.DInspect.Models.Entity;
using Service.DInspect.Models.Response;
using Service.DInspect.Repositories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Service.DInspect.Services
{
    public class LubricantMappingService : ServiceBase
    {
        protected string _container;
        protected IConnectionFactory _connectionFactory;
        //protected IRepositoryBase _sosHistory, _serviceSheetRepository, _serviceSheetDetailRepository, _lubricantMappingRepository;

        public LubricantMappingService(MySetting appSetting, IConnectionFactory connectionFactory, string container, string accessToken) : base(appSetting, connectionFactory, container, accessToken)
        {
            _container = container;
            _connectionFactory = connectionFactory;
            _repository = new LubricantMappingRepository(connectionFactory, container);
        }

        public async Task<byte[]> GenerateLubricantMapping(LubricantMappingRequest model)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var ep = new ExcelPackage())
            {
                #region Content
                var _repoServiceSheet = new LubricantMappingRepository(_connectionFactory, EnumContainer.LubricantMapping);

                Dictionary<string, object> paramLubcricant = new Dictionary<string, object>();

                if (!string.IsNullOrEmpty(model.modelId))
                    paramLubcricant.Add("modelId", model.modelId);

                if (!string.IsNullOrEmpty(model.psTypeId))
                    paramLubcricant.Add("psTypeId", model.psTypeId);

                if (!string.IsNullOrEmpty(model.siteId))
                    paramLubcricant.Add("site", model.siteId);

                paramLubcricant.Add("isDeleted", "false");

                var result = await _repoServiceSheet.GetDataByParam(paramLubcricant);

                ExcelWorksheet Sheet = ep.Workbook.Worksheets.Add($"Lubricant");

                Sheet.Cells["A1"].Value = "ID";
                Sheet.Cells["B1"].Value = result.id.ToString();
                Sheet.Cells["A2"].Value = "Model";
                Sheet.Cells["B2"].Value = result.modelId.ToString();
                Sheet.Cells["A3"].Value = "PsTypeId";
                Sheet.Cells["B3"].Value = result.psTypeId.ToString();
                Sheet.Cells["A4"].Value = "Site";
                Sheet.Cells["B4"].Value = result.site.ToString();

                int row = 7;

                Sheet.Cells["A6"].Value = "Key";
                Sheet.Cells["B6"].Value = "TaskKeyOilSample";
                Sheet.Cells["C6"].Value = "TaskKeyOilChange";
                Sheet.Cells["D6"].Value = "TaskKeyOilLevelCheck";
                Sheet.Cells["E6"].Value = "TaskTopUpLevelCheck";
                Sheet.Cells["F6"].Value = "CompartmentLubricant";
                Sheet.Cells["G6"].Value = "CompartmentCode";
                Sheet.Cells["H6"].Value = "RecommendedLubricant";
                Sheet.Cells["I6"].Value = "Volume";
                Sheet.Cells["J6"].Value = "UoM";
                Sheet.Cells["K6"].Value = "LubricantType";
                Sheet.Cells["L6"].Value = "IsSOS";

                foreach (var data in result.detail)
                {
                    dynamic item = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(data));
                    Sheet.Cells[string.Format("A{0}", row)].Value = item.key.ToString();
                    Sheet.Cells[string.Format("B{0}", row)].Value = item.taskKeyOilSample.ToString();
                    Sheet.Cells[string.Format("C{0}", row)].Value = item.taskKeyOilChange.ToString();
                    Sheet.Cells[string.Format("D{0}", row)].Value = item.taskKeyOilLevelCheck.ToString();
                    Sheet.Cells[string.Format("E{0}", row)].Value = item.taskTopUpLevelCheck.ToString();
                    Sheet.Cells[string.Format("F{0}", row)].Value = item.compartmentLubricant.ToString();
                    Sheet.Cells[string.Format("G{0}", row)].Value = item.compartmentCode.ToString();
                    Sheet.Cells[string.Format("H{0}", row)].Value = item.recommendedLubricant.ToString();
                    Sheet.Cells[string.Format("I{0}", row)].Value = item.volume.ToString();
                    Sheet.Cells[string.Format("J{0}", row)].Value = item.uoM.ToString();
                    Sheet.Cells[string.Format("K{0}", row)].Value = item.lubricantType.ToString();
                    Sheet.Cells[string.Format("L{0}", row)].Value = item.isSOS.ToString();

                    row++;
                }
                #endregion

                var stream = new MemoryStream(ep.GetAsByteArray());
                return stream.ToArray();
            }
        }

        public async Task<dynamic> UploadLubricantMapping(IFormFile files)
        {
            MappingRecommendedLubricant dataMapping = new MappingRecommendedLubricant();
            dataMapping.detail = new List<DetailMappingLubricant>();

            using (var excel = files.OpenReadStream())
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage(excel);

                ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                var rowCount = worksheet.Dimension.Rows;

                dataMapping.site = worksheet.Cells[4, 2].Value.ToString();
                dataMapping.modelId = worksheet.Cells[2, 2].Value.ToString();
                dataMapping.psTypeId = worksheet.Cells[3, 2].Value.ToString();

                Dictionary<string, object> paramMapping = new Dictionary<string, object>();
                paramMapping.Add("modelId", dataMapping.modelId);
                paramMapping.Add("psTypeId", dataMapping.psTypeId);
                paramMapping.Add("site", dataMapping.site);

                var resultMapping = await _repository.GetDataByParam(paramMapping);

                if (resultMapping == null)
                {
                    for (int row = 7; row <= rowCount; row++)
                    {
                        DetailMappingLubricant detailMapping = new DetailMappingLubricant();
                        detailMapping.key = worksheet.Cells[row, 1].Value == null ? string.Empty : worksheet.Cells[row, 1].Value.ToString();
                        detailMapping.taskKeyOilSample = worksheet.Cells[row, 2].Value == null ? string.Empty : worksheet.Cells[row, 2].Value.ToString();
                        detailMapping.taskKeyOilChange = worksheet.Cells[row, 3].Value == null ? string.Empty : worksheet.Cells[row, 3].Value.ToString();
                        detailMapping.taskKeyOilLevelCheck = worksheet.Cells[row, 4].Value == null ? string.Empty : worksheet.Cells[row, 4].Value.ToString();
                        detailMapping.taskTopUpLevelCheck = worksheet.Cells[row, 5].Value == null ? string.Empty : worksheet.Cells[row, 5].Value.ToString();
                        detailMapping.compartmentLubricant = worksheet.Cells[row, 6].Value == null ? string.Empty : worksheet.Cells[row, 6].Value.ToString();
                        detailMapping.compartmentCode = worksheet.Cells[row, 7].Value == null ? string.Empty : worksheet.Cells[row, 7].Value.ToString();
                        detailMapping.recommendedLubricant = worksheet.Cells[row, 8].Value == null ? string.Empty : worksheet.Cells[row, 8].Value.ToString();
                        detailMapping.volume = worksheet.Cells[row, 9].Value == null ? string.Empty : worksheet.Cells[row, 9].Value.ToString();
                        detailMapping.uoM = worksheet.Cells[row, 10].Value == null ? string.Empty : worksheet.Cells[row, 10].Value.ToString();
                        detailMapping.lubricantType = worksheet.Cells[row, 11].Value == null ? string.Empty : worksheet.Cells[row, 11].Value.ToString();
                        detailMapping.isSOS = worksheet.Cells[row, 12].Value == null ? string.Empty : worksheet.Cells[row, 12].Value.ToString();

                        dataMapping.detail.Add(detailMapping);
                    }

                    CreateRequest createRequest = new CreateRequest()
                    {
                        entity = dataMapping,
                        employee = new EmployeeModel() { id = "SYSTEM", name = "SYSTEM" }
                    };

                    await _repository.Create(createRequest);
                }
                else
                {
                    for (int row = 7; row <= rowCount; row++)
                    {
                        DetailMappingLubricant detailMapping = new DetailMappingLubricant();
                        detailMapping.key = worksheet.Cells[row, 1].Value == null ? string.Empty : worksheet.Cells[row, 1].Value.ToString();
                        detailMapping.taskKeyOilSample = worksheet.Cells[row, 2].Value == null ? string.Empty : worksheet.Cells[row, 2].Value.ToString();
                        detailMapping.taskKeyOilChange = worksheet.Cells[row, 3].Value == null ? string.Empty : worksheet.Cells[row, 3].Value.ToString();
                        detailMapping.taskKeyOilLevelCheck = worksheet.Cells[row, 4].Value == null ? string.Empty : worksheet.Cells[row, 4].Value.ToString();
                        detailMapping.taskTopUpLevelCheck = worksheet.Cells[row, 5].Value == null ? string.Empty : worksheet.Cells[row, 5].Value.ToString();
                        detailMapping.compartmentLubricant = worksheet.Cells[row, 6].Value == null ? string.Empty : worksheet.Cells[row, 6].Value.ToString();
                        detailMapping.compartmentCode = worksheet.Cells[row, 7].Value == null ? string.Empty : worksheet.Cells[row, 7].Value.ToString();
                        detailMapping.recommendedLubricant = worksheet.Cells[row, 8].Value == null ? string.Empty : worksheet.Cells[row, 8].Value.ToString();
                        detailMapping.volume = worksheet.Cells[row, 9].Value == null ? string.Empty : worksheet.Cells[row, 9].Value.ToString();
                        detailMapping.uoM = worksheet.Cells[row, 10].Value == null ? string.Empty : worksheet.Cells[row, 10].Value.ToString();
                        detailMapping.lubricantType = worksheet.Cells[row, 11].Value == null ? string.Empty : worksheet.Cells[row, 11].Value.ToString();
                        detailMapping.isSOS = worksheet.Cells[row, 12].Value == null ? string.Empty : worksheet.Cells[row, 12].Value.ToString();

                        dataMapping.detail.Add(detailMapping);
                    }

                    List<PropertyParam> propertyParams = new List<PropertyParam>() {
                        new PropertyParam()
                        {
                            propertyName = EnumQuery.Detail,
                            propertyValue = JsonConvert.SerializeObject(dataMapping.detail)
                        }
                    };

                    UpdateRequest updateDataParams = new UpdateRequest();
                    //updateDataParams.id = worksheet.Cells[1, 2].Value.ToString();
                    updateDataParams.id = resultMapping.id.ToString();
                    updateDataParams.workOrder = "";
                    updateDataParams.updateParams = new List<UpdateParam>();
                    updateDataParams.employee = new EmployeeModel { id = "SYSTEM", name = "SYSTEM" };

                    updateDataParams.updateParams.Add(new UpdateParam()
                    {
                        keyValue = "MAPPING",
                        propertyParams = propertyParams
                    });

                    var resultUpdateHeader = await _repository.Update(updateDataParams, dataMapping);
                }
            }

            return new ServiceResult
            {
                Message = "Data insert successfully",
                IsError = false,
                Content = dataMapping
            };
        }

        public async Task<ServiceResult> ValidateData(LubricantMappingRequest model)
        {
            var _repoServiceSheet = new LubricantMappingRepository(_connectionFactory, EnumContainer.LubricantMapping);

            Dictionary<string, object> paramLubcricant = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(model.modelId))
                paramLubcricant.Add("modelId", model.modelId);

            if (!string.IsNullOrEmpty(model.psTypeId))
                paramLubcricant.Add("psTypeId", model.psTypeId);

            if (!string.IsNullOrEmpty(model.siteId))
                paramLubcricant.Add("site", model.siteId);

            paramLubcricant.Add("isDeleted", "false");

            var result = await _repoServiceSheet.GetDataByParam(paramLubcricant);

            if (result == null)
            {
                return new ServiceResult
                {
                    Message = "Data Not Found",
                    IsError = true,
                    Content = null
                };
            }

            return new ServiceResult
            {
                Message = "Data Valid",
                IsError = false,
                Content = null
            };
        }

        #region Interim Lubricant Mapping
        public async Task<byte[]> GenerateInterimLubricantMapping(InterimLubricantMappingRequest model)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var ep = new ExcelPackage())
            {
                #region Content
                var _repoServiceSheet = new LubricantMappingRepository(_connectionFactory, EnumContainer.LubricantMapping);

                Dictionary<string, object> paramLubcricant = new Dictionary<string, object>();

                if (!string.IsNullOrEmpty(model.modelId))
                    paramLubcricant.Add("modelId", model.modelId);

                if (!string.IsNullOrEmpty(model.siteId))
                    paramLubcricant.Add("site", model.siteId);

                if (!string.IsNullOrEmpty(model.eformType))
                    paramLubcricant.Add("eformType", model.eformType);

                paramLubcricant.Add("isDeleted", "false");

                var result = await _repoServiceSheet.GetDataByParam(paramLubcricant);

                ExcelWorksheet Sheet = ep.Workbook.Worksheets.Add($"Interim Lubricant");

                Sheet.Cells["A1"].Value = "ID";
                Sheet.Cells["B1"].Value = result.id.ToString();
                Sheet.Cells["A2"].Value = "Model";
                Sheet.Cells["B2"].Value = result.modelId.ToString();
                Sheet.Cells["A3"].Value = "Site";
                Sheet.Cells["B3"].Value = result.site.ToString();
                Sheet.Cells["A4"].Value = "eformType";
                Sheet.Cells["B4"].Value = result.eformType.ToString();

                int row = 7;

                Sheet.Cells["A6"].Value = "Key";
                Sheet.Cells["B6"].Value = "TaskKeyOilSample";
                Sheet.Cells["C6"].Value = "TaskKeyOilChange";
                Sheet.Cells["D6"].Value = "TaskKeyOilLevelCheck";
                Sheet.Cells["E6"].Value = "TaskTopUpLevelCheck";
                Sheet.Cells["F6"].Value = "CompartmentLubricant";
                Sheet.Cells["G6"].Value = "CompartmentCode";
                Sheet.Cells["H6"].Value = "RecommendedLubricant";
                Sheet.Cells["I6"].Value = "Volume";
                Sheet.Cells["J6"].Value = "UoM";
                Sheet.Cells["K6"].Value = "LubricantType";
                Sheet.Cells["L6"].Value = "IsSOS";

                foreach (var data in result.detail)
                {
                    dynamic item = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(data));
                    Sheet.Cells[string.Format("A{0}", row)].Value = item.key.ToString();
                    Sheet.Cells[string.Format("B{0}", row)].Value = item.taskKeyOilSample.ToString();
                    Sheet.Cells[string.Format("C{0}", row)].Value = item.taskKeyOilChange.ToString();
                    Sheet.Cells[string.Format("D{0}", row)].Value = item.taskKeyOilLevelCheck.ToString();
                    Sheet.Cells[string.Format("E{0}", row)].Value = item.taskTopUpLevelCheck.ToString();
                    Sheet.Cells[string.Format("F{0}", row)].Value = item.compartmentLubricant.ToString();
                    Sheet.Cells[string.Format("G{0}", row)].Value = item.compartmentCode.ToString();
                    Sheet.Cells[string.Format("H{0}", row)].Value = item.recommendedLubricant.ToString();
                    Sheet.Cells[string.Format("I{0}", row)].Value = item.volume.ToString();
                    Sheet.Cells[string.Format("J{0}", row)].Value = item.uoM.ToString();
                    Sheet.Cells[string.Format("K{0}", row)].Value = item.lubricantType.ToString();
                    Sheet.Cells[string.Format("L{0}", row)].Value = item.isSOS.ToString();

                    row++;
                }
                #endregion

                var stream = new MemoryStream(ep.GetAsByteArray());
                return stream.ToArray();
            }
        }
        public async Task<dynamic> UploadInterimLubricantMapping(IFormFile files)
        {
            MappingRecommendedInterimLubricant dataMapping = new MappingRecommendedInterimLubricant();
            dataMapping.detail = new List<DetailMappingLubricant>();

            using (var excel = files.OpenReadStream())
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage(excel);

                ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                var rowCount = worksheet.Dimension.Rows;

                dataMapping.modelId = worksheet.Cells[2, 2].Value.ToString();
                dataMapping.site = worksheet.Cells[3, 2].Value.ToString();
                dataMapping.eformType = worksheet.Cells[4, 2].Value.ToString();

                Dictionary<string, object> paramMapping = new Dictionary<string, object>();
                paramMapping.Add("modelId", dataMapping.modelId);
                paramMapping.Add("site", dataMapping.site);
                paramMapping.Add("eformType", dataMapping.eformType);

                var resultMapping = await _repository.GetDataByParam(paramMapping);

                if (resultMapping == null)
                {
                    for (int row = 7; row <= rowCount; row++)
                    {
                        DetailMappingLubricant detailMapping = new DetailMappingLubricant();
                        detailMapping.key = worksheet.Cells[row, 1].Value == null ? string.Empty : worksheet.Cells[row, 1].Value.ToString();
                        detailMapping.taskKeyOilSample = worksheet.Cells[row, 2].Value == null ? string.Empty : worksheet.Cells[row, 2].Value.ToString();
                        detailMapping.taskKeyOilChange = worksheet.Cells[row, 3].Value == null ? string.Empty : worksheet.Cells[row, 3].Value.ToString();
                        detailMapping.taskKeyOilLevelCheck = worksheet.Cells[row, 4].Value == null ? string.Empty : worksheet.Cells[row, 4].Value.ToString();
                        detailMapping.taskTopUpLevelCheck = worksheet.Cells[row, 5].Value == null ? string.Empty : worksheet.Cells[row, 5].Value.ToString();
                        detailMapping.compartmentLubricant = worksheet.Cells[row, 6].Value == null ? string.Empty : worksheet.Cells[row, 6].Value.ToString();
                        detailMapping.compartmentCode = worksheet.Cells[row, 7].Value == null ? string.Empty : worksheet.Cells[row, 7].Value.ToString();
                        detailMapping.recommendedLubricant = worksheet.Cells[row, 8].Value == null ? string.Empty : worksheet.Cells[row, 8].Value.ToString();
                        detailMapping.volume = worksheet.Cells[row, 9].Value == null ? string.Empty : worksheet.Cells[row, 9].Value.ToString();
                        detailMapping.uoM = worksheet.Cells[row, 10].Value == null ? string.Empty : worksheet.Cells[row, 10].Value.ToString();
                        detailMapping.lubricantType = worksheet.Cells[row, 11].Value == null ? string.Empty : worksheet.Cells[row, 11].Value.ToString();
                        detailMapping.isSOS = worksheet.Cells[row, 12].Value == null ? string.Empty : worksheet.Cells[row, 12].Value.ToString();

                        dataMapping.detail.Add(detailMapping);
                    }

                    CreateRequest createRequest = new CreateRequest()
                    {
                        entity = dataMapping,
                        employee = new EmployeeModel() { id = "SYSTEM", name = "SYSTEM" }
                    };

                    await _repository.Create(createRequest);
                }
                else
                {
                    for (int row = 7; row <= rowCount; row++)
                    {
                        DetailMappingLubricant detailMapping = new DetailMappingLubricant();
                        detailMapping.key = worksheet.Cells[row, 1].Value == null ? string.Empty : worksheet.Cells[row, 1].Value.ToString();
                        detailMapping.taskKeyOilSample = worksheet.Cells[row, 2].Value == null ? string.Empty : worksheet.Cells[row, 2].Value.ToString();
                        detailMapping.taskKeyOilChange = worksheet.Cells[row, 3].Value == null ? string.Empty : worksheet.Cells[row, 3].Value.ToString();
                        detailMapping.taskKeyOilLevelCheck = worksheet.Cells[row, 4].Value == null ? string.Empty : worksheet.Cells[row, 4].Value.ToString();
                        detailMapping.taskTopUpLevelCheck = worksheet.Cells[row, 5].Value == null ? string.Empty : worksheet.Cells[row, 5].Value.ToString();
                        detailMapping.compartmentLubricant = worksheet.Cells[row, 6].Value == null ? string.Empty : worksheet.Cells[row, 6].Value.ToString();
                        detailMapping.compartmentCode = worksheet.Cells[row, 7].Value == null ? string.Empty : worksheet.Cells[row, 7].Value.ToString();
                        detailMapping.recommendedLubricant = worksheet.Cells[row, 8].Value == null ? string.Empty : worksheet.Cells[row, 8].Value.ToString();
                        detailMapping.volume = worksheet.Cells[row, 9].Value == null ? string.Empty : worksheet.Cells[row, 9].Value.ToString();
                        detailMapping.uoM = worksheet.Cells[row, 10].Value == null ? string.Empty : worksheet.Cells[row, 10].Value.ToString();
                        detailMapping.lubricantType = worksheet.Cells[row, 11].Value == null ? string.Empty : worksheet.Cells[row, 11].Value.ToString();
                        detailMapping.isSOS = worksheet.Cells[row, 12].Value == null ? string.Empty : worksheet.Cells[row, 12].Value.ToString();

                        dataMapping.detail.Add(detailMapping);
                    }

                    List<PropertyParam> propertyParams = new List<PropertyParam>() {
                        new PropertyParam()
                        {
                            propertyName = EnumQuery.Detail,
                            propertyValue = JsonConvert.SerializeObject(dataMapping.detail)
                        }
                    };

                    UpdateRequest updateDataParams = new UpdateRequest();
                    //updateDataParams.id = worksheet.Cells[1, 2].Value.ToString();
                    updateDataParams.id = resultMapping.id.ToString();
                    updateDataParams.workOrder = "";
                    updateDataParams.updateParams = new List<UpdateParam>();
                    updateDataParams.employee = new EmployeeModel { id = "SYSTEM", name = "SYSTEM" };

                    updateDataParams.updateParams.Add(new UpdateParam()
                    {
                        keyValue = "MAPPING",
                        propertyParams = propertyParams
                    });

                    var resultUpdateHeader = await _repository.Update(updateDataParams, dataMapping);
                }
            }

            return new ServiceResult
            {
                Message = "Data insert successfully",
                IsError = false,
                Content = dataMapping
            };
        }

        public async Task<ServiceResult> ValidateData(InterimLubricantMappingRequest model)
        {
            var _repoServiceSheet = new LubricantMappingRepository(_connectionFactory, EnumContainer.LubricantMapping);

            Dictionary<string, object> paramLubcricant = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(model.modelId))
                paramLubcricant.Add("modelId", model.modelId);

            if (!string.IsNullOrEmpty(model.siteId))
                paramLubcricant.Add("site", model.siteId);

            if (!string.IsNullOrEmpty(model.eformType))
                paramLubcricant.Add("eformType", model.eformType);

            paramLubcricant.Add("isDeleted", "false");

            var result = await _repoServiceSheet.GetDataByParam(paramLubcricant);

            if (result == null)
            {
                return new ServiceResult
                {
                    Message = "Data Not Found",
                    IsError = true,
                    Content = null
                };
            }

            return new ServiceResult
            {
                Message = "Data Valid",
                IsError = false,
                Content = null
            };
        }
        #endregion
    }
}
