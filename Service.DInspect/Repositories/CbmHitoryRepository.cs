using Newtonsoft.Json.Linq;
using Service.DInspect.Interfaces;
using System.Linq;
using System.Threading.Tasks;

namespace Service.DInspect.Repositories
{
    public class CbmHitoryRepository : RepositoryBase
    {
        public CbmHitoryRepository(IConnectionFactory connectionFactory, string container) : base(connectionFactory, container)
        {
        }

        public virtual async Task<dynamic> GetDataPreviousCbmHistory(string equipment, string modelId)
        {
            //string query = $"SELECT c.workOrder, c.taskDescription, c.taskKey, c.detail.rating, ARRAY_SLICE(c.detail.history,-1)[0] AS lastItems FROM c  where c.equipment = \"{equipment}\" and  c.modelId = \"{modelId}\"";
            string query = $"SELECT c.workOrder, c.replacementValue, c.currentValue, c.taskDescription, c.taskKey, c.detail.rating, ARRAY_SLICE(c.detail.history,-1)[0] AS lastItems, c.updatedDate = \"\" ? udf.formatdatetime(c.createdDate) : udf.formatdatetime(c.updatedDate) as serviceDataConvert, c.source FROM c  where c.equipment = \"{equipment}\" and  c.modelId = \"{modelId}\"";

            var response = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(query));

            JArray results = new JArray();

            while (response.HasMoreResults)
            {
                foreach (var item in await response.ReadNextAsync())
                    results.Add(item);
            }

            //var topResult = results.OrderByDescending(x => x["serviceDataConvert"]).FirstOrDefault();

            return results;
        }
    }
}
