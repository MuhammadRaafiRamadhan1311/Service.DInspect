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

namespace Service.DInspect.Services
{
    public class ServiceSheetDetailService : ServiceBase
    {
        protected string _container;
        protected IConnectionFactory _connectionFactory;
        protected IRepositoryBase _servicesheetHeaderRepository;
        protected IRepositoryBase _defectHeaderRepository;
        protected IRepositoryBase _defectDetailRepository;
        protected IRepositoryBase _psTypeSettingRepository;
        protected IRepositoryBase _taskTandemRepository;
        //private readonly ILogger<ServiceSheetDetailService> _logger;

        public ServiceSheetDetailService(MySetting appSetting, IConnectionFactory connectionFactory, string container, string accessToken, ILoggerFactory logger) : base(appSetting, connectionFactory, container, accessToken)
        {
            _container = container;
            _connectionFactory = connectionFactory;
            _repository = new ServiceSheetDetailRepository(connectionFactory, container);
            _servicesheetHeaderRepository = new ServiceSheetHeaderRepository(connectionFactory, EnumContainer.ServiceSheetHeader);
            _defectHeaderRepository = new DefectHeaderRepository(connectionFactory, EnumContainer.DefectHeader);
            _defectDetailRepository = new DefectDetailRepository(connectionFactory, EnumContainer.DefectDetail);
            _psTypeSettingRepository = new PsTypeSettingRepository(connectionFactory, EnumContainer.PsTypeSetting);
            _taskTandemRepository = new TaskTandemRepository(connectionFactory, EnumContainer.TaskTandem);
            //_logger = logger.CreateLogger<ServiceSheetDetailService>();
        }

        public async Task<ServiceResult> GetServiceSheetExcel(Dictionary<string, string> param)
        {
            List<string> fieldsParam = new List<string>() { EnumQuery.ID, EnumQuery.HeaderId, EnumQuery.SSWorkorder, EnumQuery.TaskKey, EnumQuery.GroupName, EnumQuery.Description, EnumQuery.Category, EnumQuery.Rating, EnumQuery.TaskValue, EnumQuery.TaskNo, EnumQuery.Uom, EnumQuery.MeasurementValue, $"{EnumQuery.CreatedBy}[\"name\"]", EnumQuery.CreatedDate, $"{EnumQuery.UpdatedBy}[\"name\"]", EnumQuery.UpdatedDate };
            var _param = new Dictionary<string, object>
            {
                { EnumQuery.SSWorkorder, param[EnumQuery.SSWorkorder]},
                { EnumQuery.IsActive, "true"},
                { EnumQuery.IsDeleted, "false"},
                { EnumQuery.Fields, fieldsParam}
            };

            var listData = await _repository.GetDataListByParam(_param);

            MemoryStream stream = new MemoryStream();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets.Add("Sheet1");

                List<string> headers = new List<string> { "id", "header_id", "work_order", "task_key", "group_name", "description", "category", "rating", "task_value", "task_number", "uom", "measurement_value", "created_by", "created_on", "changed_by", "changed_on", "sync_date" };

                var count = 0;
                foreach (var header in headers)
                {
                    count++;
                    worksheet.Cells[1, count].Value = header;
                }

                worksheet.Cells.LoadFromCollection(listData, true);
                worksheet.Cells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                worksheet.Cells[1, 1, 1, worksheet.Dimension.Columns].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[1, 1, 1, worksheet.Dimension.Columns].Style.Fill.BackgroundColor.SetColor(Color.Yellow);
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
                Message = "Get  service sheet history successfully",
                IsError = false,
                Content = stream
            };
        }
    }
}