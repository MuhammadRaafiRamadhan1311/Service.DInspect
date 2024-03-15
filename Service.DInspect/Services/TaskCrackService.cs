using ClosedXML.Excel;
using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using Service.DInspect.Interfaces;
using Service.DInspect.Models;
using Service.DInspect.Models.Enum;
using Service.DInspect.Models.Request;
using Service.DInspect.Models.Entity;
using Service.DInspect.Repositories;
using Service.DInspect.Helpers;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Threading.Tasks;

namespace Service.DInspect.Services
{
    public class TaskCrackService : ServiceBase
    {
        public TaskCrackService(MySetting appSetting, IConnectionFactory connectionFactory, string container, string accessToken) : base(appSetting, connectionFactory, container, accessToken)
        {
            _repository = new TaskCrackRepository(connectionFactory, container);
        }

        public async Task<ServiceResult> UploadTaskCrack(IFormFile files)
        {
            List<TaskCrackUploadModel> result = new List<TaskCrackUploadModel>();

            using (var excel = files.OpenReadStream())
            {
                using var workBook = new XLWorkbook(excel);
                IXLWorksheet workSheet = workBook.Worksheets.Where(x => x.Name == "TaskCrack").FirstOrDefault();

                int row = 2;
                int countData = workSheet.RowsUsed().Count() - 1;
                for (int i = 0; i < countData; i++)
                {
                    TaskCrackUploadModel taskCrack = new TaskCrackUploadModel()
                    {
                        id = null,
                        modelId = workSheet.Cell(string.Format("A{0}", row)).Value.ToString(),
                        psTypeId = workSheet.Cell(string.Format("B{0}", row)).Value.ToString(),
                        taskId = workSheet.Cell(string.Format("C{0}", row)).Value.ToString(),
                        taskCrackCode = workSheet.Cell(string.Format("D{0}", row)).Value.ToString(),
                        locationDesc = workSheet.Cell(string.Format("E{0}", row)).Value.ToString(),
                        uom = "mm"
                    };

                    Dictionary<string, object> paramMapping = new Dictionary<string, object>();
                    paramMapping.Add("modelId", taskCrack.modelId);
                    paramMapping.Add("psTypeId", taskCrack.psTypeId);
                    paramMapping.Add("taskId", taskCrack.taskId);

                    var resultData = await _repository.GetDataByParam(paramMapping);

                    if (resultData != null)
                    {
                        string _id = resultData[EnumQuery.ID];
                        taskCrack.id = _id;
                    }
                    else
                    {
                        taskCrack.id = Guid.NewGuid().ToString();
                    }

                    CreateRequest createRequest = new CreateRequest()
                    {
                        entity = taskCrack,
                        employee = new EmployeeModel() { id = "SYSTEM", name = "SYSTEM" }
                    };

                    await _repository.Upsert(createRequest);

                    result.Add(taskCrack);

                    row++;
                }
            }

            return new ServiceResult
            {
                Message = "Data insert successfully",
                IsError = false,
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


            return new ServiceResult
            {
                Message = "",
                IsError = false,
                Content = "Model copied successfully"
            };
        }
    }
}