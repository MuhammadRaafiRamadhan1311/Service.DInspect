using Newtonsoft.Json.Linq;
using Service.DInspect.Models.Helper;
using Service.DInspect.Models.Request;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.DInspect.Interfaces
{
    public interface IRepositoryBase
    {
        Task<IEnumerable<dynamic>> GetAllData();
        Task<IEnumerable<dynamic>> GetActiveData();
        Task<dynamic> Get(string id);
        Task<dynamic> GetDataListByParam(Dictionary<string, object> param, bool includeSoftDeleted = false);
        Task<dynamic> GetDataByParam(Dictionary<string, object> param);
        Task<dynamic> GetDataListByParam(List<FilterHelperModel> param);
        Task<dynamic> GetDataListByParam(Dictionary<string, object> param, int limit, string orderBy, string orderType);
        Task<dynamic> Create(CreateRequest createRequest);
        Task<dynamic> CreateList(CreateRequest createRequest);
        Task<dynamic> Create(CreateRequest createRequest, Dictionary<string, string> adjustFields);
        Task<dynamic> Update(UpdateRequest updateRequest, dynamic rsc);
        Task<dynamic> Delete(DeleteRequest deleteRequest);
        Task<dynamic> HardDelete(DeleteRequest deleteRequest);
        Task<dynamic> DeleteByParam(DeleteByParamRequest deleteByParamRequest);
        Task<dynamic> HardDeleteByParam(DeleteByParamRequest deleteByParamRequest);
        Task<dynamic> GetFieldValue(GetFieldValueRequest getFieldValueRequest, bool isAllowNoField = false);
        Task<dynamic> GetFieldValueList(GetFieldValueListRequest getFieldValueRequest, bool isAllowNoField = false);
        Task<JArray> GetActiveDataJArray();
        Task<JArray> GetDataListByParamJArray(Dictionary<string, object> param);
        Task<dynamic> Upsert(CreateRequest createRequest);
    }
}
