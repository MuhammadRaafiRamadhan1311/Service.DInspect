using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service.DInspect.Interfaces;
using Service.DInspect.Models.Enum;
using Service.DInspect.Models.Request;
using Service.DInspect.Models.Response;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.DInspect.Repositories
{
    public class ServiceSheetDetailRepository : RepositoryBase
    {
        public ServiceSheetDetailRepository(IConnectionFactory connectionFactory, string container) : base(connectionFactory, container)
        {
        }

        public virtual async Task<JArray> GetDataCalibration(string workOrder)
        {
            var containerName = _container.Id;
            string query = $"SELECT task.key, task.taskType, task.showParameter, task.isActive, task.isDeleted, task.updatedBy, task.header, task.description, task.category, task.rating, task.updatedDate, task.groupTaskId, task.items FROM c join subGroup in c.subGroup join taskGroup in subGroup.taskGroup join task in taskGroup.task where c.workOrder = \"{workOrder}\" and c.key <> \"GENERAL\" and ((task.category = \"CBM\" and task.rating = \"CALIBRATION\") or (task.showParameter = \"cylinderHeightNeedAdjustment\"))";

            var response = await Task.Run(() => _container.GetItemQueryIterator<JObject>(query));

            JArray results = new JArray();

            while (response.HasMoreResults)
            {
                foreach (var item in await response.ReadNextAsync())
                    results.Add(item);
            }

            return results;
        }

        public virtual async Task<JArray> GetDataTandem(string workOrder)
        {
            string query = $"SELECT task.key, task.category, task.rating, items[\"key\"] as itemKey, items[\"value\"] as itemValue FROM c join subGroup in c.subGroup join taskGroup in subGroup.taskGroup join task in taskGroup.task join items in task.items where c.workOrder = \"{workOrder}\" and c.key <> \"GENERAL\" and task.category= \"CBM\" and task.rating IN (\"AUTOMATIC_PREVIOUS\", \"AUTOMATIC_PREVIOUS_GROUP\") and items.categoryItemType IN (\"paramRating\", \"paramRatingDynamic\")";

            var response = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(query));

            JArray results = new JArray();

            while (response.HasMoreResults)
            {
                foreach (var item in await response.ReadNextAsync())
                    results.Add(item);
            }

            return results;
        }

        public virtual async Task<JArray> GetRecomendedLubricant(string modelId, string workOrder, string psType)
        {
            string query = $"SELECT taskGroup.key as taskGroupKey, task.key, items[\"column1\"][\"value\"] as compartment, items[\"column2\"][\"value\"] as recomendedLubricant, CONTAINS(items[\"column3\"][\"value\"],' ') = true ? SUBSTRING(items[\"column3\"][\"value\"],0,INDEX_OF(items[\"column3\"][\"value\"],' ')) : items[\"column3\"][\"value\"] as oilStandart FROM c join subGroup in c.subGroup join taskGroup in subGroup.taskGroup join task in taskGroup.task join items in task.items where c.modelId = \"{modelId}\" and c.psTypeId = \"{psType}\" and c.key = \"{EnumGroup.LubeService}\" and taskGroup.key = \"{EnumQuery.RecomendationLubricant}\" and c.workOrder = \"{workOrder}\"";

            var response = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(query));

            JArray results = new JArray();

            while (response.HasMoreResults)
            {
                foreach (var item in await response.ReadNextAsync())
                    results.Add(item);
            }
            return results;
        }

        public virtual async Task<dynamic> GetOilCompartment(string modelId, string workOrder, string psType, string groupKey)
        {
            string query = $"SELECT taskGroup.key as taskGroupKey, task.key,task.taskValue as oilChange,task.items[1][\"value\"] as compartment,task.items[2][\"value\"] as oilStandart,task.items[3][\"itemType\"] = \"input\" ? task.items[3][\"value\"] : '' as oilAdded FROM c join subGroup in c.subGroup join taskGroup in subGroup.taskGroup join task in taskGroup.task join items in task.items where c.modelId =  \"{modelId}\" and c.psTypeId = \"{psType}\" and c.key = \"{EnumGroup.LubeService}\" and taskGroup.key = \"{groupKey}\" and task.description <> '' and c.workOrder = \"{workOrder}\" and items.itemType in ('label', 'html') and RegexMatch(items[\"value\"], \"[0-9]\") = false";

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
            string query = $"SELECT c.id, task.key, task.seqId, task.taskType, task.isActive, task.isDeleted, task.updatedBy, task.header, task.description, task.category, task.rating, task.updatedDate,        task.groupTaskId,     (IS_DEFINED(task.adjustment) and task.adjustment[\"rating\"] <> \"\") or (IS_DEFINED(task.replacement) and task.replacement[\"rating\"] <> \"\") ? true : false as cbmAdjustmentReplacement,       IS_DEFINED(task.adjustment) ? task.adjustment[\"pictures\"] = [] ? \"\" : task.adjustment[\"pictures\"] :     IS_DEFINED(task.replacement) ? task.replacement[\"pictures\"] = [] ? \"\" : task.replacement[\"pictures\"] :    \"\" as pictures,       task.items,        IS_DEFINED(task.adjustment) ? task.adjustment[\"rating\"] <> \"\" ? task.adjustment[\"rating\"] : task.taskValue :      IS_DEFINED(task.replacement) ? task.replacement[\"rating\"] <> \"\" ? task.replacement[\"rating\"] : task.taskValue : task.taskValue as taskValue,      task.items[0][\"value\"] as taskNo,      task.rating = \"MANUAL\" OR task.rating = \"NORMAL\"  ? \"\"          : task.items[3][\"categoryItemType\"] = \"dropdownTool\" ? task.items[5][\"value\"]         : task.items[3][\"categoryItemType\"] = \"brakeTypeDropdown\" ? task.items[6][\"value\"]         : task.items[4][\"categoryItemType\"] = \"dropdownToolDisc\" ? task.items[6][\"value\"] : task.items[4][\"categoryItemType\"] = \"dropdownTool\" ? task.items[6][\"value\"] : task.rating = \"AUTOMATIC\" AND  task.items[3][\"valueItemType\"] = \"comment\" ? task.items[5][\"value\"]  : task.rating = \"AUTOMATIC\" ? task.items[4][\"value\"]          : task.items[5][\"value\"] = [] ? \"\"          : task.items[5][\"categoryItemType\"] = \"dropdownTool\" ? task.items[7][\"value\"] : task.items[5][\"value\"] as uom,      task.rating = \"MANUAL\" OR task.rating = \"NORMAL\" ? \"\"          : task.items[3][\"categoryItemType\"] = \"dropdownTool\" ? task.items[4][\"value\"]         : task.items[3][\"categoryItemType\"] = \"brakeTypeDropdown\" ? task.items[5][\"value\"]         : task.items[4][\"categoryItemType\"] = \"dropdownToolDisc\" ? task.items[5][\"value\"]         : IS_DEFINED(task.adjustment) ? task.adjustment[\"measurement\"] <> \"\" ? task.adjustment[\"measurement\"]:task.items[3][\"value\"]       : IS_DEFINED(task.replacement) ? task.replacement[\"measurement\"] <> \"\" ? task.replacement[\"measurement\"] : task.items[4][\"value\"] : task.items[4][\"categoryItemType\"] = \"dropdownTool\" ? task.items[5][\"value\"]      : task.rating = \"AUTOMATIC\" ? IS_DEFINED(task.items[3][\"valueItemType\"]) ? task.items[4][\"value\"] : task.items[3][\"value\"]           : task.items[5][\"categoryItemType\"] = \"dropdownTool\" ? task.items[6][\"value\"] : task.items[4][\"value\"] as measurementValue, (IS_DEFINED(task.adjustment) and task.adjustment[\"rating\"] <> \"\") = true ? task.adjustment : (IS_DEFINED(task.replacement) and task.replacement[\"rating\"] <> \"\") = true ? task.replacement : \"\" as cbmAdjustmentReplacementValue, (IS_DEFINED(task.adjustment) and task.adjustment[\"rating\"] <> \"\") or (IS_DEFINED(task.replacement) and task.replacement[\"rating\"] <> \"\") = true ? task.items[3][\"value\"] : \"\"  as nonCbmAdjustmentReplacementMeasurementValue, (IS_DEFINED(task.adjustment) and task.adjustment[\"rating\"] <> \"\") or (IS_DEFINED(task.replacement) and task.replacement[\"rating\"] <> \"\") = true ? task.items[5][\"value\"] : \"\" as nonCbmAdjustmentReplacementRating, task.SectionData, IS_DEFINED(task.replacement) ? task.items[4][\"value\"] : task.items[5][\"categoryItemType\"] = \"dropdownTool\" ? task.items[6][\"value\"] : task.items[3][\"value\"] as currentValue,\r\n    IS_DEFINED(task.replacement) ? task.items[6][\"value\"] : task.items[5][\"categoryItemType\"] = \"dropdownTool\" ? task.items[8][\"value\"] : task.items[5][\"value\"] as currentRating,\r\n    IS_DEFINED(task.replacement) ? task.replacement.measurement : \"\" as replacementValue,\r\n    IS_DEFINED(task.replacement) ? task.replacement.rating : \"\" as replacementRating, IS_DEFINED(task.replacement) as taskReplacement FROM c join subGroup in c.subGroup join taskGroup in subGroup.taskGroup join task in taskGroup.task ";
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

        public virtual async Task<dynamic> GetDataServiceSheetDetailCurrent(DetailServiceSheet model)
        {
            string query = $"SELECT  c.id,  c.modelId,   c.psTypeId,   task.key,    task.category,   task.updatedDate, task.taskValue FROM c join subGroup in c.subGroup join taskGroup in subGroup.taskGroup join task in taskGroup.task join items in task.items where c.workOrder = \"{model.workOrder}\" and c.key <> 'GENERAL' and task.key <> 'safetyTask000000' and items[\"key\"] = \"{model.taskKey}\" order by c.groupSeq";

            var response = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(query));

            JArray results = new JArray();

            while (response.HasMoreResults)
            {
                foreach (var item in await response.ReadNextAsync())
                    results.Add(item);
            }
            return results;
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

        public virtual async Task<dynamic> GetDataServiceSheetProgress(ModelGetTaskProgress model)
        {
            string query = $"SELECT c.modelId, c.psTypeId, c.key as keyGroup, task.showParameter, task.key, task.category, task.rating, task.updatedBy, task.updatedDate, task.groupTaskId = null ? '' : task.groupTaskId as groupTaskId, task.taskValue, task.parentGroupTaskId, task.childGroupTaskId, task.items[3][\"disabledByItemKey\"] FROM c  join subGroup in c.subGroup  join taskGroup in subGroup.taskGroup  join task in taskGroup.task  where (c.modelId = \"{model.modelId}\" and      c.psTypeId = \"{model.psTypeId}\" and  c.workOrder = \"{model.workOrder}\" and  c.headerId = \"{model.headerId}\" and task.key <> 'safetyTask000000' and task.taskValue != null) or (c.modelId = \"{model.modelId}\" and      c.psTypeId = \"{model.psTypeId}\" and  c.workOrder = \"{model.workOrder}\" and  c.headerId = \"{model.headerId}\" and task.key <> 'safetyTask000000' and task.showParameter = 'cylinderHeightNeedAdjustment') order by c.groupSeq";

            var response = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(query));

            JArray results = new JArray();

            while (response.HasMoreResults)
            {
                foreach (var item in await response.ReadNextAsync())
                    results.Add(item);
            }
            return results;
        }

        public virtual async Task<dynamic> GetDataServiceSheetProgressPerTab(ModelGetTaskProgress model)
        {
            string query = $"SELECT c.modelId, c.psTypeId, c.key, (SELECT '' as name, c.key as key, (SELECT '' as name, c.key, '' as task) as taskGroup) as subGroup FROM c   where      c.modelId = \"{model.modelId}\" and c.psTypeId = \"{model.psTypeId}\" and c.workOrder = \"{model.workOrder}\" and c.headerId = \"{model.headerId}\"  order by c.groupSeq";

            var response = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(query));

            JArray results = new JArray();

            while (response.HasMoreResults)
            {
                foreach (var item in await response.ReadNextAsync())
                    results.Add(item);
            }
            return results;
        }

        public virtual async Task<JArray> GetDataReplacement(string workOrder)
        {
            string query = $"SELECT task.key, task.category, task.rating, items[\"key\"] as itemKey, items[\"value\"] as itemValue, task[\"replacement\"] as replacement FROM c join subGroup in c.subGroup join taskGroup in subGroup.taskGroup join task in taskGroup.task join items in task.items where c.workOrder = \"{workOrder}\" and c.key <> \"GENERAL\" and task.category= \"CBM\" and task.rating IN (\"AUTOMATIC_REPLACEMENT\", \"AUTOMATIC_REPLACEMENT_GAP\") and items.categoryItemType = \"paramRating\"";

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

        public virtual async Task<dynamic> GetCommentId(string workOrder, string rating)
        {
            string query = $"SELECT DISTINCT task.key, task.category, task.rating, task.commentId, task.items[17][\"value\"] as commentGroupTask FROM c join subGroup in c.subGroup join taskGroup in subGroup.taskGroup join task in taskGroup.task join items in task.items WHERE c.workOrder = \"{workOrder}\" and  task.rating = \"{rating}\"";

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
