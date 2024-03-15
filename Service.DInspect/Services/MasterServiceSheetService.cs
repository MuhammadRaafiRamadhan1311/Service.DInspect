using ClosedXML.Excel;
using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Office2019.Drawing.Model3D;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Service.DInspect.Helpers;
using Service.DInspect.Interfaces;
using Service.DInspect.Models;
using Service.DInspect.Models.ADM;
using Service.DInspect.Models.Enum;
using Service.DInspect.Models.Helper;
using Service.DInspect.Models.Request;
using Service.DInspect.Models.Response;
using Service.DInspect.Models.Entity;
using Service.DInspect.Models.Helper;
using Service.DInspect.Repositories;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Service.DInspect.Services
{
    public class MasterServiceSheetService : ServiceBase
    {
        protected IConnectionFactory _connectionFactory;
        private IRepositoryBase _repositoryGenerateJson;
        private IRepositoryBase _repositoryGenerateJsonType;
        private IRepositoryBase _repositoryTemp;
        private IRepositoryBase _repositoryGenerateConfigModel;
        private string _container;

        public MasterServiceSheetService(MySetting appSetting, IConnectionFactory connectionFactory, string container, string accessToken) : base(appSetting, connectionFactory, container, accessToken)
        {
            _container = container;
            _connectionFactory = connectionFactory;
            _repository = new MasterServiceSheetRepository(connectionFactory, container);
            _repositoryTemp = new TempMasterServiceSheetRepository(connectionFactory, EnumContainer.TempMasterServiceSheet);
            _repositoryGenerateJson = new GenerateJsonRepository(connectionFactory, EnumContainer.GenerateJson);
            _repositoryGenerateJsonType = new GenerateJsonTypeRepository(connectionFactory, EnumContainer.GenerateJsonType);
            _repositoryGenerateConfigModel = new GenerateConfigModelRepository(connectionFactory, EnumContainer.GenerateConfigModel);
        }

        public async Task<byte[]> ExportToParameter(string modelId, string psTypeId)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var ep = new ExcelPackage())
            {
                #region Header
                ExcelWorksheet Sheet = ep.Workbook.Worksheets.Add("Parameter");


                Sheet.Cells["A1"].Value = "ID CBM Parameter";
                Sheet.Cells["B1"].Value = "ID Component";
                Sheet.Cells["C1"].Value = "ID Type Parameter";
                Sheet.Cells["D1"].Value = "CBM Group";
                Sheet.Cells["E1"].Value = "Area CBM";
                Sheet.Cells["F1"].Value = "Model";
                Sheet.Cells["G1"].Value = "PS Type";
                Sheet.Cells["H1"].Value = "Task Number";
                Sheet.Cells["I1"].Value = "No Detail";
                Sheet.Cells["J1"].Value = "CBM Parameter";
                Sheet.Cells["K1"].Value = "Parameter";
                Sheet.Cells["L1"].Value = "Value Min";
                Sheet.Cells["M1"].Value = "Value Max";
                Sheet.Cells["N1"].Value = "UOM";
                Sheet.Cells["O1"].Value = "Status Converter";
                Sheet.Cells["P1"].Value = "Status Converter Description";
                Sheet.Cells["Q1"].Value = "Status";
                Sheet.Cells["R1"].Value = "Status Description";
                Sheet.Cells["S1"].Value = "Start Date";
                Sheet.Cells["T1"].Value = "End Date";
                Sheet.Cells["U1"].Value = "Task Key";
                #endregion


                #region Content
                var _repoServiceSheet = new MasterServiceSheetRepository(_connectionFactory, EnumContainer.MasterServiceSheet);
                var result = await _repoServiceSheet.GetDataForParameter(modelId, psTypeId);

                int row = 2;

                CallAPIHelper callAPI = new CallAPIHelper(_accessToken);
                ApiResponse response = await callAPI.Get($"{EnumUrl.GetStatusConverter}");
                List<StatusConverterModel> statusConverter = JsonConvert.DeserializeObject<List<StatusConverterModel>>(JsonConvert.SerializeObject(response.Result.Content));

                foreach (var data in result)
                {
                    MasterServiceSheetParameterResponse item = JsonConvert.DeserializeObject<MasterServiceSheetParameterResponse>(JsonConvert.SerializeObject(data));

                    if (item.rating == EnumRatingValue.Calibration)
                    {
                        for (int i = 5; i <= 6; i++)
                        {
                            Sheet.Cells[string.Format("F{0}", row)].Value = item.modelId.Trim();
                            Sheet.Cells[string.Format("G{0}", row)].Value = $"{item.psTypeId} hrs".Trim();
                            Sheet.Cells[string.Format("H{0}", row)].Value = Regex.Replace(item.value, "[a-zA-Z_-]", "").Trim();
                            Sheet.Cells[string.Format("I{0}", row)].Value = Regex.Replace(item.value, "[0-9]", "").Trim();
                            Sheet.Cells[string.Format("O{0}", row)].Value = statusConverter.Where(x => x.StatusConverterId == i).Select(x => x.StatusConverter).FirstOrDefault().ToString().Trim();
                            Sheet.Cells[string.Format("P{0}", row)].Value = statusConverter.Where(x => x.StatusConverterId == i).Select(x => x.StatusConverterDescription).FirstOrDefault().ToString().Trim();
                            Sheet.Cells[string.Format("Q{0}", row)].Value = statusConverter.Where(x => x.StatusConverterId == i).Select(x => x.StatusConverter).FirstOrDefault().ToString().Trim();
                            Sheet.Cells[string.Format("R{0}", row)].Value = statusConverter.Where(x => x.StatusConverterId == i).Select(x => x.StatusConverterDescription).FirstOrDefault().ToString().Trim();
                            Sheet.Cells[string.Format("U{0}", row)].Value = item.key.Trim();
                            row++;
                        }
                    }
                    else //automatic 
                    {
                        for (int i = 1; i <= 4; i++)
                        {
                            Sheet.Cells[string.Format("F{0}", row)].Value = item.modelId.Trim();
                            Sheet.Cells[string.Format("G{0}", row)].Value = $"{item.psTypeId} hrs";
                            Sheet.Cells[string.Format("H{0}", row)].Value = Regex.Replace(item.value, "[a-zA-Z_-]", "").Trim();
                            Sheet.Cells[string.Format("I{0}", row)].Value = Regex.Replace(item.value, "[0-9]", "").Trim();
                            Sheet.Cells[string.Format("O{0}", row)].Value = statusConverter.Where(x => x.StatusConverterId == i).Select(x => x.StatusConverter).FirstOrDefault().ToString().Trim();
                            Sheet.Cells[string.Format("P{0}", row)].Value = statusConverter.Where(x => x.StatusConverterId == i).Select(x => x.StatusConverterDescription).FirstOrDefault().ToString().Trim();
                            Sheet.Cells[string.Format("Q{0}", row)].Value = statusConverter.Where(x => x.StatusConverterId == i).Select(x => x.StatusConverter).FirstOrDefault().ToString().Trim();
                            Sheet.Cells[string.Format("R{0}", row)].Value = statusConverter.Where(x => x.StatusConverterId == i).Select(x => x.StatusConverterDescription).FirstOrDefault().ToString().Trim();
                            Sheet.Cells[string.Format("U{0}", row)].Value = item.key.Trim();
                            row++;
                        }
                    }


                }
                #endregion
                row--;

                var stream = new MemoryStream(ep.GetAsByteArray());
                return stream.ToArray();
            }
        }

        public async Task<ServiceResult> GetDataCBM(dynamic param)
        {
            var _repoMasterCBM = new MasterServiceSheetRepository(_connectionFactory, EnumContainer.MasterServiceSheet);
            var result = await _repoMasterCBM.GetDataCBM(param);

            return new ServiceResult
            {
                Message = "Data updated successfully",
                IsError = false,
                Content = result
            };
        }

        public async Task<ServiceResult> GenerateTemplate(GenerateModelJson model)
        {
            // WO TESTING 9992703092

            ServiceResult result = new ServiceResult();
            dynamic resultHeaderCbmAutomatic = null;
            dynamic resultHeaderCbmAutomaticRoller = null;
            dynamic resultHeaderDefectGroup = null;
            Dictionary<string, object> dataJsonResult = new Dictionary<string, object>();
            List<dynamic> resultList = new List<dynamic>();

            List<dynamic> psType250 = new List<dynamic>();
            List<dynamic> psType500 = new List<dynamic>();
            List<dynamic> psType1000 = new List<dynamic>();
            List<dynamic> psType2000 = new List<dynamic>();
            List<dynamic> psType4000 = new List<dynamic>();

            using (var excel = model.file.OpenReadStream())
            {
                using var workBook = new XLWorkbook(excel);
                IXLWorksheet workSheet = workBook.Worksheets.Where(x => x.Name == model.workSheet).FirstOrDefault();

                string currentRow = model.startRow;
                string endRow = model.endRow;

                Regex digitsOnly = new Regex(@"[^\d]");
                Regex charOnly = new Regex(@"[a-zA-Z ]");

                // Preparation
                var dataWeworkSheetCount = workSheet.Range($"A{currentRow}:R{endRow}").RowCount();
                var dataWeworkSheet = workSheet.Range($"A{currentRow}:R{endRow}")
                    .CellsUsed()
                    .Select(c => new AddressMappingData
                    {
                        Row = Convert.ToInt32(digitsOnly.Replace(c.Address.ToString(), "")),
                        Value = c.Value.ToString(),
                        Address = c.Address.ToString()
                    }).ToList();

                // Model and Group
                var modelList = dataWeworkSheet.Where(x => x.Address.Contains("A")).Select(x => x.Value).Distinct().FirstOrDefault();
                var groupList = dataWeworkSheet.Where(x => x.Address.Contains("B")).Select(x => x.Value).Distinct().ToList();

                foreach (var groupName in groupList.Where(x => x == model.groupName))
                {
                    var listGenerateColumn = new List<GenerateColumnJson>();

                    for (int i = 0; i < dataWeworkSheetCount; i++)
                    {
                        var generateColumn = new GenerateColumnJson();
                        foreach (var row in dataWeworkSheet.Where(x => x.Row == i + Convert.ToInt32(currentRow)))
                        {
                            generateColumn.Row = row.Row;
                            generateColumn.Model = generateColumn.Model != null ? generateColumn.Model : row.Address.Contains("A") ? row.Value.Trim() : null;
                            generateColumn.GroupName = generateColumn.GroupName != null ? generateColumn.GroupName : row.Address.Contains("B") ? row.Value.Trim() : null;
                            generateColumn.Section = generateColumn.Section != null ? generateColumn.Section : row.Address.Contains("C") ? row.Value.Trim() : null;
                            generateColumn.Description = generateColumn.Description != null ? generateColumn.Description : row.Address.Contains("D") ? row.Value.Trim() : null;
                            generateColumn.Category = generateColumn.Category != null ? generateColumn.Category : row.Address.Contains("E") ? row.Value.Trim() : null;
                            generateColumn.Number250 = generateColumn.Number250 != null ? generateColumn.Number250 : row.Address.Contains("F") ? row.Value.Trim() : null;
                            generateColumn.Number500 = generateColumn.Number500 != null ? generateColumn.Number500 : row.Address.Contains("G") ? row.Value.Trim() : null;
                            generateColumn.Number1000 = generateColumn.Number1000 != null ? generateColumn.Number1000 : row.Address.Contains("H") ? row.Value.Trim() : null;
                            generateColumn.Number2000 = generateColumn.Number2000 != null ? generateColumn.Number2000 : row.Address.Contains("I") ? row.Value.Trim() : null;

                            if (generateColumn.Number4000 != null)
                            {
                                generateColumn.Number4000 = generateColumn.Number4000 != null ? generateColumn.Number4000 : row.Address.Contains("J") ? row.Value.Trim() : null;
                            }
                            else
                            {
                                generateColumn.Number4000 = generateColumn.Number2000 != null ? generateColumn.Number2000 : row.Address.Contains("J") ? row.Value.Trim() : null;
                            }

                            //generateColumn.Number4000 = generateColumn.Number4000 != null ? generateColumn.Number4000 : row.Address.Contains("J") ? row.Value.Trim() : null;
                            //generateColumn.Number4000 = generateColumn.Number4000 != null ? generateColumn.Number4000 : row.Address.Contains("J") ? row.Value.Trim() : null;

                            generateColumn.GuidTable = generateColumn.GuidTable != null ? generateColumn.GuidTable : row.Address.Contains("K") ? row.Value.Trim() : null;
                            generateColumn.ImageData = generateColumn.ImageData != null ? generateColumn.ImageData : row.Address.Contains("L") ? row.Value.Trim() : null;
                            generateColumn.Table = generateColumn.Table != null ? generateColumn.Table : row.Address.Contains("M") ? row.Value.Trim() : null;
                            generateColumn.ServiceMappingValue = generateColumn.ServiceMappingValue != null ? generateColumn.ServiceMappingValue : row.Address.Contains("N") ? row.Value.Trim() : null;
                            generateColumn.SOS = generateColumn.SOS != null ? generateColumn.SOS : row.Address.Contains("O") ? row.Value.Trim() : null;
                            generateColumn.SectionColumn = generateColumn.SectionColumn != null ? generateColumn.SectionColumn : row.Address.Contains("P") ? row.Value.Trim() : null;
                            generateColumn.TaskKey = generateColumn.TaskKey != null ? generateColumn.TaskKey : row.Address.Contains("Q") ? row.Value.Trim() : null;
                            generateColumn.GroupTaskId = generateColumn.GroupTaskId != null ? generateColumn.GroupTaskId : row.Address.Contains("R") ? row.Value.Trim() : null;
                        }

                        listGenerateColumn.Add(generateColumn);
                    }

                    #region Group Task Id
                    // CBM Populate Data
                    var dataColumnOrder = listGenerateColumn.Where(x => (x.Category == EnumCategoryServiceSheet.CBM ||
                                                                         x.Category == EnumCategoryServiceSheet.CBM_MANUAL ||
                                                                         x.Category == EnumCategoryServiceSheet.CBM_ADJUSTMENT ||
                                                                         x.Category == EnumCategoryServiceSheet.CBM_ADJUSTMENT_CARRY_ROLLER ||
                                                                         x.Category == EnumCategoryServiceSheet.CBM_NORMAL ||
                                                                         x.Category == EnumCategoryServiceSheet.CBM_CALCULATE ||
                                                                         x.Category == EnumCategoryServiceSheet.CBM_CALCULATE_RESULT ||
                                                                         x.Category == EnumCategoryServiceSheet.CBM_MANUAL_DOUBLE ||
                                                                         x.Category == EnumCategoryServiceSheet.CBM_CALIBRATION ||
                                                                         x.Category == EnumCategoryServiceSheet.CBM_BRAKE ||
                                                                         x.Category == EnumCategoryServiceSheet.DEFECT_MEASUREMENT) &&
                                                                        (x.Number4000.ToLower().Contains("a") ||
                                                                         x.Number4000.ToLower().Contains("a1") ||
                                                                         x.Number4000.ToLower().Contains("b1") ||
                                                                         x.Number4000.Contains("c1") ||
                                                                         x.Number4000.Contains("d1"))).ToList();
                    dataColumnOrder.ForEach(c => c.FirstRow = true);
                    //dataColumnOrder.ForEach(c => c.GroupTaskId = Guid.NewGuid().ToString());

                    // NORMAL Populate Data
                    var dataColumnOrderNormal = listGenerateColumn.Where(x => (x.Category == EnumCategoryServiceSheet.Defect || x.Category == EnumCategoryServiceSheet.Service) &&
                                                                              x.Number4000.Contains("a")).ToList();
                    //dataColumnOrderNormal.ForEach(c => c.GroupTaskId = Guid.NewGuid().ToString());

                    // CRACK Populate Data
                    var dataColumnOrderCrack = listGenerateColumn.Where(x => (x.Category == EnumCategoryServiceSheet.Crack || x.Category == EnumCategoryServiceSheet.CRACK_SUBTASK) &&
                                                                             (x.Number4000.ToLower().Contains("a") ||
                                                                              x.Number4000.ToLower().Contains("a1") ||
                                                                              x.Number4000.ToLower().Contains("b1") ||
                                                                              x.Number4000.ToLower().Contains("c1") ||
                                                                              x.Number4000.ToLower().Contains("d1") ||
                                                                              x.Number4000.ToLower().Contains("e1") ||
                                                                              x.Number4000.ToLower().Contains("f1") ||
                                                                              x.Number4000.ToLower().Contains("g1") ||
                                                                              x.Number4000.ToLower().Contains("h1") ||
                                                                              x.Number4000.ToLower().Contains("i1") ||
                                                                              x.Number4000.ToLower().Contains("j1") ||
                                                                              x.Number4000.ToLower().Contains("k1") ||
                                                                              x.Number4000.ToLower().Contains("l1") ||
                                                                              x.Number4000.ToLower().Contains("m1") ||
                                                                              x.Number4000.ToLower().Contains("n1"))).ToList();

                    dataColumnOrderCrack = listGenerateColumn.Where(x => x.Category == EnumCategoryServiceSheet.Crack &&
                                                                             (x.Number4000.ToLower().Contains("a1") ||
                                                                              x.Number4000.ToLower().Contains("b1") ||
                                                                              x.Number4000.ToLower().Contains("c1") ||
                                                                              x.Number4000.ToLower().Contains("d1") ||
                                                                              x.Number4000.ToLower().Contains("e1") ||
                                                                              x.Number4000.ToLower().Contains("f1") ||
                                                                              x.Number4000.ToLower().Contains("g1") ||
                                                                              x.Number4000.ToLower().Contains("h1") ||
                                                                              x.Number4000.ToLower().Contains("i1") ||
                                                                              x.Number4000.ToLower().Contains("j1") ||
                                                                              x.Number4000.ToLower().Contains("k1") ||
                                                                              x.Number4000.ToLower().Contains("l1") ||
                                                                              x.Number4000.ToLower().Contains("m1") ||
                                                                              x.Number4000.ToLower().Contains("n1"))).ToList();

                    dataColumnOrderCrack.ForEach(c => c.FirstRow = true);
                    //dataColumnOrderCrack.ForEach(c => c.GroupTaskId = Guid.NewGuid().ToString());

                    // Assigment GroupTaskId per Group CBM
                    foreach (var itemGroupTask in dataColumnOrder)
                    {
                        var input = itemGroupTask.Number4000;

                        //if (input.Length == 3)
                        //{
                        //    var dataColum = listGenerateColumn.Where(x => !string.IsNullOrEmpty(x.Number4000) &&
                        //                                                  x.Number4000.Length == 3 &&
                        //                                                  digitsOnly.Replace(x.Number4000, "").Substring(0, 2) == digitsOnly.Replace(itemGroupTask.Number4000, "").Substring(0, 2)).ToList();
                        //    dataColum.ForEach(c => c.GroupTaskId = itemGroupTask.GroupTaskId);
                        //}
                        if (input.Length == 4)
                        {
                            var dataColum = listGenerateColumn.Where(x => !string.IsNullOrEmpty(x.Number4000) &&
                                                                          x.Number4000.Length == 4 &&
                                                                          digitsOnly.Replace(x.Number4000, "").Substring(0, 2) == digitsOnly.Replace(itemGroupTask.Number4000, "").Substring(0, 2)).ToList();
                            //dataColum.ForEach(c => c.GroupTaskId = itemGroupTask.GroupTaskId);
                        }
                        else if (input.Length == 5)
                        {
                            var dataColum = listGenerateColumn.Where(x => !string.IsNullOrEmpty(x.Number4000) &&
                                                                          x.Number4000.Length == 5 &&
                                                                          digitsOnly.Replace(x.Number4000, "").Substring(0, 3) == digitsOnly.Replace(itemGroupTask.Number4000, "").Substring(0, 3)).ToList();
                            //dataColum.ForEach(c => c.GroupTaskId = itemGroupTask.GroupTaskId);
                        }
                        else
                        {
                            var dataColum = listGenerateColumn.Where(x => !string.IsNullOrEmpty(x.Number4000) &&
                                                                          digitsOnly.Replace(x.Number4000, "") == digitsOnly.Replace(itemGroupTask.Number4000, "")).ToList();
                            //dataColum.ForEach(c => c.GroupTaskId = itemGroupTask.GroupTaskId);
                        }
                    }

                    // Set Last Row CBM
                    var dataColumnOrderLast = listGenerateColumn.Where(x => x.Category == EnumCategoryServiceSheet.CBM ||
                                                                            x.Category == EnumCategoryServiceSheet.CBM_MANUAL ||
                                                                            x.Category == EnumCategoryServiceSheet.CBM_ADJUSTMENT ||
                                                                            x.Category == EnumCategoryServiceSheet.CBM_ADJUSTMENT_CARRY_ROLLER ||
                                                                            x.Category == EnumCategoryServiceSheet.CBM_NORMAL ||
                                                                            //x.Category == EnumCategoryServiceSheet.CBM_CALCULATE ||
                                                                            x.Category == EnumCategoryServiceSheet.CBM_CALCULATE_RESULT ||
                                                                            x.Category == EnumCategoryServiceSheet.CBM_MANUAL_DOUBLE ||
                                                                            x.Category == EnumCategoryServiceSheet.CBM_CALIBRATION ||
                                                                            x.Category == EnumCategoryServiceSheet.CBM_BRAKE ||
                                                                            x.Category == EnumCategoryServiceSheet.DEFECT_MEASUREMENT).OrderByDescending(x => x.Number4000).GroupBy(x => x.GroupTaskId).ToList();
                    foreach (var itemLast in dataColumnOrderLast)
                    {
                        var dataLastRow = itemLast.FirstOrDefault();
                        dataLastRow.LastRow = true;
                    }

                    // Assigment GroupTaskId per Group NORMAL
                    foreach (var itemGroupTaskNormal in dataColumnOrderNormal)
                    {
                        var input = itemGroupTaskNormal.Number4000;

                        if (input.Length == 3)
                        {
                            var dataColum = listGenerateColumn.Where(x => !string.IsNullOrEmpty(x.Number4000) &&
                                                                          x.Number4000.Length == 3 &&
                                                                          digitsOnly.Replace(x.Number4000, "").Substring(0, 2) == digitsOnly.Replace(itemGroupTaskNormal.Number4000, "").Substring(0, 2)).ToList();
                            //dataColum.ForEach(c => c.GroupTaskId = itemGroupTaskNormal.GroupTaskId);
                        }
                        else
                        {
                            var dataColum = listGenerateColumn.Where(x => !string.IsNullOrEmpty(x.Number4000) &&
                                                                      digitsOnly.Replace(x.Number4000, "") == digitsOnly.Replace(itemGroupTaskNormal.Number4000, "")).ToList();
                            //dataColum.ForEach(c => c.GroupTaskId = itemGroupTaskNormal.GroupTaskId);
                        }
                    }

                    // Assigment GroupTaskId per Group CRACK
                    foreach (var itemGroupTaskCrack in dataColumnOrderCrack)
                    {
                        if (itemGroupTaskCrack.Number4000.Length == 4)
                        {
                            var dataColum = listGenerateColumn.Where(x => !string.IsNullOrEmpty(x.Number4000) &&
                                                                     (x.Category == EnumCategoryServiceSheet.Crack || x.Category == EnumCategoryServiceSheet.CRACK_SUBTASK) && digitsOnly.Replace(x.Number4000, "").Substring(0, 3) == digitsOnly.Replace(itemGroupTaskCrack.Number4000, "").Substring(0, 3)).ToList();
                            //dataColum.ForEach(c => c.GroupTaskId = itemGroupTaskCrack.GroupTaskId);
                        }
                        else if (itemGroupTaskCrack.Number4000.Length == 5)
                        {
                            var dataColum = listGenerateColumn.Where(x => !string.IsNullOrEmpty(x.Number4000) &&
                                                                     (x.Category == EnumCategoryServiceSheet.Crack || x.Category == EnumCategoryServiceSheet.CRACK_SUBTASK) && digitsOnly.Replace(x.Number4000, "").Substring(0, 3) == digitsOnly.Replace(itemGroupTaskCrack.Number4000, "").Substring(0, 3)).ToList();
                            //dataColum.ForEach(c => c.GroupTaskId = itemGroupTaskCrack.GroupTaskId);
                        }
                        else
                        {
                            var dataColum = listGenerateColumn.Where(x => !string.IsNullOrEmpty(x.Number4000) &&
                                                                     (x.Category == EnumCategoryServiceSheet.Crack || x.Category == EnumCategoryServiceSheet.CRACK_SUBTASK) && digitsOnly.Replace(x.Number4000, "").Substring(0, 2) == digitsOnly.Replace(itemGroupTaskCrack.Number4000, "").Substring(0, 2)).ToList();
                            //dataColum.ForEach(c => c.GroupTaskId = itemGroupTaskCrack.GroupTaskId);
                        }
                    }

                    // Set Last Row CRACK
                    var dataColumnOrderCrackLast = listGenerateColumn.Where(x => x.Category == EnumCategoryServiceSheet.Crack || x.Category == EnumCategoryServiceSheet.CRACK_SUBTASK).OrderByDescending(x => x.Number4000).GroupBy(x => x.GroupTaskId).ToList();
                    foreach (var itemLast in dataColumnOrderCrackLast)
                    {
                        var dataLastRow = itemLast.FirstOrDefault();
                        dataLastRow.LastRow = true;
                    }

                    // NORMAL GROUP Populate Data
                    var dataColumnOrderNormalGroup = listGenerateColumn.Where(x => (x.Category == EnumCategoryServiceSheet.Defect_Group ||
                                                                                    x.Category == EnumCategoryServiceSheet.Service_With_Header) &&
                                                                                    x.Number4000.Contains("a")).ToList();
                    dataColumnOrderNormalGroup.ForEach(c => c.FirstRow = true);
                    //dataColumnOrderNormalGroup.ForEach(c => c.GroupTaskId = Guid.NewGuid().ToString());

                    // Assigment GroupTaskId per Defect Group
                    foreach (var itemGroupTask in dataColumnOrderNormalGroup)
                    {
                        var dataColum = listGenerateColumn.Where(x => !string.IsNullOrEmpty(x.Number4000) && digitsOnly.Replace(x.Number4000, "") == digitsOnly.Replace(itemGroupTask.Number4000, "")).ToList();
                        //dataColum.ForEach(c => c.GroupTaskId = itemGroupTask.GroupTaskId);
                    }

                    var dataColumnOrderNormalGroupLast = listGenerateColumn.Where(x => x.Category == EnumCategoryServiceSheet.Defect_Group ||
                                                                                        x.Category == EnumCategoryServiceSheet.Service_With_Header).OrderByDescending(x => x.Number4000).GroupBy(x => x.GroupTaskId).ToList();
                    foreach (var itemLast in dataColumnOrderNormalGroupLast)
                    {
                        var dataLastRow = itemLast.FirstOrDefault();
                        dataLastRow.LastRow = true;
                    }
                    #endregion

                    #region Get Config and Set Value Based on Excel File
                    foreach (var itemColumn in listGenerateColumn)
                    {
                        string category = string.Empty;
                        string rating = string.Empty;

                        switch (itemColumn.Category)
                        {
                            case "Section":
                                category = EnumCategoryServiceSheet.NORMAL;

                                if (itemColumn.SectionColumn == "4") { rating = EnumRatingServiceSheet.SECTION_COLUMN_4; }
                                else if (itemColumn.SectionColumn == "5") { rating = EnumRatingServiceSheet.SECTION_COLUMN_5; }
                                else if (itemColumn.SectionColumn == "6") { rating = EnumRatingServiceSheet.SECTION_COLUMN_6; }
                                else { rating = EnumRatingServiceSheet.SECTION; }

                                break;
                            case "Defect":
                                category = EnumCategoryServiceSheet.NORMAL;
                                rating = EnumRatingServiceSheet.NO;
                                break;
                            case "Defect_Group":
                                category = EnumCategoryServiceSheet.NORMAL;
                                rating = EnumRatingServiceSheet.NO_GROUPING;

                                if (itemColumn.FirstRow)
                                {
                                    var paramCbmHeader = new Dictionary<string, object>();
                                    paramCbmHeader.Add("category", category);
                                    paramCbmHeader.Add("rating", EnumRatingServiceSheet.NO_GROUP_HEADER);

                                    resultHeaderDefectGroup = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);
                                }
                                else
                                {
                                    resultHeaderDefectGroup = null;
                                }

                                break;
                            case "DEFECT_MEASUREMENT":
                                category = EnumCategoryServiceSheet.NORMAL;
                                rating = EnumRatingServiceSheet.DEFECT_MEASUREMENT;

                                if (itemColumn.FirstRow)
                                {
                                    var paramCbmHeader = new Dictionary<string, object>();
                                    paramCbmHeader.Add("category", category);
                                    paramCbmHeader.Add("rating", EnumRatingServiceSheet.DEFECT_MEASUREMENT_HEADER);

                                    resultHeaderDefectGroup = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);
                                }
                                else
                                {
                                    resultHeaderDefectGroup = null;
                                }

                                break;
                            case "CBM":
                                category = EnumCategoryServiceSheet.CBM;
                                rating = EnumRatingServiceSheet.AUTOMATIC;

                                if (itemColumn.FirstRow)
                                {
                                    var paramCbmHeader = new Dictionary<string, object>();
                                    paramCbmHeader.Add("category", category);
                                    paramCbmHeader.Add("rating", EnumRatingServiceSheet.AUTOMATIC_HEADER);

                                    resultHeaderCbmAutomatic = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);
                                }
                                else
                                {
                                    resultHeaderCbmAutomatic = null;
                                }

                                break;
                            case "CBM_MANUAL":
                                category = EnumCategoryServiceSheet.CBM;
                                rating = EnumRatingServiceSheet.MANUAL;

                                if (itemColumn.FirstRow)
                                {
                                    var paramCbmHeader = new Dictionary<string, object>();
                                    paramCbmHeader.Add("category", category);
                                    paramCbmHeader.Add("rating", EnumRatingServiceSheet.MANUAL_HEADER);

                                    resultHeaderCbmAutomatic = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);
                                }
                                else
                                {
                                    resultHeaderCbmAutomatic = null;
                                }

                                break;
                            case "CBM_MANUAL_DOUBLE":
                                category = EnumCategoryServiceSheet.CBM;
                                rating = EnumRatingServiceSheet.MANUAL_DOUBLE;

                                if (itemColumn.FirstRow)
                                {
                                    var paramCbmHeader = new Dictionary<string, object>();
                                    paramCbmHeader.Add("category", category);
                                    paramCbmHeader.Add("rating", EnumRatingServiceSheet.MANUAL_DOUBLE_HEADER);

                                    resultHeaderCbmAutomatic = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);
                                }
                                else
                                {
                                    resultHeaderCbmAutomatic = null;
                                }

                                break;
                            case "CBM_ADJUSTMENT":
                                category = EnumCategoryServiceSheet.CBM;
                                rating = EnumRatingServiceSheet.AUTOMATIC_ADJUSTMENT;

                                if (itemColumn.FirstRow)
                                {
                                    var paramCbmHeader = new Dictionary<string, object>();
                                    paramCbmHeader.Add("category", category);
                                    paramCbmHeader.Add("rating", EnumRatingServiceSheet.AUTOMATIC_HEADER);

                                    resultHeaderCbmAutomatic = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);
                                }
                                else
                                {
                                    resultHeaderCbmAutomatic = null;
                                }

                                break;
                            case "CBM_ADJUSTMENT_CARRY_ROLLER":
                                category = EnumCategoryServiceSheet.CBM;
                                rating = EnumRatingServiceSheet.AUTOMATIC_ADJUSTMENT_CARRY_ROLLER;

                                if (itemColumn.FirstRow)
                                {
                                    var paramCbmHeader = new Dictionary<string, object>();
                                    paramCbmHeader.Add("category", category);
                                    paramCbmHeader.Add("rating", EnumRatingServiceSheet.AUTOMATIC_HEADER);

                                    resultHeaderCbmAutomatic = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);


                                    var paramCbmHeaderRoller = new Dictionary<string, object>();
                                    paramCbmHeaderRoller.Add("category", category);
                                    paramCbmHeaderRoller.Add("rating", EnumRatingServiceSheet.AUTOMATIC_HEADER_CARRY_ROLLER);

                                    resultHeaderCbmAutomaticRoller = await _repositoryGenerateJson.GetDataByParam(paramCbmHeaderRoller);
                                }
                                else
                                {
                                    resultHeaderCbmAutomatic = null;
                                }

                                break;
                            case "CBM_NORMAL":
                                category = EnumCategoryServiceSheet.CBM;
                                rating = EnumRatingServiceSheet.NORMAL;

                                if (itemColumn.FirstRow)
                                {
                                    var paramCbmHeader = new Dictionary<string, object>();
                                    paramCbmHeader.Add("category", category);
                                    paramCbmHeader.Add("rating", EnumRatingServiceSheet.NORMAL_HEADER);

                                    resultHeaderCbmAutomatic = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);
                                }
                                else
                                {
                                    resultHeaderCbmAutomatic = null;
                                }

                                break;
                            case "CBM_CALCULATE":
                                category = EnumCategoryServiceSheet.CBM;
                                rating = EnumRatingServiceSheet.CALCULATE_AVG;

                                if (itemColumn.FirstRow)
                                {
                                    var paramCbmHeader = new Dictionary<string, object>();
                                    paramCbmHeader.Add("category", category);
                                    paramCbmHeader.Add("rating", EnumRatingServiceSheet.AUTOMATIC_HEADER);

                                    resultHeaderCbmAutomatic = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);
                                }
                                else
                                {
                                    resultHeaderCbmAutomatic = null;
                                }

                                break;
                            case "CBM_CALCULATE_RESULT":
                                category = EnumCategoryServiceSheet.CBM;
                                rating = EnumRatingServiceSheet.CALCULATE_AVG_RESULT;

                                break;
                            case "CBM_DESC":
                                category = EnumCategoryServiceSheet.NORMAL_DESC;
                                rating = EnumRatingServiceSheet.NO;
                                break;
                            case "CBM_CALIBRATION":
                                category = EnumCategoryServiceSheet.CBM;
                                rating = EnumRatingServiceSheet.CALIBRATION;

                                if (itemColumn.FirstRow)
                                {
                                    var paramCbmHeader = new Dictionary<string, object>();
                                    paramCbmHeader.Add("category", category);
                                    paramCbmHeader.Add("rating", EnumRatingServiceSheet.CALIBRATION_HEADER);

                                    resultHeaderCbmAutomatic = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);
                                }
                                else
                                {
                                    resultHeaderCbmAutomatic = null;
                                }

                                break;
                            case "CBM_BRAKE":
                                category = EnumCategoryServiceSheet.CBM;
                                rating = EnumRatingServiceSheet.BRAKE;

                                if (itemColumn.FirstRow)
                                {
                                    var paramCbmHeader = new Dictionary<string, object>();
                                    paramCbmHeader.Add("category", category);
                                    paramCbmHeader.Add("rating", EnumRatingServiceSheet.BRAKE_HEADER);

                                    resultHeaderCbmAutomatic = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);
                                }
                                else
                                {
                                    resultHeaderCbmAutomatic = null;
                                }

                                break;
                            case "NORMAL_DESC":
                                category = EnumCategoryServiceSheet.NORMAL_DESC;
                                rating = EnumRatingServiceSheet.NO;
                                break;
                            case "Service":
                                category = EnumCategoryServiceSheet.NORMAL_WITH_COMMENT;
                                rating = EnumRatingServiceSheet.NO;
                                break;
                            case "Service_Mapping":
                                category = EnumCategoryServiceSheet.NORMAL_WITH_MAPPING;
                                rating = EnumRatingServiceSheet.NO;
                                break;
                            case "Service_Input":
                                category = EnumCategoryServiceSheet.NORMAL_WITH_INPUT;
                                rating = EnumRatingServiceSheet.NO;
                                break;
                            case "Service_With_Header":
                                category = EnumCategoryServiceSheet.NORMAL_WITH_HEADER;
                                rating = EnumRatingServiceSheet.NO;

                                if (itemColumn.FirstRow)
                                {
                                    var paramCbmHeader = new Dictionary<string, object>();
                                    paramCbmHeader.Add("category", EnumCategoryServiceSheet.NORMAL_WITH_HEADER);
                                    paramCbmHeader.Add("rating", EnumRatingServiceSheet.HEADER);

                                    resultHeaderDefectGroup = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);
                                }
                                else
                                {
                                    resultHeaderDefectGroup = null;
                                }

                                break;
                            case "Crack":
                                category = EnumCategoryServiceSheet.CRACK;
                                rating = EnumRatingServiceSheet.BLANK;
                                break;
                            case "CRACK_NON_GROUP":
                                category = EnumCategoryServiceSheet.CRACK;
                                rating = EnumRatingServiceSheet.NON_GROUP;
                                break;
                            case "CRACK_SUBTASK":
                                category = EnumCategoryServiceSheet.CRACK;
                                rating = EnumRatingServiceSheet.SUBTASK;
                                break;
                            case "INFO_SECTION":
                                category = EnumCategoryServiceSheet.NOTE;
                                rating = EnumRatingServiceSheet.CRACK_SECTION;
                                break;
                            case "Note":
                                category = EnumCategoryServiceSheet.NOTE;
                                rating = EnumRatingServiceSheet.NORMAL;
                                break;
                            case "SKIP_PRESERVICE":
                                category = EnumCategoryServiceSheet.NOTE;
                                rating = EnumRatingServiceSheet.SKIP_PRESERVICE;
                                break;
                            case "NORMAL_INPUT":
                                category = "NORMAL";
                                rating = "INPUT";
                                break;
                            case "BORDER_DESC":
                                category = EnumCategoryServiceSheet.NORMAL_DESC_BORDER;
                                rating = EnumRatingServiceSheet.NO;
                                break;
                        }

                        var param = new Dictionary<string, object>();
                        param.Add("category", category);
                        param.Add("rating", rating);

                        var dataJson = await _repositoryGenerateJson.GetDataByParam(param);

                        if (dataJson != null)
                        {
                            dynamic res = dataJson.content;
                            dynamic resCbmHeader = resultHeaderCbmAutomatic == null ? null : resultHeaderCbmAutomatic.content;
                            dynamic resCbmHeaderRoller = resultHeaderCbmAutomaticRoller == null ? null : resultHeaderCbmAutomaticRoller.content;

                            if (resCbmHeader != null)
                            {
                                resCbmHeader.key = Guid.NewGuid().ToString();
                                resCbmHeader.description250 = $"{itemColumn.Number250};;{itemColumn.Description}";
                                resCbmHeader.description500 = $"{itemColumn.Number500};;{itemColumn.Description}";
                                resCbmHeader.description1000 = $"{itemColumn.Number1000};;{itemColumn.Description}";
                                resCbmHeader.description2000 = $"{itemColumn.Number2000};;{itemColumn.Description}";
                                resCbmHeader.description4000 = $"{itemColumn.Number4000};;{itemColumn.Description}";

                                foreach (var itemCbmHeader in resCbmHeader.items)
                                {
                                    itemCbmHeader.key = Guid.NewGuid().ToString();
                                }
                            }

                            if (resCbmHeaderRoller != null)
                            {
                                resCbmHeaderRoller.key = Guid.NewGuid().ToString();

                                foreach (var itemCbmHeaderRoller in resCbmHeaderRoller.items)
                                {
                                    itemCbmHeaderRoller.key = Guid.NewGuid().ToString();
                                }
                            }

                            dynamic resDefectGroupHeader = resultHeaderDefectGroup == null ? null : resultHeaderDefectGroup.content;

                            if (resDefectGroupHeader != null)
                            {
                                resDefectGroupHeader.SectionData = itemColumn.Section;
                                resDefectGroupHeader.key = Guid.NewGuid().ToString();
                                resDefectGroupHeader.description250 = $"{itemColumn.Number250};;{itemColumn.Description}";
                                resDefectGroupHeader.description500 = $"{itemColumn.Number500};;{itemColumn.Description}";
                                resDefectGroupHeader.description1000 = $"{itemColumn.Number1000};;{itemColumn.Description}";
                                resDefectGroupHeader.description2000 = $"{itemColumn.Number2000};;{itemColumn.Description}";
                                resDefectGroupHeader.description4000 = $"{itemColumn.Number4000};;{itemColumn.Description}";

                                foreach (var itemDefectGroupHeader in resDefectGroupHeader.items)
                                {
                                    itemDefectGroupHeader.key = Guid.NewGuid().ToString();
                                }
                            }

                            var detaiNumberData = charOnly.Matches(itemColumn.Number2000 == null ? string.Empty : itemColumn.Number2000).FirstOrDefault()?.Value;
                            var detailDescription = itemColumn.Description;

                            if (itemColumn.Description != null)
                            {
                                if (detailDescription.Contains("a1. ")) { detailDescription = detailDescription.Replace("a1. ", ""); }
                                else if (detailDescription.Contains("a2. ")) { detailDescription = detailDescription.Replace("a2. ", ""); }
                                else if (detailDescription.Contains("b1. ")) { detailDescription = detailDescription.Replace("b1. ", ""); }
                                else if (detailDescription.Contains("b2. ")) { detailDescription = detailDescription.Replace("b2. ", ""); }
                                else if (detailDescription.Contains("a. ")) { detailDescription = detailDescription.Replace("a. ", ""); }
                                else if (detailDescription.Contains("b. ")) { detailDescription = detailDescription.Replace("b. ", ""); }
                                else if (detailDescription.Contains("c. ")) { detailDescription = detailDescription.Replace("c. ", ""); }
                                else if (detailDescription.Contains("d. ")) { detailDescription = detailDescription.Replace("d. ", ""); }
                                else if (detailDescription.Contains("e. ")) { detailDescription = detailDescription.Replace("e. ", ""); }
                                else if (detailDescription.Contains("f. ")) { detailDescription = detailDescription.Replace("f. ", ""); }
                                else if (detailDescription.Contains("g. ")) { detailDescription = detailDescription.Replace("g. ", ""); }
                                else if (detailDescription.Contains("h. ")) { detailDescription = detailDescription.Replace("h. ", ""); }
                                else if (detailDescription.Contains("i. ")) { detailDescription = detailDescription.Replace("i. ", ""); }
                                else if (detailDescription.Contains("j. ")) { detailDescription = detailDescription.Replace("j. ", ""); }
                                else if (detailDescription.Contains("k. ")) { detailDescription = detailDescription.Replace("k. ", ""); }
                                else if (detailDescription.Contains("l. ")) { detailDescription = detailDescription.Replace("l. ", ""); }
                                else if (detailDescription.Contains("m. ")) { detailDescription = detailDescription.Replace("m. ", ""); }
                            }

                            res.SectionData = itemColumn.Section;
                            if (!string.IsNullOrEmpty(itemColumn.TaskKey))
                            {
                                res.key = itemColumn.TaskKey;
                            }
                            else
                            {
                                res.key = Guid.NewGuid().ToString();
                            }

                            res.groupTaskId = itemColumn.GroupTaskId == null ? string.Empty : itemColumn.GroupTaskId;

                            //res.description = $"{itemColumn.Number4000};;{itemColumn.Description}";
                            //res.description250 = $"{itemColumn.Number250};;{itemColumn.Description}";
                            //res.description500 = $"{itemColumn.Number500};;{itemColumn.Description}";
                            //res.description1000 = $"{itemColumn.Number1000};;{itemColumn.Description}";
                            ////res.description2000 = $"{itemColumn.Number2000};;{itemColumn.Description}";
                            //res.description2000 = $"{charOnly.Replace(itemColumn.Number2000 == null ? string.Empty : itemColumn.Number2000, "#").Split("#")[0]};{detaiNumberData};{itemColumn.Description}";
                            //res.description4000 = $"{itemColumn.Number4000};;{itemColumn.Description}";

                            //res.description250 = $"{charOnly.Replace(itemColumn.Number250 == null ? string.Empty : itemColumn.Number250, "#").Split("#")[0]};{detaiNumberData};{itemColumn.Description}";
                            //res.description500 = $"{charOnly.Replace(itemColumn.Number500 == null ? string.Empty : itemColumn.Number500, "#").Split("#")[0]};{detaiNumberData};{itemColumn.Description}";
                            //res.description1000 = $"{charOnly.Replace(itemColumn.Number1000 == null ? string.Empty : itemColumn.Number1000, "#").Split("#")[0]};{detaiNumberData};{itemColumn.Description}";
                            //res.description2000 = $"{charOnly.Replace(itemColumn.Number2000 == null ? string.Empty : itemColumn.Number2000, "#").Split("#")[0]};{detaiNumberData};{detailDescription}";
                            //res.description4000 = $"{charOnly.Replace(itemColumn.Number4000 == null ? string.Empty : itemColumn.Number4000, "#").Split("#")[0]};{detaiNumberData};{itemColumn.Description}";

                            res.description250 = $"{charOnly.Replace(itemColumn.Number250 == null ? string.Empty : itemColumn.Number250, "#").Split("#")[0]};{detaiNumberData};{detailDescription}";
                            res.description500 = $"{charOnly.Replace(itemColumn.Number500 == null ? string.Empty : itemColumn.Number500, "#").Split("#")[0]};{detaiNumberData};{detailDescription}";
                            res.description1000 = $"{charOnly.Replace(itemColumn.Number1000 == null ? string.Empty : itemColumn.Number1000, "#").Split("#")[0]};{detaiNumberData};{detailDescription}";
                            res.description2000 = $"{charOnly.Replace(itemColumn.Number2000 == null ? string.Empty : itemColumn.Number2000, "#").Split("#")[0]};{detaiNumberData};{detailDescription}";
                            res.description4000 = $"{charOnly.Replace(itemColumn.Number4000 == null ? string.Empty : itemColumn.Number4000, "#").Split("#")[0]};{detaiNumberData};{detailDescription}";

                            if (itemColumn.Category == EnumCategoryServiceSheet.CBM_ADJUSTMENT) { res.adjustment.key = Guid.NewGuid().ToString(); }

                            string mappingKeyIdData = string.Empty;
                            foreach (var resItem in res.items)
                            {
                                resItem.key = Guid.NewGuid().ToString();
                                if (resItem.categoryItemType != null && resItem.categoryItemType == EnumCommonProperty.MappingParamKey)
                                {
                                    mappingKeyIdData = resItem.key;
                                }

                                if (itemColumn.Category != EnumCategoryServiceSheet.CBM_CALCULATE && resItem.mappingKeyId != null && resItem.mappingKeyId == EnumCommonProperty.MappingParamKeyTag)
                                {
                                    resItem.mappingKeyId = mappingKeyIdData;
                                }

                                if (resItem.isTaskValue != null && resItem.isTaskValue == "true")
                                {
                                    res.valueDisabled = resItem.key;
                                }

                                if (itemColumn.Category == EnumCategoryServiceSheet.CBM_CALCULATE_RESULT && resItem.categoryItemType != null && resItem.categoryItemType == EnumCommonProperty.ResultParamRating)
                                {
                                    //var takeOutChar = charOnly.Replace(itemColumn.Number4000, "#");
                                    //var splitNumber = takeOutChar.Split('#');

                                    List<dynamic> calculateAvg = psType4000.Where(x => x.typeCategory == EnumCommonProperty.CalculateAvg
                                                                                   //digitsOnly.Replace(x.value4000, "").Substring(0, 2) == digitsOnly.Replace(itemColumn.Number4000, "").Substring(0, 2)
                                                                                   //charOnly.Replace(x.Number4000, "#").Split("#")[0] == charOnly.Replace(itemColumn.Number4000, "#").Split("#").FirstOrDefault()
                                                                                   ).ToList();

                                    List<string> mappingKeys = new List<string>();

                                    // Target Calculate Key Id
                                    foreach (var itemCalculate in calculateAvg)
                                    {
                                        foreach (var items in itemCalculate.items)
                                        {
                                            if (items.targetCalculateKeyId != null)
                                            {
                                                items.targetCalculateKeyId = resItem.key;
                                            }

                                            if (items.category == EnumCommonProperty.CbmCalculateAvg)
                                            {
                                                mappingKeys.Add(items.key.ToString());
                                            }
                                        }
                                    }

                                    // mappingKeyId
                                    foreach (var itemCalculate in calculateAvg)
                                    {
                                        foreach (var items in itemCalculate.items)
                                        {
                                            if (items.category == EnumCommonProperty.CbmCalculateAvg)
                                            {
                                                items.mappingKeyId = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(mappingKeys));
                                            }
                                        }
                                    }

                                    //resItem.mappingKeyId = mappingKeyIdData;
                                }

                                switch (resItem.value.ToString())
                                {
                                    case "<<itemDetailNumber>>":
                                        if (!string.IsNullOrEmpty(itemColumn.Number250)) { resItem.value250 = itemColumn.Number250; }
                                        if (!string.IsNullOrEmpty(itemColumn.Number500)) { resItem.value500 = itemColumn.Number500; }
                                        if (!string.IsNullOrEmpty(itemColumn.Number1000)) { resItem.value1000 = itemColumn.Number1000; }
                                        if (!string.IsNullOrEmpty(itemColumn.Number2000)) { resItem.value2000 = itemColumn.Number2000; }
                                        if (!string.IsNullOrEmpty(itemColumn.Number4000)) { resItem.value4000 = itemColumn.Number4000; }

                                        break;
                                    case "<<itemNumber>>":
                                        #region Numbering 250
                                        if (!string.IsNullOrEmpty(itemColumn.Number250))
                                        {
                                            if (charOnly.IsMatch(itemColumn.Number250))
                                            {
                                                if (itemColumn.Category == EnumCategoryServiceSheet.CBM ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_MANUAL ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_ADJUSTMENT ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_ADJUSTMENT_CARRY_ROLLER ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_NORMAL ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.Defect_Group ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.DEFECT_MEASUREMENT ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_CALCULATE ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_CALCULATE_RESULT ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_MANUAL_DOUBLE ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_CALIBRATION ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_BRAKE)
                                                {
                                                    var input = itemColumn.Number250;
                                                    if (input.Length == 2)
                                                    {
                                                        var pattern = new Regex(@"\d{1}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        resItem.value250 = resulttt.Replace(itemColumn.Number250, "");
                                                    }
                                                    else if (input.Length == 3 || input.Length == 4)
                                                    {
                                                        var takeOutChar = charOnly.Replace(itemColumn.Number250, "#");
                                                        var splitNumber = takeOutChar.Split('#');

                                                        if (splitNumber.Length > 0 && splitNumber[0].Length == 3)
                                                        {
                                                            var pattern = new Regex(@"\d{3}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value250 = resulttt.Replace(itemColumn.Number250, "");
                                                        }
                                                        else
                                                        {
                                                            var pattern = new Regex(@"\d{2}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value250 = resulttt.Replace(itemColumn.Number250, "");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        var pattern = new Regex(@"\d{3}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        resItem.value250 = resulttt.Replace(itemColumn.Number250, "");
                                                    }

                                                    res.description250 = $"{charOnly.Replace(itemColumn.Number250 == null ? string.Empty : itemColumn.Number250, "#").Split("#")[0]};{resItem.value250};{detailDescription}";
                                                }
                                                else if (itemColumn.Category == "Crack")
                                                {
                                                    var input = itemColumn.Number250;
                                                    if (input.Length == 2)
                                                    {
                                                        var pattern = new Regex(@"\d{1}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        resItem.value250 = resulttt.Replace(itemColumn.Number250, "");
                                                    }
                                                    else if (input.Length == 3 || input.Length == 4)
                                                    {
                                                        var pattern = new Regex(@"\d{2}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        resItem.value250 = resulttt.Replace(itemColumn.Number250, "");
                                                    }
                                                    else
                                                    {
                                                        var pattern = new Regex(@"\d{3}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        var firstCrack = resulttt.Replace(itemColumn.Number250, "");
                                                        if (firstCrack.Contains("2"))
                                                        {
                                                            resItem.style.visibility = "hidden";
                                                        }

                                                        var valueNumber = itemColumn.Number250;

                                                        if (valueNumber.Contains("1"))
                                                        {
                                                            valueNumber = valueNumber.Substring(0, 3);
                                                        }
                                                        resItem.value250 = valueNumber;
                                                    }
                                                }
                                                else
                                                {
                                                    resItem.value250 = itemColumn.Number250;
                                                    resItem.style.visibility = "hidden";
                                                }
                                            }
                                            else
                                            {
                                                resItem.value250 = itemColumn.Number250;
                                            }
                                        }
                                        #endregion

                                        #region Numbering 500
                                        if (!string.IsNullOrEmpty(itemColumn.Number500))
                                        {
                                            if (charOnly.IsMatch(itemColumn.Number500))
                                            {
                                                if (itemColumn.Category == EnumCategoryServiceSheet.CBM ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_MANUAL ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_ADJUSTMENT ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_ADJUSTMENT_CARRY_ROLLER ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_NORMAL ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.Defect_Group ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.DEFECT_MEASUREMENT ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_CALCULATE ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_CALCULATE_RESULT ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_MANUAL_DOUBLE ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_CALIBRATION ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_BRAKE)
                                                {
                                                    var input = itemColumn.Number500;
                                                    if (input.Length == 2)
                                                    {
                                                        var pattern = new Regex(@"\d{1}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        resItem.value500 = resulttt.Replace(itemColumn.Number500, "");
                                                    }
                                                    else if (input.Length == 3 || input.Length == 4)
                                                    {
                                                        var takeOutChar = charOnly.Replace(itemColumn.Number500, "#");
                                                        var splitNumber = takeOutChar.Split('#');

                                                        if (splitNumber.Length > 0 && splitNumber[0].Length == 3)
                                                        {
                                                            var pattern = new Regex(@"\d{3}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value500 = resulttt.Replace(itemColumn.Number500, "");
                                                        }
                                                        else
                                                        {
                                                            var pattern = new Regex(@"\d{2}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value500 = resulttt.Replace(itemColumn.Number500, "");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        var pattern = new Regex(@"\d{3}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        resItem.value500 = resulttt.Replace(itemColumn.Number500, "");
                                                    }

                                                    res.description500 = $"{charOnly.Replace(itemColumn.Number500 == null ? string.Empty : itemColumn.Number500, "#").Split("#")[0]};{resItem.value500};{detailDescription}";
                                                }
                                                else if (itemColumn.Category == "Crack")
                                                {
                                                    var input = itemColumn.Number500;
                                                    var value500 = "";
                                                    if (input.Length == 2)
                                                    {
                                                        var pattern = new Regex(@"\d{1}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        resItem.value500 = resulttt.Replace(itemColumn.Number500, "");
                                                    }
                                                    else if (input.Length == 3 || input.Length == 4)
                                                    {
                                                        var pattern = new Regex(@"\d{2}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        resItem.value500 = resulttt.Replace(itemColumn.Number500, "");
                                                    }
                                                    else
                                                    {
                                                        var pattern = new Regex(@"\d{3}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        var firstCrack = resulttt.Replace(itemColumn.Number500, "");
                                                        if (firstCrack.Contains("2"))
                                                        {
                                                            resItem.style.visibility = "hidden";
                                                        }

                                                        var valueNumber = itemColumn.Number500;

                                                        if (valueNumber.Contains("1"))
                                                        {
                                                            valueNumber = valueNumber.Substring(0, 3);
                                                        }
                                                        resItem.value500 = valueNumber;
                                                        value500 = resulttt.Replace(itemColumn.Number500, "");
                                                    }
                                                    res.description500 = $"{charOnly.Replace(itemColumn.Number500 == null ? string.Empty : itemColumn.Number500, "#").Split("#")[0]};{value500};{detailDescription}";
                                                }
                                                else
                                                {
                                                    resItem.value500 = itemColumn.Number500;
                                                    resItem.style.visibility = "hidden";
                                                }
                                            }
                                            else
                                            {
                                                resItem.value500 = itemColumn.Number500;
                                            }
                                        }
                                        #endregion

                                        #region Numbering 1000
                                        if (!string.IsNullOrEmpty(itemColumn.Number1000))
                                        {
                                            if (charOnly.IsMatch(itemColumn.Number1000))
                                            {
                                                if (itemColumn.Category == EnumCategoryServiceSheet.CBM ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_MANUAL ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_ADJUSTMENT ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_ADJUSTMENT_CARRY_ROLLER ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_NORMAL ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.Defect_Group ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.DEFECT_MEASUREMENT ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_CALCULATE ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_CALCULATE_RESULT ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_MANUAL_DOUBLE ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_CALIBRATION ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_BRAKE)
                                                {
                                                    var input = itemColumn.Number1000;
                                                    if (input.Length == 2)
                                                    {
                                                        var pattern = new Regex(@"\d{1}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        resItem.value1000 = resulttt.Replace(itemColumn.Number1000, "");
                                                    }
                                                    else if (input.Length == 3 || input.Length == 4)
                                                    {
                                                        var takeOutChar = charOnly.Replace(itemColumn.Number1000, "#");

                                                        var splitNumber = takeOutChar.Split('#');

                                                        if (splitNumber.Length > 0 && splitNumber[0].Length == 3)
                                                        {
                                                            var pattern = new Regex(@"\d{3}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value1000 = resulttt.Replace(itemColumn.Number1000, "");
                                                        }
                                                        else
                                                        {
                                                            var pattern = new Regex(@"\d{2}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value1000 = resulttt.Replace(itemColumn.Number1000, "");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        var pattern = new Regex(@"\d{3}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        resItem.value1000 = resulttt.Replace(itemColumn.Number1000, "");
                                                    }

                                                    res.description1000 = $"{charOnly.Replace(itemColumn.Number1000 == null ? string.Empty : itemColumn.Number1000, "#").Split("#")[0]};{resItem.value1000};{detailDescription}";
                                                }
                                                else if (itemColumn.Category == "Crack")
                                                {
                                                    var input = itemColumn.Number1000;
                                                    var value1000 = "";
                                                    if (input.Length == 2)
                                                    {
                                                        var pattern = new Regex(@"\d{1}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        resItem.value1000 = resulttt.Replace(itemColumn.Number1000, "");
                                                    }
                                                    else if (input.Length == 3 || input.Length == 4)
                                                    {
                                                        var pattern = new Regex(@"\d{2}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        resItem.value1000 = resulttt.Replace(itemColumn.Number1000, "");
                                                    }
                                                    else
                                                    {
                                                        var pattern = new Regex(@"\d{3}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        var firstCrack = resulttt.Replace(itemColumn.Number1000, "");
                                                        if (firstCrack.Contains("2"))
                                                        {
                                                            resItem.style.visibility = "hidden";
                                                        }

                                                        var valueNumber = itemColumn.Number1000;

                                                        if (valueNumber.Contains("1"))
                                                        {
                                                            valueNumber = valueNumber.Substring(0, 3);
                                                        }
                                                        resItem.value1000 = valueNumber;
                                                        value1000 = resulttt.Replace(itemColumn.Number1000, "");
                                                    }

                                                    res.description1000 = $"{charOnly.Replace(itemColumn.Number1000 == null ? string.Empty : itemColumn.Number1000, "#").Split("#")[0]};{value1000};{detailDescription}";
                                                }
                                                else
                                                {
                                                    resItem.value1000 = itemColumn.Number1000;
                                                    resItem.style.visibility = "hidden";
                                                }
                                            }
                                            else
                                            {
                                                resItem.value1000 = itemColumn.Number1000;
                                            }
                                        }
                                        #endregion

                                        #region Numbering 2000
                                        if (!string.IsNullOrEmpty(itemColumn.Number2000))
                                        {
                                            if (charOnly.IsMatch(itemColumn.Number2000))
                                            {
                                                if (itemColumn.Category == EnumCategoryServiceSheet.CBM ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_MANUAL ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_ADJUSTMENT ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_ADJUSTMENT_CARRY_ROLLER ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_NORMAL ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.Defect_Group ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.DEFECT_MEASUREMENT ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_CALCULATE ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_CALCULATE_RESULT ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_MANUAL_DOUBLE ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_CALIBRATION ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_BRAKE)
                                                {
                                                    var input = itemColumn.Number2000;
                                                    if (input.Length == 2)
                                                    {
                                                        var pattern = new Regex(@"\d{1}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        resItem.value2000 = resulttt.Replace(itemColumn.Number2000, "");
                                                    }
                                                    else if (input.Length == 3 || input.Length == 4)
                                                    {
                                                        var takeOutChar = charOnly.Replace(itemColumn.Number2000, "#");
                                                        var splitNumber = takeOutChar.Split('#');

                                                        if (splitNumber.Length > 0 && splitNumber[0].Length == 3)
                                                        {
                                                            var pattern = new Regex(@"\d{3}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value2000 = resulttt.Replace(itemColumn.Number2000, "");
                                                        }
                                                        else
                                                        {
                                                            var pattern = new Regex(@"\d{2}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value2000 = resulttt.Replace(itemColumn.Number2000, "");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        var pattern = new Regex(@"\d{3}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        resItem.value2000 = resulttt.Replace(itemColumn.Number2000, "");
                                                    }

                                                    res.description2000 = $"{charOnly.Replace(itemColumn.Number2000 == null ? string.Empty : itemColumn.Number2000, "#").Split("#")[0]};{resItem.value2000};{detailDescription}";
                                                }
                                                else if (itemColumn.Category == "Crack")
                                                {
                                                    var input = itemColumn.Number2000;
                                                    var value2000 = "";
                                                    if (input.Length == 2)
                                                    {
                                                        var pattern = new Regex(@"\d{1}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        resItem.value2000 = resulttt.Replace(itemColumn.Number2000, "");
                                                    }
                                                    else if (input.Length == 3 || input.Length == 4)
                                                    {
                                                        var pattern = new Regex(@"\d{2}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        resItem.value2000 = resulttt.Replace(itemColumn.Number2000, "");
                                                    }
                                                    else
                                                    {
                                                        var pattern = new Regex(@"\d{3}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        var firstCrack = resulttt.Replace(itemColumn.Number2000, "");
                                                        if (firstCrack.Contains("2"))
                                                        {
                                                            resItem.style.visibility = "hidden";
                                                        }

                                                        var valueNumber = itemColumn.Number2000;

                                                        if (valueNumber.Contains("1"))
                                                        {
                                                            valueNumber = valueNumber.Substring(0, 3);
                                                        }
                                                        resItem.value2000 = valueNumber;
                                                        value2000 = resulttt.Replace(itemColumn.Number2000, "");
                                                    }

                                                    res.description2000 = $"{charOnly.Replace(itemColumn.Number2000 == null ? string.Empty : itemColumn.Number2000, "#").Split("#")[0]};{value2000};{detailDescription}";
                                                }
                                                else
                                                {
                                                    resItem.value2000 = itemColumn.Number2000;
                                                    resItem.style.visibility = "hidden";
                                                }
                                            }
                                            else
                                            {
                                                resItem.value2000 = itemColumn.Number2000;
                                            }
                                        }
                                        #endregion

                                        #region Numbering 4000
                                        if (!string.IsNullOrEmpty(itemColumn.Number4000))
                                        {
                                            if (charOnly.IsMatch(itemColumn.Number4000))
                                            {
                                                if (itemColumn.Category == EnumCategoryServiceSheet.CBM ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_MANUAL ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_ADJUSTMENT ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_ADJUSTMENT_CARRY_ROLLER ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_NORMAL ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.Defect_Group ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.DEFECT_MEASUREMENT ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_CALCULATE ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_CALCULATE_RESULT ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_MANUAL_DOUBLE ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_CALIBRATION ||
                                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_BRAKE)
                                                {
                                                    var input = itemColumn.Number4000;
                                                    if (input.Length == 2)
                                                    {
                                                        var pattern = new Regex(@"\d{1}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        resItem.value4000 = resulttt.Replace(itemColumn.Number4000, "");
                                                    }
                                                    else if (input.Length == 3 || input.Length == 4)
                                                    {
                                                        var takeOutChar = charOnly.Replace(itemColumn.Number4000, "#");
                                                        var splitNumber = takeOutChar.Split('#');

                                                        if (splitNumber.Length > 0 && splitNumber[0].Length == 3)
                                                        {
                                                            var pattern = new Regex(@"\d{3}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value4000 = resulttt.Replace(itemColumn.Number4000, "");
                                                        }
                                                        else
                                                        {
                                                            var pattern = new Regex(@"\d{2}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value4000 = resulttt.Replace(itemColumn.Number4000, "");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        var pattern = new Regex(@"\d{3}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        resItem.value4000 = resulttt.Replace(itemColumn.Number4000, "");
                                                    }

                                                    res.description4000 = $"{charOnly.Replace(itemColumn.Number4000 == null ? string.Empty : itemColumn.Number4000, "#").Split("#")[0]};{resItem.value4000};{detailDescription}";
                                                }
                                                else if (itemColumn.Category == "Crack")
                                                {
                                                    var input = itemColumn.Number4000;
                                                    var value4000 = "";
                                                    if (input.Length == 2)
                                                    {
                                                        var pattern = new Regex(@"\d{1}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        resItem.value4000 = resulttt.Replace(itemColumn.Number4000, "");
                                                    }
                                                    else if (input.Length == 3 || input.Length == 4)
                                                    {
                                                        var pattern = new Regex(@"\d{2}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        resItem.value4000 = resulttt.Replace(itemColumn.Number4000, "");
                                                    }
                                                    else
                                                    {
                                                        var pattern = new Regex(@"\d{3}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        var firstCrack = resulttt.Replace(itemColumn.Number4000, "");
                                                        if (firstCrack.Contains("2"))
                                                        {
                                                            resItem.style.visibility = "hidden";
                                                        }

                                                        var valueNumber = itemColumn.Number4000;

                                                        if (valueNumber.Contains("1"))
                                                        {
                                                            valueNumber = valueNumber.Substring(0, 3);
                                                        }
                                                        resItem.value4000 = valueNumber;
                                                        value4000 = resulttt.Replace(itemColumn.Number4000, "");

                                                    }

                                                    res.description4000 = $"{charOnly.Replace(itemColumn.Number4000 == null ? string.Empty : itemColumn.Number4000, "#").Split("#")[0]};{value4000};{detailDescription}";
                                                }
                                                else
                                                {
                                                    resItem.value4000 = itemColumn.Number4000;
                                                    resItem.style.visibility = "hidden";
                                                }
                                            }
                                            else
                                            {
                                                resItem.value4000 = itemColumn.Number4000;
                                            }
                                        }
                                        #endregion

                                        break;
                                    case "<<itemCrackCode>>":
                                        #region Numbering 250
                                        if (!string.IsNullOrEmpty(itemColumn.Number250))
                                        {
                                            if (itemColumn.Category == "Crack" && charOnly.IsMatch(itemColumn.Number250))
                                            {
                                                var input = itemColumn.Number250;
                                                if (input.Length == 2)
                                                {
                                                    var pattern = new Regex(@"\d{1}");
                                                    var resulttt = pattern.Replace(input, "");

                                                    resItem.Number250 = resulttt.Replace(itemColumn.Number250, "");
                                                }
                                                else if (input.Length == 3 || input.Length == 4)
                                                {
                                                    var pattern = new Regex(@"\d{2}");
                                                    var resulttt = pattern.Replace(input, "");

                                                    resItem.Number250 = resulttt.Replace(itemColumn.Number250, "");
                                                }
                                                else
                                                {
                                                    var pattern = new Regex(@"\d{3}");
                                                    var resulttt = pattern.Replace(input, "");

                                                    var firstCrack = resulttt.Replace(itemColumn.Number250, "");

                                                    resItem.Number250 = firstCrack.ToUpper();
                                                }
                                            }
                                        }
                                        #endregion

                                        #region Numbering 500
                                        if (!string.IsNullOrEmpty(itemColumn.Number500))
                                        {
                                            if (itemColumn.Category == "Crack" && charOnly.IsMatch(itemColumn.Number500))
                                            {
                                                var input = itemColumn.Number500;
                                                if (input.Length == 2)
                                                {
                                                    var pattern = new Regex(@"\d{1}");
                                                    var resulttt = pattern.Replace(input, "");

                                                    resItem.Number500 = resulttt.Replace(itemColumn.Number500, "");
                                                }
                                                else if (input.Length == 3 || input.Length == 4)
                                                {
                                                    var pattern = new Regex(@"\d{2}");
                                                    var resulttt = pattern.Replace(input, "");

                                                    resItem.Number500 = resulttt.Replace(itemColumn.Number500, "");
                                                }
                                                else
                                                {
                                                    var pattern = new Regex(@"\d{3}");
                                                    var resulttt = pattern.Replace(input, "");

                                                    var firstCrack = resulttt.Replace(itemColumn.Number500, "");

                                                    resItem.Number500 = firstCrack.ToUpper();
                                                }
                                            }
                                        }
                                        #endregion

                                        #region Numbering 1000
                                        if (!string.IsNullOrEmpty(itemColumn.Number1000))
                                        {
                                            if (itemColumn.Category == "Crack" && charOnly.IsMatch(itemColumn.Number1000))
                                            {
                                                var input = itemColumn.Number1000;
                                                if (input.Length == 2)
                                                {
                                                    var pattern = new Regex(@"\d{1}");
                                                    var resulttt = pattern.Replace(input, "");

                                                    resItem.Number1000 = resulttt.Replace(itemColumn.Number1000, "");
                                                }
                                                else if (input.Length == 3 || input.Length == 4)
                                                {
                                                    var pattern = new Regex(@"\d{2}");
                                                    var resulttt = pattern.Replace(input, "");

                                                    resItem.Number1000 = resulttt.Replace(itemColumn.Number1000, "");
                                                }
                                                else
                                                {
                                                    var pattern = new Regex(@"\d{3}");
                                                    var resulttt = pattern.Replace(input, "");

                                                    var firstCrack = resulttt.Replace(itemColumn.Number1000, "");

                                                    resItem.Number1000 = firstCrack.ToUpper();
                                                }
                                            }
                                        }
                                        #endregion

                                        #region Numbering 2000
                                        if (!string.IsNullOrEmpty(itemColumn.Number2000))
                                        {
                                            if (itemColumn.Category == "Crack" && charOnly.IsMatch(itemColumn.Number2000))
                                            {
                                                var input = itemColumn.Number2000;
                                                if (input.Length == 2)
                                                {
                                                    var pattern = new Regex(@"\d{1}");
                                                    var resulttt = pattern.Replace(input, "");

                                                    resItem.Number2000 = resulttt.Replace(itemColumn.Number2000, "");
                                                }
                                                else if (input.Length == 3 || input.Length == 4)
                                                {
                                                    var pattern = new Regex(@"\d{2}");
                                                    var resulttt = pattern.Replace(input, "");

                                                    resItem.Number2000 = resulttt.Replace(itemColumn.Number2000, "");
                                                }
                                                else
                                                {
                                                    var pattern = new Regex(@"\d{3}");
                                                    var resulttt = pattern.Replace(input, "");

                                                    var firstCrack = resulttt.Replace(itemColumn.Number2000, "");

                                                    resItem.Number2000 = firstCrack.ToUpper();
                                                }
                                            }
                                        }
                                        #endregion

                                        #region Numbering 4000
                                        if (!string.IsNullOrEmpty(itemColumn.Number4000))
                                        {
                                            if (itemColumn.Category == "Crack" && charOnly.IsMatch(itemColumn.Number4000))
                                            {
                                                var input = itemColumn.Number4000;
                                                if (input.Length == 2)
                                                {
                                                    var pattern = new Regex(@"\d{1}");
                                                    var resulttt = pattern.Replace(input, "");

                                                    resItem.Number4000 = resulttt.Replace(itemColumn.Number4000, "");
                                                }
                                                else if (input.Length == 3 || input.Length == 4)
                                                {
                                                    var pattern = new Regex(@"\d{2}");
                                                    var resulttt = pattern.Replace(input, "");

                                                    resItem.Number4000 = resulttt.Replace(itemColumn.Number4000, "");
                                                }
                                                else
                                                {
                                                    var pattern = new Regex(@"\d{3}");
                                                    var resulttt = pattern.Replace(input, "");

                                                    var firstCrack = resulttt.Replace(itemColumn.Number4000, "");

                                                    resItem.Number4000 = firstCrack.ToUpper();
                                                }
                                            }
                                        }
                                        #endregion
                                        break;
                                    case "<<itemDesc>>":
                                        var dataDesc = itemColumn.Description;

                                        if (itemColumn.Description != null && (itemColumn.Category == EnumCategoryServiceSheet.CBM ||
                                                                               itemColumn.Category == EnumCategoryServiceSheet.CBM_MANUAL ||
                                                                               itemColumn.Category == EnumCategoryServiceSheet.CBM_ADJUSTMENT ||
                                                                               itemColumn.Category == EnumCategoryServiceSheet.CBM_ADJUSTMENT_CARRY_ROLLER ||
                                                                               itemColumn.Category == EnumCategoryServiceSheet.CBM_NORMAL ||
                                                                               itemColumn.Category == EnumCategoryServiceSheet.CBM_CALCULATE ||
                                                                               itemColumn.Category == EnumCategoryServiceSheet.CBM_CALCULATE_RESULT ||
                                                                               itemColumn.Category == EnumCategoryServiceSheet.CBM_MANUAL_DOUBLE ||
                                                                               itemColumn.Category == EnumCategoryServiceSheet.CBM_CALIBRATION ||
                                                                               itemColumn.Category == EnumCategoryServiceSheet.CBM_BRAKE ||
                                                                               itemColumn.Category == EnumCategoryServiceSheet.DEFECT_MEASUREMENT
                                                                               ))
                                        {
                                            if (dataDesc.Contains("a1. ")) { dataDesc = dataDesc.Replace("a1. ", ""); }
                                            else if (dataDesc.Contains("a2. ")) { dataDesc = dataDesc.Replace("a2. ", ""); }
                                            else if (dataDesc.Contains("b1. ")) { dataDesc = dataDesc.Replace("b1. ", ""); }
                                            else if (dataDesc.Contains("b2. ")) { dataDesc = dataDesc.Replace("b2. ", ""); }
                                            else if (dataDesc.Contains("a. ")) { dataDesc = dataDesc.Replace("a. ", ""); }
                                            else if (dataDesc.Contains("b. ")) { dataDesc = dataDesc.Replace("b. ", ""); }
                                            else if (dataDesc.Contains("c. ")) { dataDesc = dataDesc.Replace("c. ", ""); }
                                            else if (dataDesc.Contains("d. ")) { dataDesc = dataDesc.Replace("d. ", ""); }
                                            else if (dataDesc.Contains("e. ")) { dataDesc = dataDesc.Replace("e. ", ""); }
                                            else if (dataDesc.Contains("f. ")) { dataDesc = dataDesc.Replace("f. ", ""); }
                                            else if (dataDesc.Contains("g. ")) { dataDesc = dataDesc.Replace("g. ", ""); }
                                            else if (dataDesc.Contains("h. ")) { dataDesc = dataDesc.Replace("h. ", ""); }
                                            else if (dataDesc.Contains("i. ")) { dataDesc = dataDesc.Replace("i. ", ""); }
                                            else if (dataDesc.Contains("j. ")) { dataDesc = dataDesc.Replace("j. ", ""); }
                                            else if (dataDesc.Contains("k. ")) { dataDesc = dataDesc.Replace("k. ", ""); }
                                            else if (dataDesc.Contains("l. ")) { dataDesc = dataDesc.Replace("l. ", ""); }
                                            else if (dataDesc.Contains("m. ")) { dataDesc = dataDesc.Replace("m. ", ""); }
                                        }

                                        resItem.value = dataDesc;
                                        break;
                                    case "<<section>>":
                                        resItem.value = itemColumn.Description;
                                        break;
                                    case "<<itemMapping>>":
                                        resItem.value = itemColumn.ServiceMappingValue;
                                        break;
                                }

                                if (itemColumn.Category != EnumCategoryServiceSheet.Crack && itemColumn.LastRow)
                                {
                                    if (resItem.style.border != null)
                                    {
                                        resItem.style.border.bottom = EnumCommonProperty.StyleBorder;
                                    }
                                }

                                if (itemColumn.Category == EnumCategoryServiceSheet.Crack && itemColumn.FirstRow)
                                {
                                    if (resItem.style.border != null)
                                    {
                                        resItem.style.border.top = EnumCommonProperty.StyleBorder;
                                    }
                                }

                                if (itemColumn.Category == EnumCategoryServiceSheet.Crack && itemColumn.LastRow)
                                {
                                    if (resItem.style.border != null)
                                    {
                                        resItem.style.border.bottom = EnumCommonProperty.StyleBorder;
                                    }
                                }
                            }

                            if ((itemColumn.Category == EnumCategoryServiceSheet.CBM ||
                                itemColumn.Category == EnumCategoryServiceSheet.CBM_MANUAL ||
                                itemColumn.Category == EnumCategoryServiceSheet.CBM_ADJUSTMENT ||
                                itemColumn.Category == EnumCategoryServiceSheet.CBM_ADJUSTMENT_CARRY_ROLLER ||
                                itemColumn.Category == EnumCategoryServiceSheet.CBM_NORMAL ||
                                itemColumn.Category == EnumCategoryServiceSheet.CBM_CALCULATE ||
                                itemColumn.Category == EnumCategoryServiceSheet.CBM_CALCULATE_RESULT ||
                                itemColumn.Category == EnumCategoryServiceSheet.CBM_MANUAL_DOUBLE ||
                                itemColumn.Category == EnumCategoryServiceSheet.CBM_CALIBRATION ||
                                itemColumn.Category == EnumCategoryServiceSheet.CBM_BRAKE ||
                                itemColumn.Category == EnumCategoryServiceSheet.DEFECT_MEASUREMENT) && resCbmHeader != null)
                            {
                                resCbmHeader.SectionData = itemColumn.Section;

                                if (resCbmHeaderRoller != null)
                                {
                                    foreach (var itemDataHeader in resCbmHeader.items)
                                    {
                                        if (itemDataHeader.style.border != null)
                                        {
                                            itemDataHeader.style.border.top = "none";
                                        }
                                    }

                                    resCbmHeaderRoller.SectionData = itemColumn.Section;

                                    if (!string.IsNullOrEmpty(itemColumn.Number250)) { psType250.Add(resCbmHeaderRoller); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number500)) { psType500.Add(resCbmHeaderRoller); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number1000)) { psType1000.Add(resCbmHeaderRoller); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number2000)) { psType2000.Add(resCbmHeaderRoller); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number4000)) { psType4000.Add(resCbmHeaderRoller); }
                                }

                                // Insert CBM Header
                                if (!string.IsNullOrEmpty(itemColumn.Number250)) { psType250.Add(resCbmHeader); }
                                if (!string.IsNullOrEmpty(itemColumn.Number500)) { psType500.Add(resCbmHeader); }
                                if (!string.IsNullOrEmpty(itemColumn.Number1000)) { psType1000.Add(resCbmHeader); }
                                if (!string.IsNullOrEmpty(itemColumn.Number2000)) { psType2000.Add(resCbmHeader); }
                                if (!string.IsNullOrEmpty(itemColumn.Number4000)) { psType4000.Add(resCbmHeader); }

                            }

                            if ((itemColumn.Category == EnumCategoryServiceSheet.Defect_Group || itemColumn.Category == EnumCategoryServiceSheet.DEFECT_MEASUREMENT || itemColumn.Category == EnumCategoryServiceSheet.Service_With_Header) && resDefectGroupHeader != null)
                            {
                                // Insert CBM Header
                                if (!string.IsNullOrEmpty(itemColumn.Number250)) { psType250.Add(resDefectGroupHeader); }
                                if (!string.IsNullOrEmpty(itemColumn.Number500)) { psType500.Add(resDefectGroupHeader); }
                                if (!string.IsNullOrEmpty(itemColumn.Number1000)) { psType1000.Add(resDefectGroupHeader); }
                                if (!string.IsNullOrEmpty(itemColumn.Number2000)) { psType2000.Add(resDefectGroupHeader); }
                                if (!string.IsNullOrEmpty(itemColumn.Number4000)) { psType4000.Add(resDefectGroupHeader); }

                            }

                            // Insert Table Schema
                            if (!string.IsNullOrEmpty(itemColumn.Table))
                            {
                                if (itemColumn.Table == EnumTableServiceSheet.LUBRICANT)
                                {
                                    var paramCbmHeader = new Dictionary<string, object>();
                                    paramCbmHeader.Add("category", EnumCategoryServiceSheet.TABLE);
                                    paramCbmHeader.Add("rating", EnumRatingServiceSheet.LUBRICANT);

                                    var resultGuid = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);
                                    var resGuide = resultGuid.content;
                                    resGuide.key = Guid.NewGuid().ToString();
                                    resGuide.SectionData = itemColumn.Section;

                                    if (!string.IsNullOrEmpty(itemColumn.Number250)) { psType250.Add(resGuide); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number500)) { psType500.Add(resGuide); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number1000)) { psType1000.Add(resGuide); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number2000)) { psType2000.Add(resGuide); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number4000)) { psType4000.Add(resGuide); }
                                }
                                else if (itemColumn.Table == EnumTableServiceSheet.BATTERY)
                                {
                                    var paramCbmHeader = new Dictionary<string, object>();
                                    paramCbmHeader.Add("category", EnumCategoryServiceSheet.TABLE);
                                    paramCbmHeader.Add("rating", EnumRatingServiceSheet.BATTERY);

                                    var resultGuid = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);
                                    var resGuide = resultGuid.content;
                                    resGuide.key = Guid.NewGuid().ToString();
                                    resGuide.SectionData = itemColumn.Section;

                                    if (!string.IsNullOrEmpty(itemColumn.Number250)) { psType250.Add(resGuide); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number500)) { psType500.Add(resGuide); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number1000)) { psType1000.Add(resGuide); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number2000)) { psType2000.Add(resGuide); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number4000)) { psType4000.Add(resGuide); }
                                }
                                else if (itemColumn.Table == EnumTableServiceSheet.BATTERY_CCA)
                                {
                                    var paramCbmHeader = new Dictionary<string, object>();
                                    paramCbmHeader.Add("category", EnumCategoryServiceSheet.TABLE);
                                    paramCbmHeader.Add("rating", EnumRatingServiceSheet.BATTERY_CCA);

                                    var resultGuid = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);
                                    var resGuide = resultGuid.content;
                                    resGuide.key = Guid.NewGuid().ToString();
                                    resGuide.SectionData = itemColumn.Section;

                                    if (!string.IsNullOrEmpty(itemColumn.Number250)) { psType250.Add(resGuide); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number500)) { psType500.Add(resGuide); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number1000)) { psType1000.Add(resGuide); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number2000)) { psType2000.Add(resGuide); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number4000)) { psType4000.Add(resGuide); }
                                }
                                else
                                {
                                    var paramCbmHeader = new Dictionary<string, object>();
                                    paramCbmHeader.Add("category", EnumCategoryServiceSheet.TABLE);
                                    paramCbmHeader.Add("rating", itemColumn.Table);

                                    var resultGuid = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);
                                    var resGuide = resultGuid.content;
                                    resGuide.key = Guid.NewGuid().ToString();
                                    resGuide.SectionData = itemColumn.Section;

                                    if (!string.IsNullOrEmpty(itemColumn.Number250)) { psType250.Add(resGuide); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number500)) { psType500.Add(resGuide); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number1000)) { psType1000.Add(resGuide); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number2000)) { psType2000.Add(resGuide); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number4000)) { psType4000.Add(resGuide); }
                                }
                            }

                            // Insert MAIN DATA
                            if (!string.IsNullOrEmpty(itemColumn.Number250)) { psType250.Add(res); }
                            if (!string.IsNullOrEmpty(itemColumn.Number500)) { psType500.Add(res); }
                            if (!string.IsNullOrEmpty(itemColumn.Number1000)) { psType1000.Add(res); }
                            if (!string.IsNullOrEmpty(itemColumn.Number2000)) { psType2000.Add(res); }
                            if (!string.IsNullOrEmpty(itemColumn.Number4000)) { psType4000.Add(res); }

                            // Insert Image
                            if (!string.IsNullOrEmpty(itemColumn.ImageData))
                            {
                                var paramCbmHeader = new Dictionary<string, object>();
                                paramCbmHeader.Add("category", EnumCategoryServiceSheet.NORMAL);
                                paramCbmHeader.Add("rating", EnumRatingServiceSheet.IMAGE);

                                var resultImage = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);
                                var resImage = resultImage.content;
                                resImage.key = Guid.NewGuid().ToString();
                                resImage.SectionData = itemColumn.Section;

                                foreach (var resImageItem in resImage.items)
                                {
                                    resImageItem.key = Guid.NewGuid().ToString();
                                    if (resImageItem.value.ToString() == EnumCommonProperty.ImageData)
                                    {
                                        resImageItem.value = itemColumn.ImageData;
                                    }
                                }

                                if (!string.IsNullOrEmpty(itemColumn.Number250)) { psType250.Add(resImage); }
                                if (!string.IsNullOrEmpty(itemColumn.Number500)) { psType500.Add(resImage); }
                                if (!string.IsNullOrEmpty(itemColumn.Number1000)) { psType1000.Add(resImage); }
                                if (!string.IsNullOrEmpty(itemColumn.Number2000)) { psType2000.Add(resImage); }
                                if (!string.IsNullOrEmpty(itemColumn.Number4000)) { psType4000.Add(resImage); }
                            }

                            // Insert Guide
                            if (!string.IsNullOrEmpty(itemColumn.GuidTable))
                            {
                                var paramCbmHeader = new Dictionary<string, object>();
                                paramCbmHeader.Add("category", EnumCategoryServiceSheet.GUIDE);
                                paramCbmHeader.Add("rating", itemColumn.GuidTable);

                                var resultGuid = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);
                                var resGuide = resultGuid.content;
                                resGuide.key = Guid.NewGuid().ToString();
                                resGuide.SectionData = itemColumn.Section;

                                if (!string.IsNullOrEmpty(itemColumn.Number250)) { psType250.Add(resGuide); }
                                if (!string.IsNullOrEmpty(itemColumn.Number500)) { psType500.Add(resGuide); }
                                if (!string.IsNullOrEmpty(itemColumn.Number1000)) { psType1000.Add(resGuide); }
                                if (!string.IsNullOrEmpty(itemColumn.Number2000)) { psType2000.Add(resGuide); }
                                if (!string.IsNullOrEmpty(itemColumn.Number4000)) { psType4000.Add(resGuide); }
                            }
                        }
                    }
                    #endregion

                    dataJsonResult.Add("250", psType250);
                    dataJsonResult.Add("500", psType500);
                    dataJsonResult.Add("1000", psType1000);
                    dataJsonResult.Add("2000", psType2000);
                    dataJsonResult.Add("4000", psType4000);

                    List<string> psTypeData = new List<string>();
                    psTypeData.Add("250");
                    psTypeData.Add("500");
                    psTypeData.Add("1000");
                    psTypeData.Add("2000");
                    psTypeData.Add("4000");

                    foreach (var itemDataPsType in psTypeData)
                    {
                        var dataTaskGroup = dataJsonResult.Where(x => x.Key == itemDataPsType).FirstOrDefault().Value;

                        var resultObj = JsonConvert.DeserializeObject<List<dynamic>>(JsonConvert.SerializeObject(dataTaskGroup));

                        if (resultObj.Count == 0)
                        {
                            continue;
                        }

                        foreach (var itemGorupResult in resultObj)
                        {
                            if (itemGorupResult.description != EnumCommonProperty.Description)
                            {
                                itemGorupResult.Remove("description250");
                                itemGorupResult.Remove("description500");
                                itemGorupResult.Remove("description1000");
                                itemGorupResult.Remove("description2000");
                                itemGorupResult.Remove("description4000");
                                continue;
                            };

                            if (itemDataPsType == "250") { itemGorupResult.description = itemGorupResult.description250; }
                            else if (itemDataPsType == "500") { itemGorupResult.description = itemGorupResult.description500; }
                            else if (itemDataPsType == "1000") { itemGorupResult.description = itemGorupResult.description1000; }
                            else if (itemDataPsType == "2000") { itemGorupResult.description = itemGorupResult.description2000; }
                            else if (itemDataPsType == "4000") { itemGorupResult.description = itemGorupResult.description4000; }

                            foreach (var itemGroups in itemGorupResult.items)
                            {
                                switch (itemGroups.value.ToString())
                                {
                                    case "<<itemDetailNumber>>":
                                        if (itemDataPsType == "250") { itemGroups.value = itemGroups.value250; }
                                        else if (itemDataPsType == "500") { itemGroups.value = itemGroups.value500; }
                                        else if (itemDataPsType == "1000") { itemGroups.value = itemGroups.value1000; }
                                        else if (itemDataPsType == "2000") { itemGroups.value = itemGroups.value2000; }
                                        else if (itemDataPsType == "4000") { itemGroups.value = itemGroups.value4000; }

                                        break;
                                    case "<<itemNumber>>":
                                        if (itemDataPsType == "250") { itemGroups.value = itemGroups.value250; }
                                        else if (itemDataPsType == "500") { itemGroups.value = itemGroups.value500; }
                                        else if (itemDataPsType == "1000") { itemGroups.value = itemGroups.value1000; }
                                        else if (itemDataPsType == "2000") { itemGroups.value = itemGroups.value2000; }
                                        else if (itemDataPsType == "4000") { itemGroups.value = itemGroups.value4000; }

                                        break;
                                    case "<<itemCrackCode>>":
                                        if (itemDataPsType == "250") { itemGroups.value = itemGroups.Number250; }
                                        else if (itemDataPsType == "500") { itemGroups.value = itemGroups.Number500; }
                                        else if (itemDataPsType == "1000") { itemGroups.value = itemGroups.Number1000; }
                                        else if (itemDataPsType == "2000") { itemGroups.value = itemGroups.Number2000; }
                                        else if (itemDataPsType == "4000") { itemGroups.value = itemGroups.Number4000; }

                                        break;
                                }

                                if (itemGroups.disabledByItemKey != null)
                                {
                                    switch (itemGroups.disabledByItemKey.ToString())
                                    {
                                        case "<<disabledByItemKey>>":
                                            if (itemDataPsType == "250") { itemGroups.disabledByItemKey = itemGorupResult.valueDisabled; }
                                            else if (itemDataPsType == "500") { itemGroups.disabledByItemKey = itemGorupResult.valueDisabled; }
                                            else if (itemDataPsType == "1000") { itemGroups.disabledByItemKey = itemGorupResult.valueDisabled; }
                                            else if (itemDataPsType == "2000") { itemGroups.disabledByItemKey = itemGorupResult.valueDisabled; }
                                            else if (itemDataPsType == "4000") { itemGroups.disabledByItemKey = itemGorupResult.valueDisabled; }

                                            break;
                                    }
                                }

                                itemGroups.Remove("value250");
                                itemGroups.Remove("value500");
                                itemGroups.Remove("value1000");
                                itemGroups.Remove("value2000");
                                itemGroups.Remove("value4000");
                            }

                            itemGorupResult.Remove("description250");
                            itemGorupResult.Remove("description500");
                            itemGorupResult.Remove("description1000");
                            itemGorupResult.Remove("description2000");
                            itemGorupResult.Remove("description4000");
                            itemGorupResult.Remove("valueDisabled");

                        }

                        #region Default Header
                        GenerateJsonModel jsonModel = new GenerateJsonModel();
                        jsonModel.subGroup = new List<SubGroup>();

                        jsonModel.modelId = modelList;
                        jsonModel.psTypeId = itemDataPsType;
                        jsonModel.workOrder = "";
                        jsonModel.groupName = groupName.Replace(" ", "_").ToUpper();
                        jsonModel.groupSeq = 2;
                        jsonModel.key = groupName.Replace(" ", "_").ToUpper();
                        jsonModel.version = "1.0.0";

                        var paramInfo = new Dictionary<string, object>();
                        paramInfo.Add("category", "NORMAL_INFO");
                        paramInfo.Add("rating", "NO");

                        var resultInfo = await _repositoryGenerateJson.GetDataByParam(paramInfo);

                        // Info Tab
                        var infoTab = listGenerateColumn.Where(x => x.Category == "InfoTab").FirstOrDefault();
                        if (infoTab != null)
                        {
                            var infoSplit = infoTab.Description.Split("|");
                            foreach (var itemInfo in resultInfo.content.items)
                            {
                                switch (itemInfo.value.ToString())
                                {
                                    case "<<warning>>":
                                        itemInfo.value = infoSplit[0];
                                        break;
                                    case "<<info>>":
                                        itemInfo.value = infoSplit[1];
                                        break;
                                }
                            }
                        }

                        TaskGroup taskGroupDataInfo = new TaskGroup();
                        taskGroupDataInfo.task = new List<dynamic>();
                        taskGroupDataInfo.name = "Information"; // Section
                        taskGroupDataInfo.key = $"{groupName.Replace(" ", "_")}_INFORMATION"; // Section
                        taskGroupDataInfo.task.Add(resultInfo.content);
                        #endregion

                        #region Merge Data
                        List<TaskGroup> taskGroupDataList = new List<TaskGroup>();

                        var sectionGroup = listGenerateColumn.Select(x => x.Section).Distinct().Where(x => x != "INFORMATION").ToList();
                        foreach (var itemSectionData in sectionGroup)
                        {
                            TaskGroup taskGroupData = new TaskGroup();
                            taskGroupData.name = itemSectionData; // Section
                            taskGroupData.key = $"{itemSectionData.Replace(" ", "_")}"; // Section
                                                                                        //taskGroupData.task = dataTaskGroup;
                            taskGroupData.task = resultObj.Where(x => x.SectionData == itemSectionData);

                            taskGroupDataList.Add(taskGroupData);
                        }

                        //taskGroupDataList.Add(taskGroupData);

                        //TaskGroup taskGroupData = new TaskGroup();
                        //taskGroupData.name = "Pre Service Check"; // Section
                        //taskGroupData.key = $"PRE_SERVICE_CHECK_DATA"; // Section
                        ////taskGroupData.task = dataTaskGroup;
                        //taskGroupData.task = resultObj;

                        SubGroup subGroupData = new SubGroup();
                        subGroupData.taskGroup = new List<TaskGroup>();
                        subGroupData.name = listGenerateColumn.FirstOrDefault() == null ? string.Empty : listGenerateColumn.FirstOrDefault().GroupName.ToUpper();
                        subGroupData.key = $"{groupName.Replace(" ", "_").ToUpper()}";
                        subGroupData.desc = "-";
                        subGroupData.taskGroup.Add(taskGroupDataInfo);
                        //subGroupData.taskGroup.Add(taskGroupData);
                        subGroupData.taskGroup.AddRange(taskGroupDataList);
                        #endregion

                        jsonModel.subGroup.Add(subGroupData);

                        resultList.Add(jsonModel);
                    }
                }

                result.IsError = false;
                result.Message = "Success";
                result.Content = resultList;
            }

            return new ServiceResult
            {
                Message = "Data updated successfully",
                IsError = false,
                Content = result
            };
        }

        public async Task<ServiceResult> GenerateTemplateFullSection(GenerateModelJson model)
        {
            try
            {
                // WO TESTING 9992703092

                ServiceResult result = new ServiceResult();
                dynamic resultHeaderCbmAutomatic = null;
                dynamic resultHeaderCbmAutomaticRoller = null;
                dynamic resultHeaderDefectGroup = null;
                //Dictionary<string, object> dataJsonResult = new Dictionary<string, object>();
                List<dynamic> resultList = new List<dynamic>();
                ServiceSizeRequest resultServiceSizeList = new ServiceSizeRequest();
                resultServiceSizeList.PsType4000 = new List<dynamic>();
                resultServiceSizeList.PsType2000 = new List<dynamic>();
                resultServiceSizeList.PsType1000 = new List<dynamic>();
                resultServiceSizeList.PsType500 = new List<dynamic>();
                resultServiceSizeList.PsType250 = new List<dynamic>();

                List<dynamic> psType250 = new List<dynamic>();
                List<dynamic> psType500 = new List<dynamic>();
                List<dynamic> psType1000 = new List<dynamic>();
                List<dynamic> psType2000 = new List<dynamic>();
                List<dynamic> psType4000 = new List<dynamic>();

                var valueDisabledBefore = "";
                var valueImageBefore = "";

                using (var excel = model.file.OpenReadStream())
                {
                    using var workBook = new XLWorkbook(excel);
                    IXLWorksheet workSheet = workBook.Worksheets.Where(x => x.Name == model.workSheet).FirstOrDefault();

                    //string currentRow = model.startRow;
                    //string endRow = model.endRow;

                    string currentRow = "2";
                    string endRow = workSheet.RowsUsed().Count().ToString();

                    string parentCurrentRow = "2";
                    string parentEndRow = workSheet.RowsUsed().Count().ToString();

                    Regex digitsOnly = new Regex(@"[^\d]");
                    Regex charOnly = new Regex(@"[a-zA-Z ]");

                    // Preparation
                    var dataWeworkSheetCount = workSheet.Range($"A{currentRow}:R{endRow}").RowCount();
                    var dataWeworkSheet = workSheet.Range($"A{currentRow}:R{endRow}")
                        .CellsUsed()
                        .Select(c => new AddressMappingData
                        {
                            Row = Convert.ToInt32(digitsOnly.Replace(c.Address.ToString(), "")),
                            Value = c.Value.ToString(),
                            Address = c.Address.ToString()
                        }).ToList();

                    // Model and Group
                    var modelList = dataWeworkSheet.Where(x => x.Address.Contains("A")).Select(x => x.Value).Distinct().FirstOrDefault();
                    var groupList = dataWeworkSheet.Where(x => x.Address.Contains("B")).Select(x => x.Value).Distinct().ToList();

                    foreach (var groupName in groupList.Where(x => x != "General"))
                    {
                        var usedGroup = workSheet.Range($"A{parentCurrentRow}:R{parentEndRow}")
                        .CellsUsed()
                        .Select(c => new AddressMappingData
                        {
                            Row = Convert.ToInt32(digitsOnly.Replace(c.Address.ToString(), "")),
                            Value = c.Value.ToString(),
                            Address = c.Address.ToString()
                        }).Where(o => o.Address.Contains("B") && o.Value.ToString() == groupName).ToList();

                        currentRow = usedGroup.OrderBy(x => x.Row).FirstOrDefault().Row.ToString();
                        endRow = usedGroup.OrderByDescending(x => x.Row).FirstOrDefault().Row.ToString();

                        var dataGroupCount = workSheet.Range($"A{currentRow}:R{endRow}").RowCount();
                        var dataWeworkSheetGroup = workSheet.Range($"A{currentRow}:R{endRow}")
                        .CellsUsed()
                        .Select(c => new AddressMappingData
                        {
                            Row = Convert.ToInt32(digitsOnly.Replace(c.Address.ToString(), "")),
                            Value = c.Value.ToString(),
                            Address = c.Address.ToString()
                        }).ToList();

                        Dictionary<string, object> dataJsonResult = new Dictionary<string, object>();

                        var listGenerateColumn = new List<GenerateColumnJson>();

                        for (int i = 0; i < dataGroupCount; i++)
                        {
                            var generateColumn = new GenerateColumnJson();
                            foreach (var row in dataWeworkSheetGroup.Where(x => x.Row == i + Convert.ToInt32(currentRow)))
                            {
                                generateColumn.Row = row.Row;
                                generateColumn.Model = generateColumn.Model != null ? generateColumn.Model : row.Address.Contains("A") ? row.Value.Trim() : null;
                                generateColumn.GroupName = generateColumn.GroupName != null ? generateColumn.GroupName : row.Address.Contains("B") ? row.Value.Trim() : null;
                                generateColumn.Section = generateColumn.Section != null ? generateColumn.Section : row.Address.Contains("C") ? row.Value.Trim() : null;
                                generateColumn.Description = generateColumn.Description != null ? generateColumn.Description : row.Address.Contains("D") ? row.Value.Trim() : null;
                                generateColumn.Category = generateColumn.Category != null ? generateColumn.Category : row.Address.Contains("E") ? row.Value.Trim() : null;
                                generateColumn.Number250 = generateColumn.Number250 != null ? generateColumn.Number250 : row.Address.Contains("F") ? row.Value.Trim() : null;
                                generateColumn.Number500 = generateColumn.Number500 != null ? generateColumn.Number500 : row.Address.Contains("G") ? row.Value.Trim() : null;
                                generateColumn.Number1000 = generateColumn.Number1000 != null ? generateColumn.Number1000 : row.Address.Contains("H") ? row.Value.Trim() : null;
                                generateColumn.Number2000 = generateColumn.Number2000 != null ? generateColumn.Number2000 : row.Address.Contains("I") ? row.Value.Trim() : null;

                                if (generateColumn.Number4000 != null)
                                {
                                    generateColumn.Number4000 = generateColumn.Number4000 != null ? generateColumn.Number4000 : row.Address.Contains("J") ? row.Value.Trim() : null;
                                }
                                else
                                {
                                    generateColumn.Number4000 = generateColumn.Number2000 != null ? generateColumn.Number2000 : row.Address.Contains("J") ? row.Value.Trim() : null;
                                }

                                //generateColumn.Number4000 = generateColumn.Number4000 != null ? generateColumn.Number4000 : row.Address.Contains("J") ? row.Value.Trim() : null;
                                //generateColumn.Number4000 = generateColumn.Number4000 != null ? generateColumn.Number4000 : row.Address.Contains("J") ? row.Value.Trim() : null;

                                generateColumn.GuidTable = generateColumn.GuidTable != null ? generateColumn.GuidTable : row.Address.Contains("K") ? row.Value.Trim() : null;
                                generateColumn.ImageData = generateColumn.ImageData != null ? generateColumn.ImageData : row.Address.Contains("L") ? row.Value.Trim() : null;
                                generateColumn.Table = generateColumn.Table != null ? generateColumn.Table : row.Address.Contains("M") ? row.Value.Trim() : null;
                                generateColumn.ServiceMappingValue = generateColumn.ServiceMappingValue != null ? generateColumn.ServiceMappingValue : row.Address.Contains("N") ? row.Value.Trim() : null;
                                generateColumn.SOS = generateColumn.SOS != null ? generateColumn.SOS : row.Address.Contains("O") ? row.Value.Trim() : null;
                                generateColumn.SectionColumn = generateColumn.SectionColumn != null ? generateColumn.SectionColumn : row.Address.Contains("P") ? row.Value.Trim() : null;
                                generateColumn.TaskKey = generateColumn.TaskKey != null ? generateColumn.TaskKey : row.Address.Contains("Q") ? row.Value.Trim() : null;
                                generateColumn.GroupTaskId = generateColumn.GroupTaskId != null ? generateColumn.GroupTaskId : row.Address.Contains("R") ? row.Value.Trim() : null;
                            }

                            listGenerateColumn.Add(generateColumn);
                        }

                        #region Group Task Id
                        // CBM Populate Data
                        var dataColumnOrder = listGenerateColumn.Where(x => (x.Category == EnumCategoryServiceSheet.CBM ||
                                                                             x.Category == EnumCategoryServiceSheet.CBM_MANUAL ||
                                                                             x.Category == EnumCategoryServiceSheet.CBM_ADJUSTMENT ||
                                                                             x.Category == EnumCategoryServiceSheet.CBM_ADJUSTMENT_CARRY_ROLLER ||
                                                                             x.Category == EnumCategoryServiceSheet.CBM_NORMAL ||
                                                                             x.Category == EnumCategoryServiceSheet.CBM_CALCULATE ||
                                                                             x.Category == EnumCategoryServiceSheet.CBM_CALCULATE_RESULT ||
                                                                             x.Category == EnumCategoryServiceSheet.CBM_MANUAL_DOUBLE ||
                                                                             x.Category == EnumCategoryServiceSheet.CBM_CALIBRATION ||
                                                                             x.Category == EnumCategoryServiceSheet.CBM_BRAKE ||
                                                                             x.Category == EnumCategoryServiceSheet.DEFECT_MEASUREMENT) &&
                                                                            (x.Number4000.ToLower().Contains("a") ||
                                                                             x.Number4000.ToLower().Contains("a1") ||
                                                                             x.Number4000.ToLower().Contains("b1") ||
                                                                             x.Number4000.Contains("c1") ||
                                                                             x.Number4000.Contains("d1"))).ToList();
                        dataColumnOrder.ForEach(c => c.FirstRow = true);
                        //dataColumnOrder.ForEach(c => c.GroupTaskId = Guid.NewGuid().ToString());

                        // NORMAL Populate Data
                        var dataColumnOrderNormal = listGenerateColumn.Where(x => (x.Category == EnumCategoryServiceSheet.Defect || x.Category == EnumCategoryServiceSheet.Service) &&
                                                                                  x.Number4000.Contains("a")).ToList();
                        //dataColumnOrderNormal.ForEach(c => c.GroupTaskId = Guid.NewGuid().ToString());

                        // CRACK Populate Data
                        var dataColumnOrderCrack = listGenerateColumn.Where(x => (x.Category == EnumCategoryServiceSheet.Crack || x.Category == EnumCategoryServiceSheet.CRACK_SUBTASK) &&
                                                                                 (x.Number4000.ToLower().Contains("a") ||
                                                                                  x.Number4000.ToLower().Contains("a1") ||
                                                                                  x.Number4000.ToLower().Contains("b1") ||
                                                                                  x.Number4000.ToLower().Contains("c1") ||
                                                                                  x.Number4000.ToLower().Contains("d1") ||
                                                                                  x.Number4000.ToLower().Contains("e1") ||
                                                                                  x.Number4000.ToLower().Contains("f1") ||
                                                                                  x.Number4000.ToLower().Contains("g1") ||
                                                                                  x.Number4000.ToLower().Contains("h1") ||
                                                                                  x.Number4000.ToLower().Contains("i1") ||
                                                                                  x.Number4000.ToLower().Contains("j1") ||
                                                                                  x.Number4000.ToLower().Contains("k1") ||
                                                                                  x.Number4000.ToLower().Contains("l1") ||
                                                                                  x.Number4000.ToLower().Contains("m1") ||
                                                                                  x.Number4000.ToLower().Contains("n1"))).ToList();

                        dataColumnOrderCrack = listGenerateColumn.Where(x => x.Category == EnumCategoryServiceSheet.Crack &&
                                                                                 (x.Number4000.ToLower().Contains("a1") ||
                                                                                  x.Number4000.ToLower().Contains("b1") ||
                                                                                  x.Number4000.ToLower().Contains("c1") ||
                                                                                  x.Number4000.ToLower().Contains("d1") ||
                                                                                  x.Number4000.ToLower().Contains("e1") ||
                                                                                  x.Number4000.ToLower().Contains("f1") ||
                                                                                  x.Number4000.ToLower().Contains("g1") ||
                                                                                  x.Number4000.ToLower().Contains("h1") ||
                                                                                  x.Number4000.ToLower().Contains("i1") ||
                                                                                  x.Number4000.ToLower().Contains("j1") ||
                                                                                  x.Number4000.ToLower().Contains("k1") ||
                                                                                  x.Number4000.ToLower().Contains("l1") ||
                                                                                  x.Number4000.ToLower().Contains("m1") ||
                                                                                  x.Number4000.ToLower().Contains("n1"))).ToList();

                        dataColumnOrderCrack.ForEach(c => c.FirstRow = true);
                        //dataColumnOrderCrack.ForEach(c => c.GroupTaskId = Guid.NewGuid().ToString());

                        // Assigment GroupTaskId per Group CBM
                        foreach (var itemGroupTask in dataColumnOrder)
                        {
                            var input = itemGroupTask.Number4000;

                            //if (input.Length == 3)
                            //{
                            //    var dataColum = listGenerateColumn.Where(x => !string.IsNullOrEmpty(x.Number4000) &&
                            //                                                  x.Number4000.Length == 3 &&
                            //                                                  digitsOnly.Replace(x.Number4000, "").Substring(0, 2) == digitsOnly.Replace(itemGroupTask.Number4000, "").Substring(0, 2)).ToList();
                            //    dataColum.ForEach(c => c.GroupTaskId = itemGroupTask.GroupTaskId);
                            //}
                            if (input.Length == 4)
                            {
                                var dataColum = listGenerateColumn.Where(x => !string.IsNullOrEmpty(x.Number4000) &&
                                                                              x.Number4000.Length == 4 &&
                                                                              digitsOnly.Replace(x.Number4000, "").Substring(0, 2) == digitsOnly.Replace(itemGroupTask.Number4000, "").Substring(0, 2)).ToList();
                                //dataColum.ForEach(c => c.GroupTaskId = itemGroupTask.GroupTaskId);
                            }
                            else if (input.Length == 5)
                            {
                                var dataColum = listGenerateColumn.Where(x => !string.IsNullOrEmpty(x.Number4000) &&
                                                                              x.Number4000.Length == 5 &&
                                                                              digitsOnly.Replace(x.Number4000, "").Substring(0, 3) == digitsOnly.Replace(itemGroupTask.Number4000, "").Substring(0, 3)).ToList();
                                //dataColum.ForEach(c => c.GroupTaskId = itemGroupTask.GroupTaskId);
                            }
                            else
                            {
                                var dataColum = listGenerateColumn.Where(x => !string.IsNullOrEmpty(x.Number4000) &&
                                                                              digitsOnly.Replace(x.Number4000, "") == digitsOnly.Replace(itemGroupTask.Number4000, "")).ToList();
                                //dataColum.ForEach(c => c.GroupTaskId = itemGroupTask.GroupTaskId);
                            }
                        }

                        // Set Last Row CBM
                        var dataColumnOrderLast = listGenerateColumn.Where(x => x.Category == EnumCategoryServiceSheet.CBM ||
                                                                                x.Category == EnumCategoryServiceSheet.CBM_MANUAL ||
                                                                                x.Category == EnumCategoryServiceSheet.CBM_ADJUSTMENT ||
                                                                                x.Category == EnumCategoryServiceSheet.CBM_ADJUSTMENT_CARRY_ROLLER ||
                                                                                x.Category == EnumCategoryServiceSheet.CBM_NORMAL ||
                                                                                //x.Category == EnumCategoryServiceSheet.CBM_CALCULATE ||
                                                                                x.Category == EnumCategoryServiceSheet.CBM_CALCULATE_RESULT ||
                                                                                x.Category == EnumCategoryServiceSheet.CBM_MANUAL_DOUBLE ||
                                                                                x.Category == EnumCategoryServiceSheet.CBM_CALIBRATION ||
                                                                                x.Category == EnumCategoryServiceSheet.CBM_BRAKE ||
                                                                                x.Category == EnumCategoryServiceSheet.DEFECT_MEASUREMENT).OrderByDescending(x => x.Number4000).GroupBy(x => x.GroupTaskId).ToList();
                        foreach (var itemLast in dataColumnOrderLast)
                        {
                            var dataLastRow = itemLast.FirstOrDefault();
                            dataLastRow.LastRow = true;
                        }

                        // Assigment GroupTaskId per Group NORMAL
                        foreach (var itemGroupTaskNormal in dataColumnOrderNormal)
                        {
                            var input = itemGroupTaskNormal.Number4000;

                            if (input.Length == 3)
                            {
                                var dataColum = listGenerateColumn.Where(x => !string.IsNullOrEmpty(x.Number4000) &&
                                                                              x.Number4000.Length == 3 &&
                                                                              digitsOnly.Replace(x.Number4000, "").Substring(0, 2) == digitsOnly.Replace(itemGroupTaskNormal.Number4000, "").Substring(0, 2)).ToList();
                                //dataColum.ForEach(c => c.GroupTaskId = itemGroupTaskNormal.GroupTaskId);
                            }
                            else
                            {
                                var dataColum = listGenerateColumn.Where(x => !string.IsNullOrEmpty(x.Number4000) &&
                                                                          digitsOnly.Replace(x.Number4000, "") == digitsOnly.Replace(itemGroupTaskNormal.Number4000, "")).ToList();
                                //dataColum.ForEach(c => c.GroupTaskId = itemGroupTaskNormal.GroupTaskId);
                            }
                        }

                        // Assigment GroupTaskId per Group CRACK
                        foreach (var itemGroupTaskCrack in dataColumnOrderCrack)
                        {
                            if (itemGroupTaskCrack.Number4000.Length == 4)
                            {
                                var dataColum = listGenerateColumn.Where(x => !string.IsNullOrEmpty(x.Number4000) &&
                                                                         (x.Category == EnumCategoryServiceSheet.Crack || x.Category == EnumCategoryServiceSheet.CRACK_SUBTASK) && digitsOnly.Replace(x.Number4000, "").Substring(0, 3) == digitsOnly.Replace(itemGroupTaskCrack.Number4000, "").Substring(0, 3)).ToList();
                                //dataColum.ForEach(c => c.GroupTaskId = itemGroupTaskCrack.GroupTaskId);
                            }
                            else if (itemGroupTaskCrack.Number4000.Length == 5)
                            {
                                var dataColum = listGenerateColumn.Where(x => !string.IsNullOrEmpty(x.Number4000) &&
                                                                         (x.Category == EnumCategoryServiceSheet.Crack || x.Category == EnumCategoryServiceSheet.CRACK_SUBTASK) && digitsOnly.Replace(x.Number4000, "").Substring(0, 3) == digitsOnly.Replace(itemGroupTaskCrack.Number4000, "").Substring(0, 3)).ToList();
                                //dataColum.ForEach(c => c.GroupTaskId = itemGroupTaskCrack.GroupTaskId);
                            }
                            else
                            {
                                var dataColum = listGenerateColumn.Where(x => !string.IsNullOrEmpty(x.Number4000) &&
                                                                         (x.Category == EnumCategoryServiceSheet.Crack || x.Category == EnumCategoryServiceSheet.CRACK_SUBTASK) && digitsOnly.Replace(x.Number4000, "").Substring(0, 2) == digitsOnly.Replace(itemGroupTaskCrack.Number4000, "").Substring(0, 2)).ToList();
                                //dataColum.ForEach(c => c.GroupTaskId = itemGroupTaskCrack.GroupTaskId);
                            }
                        }

                        // Set Last Row CRACK
                        var dataColumnOrderCrackLast = listGenerateColumn.Where(x => x.Category == EnumCategoryServiceSheet.Crack || x.Category == EnumCategoryServiceSheet.CRACK_SUBTASK).OrderByDescending(x => x.Number4000).GroupBy(x => x.GroupTaskId).ToList();
                        foreach (var itemLast in dataColumnOrderCrackLast)
                        {
                            var dataLastRow = itemLast.FirstOrDefault();
                            dataLastRow.LastRow = true;
                        }

                        // NORMAL GROUP Populate Data
                        var dataColumnOrderNormalGroup = listGenerateColumn.Where(x => (x.Category == EnumCategoryServiceSheet.Defect_Group ||
                                                                                        x.Category == EnumCategoryServiceSheet.Service_With_Header) &&
                                                                                        x.Number4000.Contains("a")).ToList();
                        dataColumnOrderNormalGroup.ForEach(c => c.FirstRow = true);
                        //dataColumnOrderNormalGroup.ForEach(c => c.GroupTaskId = Guid.NewGuid().ToString());

                        // Assigment GroupTaskId per Defect Group
                        foreach (var itemGroupTask in dataColumnOrderNormalGroup)
                        {
                            var dataColum = listGenerateColumn.Where(x => !string.IsNullOrEmpty(x.Number4000) && digitsOnly.Replace(x.Number4000, "") == digitsOnly.Replace(itemGroupTask.Number4000, "")).ToList();
                            //dataColum.ForEach(c => c.GroupTaskId = itemGroupTask.GroupTaskId);
                        }

                        var dataColumnOrderNormalGroupLast = listGenerateColumn.Where(x => x.Category == EnumCategoryServiceSheet.Defect_Group ||
                                                                                            x.Category == EnumCategoryServiceSheet.Service_With_Header).OrderByDescending(x => x.Number4000).GroupBy(x => x.GroupTaskId).ToList();
                        foreach (var itemLast in dataColumnOrderNormalGroupLast)
                        {
                            var dataLastRow = itemLast.FirstOrDefault();
                            dataLastRow.LastRow = true;
                        }
                        #endregion

                        #region Get Config and Set Value Based on Excel File
                        foreach (var itemColumn in listGenerateColumn)
                        {
                            string category = string.Empty;
                            string rating = string.Empty;

                            switch (itemColumn.Category)
                            {
                                case "Section":
                                    category = EnumCategoryServiceSheet.NORMAL;

                                    if (itemColumn.SectionColumn == "4") { rating = EnumRatingServiceSheet.SECTION_COLUMN_4; }
                                    else if (itemColumn.SectionColumn == "5") { rating = EnumRatingServiceSheet.SECTION_COLUMN_5; }
                                    else if (itemColumn.SectionColumn == "6") { rating = EnumRatingServiceSheet.SECTION_COLUMN_6; }
                                    else { rating = EnumRatingServiceSheet.SECTION; }

                                    break;
                                case "Defect":
                                    category = EnumCategoryServiceSheet.NORMAL;
                                    rating = EnumRatingServiceSheet.NO;
                                    break;
                                case "Defect_Group":
                                    category = EnumCategoryServiceSheet.NORMAL;
                                    rating = EnumRatingServiceSheet.NO_GROUPING;

                                    if (itemColumn.FirstRow)
                                    {
                                        var paramCbmHeader = new Dictionary<string, object>();
                                        paramCbmHeader.Add("category", category);
                                        paramCbmHeader.Add("rating", EnumRatingServiceSheet.NO_GROUP_HEADER);

                                        resultHeaderDefectGroup = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);
                                    }
                                    else
                                    {
                                        resultHeaderDefectGroup = null;
                                    }

                                    break;
                                case "DEFECT_MEASUREMENT":
                                    category = EnumCategoryServiceSheet.NORMAL;
                                    rating = EnumRatingServiceSheet.DEFECT_MEASUREMENT;

                                    if (itemColumn.FirstRow)
                                    {
                                        var paramCbmHeader = new Dictionary<string, object>();
                                        paramCbmHeader.Add("category", category);
                                        paramCbmHeader.Add("rating", EnumRatingServiceSheet.DEFECT_MEASUREMENT_HEADER);

                                        resultHeaderDefectGroup = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);
                                    }
                                    else
                                    {
                                        resultHeaderDefectGroup = null;
                                    }

                                    break;
                                case "CBM":
                                    category = EnumCategoryServiceSheet.CBM;
                                    rating = EnumRatingServiceSheet.AUTOMATIC;

                                    if (itemColumn.FirstRow)
                                    {
                                        var paramCbmHeader = new Dictionary<string, object>();
                                        paramCbmHeader.Add("category", category);
                                        paramCbmHeader.Add("rating", EnumRatingServiceSheet.AUTOMATIC_HEADER);

                                        resultHeaderCbmAutomatic = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);
                                    }
                                    else
                                    {
                                        resultHeaderCbmAutomatic = null;
                                    }

                                    break;
                                case "CBM_MANUAL":
                                    category = EnumCategoryServiceSheet.CBM;
                                    rating = EnumRatingServiceSheet.MANUAL;

                                    if (itemColumn.FirstRow)
                                    {
                                        var paramCbmHeader = new Dictionary<string, object>();
                                        paramCbmHeader.Add("category", category);
                                        paramCbmHeader.Add("rating", EnumRatingServiceSheet.MANUAL_HEADER);

                                        resultHeaderCbmAutomatic = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);
                                    }
                                    else
                                    {
                                        resultHeaderCbmAutomatic = null;
                                    }

                                    break;
                                case "CBM_MANUAL_DOUBLE":
                                    category = EnumCategoryServiceSheet.CBM;
                                    rating = EnumRatingServiceSheet.MANUAL_DOUBLE;

                                    if (itemColumn.FirstRow)
                                    {
                                        var paramCbmHeader = new Dictionary<string, object>();
                                        paramCbmHeader.Add("category", category);
                                        paramCbmHeader.Add("rating", EnumRatingServiceSheet.MANUAL_DOUBLE_HEADER);

                                        resultHeaderCbmAutomatic = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);
                                    }
                                    else
                                    {
                                        resultHeaderCbmAutomatic = null;
                                    }

                                    break;
                                case "CBM_ADJUSTMENT":
                                    category = EnumCategoryServiceSheet.CBM;
                                    rating = EnumRatingServiceSheet.AUTOMATIC_ADJUSTMENT;

                                    if (itemColumn.FirstRow)
                                    {
                                        var paramCbmHeader = new Dictionary<string, object>();
                                        paramCbmHeader.Add("category", category);
                                        paramCbmHeader.Add("rating", EnumRatingServiceSheet.AUTOMATIC_HEADER);

                                        resultHeaderCbmAutomatic = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);
                                    }
                                    else
                                    {
                                        resultHeaderCbmAutomatic = null;
                                    }

                                    break;
                                case "CBM_ADJUSTMENT_CARRY_ROLLER":
                                    category = EnumCategoryServiceSheet.CBM;
                                    rating = EnumRatingServiceSheet.AUTOMATIC_ADJUSTMENT_CARRY_ROLLER;

                                    if (itemColumn.FirstRow)
                                    {
                                        var paramCbmHeader = new Dictionary<string, object>();
                                        paramCbmHeader.Add("category", category);
                                        paramCbmHeader.Add("rating", EnumRatingServiceSheet.AUTOMATIC_HEADER);

                                        resultHeaderCbmAutomatic = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);


                                        var paramCbmHeaderRoller = new Dictionary<string, object>();
                                        paramCbmHeaderRoller.Add("category", category);
                                        paramCbmHeaderRoller.Add("rating", EnumRatingServiceSheet.AUTOMATIC_HEADER_CARRY_ROLLER);

                                        resultHeaderCbmAutomaticRoller = await _repositoryGenerateJson.GetDataByParam(paramCbmHeaderRoller);
                                    }
                                    else
                                    {
                                        resultHeaderCbmAutomatic = null;
                                    }

                                    break;
                                case "CBM_NORMAL":
                                    category = EnumCategoryServiceSheet.CBM;
                                    rating = EnumRatingServiceSheet.NORMAL;

                                    if (itemColumn.FirstRow)
                                    {
                                        var paramCbmHeader = new Dictionary<string, object>();
                                        paramCbmHeader.Add("category", category);
                                        paramCbmHeader.Add("rating", EnumRatingServiceSheet.NORMAL_HEADER);

                                        resultHeaderCbmAutomatic = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);
                                    }
                                    else
                                    {
                                        resultHeaderCbmAutomatic = null;
                                    }

                                    break;
                                case "CBM_CALCULATE":
                                    category = EnumCategoryServiceSheet.CBM;
                                    rating = EnumRatingServiceSheet.CALCULATE_AVG;

                                    if (itemColumn.FirstRow)
                                    {
                                        var paramCbmHeader = new Dictionary<string, object>();
                                        paramCbmHeader.Add("category", category);
                                        paramCbmHeader.Add("rating", EnumRatingServiceSheet.AUTOMATIC_HEADER);

                                        resultHeaderCbmAutomatic = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);
                                    }
                                    else
                                    {
                                        resultHeaderCbmAutomatic = null;
                                    }

                                    break;
                                case "CBM_CALCULATE_RESULT":
                                    category = EnumCategoryServiceSheet.CBM;
                                    rating = EnumRatingServiceSheet.CALCULATE_AVG_RESULT;

                                    break;
                                case "CBM_DESC":
                                    category = EnumCategoryServiceSheet.NORMAL_DESC;
                                    rating = EnumRatingServiceSheet.NO;
                                    break;
                                case "CBM_CALIBRATION":
                                    category = EnumCategoryServiceSheet.CBM;
                                    rating = EnumRatingServiceSheet.CALIBRATION;

                                    if (itemColumn.FirstRow)
                                    {
                                        var paramCbmHeader = new Dictionary<string, object>();
                                        paramCbmHeader.Add("category", category);
                                        paramCbmHeader.Add("rating", EnumRatingServiceSheet.CALIBRATION_HEADER);

                                        resultHeaderCbmAutomatic = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);
                                    }
                                    else
                                    {
                                        resultHeaderCbmAutomatic = null;
                                    }

                                    break;
                                case "CBM_BRAKE":
                                    category = EnumCategoryServiceSheet.CBM;
                                    rating = EnumRatingServiceSheet.BRAKE;

                                    if (itemColumn.FirstRow)
                                    {
                                        var paramCbmHeader = new Dictionary<string, object>();
                                        paramCbmHeader.Add("category", category);
                                        paramCbmHeader.Add("rating", EnumRatingServiceSheet.BRAKE_HEADER);

                                        resultHeaderCbmAutomatic = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);
                                    }
                                    else
                                    {
                                        resultHeaderCbmAutomatic = null;
                                    }

                                    break;
                                case "NORMAL_DESC":
                                    category = EnumCategoryServiceSheet.NORMAL_DESC;
                                    rating = EnumRatingServiceSheet.NO;
                                    break;
                                case "Service":
                                    category = EnumCategoryServiceSheet.NORMAL_WITH_COMMENT;
                                    rating = EnumRatingServiceSheet.NO;
                                    break;
                                case "Service_Mapping":
                                    category = EnumCategoryServiceSheet.NORMAL_WITH_MAPPING;
                                    rating = EnumRatingServiceSheet.NO;
                                    break;
                                case "Service_Input":
                                    category = EnumCategoryServiceSheet.NORMAL_WITH_INPUT;
                                    rating = EnumRatingServiceSheet.NO;
                                    break;
                                case "Service_With_Header":
                                    category = EnumCategoryServiceSheet.NORMAL_WITH_HEADER;
                                    rating = EnumRatingServiceSheet.NO;

                                    if (itemColumn.FirstRow)
                                    {
                                        var paramCbmHeader = new Dictionary<string, object>();
                                        paramCbmHeader.Add("category", EnumCategoryServiceSheet.NORMAL_WITH_HEADER);
                                        paramCbmHeader.Add("rating", EnumRatingServiceSheet.HEADER);

                                        resultHeaderDefectGroup = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);
                                    }
                                    else
                                    {
                                        resultHeaderDefectGroup = null;
                                    }

                                    break;
                                case "Crack":
                                    category = EnumCategoryServiceSheet.CRACK;
                                    rating = EnumRatingServiceSheet.BLANK;
                                    break;
                                case "CRACK_NON_GROUP":
                                    category = EnumCategoryServiceSheet.CRACK;
                                    rating = EnumRatingServiceSheet.NON_GROUP;
                                    break;
                                case "CRACK_SUBTASK":
                                    category = EnumCategoryServiceSheet.CRACK;
                                    rating = EnumRatingServiceSheet.SUBTASK;
                                    break;
                                case "INFO_SECTION":
                                    category = EnumCategoryServiceSheet.NOTE;
                                    rating = EnumRatingServiceSheet.CRACK_SECTION;
                                    break;
                                case "Note":
                                    category = EnumCategoryServiceSheet.NOTE;
                                    rating = EnumRatingServiceSheet.NORMAL;
                                    break;
                                case "SKIP_PRESERVICE":
                                    category = EnumCategoryServiceSheet.NOTE;
                                    rating = EnumRatingServiceSheet.SKIP_PRESERVICE;
                                    break;
                                case "NORMAL_INPUT":
                                    category = "NORMAL";
                                    rating = "INPUT";
                                    break;
                                case "BORDER_DESC":
                                    category = EnumCategoryServiceSheet.NORMAL_DESC_BORDER;
                                    rating = EnumRatingServiceSheet.NO;
                                    break;
                            }

                            var param = new Dictionary<string, object>();
                            param.Add("category", category);
                            param.Add("rating", rating);

                            var dataJson = await _repositoryGenerateJson.GetDataByParam(param);

                            if (dataJson != null)
                            {
                                dynamic res = dataJson.content;
                                dynamic resCbmHeader = resultHeaderCbmAutomatic == null ? null : resultHeaderCbmAutomatic.content;
                                dynamic resCbmHeaderRoller = resultHeaderCbmAutomaticRoller == null ? null : resultHeaderCbmAutomaticRoller.content;

                                if (resCbmHeader != null)
                                {
                                    resCbmHeader.key = Guid.NewGuid().ToString();
                                    resCbmHeader.description250 = $"{itemColumn.Number250};;{itemColumn.Description}";
                                    resCbmHeader.description500 = $"{itemColumn.Number500};;{itemColumn.Description}";
                                    resCbmHeader.description1000 = $"{itemColumn.Number1000};;{itemColumn.Description}";
                                    resCbmHeader.description2000 = $"{itemColumn.Number2000};;{itemColumn.Description}";
                                    resCbmHeader.description4000 = $"{itemColumn.Number4000};;{itemColumn.Description}";

                                    foreach (var itemCbmHeader in resCbmHeader.items)
                                    {
                                        itemCbmHeader.key = Guid.NewGuid().ToString();
                                    }
                                }

                                if (resCbmHeaderRoller != null)
                                {
                                    resCbmHeaderRoller.key = Guid.NewGuid().ToString();

                                    foreach (var itemCbmHeaderRoller in resCbmHeaderRoller.items)
                                    {
                                        itemCbmHeaderRoller.key = Guid.NewGuid().ToString();
                                    }
                                }

                                dynamic resDefectGroupHeader = resultHeaderDefectGroup == null ? null : resultHeaderDefectGroup.content;

                                if (resDefectGroupHeader != null)
                                {
                                    resDefectGroupHeader.SectionData = itemColumn.Section;
                                    resDefectGroupHeader.key = Guid.NewGuid().ToString();
                                    resDefectGroupHeader.description250 = $"{itemColumn.Number250};;{itemColumn.Description}";
                                    resDefectGroupHeader.description500 = $"{itemColumn.Number500};;{itemColumn.Description}";
                                    resDefectGroupHeader.description1000 = $"{itemColumn.Number1000};;{itemColumn.Description}";
                                    resDefectGroupHeader.description2000 = $"{itemColumn.Number2000};;{itemColumn.Description}";
                                    resDefectGroupHeader.description4000 = $"{itemColumn.Number4000};;{itemColumn.Description}";

                                    foreach (var itemDefectGroupHeader in resDefectGroupHeader.items)
                                    {
                                        itemDefectGroupHeader.key = Guid.NewGuid().ToString();
                                    }
                                }

                                var detaiNumberData = charOnly.Matches(itemColumn.Number2000 == null ? string.Empty : itemColumn.Number2000).FirstOrDefault()?.Value;
                                var detailDescription = itemColumn.Description;

                                if (itemColumn.Description != null)
                                {
                                    if (detailDescription.Contains("a1. "))
                                    {
                                        //detailDescription = detailDescription.Substring(0, 4) == "a1. " ? detailDescription.Replace("a1. ", "") : detailDescription; 
                                        detailDescription = detailDescription.Substring(0, 4) == "a1. " ? detailDescription.Remove(0, 4) : detailDescription;
                                    }
                                    else if (detailDescription.Contains("a2. "))
                                    {
                                        //detailDescription = detailDescription.Substring(0, 4) == "a2. " ? detailDescription.Replace("a2. ", "") : detailDescription; 
                                        detailDescription = detailDescription.Substring(0, 4) == "a2. " ? detailDescription.Remove(0, 4) : detailDescription;
                                    }
                                    else if (detailDescription.Contains("b1. "))
                                    {
                                        //detailDescription = detailDescription.Substring(0, 4) == "b1. " ? detailDescription.Replace("b1. ", "") : detailDescription; 
                                        detailDescription = detailDescription.Substring(0, 4) == "b1. " ? detailDescription.Remove(0, 4) : detailDescription;
                                    }
                                    else if (detailDescription.Contains("b2. "))
                                    {
                                        //detailDescription = detailDescription.Substring(0, 4) == "b2. " ? detailDescription.Replace("b2. ", "") : detailDescription; 
                                        detailDescription = detailDescription.Substring(0, 4) == "b2. " ? detailDescription.Remove(0, 4) : detailDescription;
                                    }
                                    else if (detailDescription.Contains("a. "))
                                    {
                                        //detailDescription = detailDescription.Substring(0, 3) == "a. " ? detailDescription.Replace("a. ", "") : detailDescription; 
                                        detailDescription = detailDescription.Substring(0, 3) == "a. " ? detailDescription.Remove(0, 3) : detailDescription;
                                    }
                                    else if (detailDescription.Contains("b. "))
                                    {
                                        //detailDescription = detailDescription.Substring(0, 3) == "b. " ? detailDescription.Replace("b. ", "") : detailDescription; 
                                        detailDescription = detailDescription.Substring(0, 3) == "b. " ? detailDescription.Remove(0, 3) : detailDescription;
                                    }
                                    else if (detailDescription.Contains("c. "))
                                    {
                                        //detailDescription = detailDescription.Substring(0, 3) == "c. " ? detailDescription.Replace("c. ", "") : detailDescription; 
                                        detailDescription = detailDescription.Substring(0, 3) == "c. " ? detailDescription.Remove(0, 3) : detailDescription;
                                    }
                                    else if (detailDescription.Contains("d. "))
                                    {
                                        //detailDescription = detailDescription.Substring(0, 3) == "d. " ? detailDescription.Replace("d. ", "") : detailDescription; 
                                        detailDescription = detailDescription.Substring(0, 3) == "d. " ? detailDescription.Remove(0, 3) : detailDescription;
                                    }
                                    else if (detailDescription.Contains("e. "))
                                    {
                                        //detailDescription = detailDescription.Substring(0, 3) == "e. " ? detailDescription.Replace("e. ", "") : detailDescription; 
                                        detailDescription = detailDescription.Substring(0, 3) == "e. " ? detailDescription.Remove(0, 3) : detailDescription;
                                    }
                                    else if (detailDescription.Contains("f. "))
                                    {
                                        //detailDescription = detailDescription.Substring(0, 3) == "f. " ? detailDescription.Replace("f. ", "") : detailDescription; 
                                        detailDescription = detailDescription.Substring(0, 3) == "f. " ? detailDescription.Remove(0, 3) : detailDescription;
                                    }
                                    else if (detailDescription.Contains("g. "))
                                    {
                                        //detailDescription = detailDescription.Substring(0, 3) == "g. " ? detailDescription.Replace("g. ", "") : detailDescription; 
                                        detailDescription = detailDescription.Substring(0, 3) == "g. " ? detailDescription.Remove(0, 3) : detailDescription;
                                    }
                                    else if (detailDescription.Contains("h. "))
                                    {
                                        //detailDescription = detailDescription.Substring(0, 3) == "h. " ? detailDescription.Replace("h. ", "") : detailDescription; 
                                        detailDescription = detailDescription.Substring(0, 3) == "h. " ? detailDescription.Remove(0, 3) : detailDescription;
                                    }
                                    else if (detailDescription.Contains("i. "))
                                    {
                                        //detailDescription = detailDescription.Substring(0, 3) == "i. " ? detailDescription.Replace("i. ", "") : detailDescription; 
                                        detailDescription = detailDescription.Substring(0, 3) == "i. " ? detailDescription.Remove(0, 3) : detailDescription;
                                    }
                                    else if (detailDescription.Contains("j. "))
                                    {
                                        //detailDescription = detailDescription.Substring(0, 3) == "j. " ? detailDescription.Replace("j. ", "") : detailDescription; 
                                        detailDescription = detailDescription.Substring(0, 3) == "j. " ? detailDescription.Remove(0, 3) : detailDescription;
                                    }
                                    else if (detailDescription.Contains("k. "))
                                    {
                                        //detailDescription = detailDescription.Substring(0, 3) == "k. " ? detailDescription.Replace("k. ", "") : detailDescription; 
                                        detailDescription = detailDescription.Substring(0, 3) == "k. " ? detailDescription.Remove(0, 3) : detailDescription;
                                    }
                                    else if (detailDescription.Contains("l. "))
                                    {
                                        //detailDescription = detailDescription.Substring(0, 3) == "l. " ? detailDescription.Replace("l. ", "") : detailDescription; 
                                        detailDescription = detailDescription.Substring(0, 3) == "l. " ? detailDescription.Remove(0, 3) : detailDescription;
                                    }
                                    else if (detailDescription.Contains("m. "))
                                    {
                                        //detailDescription = detailDescription.Substring(0, 3) == "m. " ? detailDescription.Replace("m. ", "") : detailDescription; 
                                        detailDescription = detailDescription.Substring(0, 3) == "m. " ? detailDescription.Remove(0, 3) : detailDescription;
                                    }
                                }

                                res.SectionData = itemColumn.Section;
                                if (!string.IsNullOrEmpty(itemColumn.TaskKey))
                                {
                                    res.key = itemColumn.TaskKey;
                                }
                                else
                                {
                                    res.key = Guid.NewGuid().ToString();
                                }

                                res.groupTaskId = itemColumn.GroupTaskId == null ? string.Empty : itemColumn.GroupTaskId;

                                //res.description = $"{itemColumn.Number4000};;{itemColumn.Description}";
                                //res.description250 = $"{itemColumn.Number250};;{itemColumn.Description}";
                                //res.description500 = $"{itemColumn.Number500};;{itemColumn.Description}";
                                //res.description1000 = $"{itemColumn.Number1000};;{itemColumn.Description}";
                                ////res.description2000 = $"{itemColumn.Number2000};;{itemColumn.Description}";
                                //res.description2000 = $"{charOnly.Replace(itemColumn.Number2000 == null ? string.Empty : itemColumn.Number2000, "#").Split("#")[0]};{detaiNumberData};{itemColumn.Description}";
                                //res.description4000 = $"{itemColumn.Number4000};;{itemColumn.Description}";

                                //res.description250 = $"{charOnly.Replace(itemColumn.Number250 == null ? string.Empty : itemColumn.Number250, "#").Split("#")[0]};{detaiNumberData};{itemColumn.Description}";
                                //res.description500 = $"{charOnly.Replace(itemColumn.Number500 == null ? string.Empty : itemColumn.Number500, "#").Split("#")[0]};{detaiNumberData};{itemColumn.Description}";
                                //res.description1000 = $"{charOnly.Replace(itemColumn.Number1000 == null ? string.Empty : itemColumn.Number1000, "#").Split("#")[0]};{detaiNumberData};{itemColumn.Description}";
                                //res.description2000 = $"{charOnly.Replace(itemColumn.Number2000 == null ? string.Empty : itemColumn.Number2000, "#").Split("#")[0]};{detaiNumberData};{detailDescription}";
                                //res.description4000 = $"{charOnly.Replace(itemColumn.Number4000 == null ? string.Empty : itemColumn.Number4000, "#").Split("#")[0]};{detaiNumberData};{itemColumn.Description}";

                                res.description250 = $"{charOnly.Replace(itemColumn.Number250 == null ? string.Empty : itemColumn.Number250, "#").Split("#")[0]};{detaiNumberData};{detailDescription}";
                                res.description500 = $"{charOnly.Replace(itemColumn.Number500 == null ? string.Empty : itemColumn.Number500, "#").Split("#")[0]};{detaiNumberData};{detailDescription}";
                                res.description1000 = $"{charOnly.Replace(itemColumn.Number1000 == null ? string.Empty : itemColumn.Number1000, "#").Split("#")[0]};{detaiNumberData};{detailDescription}";
                                res.description2000 = $"{charOnly.Replace(itemColumn.Number2000 == null ? string.Empty : itemColumn.Number2000, "#").Split("#")[0]};{detaiNumberData};{detailDescription}";
                                res.description4000 = $"{charOnly.Replace(itemColumn.Number4000 == null ? string.Empty : itemColumn.Number4000, "#").Split("#")[0]};{detaiNumberData};{detailDescription}";

                                if (itemColumn.Category == EnumCategoryServiceSheet.CBM_ADJUSTMENT) { res.adjustment.key = Guid.NewGuid().ToString(); }

                                string mappingKeyIdData = string.Empty;
                                foreach (var resItem in res.items)
                                {
                                    resItem.key = Guid.NewGuid().ToString();
                                    if (resItem.categoryItemType != null && resItem.categoryItemType == EnumCommonProperty.MappingParamKey)
                                    {
                                        mappingKeyIdData = resItem.key;
                                    }

                                    if (itemColumn.Category != EnumCategoryServiceSheet.CBM_CALCULATE && resItem.mappingKeyId != null && resItem.mappingKeyId == EnumCommonProperty.MappingParamKeyTag)
                                    {
                                        resItem.mappingKeyId = mappingKeyIdData;
                                    }

                                    if (resItem.isTaskValue != null && resItem.isTaskValue == "true")
                                    {
                                        res.valueDisabled = resItem.key;
                                        valueDisabledBefore = resItem.key;
                                    }

                                    if (itemColumn.Category == EnumCategoryServiceSheet.CBM_CALCULATE_RESULT && resItem.categoryItemType != null && resItem.categoryItemType == EnumCommonProperty.ResultParamRating)
                                    {
                                        //var takeOutChar = charOnly.Replace(itemColumn.Number4000, "#");
                                        //var splitNumber = takeOutChar.Split('#');

                                        List<dynamic> calculateAvg = psType4000.Where(x => x.typeCategory == EnumCommonProperty.CalculateAvg
                                                                                       //digitsOnly.Replace(x.value4000, "").Substring(0, 2) == digitsOnly.Replace(itemColumn.Number4000, "").Substring(0, 2)
                                                                                       //charOnly.Replace(x.Number4000, "#").Split("#")[0] == charOnly.Replace(itemColumn.Number4000, "#").Split("#").FirstOrDefault()
                                                                                       ).ToList();

                                        List<string> mappingKeys = new List<string>();

                                        // Target Calculate Key Id
                                        foreach (var itemCalculate in calculateAvg)
                                        {
                                            foreach (var items in itemCalculate.items)
                                            {
                                                if (items.targetCalculateKeyId != null)
                                                {
                                                    items.targetCalculateKeyId = resItem.key;
                                                }

                                                if (items.category == EnumCommonProperty.CbmCalculateAvg)
                                                {
                                                    mappingKeys.Add(items.key.ToString());
                                                }
                                            }
                                        }

                                        // mappingKeyId
                                        foreach (var itemCalculate in calculateAvg)
                                        {
                                            foreach (var items in itemCalculate.items)
                                            {
                                                if (items.category == EnumCommonProperty.CbmCalculateAvg)
                                                {
                                                    items.mappingKeyId = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(mappingKeys));
                                                }
                                            }
                                        }

                                        //resItem.mappingKeyId = mappingKeyIdData;
                                    }

                                    switch (resItem.value.ToString())
                                    {
                                        case "<<itemDetailNumber>>":
                                            if (!string.IsNullOrEmpty(itemColumn.Number250)) { resItem.value250 = itemColumn.Number250; }
                                            if (!string.IsNullOrEmpty(itemColumn.Number500)) { resItem.value500 = itemColumn.Number500; }
                                            if (!string.IsNullOrEmpty(itemColumn.Number1000)) { resItem.value1000 = itemColumn.Number1000; }
                                            if (!string.IsNullOrEmpty(itemColumn.Number2000)) { resItem.value2000 = itemColumn.Number2000; }
                                            if (!string.IsNullOrEmpty(itemColumn.Number4000)) { resItem.value4000 = itemColumn.Number4000; }

                                            break;
                                        case "<<itemNumber>>":
                                            #region Numbering 250
                                            if (!string.IsNullOrEmpty(itemColumn.Number250))
                                            {
                                                if (charOnly.IsMatch(itemColumn.Number250))
                                                {
                                                    if (itemColumn.Category == EnumCategoryServiceSheet.CBM ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_MANUAL ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_ADJUSTMENT ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_ADJUSTMENT_CARRY_ROLLER ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_NORMAL ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.Defect_Group ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.DEFECT_MEASUREMENT ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_CALCULATE ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_CALCULATE_RESULT ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_MANUAL_DOUBLE ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_CALIBRATION ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_BRAKE)
                                                    {
                                                        var input = itemColumn.Number250;
                                                        if (input.Length == 2)
                                                        {
                                                            var pattern = new Regex(@"\d{1}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value250 = resulttt.Replace(itemColumn.Number250, "");
                                                        }
                                                        else if (input.Length == 3 || input.Length == 4)
                                                        {
                                                            var takeOutChar = charOnly.Replace(itemColumn.Number250, "#");
                                                            var splitNumber = takeOutChar.Split('#');

                                                            if (splitNumber.Length > 0 && splitNumber[0].Length == 3)
                                                            {
                                                                var pattern = new Regex(@"\d{3}");
                                                                var resulttt = pattern.Replace(input, "");

                                                                resItem.value250 = resulttt.Replace(itemColumn.Number250, "");
                                                            }
                                                            else
                                                            {
                                                                var pattern = new Regex(@"\d{2}");
                                                                var resulttt = pattern.Replace(input, "");

                                                                resItem.value250 = resulttt.Replace(itemColumn.Number250, "");
                                                            }
                                                        }
                                                        else
                                                        {
                                                            var pattern = new Regex(@"\d{3}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value250 = resulttt.Replace(itemColumn.Number250, "");
                                                        }

                                                        res.description250 = $"{charOnly.Replace(itemColumn.Number250 == null ? string.Empty : itemColumn.Number250, "#").Split("#")[0]};{resItem.value250};{detailDescription}";
                                                    }
                                                    else if (itemColumn.Category == "Crack")
                                                    {
                                                        var input = itemColumn.Number250;
                                                        if (input.Length == 2)
                                                        {
                                                            var pattern = new Regex(@"\d{1}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value250 = resulttt.Replace(itemColumn.Number250, "");
                                                        }
                                                        else if (input.Length == 3 || input.Length == 4)
                                                        {
                                                            var pattern = new Regex(@"\d{2}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value250 = resulttt.Replace(itemColumn.Number250, "");
                                                        }
                                                        else
                                                        {
                                                            var pattern = new Regex(@"\d{3}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            var firstCrack = resulttt.Replace(itemColumn.Number250, "");
                                                            if (firstCrack.Contains("2"))
                                                            {
                                                                resItem.style.visibility = "hidden";
                                                            }

                                                            var valueNumber = itemColumn.Number250;

                                                            if (valueNumber.Contains("1"))
                                                            {
                                                                valueNumber = valueNumber.Substring(0, 3);
                                                            }
                                                            resItem.value250 = valueNumber;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        resItem.value250 = itemColumn.Number250;
                                                        resItem.style.visibility = "hidden";
                                                    }
                                                }
                                                else
                                                {
                                                    resItem.value250 = itemColumn.Number250;
                                                }
                                            }
                                            #endregion

                                            #region Numbering 500
                                            if (!string.IsNullOrEmpty(itemColumn.Number500))
                                            {
                                                if (charOnly.IsMatch(itemColumn.Number500))
                                                {
                                                    if (itemColumn.Category == EnumCategoryServiceSheet.CBM ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_MANUAL ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_ADJUSTMENT ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_ADJUSTMENT_CARRY_ROLLER ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_NORMAL ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.Defect_Group ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.DEFECT_MEASUREMENT ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_CALCULATE ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_CALCULATE_RESULT ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_MANUAL_DOUBLE ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_CALIBRATION ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_BRAKE)
                                                    {
                                                        var input = itemColumn.Number500;
                                                        if (input.Length == 2)
                                                        {
                                                            var pattern = new Regex(@"\d{1}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value500 = resulttt.Replace(itemColumn.Number500, "");
                                                        }
                                                        else if (input.Length == 3 || input.Length == 4)
                                                        {
                                                            var takeOutChar = charOnly.Replace(itemColumn.Number500, "#");
                                                            var splitNumber = takeOutChar.Split('#');

                                                            if (splitNumber.Length > 0 && splitNumber[0].Length == 3)
                                                            {
                                                                var pattern = new Regex(@"\d{3}");
                                                                var resulttt = pattern.Replace(input, "");

                                                                resItem.value500 = resulttt.Replace(itemColumn.Number500, "");
                                                            }
                                                            else
                                                            {
                                                                var pattern = new Regex(@"\d{2}");
                                                                var resulttt = pattern.Replace(input, "");

                                                                resItem.value500 = resulttt.Replace(itemColumn.Number500, "");
                                                            }
                                                        }
                                                        else
                                                        {
                                                            var pattern = new Regex(@"\d{3}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value500 = resulttt.Replace(itemColumn.Number500, "");
                                                        }

                                                        res.description500 = $"{charOnly.Replace(itemColumn.Number500 == null ? string.Empty : itemColumn.Number500, "#").Split("#")[0]};{resItem.value500};{detailDescription}";
                                                    }
                                                    else if (itemColumn.Category == "Crack")
                                                    {
                                                        var input = itemColumn.Number500;
                                                        var value500 = "";
                                                        if (input.Length == 2)
                                                        {
                                                            var pattern = new Regex(@"\d{1}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value500 = resulttt.Replace(itemColumn.Number500, "");
                                                        }
                                                        else if (input.Length == 3 || input.Length == 4)
                                                        {
                                                            var pattern = new Regex(@"\d{2}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value500 = resulttt.Replace(itemColumn.Number500, "");
                                                        }
                                                        else
                                                        {
                                                            var pattern = new Regex(@"\d{3}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            var firstCrack = resulttt.Replace(itemColumn.Number500, "");
                                                            if (firstCrack.Contains("2"))
                                                            {
                                                                resItem.style.visibility = "hidden";
                                                            }

                                                            var valueNumber = itemColumn.Number500;

                                                            if (valueNumber.Contains("1"))
                                                            {
                                                                valueNumber = valueNumber.Substring(0, 3);
                                                            }
                                                            resItem.value500 = valueNumber;
                                                            value500 = resulttt.Replace(itemColumn.Number500, "");
                                                        }
                                                        res.description500 = $"{charOnly.Replace(itemColumn.Number500 == null ? string.Empty : itemColumn.Number500, "#").Split("#")[0]};{value500};{detailDescription}";
                                                    }
                                                    else
                                                    {
                                                        resItem.value500 = itemColumn.Number500;
                                                        resItem.style.visibility = "hidden";
                                                    }
                                                }
                                                else
                                                {
                                                    resItem.value500 = itemColumn.Number500;
                                                }
                                            }
                                            #endregion

                                            #region Numbering 1000
                                            if (!string.IsNullOrEmpty(itemColumn.Number1000))
                                            {
                                                if (charOnly.IsMatch(itemColumn.Number1000))
                                                {
                                                    if (itemColumn.Category == EnumCategoryServiceSheet.CBM ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_MANUAL ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_ADJUSTMENT ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_ADJUSTMENT_CARRY_ROLLER ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_NORMAL ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.Defect_Group ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.DEFECT_MEASUREMENT ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_CALCULATE ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_CALCULATE_RESULT ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_MANUAL_DOUBLE ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_CALIBRATION ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_BRAKE)
                                                    {
                                                        var input = itemColumn.Number1000;
                                                        if (input.Length == 2)
                                                        {
                                                            var pattern = new Regex(@"\d{1}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value1000 = resulttt.Replace(itemColumn.Number1000, "");
                                                        }
                                                        else if (input.Length == 3 || input.Length == 4)
                                                        {
                                                            var takeOutChar = charOnly.Replace(itemColumn.Number1000, "#");

                                                            var splitNumber = takeOutChar.Split('#');

                                                            if (splitNumber.Length > 0 && splitNumber[0].Length == 3)
                                                            {
                                                                var pattern = new Regex(@"\d{3}");
                                                                var resulttt = pattern.Replace(input, "");

                                                                resItem.value1000 = resulttt.Replace(itemColumn.Number1000, "");
                                                            }
                                                            else
                                                            {
                                                                var pattern = new Regex(@"\d{2}");
                                                                var resulttt = pattern.Replace(input, "");

                                                                resItem.value1000 = resulttt.Replace(itemColumn.Number1000, "");
                                                            }
                                                        }
                                                        else
                                                        {
                                                            var pattern = new Regex(@"\d{3}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value1000 = resulttt.Replace(itemColumn.Number1000, "");
                                                        }

                                                        res.description1000 = $"{charOnly.Replace(itemColumn.Number1000 == null ? string.Empty : itemColumn.Number1000, "#").Split("#")[0]};{resItem.value1000};{detailDescription}";
                                                    }
                                                    else if (itemColumn.Category == "Crack")
                                                    {
                                                        var input = itemColumn.Number1000;
                                                        var value1000 = "";
                                                        if (input.Length == 2)
                                                        {
                                                            var pattern = new Regex(@"\d{1}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value1000 = resulttt.Replace(itemColumn.Number1000, "");
                                                        }
                                                        else if (input.Length == 3 || input.Length == 4)
                                                        {
                                                            var pattern = new Regex(@"\d{2}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value1000 = resulttt.Replace(itemColumn.Number1000, "");
                                                        }
                                                        else
                                                        {
                                                            var pattern = new Regex(@"\d{3}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            var firstCrack = resulttt.Replace(itemColumn.Number1000, "");
                                                            if (firstCrack.Contains("2"))
                                                            {
                                                                resItem.style.visibility = "hidden";
                                                            }

                                                            var valueNumber = itemColumn.Number1000;

                                                            if (valueNumber.Contains("1"))
                                                            {
                                                                valueNumber = valueNumber.Substring(0, 3);
                                                            }
                                                            resItem.value1000 = valueNumber;
                                                            value1000 = resulttt.Replace(itemColumn.Number1000, "");
                                                        }

                                                        res.description1000 = $"{charOnly.Replace(itemColumn.Number1000 == null ? string.Empty : itemColumn.Number1000, "#").Split("#")[0]};{value1000};{detailDescription}";
                                                    }
                                                    else
                                                    {
                                                        resItem.value1000 = itemColumn.Number1000;
                                                        resItem.style.visibility = "hidden";
                                                    }
                                                }
                                                else
                                                {
                                                    resItem.value1000 = itemColumn.Number1000;
                                                }
                                            }
                                            #endregion

                                            #region Numbering 2000
                                            if (!string.IsNullOrEmpty(itemColumn.Number2000))
                                            {
                                                if (charOnly.IsMatch(itemColumn.Number2000))
                                                {
                                                    if (itemColumn.Category == EnumCategoryServiceSheet.CBM ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_MANUAL ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_ADJUSTMENT ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_ADJUSTMENT_CARRY_ROLLER ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_NORMAL ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.Defect_Group ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.DEFECT_MEASUREMENT ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_CALCULATE ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_CALCULATE_RESULT ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_MANUAL_DOUBLE ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_CALIBRATION ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_BRAKE)
                                                    {
                                                        var input = itemColumn.Number2000;
                                                        if (input.Length == 2)
                                                        {
                                                            var pattern = new Regex(@"\d{1}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value2000 = resulttt.Replace(itemColumn.Number2000, "");
                                                        }
                                                        else if (input.Length == 3 || input.Length == 4)
                                                        {
                                                            var takeOutChar = charOnly.Replace(itemColumn.Number2000, "#");
                                                            var splitNumber = takeOutChar.Split('#');

                                                            if (splitNumber.Length > 0 && splitNumber[0].Length == 3)
                                                            {
                                                                var pattern = new Regex(@"\d{3}");
                                                                var resulttt = pattern.Replace(input, "");

                                                                resItem.value2000 = resulttt.Replace(itemColumn.Number2000, "");
                                                            }
                                                            else
                                                            {
                                                                var pattern = new Regex(@"\d{2}");
                                                                var resulttt = pattern.Replace(input, "");

                                                                resItem.value2000 = resulttt.Replace(itemColumn.Number2000, "");
                                                            }
                                                        }
                                                        else
                                                        {
                                                            var pattern = new Regex(@"\d{3}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value2000 = resulttt.Replace(itemColumn.Number2000, "");
                                                        }

                                                        res.description2000 = $"{charOnly.Replace(itemColumn.Number2000 == null ? string.Empty : itemColumn.Number2000, "#").Split("#")[0]};{resItem.value2000};{detailDescription}";
                                                    }
                                                    else if (itemColumn.Category == "Crack")
                                                    {
                                                        var input = itemColumn.Number2000;
                                                        var value2000 = "";
                                                        if (input.Length == 2)
                                                        {
                                                            var pattern = new Regex(@"\d{1}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value2000 = resulttt.Replace(itemColumn.Number2000, "");
                                                        }
                                                        else if (input.Length == 3 || input.Length == 4)
                                                        {
                                                            var pattern = new Regex(@"\d{2}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value2000 = resulttt.Replace(itemColumn.Number2000, "");
                                                        }
                                                        else
                                                        {
                                                            var pattern = new Regex(@"\d{3}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            var firstCrack = resulttt.Replace(itemColumn.Number2000, "");
                                                            if (firstCrack.Contains("2"))
                                                            {
                                                                resItem.style.visibility = "hidden";
                                                            }

                                                            var valueNumber = itemColumn.Number2000;

                                                            if (valueNumber.Contains("1"))
                                                            {
                                                                valueNumber = valueNumber.Substring(0, 3);
                                                            }
                                                            resItem.value2000 = valueNumber;
                                                            value2000 = resulttt.Replace(itemColumn.Number2000, "");
                                                        }

                                                        res.description2000 = $"{charOnly.Replace(itemColumn.Number2000 == null ? string.Empty : itemColumn.Number2000, "#").Split("#")[0]};{value2000};{detailDescription}";
                                                    }
                                                    else
                                                    {
                                                        resItem.value2000 = itemColumn.Number2000;
                                                        resItem.style.visibility = "hidden";
                                                    }
                                                }
                                                else
                                                {
                                                    resItem.value2000 = itemColumn.Number2000;
                                                }
                                            }
                                            #endregion

                                            #region Numbering 4000
                                            if (!string.IsNullOrEmpty(itemColumn.Number4000))
                                            {
                                                if (charOnly.IsMatch(itemColumn.Number4000))
                                                {
                                                    if (itemColumn.Category == EnumCategoryServiceSheet.CBM ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_MANUAL ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_ADJUSTMENT ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_ADJUSTMENT_CARRY_ROLLER ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_NORMAL ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.Defect_Group ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.DEFECT_MEASUREMENT ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_CALCULATE ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_CALCULATE_RESULT ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_MANUAL_DOUBLE ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_CALIBRATION ||
                                                        itemColumn.Category == EnumCategoryServiceSheet.CBM_BRAKE)
                                                    {
                                                        var input = itemColumn.Number4000;
                                                        if (input.Length == 2)
                                                        {
                                                            var pattern = new Regex(@"\d{1}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value4000 = resulttt.Replace(itemColumn.Number4000, "");
                                                        }
                                                        else if (input.Length == 3 || input.Length == 4)
                                                        {
                                                            var takeOutChar = charOnly.Replace(itemColumn.Number4000, "#");
                                                            var splitNumber = takeOutChar.Split('#');

                                                            if (splitNumber.Length > 0 && splitNumber[0].Length == 3)
                                                            {
                                                                var pattern = new Regex(@"\d{3}");
                                                                var resulttt = pattern.Replace(input, "");

                                                                resItem.value4000 = resulttt.Replace(itemColumn.Number4000, "");
                                                            }
                                                            else
                                                            {
                                                                var pattern = new Regex(@"\d{2}");
                                                                var resulttt = pattern.Replace(input, "");

                                                                resItem.value4000 = resulttt.Replace(itemColumn.Number4000, "");
                                                            }
                                                        }
                                                        else
                                                        {
                                                            var pattern = new Regex(@"\d{3}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value4000 = resulttt.Replace(itemColumn.Number4000, "");
                                                        }

                                                        res.description4000 = $"{charOnly.Replace(itemColumn.Number4000 == null ? string.Empty : itemColumn.Number4000, "#").Split("#")[0]};{resItem.value4000};{detailDescription}";
                                                    }
                                                    else if (itemColumn.Category == "Crack")
                                                    {
                                                        var input = itemColumn.Number4000;
                                                        var value4000 = "";
                                                        if (input.Length == 2)
                                                        {
                                                            var pattern = new Regex(@"\d{1}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value4000 = resulttt.Replace(itemColumn.Number4000, "");
                                                        }
                                                        else if (input.Length == 3 || input.Length == 4)
                                                        {
                                                            var pattern = new Regex(@"\d{2}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value4000 = resulttt.Replace(itemColumn.Number4000, "");
                                                        }
                                                        else
                                                        {
                                                            var pattern = new Regex(@"\d{3}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            var firstCrack = resulttt.Replace(itemColumn.Number4000, "");
                                                            if (firstCrack.Contains("2"))
                                                            {
                                                                resItem.style.visibility = "hidden";
                                                            }

                                                            var valueNumber = itemColumn.Number4000;

                                                            if (valueNumber.Contains("1"))
                                                            {
                                                                valueNumber = valueNumber.Substring(0, 3);
                                                            }
                                                            resItem.value4000 = valueNumber;
                                                            value4000 = resulttt.Replace(itemColumn.Number4000, "");

                                                        }

                                                        res.description4000 = $"{charOnly.Replace(itemColumn.Number4000 == null ? string.Empty : itemColumn.Number4000, "#").Split("#")[0]};{value4000};{detailDescription}";
                                                    }
                                                    else
                                                    {
                                                        resItem.value4000 = itemColumn.Number4000;
                                                        resItem.style.visibility = "hidden";
                                                    }
                                                }
                                                else
                                                {
                                                    resItem.value4000 = itemColumn.Number4000;
                                                }
                                            }
                                            #endregion

                                            break;
                                        case "<<itemCrackCode>>":
                                            #region Numbering 250
                                            if (!string.IsNullOrEmpty(itemColumn.Number250))
                                            {
                                                if (itemColumn.Category == "Crack" && charOnly.IsMatch(itemColumn.Number250))
                                                {
                                                    var input = itemColumn.Number250;
                                                    if (input.Length == 2)
                                                    {
                                                        var pattern = new Regex(@"\d{1}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        resItem.Number250 = resulttt.Replace(itemColumn.Number250, "");
                                                    }
                                                    else if (input.Length == 3 || input.Length == 4)
                                                    {
                                                        var pattern = new Regex(@"\d{2}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        resItem.Number250 = resulttt.Replace(itemColumn.Number250, "");
                                                    }
                                                    else
                                                    {
                                                        var pattern = new Regex(@"\d{3}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        var firstCrack = resulttt.Replace(itemColumn.Number250, "");

                                                        resItem.Number250 = firstCrack.ToUpper();
                                                    }
                                                }
                                            }
                                            #endregion

                                            #region Numbering 500
                                            if (!string.IsNullOrEmpty(itemColumn.Number500))
                                            {
                                                if (itemColumn.Category == "Crack" && charOnly.IsMatch(itemColumn.Number500))
                                                {
                                                    var input = itemColumn.Number500;
                                                    if (input.Length == 2)
                                                    {
                                                        var pattern = new Regex(@"\d{1}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        resItem.Number500 = resulttt.Replace(itemColumn.Number500, "");
                                                    }
                                                    else if (input.Length == 3 || input.Length == 4)
                                                    {
                                                        var pattern = new Regex(@"\d{2}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        resItem.Number500 = resulttt.Replace(itemColumn.Number500, "");
                                                    }
                                                    else
                                                    {
                                                        var pattern = new Regex(@"\d{3}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        var firstCrack = resulttt.Replace(itemColumn.Number500, "");

                                                        resItem.Number500 = firstCrack.ToUpper();
                                                    }
                                                }
                                            }
                                            #endregion

                                            #region Numbering 1000
                                            if (!string.IsNullOrEmpty(itemColumn.Number1000))
                                            {
                                                if (itemColumn.Category == "Crack" && charOnly.IsMatch(itemColumn.Number1000))
                                                {
                                                    var input = itemColumn.Number1000;
                                                    if (input.Length == 2)
                                                    {
                                                        var pattern = new Regex(@"\d{1}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        resItem.Number1000 = resulttt.Replace(itemColumn.Number1000, "");
                                                    }
                                                    else if (input.Length == 3 || input.Length == 4)
                                                    {
                                                        var pattern = new Regex(@"\d{2}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        resItem.Number1000 = resulttt.Replace(itemColumn.Number1000, "");
                                                    }
                                                    else
                                                    {
                                                        var pattern = new Regex(@"\d{3}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        var firstCrack = resulttt.Replace(itemColumn.Number1000, "");

                                                        resItem.Number1000 = firstCrack.ToUpper();
                                                    }
                                                }
                                            }
                                            #endregion

                                            #region Numbering 2000
                                            if (!string.IsNullOrEmpty(itemColumn.Number2000))
                                            {
                                                if (itemColumn.Category == "Crack" && charOnly.IsMatch(itemColumn.Number2000))
                                                {
                                                    var input = itemColumn.Number2000;
                                                    if (input.Length == 2)
                                                    {
                                                        var pattern = new Regex(@"\d{1}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        resItem.Number2000 = resulttt.Replace(itemColumn.Number2000, "");
                                                    }
                                                    else if (input.Length == 3 || input.Length == 4)
                                                    {
                                                        var pattern = new Regex(@"\d{2}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        resItem.Number2000 = resulttt.Replace(itemColumn.Number2000, "");
                                                    }
                                                    else
                                                    {
                                                        var pattern = new Regex(@"\d{3}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        var firstCrack = resulttt.Replace(itemColumn.Number2000, "");

                                                        resItem.Number2000 = firstCrack.ToUpper();
                                                    }
                                                }
                                            }
                                            #endregion

                                            #region Numbering 4000
                                            if (!string.IsNullOrEmpty(itemColumn.Number4000))
                                            {
                                                if (itemColumn.Category == "Crack" && charOnly.IsMatch(itemColumn.Number4000))
                                                {
                                                    var input = itemColumn.Number4000;
                                                    if (input.Length == 2)
                                                    {
                                                        var pattern = new Regex(@"\d{1}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        resItem.Number4000 = resulttt.Replace(itemColumn.Number4000, "");
                                                    }
                                                    else if (input.Length == 3 || input.Length == 4)
                                                    {
                                                        var pattern = new Regex(@"\d{2}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        resItem.Number4000 = resulttt.Replace(itemColumn.Number4000, "");
                                                    }
                                                    else
                                                    {
                                                        var pattern = new Regex(@"\d{3}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        var firstCrack = resulttt.Replace(itemColumn.Number4000, "");

                                                        resItem.Number4000 = firstCrack.ToUpper();
                                                    }
                                                }
                                            }
                                            #endregion
                                            break;
                                        case "<<itemDesc>>":
                                            var dataDesc = itemColumn.Description;

                                            if (itemColumn.Description != null && (itemColumn.Category == EnumCategoryServiceSheet.CBM ||
                                                                                   itemColumn.Category == EnumCategoryServiceSheet.CBM_MANUAL ||
                                                                                   itemColumn.Category == EnumCategoryServiceSheet.CBM_ADJUSTMENT ||
                                                                                   itemColumn.Category == EnumCategoryServiceSheet.CBM_ADJUSTMENT_CARRY_ROLLER ||
                                                                                   itemColumn.Category == EnumCategoryServiceSheet.CBM_NORMAL ||
                                                                                   itemColumn.Category == EnumCategoryServiceSheet.CBM_CALCULATE ||
                                                                                   itemColumn.Category == EnumCategoryServiceSheet.CBM_CALCULATE_RESULT ||
                                                                                   itemColumn.Category == EnumCategoryServiceSheet.CBM_MANUAL_DOUBLE ||
                                                                                   itemColumn.Category == EnumCategoryServiceSheet.CBM_CALIBRATION ||
                                                                                   itemColumn.Category == EnumCategoryServiceSheet.CBM_BRAKE ||
                                                                                   itemColumn.Category == EnumCategoryServiceSheet.DEFECT_MEASUREMENT
                                                                                   ))
                                            {
                                                if (dataDesc.Contains("a1. ")) { dataDesc = dataDesc.Replace("a1. ", ""); }
                                                else if (dataDesc.Contains("a2. ")) { dataDesc = dataDesc.Replace("a2. ", ""); }
                                                else if (dataDesc.Contains("b1. ")) { dataDesc = dataDesc.Replace("b1. ", ""); }
                                                else if (dataDesc.Contains("b2. ")) { dataDesc = dataDesc.Replace("b2. ", ""); }
                                                else if (dataDesc.Contains("a. ")) { dataDesc = dataDesc.Replace("a. ", ""); }
                                                else if (dataDesc.Contains("b. ")) { dataDesc = dataDesc.Replace("b. ", ""); }
                                                else if (dataDesc.Contains("c. ")) { dataDesc = dataDesc.Replace("c. ", ""); }
                                                else if (dataDesc.Contains("d. ")) { dataDesc = dataDesc.Replace("d. ", ""); }
                                                else if (dataDesc.Contains("e. ")) { dataDesc = dataDesc.Replace("e. ", ""); }
                                                else if (dataDesc.Contains("f. ")) { dataDesc = dataDesc.Replace("f. ", ""); }
                                                else if (dataDesc.Contains("g. ")) { dataDesc = dataDesc.Replace("g. ", ""); }
                                                else if (dataDesc.Contains("h. ")) { dataDesc = dataDesc.Replace("h. ", ""); }
                                                else if (dataDesc.Contains("i. ")) { dataDesc = dataDesc.Replace("i. ", ""); }
                                                else if (dataDesc.Contains("j. ")) { dataDesc = dataDesc.Replace("j. ", ""); }
                                                else if (dataDesc.Contains("k. ")) { dataDesc = dataDesc.Replace("k. ", ""); }
                                                else if (dataDesc.Contains("l. ")) { dataDesc = dataDesc.Replace("l. ", ""); }
                                                else if (dataDesc.Contains("m. ")) { dataDesc = dataDesc.Replace("m. ", ""); }
                                            }

                                            resItem.value = dataDesc;
                                            break;
                                        case "<<section>>":
                                            resItem.value = itemColumn.Description;
                                            break;
                                        case "<<itemMapping>>":
                                            resItem.value = itemColumn.ServiceMappingValue;
                                            break;
                                    }

                                    if (itemColumn.Category != EnumCategoryServiceSheet.Crack && itemColumn.LastRow)
                                    {
                                        if (resItem.style.border != null)
                                        {
                                            resItem.style.border.bottom = EnumCommonProperty.StyleBorder;
                                        }
                                    }

                                    if (itemColumn.Category == EnumCategoryServiceSheet.Crack && itemColumn.FirstRow)
                                    {
                                        if (resItem.style.border != null)
                                        {
                                            resItem.style.border.top = EnumCommonProperty.StyleBorder;
                                        }
                                    }

                                    if (itemColumn.Category == EnumCategoryServiceSheet.Crack && itemColumn.LastRow)
                                    {
                                        if (resItem.style.border != null)
                                        {
                                            resItem.style.border.bottom = EnumCommonProperty.StyleBorder;
                                        }
                                    }

                                    if ((itemColumn.Category == EnumCategoryServiceSheet.Crack || itemColumn.Category == EnumCategoryServiceSheet.CRACK_NON_GROUP || itemColumn.Category == EnumCategoryServiceSheet.CRACK_SUBTASK) && resItem.itemType == "dropDown")
                                    {
                                        resItem.imageMapping = valueImageBefore;
                                    }
                                }

                                if ((itemColumn.Category == EnumCategoryServiceSheet.CBM ||
                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_MANUAL ||
                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_ADJUSTMENT ||
                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_ADJUSTMENT_CARRY_ROLLER ||
                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_NORMAL ||
                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_CALCULATE ||
                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_CALCULATE_RESULT ||
                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_MANUAL_DOUBLE ||
                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_CALIBRATION ||
                                    itemColumn.Category == EnumCategoryServiceSheet.CBM_BRAKE ||
                                    itemColumn.Category == EnumCategoryServiceSheet.DEFECT_MEASUREMENT) && resCbmHeader != null)
                                {
                                    resCbmHeader.SectionData = itemColumn.Section;

                                    if (resCbmHeaderRoller != null)
                                    {
                                        foreach (var itemDataHeader in resCbmHeader.items)
                                        {
                                            if (itemDataHeader.style.border != null)
                                            {
                                                itemDataHeader.style.border.top = "none";
                                            }
                                        }

                                        resCbmHeaderRoller.SectionData = itemColumn.Section;

                                        if (!string.IsNullOrEmpty(itemColumn.Number250)) { psType250.Add(resCbmHeaderRoller); }
                                        if (!string.IsNullOrEmpty(itemColumn.Number500)) { psType500.Add(resCbmHeaderRoller); }
                                        if (!string.IsNullOrEmpty(itemColumn.Number1000)) { psType1000.Add(resCbmHeaderRoller); }
                                        if (!string.IsNullOrEmpty(itemColumn.Number2000)) { psType2000.Add(resCbmHeaderRoller); }
                                        if (!string.IsNullOrEmpty(itemColumn.Number4000)) { psType4000.Add(resCbmHeaderRoller); }
                                    }

                                    // Insert CBM Header
                                    if (!string.IsNullOrEmpty(itemColumn.Number250)) { psType250.Add(resCbmHeader); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number500)) { psType500.Add(resCbmHeader); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number1000)) { psType1000.Add(resCbmHeader); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number2000)) { psType2000.Add(resCbmHeader); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number4000)) { psType4000.Add(resCbmHeader); }

                                }

                                if ((itemColumn.Category == EnumCategoryServiceSheet.Defect_Group || itemColumn.Category == EnumCategoryServiceSheet.DEFECT_MEASUREMENT || itemColumn.Category == EnumCategoryServiceSheet.Service_With_Header) && resDefectGroupHeader != null)
                                {
                                    // Insert CBM Header
                                    if (!string.IsNullOrEmpty(itemColumn.Number250)) { psType250.Add(resDefectGroupHeader); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number500)) { psType500.Add(resDefectGroupHeader); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number1000)) { psType1000.Add(resDefectGroupHeader); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number2000)) { psType2000.Add(resDefectGroupHeader); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number4000)) { psType4000.Add(resDefectGroupHeader); }

                                }

                                // Insert Table Schema
                                if (!string.IsNullOrEmpty(itemColumn.Table))
                                {
                                    if (itemColumn.Table == EnumTableServiceSheet.LUBRICANT)
                                    {
                                        var paramCbmHeader = new Dictionary<string, object>();
                                        paramCbmHeader.Add("category", EnumCategoryServiceSheet.TABLE);
                                        paramCbmHeader.Add("rating", EnumRatingServiceSheet.LUBRICANT);

                                        var resultGuid = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);
                                        var resGuide = resultGuid.content;
                                        resGuide.key = Guid.NewGuid().ToString();
                                        resGuide.SectionData = itemColumn.Section;

                                        if (!string.IsNullOrEmpty(itemColumn.Number250)) { psType250.Add(resGuide); }
                                        if (!string.IsNullOrEmpty(itemColumn.Number500)) { psType500.Add(resGuide); }
                                        if (!string.IsNullOrEmpty(itemColumn.Number1000)) { psType1000.Add(resGuide); }
                                        if (!string.IsNullOrEmpty(itemColumn.Number2000)) { psType2000.Add(resGuide); }
                                        if (!string.IsNullOrEmpty(itemColumn.Number4000)) { psType4000.Add(resGuide); }
                                    }
                                    else if (itemColumn.Table == EnumTableServiceSheet.BATTERY)
                                    {
                                        var paramCbmHeader = new Dictionary<string, object>();
                                        paramCbmHeader.Add("category", EnumCategoryServiceSheet.TABLE);
                                        paramCbmHeader.Add("rating", EnumRatingServiceSheet.BATTERY);

                                        var resultGuid = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);
                                        var resGuide = resultGuid.content;

                                        foreach (var dataItemsTable in resGuide.items)
                                        {
                                            dataItemsTable["column2"].disabledByItemKey = valueDisabledBefore;
                                            dataItemsTable["column3"].disabledByItemKey = valueDisabledBefore;
                                            dataItemsTable["column4"].disabledByItemKey = valueDisabledBefore;
                                            dataItemsTable["column5"].disabledByItemKey = valueDisabledBefore;
                                        }

                                        resGuide.key = Guid.NewGuid().ToString();
                                        resGuide.disabledByItemKey = valueDisabledBefore;
                                        resGuide.SectionData = itemColumn.Section;

                                        if (!string.IsNullOrEmpty(itemColumn.Number250)) { psType250.Add(resGuide); }
                                        if (!string.IsNullOrEmpty(itemColumn.Number500)) { psType500.Add(resGuide); }
                                        if (!string.IsNullOrEmpty(itemColumn.Number1000)) { psType1000.Add(resGuide); }
                                        if (!string.IsNullOrEmpty(itemColumn.Number2000)) { psType2000.Add(resGuide); }
                                        if (!string.IsNullOrEmpty(itemColumn.Number4000)) { psType4000.Add(resGuide); }
                                    }
                                    else if (itemColumn.Table == EnumTableServiceSheet.BATTERY_CCA)
                                    {
                                        var paramCbmHeader = new Dictionary<string, object>();
                                        paramCbmHeader.Add("category", EnumCategoryServiceSheet.TABLE);
                                        paramCbmHeader.Add("rating", EnumRatingServiceSheet.BATTERY_CCA);

                                        var resultGuid = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);
                                        var resGuide = resultGuid.content;
                                        foreach (var dataItemsTable in resGuide.items)
                                        {
                                            dataItemsTable["column2"].disabledByItemKey = valueDisabledBefore;
                                            dataItemsTable["column3"].disabledByItemKey = valueDisabledBefore;
                                        }

                                        resGuide.key = Guid.NewGuid().ToString();
                                        resGuide.disabledByItemKey = valueDisabledBefore;
                                        resGuide.SectionData = itemColumn.Section;

                                        if (!string.IsNullOrEmpty(itemColumn.Number250)) { psType250.Add(resGuide); }
                                        if (!string.IsNullOrEmpty(itemColumn.Number500)) { psType500.Add(resGuide); }
                                        if (!string.IsNullOrEmpty(itemColumn.Number1000)) { psType1000.Add(resGuide); }
                                        if (!string.IsNullOrEmpty(itemColumn.Number2000)) { psType2000.Add(resGuide); }
                                        if (!string.IsNullOrEmpty(itemColumn.Number4000)) { psType4000.Add(resGuide); }
                                    }
                                    else
                                    {
                                        var paramCbmHeader = new Dictionary<string, object>();
                                        paramCbmHeader.Add("category", EnumCategoryServiceSheet.TABLE);
                                        paramCbmHeader.Add("rating", itemColumn.Table);

                                        var resultGuid = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);
                                        var resGuide = resultGuid.content;
                                        resGuide.key = Guid.NewGuid().ToString();
                                        resGuide.SectionData = itemColumn.Section;

                                        if (!string.IsNullOrEmpty(itemColumn.Number250)) { psType250.Add(resGuide); }
                                        if (!string.IsNullOrEmpty(itemColumn.Number500)) { psType500.Add(resGuide); }
                                        if (!string.IsNullOrEmpty(itemColumn.Number1000)) { psType1000.Add(resGuide); }
                                        if (!string.IsNullOrEmpty(itemColumn.Number2000)) { psType2000.Add(resGuide); }
                                        if (!string.IsNullOrEmpty(itemColumn.Number4000)) { psType4000.Add(resGuide); }
                                    }
                                }

                                // Insert MAIN DATA
                                if (!string.IsNullOrEmpty(itemColumn.Number250)) { psType250.Add(res); }
                                if (!string.IsNullOrEmpty(itemColumn.Number500)) { psType500.Add(res); }
                                if (!string.IsNullOrEmpty(itemColumn.Number1000)) { psType1000.Add(res); }
                                if (!string.IsNullOrEmpty(itemColumn.Number2000)) { psType2000.Add(res); }
                                if (!string.IsNullOrEmpty(itemColumn.Number4000)) { psType4000.Add(res); }

                                // Insert Image
                                if (!string.IsNullOrEmpty(itemColumn.ImageData))
                                {
                                    itemColumn.ImageData = itemColumn.ImageData.Contains("YES") ? "135" : itemColumn.ImageData;

                                    var paramCbmHeader = new Dictionary<string, object>();
                                    paramCbmHeader.Add("category", EnumCategoryServiceSheet.NORMAL);
                                    paramCbmHeader.Add("rating", EnumRatingServiceSheet.IMAGE);

                                    var resultImage = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);
                                    var resImage = resultImage.content;
                                    resImage.key = Guid.NewGuid().ToString();
                                    resImage.SectionData = itemColumn.Section;

                                    foreach (var resImageItem in resImage.items)
                                    {
                                        resImageItem.key = Guid.NewGuid().ToString();
                                        if (resImageItem.value.ToString() == EnumCommonProperty.ImageData)
                                        {
                                            resImageItem.value = itemColumn.ImageData;
                                        }
                                        valueImageBefore = resImageItem.key;
                                    }

                                    if (!string.IsNullOrEmpty(itemColumn.Number250)) { psType250.Add(resImage); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number500)) { psType500.Add(resImage); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number1000)) { psType1000.Add(resImage); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number2000)) { psType2000.Add(resImage); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number4000)) { psType4000.Add(resImage); }
                                }

                                // Insert Guide
                                if (!string.IsNullOrEmpty(itemColumn.GuidTable))
                                {
                                    var paramCbmHeader = new Dictionary<string, object>();
                                    paramCbmHeader.Add("category", EnumCategoryServiceSheet.GUIDE);
                                    paramCbmHeader.Add("rating", itemColumn.GuidTable);

                                    var resultGuid = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);
                                    var resGuide = resultGuid.content;
                                    resGuide.key = Guid.NewGuid().ToString();
                                    resGuide.SectionData = itemColumn.Section;

                                    if (!string.IsNullOrEmpty(itemColumn.Number250)) { psType250.Add(resGuide); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number500)) { psType500.Add(resGuide); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number1000)) { psType1000.Add(resGuide); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number2000)) { psType2000.Add(resGuide); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number4000)) { psType4000.Add(resGuide); }
                                }
                            }
                        }
                        #endregion

                        var dataCek = dataJsonResult.Where(x => x.Key == "2000").FirstOrDefault().Value;

                        dataJsonResult.Add("250", psType250);
                        dataJsonResult.Add("500", psType500);
                        dataJsonResult.Add("1000", psType1000);
                        dataJsonResult.Add("2000", psType2000);
                        dataJsonResult.Add("4000", psType4000);

                        List<string> psTypeData = new List<string>();
                        psTypeData.Add("250");
                        psTypeData.Add("500");
                        psTypeData.Add("1000");
                        psTypeData.Add("2000");
                        psTypeData.Add("4000");

                        foreach (var itemDataPsType in psTypeData)
                        {
                            var dataTaskGroup = dataJsonResult.Where(x => x.Key == itemDataPsType).FirstOrDefault().Value;

                            var resultObj = JsonConvert.DeserializeObject<List<dynamic>>(JsonConvert.SerializeObject(dataTaskGroup));

                            if (resultObj.Count == 0)
                            {
                                continue;
                            }

                            foreach (var itemGorupResult in resultObj)
                            {
                                if (itemGorupResult.description != EnumCommonProperty.Description)
                                {
                                    itemGorupResult.Remove("description250");
                                    itemGorupResult.Remove("description500");
                                    itemGorupResult.Remove("description1000");
                                    itemGorupResult.Remove("description2000");
                                    itemGorupResult.Remove("description4000");
                                    continue;
                                };

                                if (itemDataPsType == "250") { itemGorupResult.description = itemGorupResult.description250; }
                                else if (itemDataPsType == "500") { itemGorupResult.description = itemGorupResult.description500; }
                                else if (itemDataPsType == "1000") { itemGorupResult.description = itemGorupResult.description1000; }
                                else if (itemDataPsType == "2000") { itemGorupResult.description = itemGorupResult.description2000; }
                                else if (itemDataPsType == "4000") { itemGorupResult.description = itemGorupResult.description4000; }

                                foreach (var itemGroups in itemGorupResult.items)
                                {
                                    switch (itemGroups.value.ToString())
                                    {
                                        case "<<itemDetailNumber>>":
                                            if (itemDataPsType == "250") { itemGroups.value = itemGroups.value250; }
                                            else if (itemDataPsType == "500") { itemGroups.value = itemGroups.value500; }
                                            else if (itemDataPsType == "1000") { itemGroups.value = itemGroups.value1000; }
                                            else if (itemDataPsType == "2000") { itemGroups.value = itemGroups.value2000; }
                                            else if (itemDataPsType == "4000") { itemGroups.value = itemGroups.value4000; }

                                            break;
                                        case "<<itemNumber>>":
                                            if (itemDataPsType == "250") { itemGroups.value = itemGroups.value250; }
                                            else if (itemDataPsType == "500") { itemGroups.value = itemGroups.value500; }
                                            else if (itemDataPsType == "1000") { itemGroups.value = itemGroups.value1000; }
                                            else if (itemDataPsType == "2000") { itemGroups.value = itemGroups.value2000; }
                                            else if (itemDataPsType == "4000") { itemGroups.value = itemGroups.value4000; }

                                            break;
                                        case "<<itemCrackCode>>":
                                            if (itemDataPsType == "250") { itemGroups.value = itemGroups.Number250; }
                                            else if (itemDataPsType == "500") { itemGroups.value = itemGroups.Number500; }
                                            else if (itemDataPsType == "1000") { itemGroups.value = itemGroups.Number1000; }
                                            else if (itemDataPsType == "2000") { itemGroups.value = itemGroups.Number2000; }
                                            else if (itemDataPsType == "4000") { itemGroups.value = itemGroups.Number4000; }

                                            break;
                                    }

                                    if (itemGroups.disabledByItemKey != null)
                                    {
                                        switch (itemGroups.disabledByItemKey.ToString())
                                        {
                                            case "<<disabledByItemKey>>":
                                                if (itemDataPsType == "250") { itemGroups.disabledByItemKey = itemGorupResult.valueDisabled; }
                                                else if (itemDataPsType == "500") { itemGroups.disabledByItemKey = itemGorupResult.valueDisabled; }
                                                else if (itemDataPsType == "1000") { itemGroups.disabledByItemKey = itemGorupResult.valueDisabled; }
                                                else if (itemDataPsType == "2000") { itemGroups.disabledByItemKey = itemGorupResult.valueDisabled; }
                                                else if (itemDataPsType == "4000") { itemGroups.disabledByItemKey = itemGorupResult.valueDisabled; }

                                                break;
                                        }
                                    }

                                    itemGroups.Remove("value250");
                                    itemGroups.Remove("value500");
                                    itemGroups.Remove("value1000");
                                    itemGroups.Remove("value2000");
                                    itemGroups.Remove("value4000");
                                }

                                itemGorupResult.Remove("description250");
                                itemGorupResult.Remove("description500");
                                itemGorupResult.Remove("description1000");
                                itemGorupResult.Remove("description2000");
                                itemGorupResult.Remove("description4000");
                                itemGorupResult.Remove("valueDisabled");

                            }

                            #region Default Header
                            GenerateJsonModel jsonModel = new GenerateJsonModel();
                            jsonModel.subGroup = new List<SubGroup>();

                            jsonModel.modelId = modelList;
                            jsonModel.psTypeId = itemDataPsType;
                            jsonModel.workOrder = "";
                            jsonModel.groupName = groupName.Replace(" ", "_").ToUpper();
                            jsonModel.groupSeq = 2;
                            jsonModel.key = groupName.Replace(" ", "_").ToUpper();
                            jsonModel.version = "1.0.0";

                            var paramInfo = new Dictionary<string, object>();
                            paramInfo.Add("category", "NORMAL_INFO");
                            paramInfo.Add("rating", "NO");

                            var resultInfo = await _repositoryGenerateJson.GetDataByParam(paramInfo);

                            // Info Tab
                            var infoTab = listGenerateColumn.Where(x => x.Category == "InfoTab").FirstOrDefault();
                            if (infoTab != null)
                            {
                                var infoSplit = infoTab.Description.Split("|");
                                foreach (var itemInfo in resultInfo.content.items)
                                {
                                    switch (itemInfo.value.ToString())
                                    {
                                        case "<<warning>>":
                                            itemInfo.value = infoSplit[0];
                                            break;
                                        case "<<info>>":
                                            itemInfo.value = infoSplit[1];
                                            break;
                                    }
                                }
                            }

                            TaskGroup taskGroupDataInfo = new TaskGroup();
                            taskGroupDataInfo.task = new List<dynamic>();
                            taskGroupDataInfo.name = "Information"; // Section
                            taskGroupDataInfo.key = $"{groupName.Replace(" ", "_")}_INFORMATION"; // Section
                            taskGroupDataInfo.task.Add(resultInfo.content);
                            #endregion

                            #region Merge Data
                            List<TaskGroup> taskGroupDataList = new List<TaskGroup>();

                            var sectionGroup = listGenerateColumn.Select(x => x.Section).Distinct().Where(x => x != "INFORMATION").ToList();
                            foreach (var itemSectionData in sectionGroup)
                            {
                                if (itemSectionData == null)
                                {
                                    var dataSectionNull = listGenerateColumn.Where(x => x.Section == itemSectionData);
                                    ServiceResult resultErr = new ServiceResult();
                                    resultErr.IsError = true;
                                    resultErr.Message = $"Error Line Excel: {JsonConvert.SerializeObject(dataSectionNull.Select(x => x.Row).ToList())}";
                                    resultErr.Content = "[Section] Cannot be null or empty";

                                    return resultErr;
                                }

                                TaskGroup taskGroupData = new TaskGroup();
                                taskGroupData.name = itemSectionData; // Section
                                taskGroupData.key = $"{itemSectionData.Replace(" ", "_")}"; // Section
                                                                                            //taskGroupData.task = dataTaskGroup;
                                taskGroupData.task = resultObj.Where(x => x.SectionData == itemSectionData);

                                taskGroupDataList.Add(taskGroupData);
                            }

                            //taskGroupDataList.Add(taskGroupData);

                            //TaskGroup taskGroupData = new TaskGroup();
                            //taskGroupData.name = "Pre Service Check"; // Section
                            //taskGroupData.key = $"PRE_SERVICE_CHECK_DATA"; // Section
                            ////taskGroupData.task = dataTaskGroup;
                            //taskGroupData.task = resultObj;

                            SubGroup subGroupData = new SubGroup();
                            subGroupData.taskGroup = new List<TaskGroup>();
                            subGroupData.name = listGenerateColumn.FirstOrDefault().GroupName;
                            subGroupData.key = $"{groupName.Replace(" ", "_")}";
                            subGroupData.desc = "-";
                            subGroupData.taskGroup.Add(taskGroupDataInfo);
                            //subGroupData.taskGroup.Add(taskGroupData);
                            subGroupData.taskGroup.AddRange(taskGroupDataList);
                            #endregion

                            jsonModel.subGroup.Add(subGroupData);

                            switch (itemDataPsType)
                            {
                                case "4000":
                                    resultServiceSizeList.PsType4000.Add(jsonModel);
                                    break;
                                case "2000":
                                    resultServiceSizeList.PsType2000.Add(jsonModel);
                                    break;
                                case "1000":
                                    resultServiceSizeList.PsType1000.Add(jsonModel);
                                    break;
                                case "500":
                                    resultServiceSizeList.PsType500.Add(jsonModel);
                                    break;
                                case "250":
                                    resultServiceSizeList.PsType250.Add(jsonModel);
                                    break;
                            }

                            //resultList.Add(jsonModel);
                        }
                    }

                    //resultList = resultServiceSizeList;

                    result.IsError = false;
                    result.Message = "Success";
                    result.Content = resultServiceSizeList;
                }

                return new ServiceResult
                {
                    Message = "Data updated successfully",
                    IsError = false,
                    Content = result
                };
            }
            catch (Exception e)
            {

                throw e;
            }
        }

        public async Task<ServiceResult> GenerateTemplateFullSectionV2(GenerateModelJson model)
        {
            string detailAddressRow = string.Empty;

            try
            {
                // WO TESTING 9992703092

                ServiceResult result = new ServiceResult();
                dynamic resultHeaderCbmAutomatic = null;
                dynamic resultHeaderCbmAutomaticRoller = null;
                dynamic resultHeaderDefectGroup = null;
                //Dictionary<string, object> dataJsonResult = new Dictionary<string, object>();
                List<dynamic> resultList = new List<dynamic>();
                ServiceSizeRequest resultServiceSizeList = new ServiceSizeRequest();
                resultServiceSizeList.PsType4000 = new List<dynamic>();
                resultServiceSizeList.PsType2000 = new List<dynamic>();
                resultServiceSizeList.PsType1000 = new List<dynamic>();
                resultServiceSizeList.PsType500 = new List<dynamic>();
                resultServiceSizeList.PsType250 = new List<dynamic>();

                List<dynamic> psType250 = new List<dynamic>();
                List<dynamic> psType500 = new List<dynamic>();
                List<dynamic> psType1000 = new List<dynamic>();
                List<dynamic> psType2000 = new List<dynamic>();
                List<dynamic> psType4000 = new List<dynamic>();

                using (var excel = model.file.OpenReadStream())
                {
                    using var workBook = new XLWorkbook(excel);
                    IXLWorksheet workSheet = workBook.Worksheets.Where(x => x.Name == model.workSheet).FirstOrDefault();

                    var _repositoryGenerateJsonTypeData = new GenerateJsonTypeRepository(_connectionFactory, EnumContainer.GenerateJsonType);

                    Dictionary<string, object> paramTypeGenerateJson = new Dictionary<string, object>();
                    //paramTypeGenerateJson.Add("type", paramJsonType);

                    var resultJsonType = await _repositoryGenerateJsonTypeData.GetDataListByParamJArray(paramTypeGenerateJson);

                    List<GenerateJsonType> jsonType = JsonConvert.DeserializeObject<List<GenerateJsonType>>(JsonConvert.SerializeObject(resultJsonType));

                    //string currentRow = model.startRow;
                    //string endRow = model.endRow;

                    string currentRow = "2";
                    string endRow = workSheet.RowsUsed().Count().ToString();

                    string parentCurrentRow = "2";
                    string parentEndRow = workSheet.RowsUsed().Count().ToString();

                    Regex digitsOnly = new Regex(@"[^\d]");
                    Regex charOnly = new Regex(@"[a-zA-Z ]");

                    // Preparation
                    var dataWeworkSheetCount = workSheet.Range($"A{currentRow}:R{endRow}").RowCount();
                    var dataWeworkSheet = workSheet.Range($"A{currentRow}:R{endRow}")
                        .CellsUsed()
                        .Select(c => new AddressMappingData
                        {
                            Row = Convert.ToInt32(digitsOnly.Replace(c.Address.ToString(), "")),
                            Value = c.Value.ToString(),
                            Address = c.Address.ToString()
                        }).ToList();

                    // Model and Group
                    var modelList = dataWeworkSheet.Where(x => x.Address.Contains("A")).Select(x => x.Value).Distinct().FirstOrDefault();
                    var groupList = dataWeworkSheet.Where(x => x.Address.Contains("B")).Select(x => x.Value).Distinct().ToList();

                    foreach (var groupName in groupList)
                    {
                        var usedGroup = workSheet.Range($"A{parentCurrentRow}:R{parentEndRow}")
                        .CellsUsed()
                        .Select(c => new AddressMappingData
                        {
                            Row = Convert.ToInt32(digitsOnly.Replace(c.Address.ToString(), "")),
                            Value = c.Value.ToString(),
                            Address = c.Address.ToString()
                        }).Where(o => o.Address.Contains("B") && o.Value.ToString() == groupName).ToList();

                        currentRow = usedGroup.OrderBy(x => x.Row).FirstOrDefault().Row.ToString();
                        endRow = usedGroup.OrderByDescending(x => x.Row).FirstOrDefault().Row.ToString();

                        var dataGroupCount = workSheet.Range($"A{currentRow}:R{endRow}").RowCount();
                        var dataWeworkSheetGroup = workSheet.Range($"A{currentRow}:R{endRow}")
                        .CellsUsed()
                        .Select(c => new AddressMappingData
                        {
                            Row = Convert.ToInt32(digitsOnly.Replace(c.Address.ToString(), "")),
                            Value = c.Value.ToString(),
                            Address = c.Address.ToString()
                        }).ToList();

                        Dictionary<string, object> dataJsonResult = new Dictionary<string, object>();

                        var listGenerateColumn = new List<GenerateColumnJson>();

                        for (int i = 0; i < dataGroupCount; i++)
                        {
                            var generateColumn = new GenerateColumnJson();
                            foreach (var row in dataWeworkSheetGroup.Where(x => x.Row == i + Convert.ToInt32(currentRow)))
                            {
                                generateColumn.Row = row.Row;
                                generateColumn.Model = generateColumn.Model != null ? generateColumn.Model : row.Address.Contains("A") ? row.Value.Trim() : null;
                                generateColumn.GroupName = generateColumn.GroupName != null ? generateColumn.GroupName : row.Address.Contains("B") ? row.Value.Trim() : null;
                                generateColumn.Section = generateColumn.Section != null ? generateColumn.Section : row.Address.Contains("C") ? row.Value.Trim() : null;
                                generateColumn.Description = generateColumn.Description != null ? generateColumn.Description : row.Address.Contains("D") ? row.Value.Trim() : null;
                                generateColumn.Category = generateColumn.Category != null ? generateColumn.Category : row.Address.Contains("E") ? row.Value.Trim() : null;
                                generateColumn.Number250 = generateColumn.Number250 != null ? generateColumn.Number250 : row.Address.Contains("F") ? row.Value.Trim() : null;
                                generateColumn.Number500 = generateColumn.Number500 != null ? generateColumn.Number500 : row.Address.Contains("G") ? row.Value.Trim() : null;
                                generateColumn.Number1000 = generateColumn.Number1000 != null ? generateColumn.Number1000 : row.Address.Contains("H") ? row.Value.Trim() : null;
                                generateColumn.Number2000 = generateColumn.Number2000 != null ? generateColumn.Number2000 : row.Address.Contains("I") ? row.Value.Trim() : null;

                                if (generateColumn.Number4000 != null)
                                {
                                    generateColumn.Number4000 = generateColumn.Number4000 != null ? generateColumn.Number4000 : row.Address.Contains("J") ? row.Value.Trim() : null;
                                }
                                else
                                {
                                    generateColumn.Number4000 = generateColumn.Number2000 != null ? generateColumn.Number2000 : row.Address.Contains("J") ? row.Value.Trim() : null;
                                }

                                //generateColumn.Number4000 = generateColumn.Number4000 != null ? generateColumn.Number4000 : row.Address.Contains("J") ? row.Value.Trim() : null;
                                //generateColumn.Number4000 = generateColumn.Number4000 != null ? generateColumn.Number4000 : row.Address.Contains("J") ? row.Value.Trim() : null;

                                generateColumn.GuidTable = generateColumn.GuidTable != null ? generateColumn.GuidTable : row.Address.Contains("K") ? row.Value.Trim() : null;
                                generateColumn.ImageData = generateColumn.ImageData != null ? generateColumn.ImageData : row.Address.Contains("L") ? row.Value.Trim() : null;
                                generateColumn.Table = generateColumn.Table != null ? generateColumn.Table : row.Address.Contains("M") ? row.Value.Trim() : null;
                                generateColumn.ServiceMappingValue = generateColumn.ServiceMappingValue != null ? generateColumn.ServiceMappingValue : row.Address.Contains("N") ? row.Value.Trim() : null;
                                generateColumn.SOS = generateColumn.SOS != null ? generateColumn.SOS : row.Address.Contains("O") ? row.Value.Trim() : null;
                                generateColumn.SectionColumn = generateColumn.SectionColumn != null ? generateColumn.SectionColumn : row.Address.Contains("P") ? row.Value.Trim() : null;
                                generateColumn.TaskKey = generateColumn.TaskKey != null ? generateColumn.TaskKey : row.Address.Contains("Q") ? row.Value.Trim() : null;
                                generateColumn.GroupTaskId = generateColumn.GroupTaskId != null ? generateColumn.GroupTaskId : row.Address.Contains("R") ? row.Value.Trim() : null;
                            }

                            listGenerateColumn.Add(generateColumn);
                        }

                        JArray paramJsonType = new JArray();
                        paramJsonType.Add(listGenerateColumn.Select(x => x.Category).Distinct());


                        listGenerateColumn = (from masterDoc in listGenerateColumn
                                              join jsonTypeDetail in jsonType on masterDoc.Category equals jsonTypeDetail.type
                                              select new GenerateColumnJson
                                              {
                                                  Row = masterDoc.Row,
                                                  FirstRow = masterDoc.FirstRow,
                                                  LastRow = masterDoc.LastRow,
                                                  GroupTaskId = masterDoc.GroupTaskId,
                                                  Model = masterDoc.Model,
                                                  GroupName = masterDoc.GroupName,
                                                  Section = masterDoc.Section,
                                                  Description = masterDoc.Description,
                                                  Category = masterDoc.Category,
                                                  Number250 = masterDoc.Number250,
                                                  Number500 = masterDoc.Number500,
                                                  Number1000 = masterDoc.Number1000,
                                                  Number2000 = masterDoc.Number2000,
                                                  Number4000 = masterDoc.Number4000,
                                                  GuidTable = masterDoc.GuidTable,
                                                  ImageData = masterDoc.ImageData,
                                                  Table = masterDoc.Table,
                                                  ServiceMappingValue = masterDoc.ServiceMappingValue,
                                                  SOS = masterDoc.SOS,
                                                  SectionColumn = masterDoc.SectionColumn,
                                                  TaskKey = masterDoc.TaskKey,
                                                  IsCbm = jsonTypeDetail.isCbm,
                                                  IsCrack = jsonTypeDetail.isCrack,
                                                  IsDefectGroup = jsonTypeDetail.isDefectGroup,
                                                  HeaderName = jsonTypeDetail.headerName
                                              }).ToList();

                        #region Group Task Id
                        // CBM Populate Data
                        var dataColumnOrder = listGenerateColumn.Where(x => (x.IsCbm == "true" || x.IsDefectGroup == "true") &&
                                                                            (x.Number4000.ToLower().Contains("a") ||
                                                                             x.Number4000.ToLower().Contains("a1") ||
                                                                             x.Number4000.ToLower().Contains("b1") ||
                                                                             x.Number4000.Contains("c1") ||
                                                                             x.Number4000.Contains("d1"))).ToList();
                        dataColumnOrder.ForEach(c => c.FirstRow = true);
                        //dataColumnOrder.ForEach(c => c.GroupTaskId = Guid.NewGuid().ToString());

                        // NORMAL Populate Data
                        var dataColumnOrderNormal = listGenerateColumn.Where(x => (x.Category == EnumCategoryServiceSheet.Defect || x.Category == EnumCategoryServiceSheet.Service) &&
                                                                                  x.Number4000.Contains("a")).ToList();
                        //dataColumnOrderNormal.ForEach(c => c.GroupTaskId = Guid.NewGuid().ToString());

                        // CRACK Populate Data
                        var dataColumnOrderCrack = listGenerateColumn.Where(x => x.IsCrack == "true" &&
                                                                                 (x.Number4000.ToLower().Contains("a") ||
                                                                                  x.Number4000.ToLower().Contains("a1") ||
                                                                                  x.Number4000.ToLower().Contains("b1") ||
                                                                                  x.Number4000.ToLower().Contains("c1") ||
                                                                                  x.Number4000.ToLower().Contains("d1") ||
                                                                                  x.Number4000.ToLower().Contains("e1") ||
                                                                                  x.Number4000.ToLower().Contains("f1") ||
                                                                                  x.Number4000.ToLower().Contains("g1") ||
                                                                                  x.Number4000.ToLower().Contains("h1") ||
                                                                                  x.Number4000.ToLower().Contains("i1") ||
                                                                                  x.Number4000.ToLower().Contains("j1") ||
                                                                                  x.Number4000.ToLower().Contains("k1") ||
                                                                                  x.Number4000.ToLower().Contains("l1") ||
                                                                                  x.Number4000.ToLower().Contains("m1") ||
                                                                                  x.Number4000.ToLower().Contains("n1"))).ToList();

                        dataColumnOrderCrack = listGenerateColumn.Where(x => x.Category == EnumCategoryServiceSheet.Crack &&
                                                                                 (x.Number4000.ToLower().Contains("a1") ||
                                                                                  x.Number4000.ToLower().Contains("b1") ||
                                                                                  x.Number4000.ToLower().Contains("c1") ||
                                                                                  x.Number4000.ToLower().Contains("d1") ||
                                                                                  x.Number4000.ToLower().Contains("e1") ||
                                                                                  x.Number4000.ToLower().Contains("f1") ||
                                                                                  x.Number4000.ToLower().Contains("g1") ||
                                                                                  x.Number4000.ToLower().Contains("h1") ||
                                                                                  x.Number4000.ToLower().Contains("i1") ||
                                                                                  x.Number4000.ToLower().Contains("j1") ||
                                                                                  x.Number4000.ToLower().Contains("k1") ||
                                                                                  x.Number4000.ToLower().Contains("l1") ||
                                                                                  x.Number4000.ToLower().Contains("m1") ||
                                                                                  x.Number4000.ToLower().Contains("n1"))).ToList();

                        dataColumnOrderCrack.ForEach(c => c.FirstRow = true);
                        //dataColumnOrderCrack.ForEach(c => c.GroupTaskId = Guid.NewGuid().ToString());

                        // Assigment GroupTaskId per Group CBM
                        foreach (var itemGroupTask in dataColumnOrder)
                        {
                            var input = itemGroupTask.Number4000;

                            //if (input.Length == 3)
                            //{
                            //    var dataColum = listGenerateColumn.Where(x => !string.IsNullOrEmpty(x.Number4000) &&
                            //                                                  x.Number4000.Length == 3 &&
                            //                                                  digitsOnly.Replace(x.Number4000, "").Substring(0, 2) == digitsOnly.Replace(itemGroupTask.Number4000, "").Substring(0, 2)).ToList();
                            //    dataColum.ForEach(c => c.GroupTaskId = itemGroupTask.GroupTaskId);
                            //}
                            if (input.Length == 4)
                            {
                                var dataColum = listGenerateColumn.Where(x => !string.IsNullOrEmpty(x.Number4000) &&
                                                                              x.Number4000.Length == 4 &&
                                                                              digitsOnly.Replace(x.Number4000, "").Substring(0, 2) == digitsOnly.Replace(itemGroupTask.Number4000, "").Substring(0, 2)).ToList();
                                //dataColum.ForEach(c => c.GroupTaskId = itemGroupTask.GroupTaskId);
                            }
                            else if (input.Length == 5)
                            {
                                var dataColum = listGenerateColumn.Where(x => !string.IsNullOrEmpty(x.Number4000) &&
                                                                              x.Number4000.Length == 5 &&
                                                                              digitsOnly.Replace(x.Number4000, "").Substring(0, 3) == digitsOnly.Replace(itemGroupTask.Number4000, "").Substring(0, 3)).ToList();
                                //dataColum.ForEach(c => c.GroupTaskId = itemGroupTask.GroupTaskId);
                            }
                            else
                            {
                                var dataColum = listGenerateColumn.Where(x => !string.IsNullOrEmpty(x.Number4000) &&
                                                                              digitsOnly.Replace(x.Number4000, "") == digitsOnly.Replace(itemGroupTask.Number4000, "")).ToList();
                                //dataColum.ForEach(c => c.GroupTaskId = itemGroupTask.GroupTaskId);
                            }
                        }

                        // Set Last Row CBM
                        var dataColumnOrderLast = listGenerateColumn.Where(x => x.IsCbm == "true").OrderByDescending(x => x.Number4000).GroupBy(x => x.GroupTaskId).ToList();
                        foreach (var itemLast in dataColumnOrderLast)
                        {
                            var dataLastRow = itemLast.FirstOrDefault();
                            dataLastRow.LastRow = true;
                        }

                        // Assigment GroupTaskId per Group NORMAL
                        foreach (var itemGroupTaskNormal in dataColumnOrderNormal)
                        {
                            var input = itemGroupTaskNormal.Number4000;

                            if (input.Length == 3)
                            {
                                var dataColum = listGenerateColumn.Where(x => !string.IsNullOrEmpty(x.Number4000) &&
                                                                              x.Number4000.Length == 3 &&
                                                                              digitsOnly.Replace(x.Number4000, "").Substring(0, 2) == digitsOnly.Replace(itemGroupTaskNormal.Number4000, "").Substring(0, 2)).ToList();
                                //dataColum.ForEach(c => c.GroupTaskId = itemGroupTaskNormal.GroupTaskId);
                            }
                            else
                            {
                                var dataColum = listGenerateColumn.Where(x => !string.IsNullOrEmpty(x.Number4000) &&
                                                                          digitsOnly.Replace(x.Number4000, "") == digitsOnly.Replace(itemGroupTaskNormal.Number4000, "")).ToList();
                                //dataColum.ForEach(c => c.GroupTaskId = itemGroupTaskNormal.GroupTaskId);
                            }
                        }

                        // Assigment GroupTaskId per Group CRACK
                        foreach (var itemGroupTaskCrack in dataColumnOrderCrack)
                        {
                            if (itemGroupTaskCrack.Number4000.Length == 4)
                            {
                                var dataColum = listGenerateColumn.Where(x => !string.IsNullOrEmpty(x.Number4000) &&
                                                                         x.IsCrack == "true" && digitsOnly.Replace(x.Number4000, "").Substring(0, 3) == digitsOnly.Replace(itemGroupTaskCrack.Number4000, "").Substring(0, 3)).ToList();
                                //dataColum.ForEach(c => c.GroupTaskId = itemGroupTaskCrack.GroupTaskId);
                            }
                            else if (itemGroupTaskCrack.Number4000.Length == 5)
                            {
                                var dataColum = listGenerateColumn.Where(x => !string.IsNullOrEmpty(x.Number4000) &&
                                                                         x.IsCrack == "true" && digitsOnly.Replace(x.Number4000, "").Substring(0, 3) == digitsOnly.Replace(itemGroupTaskCrack.Number4000, "").Substring(0, 3)).ToList();
                                //dataColum.ForEach(c => c.GroupTaskId = itemGroupTaskCrack.GroupTaskId);
                            }
                            else
                            {
                                var dataColum = listGenerateColumn.Where(x => !string.IsNullOrEmpty(x.Number4000) &&
                                                                         x.IsCrack == "true" && digitsOnly.Replace(x.Number4000, "").Substring(0, 2) == digitsOnly.Replace(itemGroupTaskCrack.Number4000, "").Substring(0, 2)).ToList();
                                //dataColum.ForEach(c => c.GroupTaskId = itemGroupTaskCrack.GroupTaskId);
                            }
                        }

                        // Set Last Row CRACK
                        var dataColumnOrderCrackLast = listGenerateColumn.Where(x => x.IsCrack == "true").OrderByDescending(x => x.Number4000).GroupBy(x => x.GroupTaskId).ToList();
                        foreach (var itemLast in dataColumnOrderCrackLast)
                        {
                            var dataLastRow = itemLast.FirstOrDefault();
                            dataLastRow.LastRow = true;
                        }

                        // NORMAL GROUP Populate Data
                        var dataColumnOrderNormalGroup = listGenerateColumn.Where(x => x.IsDefectGroup == "true" &&
                                                                                        x.Number4000.Contains("a")).ToList();
                        dataColumnOrderNormalGroup.ForEach(c => c.FirstRow = true);
                        //dataColumnOrderNormalGroup.ForEach(c => c.GroupTaskId = Guid.NewGuid().ToString());

                        // Assigment GroupTaskId per Defect Group
                        foreach (var itemGroupTask in dataColumnOrderNormalGroup)
                        {
                            var dataColum = listGenerateColumn.Where(x => !string.IsNullOrEmpty(x.Number4000) && digitsOnly.Replace(x.Number4000, "") == digitsOnly.Replace(itemGroupTask.Number4000, "")).ToList();
                            //dataColum.ForEach(c => c.GroupTaskId = itemGroupTask.GroupTaskId);
                        }

                        var dataColumnOrderNormalGroupLast = listGenerateColumn.Where(x => x.IsDefectGroup == "true").OrderByDescending(x => x.Number4000).GroupBy(x => x.GroupTaskId).ToList();
                        foreach (var itemLast in dataColumnOrderNormalGroupLast)
                        {
                            var dataLastRow = itemLast.FirstOrDefault();
                            dataLastRow.LastRow = true;
                        }
                        #endregion

                        #region Get Config and Set Value Based on Excel File
                        foreach (var itemColumn in listGenerateColumn)
                        {
                            detailAddressRow = $"[{itemColumn.Row.ToString()}] - {itemColumn.Description}";

                            string category = string.Empty;
                            string rating = string.Empty;

                            //Dictionary<string, object> paramType = new Dictionary<string, object>();
                            //paramType.Add("type", itemColumn.Category);
                            //paramType.Add("sectionColumn", !string.IsNullOrEmpty(itemColumn.SectionColumn) ? itemColumn.SectionColumn : string.Empty);

                            //var resultType = await _repositoryGenerateJsonTypeData.GetDataByParam(paramType);

                            var resultTypeJson = jsonType.Where(x => x.type == itemColumn.Category);
                            if (!string.IsNullOrEmpty(itemColumn.SectionColumn))
                            {
                                resultTypeJson = resultTypeJson.Where(x => x.sectionColumn == itemColumn.SectionColumn);
                            }

                            var resultType = resultTypeJson.FirstOrDefault();

                            category = resultType != null ? resultType.category.ToString() : string.Empty;
                            rating = resultType != null ? resultType.rating.ToString() : string.Empty;

                            if (itemColumn.FirstRow && resultType != null && !string.IsNullOrEmpty(resultType.headerName.ToString()) && itemColumn.Category.Contains("Defect"))
                            {
                                var paramCbmHeader = new Dictionary<string, object>();
                                paramCbmHeader.Add("category", resultType.category.ToString());
                                paramCbmHeader.Add("rating", resultType.headerName.ToString());

                                resultHeaderDefectGroup = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);
                            }
                            else
                            {
                                resultHeaderDefectGroup = null;
                            }

                            if (itemColumn.FirstRow && resultType != null && !string.IsNullOrEmpty(resultType.headerName.ToString()) && itemColumn.Category.Contains("CBM"))
                            {
                                var paramCbmHeader = new Dictionary<string, object>();
                                paramCbmHeader.Add("category", resultType.category.ToString());
                                paramCbmHeader.Add("rating", resultType.headerName.ToString());

                                resultHeaderCbmAutomatic = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);

                                if (itemColumn.Category.Contains("ROLLER"))
                                {
                                    var paramCbmHeaderRoller = new Dictionary<string, object>();
                                    paramCbmHeaderRoller.Add("category", category);
                                    paramCbmHeaderRoller.Add("rating", EnumRatingServiceSheet.AUTOMATIC_HEADER_CARRY_ROLLER);

                                    resultHeaderCbmAutomaticRoller = await _repositoryGenerateJson.GetDataByParam(paramCbmHeaderRoller);
                                }
                            }
                            else
                            {
                                resultHeaderCbmAutomatic = null;
                            }


                            var param = new Dictionary<string, object>();
                            param.Add("category", category);
                            param.Add("rating", rating);

                            var dataJson = await _repositoryGenerateJson.GetDataByParam(param);

                            if (dataJson != null)
                            {
                                dynamic res = dataJson.content;
                                dynamic resCbmHeader = resultHeaderCbmAutomatic == null ? null : resultHeaderCbmAutomatic.content;
                                dynamic resCbmHeaderRoller = resultHeaderCbmAutomaticRoller == null ? null : resultHeaderCbmAutomaticRoller.content;

                                if (resCbmHeader != null)
                                {
                                    resCbmHeader.key = Guid.NewGuid().ToString();
                                    resCbmHeader.description250 = $"{itemColumn.Number250};;{itemColumn.Description}";
                                    resCbmHeader.description500 = $"{itemColumn.Number500};;{itemColumn.Description}";
                                    resCbmHeader.description1000 = $"{itemColumn.Number1000};;{itemColumn.Description}";
                                    resCbmHeader.description2000 = $"{itemColumn.Number2000};;{itemColumn.Description}";
                                    resCbmHeader.description4000 = $"{itemColumn.Number4000};;{itemColumn.Description}";

                                    foreach (var itemCbmHeader in resCbmHeader.items)
                                    {
                                        itemCbmHeader.key = Guid.NewGuid().ToString();
                                    }
                                }

                                if (resCbmHeaderRoller != null)
                                {
                                    resCbmHeaderRoller.key = Guid.NewGuid().ToString();

                                    foreach (var itemCbmHeaderRoller in resCbmHeaderRoller.items)
                                    {
                                        itemCbmHeaderRoller.key = Guid.NewGuid().ToString();
                                    }
                                }

                                dynamic resDefectGroupHeader = resultHeaderDefectGroup == null ? null : resultHeaderDefectGroup.content;

                                if (resDefectGroupHeader != null)
                                {
                                    resDefectGroupHeader.SectionData = itemColumn.Section;
                                    resDefectGroupHeader.key = Guid.NewGuid().ToString();
                                    resDefectGroupHeader.description250 = $"{itemColumn.Number250};;{itemColumn.Description}";
                                    resDefectGroupHeader.description500 = $"{itemColumn.Number500};;{itemColumn.Description}";
                                    resDefectGroupHeader.description1000 = $"{itemColumn.Number1000};;{itemColumn.Description}";
                                    resDefectGroupHeader.description2000 = $"{itemColumn.Number2000};;{itemColumn.Description}";
                                    resDefectGroupHeader.description4000 = $"{itemColumn.Number4000};;{itemColumn.Description}";

                                    foreach (var itemDefectGroupHeader in resDefectGroupHeader.items)
                                    {
                                        itemDefectGroupHeader.key = Guid.NewGuid().ToString();
                                    }
                                }

                                var detaiNumberData = charOnly.Matches(itemColumn.Number2000 == null ? string.Empty : itemColumn.Number2000).FirstOrDefault()?.Value;
                                var detailDescription = itemColumn.Description;

                                if (itemColumn.Description != null)
                                {
                                    if (detailDescription.Contains("a1. ")) { detailDescription = detailDescription.Replace("a1. ", ""); }
                                    else if (detailDescription.Contains("a2. ")) { detailDescription = detailDescription.Replace("a2. ", ""); }
                                    else if (detailDescription.Contains("b1. ")) { detailDescription = detailDescription.Replace("b1. ", ""); }
                                    else if (detailDescription.Contains("b2. ")) { detailDescription = detailDescription.Replace("b2. ", ""); }
                                    else if (detailDescription.Contains("a. ")) { detailDescription = detailDescription.Replace("a. ", ""); }
                                    else if (detailDescription.Contains("b. ")) { detailDescription = detailDescription.Replace("b. ", ""); }
                                    else if (detailDescription.Contains("c. ")) { detailDescription = detailDescription.Replace("c. ", ""); }
                                    else if (detailDescription.Contains("d. ")) { detailDescription = detailDescription.Replace("d. ", ""); }
                                    else if (detailDescription.Contains("e. ")) { detailDescription = detailDescription.Replace("e. ", ""); }
                                    else if (detailDescription.Contains("f. ")) { detailDescription = detailDescription.Replace("f. ", ""); }
                                    else if (detailDescription.Contains("g. ")) { detailDescription = detailDescription.Replace("g. ", ""); }
                                    else if (detailDescription.Contains("h. ")) { detailDescription = detailDescription.Replace("h. ", ""); }
                                    else if (detailDescription.Contains("i. ")) { detailDescription = detailDescription.Replace("i. ", ""); }
                                    else if (detailDescription.Contains("j. ")) { detailDescription = detailDescription.Replace("j. ", ""); }
                                    else if (detailDescription.Contains("k. ")) { detailDescription = detailDescription.Replace("k. ", ""); }
                                    else if (detailDescription.Contains("l. ")) { detailDescription = detailDescription.Replace("l. ", ""); }
                                    else if (detailDescription.Contains("m. ")) { detailDescription = detailDescription.Replace("m. ", ""); }
                                }

                                res.SectionData = itemColumn.Section;
                                if (!string.IsNullOrEmpty(itemColumn.TaskKey))
                                {
                                    res.key = itemColumn.TaskKey;
                                }
                                else
                                {
                                    res.key = Guid.NewGuid().ToString();
                                }

                                res.groupTaskId = itemColumn.GroupTaskId == null ? string.Empty : itemColumn.GroupTaskId;

                                //res.description = $"{itemColumn.Number4000};;{itemColumn.Description}";
                                //res.description250 = $"{itemColumn.Number250};;{itemColumn.Description}";
                                //res.description500 = $"{itemColumn.Number500};;{itemColumn.Description}";
                                //res.description1000 = $"{itemColumn.Number1000};;{itemColumn.Description}";
                                ////res.description2000 = $"{itemColumn.Number2000};;{itemColumn.Description}";
                                //res.description2000 = $"{charOnly.Replace(itemColumn.Number2000 == null ? string.Empty : itemColumn.Number2000, "#").Split("#")[0]};{detaiNumberData};{itemColumn.Description}";
                                //res.description4000 = $"{itemColumn.Number4000};;{itemColumn.Description}";

                                //res.description250 = $"{charOnly.Replace(itemColumn.Number250 == null ? string.Empty : itemColumn.Number250, "#").Split("#")[0]};{detaiNumberData};{itemColumn.Description}";
                                //res.description500 = $"{charOnly.Replace(itemColumn.Number500 == null ? string.Empty : itemColumn.Number500, "#").Split("#")[0]};{detaiNumberData};{itemColumn.Description}";
                                //res.description1000 = $"{charOnly.Replace(itemColumn.Number1000 == null ? string.Empty : itemColumn.Number1000, "#").Split("#")[0]};{detaiNumberData};{itemColumn.Description}";
                                //res.description2000 = $"{charOnly.Replace(itemColumn.Number2000 == null ? string.Empty : itemColumn.Number2000, "#").Split("#")[0]};{detaiNumberData};{detailDescription}";
                                //res.description4000 = $"{charOnly.Replace(itemColumn.Number4000 == null ? string.Empty : itemColumn.Number4000, "#").Split("#")[0]};{detaiNumberData};{itemColumn.Description}";

                                res.description250 = $"{charOnly.Replace(itemColumn.Number250 == null ? string.Empty : itemColumn.Number250, "#").Split("#")[0]};{detaiNumberData};{detailDescription}";
                                res.description500 = $"{charOnly.Replace(itemColumn.Number500 == null ? string.Empty : itemColumn.Number500, "#").Split("#")[0]};{detaiNumberData};{detailDescription}";
                                res.description1000 = $"{charOnly.Replace(itemColumn.Number1000 == null ? string.Empty : itemColumn.Number1000, "#").Split("#")[0]};{detaiNumberData};{detailDescription}";
                                res.description2000 = $"{charOnly.Replace(itemColumn.Number2000 == null ? string.Empty : itemColumn.Number2000, "#").Split("#")[0]};{detaiNumberData};{detailDescription}";
                                res.description4000 = $"{charOnly.Replace(itemColumn.Number4000 == null ? string.Empty : itemColumn.Number4000, "#").Split("#")[0]};{detaiNumberData};{detailDescription}";

                                if (itemColumn.Category == EnumCategoryServiceSheet.CBM_ADJUSTMENT) { res.adjustment.key = Guid.NewGuid().ToString(); }

                                string mappingKeyIdData = string.Empty;
                                foreach (var resItem in res.items)
                                {
                                    resItem.key = Guid.NewGuid().ToString();
                                    if (resItem.categoryItemType != null && resItem.categoryItemType == EnumCommonProperty.MappingParamKey)
                                    {
                                        mappingKeyIdData = resItem.key;
                                    }

                                    if (itemColumn.Category != EnumCategoryServiceSheet.CBM_CALCULATE && resItem.mappingKeyId != null && resItem.mappingKeyId == EnumCommonProperty.MappingParamKeyTag)
                                    {
                                        resItem.mappingKeyId = mappingKeyIdData;
                                    }

                                    if (resItem.isTaskValue != null && resItem.isTaskValue == "true")
                                    {
                                        res.valueDisabled = resItem.key;
                                    }

                                    if (itemColumn.Category == EnumCategoryServiceSheet.CBM_CALCULATE_RESULT && resItem.categoryItemType != null && resItem.categoryItemType == EnumCommonProperty.ResultParamRating)
                                    {
                                        //var takeOutChar = charOnly.Replace(itemColumn.Number4000, "#");
                                        //var splitNumber = takeOutChar.Split('#');

                                        List<dynamic> calculateAvg = psType4000.Where(x => x.typeCategory == EnumCommonProperty.CalculateAvg
                                                                                       //digitsOnly.Replace(x.value4000, "").Substring(0, 2) == digitsOnly.Replace(itemColumn.Number4000, "").Substring(0, 2)
                                                                                       //charOnly.Replace(x.Number4000, "#").Split("#")[0] == charOnly.Replace(itemColumn.Number4000, "#").Split("#").FirstOrDefault()
                                                                                       ).ToList();

                                        List<string> mappingKeys = new List<string>();

                                        // Target Calculate Key Id
                                        foreach (var itemCalculate in calculateAvg)
                                        {
                                            foreach (var items in itemCalculate.items)
                                            {
                                                if (items.targetCalculateKeyId != null)
                                                {
                                                    items.targetCalculateKeyId = resItem.key;
                                                }

                                                if (items.category == EnumCommonProperty.CbmCalculateAvg)
                                                {
                                                    mappingKeys.Add(items.key.ToString());
                                                }
                                            }
                                        }

                                        // mappingKeyId
                                        foreach (var itemCalculate in calculateAvg)
                                        {
                                            foreach (var items in itemCalculate.items)
                                            {
                                                if (items.category == EnumCommonProperty.CbmCalculateAvg)
                                                {
                                                    items.mappingKeyId = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(mappingKeys));
                                                }
                                            }
                                        }

                                        //resItem.mappingKeyId = mappingKeyIdData;
                                    }

                                    switch (resItem.value.ToString())
                                    {
                                        case "<<itemDetailNumber>>":
                                            if (!string.IsNullOrEmpty(itemColumn.Number250)) { resItem.value250 = itemColumn.Number250; }
                                            if (!string.IsNullOrEmpty(itemColumn.Number500)) { resItem.value500 = itemColumn.Number500; }
                                            if (!string.IsNullOrEmpty(itemColumn.Number1000)) { resItem.value1000 = itemColumn.Number1000; }
                                            if (!string.IsNullOrEmpty(itemColumn.Number2000)) { resItem.value2000 = itemColumn.Number2000; }
                                            if (!string.IsNullOrEmpty(itemColumn.Number4000)) { resItem.value4000 = itemColumn.Number4000; }

                                            break;
                                        case "<<itemNumber>>":
                                            #region Numbering 250
                                            if (!string.IsNullOrEmpty(itemColumn.Number250))
                                            {
                                                if (charOnly.IsMatch(itemColumn.Number250))
                                                {
                                                    if (itemColumn.IsCbm == "true")
                                                    {
                                                        var input = itemColumn.Number250;
                                                        if (input.Length == 2)
                                                        {
                                                            var pattern = new Regex(@"\d{1}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value250 = resulttt.Replace(itemColumn.Number250, "");
                                                        }
                                                        else if (input.Length == 3 || input.Length == 4)
                                                        {
                                                            var takeOutChar = charOnly.Replace(itemColumn.Number250, "#");
                                                            var splitNumber = takeOutChar.Split('#');

                                                            if (splitNumber.Length > 0 && splitNumber[0].Length == 3)
                                                            {
                                                                var pattern = new Regex(@"\d{3}");
                                                                var resulttt = pattern.Replace(input, "");

                                                                resItem.value250 = resulttt.Replace(itemColumn.Number250, "");
                                                            }
                                                            else
                                                            {
                                                                var pattern = new Regex(@"\d{2}");
                                                                var resulttt = pattern.Replace(input, "");

                                                                resItem.value250 = resulttt.Replace(itemColumn.Number250, "");
                                                            }
                                                        }
                                                        else
                                                        {
                                                            var pattern = new Regex(@"\d{3}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value250 = resulttt.Replace(itemColumn.Number250, "");
                                                        }

                                                        res.description250 = $"{charOnly.Replace(itemColumn.Number250 == null ? string.Empty : itemColumn.Number250, "#").Split("#")[0]};{resItem.value250};{detailDescription}";
                                                    }
                                                    else if (itemColumn.Category == "Crack")
                                                    {
                                                        var input = itemColumn.Number250;
                                                        if (input.Length == 2)
                                                        {
                                                            var pattern = new Regex(@"\d{1}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value250 = resulttt.Replace(itemColumn.Number250, "");
                                                        }
                                                        else if (input.Length == 3 || input.Length == 4)
                                                        {
                                                            var pattern = new Regex(@"\d{2}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value250 = resulttt.Replace(itemColumn.Number250, "");
                                                        }
                                                        else
                                                        {
                                                            var pattern = new Regex(@"\d{3}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            var firstCrack = resulttt.Replace(itemColumn.Number250, "");
                                                            if (firstCrack.Contains("2"))
                                                            {
                                                                resItem.style.visibility = "hidden";
                                                            }

                                                            var valueNumber = itemColumn.Number250;

                                                            if (valueNumber.Contains("1"))
                                                            {
                                                                valueNumber = valueNumber.Substring(0, 3);
                                                            }
                                                            resItem.value250 = valueNumber;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        resItem.value250 = itemColumn.Number250;
                                                        resItem.style.visibility = "hidden";
                                                    }
                                                }
                                                else
                                                {
                                                    resItem.value250 = itemColumn.Number250;
                                                }
                                            }
                                            #endregion

                                            #region Numbering 500
                                            if (!string.IsNullOrEmpty(itemColumn.Number500))
                                            {
                                                if (charOnly.IsMatch(itemColumn.Number500))
                                                {
                                                    if (itemColumn.IsCbm == "true")
                                                    {
                                                        var input = itemColumn.Number500;
                                                        if (input.Length == 2)
                                                        {
                                                            var pattern = new Regex(@"\d{1}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value500 = resulttt.Replace(itemColumn.Number500, "");
                                                        }
                                                        else if (input.Length == 3 || input.Length == 4)
                                                        {
                                                            var takeOutChar = charOnly.Replace(itemColumn.Number500, "#");
                                                            var splitNumber = takeOutChar.Split('#');

                                                            if (splitNumber.Length > 0 && splitNumber[0].Length == 3)
                                                            {
                                                                var pattern = new Regex(@"\d{3}");
                                                                var resulttt = pattern.Replace(input, "");

                                                                resItem.value500 = resulttt.Replace(itemColumn.Number500, "");
                                                            }
                                                            else
                                                            {
                                                                var pattern = new Regex(@"\d{2}");
                                                                var resulttt = pattern.Replace(input, "");

                                                                resItem.value500 = resulttt.Replace(itemColumn.Number500, "");
                                                            }
                                                        }
                                                        else
                                                        {
                                                            var pattern = new Regex(@"\d{3}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value500 = resulttt.Replace(itemColumn.Number500, "");
                                                        }

                                                        res.description500 = $"{charOnly.Replace(itemColumn.Number500 == null ? string.Empty : itemColumn.Number500, "#").Split("#")[0]};{resItem.value500};{detailDescription}";
                                                    }
                                                    else if (itemColumn.Category == "Crack")
                                                    {
                                                        var input = itemColumn.Number500;
                                                        var value500 = "";
                                                        if (input.Length == 2)
                                                        {
                                                            var pattern = new Regex(@"\d{1}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value500 = resulttt.Replace(itemColumn.Number500, "");
                                                        }
                                                        else if (input.Length == 3 || input.Length == 4)
                                                        {
                                                            var pattern = new Regex(@"\d{2}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value500 = resulttt.Replace(itemColumn.Number500, "");
                                                        }
                                                        else
                                                        {
                                                            var pattern = new Regex(@"\d{3}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            var firstCrack = resulttt.Replace(itemColumn.Number500, "");
                                                            if (firstCrack.Contains("2"))
                                                            {
                                                                resItem.style.visibility = "hidden";
                                                            }

                                                            var valueNumber = itemColumn.Number500;

                                                            if (valueNumber.Contains("1"))
                                                            {
                                                                valueNumber = valueNumber.Substring(0, 3);
                                                            }
                                                            resItem.value500 = valueNumber;
                                                            value500 = resulttt.Replace(itemColumn.Number500, "");
                                                        }
                                                        res.description500 = $"{charOnly.Replace(itemColumn.Number500 == null ? string.Empty : itemColumn.Number500, "#").Split("#")[0]};{value500};{detailDescription}";
                                                    }
                                                    else
                                                    {
                                                        resItem.value500 = itemColumn.Number500;
                                                        resItem.style.visibility = "hidden";
                                                    }
                                                }
                                                else
                                                {
                                                    resItem.value500 = itemColumn.Number500;
                                                }
                                            }
                                            #endregion

                                            #region Numbering 1000
                                            if (!string.IsNullOrEmpty(itemColumn.Number1000))
                                            {
                                                if (charOnly.IsMatch(itemColumn.Number1000))
                                                {
                                                    if (itemColumn.IsCbm == "true")
                                                    {
                                                        var input = itemColumn.Number1000;
                                                        if (input.Length == 2)
                                                        {
                                                            var pattern = new Regex(@"\d{1}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value1000 = resulttt.Replace(itemColumn.Number1000, "");
                                                        }
                                                        else if (input.Length == 3 || input.Length == 4)
                                                        {
                                                            var takeOutChar = charOnly.Replace(itemColumn.Number1000, "#");

                                                            var splitNumber = takeOutChar.Split('#');

                                                            if (splitNumber.Length > 0 && splitNumber[0].Length == 3)
                                                            {
                                                                var pattern = new Regex(@"\d{3}");
                                                                var resulttt = pattern.Replace(input, "");

                                                                resItem.value1000 = resulttt.Replace(itemColumn.Number1000, "");
                                                            }
                                                            else
                                                            {
                                                                var pattern = new Regex(@"\d{2}");
                                                                var resulttt = pattern.Replace(input, "");

                                                                resItem.value1000 = resulttt.Replace(itemColumn.Number1000, "");
                                                            }
                                                        }
                                                        else
                                                        {
                                                            var pattern = new Regex(@"\d{3}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value1000 = resulttt.Replace(itemColumn.Number1000, "");
                                                        }

                                                        res.description1000 = $"{charOnly.Replace(itemColumn.Number1000 == null ? string.Empty : itemColumn.Number1000, "#").Split("#")[0]};{resItem.value1000};{detailDescription}";
                                                    }
                                                    else if (itemColumn.Category == "Crack")
                                                    {
                                                        var input = itemColumn.Number1000;
                                                        var value1000 = "";
                                                        if (input.Length == 2)
                                                        {
                                                            var pattern = new Regex(@"\d{1}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value1000 = resulttt.Replace(itemColumn.Number1000, "");
                                                        }
                                                        else if (input.Length == 3 || input.Length == 4)
                                                        {
                                                            var pattern = new Regex(@"\d{2}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value1000 = resulttt.Replace(itemColumn.Number1000, "");
                                                        }
                                                        else
                                                        {
                                                            var pattern = new Regex(@"\d{3}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            var firstCrack = resulttt.Replace(itemColumn.Number1000, "");
                                                            if (firstCrack.Contains("2"))
                                                            {
                                                                resItem.style.visibility = "hidden";
                                                            }

                                                            var valueNumber = itemColumn.Number1000;

                                                            if (valueNumber.Contains("1"))
                                                            {
                                                                valueNumber = valueNumber.Substring(0, 3);
                                                            }
                                                            resItem.value1000 = valueNumber;
                                                            value1000 = resulttt.Replace(itemColumn.Number1000, "");
                                                        }

                                                        res.description1000 = $"{charOnly.Replace(itemColumn.Number1000 == null ? string.Empty : itemColumn.Number1000, "#").Split("#")[0]};{value1000};{detailDescription}";
                                                    }
                                                    else
                                                    {
                                                        resItem.value1000 = itemColumn.Number1000;
                                                        resItem.style.visibility = "hidden";
                                                    }
                                                }
                                                else
                                                {
                                                    resItem.value1000 = itemColumn.Number1000;
                                                }
                                            }
                                            #endregion

                                            #region Numbering 2000
                                            if (!string.IsNullOrEmpty(itemColumn.Number2000))
                                            {
                                                if (charOnly.IsMatch(itemColumn.Number2000))
                                                {
                                                    if (itemColumn.IsCbm == "true")
                                                    {
                                                        var input = itemColumn.Number2000;
                                                        if (input.Length == 2)
                                                        {
                                                            var pattern = new Regex(@"\d{1}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value2000 = resulttt.Replace(itemColumn.Number2000, "");
                                                        }
                                                        else if (input.Length == 3 || input.Length == 4)
                                                        {
                                                            var takeOutChar = charOnly.Replace(itemColumn.Number2000, "#");
                                                            var splitNumber = takeOutChar.Split('#');

                                                            if (splitNumber.Length > 0 && splitNumber[0].Length == 3)
                                                            {
                                                                var pattern = new Regex(@"\d{3}");
                                                                var resulttt = pattern.Replace(input, "");

                                                                resItem.value2000 = resulttt.Replace(itemColumn.Number2000, "");
                                                            }
                                                            else
                                                            {
                                                                var pattern = new Regex(@"\d{2}");
                                                                var resulttt = pattern.Replace(input, "");

                                                                resItem.value2000 = resulttt.Replace(itemColumn.Number2000, "");
                                                            }
                                                        }
                                                        else
                                                        {
                                                            var pattern = new Regex(@"\d{3}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value2000 = resulttt.Replace(itemColumn.Number2000, "");
                                                        }

                                                        res.description2000 = $"{charOnly.Replace(itemColumn.Number2000 == null ? string.Empty : itemColumn.Number2000, "#").Split("#")[0]};{resItem.value2000};{detailDescription}";
                                                    }
                                                    else if (itemColumn.Category == "Crack")
                                                    {
                                                        var input = itemColumn.Number2000;
                                                        var value2000 = "";
                                                        if (input.Length == 2)
                                                        {
                                                            var pattern = new Regex(@"\d{1}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value2000 = resulttt.Replace(itemColumn.Number2000, "");
                                                        }
                                                        else if (input.Length == 3 || input.Length == 4)
                                                        {
                                                            var pattern = new Regex(@"\d{2}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value2000 = resulttt.Replace(itemColumn.Number2000, "");
                                                        }
                                                        else
                                                        {
                                                            var pattern = new Regex(@"\d{3}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            var firstCrack = resulttt.Replace(itemColumn.Number2000, "");
                                                            if (firstCrack.Contains("2"))
                                                            {
                                                                resItem.style.visibility = "hidden";
                                                            }

                                                            var valueNumber = itemColumn.Number2000;

                                                            if (valueNumber.Contains("1"))
                                                            {
                                                                valueNumber = valueNumber.Substring(0, 3);
                                                            }
                                                            resItem.value2000 = valueNumber;
                                                            value2000 = resulttt.Replace(itemColumn.Number2000, "");
                                                        }

                                                        res.description2000 = $"{charOnly.Replace(itemColumn.Number2000 == null ? string.Empty : itemColumn.Number2000, "#").Split("#")[0]};{value2000};{detailDescription}";
                                                    }
                                                    else
                                                    {
                                                        resItem.value2000 = itemColumn.Number2000;
                                                        resItem.style.visibility = "hidden";
                                                    }
                                                }
                                                else
                                                {
                                                    resItem.value2000 = itemColumn.Number2000;
                                                }
                                            }
                                            #endregion

                                            #region Numbering 4000
                                            if (!string.IsNullOrEmpty(itemColumn.Number4000))
                                            {
                                                if (charOnly.IsMatch(itemColumn.Number4000))
                                                {
                                                    if (itemColumn.IsCbm == "true")
                                                    {
                                                        var input = itemColumn.Number4000;
                                                        if (input.Length == 2)
                                                        {
                                                            var pattern = new Regex(@"\d{1}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value4000 = resulttt.Replace(itemColumn.Number4000, "");
                                                        }
                                                        else if (input.Length == 3 || input.Length == 4)
                                                        {
                                                            var takeOutChar = charOnly.Replace(itemColumn.Number4000, "#");
                                                            var splitNumber = takeOutChar.Split('#');

                                                            if (splitNumber.Length > 0 && splitNumber[0].Length == 3)
                                                            {
                                                                var pattern = new Regex(@"\d{3}");
                                                                var resulttt = pattern.Replace(input, "");

                                                                resItem.value4000 = resulttt.Replace(itemColumn.Number4000, "");
                                                            }
                                                            else
                                                            {
                                                                var pattern = new Regex(@"\d{2}");
                                                                var resulttt = pattern.Replace(input, "");

                                                                resItem.value4000 = resulttt.Replace(itemColumn.Number4000, "");
                                                            }
                                                        }
                                                        else
                                                        {
                                                            var pattern = new Regex(@"\d{3}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value4000 = resulttt.Replace(itemColumn.Number4000, "");
                                                        }

                                                        res.description4000 = $"{charOnly.Replace(itemColumn.Number4000 == null ? string.Empty : itemColumn.Number4000, "#").Split("#")[0]};{resItem.value4000};{detailDescription}";
                                                    }
                                                    else if (itemColumn.Category == "Crack")
                                                    {
                                                        var input = itemColumn.Number4000;
                                                        var value4000 = "";
                                                        if (input.Length == 2)
                                                        {
                                                            var pattern = new Regex(@"\d{1}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value4000 = resulttt.Replace(itemColumn.Number4000, "");
                                                        }
                                                        else if (input.Length == 3 || input.Length == 4)
                                                        {
                                                            var pattern = new Regex(@"\d{2}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            resItem.value4000 = resulttt.Replace(itemColumn.Number4000, "");
                                                        }
                                                        else
                                                        {
                                                            var pattern = new Regex(@"\d{3}");
                                                            var resulttt = pattern.Replace(input, "");

                                                            var firstCrack = resulttt.Replace(itemColumn.Number4000, "");
                                                            if (firstCrack.Contains("2"))
                                                            {
                                                                resItem.style.visibility = "hidden";
                                                            }

                                                            var valueNumber = itemColumn.Number4000;

                                                            if (valueNumber.Contains("1"))
                                                            {
                                                                valueNumber = valueNumber.Substring(0, 3);
                                                            }
                                                            resItem.value4000 = valueNumber;
                                                            value4000 = resulttt.Replace(itemColumn.Number4000, "");

                                                        }

                                                        res.description4000 = $"{charOnly.Replace(itemColumn.Number4000 == null ? string.Empty : itemColumn.Number4000, "#").Split("#")[0]};{value4000};{detailDescription}";
                                                    }
                                                    else
                                                    {
                                                        resItem.value4000 = itemColumn.Number4000;
                                                        resItem.style.visibility = "hidden";
                                                    }
                                                }
                                                else
                                                {
                                                    resItem.value4000 = itemColumn.Number4000;
                                                }
                                            }
                                            #endregion

                                            break;
                                        case "<<itemCrackCode>>":
                                            #region Numbering 250
                                            if (!string.IsNullOrEmpty(itemColumn.Number250))
                                            {
                                                if (itemColumn.Category == "Crack" && charOnly.IsMatch(itemColumn.Number250))
                                                {
                                                    var input = itemColumn.Number250;
                                                    if (input.Length == 2)
                                                    {
                                                        var pattern = new Regex(@"\d{1}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        resItem.Number250 = resulttt.Replace(itemColumn.Number250, "");
                                                    }
                                                    else if (input.Length == 3 || input.Length == 4)
                                                    {
                                                        var pattern = new Regex(@"\d{2}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        resItem.Number250 = resulttt.Replace(itemColumn.Number250, "");
                                                    }
                                                    else
                                                    {
                                                        var pattern = new Regex(@"\d{3}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        var firstCrack = resulttt.Replace(itemColumn.Number250, "");

                                                        resItem.Number250 = firstCrack.ToUpper();
                                                    }
                                                }
                                            }
                                            #endregion

                                            #region Numbering 500
                                            if (!string.IsNullOrEmpty(itemColumn.Number500))
                                            {
                                                if (itemColumn.Category == "Crack" && charOnly.IsMatch(itemColumn.Number500))
                                                {
                                                    var input = itemColumn.Number500;
                                                    if (input.Length == 2)
                                                    {
                                                        var pattern = new Regex(@"\d{1}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        resItem.Number500 = resulttt.Replace(itemColumn.Number500, "");
                                                    }
                                                    else if (input.Length == 3 || input.Length == 4)
                                                    {
                                                        var pattern = new Regex(@"\d{2}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        resItem.Number500 = resulttt.Replace(itemColumn.Number500, "");
                                                    }
                                                    else
                                                    {
                                                        var pattern = new Regex(@"\d{3}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        var firstCrack = resulttt.Replace(itemColumn.Number500, "");

                                                        resItem.Number500 = firstCrack.ToUpper();
                                                    }
                                                }
                                            }
                                            #endregion

                                            #region Numbering 1000
                                            if (!string.IsNullOrEmpty(itemColumn.Number1000))
                                            {
                                                if (itemColumn.Category == "Crack" && charOnly.IsMatch(itemColumn.Number1000))
                                                {
                                                    var input = itemColumn.Number1000;
                                                    if (input.Length == 2)
                                                    {
                                                        var pattern = new Regex(@"\d{1}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        resItem.Number1000 = resulttt.Replace(itemColumn.Number1000, "");
                                                    }
                                                    else if (input.Length == 3 || input.Length == 4)
                                                    {
                                                        var pattern = new Regex(@"\d{2}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        resItem.Number1000 = resulttt.Replace(itemColumn.Number1000, "");
                                                    }
                                                    else
                                                    {
                                                        var pattern = new Regex(@"\d{3}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        var firstCrack = resulttt.Replace(itemColumn.Number1000, "");

                                                        resItem.Number1000 = firstCrack.ToUpper();
                                                    }
                                                }
                                            }
                                            #endregion

                                            #region Numbering 2000
                                            if (!string.IsNullOrEmpty(itemColumn.Number2000))
                                            {
                                                if (itemColumn.Category == "Crack" && charOnly.IsMatch(itemColumn.Number2000))
                                                {
                                                    var input = itemColumn.Number2000;
                                                    if (input.Length == 2)
                                                    {
                                                        var pattern = new Regex(@"\d{1}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        resItem.Number2000 = resulttt.Replace(itemColumn.Number2000, "");
                                                    }
                                                    else if (input.Length == 3 || input.Length == 4)
                                                    {
                                                        var pattern = new Regex(@"\d{2}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        resItem.Number2000 = resulttt.Replace(itemColumn.Number2000, "");
                                                    }
                                                    else
                                                    {
                                                        var pattern = new Regex(@"\d{3}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        var firstCrack = resulttt.Replace(itemColumn.Number2000, "");

                                                        resItem.Number2000 = firstCrack.ToUpper();
                                                    }
                                                }
                                            }
                                            #endregion

                                            #region Numbering 4000
                                            if (!string.IsNullOrEmpty(itemColumn.Number4000))
                                            {
                                                if (itemColumn.Category == "Crack" && charOnly.IsMatch(itemColumn.Number4000))
                                                {
                                                    var input = itemColumn.Number4000;
                                                    if (input.Length == 2)
                                                    {
                                                        var pattern = new Regex(@"\d{1}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        resItem.Number4000 = resulttt.Replace(itemColumn.Number4000, "");
                                                    }
                                                    else if (input.Length == 3 || input.Length == 4)
                                                    {
                                                        var pattern = new Regex(@"\d{2}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        resItem.Number4000 = resulttt.Replace(itemColumn.Number4000, "");
                                                    }
                                                    else
                                                    {
                                                        var pattern = new Regex(@"\d{3}");
                                                        var resulttt = pattern.Replace(input, "");

                                                        var firstCrack = resulttt.Replace(itemColumn.Number4000, "");

                                                        resItem.Number4000 = firstCrack.ToUpper();
                                                    }
                                                }
                                            }
                                            #endregion
                                            break;
                                        case "<<itemDesc>>":
                                            var dataDesc = itemColumn.Description;

                                            if (itemColumn.Description != null && itemColumn.IsCbm == "true")
                                            {
                                                if (dataDesc.Contains("a1. ")) { dataDesc = dataDesc.Replace("a1. ", ""); }
                                                else if (dataDesc.Contains("a2. ")) { dataDesc = dataDesc.Replace("a2. ", ""); }
                                                else if (dataDesc.Contains("b1. ")) { dataDesc = dataDesc.Replace("b1. ", ""); }
                                                else if (dataDesc.Contains("b2. ")) { dataDesc = dataDesc.Replace("b2. ", ""); }
                                                else if (dataDesc.Contains("a. ")) { dataDesc = dataDesc.Replace("a. ", ""); }
                                                else if (dataDesc.Contains("b. ")) { dataDesc = dataDesc.Replace("b. ", ""); }
                                                else if (dataDesc.Contains("c. ")) { dataDesc = dataDesc.Replace("c. ", ""); }
                                                else if (dataDesc.Contains("d. ")) { dataDesc = dataDesc.Replace("d. ", ""); }
                                                else if (dataDesc.Contains("e. ")) { dataDesc = dataDesc.Replace("e. ", ""); }
                                                else if (dataDesc.Contains("f. ")) { dataDesc = dataDesc.Replace("f. ", ""); }
                                                else if (dataDesc.Contains("g. ")) { dataDesc = dataDesc.Replace("g. ", ""); }
                                                else if (dataDesc.Contains("h. ")) { dataDesc = dataDesc.Replace("h. ", ""); }
                                                else if (dataDesc.Contains("i. ")) { dataDesc = dataDesc.Replace("i. ", ""); }
                                                else if (dataDesc.Contains("j. ")) { dataDesc = dataDesc.Replace("j. ", ""); }
                                                else if (dataDesc.Contains("k. ")) { dataDesc = dataDesc.Replace("k. ", ""); }
                                                else if (dataDesc.Contains("l. ")) { dataDesc = dataDesc.Replace("l. ", ""); }
                                                else if (dataDesc.Contains("m. ")) { dataDesc = dataDesc.Replace("m. ", ""); }
                                            }

                                            resItem.value = dataDesc;
                                            break;
                                        case "<<section>>":
                                            resItem.value = itemColumn.Description;
                                            break;
                                        case "<<itemMapping>>":
                                            resItem.value = itemColumn.ServiceMappingValue;
                                            break;
                                    }

                                    if (itemColumn.Category != EnumCategoryServiceSheet.Crack && itemColumn.LastRow)
                                    {
                                        if (resItem.style.border != null)
                                        {
                                            resItem.style.border.bottom = EnumCommonProperty.StyleBorder;
                                        }
                                    }

                                    if (itemColumn.Category == EnumCategoryServiceSheet.Crack && itemColumn.FirstRow)
                                    {
                                        if (resItem.style.border != null)
                                        {
                                            resItem.style.border.top = EnumCommonProperty.StyleBorder;
                                        }
                                    }

                                    if (itemColumn.Category == EnumCategoryServiceSheet.Crack && itemColumn.LastRow)
                                    {
                                        if (resItem.style.border != null)
                                        {
                                            resItem.style.border.bottom = EnumCommonProperty.StyleBorder;
                                        }
                                    }
                                }

                                if (itemColumn.IsCbm == "true" && resCbmHeader != null)
                                {
                                    resCbmHeader.SectionData = itemColumn.Section;

                                    if (resCbmHeaderRoller != null)
                                    {
                                        foreach (var itemDataHeader in resCbmHeader.items)
                                        {
                                            if (itemDataHeader.style.border != null)
                                            {
                                                itemDataHeader.style.border.top = "none";
                                            }
                                        }

                                        resCbmHeaderRoller.SectionData = itemColumn.Section;

                                        if (!string.IsNullOrEmpty(itemColumn.Number250)) { psType250.Add(resCbmHeaderRoller); }
                                        if (!string.IsNullOrEmpty(itemColumn.Number500)) { psType500.Add(resCbmHeaderRoller); }
                                        if (!string.IsNullOrEmpty(itemColumn.Number1000)) { psType1000.Add(resCbmHeaderRoller); }
                                        if (!string.IsNullOrEmpty(itemColumn.Number2000)) { psType2000.Add(resCbmHeaderRoller); }
                                        if (!string.IsNullOrEmpty(itemColumn.Number4000)) { psType4000.Add(resCbmHeaderRoller); }
                                    }

                                    // Insert CBM Header
                                    if (!string.IsNullOrEmpty(itemColumn.Number250)) { psType250.Add(resCbmHeader); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number500)) { psType500.Add(resCbmHeader); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number1000)) { psType1000.Add(resCbmHeader); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number2000)) { psType2000.Add(resCbmHeader); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number4000)) { psType4000.Add(resCbmHeader); }

                                }

                                if ((itemColumn.Category == EnumCategoryServiceSheet.Defect_Group || itemColumn.Category == EnumCategoryServiceSheet.DEFECT_MEASUREMENT || itemColumn.Category == EnumCategoryServiceSheet.Service_With_Header) && resDefectGroupHeader != null)
                                {
                                    // Insert CBM Header
                                    if (!string.IsNullOrEmpty(itemColumn.Number250)) { psType250.Add(resDefectGroupHeader); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number500)) { psType500.Add(resDefectGroupHeader); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number1000)) { psType1000.Add(resDefectGroupHeader); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number2000)) { psType2000.Add(resDefectGroupHeader); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number4000)) { psType4000.Add(resDefectGroupHeader); }

                                }

                                // Insert Table Schema
                                if (!string.IsNullOrEmpty(itemColumn.Table))
                                {
                                    if (itemColumn.Table == EnumTableServiceSheet.LUBRICANT)
                                    {
                                        var paramCbmHeader = new Dictionary<string, object>();
                                        paramCbmHeader.Add("category", EnumCategoryServiceSheet.TABLE);
                                        paramCbmHeader.Add("rating", EnumRatingServiceSheet.LUBRICANT);

                                        var resultGuid = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);
                                        var resGuide = resultGuid.content;
                                        resGuide.key = Guid.NewGuid().ToString();
                                        resGuide.SectionData = itemColumn.Section;

                                        if (!string.IsNullOrEmpty(itemColumn.Number250)) { psType250.Add(resGuide); }
                                        if (!string.IsNullOrEmpty(itemColumn.Number500)) { psType500.Add(resGuide); }
                                        if (!string.IsNullOrEmpty(itemColumn.Number1000)) { psType1000.Add(resGuide); }
                                        if (!string.IsNullOrEmpty(itemColumn.Number2000)) { psType2000.Add(resGuide); }
                                        if (!string.IsNullOrEmpty(itemColumn.Number4000)) { psType4000.Add(resGuide); }
                                    }
                                    else if (itemColumn.Table == EnumTableServiceSheet.BATTERY)
                                    {
                                        var paramCbmHeader = new Dictionary<string, object>();
                                        paramCbmHeader.Add("category", EnumCategoryServiceSheet.TABLE);
                                        paramCbmHeader.Add("rating", EnumRatingServiceSheet.BATTERY);

                                        var resultGuid = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);
                                        var resGuide = resultGuid.content;
                                        resGuide.key = Guid.NewGuid().ToString();
                                        resGuide.SectionData = itemColumn.Section;

                                        if (!string.IsNullOrEmpty(itemColumn.Number250)) { psType250.Add(resGuide); }
                                        if (!string.IsNullOrEmpty(itemColumn.Number500)) { psType500.Add(resGuide); }
                                        if (!string.IsNullOrEmpty(itemColumn.Number1000)) { psType1000.Add(resGuide); }
                                        if (!string.IsNullOrEmpty(itemColumn.Number2000)) { psType2000.Add(resGuide); }
                                        if (!string.IsNullOrEmpty(itemColumn.Number4000)) { psType4000.Add(resGuide); }
                                    }
                                    else if (itemColumn.Table == EnumTableServiceSheet.BATTERY_CCA)
                                    {
                                        var paramCbmHeader = new Dictionary<string, object>();
                                        paramCbmHeader.Add("category", EnumCategoryServiceSheet.TABLE);
                                        paramCbmHeader.Add("rating", EnumRatingServiceSheet.BATTERY_CCA);

                                        var resultGuid = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);
                                        var resGuide = resultGuid.content;
                                        resGuide.key = Guid.NewGuid().ToString();
                                        resGuide.SectionData = itemColumn.Section;

                                        if (!string.IsNullOrEmpty(itemColumn.Number250)) { psType250.Add(resGuide); }
                                        if (!string.IsNullOrEmpty(itemColumn.Number500)) { psType500.Add(resGuide); }
                                        if (!string.IsNullOrEmpty(itemColumn.Number1000)) { psType1000.Add(resGuide); }
                                        if (!string.IsNullOrEmpty(itemColumn.Number2000)) { psType2000.Add(resGuide); }
                                        if (!string.IsNullOrEmpty(itemColumn.Number4000)) { psType4000.Add(resGuide); }
                                    }
                                    else
                                    {
                                        var paramCbmHeader = new Dictionary<string, object>();
                                        paramCbmHeader.Add("category", EnumCategoryServiceSheet.TABLE);
                                        paramCbmHeader.Add("rating", itemColumn.Table);

                                        var resultGuid = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);
                                        var resGuide = resultGuid.content;
                                        resGuide.key = Guid.NewGuid().ToString();
                                        resGuide.SectionData = itemColumn.Section;

                                        if (!string.IsNullOrEmpty(itemColumn.Number250)) { psType250.Add(resGuide); }
                                        if (!string.IsNullOrEmpty(itemColumn.Number500)) { psType500.Add(resGuide); }
                                        if (!string.IsNullOrEmpty(itemColumn.Number1000)) { psType1000.Add(resGuide); }
                                        if (!string.IsNullOrEmpty(itemColumn.Number2000)) { psType2000.Add(resGuide); }
                                        if (!string.IsNullOrEmpty(itemColumn.Number4000)) { psType4000.Add(resGuide); }
                                    }
                                }

                                // Insert MAIN DATA
                                if (!string.IsNullOrEmpty(itemColumn.Number250)) { psType250.Add(res); }
                                if (!string.IsNullOrEmpty(itemColumn.Number500)) { psType500.Add(res); }
                                if (!string.IsNullOrEmpty(itemColumn.Number1000)) { psType1000.Add(res); }
                                if (!string.IsNullOrEmpty(itemColumn.Number2000)) { psType2000.Add(res); }
                                if (!string.IsNullOrEmpty(itemColumn.Number4000)) { psType4000.Add(res); }

                                // Insert Image
                                if (!string.IsNullOrEmpty(itemColumn.ImageData))
                                {
                                    itemColumn.ImageData = itemColumn.ImageData.Contains("YES") ? "135" : itemColumn.ImageData;

                                    var paramCbmHeader = new Dictionary<string, object>();
                                    paramCbmHeader.Add("category", EnumCategoryServiceSheet.NORMAL);
                                    paramCbmHeader.Add("rating", EnumRatingServiceSheet.IMAGE);

                                    var resultImage = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);
                                    var resImage = resultImage.content;
                                    resImage.key = Guid.NewGuid().ToString();
                                    resImage.SectionData = itemColumn.Section;

                                    foreach (var resImageItem in resImage.items)
                                    {
                                        resImageItem.key = Guid.NewGuid().ToString();
                                        if (resImageItem.value.ToString() == EnumCommonProperty.ImageData)
                                        {
                                            resImageItem.value = itemColumn.ImageData;
                                        }
                                    }

                                    if (!string.IsNullOrEmpty(itemColumn.Number250)) { psType250.Add(resImage); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number500)) { psType500.Add(resImage); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number1000)) { psType1000.Add(resImage); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number2000)) { psType2000.Add(resImage); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number4000)) { psType4000.Add(resImage); }
                                }

                                // Insert Guide
                                if (!string.IsNullOrEmpty(itemColumn.GuidTable))
                                {
                                    var paramCbmHeader = new Dictionary<string, object>();
                                    paramCbmHeader.Add("category", EnumCategoryServiceSheet.GUIDE);
                                    paramCbmHeader.Add("rating", itemColumn.GuidTable);

                                    var resultGuid = await _repositoryGenerateJson.GetDataByParam(paramCbmHeader);
                                    var resGuide = resultGuid.content;
                                    resGuide.key = Guid.NewGuid().ToString();
                                    resGuide.SectionData = itemColumn.Section;

                                    if (!string.IsNullOrEmpty(itemColumn.Number250)) { psType250.Add(resGuide); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number500)) { psType500.Add(resGuide); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number1000)) { psType1000.Add(resGuide); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number2000)) { psType2000.Add(resGuide); }
                                    if (!string.IsNullOrEmpty(itemColumn.Number4000)) { psType4000.Add(resGuide); }
                                }
                            }
                        }
                        #endregion

                        var dataCek = dataJsonResult.Where(x => x.Key == "2000").FirstOrDefault().Value;

                        dataJsonResult.Add("250", psType250);
                        dataJsonResult.Add("500", psType500);
                        dataJsonResult.Add("1000", psType1000);
                        dataJsonResult.Add("2000", psType2000);
                        dataJsonResult.Add("4000", psType4000);

                        List<string> psTypeData = new List<string>();
                        psTypeData.Add("250");
                        psTypeData.Add("500");
                        psTypeData.Add("1000");
                        psTypeData.Add("2000");
                        psTypeData.Add("4000");

                        foreach (var itemDataPsType in psTypeData)
                        {
                            var dataTaskGroup = dataJsonResult.Where(x => x.Key == itemDataPsType).FirstOrDefault().Value;

                            var resultObj = JsonConvert.DeserializeObject<List<dynamic>>(JsonConvert.SerializeObject(dataTaskGroup));

                            if (resultObj.Count == 0)
                            {
                                continue;
                            }

                            foreach (var itemGorupResult in resultObj)
                            {
                                if (itemGorupResult.description != EnumCommonProperty.Description)
                                {
                                    itemGorupResult.Remove("description250");
                                    itemGorupResult.Remove("description500");
                                    itemGorupResult.Remove("description1000");
                                    itemGorupResult.Remove("description2000");
                                    itemGorupResult.Remove("description4000");
                                    continue;
                                };

                                if (itemDataPsType == "250") { itemGorupResult.description = itemGorupResult.description250; }
                                else if (itemDataPsType == "500") { itemGorupResult.description = itemGorupResult.description500; }
                                else if (itemDataPsType == "1000") { itemGorupResult.description = itemGorupResult.description1000; }
                                else if (itemDataPsType == "2000") { itemGorupResult.description = itemGorupResult.description2000; }
                                else if (itemDataPsType == "4000") { itemGorupResult.description = itemGorupResult.description4000; }

                                foreach (var itemGroups in itemGorupResult.items)
                                {
                                    switch (itemGroups.value.ToString())
                                    {
                                        case "<<itemDetailNumber>>":
                                            if (itemDataPsType == "250") { itemGroups.value = itemGroups.value250; }
                                            else if (itemDataPsType == "500") { itemGroups.value = itemGroups.value500; }
                                            else if (itemDataPsType == "1000") { itemGroups.value = itemGroups.value1000; }
                                            else if (itemDataPsType == "2000") { itemGroups.value = itemGroups.value2000; }
                                            else if (itemDataPsType == "4000") { itemGroups.value = itemGroups.value4000; }

                                            break;
                                        case "<<itemNumber>>":
                                            if (itemDataPsType == "250") { itemGroups.value = itemGroups.value250; }
                                            else if (itemDataPsType == "500") { itemGroups.value = itemGroups.value500; }
                                            else if (itemDataPsType == "1000") { itemGroups.value = itemGroups.value1000; }
                                            else if (itemDataPsType == "2000") { itemGroups.value = itemGroups.value2000; }
                                            else if (itemDataPsType == "4000") { itemGroups.value = itemGroups.value4000; }

                                            break;
                                        case "<<itemCrackCode>>":
                                            if (itemDataPsType == "250") { itemGroups.value = itemGroups.Number250; }
                                            else if (itemDataPsType == "500") { itemGroups.value = itemGroups.Number500; }
                                            else if (itemDataPsType == "1000") { itemGroups.value = itemGroups.Number1000; }
                                            else if (itemDataPsType == "2000") { itemGroups.value = itemGroups.Number2000; }
                                            else if (itemDataPsType == "4000") { itemGroups.value = itemGroups.Number4000; }

                                            break;
                                    }

                                    if (itemGroups.disabledByItemKey != null)
                                    {
                                        switch (itemGroups.disabledByItemKey.ToString())
                                        {
                                            case "<<disabledByItemKey>>":
                                                if (itemDataPsType == "250") { itemGroups.disabledByItemKey = itemGorupResult.valueDisabled; }
                                                else if (itemDataPsType == "500") { itemGroups.disabledByItemKey = itemGorupResult.valueDisabled; }
                                                else if (itemDataPsType == "1000") { itemGroups.disabledByItemKey = itemGorupResult.valueDisabled; }
                                                else if (itemDataPsType == "2000") { itemGroups.disabledByItemKey = itemGorupResult.valueDisabled; }
                                                else if (itemDataPsType == "4000") { itemGroups.disabledByItemKey = itemGorupResult.valueDisabled; }

                                                break;
                                        }
                                    }

                                    itemGroups.Remove("value250");
                                    itemGroups.Remove("value500");
                                    itemGroups.Remove("value1000");
                                    itemGroups.Remove("value2000");
                                    itemGroups.Remove("value4000");
                                }

                                itemGorupResult.Remove("description250");
                                itemGorupResult.Remove("description500");
                                itemGorupResult.Remove("description1000");
                                itemGorupResult.Remove("description2000");
                                itemGorupResult.Remove("description4000");
                                itemGorupResult.Remove("valueDisabled");

                            }

                            #region Default Header
                            GenerateJsonModel jsonModel = new GenerateJsonModel();
                            jsonModel.subGroup = new List<SubGroup>();

                            jsonModel.modelId = modelList;
                            jsonModel.psTypeId = itemDataPsType;
                            jsonModel.workOrder = "";
                            jsonModel.groupName = groupName.Replace(" ", "_").ToUpper();
                            jsonModel.groupSeq = 2;
                            jsonModel.key = groupName.Replace(" ", "_").ToUpper();
                            jsonModel.version = "1.0.0";

                            var paramInfo = new Dictionary<string, object>();
                            paramInfo.Add("category", "NORMAL_INFO");
                            paramInfo.Add("rating", "NO");

                            var resultInfo = await _repositoryGenerateJson.GetDataByParam(paramInfo);

                            // Info Tab
                            var infoTab = listGenerateColumn.Where(x => x.Category == "InfoTab").FirstOrDefault();
                            if (infoTab != null)
                            {
                                var infoSplit = infoTab.Description.Split("|");
                                foreach (var itemInfo in resultInfo.content.items)
                                {
                                    switch (itemInfo.value.ToString())
                                    {
                                        case "<<warning>>":
                                            itemInfo.value = infoSplit[0];
                                            break;
                                        case "<<info>>":
                                            itemInfo.value = infoSplit[1];
                                            break;
                                    }
                                }
                            }

                            TaskGroup taskGroupDataInfo = new TaskGroup();
                            taskGroupDataInfo.task = new List<dynamic>();
                            taskGroupDataInfo.name = "Information"; // Section
                            taskGroupDataInfo.key = $"{groupName.Replace(" ", "_").ToUpper()}_INFORMATION"; // Section
                            taskGroupDataInfo.task.Add(resultInfo.content);
                            #endregion

                            #region Merge Data
                            List<TaskGroup> taskGroupDataList = new List<TaskGroup>();

                            var sectionGroup = listGenerateColumn.Select(x => x.Section).Where(x => x != "INFORMATION").ToList();
                            //var sectionGroup = listGenerateColumn.Where(x => x.Section != "INFORMATION").ToList();
                            foreach (var itemSectionData in sectionGroup)
                            {
                                if (itemSectionData == null)
                                {
                                    var dataSectionNull = listGenerateColumn.Where(x => x.Section == itemSectionData);
                                    ServiceResult resultErr = new ServiceResult();
                                    resultErr.IsError = true;
                                    resultErr.Message = $"Error Line Excel: {JsonConvert.SerializeObject(dataSectionNull.Select(x => x.Row).ToList())}";
                                    resultErr.Content = "[Section] Cannot be null or empty";

                                    return resultErr;
                                }

                                TaskGroup taskGroupData = new TaskGroup();
                                taskGroupData.name = itemSectionData; // Section
                                taskGroupData.key = $"{itemSectionData.Replace(" ", "_")}"; // Section
                                                                                            //taskGroupData.task = dataTaskGroup;
                                taskGroupData.task = resultObj.Where(x => x.SectionData == itemSectionData);

                                taskGroupDataList.Add(taskGroupData);
                            }

                            //taskGroupDataList.Add(taskGroupData);

                            //TaskGroup taskGroupData = new TaskGroup();
                            //taskGroupData.name = "Pre Service Check"; // Section
                            //taskGroupData.key = $"PRE_SERVICE_CHECK_DATA"; // Section
                            ////taskGroupData.task = dataTaskGroup;
                            //taskGroupData.task = resultObj;

                            SubGroup subGroupData = new SubGroup();
                            subGroupData.taskGroup = new List<TaskGroup>();
                            subGroupData.name = listGenerateColumn.FirstOrDefault() == null ? string.Empty : listGenerateColumn.FirstOrDefault().GroupName.ToUpper();
                            subGroupData.key = $"{groupName.Replace(" ", "_").ToUpper()}";
                            subGroupData.desc = "-";
                            subGroupData.taskGroup.Add(taskGroupDataInfo);
                            //subGroupData.taskGroup.Add(taskGroupData);
                            subGroupData.taskGroup.AddRange(taskGroupDataList);
                            #endregion

                            jsonModel.subGroup.Add(subGroupData);

                            switch (itemDataPsType)
                            {
                                case "4000":
                                    resultServiceSizeList.PsType4000.Add(jsonModel);
                                    break;
                                case "2000":
                                    resultServiceSizeList.PsType2000.Add(jsonModel);
                                    break;
                                case "1000":
                                    resultServiceSizeList.PsType1000.Add(jsonModel);
                                    break;
                                case "500":
                                    resultServiceSizeList.PsType500.Add(jsonModel);
                                    break;
                                case "250":
                                    resultServiceSizeList.PsType250.Add(jsonModel);
                                    break;
                            }

                            //resultList.Add(jsonModel);
                        }
                    }

                    //resultList = resultServiceSizeList;

                    result.IsError = false;
                    result.Message = "Success";
                    result.Content = resultServiceSizeList;
                }

                return new ServiceResult
                {
                    Message = "Data updated successfully",
                    IsError = false,
                    Content = result
                };
            }
            catch (Exception e)
            {

                ServiceResult resultErr = new ServiceResult();
                resultErr.IsError = true;
                resultErr.Message = $"Error Line Excel: {detailAddressRow}";
                resultErr.Content = e.ToString();

                return resultErr;
            }
        }

        public async Task<byte[]> GenerateTaskCrackMapping(string modelId, string psTypeId)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var ep = new ExcelPackage())
            {
                #region Header
                ExcelWorksheet Sheet = ep.Workbook.Worksheets.Add("TaskCrack");

                Sheet.Cells["A1"].Value = "Model";
                Sheet.Cells["B1"].Value = "PsType";
                Sheet.Cells["C1"].Value = "TaskKey";
                Sheet.Cells["D1"].Value = "CrackCode";
                Sheet.Cells["E1"].Value = "LocationDesc";
                #endregion

                #region Content
                var _repoServiceSheet = new MasterServiceSheetRepository(_connectionFactory, EnumContainer.MasterServiceSheet);
                var result = await _repoServiceSheet.GetTaskCrackMapping(modelId, psTypeId);

                int row = 2;

                foreach (var data in result)
                {
                    MasterServiceSheetTaskCrackResponse item = JsonConvert.DeserializeObject<MasterServiceSheetTaskCrackResponse>(JsonConvert.SerializeObject(data));
                    Sheet.Cells[string.Format("A{0}", row)].Value = item.modelId;
                    Sheet.Cells[string.Format("B{0}", row)].Value = item.psTypeId;
                    Sheet.Cells[string.Format("C{0}", row)].Value = item.taskKey;
                    Sheet.Cells[string.Format("D{0}", row)].Value = item.crackCode;
                    Sheet.Cells[string.Format("E{0}", row)].Value = item.locationDesc;

                    row++;
                }
                #endregion

                var stream = new MemoryStream(ep.GetAsByteArray());
                return stream.ToArray();
            }
        }

        public async Task<byte[]> GenerateFileMasterTemplate(string modelId, string psTypeId)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var ep = new ExcelPackage())
            {
                #region Header
                ExcelWorksheet Sheet = ep.Workbook.Worksheets.Add("Mapping Service");

                Sheet.Cells["A1"].Value = "ModelId";
                Sheet.Cells["B1"].Value = "Tab";
                Sheet.Cells["C1"].Value = "Section";
                Sheet.Cells["D1"].Value = "MyLable";
                Sheet.Cells["E1"].Value = "Type";
                Sheet.Cells["F1"].Value = "250";
                Sheet.Cells["G1"].Value = "500";
                Sheet.Cells["H1"].Value = "1000";
                Sheet.Cells["I1"].Value = "2000";
                Sheet.Cells["J1"].Value = "4000";
                Sheet.Cells["K1"].Value = "Guide Table";
                Sheet.Cells["L1"].Value = "Image";
                Sheet.Cells["M1"].Value = "Table";
                Sheet.Cells["N1"].Value = "Service Mapping";
                Sheet.Cells["O1"].Value = "SOS";
                Sheet.Cells["P1"].Value = "Section Column";
                Sheet.Cells["Q1"].Value = "Task Key";
                Sheet.Cells["R1"].Value = "Group Task Id";

                System.Drawing.Color colFromHex = System.Drawing.ColorTranslator.FromHtml("#8EA9DB");
                Sheet.Cells["A1:R1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                Sheet.Cells["A1:R1"].Style.Fill.BackgroundColor.SetColor(colFromHex);
                Sheet.Cells["A1:R1"].Style.Font.Bold = true;
                Sheet.Row(1).Height = 35;
                #endregion

                #region Content
                var _repoServiceSheet = new MasterServiceSheetRepository(_connectionFactory, EnumContainer.MasterServiceSheet);
                var result = await _repoServiceSheet.GetMasterServiceSheetTemplate(modelId, psTypeId);

                List<MasterServiceSheetGenerateTaskResponse> dataResult = JsonConvert.DeserializeObject<List<MasterServiceSheetGenerateTaskResponse>>(JsonConvert.SerializeObject(result));

                var dataHeader = dataResult.Where(x => x.psTypeId == "2000").ToList();

                int row = 2;

                foreach (var item in dataHeader)
                {
                    var dataNumber250 = dataResult.Where(x => x.TaskKey == item.TaskKey && x.psTypeId == "250").FirstOrDefault();
                    item.Number250 = dataNumber250 != null ? dataNumber250.taskNo : string.Empty;

                    var dataNumber500 = dataResult.Where(x => x.TaskKey == item.TaskKey && x.psTypeId == "500").FirstOrDefault();
                    item.Number500 = dataNumber500 != null ? dataNumber500.taskNo : string.Empty;

                    var dataNumber1000 = dataResult.Where(x => x.TaskKey == item.TaskKey && x.psTypeId == "1000").FirstOrDefault();
                    item.Number1000 = dataNumber1000 != null ? dataNumber1000.taskNo : string.Empty;

                    //var dataNumber2000 = dataResult.Where(x => x.TaskKey == item.TaskKey && x.psTypeId == "2000").FirstOrDefault();
                    //item.Number2000 = dataNumber2000 != null ? dataNumber2000.taskNo : string.Empty;

                    //MasterServiceSheetGenerateTaskResponse item = JsonConvert.DeserializeObject<MasterServiceSheetGenerateTaskResponse>(JsonConvert.SerializeObject(data));
                    Sheet.Cells[string.Format("A{0}", row)].Value = item.modelId;
                    Sheet.Cells[string.Format("B{0}", row)].Value = item.tab;
                    Sheet.Cells[string.Format("C{0}", row)].Value = item.section;
                    Sheet.Cells[string.Format("D{0}", row)].Value = item.description;
                    Sheet.Cells[string.Format("E{0}", row)].Value = item.type;
                    Sheet.Cells[string.Format("F{0}", row)].Value = item.Number250;
                    Sheet.Cells[string.Format("G{0}", row)].Value = item.Number500;
                    Sheet.Cells[string.Format("H{0}", row)].Value = item.Number1000;
                    Sheet.Cells[string.Format("I{0}", row)].Value = item.Number2000;
                    Sheet.Cells[string.Format("J{0}", row)].Value = item.Number4000;
                    Sheet.Cells[string.Format("K{0}", row)].Value = string.Empty; // Guide Table
                    Sheet.Cells[string.Format("L{0}", row)].Value = string.Empty; // Image
                    Sheet.Cells[string.Format("M{0}", row)].Value = string.Empty; // Table
                    Sheet.Cells[string.Format("N{0}", row)].Value = item.ServiceMapping; // ServiceMapping
                    Sheet.Cells[string.Format("O{0}", row)].Value = string.Empty; // SOS
                    Sheet.Cells[string.Format("P{0}", row)].Value = string.Empty; // SectionColumn
                    Sheet.Cells[string.Format("Q{0}", row)].Value = item.TaskKey; // TaskKey
                    Sheet.Cells[string.Format("R{0}", row)].Value = item.GroupTaskId; // GroupTaskId

                    row++;
                }
                #endregion

                var stream = new MemoryStream(ep.GetAsByteArray());
                return stream.ToArray();
            }
        }

        public async Task<ServiceResult> ValidateMasterDoc(GenerateModelJson model)
        {
            List<ValidateMasterDoc> resultValidate = new List<ValidateMasterDoc>();

            MappingRecommendedLubricant dataMapping = new MappingRecommendedLubricant();
            dataMapping.detail = new List<DetailMappingLubricant>();

            using (var excel = model.file.OpenReadStream())
            {
                using var workBook = new XLWorkbook(excel);
                IXLWorksheet workSheet = workBook.Worksheets.Where(x => x.Name == model.workSheet).FirstOrDefault();

                var _repositoryGenerateJsonType = new GenerateJsonTypeRepository(_connectionFactory, EnumContainer.GenerateJsonType);

                #region Data Guide Table
                Dictionary<string, object> paramGuideTable = new Dictionary<string, object>();
                paramGuideTable.Add(EnumQuery.Category, EnumQuery.Guide);

                var resultGuideTable = await _repositoryGenerateJson.GetDataListByParam(paramGuideTable);
                List<GenerateJson> objMasterJson = JsonConvert.DeserializeObject<List<GenerateJson>>(JsonConvert.SerializeObject(resultGuideTable));
                #endregion

                #region Data Table
                Dictionary<string, object> paramTable = new Dictionary<string, object>();
                paramTable.Add(EnumQuery.Category, EnumQuery.Table);

                var resultTable = await _repositoryGenerateJson.GetDataListByParam(paramTable);
                List<GenerateJson> objMasterTableJson = JsonConvert.DeserializeObject<List<GenerateJson>>(JsonConvert.SerializeObject(resultTable));
                #endregion

                #region Data Master Type
                Dictionary<string, object> paramType = new Dictionary<string, object>();

                var resultType = await _repositoryGenerateJsonType.GetDataListByParam(paramType);
                List<GenerateJsonType> objMasterType = JsonConvert.DeserializeObject<List<GenerateJsonType>>(JsonConvert.SerializeObject(resultType));
                #endregion

                string currentRow = "2";
                string endRow = workSheet.RowsUsed().Count().ToString();

                Regex digitsOnly = new Regex(@"[^\d]");
                Regex charOnly = new Regex(@"[a-zA-Z ]");

                // Preparation
                var dataWeworkSheetCount = workSheet.Range($"A{currentRow}:R{endRow}").RowCount();
                var dataWeworkSheet = workSheet.Range($"A{currentRow}:R{endRow}")
                    .CellsUsed()
                    .Select(c => new AddressMappingData
                    {
                        Row = Convert.ToInt32(digitsOnly.Replace(c.Address.ToString(), "")),
                        Value = c.Value.ToString(),
                        Address = c.Address.ToString()
                    }).ToList();

                var dataGroupCount = workSheet.Range($"A{currentRow}:R{endRow}").RowCount();
                var dataWeworkSheetGroup = workSheet.Range($"A{currentRow}:R{endRow}")
                .CellsUsed()
                .Select(c => new AddressMappingData
                {
                    Row = Convert.ToInt32(digitsOnly.Replace(c.Address.ToString(), "")),
                    Value = c.Value.ToString(),
                    Address = c.Address.ToString()
                }).ToList();

                Dictionary<string, object> dataJsonResult = new Dictionary<string, object>();

                var listGenerateColumn = new List<GenerateColumnJson>();

                for (int i = 0; i < dataGroupCount; i++)
                {
                    var generateColumn = new GenerateColumnJson();
                    foreach (var row in dataWeworkSheetGroup.Where(x => x.Row == i + Convert.ToInt32(currentRow)))
                    {
                        generateColumn.Row = row.Row;
                        generateColumn.Model = generateColumn.Model != null ? generateColumn.Model : row.Address.Contains("A") ? row.Value.Trim() : null;
                        generateColumn.GroupName = generateColumn.GroupName != null ? generateColumn.GroupName : row.Address.Contains("B") ? row.Value.Trim() : null;
                        generateColumn.Section = generateColumn.Section != null ? generateColumn.Section : row.Address.Contains("C") ? row.Value.Trim() : null;
                        generateColumn.Description = generateColumn.Description != null ? generateColumn.Description : row.Address.Contains("D") ? row.Value.Trim() : null;
                        generateColumn.Category = generateColumn.Category != null ? generateColumn.Category : row.Address.Contains("E") ? row.Value.Trim() : null;
                        generateColumn.Number250 = generateColumn.Number250 != null ? generateColumn.Number250 : row.Address.Contains("F") ? row.Value.Trim() : null;
                        generateColumn.Number500 = generateColumn.Number500 != null ? generateColumn.Number500 : row.Address.Contains("G") ? row.Value.Trim() : null;
                        generateColumn.Number1000 = generateColumn.Number1000 != null ? generateColumn.Number1000 : row.Address.Contains("H") ? row.Value.Trim() : null;
                        generateColumn.Number2000 = generateColumn.Number2000 != null ? generateColumn.Number2000 : row.Address.Contains("I") ? row.Value.Trim() : null;

                        if (generateColumn.Number4000 != null)
                        {
                            generateColumn.Number4000 = generateColumn.Number4000 != null ? generateColumn.Number4000 : row.Address.Contains("J") ? row.Value.Trim() : null;
                        }
                        else
                        {
                            generateColumn.Number4000 = generateColumn.Number2000 != null ? generateColumn.Number2000 : row.Address.Contains("J") ? row.Value.Trim() : null;
                        }

                        generateColumn.GuidTable = generateColumn.GuidTable != null ? generateColumn.GuidTable : row.Address.Contains("K") ? row.Value.Trim() : null;
                        generateColumn.ImageData = generateColumn.ImageData != null ? generateColumn.ImageData : row.Address.Contains("L") ? row.Value.Trim() : null;
                        generateColumn.Table = generateColumn.Table != null ? generateColumn.Table : row.Address.Contains("M") ? row.Value.Trim() : null;
                        generateColumn.ServiceMappingValue = generateColumn.ServiceMappingValue != null ? generateColumn.ServiceMappingValue : row.Address.Contains("N") ? row.Value.Trim() : null;
                        generateColumn.SOS = generateColumn.SOS != null ? generateColumn.SOS : row.Address.Contains("O") ? row.Value.Trim() : null;
                        generateColumn.SectionColumn = generateColumn.SectionColumn != null ? generateColumn.SectionColumn : row.Address.Contains("P") ? row.Value.Trim() : null;
                        generateColumn.TaskKey = generateColumn.TaskKey != null ? generateColumn.TaskKey : row.Address.Contains("Q") ? row.Value.Trim() : null;
                        generateColumn.GroupTaskId = generateColumn.GroupTaskId != null ? generateColumn.GroupTaskId : row.Address.Contains("R") ? row.Value.Trim() : null;
                    }

                    listGenerateColumn.Add(generateColumn);
                }

                #region ModelId
                var resultModelId = listGenerateColumn.Where(x => x.Model == null)
                                                      .Select(o => new ValidateMasterDoc
                                                      {
                                                          LineError = $"A{o.Row.ToString()}",
                                                          Message = "[MODEL ID] Cannot be null"
                                                      }).ToList();
                if (resultModelId.Any())
                    resultValidate.AddRange(resultModelId);
                #endregion

                #region Tab

                List<string> enumGroupList = new List<string>();
                List<string> quotedEnumGroupList = new List<string>();

                PropertyInfo[] properties = typeof(EnumGroup).GetProperties(BindingFlags.Public | BindingFlags.Static);

                foreach (var property in properties)
                {
                    string value = property.GetValue(null) as string;

                    if (value != EnumGroup.General.ToString() && value != EnumGroup.SuckAndBlow.ToString() && value != EnumGroup.DefectIdentifiedService.ToString())
                    {
                        string formattedValue = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value.Replace("_", " ").ToLower());
                        var quottedValue = $"'{formattedValue}'";

                        enumGroupList.Add(formattedValue);
                        quotedEnumGroupList.Add(quottedValue);
                    }
                }

                var resultTab = listGenerateColumn.Where(x => string.IsNullOrEmpty(x.GroupName) || !enumGroupList.Any(y => y.Contains(x.GroupName)))
                                                  .Select(o => new ValidateMasterDoc
                                                  {
                                                      LineError = $"B{o.Row.ToString()}",
                                                      Message = "[TAB] must not be empty and should match one of the following values: " + string.Join(", ", quotedEnumGroupList) + "."
                                                  }).ToList();
                if (resultTab.Any())
                    resultValidate.AddRange(resultTab);
                #endregion

                #region Description

                var resultDesc = listGenerateColumn.Where(x => x.Description != null && x.Description.Contains(EnumUrl.appSetting.BlobUrl))
                                                  .Select(o => new ValidateMasterDoc
                                                  {
                                                      LineError = $"D{o.Row.ToString()}",
                                                      Message = $"[DESCRIPTION] must not include the substring '{EnumUrl.appSetting.BlobUrl}'. Replace any occurrences with '{{BLOB_URL}}'. For instance, if the description is currently '{EnumUrl.appSetting.BlobUrl}/utility/DMA/BA-SE-P38', it should be modified to '{{BLOB_URL}}/utility/DMA/BA-SE-P38'."
                                                  }).ToList();
                if (resultDesc.Any())
                    resultValidate.AddRange(resultDesc);

                #endregion

                #region Type
                var resultMasterType = (from masterDoc in listGenerateColumn
                                        join masterType in objMasterType on masterDoc.Category equals masterType.type into gj
                                        from subpet in gj.DefaultIfEmpty()
                                        select new ValidateMasterDoc
                                        {
                                            LineError = $"E{masterDoc.Row.ToString()}",
                                            Message = subpet?.type == null ? $"[MASTER TYPE] '{masterDoc.Category}' Not Found" : string.Empty
                                        })
                                        .Where(p => !string.IsNullOrEmpty(p.Message)).ToList();

                if (resultMasterType.Any())
                    resultValidate.AddRange(resultMasterType);
                #endregion

                #region Guide Table
                var resultMasterGuideTable = (from masterDoc in listGenerateColumn
                                              join masterJson in objMasterJson on masterDoc.GuidTable equals masterJson.rating into gj
                                              from subpet in gj.DefaultIfEmpty()
                                              select new ValidateMasterDoc
                                              {
                                                  LineError = $"K{masterDoc.Row.ToString()}",
                                                  Message = subpet?.rating == null && !string.IsNullOrEmpty(masterDoc.GuidTable) ? $"[GUIDE TABLE] '{masterDoc.GuidTable}' Not Found" : string.Empty
                                              })
                                              .Where(p => !string.IsNullOrEmpty(p.Message)).ToList();

                if (resultMasterGuideTable.Any())
                    resultValidate.AddRange(resultMasterGuideTable);
                #endregion

                #region Image
                var resultImage = listGenerateColumn.Where(x => !string.IsNullOrEmpty(x.ImageData) && !x.ImageData.Contains("YES") && !int.TryParse(x.ImageData, out int n))
                                                    .Select(o => new ValidateMasterDoc
                                                    {
                                                        LineError = $"L{o.Row.ToString()}",
                                                        Message = $"[IMAGE] ID '{o.ImageData}' Not Found, Default Value is 135 or YES"
                                                    }).ToList();
                if (resultImage.Any())
                    resultValidate.AddRange(resultImage);
                #endregion

                #region Table
                var resultMasterTable = (from masterDoc in listGenerateColumn
                                         join masterJson in objMasterTableJson on masterDoc.Table equals masterJson.rating into gj
                                         from subpet in gj.DefaultIfEmpty()
                                         select new ValidateMasterDoc
                                         {
                                             LineError = $"M{masterDoc.Row.ToString()}",
                                             Message = subpet?.rating == null && !string.IsNullOrEmpty(masterDoc.Table) ? $"[TABLE] '{masterDoc.Table}' Not Found" : string.Empty
                                         })
                                        .Where(p => !string.IsNullOrEmpty(p.Message)).ToList();

                if (resultMasterTable.Any())
                    resultValidate.AddRange(resultMasterTable);
                #endregion

                #region Task Key
                var resultTaskKey = listGenerateColumn.Where(x => x.TaskKey == null)
                                                      .Select(o => new ValidateMasterDoc
                                                      {
                                                          LineError = $"Q{o.Row.ToString()}",
                                                          Message = "[TASK KEY] Cannot be null"
                                                      }).ToList();
                if (resultTaskKey.Any())
                    resultValidate.AddRange(resultTaskKey);

                var resultTaskKeyDuplicate = listGenerateColumn.GroupBy(x => x.TaskKey)
                                                .Where(g => g.Count() > 1 && !string.IsNullOrEmpty(g.Key))
                                                .Select(y => new ValidateMasterDoc
                                                {
                                                    LineError = $"Q{JsonConvert.SerializeObject(y.Select(p => p.Row).ToArray())}",
                                                    Message = $"[TASK KEY] Duplicate Key '{y.Key}', Total Duplicate: {y.Count()}"
                                                }).ToList();

                if (resultTaskKeyDuplicate.Any())
                    resultValidate.AddRange(resultTaskKeyDuplicate);
                #endregion

                #region Description InfoTab
                var resultDescInfoTab = listGenerateColumn.Where(x => x.Category == "InfoTab" && !x.Description.Contains("|"))
                                                      .Select(o => new ValidateMasterDoc
                                                      {
                                                          LineError = $"D{o.Row.ToString()}",
                                                          Message = "[MY LABLE] You have not used a separator '|' in the 'InfoTab' type."
                                                      }).ToList();
                if (resultDescInfoTab.Any())
                    resultValidate.AddRange(resultDescInfoTab);
                #endregion

            }

            if (resultValidate.Any())
            {
                string messageError = string.Empty;
                if (resultValidate.Where(x => x.Message.Contains("[TASK KEY]")).Any())
                {
                    messageError = $"Data Invalid Please Check Documentation: {EnumCaption.LinkDocumentation}, and please use the following formula excel for generate GUID: {EnumCaption.FormulaGuid}";
                }
                else
                {
                    messageError = $"Data Invalid Please Check Documentation: {EnumCaption.LinkDocumentation}";
                }

                return new ServiceResult
                {
                    Message = messageError,
                    IsError = true,
                    Content = resultValidate
                };
            }

            return new ServiceResult
            {
                Message = "Data Valid",
                IsError = false,
                Content = null
            };
        }

        public async Task<ServiceResult> SaveTemplate(List<InsertTemplate> model)
        {
            #region Validate
            JArray dataModel = JArray.FromObject(model.Select(o => o.Details.Select(x => x.modelId.ToString()).Distinct()).FirstOrDefault());

            //List<string> fieldsParam = new List<string>() { EnumQuery.ModelId, EnumQuery.PsTypeId };

            //Dictionary<string, object> param = new Dictionary<string, object>();
            //param.Add(EnumQuery.ModelId, dataModel);
            //param.Add(EnumQuery.Fields, fieldsParam);

            //var validateModel = await _repository.GetDataListByParamJArray(param);
            //if (validateModel.Any())
            //{
            //    return new ServiceResult
            //    {
            //        Message = $"Data ModelId {dataModel.ToString().Replace("\r\n", "").Replace("\"", "")} already exists",
            //        IsError = true,
            //        Content = null
            //    };
            //}

            List<string> fieldsParamAllowed = new List<string>() { EnumQuery.ModelId };

            Dictionary<string, object> paramAllowed = new Dictionary<string, object>();
            paramAllowed.Add(EnumQuery.ModelId, dataModel);
            paramAllowed.Add(EnumQuery.IsAllowed, "true");
            paramAllowed.Add(EnumQuery.Fields, fieldsParamAllowed);

            var validateAllowedCreateData = await _repositoryGenerateConfigModel.GetDataListByParamJArray(paramAllowed);
            if (!validateAllowedCreateData.Any())
            {
                return new ServiceResult
                {
                    Message = $"Model {dataModel.ToString().Replace("\r\n", "").Replace("\"", "")} do not have permission to change.",
                    IsError = true,
                    Content = null
                };
            }
            #endregion

            #region Delete Current Data
            List<string> fieldsParamDataCurrent = new List<string>() { EnumQuery.ModelId, EnumQuery.PsTypeId, EnumQuery.ID };

            Dictionary<string, object> paramDataCurrent = new Dictionary<string, object>();
            paramDataCurrent.Add(EnumQuery.ModelId, dataModel);
            paramDataCurrent.Add(EnumQuery.Fields, fieldsParamDataCurrent);

            var dataCurrent = await _repositoryTemp.GetDataListByParamJArray(paramDataCurrent);
            if (dataCurrent.Any())
            {
                foreach (var itemDelete in dataCurrent)
                {
                    DeleteByParamRequest workOrderParam = new DeleteByParamRequest()
                    {
                        deleteParams = new Dictionary<string, object>()
                        {
                            { EnumQuery.ID, StaticHelper.GetPropValue(itemDelete, EnumQuery.ID)?.ToString() }
                        },
                        employee = new EmployeeModel() { id = "SYSTEM", name = "SYSTEM" }
                    };

                    var deleteCurrent = await _repositoryTemp.DeleteByParam(workOrderParam);
                }
            }
            #endregion

            foreach (var item in model)
            {
                foreach (var itemDetail in item.Details)
                {
                    var modelDetail = new CreateRequest();
                    modelDetail.employee = new EmployeeModel();

                    modelDetail.employee.id = "SYSTEM";
                    modelDetail.employee.name = "SYSTEM";
                    modelDetail.entity = itemDetail;

                    var resultAddDetail = await _repositoryTemp.Create(modelDetail);

                    if (resultAddDetail == null)
                    {
                        return new ServiceResult()
                        {
                            IsError = true,
                            Message = "Insert Data Failed"
                        };
                    }
                }
            }

            return new ServiceResult
            {
                Message = "Data insert successfully",
                IsError = false,
                Content = null
            };
        }

        public async Task<ServiceResult> GetServicesheet(ServicesheetRequest servicesheetRequest)
        {
            var repo = new MasterServiceSheetRepository(_connectionFactory, _container);
            dynamic result = await repo.GetServicesheet(servicesheetRequest);

            return new ServiceResult
            {
                IsError = false,
                Message = "Get Service Sheet successfully",
                Content = result
            };
        }

        public async Task<byte[]> DownloadPreviousTandem(string modelId, string psTypeId, string previousTaskType)
        {
            #region Master Servicesheet

            ServiceSheetDetailService serviceSheetDetailService = new ServiceSheetDetailService(_appSetting, _connectionFactory, EnumContainer.ServiceSheetDetail, _accessToken, null);

            ServicesheetRequest servicesheetRequest = new ServicesheetRequest();

            servicesheetRequest.selectedFields = new List<SelectRequest>() {
                new SelectRequest(){ level = EnumLevel.Tab, fieldName = EnumQuery.ModelId },
                new SelectRequest(){ level = EnumLevel.Tab, fieldName = EnumQuery.PsTypeId },
                new SelectRequest(){ level = EnumLevel.Task, fieldName = EnumQuery.Key },
                new SelectRequest(){ level = EnumLevel.Task, fieldName = EnumQuery.Description},
                new SelectRequest(){ level = EnumLevel.Task, fieldName = EnumQuery.Items }
            };

            servicesheetRequest.parameters = new List<ParameterRequest>() {
                new ParameterRequest() { level = EnumLevel.Tab, fieldName = EnumQuery.ModelId, fieldValue = modelId },
                new ParameterRequest() { level = EnumLevel.Tab, fieldName = EnumQuery.PsTypeId, fieldValue = psTypeId }
            };

            if (previousTaskType.ToLower() == EnumPreviousTaskType.Tandem.ToLower())
            {
                servicesheetRequest.parameters.Add(new ParameterRequest() { level = EnumLevel.Task, fieldName = EnumQuery.Rating, fieldValue = EnumPreviousTaskType.TandemRating });
            }
            else if (previousTaskType.ToLower() == EnumPreviousTaskType.Replacement.ToLower())
            {
                servicesheetRequest.parameters.Add(new ParameterRequest() { level = EnumLevel.Task, fieldName = EnumQuery.Rating, fieldValue = EnumPreviousTaskType.ReplacementRating });
            }
            else if (previousTaskType.ToLower() == EnumPreviousTaskType.ReplacementGap.ToLower())
            {
                servicesheetRequest.parameters.Add(new ParameterRequest() { level = EnumLevel.Task, fieldName = EnumQuery.Rating, fieldValue = EnumPreviousTaskType.ReplacementGapRating });
            }

            var serviceSheetTandemResult = await GetServicesheet(servicesheetRequest);
            List<TaskTandemHelperModel> serviceSheetTandems = JsonConvert.DeserializeObject<List<TaskTandemHelperModel>>(JsonConvert.SerializeObject(serviceSheetTandemResult.Content));

            #endregion

            #region Master Task Tandem

            Dictionary<string, object> taskTandemParams = new Dictionary<string, object>() {
                { EnumQuery.ModelId, modelId },
                { EnumQuery.PsTypeId, psTypeId }
            };

            var taskTandemRepo = new TaskTandemRepository(_connectionFactory, EnumContainer.TaskTandem);
            var taskTandemResult = await taskTandemRepo.GetDataListByParam(taskTandemParams);
            List<TaskTandemResponse> taskTandems = JsonConvert.DeserializeObject<List<TaskTandemResponse>>(JsonConvert.SerializeObject(taskTandemResult));

            #endregion

            #region Generate Excel

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var ep = new ExcelPackage())
            {
                ExcelWorksheet Sheet = ep.Workbook.Worksheets.Add("Previous Value");

                Sheet.Cells["A1"].Value = "ID";
                Sheet.Cells["B1"].Value = "Model ID";
                Sheet.Cells["C1"].Value = "PS Type";
                Sheet.Cells["D1"].Value = "Task ID";
                Sheet.Cells["E1"].Value = "Task No";
                Sheet.Cells["F1"].Value = "Sub Task";
                Sheet.Cells["G1"].Value = "Task Desc";
                Sheet.Cells["H1"].Value = "UoM";

                int row = 2;

                try
                {
                    foreach (var serviceSheetTandem in serviceSheetTandems)
                    {
                        Sheet.Cells[$"A{row}"].Value = taskTandems.Where(x => x.taskId == serviceSheetTandem.key).FirstOrDefault()?.id;
                        Sheet.Cells[$"B{row}"].Value = serviceSheetTandem.modelId;
                        Sheet.Cells[$"C{row}"].Value = serviceSheetTandem.psTypeId;
                        Sheet.Cells[$"D{row}"].Value = serviceSheetTandem.key;
                        Sheet.Cells[$"E{row}"].Value = string.IsNullOrEmpty(serviceSheetTandem.description) ? string.Empty : serviceSheetTandem.description.Split(";")?[0]?.ToString();
                        Sheet.Cells[$"F{row}"].Value = string.IsNullOrEmpty(serviceSheetTandem.description) ? string.Empty : serviceSheetTandem.description.Split(";")?[1]?.ToString();
                        Sheet.Cells[$"G{row}"].Value = string.IsNullOrEmpty(serviceSheetTandem.description) ? string.Empty : serviceSheetTandem.description.Split(";")?[2]?.ToString();
                        Sheet.Cells[$"H{row}"].Value = ((JObject)serviceSheetTandem.items[5])[EnumQuery.Value]?.Value<string>();

                        row++;
                    }
                }
                catch (Exception ex)
                {

                    throw;
                }

                var stream = new MemoryStream(ep.GetAsByteArray());
                return stream.ToArray();
            }

            #endregion
        }

        public async Task<ServiceResult> UploadPreviousTandem(IFormFile files)
        {
            var previousTandemRepo = new TaskTandemRepository(_connectionFactory, EnumContainer.TaskTandem);

            using (var excel = files.OpenReadStream())
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage(excel);

                ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                var rowCount = worksheet.Dimension.Rows;

                for (int row = 2; row <= rowCount; row++)
                {
                    TaskTandemRequest data = new TaskTandemRequest()
                    {
                        id = worksheet.Cells[row, 1].Value == null ? Guid.NewGuid().ToString() : worksheet.Cells[row, 1].Value.ToString(),
                        modelId = worksheet.Cells[row, 2].Value == null ? string.Empty : worksheet.Cells[row, 2].Value.ToString(),
                        psTypeId = worksheet.Cells[row, 3].Value == null ? string.Empty : worksheet.Cells[row, 3].Value.ToString(),
                        taskId = worksheet.Cells[row, 4].Value == null ? string.Empty : worksheet.Cells[row, 4].Value.ToString(),
                        taskCrackCode = worksheet.Cells[row, 6].Value == null ? string.Empty : worksheet.Cells[row, 6].Value.ToString(),
                        locationDesc = worksheet.Cells[row, 7].Value == null ? string.Empty : worksheet.Cells[row, 7].Value.ToString(),
                        uom = worksheet.Cells[row, 8].Value == null ? string.Empty : worksheet.Cells[row, 8].Value.ToString()
                    };

                    CreateRequest createRequest = new CreateRequest()
                    {
                        entity = data,
                        employee = new EmployeeModel() { id = "SYSTEM", name = "SYSTEM" }
                    };

                    await previousTandemRepo.Upsert(createRequest);
                }
            }

            return new ServiceResult
            {
                Message = "Data insert successfully",
                IsError = false,
                Content = null
            };
        }

        public async Task<ServiceResult> CreateTaskCalibration(string modelId, string psTypeId)
        {
            var taskCalibrationRepo = new TaskCalibrationRepository(_connectionFactory, EnumContainer.TaskCalibration);

            Dictionary<string, object> taskCalibrationParams = new Dictionary<string, object>() {
                { EnumQuery.ModelId, modelId },
                { EnumQuery.PsTypeId, psTypeId }
            };

            var taskCalibrationResult = await taskCalibrationRepo.GetDataByParam(taskCalibrationParams);

            if (taskCalibrationResult == null)
            {
                taskCalibrationResult = await taskCalibrationRepo.GetDataByParam(new Dictionary<string, object>());

                List<string> deletedProperties = new List<string>() {
                    EnumQuery.ID,
                    EnumQuery.IsActive,
                    EnumQuery.IsDeleted,
                    EnumQuery.CreatedBy,
                    EnumQuery.CreatedDate,
                    EnumQuery.UpdatedBy,
                    EnumQuery.UpdatedDate
                };

                ((JObject)taskCalibrationResult).Properties()
                    .Where(attr => deletedProperties.Contains(attr.Name))
                    .ToList()
                    .ForEach(attr => attr.Remove());

                taskCalibrationResult[EnumQuery.ModelId] = modelId;
                taskCalibrationResult[EnumQuery.PsTypeId] = psTypeId;

                CreateRequest createRequest = new CreateRequest()
                {
                    entity = taskCalibrationResult,
                    employee = new EmployeeModel()
                    {
                        id = "SYSTEM",
                        name = "SYSTEM"
                    }
                };

                var result = await taskCalibrationRepo.Create(createRequest);

                return new ServiceResult
                {
                    Message = "Create task calibration successfully",
                    IsError = false,
                    Content = null
                };
            }
            else
            {
                return new ServiceResult
                {
                    Message = $"Task calibration {modelId} {psTypeId} hours already exist.",
                    IsError = true,
                    Content = null
                };
            }
        }

        public async Task<byte[]> DownloadCBMDefaultValue(string modelId, string psTypeId)
        {
            #region Master Servicesheet

            ServiceSheetDetailService serviceSheetDetailService = new ServiceSheetDetailService(_appSetting, _connectionFactory, EnumContainer.ServiceSheetDetail, _accessToken, null);

            ServicesheetRequest servicesheetRequest = new ServicesheetRequest();

            servicesheetRequest.selectedFields = new List<SelectRequest>() {
                new SelectRequest(){ level = EnumLevel.Tab, fieldName = EnumQuery.ModelId },
                new SelectRequest(){ level = EnumLevel.Tab, fieldName = EnumQuery.PsTypeId },
                new SelectRequest(){ level = EnumLevel.Task, fieldName = EnumQuery.Key },
                new SelectRequest(){ level = EnumLevel.Task, fieldName = EnumQuery.Description},
                new SelectRequest(){ level = EnumLevel.Task, fieldName = EnumQuery.Items }
            };

            JArray ratings = new JArray() {
                EnumPreviousTaskType.ReplacementRating,
                EnumPreviousTaskType.ReplacementGapRating
            };

            servicesheetRequest.parameters = new List<ParameterRequest>() {
                new ParameterRequest() { level = EnumLevel.Tab, fieldName = EnumQuery.ModelId, fieldValue = modelId },
                new ParameterRequest() { level = EnumLevel.Tab, fieldName = EnumQuery.PsTypeId, fieldValue = psTypeId },
                new ParameterRequest() { level = EnumLevel.Task, fieldName = EnumQuery.Rating, fieldValue = ratings }
            };

            var serviceSheetTandemResult = await GetServicesheet(servicesheetRequest);
            List<TaskTandemHelperModel> serviceSheetReplacementGaps = JsonConvert.DeserializeObject<List<TaskTandemHelperModel>>(JsonConvert.SerializeObject(serviceSheetTandemResult.Content));

            #endregion

            #region Task CBM Default Value

            Dictionary<string, object> taskTandemParams = new Dictionary<string, object>() {
                { EnumQuery.ModelId, modelId },
                { EnumQuery.PsTypeId, psTypeId }
            };

            var taskDefaultValueRepo = new TaskTandemRepository(_connectionFactory, EnumContainer.TaskCbmDefaultValue);
            var taskDefaultValueResult = await taskDefaultValueRepo.GetDataListByParam(taskTandemParams);
            List<TaskCBMDefaultValueResponse> taskDefaultValues = JsonConvert.DeserializeObject<List<TaskCBMDefaultValueResponse>>(JsonConvert.SerializeObject(taskDefaultValueResult));

            #endregion

            #region Generate Excel

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var ep = new ExcelPackage())
            {
                ExcelWorksheet Sheet = ep.Workbook.Worksheets.Add("Previous Value");

                Sheet.Cells["A1"].Value = "ID";
                Sheet.Cells["B1"].Value = "Model ID";
                Sheet.Cells["C1"].Value = "PS Type";
                Sheet.Cells["D1"].Value = "Task ID";
                Sheet.Cells["E1"].Value = "Task No";
                Sheet.Cells["F1"].Value = "Sub Task";
                Sheet.Cells["G1"].Value = "Task Desc";
                Sheet.Cells["H1"].Value = "Default Value";

                int row = 2;

                foreach (var serviceSheet in serviceSheetReplacementGaps)
                {
                    Sheet.Cells[$"A{row}"].Value = taskDefaultValues.Where(x => x.taskId == serviceSheet.key).FirstOrDefault()?.id;
                    Sheet.Cells[$"B{row}"].Value = serviceSheet.modelId;
                    Sheet.Cells[$"C{row}"].Value = serviceSheet.psTypeId;
                    Sheet.Cells[$"D{row}"].Value = serviceSheet.key;
                    Sheet.Cells[$"E{row}"].Value = serviceSheet.description.Split(";")?[0]?.ToString();
                    Sheet.Cells[$"F{row}"].Value = serviceSheet.description.Split(";")?[1]?.ToString();
                    Sheet.Cells[$"G{row}"].Value = serviceSheet.description.Split(";")?[2]?.ToString();
                    Sheet.Cells[$"H{row}"].Value = taskDefaultValues.Where(x => x.taskId == serviceSheet.key).FirstOrDefault()?.defaultValue;

                    row++;
                }

                var stream = new MemoryStream(ep.GetAsByteArray());
                return stream.ToArray();
            }

            #endregion
        }

        public async Task<ServiceResult> UploadCBMDefaultValue(IFormFile files)
        {
            var taskDefaultValueRepo = new TaskCbmDefaultValueRepository(_connectionFactory, EnumContainer.TaskCbmDefaultValue);

            using (var excel = files.OpenReadStream())
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage(excel);

                ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                var rowCount = worksheet.Dimension.Rows;

                for (int row = 2; row <= rowCount; row++)
                {
                    TaskCBMDefaultValueRequest data = new TaskCBMDefaultValueRequest()
                    {
                        id = worksheet.Cells[row, 1].Value == null ? Guid.NewGuid().ToString() : worksheet.Cells[row, 1].Value.ToString(),
                        modelId = worksheet.Cells[row, 2].Value == null ? string.Empty : worksheet.Cells[row, 2].Value.ToString(),
                        psTypeId = worksheet.Cells[row, 3].Value == null ? string.Empty : worksheet.Cells[row, 3].Value.ToString(),
                        taskId = worksheet.Cells[row, 4].Value == null ? string.Empty : worksheet.Cells[row, 4].Value.ToString(),
                        defaultValue = worksheet.Cells[row, 8].Value == null ? string.Empty : worksheet.Cells[row, 8].Value.ToString()
                    };

                    CreateRequest createRequest = new CreateRequest()
                    {
                        entity = data,
                        employee = new EmployeeModel() { id = "SYSTEM", name = "SYSTEM" }
                    };

                    await taskDefaultValueRepo.Upsert(createRequest);
                }
            }

            return new ServiceResult
            {
                Message = "Data insert successfully",
                IsError = false,
                Content = null
            };
        }

        public async Task<ServiceResult> ResetUpdatedDate(ResetUpdatedDateRequest request)
        {
            try
            {
                var repoMasterServiceSheet = new MasterServiceSheetRepository(_connectionFactory, EnumContainer.MasterServiceSheet);

                Dictionary<string, object> param = new Dictionary<string, object>();
                if (request.modelIds != null)
                    param.Add(EnumQuery.ModelId, JArray.FromObject(request.modelIds));

                var taskWithUpdatedDate = await repoMasterServiceSheet.GetDataMasterServiceWithUpdatedDate(param);

                var taskUpdatedCount = 0;

                if (taskWithUpdatedDate.Count > 0)
                {
                    foreach (var task in taskWithUpdatedDate)
                    {

                        var rsc = await repoMasterServiceSheet.Get((string)task.id);

                        var propertyParam = new PropertyParam()
                        {
                            propertyName = EnumQuery.UpdatedDate,
                            propertyValue = string.Empty
                        };

                        var updateParam = new UpdateParam()
                        {
                            keyValue = (string)task.key,
                            propertyParams = new List<PropertyParam> { propertyParam }
                        };

                        var updateRequest = new UpdateRequest()
                        {
                            id = (string)task.id,
                            updateParams = new List<UpdateParam> {
                                updateParam
                            },
                            employee = request.employee
                        };
                        var result = await repoMasterServiceSheet.Update(updateRequest, rsc);
                        taskUpdatedCount++;
                    }
                }

                return new ServiceResult
                {
                    Message = $"{taskUpdatedCount} task updated successfully from md_servicesheet",
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

        public async Task<ServiceResult> GetMasterServiceSheet(string modelId)
        {
            ServiceSizeRequest resultServiceSizeList = new ServiceSizeRequest();

            List<dynamic> psType250 = new List<dynamic>();
            List<dynamic> psType500 = new List<dynamic>();
            List<dynamic> psType1000 = new List<dynamic>();
            List<dynamic> psType2000 = new List<dynamic>();
            List<dynamic> psType4000 = new List<dynamic>();

            #region 4000
            Dictionary<string, object> paramModel4000 = new Dictionary<string, object>();
            paramModel4000.Add(EnumQuery.ModelId, modelId);
            paramModel4000.Add(EnumQuery.PsTypeId, "4000");

            var resultModel4000 = await _repository.GetDataListByParam(paramModel4000);
            psType4000.AddRange(resultModel4000);
            #endregion

            #region 2000
            Dictionary<string, object> paramModel2000 = new Dictionary<string, object>();
            paramModel2000.Add(EnumQuery.ModelId, modelId);
            paramModel2000.Add(EnumQuery.PsTypeId, "2000");

            var resultModel2000 = await _repository.GetDataListByParam(paramModel2000);
            psType2000.AddRange(resultModel2000);
            #endregion

            #region 1000
            Dictionary<string, object> paramModel1000 = new Dictionary<string, object>();
            paramModel1000.Add(EnumQuery.ModelId, modelId);
            paramModel1000.Add(EnumQuery.PsTypeId, "1000");

            var resultModel1000 = await _repository.GetDataListByParam(paramModel1000);
            psType1000.AddRange(resultModel1000);
            #endregion

            #region 500
            Dictionary<string, object> paramModel500 = new Dictionary<string, object>();
            paramModel500.Add(EnumQuery.ModelId, modelId);
            paramModel500.Add(EnumQuery.PsTypeId, "500");

            var resultModel500 = await _repository.GetDataListByParam(paramModel500);
            psType500.AddRange(resultModel500);
            #endregion

            #region 250
            Dictionary<string, object> paramModel250 = new Dictionary<string, object>();
            paramModel250.Add(EnumQuery.ModelId, modelId);
            paramModel250.Add(EnumQuery.PsTypeId, "250");

            var resultModel250 = await _repository.GetDataListByParam(paramModel250);
            psType250.AddRange(resultModel250);
            #endregion

            Dictionary<string, object> result = new Dictionary<string, object>();

            if (psType4000.Count() != 0)
                result.Add("PsType4000", psType4000);

            if (psType2000.Count() != 0)
                result.Add("PsType2000", psType2000);

            if (psType1000.Count() != 0)
                result.Add("PsType1000", psType1000);

            if (psType500.Count() != 0)
                result.Add("PsType500", psType500);

            if (psType250.Count() != 0)
                result.Add("PsType250", psType250);

            return new ServiceResult() { IsError = false, Content = result };
        }

        public async Task<ServiceResult> CheckModelDocuments(Dictionary<string, object> param)
        {
            var models = (JArray)param.GetValueOrDefault("modelId", new JArray());
            List<string> modelIds = models.Select(j => (string)j).ToList();
            var repo = new MasterServiceSheetRepository(_connectionFactory, _container);
            dynamic result = await repo.CheckModelDocuments(modelIds);

            return new ServiceResult
            {
                IsError = false,
                Message = "Get document list successfully",
                Content = result
            };
        }


        public async Task<ServiceResult> CopyModelExisting(string modelId, string newModelId)
        {

            var dataParam = new Dictionary<string, object>
            {
                { EnumQuery.ModelId, modelId },
                { EnumQuery.IsDeleted, "false" }
            };

            var result = await _repository.GetDataListByParam(dataParam);

            foreach (var item in result)
            {
                item[EnumQuery.ModelId] = newModelId;

                if (item[EnumQuery.GroupName] == EnumGroup.General)
                {
                    string groupName = StaticHelper.GetPropValue(item, EnumQuery.Form);
                    if (!string.IsNullOrEmpty(groupName))
                    {
                        item[EnumQuery.Form] = groupName.Replace(modelId, newModelId);
                    }
                }

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

                var createReq = new CreateRequest();
                createReq.employee = new EmployeeModel();

                createReq.employee.id = "SYSTEM";
                createReq.employee.name = "SYSTEM";
                createReq.entity = item;

                await _repository.Create(createReq);
            }


            // previout tandem
            var taskTandemRepo = new TaskTandemRepository(_connectionFactory, EnumContainer.TaskTandem);
            var taskTandemResult = await taskTandemRepo.GetDataListByParam(dataParam);

            foreach (var item in taskTandemResult)
            {
                item[EnumQuery.ModelId] = newModelId;

                if (item[EnumQuery.GroupName] == EnumGroup.General)
                {
                    string groupName = StaticHelper.GetPropValue(item, EnumQuery.Form);
                    if (!string.IsNullOrEmpty(groupName))
                    {
                        item[EnumQuery.Form] = groupName.Replace(modelId, newModelId);
                    }
                }

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

                var createReq = new CreateRequest();
                createReq.employee = new EmployeeModel();

                createReq.employee.id = "SYSTEM";
                createReq.employee.name = "SYSTEM";
                createReq.entity = item;

                await taskTandemRepo.Create(createReq);
            }

            return new ServiceResult
            {
                Message = "",
                IsError = false,
                Content = "Model copied successfully"
            };
        }
    }
}
