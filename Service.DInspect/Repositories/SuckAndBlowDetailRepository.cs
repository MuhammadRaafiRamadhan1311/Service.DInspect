using Newtonsoft.Json.Linq;
using Service.DInspect.Interfaces;
using Service.DInspect.Models.Request;
using System.Threading.Tasks;

namespace Service.DInspect.Repositories
{
    public class SuckAndBlowDetailRepository : RepositoryBase
    {
        public SuckAndBlowDetailRepository(IConnectionFactory connectionFactory, string container) : base(connectionFactory, container)
        {
        }

        public virtual async Task<dynamic> GetDataItemsDetailValue(string id, string itemKey)
        {
            string query = $"SELECT     c.modelId,    c.psTypeId,    task.key,     task.category,     items[\"itemType\"],    items[\"isTaskValue\"],    task.taskValue,    CONCAT(task.items[0][\"value\"], \". \", task.items[1][\"value\"], \" \", task.items[2][\"value\"]) as description, task.updatedBy, items[\"value\"], items[\"style\"] FROM c join subGroup in c.subGroup join taskGroup in subGroup.taskGroup join task in taskGroup.task join items in task.items where      items[\"key\"] = \"{itemKey}\" and     c.id = \"{id}\" order by c.groupSeq";

            var response = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(query));

            JArray results = new JArray();

            while (response.HasMoreResults)
            {
                foreach (var item in await response.ReadNextAsync())
                    results.Add(item);
            }
            return results;
        }

        public virtual async Task<dynamic> GetDataServiceSheetDetailByKey(DetailServiceSheet model)
        {
            string query = $"SELECT c.id, task.key, task.seqId, task.taskType, task.isActive, task.isDeleted, task.updatedBy, task.header, task.description, task.category, task.rating, task.updatedDate,        task.groupTaskId,     (IS_DEFINED(task.adjustment) and task.adjustment[\"rating\"] <> \"\") or (IS_DEFINED(task.replacement) and task.replacement[\"rating\"] <> \"\") ? true : false as cbmAdjustmentReplacement,       IS_DEFINED(task.adjustment) ? task.adjustment[\"pictures\"] = [] ? \"\" : task.adjustment[\"pictures\"] :     IS_DEFINED(task.replacement) ? task.replacement[\"pictures\"] = [] ? \"\" : task.replacement[\"pictures\"] :    \"\" as pictures,       task.items,        IS_DEFINED(task.adjustment) ? task.adjustment[\"rating\"] <> \"\" ? task.adjustment[\"rating\"] : task.taskValue :      IS_DEFINED(task.replacement) ? task.replacement[\"rating\"] <> \"\" ? task.replacement[\"rating\"] : task.taskValue : task.taskValue as taskValue,      task.items[0][\"value\"] as taskNo,      task.rating = \"MANUAL\" OR task.rating = \"NORMAL\"  ? \"\"          : task.items[3][\"categoryItemType\"] = \"dropdownTool\" ? task.items[5][\"value\"]         : task.items[3][\"categoryItemType\"] = \"brakeTypeDropdown\" ? task.items[6][\"value\"]         : task.items[4][\"categoryItemType\"] = \"dropdownToolDisc\" ? task.items[6][\"value\"] : task.items[4][\"categoryItemType\"] = \"dropdownTool\" ? task.items[6][\"value\"] : task.rating = \"AUTOMATIC\" AND  task.items[3][\"valueItemType\"] = \"comment\" ? task.items[5][\"value\"]  : task.rating = \"AUTOMATIC\" ? task.items[4][\"value\"]          : task.items[5][\"value\"] = [] ? \"\"          : task.items[5][\"value\"] as uom,      task.rating = \"MANUAL\" OR task.rating = \"NORMAL\" ? \"\"          : task.items[3][\"categoryItemType\"] = \"dropdownTool\" ? task.items[4][\"value\"]         : task.items[3][\"categoryItemType\"] = \"brakeTypeDropdown\" ? task.items[5][\"value\"]         : task.items[4][\"categoryItemType\"] = \"dropdownToolDisc\" ? task.items[5][\"value\"]         : IS_DEFINED(task.adjustment) ? task.adjustment[\"measurement\"] <> \"\" ? task.adjustment[\"measurement\"]:task.items[3][\"value\"]       : IS_DEFINED(task.replacement) ? task.replacement[\"measurement\"] <> \"\" ? task.replacement[\"measurement\"] : task.items[4][\"value\"] : task.items[4][\"categoryItemType\"] = \"dropdownTool\" ? task.items[5][\"value\"]      : task.rating = \"AUTOMATIC\" ? IS_DEFINED(task.items[3][\"valueItemType\"]) ? task.items[4][\"value\"] : task.items[3][\"value\"]           : task.items[4][\"value\"] as measurementValue, (IS_DEFINED(task.adjustment) and task.adjustment[\"rating\"] <> \"\") = true ? task.adjustment : (IS_DEFINED(task.replacement) and task.replacement[\"rating\"] <> \"\") = true ? task.replacement : \"\" as cbmAdjustmentReplacementValue, (IS_DEFINED(task.adjustment) and task.adjustment[\"rating\"] <> \"\") or (IS_DEFINED(task.replacement) and task.replacement[\"rating\"] <> \"\") = true ? task.items[3][\"value\"] : \"\"  as nonCbmAdjustmentReplacementMeasurementValue, (IS_DEFINED(task.adjustment) and task.adjustment[\"rating\"] <> \"\") or (IS_DEFINED(task.replacement) and task.replacement[\"rating\"] <> \"\") = true ? task.items[6][\"value\"] : \"\" as nonCbmAdjustmentReplacementRating, task.SectionData FROM c join subGroup in c.subGroup join taskGroup in subGroup.taskGroup join task in taskGroup.task ";
            //where     c.workOrder = \"{model.workOrder}\" and  task.key = \"{model.taskKey}\"";
            if (model.taskKey == null || model.taskKey == "")
            {
                query += $"where c.workOrder = \"{model.workOrder}\" and task.rating like \"%REPLACEMENT%\"";
            }
            else
            {
                query += $"where     c.workOrder = \"{model.workOrder}\" and  task.key = \"{model.taskKey}\"";
            }

            var response = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(query));

