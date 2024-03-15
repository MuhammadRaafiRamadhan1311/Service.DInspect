using Microsoft.AspNetCore.Mvc;
using Service.DInspect.Models;
using Service.DInspect.Models.Request;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.DInspect.Interfaces
{
    public interface IBaseController
    {
        Task<ActionResult<ApiResponse>> GetAllData();
        Task<ActionResult<ApiResponse>> GetActiveData();
        Task<ActionResult<ApiResponse>> Get(string id);
        Task<ActionResult<ApiResponse>> GetDataListByParam(Dictionary<string, object> param);
        Task<ActionResult<ApiResponse>> GetDataByParam(Dictionary<string, object> param);
        Task<ActionResult<ApiResponse>> Post(CreateRequest createRequest);
        Task<ActionResult<ApiResponse>> Put(UpdateRequest updateRequest);
        Task<ActionResult<ApiResponse>> Delete(DeleteRequest deleteRequest);
        Task<ActionResult<ApiResponse>> GetFieldValue(GetFieldValueRequest getFieldValueRequest);
    }
}
