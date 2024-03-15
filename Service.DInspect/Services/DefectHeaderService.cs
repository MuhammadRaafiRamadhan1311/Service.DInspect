using Azure.Core;
using Service.DInspect.Interfaces;
using Service.DInspect.Models.Enum;
using Service.DInspect.Models.Request;
using Service.DInspect.Models;
using Service.DInspect.Repositories;
using Service.DInspect.Services.Helpers;
using Service.DInspect.Services;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OfficeOpenXml.Style;
using OfficeOpenXml;
using System.IO;

namespace Service.DInspect.Services
{
    public class DefectHeaderService : ServiceBase
    {
        private string _container;
        private IConnectionFactory _connectionFactory;
        private IRepositoryBase _serviceSheetDetail;
        protected IRepositoryBase _serviceSheetHeaderRepository;
        protected IRepositoryBase _defectDetailRepository;
        protected IRepositoryBase _psTypeSettingRepository;
        protected IRepositoryBase _taskCrackRepository;

        public DefectHeaderService(MySetting appSetting, IConnectionFactory connectionFactory, string container, string accessToken) : base(appSetting, connectionFactory, container, accessToken)
        {
            _connectionFactory = connectionFactory;
            _repository = new DefectHeaderRepository(connectionFactory, container);
            _serviceSheetDetail = new ServiceSheetDetailRepository(connectionFactory, EnumContainer.ServiceSheetDetail);
            _defectDetailRepository = new DefectDetailRepository(connectionFactory, EnumContainer.DefectHeader);
            _psTypeSettingRepository = new PsTypeSettingRepository(connectionFactory, EnumContainer.PsTypeSetting);
            _taskCrackRepository = new TaskCrackRepository(connectionFactory, EnumContainer.TaskCrack);
        }

        public async Task<ServiceResult> GetDefectExcel(Dictionary<string, object> param)
        {
            List<string> fieldsparam = new List<string>() { EnumQuery.Workorder, EnumQuery.Form, EnumQuery.ServiceSheetDetailId, EnumQuery.InterventionId, EnumQuery.InterventionHeaderId, EnumQuery.Category, EnumQuery.TaskId, EnumQuery.TaskNo, EnumQuery.TaskDesc, EnumQuery.priorityType, EnumQuery.DefectWorkorder, EnumQuery.formDefect, EnumQuery.DefectType, EnumQuery.TaskValue, EnumQuery.RepairedStatus, EnumQuery.CbmNAStatus, EnumQuery.CbmMeasurement, EnumQuery.CbmUom, EnumQuery.CbmImageKey, EnumQuery.CbmImageProp, EnumQuery.CbmRatingType, EnumQuery.isCbmAdjustment, EnumQuery.Supervisor, EnumQuery.Status, EnumQuery.PlannerStatus, EnumQuery.DeclineReason, EnumQuery.DeclineBy, EnumQuery.DeclineDate };
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
                var worksheet = package.Workbook.Worksheets.Add("Sheet1");

                List<string> headers = new List<string> { "workorder", "form", "serviceSheetDetailId", "interventionId", "interventionHeaderId", "taskId", "taskNo", "taskDesc", "priorityType", "defectWorkorder", "formDefect", "defectType", "taskValue", "repairedStatus", "cbmNAStatus", "cbmMeasurement", "cbmUom", "cbmImageKey", "cbmImageProp", "cbmRatingType", "isCbmAdjustment", "supervisor", "status", "plannerStatus", "declineReason", "declineBy", "declineDate" };

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
                    string servicesheetDetailId = GetValueOrDefault(item.servicesheetDetailId);
                    worksheet.Cells[string.Format("A{0}", row)].Value = GetValueOrDefault(item.workorder);
                    worksheet.Cells[string.Format("B{0}", row)].Value = GetValueOrDefault(item.form);
                    worksheet.Cells[string.Format("C{0}", row)].Value = GetValueOrDefault(item.serviceSheetDetailId);
                    worksheet.Cells[string.Format("D{0}", row)].Value = GetValueOrDefault(item.interventionId);
                    worksheet.Cells[string.Format("E{0}", row)].Value = GetValueOrDefault(item.interventionHeaderId);
                    worksheet.Cells[string.Format("F{0}", row)].Value = GetValueOrDefault(item.taskId);
                    worksheet.Cells[string.Format("G{0}", row)].Value = GetValueOrDefault(item.taskNo);
                    worksheet.Cells[string.Format("H{0}", row)].Value = GetValueOrDefault(item.taskDesc);
                    worksheet.Cells[string.Format("I{0}", row)].Value = GetValueOrDefault(item.priorityType);
                    worksheet.Cells[string.Format("J{0}", row)].Value = GetValueOrDefault(item.defectWorkorder);
                    worksheet.Cells[string.Format("K{0}", row)].Value = GetValueOrDefault(item.formDefect);
                    worksheet.Cells[string.Format("L{0}", row)].Value = GetValueOrDefault(item.defectType);
                    worksheet.Cells[string.Format("M{0}", row)].Value = GetValueOrDefault(item.taskValue);
                    worksheet.Cells[string.Format("N{0}", row)].Value = GetValueOrDefault(item.repairedStatus);
                    worksheet.Cells[string.Format("O{0}", row)].Value = GetValueOrDefault(item.cbmNAStatus);
                    worksheet.Cells[string.Format("P{0}", row)].Value = GetValueOrDefault(item.cbmMeasurement);
                    worksheet.Cells[string.Format("Q{0}", row)].Value = GetValueOrDefault(item.cbmUom);
                    worksheet.Cells[string.Format("R{0}", row)].Value = GetValueOrDefault(item.cbmImageKey);
                    worksheet.Cells[string.Format("S{0}", row)].Value = GetValueOrDefault(item.cbmImageProp);
                    worksheet.Cells[string.Format("T{0}", row)].Value = GetValueOrDefault(item.cbmRatingType);
                    worksheet.Cells[string.Format("U{0}", row)].Value = GetValueOrDefault(item.isCbmAdjustment);
                    worksheet.Cells[string.Format("V{0}", row)].Value = GetValueOrDefault(item.supervisor);
                    worksheet.Cells[string.Format("W{0}", row)].Value = GetValueOrDefault(item.status);
                    worksheet.Cells[string.Format("X{0}", row)].Value = GetValueOrDefault(item.plannerStatus);
                    worksheet.Cells[string.Format("Y{0}", row)].Value = GetValueOrDefault(item.declineReason);
                    worksheet.Cells[string.Format("Z{0}", row)].Value = GetValueOrDefault(item.declineBy?.name);
                    worksheet.Cells[string.Format("Z1{0}", row)].Value = GetValueOrDefault(item.declineDate);

                    row++;
                }

                string GetValueOrDefault(object value)
                {
                    return value != null ? value.ToString() : "";
                }


                worksheet.Cells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                worksheet.Cells[1, 1, 1, worksheet.Dimension.Columns].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[1, 1, 1, worksheet.Dimension.Columns].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.GreenYellow);
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
