using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service.DInspect.Interfaces;
using Service.DInspect.Models.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.DInspect.Repositories
{
    public class SOSRepository : RepositoryBase
    {
        public SOSRepository(IConnectionFactory connectionFactory, string container) : base(connectionFactory, container)
        {
        }

        public virtual async Task<JArray> GetDataSosLatest(Dictionary<string, object> paramLatestSmu)
        {
            string query = $"SELECT subgroup.key keyCompartment, task.key, c.meterHrs, udf.formatdatetime(task.updatedDate) updatedDate FROM c join subgroup in c.subGroup join task in subgroup.task WHERE c.equipment = \"{paramLatestSmu[EnumQuery.Equipment]}\" and task.name = \"{EnumQuery.LubeServiceChange}\" and task.taskValue = \"{EnumTaskValue.NormalOK}\" or (task.taskValue = \"{EnumTaskValue.IntNormalCompleted}\" AND c.eformType = \"{EnumEformType.EformIntervention}\") and task.updatedDate != '' and c.isDeleted = \"false\" and subgroup.key != ''";

            var response = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(query));

            JArray results = new JArray();

            while (response.HasMoreResults)
            {
                foreach (var item in await response.ReadNextAsync())
                    results.Add(item);
            }

            var topResult = results.OrderByDescending(x => x["updatedDate"]).ToList();

            return JArray.Parse(JsonConvert.SerializeObject(topResult));
        }
    }
}
