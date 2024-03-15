using Service.DInspect.Models;
using Service.DInspect.Models.Request;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.DInspect.Interfaces
{
    public interface IServiceBase
    {
        Task<ServiceResult> GetAllData();
        Task<ServiceResult> GetActiveData();
        Task<ServiceResult> Get(string id);
        Task<ServiceResult> GetDataListByParam(Dictionary<string, object> param);
        Task<ServiceResult> GetDataByParam(Dictionary<string, object> param);
        Task<ServiceResult> GetDataListByParam(Dictionary<string, object> param, int limit, string orderBy, string orderType);
        Task<ServiceResult> Post(CreateRequest createRequest);
        Task<ServiceResult> Post(CreateRequest createRequest, Dictionary<string, string> adjustFields);
        Task<ServiceResult> Put(UpdateRequest updateRequest);
        Task<ServiceResult> Delete(DeleteRequest deleteRequest);
        Task<ServiceResult> GetFieldValue(GetFieldValueRequest getFieldValueRequest);
        Task<ServiceResult> Upsert(CreateRequest createRequest);
    }
}
