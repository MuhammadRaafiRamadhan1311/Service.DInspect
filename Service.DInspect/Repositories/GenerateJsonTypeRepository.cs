using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service.DInspect.Interfaces;
using Service.DInspect.Models.Enum;
using Service.DInspect.Models.Response;
using System;
using System.Threading.Tasks;

namespace Service.DInspect.Repositories
{
    public class GenerateJsonTypeRepository : RepositoryBase
    {
        public GenerateJsonTypeRepository(IConnectionFactory connectionFactory, string container) : base(connectionFactory, container)
        {
        }

        public virtual async Task<dynamic> GetDataTypeList(string[] lisType)
        {
            string dataQuery = string.Join(", ", lisType);
            string result = string.Concat(lisType);

            string query = $"SELECT * FROM c WHERE c.type IN (\"{dataQuery}\")";

            var response = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(query));

            JArray results = new JArray();

            while (response.HasMoreResults)
            {
                foreach (var item in await response.ReadNextAsync())
                    results.Add(item);
            }

            return results;
        }
    }
}
