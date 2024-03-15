using Service.DInspect.Interfaces;
using Service.DInspect.Models;
using Service.DInspect.Models.Enum;
using Service.DInspect.Models.Entity;
using Service.DInspect.Models.Request;
using Service.DInspect.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.DInspect.Services
{
    public class TaskCalibrationService : ServiceBase
    {
        public TaskCalibrationService(MySetting appSetting, IConnectionFactory connectionFactory, string container, string accessToken) : base(appSetting, connectionFactory, container, accessToken)
        {
            _repository = new TaskCalibrationRepository(connectionFactory, container);
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