            JArray results = new JArray();

            while (response.HasMoreResults)
            {
                foreach (var item in await response.ReadNextAsync())
                    results.Add(item);
            }
            return results;
        }

        public virtual async Task<dynamic> GetDataMasterServiceByKey(string modelId, string taskKey, string psTypeId, string workOrder, string rating)
        {
            string query = $"SELECT  task.key,   task.seqId,   task.taskType,   task.isActive,   task.isDeleted,   \"\" as updatedBy,   task.header,   task.description,   task.category,   task.rating,  \"\" as updatedDate,   task.groupTaskId,   IS_DEFINED(task.adjustment) ? task.adjustment[\"rating\"] <> \"\" ? [task.adjustment] :  task.items : IS_DEFINED(task.replacement) ? task.replacement[\"rating\"] <> \"\"?[task.replacement] : task.items : task.items as items,   \"\" as taskValue FROM c join subGroup in c.subGroup join taskGroup in subGroup.taskGroup join task in taskGroup.task ";
            if (rating != "2")
            {
                query += $"where     c.modelId = \"{modelId}\" and  task.key = \"{taskKey}\" and  c.psTypeId = \"{psTypeId}\" and c.workOrder = \"{workOrder}\" and c.isActive = \"true\" and c.isDeleted = \"false\"";
            }
            else
            {
                query += $"join items in task.items  where     c.modelId = \"{modelId}\" and  items.mappingKeyId = \"{taskKey}\" and  c.psTypeId = \"{psTypeId}\" and c.workOrder = \"{workOrder}\" and c.isActive = \"true\" and c.isDeleted = \"false\"";
            }
            var response = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(query));

            JArray results = new JArray();

            while (response.HasMoreResults)
            {
                foreach (var item in await response.ReadNextAsync())
                    results.Add(item);
            }
            return results;
        }

        public virtual async Task<dynamic> GetDataMasterServiceByCalculateKey(string workorder, string calculateKey)
        {
            string query = $"SELECT  task.key,   task.seqId,   task.taskType,   task.isActive,   task.isDeleted,   task.updatedBy,   task.header,   task.description,   task.category,   task.rating,   task.updatedDate,   task.groupTaskId,   task.items,   task.taskValue FROM c join subGroup in c.subGroup join taskGroup in subGroup.taskGroup join task in taskGroup.task where     c.workOrder = \"{workorder}\" and  task.items[3][\"targetCalculateKeyId\"] = \"{calculateKey}\" and c.isActive = \"true\" and c.isDeleted = \"false\"";

            var response = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(query));

            JArray results = new JArray();

            while (response.HasMoreResults)
            {
                foreach (var item in await response.ReadNextAsync())
                    results.Add(item);
            }
            return results;
        }

        public virtual async Task<dynamic> GetDataReplacementPhotosByKey(string workOrder, string groupTaskId)
        {
            string query = $"SELECT task.key, task.items FROM c join subGroup in c.subGroup join taskGroup in subGroup.taskGroup join task in taskGroup.task where c.workOrder = \"{workOrder}\" and  task.groupTaskId = \"{groupTaskId}\" and task.rating = 'CAB_SIDE'";

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
