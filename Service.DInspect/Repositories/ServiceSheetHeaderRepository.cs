using Newtonsoft.Json.Linq;
using Service.DInspect.Interfaces;
using Service.DInspect.Models.Request;
using Service.DInspect.Models.Enum;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Service.DInspect.Repositories
{
    public class ServiceSheetHeaderRepository : RepositoryBase
    {
        public ServiceSheetHeaderRepository(IConnectionFactory connectionFactory, string container) : base(connectionFactory, container)
        {
        }

        public virtual async Task<dynamic> GetDataTandemLatest(TandemParam param)
        {
            string query = $"SELECT c.workOrder, c[\"status\"], c.serviceEnd, udf.formatdatetime(c.serviceEnd) as serviceDataConvert FROM c WHERE c.modelId = \"{param.modelId}\" AND c.equipment = \"{param.equipment}\" AND c.psTypeId != \"250\" AND  c[\"status\"] = \"{param.status}\" AND c.defectStatus = \"Completed\" AND c.serviceEnd != \"\"";

            var response = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(query));

            JArray results = new JArray();

            while (response.HasMoreResults)
            {
                foreach (var item in await response.ReadNextAsync())
                    results.Add(item);
            }

            var topResult = results.OrderByDescending(x => x["serviceDataConvert"]).FirstOrDefault();

            return topResult;
        }

        public virtual async Task<dynamic> GetDataPrevCrackLatest(PrevCrackParam param)
        {
            string query = $"SELECT c.workOrder, c[\"status\"], c.serviceStart, c.serviceEnd, udf.formatdatetime(c.serviceEnd) as serviceDataConvert FROM c WHERE c.modelId = \"{param.modelId}\" AND c.equipment = \"{param.equipment}\" AND c.psTypeId != \"250\" AND  c[\"status\"] = \"{param.status}\" AND c.serviceEnd != \"\"";

            var response = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(query));

            JArray results = new JArray();

            while (response.HasMoreResults)
            {
                foreach (var item in await response.ReadNextAsync())
                    results.Add(item);
            }

            var topResult = results.OrderByDescending(x => x["serviceDataConvert"]).FirstOrDefault();

            return topResult;
        }
    }
}
