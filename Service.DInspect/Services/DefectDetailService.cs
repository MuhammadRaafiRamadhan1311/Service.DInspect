using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Service.DInspect.Interfaces;
using Service.DInspect.Models;
using Service.DInspect.Models.Enum;
using OfficeOpenXml;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Database;
using OfficeOpenXml.Style;
using Service.DInspect.Helpers;
using Service.DInspect.Interfaces;
using Service.DInspect.Models.ADM;
using Service.DInspect.Models.Entity;
using Service.DInspect.Models.Request;
using Service.DInspect.Models.Response;
using Service.DInspect.Repositories;
using Service.DInspect.Services.Helpers;
using Service.DInspect.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using LicenseContext = OfficeOpenXml.LicenseContext;
using Service.DInspect.Interfaces;
using Service.DInspect.Models.Enum;
using Service.DInspect.Models;
using Service.DInspect.Repositories;
using Service.DInspect.Services;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;
using Newtonsoft.Json;


namespace Service.DInspect.Services
{
    public class DefectDetailService : ServiceBase
    {
        protected IConnectionFactory _connectionFactory;
        protected IRepositoryBase _serviceSheetHeaderRepository;
        protected IRepositoryBase _defectHeaderRepository;
        protected IRepositoryBase _psTypeSettingRepository;
        protected IRepositoryBase _taskCrackRepository;

        public DefectDetailService(MySetting appSetting, IConnectionFactory connectionFactory, string container, string accessToken) : base(appSetting, connectionFactory, container, accessToken)
        {
            _connectionFactory = connectionFactory;
            _repository = new DefectDetailRepository(connectionFactory, container);
            _serviceSheetHeaderRepository = new ServiceSheetHeaderRepository(connectionFactory, EnumContainer.ServiceSheetHeader);
            _defectHeaderRepository = new DefectHeaderRepository(connectionFactory, EnumContainer.DefectHeader);
            _psTypeSettingRepository = new PsTypeSettingRepository(connectionFactory, EnumContainer.PsTypeSetting);
            _taskCrackRepository = new TaskCrackRepository(connectionFactory, EnumContainer.TaskCrack);
        }

        public async Task<ServiceResult> GetDefectExcel(Dictionary<string, object> param)
        {
            List<string> fieldsparam = new List<string>() { EnumQuery.Key, EnumQuery.Workorder, EnumQuery.DefectHeaderId, EnumQuery.ServiceSheetDetailId, EnumQuery.InterventionId, EnumQuery.InterventionHeaderId, EnumQuery.TaskId, EnumQuery.Detail, EnumQuery.CreatedBy, EnumQuery.CreatedDate, EnumQuery.UpdatedBy, EnumQuery.UpdatedDate };
            var _param = new Dictionary<string, object>
            {
                { EnumQuery.Workorder, param[EnumQuery.Workorder]},
                { EnumQuery.Fields, fieldsparam}
            };

            var ListData = await _repository.GetDataListByParam(param);

            MemoryStream stream = new MemoryStream();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets.Add("Sheet2");

                List<string> headers = new List<string> { "key", "workorder", "defectHeaderId", "servicesheetDetailId", "interventionId", "interventionHeaderId", "taskId", "detail", "createdBy", "createdDate", "updatedBy", "updatedDate" };

                var count = 1;
                foreach (var header in headers)
                {
                    worksheet.Cells[1, count].Value = header;
                    count++;
                }

                int row = 2;

                foreach (var data in ListData)
                {
                    dynamic item = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(data));
                    worksheet.Cells[string.Format("A{0}", row)].Value = item.key.ToString();
                    worksheet.Cells[string.Format("B{0}", row)].Value = item.workorder.ToString();
                    worksheet.Cells[string.Format("C{0}", row)].Value = item.defectHeaderId.ToString();
                    worksheet.Cells[string.Format("D{0}", row)].Value = item.servicesheetDetailId.ToString();
                    worksheet.Cells[string.Format("E{0}", row)].Value = (item.interventionId != null) ? item.interventionId.ToString() : "";
                    worksheet.Cells[string.Format("F{0}", row)].Value = (item.interventionHeaderId != null) ? item.interventionHeaderId.ToString() : "";
                    worksheet.Cells[string.Format("G{0}", row)].Value = item.taskId.ToString();
                    worksheet.Cells[string.Format("H{0}", row)].Value = item.detail.ToString();
                    worksheet.Cells[string.Format("I{0}", row)].Value = item.createdBy.name.ToString();
                    worksheet.Cells[string.Format("J{0}", row)].Value = item.createdDate.ToString();
                    worksheet.Cells[string.Format("K{0}", row)].Value = (item.updatedBy.ToString() == "") ? "" : item.updatedBy.name.ToString();
                    worksheet.Cells[string.Format("L{0}", row)].Value = item.updatedDate.ToString();

                    row++;
                }

                worksheet.Cells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                worksheet.Cells[1, 1, 1, worksheet.Dimension.Columns].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[1, 1, 1, worksheet.Dimension.Columns].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Yellow);
                worksheet.Cells[1, 1, 1, worksheet.Dimension.Columns].Style.Font.Bold = true;
                worksheet.Cells[worksheet.Dimension.Address].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[worksheet.Dimension.Address].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[worksheet.Dimension.Address].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[worksheet.Dimension.Address].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                package.SaveAs(stream);
                stream.Position = 0;

            }

            return new ServiceResult
            {
                Message = "Get  defect history successfully",
                IsError = false,
                Content = stream
            };
        }

 
    }
}
